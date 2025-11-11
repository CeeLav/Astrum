using UnityEngine;
using Astrum.CommonBase;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using TrueSync;

namespace Astrum.View.Components
{
    /// <summary>
    /// 投射物表现层组件
    /// 负责投射物的视觉效果（拖尾、粒子、命中特效）和逻辑-表现层位置同步
    /// </summary>
    public sealed class ProjectileViewComponent : ViewComponent
    {
        // 特效引用
        private TrailRenderer _trailRenderer;
        private ParticleSystem _loopEffect;
        private ParticleSystem _hitEffect;

        // 视觉同步数据
        private struct VisualSyncData
        {
            public Vector3 visualPosition;          // 当前视觉位置
            public Vector3 lastLogicPosition;       // 上一帧逻辑位置
            public Vector3 initialVisualSpawnPos;   // 初始视觉出射点（来自 SocketRefs）
            public Vector3 initialLogicSpawnPos;    // 初始逻辑出射点（来自 Entity.TransComponent）
            public bool isInitialized;              // 是否已初始化
            public float timeSinceLastLogicUpdate;  // 距上次逻辑更新的时间
        }

        private VisualSyncData _visualSync;

        // 同步配置
        [SerializeField]
        private float _catchUpSpeed = 10f;          // 追赶速度（单位/秒）

        [SerializeField]
        private float _maxCatchUpDistance = 2f;     // 最大追赶距离，超过则直接瞬移

        protected override void OnInitialize()
        {
            // 查找或创建拖尾组件
            _trailRenderer = _ownerEntityView?.GameObject?.GetComponentInChildren<TrailRenderer>();
            if (_trailRenderer == null && _ownerEntityView?.GameObject != null)
            {
                var trailObj = new GameObject("Trail");
                trailObj.transform.SetParent(_ownerEntityView.GameObject.transform, false);
                _trailRenderer = trailObj.AddComponent<TrailRenderer>();
                ConfigureDefaultTrail(_trailRenderer);
            }

            // 查找循环特效（可选）
            var effectTransforms = _ownerEntityView?.GameObject?.GetComponentsInChildren<Transform>();
            if (effectTransforms != null)
            {
                foreach (var t in effectTransforms)
                {
                    if (t.name.Contains("LoopEffect") || t.name.Contains("Particle"))
                    {
                        _loopEffect = t.GetComponent<ParticleSystem>();
                        if (_loopEffect != null)
                            break;
                    }
                }
            }

            // 初始化同步数据
            _visualSync = new VisualSyncData
            {
                isInitialized = false,
                timeSinceLastLogicUpdate = 0f
            };
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (!_isEnabled || OwnerEntity == null)
                return;

            var transComponent = OwnerEntity.GetComponent<TransComponent>();
            if (transComponent == null)
                return;

            var logicPos = transComponent.Position;
            Vector3 currentLogicPosition = new Vector3((float)logicPos.x, (float)logicPos.y, (float)logicPos.z);

            // 首次更新：建立初始偏移
            if (!_visualSync.isInitialized)
            {
                InitializeVisualPosition(currentLogicPosition);
                return;
            }

            // 持续更新：插值追赶逻辑位置
            UpdateVisualPosition(currentLogicPosition, deltaTime);

            // 同步 GameObject 位置
            if (_ownerEntityView != null)
            {
                _ownerEntityView.SetWorldPosition(_visualSync.visualPosition);
            }

            // 记录本帧逻辑位置
            _visualSync.lastLogicPosition = currentLogicPosition;
            _visualSync.timeSinceLastLogicUpdate += deltaTime;
        }

        protected override void OnDestroy()
        {
            StopAllEffects();
        }

        protected override void OnSyncData(object data)
        {
            // 预留：用于接收逻辑层的同步数据（如命中事件）
        }

        /// <summary>
        /// 初始化视觉位置（首次调用）
        /// </summary>
        private void InitializeVisualPosition(Vector3 currentLogicPosition)
        {
            _visualSync.initialVisualSpawnPos = currentLogicPosition;

            if (TryResolveInitialVisualSpawnPosition(out var resolvedPosition))
            {
                _visualSync.initialVisualSpawnPos = resolvedPosition;
            }

            _visualSync.initialLogicSpawnPos = currentLogicPosition;
            _visualSync.visualPosition = _visualSync.initialVisualSpawnPos;
            _visualSync.lastLogicPosition = currentLogicPosition;
            _visualSync.isInitialized = true;
            _visualSync.timeSinceLastLogicUpdate = 0f;

            // 立即同步到 GameObject
            if (_ownerEntityView != null)
            {
                _ownerEntityView.SetWorldPosition(_visualSync.visualPosition);
            }

            // 启动循环特效
            if (_loopEffect != null && !_loopEffect.isPlaying)
            {
                _loopEffect.Play();
            }
        }

        /// <summary>
        /// 更新视觉位置（每帧调用）
        /// </summary>
        private void UpdateVisualPosition(Vector3 currentLogicPosition, float deltaTime)
        {
            // 计算逻辑层的位移
            var logicDelta = currentLogicPosition - _visualSync.lastLogicPosition;

            // 计算初始偏移（视觉出射点 - 逻辑出射点）
            var initialOffset = _visualSync.initialVisualSpawnPos - _visualSync.initialLogicSpawnPos;

            // 目标视觉位置 = 当前逻辑位置 + 初始偏移
            var targetVisualPosition = currentLogicPosition + initialOffset;

            // 计算当前视觉位置与目标的距离
            var distance = Vector3.Distance(_visualSync.visualPosition, targetVisualPosition);

            // 如果距离过大，直接瞬移（避免视觉穿模）
            if (distance > _maxCatchUpDistance)
            {
                _visualSync.visualPosition = targetVisualPosition;
            }
            else
            {
                // 否则平滑插值追赶
                _visualSync.visualPosition = Vector3.MoveTowards(
                    _visualSync.visualPosition,
                    targetVisualPosition,
                    _catchUpSpeed * deltaTime
                );
            }
        }

        /// <summary>
        /// 播放命中特效
        /// </summary>
        /// <param name="hitPosition">命中位置（逻辑层坐标）</param>
        public void PlayHitEffect(TSVector hitPosition)
        {
            if (_hitEffect != null)
            {
                var worldPos = new Vector3((float)hitPosition.x, (float)hitPosition.y, (float)hitPosition.z);
                _hitEffect.transform.position = worldPos;
                _hitEffect.Play();
            }

            // 停止拖尾
            if (_trailRenderer != null)
            {
                _trailRenderer.emitting = false;
            }

            // 停止循环特效
            if (_loopEffect != null && _loopEffect.isPlaying)
            {
                _loopEffect.Stop();
            }
        }

        /// <summary>
        /// 重置视觉状态（用于对象池回收）
        /// </summary>
        public void ResetVisual()
        {
            _visualSync = new VisualSyncData
            {
                isInitialized = false,
                timeSinceLastLogicUpdate = 0f
            };

            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                _trailRenderer.emitting = true;
            }

            if (_loopEffect != null)
            {
                _loopEffect.Stop();
                _loopEffect.Clear();
            }

            if (_hitEffect != null)
            {
                _hitEffect.Stop();
                _hitEffect.Clear();
            }
        }

        /// <summary>
        /// 停止所有特效
        /// </summary>
        private void StopAllEffects()
        {
            if (_trailRenderer != null)
            {
                _trailRenderer.emitting = false;
            }

            if (_loopEffect != null && _loopEffect.isPlaying)
            {
                _loopEffect.Stop();
            }

            if (_hitEffect != null && _hitEffect.isPlaying)
            {
                _hitEffect.Stop();
            }
        }

        /// <summary>
        /// 配置默认拖尾效果
        /// </summary>
        private void ConfigureDefaultTrail(TrailRenderer trail)
        {
            trail.time = 0.5f;
            trail.startWidth = 0.2f;
            trail.endWidth = 0.05f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(1f, 0.5f, 0f, 1f);
            trail.endColor = new Color(1f, 0.5f, 0f, 0f);
        }

        private bool TryResolveInitialVisualSpawnPosition(out Vector3 position)
        {
            position = Vector3.zero;

            var projectileComponent = OwnerEntity?.GetComponent<ProjectileComponent>();
            if (projectileComponent == null)
            {
                return false;
            }

            var socketName = projectileComponent.SocketName;
            if (string.IsNullOrWhiteSpace(socketName))
            {
                return false;
            }

            var stage = _ownerEntityView?.Stage;
            if (stage == null)
            {
                return false;
            }

            var casterView = stage.GetEntityView(projectileComponent.CasterId);
            if (casterView?.GameObject == null)
            {
                return false;
            }

            var socketRefs = casterView.GameObject.GetComponent<SocketRefs>();
            if (socketRefs == null)
            {
                return false;
            }

            if (socketRefs.TryGetWorldPosition(socketName, out var socketPosition, out _))
            {
                position = socketPosition;
                return true;
            }

            return false;
        }
    }
}

