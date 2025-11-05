using System.Collections.Generic;
using MemoryPack;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// Capability 在实体上的状态信息
    /// </summary>
    [MemoryPackable]
    public partial struct CapabilityState
    {
        /// <summary>
        /// 是否激活（满足激活条件且未被禁用）
        /// 注意：CapabilityState 在字典中存在即表示实体拥有此 Capability
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// 已激活持续时间（帧数，仅在 TrackActiveDuration 为 true 时更新）
        /// 当 IsActive 为 true 时，每帧递增 1
        /// </summary>
        public int ActiveDuration { get; set; }
        
        /// <summary>
        /// 已禁用持续时间（帧数，仅在 TrackDeactiveDuration 为 true 时更新）
        /// 当 IsActive 为 false 时，每帧递增 1
        /// </summary>
        public int DeactiveDuration { get; set; }
        
        /// <summary>
        /// 自定义数据（预留字段，用于存储 Capability 特定的简单状态）
        /// 例如：冷却时间、计数器等
        /// 注意：MemoryPack 不支持 Dictionary&lt;string, object&gt;，使用 Dictionary&lt;string, string&gt; 存储序列化后的 JSON
        /// 或者使用更具体的类型，如 Dictionary&lt;string, int&gt;、Dictionary&lt;string, float&gt; 等
        /// </summary>
        [MemoryPackIgnore]
        public Dictionary<string, object> CustomData { get; set; }
        
        /// <summary>
        /// 自定义数据序列化版本（用于 MemoryPack）
        /// 存储简单的键值对，值只能是基础类型（int, float, string, bool）
        /// </summary>
        public Dictionary<string, string> CustomDataSerialized { get; set; }
    }
}

