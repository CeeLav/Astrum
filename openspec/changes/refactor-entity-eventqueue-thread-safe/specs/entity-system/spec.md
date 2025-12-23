## MODIFIED Requirements

### Requirement: Entity 事件队列系统

系统 SHALL 提供线程安全的 Entity 事件队列，允许任意线程（包括 Client/View 主线程）向 Logic 线程发送事件以修改实体数据。

#### Scenario: Client 线程安全发送事件

- **WHEN** Client 线程调用 `entity.QueueEvent(new SetPositionEvent { Position = pos })`
- **THEN** 事件被添加到线程安全的队列中
- **AND** Logic 线程在事件处理阶段处理该事件
- **AND** 目标实体的 `TransComponent.Position` 被更新
- **AND** `TransComponent` 被标记为 dirty 以导出到 ViewRead

#### Scenario: 多线程并发入队事件

- **WHEN** 多个线程同时调用 `entity.QueueEvent<T>()`
- **THEN** 所有事件都被正确添加到队列
- **AND** 无数据竞争或崩溃
- **AND** 事件顺序按入队时间排列（FIFO）

#### Scenario: Logic 线程消费事件时其他线程写入

- **WHEN** Logic 线程正在处理事件队列
- **AND** 其他线程同时写入新事件
- **THEN** 新事件被正确添加到队列
- **AND** Logic 线程可能在当前帧或下一帧处理新事件
- **AND** 无数据竞争或崩溃

#### Scenario: 延迟初始化事件队列

- **WHEN** 首次调用 `entity.QueueEvent<T>()` 时事件队列为 null
- **THEN** 系统以线程安全的方式创建队列（使用 `Interlocked.CompareExchange`）
- **AND** 多个线程同时初始化时只有一个队列实例被创建
- **AND** 所有后续事件正确入队

#### Scenario: 事件队列清空

- **WHEN** 调用 `entity.ClearEventQueue()`
- **THEN** 使用 `TryDequeue` 循环清空队列
- **AND** 操作是线程安全的

#### Scenario: API 向后兼容

- **WHEN** Logic 内部代码调用 `entity.QueueEvent<T>()`
- **THEN** 行为与改造前完全一致
- **AND** 无需修改现有代码
- **AND** API 签名保持不变

### Requirement: 事件数据约束

系统 SHALL 要求事件数据必须是值类型（struct），以避免 GC 分配和线程安全问题。

#### Scenario: 事件数据为值类型

- **WHEN** 定义新的事件类型
- **THEN** 事件数据必须实现 `IEvent` 接口
- **AND** 事件数据必须是 `struct`（值类型）
- **AND** 事件数据字段应为值类型或值类型集合

#### Scenario: 事件数据大小限制

- **WHEN** 事件数据大小超过建议阈值（256 bytes）
- **THEN** 系统记录警告日志
- **AND** 建议使用 ID 引用或序列化字节数组传递大数据

### Requirement: CapabilitySystem 事件消费

系统 SHALL 使用 `TryDequeue` 从 `ConcurrentQueue<EntityEvent>` 中消费事件。

#### Scenario: 使用 TryDequeue 消费事件

- **WHEN** `CapabilitySystem.ProcessTargetedEvents()` 处理事件队列
- **THEN** 使用 `while (eventQueue.TryDequeue(out var evt))` 消费事件
- **AND** 自然处理空队列情况（无需额外检查）
- **AND** 线程安全地消费事件

#### Scenario: 保持事件处理顺序

- **WHEN** 多个事件入队后被处理
- **THEN** 事件按 FIFO 顺序处理
- **AND** 事件处理机制与改造前一致
