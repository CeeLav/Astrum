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
        /// 写入所有动作数据（协调各类型动作的导出）
        /// </summary>
        public static bool WriteSkillActionData(List<SkillActionEditorData> actions)
        {
            if (actions == null || actions.Count == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} No action data to write");
                return false;
            }
            
            try
            {
                // 转换为 ActionEditorData（新架构）
                var actionDataList = ActionEditorDataAdapter.ToActionEditorDataList(actions);
                
                // 使用新的 ActionDataDispatcher 保存所有数据
                bool success = ActionDataDispatcher.SaveAll(actionDataList);
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to write action data: {ex.Message}\n{ex.StackTrace}");
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
                Commands = editorData.Commands != null ? new List<string>(editorData.Commands) : new List<string>(),
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
                TriggerFrames = editorData.TriggerFrames ?? "",
                RootMotionData = editorData.RootMotionDataArray ?? new List<int>()
            };
            
            return tableData;
        }
        
        /// <summary>
        /// 写入 ActionTable CSV（更新编辑器管理的动作，保留其他动作）
        /// </summary>
        private static bool WriteActionTableCSV(List<ActionTableData> editorActionTableData)
        {
            var config = ActionTableData.GetTableConfig();
            
            try
            {
                // 1. 读取现有的所有动作
                var existingActions = ActionTableData.ReadAllActions();
                
                // 2. 获取编辑器动作的 ActionId 集合（用于去重）
                var editorActionIds = new HashSet<int>(editorActionTableData.Select(a => a.ActionId));
                
                // 3. 过滤出非编辑器管理的动作（保留那些不在编辑器中的 ActionId）
                var nonEditorActions = existingActions
                    .Where(a => !editorActionIds.Contains(a.ActionId))
                    .ToList();
                
                // 4. 合并：非编辑器动作 + 编辑器动作
                var mergedActions = new List<ActionTableData>();
                mergedActions.AddRange(nonEditorActions);
                mergedActions.AddRange(editorActionTableData);
                
                // 5. 按 ActionId 排序
                mergedActions = mergedActions.OrderBy(a => a.ActionId).ToList();
                
                // 6. 写入合并后的完整列表
                bool success = LubanCSVWriter.WriteTable(config, mergedActions, enableBackup: true);
                
                if (success)
                {
                    var typesSummary = editorActionTableData
                        .GroupBy(a => a.ActionType?.ToLower() ?? "unknown")
                        .Select(g => $"{g.Key}({g.Count()})")
                        .ToList();
                    Debug.Log($"{LOG_PREFIX} Successfully wrote ActionTable: {nonEditorActions.Count} external actions + {editorActionTableData.Count} editor actions [{string.Join(", ", typesSummary)}] = {mergedActions.Count} total");
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
        /// 写入 SkillActionTable CSV（仅更新 skill 类型动作，保留其他类型）
        /// </summary>
        private static bool WriteSkillActionTableCSV(List<SkillActionTableData> skillTableData)
        {
            var config = SkillActionTableData.GetTableConfig();
            
            try
            {
                // 1. 读取现有数据
                var existing = LubanCSVReader.ReadTable<SkillActionTableData>(config);
                var cache = new Dictionary<int, SkillActionTableData>();
                
                foreach (var entry in existing)
                {
                    if (!cache.ContainsKey(entry.ActionId))
                    {
                        cache[entry.ActionId] = entry;
                    }
                }
                
                // 2. 更新 skill 类型动作
                foreach (var entry in skillTableData)
                {
                    cache[entry.ActionId] = entry;
                }
                
                // 3. 排序并写入
                var merged = cache.Values.OrderBy(data => data.ActionId).ToList();
                bool success = LubanCSVWriter.WriteTable(config, merged, enableBackup: true);
                
                if (success)
                {
                    Debug.Log($"{LOG_PREFIX} Successfully wrote SkillActionTable with {merged.Count} records");
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
        /// 转换编辑器数据为 MoveActionTable 数据列表
        /// 仅处理 ActionType == "move" 的动作
        /// </summary>
        private static List<MoveActionTableData> ConvertToMoveActionTableDataList(List<SkillActionEditorData> editorDataList)
        {
            var tableDataList = new List<MoveActionTableData>();

            foreach (var editorData in editorDataList)
            {
                var tableData = ConvertToMoveActionTableData(editorData);
                if (tableData != null)
                {
                    tableDataList.Add(tableData);
                }
            }

            return tableDataList;
        }

        private static MoveActionTableData ConvertToMoveActionTableData(SkillActionEditorData editorData)
        {
            if (editorData == null)
            {
                return null;
            }

            if (!string.Equals(editorData.ActionType, "move", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            int moveSpeed = CalculateMoveSpeed(editorData);

            return new MoveActionTableData
            {
                ActionId = editorData.ActionId,
                MoveSpeed = moveSpeed,
                RootMotionData = editorData.RootMotionDataArray ?? new List<int>()
            };
        }

        private static bool WriteMoveActionTableCSV(List<MoveActionTableData> moveActionData)
        {
            var config = MoveActionTableData.GetTableConfig();

            try
            {
                // 读取现有数据
                var existing = MoveActionTableData.ReadAll();
                var cache = new Dictionary<int, MoveActionTableData>();

                // 缓存现有数据
                foreach (var entry in existing)
                {
                    if (!cache.ContainsKey(entry.ActionId))
                    {
                        cache[entry.ActionId] = entry;
                    }
                }

                // 更新新数据（完全替换，包括 RootMotionData）
                foreach (var entry in moveActionData)
                {
                    // 调试日志
                    if (entry.RootMotionData != null && entry.RootMotionData.Count > 0)
                    {
                        Debug.Log($"{LOG_PREFIX} Writing MoveAction {entry.ActionId} with {entry.RootMotionData.Count} root motion data points");
                    }
                    else
                    {
                        Debug.LogWarning($"{LOG_PREFIX} MoveAction {entry.ActionId} has no root motion data");
                    }
                    
                    cache[entry.ActionId] = entry;
                }

                // 排序并写入
                var merged = cache.Values.OrderBy(data => data.ActionId).ToList();
                bool success = LubanCSVWriter.WriteTable(config, merged, enableBackup: true);

                if (success)
                {
                    Debug.Log($"{LOG_PREFIX} Successfully wrote MoveActionTable with {merged.Count} records");
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to write MoveActionTable");
                }

                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to write MoveActionTable CSV: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        private static int CalculateMoveSpeed(SkillActionEditorData editorData)
        {
            if (editorData == null || editorData.RootMotionDataArray == null || editorData.RootMotionDataArray.Count <= 1)
            {
                return 0;
            }

            var data = editorData.RootMotionDataArray;
            int frameCount = data[0];

            if (frameCount <= 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} Move action {editorData.ActionId} has invalid root motion frame count: {frameCount}");
                return 0;
            }

            int expectedLength = 1 + frameCount * 7;
            if (data.Count < expectedLength)
            {
                Debug.LogWarning($"{LOG_PREFIX} Move action {editorData.ActionId} root motion data length mismatch. Expected {expectedLength}, got {data.Count}");
                return 0;
            }

            double totalDistance = 0d;

            for (int frame = 0; frame < frameCount; frame++)
            {
                int baseIndex = 1 + frame * 7;
                double dx = data[baseIndex] / 1000.0;
                double dz = data[baseIndex + 2] / 1000.0;

                double stepDistance = Math.Sqrt(dx * dx + dz * dz);
                totalDistance += stepDistance;
            }

            // Root motion采样为20FPS（50ms一次）
            double totalTimeSeconds = frameCount / 20.0;
            if (totalTimeSeconds <= 0.0)
            {
                return 0;
            }

            double speed = totalDistance / totalTimeSeconds;
            if (double.IsNaN(speed) || double.IsInfinity(speed) || speed <= 0.0)
            {
                return 0;
            }

            int scaledSpeed = Mathf.FloorToInt((float)(speed * 1000.0));
            return Mathf.Max(0, scaledSpeed);
        }
        
        /// <summary>
        /// 校验触发帧格式（JSON格式）
        /// </summary>
        private static bool ValidateTriggerFramesFormat(string triggerFramesStr)
        {
            if (string.IsNullOrWhiteSpace(triggerFramesStr))
                return true; // 空字符串是有效的
            
            try
            {
                // 尝试解析JSON数组
                var triggerFrames = Data.TriggerFrameData.ParseFromJSON(triggerFramesStr);
                if (triggerFrames == null)
                    return false;
                
                // 验证每个触发帧数据
                foreach (var triggerFrame in triggerFrames)
                {
                    // 验证Type字段
                    if (string.IsNullOrEmpty(triggerFrame.Type))
                        return false;
                    
                    // 验证帧范围（必须有Frame或StartFrame/EndFrame）
                    if (!triggerFrame.Frame.HasValue && 
                        (!triggerFrame.StartFrame.HasValue || !triggerFrame.EndFrame.HasValue))
                        return false;
                    
                    // 验证帧值有效性
                    if (triggerFrame.Frame.HasValue && triggerFrame.Frame.Value < 0)
                        return false;
                    if (triggerFrame.StartFrame.HasValue && triggerFrame.StartFrame.Value < 0)
                        return false;
                    if (triggerFrame.EndFrame.HasValue && triggerFrame.EndFrame.Value < 0)
                        return false;
                    if (triggerFrame.StartFrame.HasValue && triggerFrame.EndFrame.HasValue &&
                        triggerFrame.StartFrame.Value > triggerFrame.EndFrame.Value)
                        return false;
                    
                    // 根据类型验证特定字段
                    switch (triggerFrame.Type)
                    {
                        case "SkillEffect":
                            if (string.IsNullOrEmpty(triggerFrame.TriggerType))
                                return false;
                            if (triggerFrame.EffectIds == null || triggerFrame.EffectIds.Count == 0 || triggerFrame.EffectIds[0] <= 0)
                                return false;
                            break;
                        case "VFX":
                            if (string.IsNullOrEmpty(triggerFrame.ResourcePath))
                                return false;
                            break;
                        case "SFX":
                            if (string.IsNullOrEmpty(triggerFrame.ResourcePath))
                                return false;
                            break;
                    }
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

