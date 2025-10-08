using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Timeline.EventData
{
    /// <summary>
    /// 临时被取消标签事件数据
    /// 对应 ActionTable 的 TempBeCancelledTags 字段
    /// </summary>
    [Serializable]
    public class TempBeCancelTagEventData
    {
        /// <summary>临时标签ID</summary>
        public string Id = "";
        
        /// <summary>被取消标签列表</summary>
        public List<string> Tags = new List<string>();
        
        /// <summary>持续时间（帧数）</summary>
        public int DurationFrames = 10;
        
        /// <summary>融合时间（帧数）</summary>
        public int BlendOutFrames = 3;
        
        /// <summary>优先级变化</summary>
        public int Priority = 0;
        
        /// <summary>备注</summary>
        public string Note = "";
        
        // === 工厂方法 ===
        
        /// <summary>
        /// 创建默认数据
        /// </summary>
        public static TempBeCancelTagEventData CreateDefault()
        {
            return new TempBeCancelTagEventData
            {
                Id = Guid.NewGuid().ToString().Substring(0, 8),
                Tags = new List<string> { "move" },
                DurationFrames = 10,
                BlendOutFrames = 3,
                Priority = 0,
                Note = ""
            };
        }
        
        /// <summary>
        /// 克隆数据
        /// </summary>
        public TempBeCancelTagEventData Clone()
        {
            return new TempBeCancelTagEventData
            {
                Id = this.Id,
                Tags = new List<string>(this.Tags),
                DurationFrames = this.DurationFrames,
                BlendOutFrames = this.BlendOutFrames,
                Priority = this.Priority,
                Note = this.Note
            };
        }
        
        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName()
        {
            if (Tags == null || Tags.Count == 0)
                return $"临时:{Id}";
            
            return $"{Id}:[{string.Join(",", Tags)}]";
        }
        
        /// <summary>
        /// 验证数据有效性
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();
            
            if (string.IsNullOrEmpty(Id))
            {
                errors.Add("临时标签ID不能为空");
            }
            
            if (Tags == null || Tags.Count == 0)
            {
                errors.Add("标签列表不能为空");
            }
            
            if (DurationFrames <= 0)
            {
                errors.Add("持续时间必须大于0");
            }
            
            if (BlendOutFrames < 0)
            {
                errors.Add("融合时间不能为负数");
            }
            
            return errors.Count == 0;
        }
    }
}
