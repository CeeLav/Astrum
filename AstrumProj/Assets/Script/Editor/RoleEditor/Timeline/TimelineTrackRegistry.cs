using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Timeline
{
    /// <summary>
    /// 时间轴轨道注册表（静态）
    /// 管理所有可用的轨道类型
    /// </summary>
    public static class TimelineTrackRegistry
    {
        private const string LOG_PREFIX = "[TimelineTrackRegistry]";
        
        // === 轨道存储 ===
        private static Dictionary<string, TimelineTrackConfig> _tracks = new Dictionary<string, TimelineTrackConfig>();
        
        // === 核心方法 ===
        
        /// <summary>
        /// 注册轨道类型
        /// </summary>
        public static void RegisterTrack(TimelineTrackConfig config)
        {
            if (config == null)
            {
                Debug.LogError($"{LOG_PREFIX} Cannot register null track config");
                return;
            }
            
            if (string.IsNullOrEmpty(config.TrackType))
            {
                Debug.LogError($"{LOG_PREFIX} Track type cannot be null or empty");
                return;
            }
            
            if (_tracks.ContainsKey(config.TrackType))
            {
                Debug.LogWarning($"{LOG_PREFIX} Track type '{config.TrackType}' already registered, overwriting");
            }
            
            _tracks[config.TrackType] = config;
            Debug.Log($"{LOG_PREFIX} Registered track: {config.TrackType} ({config.TrackName})");
        }
        
        /// <summary>
        /// 注销轨道类型
        /// </summary>
        public static void UnregisterTrack(string trackType)
        {
            if (string.IsNullOrEmpty(trackType))
            {
                Debug.LogError($"{LOG_PREFIX} Track type cannot be null or empty");
                return;
            }
            
            if (_tracks.ContainsKey(trackType))
            {
                _tracks.Remove(trackType);
                Debug.Log($"{LOG_PREFIX} Unregistered track: {trackType}");
            }
            else
            {
                Debug.LogWarning($"{LOG_PREFIX} Track type '{trackType}' not found");
            }
        }
        
        /// <summary>
        /// 获取轨道配置
        /// </summary>
        public static TimelineTrackConfig GetTrack(string trackType)
        {
            if (string.IsNullOrEmpty(trackType))
                return null;
            
            return _tracks.ContainsKey(trackType) ? _tracks[trackType] : null;
        }
        
        /// <summary>
        /// 检查轨道是否已注册
        /// </summary>
        public static bool HasTrack(string trackType)
        {
            return !string.IsNullOrEmpty(trackType) && _tracks.ContainsKey(trackType);
        }
        
        /// <summary>
        /// 获取所有轨道配置（按排序顺序）
        /// </summary>
        public static List<TimelineTrackConfig> GetAllTracks()
        {
            return _tracks.Values.OrderBy(t => t.SortOrder).ToList();
        }
        
        /// <summary>
        /// 获取所有可见轨道
        /// </summary>
        public static List<TimelineTrackConfig> GetVisibleTracks()
        {
            return _tracks.Values
                .Where(t => t.IsVisible)
                .OrderBy(t => t.SortOrder)
                .ToList();
        }
        
        /// <summary>
        /// 获取轨道数量
        /// </summary>
        public static int GetTrackCount()
        {
            return _tracks.Count;
        }
        
        /// <summary>
        /// 清除所有轨道
        /// </summary>
        public static void Clear()
        {
            _tracks.Clear();
            Debug.Log($"{LOG_PREFIX} Cleared all tracks");
        }
        
        /// <summary>
        /// 获取所有轨道类型名称
        /// </summary>
        public static List<string> GetAllTrackTypes()
        {
            return _tracks.Keys.ToList();
        }
    }
}
