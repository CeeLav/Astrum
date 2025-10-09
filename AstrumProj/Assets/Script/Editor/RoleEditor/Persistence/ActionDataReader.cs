using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using Astrum.LogicCore.ActionSystem;
using Newtonsoft.Json;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 动作数据读取器
    /// 从 ActionTable.csv 读取数据并转换为编辑器数据模型
    /// </summary>
    public static class ActionDataReader
    {
        private const string LOG_PREFIX = "[ActionDataReader]";
        
        /// <summary>
        /// 读取所有动作数据
        /// </summary>
        public static List<ActionEditorData> ReadActionData()
        {
            var editorDataList = new List<ActionEditorData>();
            
            try
            {
                // 读取 ActionTable CSV
                var tableDataList = ReadActionTableCSV();
                
                if (tableDataList == null || tableDataList.Count == 0)
                {
                    Debug.LogWarning($"{LOG_PREFIX} No action data found in ActionTable");
                    return editorDataList;
                }
                
                // 转换为编辑器数据
                foreach (var tableData in tableDataList)
                {
                    var editorData = ConvertToEditorData(tableData);
                    if (editorData != null)
                    {
                        editorDataList.Add(editorData);
                    }
                }
                
                Debug.Log($"{LOG_PREFIX} Successfully loaded {editorDataList.Count} actions");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to read action data: {ex.Message}\n{ex.StackTrace}");
            }
            
            return editorDataList;
        }
        
        /// <summary>
        /// 读取 ActionTable CSV
        /// </summary>
        private static List<ActionTableData> ReadActionTableCSV()
        {
            var config = ActionTableData.GetTableConfig();
            
            try
            {
                var data = LubanCSVReader.ReadTable<ActionTableData>(config);
                Debug.Log($"{LOG_PREFIX} Read {data.Count} records from {config.FilePath}");
                return data;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to read ActionTable CSV: {ex.Message}");
                return new List<ActionTableData>();
            }
        }
        
        /// <summary>
        /// 转换表数据为编辑器数据
        /// </summary>
        private static ActionEditorData ConvertToEditorData(ActionTableData tableData)
        {
            if (tableData == null) return null;
            
            var editorData = ActionEditorData.CreateDefault(tableData.ActionId);
            
            // 复制基础字段
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
            editorData.CancelTags = ParseCancelTagsFromJson(tableData.CancelTags ?? "");
            
            // 从路径加载 AnimationClip
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
            
            // CancelTags 不需要解析为时间轴事件（它们是动作属性）
            // TempBeCancelledTags 是运行时数据，不从静态表读取
            
            // TODO: 后续可以从其他字段解析特效、音效等事件
            
            return events;
        }
        
        /// <summary>
        /// 解析 CancelTags JSON 字符串为编辑器数据结构
        /// </summary>
        private static List<EditorCancelTag> ParseCancelTagsFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new List<EditorCancelTag>();
                
            try
            {
                var jsonData = JsonConvert.DeserializeObject<List<CancelTagJsonData>>(json);
                return jsonData?.Select(ct => new EditorCancelTag(
                    ct.Tag,
                    ct.StartFromFrames,
                    ct.BlendInFrames,
                    ct.Priority
                )).ToList() ?? new List<EditorCancelTag>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ActionDataReader] Failed to parse CancelTags JSON: {ex.Message}");
                return new List<EditorCancelTag>();
            }
        }
        
        /// <summary>
        /// CancelTag JSON 数据结构
        /// </summary>
        [Serializable]
        private class CancelTagJsonData
        {
            public string Tag { get; set; } = string.Empty;
            public int StartFromFrames { get; set; }
            public int BlendInFrames { get; set; }
            public int Priority { get; set; }
        }
    }
}
