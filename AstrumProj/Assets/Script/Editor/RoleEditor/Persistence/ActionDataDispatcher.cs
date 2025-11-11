using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 动作数据分发器
    /// 负责将编辑器数据分发到各个表的 Writer
    /// </summary>
    public static class ActionDataDispatcher
    {
        private const string LOG_PREFIX = "[ActionDataDispatcher]";
        
        /// <summary>
        /// 保存所有动作数据
        /// </summary>
        public static bool SaveAll(List<ActionEditorData> actions)
        {
            if (actions == null || actions.Count == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} No actions to save");
                return false;
            }
            
            Debug.Log($"{LOG_PREFIX} SaveAll called with {actions.Count} actions");
            
            try
            {
                // 在写入前强制同步时间轴事件到触发帧（仅 skill 类型）
                var skillActionsToSync = actions.Where(a => a.IsSkill && a.SkillExtension != null).ToList();
                Debug.Log($"{LOG_PREFIX} Found {skillActionsToSync.Count} skill actions to sync");
                
                foreach (var action in skillActionsToSync)
                {
                    Debug.Log($"{LOG_PREFIX} Syncing action {action.ActionId} (Type: {action.ActionType}, TimelineEvents: {action.TimelineEvents?.Count ?? 0})");
                    SyncTimelineToTriggerFrames(action);
                }
                
                bool allSuccess = true;
                
                // 1. 写入 ActionTable（所有动作的基础数据）
                bool actionSuccess = WriteActionTable(actions);
                if (!actionSuccess)
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to write ActionTable");
                    allSuccess = false;
                }
                
                // 2. 写入 SkillActionTable（仅 skill 类型）
                var skillActions = actions.Where(a => a.IsSkill && a.SkillExtension != null).ToList();
                if (skillActions.Count > 0)
                {
                    bool skillSuccess = SkillActionExtensionWriter.WriteAll(skillActions);
                    if (!skillSuccess)
                    {
                        Debug.LogError($"{LOG_PREFIX} Failed to write SkillActionTable for {skillActions.Count} skill actions");
                        allSuccess = false;
                    }
                }
                
                // 3. 写入 MoveActionTable（仅 move 类型）
                var moveActions = actions.Where(a => a.IsMove && a.MoveExtension != null).ToList();
                if (moveActions.Count > 0)
                {
                    bool moveSuccess = MoveActionExtensionWriter.WriteAll(moveActions);
                    if (!moveSuccess)
                    {
                        Debug.LogError($"{LOG_PREFIX} Failed to write MoveActionTable for {moveActions.Count} move actions");
                        allSuccess = false;
                    }
                }
                
                if (allSuccess)
                {
                    var typesSummary = actions
                        .GroupBy(a => a.ActionType?.ToLower() ?? "unknown")
                        .Select(g => $"{g.Key}({g.Count()})")
                        .ToList();
                    Debug.Log($"{LOG_PREFIX} Successfully saved {actions.Count} actions [{string.Join(", ", typesSummary)}]");
                    
                    // 自动打表
                    LubanTableGenerator.GenerateClientTables(showDialog: false);
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX} Some tables failed to write");
                }
                
                return allSuccess;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to save actions: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 写入 ActionTable（所有动作的基础数据）
        /// </summary>
        private static bool WriteActionTable(List<ActionEditorData> actions)
        {
            try
            {
                // 转换为表数据
                var tableDataList = new List<ActionTableData>();
                foreach (var action in actions)
                {
                    var tableData = ConvertToActionTableData(action);
                    tableDataList.Add(tableData);
                }
                
                // 读取现有数据
                var config = ActionTableData.GetTableConfig();
                var existing = LubanCSVReader.ReadTable<ActionTableData>(config);
                
                // 获取编辑器管理的 ActionId
                var editorActionIds = new HashSet<int>(tableDataList.Select(a => a.ActionId));
                
                // 保留非编辑器管理的动作
                var nonEditorActions = existing.Where(a => !editorActionIds.Contains(a.ActionId)).ToList();
                
                // 合并
                var merged = new List<ActionTableData>();
                merged.AddRange(nonEditorActions);
                merged.AddRange(tableDataList);
                merged = merged.OrderBy(a => a.ActionId).ToList();
                
                // 写入
                bool success = LubanCSVWriter.WriteTable(config, merged, enableBackup: true);
                
                if (success)
                {
                    var typesSummary = tableDataList
                        .GroupBy(a => a.ActionType?.ToLower() ?? "unknown")
                        .Select(g => $"{g.Key}({g.Count()})")
                        .ToList();
                    Debug.Log($"{LOG_PREFIX} Successfully wrote ActionTable: {nonEditorActions.Count} external + {tableDataList.Count} editor [{string.Join(", ", typesSummary)}] = {merged.Count} total");
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to write ActionTable");
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to write ActionTable: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 转换编辑器数据为 ActionTable 数据
        /// </summary>
        private static ActionTableData ConvertToActionTableData(ActionEditorData action)
        {
            var tableData = new ActionTableData
            {
                ActionId = action.ActionId,
                ActionName = action.ActionName,
                ActionType = action.ActionType,
                Duration = action.Duration,
                AnimationPath = action.AnimationPath,
                AutoNextActionId = action.AutoNextActionId,
                KeepPlayingAnim = action.KeepPlayingAnim,
                AutoTerminate = action.AutoTerminate,
                Commands = action.Commands != null ? new List<string>(action.Commands) : new List<string>(),
                Priority = action.Priority
            };
            
            // 序列化时间轴事件
            tableData.BeCancelledTags = ActionCancelTagSerializer.SerializeBeCancelledTags(action.TimelineEvents);
            
            // CancelTags 从运行时数据结构序列化为 JSON
            tableData.CancelTags = ActionDataWriter.SerializeCancelTagsToJson(action.CancelTags ?? new List<EditorCancelTag>());
            
            return tableData;
        }
        
        /// <summary>
        /// 同步时间轴事件到触发帧（仅 skill 类型）
        /// </summary>
        private static void SyncTimelineToTriggerFrames(ActionEditorData action)
        {
            if (action.SkillExtension == null) return;
            
            // 从时间轴事件构建触发帧数据
            var triggerEffects = new List<TriggerFrameData>();
            
            if (action.TimelineEvents != null)
            {
                foreach (var evt in action.TimelineEvents)
                {
                    TriggerFrameData data = null;
                    
                    try
                    {
                        switch (evt.TrackType)
                        {
                            case "SkillEffect":
                                data = ConvertSkillEffectToTriggerFrame(evt);
                                break;
                            case "VFX":
                                data = ConvertVFXToTriggerFrame(evt);
                                break;
                            case "SFX":
                                data = ConvertSFXToTriggerFrame(evt);
                                break;
                            default:
                                // 忽略其他轨道类型（如BeCancelTag）
                                continue;
                        }
                        
                        if (data != null)
                        {
                            // 设置帧范围（单帧或多帧）
                            if (evt.IsSingleFrame())
                            {
                                data.Frame = evt.StartFrame;
                            }
                            else
                            {
                                data.StartFrame = evt.StartFrame;
                                data.EndFrame = evt.EndFrame;
                            }
                            
                            triggerEffects.Add(data);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"{LOG_PREFIX} Failed to process event {evt.EventId}: {ex.Message}");
                    }
                }
            }
            
            // 序列化为 JSON
            action.SkillExtension.TriggerFrames = TriggerFrameData.SerializeToJSON(triggerEffects);
            action.SkillExtension.TriggerEffects = triggerEffects;
            
            Debug.Log($"{LOG_PREFIX} Synced {triggerEffects.Count} trigger frames for action {action.ActionId}");
        }
        
        /// <summary>
        /// 转换技能效果事件为触发帧数据
        /// </summary>
        private static TriggerFrameData ConvertSkillEffectToTriggerFrame(Timeline.TimelineEvent evt)
        {
            var eventData = evt.GetEventData<Timeline.EventData.SkillEffectEventData>();
            if (eventData == null)
            {
                Debug.LogWarning($"{LOG_PREFIX} Failed to parse SkillEffect event data: {evt.EventId}");
                return null;
            }
            
            return new TriggerFrameData
            {
                Type = "SkillEffect",
                TriggerType = eventData.TriggerType,
                EffectIds = eventData.EffectIds != null ? new List<int>(eventData.EffectIds) : new List<int>(),
                CollisionInfo = eventData.CollisionInfo,
                SocketName = eventData.SocketName,
                SocketOffset = eventData.SocketOffset
            };
        }
        
        /// <summary>
        /// 转换特效事件为触发帧数据
        /// </summary>
        private static TriggerFrameData ConvertVFXToTriggerFrame(Timeline.TimelineEvent evt)
        {
            var eventData = evt.GetEventData<Timeline.EventData.VFXEventData>();
            if (eventData == null)
            {
                Debug.LogWarning($"{LOG_PREFIX} Failed to parse VFX event data: {evt.EventId}");
                return null;
            }
            
            return new TriggerFrameData
            {
                Type = "VFX",
                ResourcePath = eventData.ResourcePath,
                PositionOffset = eventData.PositionOffset,
                Rotation = eventData.Rotation,
                Scale = eventData.Scale,
                PlaybackSpeed = eventData.PlaybackSpeed,
                FollowCharacter = eventData.FollowCharacter,
                Loop = eventData.Loop
            };
        }
        
        /// <summary>
        /// 转换音效事件为触发帧数据
        /// </summary>
        private static TriggerFrameData ConvertSFXToTriggerFrame(Timeline.TimelineEvent evt)
        {
            var eventData = evt.GetEventData<Timeline.EventData.SFXEventData>();
            if (eventData == null)
            {
                Debug.LogWarning($"{LOG_PREFIX} Failed to parse SFX event data: {evt.EventId}");
                return null;
            }
            
            return new TriggerFrameData
            {
                Type = "SFX",
                ResourcePath = eventData.ResourcePath,
                Volume = eventData.Volume
            };
        }
    }
}

