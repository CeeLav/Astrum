using UnityEngine;

namespace Astrum.Editor.RoleEditor.Timeline
{
    /// <summary>
    /// 时间轴布局计算器
    /// 负责计算时间轴各个区域的位置和大小
    /// </summary>
    public class TimelineLayoutCalculator
    {
        // === 布局常量 ===
        private const float TRACK_HEADER_WIDTH = 120f;    // 轨道标题宽度
        private const float FRAME_SCALE_HEIGHT = 30f;      // 帧刻度高度
        private const float PLAYHEAD_HEIGHT = 20f;         // 播放头高度
        private const float MIN_PIXELS_PER_FRAME = 0.5f;   // 最小缩放（允许更小以容纳长动画）
        private const float MAX_PIXELS_PER_FRAME = 20f;    // 最大缩放
        private const float DEFAULT_PIXELS_PER_FRAME = 5f; // 默认缩放
        
        // === 布局参数 ===
        private float _pixelsPerFrame = DEFAULT_PIXELS_PER_FRAME;
        private float _trackHeaderWidth = TRACK_HEADER_WIDTH;
        private Rect _totalRect;
        private int _totalFrames;
        private int _trackCount;
        
        // === 计算结果缓存 ===
        private Rect _frameScaleRect;
        private Rect _playheadRect;
        private Rect _trackAreaRect;
        private float _contentWidth;
        private float _contentHeight;
        
        // === 核心方法 ===
        
        /// <summary>
        /// 计算布局
        /// </summary>
        public void CalculateLayout(Rect totalRect, int totalFrames, int trackCount)
        {
            _totalRect = totalRect;
            _totalFrames = Mathf.Max(totalFrames, 1);
            _trackCount = Mathf.Max(trackCount, 0);
            
            // 计算内容宽度和高度
            _contentWidth = _totalFrames * _pixelsPerFrame;
            _contentHeight = 0;
            
            // 计算各区域矩形
            CalculateFrameScaleRect();
            CalculatePlayheadRect();
            CalculateTrackAreaRect();
        }
        
        /// <summary>
        /// 设置缩放级别
        /// </summary>
        public void SetZoomLevel(float pixelsPerFrame)
        {
            _pixelsPerFrame = Mathf.Clamp(pixelsPerFrame, MIN_PIXELS_PER_FRAME, MAX_PIXELS_PER_FRAME);
        }
        
        /// <summary>
        /// 放大
        /// </summary>
        public void ZoomIn(float delta = 1f)
        {
            SetZoomLevel(_pixelsPerFrame + delta);
        }
        
        /// <summary>
        /// 缩小
        /// </summary>
        public void ZoomOut(float delta = 1f)
        {
            SetZoomLevel(_pixelsPerFrame - delta);
        }
        
        /// <summary>
        /// 自动适应：计算合适的缩放级别，使时间轴填满可用宽度
        /// </summary>
        public void FitToWidth(float availableWidth, int totalFrames)
        {
            if (totalFrames <= 0) return;
            
            // 计算内容区域宽度（排除轨道标题）
            float contentWidth = availableWidth - _trackHeaderWidth;
            
            // 计算合适的缩放级别
            float idealPixelsPerFrame = contentWidth / totalFrames;
            
            // 只限制最小值，不限制最大值（自动适应优先填满宽度）
            if (idealPixelsPerFrame < MIN_PIXELS_PER_FRAME)
            {
                // 如果计算出的缩放太小，使用最小缩放，允许横向滚动
                _pixelsPerFrame = MIN_PIXELS_PER_FRAME;
            }
            else
            {
                // 使用计算出的值，优先填满宽度（不受MAX_PIXELS_PER_FRAME限制）
                _pixelsPerFrame = idealPixelsPerFrame;
            }
            
            // Debug.Log($"[TimelineLayoutCalculator] FitToWidth: availableWidth={availableWidth}, totalFrames={totalFrames}, idealPixelsPerFrame={idealPixelsPerFrame:F2}, finalPixelsPerFrame={_pixelsPerFrame:F2}");
        }
        
        // === 区域获取方法 ===
        
        /// <summary>
        /// 获取帧刻度区域
        /// </summary>
        public Rect GetFrameScaleRect()
        {
            return _frameScaleRect;
        }
        
        /// <summary>
        /// 获取播放头区域
        /// </summary>
        public Rect GetPlayheadRect()
        {
            return _playheadRect;
        }
        
        /// <summary>
        /// 获取轨道区域
        /// </summary>
        public Rect GetTrackAreaRect()
        {
            return _trackAreaRect;
        }
        
        /// <summary>
        /// 获取指定轨道的矩形
        /// </summary>
        public Rect GetTrackRect(int trackIndex, float trackHeight)
        {
            float y = _trackAreaRect.y;
            
            // 计算前面所有轨道的总高度
            for (int i = 0; i < trackIndex; i++)
            {
                y += trackHeight; // 简化：假设所有轨道高度相同
            }
            
            return new Rect(
                _trackAreaRect.x,
                y,
                _trackAreaRect.width,
                trackHeight
            );
        }
        
        /// <summary>
        /// 获取轨道标题区域
        /// </summary>
        public Rect GetTrackHeaderRect(int trackIndex, float trackHeight)
        {
            Rect trackRect = GetTrackRect(trackIndex, trackHeight);
            
            return new Rect(
                trackRect.x,
                trackRect.y,
                _trackHeaderWidth,
                trackHeight
            );
        }
        
        /// <summary>
        /// 获取轨道内容区域（排除标题）
        /// </summary>
        public Rect GetTrackContentRect(int trackIndex, float trackHeight)
        {
            Rect trackRect = GetTrackRect(trackIndex, trackHeight);
            
            return new Rect(
                trackRect.x + _trackHeaderWidth,
                trackRect.y,
                trackRect.width - _trackHeaderWidth,
                trackHeight
            );
        }
        
        /// <summary>
        /// 获取事件的矩形
        /// </summary>
        public Rect GetEventRect(TimelineEvent evt, Rect trackContentRect)
        {
            float startX = FrameToPixel(evt.StartFrame);
            float endX = FrameToPixel(evt.EndFrame + 1); // +1 因为结束帧是包含的
            float width = endX - startX;
            
            return new Rect(
                trackContentRect.x + startX,
                trackContentRect.y + 2, // 上边距
                width,
                trackContentRect.height - 4 // 上下边距
            );
        }
        
        /// <summary>
        /// 获取播放头的命中区域（用于检测点击）
        /// </summary>
        public Rect GetPlayheadHitRect(int currentFrame, Rect trackAreaRect)
        {
            float playheadX = _trackHeaderWidth + FrameToPixel(currentFrame);
            const float PLAYHEAD_HIT_TOLERANCE = 5f; // 播放头点击容差（±5px）
            
            return new Rect(
                playheadX - PLAYHEAD_HIT_TOLERANCE,
                trackAreaRect.y,
                PLAYHEAD_HIT_TOLERANCE * 2,
                trackAreaRect.height
            );
        }
        
        // === 坐标转换 ===
        
        /// <summary>
        /// 像素X坐标转换为帧号
        /// </summary>
        public int PixelToFrame(float pixelX)
        {
            float relativeX = pixelX - _trackHeaderWidth;
            int frame = Mathf.FloorToInt(relativeX / _pixelsPerFrame);
            return Mathf.Clamp(frame, 0, _totalFrames - 1);
        }
        
        /// <summary>
        /// 帧号转换为像素X坐标
        /// </summary>
        public float FrameToPixel(int frame)
        {
            return frame * _pixelsPerFrame;
        }
        
        // === 属性 ===
        
        public float GetPixelsPerFrame()
        {
            return _pixelsPerFrame;
        }
        
        public float GetContentWidth()
        {
            return _contentWidth;
        }
        
        public float GetContentHeight()
        {
            return _contentHeight;
        }
        
        public float GetTrackHeaderWidth()
        {
            return _trackHeaderWidth;
        }
        
        // === 私有计算方法 ===
        
        private void CalculateFrameScaleRect()
        {
            _frameScaleRect = new Rect(
                _totalRect.x,
                _totalRect.y,
                _totalRect.width,
                FRAME_SCALE_HEIGHT
            );
        }
        
        private void CalculatePlayheadRect()
        {
            _playheadRect = new Rect(
                _totalRect.x,
                _totalRect.y + FRAME_SCALE_HEIGHT,
                _totalRect.width,
                PLAYHEAD_HEIGHT
            );
        }
        
        private void CalculateTrackAreaRect()
        {
            float y = _totalRect.y + FRAME_SCALE_HEIGHT + PLAYHEAD_HEIGHT;
            float height = _totalRect.height - FRAME_SCALE_HEIGHT - PLAYHEAD_HEIGHT;
            
            _trackAreaRect = new Rect(
                _totalRect.x,
                y,
                _totalRect.width,
                height
            );
        }
    }
}
