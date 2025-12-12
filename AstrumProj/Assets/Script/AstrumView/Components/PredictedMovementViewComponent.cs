using UnityEngine;
using Astrum.CommonBase;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using TrueSync;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Astrum.View.Components
{
    /// <summary>
    /// 预测移动表现组件：
    /// - 逻辑层提供权威位置与 IsMoving（MovementComponent.IsMoving）
    /// - 表现层仅在 IsMoving=true 时更新位置；静止时不做拉回，避免被纠偏拉回
    /// - 若逻辑帧停更但仍处于移动状态：按缓存的速度/朝向惯性前进，纠偏只做“横向回轨”（不后拉）
    /// </summary>
    public sealed class PredictedMovementViewComponent : ViewComponent
    {
        // ===== 可调参数（非 MonoBehaviour，主要用于运行时调试/配置）=====

        public float wLogicDirection = 1.0f;
        public float wCorrectionDirection = 0.3f;
        public float accel = 10f;
        public float posFixLerp = 0.5f;

        /// <summary>
        /// 极端保护：当视觉与逻辑偏差过大时，允许瞬移对齐（即使静止也允许）。
        /// 用户确认只允许这一类例外。
        /// </summary>
        public float hardSnapDistance = 5f;

        /// <summary>
        /// 计算逻辑方向的最小位移阈值（避免噪声导致方向抖动）
        /// </summary>
        public float minLogicDeltaForDir = 0.001f;

        // ===== 表现层内部状态 =====
        private Vector3 _posVisual;
        private Vector3 _dirVisual = Vector3.forward;
        private float _speedVisual;

        // ===== 逻辑输入缓存（避免在逻辑帧停更时使用旧 posLogic 方向纠偏后拉）=====
        private int _lastLogicFrameSeen = int.MinValue;
        private Vector3 _lastPosLogicSeen;
        private Vector3 _cachedDirLogic = Vector3.forward;
        private float _cachedSpeedLogic;

        // 由脏组件同步驱动的权威移动状态
        private bool _isMovingLogicCached;

        public override int[] GetWatchedComponentIds()
        {
            return new[] { MovementComponent.ComponentTypeId };
        }

        public override void SyncDataFromComponent(int componentTypeId)
        {
            if (OwnerEntity == null)
                return;

            if (componentTypeId != MovementComponent.ComponentTypeId)
                return;

            var move = OwnerEntity.GetComponent<MovementComponent>();
            if (move == null)
                return;

            _isMovingLogicCached = move.IsMoving;
        }

        protected override void OnInitialize()
        {
            if (_ownerEntityView == null)
                return;

            _posVisual = _ownerEntityView.GetWorldPosition();

            var entity = OwnerEntity;
            if (entity == null)
                return;

            // Projectile 由 ProjectileViewComponent 自己驱动位置/朝向，这里完全不介入
            if (entity.GetComponent<ProjectileComponent>() != null)
                return;

            var trans = entity.GetComponent<TransComponent>();
            if (trans != null)
            {
                _lastPosLogicSeen = ToVector3(trans.Position);
                _cachedDirLogic = ToVector3(trans.Forward);
                if (_cachedDirLogic.sqrMagnitude > 1e-8f)
                    _cachedDirLogic.Normalize();
            }
            else
            {
                _lastPosLogicSeen = _posVisual;
            }

            _lastLogicFrameSeen = entity.World?.CurFrame ?? int.MinValue;

            var moveComp = entity.GetComponent<MovementComponent>();
            if (moveComp != null)
            {
                _cachedSpeedLogic = moveComp.Speed.AsFloat();
                _speedVisual = _cachedSpeedLogic;
                _isMovingLogicCached = moveComp.IsMoving;
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (!_isEnabled || _ownerEntityView == null)
                return;

            var entity = OwnerEntity;
            if (entity == null)
                return;

            var trans = entity.GetComponent<TransComponent>();
            if (trans == null)
                return;

            // 内部状态同步：不修改 transform，仅同步缓存
            _posVisual = _ownerEntityView.GetWorldPosition();

            var posLogic = ToVector3(trans.Position);
            var logicFrame = entity.World?.CurFrame ?? _lastLogicFrameSeen;
            var logicFrameAdvanced = logicFrame != _lastLogicFrameSeen;

            // speed 允许轻量轮询（不依赖输入），避免 dash/属性变化时长期不更新
            var moveComp = entity.GetComponent<MovementComponent>();
            if (moveComp != null)
            {
                _cachedSpeedLogic = moveComp.Speed.AsFloat();
                //ASLogger.Instance.Info($"speed: {_cachedSpeedLogic}");
            }

            // 旋转始终按逻辑层权威同步（不参与“静止不拉回”的限制）
            ApplyRotationFromLogic(trans);

            // 静止：不更新位置（避免拉回），仅允许偏差过大时硬对齐
            if (!_isMovingLogicCached)
            {
                if ((_posVisual - posLogic).magnitude > hardSnapDistance)
                {
                    _posVisual = posLogic;
                    _ownerEntityView.SetWorldPosition(_posVisual);
                }
                return;
            }

            // 逻辑帧推进：更新缓存逻辑方向（差分），并进行常规纠偏合成
            if (logicFrameAdvanced)
            {
                var deltaLogic = posLogic - _lastPosLogicSeen;
                if (deltaLogic.sqrMagnitude > (minLogicDeltaForDir * minLogicDeltaForDir))
                {
                    _cachedDirLogic = deltaLogic.normalized;
                }
                else
                {
                    // 兜底：使用逻辑朝向 forward
                    var fwd = ToVector3(trans.Forward);
                    if (fwd.sqrMagnitude > 1e-8f)
                        _cachedDirLogic = fwd.normalized;
                }

                _lastPosLogicSeen = posLogic;
                _lastLogicFrameSeen = logicFrame;

                _speedVisual = _cachedSpeedLogic;//Mathf.MoveTowards(_speedVisual, _cachedSpeedLogic, accel * deltaTime);

                var correction = posLogic - _posVisual;
                var dirMove = _cachedDirLogic * wLogicDirection;
                if (correction.sqrMagnitude > 1e-8f)
                    dirMove += correction.normalized * wCorrectionDirection;

                if (dirMove.sqrMagnitude < 1e-8f)
                    dirMove = _cachedDirLogic.sqrMagnitude > 1e-8f ? _cachedDirLogic : _dirVisual;

                dirMove.Normalize();
                _dirVisual = dirMove;

                _posVisual += _dirVisual * _speedVisual * deltaTime;
                _posVisual = Vector3.Lerp(_posVisual, posLogic, posFixLerp * deltaTime);

                _ownerEntityView.SetWorldPosition(_posVisual);
                return;
            }

            // 逻辑帧停更：保持惯性前进，纠偏仅做横向回轨（剔除沿运动方向的误差分量，避免后拉）
            _posVisual += _dirVisual * _speedVisual * deltaTime;

            {
                var error = posLogic - _posVisual;
                var errorPerp = error - _dirVisual * Vector3.Dot(error, _dirVisual);

                var steer = _dirVisual * wLogicDirection;
                if (errorPerp.sqrMagnitude > 1e-8f)
                    steer += errorPerp.normalized * wCorrectionDirection;

                if (steer.sqrMagnitude > 1e-8f)
                    _dirVisual = steer.normalized;
            }

            if ((_posVisual - posLogic).magnitude > hardSnapDistance)
            {
                _posVisual = posLogic;
            }

            _ownerEntityView.SetWorldPosition(_posVisual);
        }

        protected override void OnDestroy()
        {
        }

        protected override void OnSyncData(object data)
        {
            // 使用脏组件同步（SyncDataFromComponent），不通过事件数据同步
        }

        protected override void OnReset()
        {
            // 不直接对齐到逻辑位置：保持当前渲染位置，避免 reset 导致瞬移/拉回
            if (_ownerEntityView == null)
                return;

            _posVisual = _ownerEntityView.GetWorldPosition();

            var entity = OwnerEntity;
            if (entity == null)
                return;

            if (entity.GetComponent<ProjectileComponent>() != null)
                return;

            var trans = entity.GetComponent<TransComponent>();
            if (trans != null)
            {
                _lastPosLogicSeen = ToVector3(trans.Position);
                _cachedDirLogic = ToVector3(trans.Forward);
                if (_cachedDirLogic.sqrMagnitude > 1e-8f)
                    _cachedDirLogic.Normalize();
            }
            else
            {
                _lastPosLogicSeen = _posVisual;
            }

            _lastLogicFrameSeen = entity.World?.CurFrame ?? int.MinValue;

            var moveComp = entity.GetComponent<MovementComponent>();
            if (moveComp != null)
            {
                _cachedSpeedLogic = moveComp.Speed.AsFloat();
                _speedVisual = _cachedSpeedLogic;
            }

            // 方向保持为当前视觉方向；如果无效则用缓存逻辑方向兜底
            if (_dirVisual.sqrMagnitude < 1e-8f)
                _dirVisual = _cachedDirLogic.sqrMagnitude > 1e-8f ? _cachedDirLogic : Vector3.forward;
        }

        private static Vector3 ToVector3(TSVector v)
        {
            return new Vector3((float)v.x, (float)v.y, (float)v.z);
        }

        private void ApplyRotationFromLogic(TransComponent trans)
        {
            if (_ownerEntityView == null || trans == null)
                return;

            var r = trans.Rotation;
            var logicRotation = new Quaternion((float)r.x, (float)r.y, (float)r.z, (float)r.w);
            _ownerEntityView.SetWorldRotation(logicRotation);
        }
    }
}

