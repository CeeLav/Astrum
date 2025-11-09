using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using Astrum.Editor.RoleEditor.Timeline;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 动作数据组装器
    /// 负责从多个表读取数据并组装成完整的 ActionEditorData
    /// </summary>
    public static class ActionDataAssembler
    {
        private const string LOG_PREFIX = "[ActionDataAssembler]";
        
        /// <summary>
        /// 组装所有动作数据
        /// </summary>
        public static List<ActionEditorData> AssembleAll()
        {
            var result = new List<ActionEditorData>();
            
            try
            {
                // 1. 读取 ActionTable（基础数据）
                var actionTableDict = ReadActionTable();
                
                // 2. 读取扩展数据
                var skillExtensions = SkillActionExtensionReader.ReadAll();
                var moveExtensions = MoveActionExtensionReader.ReadAll();
                
                // 3. 组装数据
                foreach (var actionData in actionTableDict.Values)
                {
                    var editorData = ConvertToEditorData(actionData);
                    
                    // 根据类型附加扩展数据
                    if (editorData.IsSkill && skillExtensions.TryGetValue(editorData.ActionId, out var skillExt))
                    {
                        editorData.SkillExtension = skillExt;
                        Debug.Log($"{LOG_PREFIX} Attached SkillExtension to ActionId {editorData.ActionId}: TriggerFrames length={skillExt.TriggerFrames?.Length ?? 0}, TriggerEffects count={skillExt.TriggerEffects?.Count ?? 0}");
                    }
                    else if (editorData.IsSkill)
                    {
                        Debug.LogWarning($"{LOG_PREFIX} ActionId {editorData.ActionId} is skill type but no SkillExtension found in SkillActionTable");
                    }
                    
                    if (editorData.IsMove && moveExtensions.TryGetValue(editorData.ActionId, out var moveExt))
                    {
                        editorData.MoveExtension = moveExt;
                    }
                    
                    result.Add(editorData);
                }
                
                // 统计各类型动作数量
                var typeCounts = result
                    .GroupBy(a => a.ActionType?.ToLower() ?? "unknown")
                    .Select(g => $"{g.Key}({g.Count()})")
                    .ToList();
                Debug.Log($"{LOG_PREFIX} Assembled {result.Count} actions [{string.Join(", ", typeCounts)}]");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to assemble action data: {ex.Message}\n{ex.StackTrace}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 读取 ActionTable 并转换为字典
        /// </summary>
        private static Dictionary<int, ActionTableData> ReadActionTable()
        {
            var dict = new Dictionary<int, ActionTableData>();
            
            try
            {
                var config = ActionTableData.GetTableConfig();
                var tableData = Core.LubanCSVReader.ReadTable<ActionTableData>(config);
                
                foreach (var data in tableData)
                {
                    if (!dict.ContainsKey(data.ActionId))
                    {
                        dict[data.ActionId] = data;
                    }
                }
                
                Debug.Log($"{LOG_PREFIX} Read {dict.Count} actions from ActionTable");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to read ActionTable: {ex.Message}");
            }
            
            return dict;
        }
        
        /// <summary>
        /// 转换表数据为编辑器数据
        /// </summary>
        private static ActionEditorData ConvertToEditorData(ActionTableData tableData)
        {
            var editorData = ActionEditorData.CreateDefault(tableData.ActionId);
            
            // 填充基础字段
            editorData.ActionId = tableData.ActionId;
            editorData.ActionName = tableData.ActionName ?? "";
            editorData.ActionType = tableData.ActionType ?? "idle";
            editorData.Duration = tableData.Duration;
            editorData.AnimationPath = tableData.AnimationPath ?? "";
            editorData.AutoNextActionId = tableData.AutoNextActionId;
            editorData.KeepPlayingAnim = tableData.KeepPlayingAnim;
            editorData.AutoTerminate = tableData.AutoTerminate;
            editorData.Command = tableData.Command ?? "";
            editorData.Priority = tableData.Priority;
            editorData.CancelTagsJson = tableData.CancelTags ?? "";
            editorData.CancelTags = ActionDataReader.ParseCancelTagsFromJson(tableData.CancelTags ?? "");
            
            // 加载动画片段
            if (!string.IsNullOrEmpty(editorData.AnimationPath))
            {
                editorData.AnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(editorData.AnimationPath);
            }
            
            // 解析时间轴事件
            editorData.TimelineEvents = ParseTimelineEvents(tableData);
            
            // 清除新建和脏标记（从文件读取的数据是干净的）
            editorData.IsNew = false;
            editorData.IsDirty = false;
            
            return editorData;
        }
        
        /// <summary>
        /// 解析时间轴事件
        /// </summary>
        private static List<TimelineEvent> ParseTimelineEvents(ActionTableData tableData)
        {
            var events = new List<TimelineEvent>();
            
            // 解析 BeCancelledTags
            var beCancelEvents = ActionCancelTagParser.ParseBeCancelledTags(
                tableData.BeCancelledTags,
                tableData.ActionId,
                tableData.Duration
            );
            events.AddRange(beCancelEvents);
            
            return events;
        }
    }
}

