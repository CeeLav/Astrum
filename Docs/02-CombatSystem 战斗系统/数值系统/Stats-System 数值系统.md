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

## 二、核心数据结构

### 2.1 StatType 枚举（属性类型定义）

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

### 2.2 Stats 通用属性容器

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

## 三、文档索引

本数值系统文档已拆分为多个模块，便于查阅和维护：

### 📚 子文档列表

| 文档 | 内容 | 说明 |
|------|------|------|
| **[Stats-Components 属性组件](./Stats-Components%20属性组件.md)** | 7个核心组件详细设计 | BaseStats, DerivedStats, DynamicStats, Buff, State, Level, Growth |
| **[Damage-Calculation 伤害计算](./Damage-Calculation%20伤害计算.md)** | 完整伤害计算公式 | 基础伤害、防御、抗性、暴击、命中、格挡判定 |
| **[Stats-Config-Tables 配置表](./Stats-Config-Tables%20配置表.md)** | 配置表设计与扩展 | RoleBaseTable, RoleGrowthTable, BuffTable, 数值规则 |

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
    - Set(DynamicResourceType.CURRENT_HP, DerivedStats.Get(StatType.HP))
    - Set(DynamicResourceType.CURRENT_MANA, DerivedStats.Get(StatType.MAX_MANA))
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
    - 扣除 CURRENT_HP 资源
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

## 五、与现有系统的集成

### 5.1 替换HealthComponent

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
        get => (int)Entity.GetComponent<DynamicStatsComponent>().Get(DynamicResourceType.CURRENT_HP);
        set => Entity.GetComponent<DynamicStatsComponent>().Set(DynamicResourceType.CURRENT_HP, (FP)value);
    }
    
    public int MaxHealth
    {
        get => (int)Entity.GetComponent<DerivedStatsComponent>().Get(StatType.HP);
    }
}

// 方式2：直接删除HealthComponent，全面使用新系统
```

### 5.2 DamageCalculator集成

**替换现有的写死数值**：
```csharp
// 旧代码（写死）
private static float GetEntityAttack(Entity entity)
{
    return 100f; // TODO
}

// 新代码（从属性系统读取）
private static FP GetEntityAttack(Entity entity)
{
    var derivedStats = entity.GetComponent<DerivedStatsComponent>();
    return derivedStats?.Get(StatType.ATK) ?? FP.Zero;
}
```

### 5.3 EntityFactory集成

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
    dynamicStats.InitializeResources(derivedStats);
    
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

## 六、开发优先级

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

## 七、验收标准

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
├─ Get(CURRENT_HP) = 1400（满血）
├─ Get(CURRENT_MANA) = 100
└─ Get(ENERGY) = 0

    ↓ 战斗中...
    
受到150%攻击力的技能伤害
    ↓ DamageCalculator.Calculate()
    
基础伤害 = 敌方ATK(100) × 1.5 = 150
    ↓ 防御减免
    
减伤后 = 150 × (1 - 112/212) = 70.8
    ↓ DynamicStats.TakeDamage()
    
Set(CURRENT_HP, 1400 - 70.8) = 1329.2
```

---

**创建日期**: 2025-10-14  
**作者**: Astrum开发团队  
**状态**: 📝 设计完成，待实现
