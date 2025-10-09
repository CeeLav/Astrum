using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Timeline
{
    /// <summary>
    /// 时间轴渲染器
    /// 负责绘制时间轴的所有可视元素
    /// </summary>
    public class TimelineRenderer
    {
        // === 颜色配置 ===
        private static readonly Color COLOR_BACKGROUND = new Color(0.25f, 0.25f, 0.25f);
        private static readonly Color COLOR_FRAME_SCALE_BG = new Color(0.2f, 0.2f, 0.2f);
        private static readonly Color COLOR_FRAME_LINE_MAJOR = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color COLOR_FRAME_LINE_MINOR = new Color(0.3f, 0.3f, 0.3f);
        private static readonly Color COLOR_FRAME_TEXT = new Color(0.8f, 0.8f, 0.8f);
        private static readonly Color COLOR_PLAYHEAD = new Color(1f, 0.2f, 0.2f);
        private static readonly Color COLOR_PLAYHEAD_MARKER = new Color(1f, 0.5f, 0f);
        private static readonly Color COLOR_TRACK_BG_ODD = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color COLOR_TRACK_BG_EVEN = new Color(0.25f, 0.25f, 0.25f);
        private static readonly Color COLOR_TRACK_HEADER = new Color(0.2f, 0.2f, 0.2f);
        private static readonly Color COLOR_TRACK_SEPARATOR = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color COLOR_TIMELINE_BORDER = new Color(0.5f, 0.5f, 0.5f); // 更明显的边框颜色
        
        // === 依赖 ===
        private TimelineLayoutCalculator _layoutCalculator;
        
        // === 滚动 ===
        private Vector2 _scrollPosition;
        
        // === 公开属性 ===
        public Vector2 GetScrollPosition() => _scrollPosition;
        
        // === 构造函数 ===
        public TimelineRenderer(TimelineLayoutCalculator layoutCalculator)
        {
            _layoutCalculator = layoutCalculator;
        }
        
        // === 核心绘制方法 ===
        
        /// <summary>
        /// 绘制帧刻度
        /// </summary>
        public void DrawFrameScale(Rect rect, int totalFrames, int currentFrame)
        {
            // 背景
            EditorGUI.DrawRect(rect, COLOR_FRAME_SCALE_BG);
            
            // 刻度线和数字
            DrawFrameLines(rect, totalFrames);
            DrawFrameNumbers(rect, totalFrames);
            
            // 边框（使用更明显的颜色）
            DrawBorder(rect, COLOR_TIMELINE_BORDER);
        }
        
         /// <summary>
         /// 绘制播放头
         /// </summary>
         public void DrawPlayhead(Rect rect, int currentFrame)
         {
             // 背景
             EditorGUI.DrawRect(rect, COLOR_BACKGROUND);
             
             // 边框
             DrawBorder(rect, COLOR_TRACK_SEPARATOR);
         }
         
         /// <summary>
         /// 绘制播放头竖线（在轨道绘制后调用，确保在最上层）
         /// </summary>
         public void DrawPlayheadLines(Rect trackAreaRect, int currentFrame)
         {
             // 计算播放头在轨道内容区域的位置
             float playheadX = _layoutCalculator.FrameToPixel(currentFrame);
             float trackHeaderWidth = _layoutCalculator.GetTrackHeaderWidth();
             
             // 播放头在轨道内容区域的X坐标
             float x = trackAreaRect.x + trackHeaderWidth + playheadX;
             
             // 绘制贯穿整个轨道区域的播放头竖线
             Vector2 start = new Vector2(x, trackAreaRect.y);
             Vector2 end = new Vector2(x, trackAreaRect.yMax);
             
             Handles.color = COLOR_PLAYHEAD;
             Handles.DrawLine(start, end);
             
             // 绘制播放头标记（三角形）
             DrawPlayheadMarker(new Rect(x - 5, trackAreaRect.y - 15, 10, 15), currentFrame);
         }
        
        /// <summary>
        /// 绘制所有轨道
        /// </summary>
        public void DrawTracks(
            Rect rect,
            List<TimelineTrackConfig> tracks,
            Dictionary<string, List<TimelineEvent>> eventsByTrack,
            int currentFrame,
            TimelineEvent selectedEvent = null,
            TimelineEvent hoverEvent = null)
        {
            // 背景
            EditorGUI.DrawRect(rect, COLOR_BACKGROUND);
            
            // 开始滚动视图
            Rect viewRect = new Rect(
                0,
                0,
                _layoutCalculator.GetContentWidth() + _layoutCalculator.GetTrackHeaderWidth(),
                rect.height
            );
            
            _scrollPosition = GUI.BeginScrollView(rect, _scrollPosition, viewRect);
            
            // 绘制每个轨道
            float currentY = 0;
            for (int i = 0; i < tracks.Count; i++)
            {
                TimelineTrackConfig track = tracks[i];
                
                if (!track.IsVisible)
                    continue;
                
                Rect trackRect = new Rect(0, currentY, viewRect.width, track.TrackHeight);
                
                // 获取该轨道的事件
                List<TimelineEvent> events = eventsByTrack.ContainsKey(track.TrackType) 
                    ? eventsByTrack[track.TrackType] 
                    : new List<TimelineEvent>();
                
                // 绘制轨道
                DrawTrack(trackRect, track, events, i, currentFrame, selectedEvent, hoverEvent);
                
                currentY += track.TrackHeight;
            }
            
            GUI.EndScrollView();
        }
        
         /// <summary>
         /// 绘制单个轨道
         /// </summary>
         public void DrawTrack(
             Rect rect,
             TimelineTrackConfig track,
             List<TimelineEvent> events,
             int trackIndex,
             int currentFrame,
             TimelineEvent selectedEvent = null,
             TimelineEvent hoverEvent = null)
         {
             // 交替背景色
             Color bgColor = (trackIndex % 2 == 0) ? COLOR_TRACK_BG_EVEN : COLOR_TRACK_BG_ODD;
             EditorGUI.DrawRect(rect, bgColor);
             
             // 轨道标题区域
             Rect headerRect = new Rect(rect.x, rect.y, _layoutCalculator.GetTrackHeaderWidth(), rect.height);
             DrawTrackHeader(headerRect, track);
             
             // 轨道内容区域
             Rect contentRect = new Rect(
                 rect.x + _layoutCalculator.GetTrackHeaderWidth(),
                 rect.y,
                 rect.width - _layoutCalculator.GetTrackHeaderWidth(),
                 rect.height
             );
             
             // 绘制事件
             foreach (TimelineEvent evt in events)
             {
                 DrawEvent(contentRect, evt, track);
                 
                 // 绘制选中/悬停高亮
                 if (selectedEvent != null && evt.EventId == selectedEvent.EventId)
                 {
                     DrawEventHighlight(contentRect, evt, new Color(1f, 0.8f, 0f, 1f), 2f); // 金色高亮，2px边框
                 }
                 else if (hoverEvent != null && evt.EventId == hoverEvent.EventId)
                 {
                     DrawEventHighlight(contentRect, evt, new Color(1f, 1f, 1f, 0.5f), 1f); // 半透明白色，1px边框
                 }
             }
             
             // 分隔线
             DrawHorizontalLine(new Vector2(rect.x, rect.yMax), rect.width, COLOR_TRACK_SEPARATOR);
         }
        
        /// <summary>
        /// 绘制轨道标题
        /// </summary>
        private void DrawTrackHeader(Rect rect, TimelineTrackConfig track)
        {
            EditorGUI.DrawRect(rect, COLOR_TRACK_HEADER);
            
            // 图标和名称
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.normal.textColor = track.TrackColor;
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.fontSize = 11;
            
            string displayText = $"{track.TrackIcon} {track.TrackName}";
            GUI.Label(new Rect(rect.x + 5, rect.y, rect.width - 10, rect.height), displayText, labelStyle);
            
            // 锁定图标
            if (track.IsLocked)
            {
                GUI.Label(new Rect(rect.xMax - 20, rect.y, 20, rect.height), "🔒");
            }
            
            // 右边框
            DrawVerticalLine(new Vector2(rect.xMax, rect.y), rect.height, COLOR_TRACK_SEPARATOR);
        }
        
        /// <summary>
        /// 绘制事件
        /// </summary>
        public void DrawEvent(Rect trackContentRect, TimelineEvent evt, TimelineTrackConfig track)
        {
            Rect eventRect = _layoutCalculator.GetEventRect(evt, trackContentRect);
            
            // 使用轨道的自定义渲染器
            if (track.EventRenderer != null)
            {
                track.EventRenderer(eventRect, evt);
            }
            else
            {
                // 默认渲染
                DrawDefaultEvent(eventRect, evt, track.TrackColor);
            }
        }
        
        /// <summary>
        /// 默认事件渲染
        /// </summary>
        private void DrawDefaultEvent(Rect rect, TimelineEvent evt, Color color)
        {
            // 背景
            Color bgColor = new Color(color.r, color.g, color.b, 0.7f);
            EditorGUI.DrawRect(rect, bgColor);
            
            // 边框
            DrawBorder(rect, color);
            
            // 显示名称
            if (rect.width > 30)
            {
                GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
                labelStyle.normal.textColor = Color.white;
                labelStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(rect, evt.DisplayName, labelStyle);
            }
        }
        
        /// <summary>
        /// 绘制事件高亮边框
        /// </summary>
        private void DrawEventHighlight(Rect trackContentRect, TimelineEvent evt, Color highlightColor, float borderWidth)
        {
            Rect eventRect = _layoutCalculator.GetEventRect(evt, trackContentRect);
            
            // 扩展矩形用于绘制边框
            Rect highlightRect = new Rect(
                eventRect.x - borderWidth,
                eventRect.y - borderWidth,
                eventRect.width + borderWidth * 2,
                eventRect.height + borderWidth * 2
            );
            
            // 绘制边框（使用 Handles）
            Handles.color = highlightColor;
            
            // 上边框
            Handles.DrawLine(
                new Vector2(highlightRect.x, highlightRect.y),
                new Vector2(highlightRect.xMax, highlightRect.y)
            );
            
            // 下边框
            Handles.DrawLine(
                new Vector2(highlightRect.x, highlightRect.yMax),
                new Vector2(highlightRect.xMax, highlightRect.yMax)
            );
            
            // 左边框
            Handles.DrawLine(
                new Vector2(highlightRect.x, highlightRect.y),
                new Vector2(highlightRect.x, highlightRect.yMax)
            );
            
            // 右边框
            Handles.DrawLine(
                new Vector2(highlightRect.xMax, highlightRect.y),
                new Vector2(highlightRect.xMax, highlightRect.yMax)
            );
        }
        
        // === 辅助绘制方法 ===
        
        private void DrawFrameLines(Rect rect, int totalFrames)
        {
            float pixelsPerFrame = _layoutCalculator.GetPixelsPerFrame();
            float trackHeaderWidth = _layoutCalculator.GetTrackHeaderWidth();
            float startX = rect.x + trackHeaderWidth; // 从轨道标题宽度之后开始绘制
            
            // 确定主刻度和次刻度间隔
            int majorInterval = GetFrameInterval(pixelsPerFrame);
            int minorInterval = Mathf.Max(1, majorInterval / 5); // 次刻度是主刻度的1/5
            
            // 如果缩放太小，不绘制次刻度
            bool drawMinorTicks = pixelsPerFrame >= 3f;
            
            for (int frame = 0; frame <= totalFrames; frame++)
            {
                float x = startX + _layoutCalculator.FrameToPixel(frame);
                
                if (x < rect.x || x > rect.xMax)
                    continue;
                
                bool isMajor = (frame % majorInterval == 0);
                bool isMinor = (frame % minorInterval == 0) && !isMajor;
                
                if (isMajor)
                {
                    // 主刻度：从时间轴基线向下延伸
                    float scaleLineHeight = rect.height * 0.7f; // 延伸到轨道区域
                    DrawVerticalLine(new Vector2(x, rect.y + rect.height), scaleLineHeight, COLOR_FRAME_LINE_MAJOR);
                }
                else if (isMinor && drawMinorTicks)
                {
                    // 次刻度：从基线开始，半高，淡色
                    DrawVerticalLine(new Vector2(x, rect.y + rect.height), rect.height * 0.4f, COLOR_FRAME_LINE_MINOR);
                }
                else if (drawMinorTicks && pixelsPerFrame >= 5f)
                {
                    // 超细刻度：从基线开始，仅在放大时显示
                    Color veryMinorColor = new Color(0.28f, 0.28f, 0.28f);
                    DrawVerticalLine(new Vector2(x, rect.y + rect.height), rect.height * 0.2f, veryMinorColor);
                }
            }
        }
        
        private void DrawFrameNumbers(Rect rect, int totalFrames)
        {
            float pixelsPerFrame = _layoutCalculator.GetPixelsPerFrame();
            float trackHeaderWidth = _layoutCalculator.GetTrackHeaderWidth();
            float startX = rect.x + trackHeaderWidth; // 从轨道标题宽度之后开始绘制
            int frameInterval = GetFrameInterval(pixelsPerFrame);
            
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.normal.textColor = COLOR_FRAME_TEXT;
            labelStyle.alignment = TextAnchor.UpperCenter;
            labelStyle.fontSize = 9;
            
            GUIStyle timeStyle = new GUIStyle(EditorStyles.miniLabel);
            timeStyle.normal.textColor = new Color(0.6f, 0.8f, 1f); // 淡蓝色
            timeStyle.alignment = TextAnchor.UpperCenter;
            timeStyle.fontSize = 8;
            
            for (int frame = 0; frame <= totalFrames; frame += frameInterval)
            {
                float x = startX + _layoutCalculator.FrameToPixel(frame);
                
                if (x < rect.x || x > rect.xMax)
                    continue;
                
                // 帧号
                Rect labelRect = new Rect(x - 20, rect.y + 2, 40, 12);
                GUI.Label(labelRect, frame.ToString(), labelStyle);
                
                // 时间 (50ms/帧)
                float timeMs = frame * 50f;
                string timeLabel;
                if (timeMs >= 1000)
                {
                    timeLabel = $"{timeMs / 1000f:F2}s";
                }
                else
                {
                    timeLabel = $"{timeMs:F0}ms";
                }
                
                Rect timeRect = new Rect(x - 20, rect.y + 14, 40, 10);
                GUI.Label(timeRect, timeLabel, timeStyle);
            }
        }
        
         private void DrawPlayheadMarker(Rect rect, int currentFrame)
         {
             // 三角形标记
             Vector3[] points = new Vector3[]
             {
                 new Vector3(rect.center.x, rect.y + 2),
                 new Vector3(rect.x, rect.y + 10),
                 new Vector3(rect.xMax, rect.y + 10)
             };
             
             Handles.color = COLOR_PLAYHEAD_MARKER;
             Handles.DrawAAConvexPolygon(points);
             
             // 帧号和时间信息
             GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
             labelStyle.normal.textColor = Color.white;
             labelStyle.alignment = TextAnchor.MiddleCenter;
             labelStyle.fontSize = 9;
             labelStyle.fontStyle = FontStyle.Bold;
             
             // 时间信息 (50ms/帧)
             float timeMs = currentFrame * 50f;
             string timeText;
             if (timeMs >= 1000)
             {
                 timeText = $"F{currentFrame} ({timeMs / 1000f:F2}s)";
             }
             else
             {
                 timeText = $"F{currentFrame} ({timeMs:F0}ms)";
             }
             
             // 计算文本大小
             Vector2 textSize = labelStyle.CalcSize(new GUIContent(timeText));
             
             // 绘制背景框（确保文本可见）
             Rect bgRect = new Rect(rect.x - textSize.x/2, rect.y + 12, textSize.x + 4, textSize.y + 2);
             EditorGUI.DrawRect(bgRect, new Color(0, 0, 0, 0.7f));
             
             // 绘制文本
             Rect labelRect = new Rect(rect.x - textSize.x/2 + 2, rect.y + 13, textSize.x, textSize.y);
             GUI.Label(labelRect, timeText, labelStyle);
         }
        
        private void DrawPlayheadLine(Rect contentRect, float x)
        {
            Vector2 start = new Vector2(contentRect.x + x, contentRect.y);
            Vector2 end = new Vector2(contentRect.x + x, contentRect.yMax);
            
            Handles.color = COLOR_PLAYHEAD;
            Handles.DrawLine(start, end);
        }
        
        private void DrawBorder(Rect rect, Color color)
        {
            Handles.color = color;
            Handles.DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y));
            Handles.DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax));
            Handles.DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.x, rect.yMax));
            Handles.DrawLine(new Vector2(rect.xMax, rect.y), new Vector2(rect.xMax, rect.yMax));
        }
        
        private void DrawHorizontalLine(Vector2 start, float width, Color color)
        {
            Handles.color = color;
            Handles.DrawLine(start, start + Vector2.right * width);
        }
        
        private void DrawVerticalLine(Vector2 start, float height, Color color)
        {
            Handles.color = color;
            Handles.DrawLine(start, start + Vector2.down * height);
        }
        
        /// <summary>
        /// 根据缩放级别获取刻度间隔
        /// 50ms/帧标准：20帧=1秒
        /// </summary>
        private int GetFrameInterval(float pixelsPerFrame)
        {
            // 优先使用整秒间隔（20帧）和半秒间隔（10帧）
            if (pixelsPerFrame >= 15) return 5;   // 250ms (0.25s)
            if (pixelsPerFrame >= 10) return 10;  // 500ms (0.5s)
            if (pixelsPerFrame >= 5) return 20;   // 1000ms (1s)
            if (pixelsPerFrame >= 3) return 40;   // 2000ms (2s)
            return 100;                            // 5000ms (5s)
        }
        
        public void SetScrollPosition(Vector2 scrollPosition)
        {
            _scrollPosition = scrollPosition;
        }
    }
}
