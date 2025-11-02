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
        
        [TableField(4, "LightAttackSkillId")]
        public int LightAttackSkillId { get; set; }
        
        [TableField(5, "HeavyAttackSkillId")]
        public int HeavyAttackSkillId { get; set; }
        
        [TableField(6, "Skill1Id")]
        public int Skill1Id { get; set; }
        
        [TableField(7, "Skill2Id")]
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
                        "lightAttackSkillId", "heavyAttackSkillId", "skill1Id", "skill2Id"
                    },
                    Types = new System.Collections.Generic.List<string>
                    {
                        "int", "string", "string", "int",
                        "int", "int", "int", "int"
                    },
                    Groups = new System.Collections.Generic.List<string>
                    {
                        "", "", "", "",
                        "", "", "", ""
                    },
                    Descriptions = new System.Collections.Generic.List<string>
                    {
                        "角色ID", "角色名称", "角色描述", "角色类型",
                        "轻击技能ID", "重击技能ID", "技能1ID", "技能2ID"
                    }
                }
            };
        }
    }
}

