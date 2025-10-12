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
        
        // === 剪贴板 ===
        private static TimelineEvent _clipboardEvent;
        
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
            int currentFrame,
            int totalFrames,
            int maxEditableFrame,
            Vector2 scrollPosition = default)
        {
            _currentFrame = currentFrame; // 保存当前帧用于碰撞检测
            _totalFrames = totalFrames; // 保存总帧数用于显示范围
            _maxEditableFrame = maxEditableFrame; // 保存可编辑帧数用于限制
            _scrollPosition = scrollPosition; // 保存滚动位置
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
                    
                case EventType.Repaint:
                    HandleRepaint(evt, rect, tracks, eventsByTrack);
                    break;
            }
        }
        
        /// <summary>
        /// 处理刻度尺区域的输入
        /// </summary>
        public void HandleFrameScaleInput(Rect frameScaleRect, Event evt)
        {
            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                if (frameScaleRect.Contains(evt.mousePosition))
                {
                    // 点击刻度尺，跳转到对应帧
                    // 注意：PixelToFrame 方法内部已经处理了 trackHeaderWidth，所以这里只需要传入相对于 frameScaleRect 的坐标
                    float relativeX = evt.mousePosition.x - frameScaleRect.x;
                    int frame = _layoutCalculator.PixelToFrame(relativeX);
                    frame = Mathf.Max(0, frame);
                    
                    _isDraggingPlayhead = true;
                    OnPlayheadMoved?.Invoke(frame);
                    evt.Use();
                }
            }
            else if (evt.type == EventType.MouseDrag && _isDraggingPlayhead)
            {
                // 拖动刻度尺，持续更新当前帧
                float relativeX = evt.mousePosition.x - frameScaleRect.x;
                int frame = _layoutCalculator.PixelToFrame(relativeX);
                frame = Mathf.Max(0, frame);
                
                OnPlayheadMoved?.Invoke(frame);
                evt.Use();
            }
            else if (evt.type == EventType.MouseUp && evt.button == 0)
            {
                if (_isDraggingPlayhead)
                {
                    _isDraggingPlayhead = false;
                    evt.Use();
                }
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
            // 使用 delayCall 延迟触发回调，避免打断当前 GUI 流程
            TimelineEvent selectedEvent = evt;
            EditorApplication.delayCall += () =>
            {
                OnEventSelected?.Invoke(selectedEvent);
            };
        }
        
        /// <summary>
        /// 获取悬停的事件
        /// </summary>
        public TimelineEvent GetHoverEvent()
        {
            return _hoverEvent;
        }
        
        // === 私有处理方法 ===
        
        private void HandleRepaint(Event evt, Rect rect, List<TimelineTrackConfig> tracks, Dictionary<string, List<TimelineEvent>> eventsByTrack)
        {
            // 在 Repaint 阶段直接检测鼠标位置并设置光标样式
            Vector2 mousePos = Event.current.mousePosition;
            const float FRAME_SCALE_HEIGHT = 30f;
            const float PLAYHEAD_HEIGHT = 20f;
            const float EDGE_WIDTH = 8f;
            float trackHeaderWidth = _layoutCalculator.GetTrackHeaderWidth();
            float trackAreaStartY = rect.y + FRAME_SCALE_HEIGHT + PLAYHEAD_HEIGHT;
            
            // 转换为 ScrollView 内部坐标
            Vector2 localMousePos = new Vector2(
                mousePos.x - rect.x + _scrollPosition.x,
                mousePos.y - trackAreaStartY + _scrollPosition.y
            );
            
            float currentY = 0;
            
            // 快速检测：只检测鼠标所在的轨道
            for (int trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
            {
                TimelineTrackConfig track = tracks[trackIndex];
                if (!track.IsVisible) continue;
                
                if (localMousePos.y >= currentY && localMousePos.y < currentY + track.TrackHeight)
                {
                    if (eventsByTrack.ContainsKey(track.TrackType))
                    {
                        List<TimelineEvent> events = eventsByTrack[track.TrackType];
                        
                        foreach (TimelineEvent timelineEvent in events)
                        {
                            // 计算事件矩形（在 ScrollView 内部坐标系中）
                            float eventStartX = trackHeaderWidth + _layoutCalculator.FrameToPixel(timelineEvent.StartFrame);
                            float eventEndX = trackHeaderWidth + _layoutCalculator.FrameToPixel(timelineEvent.EndFrame + 1);
                            float eventY = currentY + 2;
                            float eventHeight = track.TrackHeight - 4;
                            
                            Rect eventRect = new Rect(eventStartX, eventY, eventEndX - eventStartX, eventHeight);
                            
                            if (eventRect.Contains(localMousePos))
                            {
                                // 检测是否在边缘
                                bool isStartEdge = localMousePos.x >= eventStartX && localMousePos.x <= eventStartX + EDGE_WIDTH;
                                bool isEndEdge = localMousePos.x >= eventEndX - EDGE_WIDTH && localMousePos.x <= eventEndX;
                                
                                // 计算窗口坐标的矩形（复用已计算的像素值）
                                float screenEventStartX = rect.x + eventStartX - _scrollPosition.x;
                                float screenEventEndX = rect.x + eventEndX - _scrollPosition.x;
                                float screenEventY = trackAreaStartY + currentY - _scrollPosition.y;
                                
                                if (isStartEdge || isEndEdge)
                                {
                                    // 在边缘，设置调整大小光标
                                    Rect leftEdgeRect = new Rect(screenEventStartX, screenEventY, EDGE_WIDTH, eventHeight);
                                    Rect rightEdgeRect = new Rect(screenEventEndX - EDGE_WIDTH, screenEventY, EDGE_WIDTH, eventHeight);
                                    EditorGUIUtility.AddCursorRect(leftEdgeRect, MouseCursor.ResizeHorizontal);
                                    EditorGUIUtility.AddCursorRect(rightEdgeRect, MouseCursor.ResizeHorizontal);
                                }
                                else
                                {
                                    // 在中心，设置移动光标
                                    Rect screenEventRect = new Rect(screenEventStartX, screenEventY, screenEventEndX - screenEventStartX, eventHeight);
                                    EditorGUIUtility.AddCursorRect(screenEventRect, MouseCursor.MoveArrow);
                                }
                                return; // 找到悬停事件，立即返回
                            }
                        }
                    }
                    break; // 找到轨道但没有命中事件，跳出循环
                }
                
                currentY += track.TrackHeight;
            }
        }
        
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
                
                // 记录鼠标点击时的帧位置（用于计算拖拽偏移）
                float relativeX = mousePos.x - rect.x;
                _dragStartFrame = _layoutCalculator.PixelToFrame(relativeX);
                
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
                
                // 使用 delayCall 延迟触发回调，避免打断当前 GUI 流程
                TimelineEvent selectedEvent = hitEvent;
                EditorApplication.delayCall += () =>
                {
                    OnEventSelected?.Invoke(selectedEvent);
                };
                evt.Use();
                return;
            }
            
            // 未点击任何内容，取消选中
            _selectedEvent = null;
            EditorApplication.delayCall += () =>
            {
                OnEventSelected?.Invoke(null);
            };
        }
        
        private void HandleMouseDrag(Event evt, Rect rect)
        {
            if (evt.button != 0) return;
            
            Vector2 mousePos = evt.mousePosition;
            
            // 拖拽播放头
            if (_isDraggingPlayhead)
            {
                float relativeX = mousePos.x - rect.x;
                int frame = _layoutCalculator.PixelToFrame(relativeX);
                OnPlayheadMoved?.Invoke(frame);
                evt.Use();
                return;
            }
            
            // 拖拽事件
            if (_isDraggingEvent && _draggingEvent != null)
            {
                float relativeX = mousePos.x - rect.x;
                int currentFrame = _layoutCalculator.PixelToFrame(relativeX);
                int frameDelta = currentFrame - _dragStartFrame;
                
                // 如果没有移动，直接返回
                if (frameDelta == 0)
                {
                    evt.Use();
                    return;
                }
                
                int eventLength = _draggingEvent.EndFrame - _draggingEvent.StartFrame;
                int newStartFrame = _draggingEvent.StartFrame + frameDelta;
                int newEndFrame = _draggingEvent.EndFrame + frameDelta;
                
                // 边界约束：限制移动，保持事件在 [0, maxEditableFrame-1] 范围内
                if (newStartFrame < 0)
                {
                    // 左边界限制
                    newStartFrame = 0;
                    newEndFrame = eventLength;
                }
                else if (newEndFrame > _maxEditableFrame - 1)
                {
                    // 右边界限制
                    newEndFrame = _maxEditableFrame - 1;
                    newStartFrame = newEndFrame - eventLength;
                }
                
                // 更新事件
                _draggingEvent.StartFrame = newStartFrame;
                _draggingEvent.EndFrame = newEndFrame;
                _dragStartFrame = currentFrame;
                
                OnEventMoved?.Invoke(_draggingEvent);
                evt.Use();
                return;
            }
            
            // 调整事件起始帧
            if (_isResizingEventStart && _draggingEvent != null)
            {
                float relativeX = mousePos.x - rect.x;
                int newStartFrame = _layoutCalculator.PixelToFrame(relativeX);
                
                // 约束：不能小于0，不能超过结束帧
                newStartFrame = Mathf.Clamp(newStartFrame, 0, _draggingEvent.EndFrame);
                
                _draggingEvent.StartFrame = newStartFrame;
                
                OnEventResized?.Invoke(_draggingEvent);
                evt.Use();
                return;
            }
            
            // 调整事件结束帧
            if (_isResizingEventEnd && _draggingEvent != null)
            {
                float relativeX = mousePos.x - rect.x;
                int newEndFrame = _layoutCalculator.PixelToFrame(relativeX);
                
                // 约束：不能小于起始帧，不能超过可编辑帧数-1
                newEndFrame = Mathf.Clamp(newEndFrame, _draggingEvent.StartFrame, _maxEditableFrame - 1);
                
                _draggingEvent.EndFrame = newEndFrame;
                
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
            
            // 更新悬停事件并检测是否在边缘
            TimelineEvent newHoverEvent = HitTestEvents(mousePos, rect, tracks, eventsByTrack, out bool isStartEdge, out bool isEndEdge);
            
            // 缓存悬停信息供 Repaint 使用
            _cachedHoverEvent = newHoverEvent;
            _cachedIsStartEdge = isStartEdge;
            _cachedIsEndEdge = isEndEdge;
            
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
                float relativeX = mousePos.x - rect.x;
                int frame = _layoutCalculator.PixelToFrame(relativeX);
                ShowContextMenu(frame, trackType);
                evt.Use();
            }
        }
        
        // === 碰撞检测 ===
        
        private bool HitTestPlayhead(Vector2 mousePos, Rect rect)
        {
            // 注意：这里的 rect 是整个时间轴区域，需要获取轨道区域
            // 但由于我们没有直接传入 trackAreaRect，我们需要重新计算
            // 简化方案：只检测播放头的X坐标范围
            
            float trackHeaderWidth = _layoutCalculator.GetTrackHeaderWidth();
            float playheadX = trackHeaderWidth + _layoutCalculator.FrameToPixel(_currentFrame);
            const float PLAYHEAD_HIT_TOLERANCE = 5f;
            
            // 检测鼠标X坐标是否在播放头附近
            if (Mathf.Abs(mousePos.x - playheadX) <= PLAYHEAD_HIT_TOLERANCE)
            {
                return true;
            }
            
            return false;
        }
        
        private int _currentFrame; // 需要从外部传入当前帧
        private int _totalFrames; // 总帧数（时间轴显示范围）
        private int _maxEditableFrame; // 可编辑帧数（技能Duration）
        private Vector2 _scrollPosition; // 滚动位置
        
        // 缓存的悬停信息（用于 Repaint）
        private TimelineEvent _cachedHoverEvent;
        private bool _cachedIsStartEdge;
        private bool _cachedIsEndEdge;
        
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
            
            const float EDGE_HIT_TOLERANCE = 8f; // 边缘点击容差（±8px）
            const float FRAME_SCALE_HEIGHT = 30f;  // 帧刻度高度
            const float PLAYHEAD_HEIGHT = 20f;     // 播放头高度
            float trackHeaderWidth = _layoutCalculator.GetTrackHeaderWidth();
            
            // 从后往前遍历轨道（优先检测上层轨道）
            // 注意：因为 ScrollView 内部坐标从 (0,0) 开始，需要转换鼠标坐标
            // 轨道区域起始Y = rect.y + 帧刻度高度 + 播放头高度
            float trackAreaStartY = rect.y + FRAME_SCALE_HEIGHT + PLAYHEAD_HEIGHT;
            
            // 将鼠标坐标转换为 ScrollView 内部坐标
            Vector2 localMousePos = new Vector2(
                mousePos.x - rect.x + _scrollPosition.x,  // 减去 rect 偏移，加上滚动偏移
                mousePos.y - trackAreaStartY + _scrollPosition.y
            );
            
            // currentY 在 ScrollView 内部坐标系中从 0 开始
            float currentY = 0;
            
            for (int trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
            {
                TimelineTrackConfig track = tracks[trackIndex];
                
                if (!track.IsVisible)
                    continue;
                
                // 检测鼠标是否在当前轨道的Y范围内（使用本地坐标）
                if (localMousePos.y >= currentY && localMousePos.y < currentY + track.TrackHeight)
                {
                    // 在当前轨道中，检测所有事件
                    if (eventsByTrack.ContainsKey(track.TrackType))
                    {
                        List<TimelineEvent> events = eventsByTrack[track.TrackType];
                        
                        // 从后往前遍历事件（优先检测后添加的事件）
                        for (int i = events.Count - 1; i >= 0; i--)
                        {
                            TimelineEvent timelineEvent = events[i];
                            
                            // 计算事件矩形（在 ScrollView 内部坐标系中）
                            float eventStartX = trackHeaderWidth + _layoutCalculator.FrameToPixel(timelineEvent.StartFrame);
                            float eventEndX = trackHeaderWidth + _layoutCalculator.FrameToPixel(timelineEvent.EndFrame + 1);
                            float eventY = currentY + 2; // 上边距
                            float eventHeight = track.TrackHeight - 4; // 上下边距
                            
                            Rect eventRect = new Rect(eventStartX, eventY, eventEndX - eventStartX, eventHeight);
                            
                            if (eventRect.Contains(localMousePos))
                            {
                                // 检测是否点击在边缘（使用本地坐标）
                                if (localMousePos.x >= eventStartX && localMousePos.x <= eventStartX + EDGE_HIT_TOLERANCE)
                                {
                                    isStartEdge = true;
                                }
                                else if (localMousePos.x >= eventEndX - EDGE_HIT_TOLERANCE && localMousePos.x <= eventEndX)
                                {
                                    isEndEdge = true;
                                }
                                
                                return timelineEvent;
                            }
                        }
                    }
                    
                    // 找到轨道但没有事件命中
                    break;
                }
                
                currentY += track.TrackHeight;
            }
            
            return null;
        }
        
        private string HitTestTrack(Vector2 mousePos, Rect rect, List<TimelineTrackConfig> tracks)
        {
            // 检测鼠标在哪个轨道上
            const float FRAME_SCALE_HEIGHT = 30f;  // 帧刻度高度
            const float PLAYHEAD_HEIGHT = 20f;     // 播放头高度
            
            // 转换为 ScrollView 内部坐标
            float trackAreaStartY = rect.y + FRAME_SCALE_HEIGHT + PLAYHEAD_HEIGHT;
            Vector2 localMousePos = new Vector2(
                mousePos.x - rect.x + _scrollPosition.x,
                mousePos.y - trackAreaStartY + _scrollPosition.y
            );
            
            // currentY 在 ScrollView 内部坐标系中从 0 开始
            float currentY = 0;
            
            for (int trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
            {
                TimelineTrackConfig track = tracks[trackIndex];
                
                if (!track.IsVisible)
                    continue;
                
                // 检测鼠标Y坐标是否在当前轨道范围内（使用本地坐标）
                if (localMousePos.y >= currentY && localMousePos.y < currentY + track.TrackHeight)
                {
                    return track.TrackType;
                }
                
                currentY += track.TrackHeight;
            }
            
            return null;
        }
        
        // === 上下文菜单 ===
        
        private void ShowContextMenu(int frame, string trackType)
        {
            GenericMenu menu = new GenericMenu();
            
            // 添加事件
            menu.AddItem(new GUIContent($"在帧 {frame} 添加事件"), false, () =>
            {
                OnAddEventRequested?.Invoke(frame, trackType);
            });
            
            menu.AddSeparator("");
            
            // 复制选中事件
            if (_selectedEvent != null)
            {
                menu.AddItem(new GUIContent("复制事件"), false, () =>
                {
                    CopySelectedEvent();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("复制事件"));
            }
            
            // 粘贴事件
            if (_clipboardEvent != null)
            {
                menu.AddItem(new GUIContent($"粘贴事件（帧 {frame}）"), false, () =>
                {
                    PasteEvent(frame, trackType);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("粘贴事件"));
            }
            
            menu.AddSeparator("");
            
            // 删除选中事件
            if (_selectedEvent != null)
            {
                menu.AddItem(new GUIContent("删除事件"), false, () =>
                {
                    DeleteSelectedEvent();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("删除事件"));
            }
            
            menu.ShowAsContext();
        }
        
        // === 剪贴板操作 ===
        
        private void CopySelectedEvent()
        {
            if (_selectedEvent != null)
            {
                _clipboardEvent = _selectedEvent.Clone();
                Debug.Log($"{LOG_PREFIX} Copied event: {_selectedEvent.EventId}");
            }
        }
        
        private void PasteEvent(int frame, string trackType)
        {
            if (_clipboardEvent != null)
            {
                TimelineEvent newEvent = _clipboardEvent.Clone();
                newEvent.StartFrame = frame;
                newEvent.EndFrame = frame + _clipboardEvent.GetDuration() - 1;
                newEvent.TrackType = trackType; // 使用目标轨道类型
                
                OnAddEventRequested?.Invoke(frame, trackType); // 触发外部添加逻辑
                Debug.Log($"{LOG_PREFIX} Pasted event at frame {frame}");
            }
        }
        
        private void DeleteSelectedEvent()
        {
            if (_selectedEvent != null)
            {
                Debug.Log($"{LOG_PREFIX} Delete event requested: {_selectedEvent.EventId}");
                // 实际删除由外部处理（通过监听 Delete 键或右键菜单）
            }
        }
    }
}
