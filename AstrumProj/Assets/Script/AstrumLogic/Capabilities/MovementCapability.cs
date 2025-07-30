using System;
using System.Collections.Generic;
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

            // 检查必需组件是否存在
            if (inputComponent?.CurrentInput == null || movementComponent == null || positionComponent == null)
            {
                var missingComponents = new List<string>();
                if (inputComponent?.CurrentInput == null) missingComponents.Add("LSInputComponent/CurrentInput");
                if (movementComponent == null) missingComponents.Add("MovementComponent");
                if (positionComponent == null) missingComponents.Add("PositionComponent");
                Console.WriteLine($"MovementCapability: 缺少组件: {string.Join(", ", missingComponents)}");
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

            // 直接移动
            if (inputMagnitude > MovementThreshold && movementComponent.CanMove)
            {
                // 根据输入方向和速度计算移动距离
                float speed = movementComponent.Speed;
                float deltaX = moveX * speed * deltaTime;
                float deltaY = moveY * speed * deltaTime;
                
                // 更新位置
                positionComponent.X += deltaX;
                positionComponent.Y += 0f;
                positionComponent.Z += deltaY; // 2D移动，Z轴不移动
            }
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
                   OwnerHasComponent<PositionComponent>();
        }

        /// <summary>
        /// 立即停止移动
        /// </summary>
        public void StopMovement()
        {
            var movementComponent = GetOwnerComponent<MovementComponent>();
            movementComponent?.Stop();
        }

        /// <summary>
        /// 设置移动速度
        /// </summary>
        /// <param name="speed">速度值</param>
        public void SetSpeed(float speed)
        {
            var movementComponent = GetOwnerComponent<MovementComponent>();
            movementComponent?.SetSpeed(speed);
        }

        /// <summary>
        /// 获取当前移动速度
        /// </summary>
        /// <returns>当前移动速度</returns>
        public float GetCurrentSpeed()
        {
            var movementComponent = GetOwnerComponent<MovementComponent>();
            return movementComponent?.Speed ?? 0f;
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
