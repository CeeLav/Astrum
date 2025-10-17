using Astrum.LogicCore.Stats;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.Core;
using Astrum.CommonBase;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 等级组件 - 管理实体的等级和经验
    /// </summary>
    [MemoryPackable]
    public partial class LevelComponent : BaseComponent
    {
        /// <summary>当前等级</summary>
        public int CurrentLevel { get; set; } = 1;
        
        /// <summary>当前经验值</summary>
        public int CurrentExp { get; set; } = 0;
        
        /// <summary>升到下一级所需经验</summary>
        public int ExpToNextLevel { get; set; } = 1000;
        
        /// <summary>最大等级</summary>
        public int MaxLevel { get; set; } = 100;
        
        [MemoryPackConstructor]
        public LevelComponent() : base() { }
        
        /// <summary>获得经验</summary>
        public bool GainExp(int amount, Entity owner)
        {
            if (CurrentLevel >= MaxLevel)
                return false;
            
            CurrentExp += amount;
            
            // 检查是否升级
            if (CurrentExp >= ExpToNextLevel)
            {
                LevelUp(owner);
                return true;
            }
            
            return false;
        }
        
        /// <summary>升级</summary>
        private void LevelUp(Entity owner)
        {
            CurrentLevel++;
            CurrentExp -= ExpToNextLevel;
            
            // 1. 更新下一级经验需求
            var roleInfo = owner.GetComponent<RoleInfoComponent>();
            if (roleInfo != null)
            {
                // 查找下一级的经验配置
                cfg.Role.RoleGrowthTable growthConfig = null;
                foreach (var config in TableConfig.Instance.Tables.TbRoleGrowthTable.DataList)
                {
                    if (config.RoleId == roleInfo.RoleId && config.Level == CurrentLevel + 1)
                    {
                        growthConfig = config;
                        break;
                    }
                }
                ExpToNextLevel = growthConfig?.RequiredExp ?? ExpToNextLevel;
            }
            
            // 2. 更新基础属性
            var baseStats = owner.GetComponent<BaseStatsComponent>();
            if (baseStats != null && roleInfo != null)
            {
                baseStats.InitializeFromConfig(roleInfo.RoleId, CurrentLevel);
            }
            
            // 3. 重新计算派生属性
            var derivedStats = owner.GetComponent<DerivedStatsComponent>();
            if (derivedStats != null && baseStats != null)
            {
                derivedStats.RecalculateAll(baseStats);
            }
            
            // 4. 满血满蓝
            var dynamicStats = owner.GetComponent<DynamicStatsComponent>();
            if (dynamicStats != null && derivedStats != null)
            {
                dynamicStats.Set(DynamicResourceType.CURRENT_HP, derivedStats.Get(StatType.HP));
                dynamicStats.Set(DynamicResourceType.CURRENT_MANA, derivedStats.Get(StatType.MAX_MANA));
            }
            
            ASLogger.Instance.Info($"[Level] Entity {owner.UniqueId} leveled up to {CurrentLevel}!");
        }
    }
}

