using System.Collections.Generic;
using UnityEngine;
using Astrum.CommonBase;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.SkillSystem;
using Astrum.View.Managers;
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
        private GameObject _loopEffectInstance;

        // 配置化特效
        private string _spawnEffectPath = string.Empty;
        private string _loopEffectPath = string.Empty;
        private string _hitEffectPath = string.Empty;

        private GameObject _spawnEffectPrefab;
        private GameObject _loopEffectPrefab;
        private GameObject _hitEffectPrefab;
        private ProjectileDefinition _projectileDefinition;
        private ProjectileEffectOffsetView _spawnOffset = ProjectileEffectOffsetView.Identity;
        private ProjectileEffectOffsetView _loopOffset = ProjectileEffectOffsetView.Identity;
        private ProjectileEffectOffsetView _hitOffset = ProjectileEffectOffsetView.Identity;
        private bool _spawnEffectPlayed;
        private readonly List<GameObject> _runtimeEffectInstances = new List<GameObject>();
        private bool _loopEffectFromConfig;

        private struct ProjectileEffectOffsetView
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;

            public static ProjectileEffectOffsetView Identity => new ProjectileEffectOffsetView
            {
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                Scale = Vector3.one
            };
        }

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
            LoadEffectConfigurationFromTable();

            if (!TrySetupLoopEffectFromConfig())
            {
                TryFindExistingLoopEffect();
                EnsureTrailRendererExists();
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

            if (!TransComponent.TryGetViewRead(OwnerEntity.World, OwnerEntity.UniqueId, out var transRead) || !transRead.IsValid)
                return;

            var logicPos = transRead.Position;
            Vector3 currentLogicPosition = new Vector3((float)logicPos.x, (float)logicPos.y, (float)logicPos.z);

            // 首次更新：建立初始偏移
            if (!_visualSync.isInitialized)
            {
                InitializeVisualPosition(currentLogicPosition);
                return;
            }

            // 持续更新：插值追赶逻辑位置
            UpdateVisualPosition(currentLogicPosition, deltaTime);

            // 同步 GameObject 位置和旋转
            if (_ownerEntityView != null)
            {
                _ownerEntityView.SetWorldPosition(_visualSync.visualPosition);
                
                // 更新特效朝向（每帧）
                UpdateEffectsRotation();
            }

            // 记录本帧逻辑位置
            _visualSync.lastLogicPosition = currentLogicPosition;
            _visualSync.timeSinceLastLogicUpdate += deltaTime;
        }

        protected override void OnDestroy()
        {
            StopAllEffects();
            CleanupRuntimeInstances(true);

            if (_loopEffectInstance != null && _loopEffectFromConfig)
            {
                UnityEngine.Object.Destroy(_loopEffectInstance);
                _loopEffectInstance = null;
            }

            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }
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

            if (!_spawnEffectPlayed)
            {
                var forward = GetProjectileLaunchDirection();
                TryPlaySpawnEffect(_visualSync.visualPosition, forward);
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
            var worldPos = ToVector3(hitPosition);
            bool played = false;

            if (!string.IsNullOrWhiteSpace(_hitEffectPath))
            {
                var prefab = LoadEffectPrefab(ref _hitEffectPrefab, _hitEffectPath);
                if (prefab != null)
                {
                    var forward = GetProjectileLaunchDirection();
                    var instance = InstantiateEffect(prefab, worldPos, forward, _hitOffset);
                    played = instance != null;
                }
            }

            if (!played && _hitEffect != null)
            {
                var forward = GetProjectileLaunchDirection();
                var orientation = Quaternion.LookRotation(forward == Vector3.zero ? Vector3.forward : forward, Vector3.up);
                var offsetPosition = worldPos + orientation * _hitOffset.Position;
                _hitEffect.transform.position = offsetPosition;
                _hitEffect.transform.rotation = orientation * _hitOffset.Rotation;
                _hitEffect.transform.localScale = Vector3.Scale(Vector3.one, _hitOffset.Scale);
                _hitEffect.Play();
                played = true;
            }

            if (played)
            {
                StopAllEffects();
            }
        }

        /// <summary>
        /// 重置视觉状态（用于对象池回收）
        /// </summary>
        public void ResetVisual()
        {
            StopAllEffects();

            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                _trailRenderer.emitting = true;
            }

            if (_loopEffect != null)
            {
                _loopEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _loopEffect.Clear();
            }

            if (_hitEffect != null)
            {
                _hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _hitEffect.Clear();
            }

            CleanupRuntimeInstances(true);
            _spawnEffectPlayed = false;

            _visualSync.isInitialized = false;
            _visualSync.initialVisualSpawnPos = Vector3.zero;
            _visualSync.initialLogicSpawnPos = Vector3.zero;
            _visualSync.visualPosition = Vector3.zero;
            _visualSync.lastLogicPosition = Vector3.zero;
            _visualSync.timeSinceLastLogicUpdate = 0f;
        }

        /// <summary>
        /// 停止所有特效
        /// </summary>
        private void StopAllEffects()
        {
            StopLoopEffect();
            CleanupRuntimeInstances(); // 清理已失效的实例，但保留正在播放的命中/开火特效
        }

        private void StopLoopEffect()
        {
            if (_loopEffect != null)
            {
                _loopEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (_trailRenderer != null)
            {
                _trailRenderer.emitting = false;
            }
        }

        private void CleanupRuntimeInstances(bool destroyAll = false)
        {
            for (int i = _runtimeEffectInstances.Count - 1; i >= 0; i--)
            {
                var instance = _runtimeEffectInstances[i];
                if (instance == null)
                {
                    _runtimeEffectInstances.RemoveAt(i);
                    continue;
                }

                if (destroyAll)
                {
                    UnityEngine.Object.Destroy(instance);
                    _runtimeEffectInstances.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 从配置表加载特效资源路径
        /// </summary>
        private void LoadEffectConfigurationFromTable()
        {
            if (OwnerEntity == null || !ProjectileComponent.TryGetViewRead(OwnerEntity.World, OwnerEntity.UniqueId, out var read) || !read.IsValid || read.ProjectileId <= 0)
            {
                _spawnEffectPath = string.Empty;
                _loopEffectPath = string.Empty;
                _hitEffectPath = string.Empty;
                return;
            }

            // 通过 ProjectileId 查询配置表
            var definition = ProjectileConfigManager.Instance.GetDefinition(read.ProjectileId);
            if (definition == null)
            {
                ASLogger.Instance.Warning($"ProjectileViewComponent: ProjectileDefinition not found for ID {read.ProjectileId}");
                _spawnEffectPath = string.Empty;
                _loopEffectPath = string.Empty;
                _hitEffectPath = string.Empty;
                _projectileDefinition = null;
                _spawnOffset = ProjectileEffectOffsetView.Identity;
                _loopOffset = ProjectileEffectOffsetView.Identity;
                _hitOffset = ProjectileEffectOffsetView.Identity;
                return;
            }

            _projectileDefinition = definition;
            _spawnEffectPath = definition.SpawnEffectPath ?? string.Empty;
            _loopEffectPath = definition.LoopEffectPath ?? string.Empty;
            _hitEffectPath = definition.HitEffectPath ?? string.Empty;
            _spawnOffset = ConvertOffset(definition.SpawnEffectOffset);
            _loopOffset = ConvertOffset(definition.LoopEffectOffset);
            _hitOffset = ConvertOffset(definition.HitEffectOffset);
        }

        private bool TrySetupLoopEffectFromConfig()
        {
            if (string.IsNullOrWhiteSpace(_loopEffectPath) || _ownerEntityView?.GameObject == null)
            {
                return false;
            }

            var prefab = LoadEffectPrefab(ref _loopEffectPrefab, _loopEffectPath);
            if (prefab == null)
            {
                return false;
            }

            if (_loopEffectInstance != null)
            {
                UnityEngine.Object.Destroy(_loopEffectInstance);
                _loopEffectInstance = null;
            }

            _loopEffectInstance = UnityEngine.Object.Instantiate(prefab, _ownerEntityView.GameObject.transform);
            _loopEffectInstance.name = "ProjectileLoopEffect";
            ApplyLocalOffset(_loopEffectInstance.transform, _loopOffset);

            _loopEffect = _loopEffectInstance.GetComponentInChildren<ParticleSystem>();
            _trailRenderer = _loopEffectInstance.GetComponentInChildren<TrailRenderer>() ?? _trailRenderer;
            _loopEffectFromConfig = true;

            if (_loopEffect != null && !_loopEffect.isPlaying)
            {
                _loopEffect.Play();
            }

            return true;
        }

        private void TryFindExistingLoopEffect()
        {
            var root = _ownerEntityView?.GameObject;
            if (root == null)
            {
                return;
            }

            if (_trailRenderer == null)
            {
                _trailRenderer = root.GetComponentInChildren<TrailRenderer>();
            }

            if (_loopEffect == null)
            {
                var particleSystems = root.GetComponentsInChildren<ParticleSystem>();
                if (particleSystems != null && particleSystems.Length > 0)
                {
                    _loopEffect = particleSystems[0];
                }
            }
        }

        private void EnsureTrailRendererExists()
        {
            if (_trailRenderer == null && _ownerEntityView?.GameObject != null)
            {
                var trailObj = new GameObject("ProjectileTrail");
                trailObj.transform.SetParent(_ownerEntityView.GameObject.transform, false);
                _trailRenderer = trailObj.AddComponent<TrailRenderer>();
                ConfigureDefaultTrail(_trailRenderer);
            }
        }

        /// <summary>
        /// 获取子弹的飞行方向（从逻辑层）
        /// </summary>
        private Vector3 GetProjectileLaunchDirection()
        {
            if (OwnerEntity != null && ProjectileComponent.TryGetViewRead(OwnerEntity.World, OwnerEntity.UniqueId, out var read) && read.IsValid)
            {
                // 优先使用当前速度方向（更准确反映实时飞行方向）
                if (read.CurrentVelocity.sqrMagnitude > FP.Epsilon)
                {
                    var velocity = read.CurrentVelocity;
                    return new Vector3((float)velocity.x, (float)velocity.y, (float)velocity.z).normalized;
                }

                // 回退到初始发射方向
                var launchDir = read.LaunchDirection;
                return new Vector3((float)launchDir.x, (float)launchDir.y, (float)launchDir.z).normalized;
            }

            // 最终回退：使用 TransComponent 的朝向
            if (OwnerEntity != null && 
                TransComponent.TryGetViewRead(OwnerEntity.World, OwnerEntity.UniqueId, out var transRead) && 
                transRead.IsValid)
            {
                var forward = transRead.Rotation * TSVector.forward;
                return new Vector3((float)forward.x, (float)forward.y, (float)forward.z).normalized;
            }

            return Vector3.forward;
        }

        /// <summary>
        /// 更新特效朝向（每帧调用）
        /// </summary>
        private void UpdateEffectsRotation()
        {
            var direction = GetProjectileLaunchDirection();

            // 更新循环特效朝向
            if (_loopEffect != null && _loopEffect.gameObject != null)
            {
                _loopEffect.transform.forward = direction;
            }

            // 更新 EntityView GameObject 的朝向（如果有拖尾等绑定在上面的特效）
            if (_ownerEntityView?.GameObject != null)
            {
                _ownerEntityView.GameObject.transform.rotation = Quaternion.LookRotation(direction == Vector3.zero ? Vector3.forward : direction, Vector3.up);
            }
        }

        private void TryPlaySpawnEffect(Vector3 position, Vector3 forward)
        {
            if (_spawnEffectPlayed || string.IsNullOrWhiteSpace(_spawnEffectPath))
            {
                return;
            }

            var prefab = LoadEffectPrefab(ref _spawnEffectPrefab, _spawnEffectPath);
            if (prefab == null)
            {
                return;
            }

            var instance = InstantiateEffect(prefab, position, forward, _spawnOffset);
            if (instance != null)
            {
                _spawnEffectPlayed = true;
            }
        }

        private GameObject InstantiateEffect(GameObject prefab, Vector3 position, Vector3 forward, ProjectileEffectOffsetView offset)
        {
            if (prefab == null)
            {
                return null;
            }

            var normalizedForward = forward.sqrMagnitude > Mathf.Epsilon ? forward.normalized : Vector3.forward;
            var baseRotation = Quaternion.LookRotation(normalizedForward, Vector3.up);
            var worldPosition = position + baseRotation * offset.Position;
            var worldRotation = baseRotation * offset.Rotation;

            var instance = UnityEngine.Object.Instantiate(prefab);
            instance.transform.position = worldPosition;
            instance.transform.rotation = worldRotation;
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, offset.Scale);

            _runtimeEffectInstances.Add(instance);
            ScheduleAutoDestroy(instance);
            return instance;
        }

        private GameObject LoadEffectPrefab(ref GameObject cache, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (cache != null)
            {
                return cache;
            }

            var prefab = ResourceManager.Instance.LoadResource<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"ProjectileViewComponent: Failed to load effect prefab at path '{path}'.");
                return null;
            }

            cache = prefab;
            return cache;
        }

        private void ScheduleAutoDestroy(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            float lifetime = 3f;
            var particle = instance.GetComponentInChildren<ParticleSystem>();
            if (particle != null)
            {
                var main = particle.main;
                float startLifetimeMax = 0f;
                switch (main.startLifetime.mode)
                {
                    case ParticleSystemCurveMode.Constant:
                        startLifetimeMax = main.startLifetime.constant;
                        break;
                    case ParticleSystemCurveMode.TwoConstants:
                        startLifetimeMax = main.startLifetime.constantMax;
                        break;
                    case ParticleSystemCurveMode.TwoCurves:
                    case ParticleSystemCurveMode.Curve:
                        startLifetimeMax = Mathf.Max(main.startLifetime.Evaluate(0f), main.startLifetime.Evaluate(1f));
                        break;
                }

                lifetime = Mathf.Max(lifetime, main.duration + startLifetimeMax);
            }

            UnityEngine.Object.Destroy(instance, lifetime);
        }

        private static Vector3 ToVector3(TSVector value)
        {
            return new Vector3((float)value.x, (float)value.y, (float)value.z);
        }

        private static ProjectileEffectOffsetView ConvertOffset(ProjectileEffectOffsetData data)
        {
            if (data == null)
            {
                return ProjectileEffectOffsetView.Identity;
            }

            var position = ToVector3(data.Position);
            var rotationEuler = ToVector3(data.Rotation);
            var scale = ToVector3(data.Scale);

            if (Mathf.Approximately(scale.x, 0f)) scale.x = 1f;
            if (Mathf.Approximately(scale.y, 0f)) scale.y = 1f;
            if (Mathf.Approximately(scale.z, 0f)) scale.z = 1f;

            return new ProjectileEffectOffsetView
            {
                Position = position,
                Rotation = Quaternion.Euler(rotationEuler),
                Scale = scale
            };
        }

        private static void ApplyLocalOffset(Transform target, ProjectileEffectOffsetView offset)
        {
            if (target == null)
            {
                return;
            }

            target.localPosition = offset.Position;
            target.localRotation = offset.Rotation;
            target.localScale = Vector3.Scale(target.localScale, offset.Scale);
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

            if (OwnerEntity == null || !ProjectileComponent.TryGetViewRead(OwnerEntity.World, OwnerEntity.UniqueId, out var read) || !read.IsValid)
            {
                return false;
            }

            var socketName = read.SocketName;
            if (string.IsNullOrWhiteSpace(socketName))
            {
                return false;
            }

            var stage = _ownerEntityView?.Stage;
            if (stage == null)
            {
                return false;
            }

            // CasterId 不在 ViewRead 中，需要通过其他方式获取或移除此功能
            // 暂时跳过此功能，因为 CasterId 不是 View 层必需的显示数据
            // TODO: 如果需要 Socket 挂点功能，需要将 CasterId 添加到 ViewRead 或使用其他方式获取
            return false;
            
            /*
            if (OwnerEntity == null || !ProjectileComponent.TryGetViewRead(OwnerEntity.World, OwnerEntity.UniqueId, out var read) || !read.IsValid)
            {
                return false;
            }
            var casterView = stage.GetEntityView(read.CasterId);
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
            */
        }
    }
}

