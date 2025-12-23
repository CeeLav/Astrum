# Change: ViewEvent 队列线程安全改造

## Why

当前 ViewEvent 系统使用 `Queue<ViewEvent>`，不是线程安全的。为后续 Logic 多线程做准备，需要确保：
1. Logic 线程可以安全地写入事件（`Entity.QueueViewEvent()`）
2. View 线程（主线程）可以安全地消费事件（`Stage.ProcessViewEvents()`）
3. 不会出现数据竞争、事件丢失或崩溃

## What Changes

- 将 `Entity._viewEventQueue` 从 `Queue<ViewEvent>` 改为 `ConcurrentQueue<ViewEvent>`
- 将 `GlobalEventQueue._globalEvents` 从 `Queue<EntityEvent>` 改为 `ConcurrentQueue<EntityEvent>`
- 移除不必要的 Swap 机制（ViewEvent 不需要像 ViewRead 那样严格帧对齐）
- 更新 `Stage.ProcessViewEvents()` 以适配 `ConcurrentQueue` 的消费方式
- 添加线程安全的延迟初始化逻辑

**技术选择：ConcurrentQueue vs 双缓冲**
- ViewEvent 是表现层反馈（特效、音效、震屏等），容忍 1-2 帧延迟
- 不需要像 ViewRead（位置、血量等）那样严格的帧对齐
- ConcurrentQueue 实现简单、性能优异、无锁设计，更适合 ViewEvent 场景

## Impact

- 影响的代码：
  - `AstrumLogic/Core/Entity.ViewEventQueue.cs` - 队列类型变更
  - `AstrumLogic/Events/GlobalEventQueue.cs` - 队列类型变更
  - `AstrumView/Core/Stage.cs` - 消费逻辑调整
- 行为变更：
  - ViewEvent 可能在 Logic 帧 N 产生，但在 View 帧 N+1 或 N+2 处理（非破坏性，表现层可接受）
  - Logic 和 View 可以真正并发处理事件队列（为多线程准备）
- 性能影响：
  - ✅ 无锁设计，性能优于双缓冲+Swap 方案
  - ✅ 避免遍历所有实体进行 Swap 的开销
  - ✅ Logic 写入事件的延迟降低（无需等待 Swap）

