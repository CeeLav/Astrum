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
        
        // === 根节点位移数据 ===
        
        /// <summary>
        /// 位移提取模式
        /// </summary>
        public enum RootMotionExtractMode
        {
            RootTransform,      // 模式1：提取根骨骼位移
            HipsDifference     // 模式2：使用参考动画计算Hips差值
        }
        
        [TitleGroup("动画位移")]
        [LabelText("提取模式")]
        [InfoBox("RootTransform: 提取根骨骼位移\nHipsDifference: 使用参考动画计算Hips位移差值", InfoMessageType.Info)]
        [ValueDropdown("GetExtractModeOptions")]
        public RootMotionExtractMode ExtractMode = RootMotionExtractMode.RootTransform;
        
        /// <summary>
        /// 参考动画文件路径（模式2使用，带位移的动画）
        /// </summary>
        [TitleGroup("动画位移")]
        [LabelText("参考动画路径")]
        [ShowIf("@ExtractMode == RootMotionExtractMode.HipsDifference")]
        [InfoBox("带位移的动画文件路径（用于与基础动画计算差值）", InfoMessageType.Info)]
        public string ReferenceAnimationPath = "";
        
        /// <summary>
        /// 参考动画片段（模式2使用）
        /// </summary>
        [TitleGroup("动画位移")]
        [LabelText("参考动画文件")]
        [ShowIf("@ExtractMode == RootMotionExtractMode.HipsDifference")]
        [InfoBox("带位移的动画文件（与基础动画做差值）", InfoMessageType.Info)]
        public AnimationClip ReferenceAnimationClip;
        
        /// <summary>
        /// Hips骨骼名称（模式2使用）
        /// </summary>
        [TitleGroup("动画位移")]
        [LabelText("Hips骨骼名称")]
        [ShowIf("@ExtractMode == RootMotionExtractMode.HipsDifference")]
        [InfoBox("角色模型中Hips骨骼的名称，默认为'Hips'", InfoMessageType.Info)]
        public string HipsBoneName = "Hips";
        
        /// <summary>
        /// 根节点位移数据（整型数组格式，用于保存到CSV）
        /// Luban 类型：array,int#sep=,
        /// 编辑器端直接使用运行时数据结构，提取时直接转为定点数并序列化
        /// </summary>
        [HideInInspector]
        public List<int> RootMotionDataArray = new List<int>();
        
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
            clone.RootMotionDataArray = new List<int>(this.RootMotionDataArray);
            
            // 深拷贝触发帧数据
            clone.TriggerEffects = new List<TriggerFrameData>();
            foreach (var effect in this.TriggerEffects)
            {
                clone.TriggerEffects.Add(new TriggerFrameData
                {
                    StartFrame = effect.StartFrame,
                    EndFrame = effect.EndFrame,
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
            TriggerEffects.Clear();
            
            if (events == null || events.Count == 0)
            {
                TriggerFrames = "";
                return;
            }
            
            foreach (var evt in events)
            {
                // 只处理技能效果轨道的事件
                if (evt.TrackType != "SkillEffect")
                    continue;
                
                try
                {
                    // 反序列化事件数据
                    var eventData = evt.GetEventData<Timeline.EventData.SkillEffectEventData>();
                    
                    if (eventData == null)
                    {
                        Debug.LogWarning($"[SkillActionData] 无法解析事件数据: {evt.EventId}");
                        continue;
                    }
                    
                    // 创建触发帧数据
                    TriggerEffects.Add(new TriggerFrameData
                    {
                        StartFrame = evt.StartFrame,
                        EndFrame = evt.EndFrame,
                        TriggerType = eventData.TriggerType,
                        CollisionInfo = eventData.CollisionInfo,
                        EffectId = eventData.EffectId
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SkillActionData] 处理事件失败 {evt.EventId}: {ex.Message}");
                }
            }
            
            // 更新 TriggerFrames 字符串
            TriggerFrames = TriggerFrameData.SerializeToString(TriggerEffects);
            
            Debug.Log($"[SkillActionData] SyncFromTimelineEvents: 同步 {TriggerEffects.Count} 个触发帧, TriggerFrames=\"{TriggerFrames}\"");
        }
        
        /// <summary>
        /// 从触发帧数据构建时间轴事件
        /// 加载流程：TriggerFrames -> TriggerEffects -> TimelineEvents
        /// </summary>
        public List<TimelineEvent> BuildTimelineFromTriggerEffects()
        {
            var timelineEvents = new List<TimelineEvent>();
            
            foreach (var effect in TriggerEffects)
            {
                // 创建技能效果事件数据
                var eventData = new Timeline.EventData.SkillEffectEventData
                {
                    EffectId = effect.EffectId,
                    TriggerType = effect.TriggerType,
                    CollisionInfo = effect.CollisionInfo
                };
                
                // 刷新效果详情（从配置表读取）
                eventData.RefreshFromTable();
                eventData.ParseCollisionInfo();
                
                // 创建时间轴事件
                var evt = new TimelineEvent
                {
                    EventId = System.Guid.NewGuid().ToString(),
                    TrackType = "SkillEffect",
                    StartFrame = effect.StartFrame,
                    EndFrame = effect.EndFrame,
                    DisplayName = eventData.GetDisplayName()
                };
                
                // 序列化事件数据
                evt.SetEventData(eventData);
                
                timelineEvents.Add(evt);
            }
            
            Debug.Log($"[SkillActionData] BuildTimelineFromTriggerEffects: 创建 {timelineEvents.Count} 个时间轴事件");
            return timelineEvents;
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
    /// 支持单帧和多帧范围
    /// </summary>
    [Serializable]
    public class TriggerFrameData
    {
        public int StartFrame;       // 起始帧
        public int EndFrame;         // 结束帧（单帧时 StartFrame == EndFrame）
        public string TriggerType;   // Collision, Direct, Condition
        public string CollisionInfo; // 碰撞盒信息（仅Collision类型使用）格式：Box:5x2x1, Sphere:3.0, Capsule:2x5, Point
        public int EffectId;         // 效果ID
        
        // 辅助属性
        public bool IsSingleFrame => StartFrame == EndFrame;
        public int Duration => EndFrame - StartFrame + 1;
        
        /// <summary>
        /// 解析字符串为触发帧列表
        /// 支持单帧和多帧格式
        /// 单帧: "Frame5:Collision(Box:5x2x1):4001"
        /// 多帧: "Frame5-10:Collision(Box:5x2x1):4001"
        /// </summary>
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
                    
                    result.Add(new TriggerFrameData
                    {
                        StartFrame = startFrame,
                        EndFrame = endFrame,
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
        /// 序列化触发帧列表为字符串
        /// 自动判断单帧/多帧格式
        /// </summary>
        public static string SerializeToString(List<TriggerFrameData> triggerEffects)
        {
            if (triggerEffects == null || triggerEffects.Count == 0)
                return "";
            
            var parts = triggerEffects
                .OrderBy(t => t.StartFrame)
                .Select(t => 
                {
                    // 帧范围部分
                    string framePart = t.IsSingleFrame 
                        ? $"Frame{t.StartFrame}" 
                        : $"Frame{t.StartFrame}-{t.EndFrame}";
                    
                    // 触发类型部分（如果有碰撞盒信息，格式化为 Collision(Box:5x2x1)）
                    string triggerPart = t.TriggerType;
                    if (!string.IsNullOrEmpty(t.CollisionInfo))
                    {
                        triggerPart = $"{t.TriggerType}({t.CollisionInfo})";
                    }
                    
                    return $"{framePart}:{triggerPart}:{t.EffectId}";
                });
            
            return string.Join(",", parts);
        }
    }
}

