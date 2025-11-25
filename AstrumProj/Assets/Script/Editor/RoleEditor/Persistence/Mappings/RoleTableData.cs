using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Core;

namespace Astrum.Editor.RoleEditor.Persistence.Mappings
{
    /// <summary>
    /// RoleBaseTable数据映射
    /// </summary>
    public class RoleTableData
    {
        [TableField(0, "id")]
        public int RoleId { get; set; }
        
        [TableField(1, "name")]
        public string RoleName { get; set; }
        
        [TableField(2, "description")]
        public string RoleDescription { get; set; }
        
        [TableField(3, "roleType")]
        public int RoleType { get; set; }
        
        [TableField(4, "defaultSkillIds")]
        public List<int> DefaultSkillIds { get; set; } = new List<int>();
        
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
                    VarNames = new List<string>
                    {
                        "id", "name", "description", "roleType",
                        "defaultSkillIds"
                    },
                    Types = new List<string>
                    {
                        "int", "string", "string", "int",
                        "(array#sep=|),int"
                    },
                    Groups = new List<string>
                    {
                        "", "", "", "",
                        ""
                    },
                    Descriptions = new List<string>
                    {
                        "角色ID", "角色名称", "角色描述", "角色类型",
                        "默认技能ID列表"
                    }
                }
            };
        }
    }
}

