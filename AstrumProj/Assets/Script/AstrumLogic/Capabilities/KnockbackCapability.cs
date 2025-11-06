using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
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
            
            ASLogger.Instance.Debug($"[KnockbackCapability] Activated for entity {entity.UniqueId}");
        }
        
        public override void OnDeactivate(Entity entity)
        {
            // 恢复用户输入位移
            entity.World?.CapabilitySystem?.EnableCapabilitiesByTag(
                entity,
                CapabilityTag.UserInputMovement,
                _knockbackInstigatorId
            );
            
            ASLogger.Instance.Debug($"[KnockbackCapability] Deactivated for entity {entity.UniqueId}");
            
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
            
            // 计算本帧位移
            FP deltaTime = entity.World.DeltaTime;
            FP moveDistance = CalculateMoveDistance(knockback, deltaTime);
            
            // 应用位移
            TSVector movement = knockback.Direction * moveDistance;
            transComponent.Position += movement;
            knockback.MovedDistance += moveDistance;
            knockback.RemainingTime -= deltaTime;
            
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

