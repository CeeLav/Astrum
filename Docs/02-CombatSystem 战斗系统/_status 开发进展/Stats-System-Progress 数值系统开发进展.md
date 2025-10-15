# 数值系统 - 开发进展

**项目**: 数值系统（属性、战斗、成长、Buff）  
**创建日期**: 2025-10-14  
**最后更新**: 2025-10-14  
**版本**: v0.1.0 (策划案完成)

---

## 📋 目录

1. [开发状态总览](#开发状态总览)
2. [依赖系统状态](#依赖系统状态)
3. [开发计划](#开发计划)
4. [待完成功能](#待完成功能)
5. [文件清单](#文件清单)
6. [技术要点](#技术要点)
7. [验收标准](#验收标准)

---

## 开发状态总览

### 当前版本
- **版本号**: v0.1.0 (策划案完成，代码未开始)
- **编译状态**: ⏳ 未开始
- **测试状态**: ⏳ 未开始
- **功能完成度**: 10% (仅策划案完成)

### 阶段划分

#### ✅ **Phase 0**: 策划案编写 - **已完成** (2025-10-14)
- ✅ 系统概述与设计理念
- ✅ 三层属性架构设计
- ✅ 7个核心组件详细设计
  - ✅ BaseStatsComponent
  - ✅ DerivedStatsComponent
  - ✅ DynamicStatsComponent
  - ✅ BuffComponent
  - ✅ StateComponent
  - ✅ LevelComponent
  - ✅ GrowthComponent
- ✅ 完整伤害计算公式
- ✅ Buff系统设计
- ✅ 配置表扩展方案
- ✅ 文档拆分优化（4个子文档）

#### ⏳ **Phase 1**: 配置表扩展与代码生成 - **计划中** 🔥
**预计工作量**: 4-6小时

**重要性**: ⭐⭐⭐ **必须先完成！其他阶段依赖配置表生成的代码**

- ⏳ 扩展 `RoleBaseTable.csv`
  - 添加高级属性字段（暴击率、暴击伤害、命中、闪避、格挡、抗性等）
  - 所有字段使用 `int` 类型
  - 更新现有角色数据（骑士、法师、重锤者等）
- ⏳ 优化 `RoleGrowthTable.csv`
  - 统一成长字段命名（attackBonus 代替 lightAttackBonus/heavyAttackBonus）
  - 添加暴击成长字段
- ⏳ 创建 `BuffTable.csv`
  - 定义表结构（buffId, buffName, buffType, duration, stackable, maxStack, modifiers, tickDamage, tickInterval）
  - 添加示例数据（力量祝福、极速、燃烧、冰冻等 10-20个）
- ⏳ 优化 `SkillEffectTable.csv`
  - 添加 damageType 字段（1=物理/2=魔法/3=真实）
  - 添加 scalingStat 字段
  - 添加 scalingRatio 字段
- ⏳ **运行 Luban 生成配置表代码**
  - 生成 C# 访问类
  - 验证字段类型正确
  - 测试配置表加载

**依赖**:
- Luban 配置表工具

**输出**:
- 生成的配置表访问代码（TbRoleBaseTable, TbRoleGrowthTable, TbBuffTable 等）
- 可直接在代码中使用 `roleConfig.BaseCritRate` 等字段

#### ⏳ **Phase 2**: 核心数据结构实现 - **计划中**
**预计工作量**: 6-8小时

- ⏳ 创建 `StatType` 枚举
- ⏳ 创建 `Stats` 通用属性容器
- ⏳ 创建 `DynamicResourceType` 枚举
- ⏳ 创建 `StateType` 枚举
- ⏳ 创建 `ModifierType` 枚举
- ⏳ 创建 `DamageType` 枚举

**文件位置**: `AstrumProj/Assets/Script/LogicCore/Combat/Stats/`

**依赖**:
- Phase 1 完成（配置表字段确定后，枚举才能对应）

#### ⏳ **Phase 3**: 基础属性组件实现 - **计划中**
**预计工作量**: 8-10小时

- ⏳ `BaseStatsComponent` 实现
  - ⏳ 配置表读取逻辑
  - ⏳ 等级成长计算
  - ⏳ 自由加点逻辑
  - ⏳ int → FP 转换（除以1000）
- ⏳ `DerivedStatsComponent` 实现
  - ⏳ 修饰器管理
  - ⏳ 属性计算公式
  - ⏳ 脏标记机制
  - ⏳ RecalculateAll() 方法
- ⏳ `DynamicStatsComponent` 实现
  - ⏳ 资源管理（HP/MP/Energy/Rage）
  - ⏳ TakeDamage() 方法（护盾逻辑）
  - ⏳ Heal() 方法
  - ⏳ ConsumeMana() 方法
  - ⏳ InitializeResources() 方法

**依赖**:
- Phase 1 完成（配置表生成后才能读取）
- ConfigManager（配置表管理）
- TrueSync.FP（定点数库）

#### ⏳ **Phase 4**: Buff系统实现 - **计划中**
**预计工作量**: 10-12小时

- ⏳ `BuffInstance` 数据结构
- ⏳ `BuffComponent` 实现
  - ⏳ Buff 添加/移除逻辑
  - ⏳ Buff 叠加逻辑
  - ⏳ Buff 持续时间管理
  - ⏳ UpdateBuffs() 每帧更新
  - ⏳ GetAllModifiers() 修饰器提取
  - ⏳ ParseBuffModifiers() 字符串解析
- ⏳ `StatModifier` 数据结构
- ⏳ Buff 配置表字符串解析（"ATK:Percent:200;SPD:Flat:1000"）

**依赖**:
- Phase 1 完成（BuffTable 配置表）
- DerivedStatsComponent

#### ⏳ **Phase 5**: 状态与成长系统 - **计划中**
**预计工作量**: 6-8小时

- ⏳ `StateComponent` 实现
  - ⏳ 状态字典管理
  - ⏳ CanMove/CanAttack/CanCastSkill 辅助方法
  - ⏳ CanTakeDamage 判断
- ⏳ `LevelComponent` 实现
  - ⏳ 经验获取逻辑
  - ⏳ 升级逻辑
  - ⏳ 满血满蓝恢复
  - ⏳ 升级时重算属性
- ⏳ `GrowthComponent` 实现
  - ⏳ 属性点分配逻辑
  - ⏳ AllocatePoint() 方法
  - ⏳ 分配后重算属性

**依赖**:
- Phase 1 完成（RoleGrowthTable 配置表）

#### ⏳ **Phase 6**: DamageCalculator 重构 - **计划中**
**预计工作量**: 8-10小时

- ⏳ 创建 `DamageResult` 数据结构（扩展）
  - ⏳ 添加 IsBlocked、IsMiss 字段
- ⏳ 重构 `DamageCalculator.Calculate()`
  - ⏳ 接入真实属性系统（DerivedStatsComponent）
  - ⏳ 实现命中判定（CheckHit）
  - ⏳ 实现格挡判定（CheckBlock）
  - ⏳ 实现暴击判定（CheckCritical）
  - ⏳ 实现防御减免（ApplyDefense）
  - ⏳ 实现抗性减免（ApplyResistance）
  - ⏳ 使用确定性随机数（TSRandom）
- ⏳ 移除硬编码数值
- ⏳ 添加详细日志

**依赖**:
- Phase 1 完成（配置表字段）
- DerivedStatsComponent
- StateComponent
- TrueSync.TSRandom

#### ⏳ **Phase 7**: 集成测试与优化 - **计划中**
**预计工作量**: 6-8小时

- ⏳ 单元测试编写
  - ⏳ Stats 容器测试
  - ⏳ BaseStats 初始化测试
  - ⏳ DerivedStats 计算测试
  - ⏳ Buff 叠加测试
  - ⏳ 伤害计算测试
- ⏳ 集成测试
  - ⏳ 完整战斗流程测试
  - ⏳ 升级流程测试
  - ⏳ 加点流程测试
- ⏳ 性能测试
  - ⏳ 100+ Entity 同时运行
  - ⏳ 属性计算耗时 < 0.1ms
  - ⏳ Buff更新耗时 < 0.05ms
- ⏳ 确定性验证
  - ⏳ 多客户端伤害计算一致性测试
  - ⏳ 随机数序列一致性测试

#### ⏳ **Phase 8**: 与现有系统集成 - **计划中**
**预计工作量**: 4-6小时

- ⏳ EntityFactory 集成
  - ⏳ 创建角色时自动添加数值组件
  - ⏳ 初始化流程
- ⏳ 替换旧的 HealthComponent
  - ⏳ 桥接模式或直接替换
- ⏳ EffectHandler 集成
  - ⏳ DamageEffectHandler 使用新计算器
  - ⏳ HealEffectHandler 使用 DynamicStats
  - ⏳ BuffEffectHandler 新增
- ⏳ UI 集成
  - ⏳ 属性面板显示
  - ⏳ Buff图标显示
  - ⏳ 经验条显示

---

## 依赖系统状态

### ✅ 已就绪的依赖

#### 1. 配置系统（Luban）
- **状态**: ✅ 就绪
- **完成度**: 100%
- **文档**: [Luban使用指南](../../04-EditorTools%20编辑器工具/Luban-Guide%20Luban使用指南.md)

**可用功能**:
- ✅ CSV表格解析
- ✅ 代码自动生成
- ✅ ConfigManager 集成
- ✅ 多语言支持

**现有配置表**:
- ✅ RoleBaseTable（需扩展）
- ✅ RoleGrowthTable（需优化）
- ✅ SkillEffectTable（需优化）
- ⏳ BuffTable（需新建）

#### 2. ECS系统（Entity-Component）
- **状态**: ✅ 就绪
- **完成度**: 100%
- **文档**: [ECC结构说明](../../05-CoreArchitecture%20核心架构/ECC-System%20ECC结构说明.md)

**可用功能**:
- ✅ Entity 基础类
- ✅ BaseComponent 基础组件
- ✅ GetComponent/AddComponent 访问
- ✅ ComponentTypeId 自动生成

#### 3. 序列化系统（MemoryPack）
- **状态**: ✅ 就绪
- **完成度**: 100%
- **文档**: [序列化最佳实践](../../05-CoreArchitecture%20核心架构/Serialization-Best-Practices%20序列化最佳实践.md)

**可用功能**:
- ✅ [MemoryPackable] 自动序列化
- ✅ [MemoryPackIgnore] 忽略字段
- ✅ Dictionary 序列化支持
- ✅ 回滚系统支持

**注意事项**:
- ⚠️ 修饰器列表不序列化（运行时计算）
- ⚠️ 脏标记不序列化

#### 4. TrueSync（定点数库）
- **状态**: ✅ 就绪
- **完成度**: 100%

**可用功能**:
- ✅ TrueSync.FP 定点数类型
- ✅ FPMath 数学运算
- ✅ TSRandom 确定性随机数
- ✅ 隐式类型转换（int → FP）

**使用原则**:
- ✅ 所有运行时数值使用 FP
- ✅ 配置表使用 int（小数*1000）
- ✅ UI显示时转换为 float

### ⏳ 待完成的依赖

#### 1. BuffTable 配置表
- **状态**: ⏳ 未创建
- **工作量**: 2-3小时
- **优先级**: P1（Phase 3 需要）

**待完成内容**:
- ⏳ 定义表结构（buffId, buffName, buffType, duration, stackable, maxStack, modifiers, tickDamage, tickInterval）
- ⏳ 添加示例数据（力量祝福、极速、燃烧、冰冻等）
- ⏳ 修饰器字符串格式验证

#### 2. MovementComponent
- **状态**: ⏳ 未实现
- **工作量**: 待评估
- **优先级**: P2（击退效果需要）

**影响范围**:
- ⏳ KnockbackEffectHandler

---

## 开发计划

### 总体预估
- **总工作量**: 48-60小时（约6-8个工作日）
- **开始时间**: 待定
- **预计完成时间**: 待定

### 里程碑

| 里程碑 | 内容 | 预计完成时间 | 状态 |
|-------|------|------------|------|
| M1 | Phase 1 完成（配置表扩展与代码生成）🔥 | 待定 | ⏳ |
| M2 | Phase 2-3 完成（核心数据结构和基础组件） | 待定 | ⏳ |
| M3 | Phase 4-5 完成（Buff和状态系统） | 待定 | ⏳ |
| M4 | Phase 6-7 完成（伤害计算和测试） | 待定 | ⏳ |
| M5 | Phase 8 完成（系统集成） | 待定 | ⏳ |

### 优先级排序

#### P0（核心基础）
1. ✅ BaseStatsComponent
2. ✅ DerivedStatsComponent
3. ✅ DynamicStatsComponent
4. ✅ 重构 DamageCalculator
5. ✅ 扩展 RoleBaseTable

#### P1（重要功能）
6. ✅ BuffComponent
7. ✅ StateComponent
8. ✅ 创建 BuffTable
9. ✅ Buff修饰器计算

#### P2（完善系统）
10. ✅ LevelComponent
11. ✅ GrowthComponent
12. ✅ 升级逻辑
13. ✅ 自由加点

---

## 待完成功能

### Phase 1: 配置表扩展与代码生成 🔥

#### RoleBaseTable 扩展字段
```csv
##var,id,name,...,baseAttack,baseDefense,baseHealth,baseSpeed,baseCritRate,baseCritDamage,baseAccuracy,baseEvasion,baseBlockRate,baseBlockValue,physicalRes,magicalRes,baseMaxMana,manaRegen,healthRegen
##type,int,string,...,int,int,int,int,int,int,int,int,int,int,int,int,int,int,int
##desc,角色ID,角色名称,...,基础攻击力,基础防御力,基础生命值,基础移动速度*1000,基础暴击率*1000,基础暴击伤害*1000,基础命中率*1000,基础闪避率*1000,基础格挡率*1000,基础格挡值,物理抗性*1000,魔法抗性*1000,基础法力上限,法力回复*1000,生命回复*1000

# 示例数据（骑士）
1001,骑士,...,80,80,1000,10000,50,2000,950,50,150,60,100,0,100,5000,2000
```

#### RoleGrowthTable 优化字段
```csv
##var,id,roleId,level,requiredExp,attackBonus,defenseBonus,healthBonus,speedBonus,critRateBonus,critDamageBonus,unlockSkillId,skillPoint
##type,int,int,int,int,int,int,int,int,int,int,int,int
##desc,ID,角色ID,等级,升级所需经验,攻击力加成,防御力加成,生命值加成,速度加成*1000,暴击率加成*1000,暴击伤害加成*1000,解锁技能ID,技能点

# 示例数据（骑士 Lv2）
2,1001,2,1000,8,8,100,100,2,50,0,1
```

#### BuffTable 新建
```csv
##var,buffId,buffName,buffType,duration,stackable,maxStack,modifiers,tickDamage,tickInterval
##type,int,string,int,int,bool,int,string,int,int
##desc,BuffID,Buff名称,类型(1=Buff/2=Debuff),持续帧数,可叠加,最大层数,属性修饰器,持续伤害*1000,触发间隔帧数

# 示例数据
5001,力量祝福,1,600,true,3,ATK:Percent:200;SPD:Flat:1000,0,0
5002,极速,1,300,false,1,SPD:Percent:500,0,0
6001,燃烧,2,300,false,1,PHYSICAL_RES:Percent:-100,10000,30
6002,冰冻,2,120,false,1,SPD:Percent:-800,0,0
```

#### SkillEffectTable 优化
```csv
# 新增字段
damageType,           # int, 1=物理/2=魔法/3=真实
scalingStat,          # int, 缩放属性(1=攻击/2=防御/3=生命)
scalingRatio          # int, 缩放比例*1000
```

### Phase 2: 核心数据结构

#### 枚举定义
```csharp
// StatType.cs
public enum StatType
{
    HP = 1, ATK = 2, DEF = 3, SPD = 4,
    CRIT_RATE = 10, CRIT_DMG = 11, ACCURACY = 12, EVASION = 13,
    BLOCK_RATE = 14, BLOCK_VALUE = 15,
    PHYSICAL_RES = 20, MAGICAL_RES = 21,
    // ... 更多属性
}

// DynamicResourceType.cs
public enum DynamicResourceType
{
    CURRENT_HP = 1, CURRENT_MANA = 2,
    ENERGY = 10, RAGE = 11, COMBO = 12,
    SHIELD = 20, INVINCIBLE_FRAMES = 21,
    // ... 更多资源
}

// StateType.cs
public enum StateType
{
    STUNNED = 1, FROZEN = 2, KNOCKED_BACK = 3,
    SILENCED = 4, DISARMED = 5,
    INVINCIBLE = 10, INVISIBLE = 11, // ... 更多状态
}

// ModifierType.cs
public enum ModifierType
{
    Flat = 1,           // 固定值加成
    Percent = 2,        // 百分比加成
    FinalMultiplier = 3 // 最终乘数
}

// DamageType.cs
public enum DamageType
{
    Physical = 1, // 物理伤害
    Magical = 2,  // 魔法伤害
    True = 3      // 真实伤害
}
```

#### Stats 通用容器
```csharp
// Stats.cs
[MemoryPackable]
public partial class Stats
{
    private Dictionary<StatType, FP> _values = new Dictionary<StatType, FP>();
    
    public FP Get(StatType type) { ... }
    public void Set(StatType type, FP value) { ... }
    public void Add(StatType type, FP delta) { ... }
    public void Clear() { ... }
    public Stats Clone() { ... }
}
```

### Phase 3: 基础属性组件

#### BaseStatsComponent
- ⏳ InitializeFromConfig(int roleId, int level)
  - 从 RoleBaseTable 读取基础值
  - int → FP 转换（整数直接转，小数除以1000）
  - 应用等级成长
- ⏳ ApplyLevelGrowth(int roleId, int level)
- ⏳ ApplyAllocatedPoints(GrowthComponent)

#### DerivedStatsComponent
- ⏳ AddModifier(StatType, StatModifier)
- ⏳ RemoveModifier(int sourceId)
- ⏳ RecalculateAll(BaseStatsComponent)
- ⏳ RecalculateIfDirty(BaseStatsComponent)
- ⏳ CalculateFinalStat(FP baseValue, StatType)

#### DynamicStatsComponent
- ⏳ Get/Set/Add 基础方法
- ⏳ TakeDamage(FP damage, DerivedStatsComponent)
- ⏳ Heal(FP amount, DerivedStatsComponent)
- ⏳ ConsumeMana(FP amount)
- ⏳ AddEnergy/AddRage
- ⏳ InitializeResources(DerivedStatsComponent)

### Phase 4: Buff系统

#### BuffComponent
- ⏳ AddBuff(BuffInstance)
  - 叠加逻辑
  - 刷新持续时间
- ⏳ RemoveBuff(int buffId)
- ⏳ UpdateBuffs()
- ⏳ GetAllModifiers()
- ⏳ ParseBuffModifiers(string modifierStr, BuffInstance)

#### BuffInstance 数据结构
```csharp
[MemoryPackable]
public partial class BuffInstance
{
    public int BuffId { get; set; }
    public int RemainingFrames { get; set; }
    public int Duration { get; set; }
    public int StackCount { get; set; } = 1;
    public bool Stackable { get; set; } = false;
    public int MaxStack { get; set; } = 1;
    public long CasterId { get; set; }
    public int BuffType { get; set; } = 1;
}
```

### Phase 5: 状态与成长

#### StateComponent
- ⏳ Get/Set/Clear 基础方法
- ⏳ CanMove()
- ⏳ CanAttack()
- ⏳ CanCastSkill()
- ⏳ CanTakeDamage()

#### LevelComponent
- ⏳ GainExp(int amount, Entity owner)
- ⏳ LevelUp(Entity owner)
  - 更新经验需求
  - 重新初始化 BaseStats
  - 重算 DerivedStats
  - 满血满蓝

#### GrowthComponent
- ⏳ AllocatePoint(StatType, Entity owner)
  - 扣除可用点数
  - 增加分配点数
  - 更新 BaseStats
  - 重算 DerivedStats

### Phase 6: DamageCalculator 重构

#### 新增方法
- ⏳ CheckHit(FP accuracy, FP evasion, int randomSeed) → bool
- ⏳ CheckBlock(FP blockRate, int randomSeed) → bool
- ⏳ CheckCritical(FP critRate, int randomSeed) → bool
- ⏳ ApplyDefense(FP damage, FP defense, DamageType) → FP
- ⏳ ApplyResistance(FP damage, DerivedStatsComponent, DamageType) → FP
- ⏳ ApplyDeterministicVariance(FP damage, int randomSeed) → FP
- ⏳ GenerateSeed(int frame, long casterId, long targetId) → int

#### DamageResult 扩展
```csharp
public class DamageResult
{
    public FP FinalDamage { get; set; }
    public bool IsCritical { get; set; }
    public bool IsBlocked { get; set; }  // 新增
    public bool IsMiss { get; set; }     // 新增
    public DamageType DamageType { get; set; }
}
```

### Phase 7: 测试清单（Phase 7 完成后开始 Phase 8）

#### 单元测试
- ⏳ `StatsTests.cs` - Stats容器测试
- ⏳ `BaseStatsComponentTests.cs` - 基础属性测试
- ⏳ `DerivedStatsComponentTests.cs` - 派生属性计算测试
- ⏳ `BuffComponentTests.cs` - Buff叠加和修饰器测试
- ⏳ `DynamicStatsComponentTests.cs` - 资源管理测试
- ⏳ `StateComponentTests.cs` - 状态判断测试
- ⏳ `LevelComponentTests.cs` - 升级流程测试
- ⏳ `DamageCalculatorTests.cs` - 伤害计算测试

#### 集成测试
- ⏳ 完整战斗流程（创建角色 → 添加Buff → 造成伤害 → 检查死亡）
- ⏳ 升级流程（获得经验 → 升级 → 属性更新 → 满血满蓝）
- ⏳ 加点流程（分配属性点 → 属性更新）

#### 性能测试
- ⏳ 100+ Entity 同时运行
- ⏳ 属性计算耗时测试
- ⏳ Buff更新耗时测试

---

## 文件清单

### 核心文件（需创建）

#### 枚举定义
```
AstrumProj/Assets/Script/LogicCore/Combat/Stats/
├── StatType.cs                      # 属性类型枚举
├── DynamicResourceType.cs           # 动态资源类型枚举
├── StateType.cs                     # 状态类型枚举
├── ModifierType.cs                  # 修饰器类型枚举
└── DamageType.cs                    # 伤害类型枚举
```

#### 数据结构
```
AstrumProj/Assets/Script/LogicCore/Combat/Stats/
├── Stats.cs                         # 通用属性容器
├── StatModifier.cs                  # 属性修饰器
├── BuffInstance.cs                  # Buff实例
└── DamageResult.cs                  # 伤害结果（扩展）
```

#### 组件
```
AstrumProj/Assets/Script/LogicCore/Combat/Stats/Components/
├── BaseStatsComponent.cs            # 基础属性组件
├── DerivedStatsComponent.cs         # 派生属性组件
├── DynamicStatsComponent.cs         # 动态属性组件
├── BuffComponent.cs                 # Buff组件
├── StateComponent.cs                # 状态组件
├── LevelComponent.cs                # 等级组件
└── GrowthComponent.cs               # 成长组件
```

#### 计算器
```
AstrumProj/Assets/Script/LogicCore/Combat/Stats/
└── DamageCalculator.cs              # 伤害计算器（重构）
```

#### 测试文件
```
AstrumProj/Assets/Script/Tests/Combat/Stats/
├── StatsTests.cs
├── BaseStatsComponentTests.cs
├── DerivedStatsComponentTests.cs
├── BuffComponentTests.cs
├── DynamicStatsComponentTests.cs
├── StateComponentTests.cs
├── LevelComponentTests.cs
└── DamageCalculatorTests.cs
```

### 配置表文件（需扩展/创建）

```
AstrumConfig/Tables/
├── RoleBaseTable.csv                # 扩展高级属性字段
├── RoleGrowthTable.csv              # 优化成长字段
├── BuffTable.csv                    # 新建Buff配置表
└── SkillEffectTable.csv             # 优化伤害类型字段
```

---

## 技术要点

### 1. 确定性计算

#### 定点数使用
```csharp
// ✅ 正确：使用 FP
FP damage = casterAttack * ratio;
FP finalDamage = damage * (FP.One - damageReduction);

// ❌ 错误：使用 float
float damage = attack * ratio; // 不确定！
```

#### 随机数使用
```csharp
// ✅ 正确：确定性随机
int seed = GenerateSeed(currentFrame, casterId, targetId);
TSRandom random = new TSRandom(seed);
FP roll = random.NextFP();

// ❌ 错误：不确定随机
float roll = UnityEngine.Random.value; // 不确定！
```

#### 配置表数值规则
```csharp
// 配置表：全部 int
baseAttack = 80        // 整数属性
baseSpeed = 10500      // 小数属性 *1000
baseCritRate = 50      // 百分比 *1000 (5%)

// 运行时：转换为 FP
BaseStats.Set(StatType.ATK, (FP)roleConfig.BaseAttack);           // 直接
BaseStats.Set(StatType.SPD, (FP)roleConfig.BaseSpeed / (FP)1000); // 除1000
BaseStats.Set(StatType.CRIT_RATE, (FP)roleConfig.BaseCritRate / (FP)1000); // 除1000
```

### 2. 性能优化

#### 脏标记机制
```csharp
// DerivedStatsComponent
private bool _isDirty = false;

public void MarkDirty() { _isDirty = true; }

public void RecalculateIfDirty(BaseStatsComponent baseStats)
{
    if (!_isDirty) return;
    RecalculateAll(baseStats);
}

// 使用：
buffComponent.UpdateBuffs();
if (buffChanged) derivedStats.MarkDirty();
derivedStats.RecalculateIfDirty(baseStats); // 帧结束统一重算
```

#### 批量处理
```csharp
// ✅ 正确：批量修改，最后重算
derivedStats.AddModifier(StatType.ATK, modifier1);
derivedStats.AddModifier(StatType.DEF, modifier2);
derivedStats.RecalculateAll(baseStats);  // 只重算1次

// ❌ 错误：每次修改都重算
derivedStats.AddModifier(StatType.ATK, modifier1);
derivedStats.RecalculateAll(baseStats);  // 重算1次
derivedStats.AddModifier(StatType.DEF, modifier2);
derivedStats.RecalculateAll(baseStats);  // 重算2次
```

### 3. 序列化注意事项

#### 标记忽略字段
```csharp
[MemoryPackable]
public partial class DerivedStatsComponent
{
    public Stats FinalStats { get; set; }  // ✅ 序列化
    
    [MemoryPackIgnore]
    private Dictionary<StatType, List<StatModifier>> _modifiers;  // ❌ 不序列化
    
    [MemoryPackIgnore]
    private bool _isDirty;  // ❌ 不序列化
}
```

#### 为什么不序列化修饰器
- 修饰器来自Buff，Buff本身会序列化
- 回滚恢复后，从Buff重新计算修饰器即可
- 减少序列化数据量

### 4. Buff修饰器解析

#### 字符串格式
```
"ATK:Percent:200;SPD:Flat:1000;CRIT_RATE:Flat:50"

解析规则：
- 分隔符：分号
- 格式：属性:类型:数值
- 数值：全部int
- Percent类型：总是除以1000
- Flat类型：根据属性判断
  - 整数属性（ATK/DEF/HP）：直接使用
  - 小数属性（SPD/CRIT_RATE）：除以1000
```

#### 解析逻辑
```csharp
private bool NeedsDivide1000(StatType type)
{
    return type switch
    {
        StatType.SPD => true,
        StatType.CRIT_RATE => true,
        StatType.CRIT_DMG => true,
        // ... 其他小数属性
        _ => false
    };
}

FP value;
if (modType == ModifierType.Percent || NeedsDivide1000(statType))
{
    value = (FP)intValue / (FP)1000;
}
else
{
    value = (FP)intValue;
}
```

---

## 验收标准

### 功能验收
- [ ] 角色属性正确读取配置表
- [ ] Buff加成正确影响最终属性
- [ ] 伤害计算使用真实属性（不再写死）
- [ ] 暴击、格挡、闪避判定生效
- [ ] 等级成长正确应用
- [ ] Buff叠加和过期正确处理
- [ ] 自由加点正确更新属性

### 性能验收
- [ ] 属性计算耗时 < 0.1ms
- [ ] Buff更新耗时 < 0.05ms
- [ ] 支持100+ Entity同时运行

### 测试验收
- [ ] 单元测试覆盖率 > 80%
- [ ] 集成测试通过
- [ ] 确定性测试通过（多客户端一致性）

### 代码质量
- [ ] 所有公共API有注释
- [ ] 遵循命名规范
- [ ] 无编译警告
- [ ] 无 TODO 标记

---

## 风险与挑战

### 技术风险

#### 1. 确定性保证
**风险**: FP计算在不同平台可能产生微小差异

**应对**:
- 使用 TrueSync.FP（已验证）
- 禁止使用 float/double
- 使用 TSRandom + 确定性种子
- 多客户端测试验证

#### 2. 性能问题
**风险**: 大量Entity时属性计算可能成为瓶颈

**应对**:
- 脏标记机制（避免频繁重算）
- 批量处理（减少重算次数）
- 性能测试验证（100+ Entity）
- 必要时使用对象池

#### 3. 配置表数据量
**风险**: Buff/成长表数据量可能很大

**应对**:
- 按需加载（仅加载当前角色相关数据）
- 数据验证工具（防止配置错误）
- 热更新支持（配置表可独立更新）

### 设计风险

#### 1. 属性扩展性
**风险**: 后续可能需要添加新属性类型

**应对**:
- 使用枚举+字典（易于扩展）
- 预留足够的枚举值空间
- 文档清晰说明扩展方式

#### 2. Buff系统复杂度
**风险**: Buff效果可能需要支持更复杂的逻辑

**应对**:
- 字符串格式保持灵活
- 预留扩展字段（effectParams）
- 必要时添加 BuffHandler 体系

---

## 下一步行动

### ⚠️ 重要：正确的开发流程

**第一步：Phase 1（必须先完成！）**
1. 扩展 RoleBaseTable.csv（添加高级属性字段）
2. 优化 RoleGrowthTable.csv（统一字段命名）
3. 创建 BuffTable.csv（定义Buff配置）
4. 优化 SkillEffectTable.csv（添加伤害类型）
5. **运行 Luban 生成配置表代码**
6. 验证生成的代码（`roleConfig.BaseCritRate` 等字段可用）

**第二步：Phase 2-8（依次进行）**
- 有了配置表生成的代码后，才能编写使用这些字段的组件代码
- 代码中可以直接使用智能提示访问字段

### 立即可做（正确顺序）
1. ✅ **开始 Phase 1**：扩展配置表并运行 Luban 生成代码 🔥
2. ⏳ 开始 Phase 2：创建核心数据结构（枚举）
3. ⏳ 开始 Phase 3：实现基础属性组件

### 待讨论
1. 配置表扩展细节确认
2. 性能目标确认
3. 开发排期安排

### 待准备
1. BuffTable 示例数据
2. 测试场景设计
3. UI集成方案

---

**文档版本**: v1.0  
**最后更新**: 2025-10-14  
**负责人**: 待指定


