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
        private int _totalFrames = 60;          // 时间轴显示范围（动画总帧数）
        private int _maxEditableFrame = 60;     // 可编辑范围（技能Duration）
        private bool _isInitialized = false;
        private float _lastRectWidth = 0;       // 用于检测窗口大小变化
        
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
            
            // 先计算一次布局以获取轨道区域（使用默认缩放）
            _layoutCalculator.CalculateLayout(rect, _totalFrames, _tracks.Count);
            Rect trackAreaRect = _layoutCalculator.GetTrackAreaRect();
            
            // 调试：检测窗口大小变化（已禁用）
            // if (Event.current.type == EventType.Repaint && Mathf.Abs(rect.width - _lastRectWidth) > 1f)
            // {
            //     Debug.Log($"[TimelineModule] 窗口大小变化: {_lastRectWidth:F1} → {rect.width:F1}, trackArea.width={trackAreaRect.width:F1}");
            //     _lastRectWidth = rect.width;
            // }
            
            // 根据轨道区域宽度自动调整缩放
            _layoutCalculator.FitToWidth(trackAreaRect.width, _totalFrames);
            
            // 重新计算布局（使用调整后的缩放）
            _layoutCalculator.CalculateLayout(rect, _totalFrames, _tracks.Count);
            
            // 绘制播放头区域
            Rect playheadRect = _layoutCalculator.GetPlayheadRect();
            _renderer.DrawPlayhead(playheadRect, _currentFrame);
            
            // 获取最终的轨道区域
            trackAreaRect = _layoutCalculator.GetTrackAreaRect();
            
            // 传递选中和悬停事件用于高亮显示
            TimelineEvent selectedEvent = _interaction.GetSelectedEvent();
            TimelineEvent hoverEvent = _interaction.GetHoverEvent();
            
            bool showBoundary = _maxEditableFrame < _totalFrames;
            _renderer.DrawTracks(trackAreaRect, _tracks, _eventsByTrack, _currentFrame, selectedEvent, hoverEvent, showBoundary, _maxEditableFrame);
            
            // 在轨道绘制完成后，获取滚动位置并绘制帧刻度（确保使用同步的滚动位置）
            Vector2 scrollPosition = _renderer.GetScrollPosition();
            Rect frameScaleRect = _layoutCalculator.GetFrameScaleRect();
            _renderer.DrawFrameScale(frameScaleRect, _totalFrames, _currentFrame, scrollPosition);
            
            // 重新绘制播放头竖线（确保在最上层）
            _renderer.DrawPlayheadLines(trackAreaRect, _currentFrame);
            
            // 处理刻度尺区域的交互（在其他交互之前处理，优先级更高）
            _interaction.HandleFrameScaleInput(frameScaleRect, Event.current);
            
            // 处理其他交互（传递滚动位置、总帧数和可编辑帧数）
            _interaction.HandleInput(rect, Event.current, _tracks, _eventsByTrack, _currentFrame, _totalFrames, _maxEditableFrame, scrollPosition);
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
        /// 设置当前帧（限制在可编辑范围内）
        /// </summary>
        public void SetCurrentFrame(int frame)
        {
            int oldFrame = _currentFrame;
            _currentFrame = Mathf.Clamp(frame, 0, _maxEditableFrame - 1);
            
            if (_currentFrame != oldFrame)
            {
                OnCurrentFrameChanged?.Invoke(_currentFrame);
            }
        }
        
        /// <summary>
        /// 设置总帧数（时间轴显示范围）
        /// </summary>
        public void SetTotalFrames(int totalFrames)
        {
            _totalFrames = Mathf.Max(totalFrames, 1);
            _currentFrame = Mathf.Min(_currentFrame, _maxEditableFrame - 1);
        }
        
        /// <summary>
        /// 设置可编辑的最大帧数（技能Duration）
        /// </summary>
        public void SetMaxEditableFrame(int maxFrame)
        {
            _maxEditableFrame = Mathf.Max(maxFrame, 1);
            _currentFrame = Mathf.Min(_currentFrame, _maxEditableFrame - 1);
        }
        
        /// <summary>
        /// 同时设置总帧数和可编辑帧数
        /// </summary>
        public void SetFrameRange(int totalFrames, int maxEditableFrame)
        {
            _totalFrames = Mathf.Max(totalFrames, 1);
            _maxEditableFrame = Mathf.Max(maxEditableFrame, 1);
            _currentFrame = Mathf.Min(_currentFrame, _maxEditableFrame - 1);
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
        
        public int GetMaxEditableFrame()
        {
            return _maxEditableFrame;
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
            // 创建新事件
            TimelineEvent newEvent = CreateDefaultEvent(frame, trackType);
            
            if (newEvent != null)
            {
                AddEvent(newEvent);
                Debug.Log($"{LOG_PREFIX} Added new event: {trackType} at frame {frame}");
            }
        }
        
        /// <summary>
        /// 创建默认事件（根据轨道类型）
        /// </summary>
        private TimelineEvent CreateDefaultEvent(int frame, string trackType)
        {
            TimelineEvent evt = new TimelineEvent
            {
                EventId = System.Guid.NewGuid().ToString(),
                TrackType = trackType,
                StartFrame = frame,
                EndFrame = Mathf.Min(frame + 10, _maxEditableFrame - 1), // 默认10帧长度，限制在可编辑范围内
                DisplayName = GetDefaultEventName(trackType)
            };
            
            // 根据轨道类型设置默认数据
            switch (trackType)
            {
                case "BeCancelTag":
                    var beCancelData = Timeline.EventData.BeCancelTagEventData.CreateDefault();
                    evt.SetEventData(beCancelData);
                    evt.DisplayName = beCancelData.GetDisplayName();
                    break;
                    
                case "VFX":
                    var vfxData = Timeline.EventData.VFXEventData.CreateDefault();
                    evt.SetEventData(vfxData);
                    evt.DisplayName = vfxData.GetDisplayName();
                    break;
                    
                case "SFX":
                    var sfxData = Timeline.EventData.SFXEventData.CreateDefault();
                    evt.SetEventData(sfxData);
                    evt.DisplayName = sfxData.GetDisplayName();
                    break;
                    
                case "CameraShake":
                    var shakeData = Timeline.EventData.CameraShakeEventData.CreateDefault();
                    evt.SetEventData(shakeData);
                    evt.DisplayName = shakeData.GetDisplayName();
                    break;
                    
                default:
                    evt.DisplayName = "新事件";
                    break;
            }
            
            return evt;
        }
        
        /// <summary>
        /// 获取默认事件名称
        /// </summary>
        private string GetDefaultEventName(string trackType)
        {
            switch (trackType)
            {
                case "BeCancelTag": return "被取消标签";
                case "VFX": return "特效";
                case "SFX": return "音效";
                case "CameraShake": return "相机震动";
                default: return "新事件";
            }
        }
    }
}
