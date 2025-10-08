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
            
            // 播放头位置
            float x = _layoutCalculator.FrameToPixel(currentFrame) + _layoutCalculator.GetTrackHeaderWidth();
            
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
            float startX = _layoutCalculator.GetTrackHeaderWidth();
            
            // 确定刻度间隔
            int frameInterval = GetFrameInterval(pixelsPerFrame);
            
            for (int frame = 0; frame <= totalFrames; frame++)
            {
                float x = startX + _layoutCalculator.FrameToPixel(frame);
                
                if (x < rect.x || x > rect.xMax)
                    continue;
                
                bool isMajor = (frame % frameInterval == 0);
                Color lineColor = isMajor ? COLOR_FRAME_LINE_MAJOR : COLOR_FRAME_LINE_MINOR;
                float lineHeight = isMajor ? rect.height : rect.height * 0.5f;
                
                DrawVerticalLine(new Vector2(x, rect.yMax - lineHeight), lineHeight, lineColor);
            }
        }
        
        private void DrawFrameNumbers(Rect rect, int totalFrames)
        {
            float pixelsPerFrame = _layoutCalculator.GetPixelsPerFrame();
            float startX = _layoutCalculator.GetTrackHeaderWidth();
            int frameInterval = GetFrameInterval(pixelsPerFrame);
            
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.normal.textColor = COLOR_FRAME_TEXT;
            labelStyle.alignment = TextAnchor.UpperCenter;
            labelStyle.fontSize = 9;
            
            for (int frame = 0; frame <= totalFrames; frame += frameInterval)
            {
                float x = startX + _layoutCalculator.FrameToPixel(frame);
                
                if (x < rect.x || x > rect.xMax)
                    continue;
                
                Rect labelRect = new Rect(x - 20, rect.y + 2, 40, 15);
                GUI.Label(labelRect, frame.ToString(), labelStyle);
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
            labelStyle.fontSize = 8;
            
            Rect labelRect = new Rect(rect.x, rect.y + 12, rect.width, 15);
            GUI.Label(labelRect, currentFrame.ToString(), labelStyle);
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
            Handles.DrawLine(start, start + Vector2.up * height);
        }
        
        private int GetFrameInterval(float pixelsPerFrame)
        {
            if (pixelsPerFrame >= 10) return 5;
            if (pixelsPerFrame >= 5) return 10;
            return 30;
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
