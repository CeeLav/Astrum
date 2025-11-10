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
    ├── PhysicsWorld / HitManager（碰撞检测）
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
    
    /// <summary>上一帧位置（用于射线检测）</summary>
    public TSVector LastPosition { get; set; } = TSVector.zero;
    
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
    public int PierceCount { get; init; } = 0;
    public IReadOnlyList<int> DefaultEffectIds { get; init; } = Array.Empty<int>();
}

public sealed class ProjectileConfigManager
{
    public static ProjectileConfigManager Instance { get; } = new ProjectileConfigManager();

    private readonly Dictionary<int, ProjectileDefinition> _definitions = new();

    public ProjectileDefinition? GetDefinition(int projectileId) =>
        _definitions.TryGetValue(projectileId, out var def) ? def : null;
}

// 说明：Projectile 不再存储碰撞形状，所有命中判定改为射线检测（上一帧 → 当前帧）。
// `PierceCount` 仅用于控制同一条射线路径允许命中的目标数量。

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

### 4.6 ProjectileViewComponent（表现层）

**职责**：负责弹道的视觉表现与生命周期同步，不参与逻辑判定。

**核心设计**：
- 表现层弹道起始位置从模型的 Socket 点（如法杖顶端）出发
- 逻辑层弹道起始位置从 Entity 的 TransComponent 位置出发
- 表现层通过插值逐渐追赶逻辑层位置，实现平滑过渡
- 参考 `TransViewComponent` 的视觉跟随机制

```csharp
using UnityEngine;
using Astrum.View.Core;
using Astrum.View.Components;
using Astrum.CommonBase;
using TrueSync;

namespace Astrum.View.Components
{
    /// <summary>
    /// 弹道表现组件 - 管理弹道视觉效果（拖尾、粒子、特效等）
    /// </summary>
    public sealed class ProjectileViewComponent : ViewComponent
    {
        // 视觉组件引用（从Prefab上获取）
        private TrailRenderer _trailRenderer;
        private ParticleSystem _loopEffect;
        private ParticleSystem _hitEffect;
        
        // 视觉位置同步数据
        private struct VisualSyncData
        {
            /// <summary>
            /// 表现层当前位置（可能与逻辑层不同）
            /// </summary>
            public Vector3 visualPosition;
            
            /// <summary>
            /// 上一逻辑帧的逻辑位置
            /// </summary>
            public Vector3 lastLogicPosition;
            
            /// <summary>
            /// 表现层初始发射位置（从Socket获取）
            /// </summary>
            public Vector3 initialVisualSpawnPos;
            
            /// <summary>
            /// 逻辑层初始发射位置（从TransComponent获取）
            /// </summary>
            public Vector3 initialLogicSpawnPos;
            
            /// <summary>
            /// 是否已完成初始化（首次同步）
            /// </summary>
            public bool isInitialized;
            
            /// <summary>
            /// 自上次逻辑更新以来的累积时间
            /// </summary>
            public float timeSinceLastLogicUpdate;
        }
        
        private VisualSyncData _visualSync;
        
        // 视觉跟随配置
        [Header("视觉跟随设置")]
        private float _catchUpSpeed = 10f; // 表现层追赶逻辑层的速度系数
        private float _maxCatchUpDistance = 2f; // 最大允许偏移距离，超过则强制同步
        
        protected override void OnInitialize()
        {
            // 从GameObject上获取视觉组件
            if (_gameObject != null)
            {
                _trailRenderer = _gameObject.GetComponent<TrailRenderer>();
                
                // 获取所有粒子系统，根据命名约定区分
                var particles = _gameObject.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particles)
                {
                    if (ps.name.Contains("Loop") || ps.name.Contains("Trail"))
                        _loopEffect = ps;
                    else if (ps.name.Contains("Hit") || ps.name.Contains("Impact"))
                        _hitEffect = ps;
                }
            }
            
            // 初始化视觉同步数据
            _visualSync = new VisualSyncData
            {
                isInitialized = false,
                timeSinceLastLogicUpdate = 0f
            };
            
            // 初始化视觉效果
            ResetVisual();
            
            // 启动循环特效
            if (_loopEffect != null && !_loopEffect.isPlaying)
                _loopEffect.Play();
            
            ASLogger.Instance.Debug($"ProjectileViewComponent.OnInitialize: 初始化弹道视图组件，EntityId={OwnerEntity?.UniqueId}");
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            if (!_isEnabled || OwnerEntity == null) return;
            
            // 获取逻辑层位置
            var transComponent = OwnerEntity.GetComponent<TransComponent>();
            if (transComponent == null) return;
            
            var logicPos = transComponent.Position;
            Vector3 currentLogicPosition = new Vector3((float)logicPos.x, (float)logicPos.y, (float)logicPos.z);
            
            // 首次初始化：记录初始位置偏移
            if (!_visualSync.isInitialized)
            {
                InitializeVisualPosition(currentLogicPosition);
                return;
            }
            
            // 更新表现层位置（插值追赶逻辑层）
            UpdateVisualPosition(currentLogicPosition, deltaTime);
            
            // 应用表现层位置到GameObject
            if (_ownerEntityView != null)
            {
                _ownerEntityView.SetWorldPosition(_visualSync.visualPosition);
            }
            
            // 记录本次逻辑位置
            _visualSync.lastLogicPosition = currentLogicPosition;
        }
        
        protected override void OnDestroy()
        {
            // 停止并清理所有视觉效果
            StopAllEffects();
            
            ASLogger.Instance.Debug($"ProjectileViewComponent.OnDestroy: 销毁弹道视图组件，EntityId={OwnerEntity?.UniqueId}");
        }
        
        protected override void OnSyncData(object data)
        {
            // 如果需要从逻辑层同步特殊数据（如轨迹类型变化）可在此处理
        }
        
        /// <summary>
        /// 初始化表现层位置（首次调用）
        /// </summary>
        /// <param name="currentLogicPosition">当前逻辑层位置</param>
        private void InitializeVisualPosition(Vector3 currentLogicPosition)
        {
            // 表现层初始位置就是当前GameObject位置（由ViewBridge根据Socket设置）
            _visualSync.initialVisualSpawnPos = _ownerEntityView?.GetWorldPosition() ?? currentLogicPosition;
            _visualSync.initialLogicSpawnPos = currentLogicPosition;
            _visualSync.visualPosition = _visualSync.initialVisualSpawnPos;
            _visualSync.lastLogicPosition = currentLogicPosition;
            _visualSync.isInitialized = true;
            
            ASLogger.Instance.Debug(
                $"ProjectileViewComponent.InitializeVisualPosition: " +
                $"VisualSpawn={_visualSync.initialVisualSpawnPos}, " +
                $"LogicSpawn={_visualSync.initialLogicSpawnPos}, " +
                $"Offset={_visualSync.initialVisualSpawnPos - _visualSync.initialLogicSpawnPos}");
        }
        
        /// <summary>
        /// 更新表现层位置（插值追赶逻辑层）
        /// </summary>
        /// <param name="currentLogicPosition">当前逻辑层位置</param>
        /// <param name="deltaTime">帧时间</param>
        private void UpdateVisualPosition(Vector3 currentLogicPosition, float deltaTime)
        {
            // 计算逻辑层的位移
            Vector3 logicDelta = currentLogicPosition - _visualSync.lastLogicPosition;
            
            // 表现层跟随逻辑层移动（保持初始偏移，但逐渐缩小）
            Vector3 targetVisualPosition = currentLogicPosition + 
                (_visualSync.initialVisualSpawnPos - _visualSync.initialLogicSpawnPos);
            
            // 插值追赶目标位置
            float currentDistance = Vector3.Distance(_visualSync.visualPosition, targetVisualPosition);
            
            // 如果距离过大，强制同步（防止异常情况）
            if (currentDistance > _maxCatchUpDistance)
            {
                _visualSync.visualPosition = targetVisualPosition;
                ASLogger.Instance.Warning(
                    $"ProjectileViewComponent: 强制同步位置，距离={currentDistance:F3}");
            }
            else
            {
                // 平滑插值追赶
                _visualSync.visualPosition = Vector3.Lerp(
                    _visualSync.visualPosition,
                    targetVisualPosition,
                    Mathf.Clamp01(_catchUpSpeed * deltaTime)
                );
            }
        }
        
        /// <summary>
        /// 设置表现层初始位置（由ViewBridge在生成时调用）
        /// </summary>
        /// <param name="visualSpawnPosition">表现层发射位置（从Socket获取）</param>
        public void SetInitialVisualSpawnPosition(Vector3 visualSpawnPosition)
        {
            if (_ownerEntityView != null)
            {
                _ownerEntityView.SetWorldPosition(visualSpawnPosition);
            }
            
            ASLogger.Instance.Debug($"ProjectileViewComponent.SetInitialVisualSpawnPosition: {visualSpawnPosition}");
        }
        
        /// <summary>
        /// 触发命中效果
        /// </summary>
        /// <param name="hitPosition">命中位置（逻辑层定点数）</param>
        public void PlayHitEffect(TSVector hitPosition)
        {
            // 停止循环特效
            if (_loopEffect != null)
                _loopEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            
            // 播放命中特效（使用当前表现层位置，更准确）
            if (_hitEffect != null)
            {
                Vector3 worldPos = _visualSync.isInitialized 
                    ? _visualSync.visualPosition 
                    : new Vector3((float)hitPosition.x, (float)hitPosition.y, (float)hitPosition.z);
                    
                _hitEffect.transform.position = worldPos;
                _hitEffect.Play();
            }
            
            ASLogger.Instance.Debug($"ProjectileViewComponent.PlayHitEffect: 播放命中特效，位置={hitPosition}");
        }
        
        /// <summary>
        /// 重置视觉效果（用于对象池回收）
        /// </summary>
        public void ResetVisual()
        {
            if (_trailRenderer != null)
                _trailRenderer.Clear();
            
            if (_loopEffect != null)
                _loopEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            if (_hitEffect != null)
                _hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            // 重置同步数据
            _visualSync = new VisualSyncData
            {
                isInitialized = false,
                timeSinceLastLogicUpdate = 0f
            };
        }
        
        /// <summary>
        /// 停止所有特效
        /// </summary>
        private void StopAllEffects()
        {
            if (_loopEffect != null)
                _loopEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            if (_hitEffect != null)
                _hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
```

**使用方式**：
- `ViewBridge` 在生成弹道实体时：
  1. 为对应的 `EntityView` 添加 `ProjectileViewComponent`
  2. 根据 `SocketRefs` 计算表现层发射位置
  3. 调用 `SetInitialVisualSpawnPosition` 设置表现层起始位置
- `ProjectileCapability` 在检测到命中时，通过 `EntityView` 获取 `ProjectileViewComponent` 并调用 `PlayHitEffect`
- 对象池回收时调用 `ResetVisual`，清除拖尾和粒子残留

**位置同步策略**：
1. **初始阶段**：表现层从 Socket 位置出发，逻辑层从 Entity 位置出发，存在初始偏移
2. **飞行阶段**：表现层通过插值逐渐追赶逻辑层位置，保持平滑过渡
3. **异常处理**：如果偏移超过阈值（`_maxCatchUpDistance`），强制同步到逻辑位置

**Prefab要求**：
- 弹道Prefab根节点或子节点上挂载 `TrailRenderer`（可选）
- 粒子系统命名约定：包含"Loop"或"Trail"的为循环特效，包含"Hit"或"Impact"的为命中特效

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
            projectileComponent.PierceCount = definition.PierceCount;
            projectileComponent.LaunchDirection = spawn.Direction.normalized;
            projectileComponent.CurrentVelocity = ComputeInitialVelocity(projectileComponent);
        }

        // 设置逻辑层位置
        var trans = projectile.GetComponent<TransComponent>();
        if (trans != null)
        {
            trans.Position = spawn.Position;
        }

        projectileComponent.LastPosition = spawn.Position;
        
        // 设置表现层初始位置（从Socket获取）
        InitializeProjectileView(projectile, caster, spawn);
    }
    
    /// <summary>
    /// 初始化弹道表现层（设置Socket发射位置）
    /// </summary>
    private void InitializeProjectileView(Entity projectile, Entity caster, (TSVector Position, TSVector Direction) logicSpawn)
    {
        // 获取ViewBridge
        var viewBridge = ViewBridge.Instance;
        if (viewBridge == null) return;
        
        // 获取弹道的EntityView
        var projectileView = viewBridge.GetEntityView(projectile.UniqueId);
        if (projectileView == null) return;
        
        // 获取ProjectileViewComponent
        var viewComponent = projectileView.GetViewComponent<ProjectileViewComponent>();
        if (viewComponent == null) return;
        
        // 计算表现层发射位置（从施法者的Socket获取）
        Vector3 visualSpawnPosition = CalculateVisualSpawnPosition(caster, logicSpawn);
        
        // 设置表现层初始位置
        viewComponent.SetInitialVisualSpawnPosition(visualSpawnPosition);
        
        ASLogger.Instance.Debug(
            $"InitializeProjectileView: ProjectileId={projectile.UniqueId}, " +
            $"LogicSpawn={logicSpawn.Position}, VisualSpawn={visualSpawnPosition}");
    }
    
    /// <summary>
    /// 计算表现层发射位置（从施法者的Socket或模型位置）
    /// </summary>
    private Vector3 CalculateVisualSpawnPosition(Entity caster, (TSVector Position, TSVector Direction) logicSpawn)
    {
        // 尝试从ViewBridge获取施法者的视图对象
        var viewBridge = ViewBridge.Instance;
        var casterView = viewBridge?.GetEntityView(caster.UniqueId);
        
        if (casterView != null)
        {
            // 尝试从SocketRefs获取发射点位置
            var socketRefs = casterView.GameObject?.GetComponent<SocketRefs>();
            if (socketRefs != null)
            {
                // 假设使用 "MuzzlePoint" 或 "WeaponTip" 作为默认发射点
                var socketTransform = socketRefs.GetSocketTransform("MuzzlePoint") 
                                   ?? socketRefs.GetSocketTransform("WeaponTip");
                
                if (socketTransform != null)
                {
                    return socketTransform.position;
                }
            }
            
            // 如果没有Socket，使用EntityView的世界位置
            return casterView.GetWorldPosition();
        }
        
        // 回退：使用逻辑层位置
        return new Vector3((float)logicSpawn.Position.x, (float)logicSpawn.Position.y, (float)logicSpawn.Position.z);
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
    component.LastPosition = trans.Position;
    trans.Position += component.CurrentVelocity;
    CheckRaycastCollision(entity, component, trans);
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
    component.LastPosition = trans.Position;
    trans.Position += component.CurrentVelocity;
    CheckRaycastCollision(entity, component, trans);
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
    
    component.LastPosition = trans.Position;
    trans.Position += component.CurrentVelocity;
    CheckRaycastCollision(entity, component, trans);
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

### 9.1 射线碰撞策略

- 每帧记录弹道的上一帧位置 `prevPos` 与当前更新后的位置 `currPos`
- 使用物理世界（或自定义空间索引）执行 `Raycast(prevPos → currPos)`
- 命中顺序：根据射线距离排序，逐个处理命中体
- 可选：对射线路径进行多段抽样（供高速弹体使用），或在帧内细分
- 在轨迹更新前写入 `component.LastPosition = trans.Position`，射线检测完成后再更新为新位置

```csharp
private void CheckRaycastCollision(Entity projectile, ProjectileComponent component, TransComponent trans)
{
    var prevPos = component.LastPosition;
    var currPos = trans.Position;
    var direction = currPos - prevPos;
    var distance = direction.magnitude;
    if (distance <= FP.Epsilon)
        return;

    var rayHits = PhysicsWorld.Raycast(prevPos, direction.normalized, distance);
    foreach (var hit in rayHits)
    {
        if (!ShouldCollide(component, hit.EntityId))
            continue;

        OnRayHit(projectile, component, hit);
        if (component.PiercedCount > component.PierceCount)
            break;
    }

    component.LastPosition = currPos;
}
```

### 9.2 穿透与命中记录

- `PierceCount`：允许穿透的目标数量（0 表示不穿透）
- `PiercedCount`：当前已穿透目标数
- `HitEntities`：已命中的实体ID集合，用于防止同一路径内重复命中

```
Raycast 命中 → 过滤（阵营、重复命中）
    ↓
触发效果 → PiercedCount++
    ↓
PiercedCount > PierceCount? 
    └── 是：销毁弹道
    └── 否：继续处理下一段射线（若有）
```

### 9.3 射线命中处理

**OnRayHit方法实现**：

```csharp
private void OnRayHit(Entity projectile, ProjectileComponent component, RaycastHit hit)
{
    var hitEntity = hit.EntityId;
    
    // 防止重复命中同一实体
    if (component.HitEntities.Contains(hitEntity))
        return;
    
    // 记录命中
    component.HitEntities.Add(hitEntity);
    component.PiercedCount++;
    
    // 获取命中实体
    var targetEntity = projectile.World.GetEntityById(hitEntity);
    if (targetEntity != null)
    {
        // 触发技能效果
        TriggerSkillEffect(projectile, component, targetEntity);
    }
    
    // 触发视觉表现
    TriggerHitVisual(projectile, hit.Position);
    
    // 检查是否应该销毁弹道（非穿透 or 超过穿透上限）
    if (component.PierceCount == 0 || component.PiercedCount > component.PierceCount)
    {
        DestroyProjectile(projectile);
    }
}

private void TriggerHitVisual(Entity projectile, TSVector hitPosition)
{
    // 通过ViewBridge获取EntityView
    var viewBridge = ViewBridge.Instance; // 假设为单例
    var entityView = viewBridge.GetEntityView(projectile.UniqueId);
    
    if (entityView != null)
    {
        // 获取ProjectileViewComponent
        var viewComponent = entityView.GetViewComponent<ProjectileViewComponent>();
        if (viewComponent != null)
        {
            // 调用视觉表现方法
            viewComponent.PlayHitEffect(hitPosition);
        }
    }
}
```

### 9.4 效果触发

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
- 逻辑层通过ViewBridge调用视图层，保持架构清晰

---

## 10. 逻辑层与表现层位置同步

### 10.1 核心问题

**问题描述**：
- **逻辑层**：弹道从 Entity 的 `TransComponent` 位置（角色中心点）出发
- **表现层**：弹道应该从模型的 Socket 点（如法杖顶端、弓箭发射点）出发
- **初始偏移**：两者存在初始位置差异，需要在飞行过程中逐渐消除

### 10.2 同步策略

#### 阶段一：初始化（首帧）

```
逻辑层生成弹道 → ViewBridge创建EntityView → 设置表现层初始位置
    ↓
逻辑层位置: Entity.TransComponent.Position (角色中心)
表现层位置: SocketRefs.GetSocketTransform("MuzzlePoint").position (法杖顶端)
    ↓
记录初始偏移: visualOffset = visualSpawn - logicSpawn
```

**实现**：
1. `ProjectileSpawnCapability.InitializeProjectileRuntime` 设置逻辑层位置
2. `ProjectileSpawnCapability.InitializeProjectileView` 调用 `ViewBridge` 获取 Socket 位置
3. `ProjectileViewComponent.SetInitialVisualSpawnPosition` 设置表现层起始位置

#### 阶段二：飞行过程（持续）

```
每帧 Update:
    逻辑层位置更新 (由 ProjectileCapability.Tick 驱动)
        ↓
    表现层追赶逻辑层 (由 ProjectileViewComponent.OnUpdate 驱动)
        ↓
    插值计算: visualPos = Lerp(currentVisualPos, logicPos + initialOffset, catchUpSpeed * deltaTime)
        ↓
    应用到 GameObject: EntityView.SetWorldPosition(visualPos)
```

**关键参数**：
- `_catchUpSpeed = 10f`: 追赶速度系数，值越大追赶越快
- `_maxCatchUpDistance = 2f`: 最大允许偏移，超过则强制同步

#### 阶段三：命中时刻

```
逻辑层检测到碰撞 (ProjectileCapability.CheckRaycastCollision)
    ↓
触发视觉效果 (TriggerHitVisual)
    ↓
使用表现层当前位置播放命中特效 (ProjectileViewComponent.PlayHitEffect)
```

**优势**：命中特效位置使用表现层位置，与玩家看到的弹道轨迹一致

### 10.3 代码流程图

```
[逻辑层] ProjectileSpawnCapability.OnProjectileSpawnRequested
    ↓
[逻辑层] CreateProjectileEntityViaFactory (创建Entity)
    ↓
[逻辑层] InitializeProjectileRuntime
    ├─ 设置 TransComponent.Position = logicSpawnPos
    └─ 调用 InitializeProjectileView
        ↓
    [桥接层] InitializeProjectileView
        ├─ 通过 ViewBridge.GetEntityView 获取 EntityView
        ├─ 通过 CalculateVisualSpawnPosition 从 SocketRefs 获取表现层位置
        └─ 调用 ProjectileViewComponent.SetInitialVisualSpawnPosition
            ↓
        [表现层] ProjectileViewComponent.SetInitialVisualSpawnPosition
            └─ EntityView.SetWorldPosition(visualSpawnPos)

[每帧更新]
[逻辑层] ProjectileCapability.Tick
    └─ 更新 TransComponent.Position (逻辑轨迹)
        ↓
[表现层] ProjectileViewComponent.OnUpdate
    ├─ 读取 TransComponent.Position (当前逻辑位置)
    ├─ 计算目标表现位置: targetPos = logicPos + initialOffset
    ├─ 插值追赶: visualPos = Lerp(currentVisualPos, targetPos, speed * dt)
    └─ EntityView.SetWorldPosition(visualPos)
```

### 10.4 配置建议

**快速追赶（适合高速弹道）**：
```csharp
_catchUpSpeed = 20f;  // 快速消除偏移
_maxCatchUpDistance = 1f;  // 较小的容错距离
```

**平滑过渡（适合慢速弹道）**：
```csharp
_catchUpSpeed = 5f;  // 缓慢追赶，保持更长时间的视觉偏移
_maxCatchUpDistance = 3f;  // 较大的容错距离
```

### 10.5 注意事项

1. **Socket命名约定**：建议统一使用 `"MuzzlePoint"` 或 `"WeaponTip"` 作为弹道发射点
2. **回退机制**：如果 Socket 不存在，自动回退到 EntityView 的世界位置
3. **对象池回收**：`ProjectileViewComponent.ResetVisual` 会重置同步数据，确保下次使用时重新初始化
4. **网络同步**：逻辑层位置由帧同步保证一致性，表现层位置仅本地计算，不参与网络同步

---

## 11. 典型应用场景

### 11.1 法师火球术（连射）

**需求**：
- 按住技能键连续射出火球
- 火球直线飞行，命中敌人造成伤害
- 松开技能键后进入后摇

**配置**：
- PrecastAction（前摇20帧）→ CastAction（30帧，尾声5帧可自我取消）→ RecoveryAction（后摇15帧）
- CastAction自我取消机制：第25-30帧设置BeCancelledTag，自己的CancelTag匹配，持续输入时循环
- Projectile：直线轨迹（TrajectoryData.BaseSpeed=0.8），生命周期300帧，球形碰撞半径0.5
- SkillEffect：伤害效果，150%攻击力

### 11.2 弓箭手蓄力箭（蓄力）

**需求**：
- 按住技能键蓄力
- 根据蓄力时长射出不同强度的箭
- 箭受重力影响，呈抛物线飞行

**配置**：
- ChargingAction（最大180帧）→ ReleaseAction（释放20帧）
- Condition触发：ChargeTime<30→弱箭，30-60→中箭，>=60→强箭
- Projectile：抛物线轨迹，重力[0,-0.05,0]，生命周期200帧
- SkillEffect：根据蓄力等级，伤害100%/150%/200%

### 11.3 追踪导弹（追踪）

**需求**：
- 射出导弹，自动追踪最近的敌人
- 导弹有转向速度限制，不会瞬间转向

**配置**：
- CastAction（生成导弹）
- Projectile：追踪轨迹，TurnRate=0.1，生命周期600帧
- 生成时查询最近敌人，将其ID存入TrajectoryData
- SkillEffect：爆炸伤害，AOE范围3.0

### 11.4 链式闪电（穿透）

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
            component.LastPosition = TSVector.zero;
            component.SkillEffectIds.Clear();
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
```