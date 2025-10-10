using Astrum.Editor.RoleEditor.Persistence.Core;

namespace Astrum.Editor.RoleEditor.Persistence.Mappings
{
    /// <summary>
    /// EntityModelTable数据映射
    /// </summary>
    public class EntityModelTableData
    {
        [TableField(0, "ModelId")]
        public int ModelId { get; set; }
        
        [TableField(1, "ModelPath")]
        public string ModelPath { get; set; }
        
        [TableField(2, "CollisionData")]
        public string CollisionData { get; set; }
        
        /// <summary>
        /// 获取表配置
        /// </summary>
        public static LubanTableConfig GetTableConfig()
        {
            return new LubanTableConfig
            {
                FilePath = "AstrumConfig/Tables/Datas/Entity/#EntityModelTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new System.Collections.Generic.List<string>
                    {
                        "modelId", "modelPath", "collisionData"
                    },
                    Types = new System.Collections.Generic.List<string>
                    {
                        "int", "string", "string"
                    },
                    Groups = new System.Collections.Generic.List<string>
                    {
                        "", "", ""
                    },
                    Descriptions = new System.Collections.Generic.List<string>
                    {
                        "模型ID", "模型路径", "碰撞盒数据"
                    }
                }
            };
        }
    }
}

