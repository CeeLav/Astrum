# Change: 创建逻辑专用线程

## Why

当前 Logic 层（World, Room, LSController）与 View 层运行在同一个 Unity 主线程中，这导致：

1. **性能限制**：Logic 计算（物理、技能、AI）和 View 渲染（Animancer、特效）相互竞争 CPU 时间
2. **帧率不稳定**：Unity 渲染帧波动会直接影响逻辑帧的稳定性
3. **无法充分利用多核**：现代 CPU 的多核性能无法有效利用

我们已经完成了 Logic-View 解耦的基础设施：
- `refactor-ecc-viewread-snapshots`：View 通过不可变快照读取 Logic 数据
- `refactor-entity-eventqueue-thread-safe`：View/Client 通过线程安全事件队列向 Logic 发送命令
- `refactor-viewevent-thread-safe`：Logic 通过线程安全队列向 View 发送视觉反馈

现在是时候将 Logic 真正分离到独立线程，实现**稳定的逻辑帧率**和**更高的 CPU 利用率**。

## What Changes

### 核心变更

1. **创建 `LogicThread` 管理类**
   - 在 `GameMode.StartGame()` 时创建并启动线程
   - 在 `GameMode.Shutdown()` 时停止并销毁线程
   - 使用 `System.Threading.Thread` 直接创建（不使用 Task/ThreadPool）

2. **独立的逻辑循环**
   - 线程内部运行固定时间步长的逻辑循环（20Hz / 50ms per frame）
   - 独立调用 `Room.Update()` / `LSController.Tick()`
   - 不受 Unity 渲染帧率影响

3. **线程同步机制**
   - 使用 `ManualResetEventSlim` 控制线程启动/停止
   - 使用 `Interlocked` 进行原子操作
   - 异常捕获和日志记录

4. **输入队列线程安全化**
   - `ClientLSController.SetOneFrameInputs()` 使用 `ConcurrentQueue` 缓冲输入
   - 主线程写入队列（网络消息处理器）
   - 逻辑线程消费队列（Tick 开始时）
   - FrameBuffer 仅在逻辑线程访问（无竞争）

5. **生命周期管理**
   - 每局游戏独立创建线程（不使用线程池复用）
   - 游戏结束时等待线程完全退出
   - 支持暂停/恢复

### 影响的系统

- **GameMode**: 负责创建和销毁 `LogicThread`
- **Room**: 不再在 Unity 主线程调用 `Update()`，改为在逻辑线程调用
- **ViewReadFrameSync**: 确保双缓冲 swap 在逻辑线程帧末执行
- **EventQueue**: 已支持线程安全，无需修改
- **FrameBuffer**: **需要重构为线程安全**，使用 `ConcurrentQueue` 缓冲主线程输入

## Impact

- **影响的规范**: `logic-threading`（新增）, `game-mode`（修改）, `view-read`（验证）
- **影响的代码**: 
  - `AstrumLogic/Core/LogicThread.cs`（新增）
  - `AstrumClient/Managers/GameModes/*.cs`（修改）
  - `AstrumLogic/Core/ClientLSController.cs`（修改，输入队列）
  - `AstrumLogic/ViewRead/ViewReadFrameSync.cs`（验证）
- **破坏性变更**: 无（向后兼容，可通过配置开关启用/禁用）
- **性能影响**: 预期提升 20-30% CPU 利用率，逻辑帧率更稳定
- **测试需求**: 单元测试（线程生命周期）、集成测试（多线程场景）、压力测试（长时间运行）

