using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Astrum.Editor.RoleEditor.Timeline;

namespace Astrum.Editor.RoleEditor.Data
{
    /// <summary>
    /// 技能动作编辑器数据模型
    /// 继承自 ActionEditorData，添加技能系统专属字段
    /// 对应 SkillActionTable 表的数据结构
    /// </summary>
    [CreateAssetMenu(fileName = "SkillActionData", menuName = "Astrum/Skill Action Editor Data")]
    public class SkillActionEditorData : ActionEditorData
    {
        // === 技能专属字段 ===
        
        [TitleGroup("技能成本")]
        [LabelText("实际法力消耗"), Range(0, 1000)]
        [InfoBox("释放该技能动作实际消耗的法力值", InfoMessageType.Info)]
        public int ActualCost = 0;
        
        [TitleGroup("技能成本")]
        [LabelText("实际冷却(帧)"), Range(0, 3600)]
        [InfoBox("实际冷却帧数 (60帧 = 1秒)", InfoMessageType.Info)]
        public int ActualCooldown = 60;
        
        [TitleGroup("技能成本")]
        [LabelText("实际冷却(秒)"), ReadOnly]
        [ShowInInspector]
        public float ActualCooldownSeconds => ActualCooldown / 60f;
        
        // === 触发帧数据（不在配置面板显示，在时间轴编辑） ===
        
        [HideInInspector]
        public string TriggerFrames = "";
        
        [HideInInspector]
        public List<TriggerFrameData> TriggerEffects = new List<TriggerFrameData>();
        
        // === 核心方法 ===
        
        /// <summary>
        /// 创建默认技能动作数据
        /// </summary>
        public static new SkillActionEditorData CreateDefault(int actionId)
        {
            var data = CreateInstance<SkillActionEditorData>();
            
            // 基类字段
            data.ActionId = actionId;
            data.ActionName = $"skill_action_{actionId}";
            data.ActionType = "skill";
            data.AnimationDuration = 60;
            data.Duration = 60;
            data.AnimationPath = "";
            data.AnimationClip = null;
            data.Priority = 10;
            data.AutoNextActionId = actionId;
            data.KeepPlayingAnim = false;
            data.AutoTerminate = false;
            data.Command = "";
            data.CancelTags = new List<EditorCancelTag>();
            data.CancelTagsJson = "";
            data.TimelineEvents = new List<TimelineEvent>();
            
            // 技能专属字段
            data.ActualCost = 0;
            data.ActualCooldown = 60;
            data.TriggerFrames = "";
            data.TriggerEffects = new List<TriggerFrameData>();
            
            data.IsNew = true;
            data.IsDirty = true;
            
            return data;
        }
        
        /// <summary>
        /// 克隆技能动作数据
        /// </summary>
        public new SkillActionEditorData Clone()
        {
            var clone = CreateInstance<SkillActionEditorData>();
            
            // 基类字段
            clone.ActionId = this.ActionId;
            clone.ActionName = this.ActionName;
            clone.ActionType = this.ActionType;
            clone.AnimationDuration = this.AnimationDuration;
            clone.Duration = this.Duration;
            clone.AnimationPath = this.AnimationPath;
            clone.AnimationClip = this.AnimationClip;
            clone.Priority = this.Priority;
            clone.AutoNextActionId = this.AutoNextActionId;
            clone.KeepPlayingAnim = this.KeepPlayingAnim;
            clone.AutoTerminate = this.AutoTerminate;
            clone.Command = this.Command;
            clone.CancelTags = new List<EditorCancelTag>(this.CancelTags);
            clone.CancelTagsJson = this.CancelTagsJson;
            
            // 深拷贝时间轴事件
            clone.TimelineEvents = new List<TimelineEvent>();
            foreach (var evt in this.TimelineEvents)
            {
                clone.TimelineEvents.Add(evt.Clone());
            }
            
            // 技能专属字段
            clone.ActualCost = this.ActualCost;
            clone.ActualCooldown = this.ActualCooldown;
            clone.TriggerFrames = this.TriggerFrames;
            
            // 深拷贝触发帧数据
            clone.TriggerEffects = new List<TriggerFrameData>();
            foreach (var effect in this.TriggerEffects)
            {
                clone.TriggerEffects.Add(new TriggerFrameData
                {
                    Frame = effect.Frame,
                    TriggerType = effect.TriggerType,
                    CollisionInfo = effect.CollisionInfo,
                    EffectId = effect.EffectId
                });
            }
            
            clone.IsNew = true;
            clone.IsDirty = true;
            
            return clone;
        }
        
        // === 触发帧与时间轴事件的双向同步（Phase 4 实现） ===
        
        /// <summary>
        /// 从时间轴事件同步到触发帧数据
        /// 主数据源：TimelineEvents -> TriggerEffects -> TriggerFrames
        /// </summary>
        public void SyncFromTimelineEvents(List<TimelineEvent> events)
        {
            // TODO: Phase 4 - 实现双向同步
            // TriggerEffects.Clear();
            // foreach (var evt in events)
            // {
            //     if (evt.TrackType == "SkillEffects")
            //     {
            //         TriggerEffects.Add(new TriggerFrameData
            //         {
            //             Frame = evt.StartFrame,
            //             TriggerType = (string)evt.EventData["TriggerType"],
            //             EffectId = (int)evt.EventData["EffectId"]
            //         });
            //     }
            // }
            // 
            // // 更新 TriggerFrames 字符串
            // TriggerFrames = TriggerFrameData.SerializeToString(TriggerEffects);
            
            Debug.Log("[SkillActionData] SyncFromTimelineEvents (待 Phase 4 实现)");
        }
        
        /// <summary>
        /// 从触发帧数据构建时间轴事件
        /// 加载流程：TriggerFrames -> TriggerEffects -> TimelineEvents
        /// </summary>
        public List<TimelineEvent> BuildTimelineFromTriggerEffects()
        {
            // TODO: Phase 4 - 实现双向同步
            // var timelineEvents = new List<TimelineEvent>();
            // 
            // foreach (var effect in TriggerEffects)
            // {
            //     var evt = new TimelineEvent
            //     {
            //         EventId = System.Guid.NewGuid().ToString(),
            //         TrackType = "SkillEffects",
            //         StartFrame = effect.Frame,
            //         EndFrame = effect.Frame,
            //         DisplayName = $"效果 {effect.EffectId}",
            //         EventData = new Dictionary<string, object>
            //         {
            //             { "TriggerType", effect.TriggerType },
            //             { "EffectId", effect.EffectId }
            //         }
            //     };
            //     timelineEvents.Add(evt);
            // }
            // 
            // return timelineEvents;
            
            Debug.Log("[SkillActionData] BuildTimelineFromTriggerEffects (待 Phase 4 实现)");
            return new List<TimelineEvent>();
        }
        
        /// <summary>
        /// 解析 TriggerFrames 字符串为 TriggerEffects 列表
        /// </summary>
        public void ParseTriggerFrames()
        {
            TriggerEffects = TriggerFrameData.ParseFromString(TriggerFrames);
        }
    }
    
    /// <summary>
    /// 触发帧数据结构
    /// 用于解析和序列化 TriggerFrames 字符串
    /// </summary>
    [Serializable]
    public class TriggerFrameData
    {
        public int Frame;
        public string TriggerType; // Collision, Direct, Condition
        public string CollisionInfo; // 碰撞盒信息（仅Collision类型使用）格式：Box:5x2x1, Sphere:3.0, Capsule:2x5, Point
        public int EffectId;
        
        /// <summary>
        /// 解析字符串为触发帧列表
        /// 格式: "Frame5:Collision(Box:5x2x1):4001,Frame10:Direct:4002"
        /// </summary>
        public static List<TriggerFrameData> ParseFromString(string triggerFramesStr)
        {
            var result = new List<TriggerFrameData>();
            
            if (string.IsNullOrWhiteSpace(triggerFramesStr))
                return result;
            
            try
            {
                string[] frames = triggerFramesStr.Split(',');
                foreach (string frameStr in frames)
                {
                    string trimmed = frameStr.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;
                    
                    string[] parts = trimmed.Split(':');
                    if (parts.Length < 3)
                    {
                        Debug.LogWarning($"[TriggerFrameData] 格式错误: {trimmed}");
                        continue;
                    }
                    
                    // 解析帧号 (移除 "Frame" 前缀)
                    string frameNumStr = parts[0].Replace("Frame", "").Trim();
                    if (!int.TryParse(frameNumStr, out int frameNum))
                    {
                        Debug.LogWarning($"[TriggerFrameData] 帧号解析失败: {parts[0]}");
                        continue;
                    }
                    
                    // 解析触发类型和碰撞盒信息
                    string triggerPart = parts[1].Trim();
                    string triggerType = triggerPart;
                    string collisionInfo = "";
                    
                    // 检查是否包含碰撞盒信息 (格式：Collision(Box:5x2x1))
                    if (triggerPart.Contains("(") && triggerPart.Contains(")"))
                    {
                        int startIndex = triggerPart.IndexOf('(');
                        int endIndex = triggerPart.IndexOf(')');
                        
                        if (startIndex >= 0 && endIndex > startIndex)
                        {
                            triggerType = triggerPart.Substring(0, startIndex).Trim();
                            collisionInfo = triggerPart.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                        }
                    }
                    
                    // 解析效果ID
                    if (!int.TryParse(parts[2].Trim(), out int effectId))
                    {
                        Debug.LogWarning($"[TriggerFrameData] 效果ID解析失败: {parts[2]}");
                        continue;
                    }
                    
                    result.Add(new TriggerFrameData
                    {
                        Frame = frameNum,
                        TriggerType = triggerType,
                        CollisionInfo = collisionInfo,
                        EffectId = effectId
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TriggerFrameData] 解析错误: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 序列化触发帧列表为字符串
        /// </summary>
        public static string SerializeToString(List<TriggerFrameData> triggerEffects)
        {
            if (triggerEffects == null || triggerEffects.Count == 0)
                return "";
            
            var parts = triggerEffects
                .OrderBy(t => t.Frame)
                .Select(t => 
                {
                    // 如果有碰撞盒信息，格式化为 Collision(Box:5x2x1)
                    string triggerPart = t.TriggerType;
                    if (!string.IsNullOrEmpty(t.CollisionInfo))
                    {
                        triggerPart = $"{t.TriggerType}({t.CollisionInfo})";
                    }
                    return $"Frame{t.Frame}:{triggerPart}:{t.EffectId}";
                });
            
            return string.Join(",", parts);
        }
    }
}

