using Astrum.LogicCore.Stats;
using System.Collections.Generic;
using Astrum.CommonBase;
using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 动态属性组件 - 存储战斗中实时变化的数值（当前值、临时资源等）
    /// </summary>
    [MemoryPackable]
    public partial class DynamicStatsComponent : BaseComponent
    {
        /// <summary>动态资源容器（使用定点数）</summary>
        public Dictionary<DynamicResourceType, FP> Resources { get; set; } = new Dictionary<DynamicResourceType, FP>();
        
        [MemoryPackConstructor]
        public DynamicStatsComponent() : base() { }
        
        /// <summary>获取资源值</summary>
        public FP Get(DynamicResourceType type)
        {
            return Resources.TryGetValue(type, out var value) ? value : FP.Zero;
        }
        
        /// <summary>设置资源值</summary>
        public void Set(DynamicResourceType type, FP value)
        {
            ASLogger.Instance.Warning($"DynamicStatsComponent Set Resource: {type} = {value}");
            Resources[type] = value;
        }
        
        /// <summary>增加资源值</summary>
        public void Add(DynamicResourceType type, FP delta)
        {
            if (Resources.TryGetValue(type, out var current))
                Resources[type] = current + delta;
            else
                Resources[type] = delta;
        }
        
        // ===== 业务方法 =====
        
        /// <summary>扣除生命值（考虑护盾）</summary>
        public FP TakeDamage(FP damage, DerivedStatsComponent derivedStats)
        {
            FP remainingDamage = damage;
            
            // 1. 先扣除护盾
            FP currentShield = Get(DynamicResourceType.SHIELD);
            if (currentShield > FP.Zero)
            {
                FP shieldDamage = TSMath.Min(currentShield, remainingDamage);
                Set(DynamicResourceType.SHIELD, currentShield - shieldDamage);
                remainingDamage -= shieldDamage;
            }
            
            // 2. 扣除生命值
            if (remainingDamage > FP.Zero)
            {
                FP currentHP = Get(DynamicResourceType.CURRENT_HP);
                FP actualDamage = TSMath.Min(currentHP, remainingDamage);
                Set(DynamicResourceType.CURRENT_HP, currentHP - actualDamage);
                return actualDamage; // 返回实际受到的生命伤害
            }
            
            return FP.Zero;
        }
        
        /// <summary>恢复生命值（不超过上限）</summary>
        public FP Heal(FP amount, DerivedStatsComponent derivedStats)
        {
            FP maxHP = derivedStats.Get(StatType.HP);
            FP currentHP = Get(DynamicResourceType.CURRENT_HP);
            FP maxHeal = maxHP - currentHP;
            FP actualHeal = TSMath.Min(amount, maxHeal);
            Set(DynamicResourceType.CURRENT_HP, currentHP + actualHeal);
            return actualHeal;
        }
        
        /// <summary>消耗法力</summary>
        public bool ConsumeMana(FP amount)
        {
            FP currentMP = Get(DynamicResourceType.CURRENT_MANA);
            if (currentMP < amount)
                return false;
            
            Set(DynamicResourceType.CURRENT_MANA, currentMP - amount);
            return true;
        }
        
        /// <summary>增加能量（自动限制在0-100）</summary>
        public void AddEnergy(FP amount)
        {
            FP newValue = TSMath.Clamp(Get(DynamicResourceType.ENERGY) + amount, FP.Zero, (FP)100);
            Set(DynamicResourceType.ENERGY, newValue);
        }
        
        /// <summary>增加怒气（自动限制在0-100）</summary>
        public void AddRage(FP amount)
        {
            FP newValue = TSMath.Clamp(Get(DynamicResourceType.RAGE) + amount, FP.Zero, (FP)100);
            Set(DynamicResourceType.RAGE, newValue);
        }
        
        /// <summary>初始化动态资源（满血满蓝）</summary>
        public void InitializeResources(DerivedStatsComponent derivedStats)
        {
            Set(DynamicResourceType.CURRENT_HP, derivedStats.Get(StatType.HP));
            Set(DynamicResourceType.CURRENT_MANA, derivedStats.Get(StatType.MAX_MANA));
            Set(DynamicResourceType.ENERGY, FP.Zero);
            Set(DynamicResourceType.RAGE, FP.Zero);
            Set(DynamicResourceType.SHIELD, FP.Zero);
            Set(DynamicResourceType.COMBO, FP.Zero);
        }
    }
}

