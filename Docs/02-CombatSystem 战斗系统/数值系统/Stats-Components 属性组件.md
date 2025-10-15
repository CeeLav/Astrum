# 属性组件详细设计

> 📦 数值系统的7个核心组件：BaseStats, DerivedStats, DynamicStats, Buff, State, Level, Growth
>
> **版本**: v1.0  
> **更新**: 2025-10-14

---

## 一、BaseStatsComponent（基础属性组件）

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

**使用示例**：
```csharp
// 创建角色时初始化
var baseStats = entity.AddComponent<BaseStatsComponent>();
baseStats.InitializeFromConfig(roleId: 1001, level: 5);

// 获取基础攻击力
FP baseAttack = baseStats.BaseStats.Get(StatType.ATK);
```

---

## 二、DerivedStatsComponent（派生属性组件）

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
    
    /// <summary>脏标记（优化：避免频繁重算）</summary>
    [MemoryPackIgnore]
    private bool _isDirty = false;
    
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
        foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
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

**使用示例**：
```csharp
// 添加Buff修饰器
var derivedStats = entity.GetComponent<DerivedStatsComponent>();
derivedStats.AddModifier(StatType.ATK, new StatModifier
{
    SourceId = 5001,  // Buff ID
    Type = ModifierType.Percent,
    Value = (FP)0.2,  // +20%
    Priority = 200
});

// 重算属性
var baseStats = entity.GetComponent<BaseStatsComponent>();
derivedStats.RecalculateAll(baseStats);

// 获取最终攻击力
FP finalAttack = derivedStats.Get(StatType.ATK);
```

---

## 三、DynamicStatsComponent（动态属性组件）

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
    
    // ===== 业务方法 =====
    
    /// <summary>扣除生命值（考虑护盾）</summary>
    public FP TakeDamage(FP damage, DerivedStatsComponent derivedStats)
    {
        FP remainingDamage = damage;
        
        // 1. 先扣除护盾
        FP currentShield = Get(DynamicResourceType.SHIELD);
        if (currentShield > FP.Zero)
        {
            FP shieldDamage = FPMath.Min(currentShield, remainingDamage);
            Set(DynamicResourceType.SHIELD, currentShield - shieldDamage);
            remainingDamage -= shieldDamage;
        }
        
        // 2. 扣除生命值
        if (remainingDamage > FP.Zero)
        {
            FP currentHP = Get(DynamicResourceType.CURRENT_HP);
            FP actualDamage = FPMath.Min(currentHP, remainingDamage);
            Set(DynamicResourceType.CURRENT_HP, currentHP - actualDamage);
            return actualDamage; // 返回实际受到的生命伤害
        }
        
        return FP.Zero;
    }
    
    /// <summary>恢复生命值（不超过上限）</summary>
    public FP Heal(FP amount, DerivedStatsComponent derivedStats)
    {
        FP maxHP = derivedStats.Get(StatType.HP);
        FP currentHP = Get(DynamicResourceType.CURRENT_HP);
        FP maxHeal = maxHP - currentHP;
        FP actualHeal = FPMath.Min(amount, maxHeal);
        Set(DynamicResourceType.CURRENT_HP, currentHP + actualHeal);
        return actualHeal;
    }
    
    /// <summary>消耗法力</summary>
    public bool ConsumeMana(FP amount)
    {
        FP currentMP = Get(DynamicResourceType.CURRENT_MANA);
        if (currentMP < amount)
            return false;
        
        Set(DynamicResourceType.CURRENT_MANA, currentMP - amount);
        return true;
    }
    
    /// <summary>增加能量（自动限制在0-100）</summary>
    public void AddEnergy(FP amount)
    {
        FP newValue = FPMath.Clamp(Get(DynamicResourceType.ENERGY) + amount, FP.Zero, (FP)100);
        Set(DynamicResourceType.ENERGY, newValue);
    }
    
    /// <summary>增加怒气（自动限制在0-100）</summary>
    public void AddRage(FP amount)
    {
        FP newValue = FPMath.Clamp(Get(DynamicResourceType.RAGE) + amount, FP.Zero, (FP)100);
        Set(DynamicResourceType.RAGE, newValue);
    }
    
    /// <summary>初始化动态资源（满血满蓝）</summary>
    public void InitializeResources(DerivedStatsComponent derivedStats)
    {
        Set(DynamicResourceType.CURRENT_HP, derivedStats.Get(StatType.HP));
        Set(DynamicResourceType.CURRENT_MANA, derivedStats.Get(StatType.MAX_MANA));
        Set(DynamicResourceType.ENERGY, FP.Zero);
        Set(DynamicResourceType.RAGE, FP.Zero);
        Set(DynamicResourceType.SHIELD, FP.Zero);
        Set(DynamicResourceType.COMBO, FP.Zero);
    }
}
```

**使用示例**：
```csharp
// 初始化资源
var dynamicStats = entity.GetComponent<DynamicStatsComponent>();
var derivedStats = entity.GetComponent<DerivedStatsComponent>();
dynamicStats.InitializeResources(derivedStats);

// 受到伤害
FP actualDamage = dynamicStats.TakeDamage((FP)150, derivedStats);

// 治疗
FP actualHeal = dynamicStats.Heal((FP)200, derivedStats);

// 消耗法力
bool success = dynamicStats.ConsumeMana((FP)50);
```

---

## 四、BuffComponent（Buff组件）

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

**使用示例**：
```csharp
// 添加Buff
var buffComp = entity.GetComponent<BuffComponent>();
buffComp.AddBuff(new BuffInstance
{
    BuffId = 5001,
    Duration = 600,
    RemainingFrames = 600,
    Stackable = true,
    MaxStack = 3
});

// 每帧更新
buffComp.UpdateBuffs();

// 应用Buff修饰器到派生属性
var derivedStats = entity.GetComponent<DerivedStatsComponent>();
derivedStats.ClearModifiers();

var buffModifiers = buffComp.GetAllModifiers();
foreach (var kvp in buffModifiers)
{
    foreach (var modifier in kvp.Value)
    {
        derivedStats.AddModifier(kvp.Key, modifier);
    }
}

derivedStats.RecalculateAll(baseStats);
```

---

## 五、StateComponent（状态组件）

**职责**：存储实体的各种状态标志

**状态类型枚举**：

```csharp
/// <summary>
/// 状态类型枚举
/// </summary>
public enum StateType
{
    // ===== 控制状态 =====
    STUNNED = 1,        // 晕眩（无法移动、攻击、释放技能）
    FROZEN = 2,         // 冰冻（无法移动、攻击）
    KNOCKED_BACK = 3,   // 击飞（强制移动）
    SILENCED = 4,       // 沉默（无法释放技能）
    DISARMED = 5,       // 缴械（无法普通攻击）
    
    // ===== 特殊状态 =====
    INVINCIBLE = 10,    // 无敌（不受伤害）
    INVISIBLE = 11,     // 隐身（不被AI检测）
    BLOCKING = 12,      // 格挡中（提高格挡率）
    DASHING = 13,       // 冲刺中（通常带无敌）
    CASTING = 14,       // 施法中（可能被打断）
    
    // ===== 死亡状态 =====
    DEAD = 20,          // 已死亡
}
```

**组件设计**：

```csharp
[MemoryPackable]
public partial class StateComponent : BaseComponent
{
    /// <summary>状态字典（使用bool标记状态是否激活）</summary>
    private Dictionary<StateType, bool> _states = new Dictionary<StateType, bool>();
    
    /// <summary>获取状态</summary>
    public bool Get(StateType type)
    {
        return _states.TryGetValue(type, out var value) && value;
    }
    
    /// <summary>设置状态</summary>
    public void Set(StateType type, bool value)
    {
        _states[type] = value;
    }
    
    /// <summary>清空所有状态</summary>
    public void Clear()
    {
        _states.Clear();
    }
    
    // ===== 辅助方法 =====
    
    /// <summary>是否可以移动</summary>
    public bool CanMove()
    {
        return !Get(StateType.STUNNED) 
            && !Get(StateType.FROZEN) 
            && !Get(StateType.KNOCKED_BACK) 
            && !Get(StateType.DEAD);
    }
    
    /// <summary>是否可以攻击</summary>
    public bool CanAttack()
    {
        return !Get(StateType.STUNNED) 
            && !Get(StateType.FROZEN) 
            && !Get(StateType.DISARMED) 
            && !Get(StateType.DEAD);
    }
    
    /// <summary>是否可以释放技能</summary>
    public bool CanCastSkill()
    {
        return !Get(StateType.STUNNED) 
            && !Get(StateType.FROZEN) 
            && !Get(StateType.SILENCED) 
            && !Get(StateType.DEAD);
    }
    
    /// <summary>是否可以受到伤害</summary>
    public bool CanTakeDamage()
    {
        return !Get(StateType.INVINCIBLE) && !Get(StateType.DEAD);
    }
}
```

**使用示例**：
```csharp
var stateComp = entity.GetComponent<StateComponent>();

// 检查是否可以移动
if (stateComp.CanMove())
{
    // 执行移动逻辑
}

// 施加眩晕状态
stateComp.Set(StateType.STUNNED, true);

// 检查状态
if (stateComp.Get(StateType.DEAD))
{
    // 处理死亡逻辑
}

// 检查死亡
if (dynamicStats.CurrentHealth <= FP.Zero)
{
    stateComp.Set(StateType.DEAD, true);
}
```

---

## 六、LevelComponent（等级组件）

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
        dynamicStats.Set(DynamicResourceType.CURRENT_HP, derivedStats.Get(StatType.HP));
        dynamicStats.Set(DynamicResourceType.CURRENT_MANA, derivedStats.Get(StatType.MAX_MANA));
        
        ASLogger.Instance.Info($"[Level] Entity {owner.UniqueId} leveled up to {CurrentLevel}!");
    }
}
```

**使用示例**：
```csharp
var levelComp = entity.GetComponent<LevelComponent>();

// 获得经验
bool leveledUp = levelComp.GainExp(1500, entity);

if (leveledUp)
{
    // 显示升级特效
    ShowLevelUpEffect(entity);
}
```

---

## 七、GrowthComponent（成长组件）

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
                baseStats.BaseStats.Add(StatType.ATK, (FP)2); // 每点+2攻击
                break;
            case StatType.DEF:
                AllocatedDefensePoints++;
                baseStats.BaseStats.Add(StatType.DEF, (FP)2);
                break;
            case StatType.HP:
                AllocatedHealthPoints++;
                baseStats.BaseStats.Add(StatType.HP, (FP)20); // 每点+20生命
                break;
            case StatType.SPD:
                AllocatedSpeedPoints++;
                baseStats.BaseStats.Add(StatType.SPD, (FP)0.1);
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

**使用示例**：
```csharp
var growthComp = entity.GetComponent<GrowthComponent>();

// 升级获得属性点
growthComp.AvailableStatPoints += 2;

// 玩家分配属性点
bool success = growthComp.AllocatePoint(StatType.ATK, entity);
```

---

## 八、性能优化

### 8.1 脏标记机制

**问题**：Buff每帧更新，但属性不一定每帧变化，频繁重算浪费性能

**解决方案**：

```csharp
// 在DerivedStatsComponent中使用脏标记
private bool _isDirty = false;

public void MarkDirty()
{
    _isDirty = true;
}

public void RecalculateIfDirty(BaseStatsComponent baseStats)
{
    if (!_isDirty) return;
    RecalculateAll(baseStats);
}

// 使用方式：
// 帧开始时更新Buff
buffComponent.UpdateBuffs();

// Buff变化时标记脏
if (buffChanged)
{
    derivedStats.MarkDirty();
}

// 帧结束时统一重算
derivedStats.RecalculateIfDirty(baseStats);
```

### 8.2 批量处理

**原则**：将多个属性修改批量处理，最后一次性重算

```csharp
// ❌ 错误：每次修改都重算
derivedStats.AddModifier(StatType.ATK, modifier1);
derivedStats.RecalculateAll(baseStats);  // 重算1次

derivedStats.AddModifier(StatType.DEF, modifier2);
derivedStats.RecalculateAll(baseStats);  // 重算2次

// ✅ 正确：批量修改，最后重算
derivedStats.AddModifier(StatType.ATK, modifier1);
derivedStats.AddModifier(StatType.DEF, modifier2);
derivedStats.RecalculateAll(baseStats);  // 只重算1次
```

### 8.3 序列化优化

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
    
    [MemoryPackIgnore]
    private bool _isDirty; // ❌ 不序列化
}
```

**为什么不序列化修饰器**：
- 修饰器来自Buff，Buff本身会序列化
- 回滚恢复后，从Buff重新计算修饰器即可
- 减少序列化数据量

---

## 九、完整使用流程

### 创建角色时初始化

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
// → Get(DynamicResourceType.CURRENT_HP) = 1000
// → Get(DynamicResourceType.CURRENT_MANA) = 100
```

### 每帧更新

```csharp
// System更新逻辑
public class StatsUpdateSystem : IUpdateSystem
{
    public void Update(float deltaTime)
    {
        foreach (var entity in _entities)
        {
            // 1. 更新Buff
            var buffComp = entity.GetComponent<BuffComponent>();
            buffComp.UpdateBuffs();
            
            // 2. 如果Buff变化，重新计算属性
            var derivedStats = entity.GetComponent<DerivedStatsComponent>();
            var baseStats = entity.GetComponent<BaseStatsComponent>();
            
            derivedStats.ClearModifiers();
            var buffModifiers = buffComp.GetAllModifiers();
            foreach (var kvp in buffModifiers)
            {
                foreach (var modifier in kvp.Value)
                {
                    derivedStats.AddModifier(kvp.Key, modifier);
                }
            }
            
            derivedStats.RecalculateIfDirty(baseStats);
        }
    }
}
```

---

**创建日期**: 2025-10-14  
**作者**: Astrum开发团队  
**状态**: 📝 设计完成，待实现

