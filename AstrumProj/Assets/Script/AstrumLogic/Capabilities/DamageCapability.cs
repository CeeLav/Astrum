using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using Astrum.LogicCore.Stats;
using Astrum.CommonBase;
using TrueSync;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 伤害处理能力 - 处理实体受到的伤害
    /// 优先级：200（与 HitReactionCapability 同级）
    /// </summary>
    public class DamageCapability : Capability<DamageCapability>
    {
        public override int Priority => 200;
  
        public override IReadOnlyCollection<CapabilityTag> Tags => new[] 
        { 
            CapabilityTag.Combat
        };
  
        public override bool ShouldActivate(Entity entity)
        {
            return base.ShouldActivate(entity) &&
                   HasComponent<DynamicStatsComponent>(entity);
        }
        
        /// <summary>
        /// 每帧更新（必须实现）
        /// 伤害处理由事件驱动，无需每帧更新
        /// </summary>
        public override void Tick(Entity entity)
        {
            // 事件处理由 RegisterEventHandlers 静态声明，CapabilitySystem 自动调度
            // 此方法保留为空，符合 Capability<T> 接口要求
        }
        
        // ====== 事件处理声明 ======
        
        /// <summary>
        /// 静态声明：该 Capability 处理的事件
        /// </summary>
        protected override void RegisterEventHandlers()
        {
            RegisterEventHandler<DamageEvent>(OnDamage);
        }
        
        // ====== 事件处理函数 ======
        
        /// <summary>
        /// 接收伤害事件，应用伤害
        /// </summary>
        private void OnDamage(Entity entity, DamageEvent evt)
        {
            ASLogger.Instance.Debug($"[DamageCapability] OnDamage called for entity {entity.UniqueId}, " +
                $"damage={evt.Damage}, critical={evt.IsCritical}");
            
            // 1. 获取组件（自身实体）
            var dynamicStats = GetComponent<DynamicStatsComponent>(entity);
            var derivedStats = GetComponent<DerivedStatsComponent>(entity);
            var stateComp = GetComponent<StateComponent>(entity);
            
            if (dynamicStats == null || derivedStats == null)
            {
                ASLogger.Instance.Warning($"[DamageCapability] Missing stats components on entity {entity.UniqueId}");
                return;
            }
            
            // 2. 检查是否可以受到伤害
            if (stateComp != null && !stateComp.CanTakeDamage())
            {
                ASLogger.Instance.Debug($"[DamageCapability] Entity {entity.UniqueId} cannot take damage (invincible or dead)");
                return;
            }
            
            // 3. 应用伤害（修改自身组件）
            FP beforeHP = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
            FP actualDamage = dynamicStats.TakeDamage(evt.Damage, derivedStats);
            FP afterHP = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
            
            // 标记 DynamicStatsComponent 为脏
            entity.MarkComponentDirty(dynamicStats.GetComponentTypeId());
            
            ASLogger.Instance.Debug($"[DamageCapability] HP: {(float)beforeHP:F2} → {(float)afterHP:F2} (-{(float)actualDamage:F2})");
            
            // 4. 死亡判定由 DeadCapability 自动检测血量处理
            if (afterHP <= FP.Zero)
            {
                ASLogger.Instance.Debug($"[DamageCapability] Entity {entity.UniqueId} HP reached 0 - Death will be handled by DeadCapability");
            }
        }
    }
}

