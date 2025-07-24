using Astrum.LogicCore.Components;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 移动能力，处理实体的移动逻辑
    /// </summary>
    public class MovementCapability : Capability
    {
        /// <summary>
        /// 移动阈值，低于此值视为停止移动
        /// </summary>
        public float MovementThreshold { get; set; } = 0.1f;

        public MovementCapability()
        {
            Priority = 100; // 移动能力优先级较高
        }

        /// <summary>
        /// 每帧更新移动逻辑
        /// </summary>
        /// <param name="deltaTime">时间差</param>
        public override void Tick(float deltaTime)
        {
            if (!CanExecute()) return;

            // 获取必需的组件
            var inputComponent = GetOwnerComponent<LSInputComponent>();
            var movementComponent = GetOwnerComponent<MovementComponent>();
            var positionComponent = GetOwnerComponent<PositionComponent>();
            var velocityComponent = GetOwnerComponent<VelocityComponent>();

            // 检查必需组件是否存在
            if (inputComponent?.CurrentInput == null || movementComponent == null || 
                positionComponent == null || velocityComponent == null)
            {
                return;
            }

            // 获取输入数据
            var input = inputComponent.CurrentInput;
            float moveX = input.MoveX;
            float moveY = input.MoveY;

            // 计算输入强度
            float inputMagnitude = (float)Math.Sqrt(moveX * moveX + moveY * moveY);
            
            // 限制输入强度在0-1之间
            if (inputMagnitude > 1f)
            {
                moveX /= inputMagnitude;
                moveY /= inputMagnitude;
                inputMagnitude = 1f;
            }

            // 应用加速度
            movementComponent.ApplyAcceleration(deltaTime, inputMagnitude);

            // 计算移动速度
            if (inputMagnitude > MovementThreshold && movementComponent.CanMove)
            {
                // 根据输入方向和当前速度计算速度向量
                float currentSpeed = movementComponent.CurrentSpeed;
                velocityComponent.VX = moveX * currentSpeed;
                velocityComponent.VY = moveY * currentSpeed;
                velocityComponent.VZ = 0f; // 2D移动，Z轴速度为0
            }
            else
            {
                // 没有输入或输入过小，应用摩擦力减速
                velocityComponent.VX *= movementComponent.Friction;
                velocityComponent.VY *= movementComponent.Friction;
                
                // 速度过小时停止移动
                if (Math.Abs(velocityComponent.VX) < 0.01f)
                    velocityComponent.VX = 0f;
                if (Math.Abs(velocityComponent.VY) < 0.01f)
                    velocityComponent.VY = 0f;
            }

            // 更新位置
            positionComponent.X += velocityComponent.VX * deltaTime;
            positionComponent.Y += velocityComponent.VY * deltaTime;
            positionComponent.Z += velocityComponent.VZ * deltaTime;

            // 更新移动组件的当前速度
            movementComponent.CurrentSpeed = velocityComponent.GetMagnitude();
        }

        /// <summary>
        /// 检查是否可以执行移动
        /// </summary>
        /// <returns>是否可以执行</returns>
        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;

            // 检查必需的组件是否存在
            return OwnerHasComponent<LSInputComponent>() && 
                   OwnerHasComponent<MovementComponent>() && 
                   OwnerHasComponent<PositionComponent>() && 
                   OwnerHasComponent<VelocityComponent>();
        }

        /// <summary>
        /// 立即停止移动
        /// </summary>
        public void StopMovement()
        {
            var movementComponent = GetOwnerComponent<MovementComponent>();
            var velocityComponent = GetOwnerComponent<VelocityComponent>();

            movementComponent?.Stop();
            if (velocityComponent != null)
            {
                velocityComponent.VX = 0f;
                velocityComponent.VY = 0f;
                velocityComponent.VZ = 0f;
            }
        }

        /// <summary>
        /// 设置移动参数
        /// </summary>
        /// <param name="maxSpeed">最大速度</param>
        /// <param name="acceleration">加速度</param>
        /// <param name="friction">摩擦力</param>
        public void SetMovementParams(float maxSpeed, float acceleration, float friction)
        {
            var movementComponent = GetOwnerComponent<MovementComponent>();
            movementComponent?.SetMovementParams(maxSpeed, acceleration, friction);
        }

        /// <summary>
        /// 获取当前移动速度
        /// </summary>
        /// <returns>当前移动速度</returns>
        public float GetCurrentSpeed()
        {
            var velocityComponent = GetOwnerComponent<VelocityComponent>();
            return velocityComponent?.GetMagnitude() ?? 0f;
        }

        /// <summary>
        /// 检查是否正在移动
        /// </summary>
        /// <returns>是否正在移动</returns>
        public bool IsMoving()
        {
            return GetCurrentSpeed() > MovementThreshold;
        }
    }
}
