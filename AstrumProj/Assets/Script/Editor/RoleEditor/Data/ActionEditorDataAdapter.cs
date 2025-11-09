using System.Collections.Generic;
using System.Linq;

namespace Astrum.Editor.RoleEditor.Data
{
    /// <summary>
    /// ActionEditorData 适配器
    /// 用于在新架构（ActionEditorData + Extensions）和旧架构（SkillActionEditorData）之间转换
    /// </summary>
    public static class ActionEditorDataAdapter
    {
        /// <summary>
        /// 将 ActionEditorData 列表转换为 SkillActionEditorData 列表（向后兼容）
        /// </summary>
        public static List<SkillActionEditorData> ToSkillActionEditorDataList(List<ActionEditorData> actions)
        {
            var result = new List<SkillActionEditorData>();
            
            foreach (var action in actions)
            {
                var skillAction = ToSkillActionEditorData(action);
                result.Add(skillAction);
            }
            
            return result;
        }
        
        /// <summary>
        /// 将单个 ActionEditorData 转换为 SkillActionEditorData
        /// </summary>
        public static SkillActionEditorData ToSkillActionEditorData(ActionEditorData action)
        {
            var skillAction = SkillActionEditorData.CreateDefault(action.ActionId);
            
            // 复制基础字段
            skillAction.ActionId = action.ActionId;
            skillAction.ActionName = action.ActionName;
            skillAction.ActionType = action.ActionType;
            skillAction.AnimationDuration = action.AnimationDuration;
            skillAction.Duration = action.Duration;
            skillAction.AnimationPath = action.AnimationPath;
            skillAction.AnimationClip = action.AnimationClip;
            skillAction.Priority = action.Priority;
            skillAction.AutoNextActionId = action.AutoNextActionId;
            skillAction.KeepPlayingAnim = action.KeepPlayingAnim;
            skillAction.AutoTerminate = action.AutoTerminate;
            skillAction.Command = action.Command;
            skillAction.CancelTags = action.CancelTags;
            skillAction.CancelTagsJson = action.CancelTagsJson;
            skillAction.TimelineEvents = action.TimelineEvents;
            skillAction.IsDirty = action.IsDirty;
            skillAction.IsNew = action.IsNew;
            
            // 复制技能扩展数据
            if (action.SkillExtension != null)
            {
                skillAction.ActualCost = action.SkillExtension.ActualCost;
                skillAction.ActualCooldown = action.SkillExtension.ActualCooldown;
                skillAction.TriggerFrames = action.SkillExtension.TriggerFrames;
                skillAction.TriggerEffects = action.SkillExtension.TriggerEffects;
                skillAction.RootMotionDataArray = action.SkillExtension.RootMotionData;
            }
            
            // 复制移动扩展数据
            if (action.MoveExtension != null)
            {
                skillAction.RootMotionDataArray = action.MoveExtension.RootMotionData;
            }
            
            return skillAction;
        }
        
        /// <summary>
        /// 将 SkillActionEditorData 列表转换为 ActionEditorData 列表（新架构）
        /// </summary>
        public static List<ActionEditorData> ToActionEditorDataList(List<SkillActionEditorData> skillActions)
        {
            var result = new List<ActionEditorData>();
            
            foreach (var skillAction in skillActions)
            {
                var action = ToActionEditorData(skillAction);
                result.Add(action);
            }
            
            return result;
        }
        
        /// <summary>
        /// 将单个 SkillActionEditorData 转换为 ActionEditorData
        /// </summary>
        public static ActionEditorData ToActionEditorData(SkillActionEditorData skillAction)
        {
            var action = ActionEditorData.CreateDefault(skillAction.ActionId);
            
            // 复制基础字段
            action.ActionId = skillAction.ActionId;
            action.ActionName = skillAction.ActionName;
            action.ActionType = skillAction.ActionType;
            action.AnimationDuration = skillAction.AnimationDuration;
            action.Duration = skillAction.Duration;
            action.AnimationPath = skillAction.AnimationPath;
            action.AnimationClip = skillAction.AnimationClip;
            action.Priority = skillAction.Priority;
            action.AutoNextActionId = skillAction.AutoNextActionId;
            action.KeepPlayingAnim = skillAction.KeepPlayingAnim;
            action.AutoTerminate = skillAction.AutoTerminate;
            action.Command = skillAction.Command;
            action.CancelTags = skillAction.CancelTags;
            action.CancelTagsJson = skillAction.CancelTagsJson;
            action.TimelineEvents = skillAction.TimelineEvents;
            action.IsDirty = skillAction.IsDirty;
            action.IsNew = skillAction.IsNew;
            
            // 根据类型创建扩展数据
            if (action.IsSkill)
            {
                action.SkillExtension = new SkillActionExtension
                {
                    ActualCost = skillAction.ActualCost,
                    ActualCooldown = skillAction.ActualCooldown,
                    TriggerFrames = skillAction.TriggerFrames,
                    TriggerEffects = skillAction.TriggerEffects,
                    RootMotionData = skillAction.RootMotionDataArray ?? new List<int>()
                };
            }
            
            if (action.IsMove)
            {
                action.MoveExtension = new MoveActionExtension
                {
                    MoveSpeed = 0, // 会在保存时自动计算
                    RootMotionData = skillAction.RootMotionDataArray ?? new List<int>()
                };
            }
            
            return action;
        }
    }
}

