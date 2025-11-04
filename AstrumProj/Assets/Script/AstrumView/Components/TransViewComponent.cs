using UnityEngine;
using System.Linq;
using Astrum.CommonBase;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.ActionSystem;
using Astrum.LogicCore.Core;

namespace Astrum.View.Components
{
    /// <summary>
    /// 移动视图组件 - 处理实体的移动表现
    /// </summary>
    public class TransViewComponent : ViewComponent
    {
        [Header("移动设置")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private bool smoothMovement = true;
        [SerializeField] private float smoothTime = 0.1f;
        
        [Header("朝向设置")]
        [SerializeField] private bool enableRotationToMovement = true;
        [SerializeField] private float rotationThreshold = 0.1f;
        [SerializeField] private bool smoothRotation = false;
        [SerializeField] private float rotationSmoothTime = 0.05f;
        
        [Header("视觉跟随设置")]
        [SerializeField] private bool enableVisualFollow = true;
        [SerializeField] private float motionBlendWeight = 0.5f;  // 动画Root感强度（0-1）
        [SerializeField] private float maxVisualOffset = 0.5f;    // 最大视觉偏移阈值（米）
        
        /// <summary>
        /// 视觉跟随模式
        /// </summary>
        public enum VisualFollowMode
        {
            Interpolation,  // 插值模式：逻辑位置变化时的插值算法（默认）
            RootMotion      // RootMotion模式：从Animator获取动画位移（技能动作时使用）
        }
        
        private VisualFollowMode _currentMode = VisualFollowMode.Interpolation;
        
        // 移动状态
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _currentVelocity;
        private bool _isMoving = false;
        
        // 朝向状态
        private Vector3 _lastPosition;
        private Vector3 _movementDirection;
        private Quaternion _rotationVelocity;
        private bool _isRotating = false;
        
        // 动画相关
        private Animator _animator;
        private int _isMovingHash;
        private int _moveSpeedHash;
        
        // 视觉同步数据
        private struct VisualSyncData
        {
            /// <summary>
            /// 上一逻辑帧的逻辑位置（浮点数）
            /// </summary>
            public Vector3 lastLogicPos;
            
            /// <summary>
            /// 上上一逻辑帧的逻辑位置（用于插值）
            /// </summary>
            public Vector3 previousLogicPos;
            
            /// <summary>
            /// 当前视觉偏移（动画带来的临时偏移）
            /// </summary>
            public Vector3 visualOffset;
            
            /// <summary>
            /// 自上次逻辑更新以来的累积时间
            /// </summary>
            public float timeSinceLastLogicUpdate;
        }
        
        private VisualSyncData _visualSync;
        
        protected override void OnInitialize()
        {
            
            // 获取动画器
            if (_ownerEntityView != null && _ownerEntityView.GameObject != null)
            {
                _animator = _ownerEntityView.GameObject.GetComponent<Animator>();
                if (_animator != null)
                {
                    _isMovingHash = Animator.StringToHash("IsMoving");
                    _moveSpeedHash = Animator.StringToHash("MoveSpeed");
                }
            }
            
            // 初始化目标位置为当前位置
            if (_ownerEntityView != null)
            {
                _targetPosition = _ownerEntityView.GetWorldPosition();
                _targetRotation = _ownerEntityView.GetWorldRotation();
                _lastPosition = _targetPosition;
                
                // 初始化视觉同步数据
                _visualSync.lastLogicPos = _targetPosition;
                _visualSync.previousLogicPos = _targetPosition;
                _visualSync.visualOffset = Vector3.zero;
                _visualSync.timeSinceLastLogicUpdate = 0f;
            }
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            if (!_isEnabled || _ownerEntityView == null) return;
            
            var ownerEntity = _ownerEntityView.OwnerEntity;
            if (ownerEntity == null)
            {
                ASLogger.Instance.Log(LogLevel.Warning, $"TransViewComponent: OwnerEntity为null，跳过更新，EntityId: {_ownerEntityView.EntityId}", "View.Transform");
                return;
            }
            
            var posC = ownerEntity.GetComponent<TransComponent>();
            if (posC == null)
            {
                ASLogger.Instance.Log(LogLevel.Error,$"TransViewComponent: 实体缺少PositionComponent，无法更新位置，实体ID: {ownerEntity.UniqueId}", "View.Transform");
                return;
            }
            var pos = posC.GetPosition();
            _targetPosition.x = (float)pos.x;
            _targetPosition.y = (float)pos.y;
            _targetPosition.z = (float)pos.z;
            
            // 根据是否启用视觉跟随选择不同的更新方式
            if (enableVisualFollow)
            {
                // 检测当前动作是否为技能动作，决定使用哪种模式
                VisualFollowMode requiredMode = DetermineVisualFollowMode(ownerEntity);
                
                // 如果模式发生变化，记录日志
                if (_currentMode != requiredMode)
                {
                    ASLogger.Instance.Info($"[TransViewComponent] Visual follow mode changed: {_currentMode} -> {requiredMode}", "View.VisualFollow");
                    _currentMode = requiredMode;
                }
                
                // 根据模式选择不同的更新方式
                if (_currentMode == VisualFollowMode.RootMotion)
                {
                    UpdateVisualFollowRootMotion(deltaTime);
                }
                else
                {
                    UpdateVisualFollowInterpolation(deltaTime);
                }
            }
            else
            {
                // 使用原有的平滑移动逻辑
                // 更新移动
                UpdateMovement(deltaTime);
                
                // 更新朝向（从逻辑层读取并平滑过渡）
                UpdateRotation(deltaTime);
            }

        }
        
        protected override void OnDestroy()
        {
            ASLogger.Instance.Info($"MovementViewComponent: 销毁移动视图组件，ID: {_componentId}");
        }
        
        protected override void OnSyncData(object data)
        {
            if (data is MovementData movementData)
            {
                // 更新目标位置和旋转
                _targetPosition = movementData.Position;
                _targetRotation = movementData.Rotation;
                _isMoving = movementData.IsMoving;
                
                ASLogger.Instance.Debug($"MovementViewComponent: 同步移动数据，位置: {_targetPosition}");
            }
        }
        
        /// <summary>
        /// 更新旋转（从逻辑层读取并平滑过渡）
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        private void UpdateRotation(float deltaTime)
        {
            if (_ownerEntityView == null) return;
            
            var ownerEntity = _ownerEntityView.OwnerEntity;
            if (ownerEntity == null) return;
            
            var transComponent = ownerEntity.GetComponent<TransComponent>();
            if (transComponent == null) return;
            
            // 从逻辑层读取目标旋转（定点数转浮点数）
            var fixedRot = transComponent.Rotation;
            Quaternion logicRotation = new Quaternion(
                (float)fixedRot.x,
                (float)fixedRot.y,
                (float)fixedRot.z,
                (float)fixedRot.w
            );
            
            // 更新目标旋转
            _targetRotation = logicRotation;
            
            // 平滑旋转到目标朝向
            Quaternion currentRotation = _ownerEntityView.GetWorldRotation();
            if (Quaternion.Angle(currentRotation, _targetRotation) > 0.1f)
            {
                Quaternion newRotation;
                if (smoothRotation)
                {
                    newRotation = Quaternion.Slerp(currentRotation, _targetRotation, rotationSmoothTime * deltaTime * 10f);
                }
                else
                {
                    newRotation = Quaternion.RotateTowards(currentRotation, _targetRotation, rotationSpeed * deltaTime);
                }
                _ownerEntityView.SetWorldRotation(newRotation);
                _isRotating = true;
            }
            else
            {
                _isRotating = false;
            }
        }
        
        /// <summary>
        /// 更新移动
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        private void UpdateMovement(float deltaTime)
        {
            if (_ownerEntityView == null) return;
            
            Vector3 currentPosition = _ownerEntityView.GetWorldPosition();
            Quaternion currentRotation = _ownerEntityView.GetWorldRotation();
        
            
            
            // 平滑移动
            if (smoothMovement)
            {
                Vector3 newPosition = Vector3.SmoothDamp(currentPosition, _targetPosition, ref _currentVelocity, smoothTime, moveSpeed);
                _ownerEntityView.SetWorldPosition(newPosition);
            }
            else
            {
                // 直接移动
                Vector3 direction = (_targetPosition - currentPosition).normalized;
                float distance = Vector3.Distance(currentPosition, _targetPosition);
                
                if (distance > 0.01f)
                {
                    Vector3 newPosition = currentPosition + direction * moveSpeed * deltaTime;
                    if (Vector3.Distance(newPosition, _targetPosition) < distance)
                    {
                        _ownerEntityView.SetWorldPosition(newPosition);
                    }
                    else
                    {
                        _ownerEntityView.SetWorldPosition(_targetPosition);
                    }
                }
            }
            
            // 更新朝向（从逻辑层读取并平滑过渡）
            UpdateRotation(deltaTime);
        }
        
        /// <summary>
        /// 设置目标位置
        /// </summary>
        /// <param name="position">目标位置</param>
        public void SetTargetPosition(Vector3 position)
        {
            _targetPosition = position;
            _isMoving = true;
        }
        
        /// <summary>
        /// 设置目标旋转
        /// </summary>
        /// <param name="rotation">目标旋转</param>
        public void SetTargetRotation(Quaternion rotation)
        {
            _targetRotation = rotation;
        }
        
        /// <summary>
        /// 停止移动
        /// </summary>
        public void StopMovement()
        {
            _isMoving = false;
            _currentVelocity = Vector3.zero;
        }
        
        /// <summary>
        /// 获取移动状态
        /// </summary>
        /// <returns>是否正在移动</returns>
        public bool IsMoving()
        {
            return _isMoving;
        }
        
        /// <summary>
        /// 获取当前速度
        /// </summary>
        /// <returns>当前速度</returns>
        public float GetCurrentSpeed()
        {
            return _currentVelocity.magnitude;
        }
        
        /// <summary>
        /// 设置是否启用朝向移动方向
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetRotationToMovementEnabled(bool enabled)
        {
            enableRotationToMovement = enabled;
        }
        
        /// <summary>
        /// 设置旋转阈值
        /// </summary>
        /// <param name="threshold">旋转阈值</param>
        public void SetRotationThreshold(float threshold)
        {
            rotationThreshold = Mathf.Max(0.01f, threshold);
        }
        
        /// <summary>
        /// 设置旋转平滑度
        /// </summary>
        /// <param name="smoothTime">平滑时间</param>
        public void SetRotationSmoothTime(float smoothTime)
        {
            rotationSmoothTime = Mathf.Max(0.01f, smoothTime);
        }
        
        /// <summary>
        /// 获取当前移动方向
        /// </summary>
        /// <returns>移动方向</returns>
        public Vector3 GetMovementDirection()
        {
            return _movementDirection;
        }
        
        /// <summary>
        /// 获取是否正在旋转
        /// </summary>
        /// <returns>是否正在旋转</returns>
        public bool IsRotating()
        {
            return _isRotating;
        }
        
        /// <summary>
        /// 确定应该使用的视觉跟随模式
        /// </summary>
        /// <param name="ownerEntity">实体</param>
        /// <returns>视觉跟随模式</returns>
        private VisualFollowMode DetermineVisualFollowMode(Entity ownerEntity)
        {
            if (ownerEntity == null) return VisualFollowMode.Interpolation;
            
            // 检查当前动作是否为技能动作
            var actionComponent = ownerEntity.GetComponent<ActionComponent>();
            if (actionComponent?.CurrentAction != null)
            {
                // 检查是否为技能动作（SkillActionInfo）
                // 通过检查类型名称来判断（因为 SkillActionInfo 可能在不同命名空间）
                var actionType = actionComponent.CurrentAction.GetType();
                if (actionType.Name == "SkillActionInfo" || actionType.FullName.Contains("SkillActionInfo"))
                {
                    return VisualFollowMode.RootMotion;
                }
            }
            
            return VisualFollowMode.Interpolation;
        }
        
        /// <summary>
        /// 更新视觉跟随逻辑（RootMotion模式 - 技能动作时使用）
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        private void UpdateVisualFollowRootMotion(float deltaTime)
        {
            if (_ownerEntityView == null) return;
            
            // 1. 获取当前逻辑位置（定点数转浮点数）
            var ownerEntity = _ownerEntityView.OwnerEntity;
            if (ownerEntity == null) return;
            
            var transComponent = ownerEntity.GetComponent<TransComponent>();
            if (transComponent == null) return;
            
            var fixedPos = transComponent.Position;
            Vector3 currentLogicPos = new Vector3(
                (float)fixedPos.x,
                (float)fixedPos.y,
                (float)fixedPos.z
            );
            
            // 2. 获取动画根骨骼本帧位移（RootMotion模式）
            Vector3 animDelta = Vector3.zero;
            Animator animator = GetAnimator();
            if (animator != null && animator.enabled)
            {
                // RootMotion模式：从 Animator 获取 deltaPosition
                animDelta = animator.deltaPosition;
                
                // 添加日志输出，检查能否从动画获取采样
                if (animDelta.sqrMagnitude > 0.0001f)
                {
                    ASLogger.Instance.Debug($"[TransViewComponent] [RootMotion] Animator deltaPosition: {animDelta}, magnitude: {animDelta.magnitude:F6}", "View.VisualFollow");
                }
                else
                {
                    // 如果 deltaPosition 为 0，记录警告
                    ASLogger.Instance.Debug($"[TransViewComponent] [RootMotion] Animator deltaPosition is zero (applyRootMotion={animator.applyRootMotion})", "View.VisualFollow");
                }
            }
            else
            {
                if (animator == null)
                {
                    ASLogger.Instance.Debug($"[TransViewComponent] [RootMotion] Animator is null, cannot get deltaPosition", "View.VisualFollow");
                }
                else if (!animator.enabled)
                {
                    ASLogger.Instance.Debug($"[TransViewComponent] [RootMotion] Animator is disabled, cannot get deltaPosition", "View.VisualFollow");
                }
            }
            
            // 3. 检测 LogicRoot 是否发生逻辑跳变
            Vector3 logicDelta = currentLogicPos - _visualSync.lastLogicPos;
            bool logicUpdated = logicDelta.sqrMagnitude > 0.0001f;
            
            if (logicUpdated)
            {
                // 逻辑帧更新：使用插值计算视觉偏移
                // 计算插值因子（当前渲染帧在两个逻辑帧之间的位置）
                float logicFrameInterval = LSConstValue.UpdateInterval / 1000.0f; // 50ms = 0.05秒
                float t = Mathf.Clamp01(_visualSync.timeSinceLastLogicUpdate / logicFrameInterval);
                
                // 在上一逻辑帧和当前逻辑帧之间插值
                Vector3 interpolatedPos = Vector3.Lerp(_visualSync.lastLogicPos, currentLogicPos, t);
                
                // 计算新的视觉偏移（插值位置相对于当前逻辑位置的偏移）
                _visualSync.visualOffset = interpolatedPos - currentLogicPos;
                
                // 更新逻辑位置记录
                _visualSync.previousLogicPos = _visualSync.lastLogicPos;
                _visualSync.lastLogicPos = currentLogicPos;
                _visualSync.timeSinceLastLogicUpdate = 0f;
                
                ASLogger.Instance.Debug($"[TransViewComponent] [RootMotion] Logic updated: lastPos={_visualSync.previousLogicPos}, currentPos={currentLogicPos}, " +
                    $"logicDelta={logicDelta}, t={t:F3}, visualOffset={_visualSync.visualOffset}", "View.VisualFollow");
            }
            else
            {
                // 逻辑帧未更新：累积时间
                _visualSync.timeSinceLastLogicUpdate += deltaTime;
            }
            
            // 4. 累积动画偏移（RootMotion模式：直接使用动画位移，不应用权重）
            _visualSync.visualOffset += animDelta * motionBlendWeight;
            
            // 5. 误差钳制防护（避免浮点长时间漂移）
            if (_visualSync.visualOffset.magnitude > maxVisualOffset)
            {
                ASLogger.Instance.Warning($"[TransViewComponent] Visual offset exceeded max ({_visualSync.visualOffset.magnitude:F3} > {maxVisualOffset}), resetting to zero", "View.VisualFollow");
                _visualSync.visualOffset = Vector3.zero;
            }
            
            // 6. 应用最终视觉位置
            Vector3 finalVisualPos = currentLogicPos + _visualSync.visualOffset;
            _ownerEntityView.SetWorldPosition(finalVisualPos);
            
            // 更新朝向（从逻辑层读取并平滑过渡）
            UpdateRotationFromLogic(deltaTime);
        }
        
        /// <summary>
        /// 更新视觉跟随逻辑（插值模式 - 非技能动作时使用）
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        private void UpdateVisualFollowInterpolation(float deltaTime)
        {
            if (_ownerEntityView == null) return;
            
            // 1. 获取当前逻辑位置（定点数转浮点数）
            var ownerEntity = _ownerEntityView.OwnerEntity;
            if (ownerEntity == null) return;
            
            var transComponent = ownerEntity.GetComponent<TransComponent>();
            if (transComponent == null) return;
            
            var fixedPos = transComponent.Position;
            Vector3 currentLogicPos = new Vector3(
                (float)fixedPos.x,
                (float)fixedPos.y,
                (float)fixedPos.z
            );
            
            // 2. 检测 LogicRoot 是否发生逻辑跳变
            Vector3 logicDelta = currentLogicPos - _visualSync.lastLogicPos;
            bool logicUpdated = logicDelta.sqrMagnitude > 0.0001f;
            
            if (logicUpdated)
            {
                // 逻辑帧更新：使用插值计算视觉偏移
                float logicFrameInterval = LSConstValue.UpdateInterval / 1000.0f; // 50ms = 0.05秒
                float t = Mathf.Clamp01(_visualSync.timeSinceLastLogicUpdate / logicFrameInterval);
                
                // 在上一逻辑帧和当前逻辑帧之间插值
                Vector3 interpolatedPos = Vector3.Lerp(_visualSync.lastLogicPos, currentLogicPos, t);
                
                // 计算新的视觉偏移（插值位置相对于当前逻辑位置的偏移）
                _visualSync.visualOffset = interpolatedPos - currentLogicPos;
                
                // 更新逻辑位置记录
                _visualSync.previousLogicPos = _visualSync.lastLogicPos;
                _visualSync.lastLogicPos = currentLogicPos;
                _visualSync.timeSinceLastLogicUpdate = 0f;
                
                ASLogger.Instance.Debug($"[TransViewComponent] [Interpolation] Logic updated: lastPos={_visualSync.previousLogicPos}, currentPos={currentLogicPos}, " +
                    $"logicDelta={logicDelta}, t={t:F3}, visualOffset={_visualSync.visualOffset}", "View.VisualFollow");
            }
            else
            {
                // 逻辑帧未更新：累积时间并继续插值
                _visualSync.timeSinceLastLogicUpdate += deltaTime;
                
                // 继续插值计算（基于累积时间）
                float logicFrameInterval = LSConstValue.UpdateInterval / 1000.0f;
                float t = Mathf.Clamp01(_visualSync.timeSinceLastLogicUpdate / logicFrameInterval);
                
                // 在上一逻辑帧和当前逻辑帧之间插值
                Vector3 interpolatedPos = Vector3.Lerp(_visualSync.lastLogicPos, currentLogicPos, t);
                
                // 更新视觉偏移（插值位置相对于当前逻辑位置的偏移）
                _visualSync.visualOffset = interpolatedPos - currentLogicPos;
            }
            
            // 3. 误差钳制防护（避免浮点长时间漂移）
            if (_visualSync.visualOffset.magnitude > maxVisualOffset)
            {
                ASLogger.Instance.Warning($"[TransViewComponent] [Interpolation] Visual offset exceeded max ({_visualSync.visualOffset.magnitude:F3} > {maxVisualOffset}), resetting to zero", "View.VisualFollow");
                _visualSync.visualOffset = Vector3.zero;
            }
            
            // 4. 应用最终视觉位置
            Vector3 finalVisualPos = currentLogicPos + _visualSync.visualOffset;
            _ownerEntityView.SetWorldPosition(finalVisualPos);
            
            // 更新朝向（从逻辑层读取并平滑过渡）
            UpdateRotationFromLogic(deltaTime);
        }
        
        /// <summary>
        /// 从逻辑层更新旋转（视觉跟随模式和普通模式共用）
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        private void UpdateRotationFromLogic(float deltaTime)
        {
            if (_ownerEntityView == null) return;
            
            var ownerEntity = _ownerEntityView.OwnerEntity;
            if (ownerEntity == null) return;
            
            var transComponent = ownerEntity.GetComponent<TransComponent>();
            if (transComponent == null) return;
            
            // 从逻辑层读取目标旋转（定点数转浮点数）
            var fixedRot = transComponent.Rotation;
            Quaternion logicRotation = new Quaternion(
                (float)fixedRot.x,
                (float)fixedRot.y,
                (float)fixedRot.z,
                (float)fixedRot.w
            );
            
            // 更新目标旋转
            _targetRotation = logicRotation;
            
            // 平滑旋转到目标朝向
            Quaternion currentRotation = _ownerEntityView.GetWorldRotation();
            if (Quaternion.Angle(currentRotation, _targetRotation) > 0.1f)
            {
                Quaternion newRotation;
                if (smoothRotation)
                {
                    newRotation = Quaternion.Slerp(currentRotation, _targetRotation, rotationSmoothTime * deltaTime * 10f);
                }
                else
                {
                    newRotation = Quaternion.RotateTowards(currentRotation, _targetRotation, rotationSpeed * deltaTime);
                }
                _ownerEntityView.SetWorldRotation(newRotation);
                _isRotating = true;
            }
            else
            {
                _isRotating = false;
            }
        }
        
        /// <summary>
        /// 获取 Animator 引用
        /// </summary>
        /// <returns>Animator 组件引用，如果不存在则返回 null</returns>
        private Animator GetAnimator()
        {
            // 方式1：从 AnimationViewComponent 获取（推荐）
            if (_ownerEntityView != null)
            {
                var animViewComponent = _ownerEntityView.ViewComponents
                    .FirstOrDefault(c => c is AnimationViewComponent) as AnimationViewComponent;
                
                if (animViewComponent != null)
                {
                    var animator = animViewComponent.GetAnimator();
                    if (animator != null)
                    {
                        return animator;
                    }
                }
                
                // 方式2：直接从 GameObject 获取（fallback）
                if (_ownerEntityView.GameObject != null)
                {
                    var animator = _ownerEntityView.GameObject.GetComponent<Animator>();
                    if (animator != null)
                    {
                        return animator;
                    }
                }
            }
            
            return null;
        }
    }
    
    /// <summary>
    /// 移动数据
    /// </summary>
    public class MovementData
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public bool IsMoving { get; set; }
        public float Speed { get; set; }
        
        public MovementData(Vector3 position, Quaternion rotation, bool isMoving = false, float speed = 0f)
        {
            Position = position;
            Rotation = rotation;
            IsMoving = isMoving;
            Speed = speed;
        }
    }
} 