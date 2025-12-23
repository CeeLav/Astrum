# Change: Entity 事件队列线程安全改造

## Why

当前 `refactor-ecc-viewread-snapshots` 改造后，View 层无法直接修改 Logic 层的组件数据（字段已改为 `internal`）。外部（Client/View 主线程）需要修改数据时（如 GM 命令、存档加载、调试工具），需要通过线程安全的方式向 Logic 线程发送事件。

### 当前问题

1. **直接写入不安全**：
   - `SinglePlayerGameMode.cs` 直接写入 `MonsterInfoComponent`, `TransComponent`, `LevelComponent`（3处）
   - `PlayerDataManager.cs` 直接写入多个组件进行存档加载/保存（15处）
   - 多线程环境下会导致数据竞争

2. **现有事件队列非线程安全**：
   - `Entity.QueueEvent<T>()` 使用 `Queue<EntityEvent>`（非线程安全）
   - `GlobalEventQueue` 使用 `Queue<EntityEvent>`（非线程安全）
   - 设计假设只在 Logic 线程内部使用，不支持跨线程调用

## What Changes

### 核心改造

**将现有事件队列改为线程安全**，允许 Client/View 线程直接调用 `entity.QueueEvent<T>()`：

```
Client/View 线程（主线程）
  ├─ 调用 entity.QueueEvent<T>(eventData)
  └─ 写入 ConcurrentQueue<EntityEvent>（线程安全）

Logic 线程（子线程）
  ├─ CapabilitySystem.ProcessEvents() 处理事件
  ├─ 分发事件到 Capability
  └─ Capability 处理事件（修改组件数据）
```

### 修改内容

1. **Entity.EventQueue 线程安全化**
   - 将 `Queue<EntityEvent>` 改为 `ConcurrentQueue<EntityEvent>`
   - 线程安全的延迟初始化（使用 `Interlocked.CompareExchange`）
   - 更新 `HasPendingEvents` 使用 `IsEmpty` 属性
   - 更新 `ClearEventQueue()` 使用 `TryDequeue` 循环清空

2. **GlobalEventQueue 线程安全化**（已完成）
   - 已在 `refactor-viewevent-thread-safe` 中完成
   - 使用 `ConcurrentQueue<EntityEvent>`

3. **CapabilitySystem 更新**
   - 使用 `TryDequeue(out var evt)` 替代 `Dequeue()`
   - 保持现有事件处理机制不变

### 事件定义示例

```csharp
// GM 事件：设置实体位置
public struct SetPositionEvent : IEvent
{
    public TSVector Position;
}

// 存档事件：加载组件数据
public struct LoadComponentDataEvent : IEvent
{
    public int ComponentTypeId;
    public byte[] SerializedData;
}
```

### 迁移路径

- **Phase 1**: Entity.EventQueue 线程安全改造
- **Phase 2**: 定义常用事件类型（SetPosition, LoadData 等）
- **Phase 3**: 迁移 `SinglePlayerGameMode` 的 3 处写入点
- **Phase 4**: 迁移 `PlayerDataManager` 的 15 处写入点

## Impact

### 影响的规范

- `entity-system` (修改) - 事件队列系统增加线程安全支持

### 影响的代码

**新增文件**:
- `AstrumLogic/Events/CommonEvents.cs` - 常用事件定义（SetPosition, LoadData 等）

**修改文件**:
- `AstrumLogic/Core/Entity.EventQueue.cs` - 改为 `ConcurrentQueue<EntityEvent>`
- `AstrumLogic/Systems/CapabilitySystem.cs` - 使用 `TryDequeue` 消费事件
- `AstrumClient/Managers/SinglePlayerGameMode.cs` - 迁移为使用事件
- `AstrumClient/Managers/PlayerDataManager.cs` - 迁移为使用事件

### 兼容性

- ✅ **完全向后兼容** - API 不变，只是内部实现改为线程安全
- ✅ **现有代码无需修改** - Logic 内部的 `entity.QueueEvent<T>()` 调用保持不变
- ✅ **渐进式迁移** - Client 代码可以逐步从直接写入改为发送事件
- ✅ **与 ViewEvent 一致** - 使用相同的线程安全设计模式

## Dependencies

- 依赖 `refactor-ecc-viewread-snapshots` 完成（组件字段已 internal）
- 参考 `refactor-viewevent-thread-safe` 的线程安全设计（已归档）

## Success Criteria

- [ ] Client 线程可以安全地调用 `entity.QueueEvent<T>()`
- [ ] `Entity.EventQueue` 使用 `ConcurrentQueue` 实现线程安全
- [ ] Logic 线程正常处理事件队列
- [ ] 事件处理不影响 Logic 帧率（性能无明显下降）
- [ ] 所有现有功能正常（GM 命令、存档加载等）
- [ ] 无数据竞争和线程安全问题
- [ ] 单元测试覆盖事件队列线程安全性

