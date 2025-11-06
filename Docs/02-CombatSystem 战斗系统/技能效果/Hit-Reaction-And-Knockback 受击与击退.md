# 受击与击退效果技术设计

**版本**: v1.0  
**创建日期**: 2025-01-08  
**状态**: 设计中  

> 📖 **相关文档**：
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

### 1.3 核心流程

```
技能触发 → 碰撞检测 → 应用技能效果
    ↓
SkillEffectSystem.QueueSkillEffect
    ↓
全局事件队列：EntityEventQueue
    ↓
HitReactionCapability.Tick
    ↓ (消费事件)
    ├─→ 播放受击动作
    ├─→ 播放受击特效/音效
    └─→ 写入 KnockbackComponent
           ↓
    KnockbackCapability.Tick
           ↓
    应用击退位移
```

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

### 2.2 Capability 设计

#### KnockbackCapability

负责处理击退位移逻辑。

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
            CapabilityTag.Combat 
        };
        
        public override bool ShouldActivate(Entity entity)
        {
            return base.ShouldActivate(entity) &&
                   HasComponent<KnockbackComponent>(entity) &&
                   HasComponent<PositionComponent>(entity);
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
        
        public override void Tick(Entity entity)
        {
            // 从全局事件队列消费针对该实体的事件
            var eventQueue = entity.World?.EntityEventQueue;
            if (eventQueue == null)
                return;
            
            // 获取并处理针对该实体的技能效果事件
            var events = eventQueue.ConsumeEvents<SkillEffectEvent>(entity.UniqueId);
            foreach (var evt in events)
            {
                ProcessSkillEffect(entity, evt);
            }
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

| 类型 | 说明 | 速度曲线 | 适用场景 |
|------|------|----------|----------|
| `Linear` | 线性击退 | 匀速 | 普通击退 |
| `Decelerate` | 减速击退 | 先快后慢 | 重击、爆炸 |
| `Launch` | 击飞（预留） | 抛物线 | 上挑、击飞技能 |

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

| 优先级 | 类型 | 说明 |
|--------|------|------|
| 高 | 死亡、击退 | 强制打断当前动作 |
| 中 | 受击、硬直 | 可打断普通动作 |
| 低 | Buff/Debuff特效 | 不打断动作 |

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

| 帧 | 操作 | 组件状态 |
|----|------|----------|
| N | 技能触发，碰撞检测 | - |
| N | QueueSkillEffect | 事件入队 |
| N+1 | HitReactionCapability.Tick | 消费事件，写入 KnockbackComponent |
| N+1 | KnockbackCapability.Tick | 开始击退，应用第一帧位移 |
| N+2 ~ N+M | KnockbackCapability.Tick | 持续应用位移 |
| N+M | 击退结束 | IsKnockingBack = false |

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

| 字段 | 说明 | 击退用途 |
|------|------|----------|
| `effectType` | 效果类型 | 固定为 `3` |
| `effectValue` | 效果数值 | 击退距离（米） |
| `effectDuration` | 持续时间 | 击退时长（秒） |
| `targetType` | 目标类型 | 1=敌人 |
| `effectRange` | 范围 | 击退不使用 |

### 6.2 扩展配置（预留）

```csv
skillEffectId,knockbackType,knockbackCurve,canInterrupt,canBeResisted
5001,0,Linear,1,1
5002,1,Decelerate,1,0
```

| 字段 | 说明 |
|------|------|
| `knockbackType` | 击退类型：0=线性，1=减速，2=击飞 |
| `knockbackCurve` | 速度曲线名称（预留） |
| `canInterrupt` | 是否可打断当前动作 |
| `canBeResisted` | 是否可被抵抗 |

---

## 实现细节

### 7.1 优先级设定

| Capability | Priority | 说明 |
|------------|----------|------|
| SkillExecutorCapability | 250 | 最先执行技能逻辑 |
| HitReactionCapability | 200 | 处理受击反馈 |
| KnockbackCapability | 150 | 应用击退位移 |
| MovementCapability | 100 | 正常移动（会被击退覆盖） |

### 7.2 状态互斥

#### 击退与移动

击退期间，`MovementCapability` 应该被禁用或忽略输入：

```csharp
// MovementCapability.Tick
var knockback = entity.GetComponent<KnockbackComponent>();
if (knockback != null && knockback.IsKnockingBack)
{
    // 击退中，禁用移动输入
    return;
}
```

#### 击退与技能

击退期间是否可以释放技能：
- **可以**：允许玩家使用位移技能逃脱
- **不可以**：完全硬直，更适合强控效果

根据策划需求配置。

### 7.3 网络同步

#### 客户端预测
- 客户端接收到击退事件时，立即应用击退（预测）
- 服务器计算权威击退轨迹
- 客户端收到服务器位置更新时，进行插值修正

#### 同步数据
- 击退起始位置
- 击退方向
- 击退速度
- 击退持续时间

### 7.4 性能优化

#### 批处理
- 同一帧多个目标的击退事件，批量处理
- 避免重复查询配置表

#### 对象池
- KnockbackComponent 复用
- SkillEffectEvent 复用

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

### A.2 测试场景

#### 测试用例 1：基础击退

1. 角色A释放击退技能
2. 命中角色B
3. 角色B播放受击动作
4. 角色B向后移动 5 米，耗时 0.3 秒
5. 击退结束，角色B恢复正常

#### 测试用例 2：击退打断

1. 角色B正在移动
2. 受到击退
3. 移动输入被忽略，执行击退
4. 击退结束后，恢复移动控制

#### 测试用例 3：连续击退

1. 角色B受到第一次击退（5米，0.3秒）
2. 击退进行到一半
3. 受到第二次击退（10米，0.5秒）
4. 第一次击退被覆盖，执行第二次击退

---

## 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| v1.0 | 2025-01-08 | 初始设计 |


