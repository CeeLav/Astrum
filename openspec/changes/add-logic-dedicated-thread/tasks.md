# Implementation Tasks: 逻辑专用线程

## 1. 输入系统线程安全化

### 1.1 网络输入队列（ConcurrentQueue）

- [ ] 1.1.1 修改 `ClientLSController` 网络输入处理
  - [ ] 1.1.1.1 添加 `ConcurrentQueue<(int frame, OneFrameInputs inputs)> _pendingInputs` 字段
  - [ ] 1.1.1.2 修改 `SetOneFrameInputs()` 方法：复制输入并加入队列（不再直接写 FrameBuffer）
  - [ ] 1.1.1.3 实现 `ProcessPendingInputs()` 私有方法：消费队列并写入 FrameBuffer
  - [ ] 1.1.1.4 在 `Tick()` 开始时调用 `ProcessPendingInputs()`（其他逻辑保持不变）
  - [ ] 1.1.1.5 添加对象池支持（OneFrameInputs 回收）

### 1.2 本地输入原子交换（Interlocked.Exchange）

- [ ] 1.2.1 修改 `ClientLSController` 本地输入处理
  - [ ] 1.2.1.1 将 `_inputSystem.ClientInput` 改为 `_clientInputBuffer`（私有字段）
  - [ ] 1.2.1.2 修改 `SetPlayerInput()` 方法：使用 `Interlocked.Exchange(ref _clientInputBuffer, input)`
  - [ ] 1.2.1.3 添加 `using System.Threading;`

- [ ] 1.2.2 修改 `LSInputSystem` 本地输入读取
  - [ ] 1.2.2.1 修改 `GetOneFrameMessages()` 方法：使用 `Interlocked.CompareExchange` 原子读取
  - [ ] 1.2.2.2 移除对 `ClientInput` 的直接引用，改为通过 LSController 访问
  - [ ] 1.2.2.3 确保读取后的输入对象仅在逻辑线程修改

- [ ] 1.2.3 修改 `ClientLSController.Tick()` 中的 ClientInput 访问
  - [ ] 1.2.3.1 将 `_inputSystem.ClientInput` 改为 `_clientInputBuffer` 的原子读取
  - [ ] 1.2.3.2 确保 Frame 和 PlayerId 的修改在原子读取之后

### 1.3 验证输入系统单线程访问

- [ ] 1.3.1 确认 `FrameBuffer.FrameInputs()` 仅在逻辑线程调用
- [ ] 1.3.2 确认 `FrameBuffer.MoveForward()` 仅在逻辑线程调用
- [ ] 1.3.3 确认 `GetOneFrameMessages()` 仅在逻辑线程调用
- [ ] 1.3.4 添加线程 ID 断言（Debug 模式）
- [ ] 1.3.5 添加注释说明：Tick() 内部逻辑不变（权威帧/预测帧/影子世界）

## 2. 创建 LogicThread 核心类

- [ ] 2.1 创建 `AstrumLogic/Core/LogicThread.cs`
  - [ ] 2.1.1 定义 LogicThread 类结构（字段、属性）
  - [ ] 2.1.2 实现构造函数（接受 Room 和 TickRate）
  - [ ] 2.1.3 实现 Start() 方法（创建并启动线程）
  - [ ] 2.1.4 实现 Stop() 方法（发送停止信号并等待退出）
  - [ ] 2.1.5 实现 Pause() 和 Resume() 方法
  - [ ] 2.1.6 实现 LogicThreadLoop() 私有方法（固定时间步长循环）
    - 调用 Room.Update(deltaTime)
    - Room.Update() 内部调用 LSController.Tick()
    - Tick() 内部逻辑保持不变（权威帧/预测帧/影子世界）
  - [ ] 2.1.7 添加异常捕获和日志记录
  - [ ] 2.1.8 添加线程同步机制（ManualResetEventSlim, volatile bool）

## 3. 集成到 GameMode

- [ ] 3.1 修改 BaseGameMode
  - [ ] 3.1.1 添加 `LogicThread` 字段（private）
  - [ ] 3.1.2 添加 `IsLogicThreadEnabled` 属性（从 GameApplication 读取）
  - [ ] 3.1.3 修改 Update() 方法：根据配置决定是否调用 Room.Update()
    ```csharp
    public override void Update(float deltaTime)
    {
        if (!IsLogicThreadEnabled)
        {
            // 单线程模式：在主线程调用
            MainRoom?.Update(deltaTime);
        }
        // 多线程模式：不在主线程调用（由 LogicThread 处理）
        
        // 主线程任务（Input, UI, View）
        MainStage?.Update(deltaTime);
    }
    ```
  
- [ ] 3.2 修改 SinglePlayerGameMode
  - [ ] 3.2.1 在 StartGame() 中根据配置决定是否创建 LogicThread
    ```csharp
    if (GameApplication.Instance.EnableLogicThread)
    {
        _logicThread = new LogicThread(MainRoom, GameApplication.Instance.LogicThreadTickRate);
        _logicThread.Start();
        ASLogger.Instance.Info("SinglePlayerGameMode: 逻辑线程已启动");
    }
    else
    {
        ASLogger.Instance.Info("SinglePlayerGameMode: 单线程模式");
    }
    ```
  - [ ] 3.2.2 在 Shutdown() 中停止并销毁 LogicThread（如果存在）
  - [ ] 3.2.3 添加 Pause/Resume 游戏时同步 LogicThread 状态
  - [ ] 3.2.4 添加日志记录和错误处理
  
- [ ] 3.3 修改 MultiplayerGameMode（同 SinglePlayerGameMode）
  - [ ] 3.3.1 在 StartGame() 中创建并启动 LogicThread
  - [ ] 3.3.2 在 Shutdown() 中停止并销毁 LogicThread
  
- [ ] 3.4 修改 ReplayGameMode（回放模式暂不支持多线程）
  - [ ] 3.4.1 添加检查：如果启用多线程则记录警告
  - [ ] 3.4.2 强制在主线程运行回放逻辑

## 4. 配置系统

- [ ] 4.1 在 GameApplication 添加全局开关
  - [ ] 4.1.1 添加 `[SerializeField] private bool enableLogicThread = false;` 字段
  - [ ] 4.1.2 添加 `public bool EnableLogicThread => enableLogicThread;` 公共属性
  - [ ] 4.1.3 在 Inspector 中添加 Header 和 Tooltip 说明
    ```csharp
    [Header("逻辑线程设置")]
    [Tooltip("启用逻辑专用线程（提升性能，但增加调试难度）")]
    [SerializeField] private bool enableLogicThread = false;
    
    [Tooltip("逻辑线程帧率（推荐 20 FPS）")]
    [SerializeField] private int logicThreadTickRate = 20;
    ```
  - [ ] 4.1.4 添加运行时检查：多线程模式下禁用 Unity Debugger 断点警告

- [ ] 4.2 GameMode 集成配置
  - [ ] 4.2.1 在 BaseGameMode 中读取 `GameApplication.Instance.EnableLogicThread`
  - [ ] 4.2.2 根据配置决定是否创建 LogicThread
  - [ ] 4.2.3 添加日志：记录当前使用的模式（单线程 / 多线程）

- [ ] 4.3 添加运行时切换（可选）
  - [ ] 4.3.1 添加命令行参数支持 `--enable-logic-thread`
  - [ ] 4.3.2 添加运行时 API：`GameApplication.SetLogicThreadEnabled(bool)` （仅在游戏未运行时）
  - [ ] 4.3.3 添加调试菜单项（Unity Editor）

## 5. ViewRead 同步验证

- [ ] 5.1 验证 ViewReadFrameSync.SwapBuffers() 在逻辑线程调用
  - [ ] 5.1.1 确认 World.LogicUpdate() 在逻辑帧末调用 SwapBuffers()
  - [ ] 5.1.2 添加线程 ID 日志验证 swap 在逻辑线程执行
  - [ ] 5.1.3 添加性能计数器记录 swap 耗时

- [ ] 5.2 验证 ViewRead 读取无竞争
  - [ ] 5.2.1 在 ViewComponent.SyncDataFromComponent() 中添加线程 ID 日志
  - [ ] 5.2.2 确认 View 层读取都在主线程

## 6. 异常处理和日志

- [ ] 6.1 LogicThread 异常处理
  - [ ] 6.1.1 捕获 LogicThreadLoop() 中的所有异常
  - [ ] 6.1.2 记录异常堆栈到 ASLogger（级别：Error）
  - [ ] 6.1.3 设置 _hasError 标志
  - [ ] 6.1.4 通知主线程（通过事件或回调）

- [ ] 6.2 主线程错误处理
  - [ ] 6.2.1 在 GameMode.Update() 中检查 LogicThread 错误状态
  - [ ] 6.2.2 如果发生错误，暂停游戏并显示错误提示
  - [ ] 6.2.3 提供重试或回退到单线程模式的选项

- [ ] 6.3 日志增强
  - [ ] 6.3.1 所有 LogicThread 日志添加 `[LogicThread]` 前缀
  - [ ] 6.3.2 记录线程 ID 和帧号
  - [ ] 6.3.3 记录关键事件（Start, Stop, Pause, Resume, Error）

## 7. 单元测试

- [ ] 7.1 输入系统测试
  - [ ] 7.1.1 测试网络输入队列
    - 测试主线程写入、逻辑线程读取
    - 测试高频输入（每帧多次调用 SetOneFrameInputs）
    - 测试输入顺序保证（FIFO）
    - 测试对象池回收
  - [ ] 7.1.2 测试本地输入原子交换
    - 测试主线程写入、逻辑线程读取
    - 测试高频输入（每帧多次调用 SetPlayerInput）
    - 测试最新值语义（只保留最新输入）
    - 测试 Interlocked 原子性

- [ ] 7.2 LogicThread 生命周期测试
  - [ ] 7.2.1 测试 Start() 和 Stop() 正常流程
  - [ ] 7.2.2 测试重复调用 Start() 的行为（应抛出异常）
  - [ ] 7.2.3 测试 Stop() 超时处理
  - [ ] 7.2.4 测试 Pause() 和 Resume()

- [ ] 7.3 固定时间步长测试
  - [ ] 7.3.1 验证帧率稳定在 20 FPS ± 5%
  - [ ] 7.3.2 验证帧间隔标准差 < 5ms
  - [ ] 7.3.3 模拟高负载（逻辑耗时 > 50ms）时的行为

- [ ] 7.4 线程安全测试
  - [ ] 7.4.1 使用 ThreadSanitizer 检测数据竞争
  - [ ] 7.4.2 压力测试：1000 帧连续运行无错误
  - [ ] 7.4.3 测试 ViewRead 读写并发
  - [ ] 7.4.4 测试输入队列并发读写

## 8. 集成测试

- [ ] 8.1 单人游戏模式
  - [ ] 8.1.1 启用多线程，运行完整游戏流程（创建角色 → 战斗 → 结束）
  - [ ] 8.1.2 验证逻辑帧率稳定
  - [ ] 8.1.3 验证 View 表现正常（动画、特效、音效）
  - [ ] 8.1.4 验证输入响应正常

- [ ] 8.2 多人游戏模式
  - [ ] 8.2.1 启用多线程，运行多人对战流程
  - [ ] 8.2.2 验证网络同步正常
  - [ ] 8.2.3 验证网络输入队列正常
  - [ ] 8.2.4 验证状态回滚正常

- [ ] 8.3 回放模式（单线程）
  - [ ] 8.3.1 验证回放模式强制使用单线程
  - [ ] 8.3.2 验证回放逻辑正常

- [ ] 8.4 暂停/恢复测试
  - [ ] 8.4.1 游戏运行中暂停，验证逻辑线程停止更新
  - [ ] 8.4.2 恢复游戏，验证逻辑线程继续更新
  - [ ] 8.4.3 验证 View 表现与暂停状态一致

## 9. 性能测试

- [ ] 9.1 CPU 利用率测试
  - [ ] 9.1.1 使用 Unity Profiler 测量单线程 vs 多线程 CPU 占用
  - [ ] 9.1.2 验证多线程模式 CPU 利用率提升 20-30%
  - [ ] 9.1.3 测量主线程和逻辑线程的独立 CPU 时间

- [ ] 9.2 帧率稳定性测试
  - [ ] 9.2.1 在不同渲染负载下（低特效 vs 高特效）测量逻辑帧率
  - [ ] 9.2.2 验证逻辑帧率不受渲染帧率影响
  - [ ] 9.2.3 记录帧率统计数据（平均值、标准差、最大/最小值）

- [ ] 9.3 延迟测试
  - [ ] 9.3.1 测量输入到 Logic 的延迟（Input → InputQueue → Logic）
  - [ ] 9.3.2 测量 Logic 到 View 的延迟（Logic Update → ViewRead Swap → View Read）
  - [ ] 9.3.3 验证总延迟 < 100ms（2帧）

## 10. 压力测试

- [ ] 10.1 长时间运行测试
  - [ ] 10.1.1 多线程模式运行 1 小时无崩溃
  - [ ] 10.1.2 内存占用稳定（无内存泄漏，包括输入队列对象池）
  - [ ] 10.1.3 CPU 占用稳定（无异常飙升）

- [ ] 10.2 高负载测试
  - [ ] 10.2.1 场景中生成 100+ 实体
  - [ ] 10.2.2 验证逻辑帧率保持稳定或合理降级
  - [ ] 10.2.3 验证无崩溃和数据错误

## 11. 文档和工具

- [ ] 11.1 更新开发文档
  - [ ] 11.1.1 在 `Docs/` 中添加多线程架构说明
  - [ ] 11.1.2 添加调试指南（如何在多线程模式下调试）
  - [ ] 11.1.3 添加性能优化建议
  - [ ] 11.1.4 添加输入队列机制说明

- [ ] 11.2 编辑器工具
  - [ ] 11.2.1 添加 LogicThread 状态监控面板（EditorWindow）
  - [ ] 11.2.2 显示逻辑帧率、CPU占用、线程状态、输入队列长度
  - [ ] 11.2.3 提供手动 Pause/Resume 按钮

- [ ] 11.3 Profiler 标记
  - [ ] 11.3.1 在 LogicThread 关键路径添加 Profiler.BeginSample/EndSample
  - [ ] 11.3.2 在 ProcessPendingInputs 添加 Profiler 标记
  - [ ] 11.3.3 添加自定义 Profiler Counter（如 LogicFrameTime, LogicFPS, InputQueueLength）

## 12. 验证和归档

- [ ] 12.1 编译验证
  - [ ] 12.1.1 `dotnet build AstrumProj.sln` 无错误
  - [ ] 12.1.2 Unity 编译无错误

- [ ] 12.2 代码审查
  - [ ] 12.2.1 确认线程安全机制正确（输入队列、ViewRead、事件队列）
  - [ ] 12.2.2 确认异常处理完善
  - [ ] 12.2.3 确认日志记录充分

- [ ] 12.3 文档验证
  - [ ] 12.3.1 所有 spec 场景都有对应测试
  - [ ] 12.3.2 design.md 的决策都已实施
  - [ ] 12.3.3 更新 QUICK_START.md 和 README.md（如有必要）

- [ ] 12.4 归档准备
  - [ ] 12.4.1 运行 `openspec validate add-logic-dedicated-thread --strict`
  - [ ] 12.4.2 修复所有验证错误
  - [ ] 12.4.3 标记所有任务为完成

