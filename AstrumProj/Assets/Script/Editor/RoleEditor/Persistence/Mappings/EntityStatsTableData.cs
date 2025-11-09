using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Core;

namespace Astrum.Editor.RoleEditor.Persistence.Mappings
{
    /// <summary>
    /// EntityStatsTable 映射类
    /// 对应 AstrumConfig/Tables/Datas/Entity/#EntityStatsTable.csv
    /// </summary>
    [System.Serializable]
    public class EntityStatsTableData
    {
        [TableField(0, "entityId")]
        public int EntityId { get; set; }
        
        [TableField(1, "baseAttack")]
        public int BaseAttack { get; set; }
        
        [TableField(2, "baseDefense")]
        public int BaseDefense { get; set; }
        
        [TableField(3, "baseHealth")]
        public int BaseHealth { get; set; }
        
        [TableField(4, "baseSpeed")]
        public int BaseSpeed { get; set; }  // 存储为 speed * 1000
        
        [TableField(5, "baseCritRate")]
        public int BaseCritRate { get; set; }  // 存储为 rate * 1000
        
        [TableField(6, "baseCritDamage")]
        public int BaseCritDamage { get; set; }  // 存储为 damage * 1000
        
        [TableField(7, "baseAccuracy")]
        public int BaseAccuracy { get; set; }  // 存储为 accuracy * 1000
        
        [TableField(8, "baseEvasion")]
        public int BaseEvasion { get; set; }  // 存储为 evasion * 1000
        
        [TableField(9, "baseBlockRate")]
        public int BaseBlockRate { get; set; }  // 存储为 rate * 1000
        
        [TableField(10, "baseBlockValue")]
        public int BaseBlockValue { get; set; }
        
        [TableField(11, "physicalRes")]
        public int PhysicalRes { get; set; }  // 存储为 res * 1000
        
        [TableField(12, "magicalRes")]
        public int MagicalRes { get; set; }  // 存储为 res * 1000
        
        [TableField(13, "baseMaxMana")]
        public int BaseMaxMana { get; set; }
        
        [TableField(14, "manaRegen")]
        public int ManaRegen { get; set; }  // 存储为 regen * 1000
        
        [TableField(15, "healthRegen")]
        public int HealthRegen { get; set; }  // 存储为 regen * 1000

        /// <summary>
        /// 获取表格配置
        /// </summary>
        public static LubanTableConfig GetTableConfig()
        {
            return new LubanTableConfig
            {
                FilePath = "AstrumConfig/Tables/Datas/Entity/#EntityStatsTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new List<string>
                    {
                        "entityId", "baseAttack", "baseDefense", "baseHealth", "baseSpeed",
                        "baseCritRate", "baseCritDamage", "baseAccuracy", "baseEvasion",
                        "baseBlockRate", "baseBlockValue", "physicalRes", "magicalRes",
                        "baseMaxMana", "manaRegen", "healthRegen"
                    },
                    Types = new List<string>
                    {
                        "int", "int", "int", "int", "int",
                        "int", "int", "int", "int",
                        "int", "int", "int", "int",
                        "int", "int", "int"
                    },
                    Groups = new List<string>
                    {
                        "", "", "", "", "",
                        "", "", "", "",
                        "", "", "", "",
                        "", "", ""
                    },
                    Descriptions = new List<string>
                    {
                        "实体ID", "基础攻击力", "基础防御力", "基础生命值", "基础移动速度*1000",
                        "基础暴击率*1000", "基础暴击伤害*1000", "基础命中率*1000", "基础闪避率*1000",
                        "基础格挡率*1000", "基础格挡值", "物理抗性*1000", "魔法抗性*1000",
                        "基础法力上限", "法力回复*1000", "生命回复*1000"
                    }
                }
            };
        }
    }
}

