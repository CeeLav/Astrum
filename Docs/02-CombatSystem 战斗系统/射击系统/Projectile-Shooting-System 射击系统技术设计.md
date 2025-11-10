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

**实现方式**：释放动作配置为循环动作，通过输入持续性判断是否继续执行

```
连射流程
前摇动作 → 释放动作 → [持续输入] → 释放动作 → [停止输入] → 后摇动作
           ↑_______________|
           循环（AutoNextActionId指向自己）
```

**关键配置**：
- `CastAction.AutoNextActionId` = 自身ID（循环）
- `CastAction.Commands` = 要求输入命令存在（如 "skill1"）
- 停止输入时，命令失效，自动切换到后摇动作

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
├── PositionComponent（位置）
├── VelocityComponent（速度）
├── ProjectileComponent（弹道数据）
│   ├── SkillEffectId（触发的效果ID）
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
├── ProjectileFactory（弹道工厂）
│   ├── 创建Projectile实体
│   ├── 配置轨迹参数
│   └── 注册到World
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
4. SkillExecutorCapability → ProjectileFactory.Create()
5. Projectile Entity → ProjectileCapability.Tick()
6. 每帧更新位置 → 碰撞检测（HitManager或内置检测）
7. 碰撞命中 → 触发SkillEffect → 销毁Projectile
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
    /// <summary>技能效果ID（碰撞时触发）</summary>
    public int SkillEffectId { get; set; } = 0;
    
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
    Bezier = 3,      // 贝塞尔曲线
    Spiral = 4       // 螺旋
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
```

### 4.2 ProjectileCapability（弹道能力）

**职责**：更新弹道运动、碰撞检测、效果触发

```csharp
/// <summary>
/// 弹道能力 - 管理弹道实体的运动和碰撞
/// </summary>
public class ProjectileCapability : Capability<ProjectileCapability>
{
    public override int Priority => 250; // 高优先级
    
    public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
    private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag> 
    { 
        CapabilityTag.Physics, 
        CapabilityTag.Combat 
    };
    
    public override void OnAttached(Entity entity)
    {
        base.OnAttached(entity);
        
        // 初始化轨迹系统
        var projectileComponent = GetComponent<ProjectileComponent>(entity);
        if (projectileComponent != null)
        {
            InitializeTrajectory(entity, projectileComponent);
        }
    }
    
    public override bool ShouldActivate(Entity entity)
    {
        return base.ShouldActivate(entity) &&
               HasComponent<ProjectileComponent>(entity) &&
               HasComponent<PositionComponent>(entity);
    }
    
    public override void Tick(Entity entity)
    {
        var projectileComponent = GetComponent<ProjectileComponent>(entity);
        if (projectileComponent == null) return;
        
        // 1. 更新生命周期
        projectileComponent.ElapsedFrames++;
        if (projectileComponent.ElapsedFrames >= projectileComponent.LifeTime)
        {
            DestroyProjectile(entity, "Lifetime expired");
            return;
        }
        
        // 2. 更新运动轨迹
        UpdateTrajectory(entity, projectileComponent);
        
        // 3. 碰撞检测
        CheckCollision(entity, projectileComponent);
    }
    
    /// <summary>
    /// 初始化轨迹系统
    /// </summary>
    private void InitializeTrajectory(Entity entity, ProjectileComponent component)
    {
        // 根据轨迹类型初始化参数
        // 例如：解析TrajectoryData，设置初始速度等
    }
    
    /// <summary>
    /// 更新运动轨迹
    /// </summary>
    private void UpdateTrajectory(Entity entity, ProjectileComponent component)
    {
        var positionComponent = GetComponent<PositionComponent>(entity);
        if (positionComponent == null) return;
        
        switch (component.TrajectoryType)
        {
            case TrajectoryType.Linear:
                UpdateLinearTrajectory(entity, component, positionComponent);
                break;
                
            case TrajectoryType.Parabola:
                UpdateParabolicTrajectory(entity, component, positionComponent);
                break;
                
            case TrajectoryType.Homing:
                UpdateHomingTrajectory(entity, component, positionComponent);
                break;
                
            // ... 其他轨迹类型
        }
    }
    
    /// <summary>
    /// 直线轨迹更新
    /// </summary>
    private void UpdateLinearTrajectory(Entity entity, ProjectileComponent component, PositionComponent position)
    {
        var velocityComponent = GetComponent<VelocityComponent>(entity);
        if (velocityComponent == null) return;
        
        // 更新位置
        position.Position += velocityComponent.Velocity;
    }
    
    /// <summary>
    /// 抛物线轨迹更新
    /// </summary>
    private void UpdateParabolicTrajectory(Entity entity, ProjectileComponent component, PositionComponent position)
    {
        var velocityComponent = GetComponent<VelocityComponent>(entity);
        if (velocityComponent == null) return;
        
        // 解析轨迹参数
        var trajectoryParams = ParseTrajectoryData<ParabolicTrajectoryData>(component.TrajectoryData);
        
        // 应用重力
        velocityComponent.Velocity += trajectoryParams.Gravity;
        
        // 更新位置
        position.Position += velocityComponent.Velocity;
    }
    
    /// <summary>
    /// 追踪轨迹更新
    /// </summary>
    private void UpdateHomingTrajectory(Entity entity, ProjectileComponent component, PositionComponent position)
    {
        var velocityComponent = GetComponent<VelocityComponent>(entity);
        if (velocityComponent == null) return;
        
        // 解析轨迹参数
        var trajectoryParams = ParseTrajectoryData<HomingTrajectoryData>(component.TrajectoryData);
        
        // 查找目标
        var targetEntity = entity.World.GetEntityById(trajectoryParams.TargetEntityId);
        if (targetEntity != null && !targetEntity.IsDestroyed)
        {
            var targetPosition = targetEntity.GetComponent<PositionComponent>();
            if (targetPosition != null)
            {
                // 计算朝向目标的方向
                var direction = (targetPosition.Position - position.Position).normalized;
                
                // 插值转向（避免瞬间转向）
                var currentDirection = velocityComponent.Velocity.normalized;
                var newDirection = TSVector.Lerp(currentDirection, direction, trajectoryParams.TurnRate);
                
                // 更新速度
                velocityComponent.Velocity = newDirection * velocityComponent.Velocity.magnitude;
            }
        }
        
        // 更新位置
        position.Position += velocityComponent.Velocity;
    }
    
    /// <summary>
    /// 碰撞检测
    /// </summary>
    private void CheckCollision(Entity entity, ProjectileComponent component)
    {
        // 根据碰撞模式执行检测
        switch (component.CollisionMode)
        {
            case ProjectileCollisionMode.Continuous:
                CheckContinuousCollision(entity, component);
                break;
                
            case ProjectileCollisionMode.Discrete:
                if (component.ElapsedFrames % 3 == 0) // 每3帧检测一次
                    CheckContinuousCollision(entity, component);
                break;
                
            case ProjectileCollisionMode.OnlyTarget:
                CheckTargetCollision(entity, component);
                break;
        }
    }
    
    /// <summary>
    /// 连续碰撞检测
    /// </summary>
    private void CheckContinuousCollision(Entity entity, ProjectileComponent component)
    {
        var collisionComponent = GetComponent<CollisionComponent>(entity);
        if (collisionComponent == null) return;
        
        // 使用HitManager进行碰撞检测
        var hitManager = entity.World.HitManager;
        if (hitManager == null) return;
        
        var hits = hitManager.QueryHits(
            entity,
            collisionComponent.CollisionShape,
            filter: CreateCollisionFilter(entity, component),
            skillInstanceId: 0 // Projectile不需要去重
        );
        
        // 处理命中
        foreach (var hitEntity in hits)
        {
            OnHit(entity, component, hitEntity);
        }
    }
    
    /// <summary>
    /// 创建碰撞过滤器
    /// </summary>
    private CollisionFilter CreateCollisionFilter(Entity entity, ProjectileComponent component)
    {
        // 过滤掉施法者和已命中的实体
        return new CollisionFilter
        {
            ExcludeEntityIds = new List<long> { component.CasterId }
                .Concat(component.HitEntities)
                .ToList()
        };
    }
    
    /// <summary>
    /// 命中处理
    /// </summary>
    private void OnHit(Entity projectile, ProjectileComponent component, Entity target)
    {
        // 记录已命中
        component.HitEntities.Add(target.UniqueId);
        
        // 触发技能效果
        TriggerSkillEffect(projectile, component, target);
        
        // 穿透判断
        component.PiercedCount++;
        if (component.PiercedCount > component.PierceCount)
        {
            // 达到穿透上限，销毁弹道
            DestroyProjectile(projectile, "Pierce limit reached");
        }
    }
    
    /// <summary>
    /// 触发技能效果
    /// </summary>
    private void TriggerSkillEffect(Entity projectile, ProjectileComponent component, Entity target)
    {
        var caster = projectile.World.GetEntityById(component.CasterId);
        if (caster == null) return;
        
        // 调用SkillEffectManager触发效果
        var effectData = new SkillEffectData
        {
            CasterEntity = caster,
            TargetEntity = target,
            EffectId = component.SkillEffectId
        };
        
        SkillEffectManager.Instance.QueueSkillEffect(effectData);
    }
    
    /// <summary>
    /// 销毁弹道
    /// </summary>
    private void DestroyProjectile(Entity entity, string reason)
    {
        ASLogger.Instance.Debug($"Projectile destroyed: {reason}");
        entity.Destroy();
    }
    
    /// <summary>
    /// 解析轨迹数据
    /// </summary>
    private T ParseTrajectoryData<T>(string json) where T : new()
    {
        if (string.IsNullOrEmpty(json))
            return new T();
            
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            ASLogger.Instance.Error($"Failed to parse trajectory data: {json}");
            return new T();
        }
    }
}

/// <summary>
/// 抛物线轨迹参数
/// </summary>
public class ParabolicTrajectoryData
{
    public TSVector Gravity { get; set; } = new TSVector(0, -0.05, 0);
}

/// <summary>
/// 追踪轨迹参数
/// </summary>
public class HomingTrajectoryData
{
    public long TargetEntityId { get; set; } = 0;
    public FP TurnRate { get; set; } = FP.FromFloat(0.1f); // 转向速率
}
```

### 4.3 ProjectileFactory（弹道工厂）

**职责**：创建和配置Projectile实体

```csharp
/// <summary>
/// 弹道工厂 - 创建Projectile实体
/// </summary>
public static class ProjectileFactory
{
    /// <summary>
    /// 创建弹道实体
    /// </summary>
    /// <param name="world">所属World</param>
    /// <param name="caster">施法者</param>
    /// <param name="config">弹道配置</param>
    /// <returns>创建的Projectile实体</returns>
    public static Entity CreateProjectile(World world, Entity caster, ProjectileConfig config)
    {
        // 1. 创建Entity
        var projectile = new Entity
        {
            Name = $"Projectile_{config.SkillEffectId}",
            World = world
        };
        
        // 2. 添加位置组件
        var casterPosition = caster.GetComponent<PositionComponent>();
        var positionComponent = new PositionComponent
        {
            Position = casterPosition?.Position ?? TSVector.zero
        };
        projectile.AddComponent(positionComponent);
        
        // 3. 添加速度组件
        var velocityComponent = new VelocityComponent
        {
            Velocity = CalculateInitialVelocity(caster, config)
        };
        projectile.AddComponent(velocityComponent);
        
        // 4. 添加弹道组件
        var projectileComponent = new ProjectileComponent
        {
            SkillEffectId = config.SkillEffectId,
            CasterId = caster.UniqueId,
            LifeTime = config.LifeTime,
            TrajectoryType = config.TrajectoryType,
            TrajectoryData = config.TrajectoryData,
            CollisionMode = config.CollisionMode,
            PierceCount = config.PierceCount
        };
        projectile.AddComponent(projectileComponent);
        
        // 5. 添加碰撞组件
        var collisionComponent = new CollisionComponent
        {
            CollisionShape = config.CollisionShape,
            IsTrigger = true // Projectile使用触发器模式
        };
        projectile.AddComponent(collisionComponent);
        
        // 6. 挂载Projectile Capability
        projectile.AttachSubArchetype("ProjectileArchetype", out _);
        
        // 7. 注册到World
        world.AddEntity(projectile);
        
        return projectile;
    }
    
    /// <summary>
    /// 计算初始速度
    /// </summary>
    private static TSVector CalculateInitialVelocity(Entity caster, ProjectileConfig config)
    {
        // 获取施法者朝向
        var direction = GetCasterDirection(caster);
        
        // 应用速度
        return direction * config.Speed;
    }
    
    /// <summary>
    /// 获取施法者朝向
    /// </summary>
    private static TSVector GetCasterDirection(Entity caster)
    {
        // TODO: 从RotationComponent或FacingComponent获取朝向
        // 暂时返回默认朝向
        return TSVector.forward;
    }
}

/// <summary>
/// 弹道配置
/// </summary>
public class ProjectileConfig
{
    public int SkillEffectId { get; set; }
    public int LifeTime { get; set; } = 300;
    public FP Speed { get; set; } = FP.FromFloat(0.5f);
    public TrajectoryType TrajectoryType { get; set; } = TrajectoryType.Linear;
    public string TrajectoryData { get; set; } = string.Empty;
    public CollisionShape CollisionShape { get; set; }
    public ProjectileCollisionMode CollisionMode { get; set; } = ProjectileCollisionMode.Continuous;
    public int PierceCount { get; set; } = 0;
}
```

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
    "BeCancelledTags": ["Roll", "Dash"]
}

// ActionTable - 释放动作
{
    "ActionId": 5002,
    "ActionType": "Skill",
    "Catalog": "Shooting",
    "TotalFrames": 30,
    "AnimationName": "FireBall_Cast",
    "AutoNextActionId": 5002,  // 循环自己（连射）
    "Commands": ["skill1"],    // 需要skill1输入才能执行
    "BeCancelledTags": ["Roll", "Dash"]
}

// SkillActionTable - 释放动作配置
{
    "ActionId": 5002,
    "SkillId": 3001,
    "TriggerFrames": "Frame10:Direct:4101"  // 第10帧生成弹道
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
                    ↓ (持续按住Skill1)
                CastAction(5002) 生成Projectile（循环）
                    ↓ (松开Skill1，命令失效)
                RecoveryAction(5003)
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

为了支持射击系统，需要扩展`SkillExecutorCapability`以处理Direct类型的触发帧，生成Projectile实体。

### 6.1 扩展HandleDirectTrigger方法

```csharp
/// <summary>
/// 处理直接触发 - 扩展支持Projectile生成
/// </summary>
private void HandleDirectTrigger(Entity caster, TriggerFrameInfo trigger)
{
    // 检查是否是Projectile类型的效果
    var effectConfig = SkillConfigManager.GetSkillEffect(trigger.EffectIds[0]);
    if (effectConfig == null) return;
    
    // 检查EffectParams中是否包含Projectile配置
    if (IsProjectileEffect(effectConfig))
    {
        SpawnProjectile(caster, effectConfig, trigger);
    }
    else
    {
        // 原有逻辑：直接触发效果
        TriggerSkillEffect(caster, caster, trigger.EffectIds[0]);
    }
}

/// <summary>
/// 判断是否是弹道效果
/// </summary>
private bool IsProjectileEffect(SkillEffectConfig effectConfig)
{
    // 检查EffectParams中是否包含"ProjectileType"字段
    if (string.IsNullOrEmpty(effectConfig.EffectParams)) return false;
    
    try
    {
        var paramsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(effectConfig.EffectParams);
        return paramsDict.ContainsKey("ProjectileType");
    }
    catch
    {
        return false;
    }
}

/// <summary>
/// 生成弹道实体
/// </summary>
private void SpawnProjectile(Entity caster, SkillEffectConfig effectConfig, TriggerFrameInfo trigger)
{
    // 解析弹道配置
    var projectileConfig = ParseProjectileConfig(effectConfig);
    if (projectileConfig == null)
    {
        ASLogger.Instance.Error($"Failed to parse projectile config for effect {effectConfig.SkillEffectId}");
        return;
    }
    
    // 创建弹道实体
    var projectile = ProjectileFactory.CreateProjectile(caster.World, caster, projectileConfig);
    
    ASLogger.Instance.Debug($"Spawned projectile for effect {effectConfig.SkillEffectId}");
}

/// <summary>
/// 解析弹道配置
/// </summary>
private ProjectileConfig ParseProjectileConfig(SkillEffectConfig effectConfig)
{
    try
    {
        var paramsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(effectConfig.EffectParams);
        
        return new ProjectileConfig
        {
            SkillEffectId = effectConfig.SkillEffectId,
            LifeTime = paramsDict.TryGetValue("LifeTime", out var lifeTime) ? Convert.ToInt32(lifeTime) : 300,
            Speed = paramsDict.TryGetValue("Speed", out var speed) ? FP.FromFloat(Convert.ToSingle(speed)) : FP.FromFloat(0.5f),
            TrajectoryType = paramsDict.TryGetValue("TrajectoryType", out var trajType) ? Enum.Parse<TrajectoryType>(trajType.ToString()) : TrajectoryType.Linear,
            TrajectoryData = paramsDict.TryGetValue("TrajectoryData", out var trajData) ? trajData.ToString() : string.Empty,
            CollisionShape = ParseCollisionShape(paramsDict),
            CollisionMode = paramsDict.TryGetValue("CollisionMode", out var collMode) ? Enum.Parse<ProjectileCollisionMode>(collMode.ToString()) : ProjectileCollisionMode.Continuous,
            PierceCount = paramsDict.TryGetValue("PierceCount", out var pierce) ? Convert.ToInt32(pierce) : 0
        };
    }
    catch (Exception ex)
    {
        ASLogger.Instance.Error($"Failed to parse projectile config: {ex.Message}");
        return null;
    }
}

/// <summary>
/// 解析碰撞形状
/// </summary>
private CollisionShape ParseCollisionShape(Dictionary<string, object> paramsDict)
{
    if (!paramsDict.TryGetValue("CollisionShape", out var shapeData))
    {
        // 默认球形碰撞，半径0.5
        return CollisionShape.CreateSphere(FP.FromFloat(0.5f));
    }
    
    // TODO: 根据shapeData解析具体的碰撞形状
    return CollisionShape.CreateSphere(FP.FromFloat(0.5f));
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
        \"ProjectileType\": \"FireBall\",
        \"LifeTime\": 300,
        \"Speed\": 0.8,
        \"TrajectoryType\": \"Linear\",
        \"CollisionShape\": \"Sphere:0.5\",
        \"CollisionMode\": \"Continuous\",
        \"PierceCount\": 0
    }",
    "VisualEffectId": 5101,
    "SoundEffectId": 6101
}
```

### 7.2 ProjectileTable（可选）

如果弹道配置复杂，可以单独创建ProjectileTable：

```json
{
    "ProjectileId": 7001,
    "ProjectileName": "FireBall",
    "LifeTime": 300,
    "Speed": 0.8,
    "TrajectoryType": "Linear",
    "TrajectoryData": "",
    "CollisionShape": "Sphere:0.5",
    "CollisionMode": "Continuous",
    "PierceCount": 0,
    "VisualPrefabPath": "Projectiles/FireBall",
    "TrailEffectId": 5102,
    "HitEffectId": 5103
}
```

然后SkillEffectTable的EffectParams简化为：

```json
{
    "EffectParams": "{\"ProjectileId\": 7001}"
}
```

---

## 8. 轨迹系统详细设计

### 8.1 直线轨迹（Linear）

**实现**：匀速直线运动

```csharp
private void UpdateLinearTrajectory(Entity entity, ProjectileComponent component, PositionComponent position)
{
    var velocityComponent = GetComponent<VelocityComponent>(entity);
    if (velocityComponent == null) return;
    
    position.Position += velocityComponent.Velocity;
}
```

**配置示例**：
```json
{
    "TrajectoryType": "Linear",
    "Speed": 0.8
}
```

### 8.2 抛物线轨迹（Parabola）

**实现**：受重力影响的抛物线运动

```csharp
private void UpdateParabolicTrajectory(Entity entity, ProjectileComponent component, PositionComponent position)
{
    var velocityComponent = GetComponent<VelocityComponent>(entity);
    if (velocityComponent == null) return;
    
    var trajectoryParams = ParseTrajectoryData<ParabolicTrajectoryData>(component.TrajectoryData);
    
    // 应用重力
    velocityComponent.Velocity += trajectoryParams.Gravity;
    
    // 更新位置
    position.Position += velocityComponent.Velocity;
}
```

**配置示例**：
```json
{
    "TrajectoryType": "Parabola",
    "Speed": 0.5,
    "TrajectoryData": "{\"Gravity\": [0, -0.05, 0]}"
}
```

### 8.3 追踪轨迹（Homing）

**实现**：朝向目标转向的追踪运动

```csharp
private void UpdateHomingTrajectory(Entity entity, ProjectileComponent component, PositionComponent position)
{
    var velocityComponent = GetComponent<VelocityComponent>(entity);
    if (velocityComponent == null) return;
    
    var trajectoryParams = ParseTrajectoryData<HomingTrajectoryData>(component.TrajectoryData);
    
    // 查找目标
    var targetEntity = entity.World.GetEntityById(trajectoryParams.TargetEntityId);
    if (targetEntity != null && !targetEntity.IsDestroyed)
    {
        var targetPosition = targetEntity.GetComponent<PositionComponent>();
        if (targetPosition != null)
        {
            // 计算朝向目标的方向
            var direction = (targetPosition.Position - position.Position).normalized;
            
            // 插值转向
            var currentDirection = velocityComponent.Velocity.normalized;
            var newDirection = TSVector.Lerp(currentDirection, direction, trajectoryParams.TurnRate);
            
            // 更新速度
            velocityComponent.Velocity = newDirection * velocityComponent.Velocity.magnitude;
        }
    }
    
    position.Position += velocityComponent.Velocity;
}
```

**配置示例**：
```json
{
    "TrajectoryType": "Homing",
    "Speed": 0.6,
    "TrajectoryData": "{\"TargetEntityId\": 12345, \"TurnRate\": 0.1}"
}
```

**目标选择策略**：
- 在生成Projectile时，通过查询系统选择最近的敌人作为目标
- 将目标ID存储在TrajectoryData中

### 8.4 贝塞尔曲线轨迹（Bezier）

**实现**：沿贝塞尔曲线运动

```csharp
private void UpdateBezierTrajectory(Entity entity, ProjectileComponent component, PositionComponent position)
{
    var trajectoryParams = ParseTrajectoryData<BezierTrajectoryData>(component.TrajectoryData);
    
    // 计算当前曲线进度（0-1）
    float t = (float)component.ElapsedFrames / component.LifeTime;
    
    // 三次贝塞尔曲线公式
    var p0 = trajectoryParams.P0;
    var p1 = trajectoryParams.P1;
    var p2 = trajectoryParams.P2;
    var p3 = trajectoryParams.P3;
    
    var newPosition = 
        p0 * (1 - t) * (1 - t) * (1 - t) +
        p1 * 3 * (1 - t) * (1 - t) * t +
        p2 * 3 * (1 - t) * t * t +
        p3 * t * t * t;
    
    position.Position = newPosition;
}
```

**配置示例**：
```json
{
    "TrajectoryType": "Bezier",
    "TrajectoryData": "{\"P0\": [0,0,0], \"P1\": [2,3,0], \"P2\": [8,3,0], \"P3\": [10,0,0]}"
}
```

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
    
    var effectData = new SkillEffectData
    {
        CasterEntity = caster,
        TargetEntity = target,
        EffectId = component.SkillEffectId
    };
    
    SkillEffectManager.Instance.QueueSkillEffect(effectData);
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
- PrecastAction（前摇20帧）→ CastAction（循环，每30帧生成一个火球）→ RecoveryAction（后摇15帧）
- Projectile：直线轨迹，速度0.8，生命周期300帧，球形碰撞半径0.5
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
- Projectile：直线轨迹，PierceCount=3，速度1.0
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
    
    public Entity Get(World world)
    {
        if (_pool.Count > 0)
        {
            var projectile = _pool.Dequeue();
            projectile.IsDestroyed = false;
            projectile.World = world;
            return projectile;
        }
        
        return new Entity { World = world };
    }
    
    public void Return(Entity projectile)
    {
        // 清理组件状态
        var component = projectile.GetComponent<ProjectileComponent>();
        if (component != null)
        {
            component.ElapsedFrames = 0;
            component.PiercedCount = 0;
            component.HitEntities.Clear();
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

```csharp
public class ProjectileManager
{
    private const int MaxProjectileCount = 100;
    private List<Entity> _activeProjectiles = new List<Entity>();
    
    public Entity SpawnProjectile(World world, Entity caster, ProjectileConfig config)
    {
        // 达到上限时，销毁最老的Projectile
        if (_activeProjectiles.Count >= MaxProjectileCount)
        {
            var oldest = _activeProjectiles[0];
            oldest.Destroy();
            _activeProjectiles.RemoveAt(0);
        }
        
        var projectile = ProjectileFactory.CreateProjectile(world, caster, config);
        _activeProjectiles.Add(projectile);
        
        return projectile;
    }
}
```

---

## 12. 与现有系统集成

### 12.1 依赖系统

| 系统 | 集成点 | 说明 |
|------|--------|------|
| **Action系统** | 射击动作基于ActionInfo | 复用动作切换、取消机制 |
| **技能系统** | SkillAction触发Projectile生成 | 复用技能配置、触发帧系统 |
| **SkillExecutorCapability** | 处理Direct触发帧，调用ProjectileFactory | 扩展HandleDirectTrigger方法 |
| **HitManager** | Projectile碰撞检测 | 复用即时查询API |
| **SkillEffectManager** | 碰撞后触发效果 | 完全复用效果系统 |
| **Entity系统** | Projectile作为Entity | 复用组件和能力架构 |

### 12.2 新增组件/能力

| 类型 | 名称 | 说明 |
|------|------|------|
| Component | ProjectileComponent | 弹道配置和状态 |
| Component | ChargingComponent | 蓄力状态 |
| Capability | ProjectileCapability | 弹道运动和碰撞逻辑 |
| Factory | ProjectileFactory | Projectile实体创建 |

### 12.3 配置表扩展

| 表格 | 扩展内容 | 说明 |
|------|---------|------|
| **SkillEffectTable** | EffectParams增加Projectile配置 | 可选：单独创建ProjectileTable |
| **ActionTable** | 支持Charging类型动作 | 用于蓄力动作 |

---

## 13. 开发路线图

### 13.1 第一阶段 - 基础弹道系统

**目标**：实现基本的直线弹道

- [ ] ProjectileComponent和ProjectileCapability实现
- [ ] ProjectileFactory实现
- [ ] 直线轨迹实现
- [ ] 碰撞检测和效果触发
- [ ] 扩展SkillExecutorCapability支持Projectile生成

### 13.2 第二阶段 - 连射机制

**目标**：支持连续射击

- [ ] 连射动作配置
- [ ] 输入持续性判断
- [ ] 动作循环逻辑测试

### 13.3 第三阶段 - 蓄力机制

**目标**：支持蓄力射击

- [ ] ChargingComponent实现
- [ ] Condition触发逻辑扩展
- [ ] 蓄力时长计算
- [ ] 多强度效果配置

### 13.4 第四阶段 - 高级轨迹

**目标**：支持多种运动轨迹

- [ ] 抛物线轨迹实现
- [ ] 追踪轨迹实现
- [ ] 贝塞尔曲线轨迹实现
- [ ] 轨迹参数配置和解析

### 13.5 第五阶段 - 优化与扩展

**目标**：性能优化和功能扩展

- [ ] Projectile对象池
- [ ] 碰撞检测优化
- [ ] 穿透机制完善
- [ ] 编辑器工具支持

---

## 14. 关键决策与取舍

### 决策1：Projectile作为Entity

**问题**：Projectile应该作为Entity还是轻量级数据结构？

**备选**：
1. 作为Entity：复用组件和能力系统，支持复杂行为
2. 轻量级结构：仅存储数据，由专门的Manager管理

**选择**：作为Entity

**理由**：
- 复用现有架构，开发成本低
- 支持复杂行为（如追踪、多段碰撞）
- 易于扩展（可添加更多组件和能力）
- 统一的序列化和网络同步

**影响**：
- 内存开销略高，需要对象池优化
- 每个Projectile占用一个Entity ID

### 决策2：轨迹系统设计

**问题**：轨迹系统应该基于代码还是配置？

**备选**：
1. 代码驱动：每种轨迹写固定代码
2. 配置驱动：通过配置文件定义轨迹参数
3. 脚本化：支持Lua/C#脚本自定义轨迹

**选择**：混合方式（代码+配置）

**理由**：
- 常用轨迹用代码实现（性能好，易调试）
- 参数通过配置调整（灵活性高）
- 未来可考虑脚本化扩展

**影响**：
- 新增轨迹类型需要修改代码
- 配置复杂度适中

### 决策3：碰撞检测方式

**问题**：使用HitManager还是内置碰撞检测？

**备选**：
1. 使用HitManager：复用现有物理系统
2. 内置检测：Projectile自己管理碰撞

**选择**：使用HitManager

**理由**：
- 复用成熟的碰撞系统
- 统一的碰撞过滤和去重逻辑
- 减少代码重复

**影响**：
- 依赖HitManager的实现
- 碰撞检测性能受HitManager限制

---

**相关文档**:
- [Action-System 动作系统](../技能系统/Action-System 动作系统.md)
- [Skill-System 技能系统](../技能系统/Skill-System 技能系统.md)
- [Skill-Effect-Runtime 技能效果运行时](../技能效果/Skill-Effect-Runtime 技能效果运行时.md)
- [物理系统开发进展](../../Physics/物理系统开发进展.md)

---

*文档版本：v1.0*  
*创建时间：2025-11-10*  
*最后更新：2025-11-10*  
*状态：设计完成*  
*Owner*: 开发团队  
*变更摘要*: 基于现有Action系统设计射击系统的多阶段动作、弹道实体和碰撞触发机制

