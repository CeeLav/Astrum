## Context

- 目标：为 Logic 多线程做准备，确保 ViewEvent 队列线程安全。
- 当前问题：`Queue<ViewEvent>` 不是线程安全的，Logic 多线程后会导致数据竞争。
- 与 ViewRead 的区别：
  - ViewRead 是状态快照（需要帧对齐、可覆盖）
  - ViewEvent 是消息队列（需要保证送达、不可覆盖、容忍延迟）

## Goals / Non-Goals

- Goals:
  - ViewEvent 队列线程安全（Logic 写、View 读，无数据竞争）
  - 保证所有事件最终送达（不丢失事件）
  - 保证事件时序（先入队的先处理）
  - 性能优异（无锁或低锁开销）
  - 实现简单（易维护、不易出错）
- Non-Goals:
  - 不要求严格的帧对齐（ViewEvent 是表现层反馈，容忍 1-2 帧延迟）
  - 不要求与 ViewRead 同帧 Swap（事件和数据的语义不同）
  - 不实施 Logic 多线程调度（仅做结构准备）

## Decisions

### Decision: 使用 ConcurrentQueue 而非双缓冲+Swap

**Why ConcurrentQueue:**
1. **语义匹配**：ViewEvent 是消息队列，需要累积消费，不能覆盖（与 ViewRead 状态快照不同）
2. **性能优异**：
   - 完全无锁设计（内部用 CAS 原子操作）
   - Logic 和 View 可以真正并发读写，无需 Swap 同步点
   - 避免遍历所有实体执行 Swap 的开销
3. **延迟最低**：Logic 写入后，View 下一次轮询即可消费（无需等待 Swap）
4. **实现简单**：代码量少，逻辑清晰，不易出错

**Why NOT 双缓冲+Swap:**
1. **Swap 开销**：即使无显式锁，也有 CAS 开销，且需要遍历所有实体
2. **过度设计**：ViewEvent 不需要像 ViewRead 那样严格的帧对齐
3. **复杂性**：需要维护 front/back 队列、Swap 逻辑、标记字段等
4. **追加难度**：双缓冲需要"追加而非替换"来保证不丢事件，增加复杂度

**Trade-offs:**
- ✅ 优势：简单、高效、低延迟、线程安全
- ⚠️ 劣势：无法保证"Logic 帧 N 的事件恰好在 View 帧 N 处理"
- 📊 评估：对于表现层事件（特效、音效、震屏等），延迟 1-2 帧（33-66ms）完全可接受

### Decision: 保持消费逻辑在 View 线程（Stage）

**How:**
- Logic 线程调用 `Entity.QueueViewEvent(evt)` → `ConcurrentQueue.Enqueue(evt)`
- View 线程在 `Stage.ProcessViewEvents()` 中遍历实体 → `ConcurrentQueue.TryDequeue(out evt)`

**Why:**
- ViewEvent 是 View 层的输入，由 View 层主导消费时机
- 保持现有架构清晰（Logic 产生事件，View 消费事件）
- 无需在 Logic 帧末 Swap，简化同步逻辑

### Decision: 延迟初始化 + 线程安全

**How:**
```csharp
if (_viewEventQueue == null)
{
    var newQueue = new ConcurrentQueue<ViewEvent>();
    Interlocked.CompareExchange(ref _viewEventQueue, newQueue, null);
}
_viewEventQueue.Enqueue(evt);
```

**Why:**
- 节省内存（大部分实体没有 ViewEvent）
- `Interlocked.CompareExchange` 确保多线程环境下只创建一次队列
- 即使多个线程同时初始化，也只有一个队列实例被使用

### Decision: GlobalEventQueue 同步改造

**Scope:**
- `GlobalEventQueue` 也改为 `ConcurrentQueue<EntityEvent>`
- 保持 API 不变（`QueueGlobalEvent`, `GetEvents`, `ClearAll`）
- 移除双缓冲逻辑（如果有）

**Why:**
- 全局事件也需要线程安全
- 保持与 Entity ViewEvent 一致的设计

## Risks / Trade-offs

### Risk: 事件延迟 1-2 帧

**场景：**
- Logic 帧 N 产生事件，View 帧 N+1 或 N+2 才处理

**Mitigation:**
- 评估：对于表现层事件（特效、音效等），延迟几十毫秒无感知
- 实体创建/销毁事件可能需要更及时，但 1-2 帧延迟仍可接受
- 如果未来发现特定事件需要严格同步，可以单独处理（优先级队列等）

### Risk: ConcurrentQueue 内存分配

**问题：**
- `ConcurrentQueue<T>` 内部使用分段数组，可能有内存开销

**Mitigation:**
- ViewEvent 通常较少（特效、音效等触发频率不高）
- 对比双缓冲（需要两个 Queue + Swap 逻辑），内存开销相近
- 可以在实施后进行性能测试，如有问题再优化

### Risk: IsEmpty 属性的弱一致性

**问题：**
- `ConcurrentQueue.IsEmpty` 在并发环境下是"快照"值，可能不准确

**Mitigation:**
```csharp
// ❌ 不可靠：Logic 线程可能在 IsEmpty 检查后立即 Enqueue
if (!queue.IsEmpty)
    while (queue.TryDequeue(out var evt)) { ... }

// ✅ 可靠：直接 TryDequeue，不依赖 IsEmpty
while (queue.TryDequeue(out var evt)) { ... }
```
- 代码中优先使用 `TryDequeue` 而非 `IsEmpty` 判断

## Migration Plan

### Phase 1: 基础结构改造
1. 修改 `Entity.ViewEventQueue.cs`：
   - 将 `_viewEventQueue` 改为 `ConcurrentQueue<ViewEvent>`
   - 添加线程安全的延迟初始化
   - 更新 `HasPendingViewEvents` 使用 `IsEmpty` 属性
   - 更新 `ClearViewEventQueue` 使用 `TryDequeue` 清空

2. 修改 `GlobalEventQueue.cs`：
   - 将 `_globalEvents` 改为 `ConcurrentQueue<EntityEvent>`
   - 更新相关方法适配新类型

3. 修改 `Stage.ProcessViewEvents()`：
   - 使用 `TryDequeue` 替代 `Dequeue`
   - 移除队列 null 检查后的二次 Count 检查（使用 TryDequeue 自然处理空队列）

### Phase 2: 编译验证
1. 执行 `dotnet build AstrumProj.sln` 确保无编译错误
2. 检查所有引用 `ViewEventQueue` 的代码是否适配新类型

### Phase 3: Unity 运行时验证
1. 在单线程模式下测试（确保行为无回归）
2. 观察事件延迟是否在可接受范围
3. 检查是否有事件丢失或时序错误
4. 性能分析（对比改造前后）

### Phase 4: 文档和注释
1. 更新代码注释，说明线程安全性
2. 在关键方法添加多线程使用说明

## Open Questions

1. **是否需要事件实体追踪优化？**
   - 当前方案遍历所有实体检查 `HasPendingViewEvents`
   - 优化方案：维护"有事件的实体列表"（ConcurrentBag）
   - 决策：先实施基础方案，性能测试后决定是否优化

2. **是否需要限制每帧处理事件数量？**
   - 防止某个实体积累大量事件导致单帧卡顿
   - 决策：先不限制，观察实际情况

3. **Entity 销毁时是否需要特殊处理事件队列？**
   - 当前会清空队列（`ClearViewEventQueue`）
   - 是否需要先处理完队列中的事件再销毁？
   - 决策：保持当前行为（清空队列），销毁事件本身会通知 View 层

