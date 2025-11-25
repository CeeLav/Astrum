using Astrum.Editor.RoleEditor.Persistence.Core;

namespace Astrum.Editor.RoleEditor.Persistence.Mappings
{
    /// <summary>
    /// BaseUnitTable数据映射
    /// </summary>
    public class BaseUnitTableData
    {
        [TableField(0, "entityId")]
        public int EntityId { get; set; }
        
        [TableField(1, "entityName")]
        public string EntityName { get; set; }
        
        [TableField(2, "archetype")]
        public string Archetype { get; set; }  // CSV中存储为字符串，类型为EArchetype但读取为string
        
        [TableField(3, "modelId")]
        public int ModelId { get; set; }
        
        [TableField(4, "idleAction")]
        public int IdleAction { get; set; }
        
        [TableField(5, "walkAction")]
        public int WalkAction { get; set; }
        
        [TableField(6, "runAction")]
        public int RunAction { get; set; }
        
        [TableField(7, "jumpAction")]
        public int JumpAction { get; set; }
        
        [TableField(8, "birthAction")]
        public int BirthAction { get; set; }
        
        [TableField(9, "deathAction")]
        public int DeathAction { get; set; }
        
        [TableField(10, "hitAction")]
        public int HitAction { get; set; }
        
        /// <summary>
        /// 获取表配置
        /// </summary>
        public static LubanTableConfig GetTableConfig()
        {
            return new LubanTableConfig
            {
                FilePath = "AstrumConfig/Tables/Datas/BaseUnit/#BaseUnitTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new System.Collections.Generic.List<string>
                    {
                        "entityId", "entityName", "archetype", "modelId",
                        "idleAction", "walkAction", "runAction", "jumpAction",
                        "birthAction", "deathAction", "hitAction"
                    },
                    Types = new System.Collections.Generic.List<string>
                    {
                        "int", "string", "EArchetype", "int",
                        "int", "int", "int", "int",
                        "int", "int", "int"
                    },
                    Groups = new System.Collections.Generic.List<string>
                    {
                        "", "", "", "", "", "", "", "", "", "", ""
                    },
                    Descriptions = new System.Collections.Generic.List<string>
                    {
                        "实体ID", "实体名称", "类型", "模型ID",
                        "静止动作", "走路动作", "跑步动作", "跳跃动作",
                        "出生动作", "死亡动作", "受击动作"
                    }
                }
            };
        }
    }
}

