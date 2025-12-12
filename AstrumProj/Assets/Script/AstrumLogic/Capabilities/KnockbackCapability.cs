using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using Astrum.LogicCore.FrameSync;
using Astrum.CommonBase;
using TrueSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 击退能力 - 处理实体的击退位移
    /// 优先级：150（高于移动，低于受击反应）
    /// </summary>
    public class KnockbackCapability : Capability<KnockbackCapability>
    {
        // ====== 元数据 ======
        
        public override int Priority => 150;
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag>
        {
            CapabilityTag.Movement,
            CapabilityTag.Combat
        };
        
        /// <summary>
        /// 用于标识是击退系统禁用用户输入位移的 instigatorId
        /// </summary>
        private long _knockbackInstigatorId;
        
        // ====== 事件处理声明 ======
        
        /// <summary>
        /// 静态声明：该 Capability 处理的事件
        /// </summary>
        protected override void RegisterEventHandlers()
        {
            RegisterEventHandler<KnockbackEvent>(OnKnockback);
        }
        
        // ====== 事件处理函数 ======
        
        /// <summary>
        /// 接收击退事件，写入组件数据
        /// </summary>
        private void OnKnockback(Entity entity, KnockbackEvent evt)
        {
            ASLogger.Instance.Debug($"[KnockbackCapability] OnKnockback called for entity {entity.UniqueId}, " +
                $"distance={evt.Distance}m, duration={evt.Duration}s");
            
            // 获取或添加击退组件
            var knockback = GetComponent<KnockbackComponent>(entity);
            if (knockback == null)
            {
                knockback = new KnockbackComponent();
                entity.AddComponent(knockback);
                ASLogger.Instance.Debug($"[KnockbackCapability] Created KnockbackComponent for entity {entity.UniqueId}");
            }
            
            // 写入击退数据（修改自身组件）
            knockback.IsKnockingBack = true;
            knockback.Direction = evt.Direction;
            knockback.TotalDistance = evt.Distance;
            knockback.RemainingTime = evt.Duration;
            knockback.Speed = evt.Distance / evt.Duration;
            knockback.MovedDistance = FP.Zero;
            knockback.Type = evt.Type;
            knockback.CasterId = evt.CasterId;
            
            //ASLogger.Instance.Debug($"[KnockbackCapability] Knockback data written: speed={knockback.Speed}m/s, direction={evt.Direction}");
        }
        
        // ====== 生命周期 ======
        
        public override bool ShouldActivate(Entity entity)
        {
            // 只在开始击退时激活
            var knockback = GetComponent<KnockbackComponent>(entity);
            return base.ShouldActivate(entity) &&
                   knockback != null &&
                   knockback.IsKnockingBack &&
                   HasComponent<TransComponent>(entity);
        }
        
        public override bool ShouldDeactivate(Entity entity)
        {
            // 击退结束时停用
            var knockback = GetComponent<KnockbackComponent>(entity);
            return base.ShouldDeactivate(entity) ||
                   knockback == null ||
                   !knockback.IsKnockingBack ||
                   !HasComponent<TransComponent>(entity);
        }
        
        public override void OnActivate(Entity entity)
        {
            base.OnActivate(entity);
            
            // 获取击退施法者ID作为标识
            var knockback = GetComponent<KnockbackComponent>(entity);
            _knockbackInstigatorId = knockback?.CasterId ?? entity.UniqueId;
            
            // 禁用用户输入位移
            entity.World?.CapabilitySystem?.DisableCapabilitiesByTag(
                entity,
                CapabilityTag.UserInputMovement,
                _knockbackInstigatorId,
                "Knockback active"
            );
            
            //ASLogger.Instance.Debug($"[KnockbackCapability] Activated for entity {entity.UniqueId}");
        }
        
        public override void OnDeactivate(Entity entity)
        {
            // 恢复用户输入位移
            entity.World?.CapabilitySystem?.EnableCapabilitiesByTag(
                entity,
                CapabilityTag.UserInputMovement,
                _knockbackInstigatorId
            );
            
            //ASLogger.Instance.Debug($"[KnockbackCapability] Deactivated for entity {entity.UniqueId}");
            
            base.OnDeactivate(entity);
        }
        
        // ====== 每帧逻辑 ======
        
        public override void Tick(Entity entity)
        {
            var knockback = GetComponent<KnockbackComponent>(entity);
            if (knockback == null || !knockback.IsKnockingBack)
                return;
            
            var transComponent = GetComponent<TransComponent>(entity);
            if (transComponent == null)
                return;
            
            // 计算本帧位移（使用帧同步固定间隔）
            FP deltaTime = FP.FromFloat(LSConstValue.UpdateInterval / 1000f); // 50ms = 0.05s
            FP moveDistance = CalculateMoveDistance(knockback, deltaTime);
            
            // 应用位移
            TSVector movement = knockback.Direction * moveDistance;
            transComponent.Position += movement;
            knockback.MovedDistance += moveDistance;
            knockback.RemainingTime -= deltaTime;

            // 本逻辑帧实际发生位移：设置 IsMoving=true（仅在变化时置脏）
            var movementComponent = GetComponent<MovementComponent>(entity);
            if (movementComponent != null && movement.sqrMagnitude > FP.EN4)
            {
                int currentFrame = entity.World?.CurFrame ?? 0;
                movementComponent.LastMoveFrame = currentFrame;
                if (!movementComponent.IsMoving)
                {
                    movementComponent.IsMoving = true;
                    entity.MarkComponentDirty(MovementComponent.ComponentTypeId);
                }
            }
            
            // 更新物理世界位置
            entity.World?.HitSystem?.UpdateEntityPosition(entity);
            
            // 检查是否结束
            if (knockback.RemainingTime <= FP.Zero ||
                knockback.MovedDistance >= knockback.TotalDistance)
            {
                EndKnockback(knockback);
            }
        }
        
        // ====== 辅助方法 ======
        
        /// <summary>
        /// 计算本帧移动距离
        /// </summary>
        private FP CalculateMoveDistance(KnockbackComponent knockback, FP deltaTime)
        {
            switch (knockback.Type)
            {
                case KnockbackType.Linear:
                    return knockback.Speed * deltaTime;
                
                case KnockbackType.Decelerate:
                    // 使用线性减速：速度随时间衰减
                    FP totalTime = knockback.TotalDistance / knockback.Speed;
                    FP progress = FP.One - (knockback.RemainingTime / totalTime);
                    FP currentSpeed = knockback.Speed * (FP.One - progress);
                    return currentSpeed * deltaTime;
                
                default:
                    return knockback.Speed * deltaTime;
            }
        }
        
        /// <summary>
        /// 结束击退
        /// </summary>
        private void EndKnockback(KnockbackComponent knockback)
        {
            knockback.IsKnockingBack = false;
            knockback.RemainingTime = FP.Zero;
            knockback.Speed = FP.Zero;
            
            ASLogger.Instance.Debug($"[KnockbackCapability] Knockback ended");
        }
    }
}

