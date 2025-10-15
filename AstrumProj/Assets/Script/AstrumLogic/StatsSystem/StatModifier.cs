using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Stats
{
    /// <summary>
    /// 属性修饰器
    /// </summary>
    [MemoryPackable]
    public partial class StatModifier
    {
        /// <summary>来源ID（Buff ID、装备ID等）</summary>
        public int SourceId { get; set; }
        
        /// <summary>修饰器类型</summary>
        public ModifierType Type { get; set; }
        
        /// <summary>数值（定点数）</summary>
        public FP Value { get; set; }
        
        /// <summary>优先级（用于排序）</summary>
        public int Priority { get; set; }
    }
}

