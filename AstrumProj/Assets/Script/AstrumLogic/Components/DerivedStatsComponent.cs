using Astrum.LogicCore.Stats;
using System;
using System.Collections.Generic;
using TrueSync;
using MemoryPack;
using Astrum.LogicCore.Capabilities;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 派生属性组件 - 存储经过修饰器计算后的最终属性值
    /// </summary>
    [MemoryPackable]
    public partial class DerivedStatsComponent : BaseComponent
    {
        /// <summary>
        /// 组件类型 ID（基于 TypeHash 的稳定哈希值，编译期常量）
        /// </summary>
        public static readonly int ComponentTypeId = TypeHash<DerivedStatsComponent>.GetHash();
        
        /// <summary>
        /// 获取组件的类型 ID
        /// </summary>
        public override int GetComponentTypeId() => ComponentTypeId;
        /// <summary>最终属性容器</summary>
        [MemoryPackInclude]
        internal Stats.Stats FinalStats { get; set; } = new Stats.Stats();
        
        /// <summary>修饰器字典（属性类型 → 修饰器列表）</summary>
        [MemoryPackIgnore]
        private Dictionary<StatType, List<StatModifier>> _modifiers = new Dictionary<StatType, List<StatModifier>>();
        
        /// <summary>脏标记（优化：避免频繁重算）</summary>
        [MemoryPackIgnore]
        private bool _isDirty = false;
        
        [MemoryPackConstructor]
        public DerivedStatsComponent(Stats.Stats finalStats) : base()
        {
            FinalStats = finalStats ?? new Stats.Stats();
            _modifiers = new Dictionary<StatType, List<StatModifier>>();
            _isDirty = false;
        }
        
        public DerivedStatsComponent() : base() { }
        
        /// <summary>添加修饰器</summary>
        public void AddModifier(StatType statType, StatModifier modifier)
        {
            if (!_modifiers.ContainsKey(statType))
            {
                _modifiers[statType] = new List<StatModifier>();
            }
            
            _modifiers[statType].Add(modifier);
            _modifiers[statType].Sort((a, b) => a.Priority.CompareTo(b.Priority));
            _isDirty = true;
        }
        
        /// <summary>移除修饰器（按来源ID）</summary>
        public void RemoveModifier(int sourceId)
        {
            foreach (var modList in _modifiers.Values)
            {
                modList.RemoveAll(m => m.SourceId == sourceId);
            }
            _isDirty = true;
        }
        
        /// <summary>清空所有修饰器</summary>
        public void ClearModifiers()
        {
            _modifiers.Clear();
            _isDirty = true;
        }
        
        /// <summary>标记为脏（需要重算）</summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }
        
        /// <summary>重新计算所有派生属性</summary>
        public void RecalculateAll(BaseStatsComponent baseStats)
        {
            FinalStats.Clear();
            
            // 遍历所有可能的属性类型
            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                FP baseValue = baseStats.BaseStats.Get(statType);
                FP finalValue = CalculateFinalStat(baseValue, statType);
                FinalStats.Set(statType, finalValue);
            }
            
            _isDirty = false;
        }
        
        /// <summary>仅在需要时重算（性能优化）</summary>
        public void RecalculateIfDirty(BaseStatsComponent baseStats)
        {
            if (!_isDirty) return;
            RecalculateAll(baseStats);
        }
        
        /// <summary>计算单个属性的最终值</summary>
        private FP CalculateFinalStat(FP baseValue, StatType statType)
        {
            if (!_modifiers.TryGetValue(statType, out var modifiers) || modifiers.Count == 0)
            {
                return baseValue;
            }
            
            FP flatBonus = FP.Zero;        // 固定加成
            FP percentBonus = FP.Zero;     // 百分比加成
            FP finalMultiplier = FP.One;   // 最终乘数
            
            // 按优先级应用修饰器
            foreach (var mod in modifiers)
            {
                switch (mod.Type)
                {
                    case ModifierType.Flat:
                        flatBonus += mod.Value;
                        break;
                    case ModifierType.Percent:
                        percentBonus += mod.Value;
                        break;
                    case ModifierType.FinalMultiplier:
                        finalMultiplier *= (FP.One + mod.Value);
                        break;
                }
            }
            
            // 计算顺序：(基础 + 固定) × (1 + 百分比) × 最终乘数
            return (baseValue + flatBonus) * (FP.One + percentBonus) * finalMultiplier;
        }
        
        /// <summary>获取指定属性的最终值（快捷方法）</summary>
        public FP Get(StatType type) => FinalStats.Get(type);
        
        /// <summary>获取指定属性的最终值（转为float）</summary>
        public float GetFloat(StatType type) => (float)FinalStats.Get(type);
        
        /// <summary>
        /// 重置 DerivedStatsComponent 状态（用于对象池回收）
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            FinalStats.Clear();
            _modifiers.Clear();
            _isDirty = false;
        }
    }
}

