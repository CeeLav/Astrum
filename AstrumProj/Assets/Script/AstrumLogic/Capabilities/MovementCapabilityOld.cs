using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Physics;
using TrueSync;
using MemoryPack;
// 使用别名避免与 BEPU 的 Entity 类冲突
using AstrumEntity = Astrum.LogicCore.Core.Entity;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 移动能力（旧架构，已废弃，保留用于兼容）
    /// 处理实体的移动逻辑
    /// </summary>
    [MemoryPackable]
    public partial class MovementCapabilityOld : Capability
    {
        /// <summary>
        /// 移动阈值，低于此值视为停止移动
        /// </summary>
        public FP MovementThreshold { get; set; } = (FP)0.1f;

        public MovementCapabilityOld()
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

            var inputComponent = GetOwnerComponent<LSInputComponent>();
            var movementComponent = GetOwnerComponent<MovementComponent>();
            var transComponent = GetOwnerComponent<TransComponent>();

            if (inputComponent?.CurrentInput == null || movementComponent == null || transComponent == null)
            {
                return;
            }

            var input = inputComponent.CurrentInput;
            FP moveX = (FP)(input.MoveX / (double)(1L << 32));
            FP moveY = (FP)(input.MoveY / (double)(1L << 32));
            FP inputMagnitude = FP.Sqrt(moveX * moveX + moveY * moveY);

            if (inputMagnitude > FP.One)
            {
                moveX /= inputMagnitude;
                moveY /= inputMagnitude;
                inputMagnitude = FP.One;
            }

            var deltaTime = LSConstValue.UpdateInterval / 1000f;

            if (inputMagnitude > MovementThreshold)
            {
                TSVector inputDirection = new TSVector(moveX, FP.Zero, moveY);
                if (inputDirection.sqrMagnitude > FP.EN4)
                {
                    transComponent.Rotation = TSQuaternion.LookRotation(inputDirection, TSVector.up);
                }
            }

            if (inputMagnitude > MovementThreshold && movementComponent.CanMove)
            {
                FP speed = movementComponent.Speed;
                FP dt = (FP)deltaTime;
                FP deltaX = moveX * speed * dt;
                FP deltaY = moveY * speed * dt;

                var pos = transComponent.Position;
                transComponent.Position = new TSVector(pos.x + deltaX, pos.y, pos.z + deltaY);

                if (OwnerEntity is AstrumEntity astrumEntity && astrumEntity.World != null)
                {
                    astrumEntity.World.HitSystem?.UpdateEntityPosition(astrumEntity);
                }
            }
        }

        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;

            return OwnerHasComponent<LSInputComponent>() &&
                   OwnerHasComponent<MovementComponent>() &&
                   OwnerHasComponent<TransComponent>();
        }
    }
}

