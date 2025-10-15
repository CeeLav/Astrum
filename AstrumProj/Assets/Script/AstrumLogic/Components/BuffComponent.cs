using Astrum.LogicCore.Stats;
using Astrum.LogicCore.Managers;
using Astrum.CommonBase;
using System;
using System.Collections.Generic;
using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// Buff组件 - 管理实体身上的所有Buff/Debuff实例
    /// </summary>
    [MemoryPackable]
    public partial class BuffComponent : BaseComponent
    {
        /// <summary>当前所有Buff实例</summary>
        public List<BuffInstance> Buffs { get; set; } = new List<BuffInstance>();
        
        [MemoryPackConstructor]
        public BuffComponent() : base() { }
        
        /// <summary>添加Buff</summary>
        public void AddBuff(BuffInstance buff)
        {
            // 1. 检查是否已存在同类型Buff
            var existing = Buffs.Find(b => b.BuffId == buff.BuffId);
            
            if (existing != null)
            {
                // 2. 处理叠加逻辑
                if (buff.Stackable)
                {
                    existing.StackCount = Math.Min(existing.StackCount + 1, buff.MaxStack);
                    existing.RemainingFrames = buff.Duration; // 刷新持续时间
                }
                else
                {
                    // 不可叠加，刷新持续时间
                    existing.RemainingFrames = buff.Duration;
                }
            }
            else
            {
                // 3. 添加新Buff
                Buffs.Add(buff);
            }
        }
        
        /// <summary>移除Buff</summary>
        public void RemoveBuff(int buffId)
        {
            Buffs.RemoveAll(b => b.BuffId == buffId);
        }
        
        /// <summary>更新所有Buff（每帧调用）</summary>
        public void UpdateBuffs()
        {
            for (int i = Buffs.Count - 1; i >= 0; i--)
            {
                var buff = Buffs[i];
                buff.RemainingFrames--;
                
                // 持续时间结束，移除Buff
                if (buff.RemainingFrames <= 0)
                {
                    Buffs.RemoveAt(i);
                }
            }
        }
        
        /// <summary>获取所有属性修饰器（供DerivedStats计算使用）</summary>
        public Dictionary<StatType, List<StatModifier>> GetAllModifiers()
        {
            var modifiersByType = new Dictionary<StatType, List<StatModifier>>();
            
            foreach (var buff in Buffs)
            {
                // 从Buff配置表读取修饰器
                var buffConfig = ConfigManager.Instance.Tables.TbBuffTable.Get(buff.BuffId);
                if (buffConfig == null) continue;
                
                // 解析Buff的修饰器字符串
                // 格式："ATK:Percent:200;SPD:Flat:1000"
                var modifiers = ParseBuffModifiers(buffConfig.Modifiers, buff);
                
                foreach (var mod in modifiers)
                {
                    if (!modifiersByType.ContainsKey(mod.StatType))
                    {
                        modifiersByType[mod.StatType] = new List<StatModifier>();
                    }
                    modifiersByType[mod.StatType].Add(mod.Modifier);
                }
            }
            
            return modifiersByType;
        }
        
        /// <summary>解析Buff修饰器字符串</summary>
        private List<(StatType StatType, StatModifier Modifier)> ParseBuffModifiers(string modifierStr, BuffInstance buff)
        {
            var result = new List<(StatType, StatModifier)>();
            if (string.IsNullOrEmpty(modifierStr) || modifierStr == "none") return result;
            
            // 格式："ATK:Percent:200;SPD:Flat:1000;CRIT_RATE:Flat:50"
            // 数值全部是int，Percent和小数需要除以1000
            var parts = modifierStr.Split(';');
            
            foreach (var part in parts)
            {
                var tokens = part.Split(':');
                if (tokens.Length != 3) continue;
                
                // 解析属性类型
                if (!Enum.TryParse<StatType>(tokens[0], out var statType))
                {
                    ASLogger.Instance.Warning($"[BuffComponent] Invalid StatType: {tokens[0]}");
                    continue;
                }
                
                // 解析修饰器类型
                if (!Enum.TryParse<ModifierType>(tokens[1], out var modType))
                {
                    ASLogger.Instance.Warning($"[BuffComponent] Invalid ModifierType: {tokens[1]}");
                    continue;
                }
                
                // 解析数值（配置表存int）
                if (!int.TryParse(tokens[2], out var intValue))
                {
                    ASLogger.Instance.Warning($"[BuffComponent] Invalid value: {tokens[2]}");
                    continue;
                }
                
                // 转换为FP
                FP value;
                if (modType == ModifierType.Percent || NeedsDivide1000(statType))
                {
                    // Percent和小数属性：除以1000
                    value = (FP)intValue / (FP)1000;
                }
                else
                {
                    // Flat整数属性：直接转换
                    value = (FP)intValue;
                }
                
                // 应用叠加层数
                value *= (FP)buff.StackCount;
                
                result.Add((statType, new StatModifier
                {
                    SourceId = buff.BuffId,
                    Type = modType,
                    Value = value,
                    Priority = GetModifierPriority(modType)
                }));
            }
            
            return result;
        }
        
        /// <summary>判断属性是否需要除以1000</summary>
        private bool NeedsDivide1000(StatType type)
        {
            return type switch
            {
                StatType.SPD => true,
                StatType.CRIT_RATE => true,
                StatType.CRIT_DMG => true,
                StatType.ACCURACY => true,
                StatType.EVASION => true,
                StatType.BLOCK_RATE => true,
                StatType.PHYSICAL_RES => true,
                StatType.MAGICAL_RES => true,
                StatType.MANA_REGEN => true,
                StatType.HEALTH_REGEN => true,
                _ => false // 其他属性不需要
            };
        }
        
        /// <summary>获取修饰器优先级</summary>
        private int GetModifierPriority(ModifierType type)
        {
            return type switch
            {
                ModifierType.Flat => 100,
                ModifierType.Percent => 200,
                ModifierType.FinalMultiplier => 300,
                _ => 0
            };
        }
    }
}

