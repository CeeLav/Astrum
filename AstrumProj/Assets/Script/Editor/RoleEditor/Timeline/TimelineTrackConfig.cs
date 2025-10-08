using System;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Timeline
{
    /// <summary>
    /// 时间轴轨道配置
    /// 定义轨道的显示和行为
    /// </summary>
    [Serializable]
    public class TimelineTrackConfig
    {
        // === 基础配置 ===
        
        /// <summary>轨道类型标识（唯一）</summary>
        public string TrackType;
        
        /// <summary>轨道显示名称</summary>
        public string TrackName;
        
        /// <summary>轨道图标（emoji或路径）</summary>
        public string TrackIcon;
        
        /// <summary>轨道颜色</summary>
        public Color TrackColor;
        
        /// <summary>轨道高度（像素）</summary>
        public float TrackHeight = 45f;
        
        /// <summary>是否可见</summary>
        public bool IsVisible = true;
        
        /// <summary>是否锁定（锁定后不可编辑）</summary>
        public bool IsLocked = false;
        
        /// <summary>排序顺序（越小越靠上）</summary>
        public int SortOrder = 0;
        
        /// <summary>是否允许事件重叠</summary>
        public bool AllowOverlap = true;
        
        // === 渲染和编辑器委托 ===
        
        /// <summary>
        /// 事件渲染器
        /// 参数：事件绘制区域, 事件数据
        /// </summary>
        public Action<Rect, TimelineEvent> EventRenderer;
        
        /// <summary>
        /// 事件编辑器
        /// 参数：事件数据
        /// 返回：是否修改了数据
        /// </summary>
        public Func<TimelineEvent, bool> EventEditor;
        
        // === 辅助方法 ===
        
        /// <summary>
        /// 创建默认轨道配置
        /// </summary>
        public static TimelineTrackConfig CreateDefault(string trackType, string trackName)
        {
            return new TimelineTrackConfig
            {
                TrackType = trackType,
                TrackName = trackName,
                TrackIcon = "●",
                TrackColor = Color.gray,
                TrackHeight = 45f,
                IsVisible = true,
                IsLocked = false,
                SortOrder = 0,
                AllowOverlap = true
            };
        }
        
        /// <summary>
        /// 克隆轨道配置
        /// </summary>
        public TimelineTrackConfig Clone()
        {
            return new TimelineTrackConfig
            {
                TrackType = this.TrackType,
                TrackName = this.TrackName,
                TrackIcon = this.TrackIcon,
                TrackColor = this.TrackColor,
                TrackHeight = this.TrackHeight,
                IsVisible = this.IsVisible,
                IsLocked = this.IsLocked,
                SortOrder = this.SortOrder,
                AllowOverlap = this.AllowOverlap,
                EventRenderer = this.EventRenderer,
                EventEditor = this.EventEditor
            };
        }
        
        public override string ToString()
        {
            return $"TrackConfig[{TrackType}]: {TrackName}";
        }
    }
}
