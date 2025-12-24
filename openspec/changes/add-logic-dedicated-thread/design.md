# Design: 逻辑专用线程

## Context

Astrum 采用 ECC（Entity-Component-Capability）架构，Logic 层（确定性逻辑）和 View 层（表现）已经在代码层面分离，但仍运行在同一个 Unity 主线程中。

**现有基础设施**:
- ViewRead 双缓冲：Logic 写 back，View 读 front，帧末 swap（无锁）
- EntityEventQueue：`ConcurrentQueue<EntityEvent>` 线程安全队列
- ViewEventQueue：`ConcurrentQueue<ViewEvent>` 线程安全队列

**关键约束**:
- 确定性逻辑必须使用 TrueSync FP Math（已满足）
- 逻辑帧率固定 20Hz（50ms per frame）
- 渲染帧率不固定（通常 30-60 FPS）

## Goals / Non-Goals

### Goals

1. **稳定的逻辑帧率**：Logic 不受渲染帧率波动影响
2. **充分利用多核**：Logic 和 View 并行运行，提升 CPU 利用率
3. **向后兼容**：可通过配置开关启用/禁用多线程模式
4. **简单可靠**：使用 `System.Threading.Thread` 直接创建，生命周期清晰

### Non-Goals

1. **不使用 Unity Job System**：Job System 不适合长期运行的逻辑线程
2. **不使用 ThreadPool**：ThreadPool 线程生命周期不可控
3. **不使用 async/await**：逻辑循环是同步的，不需要异步模型
4. **不支持多个逻辑线程**：单个逻辑线程足够（ECS 内部已是数据导向，易并行化）

## Decisions

### Decision 1: 使用 `System.Threading.Thread` 直接创建

**理由**:
- **生命周期可控**：每局游戏创建，游戏结束销毁，清晰明确
- **简单直接**：不需要复杂的线程池管理或 Unity Job System 调度
- **调试友好**：线程有独立名称，易于 Profiler 识别

**Alternatives Considered**:
- **Unity Job System**：适合短时并行任务（如批量物理计算），不适合长期运行的逻辑循环
- **ThreadPool**：线程复用导致生命周期不可控，调试困难
- **Task**：基于 ThreadPool，同样问题

### Decision 2: 持续轮询 + 短休眠

**逻辑线程伪代码**:
```csharp
void LogicThreadLoop()
{
    while (_isRunning)
    {
        // 持续调用 Tick()，让其自行判断是否需要执行
        _room?.Update(0); // deltaTime 可以传 0 或实际值
        // → Room.Update() 调用 LSController.Tick()
        // → Tick() 内部逻辑保持不变：
        //    1. ProcessPendingInputs() - 消费输入队列
        //    2. 处理权威帧（有多少处理多少，处理完就退出）
        //       while (ProcessedAuthorityFrame < AuthorityFrame) { ... }
        //    3. 处理预测帧（基于时间戳判断，时间未到则跳过）
        //       while (currentTime >= CreationTime + (PredictionFrame + 1) * 50ms) { ... }
        //    4. CheckAndRollbackShadow() - 比对和回滚
        
        // 短暂休眠，避免 CPU 空转
        Thread.Sleep(1); // 1ms 休眠
        // ✅ 权威帧最多延迟 1ms（可接受）
        // ✅ 预测帧时间控制在 Tick 内部（不受影响）
        // ✅ CPU 占用降低到 5-10%
    }
}
```

**关键说明**:
- **不改变 Tick() 内部逻辑**：ClientLSController.Tick() 已有完整的权威帧/预测帧/影子世界逻辑，LogicThread 只是改变了调用位置
- **权威帧驱动方式**：有输入就立即处理，无需等待固定间隔
  - `while (ProcessedAuthorityFrame < AuthorityFrame)` - 处理所有待处理的权威帧
  - 网络下发 3 帧输入 → 一次 Tick() 处理 3 帧 → 立即返回
- **预测帧驱动方式**：基于时间戳判断（20 FPS = 50ms 间隔）
  - `while (currentTime >= CreationTime + (PredictionFrame + 1) * 50ms)` - 时间到才执行
  - 时间未到 → 跳过预测帧 → 立即返回
- **仅添加输入队列消费**：在 Tick() 开始时调用 ProcessPendingInputs()，将网络输入从队列写入 FrameBuffer

**理由**:
- **低延迟响应**：权威帧有输入就处理，无需等待 50ms
- **自然的时间控制**：预测帧在 Tick() 内部已有时间戳判断，无需外部强制
- **简单高效**：持续轮询 + 1ms 休眠，平衡响应速度和 CPU 占用
- **向后兼容**：现有 LSController 逻辑完全保持，无需重构

**为什么需要短休眠（Thread.Sleep(1)）？**

**核心问题**：Tick() 内部已有时间控制，为什么还需要休眠？

```csharp
public void Tick()
{
    // 权威帧：有多少处理多少，处理完立即返回
    while (ProcessedAuthorityFrame < AuthorityFrame) { ... }
    
    // 预测帧：时间未到则跳过，立即返回
    while (currentTime >= CreationTime + (PredictionFrame + 1) * 50ms) { ... }
    
    // ← 如果没有输入且时间未到，这里直接 return
}
```

**如果不休眠：**
```csharp
// ❌ 完全不休眠的问题
while (_isRunning)
{
    _room?.Update(0);
    // ← Tick() 立即返回（没有工作）
    // ← 循环以最快速度执行（数百万次/秒）
    // ← CPU 100% 空转，其他线程饥饿
}
```

**1ms 短休眠的作用：**
```csharp
// ✅ 正确示例：1ms 短休眠
while (_isRunning)
{
    _room?.Update(0);
    Thread.Sleep(1); // 1ms 休眠
    // ✅ 降低轮询频率到 1000 次/秒
    // ✅ CPU 从 100% 降到 5-10%
    // ✅ 权威帧最多延迟 1ms（相比网络延迟 50-200ms 可忽略）
    // ✅ 预测帧不受影响（时间控制在 Tick 内部）
}
```

**三种场景分析：**

| 场景 | Tick() 行为 | 1ms 休眠影响 |
|------|-----------|-------------|
| **网络输入到达** | 处理权威帧（10ms） | 最多延迟 1ms（总延迟 11ms，可接受） |
| **预测帧时间到** | 处理预测帧（5ms） | 无影响（时间到了才执行） |
| **无输入 + 时间未到** | 立即返回（<1ms） | 避免 CPU 空转（节省 99.9% CPU） |

**Alternatives Considered**:

| 方案 | 权威帧延迟 | CPU 占用 | 复杂度 | 推荐度 |
|------|----------|---------|--------|--------|
| **1ms 短休眠** | 最高 1ms | ~5-10% | 简单 | ✅ **推荐** |
| **完全不休眠** | <1ms | ~100% | 简单 | ❌ CPU 空转 |
| **事件驱动（Wait）** | <1ms | ~2-5% | 复杂 | ⚡ 最优但复杂 |
| **50ms 固定休眠** | 最高 50ms | ~20% | 简单 | ❌ 延迟太高 |

**为什么选择 1ms 短休眠？**
1. **低延迟**：权威帧最多延迟 1ms，相比网络延迟（50-200ms）可忽略
2. **节省 CPU**：从 100% 降到 5-10%，避免其他线程饥饿
3. **预测帧不受影响**：时间控制在 Tick 内部（50ms 周期），1ms 误差 = 2%
4. **简单可靠**：无需复杂的事件驱动机制

**事件驱动方案（可选优化）**:
```csharp
private ManualResetEventSlim _wakeupEvent = new(false);

void LogicThreadLoop()
{
    while (_isRunning)
    {
        _room?.Update(0);
        _wakeupEvent.Wait(5); // 最长等待 5ms
        _wakeupEvent.Reset();
    }
}

// 主线程：有输入时唤醒
public void SetOneFrameInputs(...)
{
    _pendingInputs.Enqueue(...);
    _wakeupEvent.Set(); // 唤醒逻辑线程
}
```
**Trade-off**：延迟更低（<1ms），但增加了复杂度（需要管理事件）

### Decision 3: 全局配置开关

**在 GameApplication 添加配置**：
```csharp
public class GameApplication : MonoBehaviour
{
    [Header("逻辑线程设置")]
    [Tooltip("启用逻辑专用线程（提升性能，但增加调试难度）")]
    [SerializeField] private bool enableLogicThread = false;
    
    [Tooltip("逻辑线程帧率（推荐 20 FPS）")]
    [SerializeField] private int logicThreadTickRate = 20;
    
    public bool EnableLogicThread => enableLogicThread;
    public int LogicThreadTickRate => logicThreadTickRate;
}
```

**GameMode 使用配置**：
```csharp
public class SinglePlayerGameMode : BaseGameMode
{
    private LogicThread _logicThread;
    
    public override void StartGame(int sceneId)
    {
        // 根据全局配置决定是否启用多线程
        if (GameApplication.Instance.EnableLogicThread)
        {
            _logicThread = new LogicThread(
                MainRoom, 
                GameApplication.Instance.LogicThreadTickRate
            );
            _logicThread.Start();
            ASLogger.Instance.Info("多线程模式已启用");
        }
        else
        {
            ASLogger.Instance.Info("单线程模式（向后兼容）");
        }
    }
    
    public override void Update(float deltaTime)
    {
        if (!GameApplication.Instance.EnableLogicThread)
        {
            // 单线程模式：主线程调用
            MainRoom?.Update(deltaTime);
        }
        // 多线程模式：不调用（由 LogicThread 处理）
        
        // 主线程任务（总是执行）
        MainStage?.Update(deltaTime);
    }
}
```

**理由**：
- **集中管理**：所有配置在 GameApplication 统一管理
- **Inspector 可见**：开发者可以直观地在 Unity Inspector 中切换
- **向后兼容**：默认 `false`，不影响现有项目
- **运行时读取**：GameMode 启动时读取配置，简单直接
- **易于调试**：单线程模式下断点正常工作，多线程模式下提升性能

**Alternatives Considered**：
- **命令行参数**：需要每次启动时指定，不够方便
- **配置文件（JSON）**：需要额外文件管理，过于复杂
- **运行时切换**：需要重启游戏才能生效，增加复杂度

### Decision 4: 线程同步机制

**启动/停止控制**:
```csharp
private Thread _logicThread;
private volatile bool _isRunning;
private ManualResetEventSlim _startEvent = new ManualResetEventSlim(false);
private ManualResetEventSlim _stopEvent = new ManualResetEventSlim(false);

public void Start()
{
    _isRunning = true;
    _logicThread = new Thread(LogicThreadLoop) { Name = "AstrumLogicThread", IsBackground = true };
    _logicThread.Start();
    _startEvent.Set(); // 通知线程开始
}

public void Stop()
{
    _isRunning = false;
    _stopEvent.Wait(5000); // 等待线程退出（最多5秒）
    _logicThread = null;
}
```

**理由**:
- **ManualResetEventSlim**：轻量级同步原语，比 `Monitor.Wait/Pulse` 更高效
- **volatile bool**：确保 `_isRunning` 在多线程间可见
- **IsBackground = true**：应用退出时自动终止线程（避免阻塞退出）

### Decision 5: 输入系统线程安全化

#### 4.1 网络输入队列（ConcurrentQueue）

**当前实现（非线程安全）**:
```csharp
// 主线程（网络消息处理器）
clientSync.SetOneFrameInputs(inputs);
  → FrameBuffer.MoveForward(frame);         // 写入 MaxFrame
  → FrameBuffer.FrameInputs(frame);         // 读取 frameInputs
  → inputs.CopyTo(aFrame);                  // 写入 frameInputs[frame].Inputs

// 逻辑线程（LSController.Tick）
var inputs = FrameBuffer.FrameInputs(frame); // 读取 frameInputs
Room.FrameTick(inputs);                      // 读取 frameInputs[frame].Inputs
```

**数据竞争**:
- `FrameBuffer.MaxFrame` 被主线程写入，被逻辑线程读取
- `frameInputs[frame]` 被主线程写入，被逻辑线程读取
- `OneFrameInputs.Inputs` (Dictionary) 被主线程和逻辑线程并发访问

**新实现（线程安全）**:
```csharp
// ClientLSController
private ConcurrentQueue<(int frame, OneFrameInputs inputs)> _pendingInputs = new();

// 主线程调用（网络消息处理器）
public void SetOneFrameInputs(OneFrameInputs inputs)
{
    var inputsCopy = OneFrameInputs.Create();
    inputs.CopyTo(inputsCopy);
    _pendingInputs.Enqueue((AuthorityFrame, inputsCopy));
}

// 逻辑线程调用（Tick 开始时）
private void ProcessPendingInputs()
{
    while (_pendingInputs.TryDequeue(out var pending))
    {
        FrameBuffer.MoveForward(pending.frame);
        var aFrame = FrameBuffer.FrameInputs(pending.frame);
        pending.inputs.CopyTo(aFrame);
        OneFrameInputs.Recycle(pending.inputs); // 对象池回收
    }
}

public void Tick()
{
    // 【新增】消费输入队列（将网络输入写入 FrameBuffer）
    ProcessPendingInputs();
    
    // 【保持不变】原有逻辑：
    // 第一步：处理权威帧插值（从 ProcessedAuthorityFrame 推进到 AuthorityFrame）
    while (ProcessedAuthorityFrame < AuthorityFrame)
    {
        ++ProcessedAuthorityFrame;
        var authorityInputs = FrameBuffer.FrameInputs(ProcessedAuthorityFrame); // 读取网络输入
        Room.MainWorld.CurFrame = ProcessedAuthorityFrame;
        Room.FrameTick(authorityInputs); // 更新权威世界
        CheckAndRollbackShadow(ProcessedAuthorityFrame); // 比对影子世界
    }
    
    // 第二步：处理预测帧（基于时间戳和本地输入）
    while (currentTime >= CreationTime + (PredictionFrame + 1) * LSConstValue.UpdateInterval)
    {
        ++PredictionFrame;
        var oneFrameInputs = _inputSystem.GetOneFrameMessages(PredictionFrame); // 本地输入
        BackupShadowInput(PredictionFrame, oneFrameInputs);
        SimulateShadow(PredictionFrame, oneFrameInputs); // 更新影子世界
        RecordShadowFrameHash(PredictionFrame);
    }
}
```

**理由**:
- **无锁设计**：`ConcurrentQueue` 是无锁的，性能优于加锁方案
- **生产者-消费者模式**：主线程（网络）生产输入，逻辑线程消费输入
- **单一写入者**：FrameBuffer 仅在逻辑线程写入，无竞争
- **对象池友好**：输入对象可以在消费后回收

**Alternatives Considered**:
- **加锁方案**：对 `SetOneFrameInputs` 和 `FrameInputs` 加锁
  - 缺点：性能较差，可能阻塞网络线程
  - 优点：实现简单，无需队列
- **双缓冲方案**：为 FrameBuffer 实现双缓冲
  - 缺点：复杂度高，内存占用大（帧数据很大）
  - 优点：完全无锁

#### 4.2 本地输入原子交换（Interlocked.Exchange）

**当前实现（非线程安全）**:
```csharp
// 主线程（InputManager.Update()）
var input = LSInputAssembler.AssembleFromRawInput(...);
clientSync.SetPlayerInput(playerId, input);
  → _inputSystem.ClientInput = input;  // 直接赋值引用

// 逻辑线程（GetOneFrameMessages）
predictionFrame.Inputs[MainPlayerId] = ClientInput; // 读取
ClientInput.Frame = frame;                          // 修改

// 逻辑线程（Tick）
_inputSystem.ClientInput.Frame = PredictionFrame;   // 修改
```

**数据竞争**:
- `ClientInput` 被主线程写入（赋值引用）
- `ClientInput` 被逻辑线程读取和修改（Frame, PlayerId）
- 同一对象被并发访问

**新实现（线程安全）**:
```csharp
// ClientLSController
private LSInput _clientInputBuffer = new LSInput();

// 主线程调用（InputManager.Update()）
public void SetPlayerInput(long playerId, LSInput input)
{
    if (input != null)
    {
        input.PlayerId = playerId;
        PlayerId = playerId;
        // 原子交换：新输入替换旧输入
        Interlocked.Exchange(ref _clientInputBuffer, input);
    }
}

// 逻辑线程调用（GetOneFrameMessages）
public OneFrameInputs GetOneFrameMessages(int frame)
{
    OneFrameInputs predictionFrame = FrameBuffer.FrameInputs(frame);
    if (MainPlayerId > 0)
    {
        // 原子读取最新输入
        var currentInput = Interlocked.CompareExchange(ref _clientInputBuffer, null, null);
        if (currentInput != null)
        {
            currentInput.Frame = frame; // 安全修改（逻辑线程独占）
            predictionFrame.Inputs[MainPlayerId] = currentInput;
        }
    }
    return predictionFrame;
}
```

**理由**:
- **原子操作**：`Interlocked.Exchange` 是无锁原子操作，性能极高
- **单值语义**：本地输入只需要最新值，不需要队列（与网络输入不同）
- **无拷贝开销**：主线程每次创建新对象，逻辑线程直接使用（无需深拷贝）
- **简单直接**：比 ConcurrentQueue 更轻量

**Alternatives Considered**:
- **ConcurrentQueue 方案**：类似网络输入
  - 缺点：本地输入只需要最新值，队列是多余的；每帧都要 Enqueue/Dequeue
  - 优点：与网络输入统一
- **加锁方案**：对 `ClientInput` 加 lock
  - 缺点：性能差，每帧都要加锁/解锁
  - 优点：实现简单
- **深拷贝方案**：每次 SetPlayerInput 都拷贝
  - 缺点：额外 GC 开销
  - 优点：完全隔离

### Decision 6: ViewRead Swap 在逻辑线程执行

**当前实现**（主线程）:
```csharp
// GameMode.Update() in Unity main thread
Room.Update(deltaTime); // 调用 LSController.Tick()
// → Room.FrameTick()
// → World.LogicUpdate() (帧末)
// → ViewReadFrameSync.SwapBuffers() (帧末)
```

**新实现**（逻辑线程）:
```csharp
// LogicThread.Loop()
Room.Update(targetDeltaTime);
// → LSController.Tick()
// → Room.FrameTick()
// → World.LogicUpdate() (帧末，逻辑线程)
// → ViewReadFrameSync.SwapBuffers() (帧末，逻辑线程)
```

**理由**:
- **无锁设计**：swap 操作只是交换引用（一个原子操作），无需加锁
- **帧一致性**：swap 在逻辑帧末执行，确保 View 读取的是完整的逻辑帧快照
- **性能最优**：无锁，无等待，无争用

### Decision 7: 暂停/恢复机制

**实现**:
```csharp
private volatile bool _isPaused;

public void Pause()
{
    _isPaused = true;
}

public void Resume()
{
    _isPaused = false;
}

void LogicThreadLoop()
{
    while (_isRunning)
    {
        if (_isPaused)
        {
            Thread.Sleep(10); // 暂停时休眠更长时间
            continue;
        }
        
        // ... 正常逻辑
    }
}
```

**理由**:
- **简单有效**：一个 volatile bool 即可
- **响应迅速**：每帧检查一次，延迟最多 50ms
- **节省 CPU**：暂停时休眠，不占用 CPU

## Risks / Trade-offs

### Risk 1: 线程同步 Bug

**风险**: ViewRead 双缓冲、事件队列虽然设计为线程安全，但实际多线程环境可能暴露隐藏 Bug

**缓解措施**:
1. 现有 `refactor-ecc-viewread-snapshots` 和 `refactor-entity-eventqueue-thread-safe` 已经过单元测试
2. 提供配置开关 `LogicThreadingEnabled`，默认关闭，逐步启用
3. 添加压力测试，长时间运行（1小时+）验证稳定性
4. 添加 ThreadSanitizer 或类似工具检测数据竞争

### Risk 2: 调试难度增加

**风险**: 多线程环境下断点调试和日志追踪更复杂

**缓解措施**:
1. 为逻辑线程设置独立名称 `"AstrumLogicThread"`
2. 所有日志包含 `Thread.CurrentThread.ManagedThreadId`
3. 使用 Unity Profiler 的 Timeline 视图查看线程活动
4. 提供单线程模式作为调试后备

### Risk 3: Unity API 限制

**风险**: Unity 的大部分 API 只能在主线程调用（如 `GameObject`, `Transform`, `MonoBehaviour`）

**缓解措施**:
1. Logic 层已经完全隔离，不依赖 Unity API（仅使用 TrueSync FP Math）
2. View 层继续在主线程运行，负责调用 Unity API
3. 如果 Logic 需要访问 Unity API，通过事件队列发送到主线程处理

### Trade-off 1: 内存占用增加

**Trade-off**: ViewRead 双缓冲需要额外内存（2倍组件数据）

**评估**: 对于 100 个实体 × 10 个组件 × 100 bytes/组件 = 100KB，可接受

### Trade-off 2: 逻辑帧延迟增加

**Trade-off**: View 读取的是上一帧的逻辑状态（1帧延迟，50ms）

**评估**:
- **网络延迟 >> 1帧延迟**：网络延迟通常 50-200ms，1帧延迟几乎不可察觉
- **客户端预测已有延迟**：预测帧本身就领先权威帧 2-3 帧，View 读取快照的延迟被预测机制掩盖

## Migration Plan

### Phase 1: 基础结构（本 Change）

1. 创建 `LogicThread` 类
2. 修改 `GameMode` 集成 `LogicThread`
3. 添加配置开关 `LogicThreadingEnabled`（默认 `false`）
4. 单元测试：线程生命周期、暂停/恢复

### Phase 2: 功能验证

1. 在开发环境启用多线程模式
2. 运行集成测试（单人游戏、多人游戏、回放）
3. 修复发现的线程安全问题

### Phase 3: 性能优化

1. 使用 Unity Profiler 测量 CPU 利用率
2. 优化热点路径（如有必要）
3. 调整逻辑帧率（如 20Hz → 30Hz）

### Phase 4: 生产发布

1. 在测试服启用多线程模式，收集玩家反馈
2. 修复稳定性问题
3. 默认启用多线程模式

### Rollback Plan

如果发现严重问题，立即将 `LogicThreadingEnabled` 设为 `false`，回退到单线程模式。所有代码都向后兼容。

## Open Questions

1. **逻辑帧率是否需要动态调整？**
   - 当前固定 20Hz，是否需要根据负载动态调整（如 10Hz - 30Hz）？
   - **决策**: 暂不需要，20Hz 已足够

2. **是否需要支持多个逻辑线程？**
   - 如房间 1 在线程 1，房间 2 在线程 2
   - **决策**: 暂不需要，单局游戏单线程足够

3. **异常处理策略？**
   - 逻辑线程抛出异常时，是否应该崩溃还是尝试恢复？
   - **决策**: 记录日志并崩溃（保证问题可见），由 Unity 的崩溃报告系统捕获

4. **是否需要 Profiler 集成？**
   - 在逻辑线程内使用 Unity Profiler API
   - **决策**: 是，使用 `Profiler.BeginSample` / `Profiler.EndSample`（线程安全）

