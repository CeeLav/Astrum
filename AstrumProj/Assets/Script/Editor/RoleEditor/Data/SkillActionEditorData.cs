using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Astrum.Editor.RoleEditor.Timeline;
using Newtonsoft.Json;
using Astrum.CommonBase;

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
        
        // === 根节点位移数据 ===
        
        /// <summary>
        /// 位移提取模式
        /// </summary>
        public enum RootMotionExtractMode
        {
            RootTransform,      // 模式1：提取根骨骼位移
            HipsDifference     // 模式2：使用参考动画计算Hips差值
        }
        
        [HideInInspector]
        public RootMotionExtractMode ExtractMode = RootMotionExtractMode.RootTransform;
        
        /// <summary>
        /// 参考动画文件路径（模式2使用，带位移的动画）
        /// </summary>
        [HideInInspector]
        public string ReferenceAnimationPath = "";
        
        /// <summary>
        /// 参考动画片段（模式2使用）
        /// </summary>
        [HideInInspector]
        public AnimationClip ReferenceAnimationClip;
        
        /// <summary>
        /// Hips骨骼名称（模式2使用）
        /// </summary>
        [HideInInspector]
        public string HipsBoneName = "Hips";
        
        /// <summary>
        /// 是否提取旋转数据
        /// </summary>
        [HideInInspector]
        public bool ExtractRotation = false;
        
        /// <summary>
        /// 是否只提取水平方向位移（X和Z，忽略Y）
        /// </summary>
        [HideInInspector]
        public bool ExtractHorizontalOnly = true;
        
        /// <summary>
        /// 根节点位移数据（整型数组格式，用于保存到CSV）
        /// Luban 类型：(array#sep=,),int
        /// 编辑器端直接使用运行时数据结构，提取时直接转为定点数并序列化
        /// </summary>
        [HideInInspector]
        public List<int> RootMotionDataArray = new List<int>();

        /// <summary>
        /// 使用的木桩模板ID
        /// </summary>
        [HideInInspector]
        public string HitDummyTemplateId = "";
        
        /// <summary>
        /// 获取提取模式选项（用于下拉菜单）
        /// </summary>
        private static ValueDropdownList<RootMotionExtractMode> GetExtractModeOptions()
        {
            return new ValueDropdownList<RootMotionExtractMode>
            {
                { "根骨骼位移 (RootTransform)", RootMotionExtractMode.RootTransform },
                { "Hips差值 (HipsDifference)", RootMotionExtractMode.HipsDifference }
            };
        }
        
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
            data.HitDummyTemplateId = string.Empty;
            
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
            
            // 根节点位移数据
            clone.ExtractMode = this.ExtractMode;
            clone.ReferenceAnimationPath = this.ReferenceAnimationPath;
            clone.ReferenceAnimationClip = this.ReferenceAnimationClip;
            clone.HipsBoneName = this.HipsBoneName;
            clone.ExtractRotation = this.ExtractRotation;
            clone.ExtractHorizontalOnly = this.ExtractHorizontalOnly;
            clone.RootMotionDataArray = new List<int>(this.RootMotionDataArray);
            clone.HitDummyTemplateId = this.HitDummyTemplateId;
            
            // 深拷贝触发帧数据
            clone.TriggerEffects = new List<TriggerFrameData>();
            foreach (var effect in this.TriggerEffects)
            {
                clone.TriggerEffects.Add(new TriggerFrameData
                {
                    Frame = effect.Frame,
                    StartFrame = effect.StartFrame,
                    EndFrame = effect.EndFrame,
                    Type = effect.Type,
                    TriggerType = effect.TriggerType,
                    EffectIds = effect.EffectIds != null ? new List<int>(effect.EffectIds) : new List<int>(),
                    CollisionInfo = effect.CollisionInfo,
                    ResourcePath = effect.ResourcePath,
                    PositionOffset = effect.PositionOffset,
                    Rotation = effect.Rotation,
                    Scale = effect.Scale,
                    PlaybackSpeed = effect.PlaybackSpeed,
                    FollowCharacter = effect.FollowCharacter,
                    Loop = effect.Loop,
                    Volume = effect.Volume
                });
            }
            
            clone.IsNew = true;
            clone.IsDirty = true;
            
            return clone;
        }
        
        // === 触发帧与时间轴事件的双向同步（Phase 4 实现） ===
        
        /// <summary>
        /// 从时间轴事件同步到触发帧数据（统一JSON格式）
        /// 主数据源：TimelineEvents -> TriggerEffects -> TriggerFrames JSON
        /// </summary>
        public void SyncFromTimelineEvents(List<TimelineEvent> events)
        {
            TriggerEffects.Clear();
            
            if (events == null || events.Count == 0)
            {
                TriggerFrames = "[]";
                return;
            }
            
            foreach (var evt in events)
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
                        
                        TriggerEffects.Add(data);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SkillActionData] 处理事件失败 {evt.EventId}: {ex.Message}");
                }
            }
            
            // 序列化为JSON
            TriggerFrames = TriggerFrameData.SerializeToJSON(TriggerEffects);
            
            Debug.Log($"[SkillActionData] SyncFromTimelineEvents: 同步 {TriggerEffects.Count} 个触发帧");
        }
        
        /// <summary>
        /// 转换技能效果事件为触发帧数据
        /// </summary>
        private TriggerFrameData ConvertSkillEffectToTriggerFrame(TimelineEvent evt)
        {
            var eventData = evt.GetEventData<Timeline.EventData.SkillEffectEventData>();
            if (eventData == null)
            {
                Debug.LogWarning($"[SkillActionData] 无法解析技能效果事件数据: {evt.EventId}");
                return null;
            }
            
            return new TriggerFrameData
            {
                Type = TriggerFrameJsonKeys.TypeSkillEffect,
                TriggerType = eventData.TriggerType,
                EffectIds = eventData.EffectIds != null ? new List<int>(eventData.EffectIds) : new List<int>(),
                CollisionInfo = eventData.CollisionInfo
            };
        }
        
        /// <summary>
        /// 转换特效事件为触发帧数据
        /// </summary>
        private TriggerFrameData ConvertVFXToTriggerFrame(TimelineEvent evt)
        {
            var eventData = evt.GetEventData<Timeline.EventData.VFXEventData>();
            if (eventData == null)
            {
                Debug.LogWarning($"[SkillActionData] 无法解析特效事件数据: {evt.EventId}");
                return null;
            }
            
            return new TriggerFrameData
            {
                Type = TriggerFrameJsonKeys.TypeVFX,
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
        private TriggerFrameData ConvertSFXToTriggerFrame(TimelineEvent evt)
        {
            var eventData = evt.GetEventData<Timeline.EventData.SFXEventData>();
            if (eventData == null)
            {
                Debug.LogWarning($"[SkillActionData] 无法解析音效事件数据: {evt.EventId}");
                return null;
            }
            
            return new TriggerFrameData
            {
                Type = TriggerFrameJsonKeys.TypeSFX,
                ResourcePath = eventData.ResourcePath,
                Volume = eventData.Volume
            };
        }
        
        /// <summary>
        /// 从触发帧数据构建时间轴事件（统一JSON格式）
        /// 加载流程：TriggerFrames JSON -> TriggerEffects -> TimelineEvents
        /// </summary>
        public List<TimelineEvent> BuildTimelineFromTriggerEffects()
        {
            var timelineEvents = new List<TimelineEvent>();
            
            foreach (var triggerData in TriggerEffects)
            {
                TimelineEvent evt = null;
                
                switch (triggerData.Type)
                {
                    case TriggerFrameJsonKeys.TypeSkillEffect:
                        evt = ConvertSkillEffectToTimelineEvent(triggerData);
                        break;
                    case TriggerFrameJsonKeys.TypeVFX:
                        evt = ConvertVFXToTimelineEvent(triggerData);
                        break;
                    case TriggerFrameJsonKeys.TypeSFX:
                        evt = ConvertSFXToTimelineEvent(triggerData);
                        break;
                    default:
                        Debug.LogWarning($"[SkillActionData] 未知的触发类型: {triggerData.Type}");
                        continue;
                }
                
                if (evt != null)
                {
                    timelineEvents.Add(evt);
                }
            }
            
            Debug.Log($"[SkillActionData] BuildTimelineFromTriggerEffects: 创建 {timelineEvents.Count} 个时间轴事件");
            return timelineEvents;
        }
        
        /// <summary>
        /// 转换技能效果触发帧数据为时间轴事件
        /// </summary>
        private TimelineEvent ConvertSkillEffectToTimelineEvent(TriggerFrameData triggerData)
        {
            var eventData = new Timeline.EventData.SkillEffectEventData
            {
                EffectIds = triggerData.EffectIds != null ? new List<int>(triggerData.EffectIds) : new List<int>(),
                TriggerType = triggerData.TriggerType,
                CollisionInfo = triggerData.CollisionInfo
            };
            
            // 刷新效果详情（从配置表读取）
            eventData.RefreshFromTable();
            eventData.ParseCollisionInfo();
            
            var evt = new TimelineEvent
            {
                EventId = System.Guid.NewGuid().ToString(),
                TrackType = "SkillEffect",
                StartFrame = triggerData.GetStartFrame(),
                EndFrame = triggerData.GetEndFrame(),
                DisplayName = eventData.GetDisplayName()
            };
            
            evt.SetEventData(eventData);
            return evt;
        }
        
        /// <summary>
        /// 转换特效触发帧数据为时间轴事件
        /// </summary>
        private TimelineEvent ConvertVFXToTimelineEvent(TriggerFrameData triggerData)
        {
            var eventData = new Timeline.EventData.VFXEventData
            {
                ResourcePath = triggerData.ResourcePath,
                PositionOffset = triggerData.PositionOffset ?? Vector3.zero,
                Rotation = triggerData.Rotation ?? Vector3.zero,
                Scale = triggerData.Scale ?? 1.0f,
                PlaybackSpeed = triggerData.PlaybackSpeed ?? 1.0f,
                FollowCharacter = triggerData.FollowCharacter ?? false,
                Loop = triggerData.Loop ?? false
            };
            
            var evt = new TimelineEvent
            {
                EventId = System.Guid.NewGuid().ToString(),
                TrackType = "VFX",
                StartFrame = triggerData.GetStartFrame(),
                EndFrame = triggerData.GetEndFrame(),
                DisplayName = eventData.GetDisplayName()
            };
            
            evt.SetEventData(eventData);
            return evt;
        }
        
        /// <summary>
        /// 转换音效触发帧数据为时间轴事件
        /// </summary>
        private TimelineEvent ConvertSFXToTimelineEvent(TriggerFrameData triggerData)
        {
            var eventData = new Timeline.EventData.SFXEventData
            {
                ResourcePath = triggerData.ResourcePath,
                Volume = triggerData.Volume ?? 0.8f
            };
            
            var evt = new TimelineEvent
            {
                EventId = System.Guid.NewGuid().ToString(),
                TrackType = "SFX",
                StartFrame = triggerData.GetStartFrame(),
                EndFrame = triggerData.GetEndFrame(),
                DisplayName = eventData.GetDisplayName()
            };
            
            evt.SetEventData(eventData);
            return evt;
        }
        
        /// <summary>
        /// 解析 TriggerFrames JSON 为 TriggerEffects 列表
        /// </summary>
        public void ParseTriggerFrames()
        {
            TriggerEffects = TriggerFrameData.ParseFromJSON(TriggerFrames);
        }
    }
    
    /// <summary>
    /// 触发帧数据结构（统一JSON格式）
    /// 支持所有类型的触发帧：SkillEffect、VFX、SFX等
    /// </summary>
    [Serializable]
    public class TriggerFrameData
    {
        // === 帧范围（二选一） ===
        
        /// <summary>单帧触发（与StartFrame/EndFrame互斥）</summary>
        public int? Frame;
        
        /// <summary>多帧范围起始帧（与Frame互斥）</summary>
        public int? StartFrame;
        
        /// <summary>多帧范围结束帧（与Frame互斥）</summary>
        public int? EndFrame;
        
        // === 通用字段 ===
        
        /// <summary>触发类型（顶层类型）：SkillEffect、VFX、SFX等</summary>
        public string Type = "";
        
        // === SkillEffect 字段 ===
        
        /// <summary>触发方式（SkillEffect内部）：Collision、Direct、Condition</summary>
        public string TriggerType = "";
        
        /// <summary>效果ID列表（SkillEffect使用，支持多个效果）</summary>
        public List<int> EffectIds = new List<int>();
        
        /// <summary>碰撞盒信息（仅Collision类型使用）</summary>
        public string CollisionInfo = "";
        
        // === VFX 字段 ===
        
        /// <summary>特效资源路径（VFX使用）</summary>
        public string ResourcePath = "";
        
        /// <summary>位置偏移（VFX使用）</summary>
        public Vector3? PositionOffset;
        
        /// <summary>旋转（VFX使用）</summary>
        public Vector3? Rotation;
        
        /// <summary>缩放（VFX使用）</summary>
        public float? Scale;
        
        /// <summary>播放速度（VFX使用）</summary>
        public float? PlaybackSpeed;
        
        /// <summary>是否跟随角色（VFX使用）</summary>
        public bool? FollowCharacter;
        
        /// <summary>是否循环播放（VFX使用）</summary>
        public bool? Loop;
        
        // === SFX 字段（预留） ===
        
        /// <summary>音量（SFX使用）</summary>
        public float? Volume;
        
        // === 辅助属性 ===
        
        /// <summary>是否为单帧事件</summary>
        public bool IsSingleFrame => Frame.HasValue || (StartFrame.HasValue && EndFrame.HasValue && StartFrame == EndFrame);
        
        /// <summary>获取起始帧</summary>
        public int GetStartFrame()
        {
            if (Frame.HasValue) return Frame.Value;
            if (StartFrame.HasValue) return StartFrame.Value;
            return 0;
        }
        
        /// <summary>获取结束帧</summary>
        public int GetEndFrame()
        {
            if (Frame.HasValue) return Frame.Value;
            if (EndFrame.HasValue) return EndFrame.Value;
            return 0;
        }
        
        /// <summary>获取持续时间（帧数）</summary>
        public int Duration => GetEndFrame() - GetStartFrame() + 1;
        
        /// <summary>
        /// 获取JSON序列化设置（处理Unity类型）
        /// </summary>
        private static JsonSerializerSettings GetJsonSettings()
        {
            return new JsonSerializerSettings
            {
                // 忽略循环引用（解决Vector3.normalized等属性的循环引用问题）
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                // 忽略默认值（减少JSON大小）
                DefaultValueHandling = DefaultValueHandling.Ignore,
                // 格式化输出（开发时便于阅读，生产环境可改为None）
                Formatting = Formatting.None
            };
        }
        
        /// <summary>
        /// 从JSON字符串解析触发帧列表
        /// </summary>
        public static List<TriggerFrameData> ParseFromJSON(string jsonStr)
        {
            var result = new List<TriggerFrameData>();
            
            if (string.IsNullOrWhiteSpace(jsonStr))
                return result;
            
            try
            {
                // 使用Newtonsoft.Json直接解析数组，使用自定义设置
                var settings = GetJsonSettings();
                result = JsonConvert.DeserializeObject<List<TriggerFrameData>>(jsonStr, settings);
                if (result == null)
                {
                    result = new List<TriggerFrameData>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TriggerFrameData] JSON解析错误: {ex.Message}\nJSON内容: {jsonStr}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 序列化触发帧列表为JSON字符串
        /// </summary>
        public static string SerializeToJSON(List<TriggerFrameData> triggerFrames)
        {
            if (triggerFrames == null || triggerFrames.Count == 0)
                return "[]";
            
            try
            {
                // 使用Newtonsoft.Json直接序列化数组，使用自定义设置
                var settings = GetJsonSettings();
                return JsonConvert.SerializeObject(triggerFrames, settings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TriggerFrameData] JSON序列化错误: {ex.Message}");
                return "[]";
            }
        }
        
        /// <summary>
        /// 解析字符串为触发帧列表（旧格式，用于数据迁移）
        /// 支持单帧和多帧格式
        /// 单帧: "Frame5:Collision(Box:5x2x1):4001"
        /// 多帧: "Frame5-10:Collision(Box:5x2x1):4001"
        /// </summary>
        [System.Obsolete("使用ParseFromJSON替代，此方法仅用于数据迁移")]
        public static List<TriggerFrameData> ParseFromString(string triggerFramesStr)
        {
            var result = new List<TriggerFrameData>();
            
            if (string.IsNullOrWhiteSpace(triggerFramesStr))
                return result;
            
            try
            {
                // 智能分割：忽略括号内的逗号
                string[] frames = SplitIgnoringParentheses(triggerFramesStr, ',');
                foreach (string frameStr in frames)
                {
                    string trimmed = frameStr.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;
                    
                    // 改进的解析逻辑：考虑碰撞盒信息中可能包含冒号和@符号
                    // 格式: "Frame10:Collision(Box:2.1x0.9x1.3@0,1,0):4022"
                    // 策略：找到第一个冒号（帧号后）和右括号外的最后一个冒号（效果ID前）
                    
                    int firstColonIndex = trimmed.IndexOf(':');
                    if (firstColonIndex < 0)
                    {
                        Debug.LogWarning($"[TriggerFrameData] 格式错误，缺少必要的分隔符: {trimmed}");
                        continue;
                    }
                    
                    // 找到右括号位置（如果有）
                    int lastParenIndex = trimmed.LastIndexOf(')');
                    
                    // 在右括号之后查找最后一个冒号，如果没有括号则查找整个字符串
                    int searchStartIndex = lastParenIndex > 0 ? lastParenIndex : firstColonIndex + 1;
                    int lastColonIndex = trimmed.IndexOf(':', searchStartIndex);
                    
                    if (lastColonIndex < 0)
                    {
                        Debug.LogWarning($"[TriggerFrameData] 格式错误，缺少效果ID分隔符: {trimmed}");
                        continue;
                    }
                    
                    // 分割三部分
                    string framePart = trimmed.Substring(0, firstColonIndex).Trim();
                    string triggerPart = trimmed.Substring(firstColonIndex + 1, lastColonIndex - firstColonIndex - 1).Trim();
                    string effectIdPart = trimmed.Substring(lastColonIndex + 1).Trim();
                    
                    // 解析帧范围 (移除 "Frame" 前缀)
                    // 支持格式：Frame5 或 Frame5-10
                    framePart = framePart.Replace("Frame", "").Trim();
                    int startFrame, endFrame;
                    
                    if (framePart.Contains("-"))
                    {
                        // 多帧格式：Frame5-10
                        string[] frameRange = framePart.Split('-');
                        if (frameRange.Length != 2 || 
                            !int.TryParse(frameRange[0].Trim(), out startFrame) ||
                            !int.TryParse(frameRange[1].Trim(), out endFrame))
                        {
                            Debug.LogWarning($"[TriggerFrameData] 帧范围解析失败: {framePart}");
                            continue;
                        }
                        
                        if (startFrame > endFrame)
                        {
                            Debug.LogWarning($"[TriggerFrameData] 起始帧({startFrame})大于结束帧({endFrame}): {framePart}");
                            continue;
                        }
                    }
                    else
                    {
                        // 单帧格式：Frame5
                        if (!int.TryParse(framePart, out startFrame))
                        {
                            Debug.LogWarning($"[TriggerFrameData] 帧号解析失败: {framePart}");
                            continue;
                        }
                        endFrame = startFrame;
                    }
                    
                    // 解析触发类型和碰撞盒信息
                    string triggerType = triggerPart;
                    string collisionInfo = "";
                    
                    // 检查是否包含碰撞盒信息 (格式：Collision(Box:1x1x0.5))
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
                    if (!int.TryParse(effectIdPart, out int effectId))
                    {
                        Debug.LogWarning($"[TriggerFrameData] 效果ID解析失败: {effectIdPart}");
                        continue;
                    }
                    
                    // 转换为新格式：添加Type字段
                    var data = new TriggerFrameData
                    {
                        Type = "SkillEffect",
                        TriggerType = triggerType,
                        CollisionInfo = collisionInfo,
                        EffectIds = new List<int> { effectId }
                    };
                    
                    // 设置帧范围（单帧或多帧）
                    if (startFrame == endFrame)
                    {
                        data.Frame = startFrame;
                    }
                    else
                    {
                        data.StartFrame = startFrame;
                        data.EndFrame = endFrame;
                    }
                    
                    result.Add(data);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TriggerFrameData] 解析错误: {ex.Message}");
            }
            
            return result;
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
        
        /// <summary>
        /// 序列化触发帧列表为字符串（旧格式，用于数据迁移）
        /// 自动判断单帧/多帧格式
        /// </summary>
        [System.Obsolete("使用SerializeToJSON替代，此方法仅用于数据迁移")]
        public static string SerializeToString(List<TriggerFrameData> triggerEffects)
        {
            if (triggerEffects == null || triggerEffects.Count == 0)
                return "";
            
            var parts = triggerEffects
                .OrderBy(t => t.GetStartFrame())
                .Select(t => 
                {
                    int startFrame = t.GetStartFrame();
                    int endFrame = t.GetEndFrame();
                    
                    // 帧范围部分
                    string framePart = t.IsSingleFrame 
                        ? $"Frame{startFrame}" 
                        : $"Frame{startFrame}-{endFrame}";
                    
                    // 触发类型部分（如果有碰撞盒信息，格式化为 Collision(Box:5x2x1)）
                    string triggerPart = t.TriggerType;
                    if (!string.IsNullOrEmpty(t.CollisionInfo))
                    {
                        triggerPart = $"{t.TriggerType}({t.CollisionInfo})";
                    }
                    
                    // 效果ID部分（使用第一个效果ID，兼容旧格式）
                    string effectIdPart = t.EffectIds != null && t.EffectIds.Count > 0
                        ? t.EffectIds[0].ToString()
                        : "0";
                    
                    return $"{framePart}:{triggerPart}:{effectIdPart}";
                });
            
            return string.Join(",", parts);
        }
    }
}

