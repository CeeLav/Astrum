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

**双轨制事件系统**：

1. **EventSystem（绑定式，保留）**：用于 View 层和系统级通信
   - UI 交互、特效播放、音效触发
   - 需要立即响应，无需排队

2. **EntityEventQueue（队列式，新增）**：用于 Logic 层实体间通信
   - 伤害、治疗、击退、Buff/Debuff
   - 需要排队处理，确保顺序和一致性
   - **静态声明处理函数，预处理，专门的事件处理循环**

### 1.2 核心思想

```
【EventSystem - 绑定式（保留）】
View组件 --发布--> EventSystem --立即回调--> 订阅者

【EntityEventQueue - 队列式（新增）】
系统/实体 --入队--> EntityEventQueue
           ↓
      CapabilitySystem.ProcessEntityEvents()
           ↓
      根据事件类型 → 查找预处理的回调映射 → 调用 Capability 处理函数
```

**新模式的核心特性**：
1. **静态声明**：Capability 类中声明自己处理的事件类型和处理函数
2. **预处理**：CapabilitySystem 注册时提取并缓存事件映射
3. **集中调度**：专门的事件处理循环统一分发事件
4. **自动生命周期**：Capability 销毁时自动停止接收事件

**优点**：
- ✅ 声明式设计，一目了然
- ✅ 预处理避免运行时反射
- ✅ 集中调度，性能更优
- ✅ 无需管理订阅/取消订阅
- ✅ 生命周期自动管理

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
// 新设计（静态声明）
public class HitReactionCapability : Capability<HitReactionCapability>
{
    // 静态声明事件处理映射（类级别，只初始化一次）
    protected override void RegisterEventHandlers()
    {
        RegisterEventHandler<SkillEffectEvent>(OnSkillEffect);
        RegisterEventHandler<DamageEvent>(OnDamage);
    }
    
    // 事件处理函数（自动被 CapabilitySystem 调度）
    private void OnSkillEffect(Entity entity, SkillEffectEvent evt)
    {
        ProcessSkillEffect(entity, evt);
    }
    
    private void OnDamage(Entity entity, DamageEvent evt)
    {
        ProcessDamage(entity, evt);
    }
}

// CapabilitySystem 会在注册时预处理这些映射，然后在事件循环中自动调度
```

**优点**：
- ✅ 声明式，一眼看出 Capability 处理哪些事件
- ✅ 预处理，避免运行时开销
- ✅ 集中调度，统一管理
- ✅ 不需要订阅/取消订阅
- ✅ Capability 销毁时自动停止接收事件
- ✅ 代码更清晰，职责更明确

---

## 架构设计

### 3.1 整体架构

```
┌─────────────────────────────────────────┐
│         EventSystem (View层)            │
│      (绑定式，保留不变)                   │
│  Subscribe/Unsubscribe/Publish          │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│               CapabilitySystem (Logic层)                 │
│                                                           │
│  ┌────────────────────────────────────────────────┐    │
│  │  事件处理映射（预处理缓存）                      │    │
│  │  Dictionary<Type, List<CapabilityEventHandler>> │    │
│  │  EventType -> [处理该事件的Capability回调列表]   │    │
│  └────────────────────────────────────────────────┘    │
│                                                           │
│  注册：RegisterCapability() 时提取 EventHandlers        │
│  调度：ProcessEntityEvents() 遍历队列分发事件            │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│               EntityEventQueue (Logic层)                 │
│                                                           │
│  ┌──────────────────────────────────────────────┐      │
│  │  Dictionary<long, Queue<EntityEvent>>        │      │
│  │  EntityId -> 该实体的事件队列                  │      │
│  └──────────────────────────────────────────────┘      │
│                                                           │
│  入队：QueueEvent(targetId, eventType, eventData)        │
│  清理：Clear(targetId)                                   │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                  Capability (Logic层)                    │
│                                                           │
│  静态声明：RegisterEventHandlers()                        │
│  处理函数：OnXXXEvent(Entity entity, XXXEvent evt)       │
└─────────────────────────────────────────────────────────┘

【调度流程】
1. 系统/实体 → EntityEventQueue.QueueEvent(targetId, event)
2. World.Update() → CapabilitySystem.ProcessEntityEvents()
3. CapabilitySystem 遍历事件队列:
   - 获取事件类型和目标实体
   - 查找预处理的回调映射
   - 调用实体对应 Capability 的处理函数
```

### 3.2 分层职责

| 层级 | 事件系统 | 模式 | 用途 |
|------|----------|------|------|
| **View层** | `EventSystem` | 绑定式 | View组件间通信（UI、特效、音效） |
| **Logic层** | `EntityEventQueue` + `CapabilitySystem` | 队列式 | 实体间游戏逻辑事件（伤害、治疗、击退） |

**分离原因**：
- **View层事件**：立即响应，无需排队（如UI按钮点击、特效播放）
- **Logic层事件**：需要排队，集中调度，确保顺序和一致性（如伤害、治疗、击退）

---

## 事件队列实现

### 4.1 核心数据结构

#### EntityEvent（事件包装）

```csharp
namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 实体事件包装（统一的事件容器）
    /// </summary>
    public struct EntityEvent
    {
        public long TargetEntityId;  // 目标实体ID
        public Type EventType;        // 事件类型
        public object EventData;      // 事件数据（struct装箱）
        public int Frame;             // 触发帧（用于排序/调试）
    }
}
```

#### EntityEventQueue（全局队列）

```csharp
namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 全局实体事件队列（简化版，只负责存储）
    /// 事件的分发由 CapabilitySystem 处理
    /// </summary>
    public class EntityEventQueue
    {
        // 全局事件队列（按目标实体ID索引）
        private readonly Dictionary<long, Queue<EntityEvent>> _entityQueues = new();
        
        // 对象池，减少GC
        private readonly Queue<Queue<EntityEvent>> _queuePool = new();
        
        /// <summary>
        /// 将事件加入目标实体的队列
        /// </summary>
        public void QueueEvent<T>(long targetEntityId, T eventData) where T : struct
        {
            if (!_entityQueues.TryGetValue(targetEntityId, out var queue))
            {
                queue = GetOrCreateQueue();
                _entityQueues[targetEntityId] = queue;
            }
            
            queue.Enqueue(new EntityEvent
            {
                TargetEntityId = targetEntityId,
                EventType = typeof(T),
                EventData = eventData, // 装箱
                Frame = 0 // 可以从 World 获取当前帧
            });
        }
        
        /// <summary>
        /// 获取指定实体的所有事件（不消费，供 CapabilitySystem 调度）
        /// </summary>
        internal Queue<EntityEvent> GetEvents(long targetEntityId)
        {
            return _entityQueues.TryGetValue(targetEntityId, out var queue) ? queue : null;
        }
        
        /// <summary>
        /// 获取所有有事件的实体ID
        /// </summary>
        internal IEnumerable<long> GetEntityIdsWithEvents()
        {
            return _entityQueues.Keys;
        }
        
        /// <summary>
        /// 清除指定实体的所有事件（实体销毁时调用）
        /// </summary>
        public void Clear(long targetEntityId)
        {
            if (_entityQueues.TryGetValue(targetEntityId, out var queue))
            {
                _entityQueues.Remove(targetEntityId);
                RecycleQueue(queue);
            }
        }
        
        /// <summary>
        /// 清除所有事件（World重置时调用）
        /// </summary>
        public void ClearAll()
        {
            foreach (var queue in _entityQueues.Values)
            {
                RecycleQueue(queue);
            }
            _entityQueues.Clear();
        }
        
        private Queue<EntityEvent> GetOrCreateQueue()
        {
            if (_queuePool.Count > 0)
                return _queuePool.Dequeue();
            
            return new Queue<EntityEvent>(16);
        }
        
        private void RecycleQueue(Queue<EntityEvent> queue)
        {
            queue.Clear();
            _queuePool.Enqueue(queue);
        }
    }
}
```

### 4.2 Capability 事件处理声明

#### Capability 基类扩展

```csharp
namespace Astrum.LogicCore.Capabilities
{
    public abstract class Capability<T> : CapabilityBase where T : Capability<T>, new()
    {
        // 事件处理委托类型
        protected delegate void EntityEventHandler<TEvent>(Entity entity, TEvent evt) where TEvent : struct;
        
        // 存储注册的事件处理器
        private Dictionary<Type, Delegate> _eventHandlers;
        
        /// <summary>
        /// 注册事件处理函数（在子类中重写）
        /// </summary>
        protected virtual void RegisterEventHandlers() { }
        
        /// <summary>
        /// 注册单个事件处理器
        /// </summary>
        protected void RegisterEventHandler<TEvent>(EntityEventHandler<TEvent> handler) where TEvent : struct
        {
            if (_eventHandlers == null)
                _eventHandlers = new Dictionary<Type, Delegate>();
            
            _eventHandlers[typeof(TEvent)] = handler;
        }
        
        /// <summary>
        /// 获取所有注册的事件处理器（供 CapabilitySystem 使用）
        /// </summary>
        internal Dictionary<Type, Delegate> GetEventHandlers()
        {
            if (_eventHandlers == null)
            {
                RegisterEventHandlers(); // 延迟初始化
            }
            return _eventHandlers;
        }
    }
}
```

#### 使用示例：HitReactionCapability

```csharp
namespace Astrum.LogicCore.Capabilities
{
    public class HitReactionCapability : Capability<HitReactionCapability>
    {
        public override int Priority => 200;
        
        // 声明处理的事件
        protected override void RegisterEventHandlers()
        {
            RegisterEventHandler<SkillEffectEvent>(OnSkillEffect);
            RegisterEventHandler<DamageEvent>(OnDamage);
        }
        
        // 事件处理函数
        private void OnSkillEffect(Entity entity, SkillEffectEvent evt)
        {
            // 处理技能效果
            var effectConfig = GetEffectConfig(evt.EffectId);
            if (effectConfig == null)
                return;
            
            switch (effectConfig.EffectType)
            {
                case 3: // 击退
                    ProcessKnockback(entity, evt, effectConfig);
                    break;
                // ... 其他效果类型
            }
        }
        
        private void OnDamage(Entity entity, DamageEvent evt)
        {
            // 处理伤害
            PlayHitAction(entity, evt.CasterId);
            PlayHitVFX(entity, evt.CasterId);
        }
    }
}
```

### 4.3 CapabilitySystem 预处理和调度

```csharp
namespace Astrum.LogicCore.Core
{
    public class CapabilitySystem
    {
        // 事件处理映射（预处理缓存）
        // Key: (EventType, CapabilityType), Value: Handler
        private readonly Dictionary<(Type, Type), Delegate> _eventHandlerCache = new();
        
        // 快速查找：EventType -> List<(CapabilityType, Handler)>
        private readonly Dictionary<Type, List<(Type, Delegate)>> _eventToHandlers = new();
        
        private World _world;
        
        /// <summary>
        /// 注册 Capability 时预处理事件处理器
        /// </summary>
        public void RegisterCapability<T>(Entity entity) where T : Capability<T>, new()
        {
            var capability = new T();
            capability.OnAttached(entity);
            
            // 提取事件处理器
            var handlers = capability.GetEventHandlers();
            if (handlers != null && handlers.Count > 0)
            {
                var capType = typeof(T);
                foreach (var kvp in handlers)
                {
                    var eventType = kvp.Key;
                    var handler = kvp.Value;
                    
                    // 缓存到全局映射
                    _eventHandlerCache[(eventType, capType)] = handler;
                    
                    // 建立快速查找索引
                    if (!_eventToHandlers.TryGetValue(eventType, out var list))
                    {
                        list = new List<(Type, Delegate)>();
                        _eventToHandlers[eventType] = list;
                    }
                    list.Add((capType, handler));
                }
            }
            
            // ... 其他注册逻辑
        }
        
        /// <summary>
        /// 专门的事件处理循环（在 World.Update 中调用）
        /// </summary>
        public void ProcessEntityEvents()
        {
            var eventQueue = _world.EntityEventQueue;
            var entityIds = eventQueue.GetEntityIdsWithEvents();
            
            foreach (var entityId in entityIds)
            {
                var entity = _world.GetEntity(entityId);
                if (entity == null)
                {
                    eventQueue.Clear(entityId); // 实体不存在，清除事件
                    continue;
                }
                
                var events = eventQueue.GetEvents(entityId);
                if (events == null || events.Count == 0)
                    continue;
                
                // 处理该实体的所有事件
                while (events.Count > 0)
                {
                    var evt = events.Dequeue();
                    DispatchEvent(entity, evt);
                }
                
                // 清理空队列
                eventQueue.Clear(entityId);
            }
        }
        
        /// <summary>
        /// 分发单个事件到对应的 Capability
        /// </summary>
        private void DispatchEvent(Entity entity, EntityEvent evt)
        {
            // 查找处理该事件类型的所有 Capability
            if (!_eventToHandlers.TryGetValue(evt.EventType, out var handlers))
                return; // 没有 Capability 处理此事件
            
            foreach (var (capType, handler) in handlers)
            {
                // 检查实体是否有该 Capability 且激活
                if (!entity.HasCapability(capType))
                    continue;
                
                var capability = entity.GetCapability(capType);
                if (!capability.IsActive)
                    continue;
                
                // 调用处理函数（反射调用，或通过 dynamic）
                InvokeHandler(handler, entity, evt.EventData);
            }
        }
        
        /// <summary>
        /// 调用事件处理器
        /// </summary>
        private void InvokeHandler(Delegate handler, Entity entity, object eventData)
        {
            try
            {
                handler.DynamicInvoke(entity, eventData); // 拆箱
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Event handler invocation failed: {ex}");
            }
        }
    }
}
```

### 4.4 集成到 World

```csharp
namespace Astrum.LogicCore.Core
{
    public class World
    {
        // 全局事件队列
        public EntityEventQueue EntityEventQueue { get; private set; }
        
        // Capability 系统
        public CapabilitySystem CapabilitySystem { get; private set; }
        
        // 其他系统...
        public HitSystem HitSystem { get; private set; }
        
        public World(string name)
        {
            EntityEventQueue = new EntityEventQueue();
            CapabilitySystem = new CapabilitySystem(this);
            // ...
        }
        
        public void Update(float deltaTime)
        {
            // 1. 更新所有 Capability（可能会产生新事件）
            CapabilitySystem.UpdateCapabilities(deltaTime);
            
            // 2. 处理本帧产生的所有事件
            CapabilitySystem.ProcessEntityEvents();
            
            // 3. 更新其他系统...
        }
        
        public void OnEntityDestroyed(Entity entity)
        {
            // 清除该实体的所有待处理事件
            EntityEventQueue.Clear(entity.UniqueId);
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

### 5.2 处理事件（静态声明）

#### HitReactionCapability（处理者）

```csharp
public class HitReactionCapability : Capability<HitReactionCapability>
{
    public override int Priority => 200;
    
    // 静态声明：该 Capability 处理哪些事件
    protected override void RegisterEventHandlers()
    {
        RegisterEventHandler<SkillEffectEvent>(OnSkillEffect);
    }
    
    // 事件处理函数（由 CapabilitySystem 自动调度）
    private void OnSkillEffect(Entity entity, SkillEffectEvent evt)
    {
        // 获取效果配置
        var config = GetEffectConfig(evt.EffectId);
        if (config == null)
            return;
        
        // 根据效果类型处理
        switch (config.EffectType)
        {
            case 1: ProcessDamage(entity, evt, config); break;
            case 2: ProcessHeal(entity, evt, config); break;
            case 3: ProcessKnockback(entity, evt, config); break;
            // ...
        }
    }
    
    private void ProcessKnockback(Entity entity, SkillEffectEvent evt, SkillEffectConfig config)
    {
        // 写入击退数据到 KnockbackComponent
        var knockback = GetOrAddComponent<KnockbackComponent>(entity);
        knockback.IsKnockingBack = true;
        knockback.Direction = CalculateDirection(evt.CasterId, entity.UniqueId);
        knockback.TotalDistance = FP.FromFloat(config.EffectValue);
        knockback.RemainingTime = FP.FromFloat(config.EffectDuration);
        knockback.Speed = knockback.TotalDistance / knockback.RemainingTime;
    }
}
```

#### 处理多种事件

```csharp
public class CombatCapability : Capability<CombatCapability>
{
    // 静态声明：处理多种事件
    protected override void RegisterEventHandlers()
    {
        RegisterEventHandler<DamageEvent>(OnDamage);
        RegisterEventHandler<HealEvent>(OnHeal);
        RegisterEventHandler<BuffEvent>(OnBuff);
    }
    
    // 每个事件类型对应一个处理函数
    private void OnDamage(Entity entity, DamageEvent evt)
    {
        var health = GetComponent<HealthComponent>(entity);
        if (health != null)
        {
            health.CurrentHP -= evt.Damage;
            ASLogger.Instance.Debug($"Entity {entity.UniqueId} took {evt.Damage} damage");
        }
    }
    
    private void OnHeal(Entity entity, HealEvent evt)
    {
        var health = GetComponent<HealthComponent>(entity);
        if (health != null)
        {
            health.CurrentHP += evt.HealAmount;
            ASLogger.Instance.Debug($"Entity {entity.UniqueId} healed {evt.HealAmount} HP");
        }
    }
    
    private void OnBuff(Entity entity, BuffEvent evt)
    {
        var buff = GetOrAddComponent<BuffComponent>(entity);
        buff.AddBuff(evt.BuffId, evt.Duration);
        ASLogger.Instance.Debug($"Entity {entity.UniqueId} received buff {evt.BuffId}");
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


