using Astrum.Editor.RoleEditor.Persistence.Core;

namespace Astrum.Editor.RoleEditor.Persistence.Mappings
{
    /// <summary>
    /// RoleBaseTable数据映射
    /// </summary>
    public class RoleTableData
    {
        [TableField(0, "RoleId")]
        public int RoleId { get; set; }
        
        [TableField(1, "RoleName")]
        public string RoleName { get; set; }
        
        [TableField(2, "RoleDescription")]
        public string RoleDescription { get; set; }
        
        [TableField(3, "RoleType")]
        public int RoleType { get; set; }
        
        [TableField(4, "BaseAttack")]
        public float BaseAttack { get; set; }
        
        [TableField(5, "BaseDefense")]
        public float BaseDefense { get; set; }
        
        [TableField(6, "BaseHealth")]
        public float BaseHealth { get; set; }
        
        [TableField(7, "BaseSpeed")]
        public float BaseSpeed { get; set; }
        
        [TableField(8, "AttackGrowth")]
        public float AttackGrowth { get; set; }
        
        [TableField(9, "DefenseGrowth")]
        public float DefenseGrowth { get; set; }
        
        [TableField(10, "HealthGrowth")]
        public float HealthGrowth { get; set; }
        
        [TableField(11, "SpeedGrowth")]
        public float SpeedGrowth { get; set; }
        
        [TableField(12, "LightAttackSkillId")]
        public int LightAttackSkillId { get; set; }
        
        [TableField(13, "HeavyAttackSkillId")]
        public int HeavyAttackSkillId { get; set; }
        
        [TableField(14, "Skill1Id")]
        public int Skill1Id { get; set; }
        
        [TableField(15, "Skill2Id")]
        public int Skill2Id { get; set; }
        
        /// <summary>
        /// 获取表配置
        /// </summary>
        public static LubanTableConfig GetTableConfig()
        {
            return new LubanTableConfig
            {
                FilePath = "AstrumConfig/Tables/Datas/Role/#RoleBaseTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new System.Collections.Generic.List<string>
                    {
                        "id", "name", "description", "roleType",
                        "baseAttack", "baseDefense", "baseHealth", "baseSpeed",
                        "attackGrowth", "defenseGrowth", "healthGrowth", "speedGrowth",
                        "lightAttackSkillId", "heavyAttackSkillId", "skill1Id", "skill2Id"
                    },
                    Types = new System.Collections.Generic.List<string>
                    {
                        "int", "string", "string", "int",
                        "float", "float", "float", "float",
                        "float", "float", "float", "float",
                        "int", "int", "int", "int"
                    },
                    Groups = new System.Collections.Generic.List<string>
                    {
                        "c", "s", "e", "",
                        "", "", "", "",
                        "", "", "", "",
                        "", "", "", ""
                    },
                    Descriptions = new System.Collections.Generic.List<string>
                    {
                        "角色ID", "角色名称", "角色描述", "角色类型",
                        "基础攻击力", "基础防御力", "基础生命值", "基础移动速度",
                        "攻击力成长", "防御力成长", "生命值成长", "移动速度成长",
                        "轻击技能ID", "重击技能ID", "技能1ID", "技能2ID"
                    }
                }
            };
        }
    }
}

