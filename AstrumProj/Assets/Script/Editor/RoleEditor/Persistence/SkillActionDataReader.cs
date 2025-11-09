using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using Astrum.LogicCore.ActionSystem;
using Astrum.Editor.RoleEditor.Timeline;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 技能动作数据读取器
    /// 从 SkillActionTable.csv 和 ActionTable.csv 读取数据并合并
    /// </summary>
    public static class SkillActionDataReader
    {
        private const string LOG_PREFIX = "[SkillActionDataReader]";
        
        /// <summary>
        /// 读取所有技能动作数据（使用新的 Assembler 架构）
        /// </summary>
        public static List<SkillActionEditorData> ReadSkillActionData()
        {
            try
            {
                // 使用新的 ActionDataAssembler 读取所有动作
                var actions = ActionDataAssembler.AssembleAll();
                
                // 转换为 SkillActionEditorData（向后兼容）
                var skillActions = ActionEditorDataAdapter.ToSkillActionEditorDataList(actions);
                
                // 为每个技能动作构建时间轴事件
                foreach (var skillAction in skillActions)
                {
                    if (skillAction.ActionType?.ToLower() == "skill")
                    {
                        // 解析触发帧字符串为触发效果列表
                        skillAction.ParseTriggerFrames();
                        
                        // 从触发效果列表构建时间轴事件，并与基础轨道事件合并
                        var triggerTimelineEvents = skillAction.BuildTimelineFromTriggerEffects() ?? new List<Timeline.TimelineEvent>();
                        if (skillAction.TimelineEvents == null)
                        {
                            skillAction.TimelineEvents = new List<Timeline.TimelineEvent>();
                        }
                        
                        skillAction.TimelineEvents.AddRange(triggerTimelineEvents);
                        skillAction.TimelineEvents.Sort((a, b) => a.StartFrame.CompareTo(b.StartFrame));
                        
                        Debug.Log($"{LOG_PREFIX} Built {triggerTimelineEvents.Count} timeline events from trigger frames for ActionId {skillAction.ActionId}");
                    }
                }
                
                return skillActions;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to read skill action data: {ex.Message}\n{ex.StackTrace}");
                return new List<SkillActionEditorData>();
            }
        }
        
        /// <summary>
        /// 读取 ActionTable 并转换为字典（以 ActionId 为键）
        /// </summary>
        private static Dictionary<int, ActionTableData> ReadActionTableAsDictionary()
        {
            var dict = new Dictionary<int, ActionTableData>();
            
            try
            {
                var config = ActionTableData.GetTableConfig();
                var actionList = LubanCSVReader.ReadTable<ActionTableData>(config);
                
                foreach (var action in actionList)
                {
                    if (!dict.ContainsKey(action.ActionId))
                    {
                        dict[action.ActionId] = action;
                    }
                }
                
                Debug.Log($"{LOG_PREFIX} Read {dict.Count} action records from ActionTable");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to read ActionTable: {ex.Message}");
            }
            
            return dict;
        }
        
        /// <summary>
        /// 读取 SkillActionTable 并转换为字典（以 ActionId 为键）
        /// </summary>
        private static Dictionary<int, SkillActionTableData> ReadSkillActionTableAsDictionary()
        {
            var dict = new Dictionary<int, SkillActionTableData>();
            
            try
            {
                var config = SkillActionTableData.GetTableConfig();
                var data = LubanCSVReader.ReadTable<SkillActionTableData>(config);
                
                foreach (var skillAction in data)
                {
                    if (!dict.ContainsKey(skillAction.ActionId))
                    {
                        dict[skillAction.ActionId] = skillAction;
                    }
                }
                
                Debug.Log($"{LOG_PREFIX} Read {dict.Count} skill action records from SkillActionTable");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"{LOG_PREFIX} Failed to read SkillActionTable (may not exist yet): {ex.Message}");
            }
            
            return dict;
        }
        
        /// <summary>
        /// 读取 MoveActionTable 并转换为字典（以 ActionId 为键）
        /// </summary>
        private static Dictionary<int, MoveActionTableData> ReadMoveActionTableAsDictionary()
        {
            var dict = new Dictionary<int, MoveActionTableData>();
            
            try
            {
                var config = MoveActionTableData.GetTableConfig();
                var moveActionList = LubanCSVReader.ReadTable<MoveActionTableData>(config);
                
                foreach (var moveAction in moveActionList)
                {
                    if (!dict.ContainsKey(moveAction.ActionId))
                    {
                        dict[moveAction.ActionId] = moveAction;
                    }
                }
                
                Debug.Log($"{LOG_PREFIX} Read {dict.Count} move action records from MoveActionTable");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"{LOG_PREFIX} Failed to read MoveActionTable (may not exist yet): {ex.Message}");
            }
            
            return dict;
        }
        
        /// <summary>
        /// 转换表数据为编辑器数据
        /// </summary>
        private static SkillActionEditorData ConvertToEditorData(
            ActionTableData actionData,
            SkillActionTableData skillActionData,
            Dictionary<int, MoveActionTableData> moveActionDataDict)
        {
            if (actionData == null) return null;
            
            // 创建技能动作编辑器数据
            var editorData = SkillActionEditorData.CreateDefault(actionData.ActionId);
            
            // 从 ActionTable 填充基础动作数据
            FillBaseActionData(editorData, actionData);
            
            // 从 SkillActionTable 填充技能专属数据（如果存在）
            if (skillActionData != null)
            {
                FillSkillActionData(editorData, skillActionData);
            }
            else
            {
                // 没有 SkillActionTable 数据时，使用默认值
                editorData.ActualCost = 0;
                editorData.ActualCooldown = 0;
                editorData.TriggerFrames = "";
            }
            
            // 如果是 move 类型，从 MoveActionTable 填充移动专属数据
            if (string.Equals(editorData.ActionType, "move", StringComparison.OrdinalIgnoreCase))
            {
                if (moveActionDataDict.TryGetValue(actionData.ActionId, out var moveActionData))
                {
                    FillMoveActionData(editorData, moveActionData);
                }
                else
                {
                    Debug.LogWarning($"{LOG_PREFIX} Move action {actionData.ActionId} not found in MoveActionTable");
                }
            }
            
            // 解析触发帧字符串为触发效果列表
            editorData.ParseTriggerFrames();
            
            // 从触发效果列表构建时间轴事件，并与基础轨道事件合并
            var triggerTimelineEvents = editorData.BuildTimelineFromTriggerEffects() ?? new List<TimelineEvent>();
            if (editorData.TimelineEvents == null)
            {
                editorData.TimelineEvents = new List<TimelineEvent>();
            }

            editorData.TimelineEvents.AddRange(triggerTimelineEvents);
            editorData.TimelineEvents.Sort((a, b) => a.StartFrame.CompareTo(b.StartFrame));
            
            // 清除新建和脏标记（从文件读取的数据是干净的）
            editorData.IsNew = false;
            editorData.IsDirty = false;
            
            return editorData;
        }
        
        /// <summary>
        /// 从 ActionTable 数据填充基础动作字段
        /// </summary>
        private static void FillBaseActionData(SkillActionEditorData editorData, ActionTableData actionData)
        {
            editorData.ActionId = actionData.ActionId;
            editorData.ActionName = actionData.ActionName ?? "";
            editorData.ActionType = actionData.ActionType ?? "skill";
            editorData.Duration = actionData.Duration;
            editorData.AnimationPath = actionData.AnimationPath ?? "";
            editorData.AutoNextActionId = actionData.AutoNextActionId;
            editorData.KeepPlayingAnim = actionData.KeepPlayingAnim;
            editorData.AutoTerminate = actionData.AutoTerminate;
            editorData.Command = actionData.Command ?? "";
            editorData.Priority = actionData.Priority;
            editorData.CancelTagsJson = actionData.CancelTags ?? "";
            editorData.CancelTags = ActionDataReader.ParseCancelTagsFromJson(actionData.CancelTags ?? "");
            
            // 从路径加载 AnimationClip
            if (!string.IsNullOrEmpty(editorData.AnimationPath))
            {
                editorData.AnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(editorData.AnimationPath);
            }
            
            // 解析时间轴事件（BeCancelledTags 等）
            editorData.TimelineEvents = ParseTimelineEvents(actionData);
        }
        
        /// <summary>
        /// 从 SkillActionTable 数据填充技能专属字段
        /// </summary>
        private static void FillSkillActionData(SkillActionEditorData editorData, SkillActionTableData skillActionData)
        {
            editorData.ActualCost = skillActionData.ActualCost;
            editorData.ActualCooldown = skillActionData.ActualCooldown;
            editorData.TriggerFrames = skillActionData.TriggerFrames ?? "";
        }
        
        /// <summary>
        /// 从 MoveActionTable 数据填充移动动作专属字段
        /// </summary>
        private static void FillMoveActionData(SkillActionEditorData editorData, MoveActionTableData moveActionData)
        {
            // 加载 RootMotionData（如果存在）
            if (moveActionData.RootMotionData != null && moveActionData.RootMotionData.Count > 0)
            {
                editorData.RootMotionDataArray = moveActionData.RootMotionData;
                Debug.Log($"{LOG_PREFIX} Loaded move action {moveActionData.ActionId} with speed {moveActionData.MoveSpeed} and {moveActionData.RootMotionData.Count} root motion data points");
            }
            else
            {
                Debug.Log($"{LOG_PREFIX} Loaded move action {moveActionData.ActionId} with speed {moveActionData.MoveSpeed}");
            }
        }
        
        /// <summary>
        /// 解析时间轴事件
        /// </summary>
        private static List<Timeline.TimelineEvent> ParseTimelineEvents(ActionTableData tableData)
        {
            var events = new List<Timeline.TimelineEvent>();
            
            // 解析 BeCancelledTags
            var beCancelEvents = ActionCancelTagParser.ParseBeCancelledTags(
                tableData.BeCancelledTags,
                tableData.ActionId,
                tableData.Duration
            );
            events.AddRange(beCancelEvents);
            
            // TODO: Phase 3 - 从触发帧数据构建技能效果时间轴事件
            
            return events;
        }
    }
}

