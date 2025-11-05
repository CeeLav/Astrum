using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Physics;
using TrueSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 移动能力（新架构，基于 Capability&lt;T&gt;）
    /// 处理实体的移动逻辑
    /// </summary>
    public class MovementCapabilityV2 : Capability<MovementCapabilityV2>
    {
        // ====== 元数据 ======
        public override int Priority => 100;
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag> 
        { 
            CapabilityTag.Movement, 
            CapabilityTag.Control 
        };
        
        // ====== 常量配置（可移至配置文件） ======
        private const string KEY_MOVEMENT_THRESHOLD = "MovementThreshold";
        private static readonly FP DEFAULT_MOVEMENT_THRESHOLD = (FP)0.1f;
        
        // ====== 生命周期 ======
        
        public override void OnAttached(Entity entity)
        {
            base.OnAttached(entity);
            
            // 初始化自定义数据
            var state = GetCapabilityState(entity);
            if (state.CustomData == null)
                state.CustomData = new Dictionary<string, object>();
            
            state.CustomData[KEY_MOVEMENT_THRESHOLD] = DEFAULT_MOVEMENT_THRESHOLD;
            SetCapabilityState(entity, state);
        }
        
        public override bool ShouldActivate(Entity entity)
        {
            // 检查必需组件是否存在
            return base.ShouldActivate(entity) &&
                   HasComponent<LSInputComponent>(entity) &&
                   HasComponent<MovementComponent>(entity) &&
                   HasComponent<TransComponent>(entity);
        }
        
        public override bool ShouldDeactivate(Entity entity)
        {
            // 缺少任何必需组件则停用
            return base.ShouldDeactivate(entity) ||
                   !HasComponent<LSInputComponent>(entity) ||
                   !HasComponent<MovementComponent>(entity) ||
                   !HasComponent<TransComponent>(entity);
        }
        
        // ====== 每帧逻辑 ======
        
        public override void Tick(Entity entity)
        {
            // 获取组件
            var inputComponent = GetComponent<LSInputComponent>(entity);
            var movementComponent = GetComponent<MovementComponent>(entity);
            var transComponent = GetComponent<TransComponent>(entity);
            
            if (inputComponent?.CurrentInput == null || movementComponent == null || transComponent == null)
                return;
            
            // 获取移动阈值
            var state = GetCapabilityState(entity);
            var threshold = (FP)(state.CustomData?.TryGetValue(KEY_MOVEMENT_THRESHOLD, out var value) == true 
                ? value 
                : DEFAULT_MOVEMENT_THRESHOLD);
            
            // 原有移动逻辑（不变）
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
            
            // 更新朝向
            if (inputMagnitude > threshold)
            {
                TSVector inputDirection = new TSVector(moveX, FP.Zero, moveY);
                if (inputDirection.sqrMagnitude > FP.EN4)
                {
                    transComponent.Rotation = TSQuaternion.LookRotation(inputDirection, TSVector.up);
                }
            }
            
            // 处理移动
            if (inputMagnitude > threshold && movementComponent.CanMove)
            {
                FP speed = movementComponent.Speed;
                FP dt = (FP)deltaTime;
                FP deltaX = moveX * speed * dt;
                FP deltaY = moveY * speed * dt;
                
                var pos = transComponent.Position;
                transComponent.Position = new TSVector(pos.x + deltaX, pos.y, pos.z + deltaY);
                
                entity.World?.HitSystem?.UpdateEntityPosition(entity);
            }
        }
    }
}

