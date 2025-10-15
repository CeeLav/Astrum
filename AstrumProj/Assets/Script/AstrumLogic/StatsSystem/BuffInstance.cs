using MemoryPack;

namespace Astrum.LogicCore.Stats
{
    /// <summary>
    /// Buff实例
    /// </summary>
    [MemoryPackable]
    public partial class BuffInstance
    {
        /// <summary>Buff配置ID</summary>
        public int BuffId { get; set; }
        
        /// <summary>剩余持续帧数</summary>
        public int RemainingFrames { get; set; }
        
        /// <summary>持续时间（总帧数）</summary>
        public int Duration { get; set; }
        
        /// <summary>叠加层数</summary>
        public int StackCount { get; set; } = 1;
        
        /// <summary>是否可叠加</summary>
        public bool Stackable { get; set; } = false;
        
        /// <summary>最大叠加层数</summary>
        public int MaxStack { get; set; } = 1;
        
        /// <summary>施法者ID（用于追踪来源）</summary>
        public long CasterId { get; set; }
        
        /// <summary>Buff类型（1=Buff, 2=Debuff）</summary>
        public int BuffType { get; set; } = 1;
    }
}

