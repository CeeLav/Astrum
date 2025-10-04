using Astrum.LogicCore.ActionSystem;
using Astrum.CommonBase;
using cfg;
using cfg.Entity;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Astrum.LogicCore.Managers
{
    /// <summary>
    /// 动作配置管理器 - 工厂类，用于组装ActionInfo（单例）
    /// </summary>
    public class ActionConfigManager : Singleton<ActionConfigManager>
    {
        
        /// <summary>
        /// 获取动作信息（工厂方法）
        /// </summary>
        /// <param name="actionId">动作ID</param>
        /// <param name="entityId">实体ID</param>
        /// <returns>组装后的ActionInfo</returns>
        public ActionInfo? GetAction(int actionId, long entityId)
        {
            // 通过ConfigManager获取表格数据
            var configManager = ConfigManager.Instance;
            if (!configManager.IsInitialized)
            {
                ASLogger.Instance.Error($"ActionConfigManager.GetAction: ConfigManager not initialized, actionId={actionId}, entityId={entityId}");
                return null;
            }
            
            var tableData = configManager.Tables.TbActionTable.Get(actionId);
            if (tableData == null)
            {
                ASLogger.Instance.Error($"ActionConfigManager.GetAction: ActionTable not found, actionId={actionId}, entityId={entityId}");
                return null;
            }
            
            // 组装ActionInfo
            var actionInfo = new ActionInfo
            {
                Id = actionId,
                Catalog = tableData.ActionType,
                Priority = tableData.Priority,
                AutoNextActionId = tableData.AutoNextActionId,
                KeepPlayingAnim = tableData.KeepPlayingAnim,
                AutoTerminate = tableData.AutoTerminate,
                Duration = tableData.Duration
            };
            
            // 解析CancelTags JSON字符串
            actionInfo.CancelTags = ParseCancelTagsJson(tableData.CancelTags);
            
            // 解析BeCancelledTags JSON字符串
            actionInfo.BeCancelledTags = ParseBeCancelledTagsJson(tableData.BeCancelledTags);
            
            // 解析TempBeCancelledTags JSON字符串
            actionInfo.TempBeCancelledTags = ParseTempBeCancelledTagsJson(tableData.TempBeCancelledTags);
            
            // 解析Command字符串为ActionCommand
            actionInfo.Commands = ParseCommandString(tableData.Command);
            
            // TODO: 根据entityId获取运行时数据，与表格数据合并
            // 例如：从实体的状态、buff等获取额外的动作修改
            if (actionInfo.AutoNextActionId <= 0)
            {
                actionInfo.AutoNextActionId = 1001;//默认是静止
            }
            return actionInfo;
        }
        
        /// <summary>
        /// 解析CancelTags JSON字符串
        /// </summary>
        private List<CancelTag> ParseCancelTagsJson(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return new List<CancelTag>();
                
            try
            {
                var jsonData = JsonConvert.DeserializeObject<List<CancelTagJsonData>>(jsonString);
                return jsonData?.Select(ct => new CancelTag
                {
                    Tag = ct.Tag,
                    StartFromFrames = ct.StartFromFrames,
                    BlendInFrames = ct.BlendInFrames,
                    Priority = ct.Priority
                }).ToList() ?? new List<CancelTag>();
            }
            catch
            {
                return new List<CancelTag>();
            }
        }
        
        /// <summary>
        /// 解析BeCancelledTags JSON字符串
        /// </summary>
        private List<BeCancelledTag> ParseBeCancelledTagsJson(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return new List<BeCancelledTag>();
                
            try
            {
                var jsonData = JsonConvert.DeserializeObject<List<BeCancelledTagJsonData>>(jsonString);
                return jsonData?.Select(bt => new BeCancelledTag
                {
                    Tags = bt.Tags ?? new List<string>(),
                    RangeFrames = bt.RangeFrames ?? new List<int>(),
                    BlendOutFrames = bt.BlendOutFrames,
                    Priority = bt.Priority
                }).ToList() ?? new List<BeCancelledTag>();
            }
            catch
            {
                return new List<BeCancelledTag>();
            }
        }
        
        /// <summary>
        /// 解析TempBeCancelledTags JSON字符串
        /// </summary>
        private List<TempBeCancelledTag> ParseTempBeCancelledTagsJson(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return new List<TempBeCancelledTag>();
                
            try
            {
                var jsonData = JsonConvert.DeserializeObject<List<TempBeCancelledTagJsonData>>(jsonString);
                return jsonData?.Select(tt => new TempBeCancelledTag
                {
                    Id = tt.Id,
                    Tags = tt.Tags ?? new List<string>(),
                    DurationFrames = tt.DurationFrames,
                    BlendOutFrames = tt.BlendOutFrames,
                    Priority = tt.Priority
                }).ToList() ?? new List<TempBeCancelledTag>();
            }
            catch
            {
                return new List<TempBeCancelledTag>();
            }
        }
        
        /// <summary>
        /// 解析Command字符串为ActionCommand
        /// </summary>
        private List<ActionCommand> ParseCommandString(string commandString)
        {
            if (string.IsNullOrEmpty(commandString))
                return new List<ActionCommand>();
                
            // 简单的命令解析，假设命令格式为 "commandName:validFrames"
            var parts = commandString.Split(':');
            if (parts.Length >= 1)
            {
                var commandName = parts[0];
                var validFrames = parts.Length > 1 && int.TryParse(parts[1], out var frames) ? frames : 0;
                
                return new List<ActionCommand>
                {
                    new ActionCommand(commandName, validFrames)
                };
            }
            
            return new List<ActionCommand>();
        }
    }
    
    /// <summary>
    /// CancelTag JSON数据结构
    /// </summary>
    public class CancelTagJsonData
    {
        public string Tag { get; set; } = string.Empty;
        public int StartFromFrames { get; set; }
        public int BlendInFrames { get; set; }
        public int Priority { get; set; }
    }
    
    /// <summary>
    /// BeCancelledTag JSON数据结构
    /// </summary>
    public class BeCancelledTagJsonData
    {
        public List<string>? Tags { get; set; }
        public List<int>? RangeFrames { get; set; }
        public int BlendOutFrames { get; set; }
        public int Priority { get; set; }
    }
    
    /// <summary>
    /// TempBeCancelledTag JSON数据结构
    /// </summary>
    public class TempBeCancelledTagJsonData
    {
        public string Id { get; set; } = string.Empty;
        public List<string>? Tags { get; set; }
        public int DurationFrames { get; set; }
        public int BlendOutFrames { get; set; }
        public int Priority { get; set; }
    }
}
