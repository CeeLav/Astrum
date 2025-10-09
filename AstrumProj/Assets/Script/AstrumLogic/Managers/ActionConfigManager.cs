using Astrum.LogicCore.ActionSystem;
using Astrum.LogicCore.SkillSystem;
using Astrum.CommonBase;
using cfg;
using cfg.Entity;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Astrum.LogicCore.Managers
{
    /// <summary>
    /// 动作配置管理器 - 工厂类，用于组装ActionInfo及其派生类（单例）
    /// </summary>
    public class ActionConfigManager : Singleton<ActionConfigManager>
    {
        /// <summary>
        /// 获取动作信息（工厂方法）
        /// 根据动作类型自动返回 ActionInfo 或 SkillActionInfo
        /// </summary>
        /// <param name="actionId">动作ID</param>
        /// <param name="entityId">实体ID</param>
        /// <returns>组装后的ActionInfo或其派生类</returns>
        public ActionInfo? GetAction(int actionId, long entityId)
        {
            // 通过ConfigManager获取表格数据
            var configManager = ConfigManager.Instance;
            if (!configManager.IsInitialized)
            {
                ASLogger.Instance.Error($"ActionConfigManager.GetAction: ConfigManager not initialized, actionId={actionId}, entityId={entityId}");
                return null;
            }
            
            var actionTable = configManager.Tables.TbActionTable.Get(actionId);
            if (actionTable == null)
            {
                ASLogger.Instance.Error($"ActionConfigManager.GetAction: ActionTable not found, actionId={actionId}, entityId={entityId}");
                return null;
            }
            
            // 检查是否是技能动作（根据 ActionType 判断）
            if (actionTable.ActionType.Equals("Skill", System.StringComparison.OrdinalIgnoreCase))
            {
                return ConstructSkillActionInfo(actionId, entityId, actionTable);
            }
            else
            {
                return ConstructActionInfo(actionId, entityId, actionTable);
            }
        }
        
        /// <summary>
        /// 构造普通动作信息
        /// </summary>
        private ActionInfo ConstructActionInfo(int actionId, long entityId, cfg.Entity.ActionTable actionTable)
        {
            var actionInfo = new NormalActionInfo();
            
            // 填充基类字段（通用逻辑）
            PopulateBaseActionFields(actionInfo, actionTable);
            
            return actionInfo;
        }
        
        /// <summary>
        /// 构造技能动作信息
        /// </summary>
        private SkillActionInfo? ConstructSkillActionInfo(int actionId, long entityId, cfg.Entity.ActionTable actionTable)
        {
            // 从 SkillActionTable 获取技能专属数据
            var configManager = ConfigManager.Instance;
            var skillActionTable = configManager.Tables.TbSkillActionTable.Get(actionId);
            
            if (skillActionTable == null)
            {
                ASLogger.Instance.Error($"ActionConfigManager.ConstructSkillActionInfo: SkillActionTable not found for actionId={actionId}");
                return null;
            }
            
            // 创建 SkillActionInfo 实例
            var skillActionInfo = new SkillActionInfo();
            
            // 复用基类字段填充逻辑
            PopulateBaseActionFields(skillActionInfo, actionTable);
            
            // 填充派生类特有字段
            PopulateSkillActionFields(skillActionInfo, skillActionTable);
            
            ASLogger.Instance.Debug($"ActionConfigManager: Constructed SkillActionInfo for actionId={actionId}, skillId={skillActionInfo.SkillId}");
            
            return skillActionInfo;
        }
        
        /// <summary>
        /// 填充 ActionInfo 基类字段（通用逻辑，避免代码重复）
        /// </summary>
        /// <param name="actionInfo">要填充的 ActionInfo 实例（或其派生类实例）</param>
        /// <param name="actionTable">ActionTable 配置数据</param>
        public void PopulateBaseActionFields(ActionInfo actionInfo, cfg.Entity.ActionTable actionTable)
        {
            // 基础字段
            actionInfo.Id = actionTable.ActionId;
            actionInfo.Catalog = actionTable.ActionType;
            actionInfo.Priority = actionTable.Priority;
            actionInfo.AutoNextActionId = actionTable.AutoNextActionId;
            actionInfo.KeepPlayingAnim = actionTable.KeepPlayingAnim;
            actionInfo.AutoTerminate = actionTable.AutoTerminate;
            actionInfo.Duration = actionTable.Duration;
            
            // 解析复杂字段（JSON 字符串）
            actionInfo.CancelTags = ParseCancelTagsJson(actionTable.CancelTags);
            actionInfo.BeCancelledTags = ParseBeCancelledTagsJson(actionTable.BeCancelledTags);
            
            // TempBeCancelledTags 是运行时数据，初始化为空列表（由技能系统动态添加）
            actionInfo.TempBeCancelledTags = new List<TempBeCancelledTag>();
            
            actionInfo.Commands = ParseCommandString(actionTable.Command);
            
            // 默认值处理
            if (actionInfo.AutoNextActionId <= 0)
            {
                actionInfo.AutoNextActionId = 1001; // 默认是静止
            }
        }
        
        /// <summary>
        /// 填充 SkillActionInfo 派生类字段
        /// </summary>
        /// <param name="skillActionInfo">要填充的 SkillActionInfo 实例</param>
        /// <param name="skillActionTable">SkillActionTable 配置数据</param>
        public void PopulateSkillActionFields(SkillActionInfo skillActionInfo, cfg.Skill.SkillActionTable skillActionTable)
        {
            skillActionInfo.SkillId = skillActionTable.SkillId;
            skillActionInfo.AttackBoxInfo = skillActionTable.AttackBoxInfo ?? string.Empty;
            skillActionInfo.ActualCost = skillActionTable.ActualCost;
            skillActionInfo.ActualCooldown = skillActionTable.ActualCooldown;
            skillActionInfo.TriggerFrames = skillActionTable.TriggerFrames ?? string.Empty;
        }
        
        // ========== 以下是 JSON 解析方法 ==========
        
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
        /// 注意：此方法保留用于运行时动态解析（如技能系统调用），不从静态表读取
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
    
    // ========== JSON解析辅助类（保持不变）==========
    
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
