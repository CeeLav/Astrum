using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using Astrum.LogicCore.ActionSystem;

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
        /// 读取所有技能动作数据
        /// </summary>
        public static List<SkillActionEditorData> ReadSkillActionData()
        {
            var editorDataList = new List<SkillActionEditorData>();
            
            try
            {
                // 1. 读取 ActionTable（基础动作数据）
                var actionDataDict = ReadActionTableAsDictionary();
                
                // 2. 读取 SkillActionTable（技能专属数据）
                var skillActionTableList = ReadSkillActionTableCSV();
                
                if (skillActionTableList == null || skillActionTableList.Count == 0)
                {
                    Debug.LogWarning($"{LOG_PREFIX} No skill action data found in SkillActionTable");
                    return editorDataList;
                }
                
                // 3. 合并两表数据
                foreach (var skillActionData in skillActionTableList)
                {
                    var editorData = ConvertToEditorData(skillActionData, actionDataDict);
                    if (editorData != null)
                    {
                        editorDataList.Add(editorData);
                    }
                }
                
                Debug.Log($"{LOG_PREFIX} Successfully loaded {editorDataList.Count} skill actions");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to read skill action data: {ex.Message}\n{ex.StackTrace}");
            }
            
            return editorDataList;
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
        /// 读取 SkillActionTable CSV
        /// </summary>
        private static List<SkillActionTableData> ReadSkillActionTableCSV()
        {
            var config = SkillActionTableData.GetTableConfig();
            
            try
            {
                var data = LubanCSVReader.ReadTable<SkillActionTableData>(config);
                Debug.Log($"{LOG_PREFIX} Read {data.Count} records from {config.FilePath}");
                return data;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to read SkillActionTable CSV: {ex.Message}");
                return new List<SkillActionTableData>();
            }
        }
        
        /// <summary>
        /// 转换表数据为编辑器数据
        /// </summary>
        private static SkillActionEditorData ConvertToEditorData(
            SkillActionTableData skillActionData,
            Dictionary<int, ActionTableData> actionDataDict)
        {
            if (skillActionData == null) return null;
            
            // 创建技能动作编辑器数据
            var editorData = SkillActionEditorData.CreateDefault(skillActionData.ActionId);
            
            // 从 ActionTable 填充基础动作数据
            if (actionDataDict.TryGetValue(skillActionData.ActionId, out var actionData))
            {
                FillBaseActionData(editorData, actionData);
            }
            else
            {
                Debug.LogWarning($"{LOG_PREFIX} ActionId {skillActionData.ActionId} not found in ActionTable, using defaults");
            }
            
            // 从 SkillActionTable 填充技能专属数据
            FillSkillActionData(editorData, skillActionData);
            
            // 解析触发帧字符串为触发效果列表
            editorData.ParseTriggerFrames();
            
            // 从触发效果列表构建时间轴事件
            editorData.TimelineEvents = editorData.BuildTimelineFromTriggerEffects();
            
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

