using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.FrameSync;
using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 移动能力，处理实体的移动逻辑
    /// </summary>
    [MemoryPackable]
    public partial class MovementCapability : Capability
    {
        /// <summary>
        /// 移动阈值，低于此值视为停止移动
        /// </summary>
        public FP MovementThreshold { get; set; } = (FP)0.1f;

        public MovementCapability()
        {
            Priority = 100; // 移动能力优先级较高
        }

        /// <summary>
        /// 每帧更新移动逻辑
        /// </summary>
        /// <param name="deltaTime">时间差</param>
        public override void Tick()
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
            // Q31.32 -> FP: 先转换为浮点数再转换为 FP
            FP moveX = (FP)(input.MoveX / (double)(1L << 32));
            FP moveY = (FP)(input.MoveY / (double)(1L << 32));

            // 计算输入强度
            FP inputMagnitude = FP.Sqrt(moveX * moveX + moveY * moveY);
            
            // 限制输入强度在0-1之间
            if (inputMagnitude > FP.One)
            {
                moveX /= inputMagnitude;
                moveY /= inputMagnitude;
                inputMagnitude = FP.One;
            }

            var deltaTime = LSConstValue.UpdateInterval / 1000f; // 秒（float）
            // 直接移动
            if (inputMagnitude > MovementThreshold && movementComponent.CanMove)
            {
                // 根据输入方向和速度计算移动距离
                FP speed = movementComponent.Speed;
                FP dt = (FP)deltaTime;
                FP deltaX = moveX * speed * dt;
                FP deltaY = moveY * speed * dt;
                
                // 更新位置
                var pos = positionComponent.Position;
                positionComponent.Position = new TSVector(pos.x + deltaX, pos.y, pos.z + deltaY);
                
                // 调试日志
                ASLogger.Instance.Info($"MovementCapability: 移动执行 - 输入:({moveX}, {moveY}), 速度:{speed}, 增量:({deltaX}, {deltaY}), 新位置:({positionComponent.Position.x}, {positionComponent.Position.z})", "Logic.Movement");
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
        public void SetSpeed(FP speed)
        {
            var movementComponent = GetOwnerComponent<MovementComponent>();
            movementComponent?.SetSpeed(speed);
        }

        /// <summary>
        /// 获取当前移动速度
        /// </summary>
        /// <returns>当前移动速度</returns>
        public FP GetCurrentSpeed()
        {
            var movementComponent = GetOwnerComponent<MovementComponent>();
            return movementComponent?.Speed ?? FP.Zero;
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
