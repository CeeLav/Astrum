using UnityEngine;
using Astrum.CommonBase;
using Astrum.LogicCore.Components;

namespace Astrum.View.Components
{
    /// <summary>
    /// 移动视图组件 - 处理实体的移动表现
    /// </summary>
    public class TransViewComponent : ViewComponent
    {
        [Header("移动设置")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private bool smoothMovement = true;
        [SerializeField] private float smoothTime = 0.1f;
        
        // 移动状态
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _currentVelocity;
        private bool _isMoving = false;
        
        // 动画相关
        private Animator _animator;
        private int _isMovingHash;
        private int _moveSpeedHash;
        
        protected override void OnInitialize()
        {
            ASLogger.Instance.Info($"MovementViewComponent: 初始化移动视图组件，ID: {_componentId}");
            
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
            
            var posC = ownerEntity.GetComponent<PositionComponent>();
            if (posC == null)
            {
                ASLogger.Instance.Log(LogLevel.Error,$"TransViewComponent: 实体缺少PositionComponent，无法更新位置，实体ID: {ownerEntity.UniqueId}", "View.Transform");
                return;
            }
            var pos = posC.GetPosition();
            _targetPosition.x = (float)pos.x;
            _targetPosition.y = (float)pos.y;
            _targetPosition.z = (float)pos.z;
            ASLogger.Instance.Debug($"MovementViewComponent: 更新目标位置，位置: {_targetPosition}", "View.Movement");
            
            // 更新移动
            UpdateMovement(deltaTime);

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
            
            // 平滑旋转
            if (Quaternion.Angle(currentRotation, _targetRotation) > 0.1f)
            {
                Quaternion newRotation = Quaternion.RotateTowards(currentRotation, _targetRotation, rotationSpeed * deltaTime);
                _ownerEntityView.SetWorldRotation(newRotation);
            }
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