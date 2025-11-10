# 射击系统技术设计

> 📖 **版本**: v1.0 | **最后更新**: 2025-11-10  
> 🎯 **适用范围**: 法师施法、弹道抛射物、射击类技能  
> 👥 **面向读者**: 系统设计师、战斗程序员  
> ✅ **目标**: 理解射击系统的多阶段动作机制、弹道实体设计和碰撞触发流程

**TL;DR**
- 射击技能基于Action系统，拆分为多个动作阶段（前摇、释放、后摇）
- 支持连射（循环释放动作）和蓄力（基于Condition触发不同效果）
- Projectile作为Entity实现，支持多种轨迹类型（直线、抛物线、追踪）
- 基于触发帧机制生成弹道，通过碰撞检测触发技能效果
- 完全复用现有的Action系统、触发帧系统和HitManager

---

## 1. 概述

射击系统是基于当前技能动作体系的扩展，用于实现法师施法、弹道抛射物等射击类技能。系统的核心设计理念是：**将射击技能拆分为多个独立的动作阶段，通过Action系统的动作切换机制实现流畅的射击流程**。

### 1.1 设计背景

当前技能系统采用三层架构（Skill → SkillAction → SkillEffect），所有技能基于Action系统执行。射击系统需要：
1. 支持多阶段动作（前摇、释放、后摇）
2. 支持连射和蓄力等复杂输入模式
3. 生成可独立运动的弹道实体
4. 碰撞检测和效果触发

### 1.2 核心目标

- **多阶段动作支持** - 前摇、释放、后摇作为独立动作，支持取消和衔接
- **连射机制** - 持续输入时循环执行释放动作
- **蓄力机制** - 基于蓄力时长触发不同强度的效果
- **弹道实体化** - Projectile作为Entity，支持复杂运动轨迹
- **物理碰撞集成** - 复用HitManager和SkillEffect系统

### 1.3 系统边界

- ✅ 负责：射击动作流程控制、弹道生成与运动、碰撞检测和效果触发
- ❌ 不负责：具体的技能效果计算（由SkillEffect系统处理）、动画表现（由动画系统处理）

---

## 2. 核心概念

### 2.1 多阶段射击动作

射击技能拆分为多个独立的Action，通过动作切换机制衔接：

```
射击动作阶段划分
├── 前摇动作 (PrecastAction)
│   ├── 播放施法动画
│   ├── 可被其他动作取消（BeCancelledTag）
│   └── 完成后自动切换到释放动作（AutoNextActionId）
│
├── 释放动作 (CastAction)
│   ├── 在触发帧生成Projectile实体（TriggerFrame）
│   ├── 播放释放特效和音效
│   ├── 支持循环执行（连射）或单次执行
│   └── 停止输入后切换到后摇动作
│
└── 后摇动作 (RecoveryAction)
    ├── 播放收招动画
    ├── 可被其他动作取消
    └── 完成后恢复到Idle状态
```

### 2.2 连射机制

**实现方式**：释放动作在尾声设置可取消标签，通过自身的CancelTag取消自己实现循环

```
连射流程
前摇动作 → 释放动作(尾声BeCancelledTag) → [持续输入+CancelTag匹配] → 释放动作 → [停止输入] → 后摇动作
           ↑________________________________________________|
           自我取消循环（CancelTag匹配自己的BeCancelledTag）
```

**关键配置**：
- `CastAction.BeCancelledTags` = 在动作尾声（如最后5-10帧）设置 `["Skill1Action"]`
- `CastAction.CancelTags` = `["Skill1Action"]`（可以取消自己）
- `CastAction.Commands` = 要求输入命令存在（如 "skill1"）
- 持续输入时，在尾声阶段触发自我取消，重新执行动作
- 停止输入时，命令失效，无法触发取消，动作自然结束后进入后摇或Idle

### 2.3 蓄力机制

**实现方式**：基于Condition类型的触发帧，根据蓄力时长判断触发哪个效果

```
蓄力流程
蓄力动作（ChargingAction）
├── 每帧累加蓄力时长（存储在Component中）
├── 松开输入时：
│   ├── 检查蓄力时长
│   └── 根据Condition触发不同的TriggerFrame
└── 生成对应强度的Projectile

触发帧配置示例：
- Frame15:Condition(ChargeTime<30):4001  → 弱火球
- Frame15:Condition(ChargeTime>=30&&ChargeTime<60):4002 → 中火球
- Frame15:Condition(ChargeTime>=60):4003 → 强火球
```

### 2.4 Projectile（弹道实体）

**核心设计**：Projectile作为Entity，挂载专用组件和能力

```
Projectile实体结构
Entity (Projectile)
├── TransComponent（位置/朝向）
├── ProjectileComponent（弹道数据）
│   ├── SkillEffectIds（触发的效果ID集合）
│   ├── CasterId（施法者ID）
│   ├── LifeTime（生命周期）
│   ├── TrajectoryType（轨迹类型）
│   └── TrajectoryData（轨迹参数）
├── CollisionComponent（碰撞体）
│   ├── CollisionShape（碰撞形状）
│   └── IsTrigger（触发器模式）
└── ProjectileCapability（弹道能力）
    ├── 更新运动轨迹
    ├── 检测碰撞
    └── 触发效果和销毁
```

---

## 3. 架构设计

### 3.1 系统层次结构

```
射击系统架构
├── ShootingAction（射击动作层）
│   ├── PrecastAction（前摇动作）
│   ├── CastAction（释放动作）
│   ├── RecoveryAction（后摇动作）
│   └── ChargingAction（蓄力动作）
│
├── Projectile Entity（弹道实体层）
│   ├── ProjectileComponent（弹道组件）
│   ├── ProjectileCapability（弹道能力）
│   └── TrajectorySystem（轨迹系统）
│
├── ProjectileSpawnCapability（抛射物生成能力）
│   ├── 监听ProjectileSpawnRequest事件
│   └── 调用实体工厂创建Projectile
│
├── EntityFactory（统一实体工厂）
│   ├── 根据Archetype创建Projectile实体
│   ├── 注入ProjectileSpawnContext
│   └── 统一注册到World/EntityManager
│
└── 触发与碰撞（复用现有系统）
    ├── SkillExecutorCapability（触发帧处理）
    ├── HitManager（碰撞检测）
    └── SkillEffectManager（效果触发）
```

### 3.2 数据流向

```
1. 玩家输入 → ActionCapability 切换到PrecastAction
2. PrecastAction完成 → 自动切换到CastAction
3. CastAction触发帧 → SkillExecutorCapability处理
4. SkillExecutorCapability 发布 `ProjectileSpawnRequestEvent`
5. ProjectileSpawnCapability → EntityFactory.CreateByArchetype()
6. Projectile Entity → ProjectileCapability.Tick()
7. 每帧更新位置 → 碰撞检测（HitManager或内置检测）
8. 碰撞命中 → 触发SkillEffect → 销毁Projectile
```

---

## 4. 组件与能力设计

### 4.1 ProjectileComponent（弹道组件）

**职责**：存储弹道实体的配置数据

```csharp
/// <summary>
/// 弹道组件 - 存储弹道实体的配置和状态
/// </summary>
[MemoryPackable]
public partial class ProjectileComponent : BaseComponent
{
    /// <summary>技能效果ID列表（碰撞时触发）</summary>
    [MemoryPackAllowSerialize]
    public List<int> SkillEffectIds { get; set; } = new List<int>();
    
    /// <summary>施法者实体ID</summary>
    public long CasterId { get; set; } = 0;
    
    /// <summary>生命周期（帧数）</summary>
    public int LifeTime { get; set; } = 300; // 5秒（60fps）
    
    /// <summary>已存活帧数</summary>
    public int ElapsedFrames { get; set; } = 0;
    
    /// <summary>轨迹类型</summary>
    public TrajectoryType TrajectoryType { get; set; } = TrajectoryType.Linear;
    
    /// <summary>轨迹参数（JSON）</summary>
    public string TrajectoryData { get; set; } = string.Empty;
    
    /// <summary>发射方向（运行时写入）</summary>
    public TSVector LaunchDirection { get; set; } = TSVector.forward;
    
    /// <summary>当前速度（帧更新使用）</summary>
    public TSVector CurrentVelocity { get; set; } = TSVector.zero;
    
    /// <summary>碰撞检测模式</summary>
    public ProjectileCollisionMode CollisionMode { get; set; } = ProjectileCollisionMode.Continuous;
    
    /// <summary>穿透次数（0=不穿透）</summary>
    public int PierceCount { get; set; } = 0;
    
    /// <summary>已穿透次数</summary>
    public int PiercedCount { get; set; } = 0;
    
    /// <summary>已命中的实体ID列表（防重复命中）</summary>
    [MemoryPackAllowSerialize]
    public HashSet<long> HitEntities { get; set; } = new HashSet<long>();
}

/// <summary>
/// 轨迹类型
/// </summary>
public enum TrajectoryType
{
    Linear = 0,      // 直线
    Parabola = 1,    // 抛物线
    Homing = 2,      // 追踪
    Spiral = 3       // 螺旋
}

/// <summary>
/// 碰撞检测模式
/// </summary>
public enum ProjectileCollisionMode
{
    Continuous = 0,  // 连续检测（每帧）
    Discrete = 1,    // 离散检测（固定间隔）
    OnlyTarget = 2   // 仅检测目标层
}

/// <summary>
/// 直线轨迹参数
/// </summary>
[MemoryPackable]
public partial class LinearTrajectoryData
{
    public FP BaseSpeed { get; set; } = FP.FromFloat(0.7f);
    public TSVector Direction { get; set; } = TSVector.forward;
}

/// <summary>
/// 抛物线轨迹参数
/// </summary>
[MemoryPackable]
public partial class ParabolicTrajectoryData
{
    public FP LaunchSpeed { get; set; } = FP.FromFloat(0.9f);
    public TSVector Direction { get; set; } = TSVector.forward;
    public TSVector Gravity { get; set; } = new TSVector(0, -0.05, 0);
}

/// <summary>
/// 追踪轨迹参数
/// </summary>
[MemoryPackable]
public partial class HomingTrajectoryData
{
    public long TargetEntityId { get; set; } = 0;
    public FP BaseSpeed { get; set; } = FP.FromFloat(0.6f);
    public FP TurnRate { get; set; } = FP.FromFloat(0.1f); // 转向速率
}
```

### 4.3 与实体工厂的集成

**职责**：复用现有实体工厂（`EntityFactory.CreateByArchetype`）创建Projectile实体，保持统一的实体生命周期管理

```csharp
/// <summary>
/// 弹道生成上下文参数
/// </summary>
public sealed class ProjectileSpawnContext
{
    public int ProjectileId { get; init; }
    public List<int> SkillEffectIds { get; init; } = new List<int>();
    public long CasterId { get; init; }
    public TSVector SpawnPosition { get; init; }
    public TSVector SpawnDirection { get; init; }
    public string? OverrideTrajectoryData { get; init; } // 可选地覆写表数据
}

/// <summary>
/// Projectile 配置定义（来自 ProjectileTable）
/// </summary>
public sealed record ProjectileDefinition
{
    public int ProjectileId { get; init; }
    public string ProjectileName { get; init; } = string.Empty;
    public string ProjectileArchetype { get; init; } = string.Empty;
    public int LifeTime { get; init; } = 300;
    public TrajectoryType TrajectoryType { get; init; } = TrajectoryType.Linear;
    public string TrajectoryData { get; init; } = string.Empty;
    public ProjectileCollisionMode CollisionMode { get; init; } = ProjectileCollisionMode.Continuous;
    public int PierceCount { get; init; } = 0;
    public CollisionShape CollisionShape { get; init; } = CollisionShape.CreateSphere(FP.FromFloat(0.5f));
    public IReadOnlyList<int> DefaultEffectIds { get; init; } = Array.Empty<int>();
}

public sealed class ProjectileConfigManager
{
    public static ProjectileConfigManager Instance { get; } = new ProjectileConfigManager();

    private readonly Dictionary<int, ProjectileDefinition> _definitions = new();

    public ProjectileDefinition? GetDefinition(int projectileId) =>
        _definitions.TryGetValue(projectileId, out var def) ? def : null;
}

```

**设计要点**：
- Projectile Archetype 中预挂 `ProjectileComponent`、`TransComponent`、`CollisionComponent` 等必需组件
- `EntityCreationParams.ExtraData` 传入 `ProjectileSpawnContext`，由 `ProjectileSpawnCapability` 或自定义初始器在 `OnAttached` 时读取
- 实体工厂内部无需 `EntityConfigId`：Projectile 通过专用表驱动，基础单位通过 `EntityConfigComponent`
- 统一由 `EntityManager` 负责回收/池化，避免重复实现对象池

### 4.4 EntityConfigComponent（基础单位专用）

**背景**：原本 `Entity` 持有 `EntityConfigId` 与 `EntityConfig` 引用，导致所有实体（含Projectile）都需要配置表 ID。为了让弹道实体完全独立于角色配置，新增 `EntityConfigComponent` 并仅挂载在 `BaseUnit` 类实体上。

```csharp
/// <summary>
/// 仅用于具备实体配置（cfg.Entity）的基础单位
/// </summary>
[MemoryPackable]
public partial class EntityConfigComponent : BaseComponent
{
    public int EntityConfigId { get; set; }

    [MemoryPackIgnore]
    public EntityBaseTable? EntityConfig =>
        EntityConfigId == 0 ? null : TableConfig.Instance.Tables.TbEntityBaseTable.Get(EntityConfigId);
}
```

- `Entity` 基类去除 `EntityConfigId`/`EntityConfig` 字段，转而通过是否存在 `EntityConfigComponent` 判断是否为角色类实体
- 弹道、召唤物等没有配置表依赖的实体无需再携带多余字段
- `EntityFactory.CreateByArchetype` 支持“不带 EntityConfigId” 创建逻辑；当需要配置表时，在 `EntityCreationParams` 中显式传入并由 `EntityConfigComponent` 初始化

### 4.5 SocketRefs MonoBehaviour

**用途**：挂载在角色模型的根节点上，缓存模型上的关键绑点（手部、法杖顶端、背部等），供抛射物生成时快速获取世界空间位置与朝向，避免逐帧查找或硬编码。

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Socket 引用管理：挂在角色模型上，提供命名绑点访问能力
/// </summary>
public sealed class SocketRefs : MonoBehaviour
{
    [System.Serializable]
    public struct SocketBinding
    {
        public string Name;
        public Transform Transform;
    }

    [SerializeField]
    private List<SocketBinding> _bindings = new List<SocketBinding>();

    private readonly Dictionary<string, Transform> _lookup = new Dictionary<string, Transform>();

    private void Awake()
    {
        foreach (var binding in _bindings)
        {
            if (!string.IsNullOrEmpty(binding.Name) && binding.Transform != null)
            {
                _lookup[binding.Name] = binding.Transform;
            }
        }
    }

    public bool TryGetWorldPosition(string socketName, out Vector3 position, out Vector3 forward)
    {
        position = default;
        forward = default;
        if (string.IsNullOrEmpty(socketName)) return false;

        if (_lookup.TryGetValue(socketName, out var transform) && transform != null)
        {
            position = transform.position;
            forward = transform.forward;
            return true;
        }
        return false;
    }
}
```

**最佳实践**：
- 由美术/动画同学在Prefab上维护 `_bindings` 列表，与真实骨骼名称解耦（可使用别名）。
- ViewBridge/Factory 在实例化角色模型后，将 `GameObject` 记录在实体视图映射中，供 `ProjectileSpawnCapability` 根据 `socketName` 查询。
- 如果触发帧未指定 `SocketName`，则回退使用 `TransComponent` 的位置与当前朝向。

---

## 5. 射击动作流程设计

### 5.1 连射流程

**配置示例**：火球连射

```json
// ActionTable - 前摇动作
{
    "ActionId": 5001,
    "ActionType": "Skill",
    "Catalog": "Shooting",
    "TotalFrames": 20,
    "AnimationName": "FireBall_Precast",
    "AutoNextActionId": 5002,  // 完成后自动切换到释放动作
    "CancelTags": ["Idle", "Move"],
    "BeCancelledTags": ["Roll", "Dash"]
}

// ActionTable - 释放动作
{
    "ActionId": 5002,
    "ActionType": "Skill",
    "Catalog": "Shooting",
    "TotalFrames": 30,
    "AnimationName": "FireBall_Cast",
    "AutoNextActionId": 5003,  // 完成后进入后摇
    "Commands": ["skill1"],    // 需要skill1输入才能执行
    "CancelTags": ["Idle", "Move", "Skill1Action"],  // 可以取消自己
    "BeCancelledTags": [
        {"Tag": "Skill1Action", "StartFrame": 25, "EndFrame": 30},  // 尾声可自我取消
        {"Tag": "Roll", "StartFrame": 0, "EndFrame": 30},
        {"Tag": "Dash", "StartFrame": 0, "EndFrame": 30}
    ]
}

// SkillActionTable - 释放动作配置
{
    "ActionId": 5002,
    "SkillId": 3001,
    "TriggerFrames": "Frame10:Direct:4101(Socket:StaffTip)"  // 第10帧在法杖顶端Socket生成弹道
}

// ActionTable - 后摇动作
{
    "ActionId": 5003,
    "ActionType": "Skill",
    "Catalog": "Shooting",
    "TotalFrames": 15,
    "AnimationName": "FireBall_Recovery",
    "AutoNextActionId": 0,  // 完成后回到Idle
    "BeCancelledTags": ["Roll", "Dash", "Attack"]
}
```

**执行流程**：

```
玩家按下Skill1 → PrecastAction(5001)
                    ↓ (完成)
                CastAction(5002) 生成Projectile
                    ↓ (到达第25-30帧尾声，持续按住Skill1)
                CastAction自我取消，重新执行 → 生成Projectile（循环）
                    ↓ (到达尾声，松开Skill1，命令失效，无法触发取消)
                CastAction完成 → RecoveryAction(5003)
                    ↓ (完成)
                Idle
```

### 5.2 蓄力流程

**配置示例**：蓄力火球

```json
// ActionTable - 蓄力动作
{
    "ActionId": 6001,
    "ActionType": "Skill",
    "Catalog": "Charging",
    "TotalFrames": 180,  // 最大蓄力3秒
    "AnimationName": "ChargedFireBall_Charge",
    "LoopAnimation": true,  // 循环播放蓄力动画
    "Commands": ["skill2"],  // 需要持续按住skill2
    "BeCancelledTags": ["Roll", "Dash"]
}

// SkillActionTable - 蓄力动作配置
{
    "ActionId": 6001,
    "SkillId": 3002,
    "TriggerFrames": "Frame0:Condition(ChargeTime<30):4201,Frame0:Condition(ChargeTime>=30&&ChargeTime<60):4202,Frame0:Condition(ChargeTime>=60):4203"
}

// ActionTable - 释放动作
{
    "ActionId": 6002,
    "ActionType": "Skill",
    "Catalog": "Charging",
    "TotalFrames": 20,
    "AnimationName": "ChargedFireBall_Release",
    "AutoNextActionId": 0
}
```

**执行流程**：

```
玩家按住Skill2 → ChargingAction(6001)
                    ├── 每帧累加蓄力时长（存储在ChargingComponent）
                    ├── 循环播放蓄力动画
                    └── 蓄力特效渐强
                    ↓ (松开Skill2，触发Condition检测)
                根据蓄力时长选择TriggerFrame
                    ├── ChargeTime < 30帧 → 触发EffectId 4201（弱）
                    ├── 30 <= ChargeTime < 60 → 触发EffectId 4202（中）
                    └── ChargeTime >= 60 → 触发EffectId 4203（强）
                    ↓
                ReleaseAction(6002) 生成对应Projectile
                    ↓ (完成)
                Idle
```

**ChargingComponent设计**：

```csharp
/// <summary>
/// 蓄力组件 - 存储蓄力状态
/// </summary>
[MemoryPackable]
public partial class ChargingComponent : BaseComponent
{
    /// <summary>当前蓄力时长（帧数）</summary>
    public int ChargeTime { get; set; } = 0;
    
    /// <summary>是否正在蓄力</summary>
    public bool IsCharging { get; set; } = false;
    
    /// <summary>
    /// 重置蓄力
    /// </summary>
    public void Reset()
    {
        ChargeTime = 0;
        IsCharging = false;
    }
}
```

---

## 6. SkillExecutorCapability扩展

为了支持射击系统，需要扩展`SkillExecutorCapability`以处理Direct类型的触发帧，并通过事件请求抛射物生成。

### 6.1 扩展HandleDirectTrigger方法

```csharp
/// <summary>
/// 处理直接触发 - 通过事件请求弹道生成
/// </summary>
private void HandleDirectTrigger(Entity caster, TriggerFrameInfo trigger)
{
    if (trigger.EffectIds == null || trigger.EffectIds.Length == 0)
        return;

    var effectConfig = SkillConfigManager.GetSkillEffect(trigger.EffectIds[0]);
    if (effectConfig == null)
        return;

    if (TryCreateProjectileRequest(caster, trigger, effectConfig, out var request))
    {
        EventSystem.Instance.Publish(request);
        return;
    }

    // 非弹道类型，沿用原有逻辑
    TriggerSkillEffect(caster, caster, trigger.EffectIds[0]);
}

private bool TryCreateProjectileRequest(Entity caster, TriggerFrameInfo trigger, SkillEffectConfig effectConfig, out ProjectileSpawnRequestEvent request)
{
    request = default;
    if (!IsProjectileEffect(effectConfig))
        return false;

    var spawn = CalculateProjectileSpawnTransform(caster, trigger);

    request = new ProjectileSpawnRequestEvent
    {
        CasterEntityId = caster.UniqueId,
        SkillEffectId = effectConfig.SkillEffectId,
        EffectParamsJson = effectConfig.EffectParams ?? string.Empty,
        TriggerInfo = trigger,
        SpawnPosition = spawn.Position,
        SpawnDirection = spawn.Direction
    };
    return true;
}

private bool IsProjectileEffect(SkillEffectConfig effectConfig)
{
    if (string.IsNullOrEmpty(effectConfig.EffectParams))
        return false;

    try
    {
        var paramsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(effectConfig.EffectParams);
        return paramsDict != null && paramsDict.ContainsKey("ProjectileId");
    }
    catch
    {
        return false;
    }
}

/// <summary>
/// 计算弹道生成的空间信息（技能逻辑层负责）
/// </summary>
private (TSVector Position, TSVector Direction) CalculateProjectileSpawnTransform(Entity caster, TriggerFrameInfo trigger)
{
    var trans = caster.GetComponent<TransComponent>();
    var direction = GetCasterDirection(caster);
    TSVector spawnPos = trans?.Position ?? TSVector.zero;

    if (!string.IsNullOrEmpty(trigger.SocketName))
    {
        // 从 View 端的 SocketRefs 读取绑定点
        var viewObject = ViewBridge.GetViewObject(caster.UniqueId);
        var socketRefs = viewObject?.GetComponent<SocketRefs>();
        if (socketRefs != null && socketRefs.TryGetWorldPosition(trigger.SocketName, out var socketPos, out var socketForward))
        {
            spawnPos = TSVector.FromVector3(socketPos);
            direction = TSVector.FromVector3(socketForward).normalized;
        }
    }

    return (spawnPos, direction);
}
```

### 6.2 ProjectileSpawnRequest 事件

`SkillExecutorCapability` 只负责发出弹道生成请求。事件负载包含施法者、技能效果、生成位置等信息，由独立能力解耦处理：

```csharp
[MemoryPackable]
public partial struct ProjectileSpawnRequestEvent : IGameEvent
{
    public long CasterEntityId { get; set; }
    public int SkillEffectId { get; set; }
    public string EffectParamsJson { get; set; }
    public TriggerFrameInfo TriggerInfo { get; set; }
    public TSVector SpawnPosition { get; set; }
    public TSVector SpawnDirection { get; set; }
}
```

### 6.3 ProjectileSpawnCapability（抛射物生成能力）

该能力挂载在角色或全局战斗管理实体上，监听 `ProjectileSpawnRequestEvent`，统一调用实体工厂生成弹道，并负责初始化运行时数据。

```csharp
public class ProjectileSpawnCapability : Capability<ProjectileSpawnCapability>
{
    protected override void RegisterEventHandlers()
    {
        RegisterEventHandler<ProjectileSpawnRequestEvent>(OnProjectileSpawnRequested);
    }

    private void OnProjectileSpawnRequested(ProjectileSpawnRequestEvent request)
    {
        var world = Entity.World;
        if (world == null)
            return;

        var caster = world.GetEntityById(request.CasterEntityId);
        if (caster == null)
            return;

        var effectConfig = SkillConfigManager.GetSkillEffect(request.SkillEffectId);
        if (effectConfig == null)
            return;

        var definition = ResolveProjectileDefinition(effectConfig, request.EffectParamsJson);
        if (definition == null)
            return;

        var effectIds = ResolveProjectileEffectIds(effectConfig, definition, request.EffectParamsJson, request.TriggerInfo);
        var projectile = CreateProjectileEntityViaFactory(
            caster,
            definition,
            effectIds,
            request.SpawnPosition,
            request.SpawnDirection);

        if (projectile == null)
        {
            ASLogger.Instance.Error($"ProjectileSpawnCapability: failed to create projectile (Id={definition.ProjectileId})");
            return;
        }

        InitializeProjectileRuntime(projectile, caster, effectIds, definition, (request.SpawnPosition, request.SpawnDirection));
    }

    private ProjectileDefinition? ResolveProjectileDefinition(SkillEffectConfig effectConfig, string overrideJson)
    {
        try
        {
            var rawJson = string.IsNullOrEmpty(overrideJson) ? effectConfig.EffectParams : overrideJson;
            if (string.IsNullOrEmpty(rawJson))
                return null;

            var paramsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(rawJson);
            if (paramsDict == null || !paramsDict.TryGetValue("ProjectileId", out var projectileIdObj))
                return null;

            var projectileId = Convert.ToInt32(projectileIdObj);
            var definition = ProjectileConfigManager.Instance.GetDefinition(projectileId);
            if (definition == null)
                return null;

            if (paramsDict.TryGetValue("TrajectoryOverride", out var overrideObj) && overrideObj is System.Text.Json.Nodes.JsonObject jsonOverride)
            {
                var mergedData = MergeTrajectoryOverride(definition.TrajectoryType, definition.TrajectoryData, jsonOverride);
                definition = definition with { TrajectoryData = mergedData };
            }

            return definition;
        }
        catch (Exception ex)
        {
            ASLogger.Instance.Error($"ResolveProjectileDefinition failed: {ex.Message}");
            return null;
        }
    }

    private IReadOnlyList<int> ResolveProjectileEffectIds(SkillEffectConfig effectConfig, ProjectileDefinition definition, string overrideJson, TriggerFrameInfo trigger)
    {
        var result = new List<int>();
        if (definition.DefaultEffectIds != null)
        {
            result.AddRange(definition.DefaultEffectIds);
        }

        if (!result.Contains(effectConfig.SkillEffectId))
        {
            result.Add(effectConfig.SkillEffectId);
        }

        if (trigger?.EffectIds != null)
        {
            foreach (var id in trigger.EffectIds)
            {
                if (!result.Contains(id))
                {
                    result.Add(id);
                }
            }
        }

        try
        {
            if (!string.IsNullOrEmpty(overrideJson))
            {
                var paramsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(overrideJson);
                if (paramsDict != null && paramsDict.TryGetValue("AdditionalEffectIds", out var additionalObj) && additionalObj is System.Text.Json.Nodes.JsonArray jsonArray)
                {
                    foreach (var node in jsonArray)
                    {
                        if (node != null && int.TryParse(node.ToString(), out var id) && !result.Contains(id))
                        {
                            result.Add(id);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ASLogger.Instance.Warning($"ResolveProjectileEffectIds parse override failed: {ex.Message}", "Projectile.Spawn");
        }

        return result;
    }

    private Entity? CreateProjectileEntityViaFactory(Entity caster, ProjectileDefinition config, IReadOnlyList<int> skillEffectIds, TSVector muzzlePos, TSVector shootDir)
    {
        var world = caster.World;
        if (world?.EntityFactory == null)
            return null;

        var spawnContext = new ProjectileSpawnContext
        {
            ProjectileId = config.ProjectileId,
            SkillEffectIds = new List<int>(skillEffectIds),
            CasterId = caster.UniqueId,
            SpawnPosition = muzzlePos,
            SpawnDirection = shootDir,
            OverrideTrajectoryData = config.TrajectoryData
        };

        return world.EntityFactory.CreateByArchetype(
            archetypeName: config.ProjectileArchetype,
            creationParams: new EntityCreationParams
            {
                SpawnPosition = muzzlePos,
                ExtraData = spawnContext
            });
    }

    private void InitializeProjectileRuntime(Entity projectile, Entity caster, IReadOnlyList<int> effectIds, ProjectileDefinition definition, (TSVector Position, TSVector Direction) spawn)
    {
        var projectileComponent = projectile.GetComponent<ProjectileComponent>();
        if (projectileComponent != null)
        {
            projectileComponent.SkillEffectIds.Clear();
            projectileComponent.SkillEffectIds.AddRange(effectIds);
            projectileComponent.CasterId = caster.UniqueId;
            projectileComponent.LifeTime = definition.LifeTime;
            projectileComponent.TrajectoryType = definition.TrajectoryType;
            projectileComponent.TrajectoryData = OverrideTrajectoryData(definition, spawn.Direction);
            projectileComponent.CollisionMode = definition.CollisionMode;
            projectileComponent.PierceCount = definition.PierceCount;
            projectileComponent.LaunchDirection = spawn.Direction.normalized;
            projectileComponent.CurrentVelocity = ComputeInitialVelocity(projectileComponent);
        }

        var trans = projectile.GetComponent<TransComponent>();
        if (trans != null)
        {
            trans.Position = spawn.Position;
        }

        var collisionComponent = projectile.GetComponent<CollisionComponent>();
        if (collisionComponent != null)
        {
            collisionComponent.CollisionShape = definition.CollisionShape;
            collisionComponent.IsTrigger = true;
        }
    }
}

private string OverrideTrajectoryData(ProjectileDefinition definition, TSVector shootDir)
{
    var normalizedDir = shootDir.normalized;

    if (!string.IsNullOrEmpty(definition.TrajectoryData))
    {
        return ApplyDirectionToTrajectory(definition.TrajectoryType, definition.TrajectoryData, normalizedDir);
    }

    switch (definition.TrajectoryType)
    {
        case TrajectoryType.Linear:
        {
            var data = ParseTrajectoryData<LinearTrajectoryData>(definition.TrajectoryData);
            data.Direction = normalizedDir;
            return System.Text.Json.JsonSerializer.Serialize(data);
        }
        case TrajectoryType.Parabola:
        {
            var data = ParseTrajectoryData<ParabolicTrajectoryData>(definition.TrajectoryData);
            data.Direction = normalizedDir;
            return System.Text.Json.JsonSerializer.Serialize(data);
        }
        case TrajectoryType.Homing:
        {
            var data = ParseTrajectoryData<HomingTrajectoryData>(definition.TrajectoryData);
            return System.Text.Json.JsonSerializer.Serialize(data); // 追踪方向由能力实时计算
        }
        default:
            return definition.TrajectoryData;
    }
}

private string ApplyDirectionToTrajectory(TrajectoryType trajectoryType, string baseData, TSVector direction)
{
    switch (trajectoryType)
    {
        case TrajectoryType.Linear:
        {
            var data = ParseTrajectoryData<LinearTrajectoryData>(baseData);
            data.Direction = direction;
            return System.Text.Json.JsonSerializer.Serialize(data);
        }
        case TrajectoryType.Parabola:
        {
            var data = ParseTrajectoryData<ParabolicTrajectoryData>(baseData);
            data.Direction = direction;
            return System.Text.Json.JsonSerializer.Serialize(data);
        }
        default:
            return baseData;
    }
}

private string MergeTrajectoryOverride(TrajectoryType type, string baseData, System.Text.Json.Nodes.JsonObject overrideObj)
{
    var baseNode = string.IsNullOrEmpty(baseData)
        ? new System.Text.Json.Nodes.JsonObject()
        : System.Text.Json.JsonNode.Parse(baseData)?.AsObject() ?? new System.Text.Json.Nodes.JsonObject();

    foreach (var kv in overrideObj)
    {
        baseNode[kv.Key] = kv.Value?.Clone();
    }

    return baseNode.ToJsonString();
}

private TSVector ComputeInitialVelocity(ProjectileComponent projectileComponent)
{
    switch (projectileComponent.TrajectoryType)
    {
        case TrajectoryType.Linear:
        {
            var linear = ParseTrajectoryData<LinearTrajectoryData>(projectileComponent.TrajectoryData);
            return linear.Direction.normalized * linear.BaseSpeed;
        }
        case TrajectoryType.Parabola:
        {
            var parabolic = ParseTrajectoryData<ParabolicTrajectoryData>(projectileComponent.TrajectoryData);
            return parabolic.Direction.normalized * parabolic.LaunchSpeed;
        }
        case TrajectoryType.Homing:
        {
            var homing = ParseTrajectoryData<HomingTrajectoryData>(projectileComponent.TrajectoryData);
            return homing.BaseSpeed * projectileComponent.LaunchDirection.normalized;
        }
        default:
            return TSVector.zero;
    }
}
```

---

## 7. 配置表设计

### 7.1 SkillEffectTable扩展

**EffectParams字段示例**（Projectile类型）：

```json
{
    "SkillEffectId": 4101,
    "EffectType": 1,  // 伤害
    "EffectValue": 150.0,
    "TargetType": 1,  // 敌人
    "EffectParams": "{
        \"ProjectileId\": 7001,
        \"TrajectoryOverride\": {
            \"Linear\": {
                \"BaseSpeed\": 0.9
            }
        },
        \"AdditionalEffectIds\": [4102, 4103]
    }",
    "VisualEffectId": 5101,
    "SoundEffectId": 6101
}
```

### 7.2 ProjectileTable

Projectile 专用配置表驱动实体工厂：

```json
{
    "ProjectileId": 7001,
    "ProjectileName": "FireBall",
    "ProjectileArchetype": "Projectile.FireBall",
    "LifeTime": 300,
    "TrajectoryType": "Linear",
    "TrajectoryData": "{\"BaseSpeed\":0.8}",
    "CollisionShape": "Sphere:0.5",
    "CollisionMode": "Continuous",
    "PierceCount": 0,
    "DefaultEffectIds": [4101],
    "TrailEffectId": 5102,
    "HitEffectId": 5103
}
```

`SkillEffectTable` 的 `EffectParams` 只需提供 `ProjectileId`，其余数据由 `ProjectileConfigManager` 加载；如需覆写基础速度等参数，使用 `TrajectoryOverride` 字段增量覆盖。
若需附加额外技能效果，可通过 `AdditionalEffectIds` 指定，由运行时合并到 `ProjectileComponent.SkillEffectIds`。

---

## 8. 轨迹系统详细设计

### 8.1 直线轨迹（Linear）

**实现**：匀速直线运动

```csharp
private void UpdateLinearTrajectory(Entity entity, ProjectileComponent component, TransComponent trans)
{
    trans.Position += component.CurrentVelocity;
}
```

**配置示例**：
```json
{
    "TrajectoryType": "Linear",
    "TrajectoryData": "{\"BaseSpeed\":0.8}"
}
```

### 8.2 抛物线轨迹（Parabola）

**实现**：受重力影响的抛物线运动

```csharp
private void UpdateParabolicTrajectory(Entity entity, ProjectileComponent component, TransComponent trans)
{
    var trajectoryParams = ParseTrajectoryData<ParabolicTrajectoryData>(component.TrajectoryData);
    
    // 应用重力
    component.CurrentVelocity += trajectoryParams.Gravity;
    
    // 更新位置
    trans.Position += component.CurrentVelocity;
}
```

**配置示例**：
```json
{
    "TrajectoryType": "Parabola",
    "TrajectoryData": "{\"LaunchSpeed\":0.5,\"Gravity\":[0,-0.05,0]}"
}
```

### 8.3 追踪轨迹（Homing）

**实现**：朝向目标转向的追踪运动

```csharp
private void UpdateHomingTrajectory(Entity entity, ProjectileComponent component, TransComponent trans)
{
    var trajectoryParams = ParseTrajectoryData<HomingTrajectoryData>(component.TrajectoryData);
    
    // 查找目标
    var targetEntity = entity.World.GetEntityById(trajectoryParams.TargetEntityId);
    if (targetEntity != null && !targetEntity.IsDestroyed)
    {
        var targetTrans = targetEntity.GetComponent<TransComponent>();
        if (targetTrans != null)
        {
            // 计算朝向目标的方向
            var direction = (targetTrans.Position - trans.Position).normalized;
            
            // 插值转向
            var currentDirection = component.CurrentVelocity.magnitude > FP.Zero
                ? component.CurrentVelocity.normalized
                : component.LaunchDirection;
            var newDirection = TSVector.Lerp(currentDirection, direction, trajectoryParams.TurnRate);
            
            // 更新速度
            component.CurrentVelocity = newDirection.normalized * trajectoryParams.BaseSpeed;
        }
    }
    
    trans.Position += component.CurrentVelocity;
}
```

**配置示例**：
```json
{
    "TrajectoryType": "Homing",
    "TrajectoryData": "{\"TargetEntityId\":12345,\"BaseSpeed\":0.6,\"TurnRate\":0.1}"
}
```

**目标选择策略**：
- 在生成Projectile时，通过查询系统选择最近的敌人作为目标
- 将目标ID存储在TrajectoryData中

---

## 9. 碰撞与效果触发

### 9.1 碰撞检测策略

**三种检测模式**：

1. **Continuous（连续检测）**：每帧检测，适用于高速弹道
2. **Discrete（离散检测）**：固定间隔检测，适用于慢速弹道，节省性能
3. **OnlyTarget（仅目标层）**：只检测特定层级的目标，适用于追踪弹道

### 9.2 穿透机制

**实现**：
- `PierceCount`：允许穿透的目标数量
- `PiercedCount`：已穿透的目标数量
- `HitEntities`：已命中的实体列表（防止重复命中同一目标）

**流程**：
```
碰撞检测 → 命中目标
    ↓
记录到HitEntities → 触发SkillEffect
    ↓
PiercedCount++
    ↓
PiercedCount > PierceCount? 
    └── Yes → 销毁Projectile
    └── No → 继续飞行
```

### 9.3 效果触发

**通过SkillEffectManager触发**：

```csharp
private void TriggerSkillEffect(Entity projectile, ProjectileComponent component, Entity target)
{
    var caster = projectile.World.GetEntityById(component.CasterId);
    if (caster == null) return;
    
    foreach (var effectId in component.SkillEffectIds)
    {
        var effectData = new SkillEffectData
        {
            CasterEntity = caster,
            TargetEntity = target,
            EffectId = effectId
        };

        SkillEffectManager.Instance.QueueSkillEffect(effectData);
    }
}
```

**优势**：
- 完全复用现有的SkillEffect系统
- 支持所有效果类型（伤害、治疗、击退、buff等）
- 统一的效果计算和结果应用

---

## 10. 典型应用场景

### 10.1 法师火球术（连射）

**需求**：
- 按住技能键连续射出火球
- 火球直线飞行，命中敌人造成伤害
- 松开技能键后进入后摇

**配置**：
- PrecastAction（前摇20帧）→ CastAction（30帧，尾声5帧可自我取消）→ RecoveryAction（后摇15帧）
- CastAction自我取消机制：第25-30帧设置BeCancelledTag，自己的CancelTag匹配，持续输入时循环
- Projectile：直线轨迹（TrajectoryData.BaseSpeed=0.8），生命周期300帧，球形碰撞半径0.5
- SkillEffect：伤害效果，150%攻击力

### 10.2 弓箭手蓄力箭（蓄力）

**需求**：
- 按住技能键蓄力
- 根据蓄力时长射出不同强度的箭
- 箭受重力影响，呈抛物线飞行

**配置**：
- ChargingAction（最大180帧）→ ReleaseAction（释放20帧）
- Condition触发：ChargeTime<30→弱箭，30-60→中箭，>=60→强箭
- Projectile：抛物线轨迹，重力[0,-0.05,0]，生命周期200帧
- SkillEffect：根据蓄力等级，伤害100%/150%/200%

### 10.3 追踪导弹（追踪）

**需求**：
- 射出导弹，自动追踪最近的敌人
- 导弹有转向速度限制，不会瞬间转向

**配置**：
- CastAction（生成导弹）
- Projectile：追踪轨迹，TurnRate=0.1，生命周期600帧
- 生成时查询最近敌人，将其ID存入TrajectoryData
- SkillEffect：爆炸伤害，AOE范围3.0

### 10.4 链式闪电（穿透）

**需求**：
- 射出闪电球，可以穿透3个敌人
- 每次穿透伤害递减

**配置**：
- CastAction（生成闪电球）
- Projectile：直线轨迹（TrajectoryData.BaseSpeed=1.0，PierceCount=3）
- SkillEffect：链式伤害处理器（自定义Handler），每次穿透伤害×0.8

---

## 11. 性能优化考虑

### 11.1 Projectile池化

**问题**：频繁创建和销毁Projectile会产生GC压力

**方案**：使用对象池

```csharp
public class ProjectilePool
{
    private static ProjectilePool _instance;
    public static ProjectilePool Instance => _instance ??= new ProjectilePool();
    
    private Queue<Entity> _pool = new Queue<Entity>();
    
    public Entity? Spawn(World world, Entity caster, ProjectileDefinition definition, ProjectileSpawnContext context)
    {
        Entity projectile;
        if (_pool.Count > 0)
        {
            projectile = _pool.Dequeue();
            projectile.World = world;
        }
        else
        {
            projectile = world.EntityFactory.CreateByArchetype(
                definition.ProjectileArchetype,
                new EntityCreationParams
                {
                    SpawnPosition = context.SpawnPosition,
                    ExtraData = context
                });
        }

        InitializeProjectileRuntime(projectile, caster, context.SkillEffectIds, definition, (context.SpawnPosition, context.SpawnDirection));
        return projectile;
    }
    
    public void Recycle(Entity projectile)
    {
        var component = projectile.GetComponent<ProjectileComponent>();
        if (component != null)
        {
            component.ElapsedFrames = 0;
            component.PiercedCount = 0;
            component.HitEntities.Clear();
            component.CurrentVelocity = TSVector.zero;
        }

        _pool.Enqueue(projectile);
    }
}
```

### 11.2 碰撞检测优化

**策略**：
1. 使用离散检测模式减少检测频率
2. 空间分区（已由HitManager实现）
3. 早期剔除（根据距离预判断）

### 11.3 Projectile数量限制

**方案**：限制同时存在的Projectile数量

```