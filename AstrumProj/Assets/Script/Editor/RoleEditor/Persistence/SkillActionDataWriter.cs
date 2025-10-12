using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 技能动作数据写入器
    /// 将编辑器数据写入到 SkillActionTable.csv 和 ActionTable.csv
    /// </summary>
    public static class SkillActionDataWriter
    {
        private const string LOG_PREFIX = "[SkillActionDataWriter]";
        
        /// <summary>
        /// 写入所有技能动作数据
        /// </summary>
        public static bool WriteSkillActionData(List<SkillActionEditorData> skillActions)
        {
            if (skillActions == null || skillActions.Count == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} No skill action data to write");
                return false;
            }
            
            try
            {
                // 在写入前强制同步时间轴事件到触发帧
                foreach (var skillAction in skillActions)
                {
                    skillAction.SyncFromTimelineEvents(skillAction.TimelineEvents);
                }
                
                // 1. 转换为 ActionTable 数据并写入
                var actionTableData = ConvertToActionTableDataList(skillActions);
                bool actionSuccess = WriteActionTableCSV(actionTableData);
                
                // 2. 转换为 SkillActionTable 数据并写入
                var skillActionTableData = ConvertToSkillActionTableDataList(skillActions);
                bool skillActionSuccess = WriteSkillActionTableCSV(skillActionTableData);
                
                bool success = actionSuccess && skillActionSuccess;
                
                if (success)
                {
                    Debug.Log($"{LOG_PREFIX} Successfully wrote {skillActions.Count} skill actions to CSV");
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to write skill action data (Action: {actionSuccess}, SkillAction: {skillActionSuccess})");
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to write skill action data: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 转换为 ActionTable 数据列表
        /// </summary>
        private static List<ActionTableData> ConvertToActionTableDataList(List<SkillActionEditorData> editorDataList)
        {
            var tableDataList = new List<ActionTableData>();
            
            foreach (var editorData in editorDataList)
            {
                var tableData = ConvertToActionTableData(editorData);
                if (tableData != null)
                {
                    tableDataList.Add(tableData);
                }
            }
            
            return tableDataList;
        }
        
        /// <summary>
        /// 转换为 SkillActionTable 数据列表
        /// </summary>
        private static List<SkillActionTableData> ConvertToSkillActionTableDataList(List<SkillActionEditorData> editorDataList)
        {
            var tableDataList = new List<SkillActionTableData>();
            
            foreach (var editorData in editorDataList)
            {
                var tableData = ConvertToSkillActionTableData(editorData);
                if (tableData != null)
                {
                    // 校验触发帧格式
                    if (!ValidateTriggerFramesFormat(editorData.TriggerFrames))
                    {
                        Debug.LogWarning($"{LOG_PREFIX} Invalid TriggerFrames format for ActionId {editorData.ActionId}: {editorData.TriggerFrames}");
                    }
                    
                    tableDataList.Add(tableData);
                }
            }
            
            return tableDataList;
        }
        
        /// <summary>
        /// 转换编辑器数据为 ActionTable 数据
        /// </summary>
        private static ActionTableData ConvertToActionTableData(SkillActionEditorData editorData)
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
            tableData.CancelTags = ActionDataWriter.SerializeCancelTagsToJson(editorData.CancelTags ?? new List<EditorCancelTag>());
            
            return tableData;
        }
        
        /// <summary>
        /// 转换编辑器数据为 SkillActionTable 数据
        /// </summary>
        private static SkillActionTableData ConvertToSkillActionTableData(SkillActionEditorData editorData)
        {
            if (editorData == null) return null;
            
            var tableData = new SkillActionTableData
            {
                ActionId = editorData.ActionId,
                ActualCost = editorData.ActualCost,
                ActualCooldown = editorData.ActualCooldown,
                TriggerFrames = editorData.TriggerFrames ?? ""
            };
            
            return tableData;
        }
        
        /// <summary>
        /// 写入 ActionTable CSV（只更新技能动作，保留其他动作）
        /// </summary>
        private static bool WriteActionTableCSV(List<ActionTableData> skillActionTableData)
        {
            var config = ActionTableData.GetTableConfig();
            
            try
            {
                // 1. 读取现有的所有动作
                var existingActions = ActionTableData.ReadAllActions();
                
                // 2. 过滤出非技能动作
                var nonSkillActions = existingActions
                    .Where(a => !string.Equals(a.ActionType, "skill", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                // 3. 获取技能动作的 ActionId 集合（用于去重）
                var skillActionIds = new HashSet<int>(skillActionTableData.Select(a => a.ActionId));
                
                // 4. 移除现有技能动作中已存在的 ActionId（避免重复）
                nonSkillActions = nonSkillActions
                    .Where(a => !skillActionIds.Contains(a.ActionId))
                    .ToList();
                
                // 5. 合并：非技能动作 + 新的技能动作
                var mergedActions = new List<ActionTableData>();
                mergedActions.AddRange(nonSkillActions);
                mergedActions.AddRange(skillActionTableData);
                
                // 6. 按 ActionId 排序
                mergedActions = mergedActions.OrderBy(a => a.ActionId).ToList();
                
                // 7. 写入合并后的完整列表
                bool success = LubanCSVWriter.WriteTable(config, mergedActions, enableBackup: true);
                
                if (success)
                {
                    Debug.Log($"{LOG_PREFIX} Successfully wrote ActionTable: {nonSkillActions.Count} non-skill actions + {skillActionTableData.Count} skill actions = {mergedActions.Count} total");
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
        /// 写入 SkillActionTable CSV
        /// </summary>
        private static bool WriteSkillActionTableCSV(List<SkillActionTableData> tableData)
        {
            var config = SkillActionTableData.GetTableConfig();
            
            try
            {
                bool success = LubanCSVWriter.WriteTable(config, tableData, enableBackup: true);
                
                if (success)
                {
                    Debug.Log($"{LOG_PREFIX} Successfully wrote SkillActionTable to {config.FilePath}");
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to write SkillActionTable");
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to write SkillActionTable CSV: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 校验触发帧格式
        /// </summary>
        private static bool ValidateTriggerFramesFormat(string triggerFramesStr)
        {
            if (string.IsNullOrWhiteSpace(triggerFramesStr))
                return true; // 空字符串是有效的
            
            try
            {
                // 使用智能分割（忽略括号内的逗号）
                string[] frames = SplitIgnoringParentheses(triggerFramesStr, ',');
                foreach (string frameStr in frames)
                {
                    string trimmed = frameStr.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;
                    
                    // 智能解析三部分：Frame / Trigger+Collision / EffectID
                    int firstColonIndex = trimmed.IndexOf(':');
                    if (firstColonIndex < 0)
                        return false;
                    
                    // 找到右括号位置（如果有）
                    int lastParenIndex = trimmed.LastIndexOf(')');
                    
                    // 在右括号之后查找最后一个冒号
                    int searchStartIndex = lastParenIndex > 0 ? lastParenIndex : firstColonIndex + 1;
                    int lastColonIndex = trimmed.IndexOf(':', searchStartIndex);
                    
                    if (lastColonIndex < 0)
                        return false;
                    
                    // 分割三部分
                    string framePart = trimmed.Substring(0, firstColonIndex).Trim();
                    string triggerPart = trimmed.Substring(firstColonIndex + 1, lastColonIndex - firstColonIndex - 1).Trim();
                    string effectIdPart = trimmed.Substring(lastColonIndex + 1).Trim();
                    
                    // 验证帧号（支持范围：Frame5-10）
                    string frameNumStr = framePart.Replace("Frame", "").Trim();
                    if (frameNumStr.Contains("-"))
                    {
                        // 多帧格式：Frame5-10
                        string[] frameRange = frameNumStr.Split('-');
                        if (frameRange.Length != 2 || 
                            !int.TryParse(frameRange[0].Trim(), out int startFrame) ||
                            !int.TryParse(frameRange[1].Trim(), out int endFrame) ||
                            startFrame < 0 || endFrame < 0 || startFrame > endFrame)
                            return false;
                    }
                    else
                    {
                        // 单帧格式：Frame5
                        if (!int.TryParse(frameNumStr, out int frameNum) || frameNum < 0)
                            return false;
                    }
                    
                    // 验证触发类型
                    if (string.IsNullOrEmpty(triggerPart))
                        return false;
                    
                    // 验证效果ID
                    if (!int.TryParse(effectIdPart, out int effectId) || effectId < 0)
                        return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Validation error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 智能分割字符串，忽略括号内的分隔符
        /// </summary>
        private static string[] SplitIgnoringParentheses(string input, char separator)
        {
            var result = new List<string>();
            var current = new System.Text.StringBuilder();
            int parenthesesDepth = 0;
            
            foreach (char c in input)
            {
                if (c == '(')
                {
                    parenthesesDepth++;
                    current.Append(c);
                }
                else if (c == ')')
                {
                    parenthesesDepth--;
                    current.Append(c);
                }
                else if (c == separator && parenthesesDepth == 0)
                {
                    // 只在括号外的分隔符才分割
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString().Trim());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            
            // 添加最后一个片段
            if (current.Length > 0)
            {
                result.Add(current.ToString().Trim());
            }
            
            return result.ToArray();
        }
    }
}

