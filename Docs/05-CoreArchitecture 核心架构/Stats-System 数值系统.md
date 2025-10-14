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

### 2.1 BaseStatsComponent（基础属性组件）

**职责**：存储实体的基础原始属性，来源于配置表

**数据来源**：
- 配置表的基础值（RoleBaseTable）
- 等级成长加成（RoleGrowthTable）
- **不包含**：Buff、装备等临时加成

**属性列表**：

```csharp
[MemoryPackable]
public partial class BaseStatsComponent : BaseComponent
{
    // ===== 基础战斗属性 =====
    
    /// <summary>基础攻击力</summary>
    public float BaseAttack { get; set; }
    
    /// <summary>基础防御力</summary>
    public float BaseDefense { get; set; }
    
    /// <summary>基础生命值上限</summary>
    public float BaseMaxHealth { get; set; }
    
    /// <summary>基础移动速度</summary>
    public float BaseSpeed { get; set; }
    
    // ===== 高级战斗属性 =====
    
    /// <summary>基础暴击率（0-1）</summary>
    public float BaseCritRate { get; set; } = 0.05f; // 默认5%
    
    /// <summary>基础暴击伤害倍率</summary>
    public float BaseCritDamage { get; set; } = 2.0f; // 默认200%
    
    /// <summary>基础命中率（0-1）</summary>
    public float BaseAccuracy { get; set; } = 0.95f; // 默认95%
    
    /// <summary>基础闪避率（0-1）</summary>
    public float BaseEvasion { get; set; } = 0.05f; // 默认5%
    
    /// <summary>基础格挡率（0-1）</summary>
    public float BaseBlockRate { get; set; } = 0.10f; // 默认10%
    
    /// <summary>基础格挡值（减免伤害）</summary>
    public float BaseBlockValue { get; set; } = 50f;
    
    // ===== 抗性属性 =====
    
    /// <summary>物理抗性（0-1，0.3表示减免30%物理伤害）</summary>
    public float PhysicalResistance { get; set; } = 0f;
    
    /// <summary>魔法抗性（0-1）</summary>
    public float MagicalResistance { get; set; } = 0f;
    
    // ===== 资源属性 =====
    
    /// <summary>基础法力值上限（如有）</summary>
    public float BaseMaxMana { get; set; } = 100f;
    
    /// <summary>法力恢复速度（每秒）</summary>
    public float ManaRegen { get; set; } = 5f;
    
    /// <summary>生命恢复速度（每秒）</summary>
    public float HealthRegen { get; set; } = 1f;
}
```

**初始化方式**：
```csharp
public void InitializeFromConfig(int roleId, int level)
{
    // 1. 从配置表读取基础值
    var roleConfig = ConfigManager.Instance.Tables.TbRoleBaseTable.Get(roleId);
    BaseAttack = roleConfig.BaseAttack;
    BaseDefense = roleConfig.BaseDefense;
    BaseMaxHealth = roleConfig.BaseHealth;
    BaseSpeed = roleConfig.BaseSpeed;
    
    // 2. 应用等级成长
    var growthConfig = ConfigManager.Instance.Tables.TbRoleGrowthTable.GetByLevel(roleId, level);
    if (growthConfig != null && level > 1)
    {
        BaseAttack += growthConfig.LightAttackBonus * (level - 1);
        BaseDefense += growthConfig.DefenseBonus * (level - 1);
        BaseMaxHealth += growthConfig.HealthBonus * (level - 1);
        BaseSpeed += growthConfig.SpeedBonus * (level - 1);
    }
}
```

---

### 2.2 DerivedStatsComponent（派生属性组件）

**职责**：存储经过修饰器计算后的最终属性值

**数据来源**：
- BaseStats（基础）
- Buff加成（临时）
- 装备加成（装备）
- 技能被动（永久）

**属性列表**：

```csharp
[MemoryPackable]
public partial class DerivedStatsComponent : BaseComponent
{
    // ===== 最终战斗属性 =====
    
    /// <summary>最终攻击力 = Base * (1 + %加成) + 固定加成</summary>
    public float FinalAttack { get; set; }
    
    /// <summary>最终防御力</summary>
    public float FinalDefense { get; set; }
    
    /// <summary>最终生命上限</summary>
    public float FinalMaxHealth { get; set; }
    
    /// <summary>最终移动速度</summary>
    public float FinalSpeed { get; set; }
    
    /// <summary>最终暴击率（0-1）</summary>
    public float FinalCritRate { get; set; }
    
    /// <summary>最终暴击伤害倍率</summary>
    public float FinalCritDamage { get; set; }
    
    /// <summary>最终命中率（0-1）</summary>
    public float FinalAccuracy { get; set; }
    
    /// <summary>最终闪避率（0-1）</summary>
    public float FinalEvasion { get; set; }
    
    /// <summary>最终格挡率（0-1）</summary>
    public float FinalBlockRate { get; set; }
    
    /// <summary>最终格挡值</summary>
    public float FinalBlockValue { get; set; }
    
    /// <summary>最终物理抗性（0-1）</summary>
    public float FinalPhysicalResistance { get; set; }
    
    /// <summary>最终魔法抗性（0-1）</summary>
    public float FinalMagicalResistance { get; set; }
    
    /// <summary>最终法力上限</summary>
    public float FinalMaxMana { get; set; }
    
    // ===== 修饰器记录（用于重新计算）=====
    
    /// <summary>攻击力加成列表（百分比）</summary>
    [MemoryPackIgnore]
    public List<StatModifier> AttackModifiers { get; set; } = new List<StatModifier>();
    
    /// <summary>防御力加成列表</summary>
    [MemoryPackIgnore]
    public List<StatModifier> DefenseModifiers { get; set; } = new List<StatModifier>();
    
    /// <summary>生命值加成列表</summary>
    [MemoryPackIgnore]
    public List<StatModifier> HealthModifiers { get; set; } = new List<StatModifier>();
    
    // ... 其他修饰器列表
    
    /// <summary>重新计算所有派生属性</summary>
    public void RecalculateAll(BaseStatsComponent baseStats)
    {
        FinalAttack = CalculateFinalStat(baseStats.BaseAttack, AttackModifiers);
        FinalDefense = CalculateFinalStat(baseStats.BaseDefense, DefenseModifiers);
        FinalMaxHealth = CalculateFinalStat(baseStats.BaseMaxHealth, HealthModifiers);
        FinalSpeed = CalculateFinalStat(baseStats.BaseSpeed, SpeedModifiers);
        FinalCritRate = CalculateFinalStat(baseStats.BaseCritRate, CritRateModifiers);
        FinalCritDamage = CalculateFinalStat(baseStats.BaseCritDamage, CritDamageModifiers);
        // ... 其他属性
    }
    
    /// <summary>计算单个属性的最终值</summary>
    private float CalculateFinalStat(float baseValue, List<StatModifier> modifiers)
    {
        float flatBonus = 0f;        // 固定加成
        float percentBonus = 0f;     // 百分比加成
        float finalMultiplier = 1f;  // 最终乘数
        
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
                    finalMultiplier *= (1f + mod.Value);
                    break;
            }
        }
        
        // 计算顺序：(基础 + 固定) * (1 + 百分比) * 最终乘数
        return (baseValue + flatBonus) * (1f + percentBonus) * finalMultiplier;
    }
}

/// <summary>属性修饰器</summary>
public class StatModifier
{
    public int SourceId;           // 来源ID（Buff ID、装备ID等）
    public ModifierType Type;      // 修饰器类型
    public float Value;            // 数值
    public int Priority;           // 优先级
}

/// <summary>修饰器类型</summary>
public enum ModifierType
{
    Flat = 1,           // 固定值加成（+50攻击）
    Percent = 2,        // 百分比加成（+20%攻击）
    FinalMultiplier = 3 // 最终乘数（1.5倍伤害）
}
```

---

### 2.3 DynamicStatsComponent（动态属性组件）

**职责**：存储战斗中实时变化的数值（当前值、临时资源等）

**特点**：
- 每帧可能变化
- 受伤害、治疗、消耗影响
- 有上限约束（来自DerivedStats）

**属性列表**：

```csharp
[MemoryPackable]
public partial class DynamicStatsComponent : BaseComponent
{
    // ===== 核心资源 =====
    
    /// <summary>当前生命值</summary>
    public float CurrentHealth { get; set; }
    
    /// <summary>当前法力值</summary>
    public float CurrentMana { get; set; }
    
    // ===== 战斗资源 =====
    
    /// <summary>当前能量值（0-100，用于技能消耗）</summary>
    public float CurrentEnergy { get; set; } = 0f;
    
    /// <summary>当前怒气值（0-100，受击/攻击获得）</summary>
    public float CurrentRage { get; set; } = 0f;
    
    /// <summary>当前连击数</summary>
    public int ComboCount { get; set; } = 0;
    
    // ===== 护盾和临时防护 =====
    
    /// <summary>护盾值（优先于生命值承受伤害）</summary>
    public float Shield { get; set; } = 0f;
    
    /// <summary>临时无敌时间（帧数）</summary>
    public int InvincibleFrames { get; set; } = 0;
    
    // ===== 控制状态计时 =====
    
    /// <summary>硬直剩余帧数</summary>
    public int StunFrames { get; set; } = 0;
    
    /// <summary>冰冻剩余帧数</summary>
    public int FreezeFrames { get; set; } = 0;
    
    /// <summary>击飞剩余帧数</summary>
    public int KnockbackFrames { get; set; } = 0;
    
    // ===== 辅助方法 =====
    
    /// <summary>扣除生命值（考虑护盾）</summary>
    public float TakeDamage(float damage, DerivedStatsComponent derivedStats)
    {
        float remainingDamage = damage;
        
        // 1. 先扣除护盾
        if (Shield > 0)
        {
            float shieldDamage = Math.Min(Shield, remainingDamage);
            Shield -= shieldDamage;
            remainingDamage -= shieldDamage;
        }
        
        // 2. 扣除生命值
        if (remainingDamage > 0)
        {
            float actualDamage = Math.Min(CurrentHealth, remainingDamage);
            CurrentHealth -= actualDamage;
            return actualDamage; // 返回实际受到的生命伤害
        }
        
        return 0f;
    }
    
    /// <summary>恢复生命值（不超过上限）</summary>
    public float Heal(float amount, DerivedStatsComponent derivedStats)
    {
        float maxHeal = derivedStats.FinalMaxHealth - CurrentHealth;
        float actualHeal = Math.Min(amount, maxHeal);
        CurrentHealth += actualHeal;
        return actualHeal;
    }
    
    /// <summary>消耗法力</summary>
    public bool ConsumeMana(float amount, DerivedStatsComponent derivedStats)
    {
        if (CurrentMana < amount)
            return false;
        
        CurrentMana -= amount;
        return true;
    }
    
    /// <summary>增加能量</summary>
    public void AddEnergy(float amount)
    {
        CurrentEnergy = Math.Min(100f, CurrentEnergy + amount);
    }
    
    /// <summary>增加怒气</summary>
    public void AddRage(float amount)
    {
        CurrentRage = Math.Min(100f, CurrentRage + amount);
    }
}
```

---

### 2.4 BuffComponent（Buff组件）

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
    public List<StatModifier> GetAllModifiers()
    {
        var modifiers = new List<StatModifier>();
        
        foreach (var buff in Buffs)
        {
            // 从Buff配置表读取修饰器
            var buffConfig = ConfigManager.Instance.Tables.TbBuffTable.Get(buff.BuffId);
            if (buffConfig != null)
            {
                modifiers.AddRange(ParseBuffModifiers(buffConfig, buff.StackCount));
            }
        }
        
        return modifiers;
    }
}

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

### 2.5 StateComponent（状态组件）

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

### 2.6 LevelComponent（等级组件）

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

### 2.7 GrowthComponent（成长组件）

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
        
        // 2. 增加对应属性的分配点
        var baseStats = owner.GetComponent<BaseStatsComponent>();
        
        switch (statType)
        {
            case StatType.Attack:
                AllocatedAttackPoints++;
                baseStats.BaseAttack += 2f; // 每点+2攻击
                break;
            case StatType.Defense:
                AllocatedDefensePoints++;
                baseStats.BaseDefense += 2f;
                break;
            case StatType.Health:
                AllocatedHealthPoints++;
                baseStats.BaseMaxHealth += 20f; // 每点+20生命
                break;
            case StatType.Speed:
                AllocatedSpeedPoints++;
                baseStats.BaseSpeed += 0.1f;
                break;
        }
        
        // 3. 重新计算派生属性
        var derivedStats = owner.GetComponent<DerivedStatsComponent>();
        derivedStats.RecalculateAll(baseStats);
        
        return true;
    }
}

public enum StatType
{
    Attack,
    Defense,
    Health,
    Speed
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
##type,int,string,int,int,bool,int,string,float,int
##desc,BuffID,Buff名称,类型(1=Buff/2=Debuff),持续帧数,可叠加,最大层数,属性修饰器,持续伤害,触发间隔

示例数据：
5001,力量祝福,1,600,true,3,Attack:Percent:0.2;Speed:Flat:1.0,0,0
5002,燃烧,2,300,false,1,PhysicalRes:Percent:-0.1,10.0,30
5003,护盾,1,180,false,1,Shield:Flat:200,0,0
5004,冰冻,2,120,false,1,Speed:Percent:-0.5,0,0
```

**字段说明**：
- `buffType`: 1=增益Buff, 2=减益Debuff
- `duration`: 持续帧数（300帧 = 15秒 @ 20FPS）
- `modifiers`: 格式 `属性:类型:数值;属性:类型:数值`
  - 例：`Attack:Percent:0.2` = 攻击力+20%
  - 例：`Defense:Flat:50` = 防御力+50
- `tickDamage`: 持续伤害（如燃烧、中毒）
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
    
    // 3. 计算基础伤害
    float baseDamage = CalculateBaseDamage(casterDerived.FinalAttack, effectConfig);
    
    // 4. 命中判定
    if (!CheckHit(casterDerived.FinalAccuracy, targetDerived.FinalEvasion))
        return new DamageResult { FinalDamage = 0, IsCritical = false, IsMiss = true };
    
    // 5. 格挡判定
    bool isBlocked = CheckBlock(targetDerived.FinalBlockRate);
    if (isBlocked)
    {
        baseDamage = Math.Max(0, baseDamage - targetDerived.FinalBlockValue);
    }
    
    // 6. 暴击判定
    bool isCritical = CheckCritical(casterDerived.FinalCritRate);
    if (isCritical)
    {
        baseDamage *= casterDerived.FinalCritDamage;
    }
    
    // 7. 应用防御减免
    float afterDefense = ApplyDefense(baseDamage, targetDerived.FinalDefense, effectConfig.DamageType);
    
    // 8. 应用抗性
    float afterResistance = ApplyResistance(afterDefense, targetDerived, effectConfig.DamageType);
    
    // 9. 应用随机浮动（±5%）
    float finalDamage = ApplyRandomVariance(afterResistance);
    
    // 10. 确保非负
    finalDamage = Math.Max(0, finalDamage);
    
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

```
基础伤害 = 攻击力 × 技能倍率

示例：
攻击力 = 100
技能倍率 = 1.5（配置表中 effectValue = 150）
基础伤害 = 100 × 1.5 = 150
```

### 5.3 防御减免公式

#### 物理/魔法伤害
```
减伤百分比 = 防御 / (防御 + 100)
最终伤害 = 基础伤害 × (1 - 减伤百分比)

示例：
基础伤害 = 150
防御力 = 50
减伤 = 50 / (50 + 100) = 33.3%
最终伤害 = 150 × (1 - 0.333) = 100
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

```
最终伤害 = 伤害 × (1 - 对应抗性)

示例：
伤害 = 100
物理抗性 = 0.2（20%）
最终伤害 = 100 × (1 - 0.2) = 80
```

**抗性来源**：
- Buff/Debuff
- 装备
- 被动技能
- 种族特性

### 5.5 暴击计算

```
是否暴击 = Random(0, 1) < 暴击率
暴击伤害 = 基础伤害 × 暴击伤害倍率

示例：
基础伤害 = 150
暴击率 = 0.25（25%）
暴击伤害倍率 = 2.5（250%）

→ 25%概率：150 × 2.5 = 375
→ 75%概率：150
```

### 5.6 命中/闪避判定

```
命中概率 = 施法者命中率 - 目标闪避率
是否命中 = Random(0, 1) < 命中概率

示例：
施法者命中率 = 0.95（95%）
目标闪避率 = 0.15（15%）
最终命中率 = 0.95 - 0.15 = 0.80（80%）
```

**极限约束**：
- 最低命中率：10%（即使目标闪避100%）
- 最高命中率：100%（无法超过）

### 5.7 格挡判定

```
是否格挡 = Random(0, 1) < 格挡率
格挡后伤害 = Max(0, 原伤害 - 格挡值)

示例：
原伤害 = 150
格挡率 = 0.30（30%）
格挡值 = 80

→ 30%概率格挡成功：Max(0, 150 - 80) = 70
→ 70%概率正常受伤：150
```

---

## 六、完整伤害计算示例

### 场景：骑士攻击法师

**施法者（骑士 Lv5）**：
```
BaseStats:
  BaseAttack = 80 + 8×4 = 112（基础80，等级加成8/级）
  BaseCritRate = 0.05
  BaseCritDamage = 2.0
  BaseAccuracy = 0.95

Buffs:
  [力量祝福] Attack +20% (Percent)
  [狂战士] CritDamage ×1.5 (FinalMultiplier)

DerivedStats:
  FinalAttack = 112 × (1 + 0.2) = 134.4
  FinalCritRate = 0.05
  FinalCritDamage = 2.0 × 1.5 = 3.0
  FinalAccuracy = 0.95
```

**目标（法师 Lv3）**：
```
BaseStats:
  BaseDefense = 40 + 4×2 = 48
  BaseEvasion = 0.10
  BaseBlockRate = 0.05
  BaseBlockValue = 30
  PhysicalResistance = 0.0
  MagicalResistance = 0.3

DerivedStats:
  FinalDefense = 48
  FinalEvasion = 0.10
  FinalBlockRate = 0.05
  FinalBlockValue = 30
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
# BuffTable
buffId,buffName,buffType,duration,stackable,maxStack,modifiers,tickDamage,tickInterval

# 增益Buff
5001,力量祝福,1,600,true,3,Attack:Percent:0.2,0,0
5002,极速,1,300,false,1,Speed:Percent:0.5,0,0
5003,护盾,1,180,false,1,Shield:Flat:200,0,0
5004,狂暴,1,600,true,5,Attack:Percent:0.1;CritRate:Flat:0.05,0,0

# 减益Debuff
6001,燃烧,2,300,false,1,PhysicalRes:Percent:-0.1,10.0,30
6002,冰冻,2,120,false,1,Speed:Percent:-0.8,0,0
6003,虚弱,2,600,true,3,Attack:Percent:-0.15,0,0
6004,破甲,2,300,false,1,Defense:Percent:-0.3,0,0
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
##var,id,name,...,baseCritRate,baseCritDamage,baseAccuracy,baseEvasion,baseBlockRate,baseBlockValue,physicalRes,magicalRes,baseMaxMana,manaRegen,healthRegen
##type,int,string,...,float,float,float,float,float,float,float,float,float,float,float
##desc,角色ID,角色名称,...,基础暴击率,基础暴击伤害,基础命中率,基础闪避率,基础格挡率,基础格挡值,物理抗性,魔法抗性,基础法力上限,法力回复,生命回复

# 示例数据（骑士）
1001,骑士,...,0.05,2.0,0.95,0.05,0.15,60,0.1,0.0,100,5.0,2.0

# 示例数据（刺客 - 高暴击低防御）
1005,刺客,...,0.25,2.5,0.98,0.20,0.05,30,0.0,0.0,80,4.0,1.0

# 示例数据（法师 - 高魔抗低物抗）
1003,法师,...,0.10,2.2,0.90,0.08,0.08,40,0.0,0.3,200,10.0,1.5
```

### 10.2 RoleGrowthTable优化

**优化字段**：

```csv
##var,id,roleId,level,requiredExp,attackBonus,defenseBonus,healthBonus,speedBonus,critRateBonus,critDamageBonus,unlockSkillId,skillPoint
##type,int,int,int,int,float,float,float,float,float,float,int,int
##desc,ID,角色ID,等级,升级所需经验,攻击力加成,防御力加成,生命值加成,速度加成,暴击率加成,暴击伤害加成,解锁技能ID,技能点

# 示例数据（骑士 Lv2）
2,1001,2,1000,8.0,8.0,100.0,0.1,0.002,0.05,0,1

# 每级成长：
# - 攻击 +8
# - 防御 +8
# - 生命 +100
# - 速度 +0.1
# - 暴击率 +0.2%
# - 暴击伤害 +0.05（+5%）
```

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

**解决方案**：
```csharp
// 方案1：使用TrueSync的FP类型（定点数）
public FP BaseAttack { get; set; }

// 方案2：关键计算使用整数（推荐）
public int BaseAttack { get; set; }  // 攻击力用整数
public int BaseCritRate { get; set; } // 暴击率用万分比（500 = 5%）
```

**建议**：
- 基础属性使用**整数**或**FP**
- 百分比属性用**万分比整数**（500 = 5.00%）
- 最终计算时转换为float，但结果四舍五入

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

### 13.3 序列化注意事项

**MemoryPack序列化**：
- ✅ 基础数据字段：正常序列化
- ❌ 修饰器列表：标记 `[MemoryPackIgnore]`（运行时计算）
- ❌ 计算结果缓存：标记 `[MemoryPackIgnore]`

```csharp
[MemoryPackable]
public partial class DerivedStatsComponent
{
    public float FinalAttack { get; set; } // ✅ 序列化
    
    [MemoryPackIgnore]
    public List<StatModifier> AttackModifiers { get; set; } // ❌ 不序列化
}
```

---

## 十四、使用示例

### 14.1 创建角色时初始化

```csharp
// 创建角色Entity
var playerEntity = EntityFactory.CreateRole(roleId: 1001, level: 1);

// 组件已自动添加和初始化：
var baseStats = playerEntity.GetComponent<BaseStatsComponent>();
// → BaseAttack = 80, BaseDefense = 80, BaseMaxHealth = 1000

var derivedStats = playerEntity.GetComponent<DerivedStatsComponent>();
derivedStats.RecalculateAll(baseStats);
// → FinalAttack = 80, FinalMaxHealth = 1000

var dynamicStats = playerEntity.GetComponent<DynamicStatsComponent>();
dynamicStats.CurrentHealth = derivedStats.FinalMaxHealth;
// → CurrentHealth = 1000
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

// 清空旧的修饰器，从Buff重新收集
derivedStats.AttackModifiers.Clear();
derivedStats.AttackModifiers.AddRange(buffComp.GetAttackModifiers());

// 重算
derivedStats.RecalculateAll(baseStats);

// 结果：
// BaseAttack = 100
// AttackModifiers = [+20% from 力量祝福]
// FinalAttack = 100 × 1.2 = 120
```

### 14.3 伤害结算

```csharp
// 施放技能，触发伤害效果
var effectConfig = ConfigManager.Instance.Tables.TbSkillEffectTable.Get(4001);

var damageResult = DamageCalculator.Calculate(
    caster: attackerEntity,
    target: targetEntity,
    effectConfig: effectConfig
);

if (damageResult.IsMiss)
{
    // 未命中，显示MISS
    return;
}

// 应用伤害
var targetDynamic = targetEntity.GetComponent<DynamicStatsComponent>();
var targetDerived = targetEntity.GetComponent<DerivedStatsComponent>();

float actualDamage = targetDynamic.TakeDamage(damageResult.FinalDamage, targetDerived);

// 伤害反馈
if (damageResult.IsCritical)
{
    ShowCriticalDamageText(actualDamage); // 💥暴击
}
else if (damageResult.IsBlocked)
{
    ShowBlockedDamageText(actualDamage); // 🛡️格挡
}
else
{
    ShowNormalDamageText(actualDamage);
}

// 检查死亡
if (targetDynamic.CurrentHealth <= 0)
{
    var stateComp = targetEntity.GetComponent<StateComponent>();
    stateComp.IsDead = true;
    OnEntityDied(targetEntity);
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
├─ RoleBaseTable(1001) → Attack=80, Defense=80, HP=1000
├─ RoleGrowthTable(Lv5) → AttackBonus=8/级
└─ BuffTable(5001) → Attack +20%

    ↓ InitializeFromConfig()
    
BaseStatsComponent
├─ BaseAttack = 80 + 8×4 = 112（基础+成长）
├─ BaseDefense = 80 + 8×4 = 112
└─ BaseMaxHealth = 1000 + 100×4 = 1400

    ↓ AddBuff(5001) → 收集修饰器
    
DerivedStatsComponent.AttackModifiers
└─ [Buff 5001: Percent +0.2]

    ↓ RecalculateAll()
    
DerivedStatsComponent
├─ FinalAttack = 112 × 1.2 = 134.4
├─ FinalDefense = 112
└─ FinalMaxHealth = 1400

    ↓ 初始化战斗资源
    
DynamicStatsComponent
├─ CurrentHealth = 1400（满血）
├─ CurrentMana = 100
└─ CurrentEnergy = 0

    ↓ 战斗中...
    
受到150%攻击力的技能伤害
    ↓ DamageCalculator.Calculate()
    
基础伤害 = 敌方攻击100 × 1.5 = 150
    ↓ 防御减免
    
减伤后 = 150 × (1 - 112/212) = 70.8
    ↓ DynamicStats.TakeDamage()
    
CurrentHealth = 1400 - 70.8 = 1329.2
```

---

**创建日期**: 2025-10-14  
**作者**: Astrum开发团队  
**状态**: 📝 设计完成，待实现

