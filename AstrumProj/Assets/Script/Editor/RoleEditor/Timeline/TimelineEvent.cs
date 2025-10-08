using System;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Timeline
{
    /// <summary>
    /// 时间轴事件（通用数据结构）
    /// 用于所有轨道类型的事件数据
    /// </summary>
    [Serializable]
    public class TimelineEvent
    {
        // === 基础信息 ===
        
        /// <summary>事件唯一标识符</summary>
        [SerializeField]
        private string _eventId = Guid.NewGuid().ToString();
        
        /// <summary>轨道类型（如 "BeCancelTag", "VFX", "SFX" 等）</summary>
        [SerializeField]
        private string _trackType = "";
        
        /// <summary>起始帧</summary>
        [SerializeField]
        private int _startFrame = 0;
        
        /// <summary>结束帧</summary>
        [SerializeField]
        private int _endFrame = 0;
        
        /// <summary>事件数据（JSON序列化字符串）</summary>
        [SerializeField]
        private string _eventData = "";
        
        /// <summary>显示名称（用于UI显示）</summary>
        [SerializeField]
        private string _displayName = "";
        
        /// <summary>备注</summary>
        [SerializeField]
        private string _note = "";
        
        // === 属性 ===
        
        public string EventId
        {
            get => _eventId;
            set => _eventId = value;
        }
        
        public string TrackType
        {
            get => _trackType;
            set => _trackType = value;
        }
        
        public int StartFrame
        {
            get => _startFrame;
            set => _startFrame = value;
        }
        
        public int EndFrame
        {
            get => _endFrame;
            set => _endFrame = value;
        }
        
        public string EventData
        {
            get => _eventData;
            set => _eventData = value;
        }
        
        public string DisplayName
        {
            get => _displayName;
            set => _displayName = value;
        }
        
        public string Note
        {
            get => _note;
            set => _note = value;
        }
        
        // === 辅助方法 ===
        
        /// <summary>
        /// 是否为单帧事件
        /// </summary>
        public bool IsSingleFrame()
        {
            return _startFrame == _endFrame;
        }
        
        /// <summary>
        /// 获取事件持续时间（帧数）
        /// </summary>
        public int GetDuration()
        {
            return _endFrame - _startFrame + 1;
        }
        
        /// <summary>
        /// 解析事件数据为具体类型
        /// </summary>
        public T GetEventData<T>() where T : class
        {
            if (string.IsNullOrEmpty(_eventData))
                return null;
            
            try
            {
                return JsonUtility.FromJson<T>(_eventData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TimelineEvent] Failed to parse event data: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 设置事件数据
        /// </summary>
        public void SetEventData<T>(T data) where T : class
        {
            if (data == null)
            {
                _eventData = "";
                return;
            }
            
            try
            {
                _eventData = JsonUtility.ToJson(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TimelineEvent] Failed to serialize event data: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 克隆事件
        /// </summary>
        public TimelineEvent Clone()
        {
            return new TimelineEvent
            {
                EventId = Guid.NewGuid().ToString(), // 生成新ID
                TrackType = this.TrackType,
                StartFrame = this.StartFrame,
                EndFrame = this.EndFrame,
                EventData = this.EventData,
                DisplayName = this.DisplayName,
                Note = this.Note
            };
        }
        
        /// <summary>
        /// 检查是否与另一个事件有重叠
        /// </summary>
        public bool OverlapsWith(TimelineEvent other)
        {
            if (other == null)
                return false;
            
            return !(this.EndFrame < other.StartFrame || this.StartFrame > other.EndFrame);
        }
        
        /// <summary>
        /// 检查指定帧是否在事件范围内
        /// </summary>
        public bool ContainsFrame(int frame)
        {
            return frame >= _startFrame && frame <= _endFrame;
        }
        
        public override string ToString()
        {
            return $"TimelineEvent[{TrackType}] {StartFrame}-{EndFrame}: {DisplayName}";
        }
    }
}
