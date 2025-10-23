using Astrum.LogicCore.Stats;
using Astrum.LogicCore.Core;
using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 成长组件 - 记录成长曲线相关数据和加点分配
    /// </summary>
    [MemoryPackable]
    public partial class GrowthComponent : BaseComponent
    {
        /// <summary>角色ID（关联成长表）</summary>
        public int RoleId { get; set; }
        
        /// <summary>可分配的属性点</summary>
        public int AvailableStatPoints { get; set; } = 0;
        
        /// <summary>已分配的攻击点</summary>
        public int AllocatedAttackPoints { get; set; } = 0;
        
        /// <summary>已分配的防御点</summary>
        public int AllocatedDefensePoints { get; set; } = 0;
        
        /// <summary>已分配的生命点</summary>
        public int AllocatedHealthPoints { get; set; } = 0;
        
        /// <summary>已分配的速度点</summary>
        public int AllocatedSpeedPoints { get; set; } = 0;
        
        [MemoryPackConstructor]
        public GrowthComponent(int roleId, int availableStatPoints, int allocatedAttackPoints, 
            int allocatedDefensePoints, int allocatedHealthPoints, int allocatedSpeedPoints) : base()
        {
            RoleId = roleId;
            AvailableStatPoints = availableStatPoints;
            AllocatedAttackPoints = allocatedAttackPoints;
            AllocatedDefensePoints = allocatedDefensePoints;
            AllocatedHealthPoints = allocatedHealthPoints;
            AllocatedSpeedPoints = allocatedSpeedPoints;
        }
        
        public GrowthComponent() : base() { }
        
        /// <summary>分配属性点</summary>
        public bool AllocatePoint(StatType statType, Entity owner)
        {
            if (AvailableStatPoints <= 0)
                return false;
            
            // 1. 扣除可用点数
            AvailableStatPoints--;
            
            // 2. 增加对应属性的分配点和基础属性
            var baseStats = owner.GetComponent<BaseStatsComponent>();
            if (baseStats == null)
                return false;
            
            switch (statType)
            {
                case StatType.ATK:
                    AllocatedAttackPoints++;
                    baseStats.BaseStats.Add(StatType.ATK, (FP)2); // 每点+2攻击
                    break;
                case StatType.DEF:
                    AllocatedDefensePoints++;
                    baseStats.BaseStats.Add(StatType.DEF, (FP)2);
                    break;
                case StatType.HP:
                    AllocatedHealthPoints++;
                    baseStats.BaseStats.Add(StatType.HP, (FP)20); // 每点+20生命
                    break;
                case StatType.SPD:
                    AllocatedSpeedPoints++;
                    baseStats.BaseStats.Add(StatType.SPD, (FP)0.1);
                    break;
                default:
                    // 不支持的属性类型
                    AvailableStatPoints++; // 返还点数
                    return false;
            }
            
            // 3. 重新计算派生属性
            var derivedStats = owner.GetComponent<DerivedStatsComponent>();
            if (derivedStats != null)
            {
                derivedStats.RecalculateAll(baseStats);
            }
            
            return true;
        }
    }
}

