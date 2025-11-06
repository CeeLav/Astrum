# 受击与击退效果技术设计

**版本**: v1.2.1  
**创建日期**: 2025-01-08  
**最后更新**: 2025-11-06  
**状态**: 架构修订中（完全封装版）

> 📖 **相关文档**：
>
> - [技能效果运行时](../技能系统/Skill-Effect-Runtime%20技能效果运行时.md) - 技能效果系统总览
> - [事件系统升级](../../05-CoreArchitecture%20核心架构/事件/Event-Queue-System%20事件队列系统.md) - 全局事件队列设计
> - [动作系统](../技能系统/Action-System%20动作系统.md) - 受击动作播放

---

## 📋 目录

1. [系统概述](#系统概述)
2. [架构设计](#架构设计)
3. [击退系统](#击退系统)
4. [受击系统](#受击系统)
5. [数据流程](#数据流程)
6. [配置设计](#配置设计)
7. [实现细节](#实现细节)

---

## 系统概述

### 1.1 设计目标

击退效果是战斗系统中常见的控制效果，本设计旨在实现：

1. **清晰的职责分离**：

   - `HitReactionCapability` - 处理受击逻辑（动作、音效、特效）
   - `KnockbackCapability` - 处理击退位移
   - `KnockbackComponent` - 存储击退数据
2. **事件驱动架构**：

   - 基于全局事件队列
   - Capability 主动消费事件，而非被动监听
   - 避免订阅/取消订阅的复杂性
3. **可扩展性**：

   - 支持多种击退类型（线性、抛物线、击飞等）
   - 支持击退打断、抵抗、减免等机制
   - 为后续硬直、霸体等效果预留接口

### 1.2 效果类型

根据技能效果表格定义：

- **类型 3 = 击退效果**
  - `EffectValue`: 击退距离（单位：米）
  - `EffectDuration`: 击退持续时间（单位：秒）
  - 方向：默认为施法者朝向，可扩展为自定义方向

### 1.3 核心流程（v1.2.1 完全封装版）

```
【1. 碰撞检测】
SkillExecutorCapability → 检测命中目标
    ↓
【2. 效果入队】
SkillEffectSystem.QueueSkillEffect(SkillEffectData)
    ↓
【3. 效果处理】
SkillEffectSystem.Update() → EffectHandler.Handle()（只读不写）
    ↓
    ├─ DamageEffectHandler (type=1)
    │   ├─ 读取 Stats 组件（计算伤害）
    │   ├─ ❌ 不修改任何组件
    │   ├─ 发送 DamageEvent → DamageCapability
    │   └─ 发送 HitReactionEvent → HitReactionCapability
    │
    ├─ KnockbackEffectHandler (type=3)
    │   ├─ 读取 Trans 组件（计算方向）
    │   ├─ ❌ 不修改任何组件
    │   ├─ 发送 KnockbackEvent → KnockbackCapability
    │   └─ 发送 HitReactionEvent → HitReactionCapability
    │
    └─ ... (其他 Handler)
    
【4. Capability 响应】（只能修改自身实体组件）
    ├─ DamageCapability (接收 DamageEvent)
    │   ├─ 修改 DynamicStats（扣血）
    │   └─ 检查死亡状态
    │
    ├─ HitReactionCapability (接收 HitReactionEvent)
    │   ├─ 播放受击动作
    │   └─ 播放受击特效
    │
    └─ KnockbackCapability (接收 KnockbackEvent)
        ├─ 写入 KnockbackComponent
        ├─ 禁用移动输入
        └─ Tick: 应用击退位移
```

### 1.4 架构原则（v1.2.1 完全封装版）

#### **职责分离**：

**EffectHandler（计算器）**：
- ✅ 读取配置表
- ✅ 读取实体组件（只读，用于计算）
- ✅ 计算效果参数（伤害、方向、距离等）
- ❌ **不修改任何组件**（包括立即生效的效果）
- ✅ 发送事件给目标实体的 Capability

**Capability（执行器）**：
- ✅ 接收 Handler 发送的事件
- ✅ 修改自身实体的组件
- ✅ 管理生命周期
- ✅ 触发表现（动作、特效）

#### **统一原则**：

| 组件           | Handler 是否可修改 | Capability 是否可修改 |
| -------------- | ------------------ | --------------------- |
| DynamicStats   | ❌ 不可以          | ✅ 只能修改自身       |
| KnockbackComp  | ❌ 不可以          | ✅ 只能修改自身       |
| StateComp      | ❌ 不可以          | ✅ 只能修改自身       |
| 所有组件       | ❌ **统一不可修改** | ✅ **只能修改自身**   |

#### **好处**：
- 🔒 **完全封装**：组件数据只能由实体自身的 Capability 修改
- 📐 **统一原则**：无例外情况，所有 Handler 都遵循相同规则
- 🔄 **可测试性**：Handler 纯计算函数，Capability 职责单一
- 🛡️ **安全性**：避免跨实体的状态修改
- 📦 **扩展性**：新增效果模式统一（Handler → Event → Capability）

---

## 架构设计

### 2.1 组件设计

#### KnockbackComponent

存储击退状态数据。

```csharp
namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 击退组件 - 存储实体的击退状态
    /// </summary>
    [MemoryPackable]
    public partial class KnockbackComponent : Component<KnockbackComponent>
    {
        /// <summary>是否正在击退</summary>
        public bool IsKnockingBack { get; set; }
  
        /// <summary>击退方向（世界空间，单位向量）</summary>
        public TSVector Direction { get; set; }
  
        /// <summary>击退速度（米/秒）</summary>
        public FP Speed { get; set; }
  
        /// <summary>击退剩余时间（秒）</summary>
        public FP RemainingTime { get; set; }
  
        /// <summary>击退总距离（用于计算）</summary>
        public FP TotalDistance { get; set; }
  
        /// <summary>已移动距离</summary>
        public FP MovedDistance { get; set; }
  
        /// <summary>击退类型</summary>
        public KnockbackType Type { get; set; }
  
        /// <summary>施法者ID（用于方向计算）</summary>
        public long CasterId { get; set; }
    }
  
    /// <summary>
    /// 击退类型
    /// </summary>
    public enum KnockbackType
    {
        /// <summary>线性击退（匀速）</summary>
        Linear = 0,
  
        /// <summary>减速击退（先快后慢）</summary>
        Decelerate = 1,
  
        /// <summary>击飞（抛物线，预留）</summary>
        Launch = 2,
    }
}
```

---

### 2.2 事件设计（v1.2.1 完全封装版）

#### DamageEvent（新增）

伤害事件，由 `DamageEffectHandler` 发送给目标实体的 `DamageCapability`。

```csharp
namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 伤害事件（由 DamageEffectHandler 发送给 DamageCapability）
    /// </summary>
    public struct DamageEvent
    {
        /// <summary>施法者ID</summary>
        public long CasterId;
        
        /// <summary>效果ID</summary>
        public int EffectId;
        
        /// <summary>计算后的最终伤害值</summary>
        public FP Damage;
        
        /// <summary>是否暴击</summary>
        public bool IsCritical;
        
        /// <summary>伤害类型（1=物理/2=魔法/3=真实）</summary>
        public int DamageType;
    }
}
```

---

#### KnockbackEvent

击退事件，由 `KnockbackEffectHandler` 发送给目标实体的 `KnockbackCapability`。

```csharp
namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 击退事件（由 KnockbackEffectHandler 发送给 KnockbackCapability）
    /// </summary>
    public struct KnockbackEvent
    {
        /// <summary>施法者ID</summary>
        public long CasterId;
        
        /// <summary>效果ID</summary>
        public int EffectId;
        
        /// <summary>击退方向（世界空间，单位向量）</summary>
        public TSVector Direction;
        
        /// <summary>击退距离（米）</summary>
        public FP Distance;
        
        /// <summary>击退持续时间（秒）</summary>
        public FP Duration;
        
        /// <summary>击退类型</summary>
        public KnockbackType Type;
    }
}
```

---

#### HitReactionEvent

受击反馈事件，由各类效果处理器（DamageEffectHandler, KnockbackEffectHandler 等）发送给 `HitReactionCapability`，用于触发受击表现。

```csharp
namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 受击反馈事件（由效果处理器发送给 HitReactionCapability）
    /// </summary>
    public struct HitReactionEvent
    {
        /// <summary>施法者ID</summary>
        public long CasterId;
        
        /// <summary>受击者ID</summary>
        public long TargetId;
        
        /// <summary>效果ID</summary>
        public int EffectId;
        
        /// <summary>效果类型（1=伤害, 2=治疗, 3=击退等）</summary>
        public int EffectType;
        
        /// <summary>受击方向（用于播放受击动作）</summary>
        public TSVector HitDirection;
        
        /// <summary>是否产生硬直</summary>
        public bool CausesStun;
    }
}
```

---

### 2.3 EffectHandler 设计（v1.2 修订）

#### KnockbackEffectHandler

**职责**：
- ✅ 计算击退方向（从施法者指向目标）
- ✅ 计算击退距离和速度
- ✅ 发送 `KnockbackEvent` 给目标
- ✅ 发送 `HitReactionEvent` 给目标（用于表现）
- ❌ **不直接修改 KnockbackComponent**

```csharp
namespace Astrum.LogicCore.SkillSystem.EffectHandlers
{
    public class KnockbackEffectHandler : IEffectHandler
    {
        public void Handle(Entity caster, Entity target, SkillEffectTable effectConfig)
        {
            // 1. 计算击退方向
            var direction = CalculateKnockbackDirection(caster, target);
            
            // 2. 读取配置参数
            FP distance = FP.FromFloat(effectConfig.EffectValue / 1000f); // 毫米 → 米
            FP duration = FP.FromFloat(effectConfig.EffectDuration); // 秒
            
            // 3. 构造击退事件
            var knockbackEvent = new KnockbackEvent
            {
                CasterId = caster.UniqueId,
                EffectId = effectConfig.SkillEffectId,
                Direction = direction,
                Distance = distance,
                Duration = duration,
                Type = KnockbackType.Linear // 默认线性，后续可从配置读取
            };
            
            // 4. 发送事件给目标的 KnockbackCapability
            target.QueueEvent(knockbackEvent);
            
            // 5. 发送受击反馈事件（用于表现）
            var hitReactionEvent = new HitReactionEvent
            {
                CasterId = caster.UniqueId,
                TargetId = target.UniqueId,
                EffectId = effectConfig.SkillEffectId,
                EffectType = effectConfig.EffectType,
                HitDirection = direction,
                CausesStun = true // 击退产生硬直
            };
            
            target.QueueEvent(hitReactionEvent);
        }
        
        private TSVector CalculateKnockbackDirection(Entity caster, Entity target)
        {
            // 只读组件数据，用于计算
            var casterTrans = caster.GetComponent<TransComponent>();
            var targetTrans = target.GetComponent<TransComponent>();
            // ... 计算逻辑 ...
        }
    }
}
```

#### DamageEffectHandler（v1.2.1 完全封装版）

**职责**：
- ✅ 读取配置表
- ✅ 读取 Stats 组件（只读，用于计算）
- ✅ 计算伤害（使用 DamageCalculator）
- ❌ **不修改任何组件**
- ✅ 发送 `DamageEvent` 给目标
- ✅ 发送 `HitReactionEvent` 给目标

```csharp
namespace Astrum.LogicCore.SkillSystem.EffectHandlers
{
    public class DamageEffectHandler : IEffectHandler
    {
        public void Handle(Entity caster, Entity target, SkillEffectTable effectConfig)
        {
            // 1. 读取组件（只读）
            var casterStats = caster.GetComponent<DerivedStatsComponent>();
            var targetStats = target.GetComponent<DynamicStatsComponent>();
            var targetDerived = target.GetComponent<DerivedStatsComponent>();
            
            if (targetStats == null || targetDerived == null)
                return;
            
            // 2. 计算伤害（纯计算，不修改状态）
            var damageResult = DamageCalculator.Calculate(
                caster, target, effectConfig, 
                caster.World?.CurFrame ?? 0
            );
            
            // 3. 发送伤害事件给目标（由 DamageCapability 接收并扣血）
            var damageEvent = new DamageEvent
            {
                CasterId = caster.UniqueId,
                EffectId = effectConfig.SkillEffectId,
                Damage = damageResult.FinalDamage,
                IsCritical = damageResult.IsCritical,
                DamageType = effectConfig.DamageType
            };
            
            target.QueueEvent(damageEvent);
            
            // 4. 发送受击反馈事件（用于播放受击动作和特效）
            var hitReactionEvent = new HitReactionEvent
            {
                CasterId = caster.UniqueId,
                TargetId = target.UniqueId,
                EffectId = effectConfig.SkillEffectId,
                EffectType = effectConfig.EffectType,
                HitDirection = CalculateHitDirection(caster, target),
                CausesStun = damageResult.IsCritical // 暴击产生硬直
            };
            
            target.QueueEvent(hitReactionEvent);
        }
        
        private TSVector CalculateHitDirection(Entity caster, Entity target)
        {
            // 只读组件数据，用于计算
            var casterTrans = caster.GetComponent<TransComponent>();
            var targetTrans = target.GetComponent<TransComponent>();
            // ... 计算逻辑 ...
        }
    }
}
```

---

### 2.4 Capability 设计

#### DamageCapability（v1.2.1 新增）

**职责**：
- ✅ 接收 `DamageEvent`，应用伤害
- ✅ 修改 DynamicStatsComponent（扣血）
- ✅ 检查死亡状态
- ✅ 发布死亡事件（View 层）

```csharp
namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 伤害处理能力 - 处理实体受到的伤害
    /// 优先级：200（与 HitReactionCapability 同级）
    /// </summary>
    public class DamageCapability : Capability<DamageCapability>
    {
        public override int Priority => 200;
  
        public override IReadOnlyCollection<CapabilityTag> Tags => new[] 
        { 
            CapabilityTag.Combat
        };
  
        public override bool ShouldActivate(Entity entity)
        {
            return base.ShouldActivate(entity) &&
                   HasComponent<DynamicStatsComponent>(entity);
        }
        
        // ====== 事件处理 ======
        
        protected override void RegisterEventHandlers()
        {
            RegisterEventHandler<DamageEvent>(OnDamage);
        }
        
        /// <summary>
        /// 接收伤害事件，应用伤害
        /// </summary>
        private void OnDamage(Entity entity, DamageEvent evt)
        {
            // 1. 获取组件（自身实体）
            var dynamicStats = GetComponent<DynamicStatsComponent>(entity);
            var derivedStats = GetComponent<DerivedStatsComponent>(entity);
            var stateComp = GetComponent<StateComponent>(entity);
            
            if (dynamicStats == null || derivedStats == null)
                return;
            
            // 2. 检查是否可以受到伤害
            if (stateComp != null && !stateComp.CanTakeDamage())
                return;
            
            // 3. 应用伤害（修改自身组件）
            FP beforeHP = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
            FP actualDamage = dynamicStats.TakeDamage(evt.Damage, derivedStats);
            FP afterHP = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
            
            ASLogger.Instance.Info($"[DamageCapability] HP: {beforeHP} → {afterHP} (-{actualDamage})");
            
            // 4. 检查死亡
            if (afterHP <= FP.Zero && stateComp != null && !stateComp.Get(StateType.DEAD))
            {
                // 设置死亡状态（修改自身组件）
                stateComp.Set(StateType.DEAD, true);
                
                // 发布死亡事件（View 层）
                var diedEvent = new EntityDiedEventData(
                    entity: entity,
                    worldId: 0,
                    roomId: 0,
                    killerId: evt.CasterId,
                    skillId: evt.EffectId
                );
                EventSystem.Instance.Publish(diedEvent);
                
                ASLogger.Instance.Info($"[DamageCapability] Entity {entity.UniqueId} DIED");
            }
        }
    }
}
```

---

#### KnockbackCapability（v1.2 修订）

**职责**：
- ✅ 接收 `KnockbackEvent`，写入 `KnockbackComponent`
- ✅ 在激活时禁用移动输入
- ✅ 每帧应用击退位移
- ✅ 击退结束时清理状态

```csharp
namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 击退能力 - 处理实体的击退位移
    /// 优先级：150（高于移动，低于技能执行）
    /// </summary>
    public class KnockbackCapability : Capability<KnockbackCapability>
    {
        public override int Priority => 150;
  
        public override IReadOnlyCollection<CapabilityTag> Tags => new[] 
        { 
            CapabilityTag.Movement, 
            CapabilityTag.Combat,
        };
        
        private long _knockbackInstigatorId;
        
        // ====== 事件处理 ======
        
        protected override void RegisterEventHandlers()
        {
            RegisterEventHandler<KnockbackEvent>(OnKnockback);
        }
        
        /// <summary>
        /// 接收击退事件，写入组件数据
        /// </summary>
        private void OnKnockback(Entity entity, KnockbackEvent evt)
        {
            var knockback = GetOrAddComponent<KnockbackComponent>(entity);
            
            // 写入击退数据
            knockback.IsKnockingBack = true;
            knockback.Direction = evt.Direction;
            knockback.TotalDistance = evt.Distance;
            knockback.RemainingTime = evt.Duration;
            knockback.Speed = evt.Distance / evt.Duration;
            knockback.MovedDistance = FP.Zero;
            knockback.Type = evt.Type;
            knockback.CasterId = evt.CasterId;
            
            ASLogger.Instance.Info($"[KnockbackCapability] Knockback data written: " +
                $"distance={evt.Distance}m, duration={evt.Duration}s");
        }
        
        // ====== 生命周期 ======
  
        public override bool ShouldActivate(Entity entity)
        {
            var knockback = GetComponent<KnockbackComponent>(entity);
            return base.ShouldActivate(entity) &&
                   knockback != null &&
                   knockback.IsKnockingBack &&
                   HasComponent<PositionComponent>(entity);
        }
    
        public override bool ShouldDeactivate(Entity entity)
        {
            var knockback = GetComponent<KnockbackComponent>(entity);
            return base.ShouldDeactivate(entity) ||
                   knockback == null ||
                   !knockback.IsKnockingBack ||
                   !HasComponent<PositionComponent>(entity);
        }
        
        public override void OnActivate(Entity entity)
        {
            base.OnActivate(entity);
            
            var knockback = GetComponent<KnockbackComponent>(entity);
            _knockbackInstigatorId = knockback?.CasterId ?? entity.UniqueId;
            
            // 禁用用户输入位移
            entity.World?.CapabilitySystem?.DisableCapabilitiesByTag(
                entity, 
                CapabilityTag.UserInputMovement, 
                _knockbackInstigatorId, 
                "Knockback active"
            );
        }
        
        public override void OnDeactivate(Entity entity)
        {
            entity.World?.CapabilitySystem?.EnableCapabilitiesByTag(
                entity, 
                CapabilityTag.UserInputMovement, 
                _knockbackInstigatorId
            );
            
            base.OnDeactivate(entity);
        }
  
        public override void Tick(Entity entity)
        {
            var knockback = GetComponent<KnockbackComponent>(entity);
            if (knockback == null || !knockback.IsKnockingBack)
                return;
  
            var position = GetComponent<PositionComponent>(entity);
            if (position == null)
                return;
  
            // 计算本帧位移
            FP deltaTime = entity.World.DeltaTime;
            FP moveDistance = CalculateMoveDistance(knockback, deltaTime);
  
            // 应用位移
            TSVector movement = knockback.Direction * moveDistance;
            position.Position += movement;
            knockback.MovedDistance += moveDistance;
            knockback.RemainingTime -= deltaTime;
  
            // 检查是否结束
            if (knockback.RemainingTime <= FP.Zero || 
                knockback.MovedDistance >= knockback.TotalDistance)
            {
                EndKnockback(knockback);
            }
        }
  
        private FP CalculateMoveDistance(KnockbackComponent knockback, FP deltaTime)
        {
            switch (knockback.Type)
            {
                case KnockbackType.Linear:
                    return knockback.Speed * deltaTime;
          
                case KnockbackType.Decelerate:
                    // 使用线性减速：速度随时间衰减
                    FP progress = FP.One - (knockback.RemainingTime / 
                        (knockback.TotalDistance / knockback.Speed));
                    FP currentSpeed = knockback.Speed * (FP.One - progress);
                    return currentSpeed * deltaTime;
          
                default:
                    return knockback.Speed * deltaTime;
            }
        }
  
        private void EndKnockback(KnockbackComponent knockback)
        {
            knockback.IsKnockingBack = false;
            knockback.RemainingTime = FP.Zero;
            knockback.Speed = FP.Zero;
        }
    }
}
```

#### HitReactionCapability

负责处理受击反馈（动作、特效、声音、写入击退数据）。

```csharp
namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 受击反应能力 - 处理实体受到技能效果的反馈
    /// 优先级：200（低于技能执行，高于击退）
    /// </summary>
    public class HitReactionCapability : Capability<HitReactionCapability>
    {
        public override int Priority => 200;
  
        public override IReadOnlyCollection<CapabilityTag> Tags => new[] 
        { 
            CapabilityTag.Combat,
            CapabilityTag.Animation
        };
  
        public override bool ShouldActivate(Entity entity)
        {
            return base.ShouldActivate(entity) &&
                   HasComponent<ActionComponent>(entity);
        }
        
        // 静态声明：该 Capability 处理的事件
        protected override void RegisterEventHandlers()
        {
            RegisterEventHandler<SkillEffectEvent>(OnSkillEffect);
        }
        
        // 事件处理函数（由 CapabilitySystem 自动调度，第一个参数必须是 Entity）
        private void OnSkillEffect(Entity entity, SkillEffectEvent evt)
        {
            ProcessSkillEffect(entity, evt);
        }
  
        private void ProcessSkillEffect(Entity entity, SkillEffectEvent evt)
        {
            // 获取效果配置
            var effectConfig = GetEffectConfig(evt.EffectId);
            if (effectConfig == null)
                return;
    
            // 根据效果类型处理
            switch (effectConfig.EffectType)
            {
                case 1: // 伤害
                    ProcessDamage(entity, evt, effectConfig);
                    break;
            
                case 2: // 治疗
                    ProcessHeal(entity, evt, effectConfig);
                    break;
            
                case 3: // 击退
                    ProcessKnockback(entity, evt, effectConfig);
                    break;
            
                case 4: // Buff
                    ProcessBuff(entity, evt, effectConfig);
                    break;
            
                case 5: // Debuff
                    ProcessDebuff(entity, evt, effectConfig);
                    break;
            }
        }
  
        private void ProcessKnockback(Entity entity, SkillEffectEvent evt, SkillEffectConfig config)
        {
            // 1. 播放受击动作
            PlayHitAction(entity, evt.CasterId);
    
            // 2. 播放受击特效
            PlayHitVFX(entity, evt.CasterId);
    
            // 3. 写入击退数据
            var knockback = GetOrAddComponent<KnockbackComponent>(entity);
            var caster = entity.World?.GetEntity(evt.CasterId);
    
            if (caster != null)
            {
                // 计算击退方向（施法者朝向目标）
                var direction = CalculateKnockbackDirection(caster, entity);
        
                // 设置击退参数
                knockback.IsKnockingBack = true;
                knockback.Direction = direction;
                knockback.TotalDistance = FP.FromFloat(config.EffectValue); // 米
                knockback.RemainingTime = FP.FromFloat(config.EffectDuration); // 秒
                knockback.Speed = knockback.TotalDistance / knockback.RemainingTime;
                knockback.MovedDistance = FP.Zero;
                knockback.Type = KnockbackType.Linear; // 默认线性
                knockback.CasterId = evt.CasterId;
        
                ASLogger.Instance.Debug($"Applied knockback: distance={config.EffectValue}m, " +
                    $"duration={config.EffectDuration}s, speed={knockback.Speed}m/s");
            }
        }
  
        private TSVector CalculateKnockbackDirection(Entity caster, Entity target)
        {
            var casterPos = GetComponent<PositionComponent>(caster);
            var targetPos = GetComponent<PositionComponent>(target);
    
            if (casterPos == null || targetPos == null)
                return TSVector.forward;
    
            // 从施法者指向目标
            TSVector direction = targetPos.Position - casterPos.Position;
            direction.y = FP.Zero; // 只在水平面击退
            return TSVector.Normalize(direction);
        }
  
        private void PlayHitAction(Entity entity, long casterId)
        {
            // TODO: 根据攻击方向播放不同的受击动作
            // 临时：播放通用受击动作
            var action = GetComponent<ActionComponent>(entity);
            if (action != null)
            {
                // action.PlayAction("Hit", priority: ActionPriority.High);
            }
        }
  
        private void PlayHitVFX(Entity entity, long casterId)
        {
            // TODO: 发布受击特效事件到 View 层
            // EventSystem.Instance.Publish(new HitVFXEvent { ... });
        }
  
        private void ProcessDamage(Entity entity, SkillEffectEvent evt, SkillEffectConfig config)
        {
            // TODO: 伤害处理
            PlayHitAction(entity, evt.CasterId);
            PlayHitVFX(entity, evt.CasterId);
        }
  
        private void ProcessHeal(Entity entity, SkillEffectEvent evt, SkillEffectConfig config)
        {
            // TODO: 治疗处理
        }
  
        private void ProcessBuff(Entity entity, SkillEffectEvent evt, SkillEffectConfig config)
        {
            // TODO: Buff处理
        }
  
        private void ProcessDebuff(Entity entity, SkillEffectEvent evt, SkillEffectConfig config)
        {
            // TODO: Debuff处理
        }
  
        private SkillEffectConfig GetEffectConfig(int effectId)
        {
            var config = TableConfig.Instance?.Tables?.TbSkillEffectTable?.GetOrDefault(effectId);
            return config;
        }
    }
}
```

---

## 击退系统

### 3.1 击退类型

| 类型           | 说明         | 速度曲线 | 适用场景       |
| -------------- | ------------ | -------- | -------------- |
| `Linear`     | 线性击退     | 匀速     | 普通击退       |
| `Decelerate` | 减速击退     | 先快后慢 | 重击、爆炸     |
| `Launch`     | 击飞（预留） | 抛物线   | 上挑、击飞技能 |

### 3.2 击退计算

#### 线性击退

```
速度 = 总距离 / 持续时间
每帧位移 = 速度 × deltaTime
```

#### 减速击退

```
进度 = 已用时间 / 总时间
当前速度 = 初始速度 × (1 - 进度)
每帧位移 = 当前速度 × deltaTime
```

### 3.3 击退中断

击退可被以下情况中断：

1. 受到新的击退效果（覆盖）
2. 实体死亡
3. 碰撞到障碍物（预留，需要物理碰撞支持）
4. 击退抵抗/霸体状态（预留）

---

## 受击系统

### 4.1 受击流程

```
接收 SkillEffectEvent
    ↓
读取效果配置
    ↓
根据效果类型分发
    ↓
├─ 伤害 → 播放受击动作 + 特效
├─ 治疗 → 播放治疗特效
├─ 击退 → 受击动作 + 写入击退数据
├─ Buff → 添加Buff组件
└─ Debuff → 添加Debuff组件
```

### 4.2 受击动作优先级

| 优先级 | 类型            | 说明             |
| ------ | --------------- | ---------------- |
| 高     | 死亡、击退      | 强制打断当前动作 |
| 中     | 受击、硬直      | 可打断普通动作   |
| 低     | Buff/Debuff特效 | 不打断动作       |

### 4.3 受击方向判定

根据施法者和目标的相对位置：

- **正面受击**：0° - 45°
- **侧面受击**：45° - 135°
- **背面受击**：135° - 180°

可播放不同的受击动作。

---

## 数据流程

### 5.1 完整流程图

```
[SkillExecutorCapability]
    技能触发 → 碰撞检测 → 命中目标
    ↓
SkillEffectSystem.QueueSkillEffect(caster, target, effectId)
    ↓
[EntityEventQueue]
    事件入队：SkillEffectEvent
    ↓
[HitReactionCapability.Tick]
    消费事件 → 查询效果配置
    ↓
    ├─ 效果类型 = 击退？
    │   ├─ 播放受击动作
    │   ├─ 播放受击特效
    │   └─ 写入 KnockbackComponent
    │       ├─ Direction: 从施法者指向目标
    │       ├─ Distance: config.EffectValue
    │       ├─ Duration: config.EffectDuration
    │       └─ Speed: Distance / Duration
    ↓
[KnockbackCapability.Tick]
    检查 KnockbackComponent.IsKnockingBack
    ↓
    计算本帧位移 = Direction × Speed × deltaTime
    ↓
    应用到 PositionComponent.Position
    ↓
    更新 RemainingTime 和 MovedDistance
    ↓
    检查是否结束 → 清除击退状态
```

### 5.2 关键时序

| 帧        | 操作                       | 组件状态                          |
| --------- | -------------------------- | --------------------------------- |
| N         | 技能触发，碰撞检测         | -                                 |
| N         | QueueSkillEffect           | 事件入队                          |
| N+1       | HitReactionCapability.Tick | 消费事件，写入 KnockbackComponent |
| N+1       | KnockbackCapability.Tick   | 开始击退，应用第一帧位移          |
| N+2 ~ N+M | KnockbackCapability.Tick   | 持续应用位移                      |
| N+M       | 击退结束                   | IsKnockingBack = false            |

---

## 配置设计

### 6.1 技能效果表（SkillEffectTable）

击退效果配置示例：

```csv
skillEffectId,effectType,effectValue,effectDuration,targetType,effectRange,description
5001,3,5.0,0.3,1,0,轻击退：5米，0.3秒
5002,3,10.0,0.5,1,0,重击退：10米，0.5秒
5003,3,3.0,0.2,1,0,小击退：3米，0.2秒
```

| 字段               | 说明     | 击退用途       |
| ------------------ | -------- | -------------- |
| `effectType`     | 效果类型 | 固定为 `3`   |
| `effectValue`    | 效果数值 | 击退距离（米） |
| `effectDuration` | 持续时间 | 击退时长（秒） |
| `targetType`     | 目标类型 | 1=敌人         |
| `effectRange`    | 范围     | 击退不使用     |

### 6.2 扩展配置（预留）

```csv
skillEffectId,knockbackType,knockbackCurve,canInterrupt,canBeResisted
5001,0,Linear,1,1
5002,1,Decelerate,1,0
```

| 字段               | 说明                             |
| ------------------ | -------------------------------- |
| `knockbackType`  | 击退类型：0=线性，1=减速，2=击飞 |
| `knockbackCurve` | 速度曲线名称（预留）             |
| `canInterrupt`   | 是否可打断当前动作               |
| `canBeResisted`  | 是否可被抵抗                     |

---

## 实现细节

### 7.1 优先级与标签

#### Capability 优先级

| Capability              | Priority | 说明                     |
| ----------------------- | -------- | ------------------------ |
| SkillExecutorCapability | 250      | 最先执行技能逻辑         |
| HitReactionCapability   | 200      | 处理受击反馈             |
| KnockbackCapability     | 150      | 应用击退位移             |
| MovementCapability      | 100      | 正常移动（会被击退覆盖） |

#### Capability 标签

**KnockbackCapability 标签**：

```csharp
Tags => new[] 
{ 
    CapabilityTag.Movement,  // 移动类
    CapabilityTag.Combat     // 战斗类
}
```

**HitReactionCapability 标签**：

```csharp
Tags => new[] 
{ 
    CapabilityTag.Combat,    // 战斗类
    CapabilityTag.Animation  // 动画类
}
```

#### 移动输入禁用机制

**设计原则**：
- 击退激活时，通过 `CapabilitySystem.DisableCapabilitiesByTag` 主动禁用 `UserInputMovement` 标签
- 击退结束时，通过 `CapabilitySystem.EnableCapabilitiesByTag` 恢复用户输入
- 与 `SkillDisplacementCapability` 使用相同的机制，保持架构一致

**实现方式**：

```csharp
// KnockbackCapability
private long _knockbackInstigatorId;

public override void OnActivate(Entity entity)
{
    base.OnActivate(entity);
    
    // 获取击退施法者ID作为标识
    var knockback = GetComponent<KnockbackComponent>(entity);
    _knockbackInstigatorId = knockback?.CasterId ?? entity.UniqueId;
    
    // 禁用用户输入位移
    entity.World?.CapabilitySystem?.DisableCapabilitiesByTag(
        entity, 
        CapabilityTag.UserInputMovement, 
        _knockbackInstigatorId, 
        "Knockback active"
    );
}

public override void OnDeactivate(Entity entity)
{
    // 恢复用户输入位移
    entity.World?.CapabilitySystem?.EnableCapabilitiesByTag(
        entity, 
        CapabilityTag.UserInputMovement, 
        _knockbackInstigatorId
    );
    
    base.OnDeactivate(entity);
}
```

**MovementCapability 现有实现**：

`MovementCapability` 已经通过检查 `UserInputMovement` 标签是否被禁用来决定是否处理移动输入：

```csharp
// MovementCapability.Tick (已实现)
public override void Tick(Entity entity)
{
    // ... 朝向更新 ...
    
    // 检查用户输入位移是否被禁用（由技能位移/击退系统禁用）
    bool isUserInputMovementDisabled = IsUserInputMovementDisabled(entity);
    
    // 处理移动（如果用户输入位移未被禁用）
    if (!isUserInputMovementDisabled && inputMagnitude > threshold && movementComponent.CanMove)
    {
        // 应用移动...
    }
}

private bool IsUserInputMovementDisabled(Entity entity)
{
    if (entity.DisabledTags == null)
        return false;
    
    if (!entity.DisabledTags.TryGetValue(CapabilityTag.UserInputMovement, out var instigators))
        return false;
    
    return instigators.Count > 0;
}
```

### 7.2 状态互斥

#### 击退与移动

击退期间，通过 Tag 系统自动禁用移动输入。

**设计思路**：

- `KnockbackCapability` 激活时，调用 `CapabilitySystem.DisableCapabilitiesByTag` 禁用 `UserInputMovement` 标签
- `MovementCapability` 在处理移动输入前，检查 `UserInputMovement` 标签是否被禁用
- 击退结束时，调用 `EnableCapabilitiesByTag` 恢复用户输入
- 与 `SkillDisplacementCapability` 使用相同机制，保持架构一致

**实现流程**：

1. **击退激活** → 禁用 `UserInputMovement`
2. **MovementCapability.Tick** → 检测到标签被禁用 → 跳过移动输入处理
3. **击退结束** → 恢复 `UserInputMovement`
4. **MovementCapability.Tick** → 标签已恢复 → 正常处理移动输入

**优势**：

- ✅ 与现有 `SkillDisplacementCapability` 机制一致，架构统一
- ✅ 无需修改 `MovementCapability`，已支持此机制
- ✅ 可扩展：硬直、眩晕等控制效果可复用同一机制
- ✅ 自动恢复：击退结束时自动恢复移动输入
- ✅ 支持多来源禁用：多个效果可同时禁用移动（通过不同 instigatorId）

#### 击退与技能

击退期间是否可以释放技能：

- **可以**：允许玩家使用位移技能逃脱
- **不可以**：完全硬直，更适合强控效果

根据策划需求配置。

---

## 附录

### A.1 相关数据结构

#### SkillEffectEvent

```csharp
namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 技能效果事件（全局事件队列使用）
    /// </summary>
    public struct SkillEffectEvent
    {
        /// <summary>施法者ID</summary>
        public long CasterId;
  
        /// <summary>目标ID</summary>
        public long TargetId;
  
        /// <summary>效果ID</summary>
        public int EffectId;
  
        /// <summary>触发时间（帧）</summary>
        public int TriggerFrame;
    }
}
```

#### SkillEffectConfig

```csharp
namespace Astrum.LogicCore.Configuration
{
    /// <summary>
    /// 技能效果配置（从表格生成）
    /// </summary>
    public class SkillEffectConfig
    {
        public int SkillEffectId { get; set; }
        public int EffectType { get; set; }      // 1=伤害, 2=治疗, 3=击退, 4=Buff, 5=Debuff
        public float EffectValue { get; set; }   // 伤害值/治疗量/击退距离
        public float EffectDuration { get; set; } // 持续时间/击退时长
        public int TargetType { get; set; }      // 1=敌人, 2=友军, 3=自身, 4=全体
        public float EffectRange { get; set; }   // 效果范围
        public string Description { get; set; }  // 描述
    }
}
```

## 版本历史

| 版本   | 日期       | 说明                                                         |
| ------ | ---------- | ------------------------------------------------------------ |
| v1.0   | 2025-01-08 | 初始设计                                                     |
| v1.1   | 2025-01-08 | 引入双模式事件系统，优化移动输入禁用机制                     |
| v1.2   | 2025-11-06 | 架构修订：职责分离优化                                       |
| v1.2.1 | 2025-11-06 | 完全封装：所有 Handler 都不修改组件，统一发送事件给 Capability |

---

## 架构变更说明（v1.2.1 完全封装版）

### 🔄 **核心变更**

#### **变更前（v1.0/v1.1）**：
```
SkillExecutorCapability → 发送 SkillEffectEvent → HitReactionCapability
                                                      ↓
                                        处理效果逻辑 + 修改组件 + 播放表现
```

**问题**：
- ❌ HitReactionCapability 职责过重（效果逻辑 + 表现）
- ❌ 外部 Capability 直接修改其他实体的组件
- ❌ 难以扩展和测试

#### **v1.2 中间版（部分改进）**：
```
SkillExecutorCapability → SkillEffectSystem
                              ↓
                        EffectHandler
                              ↓
                    ┌─────────┴──────────┐
                    ↓                    ↓
            直接修改组件          发送事件
         (伤害直接扣血)      (击退发事件)  ← 不一致！
```

**问题**：
- ❌ 伤害和击退处理方式不统一
- ❌ Handler 仍然可以修改组件（破坏封装）

#### **变更后（v1.2.1 完全封装版）**：
```
SkillExecutorCapability → SkillEffectSystem
                              ↓
                        EffectHandler（只读不写）
                              ↓
                         只发送事件
                              ↓
            ┌─────────────────┼─────────────────┐
            ↓                 ↓                 ↓
      DamageEvent      KnockbackEvent   HitReactionEvent
            ↓                 ↓                 ↓
   DamageCapability   KnockbackCapability  HitReactionCapability
            ↓                 ↓                 ↓
       修改自身组件      修改自身组件         播放表现
```

**优势**：
- ✅ **完全封装**：Handler 完全不修改组件
- ✅ **统一原则**：所有效果都通过事件，无例外
- ✅ **职责清晰**：Handler 纯计算，Capability 纯执行
- ✅ **安全性**：防止跨实体的组件修改

### 📋 **新增事件**

1. **`DamageEvent`**（v1.2.1 新增）
   - 由 `DamageEffectHandler` 发送
   - 接收方：`DamageCapability`
   - 包含：伤害值、是否暴击、伤害类型

2. **`KnockbackEvent`**
   - 由 `KnockbackEffectHandler` 发送
   - 接收方：`KnockbackCapability`
   - 包含：方向、距离、持续时间、类型

3. **`HitReactionEvent`**
   - 由所有 EffectHandler 发送
   - 接收方：`HitReactionCapability`
   - 包含：受击方向、效果类型、是否硬直

### 🔧 **职责重新分配**

#### EffectHandler（只读外部，只发送事件）：
| Handler                  | 读取数据      | 修改数据 | 发送事件                         |
| ------------------------ | ------------- | -------- | -------------------------------- |
| `DamageEffectHandler`    | Stats 组件    | ❌ 不修改 | DamageEvent, HitReactionEvent    |
| `KnockbackEffectHandler` | Trans 组件    | ❌ 不修改 | KnockbackEvent, HitReactionEvent |
| `HealEffectHandler`      | Stats 组件    | ❌ 不修改 | HealEvent, HitReactionEvent      |

> **核心原则**：**Handler 只读不写，所有组件修改都由实体自身的 Capability 完成**

#### Capability（接收事件，修改自身组件）：
| Capability              | 接收事件         | 修改组件            | Tick 职责        |
| ----------------------- | ---------------- | ------------------- | ---------------- |
| `DamageCapability`      | DamageEvent      | DynamicStats（扣血） | 检查死亡状态     |
| `HealCapability`        | HealEvent        | DynamicStats（加血） | -                |
| `HitReactionCapability` | HitReactionEvent | ❌ 不修改           | 播放受击表现     |
| `KnockbackCapability`   | KnockbackEvent   | KnockbackComponent  | 应用击退位移     |

> **核心原则**：**Capability 只能修改自身实体的组件，不能修改其他实体**
