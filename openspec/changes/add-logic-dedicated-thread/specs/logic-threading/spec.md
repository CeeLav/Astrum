# Capability: Logic Threading

## ADDED Requirements

### Requirement: 独立逻辑线程

系统 SHALL 在游戏会话启动时创建独立的逻辑线程，在会话结束时销毁该线程。

#### Scenario: 游戏启动时创建线程

- **WHEN** GameMode.StartGame() 被调用
- **THEN** LogicThread 实例被创建并启动
- **AND** 逻辑线程进入运行状态

#### Scenario: 游戏结束时销毁线程

- **WHEN** GameMode.Shutdown() 被调用
- **THEN** LogicThread 收到停止信号
- **AND** 逻辑线程在 5 秒内退出
- **AND** LogicThread 实例被清理

### Requirement: 固定时间步长逻辑循环

逻辑线程 SHALL 以固定时间步长（20Hz, 50ms per frame）运行逻辑更新，不受渲染帧率影响。

#### Scenario: 逻辑帧以固定间隔执行

- **WHEN** 逻辑线程运行
- **THEN** 每帧调用 Room.Update(0.05f)
- **AND** 实际帧率在 19-21 FPS 范围内（允许 5% 误差）
- **AND** 帧间隔标准差 < 5ms

#### Scenario: 逻辑帧不受渲染帧率影响

- **WHEN** 渲染帧率波动（30 FPS - 60 FPS）
- **THEN** 逻辑帧率保持 20 FPS ± 5%
- **AND** 逻辑计算时间不受渲染负载影响

### Requirement: 暂停和恢复

逻辑线程 SHALL 支持暂停和恢复操作。

#### Scenario: 暂停逻辑线程

- **WHEN** LogicThread.Pause() 被调用
- **THEN** 逻辑线程停止调用 Room.Update()
- **AND** 线程保持运行但空闲（低 CPU 占用）

#### Scenario: 恢复逻辑线程

- **WHEN** LogicThread.Resume() 被调用（在暂停状态下）
- **THEN** 逻辑线程恢复调用 Room.Update()
- **AND** 逻辑帧率恢复到 20 FPS

### Requirement: 异常处理和日志

逻辑线程 SHALL 捕获异常并记录详细日志，确保问题可追溯。

#### Scenario: 逻辑线程异常记录

- **WHEN** 逻辑线程内部抛出异常
- **THEN** 异常被捕获并记录到日志（包含堆栈跟踪）
- **AND** 逻辑线程终止
- **AND** 主线程收到错误通知

#### Scenario: 线程启动失败

- **WHEN** 逻辑线程启动失败（如资源不足）
- **THEN** 错误被记录到日志
- **AND** GameMode.StartGame() 抛出异常
- **AND** 游戏回退到主线程模式（如果配置允许）

### Requirement: 配置开关

系统 SHALL 提供配置开关 `LogicThreadingEnabled` 以启用/禁用多线程模式。

#### Scenario: 多线程模式启用

- **WHEN** `LogicThreadingEnabled` 为 `true`
- **THEN** 逻辑在独立线程运行
- **AND** View 在主线程读取 ViewRead 快照

#### Scenario: 多线程模式禁用（向后兼容）

- **WHEN** `LogicThreadingEnabled` 为 `false`
- **THEN** 逻辑在主线程运行（旧行为）
- **AND** View 直接读取 Logic 组件（旧行为）

### Requirement: 输入系统线程安全

系统 SHALL 使用线程安全机制处理网络输入和本地输入，确保主线程和逻辑线程无竞争。

#### Scenario: 网络输入写入队列

- **WHEN** 主线程（网络消息处理器）调用 SetOneFrameInputs(inputs)
- **THEN** 输入被复制并加入 ConcurrentQueue
- **AND** 主线程立即返回（不阻塞）

#### Scenario: 逻辑线程消费网络输入队列

- **WHEN** 逻辑线程 Tick() 开始时调用 ProcessPendingInputs()
- **THEN** 所有队列中的网络输入被消费并写入 FrameBuffer
- **AND** 输入对象被回收到对象池

#### Scenario: 本地输入原子交换

- **WHEN** 主线程（InputManager.Update）调用 SetPlayerInput(playerId, input)
- **THEN** 新输入通过 Interlocked.Exchange 原子替换旧输入
- **AND** 主线程立即返回（不阻塞）

#### Scenario: 逻辑线程读取本地输入

- **WHEN** 逻辑线程 GetOneFrameMessages() 调用时
- **THEN** 使用 Interlocked.CompareExchange 原子读取最新本地输入
- **AND** 读取到的输入对象仅在逻辑线程修改
- **AND** 无数据竞争

#### Scenario: FrameBuffer 单线程访问

- **WHEN** 输入队列被消费后
- **THEN** FrameBuffer 仅在逻辑线程访问（读取和写入）
- **AND** 无数据竞争

### Requirement: 线程安全验证

逻辑线程 SHALL 与其他线程通过线程安全机制通信，确保数据一致性。

#### Scenario: ViewRead 双缓冲无竞争

- **WHEN** 逻辑线程写入 ViewRead back buffer
- **AND** View 线程读取 ViewRead front buffer
- **THEN** 无数据竞争（通过 ThreadSanitizer 验证）
- **AND** View 读取的数据一致且完整

#### Scenario: 事件队列线程安全

- **WHEN** View 线程调用 Entity.QueueEvent()
- **AND** 逻辑线程处理事件队列
- **THEN** 无事件丢失
- **AND** 事件按顺序处理（FIFO）
- **AND** 无数据竞争

#### Scenario: 输入队列线程安全

- **WHEN** 主线程调用 SetOneFrameInputs() 多次
- **AND** 逻辑线程并发调用 ProcessPendingInputs()
- **THEN** 所有输入按顺序处理
- **AND** 无输入丢失或重复
- **AND** 无数据竞争

## MODIFIED Requirements

_无_

## REMOVED Requirements

_无_

