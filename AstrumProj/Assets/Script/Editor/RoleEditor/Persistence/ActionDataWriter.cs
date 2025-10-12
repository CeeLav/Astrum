using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using Astrum.LogicCore.ActionSystem;
using Newtonsoft.Json;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 动作数据写入器
    /// 将编辑器数据写入到 ActionTable.csv
    /// </summary>
    public static class ActionDataWriter
    {
        private const string LOG_PREFIX = "[ActionDataWriter]";
        
        /// <summary>
        /// 写入所有动作数据
        /// </summary>
        public static bool WriteActionData(List<ActionEditorData> actions)
        {
            if (actions == null || actions.Count == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} No action data to write");
                return false;
            }
            
            try
            {
                // 转换为表数据
                var tableDataList = ConvertToTableDataList(actions);
                
                // 写入 CSV
                bool success = WriteActionTableCSV(tableDataList);
                
                if (success)
                {
                    Debug.Log($"{LOG_PREFIX} Successfully wrote {actions.Count} actions to CSV");
                    
                    // 自动打表
                    Core.LubanTableGenerator.GenerateClientTables(showDialog: false);
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to write action data: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 转换编辑器数据列表为表数据列表
        /// </summary>
        private static List<ActionTableData> ConvertToTableDataList(List<ActionEditorData> editorDataList)
        {
            var tableDataList = new List<ActionTableData>();
            
            foreach (var editorData in editorDataList)
            {
                var tableData = ConvertToTableData(editorData);
                if (tableData != null)
                {
                    tableDataList.Add(tableData);
                }
            }
            
            return tableDataList;
        }
        
        /// <summary>
        /// 转换编辑器数据为表数据
        /// </summary>
        private static ActionTableData ConvertToTableData(ActionEditorData editorData)
        {
            if (editorData == null) return null;
            
            var tableData = new ActionTableData
            {
                ActionId = editorData.ActionId,
                ActionName = editorData.ActionName,
                ActionType = editorData.ActionType,
                Duration = editorData.Duration,
                AnimationPath = editorData.AnimationPath,
                AutoNextActionId = editorData.AutoNextActionId,
                KeepPlayingAnim = editorData.KeepPlayingAnim,
                AutoTerminate = editorData.AutoTerminate,
                Command = editorData.Command,
                Priority = editorData.Priority
            };
            
            // 序列化时间轴事件
            tableData.BeCancelledTags = ActionCancelTagSerializer.SerializeBeCancelledTags(editorData.TimelineEvents);
            
            // CancelTags 从运行时数据结构序列化为 JSON
            tableData.CancelTags = SerializeCancelTagsToJson(editorData.CancelTags ?? new List<EditorCancelTag>());
            
            // TempBeCancelledTags 是运行时数据，不写入静态表
            
            return tableData;
        }
        
        /// <summary>
        /// 写入 ActionTable CSV
        /// </summary>
        private static bool WriteActionTableCSV(List<ActionTableData> tableData)
        {
            var config = ActionTableData.GetTableConfig();
            
            try
            {
                bool success = LubanCSVWriter.WriteTable(config, tableData, enableBackup: true);
                
                if (success)
                {
                    Debug.Log($"{LOG_PREFIX} Successfully wrote ActionTable to {config.FilePath}");
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to write ActionTable");
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to write ActionTable CSV: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 序列化 CancelTags 为 JSON 字符串
        /// </summary>
        internal static string SerializeCancelTagsToJson(List<EditorCancelTag> cancelTags)
        {
            if (cancelTags == null || cancelTags.Count == 0)
                return "";
                
            try
            {
                var jsonData = cancelTags.Select(ct => new CancelTagJsonData
                {
                    Tag = ct.tag,
                    StartFromFrames = ct.startFrame,
                    BlendInFrames = ct.blendFrame,
                    Priority = ct.priority
                }).ToList();
                
                return JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ActionDataWriter] Failed to serialize CancelTags: {ex.Message}");
                return "";
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
