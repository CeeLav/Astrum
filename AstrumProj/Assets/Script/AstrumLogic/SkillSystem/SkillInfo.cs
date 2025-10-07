using MemoryPack;
using System.Collections.Generic;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 技能信息 - 技能系统的核心运行时数据
    /// 对应SkillTable配置
    /// </summary>
    [MemoryPackable]
    public partial class SkillInfo
    {
        /// <summary>技能ID</summary>
        public int SkillId { get; set; } = 0;
        
        /// <summary>当前等级（运行时数据）</summary>
        public int CurrentLevel { get; set; } = 1;
        
        /// <summary>技能名称</summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>技能描述</summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>技能类型（1=攻击,2=控制,3=位移,4=buff）</summary>
        public int SkillType { get; set; } = 0;
        
        /// <summary>技能图标ID</summary>
        public int IconId { get; set; } = 0;
        
        /// <summary>学习所需等级</summary>
        public int RequiredLevel { get; set; } = 0;
        
        /// <summary>技能最大等级</summary>
        public int MaxLevel { get; set; } = 0;
        
        /// <summary>展示用冷却时间</summary>
        public float DisplayCooldown { get; set; } = 0f;
        
        /// <summary>展示用法力消耗</summary>
        public int DisplayCost { get; set; } = 0;
        
        /// <summary>技能动作ID列表（由SkillTable提供，运行时按等级构造实例）</summary>
        [MemoryPackAllowSerialize]
        public List<int> SkillActionIds { get; set; } = new();
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public SkillInfo()
        {
        }
        
        /// <summary>
        /// MemoryPack构造函数
        /// </summary>
        [MemoryPackConstructor]
        public SkillInfo(int skillId, int currentLevel, string name, string description, 
            int skillType, int iconId, int requiredLevel, int maxLevel,
            float displayCooldown, int displayCost, List<int> skillActionIds)
        {
            SkillId = skillId;
            CurrentLevel = currentLevel;
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            SkillType = skillType;
            IconId = iconId;
            RequiredLevel = requiredLevel;
            MaxLevel = maxLevel;
            DisplayCooldown = displayCooldown;
            DisplayCost = displayCost;
            SkillActionIds = skillActionIds ?? new List<int>();
        }
    }
}

