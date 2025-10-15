using System.Collections.Generic;
using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Stats
{
    /// <summary>
    /// 通用属性容器 - 使用字典存储任意属性（定点数）
    /// </summary>
    [MemoryPackable]
    public partial class Stats
    {
        /// <summary>属性值字典（使用定点数确保确定性）</summary>
        public Dictionary<StatType, FP> Values { get; set; } = new Dictionary<StatType, FP>();
        
        /// <summary>获取属性值</summary>
        public FP Get(StatType type)
        {
            return Values.TryGetValue(type, out var value) ? value : FP.Zero;
        }
        
        /// <summary>设置属性值</summary>
        public void Set(StatType type, FP value)
        {
            Values[type] = value;
        }
        
        /// <summary>增加属性值</summary>
        public void Add(StatType type, FP delta)
        {
            if (Values.TryGetValue(type, out var current))
                Values[type] = current + delta;
            else
                Values[type] = delta;
        }
        
        /// <summary>清空所有属性</summary>
        public void Clear()
        {
            Values.Clear();
        }
        
        /// <summary>复制属性</summary>
        public Stats Clone()
        {
            var clone = new Stats();
            foreach (var kvp in Values)
            {
                clone.Values[kvp.Key] = kvp.Value;
            }
            return clone;
        }
        
        /// <summary>获取所有属性（调试用）</summary>
        public Dictionary<StatType, FP> GetAll()
        {
            return new Dictionary<StatType, FP>(Values);
        }
    }
}

