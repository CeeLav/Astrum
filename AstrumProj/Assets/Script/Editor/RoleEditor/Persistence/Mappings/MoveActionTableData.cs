using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Core;

namespace Astrum.Editor.RoleEditor.Persistence.Mappings
{
    /// <summary>
    /// MoveActionTable 表数据映射
    /// 对应 AstrumConfig/Tables/Datas/Entity/#MoveActionTable.csv
    /// </summary>
    public class MoveActionTableData
    {
        [TableField(0, "actionId")]
        public int ActionId { get; set; }

        [TableField(1, "moveSpeed")]
        public int MoveSpeed { get; set; }

        public static LubanTableConfig GetTableConfig()
        {
            return new LubanTableConfig
            {
                FilePath = "AstrumConfig/Tables/Datas/Entity/#MoveActionTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new List<string>
                    {
                        "actionId", "moveSpeed"
                    },
                    Types = new List<string>
                    {
                        "int", "int"
                    },
                    Groups = new List<string>
                    {
                        "", ""
                    },
                    Descriptions = new List<string>
                    {
                        "动作ID", "基础移动速度(×1000)"
                    }
                }
            };
        }

        public static List<MoveActionTableData> ReadAll()
        {
            var config = GetTableConfig();
            return LubanCSVReader.ReadTable<MoveActionTableData>(config);
        }
    }
}

