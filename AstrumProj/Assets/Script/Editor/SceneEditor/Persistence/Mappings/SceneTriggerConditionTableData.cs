using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Core;
using cfg;

namespace Astrum.Editor.SceneEditor.Persistence.Mappings
{
    public class SceneTriggerConditionTableData
    {
        [TableField(0, "ID")]
        public int ID { get; set; }
        
        [TableField(1, "conditionType")]
        public TriggerConditionType ConditionType { get; set; }
        
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
                FilePath = "AstrumConfig/Tables/Datas/Scene/#SceneTriggerConditionTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new List<string> { "ID", "conditionType", "intParams", "floatParams", "stringParams", "notes" },
                    Types = new List<string> { "int", "TriggerConditionType", "(array#sep=|),int", "(array#sep=|),float", "(array#sep=|),string", "string" },
                    Groups = new List<string> { "", "", "", "", "", "" },
                    Descriptions = new List<string> { "条件序号", "条件类型", "整型参数列表", "浮点参数列表", "字符串参数列表", "备注" }
                }
            };
        }
    }
}

