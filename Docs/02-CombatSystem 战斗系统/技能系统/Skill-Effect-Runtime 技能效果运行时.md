# 技能效果运行时

> 📖 **相关文档**：
> - [技能系统](Skill-System%20技能系统.md) - 技能系统三层架构设计
> - [动作系统](Action-System%20动作系统.md) - 动作系统设计
> - [物理系统](../../03-PhysicsSystem%20物理系统/) - 物理碰撞检测系统
- ✅ **数据结构确定**：HitBoxData 已合并为 CollisionShape（统一概念）
- ✅ **配置系统集成**：碰撞数据可从配置表自动加载
- ✅ **编辑器工具就绪**：角色编辑器支持碰撞盒生成和预览
- 🎯 **准备开发**：基础设施已就绪，可以开始技能效果系统开发

---

## 📋 目录

1. [系统概述](#系统概述)
2. [运行时架构](#运行时架构)
3. [SkillExecutorCapability - 技能动作执行能力](#SkillExecutorCapability---技能动作执行能力)
4. [HitManager - 碰撞检测管理器](#hitmanager---碰撞检测管理器)
5. [SkillEffectManager - 技能效果管理器](#skilleffectmanager---技能效果管理器)
6. [DamageCalculator - 伤害计算模块](#damagecalculator---伤害计算模块)
7. [效果处理器体系](#效果处理器体系)
8. [运行时流程](#运行时流程)
9. [扩展性设计](#扩展性设计)
10. [技术实现要点](#技术实现要点)
11. [配置示例](#配置示例)

---

## 系统概述

### 1.1 设计背景

技能系统策划案定义了 **Skill → SkillAction → SkillEffect** 三层逻辑结构，将技能的概念、执行和效果完全分离。本文档进一步设计第四层：**技能执行运行时（Runtime）层**，负责在游戏实际运行时处理碰撞检测、伤害计算和统一效果触发。

### 1.2 核心目标

- **碰撞检测统一管理** - 通过HitManager统一处理所有攻击碰撞盒
- **效果触发统一分发** - 通过SkillEffectManager统一处理所有技能效果
- **伤害计算模块化** - 独立的伤害计算模块，易于扩展和调整
- **高性能实时处理** - 同帧完成检测和处理，保证即时反馈
- **扩展性与灵活性** - 支持各种效果类型的插件式扩展

### 1.3 设计理念

**核心原则**：SkillAction只负责"在时间轴上触发事件"，运行时层负责"执行事件产生的效果"

```
概念层：Skill（技能概念）
   ↓
执行层：SkillAction（动作时间轴）
   ↓
触发层：TriggerFrame（触发帧事件）
   ↓
运行时层：SkillExecutorCapability（内部执行） → HitManager/SkillEffectManager → DamageCalculator
   ↓
结果层：游戏效果（伤害、治疗、buff等）
```

---

## 运行时架构

### 2.1 运行时层级结构

```
技能运行时层
├── SkillExecutorCapability（技能动作执行能力）
│   ├── 处理 TriggerFrame 触发时机
│   ├── 调用 HitManager 即时检测命中
│   ├── 调用 SkillEffectManager 触发效果
│   └── 控制时序与多段效果
│
├── HitManager（碰撞检测管理器）
│   ├── 管理当前活动攻击盒（HitBox）
│   ├── 碰撞检测与命中缓存
│   ├── 防重复命中逻辑
│   └── 输出命中事件
│
├── SkillEffectManager（技能效果管理器）
│   ├── 统一接收效果触发请求
│   ├── 调用具体 EffectHandler
│   ├── 管理跨帧/延迟效果
│   └── 队列化处理效果
│
├── DamageCalculator（伤害计算模块）
│   ├── 读取 SkillEffect 配置
│   ├── 读取角色属性
│   ├── 计算最终数值（含暴击、加成、减免）
│   └── 输出 DamageResult（伤害结果）
│
└── EffectHandlers（效果处理器集合）
    ├── DamageEffectHandler（伤害）
    ├── HealEffectHandler（治疗）
    ├── KnockbackEffectHandler（击退）
    ├── BuffEffectHandler（增益）
    └── DebuffEffectHandler（减益）
```

### 2.2 模块职责分工

| 模块 | 职责 | 输入 | 输出 |
|-----|------|------|------|
| SkillExecutorCapability | 技能执行调度 | SkillAction + TriggerFrame | 即时命中检测 / SkillEffect请求 |
| HitManager | 碰撞检测 | HitBox数据 + 世界状态 | 命中Entity列表 |
| SkillEffectManager | 效果分发 | SkillEffect请求 | 调用EffectHandler |
| DamageCalculator | 伤害计算 | 施法者/目标属性 + 效果配置 | DamageResult |
| EffectHandler | 效果执行 | SkillEffectData + Entity | 修改Entity状态 |

### 2.3 数据流向

```
SkillAction 执行到 TriggerFrame
    ↓
SkillExecutorCapability 解析触发类型
    ↓
┌──────────┬─────────────┬──────────────┐
│ Collision│   Direct    │  Condition   │
│ 碰撞触发  │  直接触发    │  条件触发     │
└──────────┴─────────────┴──────────────┘
    ↓              ↓             ↓
HitManager    SkillEffect   条件即时检测
  即时检测         ↓             ↓
    ↓              ↓             ↓
命中Entity列表 ──→ SkillEffectManager ←─ 条件满足
                   ↓
           查询 EffectType
                   ↓
           调用对应 Handler
                   ↓
      ┌────────────┼────────────┐
   Damage        Heal        Buff
      ↓            ↓            ↓
DamageCalculator  修改HP      添加Buff组件
      ↓
  应用伤害到目标
```

---

## SkillExecutorCapability - 技能动作执行能力

### 3.1 核心职责

SkillExecutorCapability 在实体上运行，负责：
1. 监听 SkillAction 的帧更新
2. 检查并处理当前帧的所有 TriggerFrame
3. 根据触发类型调用对应的处理模块

#### 3.1.1 触发帧数据结构

```csharp
public class TriggerFrameInfo
{
    public int Frame { get; set; }
    public TriggerType Type { get; set; }
    public int EffectId { get; set; }
    public CollisionShape? CollisionShape { get; set; } // 更新为 CollisionShape
    public TriggerCondition? Condition { get; set; }
}

public class TriggerCondition
{
    public float EnergyMin { get; set; }
    public string RequiredTag { get; set; }
}
```

### 3.2 核心结构

```csharp
public class SkillExecutorCapability
{
    private readonly HitManager _hitManager;
    private readonly SkillEffectManager _effectManager;
    
    /// <summary>
    /// 处理技能动作的当前帧
    /// </summary>
    public void ProcessFrame(Entity caster, SkillActionInfo actionInfo, int currentFrame)
    {
        // 获取当前帧的所有触发事件
        var triggers = actionInfo.GetTriggersAtFrame(currentFrame);
        
        foreach (var trigger in triggers)
        {
            switch (trigger.Type)
            {
                case TriggerType.Collision:
                    HandleCollisionTrigger(caster, actionInfo, trigger);
                    break;
                    
                case TriggerType.Direct:
                    HandleDirectTrigger(caster, trigger);
                    break;
                    
                case TriggerType.Condition:
                    HandleConditionTrigger(caster, actionInfo, trigger);
                    break;
            }
        }
    }
    
    /// <summary>
    /// 处理碰撞触发（即时检测）- 已对齐实际实现
    /// </summary>
    private void HandleCollisionTrigger(Entity caster, SkillActionInfo actionInfo, TriggerFrameInfo trigger)
    {
        var shape = trigger.CollisionShape; // 更新为 CollisionShape
        if (shape == null) return;

        // 即时调用 HitManager 进行检测
        // 传入施法者、碰撞形状、可选过滤器、技能实例ID
        var hits = _hitManager.QueryHits(
            caster, 
            shape, 
            filter: null, // 可以从 SkillAction 配置获取
            skillInstanceId: actionInfo.SkillInstanceId
        );
        
        foreach (var target in hits)
        {
            TriggerSkillEffect(caster, target, trigger.EffectId);
        }
    }
    
    /// <summary>
    /// 处理直接触发
    /// </summary>
    private void HandleDirectTrigger(Entity caster, TriggerFrameInfo trigger)
    {
        // 直接触发效果，无需碰撞检测
        TriggerSkillEffect(caster, caster, trigger.EffectId);
    }
    
    /// <summary>
    /// 触发技能效果
    /// </summary>
    private void TriggerSkillEffect(Entity caster, Entity target, int effectId)
    {
        _effectManager.QueueSkillEffect(new SkillEffectData
        {
            CasterEntity = caster,
            TargetEntity = target,
            EffectId = effectId
        });
    }
}
```

### 3.3 触发类型处理

| 触发类型 | 处理方式 | 使用场景 |
|---------|---------|---------|
| **Collision** | 即时调用HitManager检测命中 | 近战攻击、冲刺技能 |
| **Direct** | 直接调用SkillEffectManager | 自身buff、远程生成弹道 |
| **Condition** | 即时检测自定义条件 | 能量阈值、状态标记、资源充足 |

### 3.4 时序控制

```csharp
// SkillAction 每帧更新时调用
public void Update(int currentFrame)
{
    // 1. SkillExecutorCapability 处理触发帧（包含即时命中检测与入队效果）
    SkillExecutorCapability.ProcessFrame(owner, skillActionInfo, currentFrame);

    // 2. SkillEffectManager 处理效果队列（同帧）
    SkillEffectManager.Update();
}
```

**设计要点**：所有处理在同帧内完成，保证即时反馈。

---

## HitManager - 碰撞检测管理器

### 4.1 核心职责

HitManager 是所有攻击盒的统一调度中心，负责：
1. 提供即时命中查询（无注册/无生命周期）
2. 支持多种几何体的 Overlap 查询（Box/Sphere）
3. 支持碰撞过滤（ExcludeEntityIds、CustomFilter）
4. 支持命中去重（技能实例级）
5. Capsule Overlap 查询
6. Sweep/Raycast 查询

### 4.2 实现

```csharp
/// <summary>
/// HitManager - 命中管理器
/// 文件位置: AstrumProj/Assets/Script/AstrumLogic/Physics/HitManager.cs
/// </summary>
public class HitManager : IDisposable
{
    private readonly BepuPhysicsWorld _physicsWorld;
    
    public HitManager(BepuPhysicsWorld physicsWorld)
    {
        _physicsWorld = physicsWorld;
    }
    
    /// <summary>
    /// 即时命中查询（已实现）
    /// </summary>
    /// <param name="caster">施法者实体</param>
    /// <param name="shape">碰撞形状（CollisionShape）</param>
    /// <param name="filter">可选的碰撞过滤器</param>
    /// <param name="skillInstanceId">技能实例ID（用于去重）</param>
    /// <returns>命中的实体列表</returns>
    public List<AstrumEntity> QueryHits(
        AstrumEntity caster, 
        CollisionShape shape, 
        CollisionFilter filter = null, 
        int skillInstanceId = 0)
    {
        // 1. 获取施法者位置和旋转
        var casterPos = caster.GetComponent<PositionComponent>()?.Position ?? TSVector.zero;
        var casterRot = TSQuaternion.identity; // TODO: 从 RotationComponent 获取
        
        // 2. 计算世界坐标下的攻击盒位置
        var worldCenter = casterPos + casterRot * shape.LocalOffset;
        var worldRotation = casterRot * shape.LocalRotation;
        
        // 3. 根据形状类型执行查询
        List<AstrumEntity> candidates = new List<AstrumEntity>();
        switch (shape.ShapeType)
        {
            case HitBoxShape.Box:
                candidates = _physicsWorld.QueryBoxOverlap(worldCenter, shape.HalfSize, worldRotation);
                break;
                
            case HitBoxShape.Sphere:
                candidates = _physicsWorld.QuerySphereOverlap(worldCenter, shape.Radius);
                break;
                
            case HitBoxShape.Capsule:
                // TODO: 实现 Capsule 查询
                break;
        }
        
        // 4. 应用过滤器
        if (filter != null)
        {
            candidates = ApplyFilter(candidates, caster, filter);
        }
        
        // 5. 去重处理
        if (skillInstanceId > 0)
        {
            candidates = ApplyDeduplication(candidates, skillInstanceId);
        }
        
        return candidates;
    }
    
    /// <summary>
    /// 注册实体到物理世界（代理方法）
    /// </summary>
    public void RegisterEntity(AstrumEntity entity)
    {
        _physicsWorld.RegisterEntity(entity);
    }
}
```

### 4.3 CollisionShape 数据结构（已实现）

```csharp
/// <summary>
/// 碰撞形状定义（已合并 HitBoxData）
/// 文件位置: AstrumProj/Assets/Script/AstrumLogic/Physics/CollisionShape.cs
/// </summary>
[MemoryPackable]
public partial struct CollisionShape
{
    // 形状类型
    public HitBoxShape ShapeType { get; set; }
    
    // 本地变换
    public TSVector LocalOffset { get; set; }      // 本地偏移
    public TSQuaternion LocalRotation { get; set; } // 本地旋转
    
    // 几何参数
    public TSVector HalfSize { get; set; }         // Box 半尺寸（实际尺寸的一半）
    public FP Radius { get; set; }                 // Sphere/Capsule 半径
    public FP Height { get; set; }                 // Capsule 高度（端到端）
    
    // 查询模式
    public HitQueryMode QueryMode { get; set; }    // Overlap / Sweep / Raycast
    
    // 工厂方法
    public static CollisionShape CreateBox(TSVector halfSize, TSVector localOffset = default, TSQuaternion localRotation = default);
    public static CollisionShape CreateSphere(FP radius, TSVector localOffset = default);
    public static CollisionShape CreateCapsule(FP radius, FP height, TSVector localOffset = default, TSQuaternion localRotation = default);
}

public enum HitBoxShape { Box = 1, Sphere = 2, Capsule = 3 }
public enum HitQueryMode { Overlap = 0, Sweep = 1, Raycast = 2 }
    public int FactionMask;             // 阵营/标签过滤（游戏逻辑层）

    // 其他
    public string Anchor;               // 绑定骨骼名（可选）
    public int MaxTargets;              // 命中上限（0=不限）
}
```

最小实践：仅必需字段为 `ShapeType + LocalOffset + （Size|Radius/Height）`；其余按需启用。

### 4.4 攻击盒解析（可选）

从 `AttackBoxInfo` 字符串解析为 `HitBoxData` 列表：

```csharp
/// <summary>
/// 解析攻击盒信息字符串
/// 格式："Box1:5x2x1,Sphere1:3"
/// </summary>
public static List<HitBoxData> ParseAttackBoxInfo(string attackBoxInfo)
{
    var result = new List<HitBoxData>();
    if (string.IsNullOrEmpty(attackBoxInfo)) return result;
    
    var boxes = attackBoxInfo.Split(',');
    foreach (var boxStr in boxes)
    {
        var parts = boxStr.Split(':');
        if (parts.Length < 3) continue;
        
        var name = parts[0];
        var sizeStr = parts[1];
        HitBoxData boxData = default;
        
        // Box类型：5x2x1
        if (sizeStr.Contains('x'))
        {
            var sizes = sizeStr.Split('x').Select(float.Parse).ToArray();
            boxData = new HitBoxData
            {
                ShapeType = HitBoxShape.Box,
                Size = new Vector3(sizes[0], sizes[1], sizes[2])
            };
        }
        // Sphere类型：3
        else
        {
            boxData = new HitBoxData
            {
                ShapeType = HitBoxShape.Sphere,
                Radius = float.Parse(sizeStr)
            };
        }
        
        if (boxData != null)
        {
            result.Add(boxData);
        }
    }
    
    return result;
}
```

---

## SkillEffectManager - 技能效果管理器

### 5.1 核心职责

SkillEffectManager 是所有技能效果的统一处理中心，负责：
1. 接收来自各处的效果触发请求
2. 队列化管理效果执行
3. 根据 EffectType 调用对应的 Handler
4. 支持即时和延迟效果

### 5.2 核心结构

```csharp
public class SkillEffectManager
{
    private static SkillEffectManager _instance;
    public static SkillEffectManager Instance => _instance ??= new SkillEffectManager();
    
    private readonly Queue<SkillEffectData> _effectQueue = new();
    private readonly Dictionary<int, ISkillEffectHandler> _handlers = new();
    
    /// <summary>
    /// 注册效果处理器
    /// </summary>
    public void RegisterHandler(int effectType, ISkillEffectHandler handler)
    {
        _handlers[effectType] = handler;
    }
    
    /// <summary>
    /// 加入效果队列
    /// </summary>
    public void QueueSkillEffect(SkillEffectData effectData)
    {
        _effectQueue.Enqueue(effectData);
    }
    
    /// <summary>
    /// 每帧更新：处理效果队列
    /// </summary>
    public void Update()
    {
        while (_effectQueue.Count > 0)
        {
            var effectData = _effectQueue.Dequeue();
            ProcessEffect(effectData);
        }
    }
    
    /// <summary>
    /// 处理单个效果
    /// </summary>
    private void ProcessEffect(SkillEffectData effectData)
    {
        // 从配置表读取效果配置
        var effectConfig = SkillConfigManager.GetSkillEffect(effectData.EffectId);
        if (effectConfig == null)
        {
            Debug.LogError($"未找到技能效果配置: {effectData.EffectId}");
            return;
        }
        
        // 获取对应的处理器
        if (!_handlers.TryGetValue(effectConfig.EffectType, out var handler))
        {
            Debug.LogError($"未注册效果类型处理器: {effectConfig.EffectType}");
            return;
        }
        
        // 执行效果
        handler.Handle(effectData.CasterEntity, effectData.TargetEntity, effectConfig);
    }
}
```

### 5.3 效果数据结构

```csharp
/// <summary>
/// 技能效果数据（运行时）
/// </summary>
public class SkillEffectData
{
    public Entity CasterEntity;             // 施法者
    public Entity TargetEntity;             // 目标
    public int EffectId;                    // 技能效果ID
}

/// <summary>
/// 技能效果配置（来自表格）
/// </summary>
public class SkillEffectConfig
{
    public int SkillEffectId;               // 效果ID
    public int EffectType;                  // 效果类型
    public float EffectValue;               // 效果数值
    public int TargetType;                  // 目标类型
    public float EffectDuration;            // 持续时间
    public float EffectRange;               // 效果范围
    public string EffectParams;             // 效果参数（JSON）
    public int VisualEffectId;              // 视觉效果ID
    public int SoundEffectId;               // 音效ID
}
```

### 5.4 效果类型定义

| EffectType | 名称 | 处理器 | 说明 |
|-----------|------|--------|------|
| 1 | 伤害 | DamageEffectHandler | 造成伤害 |
| 2 | 治疗 | HealEffectHandler | 恢复生命值 |
| 3 | 击退 | KnockbackEffectHandler | 击退目标 |
| 4 | Buff | BuffEffectHandler | 添加增益效果 |
| 5 | Debuff | DebuffEffectHandler | 添加减益效果 |
| 6 | 位移 | DisplacementEffectHandler | 移动目标位置 |
| 7 | 召唤 | SummonEffectHandler | 召唤单位 |

### 5.5 初始化注册

```csharp
public void Initialize()
{
    // 注册所有效果处理器
    RegisterHandler(1, new DamageEffectHandler());
    RegisterHandler(2, new HealEffectHandler());
    RegisterHandler(3, new KnockbackEffectHandler());
    RegisterHandler(4, new BuffEffectHandler());
    RegisterHandler(5, new DebuffEffectHandler());
    RegisterHandler(6, new DisplacementEffectHandler());
    RegisterHandler(7, new SummonEffectHandler());
}
```

---

## DamageCalculator - 伤害计算模块

### 6.1 核心职责

DamageCalculator 是独立的伤害计算模块，负责：
1. 读取施法者和目标的属性
2. 读取技能效果配置
3. 计算基础伤害
4. 应用暴击、加成、减免等修正
5. 输出最终伤害结果

### 6.2 核心结构

```csharp
public static class DamageCalculator
{
    /// <summary>
    /// 计算伤害
    /// </summary>
    public static DamageResult Calculate(Entity caster, Entity target, SkillEffectConfig effectConfig)
    {
        var casterStats = caster.GetComponent<StatComponent>();
        var targetStats = target.GetComponent<StatComponent>();
        
        // 1. 计算基础伤害
        float baseDamage = CalculateBaseDamage(casterStats, effectConfig);
        
        // 2. 暴击判定
        bool isCritical = CheckCritical(casterStats);
        if (isCritical)
        {
            baseDamage *= casterStats.CritDamageMultiplier;
        }
        
        // 3. 应用防御减免
        float finalDamage = ApplyDefense(baseDamage, targetStats);
        
        // 4. 应用属性克制
        finalDamage = ApplyElementalModifier(finalDamage, casterStats, targetStats, effectConfig);
        
        // 5. 应用随机浮动
        finalDamage = ApplyRandomVariance(finalDamage);
        
        // 6. 确保伤害非负
        finalDamage = Mathf.Max(0, finalDamage);
        
        return new DamageResult
        {
            FinalDamage = finalDamage,
            IsCritical = isCritical,
            DamageType = ParseDamageType(effectConfig.EffectParams)
        };
    }
    
    /// <summary>
    /// 计算基础伤害
    /// </summary>
    private static float CalculateBaseDamage(StatComponent casterStats, SkillEffectConfig effectConfig)
    {
        // EffectValue 通常是百分比（如150表示150%攻击力）
        float ratio = effectConfig.EffectValue / 100f;
        return casterStats.Attack * ratio;
    }
    
    /// <summary>
    /// 暴击判定
    /// </summary>
    private static bool CheckCritical(StatComponent casterStats)
    {
        return Random.value < casterStats.CritRate;
    }
    
    /// <summary>
    /// 应用防御减免
    /// </summary>
    private static float ApplyDefense(float baseDamage, StatComponent targetStats)
    {
        // 简单的防御公式：伤害 - (防御 * 0.5)
        // 可以替换为更复杂的公式
        float reduction = targetStats.Defense * 0.5f;
        return baseDamage - reduction;
    }
    
    /// <summary>
    /// 应用属性克制
    /// </summary>
    private static float ApplyElementalModifier(float damage, StatComponent casterStats, StatComponent targetStats, SkillEffectConfig effectConfig)
    {
        // 解析伤害类型
        var damageType = ParseDamageType(effectConfig.EffectParams);
        
        // 根据属性克制关系调整伤害
        float multiplier = GetElementalMultiplier(casterStats.Element, targetStats.Element);
        return damage * multiplier;
    }
    
    /// <summary>
    /// 应用随机浮动（±5%）
    /// </summary>
    private static float ApplyRandomVariance(float damage)
    {
        float variance = Random.Range(0.95f, 1.05f);
        return damage * variance;
    }
}
```

### 6.3 伤害结果

```csharp
/// <summary>
/// 伤害结果
/// </summary>
public class DamageResult
{
    public float FinalDamage;               // 最终伤害
    public bool IsCritical;                 // 是否暴击
    public DamageType DamageType;           // 伤害类型
}

public enum DamageType
{
    Physical = 1,                           // 物理伤害
    Magical = 2,                            // 魔法伤害
    True = 3                                // 真实伤害（无视防御）
}
```

### 6.4 伤害公式可视化

```
基础伤害 = 攻击力 × 技能倍率(%)
    ↓
暴击判定：若触发 → 基础伤害 × 暴击倍率
    ↓
防御减免：伤害 - (防御 × 0.5)
    ↓
属性克制：伤害 × 克制系数(0.5 / 1.0 / 2.0)
    ↓
随机浮动：伤害 × [0.95, 1.05]
    ↓
最终伤害 = Max(0, 结果)
```

---

## 效果处理器体系

### 7.1 处理器接口

```csharp
/// <summary>
/// 技能效果处理器接口
/// </summary>
public interface ISkillEffectHandler
{
    void Handle(Entity caster, Entity target, SkillEffectConfig effectConfig);
}
```

### 7.2 伤害处理器

```csharp
public class DamageEffectHandler : ISkillEffectHandler
{
    public void Handle(Entity caster, Entity target, SkillEffectConfig effectConfig)
    {
        // 1. 计算伤害
        var damageResult = DamageCalculator.Calculate(caster, target, effectConfig);
        
        // 2. 应用伤害到目标
        var healthComponent = target.GetComponent<HealthComponent>();
        healthComponent.CurrentHP -= damageResult.FinalDamage;
        
        // 3. 触发伤害事件
        EventSystem.Trigger(new DamageEvent
        {
            Caster = caster,
            Target = target,
            Damage = damageResult.FinalDamage,
            IsCritical = damageResult.IsCritical
        });
        
        // 4. 播放视觉效果
        if (effectConfig.VisualEffectId > 0)
        {
            EffectManager.PlayEffect(effectConfig.VisualEffectId, target.Transform.position);
        }
        
        // 5. 播放音效
        if (effectConfig.SoundEffectId > 0)
        {
            AudioManager.PlaySound(effectConfig.SoundEffectId);
        }
    }
}
```

### 7.3 治疗处理器

```csharp
public class HealEffectHandler : ISkillEffectHandler
{
    public void Handle(Entity caster, Entity target, SkillEffectConfig effectConfig)
    {
        var healthComponent = target.GetComponent<HealthComponent>();
        var healAmount = effectConfig.EffectValue;
        
        healthComponent.CurrentHP = Mathf.Min(
            healthComponent.MaxHP, 
            healthComponent.CurrentHP + healAmount
        );
        
        EventSystem.Trigger(new HealEvent
        {
            Caster = caster,
            Target = target,
            HealAmount = healAmount
        });
    }
}
```

### 7.4 击退处理器

```csharp
public class KnockbackEffectHandler : ISkillEffectHandler
{
    public void Handle(Entity caster, Entity target, SkillEffectConfig effectConfig)
    {
        // 解析击退参数
        var knockbackParams = JsonUtility.FromJson<KnockbackParams>(effectConfig.EffectParams);
        
        // 计算击退方向
        Vector3 direction = (target.Transform.position - caster.Transform.position).normalized;
        
        // 应用击退力
        var movementComponent = target.GetComponent<MovementComponent>();
        movementComponent.ApplyForce(direction * knockbackParams.Force, knockbackParams.Duration);
        
        EventSystem.Trigger(new KnockbackEvent
        {
            Caster = caster,
            Target = target,
            Force = knockbackParams.Force
        });
    }
}

public class KnockbackParams
{
    public float Force;                     // 击退力度
    public float Duration;                  // 持续时间
}
```

### 7.5 Buff处理器

```csharp
public class BuffEffectHandler : ISkillEffectHandler
{
    public void Handle(Entity caster, Entity target, SkillEffectConfig effectConfig)
    {
        var buffParams = JsonUtility.FromJson<BuffParams>(effectConfig.EffectParams);
        
        var buffComponent = target.GetComponent<BuffComponent>();
        buffComponent.AddBuff(new BuffInstance
        {
            BuffId = buffParams.BuffId,
            Duration = effectConfig.EffectDuration,
            StackCount = 1,
            Caster = caster
        });
    }
}

public class BuffParams
{
    public int BuffId;                      // Buff ID
    public bool Stackable;                  // 是否可叠加
}
```

---

## 运行时流程

### 8.1 同帧执行流程

```
当前帧开始
    ↓
1. 所有 SkillExecutorCapability 更新
    ├─ 执行当前技能动作逻辑
    └─ 更新动作帧计数
    ↓
2. SkillExecutorCapability 处理触发帧
    ├─ 检查当前帧是否有触发事件
    ├─ Collision → HitManager.QueryHits → SkillEffectManager.QueueEffect
    └─ Direct → SkillEffectManager.QueueEffect
    ↓
3. （移除）HitManager.Update()（即时检测替代）
    ↓
4. SkillEffectManager.Update()
    ├─ 处理效果队列
    ├─ 调用对应 EffectHandler
    └─ DamageCalculator / BuffSystem / etc.
    ↓
5. 应用结果到游戏世界
    ├─ 修改 HP
    ├─ 添加 Buff
    └─ 触发事件
    ↓
当前帧结束
```

### 8.2 多段技能示例

**场景**：剑士的"三连斩"技能

**SkillActionTable配置**：
```
TriggerFrames: "Frame5:Collision:4001,Frame15:Collision:4002,Frame25:Collision:4003"
```

**执行流程**：
```
Frame 5:
    → SkillExecutorCapability 触发 Collision(4001)
    → HitManager 注册 HitBox1
    → 检测到敌人A、B
    → 对A、B分别触发 SkillEffect(4001)
    → DamageCalculator 计算伤害
    → 应用伤害

Frame 15:
    → SkillExecutorCapability 触发 Collision(4002)
    → HitManager 注册 HitBox2
    → 检测到敌人A、C
    → 对A、C分别触发 SkillEffect(4002)
    → 应用伤害

Frame 25:
    → SkillExecutorCapability 触发 Collision(4003)
    → HitManager 注册 HitBox3
    → 检测到敌人B
    → 对B触发 SkillEffect(4003)
    → 应用伤害
```

**关键点**：
- 每个 HitBox 独立管理命中记录
- 同一敌人可以被不同阶段的攻击命中
- 所有检测在触发帧当帧完成

### 8.3 性能优化策略

| 优化点 | 策略 | 效果 |
|-------|------|------|
| 碰撞检测 | 空间分区（如四叉树） | 减少检测次数 |
| 效果队列 | 对象池 | 减少GC |
| 配置读取 | 缓存配置数据 | 避免重复查询 |
| 伤害计算 | 预计算属性 | 减少实时计算 |

---

## 扩展性设计

### 9.1 新增效果类型

**步骤**：
1. 定义效果类型ID（如 8 = 眩晕）
2. 创建 `StunEffectHandler : ISkillEffectHandler`
3. 在 `SkillEffectManager.Initialize()` 中注册
4. 在 `SkillEffectTable` 中添加配置

**示例**：
```csharp
public class StunEffectHandler : ISkillEffectHandler
{
    public void Handle(Entity caster, Entity target, SkillEffectConfig effectConfig)
    {
        var stunComponent = target.GetComponent<StunComponent>();
        stunComponent.ApplyStun(effectConfig.EffectDuration);
    }
}

// 注册
RegisterHandler(8, new StunEffectHandler());
```

### 9.2 复杂效果组合

通过 `EffectParams` 字段支持复杂参数：

**示例**：连锁闪电
```json
{
    "SkillEffectId": 5001,
    "EffectType": 1,
    "EffectValue": 100.0,
    "EffectParams": "{\"ChainCount\":3,\"ChainRange\":5.0,\"DamageDecay\":0.7}"
}
```

**处理器实现**：
```csharp
public class ChainLightningHandler : ISkillEffectHandler
{
    public void Handle(Entity caster, Entity target, SkillEffectConfig effectConfig)
    {
        var chainParams = JsonUtility.FromJson<ChainParams>(effectConfig.EffectParams);
        
        var currentTarget = target;
        float currentDamage = effectConfig.EffectValue;
        
        for (int i = 0; i < chainParams.ChainCount; i++)
        {
            // 应用伤害到当前目标
            ApplyDamage(currentTarget, currentDamage);
            
            // 查找下一个目标
            var nextTarget = FindNearestEnemy(currentTarget, chainParams.ChainRange);
            if (nextTarget == null) break;
            
            // 伤害衰减
            currentDamage *= chainParams.DamageDecay;
            currentTarget = nextTarget;
        }
    }
}
```

### 9.3 条件触发扩展

采用即时检测策略：在触发帧由 `SkillExecutorCapability` 直接判定是否满足自定义条件（能量、状态标记、资源是否充足等），满足则立即触发效果。

**示例**：即时检测能量阈值
```csharp
private void HandleConditionTrigger(Entity caster, SkillActionInfo actionInfo, TriggerFrameInfo trigger)
{
    if (IsConditionSatisfied(caster, actionInfo, trigger))
    {
        // 条件满足，立即触发
        TriggerSkillEffect(caster, caster, trigger.EffectId);
    }
}

private bool IsConditionSatisfied(Entity caster, SkillActionInfo actionInfo, TriggerFrameInfo trigger)
{
    var cond = trigger.Condition;
    if (cond == null) return true;

    var energy = caster.GetComponent<ResourceComponent>().Get("Energy");
    if (energy < cond.EnergyMin) return false;

    var status = caster.GetComponent<StatusComponent>();
    if (!string.IsNullOrEmpty(cond.RequiredTag) && !status.HasTag(cond.RequiredTag)) return false;

    return true;
}
```

---

## 技术实现要点

### 10.1 模块依赖

```
GameLogicSystem (主循环)
    ↓
SkillExecutorCapability (技能能力)
    ↓
SkillExecutorCapability（内部执行）
    ↓
┌──────────────────┬──────────────────┐
│ HitManager       │ SkillEffectMgr   │
└──────────────────┴──────────────────┘
            ↓
    DamageCalculator
            ↓
    Entity Components
```

### 10.2 初始化顺序

```csharp
// 游戏启动时初始化
public class CombatSystemInitializer
{
    public static void Initialize()
    {
        // 1. 初始化 HitManager（单例）
        HitManager.Instance.Initialize();
        
        // 2. 初始化 SkillEffectManager（单例）
        SkillEffectManager.Instance.Initialize();
        
        // 3. 注册所有效果处理器
        RegisterEffectHandlers();
        
        // 4. 注册到主循环
        GameLoop.RegisterUpdate(HitManager.Instance.Update);
        GameLoop.RegisterUpdate(SkillEffectManager.Instance.Update);
    }
    
    private static void RegisterEffectHandlers()
    {
        var manager = SkillEffectManager.Instance;
        manager.RegisterHandler(1, new DamageEffectHandler());
        manager.RegisterHandler(2, new HealEffectHandler());
        manager.RegisterHandler(3, new KnockbackEffectHandler());
        manager.RegisterHandler(4, new BuffEffectHandler());
        manager.RegisterHandler(5, new DebuffEffectHandler());
    }
}
```

### 10.3 调试与可视化

```csharp
#if UNITY_EDITOR
public class CombatDebugger
{
    [MenuItem("Astrum/Combat/Toggle HitBox Visualization")]
    public static void ToggleHitBoxVisualization()
    {
        HitManager.Instance.EnableDebugDraw = !HitManager.Instance.EnableDebugDraw;
    }
}

// 在 HitManager 中
public void OnDrawGizmos()
{
    if (!EnableDebugDraw) return;
    
    foreach (var hitBox in _activeHitBoxes)
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(hitBox.WorldCenter, hitBox.BoxData.Size);
    }
}
#endif
```

---

## 配置示例

### 11.1 剑士冲刺斩

**SkillTable**：
```json
{
    "Id": 2001,
    "Name": "冲刺斩",
    "SkillActionIds": [3001],
    "DisplayCooldown": 5.0,
    "DisplayCost": 20
}
```

**SkillActionTable**：
```json
{
    "ActionId": 3001,
    "SkillId": 2001,
    "AttackBoxInfo": "Box:5x2x1:10",
    "ActualCost": 15,
    "ActualCooldown": 300,
    "TriggerFrames": "Frame8:Collision:4001,Frame12:Direct:4002"
}
```

**SkillEffectTable**：
```json
[
    {
        "SkillEffectId": 4001,
        "EffectType": 1,
        "EffectValue": 150.0,
        "TargetType": 1,
        "EffectParams": "{\"DamageType\":\"Physical\"}"
    },
    {
        "SkillEffectId": 4002,
        "EffectType": 3,
        "EffectValue": 5.0,
        "TargetType": 1,
        "EffectParams": "{\"Force\":5.0,\"Duration\":0.3}"
    }
]
```

**执行流程**：
```
Frame 8:
    → 碰撞检测，命中敌人A、B
    → 对A、B造成 150% 攻击力的物理伤害

Frame 12:
    → 直接触发，目标为自身或之前命中的敌人
    → 击退目标（力度5.0，持续0.3秒）
```

### 11.2 法师火球术

**SkillActionTable**：
```json
{
    "ActionId": 3101,
    "SkillId": 2101,
    "AttackBoxInfo": "",
    "TriggerFrames": "Frame15:Direct:4101"
}
```

**SkillEffectTable**：
```json
{
    "SkillEffectId": 4101,
    "EffectType": 1,
    "EffectValue": 200.0,
    "TargetType": 1,
    "EffectRange": 3.0,
    "EffectParams": "{\"DamageType\":\"Magical\",\"AOE\":true,\"AOERadius\":3.0}"
}
```

**处理器扩展**：
```csharp
// DamageEffectHandler 中支持 AOE
if (effectParams.AOE)
{
    var targets = PhysicsWorld.OverlapSphere(
        target.Transform.position, 
        effectParams.AOERadius,
        LayerMask.GetMask("Enemy")
    );
    
    foreach (var t in targets)
    {
        ApplyDamage(t, damageResult.FinalDamage);
    }
}
```

---

## 附录

### A. 与现有系统集成

| 现有系统 | 集成点 | 说明 |
|---------|-------|------|
| **Action系统** | SkillExecutorCapability | 在 SkillAction 的 Update 中调用 SkillExecutorCapability |
| **ECS系统** | Entity组件 | HitManager 和 EffectHandler 操作 Entity 组件 |
| **事件系统** | 效果触发 | 伤害、治疗等事件通过 EventSystem 广播 |
| **Buff系统** | BuffEffectHandler | 调用 BuffComponent 添加/移除 Buff |
| **特效系统** | EffectHandler | 播放视觉效果和音效 |

### B. 性能指标

| 指标 | 目标 | 说明 |
|-----|------|------|
| HitBox数量 | <100 | 同时活动的攻击盒数量 |
| 碰撞检测耗时 | <1ms/帧 | 所有HitBox的检测总耗时 |
| 效果队列处理 | <0.5ms/帧 | 处理所有效果的总耗时 |
| 内存占用 | <5MB | 运行时数据占用 |

### C. 未来扩展方向

1. **预测性碰撞检测** - 支持高速移动的攻击判定
2. **碰撞回调扩展** - 支持穿透、反弹等特殊碰撞行为
3. **效果预演系统** - 编辑器中实时预览技能效果
4. **录像回放** - 记录和回放技能执行过程

---

## 12. 依赖系统

### 12.1 物理碰撞检测系统

- HitManager 即时查询
- Box/Sphere Overlap 查询
- 碰撞过滤与去重
- CollisionShape 数据结构
- 配置表集成

### 12.2 配置系统

- CollisionData 支持
- EntityModelTable 可用
- ConfigManager 单例初始化

### 12.3 编辑器工具

- 角色编辑器：碰撞盒生成和预览
- 数据持久化：保存/加载碰撞数据

### 12.4 技能配置表

- SkillTable、SkillActionTable、SkillEffectTable

### 12.5 动作系统集成

- 需要理解 ActionTable 结构
- 影响 SkillExecutorCapability 设计

### 12.6 属性系统

- 角色属性读取接口
- 影响 DamageCalculator

### 12.7 RotationComponent

- 实体旋转组件
- 影响碰撞检测精度

---

**文档版本**: v2.0  
**维护者**: Astrum Team  
**创建日期**: 2025-10-09  
**最后更新**: 2025-10-10

© Astrum Skill Effect Runtime Design Document

