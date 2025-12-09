using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Core;

namespace Astrum.Editor.SceneEditor.Persistence.Mappings
{
    public class SceneTriggerTableData
    {
        [TableField(0, "ID")]
        public int ID { get; set; }
        
        [TableField(1, "conditionList")]
        public List<int> ConditionList { get; set; } = new List<int>();
        
        [TableField(2, "actionList")]
        public List<int> ActionList { get; set; } = new List<int>();
        
        public static LubanTableConfig GetTableConfig()
        {
            return new LubanTableConfig
            {
                FilePath = "AstrumConfig/Tables/Datas/Scene/#SceneTriggerTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new List<string> { "ID", "conditionList", "actionList" },
                    Types = new List<string> { "int", "(array#sep=|),int", "(array#sep=|),int" },
                    Groups = new List<string> { "", "", "" },
                    Descriptions = new List<string> { "触发器ID", "条件序号（多条件与）", "动作序号" }
                }
            };
        }
    }
}

