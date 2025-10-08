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
        
        // === 依赖 ===
        private TimelineLayoutCalculator _layoutCalculator;
        
        // === 滚动 ===
        private Vector2 _scrollPosition;
        
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
            
            // 边框
            DrawBorder(rect, COLOR_TRACK_SEPARATOR);
        }
        
        /// <summary>
        /// 绘制播放头
        /// </summary>
        public void DrawPlayhead(Rect rect, int currentFrame)
        {
            // 背景
            EditorGUI.DrawRect(rect, COLOR_BACKGROUND);
            
            // 播放头位置（相对于轨道内容区域）
            float x = rect.x + _layoutCalculator.GetTrackHeaderWidth() + _layoutCalculator.FrameToPixel(currentFrame);
            
            // 绘制播放头标记
            DrawPlayheadMarker(new Rect(x - 5, rect.y, 10, rect.height), currentFrame);
            
            // 边框
            DrawBorder(rect, COLOR_TRACK_SEPARATOR);
        }
        
        /// <summary>
        /// 绘制所有轨道
        /// </summary>
        public void DrawTracks(
            Rect rect,
            List<TimelineTrackConfig> tracks,
            Dictionary<string, List<TimelineEvent>> eventsByTrack,
            int currentFrame)
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
                DrawTrack(trackRect, track, events, i, currentFrame);
                
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
            int currentFrame)
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
            
            // 绘制播放头竖线
            float playheadX = _layoutCalculator.FrameToPixel(currentFrame);
            DrawPlayheadLine(contentRect, playheadX);
            
            // 绘制事件
            foreach (TimelineEvent evt in events)
            {
                DrawEvent(contentRect, evt, track);
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
                    // 主刻度：全高，明显
                    DrawVerticalLine(new Vector2(x, rect.y), rect.height, COLOR_FRAME_LINE_MAJOR);
                }
                else if (isMinor && drawMinorTicks)
                {
                    // 次刻度：半高，淡色
                    DrawVerticalLine(new Vector2(x, rect.y + rect.height * 0.6f), rect.height * 0.4f, COLOR_FRAME_LINE_MINOR);
                }
                else if (drawMinorTicks && pixelsPerFrame >= 5f)
                {
                    // 超细刻度：仅在放大时显示
                    Color veryMinorColor = new Color(0.28f, 0.28f, 0.28f);
                    DrawVerticalLine(new Vector2(x, rect.y + rect.height * 0.8f), rect.height * 0.2f, veryMinorColor);
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
            
            // 帧号文本
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
            
            Rect labelRect = new Rect(rect.x - 10, rect.y + 12, rect.width + 20, 15);
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
        
        // === 属性 ===
        
        public Vector2 GetScrollPosition()
        {
            return _scrollPosition;
        }
        
        public void SetScrollPosition(Vector2 scrollPosition)
        {
            _scrollPosition = scrollPosition;
        }
    }
}
