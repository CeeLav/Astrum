using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Timeline
{
    /// <summary>
    /// 时间轴交互处理器
    /// 负责处理所有用户输入和交互
    /// </summary>
    public class TimelineInteraction
    {
        private const string LOG_PREFIX = "[TimelineInteraction]";
        
        // === 依赖 ===
        private TimelineLayoutCalculator _layoutCalculator;
        
        // === 交互状态 ===
        private TimelineEvent _selectedEvent;
        private TimelineEvent _draggingEvent;
        private TimelineEvent _hoverEvent;
        
        private bool _isDraggingPlayhead = false;
        private bool _isDraggingEvent = false;
        private bool _isResizingEventStart = false;
        private bool _isResizingEventEnd = false;
        
        private Vector2 _dragStartPosition;
        private int _dragStartFrame;
        
        // === 构造函数 ===
        public TimelineInteraction(TimelineLayoutCalculator layoutCalculator)
        {
            _layoutCalculator = layoutCalculator;
        }
        
        // === 事件 ===
        public event Action<TimelineEvent> OnEventSelected;
        public event Action<TimelineEvent> OnEventMoved;
        public event Action<TimelineEvent> OnEventResized;
        public event Action<int> OnPlayheadMoved;
        public event Action<int, string> OnAddEventRequested; // 参数：帧号，轨道类型
        
        // === 核心方法 ===
        
        /// <summary>
        /// 处理输入
        /// </summary>
        public void HandleInput(
            Rect rect,
            Event evt,
            List<TimelineTrackConfig> tracks,
            Dictionary<string, List<TimelineEvent>> eventsByTrack,
            int currentFrame)
        {
            EventType eventType = evt.type;
            
            switch (eventType)
            {
                case EventType.MouseDown:
                    HandleMouseDown(evt, rect, tracks, eventsByTrack);
                    break;
                    
                case EventType.MouseDrag:
                    HandleMouseDrag(evt, rect);
                    break;
                    
                case EventType.MouseUp:
                    HandleMouseUp(evt);
                    break;
                    
                case EventType.MouseMove:
                    HandleMouseMove(evt, rect, tracks, eventsByTrack);
                    break;
                    
                case EventType.KeyDown:
                    HandleKeyDown(evt, currentFrame);
                    break;
                    
                case EventType.ContextClick:
                    HandleContextClick(evt, rect, tracks);
                    break;
            }
        }
        
        /// <summary>
        /// 获取当前选中的事件
        /// </summary>
        public TimelineEvent GetSelectedEvent()
        {
            return _selectedEvent;
        }
        
        /// <summary>
        /// 设置选中的事件
        /// </summary>
        public void SetSelectedEvent(TimelineEvent evt)
        {
            _selectedEvent = evt;
            OnEventSelected?.Invoke(evt);
        }
        
        /// <summary>
        /// 获取悬停的事件
        /// </summary>
        public TimelineEvent GetHoverEvent()
        {
            return _hoverEvent;
        }
        
        // === 私有处理方法 ===
        
        private void HandleMouseDown(
            Event evt,
            Rect rect,
            List<TimelineTrackConfig> tracks,
            Dictionary<string, List<TimelineEvent>> eventsByTrack)
        {
            if (evt.button != 0) return; // 只处理左键
            
            Vector2 mousePos = evt.mousePosition;
            _dragStartPosition = mousePos;
            
            // 测试是否点击了播放头
            if (HitTestPlayhead(mousePos, rect))
            {
                _isDraggingPlayhead = true;
                evt.Use();
                return;
            }
            
            // 测试是否点击了事件
            TimelineEvent hitEvent = HitTestEvents(mousePos, rect, tracks, eventsByTrack, out bool isStartEdge, out bool isEndEdge);
            
            if (hitEvent != null)
            {
                _selectedEvent = hitEvent;
                _draggingEvent = hitEvent;
                _dragStartFrame = hitEvent.StartFrame;
                
                if (isStartEdge)
                {
                    _isResizingEventStart = true;
                }
                else if (isEndEdge)
                {
                    _isResizingEventEnd = true;
                }
                else
                {
                    _isDraggingEvent = true;
                }
                
                OnEventSelected?.Invoke(hitEvent);
                evt.Use();
                return;
            }
            
            // 未点击任何内容，取消选中
            _selectedEvent = null;
            OnEventSelected?.Invoke(null);
        }
        
        private void HandleMouseDrag(Event evt, Rect rect)
        {
            if (evt.button != 0) return;
            
            Vector2 mousePos = evt.mousePosition;
            
            // 拖拽播放头
            if (_isDraggingPlayhead)
            {
                int frame = _layoutCalculator.PixelToFrame(mousePos.x);
                OnPlayheadMoved?.Invoke(frame);
                evt.Use();
                return;
            }
            
            // 拖拽事件
            if (_isDraggingEvent && _draggingEvent != null)
            {
                int currentFrame = _layoutCalculator.PixelToFrame(mousePos.x);
                int frameDelta = currentFrame - _dragStartFrame;
                
                _draggingEvent.StartFrame = Mathf.Max(0, _draggingEvent.StartFrame + frameDelta);
                _draggingEvent.EndFrame = Mathf.Max(_draggingEvent.StartFrame, _draggingEvent.EndFrame + frameDelta);
                _dragStartFrame = currentFrame;
                
                OnEventMoved?.Invoke(_draggingEvent);
                evt.Use();
                return;
            }
            
            // 调整事件起始帧
            if (_isResizingEventStart && _draggingEvent != null)
            {
                int newStartFrame = _layoutCalculator.PixelToFrame(mousePos.x);
                _draggingEvent.StartFrame = Mathf.Clamp(newStartFrame, 0, _draggingEvent.EndFrame);
                
                OnEventResized?.Invoke(_draggingEvent);
                evt.Use();
                return;
            }
            
            // 调整事件结束帧
            if (_isResizingEventEnd && _draggingEvent != null)
            {
                int newEndFrame = _layoutCalculator.PixelToFrame(mousePos.x);
                _draggingEvent.EndFrame = Mathf.Max(newEndFrame, _draggingEvent.StartFrame);
                
                OnEventResized?.Invoke(_draggingEvent);
                evt.Use();
                return;
            }
        }
        
        private void HandleMouseUp(Event evt)
        {
            if (evt.button != 0) return;
            
            _isDraggingPlayhead = false;
            _isDraggingEvent = false;
            _isResizingEventStart = false;
            _isResizingEventEnd = false;
            _draggingEvent = null;
            
            evt.Use();
        }
        
        private void HandleMouseMove(
            Event evt,
            Rect rect,
            List<TimelineTrackConfig> tracks,
            Dictionary<string, List<TimelineEvent>> eventsByTrack)
        {
            Vector2 mousePos = evt.mousePosition;
            
            // 更新悬停事件
            TimelineEvent newHoverEvent = HitTestEvents(mousePos, rect, tracks, eventsByTrack, out _, out _);
            
            if (newHoverEvent != _hoverEvent)
            {
                _hoverEvent = newHoverEvent;
                // 可以触发重绘显示Tooltip
            }
        }
        
        private void HandleKeyDown(Event evt, int currentFrame)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Delete:
                    // 删除选中事件（需要在外部处理）
                    if (_selectedEvent != null)
                    {
                        Debug.Log($"{LOG_PREFIX} Delete key pressed for event {_selectedEvent.EventId}");
                        evt.Use();
                    }
                    break;
                    
                case KeyCode.LeftArrow:
                    // 上一帧
                    OnPlayheadMoved?.Invoke(Mathf.Max(0, currentFrame - 1));
                    evt.Use();
                    break;
                    
                case KeyCode.RightArrow:
                    // 下一帧
                    OnPlayheadMoved?.Invoke(currentFrame + 1);
                    evt.Use();
                    break;
                    
                case KeyCode.Home:
                    // 跳转到开头
                    OnPlayheadMoved?.Invoke(0);
                    evt.Use();
                    break;
            }
        }
        
        private void HandleContextClick(Event evt, Rect rect, List<TimelineTrackConfig> tracks)
        {
            Vector2 mousePos = evt.mousePosition;
            
            // 判断点击在哪个轨道
            string trackType = HitTestTrack(mousePos, rect, tracks);
            
            if (!string.IsNullOrEmpty(trackType))
            {
                int frame = _layoutCalculator.PixelToFrame(mousePos.x);
                ShowContextMenu(frame, trackType);
                evt.Use();
            }
        }
        
        // === 碰撞检测 ===
        
        private bool HitTestPlayhead(Vector2 mousePos, Rect rect)
        {
            // 简化：检测鼠标是否在播放头附近（10像素范围）
            // 实际位置需要根据 layoutCalculator 计算
            return false; // 占位实现
        }
        
        private TimelineEvent HitTestEvents(
            Vector2 mousePos,
            Rect rect,
            List<TimelineTrackConfig> tracks,
            Dictionary<string, List<TimelineEvent>> eventsByTrack,
            out bool isStartEdge,
            out bool isEndEdge)
        {
            isStartEdge = false;
            isEndEdge = false;
            
            // 遍历所有轨道的所有事件
            // 占位实现，实际需要根据轨道和事件位置计算
            
            return null;
        }
        
        private string HitTestTrack(Vector2 mousePos, Rect rect, List<TimelineTrackConfig> tracks)
        {
            // 检测鼠标在哪个轨道上
            // 占位实现
            return null;
        }
        
        // === 上下文菜单 ===
        
        private void ShowContextMenu(int frame, string trackType)
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent($"在帧 {frame} 添加事件"), false, () =>
            {
                OnAddEventRequested?.Invoke(frame, trackType);
            });
            
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("粘贴事件"), false, () => { });
            
            menu.ShowAsContext();
        }
    }
}
