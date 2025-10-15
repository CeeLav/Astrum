# 数值系统策划案

> 📊 完整的数值体系设计：属性、战斗、成长、Buff
>
> **状态**: 📝 设计阶段  
> **版本**: v1.0  
> **更新**: 2025-10-14

---

## 一、系统概述

### 1.1 设计理念

数值系统是游戏战斗的核心基础，负责管理所有实体的属性、状态和数值计算。本系统采用**三层属性架构**，清晰分离基础数值、计算数值和动态数值，支持复杂的Buff、成长、装备等系统扩展。

**核心原则**：
- **数据驱动**：所有基础数值来自配置表
- **分层清晰**：基础→派生→动态，各司其职
- **易于扩展**：支持Buff、装备、技能加成等修饰器
- **性能优先**：运行时数据紧凑，计算高效
- **确定性**：支持帧同步，避免浮点误差

### 1.2 三层属性架构

```
┌──────────────────────────────────────────────────────┐
│                   数值系统架构                         │
└──────────────────────────────────────────────────────┘
                         │
         ┌───────────────┼───────────────┐
         ↓               ↓               ↓
┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│  BaseStats  │  │ DerivedStats│  │DynamicStats │
│  基础属性    │→ │  派生属性   │→ │ 动态属性    │
└─────────────┘  └─────────────┘  └─────────────┘
     ↑                  ↑                  ↑
     │                  │                  │
  配置表            修饰器计算          战斗实时
  
  Level/Growth      Buff/Equipment      HP/MP/能量
```

---

## 二、运行时组件设计

### 2.1 核心数据结构

#### StatType 枚举（属性类型定义）

```csharp
/// <summary>
/// 属性类型枚举 - 定义游戏中所有数值属性
/// </summary>
public enum StatType
{
    // ===== 基础战斗属性 =====
    HP = 1,              // 生命值上限
    ATK = 2,             // 攻击力
    DEF = 3,             // 防御力
    SPD = 4,             // 移动速度
    
    // ===== 高级战斗属性 =====
    CRIT_RATE = 10,      // 暴击率
    CRIT_DMG = 11,       // 暴击伤害倍率
    ACCURACY = 12,       // 命中率
    EVASION = 13,        // 闪避率
    BLOCK_RATE = 14,     // 格挡率
    BLOCK_VALUE = 15,    // 格挡值
    
    // ===== 抗性属性 =====
    PHYSICAL_RES = 20,   // 物理抗性
    MAGICAL_RES = 21,    // 魔法抗性
    
    // ===== 元素属性（可扩展）=====
    ELEMENT_FIRE = 30,   // 火元素强度
    ELEMENT_ICE = 31,    // 冰元素强度
    ELEMENT_LIGHTNING = 32, // 雷元素强度
    ELEMENT_DARK = 33,   // 暗元素强度
    
    // ===== 资源属性 =====
    MAX_MANA = 40,       // 法力值上限
    MANA_REGEN = 41,     // 法力恢复速度
    HEALTH_REGEN = 42,   // 生命恢复速度
    
    // ===== 其他可扩展属性 =====
    ATTACK_SPEED = 50,   // 攻击速度
    CAST_SPEED = 51,     // 施法速度
    COOLDOWN_REDUCTION = 52, // 冷却缩减
    LIFESTEAL = 53,      // 生命偷取
    EXP_GAIN = 54,       // 经验获取加成
}
```

#### Stats 通用属性容器

```csharp
using TrueSync;

/// <summary>
/// 通用属性容器 - 使用字典存储任意属性（定点数）
/// </summary>
[MemoryPackable]
public partial class Stats
{
    /// <summary>属性值字典（使用定点数确保确定性）</summary>
    private Dictionary<StatType, FP> _values = new Dictionary<StatType, FP>();
    
    /// <summary>获取属性值</summary>
    public FP Get(StatType type)
    {
        return _values.TryGetValue(type, out var value) ? value : FP.Zero;
    }
    
    /// <summary>设置属性值</summary>
    public void Set(StatType type, FP value)
    {
        _values[type] = value;
    }
    
    /// <summary>增加属性值</summary>
    public void Add(StatType type, FP delta)
    {
        if (_values.TryGetValue(type, out var current))
            _values[type] = current + delta;
        else
            _values[type] = delta;
    }
    
    /// <summary>清空所有属性</summary>
    public void Clear()
    {
        _values.Clear();
    }
    
    /// <summary>复制属性</summary>
    public Stats Clone()
    {
        var clone = new Stats();
        foreach (var kvp in _values)
        {
            clone._values[kvp.Key] = kvp.Value;
        }
        return clone;
    }
    
    /// <summary>获取所有属性（调试用）</summary>
    public Dictionary<StatType, FP> GetAll()
    {
        return new Dictionary<StatType, FP>(_values);
    }
    
}
```

**设计说明**：
- ✅ 使用 `TrueSync.FP`（定点数）保证确定性
- ✅ 配置表使用int存储，小数属性扩大1000倍
- ✅ 读取时一次性转换为FP
- ✅ 运行时全部使用FP计算
- ✅ UI显示时转换：`(float)stats.Get(type)`
- ✅ 支持帧同步和状态回滚

---

### 2.2 BaseStatsComponent（基础属性组件）

**职责**：存储实体的基础原始属性，来源于配置表

**数据来源**：
- 配置表的基础值（RoleBaseTable）
- 等级成长加成（RoleGrowthTable）
- **不包含**：Buff、装备等临时加成

**组件设计**：

```csharp
[MemoryPackable]
public partial class BaseStatsComponent : BaseComponent
{
    /// <summary>基础属性容器</summary>
    public Stats BaseStats { get; set; } = new Stats();
    
    /// <summary>从配置表初始化</summary>
    public void InitializeFromConfig(int roleId, int level)
    {
        // 1. 清空现有属性
        BaseStats.Clear();
        
        // 2. 从配置表读取基础值
        var roleConfig = ConfigManager.Instance.Tables.TbRoleBaseTable.Get(roleId);
        if (roleConfig == null)
        {
            ASLogger.Instance.Error($"[BaseStats] RoleBaseTable not found for roleId={roleId}");
            return;
        }
        
        // 3. 设置基础四维（配置表存int，直接转FP）
        BaseStats.Set(StatType.ATK, (FP)roleConfig.BaseAttack);
        BaseStats.Set(StatType.DEF, (FP)roleConfig.BaseDefense);
        BaseStats.Set(StatType.HP, (FP)roleConfig.BaseHealth);
        
        // 速度是小数，配置表存1000倍（如10.5存为10500）
        BaseStats.Set(StatType.SPD, (FP)roleConfig.BaseSpeed / (FP)1000);
        
        // 4. 设置高级属性（百分比属性，配置表存1000倍）
        // 如暴击率5%，配置表存50，运行时除以1000得到0.05
        BaseStats.Set(StatType.CRIT_RATE, (FP)roleConfig.BaseCritRate / (FP)1000);
        BaseStats.Set(StatType.CRIT_DMG, (FP)roleConfig.BaseCritDamage / (FP)1000);
        BaseStats.Set(StatType.ACCURACY, (FP)roleConfig.BaseAccuracy / (FP)1000);
        BaseStats.Set(StatType.EVASION, (FP)roleConfig.BaseEvasion / (FP)1000);
        BaseStats.Set(StatType.BLOCK_RATE, (FP)roleConfig.BaseBlockRate / (FP)1000);
        BaseStats.Set(StatType.BLOCK_VALUE, (FP)roleConfig.BaseBlockValue);
        
        // 5. 设置抗性（百分比，配置表存1000倍）
        BaseStats.Set(StatType.PHYSICAL_RES, (FP)roleConfig.PhysicalRes / (FP)1000);
        BaseStats.Set(StatType.MAGICAL_RES, (FP)roleConfig.MagicalRes / (FP)1000);
        
        // 6. 设置资源属性
        BaseStats.Set(StatType.MAX_MANA, (FP)roleConfig.BaseMaxMana);
        BaseStats.Set(StatType.MANA_REGEN, (FP)roleConfig.ManaRegen / (FP)1000);
        BaseStats.Set(StatType.HEALTH_REGEN, (FP)roleConfig.HealthRegen / (FP)1000);
        
        // 7. 应用等级成长
        if (level > 1)
        {
            ApplyLevelGrowth(roleId, level);
        }
    }
    
    /// <summary>应用等级成长</summary>
    private void ApplyLevelGrowth(int roleId, int level)
    {
        var growthConfig = ConfigManager.Instance.Tables.TbRoleGrowthTable.GetByLevel(roleId, level);
        if (growthConfig == null) return;
        
        FP levelDelta = (FP)(level - 1);
        
        // 配置表存int，需要转换
        BaseStats.Add(StatType.ATK, (FP)growthConfig.AttackBonus * levelDelta);
        BaseStats.Add(StatType.DEF, (FP)growthConfig.DefenseBonus * levelDelta);
        BaseStats.Add(StatType.HP, (FP)growthConfig.HealthBonus * levelDelta);
        
        // 小数属性（配置表存1000倍）
        BaseStats.Add(StatType.SPD, (FP)growthConfig.SpeedBonus / (FP)1000 * levelDelta);
        BaseStats.Add(StatType.CRIT_RATE, (FP)growthConfig.CritRateBonus / (FP)1000 * levelDelta);
        BaseStats.Add(StatType.CRIT_DMG, (FP)growthConfig.CritDamageBonus / (FP)1000 * levelDelta);
    }
    
    /// <summary>应用自由加点</summary>
    public void ApplyAllocatedPoints(GrowthComponent growthComp)
    {
        // 每点加成（使用定点数）
        FP ATTACK_PER_POINT = (FP)2;
        FP DEFENSE_PER_POINT = (FP)2;
        FP HEALTH_PER_POINT = (FP)20;
        FP SPEED_PER_POINT = (FP)0.1;
        
        BaseStats.Add(StatType.ATK, (FP)growthComp.AllocatedAttackPoints * ATTACK_PER_POINT);
        BaseStats.Add(StatType.DEF, (FP)growthComp.AllocatedDefensePoints * DEFENSE_PER_POINT);
        BaseStats.Add(StatType.HP, (FP)growthComp.AllocatedHealthPoints * HEALTH_PER_POINT);
        BaseStats.Add(StatType.SPD, (FP)growthComp.AllocatedSpeedPoints * SPEED_PER_POINT);
    }
}
```

---

### 2.3 DerivedStatsComponent（派生属性组件）

**职责**：存储经过修饰器计算后的最终属性值

**数据来源**：
- BaseStats（基础）
- Buff加成（临时）
- 装备加成（装备）
- 技能被动（永久）

**组件设计**：

```csharp
[MemoryPackable]
public partial class DerivedStatsComponent : BaseComponent
{
    /// <summary>最终属性容器</summary>
    public Stats FinalStats { get; set; } = new Stats();
    
    /// <summary>修饰器字典（属性类型 → 修饰器列表）</summary>
    [MemoryPackIgnore]
    private Dictionary<StatType, List<StatModifier>> _modifiers = new Dictionary<StatType, List<StatModifier>>();
    
    /// <summary>添加修饰器</summary>
    public void AddModifier(StatType statType, StatModifier modifier)
    {
        if (!_modifiers.ContainsKey(statType))
        {
            _modifiers[statType] = new List<StatModifier>();
        }
        
        _modifiers[statType].Add(modifier);
        _modifiers[statType].Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }
    
    /// <summary>移除修饰器（按来源ID）</summary>
    public void RemoveModifier(int sourceId)
    {
        foreach (var modList in _modifiers.Values)
        {
            modList.RemoveAll(m => m.SourceId == sourceId);
        }
    }
    
    /// <summary>清空所有修饰器</summary>
    public void ClearModifiers()
    {
        _modifiers.Clear();
    }
    
    /// <summary>重新计算所有派生属性</summary>
    public void RecalculateAll(BaseStatsComponent baseStats)
    {
        FinalStats.Clear();
        
        // 遍历所有可能的属性类型
        foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
        {
            float baseValue = baseStats.BaseStats.Get(statType);
            float finalValue = CalculateFinalStat(baseValue, statType);
            FinalStats.Set(statType, finalValue);
        }
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
}

/// <summary>属性修饰器</summary>
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

/// <summary>修饰器类型</summary>
public enum ModifierType
{
    Flat = 1,           // 固定值加成（+50攻击）
    Percent = 2,        // 百分比加成（+20%攻击）
    FinalMultiplier = 3 // 最终乘数（×1.5伤害）
}
```

---

### 2.4 DynamicStatsComponent（动态属性组件）

**职责**：存储战斗中实时变化的数值（当前值、临时资源等）

**特点**：
- 每帧可能变化
- 受伤害、治疗、消耗影响
- 有上限约束（来自DerivedStats）

**动态资源类型枚举**：

```csharp
/// <summary>
/// 动态资源类型
/// </summary>
public enum DynamicResourceType
{
    // ===== 核心资源 =====
    CURRENT_HP = 1,      // 当前生命值
    CURRENT_MANA = 2,    // 当前法力值
    
    // ===== 战斗资源 =====
    ENERGY = 10,         // 能量（0-100）
    RAGE = 11,           // 怒气（0-100）
    COMBO = 12,          // 连击数
    
    // ===== 临时防护 =====
    SHIELD = 20,         // 护盾值
    INVINCIBLE_FRAMES = 21, // 无敌帧数
    
    // ===== 控制状态计时 =====
    STUN_FRAMES = 30,    // 硬直剩余帧数
    FREEZE_FRAMES = 31,  // 冰冻剩余帧数
    KNOCKBACK_FRAMES = 32, // 击飞剩余帧数
}
```

**组件设计**：

```csharp
using TrueSync;

[MemoryPackable]
public partial class DynamicStatsComponent : BaseComponent
{
    /// <summary>动态资源容器（使用定点数）</summary>
    private Dictionary<DynamicResourceType, FP> _resources = new Dictionary<DynamicResourceType, FP>();
    
    /// <summary>获取资源值</summary>
    public FP Get(DynamicResourceType type)
    {
        return _resources.TryGetValue(type, out var value) ? value : FP.Zero;
    }
    
    /// <summary>设置资源值</summary>
    public void Set(DynamicResourceType type, FP value)
    {
        _resources[type] = value;
    }
    
    /// <summary>增加资源值</summary>
    public void Add(DynamicResourceType type, FP delta)
    {
        if (_resources.TryGetValue(type, out var current))
            _resources[type] = current + delta;
        else
            _resources[type] = delta;
    }
    
    // ===== 便捷访问器 =====
    
    public FP CurrentHealth
    {
        get => Get(DynamicResourceType.CURRENT_HP);
        set => Set(DynamicResourceType.CURRENT_HP, value);
    }
    
    public FP CurrentMana
    {
        get => Get(DynamicResourceType.CURRENT_MANA);
        set => Set(DynamicResourceType.CURRENT_MANA, value);
    }
    
    public FP Energy
    {
        get => Get(DynamicResourceType.ENERGY);
        set => Set(DynamicResourceType.ENERGY, FPMath.Clamp(value, FP.Zero, (FP)100));
    }
    
    public FP Rage
    {
        get => Get(DynamicResourceType.RAGE);
        set => Set(DynamicResourceType.RAGE, FPMath.Clamp(value, FP.Zero, (FP)100));
    }
    
    public FP Shield
    {
        get => Get(DynamicResourceType.SHIELD);
        set => Set(DynamicResourceType.SHIELD, FPMath.Max(FP.Zero, value));
    }
    
    public int ComboCount
    {
        get => (int)Get(DynamicResourceType.COMBO);
        set => Set(DynamicResourceType.COMBO, (FP)value);
    }
    
    // ===== 辅助方法 =====
    
    /// <summary>扣除生命值（考虑护盾）</summary>
    public FP TakeDamage(FP damage, DerivedStatsComponent derivedStats)
    {
        FP remainingDamage = damage;
        
        // 1. 先扣除护盾
        FP currentShield = Shield;
        if (currentShield > FP.Zero)
        {
            FP shieldDamage = FPMath.Min(currentShield, remainingDamage);
            Shield = currentShield - shieldDamage;
            remainingDamage -= shieldDamage;
        }
        
        // 2. 扣除生命值
        if (remainingDamage > FP.Zero)
        {
            FP currentHP = CurrentHealth;
            FP actualDamage = FPMath.Min(currentHP, remainingDamage);
            CurrentHealth = currentHP - actualDamage;
            return actualDamage; // 返回实际受到的生命伤害
        }
        
        return FP.Zero;
    }
    
    /// <summary>恢复生命值（不超过上限）</summary>
    public FP Heal(FP amount, DerivedStatsComponent derivedStats)
    {
        FP maxHP = derivedStats.Get(StatType.HP);
        FP currentHP = CurrentHealth;
        FP maxHeal = maxHP - currentHP;
        FP actualHeal = FPMath.Min(amount, maxHeal);
        CurrentHealth = currentHP + actualHeal;
        return actualHeal;
    }
    
    /// <summary>消耗法力</summary>
    public bool ConsumeMana(FP amount)
    {
        FP currentMP = CurrentMana;
        if (currentMP < amount)
            return false;
        
        CurrentMana = currentMP - amount;
        return true;
    }
    
    /// <summary>增加能量</summary>
    public void AddEnergy(FP amount)
    {
        Energy = FPMath.Min((FP)100, Energy + amount);
    }
    
    /// <summary>增加怒气</summary>
    public void AddRage(FP amount)
    {
        Rage = FPMath.Min((FP)100, Rage + amount);
    }
    
    /// <summary>初始化动态资源（满血满蓝）</summary>
    public void InitializeResources(DerivedStatsComponent derivedStats)
    {
        CurrentHealth = derivedStats.Get(StatType.HP);
        CurrentMana = derivedStats.Get(StatType.MAX_MANA);
        Energy = FP.Zero;
        Rage = FP.Zero;
        Shield = FP.Zero;
        ComboCount = 0;
    }
}
```

**确定性说明**：
- ✅ 所有数值使用 `FP`（定点数）存储
- ✅ 数学运算使用 `FPMath` 工具类
- ✅ 保证帧同步时每个客户端计算结果完全一致
- ✅ 避免float的精度误差和不确定性
```

**设计优势**：
- ✅ 灵活扩展：添加新资源类型只需扩展枚举
- ✅ 紧凑存储：只存储非零值
- ✅ 类型安全：通过枚举访问
- ✅ 调试友好：可遍历所有资源

---

### 2.5 BuffComponent（Buff组件）

**职责**：管理实体身上的所有Buff/Debuff实例

**功能**：
- Buff添加、移除、叠加
- Buff持续时间管理
- Buff效果计算和应用

**数据结构**：

```csharp
[MemoryPackable]
public partial class BuffComponent : BaseComponent
{
    /// <summary>当前所有Buff实例</summary>
    public List<BuffInstance> Buffs { get; set; } = new List<BuffInstance>();
    
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
            // 格式："ATK:Percent:0.2;SPD:Flat:1.0"
            var modifiers = ParseBuffModifiers(buffConfig.Modifiers, buff);
            
            foreach (var mod in modifiers)
            {
                if (!modifiersByType.ContainsKey(mod.StatType))
                {
                    modifiersByType[mod.StatType] = new List<StatModifier>();
                }
                modifiersByType[mod.StatType].Add(mod);
            }
        }
        
        return modifiersByType;
    }
    
    /// <summary>解析Buff修饰器字符串</summary>
    private List<(StatType StatType, StatModifier Modifier)> ParseBuffModifiers(string modifierStr, BuffInstance buff)
    {
        var result = new List<(StatType, StatModifier)>();
        if (string.IsNullOrEmpty(modifierStr)) return result;
        
        // 格式："ATK:Percent:200;SPD:Flat:1000;CRIT_RATE:Flat:50"
        // 数值全部是int，Percent和小数需要除以1000
        var parts = modifierStr.Split(';');
        
        foreach (var part in parts)
        {
            var tokens = part.Split(':');
            if (tokens.Length != 3) continue;
            
            // 解析属性类型
            if (!System.Enum.TryParse<StatType>(tokens[0], out var statType))
                continue;
            
            // 解析修饰器类型
            if (!System.Enum.TryParse<ModifierType>(tokens[1], out var modType))
                continue;
            
            // 解析数值（配置表存int）
            if (!int.TryParse(tokens[2], out var intValue))
                continue;
            
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
```

**修饰器字符串格式**：
```
格式：属性:类型:数值;属性:类型:数值;...
注意：数值全部为int

示例：
"ATK:Percent:200;SPD:Flat:1000;CRIT_RATE:Flat:50"
解析为：
  - ATK +20% (Percent, 200/1000=0.2)
  - SPD +1.0 (Flat, 1000/1000=1.0)
  - CRIT_RATE +5% (Flat, 50/1000=0.05)

规则：
  - Percent类型：总是除以1000
  - Flat类型：根据属性类型判断
    - 整数属性（ATK/DEF/HP等）：直接使用
    - 小数属性（SPD/CRIT_RATE等）：除以1000
```

**Buff实例数据结构**：

```csharp
/// <summary>Buff实例</summary>
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
```

---

### 2.6 StateComponent（状态组件）

**职责**：存储实体的各种状态标志

**状态列表**：

```csharp
[MemoryPackable]
public partial class StateComponent : BaseComponent
{
    // ===== 控制状态 =====
    
    /// <summary>是否被晕眩</summary>
    public bool IsStunned { get; set; } = false;
    
    /// <summary>是否被冰冻</summary>
    public bool IsFrozen { get; set; } = false;
    
    /// <summary>是否被击飞</summary>
    public bool IsKnockedBack { get; set; } = false;
    
    /// <summary>是否被沉默（无法释放技能）</summary>
    public bool IsSilenced { get; set; } = false;
    
    /// <summary>是否被缴械（无法普通攻击）</summary>
    public bool IsDisarmed { get; set; } = false;
    
    // ===== 特殊状态 =====
    
    /// <summary>是否无敌</summary>
    public bool IsInvincible { get; set; } = false;
    
    /// <summary>是否隐身</summary>
    public bool IsInvisible { get; set; } = false;
    
    /// <summary>是否在格挡状态</summary>
    public bool IsBlocking { get; set; } = false;
    
    /// <summary>是否在冲刺状态</summary>
    public bool IsDashing { get; set; } = false;
    
    /// <summary>是否在施法状态</summary>
    public bool IsCasting { get; set; } = false;
    
    // ===== 死亡状态 =====
    
    /// <summary>是否已死亡</summary>
    public bool IsDead { get; set; } = false;
    
    // ===== 辅助方法 =====
    
    /// <summary>是否可以移动</summary>
    public bool CanMove()
    {
        return !IsStunned && !IsFrozen && !IsKnockedBack && !IsDead;
    }
    
    /// <summary>是否可以攻击</summary>
    public bool CanAttack()
    {
        return !IsStunned && !IsFrozen && !IsDisarmed && !IsDead;
    }
    
    /// <summary>是否可以释放技能</summary>
    public bool CanCastSkill()
    {
        return !IsStunned && !IsFrozen && !IsSilenced && !IsDead;
    }
    
    /// <summary>是否可以受到伤害</summary>
    public bool CanTakeDamage()
    {
        return !IsInvincible && !IsDead;
    }
}
```

---

### 2.7 LevelComponent（等级组件）

**职责**：管理实体的等级和经验

```csharp
[MemoryPackable]
public partial class LevelComponent : BaseComponent
{
    /// <summary>当前等级</summary>
    public int CurrentLevel { get; set; } = 1;
    
    /// <summary>当前经验值</summary>
    public int CurrentExp { get; set; } = 0;
    
    /// <summary>升到下一级所需经验</summary>
    public int ExpToNextLevel { get; set; } = 1000;
    
    /// <summary>最大等级</summary>
    public int MaxLevel { get; set; } = 100;
    
    /// <summary>获得经验</summary>
    public bool GainExp(int amount, Entity owner)
    {
        if (CurrentLevel >= MaxLevel)
            return false;
        
        CurrentExp += amount;
        
        // 检查是否升级
        if (CurrentExp >= ExpToNextLevel)
        {
            LevelUp(owner);
            return true;
        }
        
        return false;
    }
    
    /// <summary>升级</summary>
    private void LevelUp(Entity owner)
    {
        CurrentLevel++;
        CurrentExp -= ExpToNextLevel;
        
        // 1. 更新下一级经验需求
        var roleInfo = owner.GetComponent<RoleInfoComponent>();
        var growthConfig = ConfigManager.Instance.Tables.TbRoleGrowthTable.GetByLevel(roleInfo.RoleId, CurrentLevel + 1);
        ExpToNextLevel = growthConfig?.RequiredExp ?? ExpToNextLevel;
        
        // 2. 更新基础属性
        var baseStats = owner.GetComponent<BaseStatsComponent>();
        baseStats.InitializeFromConfig(roleInfo.RoleId, CurrentLevel);
        
        // 3. 重新计算派生属性
        var derivedStats = owner.GetComponent<DerivedStatsComponent>();
        derivedStats.RecalculateAll(baseStats);
        
        // 4. 满血满蓝
        var dynamicStats = owner.GetComponent<DynamicStatsComponent>();
        dynamicStats.CurrentHealth = derivedStats.FinalMaxHealth;
        dynamicStats.CurrentMana = derivedStats.FinalMaxMana;
        
        ASLogger.Instance.Info($"[Level] Entity {owner.UniqueId} leveled up to {CurrentLevel}!");
    }
}
```

---

### 2.8 GrowthComponent（成长组件）

**职责**：记录成长曲线相关数据和加点分配

```csharp
[MemoryPackable]
public partial class GrowthComponent : BaseComponent
{
    /// <summary>角色ID（关联成长表）</summary>
    public int RoleId { get; set; }
    
    /// <summary>可分配的属性点</summary>
    public int AvailableStatPoints { get; set; } = 0;
    
    /// <summary>已分配的攻击点</summary>
    public int AllocatedAttackPoints { get; set; } = 0;
    
    /// <summary>已分配的防御点</summary>
    public int AllocatedDefensePoints { get; set; } = 0;
    
    /// <summary>已分配的生命点</summary>
    public int AllocatedHealthPoints { get; set; } = 0;
    
    /// <summary>已分配的速度点</summary>
    public int AllocatedSpeedPoints { get; set; } = 0;
    
    /// <summary>分配属性点</summary>
    public bool AllocatePoint(StatType statType, Entity owner)
    {
        if (AvailableStatPoints <= 0)
            return false;
        
        // 1. 扣除可用点数
        AvailableStatPoints--;
        
        // 2. 增加对应属性的分配点和基础属性
        var baseStats = owner.GetComponent<BaseStatsComponent>();
        
        switch (statType)
        {
            case StatType.ATK:
                AllocatedAttackPoints++;
                baseStats.BaseStats.Add(StatType.ATK, 2f); // 每点+2攻击
                break;
            case StatType.DEF:
                AllocatedDefensePoints++;
                baseStats.BaseStats.Add(StatType.DEF, 2f);
                break;
            case StatType.HP:
                AllocatedHealthPoints++;
                baseStats.BaseStats.Add(StatType.HP, 20f); // 每点+20生命
                break;
            case StatType.SPD:
                AllocatedSpeedPoints++;
                baseStats.BaseStats.Add(StatType.SPD, 0.1f);
                break;
            default:
                // 不支持的属性类型
                AvailableStatPoints++; // 返还点数
                return false;
        }
        
        // 3. 重新计算派生属性
        var derivedStats = owner.GetComponent<DerivedStatsComponent>();
        derivedStats.RecalculateAll(baseStats);
        
        return true;
    }
}
```

---

## 三、静态配置表设计

### 3.1 现有配置表评估

#### ✅ RoleBaseTable（角色基础表）
**当前字段**：
```csv
id, name, description, roleType,
baseAttack, baseDefense, baseHealth, baseSpeed,
attackGrowth, defenseGrowth, healthGrowth, speedGrowth,
lightAttackSkillId, heavyAttackSkillId, skill1Id, skill2Id
```

**评估**：
- ✅ 基础四维足够（攻防血速）
- ✅ 成长值已定义
- ❌ **缺少高级属性**（暴击率、暴击伤害、命中、闪避等）

**建议扩展**：
```csv
baseCritRate,      # 基础暴击率（0.05表示5%）
baseCritDamage,    # 基础暴击伤害（2.0表示200%）
baseAccuracy,      # 基础命中率（0.95表示95%）
baseEvasion,       # 基础闪避率（0.05表示5%）
baseBlockRate,     # 基础格挡率（0.10表示10%）
baseBlockValue,    # 基础格挡值（50）
physicalRes,       # 物理抗性（0.1表示减免10%）
magicalRes,        # 魔法抗性
baseMaxMana,       # 基础法力上限
manaRegen,         # 法力回复速度
healthRegen        # 生命回复速度
```

#### ✅ RoleGrowthTable（角色成长表）
**当前字段**：
```csv
id, roleId, level, requiredExp,
lightAttackBonus, heavyAttackBonus, defenseBonus, healthBonus, speedBonus,
unlockSkillId, skillPoint
```

**评估**：
- ✅ 基础成长已定义
- ✅ 经验需求已定义
- ❌ **lightAttackBonus/heavyAttackBonus 语义不清**，应该统一为攻击力加成

**建议优化**：
```csv
id, roleId, level, requiredExp,
attackBonus,       # 每级攻击力加成（统一）
defenseBonus,      # 每级防御力加成
healthBonus,       # 每级生命值加成
speedBonus,        # 每级速度加成
critRateBonus,     # 每级暴击率加成（0.005表示0.5%）
critDamageBonus,   # 每级暴击伤害加成
unlockSkillId,     # 解锁的技能ID
skillPoint         # 获得的技能点
```

#### ✅ SkillEffectTable（技能效果表）
**当前字段**：
```csv
skillEffectId, effectType, effectValue, targetType,
effectDuration, effectRange, castTime, effectParams,
visualEffectId, soundEffectId
```

**评估**：
- ✅ 效果类型已定义（1=伤害, 2=治疗, 3=击退）
- ✅ effectValue字段可用（当前是百分比，如150.0）
- ⚠️ **effectValue语义不明确**（是百分比还是固定值？）

**建议明确**：
```csv
effectValue,       # 效果数值（伤害效果时=攻击力百分比，治疗效果时=固定值）
damageType,        # 伤害类型（1=物理, 2=魔法, 3=真实）仅伤害效果有效
scalingStat,       # 缩放属性（1=攻击力, 2=防御力, 3=生命上限）
scalingRatio       # 缩放比例（1.5表示150%）
```

### 3.2 新增配置表

#### 📝 BuffTable（Buff配置表）

**用途**：定义所有Buff/Debuff的效果和数值

```csv
##var,buffId,buffName,buffType,duration,stackable,maxStack,modifiers,tickDamage,tickInterval
##type,int,string,int,int,bool,int,string,int,int
##desc,BuffID,Buff名称,类型(1=Buff/2=Debuff),持续帧数,可叠加,最大层数,属性修饰器,持续伤害*1000,触发间隔帧数

示例数据：
5001,力量祝福,1,600,true,3,ATK:Percent:200;SPD:Flat:1000,0,0
5002,燃烧,2,300,false,1,PHYSICAL_RES:Percent:-100,10000,30
5003,护盾,1,180,false,1,无,0,0
5004,冰冻,2,120,false,1,SPD:Percent:-500,0,0

# 解释：
# 5001 - 力量祝福：ATK +20%（200/1000），SPD +1.0（1000/1000）
# 5002 - 燃烧：物理抗性-10%（-100/1000），每30帧造成10.0伤害（10000/1000）
# 5003 - 护盾：特殊处理，护盾值在effectParams中定义
# 5004 - 冰冻：速度-50%（-500/1000）
```

**字段说明**：
- `buffType`: 1=增益Buff, 2=减益Debuff
- `duration`: 持续帧数（300帧 = 15秒 @ 20FPS）
- `modifiers`: 格式 `属性:类型:数值;属性:类型:数值`（**数值为int**）
  - 例：`ATK:Percent:200` = 攻击力+20%（200/1000=0.2）
  - 例：`DEF:Flat:50` = 防御力+50（直接）
  - 例：`SPD:Flat:1000` = 速度+1.0（1000/1000）
- `tickDamage`: 持续伤害*1000（如10.0伤害存为10000）
- `tickInterval`: 伤害间隔帧数

#### 📝 AttributeTable（属性配置表）（可选）

如果需要更灵活的属性定义：

```csv
##var,attrId,attrName,minValue,maxValue,defaultValue,description
##type,int,string,float,float,float,string
##desc,属性ID,属性名称,最小值,最大值,默认值,描述

1,Attack,攻击力,0,9999,100,影响物理和魔法伤害
2,Defense,防御力,0,9999,50,减少受到的物理伤害
3,MaxHealth,生命上限,1,99999,1000,角色最大生命值
4,Speed,移动速度,0,20,10,影响移动和攻击速度
5,CritRate,暴击率,0,1,0.05,攻击暴击概率
6,CritDamage,暴击伤害,1,10,2.0,暴击时的伤害倍率
7,Accuracy,命中率,0,1,0.95,攻击命中概率
8,Evasion,闪避率,0,1,0.05,闪避攻击概率
```

---

## 四、属性计算流程

### 4.1 完整计算流程

```
游戏启动/角色创建
    ↓
[1] 初始化 BaseStatsComponent
    - 从 RoleBaseTable 读取基础值
    - 从 RoleGrowthTable 读取等级加成
    - 从 GrowthComponent 读取加点分配
    ↓
[2] 初始化 DerivedStatsComponent
    - 收集 BuffComponent 的修饰器
    - 收集装备的修饰器（如有）
    - 按公式计算最终属性
    ↓
[3] 初始化 DynamicStatsComponent
    - CurrentHealth = DerivedStats.FinalMaxHealth
    - CurrentMana = DerivedStats.FinalMaxMana
    - 其他资源初始化为0或默认值
    ↓
游戏运行中...
    ↓
[4] Buff变化时
    - BuffComponent.AddBuff() / RemoveBuff()
    - 触发 DerivedStats.RecalculateAll()
    - 更新最终属性
    ↓
[5] 升级时
    - LevelComponent.LevelUp()
    - 更新 BaseStats（读取新等级的配置）
    - 触发 DerivedStats.RecalculateAll()
    - 恢复 DynamicStats（满血满蓝）
    ↓
[6] 受到伤害时
    - DamageCalculator.Calculate() 使用 DerivedStats
    - DynamicStats.TakeDamage()
    - 扣除 CurrentHealth
```

### 4.2 属性计算公式

#### 最终属性计算
```
最终属性 = (基础属性 + 固定加成) × (1 + 百分比加成) × 最终乘数

示例：
基础攻击 = 100
Buff1: +20攻击（Flat）
Buff2: +30%攻击（Percent）
Buff3: ×1.5伤害（FinalMultiplier）

最终攻击 = (100 + 20) × (1 + 0.3) × 1.5 = 234
```

#### 修饰器优先级
```
1. Flat（固定加成）     - 优先级 100
2. Percent（百分比加成） - 优先级 200
3. FinalMultiplier（最终乘数） - 优先级 300
```

---

## 五、伤害计算公式

### 5.1 完整伤害计算流程

```csharp
public static DamageResult Calculate(Entity caster, Entity target, SkillEffectTable effectConfig)
{
    // 1. 获取施法者和目标的派生属性
    var casterDerived = caster.GetComponent<DerivedStatsComponent>();
    var targetDerived = target.GetComponent<DerivedStatsComponent>();
    var targetState = target.GetComponent<StateComponent>();
    
    // 2. 检查是否可以受伤
    if (!targetState.CanTakeDamage())
        return new DamageResult { FinalDamage = 0, IsCritical = false };
    
    // 3. 获取属性值（定点数）
    FP casterAttack = casterDerived.Get(StatType.ATK);
    FP casterAccuracy = casterDerived.Get(StatType.ACCURACY);
    FP casterCritRate = casterDerived.Get(StatType.CRIT_RATE);
    FP casterCritDamage = casterDerived.Get(StatType.CRIT_DMG);
    
    FP targetDefense = targetDerived.Get(StatType.DEF);
    FP targetEvasion = targetDerived.Get(StatType.EVASION);
    FP targetBlockRate = targetDerived.Get(StatType.BLOCK_RATE);
    FP targetBlockValue = targetDerived.Get(StatType.BLOCK_VALUE);
    
    // 4. 计算基础伤害
    FP baseDamage = CalculateBaseDamage(casterAttack, effectConfig);
    
    // 5. 命中判定
    if (!CheckHit(casterAccuracy, targetEvasion))
        return new DamageResult { FinalDamage = FP.Zero, IsCritical = false, IsMiss = true };
    
    // 6. 格挡判定
    bool isBlocked = CheckBlock(targetBlockRate);
    if (isBlocked)
    {
        baseDamage = FPMath.Max(FP.Zero, baseDamage - targetBlockValue);
    }
    
    // 7. 暴击判定
    bool isCritical = CheckCritical(casterCritRate);
    if (isCritical)
    {
        baseDamage *= casterCritDamage;
    }
    
    // 8. 应用防御减免
    FP afterDefense = ApplyDefense(baseDamage, targetDefense, effectConfig.DamageType);
    
    // 9. 应用抗性
    FP afterResistance = ApplyResistance(afterDefense, targetDerived, effectConfig.DamageType);
    
    // 10. 应用随机浮动（注意：使用确定性随机）
    FP finalDamage = ApplyDeterministicVariance(afterResistance, randomSeed);
    
    // 11. 确保非负
    finalDamage = FPMath.Max(FP.Zero, finalDamage);
    
    return new DamageResult
    {
        FinalDamage = finalDamage,
        IsCritical = isCritical,
        IsBlocked = isBlocked,
        DamageType = effectConfig.DamageType
    };
}
```

### 5.2 基础伤害计算

```csharp
private static FP CalculateBaseDamage(FP attack, SkillEffectTable effectConfig)
{
    // effectValue 通常是百分比（如150表示150%攻击力）
    FP ratio = (FP)effectConfig.EffectValue / (FP)100;
    return attack * ratio;
}
```

示例：
```
攻击力 = FP(100)
技能倍率 = FP(1.5)（配置表中 effectValue = 150）
基础伤害 = FP(100) × FP(1.5) = FP(150)
```

### 5.3 防御减免公式

#### 物理/魔法伤害

```csharp
private static FP ApplyDefense(FP baseDamage, FP defense, DamageType damageType)
{
    // 真实伤害无视防御
    if (damageType == DamageType.True)
        return baseDamage;
    
    // 减伤公式：减伤百分比 = 防御 / (防御 + 100)
    FP damageReduction = defense / (defense + (FP)100);
    return baseDamage * (FP.One - damageReduction);
}
```

示例：
```
基础伤害 = FP(150)
防御力 = FP(50)
减伤 = FP(50) / (FP(50) + FP(100)) = FP(0.333) (33.3%)
最终伤害 = FP(150) × (FP.One - FP(0.333)) = FP(100)
```

**防御收益曲线**：
- 防御 25 → 约20%减伤
- 防御 50 → 约33%减伤
- 防御 100 → 约50%减伤
- 防御 200 → 约67%减伤
- 防御 400 → 约80%减伤

#### 真实伤害
```
真实伤害 = 基础伤害（无视防御）
```

### 5.4 抗性减免

```csharp
private static FP ApplyResistance(FP damage, DerivedStatsComponent targetDerived, DamageType damageType)
{
    FP resistance = FP.Zero;
    
    switch (damageType)
    {
        case DamageType.Physical:
            resistance = targetDerived.Get(StatType.PHYSICAL_RES);
            break;
        case DamageType.Magical:
            resistance = targetDerived.Get(StatType.MAGICAL_RES);
            break;
        case DamageType.True:
            return damage; // 真实伤害无视抗性
    }
    
    return damage * (FP.One - resistance);
}
```

示例：
```
伤害 = FP(100)
物理抗性 = FP(0.2)（20%）
最终伤害 = FP(100) × (FP.One - FP(0.2)) = FP(80)
```

**抗性来源**：
- Buff/Debuff
- 装备
- 被动技能
- 种族特性

### 5.5 暴击计算

```csharp
private static bool CheckCritical(FP critRate, int randomSeed)
{
    // 使用确定性随机（基于种子）
    TSRandom random = new TSRandom(randomSeed);
    FP roll = random.NextFP(); // 返回 [0, 1) 的定点数
    return roll < critRate;
}
```

示例：
```
基础伤害 = FP(150)
暴击率 = FP(0.25)（25%）
暴击伤害倍率 = FP(2.5)（250%）
随机值 = FP(0.18)（基于种子确定性生成）

→ 0.18 < 0.25 → 暴击成功！
→ 暴击伤害 = FP(150) × FP(2.5) = FP(375)
```

**确定性保证**：
- ✅ 使用 `TSRandom`（TrueSync的确定性随机）
- ✅ 基于统一的种子（如帧号+实体ID）
- ✅ 所有客户端生成相同的随机数序列

### 5.6 命中/闪避判定

```csharp
private static bool CheckHit(FP accuracy, FP evasion, int randomSeed)
{
    // 最终命中概率 = 命中率 - 闪避率
    FP hitChance = accuracy - evasion;
    
    // 极限约束
    hitChance = FPMath.Clamp(hitChance, (FP)0.1, FP.One); // [10%, 100%]
    
    TSRandom random = new TSRandom(randomSeed);
    FP roll = random.NextFP();
    return roll < hitChance;
}
```

示例：
```
施法者命中率 = FP(0.95)（95%）
目标闪避率 = FP(0.15)（15%）
最终命中率 = FP(0.95) - FP(0.15) = FP(0.80)（80%）
随机值 = FP(0.65)（确定性生成）

→ 0.65 < 0.80 → 命中成功！
```

**极限约束**：
- 最低命中率：10%（即使目标闪避100%）
- 最高命中率：100%（无法超过）

### 5.7 格挡判定

```csharp
private static bool CheckBlock(FP blockRate, int randomSeed)
{
    TSRandom random = new TSRandom(randomSeed);
    FP roll = random.NextFP();
    return roll < blockRate;
}
```

示例：
```
原伤害 = FP(150)
格挡率 = FP(0.30)（30%）
格挡值 = FP(80)
随机值 = FP(0.22)

→ 0.22 < 0.30 → 格挡成功！
→ 格挡后伤害 = FPMath.Max(FP.Zero, FP(150) - FP(80)) = FP(70)
```

---

## 六、完整伤害计算示例

### 场景：骑士攻击法师

**施法者（骑士 Lv5）**：
```
BaseStats:
  ATK = 80 + 8×4 = 112（基础80，等级加成8/级）
  CRIT_RATE = 0.05
  CRIT_DMG = 2.0
  ACCURACY = 0.95

Buffs:
  [力量祝福] ATK +20% (Percent)
  [狂战士] CRIT_DMG ×1.5 (FinalMultiplier)

DerivedStats:
  FinalStats.Get(StatType.ATK) = 112 × 1.2 = 134.4
  FinalStats.Get(StatType.CRIT_RATE) = 0.05
  FinalStats.Get(StatType.CRIT_DMG) = 2.0 × 1.5 = 3.0
  FinalStats.Get(StatType.ACCURACY) = 0.95
```

**目标（法师 Lv3）**：
```
BaseStats:
  DEF = 40 + 4×2 = 48
  EVASION = 0.10
  BLOCK_RATE = 0.05
  BLOCK_VALUE = 30
  PHYSICAL_RES = 0.0
  MAGICAL_RES = 0.3

DerivedStats:
  FinalStats.Get(StatType.DEF) = 48
  FinalStats.Get(StatType.EVASION) = 0.10
  FinalStats.Get(StatType.BLOCK_RATE) = 0.05
  FinalStats.Get(StatType.BLOCK_VALUE) = 30
```

**技能效果配置**：
```
SkillEffectId: 4001
EffectType: 1（伤害）
EffectValue: 150（150%攻击力）
DamageType: 1（物理）
```

**计算流程**：

```
[1] 基础伤害计算
    = FinalAttack × (EffectValue / 100)
    = 134.4 × 1.5
    = 201.6

[2] 命中判定
    命中概率 = FinalAccuracy - FinalEvasion
             = 0.95 - 0.10 = 0.85（85%）
    随机数 = 0.42 → 命中成功！

[3] 格挡判定
    格挡概率 = FinalBlockRate = 0.05（5%）
    随机数 = 0.82 → 格挡失败

[4] 暴击判定
    暴击概率 = FinalCritRate = 0.05（5%）
    随机数 = 0.03 → 暴击成功！💥
    暴击伤害 = 201.6 × 3.0 = 604.8

[5] 防御减免
    减伤百分比 = Defense / (Defense + 100)
                = 48 / 148 = 0.324（32.4%）
    伤害 = 604.8 × (1 - 0.324) = 408.8

[6] 抗性减免（物理抗性）
    抗性 = FinalPhysicalResistance = 0.0
    伤害 = 408.8 × (1 - 0.0) = 408.8

[7] 随机浮动（±5%）
    随机数 = 0.62 → 浮动 = 0.97
    最终伤害 = 408.8 × 0.97 = 396.5

[8] 结果
    ✅ 命中
    ✅ 暴击
    ❌ 格挡失败
    最终伤害：396.5
```

---

## 七、成长系统设计

### 7.1 等级成长

**经验获取**：
- 击杀敌人
- 完成关卡
- 使用经验道具

**升级收益**（以骑士为例）：
```
等级1→2：
  需要经验：1000
  属性加成：攻击+8，防御+8，生命+100，速度+0.1
  技能点：+1

等级2→3：
  需要经验：2500
  属性加成：攻击+8，防御+8，生命+100，速度+0.1
  技能点：+1
  
等级5、10、15...（每5级）：
  技能点：+2（额外奖励）
  可能解锁新技能
```

### 7.2 自由加点系统

**加点规则**：
- 每级获得 1-2 技能点
- 可分配到四维属性
- 每点收益：
  - 攻击：+2
  - 防御：+2
  - 生命：+20
  - 速度：+0.1

**加点示例**：
```
骑士 Lv10，共15点可分配
玩家分配：
  攻击：5点 → +10攻击
  防御：3点 → +6防御
  生命：7点 → +140生命
  速度：0点

最终属性：
  攻击 = 80（基础）+ 72（等级）+ 10（加点）= 162
  防御 = 80（基础）+ 72（等级）+ 6（加点）= 158
  生命 = 1000（基础）+ 900（等级）+ 140（加点）= 2040
  速度 = 10（基础）+ 0.9（等级）= 10.9
```

---

## 八、Buff系统设计

### 8.1 Buff生命周期

```
Buff触发（技能效果/道具/环境）
    ↓
[1] BuffComponent.AddBuff(buffInstance)
    - 检查是否已存在同ID Buff
    - 可叠加：增加层数，刷新时间
    - 不可叠加：刷新时间
    ↓
[2] 添加属性修饰器到 DerivedStatsComponent
    - 从 BuffTable 读取修饰器配置
    - 添加到对应属性的修饰器列表
    ↓
[3] DerivedStatsComponent.RecalculateAll()
    - 重新计算所有最终属性
    ↓
每帧更新
    ↓
[4] BuffComponent.UpdateBuffs()
    - 减少所有Buff的剩余帧数
    - 移除过期Buff
    - 触发持续伤害（如燃烧）
    ↓
[5] Buff移除时
    - 从修饰器列表移除
    - 重新计算属性
```

### 8.2 Buff叠加规则

#### 同ID Buff
```
情况1：可叠加Buff（如"力量祝福"）
  已有：[力量祝福] 2层，剩余200帧
  新增：[力量祝福] 1层，300帧
  
  结果：[力量祝福] 3层（Max 3），剩余300帧（刷新）
  效果：Attack +20% × 3层 = +60%

情况2：不可叠加Buff（如"护盾"）
  已有：[护盾] 剩余100帧
  新增：[护盾] 180帧
  
  结果：[护盾] 剩余180帧（刷新时间）
  效果：Shield +200（不变）
```

#### 不同ID Buff
```
可以同时存在，分别生效

示例：
  [力量祝福] Attack +20%
  [战斗狂热] Attack +10%
  
  总效果：Attack +30%
```

### 8.3 Buff配置示例

```csv
# BuffTable（所有数值都是int）
buffId,buffName,buffType,duration,stackable,maxStack,modifiers,tickDamage,tickInterval

# 增益Buff
5001,力量祝福,1,600,true,3,ATK:Percent:200,0,0
5002,极速,1,300,false,1,SPD:Percent:500,0,0
5003,护盾,1,180,false,1,无,0,0
5004,狂暴,1,600,true,5,ATK:Percent:100;CRIT_RATE:Flat:50,0,0

# 减益Debuff
6001,燃烧,2,300,false,1,PHYSICAL_RES:Percent:-100,10000,30
6002,冰冻,2,120,false,1,SPD:Percent:-800,0,0
6003,虚弱,2,600,true,3,ATK:Percent:-150,0,0
6004,破甲,2,300,false,1,DEF:Percent:-300,0,0

# 解释：
# 5001 - ATK +20%（200/1000=0.2）
# 5002 - SPD +50%（500/1000=0.5）
# 5004 - ATK +10%（100/1000=0.1），CRIT_RATE +5%（50/1000=0.05）
# 6001 - 物理抗性-10%（-100/1000=-0.1），每30帧10.0伤害（10000/1000=10.0）
# 6002 - 速度-80%（-800/1000=-0.8）
```

---

## 九、状态系统设计

### 9.1 StateComponent详细设计

**控制状态**（无法行动）：
- `IsStunned` - 晕眩（无法移动、攻击、释放技能）
- `IsFrozen` - 冰冻（无法移动、攻击）
- `IsKnockedBack` - 击飞（强制移动）
- `IsSilenced` - 沉默（无法释放技能）
- `IsDisarmed` - 缴械（无法普通攻击）

**特殊状态**：
- `IsInvincible` - 无敌（不受伤害）
- `IsInvisible` - 隐身（不被AI检测）
- `IsBlocking` - 格挡中（提高格挡率）
- `IsDashing` - 冲刺中（通常带无敌）
- `IsCasting` - 施法中（可能被打断）

**状态检查**：
```csharp
// 是否可以移动
public bool CanMove()
{
    return !IsStunned && !IsFrozen && !IsKnockedBack && !IsDead;
}

// 是否可以攻击
public bool CanAttack()
{
    return !IsStunned && !IsFrozen && !IsDisarmed && !IsDead;
}

// 是否可以释放技能
public bool CanCastSkill()
{
    return !IsStunned && !IsFrozen && !IsSilenced && !IsDead;
}

// 是否可以受到伤害
public bool CanTakeDamage()
{
    return !IsInvincible && !IsDead;
}
```

---

## 十、配置表扩展方案

### 10.1 RoleBaseTable扩展

**新增字段**（高级属性）：

```csv
##var,id,name,...,baseAttack,baseDefense,baseHealth,baseSpeed,baseCritRate,baseCritDamage,baseAccuracy,baseEvasion,baseBlockRate,baseBlockValue,physicalRes,magicalRes,baseMaxMana,manaRegen,healthRegen
##type,int,string,...,int,int,int,int,int,int,int,int,int,int,int,int,int,int,int
##desc,角色ID,角色名称,...,基础攻击力,基础防御力,基础生命值,基础移动速度*1000,基础暴击率*1000,基础暴击伤害*1000,基础命中率*1000,基础闪避率*1000,基础格挡率*1000,基础格挡值,物理抗性*1000,魔法抗性*1000,基础法力上限,法力回复*1000,生命回复*1000

# 示例数据（骑士）
1001,骑士,...,80,80,1000,10000,50,2000,950,50,150,60,100,0,100,5000,2000
# 解释：
# - 攻击80、防御80、生命1000（整数）
# - 速度10.0（10000/1000）
# - 暴击率5%（50/1000=0.05）
# - 暴击伤害200%（2000/1000=2.0）
# - 命中率95%（950/1000=0.95）
# - 闪避率5%（50/1000=0.05）
# - 格挡率15%（150/1000=0.15）
# - 格挡值60（整数）
# - 物理抗性10%（100/1000=0.1）
# - 魔法抗性0%（0/1000=0）
# - 法力上限100（整数）
# - 法力回复5.0/秒（5000/1000）
# - 生命回复2.0/秒（2000/1000）

# 示例数据（刺客 - 高暴击低防御）
1005,刺客,...,70,50,600,7000,250,2500,980,200,50,30,0,0,80,4000,1000
# - 暴击率25%（250/1000）
# - 暴击伤害250%（2500/1000）
# - 闪避率20%（200/1000）
# - 速度7.0（7000/1000）

# 示例数据（法师 - 高魔抗低物抗）
1003,法师,...,50,40,700,4500,100,2200,900,80,80,40,0,300,200,10000,1500
# - 魔法抗性30%（300/1000）
# - 速度4.5（4500/1000）
# - 法力上限200
```

**配置规则总表**：

| 属性类型 | 配置表类型 | 配置示例 | 运行时值 | 转换规则 |
|---------|----------|---------|---------|---------|
| 攻击/防御/生命 | int | 80 | FP(80) | 直接转换 |
| 速度 | int | 10500 | FP(10.5) | 除以1000 |
| 暴击率 | int | 50 | FP(0.05) | 除以1000 |
| 暴击伤害 | int | 2000 | FP(2.0) | 除以1000 |
| 命中率/闪避率 | int | 950/50 | FP(0.95)/FP(0.05) | 除以1000 |
| 抗性 | int | 100 | FP(0.1) | 除以1000 |
| 回复速度 | int | 5000 | FP(5.0) | 除以1000 |

**记忆法则**：
- ✅ **整数属性**：直接配置（攻击、防御、生命、法力上限等）
- ✅ **小数属性**：**扩大1000倍**存储（速度、回复速度等）
- ✅ **百分比属性**：**扩大1000倍**（暴击率、抗性等，5%存为50）
- ✅ **运行时读取**：需要除以1000的属性，代码中判断

### 10.2 RoleGrowthTable优化

**优化字段**：

```csv
##var,id,roleId,level,requiredExp,attackBonus,defenseBonus,healthBonus,speedBonus,critRateBonus,critDamageBonus,unlockSkillId,skillPoint
##type,int,int,int,int,int,int,int,int,int,int,int,int
##desc,ID,角色ID,等级,升级所需经验,攻击力加成,防御力加成,生命值加成,速度加成*1000,暴击率加成*1000,暴击伤害加成*1000,解锁技能ID,技能点

# 示例数据（骑士 Lv2）
2,1001,2,1000,8,8,100,100,2,50,0,1
# 解释：
# - 攻击 +8
# - 防御 +8
# - 生命 +100
# - 速度 +0.1（100/1000）
# - 暴击率 +0.2%（2/1000）
# - 暴击伤害 +5%（50/1000=0.05）

# 示例数据（刺客 Lv2 - 高速度和暴击成长）
12,1005,2,1000,7,5,60,200,5,100,0,1
# - 攻击 +7
# - 速度 +0.2（200/1000）
# - 暴击率 +0.5%（5/1000）
# - 暴击伤害 +10%（100/1000）
```

**配置规则**：
- **整数加成**：直接配置（攻击、防御、生命等）
- **小数加成**：**扩大1000倍**（速度、暴击率、暴击伤害等）

### 10.3 新增BuffTable

见 [八、Buff系统设计](#82-buff叠加规则) 中的配置示例。

### 10.4 SkillEffectTable优化

**新增字段**：

```csv
##var,skillEffectId,...,damageType,scalingStat,scalingRatio
##type,int,...,int,int,float
##desc,效果ID,...,伤害类型(1=物理/2=魔法/3=真实),缩放属性(1=攻击/2=防御/3=生命),缩放比例

# 示例：物理伤害，150%攻击力缩放
4001,重击,1,150.0,1,0.0,0.0,Physical,1,1,1.5

# 示例：魔法伤害，200%攻击力缩放
4013,火球术,1,200.0,1,0.0,3.0,Magical,1,1,2.0

# 示例：真实伤害，100%生命上限缩放（斩杀技能）
4099,终结技,1,0.0,1,0.0,0.0,True,3,3,0.1
```

---

## 十一、数值平衡参考

### 11.1 基础数值建议

**战士型（骑士、重锤者）**：
```
攻击：80-100
防御：80-120
生命：1000-1200
速度：3.5-10
暴击率：5-10%
```

**敏捷型（刺客、弓手）**：
```
攻击：60-80
防御：50-60
生命：600-800
速度：6-10
暴击率：20-30%
```

**法师型（法师）**：
```
攻击：50-60
防御：40-50
生命：700-800
速度：4.5-6
暴击率：10-15%
魔法抗性：20-30%
```

### 11.2 成长曲线建议

**攻击力成长**：
```
战士型：每级 +8-10
敏捷型：每级 +6-8
法师型：每级 +5-7
```

**生命值成长**：
```
战士型：每级 +100-120
敏捷型：每级 +60-80
法师型：每级 +70-90
```

### 11.3 伤害倍率建议

**普通攻击**：
```
轻击：80-120%攻击力
重击：150-200%攻击力
```

**技能伤害**：
```
小技能：150-250%攻击力
中技能：300-500%攻击力
大招：800-1500%攻击力
```

**Buff加成**：
```
普通Buff：+10-20%属性
强力Buff：+30-50%属性
终极Buff：+100%属性（短时间）
```

---

## 十二、实现清单

### Phase 1：核心组件实现

1. **创建 BaseStatsComponent.cs**
   - 定义所有基础属性字段
   - 实现 InitializeFromConfig() 方法
   - 支持等级成长和加点

2. **创建 DerivedStatsComponent.cs**
   - 定义所有派生属性字段
   - 实现 RecalculateAll() 方法
   - 实现修饰器管理

3. **创建 DynamicStatsComponent.cs**
   - 定义动态资源（HP、MP、能量等）
   - 实现 TakeDamage()、Heal() 等方法
   - 实现资源消耗和恢复

4. **创建 BuffComponent.cs**
   - 实现 Buff 添加/移除/更新
   - 实现 Buff 叠加逻辑
   - 实现修饰器提取

5. **创建 StateComponent.cs**
   - 定义所有状态标志
   - 实现状态检查方法

6. **创建 LevelComponent.cs**
   - 实现等级和经验管理
   - 实现升级逻辑

7. **创建 GrowthComponent.cs**
   - 实现自由加点系统
   - 实现加点分配逻辑

### Phase 2：配置表扩展

8. **扩展 RoleBaseTable**
   - 添加高级属性字段
   - 更新现有角色数据

9. **优化 RoleGrowthTable**
   - 统一成长字段命名
   - 添加暴击成长

10. **创建 BuffTable**
    - 定义Buff配置结构
    - 添加示例Buff数据

11. **优化 SkillEffectTable**
    - 添加伤害类型、缩放属性字段
    - 更新现有技能效果数据

### Phase 3：伤害计算重构

12. **重构 DamageCalculator.cs**
    - 实现完整的伤害计算公式
    - 接入真实的属性系统
    - 实现命中/闪避/格挡/暴击判定

13. **创建 StatModifier.cs**
    - 定义修饰器数据结构
    - 实现修饰器解析

14. **创建 DamageResult.cs 扩展**
    - 添加更多伤害信息（格挡、闪避等）

### Phase 4：集成测试

15. **属性系统集成测试**
    - 测试三层属性计算正确性
    - 测试Buff加成生效
    - 测试等级成长

16. **伤害计算集成测试**
    - 测试各种伤害类型
    - 测试暴击、格挡、闪避
    - 测试防御和抗性

17. **Buff系统集成测试**
    - 测试Buff叠加
    - 测试Buff过期移除
    - 测试持续伤害

---

## 十三、技术要点

### 13.1 确定性计算

**问题**：帧同步需要确定性，但浮点数不确定

**解决方案（本系统采用）**：

#### ✅ 全面使用定点数（FP）

```csharp
using TrueSync;

// 所有属性值使用FP
public class Stats
{
    private Dictionary<StatType, FP> _values;
    public FP Get(StatType type) => _values.TryGetValue(type, out var v) ? v : FP.Zero;
}

// 所有计算使用FP
FP baseDamage = casterAttack * ratio;
FP finalDamage = baseDamage * (FP.One - damageReduction);
```

#### ✅ 确定性随机数

```csharp
// 使用TrueSync的TSRandom，基于种子生成
int seed = GenerateSeed(frameNumber, casterId, targetId);
TSRandom random = new TSRandom(seed);
FP randomValue = random.NextFP(); // [0, 1)

// 所有客户端使用相同种子，生成相同随机序列
```

#### ✅ 种子生成策略

```csharp
private static int GenerateSeed(int frame, long casterId, long targetId)
{
    // 确保所有客户端生成相同种子
    return HashCode.Combine(frame, casterId, targetId);
}

// 用法：
int seed = GenerateSeed(currentFrame, caster.UniqueId, target.UniqueId);
bool isCrit = CheckCritical(critRate, seed);
```

**确定性保证**：
- ✅ 数值存储：使用 `TrueSync.FP`（64位定点数）
- ✅ 数学运算：使用 `FPMath` 工具类
- ✅ 随机判定：使用 `TSRandom` + 确定性种子
- ✅ 所有客户端：相同输入→相同输出

**注意事项**：
- ❌ 禁止使用 `float`、`double` 存储运行时数值
- ❌ 禁止使用 `System.Random`（不确定）
- ❌ 禁止使用 `UnityEngine.Random`（不确定）
- ❌ 禁止使用 `DateTime.Now`（不同步）

### 13.2 性能优化

**避免频繁重算**：
```csharp
// 方案：脏标记机制
public class DerivedStatsComponent
{
    private bool _isDirty = false;
    
    public void MarkDirty()
    {
        _isDirty = true;
    }
    
    public void RecalculateIfDirty(BaseStatsComponent baseStats)
    {
        if (!_isDirty) return;
        
        RecalculateAll(baseStats);
        _isDirty = false;
    }
}

// 使用：
buffComponent.AddBuff(buff);
derivedStats.MarkDirty();

// 帧结束时统一重算
derivedStats.RecalculateIfDirty(baseStats);
```

### 13.3 配置表数值规则

**核心原则**：配置表全部使用 `int` 类型，避免float的不确定性

**配置规则**：

#### 整数属性（直接配置）
```csv
baseAttack = 80      # 攻击力80 → 运行时 FP(80)
baseDefense = 80     # 防御力80 → 运行时 FP(80)
baseHealth = 1000    # 生命值1000 → 运行时 FP(1000)
```

#### 小数属性（扩大1000倍）
```csv
baseSpeed = 10500    # 速度10.5 → 运行时 FP(10500)/FP(1000) = FP(10.5)
manaRegen = 5000     # 法力回复5.0/秒 → 运行时 FP(5000)/FP(1000) = FP(5.0)
```

#### 百分比属性（扩大1000倍）
```csv
baseCritRate = 50    # 暴击率5% → 运行时 FP(50)/FP(1000) = FP(0.05)
baseCritDamage = 2000 # 暴击伤害200% → 运行时 FP(2000)/FP(1000) = FP(2.0)
baseAccuracy = 950   # 命中率95% → 运行时 FP(950)/FP(1000) = FP(0.95)
physicalRes = 100    # 物理抗性10% → 运行时 FP(100)/FP(1000) = FP(0.1)
```

**代码示例**：
```csharp
// 配置表字段类型
public class RoleBaseTable
{
    public int BaseAttack;     // 整数属性
    public int BaseSpeed;      // 小数属性*1000
    public int BaseCritRate;   // 百分比*1000
}

// 运行时转换
BaseStats.Set(StatType.ATK, (FP)roleConfig.BaseAttack);                    // 直接转换
BaseStats.Set(StatType.SPD, (FP)roleConfig.BaseSpeed / (FP)1000);         // 除以1000
BaseStats.Set(StatType.CRIT_RATE, (FP)roleConfig.BaseCritRate / (FP)1000); // 除以1000
```

**优势**：
- ✅ 配置表全是int，易于编辑和校验
- ✅ 避免float精度问题
- ✅ CSV文件更清晰（50 vs 0.05）
- ✅ 运行时一次性转换为FP

### 13.4 序列化注意事项

**MemoryPack序列化**：
- ✅ `Dictionary<StatType, FP>`：正常序列化
- ✅ `Stats`对象：完整序列化
- ❌ 修饰器列表：标记 `[MemoryPackIgnore]`（运行时计算）
- ❌ `_modifiers` 字典：标记 `[MemoryPackIgnore]`

```csharp
[MemoryPackable]
public partial class DerivedStatsComponent
{
    public Stats FinalStats { get; set; } // ✅ 序列化
    
    [MemoryPackIgnore]
    private Dictionary<StatType, List<StatModifier>> _modifiers; // ❌ 不序列化
}
```

**为什么不序列化修饰器**：
- 修饰器来自Buff，Buff本身会序列化
- 回滚恢复后，从Buff重新计算修饰器即可
- 减少序列化数据量

---

## 十四、使用示例

### 14.1 创建角色时初始化

```csharp
// 创建角色Entity
var playerEntity = EntityFactory.CreateRole(roleId: 1001, level: 1);

// 组件已自动添加和初始化：
var baseStats = playerEntity.GetComponent<BaseStatsComponent>();
baseStats.InitializeFromConfig(1001, 1);
// → BaseStats.Get(StatType.ATK) = 80
// → BaseStats.Get(StatType.DEF) = 80
// → BaseStats.Get(StatType.HP) = 1000

var derivedStats = playerEntity.GetComponent<DerivedStatsComponent>();
derivedStats.RecalculateAll(baseStats);
// → FinalStats.Get(StatType.ATK) = 80
// → FinalStats.Get(StatType.HP) = 1000

var dynamicStats = playerEntity.GetComponent<DynamicStatsComponent>();
dynamicStats.InitializeResources(derivedStats);
// → CurrentHealth = 1000
// → CurrentMana = 100
```

### 14.2 添加Buff

```csharp
// 添加"力量祝福"Buff
var buffComp = entity.GetComponent<BuffComponent>();
buffComp.AddBuff(new BuffInstance
{
    BuffId = 5001,              // 力量祝福
    Duration = 600,             // 30秒（600帧 @ 20FPS）
    RemainingFrames = 600,
    StackCount = 1,
    Stackable = true,
    MaxStack = 3,
    CasterId = casterEntity.UniqueId
});

// 重新计算属性
var baseStats = entity.GetComponent<BaseStatsComponent>();
var derivedStats = entity.GetComponent<DerivedStatsComponent>();

// 1. 清空旧的修饰器
derivedStats.ClearModifiers();

// 2. 从Buff收集新的修饰器
var buffModifiers = buffComp.GetAllModifiers();
foreach (var kvp in buffModifiers)
{
    foreach (var modifier in kvp.Value)
    {
        derivedStats.AddModifier(kvp.Key, modifier);
    }
}

// 3. 重算所有属性
derivedStats.RecalculateAll(baseStats);

// 结果：
// BaseStats.Get(StatType.ATK) = 100
// Buff修饰器: ATK +20% (Percent)
// FinalStats.Get(StatType.ATK) = 100 × 1.2 = 120
```

### 14.3 伤害结算

```csharp
// 施放技能，触发伤害效果
var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);

// 生成确定性随机种子
int seed = GenerateSeed(currentFrame, attackerEntity.UniqueId, targetEntity.UniqueId);

var damageResult = DamageCalculator.Calculate(
    caster: attackerEntity,
    target: targetEntity,
    effectConfig: effectConfig,
    randomSeed: seed
);

if (damageResult.IsMiss)
{
    // 未命中，显示MISS
    return;
}

// 应用伤害
var targetDynamic = targetEntity.GetComponent<DynamicStatsComponent>();
var targetDerived = targetEntity.GetComponent<DerivedStatsComponent>();

FP actualDamage = targetDynamic.TakeDamage(damageResult.FinalDamage, targetDerived);

// 伤害反馈（转为float用于UI显示）
float displayDamage = (float)actualDamage;

if (damageResult.IsCritical)
{
    ShowCriticalDamageText(displayDamage); // 💥暴击
}
else if (damageResult.IsBlocked)
{
    ShowBlockedDamageText(displayDamage); // 🛡️格挡
}
else
{
    ShowNormalDamageText(displayDamage);
}

// 检查死亡
if (targetDynamic.CurrentHealth <= FP.Zero)
{
    var stateComp = targetEntity.GetComponent<StateComponent>();
    stateComp.IsDead = true;
    OnEntityDied(targetEntity);
}
```

**DamageResult数据结构**：
```csharp
public class DamageResult
{
    public FP FinalDamage { get; set; }      // 最终伤害（定点数）
    public bool IsCritical { get; set; }     // 是否暴击
    public bool IsBlocked { get; set; }      // 是否格挡
    public bool IsMiss { get; set; }         // 是否未命中
    public DamageType DamageType { get; set; } // 伤害类型
}
```

### 14.4 升级处理

```csharp
// 获得经验
var levelComp = entity.GetComponent<LevelComponent>();
bool leveledUp = levelComp.GainExp(1500, entity);

if (leveledUp)
{
    // LevelUp() 内部已自动：
    // 1. 更新 BaseStats
    // 2. 重算 DerivedStats
    // 3. 恢复 DynamicStats
    // 4. 增加技能点
    
    // UI显示升级特效
    ShowLevelUpEffect(entity);
}
```

---

## 十五、与现有系统的集成

### 15.1 替换HealthComponent

**现状**：
```csharp
// 旧的HealthComponent
public int CurrentHealth { get; set; }
public int MaxHealth { get; set; }
```

**迁移方案**：
```csharp
// 方式1：保留HealthComponent作为桥接
public class HealthComponent : BaseComponent
{
    public int CurrentHealth 
    {
        get => (int)Entity.GetComponent<DynamicStatsComponent>().CurrentHealth;
        set => Entity.GetComponent<DynamicStatsComponent>().CurrentHealth = value;
    }
    
    public int MaxHealth
    {
        get => (int)Entity.GetComponent<DerivedStatsComponent>().FinalMaxHealth;
    }
}

// 方式2：直接删除HealthComponent，全面使用新系统
```

### 15.2 DamageCalculator集成

**替换现有的写死数值**：
```csharp
// 旧代码（写死）
private static float GetEntityAttack(Entity entity)
{
    return 100f; // TODO
}

// 新代码（从属性系统读取）
private static float GetEntityAttack(Entity entity)
{
    var derivedStats = entity.GetComponent<DerivedStatsComponent>();
    return derivedStats?.FinalAttack ?? 0f;
}
```

### 15.3 EntityFactory集成

**创建Entity时自动添加组件**：
```csharp
public static Entity CreateRole(int roleId, int level = 1)
{
    var entity = new Entity();
    
    // 添加所有数值组件
    var baseStats = entity.AddComponent<BaseStatsComponent>();
    baseStats.InitializeFromConfig(roleId, level);
    
    var derivedStats = entity.AddComponent<DerivedStatsComponent>();
    derivedStats.RecalculateAll(baseStats);
    
    var dynamicStats = entity.AddComponent<DynamicStatsComponent>();
    dynamicStats.CurrentHealth = derivedStats.FinalMaxHealth;
    dynamicStats.CurrentMana = derivedStats.FinalMaxMana;
    
    entity.AddComponent<BuffComponent>();
    entity.AddComponent<StateComponent>();
    
    var levelComp = entity.AddComponent<LevelComponent>();
    levelComp.CurrentLevel = level;
    
    var growthComp = entity.AddComponent<GrowthComponent>();
    growthComp.RoleId = roleId;
    
    // ... 其他组件
    
    return entity;
}
```

---

## 十六、开发优先级

### P0（核心基础）
1. ✅ BaseStatsComponent
2. ✅ DerivedStatsComponent
3. ✅ DynamicStatsComponent
4. ✅ 重构 DamageCalculator
5. ✅ 扩展 RoleBaseTable

### P1（重要功能）
6. ✅ BuffComponent
7. ✅ StateComponent
8. ✅ 创建 BuffTable
9. ✅ Buff修饰器计算

### P2（完善系统）
10. ✅ LevelComponent
11. ✅ GrowthComponent
12. ✅ 升级逻辑
13. ✅ 自由加点

---

## 十七、验收标准

### 功能验收
- [ ] 角色属性正确读取配置表
- [ ] Buff加成正确影响最终属性
- [ ] 伤害计算使用真实属性（不再写死）
- [ ] 暴击、格挡、闪避判定生效
- [ ] 等级成长正确应用
- [ ] Buff叠加和过期正确处理

### 性能验收
- [ ] 属性计算耗时 < 0.1ms
- [ ] Buff更新耗时 < 0.05ms
- [ ] 支持100+ Entity同时运行

### 测试验收
- [ ] 单元测试覆盖率 > 80%
- [ ] 集成测试通过
- [ ] 数值平衡测试通过

---

## 附录：数据流转示例图

```
┌─────────────────────────────────────────────────────┐
│              数值系统完整流转示例                      │
└─────────────────────────────────────────────────────┘

配置表
├─ RoleBaseTable(1001) → ATK=80, DEF=80, HP=1000
├─ RoleGrowthTable(Lv5) → AttackBonus=8/级
└─ BuffTable(5001) → ATK +20%

    ↓ InitializeFromConfig()
    
BaseStatsComponent
├─ BaseStats.Get(StatType.ATK) = 80 + 8×4 = 112（基础+成长）
├─ BaseStats.Get(StatType.DEF) = 80 + 8×4 = 112
└─ BaseStats.Get(StatType.HP) = 1000 + 100×4 = 1400

    ↓ AddBuff(5001) → 收集修饰器
    
DerivedStatsComponent 修饰器
└─ ATK: [Buff 5001: Percent +0.2]

    ↓ RecalculateAll()
    
DerivedStatsComponent
├─ FinalStats.Get(StatType.ATK) = 112 × 1.2 = 134.4
├─ FinalStats.Get(StatType.DEF) = 112
└─ FinalStats.Get(StatType.HP) = 1400

    ↓ InitializeResources()
    
DynamicStatsComponent
├─ CurrentHealth = 1400（满血）
├─ CurrentMana = 100
└─ Energy = 0

    ↓ 战斗中...
    
受到150%攻击力的技能伤害
    ↓ DamageCalculator.Calculate()
    
基础伤害 = 敌方ATK(100) × 1.5 = 150
    ↓ 防御减免
    
减伤后 = 150 × (1 - 112/212) = 70.8
    ↓ DynamicStats.TakeDamage()
    
CurrentHealth = 1400 - 70.8 = 1329.2
```

---

**创建日期**: 2025-10-14  
**作者**: Astrum开发团队  
**状态**: 📝 设计完成，待实现


