# 事件队列系统设计

**版本**: v1.0  
**创建日期**: 2025-01-08  
**状态**: 设计中  

> 📖 **相关文档**：
> - [受击与击退](../../02-CombatSystem%20战斗系统/技能效果/Hit-Reaction-And-Knockback%20受击与击退.md) - 事件消费示例
> - [ECC系统](../ECC/ECC-System%20ECC结构说明.md) - Entity-Component-Capability架构
> - [Capability优化重构方案](../ECC/Capability-Optimization-Proposal%20Capability优化重构方案.md)

---

## 📋 目录

1. [系统概述](#系统概述)
2. [设计动机](#设计动机)
3. [架构设计](#架构设计)
4. [事件队列实现](#事件队列实现)
5. [使用方式](#使用方式)
6. [性能优化](#性能优化)
7. [迁移指南](#迁移指南)

---

## 系统概述

### 1.1 设计目标

当前系统中，实体通过 `EventSystem.Subscribe/Unsubscribe` 来监听事件。这种方式存在以下问题：

1. **订阅管理复杂**：需要在合适的时机订阅和取消订阅
2. **生命周期耦合**：实体销毁时容易忘记取消订阅，导致内存泄漏
3. **性能开销**：每次事件触发都需要遍历所有订阅者
4. **实体主动性过强**：实体需要知道哪些事件需要监听

**新设计**：引入**全局事件队列**，实体不再主动监听事件，而是在 Capability 更新时**主动消费**自己关心的事件。

### 1.2 核心思想

```
旧模式（推送模式）：
事件发布者 --推送--> EventSystem --通知--> 订阅者

新模式（拉取模式）：
事件发布者 --入队--> EntityEventQueue <--拉取-- Capability
```

**优点**：
- ✅ 无需管理订阅/取消订阅
- ✅ 生命周期由 Capability 自动管理
- ✅ 只处理自己需要的事件
- ✅ 更好的性能（批量处理）
- ✅ 更清晰的职责分离

---

## 设计动机

### 2.1 现有问题

#### 问题 1：订阅管理复杂

```csharp
// 当前代码（需要手动管理订阅）
public class SomeCapability : Capability
{
    public override void OnActivated(Entity entity)
    {
        // 必须记得订阅
        EventSystem.Instance.Subscribe<DamageEvent>(OnDamageEvent);
    }
    
    public override void OnDeactivated(Entity entity)
    {
        // 必须记得取消订阅，否则内存泄漏
        EventSystem.Instance.Unsubscribe<DamageEvent>(OnDamageEvent);
    }
    
    private void OnDamageEvent(DamageEvent evt)
    {
        // 需要检查事件是否是针对自己的
        if (evt.TargetId != Owner.UniqueId)
            return;
        
        // 处理事件
    }
}
```

**问题**：
- 容易忘记取消订阅
- 需要在回调中过滤事件
- 订阅/取消的时机不好控制

#### 问题 2：生命周期耦合

```csharp
// 实体销毁时，如果忘记取消订阅
entity.Destroy();

// EventSystem 中仍然持有引用，导致：
// 1. 内存泄漏
// 2. 可能触发空引用异常
```

#### 问题 3：性能开销

```csharp
// 当前 EventSystem.Publish 实现
public void Publish<T>(T eventData)
{
    // 遍历所有订阅者
    foreach (var subscriber in _subscribers)
    {
        subscriber.Invoke(eventData); // 即使不是目标实体也会触发
    }
}
```

**问题**：
- 100 个订阅者，即使只有 1 个是目标，也要调用 100 次
- 每次调用都需要判断 `if (evt.TargetId != MyId)`

### 2.2 新设计优势

```csharp
// 新设计（无需订阅）
public class HitReactionCapability : Capability
{
    public override void Tick(Entity entity)
    {
        // 直接从队列中拉取针对自己的事件
        var events = entity.World.EntityEventQueue.ConsumeEvents<SkillEffectEvent>(entity.UniqueId);
        
        // 批量处理
        foreach (var evt in events)
        {
            ProcessSkillEffect(entity, evt);
        }
    }
}
```

**优点**：
- ✅ 不需要订阅/取消订阅
- ✅ Capability 销毁时自动停止消费
- ✅ 只获取自己关心的事件，性能更好
- ✅ 代码更简洁，职责更清晰

---

## 架构设计

### 3.1 整体架构

```
┌─────────────────────────────────────────┐
│         EventSystem (View层)            │
│  (保留用于View层组件间通信)              │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│      EntityEventQueue (Logic层)         │
│                                          │
│  ┌────────────────────────────────┐    │
│  │  Dictionary<long, EventQueue>  │    │
│  │  EntityId -> 该实体的事件队列    │    │
│  └────────────────────────────────┘    │
│                                          │
│  入队：QueueEvent(targetId, event)      │
│  出队：ConsumeEvents<T>(targetId)        │
│  清理：ClearEvents(targetId)             │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│           World                          │
│  - EntityEventQueue                      │
│  - Entities                              │
│  - Systems                               │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│         Capability                       │
│  - Tick() 中主动消费事件                  │
└─────────────────────────────────────────┘
```

### 3.2 分层职责

| 层级 | 事件系统 | 用途 |
|------|----------|------|
| **View层** | `EventSystem` | View组件间通信（UI、特效、音效） |
| **Logic层** | `EntityEventQueue` | 实体间游戏逻辑事件 |

**分离原因**：
- View层事件：立即响应，无需排队（如UI按钮点击）
- Logic层事件：需要排队，确保顺序和一致性（如伤害、治疗）

---

## 事件队列实现

### 4.1 核心数据结构

#### EntityEventQueue

```csharp
namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 全局实体事件队列
    /// 存储针对特定实体的事件，由 Capability 主动消费
    /// </summary>
    public class EntityEventQueue
    {
        // 每个实体有自己的事件队列
        private readonly Dictionary<long, EntityEventList> _entityQueues = new();
        
        // 对象池，减少GC
        private readonly Queue<EntityEventList> _eventListPool = new();
        
        /// <summary>
        /// 将事件加入目标实体的队列
        /// </summary>
        public void QueueEvent<T>(long targetEntityId, T eventData) where T : struct
        {
            if (!_entityQueues.TryGetValue(targetEntityId, out var queue))
            {
                queue = GetOrCreateEventList();
                _entityQueues[targetEntityId] = queue;
            }
            
            queue.Add(eventData);
        }
        
        /// <summary>
        /// 消费指定类型的事件（批量获取并清除）
        /// </summary>
        public List<T> ConsumeEvents<T>(long targetEntityId) where T : struct
        {
            if (!_entityQueues.TryGetValue(targetEntityId, out var queue))
                return EmptyList<T>();
            
            var result = queue.GetEvents<T>();
            
            // 如果该实体的事件队列为空，回收队列对象
            if (queue.IsEmpty)
            {
                _entityQueues.Remove(targetEntityId);
                RecycleEventList(queue);
            }
            
            return result;
        }
        
        /// <summary>
        /// 清除指定实体的所有事件（实体销毁时调用）
        /// </summary>
        public void ClearEvents(long targetEntityId)
        {
            if (_entityQueues.TryGetValue(targetEntityId, out var queue))
            {
                _entityQueues.Remove(targetEntityId);
                RecycleEventList(queue);
            }
        }
        
        /// <summary>
        /// 清除所有事件（World重置时调用）
        /// </summary>
        public void ClearAll()
        {
            foreach (var queue in _entityQueues.Values)
            {
                RecycleEventList(queue);
            }
            _entityQueues.Clear();
        }
        
        private EntityEventList GetOrCreateEventList()
        {
            if (_eventListPool.Count > 0)
                return _eventListPool.Dequeue();
            
            return new EntityEventList();
        }
        
        private void RecycleEventList(EntityEventList list)
        {
            list.Clear();
            _eventListPool.Enqueue(list);
        }
        
        private static List<T> EmptyList<T>() => new List<T>(0);
    }
}
```

#### EntityEventList

```csharp
namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 单个实体的事件列表（支持多种事件类型）
    /// </summary>
    internal class EntityEventList
    {
        // 按类型存储事件
        private readonly Dictionary<Type, IList> _eventsByType = new();
        
        public bool IsEmpty => _eventsByType.Count == 0;
        
        public void Add<T>(T eventData) where T : struct
        {
            var type = typeof(T);
            
            if (!_eventsByType.TryGetValue(type, out var list))
            {
                list = new List<T>();
                _eventsByType[type] = list;
            }
            
            ((List<T>)list).Add(eventData);
        }
        
        public List<T> GetEvents<T>() where T : struct
        {
            var type = typeof(T);
            
            if (_eventsByType.TryGetValue(type, out var list))
            {
                var result = new List<T>((List<T>)list);
                _eventsByType.Remove(type); // 消费后移除
                return result;
            }
            
            return new List<T>(0);
        }
        
        public void Clear()
        {
            _eventsByType.Clear();
        }
    }
}
```

### 4.2 集成到 World

```csharp
namespace Astrum.LogicCore.Core
{
    public class World
    {
        // 全局事件队列
        public EntityEventQueue EntityEventQueue { get; private set; }
        
        // 其他系统...
        public HitSystem HitSystem { get; private set; }
        public SkillEffectSystem SkillEffectSystem { get; private set; }
        
        public World(string name)
        {
            EntityEventQueue = new EntityEventQueue();
            // ...
        }
        
        public void OnEntityDestroyed(Entity entity)
        {
            // 清除该实体的所有待处理事件
            EntityEventQueue.ClearEvents(entity.UniqueId);
        }
        
        public void Reset()
        {
            // 清除所有事件
            EntityEventQueue.ClearAll();
            // ...
        }
    }
}
```

---

## 使用方式

### 5.1 发布事件

#### SkillEffectSystem（发布者）

```csharp
namespace Astrum.LogicCore.Systems
{
    public class SkillEffectSystem
    {
        private World _world;
        
        public void QueueSkillEffect(SkillEffectData data)
        {
            // 构造事件
            var evt = new SkillEffectEvent
            {
                CasterId = data.CasterId,
                TargetId = data.TargetId,
                EffectId = data.EffectId,
                TriggerFrame = _world.CurrentFrame
            };
            
            // 加入目标实体的事件队列
            _world.EntityEventQueue.QueueEvent(data.TargetId, evt);
            
            ASLogger.Instance.Debug($"Queued SkillEffect event: {data.CasterId} → {data.TargetId}, effectId={data.EffectId}");
        }
    }
}
```

#### 其他发布示例

```csharp
// 伤害事件
_world.EntityEventQueue.QueueEvent(targetId, new DamageEvent
{
    AttackerId = attackerId,
    Damage = 100
});

// Buff事件
_world.EntityEventQueue.QueueEvent(targetId, new BuffEvent
{
    BuffId = 2001,
    Duration = 10.0f
});
```

### 5.2 消费事件

#### HitReactionCapability（消费者）

```csharp
public class HitReactionCapability : Capability<HitReactionCapability>
{
    public override void Tick(Entity entity)
    {
        // 从队列中获取针对该实体的技能效果事件
        var events = entity.World.EntityEventQueue.ConsumeEvents<SkillEffectEvent>(entity.UniqueId);
        
        // 批量处理
        foreach (var evt in events)
        {
            ProcessSkillEffect(entity, evt);
        }
    }
    
    private void ProcessSkillEffect(Entity entity, SkillEffectEvent evt)
    {
        // 获取效果配置
        var config = GetEffectConfig(evt.EffectId);
        
        // 根据效果类型处理
        switch (config.EffectType)
        {
            case 1: ProcessDamage(entity, evt, config); break;
            case 2: ProcessHeal(entity, evt, config); break;
            case 3: ProcessKnockback(entity, evt, config); break;
            // ...
        }
    }
}
```

#### 消费多种事件

```csharp
public class CombatCapability : Capability<CombatCapability>
{
    public override void Tick(Entity entity)
    {
        // 消费伤害事件
        var damageEvents = entity.World.EntityEventQueue.ConsumeEvents<DamageEvent>(entity.UniqueId);
        foreach (var evt in damageEvents)
        {
            ApplyDamage(entity, evt);
        }
        
        // 消费治疗事件
        var healEvents = entity.World.EntityEventQueue.ConsumeEvents<HealEvent>(entity.UniqueId);
        foreach (var evt in healEvents)
        {
            ApplyHeal(entity, evt);
        }
        
        // 消费Buff事件
        var buffEvents = entity.World.EntityEventQueue.ConsumeEvents<BuffEvent>(entity.UniqueId);
        foreach (var evt in buffEvents)
        {
            AddBuff(entity, evt);
        }
    }
}
```

### 5.3 事件定义

#### 技能效果事件

```csharp
namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 技能效果事件
    /// </summary>
    public struct SkillEffectEvent
    {
        /// <summary>施法者ID</summary>
        public long CasterId;
        
        /// <summary>目标ID</summary>
        public long TargetId;
        
        /// <summary>效果ID</summary>
        public int EffectId;
        
        /// <summary>触发帧</summary>
        public int TriggerFrame;
    }
}
```

#### 伤害事件

```csharp
public struct DamageEvent
{
    public long AttackerId;
    public long TargetId;
    public float Damage;
    public int DamageType; // 1=物理, 2=魔法
}
```

#### Buff事件

```csharp
public struct BuffEvent
{
    public long CasterId;
    public long TargetId;
    public int BuffId;
    public float Duration;
}
```

---

## 性能优化

### 6.1 对象池

#### 事件对象池

```csharp
public class EntityEventQueue
{
    // List<T> 对象池
    private readonly Dictionary<Type, Queue<IList>> _listPools = new();
    
    private List<T> GetList<T>()
    {
        var type = typeof(T);
        if (_listPools.TryGetValue(type, out var pool) && pool.Count > 0)
        {
            return (List<T>)pool.Dequeue();
        }
        return new List<T>();
    }
    
    private void RecycleList<T>(List<T> list)
    {
        list.Clear();
        var type = typeof(T);
        if (!_listPools.TryGetValue(type, out var pool))
        {
            pool = new Queue<IList>();
            _listPools[type] = pool;
        }
        pool.Enqueue(list);
    }
}
```

### 6.2 批量处理

```csharp
// 单帧产生100个伤害事件
for (int i = 0; i < 100; i++)
{
    _world.EntityEventQueue.QueueEvent(targetId, damageEvents[i]);
}

// Capability 批量消费
var events = _world.EntityEventQueue.ConsumeEvents<DamageEvent>(entity.UniqueId);
// 一次性处理100个事件
ProcessDamagesBatch(events);
```

### 6.3 内存优化

#### 空队列自动回收

```csharp
public List<T> ConsumeEvents<T>(long targetEntityId) where T : struct
{
    if (!_entityQueues.TryGetValue(targetEntityId, out var queue))
        return EmptyList<T>();
    
    var result = queue.GetEvents<T>();
    
    // 如果队列为空，移除并回收
    if (queue.IsEmpty)
    {
        _entityQueues.Remove(targetEntityId);
        RecycleEventList(queue);
    }
    
    return result;
}
```

### 6.4 性能对比

| 指标 | 旧设计（EventSystem） | 新设计（EntityEventQueue） |
|------|----------------------|---------------------------|
| 订阅管理 | 需要手动订阅/取消 | 无需订阅 |
| 事件过滤 | 每个订阅者都触发，需自行过滤 | 直接获取目标事件 |
| 内存泄漏风险 | 高（忘记取消订阅） | 低（自动清理） |
| GC压力 | 中（委托分配） | 低（对象池） |
| 批量处理 | 不支持 | 支持 |

---

## 迁移指南

### 7.1 从 EventSystem 迁移

#### 旧代码（基于 EventSystem）

```csharp
public class OldDamageCapability : Capability
{
    public override void OnActivated(Entity entity)
    {
        EventSystem.Instance.Subscribe<DamageEvent>(OnDamageEvent);
    }
    
    public override void OnDeactivated(Entity entity)
    {
        EventSystem.Instance.Unsubscribe<DamageEvent>(OnDamageEvent);
    }
    
    private void OnDamageEvent(DamageEvent evt)
    {
        if (evt.TargetId != Owner.UniqueId)
            return;
        
        ApplyDamage(evt.Damage);
    }
}
```

#### 新代码（基于 EntityEventQueue）

```csharp
public class NewDamageCapability : Capability<NewDamageCapability>
{
    public override void Tick(Entity entity)
    {
        // 获取针对该实体的伤害事件
        var events = entity.World.EntityEventQueue.ConsumeEvents<DamageEvent>(entity.UniqueId);
        
        foreach (var evt in events)
        {
            ApplyDamage(evt.Damage);
        }
    }
}
```

### 7.2 迁移步骤

1. **识别订阅的事件类型**
   - 找出 `Subscribe<T>` 的所有位置
   - 确认事件结构体定义

2. **修改事件定义**
   - 确保事件是 `struct`（值类型）
   - 添加 `TargetId` 字段（如果还没有）

3. **移除订阅/取消逻辑**
   - 删除 `Subscribe/Unsubscribe` 调用
   - 删除 `OnActivated/OnDeactivated` 中的订阅代码

4. **在 Tick 中消费事件**
   - 使用 `ConsumeEvents<T>` 获取事件
   - 批量处理事件列表

5. **修改事件发布**
   - 将 `EventSystem.Publish` 改为 `EntityEventQueue.QueueEvent`
   - 指定目标实体ID

### 7.3 兼容性考虑

#### View层事件保留 EventSystem

```csharp
// View层事件仍然使用 EventSystem（立即响应）
EventSystem.Instance.Publish(new UIButtonClickEvent { ButtonId = 123 });
EventSystem.Instance.Publish(new PlaySoundEvent { SoundId = "sword_hit" });
EventSystem.Instance.Publish(new VFXTriggerEvent { EffectPath = "Effects/Explosion" });
```

#### Logic层事件使用 EntityEventQueue

```csharp
// Logic层事件使用 EntityEventQueue（排队处理）
_world.EntityEventQueue.QueueEvent(targetId, new DamageEvent { ... });
_world.EntityEventQueue.QueueEvent(targetId, new BuffEvent { ... });
_world.EntityEventQueue.QueueEvent(targetId, new KnockbackEvent { ... });
```

---

## 附录

### A.1 完整示例

#### 发布-消费完整流程

```csharp
// ========== 发布者：SkillExecutorCapability ==========
public class SkillExecutorCapability : Capability<SkillExecutorCapability>
{
    public override void Tick(Entity entity)
    {
        // ... 技能逻辑 ...
        
        // 碰撞检测命中目标
        var targets = HitSystem.QueryHits(caster, collisionShape);
        
        foreach (var target in targets)
        {
            // 发布技能效果事件到目标实体的队列
            entity.World.EntityEventQueue.QueueEvent(target.UniqueId, new SkillEffectEvent
            {
                CasterId = entity.UniqueId,
                TargetId = target.UniqueId,
                EffectId = 5001, // 击退效果
                TriggerFrame = entity.World.CurrentFrame
            });
        }
    }
}

// ========== 消费者：HitReactionCapability ==========
public class HitReactionCapability : Capability<HitReactionCapability>
{
    public override void Tick(Entity entity)
    {
        // 消费技能效果事件
        var events = entity.World.EntityEventQueue.ConsumeEvents<SkillEffectEvent>(entity.UniqueId);
        
        foreach (var evt in events)
        {
            // 获取效果配置
            var config = TableConfig.Instance.Tables.TbSkillEffectTable.GetOrDefault(evt.EffectId);
            
            if (config.EffectType == 3) // 击退
            {
                // 播放受击动作
                PlayHitAnimation(entity, evt.CasterId);
                
                // 写入击退数据
                var knockback = entity.GetOrAddComponent<KnockbackComponent>();
                knockback.IsKnockingBack = true;
                knockback.Distance = config.EffectValue;
                knockback.Duration = config.EffectDuration;
                // ...
            }
        }
    }
}

// ========== 消费者：KnockbackCapability ==========
public class KnockbackCapability : Capability<KnockbackCapability>
{
    public override void Tick(Entity entity)
    {
        var knockback = entity.GetComponent<KnockbackComponent>();
        if (knockback == null || !knockback.IsKnockingBack)
            return;
        
        // 应用击退位移
        var position = entity.GetComponent<PositionComponent>();
        position.Position += knockback.Direction * knockback.Speed * entity.World.DeltaTime;
        
        // 更新剩余时间
        knockback.RemainingTime -= entity.World.DeltaTime;
        if (knockback.RemainingTime <= 0)
        {
            knockback.IsKnockingBack = false;
        }
    }
}
```

### A.2 调试工具

#### 事件队列监视器

```csharp
public class EventQueueDebugger
{
    public static void PrintQueueStatus(EntityEventQueue queue)
    {
        var stats = queue.GetStatistics();
        
        Debug.Log($"=== Event Queue Statistics ===");
        Debug.Log($"Total Entities: {stats.TotalEntities}");
        Debug.Log($"Total Events: {stats.TotalEvents}");
        Debug.Log($"Event Types: {string.Join(", ", stats.EventTypes)}");
        
        foreach (var (entityId, count) in stats.EventsPerEntity)
        {
            Debug.Log($"  Entity {entityId}: {count} events");
        }
    }
}
```

---

## 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| v1.0 | 2025-01-08 | 初始设计 |


