# EntityView Event Queue Design

## Context

当前系统架构：
- **逻辑层**：Entity、Component、Capability、World（使用 TrueSync FP Math，确定性计算）
- **视图层**：Stage、EntityView、ViewComponent（使用 Unity，负责渲染和表现）
- **事件系统**：EventSystem 全局单例，支持订阅/发布模式

当前问题：
1. 逻辑层通过 EventSystem 同步触发视图层回调（如 `Stage.OnEntityCreated`）
2. 回调在逻辑层执行期间立即执行，增加帧耗时
3. 无法将逻辑层移到独立线程，因为视图层（Unity）必须在主线程

## Goals / Non-Goals

### Goals
- 逻辑层和视图层解耦，逻辑层不再直接触发视图层回调
- 为 EntityView 添加事件队列机制，类似 Entity.EventQueue
- 视图层在合适的时机批量处理事件
- 保持向后兼容，不影响现有功能

### Non-Goals
- 不修改 EventSystem 本身（保留用于其他系统级事件）
- 不改变 Entity.EventQueue 的设计（仅借鉴其模式）
- 不在本阶段实现逻辑层多线程化（为未来铺路）

## Decisions

### Decision 1: 事件队列设计

**选择**：将 ViewEvent 队列放在 Entity 上，而不是 EntityView 上

**结构**：
```csharp
// ViewEvents.cs
public struct ViewEvent
{
    public ViewEventType EventType;
    public object EventData;
    public int Frame;
}

public enum ViewEventType
{
    EntityCreated,
    EntityDestroyed,
    SubArchetypeChanged,
    ComponentDirty,
    WorldRollback
}

// Entity.ViewEventQueue.cs (新增 partial class)
public partial class Entity
{
    [MemoryPack.MemoryPackIgnore]
    private Queue<ViewEvent> _viewEventQueue;
    
    [MemoryPack.MemoryPackIgnore]
    public bool HasPendingViewEvents => _viewEventQueue != null && _viewEventQueue.Count > 0;
    
    public void QueueViewEvent(ViewEvent evt) { /* ... */ }
    internal Queue<ViewEvent> ViewEventQueue => _viewEventQueue;
    internal void ClearViewEventQueue() { /* ... */ }
}
```

**替代方案**：
- A. ViewEvent 队列放在 EntityView 上
  - ❌ 拒绝：EntityView 可能还未创建，需要额外的缓存机制
- B. ViewEvent 队列放在 Stage 上（全局）
  - ❌ 拒绝：需要维护 EntityId 到队列的映射，复杂度高
- C. 复用 Entity.EventQueue（逻辑层和视图层共用）
  - ❌ 拒绝：混合了逻辑层事件和视图层事件，职责不清

**理由**：
1. **创建顺序友好**：Entity 总是先于 EntityView 创建，天然解决事件缓存问题
2. **设计一致性**：与 Entity.EventQueue 保持完全一致的模式
3. **简化 Stage**：不需要临时缓存，只负责查询和分发
4. **职责清晰**：Entity 存储所有事件数据，Stage 协调处理流程

### Decision 2: 事件路由机制

**问题**：逻辑层如何将事件路由到对应的 EntityView？

**选择**：事件存储在 Entity 上，Stage 轮询并分发到 EntityView

**流程**：
```
World.CreateEntity()
  ↓
entity.QueueViewEvent(ViewEvent { EventType = EntityCreated, ... })
  ↓
Entity._viewEventQueue.Enqueue(event)
  ↓
[等待下一帧]
  ↓
Stage.Update() → ProcessViewEvents()
  ↓
遍历所有 Entity，检查 entity.HasPendingViewEvents
  ↓
获取或创建对应的 EntityView
  ↓
while (entity.ViewEventQueue.Count > 0)
  {
    var evt = entity.ViewEventQueue.Dequeue();
    entityView.ProcessEvent(evt);
  }
```

**替代方案**：
- A. 逻辑层将事件发送到 Stage，Stage 缓存
  - ❌ 拒绝：需要处理 EntityView 不存在的情况，增加复杂度
- B. 逻辑层直接访问 EntityView
  - ❌ 拒绝：逻辑层需要引用视图层，耦合度高

**理由**：
1. **自然的数据流**：Entity 产生事件 → Entity 存储事件 → Stage 查询处理
2. **无需额外缓存**：Entity 比 EntityView 先创建，事件天然有存储位置
3. **与 Entity.EventQueue 一致**：CapabilitySystem 轮询 Entity.EventQueue，Stage 轮询 Entity.ViewEventQueue
4. **职责单一**：Entity 负责存储，Stage 负责协调，EntityView 负责处理

### Decision 3: EntityView 创建时机

**问题**：如果 EntityView 还未创建，事件如何处理？

**选择**：事件存储在 Entity 上，EntityView 不存在时自动创建

**实现**：
```csharp
// Stage.cs
private void ProcessViewEvents()
{
    if (_room?.MainWorld == null) return;
    
    // 遍历所有 Entity，检查是否有待处理的视图事件
    foreach (var entity in _room.MainWorld.Entities.Values)
    {
        if (entity == null || !entity.HasPendingViewEvents)
            continue;
        
        var entityId = entity.UniqueId;
        var eventQueue = entity.ViewEventQueue;
        
        // 获取或创建 EntityView
        if (!_entityViews.TryGetValue(entityId, out var entityView))
        {
            // 第一个事件应该是 EntityCreated，如果不是则跳过
            if (eventQueue.Peek().EventType != ViewEventType.EntityCreated)
            {
                ASLogger.Instance.Warning($"Stage: Entity {entityId} 的第一个视图事件不是 EntityCreated，跳过处理");
                continue;
            }
            
            // 创建 EntityView
            entityView = CreateEntityViewInternal(entityId);
            if (entityView == null)
            {
                ASLogger.Instance.Error($"Stage: 无法创建 EntityView for Entity {entityId}");
                continue;
            }
            _entityViews[entityId] = entityView;
        }
        
        // 处理所有事件
        int processedCount = 0;
        while (eventQueue.Count > 0)
        {
            var evt = eventQueue.Dequeue();
            entityView.ProcessEvent(evt);
            processedCount++;
        }
        
        ASLogger.Instance.Debug($"Stage: 处理了 {processedCount} 个视图事件，Entity {entityId}");
    }
}
```

**优势**：
1. **无需缓存**：Entity 创建时事件就入队了，不需要 Stage 的临时缓存
2. **创建顺序保证**：Entity 总是先于 EntityView 创建
3. **简化逻辑**：Stage 只需要检查 `entity.HasPendingViewEvents`
4. **内存友好**：不需要额外的 `_pendingViewEvents` 字典

### Decision 4: 跨层数据结构的权衡

**问题**：ViewEvent 是视图层概念，放在 Entity（逻辑层）是否合理？

**选择**：接受这种跨层设计，因为收益大于成本

**分析**：
- **成本**：Entity 需要包含视图相关的数据结构
- **收益**：
  1. 完全解决事件缓存问题
  2. 与 Entity.EventQueue 设计一致
  3. 简化 Stage 实现
  4. 为多线程化铺路（Entity 可以在独立线程中入队事件）

**职责划分**：
- `ViewEvent`：纯数据结构，不包含任何视图逻辑
- `Entity._viewEventQueue`：数据存储，类似 `Entity._eventQueue`
- `EntityView.ProcessEvent()`：事件处理逻辑，真正的视图层代码

**类比**：
```
Entity.EventQueue       → 逻辑层事件（Capability 处理）
Entity.ViewEventQueue   → 视图层事件（EntityView 处理）
```

两者都存储在 Entity 上，但处理者不同，职责清晰。

### Decision 6: 兼容性与性能

**向后兼容**：
- EventSystem 保持不变，仅实体相关事件使用队列
- 现有代码（UI、音效、特效）继续使用 EventSystem
- 提供配置开关，允许切换回同步模式（用于调试）

**性能优化**：
- 批量处理：一次 Update 处理所有待处理事件
- 延迟创建：Queue 延迟创建，节省内存
- 清理空队列：避免内存泄漏

## Risks / Trade-offs

### Risk 1: 事件延迟
- **风险**：事件不再立即处理，可能导致视图层滞后
- **缓解**：每帧处理一次，延迟在 1 帧以内（16ms）
- **影响**：对大多数场景可接受，特效和动画本身就有延迟

### Risk 2: 事件顺序
- **风险**：多个事件的顺序可能影响结果
- **缓解**：保证入队顺序，按 FIFO 处理
- **影响**：与当前同步调用的顺序一致

### Risk 3: 调试难度
- **风险**：异步处理增加调试难度
- **缓解**：提供详细日志，配置开关可切换回同步模式
- **影响**：可控，开发阶段可使用同步模式

### Trade-off: 内存 vs 性能
- **成本**：每个 EntityView 额外的 Queue 实例（约 80 bytes）
- **收益**：减少逻辑层阻塞，提升帧率
- **结论**：内存成本可接受

## Migration Plan

### Phase 1: 添加事件队列基础设施（不影响现有功能）
1. 添加 `ViewEvent` 和 `ViewEventType` 定义
2. 在 Entity 中添加 `ViewEventQueue` 扩展（partial class）
3. 添加 `Entity.HasViewLayer` 静态标记（默认 false）
4. 添加配置项 `USE_VIEW_EVENT_QUEUE`（默认 false）

### Phase 2: 实现事件队列处理逻辑
1. 在 Entity.ViewEventQueue 中实现 `QueueViewEvent()`（检查 HasViewLayer）
2. 在 Stage 中设置 `Entity.HasViewLayer = true`
3. 实现 `Stage.ProcessViewEvents()`（轮询 Entity）
4. 实现 `EntityView.ProcessEvent()`
5. 在 Stage.Update() 中调用 ProcessViewEvents()（当 USE_VIEW_EVENT_QUEUE=true 时）

### Phase 3: 迁移事件发布点
1. 修改 `World.CreateEntity()` 调用 `entity.QueueViewEvent()`
2. 修改 `World.DestroyEntity()` 调用 `entity.QueueViewEvent()`
3. 修改 `Entity` 子原型变化调用 `this.QueueViewEvent()`
4. 修改脏标记同步使用 QueueViewEvent（可选）

### Phase 4: 测试与验证
1. 单元测试：事件队列基础功能
2. 集成测试：完整的实体生命周期
3. 性能测试：大量实体场景
4. 启用 `USE_VIEW_EVENT_QUEUE=true` 作为默认值

### Phase 5: 清理旧代码
1. 移除 Stage 中的 EventSystem 订阅（保留注释）
2. 移除配置开关（或保留用于调试）
3. 更新文档

### Rollback Plan
- 如果发现严重问题，设置 `USE_VIEW_EVENT_QUEUE=false` 回退到同步模式
- 所有代码保持双模式兼容，直到稳定后才移除旧代码

## Open Questions

1. **Q**: ViewComponent 的数据同步是否也应该使用队列？
   - **A**: 暂不改动，ViewComponent 当前的脏标记机制已经足够（Stage 轮询脏组件）
   
2. **Q**: 是否需要事件优先级？
   - **A**: 暂不需要，按 FIFO 处理即可。未来如有需求可扩展

3. **Q**: 事件队列是否需要持久化（用于回放/回滚）？
   - **A**: 不需要，视图事件是瞬时的，回放时重新生成即可

4. **Q**: 多个 Stage 如何处理？
   - **A**: 每个 Stage 独立的事件队列，互不干扰

