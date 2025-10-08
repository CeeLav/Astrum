using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Timeline
{
    /// <summary>
    /// 时间轴编辑器模块（主控制器）
    /// 整合渲染、交互、布局等子模块
    /// </summary>
    public class TimelineEditorModule
    {
        private const string LOG_PREFIX = "[TimelineEditorModule]";
        
        // === 子模块 ===
        private TimelineLayoutCalculator _layoutCalculator;
        private TimelineRenderer _renderer;
        private TimelineInteraction _interaction;
        
        // === 核心数据 ===
        private List<TimelineEvent> _events = new List<TimelineEvent>();
        private List<TimelineTrackConfig> _tracks = new List<TimelineTrackConfig>();
        private Dictionary<string, List<TimelineEvent>> _eventsByTrack = new Dictionary<string, List<TimelineEvent>>();
        
        private int _currentFrame = 0;
        private int _totalFrames = 60;
        private bool _isInitialized = false;
        
        // === 事件 ===
        public event Action<TimelineEvent> OnEventSelected;
        public event Action<TimelineEvent> OnEventAdded;
        public event Action<TimelineEvent> OnEventRemoved;
        public event Action<TimelineEvent> OnEventModified;
        public event Action<int> OnCurrentFrameChanged;
        
        // === 构造函数 ===
        public TimelineEditorModule()
        {
            _layoutCalculator = new TimelineLayoutCalculator();
            _renderer = new TimelineRenderer(_layoutCalculator);
            _interaction = new TimelineInteraction(_layoutCalculator);
            
            // 订阅交互事件
            _interaction.OnEventSelected += HandleEventSelected;
            _interaction.OnEventMoved += HandleEventMoved;
            _interaction.OnEventResized += HandleEventResized;
            _interaction.OnPlayheadMoved += HandlePlayheadMoved;
            _interaction.OnAddEventRequested += HandleAddEventRequested;
        }
        
        // === 初始化 ===
        
        /// <summary>
        /// 初始化时间轴
        /// </summary>
        public void Initialize(int totalFrames)
        {
            _totalFrames = Mathf.Max(totalFrames, 1);
            _currentFrame = 0;
            _isInitialized = true;
            
            Debug.Log($"{LOG_PREFIX} Initialized with {_totalFrames} frames");
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            _events.Clear();
            _tracks.Clear();
            _eventsByTrack.Clear();
            _isInitialized = false;
        }
        
        // === 核心绘制方法 ===
        
        /// <summary>
        /// 绘制时间轴
        /// </summary>
        public void DrawTimeline(Rect rect)
        {
            if (!_isInitialized)
            {
                UnityEditor.EditorGUI.HelpBox(rect, "时间轴未初始化", UnityEditor.MessageType.Warning);
                return;
            }
            
            // 计算布局
            _layoutCalculator.CalculateLayout(rect, _totalFrames, _tracks.Count);
            
            // 绘制帧刻度
            Rect frameScaleRect = _layoutCalculator.GetFrameScaleRect();
            _renderer.DrawFrameScale(frameScaleRect, _totalFrames, _currentFrame);
            
            // 绘制播放头区域
            Rect playheadRect = _layoutCalculator.GetPlayheadRect();
            _renderer.DrawPlayhead(playheadRect, _currentFrame);
            
            // 绘制轨道区域
            Rect trackAreaRect = _layoutCalculator.GetTrackAreaRect();
            _renderer.DrawTracks(trackAreaRect, _tracks, _eventsByTrack, _currentFrame);
            
            // 处理交互
            _interaction.HandleInput(rect, Event.current, _tracks, _eventsByTrack, _currentFrame);
        }
        
        // === 数据设置方法 ===
        
        /// <summary>
        /// 设置事件列表
        /// </summary>
        public void SetEvents(List<TimelineEvent> events)
        {
            _events = events ?? new List<TimelineEvent>();
            RebuildEventsByTrack();
            
            Debug.Log($"{LOG_PREFIX} Set {_events.Count} events");
        }
        
        /// <summary>
        /// 设置轨道列表
        /// </summary>
        public void SetTracks(List<TimelineTrackConfig> tracks)
        {
            _tracks = tracks ?? new List<TimelineTrackConfig>();
            RebuildEventsByTrack();
            
            Debug.Log($"{LOG_PREFIX} Set {_tracks.Count} tracks");
        }
        
        /// <summary>
        /// 设置当前帧
        /// </summary>
        public void SetCurrentFrame(int frame)
        {
            int oldFrame = _currentFrame;
            _currentFrame = Mathf.Clamp(frame, 0, _totalFrames - 1);
            
            if (_currentFrame != oldFrame)
            {
                OnCurrentFrameChanged?.Invoke(_currentFrame);
            }
        }
        
        /// <summary>
        /// 设置总帧数
        /// </summary>
        public void SetTotalFrames(int totalFrames)
        {
            _totalFrames = Mathf.Max(totalFrames, 1);
            _currentFrame = Mathf.Min(_currentFrame, _totalFrames - 1);
        }
        
        // === 事件管理 ===
        
        /// <summary>
        /// 添加事件
        /// </summary>
        public void AddEvent(TimelineEvent evt)
        {
            if (evt == null)
            {
                Debug.LogError($"{LOG_PREFIX} Cannot add null event");
                return;
            }
            
            _events.Add(evt);
            RebuildEventsByTrack();
            
            OnEventAdded?.Invoke(evt);
            Debug.Log($"{LOG_PREFIX} Added event: {evt.EventId}");
        }
        
        /// <summary>
        /// 移除事件
        /// </summary>
        public void RemoveEvent(TimelineEvent evt)
        {
            if (evt == null) return;
            
            if (_events.Remove(evt))
            {
                RebuildEventsByTrack();
                OnEventRemoved?.Invoke(evt);
                Debug.Log($"{LOG_PREFIX} Removed event: {evt.EventId}");
            }
        }
        
        /// <summary>
        /// 移除选中的事件
        /// </summary>
        public void RemoveSelectedEvent()
        {
            TimelineEvent selected = _interaction.GetSelectedEvent();
            if (selected != null)
            {
                RemoveEvent(selected);
            }
        }
        
        /// <summary>
        /// 获取选中的事件
        /// </summary>
        public TimelineEvent GetSelectedEvent()
        {
            return _interaction.GetSelectedEvent();
        }
        
        /// <summary>
        /// 获取所有事件
        /// </summary>
        public List<TimelineEvent> GetAllEvents()
        {
            return _events;
        }
        
        // === 属性 ===
        
        public int GetCurrentFrame()
        {
            return _currentFrame;
        }
        
        public int GetTotalFrames()
        {
            return _totalFrames;
        }
        
        public float GetPixelsPerFrame()
        {
            return _layoutCalculator.GetPixelsPerFrame();
        }
        
        public void SetZoomLevel(float pixelsPerFrame)
        {
            _layoutCalculator.SetZoomLevel(pixelsPerFrame);
        }
        
        public void ZoomIn()
        {
            _layoutCalculator.ZoomIn();
        }
        
        public void ZoomOut()
        {
            _layoutCalculator.ZoomOut();
        }
        
        // === 私有方法 ===
        
        private void RebuildEventsByTrack()
        {
            _eventsByTrack.Clear();
            
            foreach (TimelineEvent evt in _events)
            {
                if (!_eventsByTrack.ContainsKey(evt.TrackType))
                {
                    _eventsByTrack[evt.TrackType] = new List<TimelineEvent>();
                }
                
                _eventsByTrack[evt.TrackType].Add(evt);
            }
            
            // 排序（按起始帧）
            foreach (var kvp in _eventsByTrack)
            {
                kvp.Value.Sort((a, b) => a.StartFrame.CompareTo(b.StartFrame));
            }
        }
        
        // === 事件处理器 ===
        
        private void HandleEventSelected(TimelineEvent evt)
        {
            OnEventSelected?.Invoke(evt);
        }
        
        private void HandleEventMoved(TimelineEvent evt)
        {
            OnEventModified?.Invoke(evt);
        }
        
        private void HandleEventResized(TimelineEvent evt)
        {
            OnEventModified?.Invoke(evt);
        }
        
        private void HandlePlayheadMoved(int frame)
        {
            SetCurrentFrame(frame);
        }
        
        private void HandleAddEventRequested(int frame, string trackType)
        {
            // 创建新事件（需要在外部实现具体逻辑）
            Debug.Log($"{LOG_PREFIX} Add event requested at frame {frame}, track {trackType}");
        }
    }
}
