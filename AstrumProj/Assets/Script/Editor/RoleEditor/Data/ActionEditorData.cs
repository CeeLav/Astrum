using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Astrum.Editor.RoleEditor.Timeline;

namespace Astrum.Editor.RoleEditor.Data
{
    /// <summary>
    /// 动作编辑器数据模型
    /// 对应 ActionTable 表的数据结构
    /// </summary>
    [CreateAssetMenu(fileName = "ActionData", menuName = "Astrum/Action Editor Data")]
    public class ActionEditorData : ScriptableObject
    {
        // === ActionTable 基础字段 ===
        
        [TitleGroup("基础信息")]
        [LabelText("动作ID"), ReadOnly]
        [InfoBox("动作ID是唯一标识符，不可修改", InfoMessageType.Info)]
        public int ActionId;
        
        [TitleGroup("基础信息")]
        [LabelText("动作名称")]
        [ValidateInput("ValidateActionName", "动作名称不能为空")]
        public string ActionName = "";
        
        [TitleGroup("基础信息")]
        [LabelText("动作类型")]
        [ValueDropdown("GetActionTypeOptions")]
        public string ActionType = "idle";
        
        [TitleGroup("基础信息")]
        [LabelText("动画总帧数"), ReadOnly]
        [InfoBox("从动画文件自动计算，只读", InfoMessageType.None)]
        public int AnimationDuration = 60;
        
        [TitleGroup("基础信息")]
        [LabelText("动作总帧数")]
        [InfoBox("可以小于动画总帧数，用于提前结束动作", InfoMessageType.Info)]
        [ValidateInput("ValidateDuration", "动作总帧数不能超过动画总帧数")]
        [PropertyRange(1, "AnimationDuration")]
        public int Duration = 60;
        
        [TitleGroup("基础信息")]
        [LabelText("动画路径")]
        [FilePath(Extensions = "anim")]
        public string AnimationPath = "";
        
        // === 动作配置字段 ===
        
        [TitleGroup("动作配置")]
        [LabelText("优先级"), Range(0, 100)]
        [InfoBox("数值越小优先级越高", InfoMessageType.None)]
        public int Priority = 10;
        
        [TitleGroup("动作配置")]
        [LabelText("自动下一动作ID")]
        [InfoBox("动作结束后自动切换到的动作ID", InfoMessageType.None)]
        public int AutoNextActionId = 0;
        
        [TitleGroup("动作配置")]
        [LabelText("保持动画播放")]
        [InfoBox("切换到此动作时是否保持播放动画", InfoMessageType.None)]
        public bool KeepPlayingAnim = false;
        
        [TitleGroup("动作配置")]
        [LabelText("自动终止")]
        [InfoBox("是否自动终止动作", InfoMessageType.None)]
        public bool AutoTerminate = false;
        
        [TitleGroup("动作配置")]
        [LabelText("命令")]
        [ValueDropdown("GetCommandOptions")]
        public string Command = "";
        
        // === 取消标签配置 ===
        
        [TitleGroup("取消标签配置")]
        [LabelText("CancelTags (取消其他动作的标签)")]
        [InfoBox("此动作可以取消带有这些标签的其他动作", InfoMessageType.Info)]
        [TableList(ShowIndexLabels = true, ShowPaging = true)]
        public List<EditorCancelTag> CancelTags = new List<EditorCancelTag>();
        
        [HideInInspector] // 隐藏原始 JSON 字符串，只用于数据持久化
        public string CancelTagsJson = "";
        
        // === 时间轴事件 ===
        
        [HideInInspector]
        public List<TimelineEvent> TimelineEvents = new List<TimelineEvent>();
        
        // === 编辑器状态 ===
        
        [HideInInspector]
        public bool IsDirty = false;
        
        [HideInInspector]
        public bool IsNew = false;
        
        // === 核心方法 ===
        
        /// <summary>
        /// 创建默认动作数据
        /// </summary>
        public static ActionEditorData CreateDefault(int id)
        {
            var data = CreateInstance<ActionEditorData>();
            data.ActionId = id;
            data.ActionName = $"action_{id}";
            data.ActionType = "idle";
            data.AnimationDuration = 60;
            data.Duration = 60;
            data.AnimationPath = "";
            data.Priority = 10;
            data.AutoNextActionId = id;
            data.KeepPlayingAnim = false;
            data.AutoTerminate = false;
            data.Command = "";
            data.CancelTags = new List<EditorCancelTag>();
            data.CancelTagsJson = "";
            data.TimelineEvents = new List<TimelineEvent>();
            data.IsNew = true;
            data.IsDirty = true;
            
            return data;
        }
        
        /// <summary>
        /// 克隆动作数据
        /// </summary>
        public ActionEditorData Clone()
        {
            var clone = CreateInstance<ActionEditorData>();
            
            clone.ActionId = this.ActionId;
            clone.ActionName = this.ActionName;
            clone.ActionType = this.ActionType;
            clone.AnimationDuration = this.AnimationDuration;
            clone.Duration = this.Duration;
            clone.AnimationPath = this.AnimationPath;
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
            
            clone.IsNew = true;
            clone.IsDirty = true;
            
            return clone;
        }
        
        /// <summary>
        /// 标记为已修改
        /// </summary>
        public void MarkDirty()
        {
            IsDirty = true;
        }
        
        /// <summary>
        /// 清除修改标记
        /// </summary>
        public void ClearDirty()
        {
            IsDirty = false;
            IsNew = false;
        }
        
        /// <summary>
        /// 获取指定轨道类型的事件列表
        /// </summary>
        public List<TimelineEvent> GetEventsByTrackType(string trackType)
        {
            return TimelineEvents.Where(e => e.TrackType == trackType).ToList();
        }
        
        /// <summary>
        /// 添加时间轴事件
        /// </summary>
        public void AddTimelineEvent(TimelineEvent evt)
        {
            if (evt == null)
            {
                Debug.LogError("[ActionEditorData] Cannot add null event");
                return;
            }
            
            TimelineEvents.Add(evt);
            MarkDirty();
        }
        
        /// <summary>
        /// 移除时间轴事件
        /// </summary>
        public void RemoveTimelineEvent(string eventId)
        {
            int removed = TimelineEvents.RemoveAll(e => e.EventId == eventId);
            if (removed > 0)
            {
                MarkDirty();
            }
        }
        
        /// <summary>
        /// 获取指定轨道类型的事件数量
        /// </summary>
        public int GetEventCount(string trackType)
        {
            return TimelineEvents.Count(e => e.TrackType == trackType);
        }
        
        /// <summary>
        /// 获取所有轨道类型及其事件数量
        /// </summary>
        public Dictionary<string, int> GetEventStatistics()
        {
            return TimelineEvents
                .GroupBy(e => e.TrackType)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        
        // === Odin 回调 ===
        
        private bool ValidateActionName(string name)
        {
            return !string.IsNullOrWhiteSpace(name);
        }
        
        private IEnumerable<string> GetActionTypeOptions()
        {
            return new[] { "idle", "walk", "run", "jump", "skill", "interact", "custom" };
        }
        
        private IEnumerable<string> GetCommandOptions()
        {
            return new[] { "", "Move", "NormalAttack", "HeavyAttack", "Skill1", "Skill2", "Jump", "Interact" };
        }
        
        private bool ValidateDuration(int duration)
        {
            if (string.IsNullOrEmpty(AnimationPath))
            {
                return duration > 0;
            }
            return duration > 0 && duration <= AnimationDuration;
        }
    }
}
