## Context

- **背景**：`refactor-ecc-viewread-snapshots` 改造后，Logic 组件字段改为 `internal`，View/Client 无法直接修改
- **问题**：外部需要修改 Logic 数据时（GM 命令、存档加载、调试工具），需要通过事件系统，但现有事件队列非线程安全
- **现状**：
  - `Entity.QueueEvent<T>()` 使用 `Queue<EntityEvent>`（非线程安全）
  - 设计假设只在 Logic 线程内部使用
  - 直接从 Client 线程调用会导致数据竞争

## Goals / Non-Goals

### Goals
- 将现有 `Entity.EventQueue` 改造为线程安全
- Client 线程可以安全调用 `entity.QueueEvent<T>()`
- 保持 Logic 帧率稳定（性能无明显下降）
- 渐进式迁移现有写入点（18 处）为使用事件
- API 完全向后兼容（只改内部实现）

### Non-Goals
- 不引入新的"命令"概念（统一使用事件）
- 不实现 Logic → Client 的响应机制（事件是单向的）
- 不支持事件优先级或取消（保持简单）
- 不改变事件处理时机和顺序

## Decisions

### Decision 1: Entity.EventQueue 改为 ConcurrentQueue（与 ViewEvent 一致）

**Why:**
- Client 线程和 Logic 线程都可能调用 `entity.QueueEvent<T>()`
- `ConcurrentQueue<T>` 无锁设计，性能优异
- 与 `refactor-viewevent-thread-safe` 保持一致的设计

**How:**
```csharp
// Entity.EventQueue.cs
public partial class Entity
{
    [MemoryPackIgnore]
    private ConcurrentQueue<EntityEvent> _eventQueue;
    
    public void QueueEvent<T>(T eventData) where T : struct, IEvent
    {
        // 线程安全的延迟初始化
        if (_eventQueue == null)
        {
            var newQueue = new ConcurrentQueue<EntityEvent>();
            Interlocked.CompareExchange(ref _eventQueue, newQueue, null);
        }
        
        _eventQueue.Enqueue(new EntityEvent
        {
            EventType = typeof(T),
            EventData = eventData,
            Frame = World?.CurFrame ?? 0
        });
    }
    
    internal ConcurrentQueue<EntityEvent> EventQueue => _eventQueue;
    public bool HasPendingEvents => _eventQueue != null && !_eventQueue.IsEmpty;
}
```

**Trade-offs:**
- ✅ 线程安全，无锁，性能优异
- ✅ API 完全向后兼容（Logic 内部代码无需修改）
- ✅ 简单直接，与 ViewEvent 设计一致
- ⚠️ 事件可能延迟 1-2 帧处理（但对于 GM/存档等场景可接受）

### Decision 2: CapabilitySystem 更新为使用 TryDequeue

**Why:**
- `ConcurrentQueue` 不支持 `Dequeue()`，需要使用 `TryDequeue()`
- 保持现有事件处理机制不变
- 与 `refactor-viewevent-thread-safe` 中的 Stage.ProcessViewEvents() 一致

**How:**
```csharp
// CapabilitySystem.ProcessTargetedEvents()
while (eventQueue.TryDequeue(out var evt))  // 改为 TryDequeue
{
    DispatchEventToEntity(entity, evt);
}
```

### Decision 3: 事件数据使用 struct（值类型，避免 GC）

**Why:**
- 保持与现有事件系统一致（`IEvent` 已经是 struct）
- 避免堆分配和 GC 压力
- 数据复制开销可接受（事件通常较小）

**Constraint:**
- 事件数据必须实现 `IEvent` 接口（struct）
- 不能包含引用类型（如 `string`, `List<T>` 等）
- 如需传递复杂数据，使用 ID 引用或序列化字节数组

## Risks / Trade-offs

### Risk 1: 事件处理延迟

**问题：**
- Client 在帧 N 发送事件，可能在 Logic 帧 N+1 或 N+2 处理
- 对于实时性要求高的操作可能不适用

**Mitigation:**
- 对于 GM 命令、存档加载等场景，1-2 帧延迟完全可接受
- 文档明确说明事件的延迟特性
- 如需实时性，考虑在 Logic 线程内部直接操作

### Risk 2: 事件数据大小限制

**问题：**
- struct 值类型复制，大数据会影响性能
- 不能直接传递引用类型（如 `string`, `List<T>`）

**Mitigation:**
- 限制事件数据大小（建议 < 256 bytes）
- 大数据使用 ID 引用或序列化字节数组
- 提供辅助方法处理常见场景（如序列化/反序列化）

### Risk 3: 事件队列积压

**问题：**
- 如果 Client 发送事件速度 > Logic 处理速度，队列会积压

**Mitigation:**
- 监控事件队列长度，超过阈值（如 1000）时警告
- 对于批量操作（如存档加载），分批发送事件
- 现有事件处理机制已有限流保护

## Migration Plan

### Phase 1: 事件队列线程安全改造

1. **Entity.EventQueue 改造**
   - 将 `Queue<EntityEvent>` 改为 `ConcurrentQueue<EntityEvent>`
   - 线程安全的延迟初始化
   - 更新 `HasPendingEvents` 和 `ClearEventQueue()`

2. **CapabilitySystem 更新**
   - 使用 `TryDequeue(out var evt)` 替代 `Dequeue()`
   - 保持现有事件处理逻辑不变

3. **单元测试**
   - 事件队列线程安全测试
   - 并发入队/出队测试
   - 性能基准测试

### Phase 2: 常用事件定义

1. **Transform 事件**
   - `SetPositionEvent`
   - `SetRotationEvent`
   - `SetScaleEvent`

2. **Stats 事件**
   - `SetHealthEvent`
   - `SetLevelEvent`

3. **Component 事件**
   - `LoadComponentDataEvent`（通用序列化）
   - `ResetComponentEvent`

### Phase 3: 迁移 SinglePlayerGameMode

迁移 3 处直接写入点：
- MonsterInfoComponent 写入 → `entity.QueueEvent<SetMonsterInfoEvent>()`
- TransComponent 写入 → `entity.QueueEvent<SetPositionEvent>()`
- LevelComponent 写入 → `entity.QueueEvent<SetLevelEvent>()`

### Phase 4: 迁移 PlayerDataManager

迁移 15 处直接写入点：
- 存档加载 → `entity.QueueEvent<LoadComponentDataEvent>()`
- 批量操作优化（分批发送事件）

### Phase 5: 文档和验证

1. 更新事件系统文档
2. 性能验证（无明显下降）
3. 线程安全验证
4. 集成测试

## Open Questions

1. **是否需要事件响应机制？**
   - 当前设计是单向的（Client → Logic）
   - 如果需要知道事件是否处理成功，可以通过 ViewRead 读取结果
   - 或者通过 ViewEvent 发送反馈到 View 层
   - 建议先保持简单

2. **是否需要事件批处理？**
   - 对于存档加载等批量操作，可以考虑批处理优化
   - 例如：`LoadMultipleComponentsEvent` 一次加载多个组件
   - 可以在 Phase 4 实施时评估

## Implementation Notes

### 事件命名约定

- 事件名称使用动词开头：`Set`, `Load`, `Reset`, `Add`, `Remove`
- 事件结构体后缀 `Event`：`SetPositionEvent`
- 事件处理方法前缀 `Handle`：`HandleSetPosition`

### 性能考虑

- 事件队列使用 `ConcurrentQueue`，无锁设计
- 事件数据使用 struct，避免 GC
- 监控事件队列长度，及时警告
- 现有事件处理机制已优化，无需额外限流

### 调试支持

- 复用现有事件日志系统
- 记录事件处理时间
- 提供事件队列状态查询接口

### 示例代码

```csharp
// Client 线程发送事件（线程安全）
var evt = new SetPositionEvent { Position = new TSVector(10, 0, 10) };
entity.QueueEvent(evt);

// Logic 线程处理事件（自动）
// 注册的 Capability 会接收到事件
```

