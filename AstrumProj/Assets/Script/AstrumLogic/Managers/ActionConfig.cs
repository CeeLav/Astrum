using Astrum.LogicCore.ActionSystem;
using Astrum.LogicCore.SkillSystem;
using Astrum.CommonBase;
using cfg;
using cfg.BaseUnit;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using TrueSync;

namespace Astrum.LogicCore.Managers
{
    /// <summary>
    /// 动作配置管理器 - 工厂类，用于组装ActionInfo及其派生类（单例）
    /// </summary>
    public class ActionConfig : Singleton<ActionConfig>
    {
        /// <summary>
        /// 获取动作信息（工厂方法）
        /// 统一返回 ActionInfo，根据 ActionType 填充对应的 Extension
        /// </summary>
        /// <param name="actionId">动作ID</param>
        /// <param name="entityId">实体ID</param>
        /// <returns>组装后的ActionInfo</returns>
        public ActionInfo? GetAction(int actionId, long entityId)
        {
            // 通过ConfigManager获取表格数据
            var configManager = TableConfig.Instance;
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
            
            // 创建 ActionInfo 实例
            var actionInfo = new ActionInfo();
            
            // 填充基础字段（通用逻辑）
            PopulateBaseActionFields(actionInfo, actionTable);
            
            // 根据 ActionType 填充对应的 Extension
            if (actionTable.ActionType.Equals("Skill", System.StringComparison.OrdinalIgnoreCase))
            {
                var skillActionTable = configManager.Tables.TbSkillActionTable.Get(actionId);
                if (skillActionTable == null)
                {
                    ASLogger.Instance.Error($"ActionConfigManager.GetAction: SkillActionTable not found for actionId={actionId}");
                    return null;
                }
                actionInfo.SkillExtension = CreateSkillExtension(skillActionTable, actionId);
            }
            else if (actionTable.ActionType.Equals("Move", System.StringComparison.OrdinalIgnoreCase))
            {
                var moveActionTable = configManager.Tables.TbMoveActionTable.Get(actionId);
                if (moveActionTable == null)
                {
                    ASLogger.Instance.Error($"ActionConfigManager.GetAction: MoveActionTable not found for actionId={actionId}");
                    return null;
                }
                actionInfo.MoveExtension = CreateMoveExtension(moveActionTable, actionId);
            }
            // 其他类型动作，Extension 保持为 null
            
            return actionInfo;
        }
        
        /// <summary>
        /// 创建移动动作扩展数据
        /// </summary>
        private MoveActionExtension CreateMoveExtension(cfg.BaseUnit.MoveActionTable moveActionTable, int actionId)
        {
            var extension = new MoveActionExtension
            {
                MoveSpeed = moveActionTable.MoveSpeed,
                RootMotionData = LoadMoveRootMotionData(moveActionTable, actionId)
            };
            
            return extension;
        }
        
        /// <summary>
        /// 创建技能动作扩展数据
        /// </summary>
        private SkillActionExtension CreateSkillExtension(cfg.Skill.SkillActionTable skillActionTable, int actionId)
        {
            // 注意：TriggerEffects 需要根据技能等级解析，这里先初始化为空列表
            // 实际使用时由 SkillConfig 根据技能等级解析 TriggerFrames
            var extension = new SkillActionExtension
            {
                ActualCost = skillActionTable.ActualCost,
                ActualCooldown = skillActionTable.ActualCooldown,
                TriggerFrames = skillActionTable.TriggerFrames ?? string.Empty,
                TriggerEffects = new List<TriggerFrameInfo>(), // 初始为空，由 SkillConfig 解析
                RootMotionData = LoadSkillRootMotionData(skillActionTable, actionId)
            };
            
            return extension;
        }
        
        /// <summary>
        /// 填充 ActionInfo 基类字段（通用逻辑，避免代码重复）
        /// </summary>
        /// <param name="actionInfo">要填充的 ActionInfo 实例（或其派生类实例）</param>
        /// <param name="actionTable">ActionTable 配置数据</param>
        public void PopulateBaseActionFields(ActionInfo actionInfo, cfg.BaseUnit.ActionTable actionTable)
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
            
            // 从Commands列表创建ActionCommand对象
            actionInfo.Commands = new List<ActionCommand>();
            if (actionTable.Commands != null && actionTable.Commands.Any())
            {
                foreach (var cmdName in actionTable.Commands)
                {
                    if (!string.IsNullOrEmpty(cmdName))
                    {
                        actionInfo.Commands.Add(new ActionCommand(cmdName, 0));
                    }
                }
            }
            
            // 默认值处理
            if (actionInfo.AutoNextActionId <= 0)
            {
                actionInfo.AutoNextActionId = 1001; // 默认是静止
            }

            // 默认动画倍率
            actionInfo.AnimationSpeedMultiplier = 1f;
        }

        
        /// <summary>
        /// 从 MoveActionTable 加载根节点位移数据
        /// </summary>
        /// <param name="moveActionTable">移动动作表数据</param>
        /// <param name="actionId">动作ID（用于日志）</param>
        /// <returns>根节点位移数据，如果不存在或加载失败则返回空的 AnimationRootMotionData</returns>
        private AnimationRootMotionData LoadMoveRootMotionData(cfg.BaseUnit.MoveActionTable moveActionTable, int actionId)
        {
            // 检查 RootMotionData 是否为空
            if (moveActionTable.RootMotionData == null || moveActionTable.RootMotionData.Length == 0)
            {
                return new AnimationRootMotionData();
            }
            
            // 转换为 List<int>（RootMotionDataConverter 需要 List<int>）
            var rootMotionDataList = new System.Collections.Generic.List<int>(moveActionTable.RootMotionData);
            
            // 转换为运行时数据（整型转定点数）
            try
            {
                var rootMotionData = RootMotionDataConverter.ConvertFromIntArray(rootMotionDataList);
                
                if (rootMotionData != null && rootMotionData.HasMotion)
                {
                    ASLogger.Instance.Debug($"[ActionConfig] Loaded root motion data for move action {actionId}: {rootMotionData.TotalFrames} frames");
                    return rootMotionData;
                }
                else
                {
                    // 数据转换成功但没有有效位移
                    return new AnimationRootMotionData();
                }
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Warning($"[ActionConfig] Failed to convert root motion data for move action {actionId}: {ex.Message}");
                return new AnimationRootMotionData();
            }
        }
        
        /// <summary>
        /// 从 SkillActionTable 加载根节点位移数据
        /// </summary>
        /// <param name="skillActionTable">技能动作表数据</param>
        /// <param name="actionId">动作ID（用于日志）</param>
        /// <returns>根节点位移数据，如果不存在或加载失败则返回空的 AnimationRootMotionData</returns>
        public AnimationRootMotionData LoadSkillRootMotionData(cfg.Skill.SkillActionTable skillActionTable, int actionId)
        {
            // 检查 RootMotionData 是否为空
            if (skillActionTable.RootMotionData == null || skillActionTable.RootMotionData.Length == 0)
            {
                return new AnimationRootMotionData();
            }
            
            // 转换为 List<int>（RootMotionDataConverter 需要 List<int>）
            var rootMotionDataList = new System.Collections.Generic.List<int>(skillActionTable.RootMotionData);
            
            // 转换为运行时数据（整型转定点数）
            try
            {
                var rootMotionData = RootMotionDataConverter.ConvertFromIntArray(rootMotionDataList);
                
                if (rootMotionData != null && rootMotionData.HasMotion)
                {
                    ASLogger.Instance.Debug($"[ActionConfig] Loaded root motion data for action {actionId}: {rootMotionData.TotalFrames} frames");
                    return rootMotionData;
                }
                else
                {
                    // 数据转换成功但没有有效位移
                    return new AnimationRootMotionData();
                }
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Warning($"[ActionConfig] Failed to convert root motion data for action {actionId}: {ex.Message}");
                return new AnimationRootMotionData();
            }
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
