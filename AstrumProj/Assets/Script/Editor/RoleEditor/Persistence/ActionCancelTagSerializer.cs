using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.Editor.RoleEditor.Timeline;
using Astrum.Editor.RoleEditor.Timeline.EventData;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 动作取消标签序列化器
    /// 负责将时间轴事件序列化回 ActionTable 的 JSON 格式
    /// </summary>
    public static class ActionCancelTagSerializer
    {
        private const string LOG_PREFIX = "[ActionCancelTagSerializer]";
        
        // === BeCancelledTags 序列化 ===
        
        /// <summary>
        /// 序列化被取消标签为 JSON 字符串
        /// 格式: [{"tags":["move","skill"],"rangeFrames":[0,60],"blendOutFrames":3,"priority":0}]
        /// </summary>
        public static string SerializeBeCancelledTags(List<TimelineEvent> events)
        {
            var beCancelEvents = events.Where(e => e.TrackType == "BeCancelTag").ToList();
            
            if (beCancelEvents.Count == 0)
                return "";
            
            var jsonObjects = new List<string>();
            
            foreach (var evt in beCancelEvents)
            {
                var data = evt.GetEventData<BeCancelTagEventData>();
                if (data == null) continue;
                
                string tagsJson = SerializeStringArray(data.Tags);
                string rangeJson = $"[{evt.StartFrame},{evt.EndFrame}]";
                
                string jsonObj = $"{{\"tags\":{tagsJson},\"rangeFrames\":{rangeJson},\"blendOutFrames\":{data.BlendOutFrames},\"priority\":{data.Priority}}}";
                jsonObjects.Add(jsonObj);
            }
            
            if (jsonObjects.Count == 0)
                return "";
            
            return "[" + string.Join(",", jsonObjects) + "]";
        }
        
        // === CancelTags 序列化 ===
        
        // 注意：TempBeCancelledTags 是运行时数据，不写入静态表
        
        /// <summary>
        /// 序列化取消标签为 JSON 字符串
        /// CancelTags 是动作属性，不是时间轴事件，这里保持原样
        /// </summary>
        public static string SerializeCancelTags(string originalCancelTags)
        {
            // CancelTags 不在时间轴编辑，保持原值
            return originalCancelTags ?? "";
        }
        
        // === 辅助方法 ===
        
        /// <summary>
        /// 序列化字符串数组为JSON
        /// </summary>
        private static string SerializeStringArray(List<string> strings)
        {
            if (strings == null || strings.Count == 0)
                return "[]";
            
            var quotedStrings = strings.Select(s => $"\"{s}\"");
            return "[" + string.Join(",", quotedStrings) + "]";
        }
    }
}
