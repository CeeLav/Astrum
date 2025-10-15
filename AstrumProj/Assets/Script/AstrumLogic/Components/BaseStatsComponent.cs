using Astrum.LogicCore.Stats;
using Astrum.LogicCore.Managers;
using Astrum.CommonBase;
using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 基础属性组件 - 存储实体的基础原始属性，来源于配置表
    /// </summary>
    [MemoryPackable]
    public partial class BaseStatsComponent : BaseComponent
    {
        /// <summary>基础属性容器</summary>
        public Stats.Stats BaseStats { get; set; } = new Stats.Stats();
        
        [MemoryPackConstructor]
        public BaseStatsComponent() : base() { }
        
        /// <summary>从配置表初始化</summary>
        public void InitializeFromConfig(int roleId, int level)
        {
            // 1. 清空现有属性
            BaseStats.Clear();
            
            // 2. 从配置表读取基础值
            var configManager = ConfigManager.Instance;
            if (!configManager.IsInitialized)
            {
                ASLogger.Instance.Error($"[BaseStats] ConfigManager not initialized");
                return;
            }
            
            var roleConfig = configManager.Tables.TbRoleBaseTable.Get(roleId);
            if (roleConfig == null)
            {
                ASLogger.Instance.Error($"[BaseStats] RoleBaseTable not found for roleId={roleId}");
                return;
            }
            
            // 3. 设置基础四维（配置表存int，直接转FP）
            BaseStats.Set(StatType.ATK, (FP)roleConfig.BaseAttack);
            BaseStats.Set(StatType.DEF, (FP)roleConfig.BaseDefense);
            BaseStats.Set(StatType.HP, (FP)roleConfig.BaseHealth);
            
            // 速度是小数，配置表存1000倍（如10.5存为10500）
            BaseStats.Set(StatType.SPD, (FP)roleConfig.BaseSpeed / (FP)1000);
            
            // 4. 设置高级属性（百分比属性，配置表存1000倍）
            // 如暴击率5%，配置表存50，运行时除以1000得到0.05
            BaseStats.Set(StatType.CRIT_RATE, (FP)roleConfig.BaseCritRate / (FP)1000);
            BaseStats.Set(StatType.CRIT_DMG, (FP)roleConfig.BaseCritDamage / (FP)1000);
            BaseStats.Set(StatType.ACCURACY, (FP)roleConfig.BaseAccuracy / (FP)1000);
            BaseStats.Set(StatType.EVASION, (FP)roleConfig.BaseEvasion / (FP)1000);
            BaseStats.Set(StatType.BLOCK_RATE, (FP)roleConfig.BaseBlockRate / (FP)1000);
            BaseStats.Set(StatType.BLOCK_VALUE, (FP)roleConfig.BaseBlockValue);
            
            // 5. 设置抗性（百分比，配置表存1000倍）
            BaseStats.Set(StatType.PHYSICAL_RES, (FP)roleConfig.PhysicalRes / (FP)1000);
            BaseStats.Set(StatType.MAGICAL_RES, (FP)roleConfig.MagicalRes / (FP)1000);
            
            // 6. 设置资源属性
            BaseStats.Set(StatType.MAX_MANA, (FP)roleConfig.BaseMaxMana);
            BaseStats.Set(StatType.MANA_REGEN, (FP)roleConfig.ManaRegen / (FP)1000);
            BaseStats.Set(StatType.HEALTH_REGEN, (FP)roleConfig.HealthRegen / (FP)1000);
            
            // 7. 应用等级成长
            if (level > 1)
            {
                ApplyLevelGrowth(roleId, level);
            }
            
            ASLogger.Instance.Debug($"[BaseStats] Initialized roleId={roleId}, level={level}, ATK={BaseStats.Get(StatType.ATK)}");
        }
        
        /// <summary>应用等级成长</summary>
        private void ApplyLevelGrowth(int roleId, int level)
        {
            var configManager = ConfigManager.Instance;
            // 查找对应的成长配置（roleId*1000 + level 作为id）
            int growthId = roleId * 1000 + level;
            var growthConfig = configManager.Tables.TbRoleGrowthTable.GetOrDefault(growthId);
            if (growthConfig == null) return;
            
            FP levelDelta = (FP)(level - 1);
            
            // 配置表存int，需要转换
            BaseStats.Add(StatType.ATK, (FP)growthConfig.AttackBonus * levelDelta);
            BaseStats.Add(StatType.DEF, (FP)growthConfig.DefenseBonus * levelDelta);
            BaseStats.Add(StatType.HP, (FP)growthConfig.HealthBonus * levelDelta);
            
            // 小数属性（配置表存1000倍）
            BaseStats.Add(StatType.SPD, (FP)growthConfig.SpeedBonus / (FP)1000 * levelDelta);
            BaseStats.Add(StatType.CRIT_RATE, (FP)growthConfig.CritRateBonus / (FP)1000 * levelDelta);
            BaseStats.Add(StatType.CRIT_DMG, (FP)growthConfig.CritDamageBonus / (FP)1000 * levelDelta);
        }
        
        /// <summary>应用自由加点</summary>
        public void ApplyAllocatedPoints(GrowthComponent growthComp)
        {
            // 每点加成（使用定点数）
            FP ATTACK_PER_POINT = (FP)2;
            FP DEFENSE_PER_POINT = (FP)2;
            FP HEALTH_PER_POINT = (FP)20;
            FP SPEED_PER_POINT = (FP)0.1;
            
            BaseStats.Add(StatType.ATK, (FP)growthComp.AllocatedAttackPoints * ATTACK_PER_POINT);
            BaseStats.Add(StatType.DEF, (FP)growthComp.AllocatedDefensePoints * DEFENSE_PER_POINT);
            BaseStats.Add(StatType.HP, (FP)growthComp.AllocatedHealthPoints * HEALTH_PER_POINT);
            BaseStats.Add(StatType.SPD, (FP)growthComp.AllocatedSpeedPoints * SPEED_PER_POINT);
        }
    }
}

