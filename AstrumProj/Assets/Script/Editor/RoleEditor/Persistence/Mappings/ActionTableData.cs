using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Core;

namespace Astrum.Editor.RoleEditor.Persistence.Mappings
{
    /// <summary>
    /// ActionTable 表数据映射
    /// 对应 AstrumConfig/Tables/Datas/Entity/#ActionTable.csv
    /// </summary>
    public class ActionTableData
    {
        [TableField(0, "actionId")]
        public int ActionId { get; set; }
        
        [TableField(1, "actionName")]
        public string ActionName { get; set; }
        
        [TableField(2, "actionType")]
        public string ActionType { get; set; }
        
        [TableField(3, "duration")]
        public int Duration { get; set; }
        
        [TableField(4, "AnimationName")]
        public string AnimationPath { get; set; }
        
        [TableField(5, "autoNextActionId")]
        public int AutoNextActionId { get; set; }
        
        [TableField(6, "keepPlayingAnim")]
        public bool KeepPlayingAnim { get; set; }
        
        [TableField(7, "AutoTerminate")]
        public bool AutoTerminate { get; set; }
        
        [TableField(8, "Command")]
        public string Command { get; set; }
        
        [TableField(9, "Priority")]
        public int Priority { get; set; }
        
        [TableField(10, "CancelTags")]
        public string CancelTags { get; set; }
        
        [TableField(11, "BeCancelledTags")]
        public string BeCancelledTags { get; set; }
        
        /// <summary>
        /// 获取表配置
        /// </summary>
        public static LubanTableConfig GetTableConfig()
        {
            return new LubanTableConfig
            {
                FilePath = "AstrumConfig/Tables/Datas/Entity/#ActionTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new List<string>
                    {
                        "actionId", "actionName", "actionType", "duration", "AnimationName",
                        "autoNextActionId", "keepPlayingAnim", "AutoTerminate", "Command", "Priority",
                        "CancelTags", "BeCancelledTags"
                    },
                    Types = new List<string>
                    {
                        "int", "string", "string", "int", "string",
                        "int", "bool", "bool", "string", "int",
                        "string", "string"
                    },
                    Groups = new List<string>
                    {
                        "", "", "", "", "",
                        "", "", "", "", "",
                        "", ""
                    },
                    Descriptions = new List<string>
                    {
                        "动作ID", "动作名称", "动作类型", "帧数", "动画路径",
                        "自动下一动作ID", "保持播放动画", "自动终止", "命令", "优先级",
                        "取消标签", "被取消标签"
                    }
                }
            };
        }
        
        /// <summary>
        /// 读取所有动作数据
        /// </summary>
        public static List<ActionTableData> ReadAllActions()
        {
            var config = GetTableConfig();
            return LubanCSVReader.ReadTable<ActionTableData>(config);
        }
    }
}
