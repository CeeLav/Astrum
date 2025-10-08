using System.Collections.Generic;
using System.Linq;
using Astrum.Editor.RoleEditor.Services;

namespace Astrum.Editor.RoleEditor.Data
{
    /// <summary>
    /// 技能数据验证器
    /// </summary>
    public static class SkillDataValidator
    {
        private const int MIN_SKILL_ID = 2000;
        private const int MAX_SKILL_ID = 9999;
        
        /// <summary>
        /// 验证技能数据
        /// </summary>
        public static bool Validate(SkillEditorData data, out List<string> errors)
        {
            errors = new List<string>();
            
            if (data == null)
            {
                errors.Add("技能数据为空");
                return false;
            }
            
            // 验证技能ID
            ValidateSkillId(data, errors);
            
            // 验证技能名称
            ValidateSkillName(data, errors);
            
            // 验证技能类型
            ValidateSkillType(data, errors);
            
            // 验证等级配置
            ValidateLevelConfig(data, errors);
            
            // 验证技能动作列表
            ValidateSkillActions(data, errors);
            
            // 验证技能动作与ActionTable的对应关系
            ValidateActionTableMapping(data, errors);
            
            // 验证触发帧配置
            ValidateTriggerFrames(data, errors);
            
            return errors.Count == 0;
        }
        
        /// <summary>
        /// 验证技能ID
        /// </summary>
        private static void ValidateSkillId(SkillEditorData data, List<string> errors)
        {
            if (data.SkillId < MIN_SKILL_ID || data.SkillId > MAX_SKILL_ID)
            {
                errors.Add($"技能ID {data.SkillId} 不在有效范围 [{MIN_SKILL_ID}, {MAX_SKILL_ID}] 内");
            }
        }
        
        /// <summary>
        /// 验证技能名称
        /// </summary>
        private static void ValidateSkillName(SkillEditorData data, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(data.SkillName))
            {
                errors.Add("技能名称不能为空");
            }
            else if (data.SkillName.Length > 50)
            {
                errors.Add($"技能名称过长（{data.SkillName.Length} > 50）");
            }
        }
        
        /// <summary>
        /// 验证技能类型
        /// </summary>
        private static void ValidateSkillType(SkillEditorData data, List<string> errors)
        {
            if ((int)data.SkillType < 1 || (int)data.SkillType > 4)
            {
                errors.Add($"技能类型 {data.SkillType} 无效（有效值: 1-4）");
            }
        }
        
        /// <summary>
        /// 验证等级配置
        /// </summary>
        private static void ValidateLevelConfig(SkillEditorData data, List<string> errors)
        {
            if (data.RequiredLevel < 1)
            {
                errors.Add($"学习所需等级 {data.RequiredLevel} 无效（最小值: 1）");
            }
            
            if (data.MaxLevel < 1)
            {
                errors.Add($"技能最大等级 {data.MaxLevel} 无效（最小值: 1）");
            }
            
            if (data.MaxLevel < data.RequiredLevel)
            {
                errors.Add($"技能最大等级 {data.MaxLevel} 不能小于学习所需等级 {data.RequiredLevel}");
            }
            
            if (data.DisplayCooldown < 0)
            {
                errors.Add($"展示冷却时间 {data.DisplayCooldown} 不能为负数");
            }
            
            if (data.DisplayCost < 0)
            {
                errors.Add($"展示法力消耗 {data.DisplayCost} 不能为负数");
            }
        }
        
        /// <summary>
        /// 验证技能动作列表
        /// </summary>
        private static void ValidateSkillActions(SkillEditorData data, List<string> errors)
        {
            if (data.SkillActions == null || data.SkillActions.Count == 0)
            {
                errors.Add("技能必须至少包含一个技能动作");
                return;
            }
            
            // 检查是否有重复的ActionId
            var actionIds = data.SkillActions.Select(a => a.ActionId).ToList();
            var duplicates = actionIds.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            
            if (duplicates.Count > 0)
            {
                errors.Add($"技能动作列表中存在重复的ActionId: {string.Join(", ", duplicates)}");
            }
            
            // 验证每个技能动作
            for (int i = 0; i < data.SkillActions.Count; i++)
            {
                var action = data.SkillActions[i];
                
                if (action.ActionId <= 0)
                {
                    errors.Add($"技能动作 #{i+1}: ActionId {action.ActionId} 无效");
                }
                
                if (action.SkillId != data.SkillId)
                {
                    errors.Add($"技能动作 #{i+1}: SkillId {action.SkillId} 与当前技能ID {data.SkillId} 不匹配");
                }
                
                if (action.ActualCost < 0)
                {
                    errors.Add($"技能动作 #{i+1}: 实际法力消耗 {action.ActualCost} 不能为负数");
                }
                
                if (action.ActualCooldown < 0)
                {
                    errors.Add($"技能动作 #{i+1}: 实际冷却时间 {action.ActualCooldown} 不能为负数");
                }
            }
        }
        
        /// <summary>
        /// 验证技能动作与ActionTable的对应关系（关键验证）
        /// </summary>
        private static void ValidateActionTableMapping(SkillEditorData data, List<string> errors)
        {
            if (data.SkillActions == null || data.SkillActions.Count == 0)
                return;
            
            foreach (var action in data.SkillActions)
            {
                // 检查ActionId是否在ActionTable中存在
                var actionTable = ConfigTableHelper.GetActionTable(action.ActionId);
                if (actionTable == null)
                {
                    errors.Add($"技能动作 ActionId {action.ActionId} 在 ActionTable 中不存在，必须一一对应");
                }
                else
                {
                    // 验证ActionType是否为"skill"
                    if (!string.IsNullOrEmpty(actionTable.ActionType) && 
                        !actionTable.ActionType.Equals("skill", System.StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"技能动作 ActionId {action.ActionId} 的 ActionType 不是 'skill'（当前: '{actionTable.ActionType}'）");
                    }
                }
            }
        }
        
        /// <summary>
        /// 验证触发帧配置
        /// </summary>
        private static void ValidateTriggerFrames(SkillEditorData data, List<string> errors)
        {
            if (data.SkillActions == null)
                return;
            
            foreach (var action in data.SkillActions)
            {
                if (action.TriggerFrames == null || action.TriggerFrames.Count == 0)
                    continue;
                
                foreach (var trigger in action.TriggerFrames)
                {
                    if (trigger.Frame < 0)
                    {
                        errors.Add($"技能动作 {action.ActionId}: 触发帧 {trigger.Frame} 不能为负数");
                    }
                    
                    if (string.IsNullOrWhiteSpace(trigger.TriggerType))
                    {
                        errors.Add($"技能动作 {action.ActionId}: 触发帧 {trigger.Frame} 的触发类型不能为空");
                    }
                    else
                    {
                        // 验证触发类型是否有效
                        var validTypes = new[] { "Collision", "Direct", "Condition" };
                        if (!validTypes.Contains(trigger.TriggerType))
                        {
                            errors.Add($"技能动作 {action.ActionId}: 触发帧 {trigger.Frame} 的触发类型 '{trigger.TriggerType}' 无效（有效值: {string.Join(", ", validTypes)}）");
                        }
                    }
                    
                    if (trigger.EffectId <= 0)
                    {
                        errors.Add($"技能动作 {action.ActionId}: 触发帧 {trigger.Frame} 的效果ID {trigger.EffectId} 无效");
                    }
                }
                
                // 检查触发帧是否按顺序排列
                var frames = action.TriggerFrames.Select(t => t.Frame).ToList();
                var sortedFrames = frames.OrderBy(f => f).ToList();
                if (!frames.SequenceEqual(sortedFrames))
                {
                    errors.Add($"技能动作 {action.ActionId}: 触发帧应按帧号从小到大排列");
                }
            }
        }
        
        /// <summary>
        /// 快速验证（只验证关键字段）
        /// </summary>
        public static bool QuickValidate(SkillEditorData data)
        {
            if (data == null)
                return false;
            
            if (string.IsNullOrWhiteSpace(data.SkillName))
                return false;
            
            if (data.SkillActions == null || data.SkillActions.Count == 0)
                return false;
            
            if (data.SkillActions.Any(a => a.ActionId <= 0))
                return false;
            
            return true;
        }
    }
}

