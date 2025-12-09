using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Core;
using cfg;

namespace Astrum.Editor.SceneEditor.Persistence.Mappings
{
    public class SceneTriggerActionTableData
    {
        [TableField(0, "ID")]
        public int ID { get; set; }
        
        [TableField(1, "actionType")]
        public TriggerActionType ActionType { get; set; }
        
        [TableField(2, "intParams")]
        public List<int> IntParams { get; set; } = new List<int>();
        
        [TableField(3, "floatParams")]
        public List<float> FloatParams { get; set; } = new List<float>();
        
        [TableField(4, "stringParams")]
        public List<string> StringParams { get; set; } = new List<string>();
        
        [TableField(5, "notes")]
        public string Notes { get; set; } = string.Empty;
        
        public static LubanTableConfig GetTableConfig()
        {
            return new LubanTableConfig
            {
                FilePath = "AstrumConfig/Tables/Datas/Scene/#SceneTriggerActionTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new List<string> { "ID", "actionType", "intParams", "floatParams", "stringParams", "notes" },
                    Types = new List<string> { "int", "TriggerActionType", "(array#sep=|),int", "(array#sep=|),float", "(array#sep=|),string", "string" },
                    Groups = new List<string> { "", "", "", "", "", "" },
                    Descriptions = new List<string> { "动作序号", "动作类型", "整型参数列表", "浮点参数列表", "字符串参数列表", "备注" }
                }
            };
        }
    }
}

