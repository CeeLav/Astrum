using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Astrum.Editor.RoleEditor.Timeline;
using Astrum.Editor.RoleEditor.Timeline.EventData;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 动作取消标签解析器
    /// 负责解析 ActionTable 中的 CancelTags、BeCancelledTags、TempBeCancelledTags 字段
    /// </summary>
    public static class ActionCancelTagParser
    {
        private const string LOG_PREFIX = "[ActionCancelTagParser]";
        
        // === BeCancelledTags 解析 ===
        
        /// <summary>
        /// 解析被取消标签（JSON格式）
        /// 格式: [{"tags":["move","skill"],"rangeFrames":[0,60],"blendOutFrames":3,"priority":0}]
        /// </summary>
        public static List<TimelineEvent> ParseBeCancelledTags(string json, int actionId, int totalFrames)
        {
            var events = new List<TimelineEvent>();
            
            if (string.IsNullOrEmpty(json))
                return events;
            
            try
            {
                // 简单的JSON解析（使用Unity的JsonUtility需要包装类）
                // 这里使用手动解析
                var tags = ParseBeCancelledTagsManual(json, totalFrames);
                
                foreach (var tagData in tags)
                {
                    var evt = new TimelineEvent
                    {
                        TrackType = "BeCancelTag",
                        StartFrame = tagData.Item1, // 起始帧
                        EndFrame = tagData.Item2,   // 结束帧
                        DisplayName = tagData.Item3 // 显示名称
                    };
                    
                    evt.SetEventData(tagData.Item4); // BeCancelTagEventData
                    events.Add(evt);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to parse BeCancelledTags for action {actionId}: {ex.Message}");
            }
            
            return events;
        }
        
        /// <summary>
        /// 手动解析 BeCancelledTags JSON
        /// 返回: (起始帧, 结束帧, 显示名称, 数据对象)
        /// </summary>
        private static List<(int, int, string, BeCancelTagEventData)> ParseBeCancelledTagsManual(string json, int totalFrames)
        {
            var result = new List<(int, int, string, BeCancelTagEventData)>();
            
            try
            {
                // 使用 Newtonsoft.Json 解析
                var jsonDataList = JsonConvert.DeserializeObject<List<BeCancelledTagJsonData>>(json);
                
                if (jsonDataList != null)
                {
                    foreach (var jsonData in jsonDataList)
                    {
                        // 创建事件数据
                        var eventData = new BeCancelTagEventData
                        {
                            Tags = jsonData.Tags ?? new List<string>(),
                            BlendOutFrames = jsonData.BlendOutFrames,
                            Priority = jsonData.Priority,
                            Note = ""
                        };
                        
                        // 解析帧范围
                        int startFrame = 0;
                        int endFrame = totalFrames - 1;
                        
                        if (jsonData.RangeFrames != null && jsonData.RangeFrames.Count >= 2)
                        {
                            startFrame = jsonData.RangeFrames[0];
                            endFrame = jsonData.RangeFrames[1];
                        }
                        
                        result.Add((startFrame, endFrame, eventData.GetDisplayName(), eventData));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} JSON parse error: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// BeCancelledTag JSON 数据结构（用于反序列化）
        /// </summary>
        [Serializable]
        private class BeCancelledTagJsonData
        {
            public List<string> tags { get; set; }
            public List<int> rangeFrames { get; set; }
            public int blendOutFrames { get; set; }
            public int priority { get; set; }
            
            // 属性映射（首字母大写）
            public List<string> Tags => tags;
            public List<int> RangeFrames => rangeFrames;
            public int BlendOutFrames => blendOutFrames;
            public int Priority => priority;
        }
        
        // === CancelTags 解析 ===
        
        // 注意：TempBeCancelledTags 是运行时数据，不从静态表解析
        
        /// <summary>
        /// 解析取消标签（JSON格式）
        /// </summary>
        public static List<TimelineEvent> ParseCancelTags(string json, int actionId)
        {
            var events = new List<TimelineEvent>();
            
            if (string.IsNullOrEmpty(json))
                return events;
            
            try
            {
                // CancelTags 不需要转换为时间轴事件
                // 它们是动作的属性，不是时间相关的事件
                // 保持原样即可
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to parse CancelTags for action {actionId}: {ex.Message}");
            }
            
            return events;
        }
    }
}
