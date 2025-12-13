using UnityEngine;
using Astrum.CommonBase;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.ActionSystem;
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
        public float wCorrectionDirection = 0.1f;
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
        
        /// <summary>
        /// 预测未来位置的帧数（用于平滑纠偏）
        /// </summary>
        public int predictionFrames = 10;

        // ===== 表现层内部状态 =====
        private Vector3 _posVisual;
        private Vector3 _dirVisual = Vector3.forward;
        private float _speedVisual;

        // 逻辑输入缓存（避免在逻辑帧停更时使用旧 posLogic 方向纠偏后拉）=====
        private int _lastLogicFrameSeen = int.MinValue;
        private Vector3 _lastPosLogicSeen;
        // 逻辑侧“移动方向”缓存（注意：不是角色朝向）
        private Vector3 _cachedDirLogic = Vector3.forward;
        private float _cachedSpeedLogic;

        // 由脏组件同步驱动的权威移动状态
        private bool _isMovingLogicCached;
        private MovementType _currentMovementTypeCached;

        // RootMotion 相关状态 =====
        private Vector3 _visualOffset;

        // 动画相关
        private Animator _cachedAnimator;

        // RootMotion 参数（可在 Inspector 中调整）
        public float motionBlendWeight = 1.0f;  // 动画 RootMotion 权重

        public override int[] GetWatchedComponentIds()
        {
            return new[] { MovementComponent.ComponentTypeId };
        }

        public override void SyncDataFromComponent(int componentTypeId)
        {
            if (OwnerEntity == null)
                return;

            if (componentTypeId == MovementComponent.ComponentTypeId)
            {
                var move = OwnerEntity.GetComponent<MovementComponent>();
                if (move == null)
                    return;

                _isMovingLogicCached = move.CurrentMovementType != MovementType.None;
                _currentMovementTypeCached = move.CurrentMovementType;
            }
        }

        /// <summary>
        /// 获取 Animator 引用
        /// </summary>
        /// <returns>Animator 组件引用，如果不存在则返回 null</returns>
        private Animator GetAnimator()
        {
            // 如果已经缓存了 Animator，直接返回
            if (_cachedAnimator != null)
            {
                return _cachedAnimator;
            }
            
            // 方式1：从 AnimationViewComponent 获取（推荐）
            if (_ownerEntityView != null)
            {
                var animViewComponent = _ownerEntityView.GetViewComponent<AnimationViewComponent>();
                if (animViewComponent != null)
                {
                    _cachedAnimator = animViewComponent.GetAnimator();
                    if (_cachedAnimator != null)
                    {
                        return _cachedAnimator;
                    }
                }
                
                // 方式2：直接从 GameObject 获取（fallback）
                var modelComp = _ownerEntityView.GetViewComponent<ModelViewComponent>();
                var model = modelComp?.ModelObject;
                if (model != null)
                {
                    _cachedAnimator = model.GetComponent<Animator>();
                    if (_cachedAnimator != null)
                    {
                        return _cachedAnimator;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 逻辑帧推进时的 RootMotion 处理
        /// </summary>
        private void UpdateRootMotionWhenLogicAdvanced(Vector3 posLogic, float deltaTime)
        {
            var animator = GetAnimator();
            if (animator == null)
            {
                return;
            }
            
            // 更新视觉偏移：插值到当前逻辑点并转换为 offset
            Vector3 interpolatedPos = Vector3.Lerp(_lastPosLogicSeen, posLogic, posFixLerp);
            _visualOffset = interpolatedPos - posLogic;
            
            // 应用动画 RootMotion 位移
            Vector3 animDelta = animator.deltaPosition * motionBlendWeight;
            _visualOffset += animDelta;
            
            // 计算最终视觉位置 = 逻辑位置 + 视觉偏移
            _posVisual = posLogic + _visualOffset;
        }
        
        /// <summary>
        /// 逻辑帧停更时的 RootMotion 处理（惯性前进 + 横向纠偏）
        /// </summary>
        private void UpdateRootMotionWhenLogicFrozen(Vector3 posLogic, float deltaTime)
        {
            var animator = GetAnimator();
            if (animator == null)
            {
                return;
            }
            
            // 应用动画 RootMotion 位移
            Vector3 animDelta = animator.deltaPosition * motionBlendWeight;
            _visualOffset += animDelta;
            
            // 惯性前进：保持当前移动方向和速度
            _posVisual += _dirVisual * _speedVisual * deltaTime;
            
            // 横向纠偏：仅在垂直于移动方向上进行纠偏，不后拉
            var error = posLogic - _posVisual;
            var errorPerp = error - _dirVisual * Vector3.Dot(error, _dirVisual);
            
            // 将横向偏差加入视觉偏移
            _visualOffset += errorPerp * posFixLerp * deltaTime;
            
            // 重新计算视觉位置
            _posVisual = posLogic + _visualOffset;
        }

        protected override void OnInitialize()
        {
            if (_ownerEntityView == null)
                return;

            _posVisual = _ownerEntityView.GetWorldPosition();
            
            // 初始化 RootMotion 状态
            _visualOffset = Vector3.zero;

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
                _isMovingLogicCached = moveComp.CurrentMovementType != MovementType.None;
                _currentMovementTypeCached = moveComp.CurrentMovementType;

                // 初始化：优先使用逻辑侧 MoveDirection 作为移动方向蓝本
                var dir = ToVector3(moveComp.MoveDirection);
                dir.y = 0f;
                if (dir.sqrMagnitude > 1e-8f)
                {
                    _cachedDirLogic = dir.normalized;
                }
            }

            // 兜底：如果 MoveDirection 无效，则使用逻辑朝向 forward
            if (_cachedDirLogic.sqrMagnitude <= 1e-8f && trans != null)
            {
                var fwd = ToVector3(trans.Forward);
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 1e-8f)
                    _cachedDirLogic = fwd.normalized;
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

            // 1. 状态同步和初始化
            Vector3 posLogic;
            int logicFrame;
            bool logicFrameAdvanced;
            UpdateStateSync(out posLogic, out logicFrame, out logicFrameAdvanced, trans);

            // 2. 速度轮询
            UpdateMoveComponentPolling(entity, trans);

            // 3. 旋转同步
            ApplyRotationFromLogic(trans);

            // 4. 静止状态处理
            if (HandleStaticState(posLogic))
                return;

            // 5. 根据移动模式处理不同逻辑
            if (_currentMovementTypeCached == MovementType.SkillDisplacement)
            {
                HandleSkillMotion(posLogic, logicFrame, logicFrameAdvanced, deltaTime);
            }
            else
            {
                HandleNormalMotion(posLogic, logicFrame, logicFrameAdvanced, deltaTime);
            }

            // 6. 应用最终位置
            _ownerEntityView.SetWorldPosition(_posVisual);
        }

        /// <summary>
        /// 更新状态同步：获取逻辑位置、逻辑帧等基本信息
        /// </summary>
        private void UpdateStateSync(out Vector3 posLogic, out int logicFrame, out bool logicFrameAdvanced, TransComponent trans)
        {
            // 内部状态同步：不修改 transform，仅同步缓存
            _posVisual = _ownerEntityView.GetWorldPosition();

            posLogic = ToVector3(trans.Position);
            logicFrame = OwnerEntity.World?.CurFrame ?? _lastLogicFrameSeen;
            logicFrameAdvanced = logicFrame != _lastLogicFrameSeen;
        }

        /// <summary>
        /// 轮询移动组件：轻量轮询速度与移动方向，避免逻辑帧停更/组件不置脏导致长期不更新
        /// </summary>
        private void UpdateMoveComponentPolling(Entity entity, TransComponent trans)
        {
            var moveComp = entity.GetComponent<MovementComponent>();
            if (moveComp != null)
            {
                _cachedSpeedLogic = moveComp.Speed.AsFloat();

                // 移动方向蓝本：优先使用逻辑侧 MoveDirection（不是朝向）
                var dir = ToVector3(moveComp.MoveDirection);
                dir.y = 0f;
                if (dir.sqrMagnitude > 1e-8f)
                    _cachedDirLogic = dir.normalized;
            }

            // 兜底：如果逻辑侧 MoveDirection 仍无效，用朝向 forward（仅用于极端情况）
            if (_cachedDirLogic.sqrMagnitude <= 1e-8f && trans != null)
            {
                var fwd = ToVector3(trans.Forward);
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 1e-8f)
                    _cachedDirLogic = fwd.normalized;
            }
        }

        /// <summary>
        /// 处理静止状态：不更新位置（避免拉回），仅允许偏差过大时硬对齐
        /// </summary>
        private bool HandleStaticState(Vector3 posLogic)
        {
            if (!_isMovingLogicCached)
            {
                if ((_posVisual - posLogic).magnitude > hardSnapDistance)
                {
                    _posVisual = posLogic;
                    _ownerEntityView.SetWorldPosition(_posVisual);
                }
                return true; // 静止状态，不需要进一步处理
            }
            return false; // 非静止状态，继续处理
        }

        /// <summary>
        /// 处理技能动作模式
        /// </summary>
        private void HandleSkillMotion(Vector3 posLogic, int logicFrame, bool logicFrameAdvanced, float deltaTime)
        {
            if (logicFrameAdvanced)
            {
                UpdateRootMotionWhenLogicAdvanced(posLogic, deltaTime);
                _lastPosLogicSeen = posLogic;
                _lastLogicFrameSeen = logicFrame;
            }
            else
            {
                UpdateRootMotionWhenLogicFrozen(posLogic, deltaTime);
            }

            // 极端保护：当视觉与逻辑偏差过大时，强制对齐并重置偏移
            ApplyExtremeProtectionForSkillMotion(posLogic);
        }

        /// <summary>
        /// 处理普通移动模式
        /// </summary>
        private void HandleNormalMotion(Vector3 posLogic, int logicFrame, bool logicFrameAdvanced, float deltaTime)
        {
            if (logicFrameAdvanced)
            {
                HandleNormalMovementWhenLogicAdvanced(posLogic, logicFrame, deltaTime);
            }
            else
            {
                HandleNormalMovementWhenLogicFrozen(posLogic, deltaTime);
            }
        }

        /// <summary>
        /// 普通移动模式下逻辑帧推进的处理
        /// </summary>
        private void HandleNormalMovementWhenLogicAdvanced(Vector3 posLogic, int logicFrame, float deltaTime)
        {
            // 更新逻辑帧和位置缓存
            _lastPosLogicSeen = posLogic;
            _lastLogicFrameSeen = logicFrame;

            // 更新视觉速度
            _speedVisual = _cachedSpeedLogic; //Mathf.MoveTowards(_speedVisual, _cachedSpeedLogic, accel * deltaTime);

            // 计算移动方向（逻辑方向 + 预计未来位置的纠偏方向）
            // 预计未来几帧的逻辑位置（基于当前逻辑方向和速度）
            float predictionTime = predictionFrames * deltaTime;
            Vector3 predictedFuturePos = posLogic + _cachedDirLogic * _cachedSpeedLogic * predictionTime;
            
            // 使用预计未来位置作为纠偏目标，而不是直接使用当前逻辑位置
            var correction = predictedFuturePos - _posVisual;
            var baseDir = _cachedDirLogic.sqrMagnitude > 1e-8f ? _cachedDirLogic : _dirVisual;
            var dirMove = baseDir * wLogicDirection;
            if (correction.sqrMagnitude > 1e-8f)
                dirMove += correction.normalized * wCorrectionDirection;

            if (dirMove.sqrMagnitude < 1e-8f)
                dirMove = baseDir;

            dirMove.Normalize();
            _dirVisual = dirMove;

            // 更新视觉位置
            _posVisual += _dirVisual * _speedVisual * deltaTime;
            // 使用预计未来位置进行平滑纠偏，而不是直接拉回当前逻辑位置
            _posVisual = Vector3.Lerp(_posVisual, predictedFuturePos, posFixLerp * deltaTime);
        }

        /// <summary>
        /// 普通移动模式下逻辑帧停更的处理：保持惯性前进，仅做横向回轨
        /// </summary>
        private void HandleNormalMovementWhenLogicFrozen(Vector3 posLogic, float deltaTime)
        {
            // 惯性前进
            _posVisual += _dirVisual * _speedVisual * deltaTime;

            // 预计未来几帧的逻辑位置（基于当前逻辑方向和速度）
            float predictionTime = predictionFrames * deltaTime;
            Vector3 predictedFuturePos = posLogic + _cachedDirLogic * _cachedSpeedLogic * predictionTime;
            
            // 横向纠偏：仅在垂直于移动方向上进行纠偏，不后拉
            // 使用预计未来位置作为纠偏目标
            var error = predictedFuturePos - _posVisual;
            var errorPerp = error - _dirVisual * Vector3.Dot(error, _dirVisual);

            var steer = _dirVisual * wLogicDirection;
            if (errorPerp.sqrMagnitude > 1e-8f)
                steer += errorPerp.normalized * wCorrectionDirection;

            if (steer.sqrMagnitude > 1e-8f)
                _dirVisual = steer.normalized;

            // 极端保护
            if ((_posVisual - posLogic).magnitude > hardSnapDistance)
            {
                _posVisual = posLogic;
            }
        }

        /// <summary>
        /// 技能动作模式下的极端保护：当视觉与逻辑偏差过大时，强制对齐并重置偏移
        /// </summary>
        private void ApplyExtremeProtectionForSkillMotion(Vector3 posLogic)
        {
            if ((_posVisual - posLogic).magnitude > hardSnapDistance)
            {
                _posVisual = posLogic;
                _visualOffset = Vector3.zero;
            }
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

            // 重置：优先使用逻辑侧 MoveDirection
            if (moveComp != null)
            {
                var dir = ToVector3(moveComp.MoveDirection);
                dir.y = 0f;
                if (dir.sqrMagnitude > 1e-8f)
                    _cachedDirLogic = dir.normalized;
            }

            // 兜底：若 MoveDirection 无效，则使用逻辑朝向 forward
            if (_cachedDirLogic.sqrMagnitude <= 1e-8f && trans != null)
            {
                var fwd = ToVector3(trans.Forward);
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 1e-8f)
                    _cachedDirLogic = fwd.normalized;
            }

            // 方向保持为当前视觉方向；如果无效则用缓存逻辑方向兜底
            if (_dirVisual.sqrMagnitude < 1e-8f)
                _dirVisual = _cachedDirLogic.sqrMagnitude > 1e-8f ? _cachedDirLogic : Vector3.forward;

            // 初始化 RootMotion 状态
            _visualOffset = Vector3.zero;
        }

        private static Vector3 ToVector3(TSVector v)
        {
            return new Vector3((float)v.x, (float)v.y, (float)v.z);
        }

        /// <summary>
        /// 朝向缓动速度因子（0-1），值越大缓动越快
        /// </summary>
        public float rotationLerpSpeed = 0.3f;
        
        private void ApplyRotationFromLogic(TransComponent trans)
        {
            if (_ownerEntityView == null || trans == null)
                return;
            
            // 如果角色停下了，就不要改方向了
            if (!_isMovingLogicCached)
                return;
            
            // 如果移动方向无效，保持当前旋转
            if (_dirVisual.sqrMagnitude < 1e-8f)
                return;
            
            // 计算移动方向对应的旋转
            Quaternion targetRotation = Quaternion.LookRotation(_cachedDirLogic, Vector3.up);
            
            // 获取当前模型旋转
            Quaternion currentRotation = _ownerEntityView.GetWorldRotation();
            
            // 使用快一点的缓动将当前旋转过渡到目标旋转
            Quaternion newRotation = Quaternion.Lerp(currentRotation, targetRotation, rotationLerpSpeed);
            
            // 应用新的旋转
            _ownerEntityView.SetWorldRotation(newRotation);
        }
    }
}

