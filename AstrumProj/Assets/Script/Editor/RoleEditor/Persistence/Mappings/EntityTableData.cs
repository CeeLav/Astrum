using Astrum.Editor.RoleEditor.Persistence.Core;

namespace Astrum.Editor.RoleEditor.Persistence.Mappings
{
    /// <summary>
    /// EntityBaseTable数据映射
    /// </summary>
    public class EntityTableData
    {
        [TableField(0, "EntityId")]
        public int EntityId { get; set; }
        
        [TableField(1, "EntityName")]
        public string EntityName { get; set; }
        
        [TableField(2, "ArchetypeName")]
        public string ArchetypeName { get; set; }
        
        [TableField(3, "ModelId")]
        public int ModelId { get; set; }
        
        [TableField(4, "IdleAction")]
        public int IdleAction { get; set; }
        
        [TableField(5, "WalkAction")]
        public int WalkAction { get; set; }
        
        [TableField(6, "RunAction")]
        public int RunAction { get; set; }
        
        [TableField(7, "JumpAction")]
        public int JumpAction { get; set; }
        
        [TableField(8, "BirthAction")]
        public int BirthAction { get; set; }
        
        [TableField(9, "DeathAction")]
        public int DeathAction { get; set; }
        
        /// <summary>
        /// 临时属性：ModelName（向后兼容，使用 EntityName）
        /// </summary>
        public string ModelName
        {
            get => EntityName;
            set => EntityName = value;
        }
        
        /// <summary>
        /// 临时属性：ModelPath（向后兼容，需要从 EntityModelTable 查询）
        /// TODO: 重构编辑器代码使用 ModelId 查询 EntityModelTable
        /// </summary>
        public string ModelPath { get; set; } = string.Empty;
        
        /// <summary>
        /// 获取表配置
        /// </summary>
        public static LubanTableConfig GetTableConfig()
        {
            return new LubanTableConfig
            {
                FilePath = "AstrumConfig/Tables/Datas/Entity/#EntityBaseTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new System.Collections.Generic.List<string>
                    {
                        "entityId", "entityName", "archetypeName", "modelId",
                        "idleAction", "walkAction", "runAction", "jumpAction",
                        "birthAction", "deathAction"
                    },
                    Types = new System.Collections.Generic.List<string>
                    {
                        "int", "string", "string", "int",
                        "int", "int", "int", "int",
                        "int", "int"
                    },
                    Groups = new System.Collections.Generic.List<string>
                    {
                        "", "", "", "", "", "", "", "", "", ""
                    },
                    Descriptions = new System.Collections.Generic.List<string>
                    {
                        "实体ID", "实体名称", "原型名", "模型ID",
                        "静止动作", "走路动作", "跑步动作", "跳跃动作",
                        "出生动作", "死亡动作"
                    }
                }
            };
        }
    }
}

