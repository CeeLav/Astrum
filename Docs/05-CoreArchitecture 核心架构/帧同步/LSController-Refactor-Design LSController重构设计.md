# LSController 重构设计

> 📖 **版本**: v1.0 | 📅 **最后更新**: 2025-01-27  
> 👥 **面向读者**: 客户端/服务器帧同步开发人员  
> 🎯 **目标**: 通过接口隔离和组合模式分离客户端和服务器的帧同步逻辑

**TL;DR**
- 当前 `LSController` 混合了客户端预测和服务器权威逻辑，导致接口臃肿
- 采用**接口隔离原则**：基础接口 + 客户端接口 + 服务器接口
- 实现分离：`ClientLSController` 和 `ServerLSController` 各自实现需要的接口
- **Room 创建时指定 LSController 类型**，统一使用 `Tick()` 方法进行更新
- 优势：类型安全、职责清晰、易于扩展、避免接口臃肿

---

## 1. 概述

当前 `LSController` 同时承担客户端预测帧同步和服务器权威帧同步的职责，导致：

1. **接口臃肿**：客户端代码看到大量服务器专用接口（如 `CollectFrameInputs`），服务器代码看到客户端专用接口（如 `Rollback`、`PredictionFrame`）
2. **职责混乱**：`Tick()` 方法包含客户端预测逻辑（RTT补偿、预测帧推进），服务器不需要这些
3. **维护困难**：修改客户端逻辑可能影响服务器，反之亦然

**重构目标**：
- 分离客户端和服务器的帧同步逻辑
- 通过接口隔离避免接口臃肿
- Room 创建时指定 LSController 类型，统一使用 `Tick()` 更新
- 提供类型安全的接口访问

**设计理念**：
- **接口隔离原则**：客户端和服务器只看到需要的接口
- **组合模式**：通过接口组合而非单一基类
- **类型安全**：编译期即可区分客户端/服务器代码
- **统一更新接口**：客户端和服务器都使用 `Tick()` 方法，但实现逻辑不同

**系统边界**：
- ✅ 负责：帧同步控制器的接口设计和实现分离
- ❌ 不负责：帧同步协议、网络通信、状态快照存储

---

## 2. 当前问题分析

### 2.1 客户端使用场景

**客户端需要的接口**：
- `PredictionFrame` / `MaxPredictionFrames` - 预测帧管理
- `Tick()` - 客户端预测更新（包含 RTT 补偿）
- `SetPlayerInput()` - 设置本地玩家输入
- `SetOneFrameInputs()` - 处理服务器广播的帧输入（包含回滚逻辑）
- `Rollback()` - 回滚到权威帧
- `GetCurrentPredictionFrameTime()` - 获取预测帧时间
- `FrameBuffer` - 帧缓冲区访问
- `SaveState()` / `LoadState()` - 状态快照（用于回滚）

**客户端不需要的接口**：
- `CollectFrameInputs()` - 服务器收集所有玩家输入
- `ProcessAuthorityFrame()` - 服务器权威帧推进

### 2.2 服务器使用场景

**服务器需要的接口**：
- `AuthorityFrame` - 权威帧管理
- `Tick()` - 服务器权威帧更新（推进权威帧并执行逻辑）
- `CollectFrameInputs()` - 收集所有玩家的输入
- `AddPlayerInput()` - 添加玩家输入到缓存
- `FrameBuffer` - 帧缓冲区访问（用于状态快照）
- `SaveState()` / `LoadState()` - 状态快照（用于重连/回放）

**服务器不需要的接口**：
- `PredictionFrame` / `MaxPredictionFrames` - 客户端预测
- `SetPlayerInput()` - 客户端输入设置
- `SetOneFrameInputs()` - 客户端回滚逻辑
- `Rollback()` - 客户端回滚
- `GetCurrentPredictionFrameTime()` - 客户端预测时间

### 2.3 共同功能

**客户端和服务器都需要**：
- `FrameBuffer` - 帧缓冲区
- `AuthorityFrame` - 权威帧（客户端读取，服务器写入）
- `CreationTime` - 创建时间
- `IsRunning` - 运行状态
- `Tick()` - 更新方法（客户端预测更新，服务器权威更新）
- `Start()` / `Stop()` - 启动/停止
- `SaveState()` / `LoadState()` - 状态快照
- `Room` - 所属房间

---

## 3. 架构设计

### 3.1 接口层次结构

```
ILSControllerBase (基础接口)
├── 共同功能：FrameBuffer、AuthorityFrame、CreationTime、IsRunning、Tick()、Start/Stop、SaveState/LoadState
│
├── IClientFrameSync (客户端接口)
│   ├── PredictionFrame { get; set; }
│   ├── MaxPredictionFrames { get; set; }
│   ├── Tick() // 客户端预测更新（RTT补偿、预测帧推进）
│   ├── SetPlayerInput(long playerId, LSInput input)
│   ├── SetOneFrameInputs(OneFrameInputs inputs) // 包含回滚逻辑
│   ├── Rollback(int frame)
│   └── GetCurrentPredictionFrameTime()
│
└── IServerFrameSync (服务器接口)
    ├── Tick() // 服务器权威帧更新（推进权威帧、执行逻辑、广播）
    ├── CollectFrameInputs(int frame) // 收集所有玩家输入
    └── AddPlayerInput(int frame, long playerId, LSInput input) // 添加玩家输入
```

### 3.2 实现类

```
ClientLSController : ILSControllerBase, IClientFrameSync
├── 实现客户端预测逻辑
├── Tick() 包含 RTT 补偿、预测帧推进
└── 包含回滚和状态验证逻辑

ServerLSController : ILSControllerBase, IServerFrameSync
├── 实现服务器权威帧推进
├── Tick() 推进权威帧、收集输入、执行逻辑、广播结果
└── 收集所有玩家输入（从缓存中）
```

### 3.3 架构图

```
┌─────────────────────────────────────────────────────────────┐
│                         Room                                 │
│  LSController: ILSControllerBase                            │
└──────────────────┬──────────────────────────────────────────┘
                   │
        ┌──────────┴──────────┐
        │                     │
        ▼                     ▼
┌─────────────────┐   ┌─────────────────┐
│ ClientLSController │   │ ServerLSController │
│ : ILSControllerBase│   │ : ILSControllerBase│
│ : IClientFrameSync │   │ : IServerFrameSync │
└─────────────────┘   └─────────────────┘
        │                     │
        │                     │
        ▼                     ▼
┌─────────────────┐   ┌─────────────────┐
│ 客户端预测逻辑    │   │ 服务器权威逻辑    │
│ - 预测帧推进     │   │ - 权威帧推进     │
│ - RTT补偿       │   │ - 输入收集       │
│ - 回滚机制      │   │ - 状态快照       │
└─────────────────┘   └─────────────────┘
```

---

## 4. 接口定义

### 4.1 ILSControllerBase（基础接口）

```csharp
/// <summary>
/// 帧同步控制器基础接口 - 包含客户端和服务器共同需要的功能
/// </summary>
public interface ILSControllerBase
{
    /// <summary>
    /// 所属房间
    /// </summary>
    Room Room { get; set; }
    
    /// <summary>
    /// 权威帧（客户端读取，服务器写入）
    /// </summary>
    int AuthorityFrame { get; set; }
    
    /// <summary>
    /// 帧缓冲区
    /// </summary>
    FrameBuffer FrameBuffer { get; }
    
    /// <summary>
    /// 创建时间（毫秒）
    /// </summary>
    long CreationTime { get; set; }
    
    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// 是否暂停
    /// </summary>
    bool IsPaused { get; set; }
    
    /// <summary>
    /// 帧率（如60FPS）
    /// </summary>
    int TickRate { get; set; }
    
    /// <summary>
    /// 更新帧同步（客户端预测更新或服务器权威更新）
    /// </summary>
    void Tick();
    
    /// <summary>
    /// 启动控制器
    /// </summary>
    void Start();
    
    /// <summary>
    /// 停止控制器
    /// </summary>
    void Stop();
    
    /// <summary>
    /// 保存当前帧状态
    /// </summary>
    void SaveState();
    
    /// <summary>
    /// 加载指定帧的状态
    /// </summary>
    World LoadState(int frame);
}
```

### 4.2 IClientFrameSync（客户端接口）

```csharp
/// <summary>
/// 客户端帧同步接口 - 包含客户端预测和回滚功能
/// </summary>
public interface IClientFrameSync : ILSControllerBase
{
    /// <summary>
    /// 预测帧
    /// </summary>
    int PredictionFrame { get; set; }
    
    /// <summary>
    /// 最大预测帧数
    /// </summary>
    int MaxPredictionFrames { get; set; }
    
    /// <summary>
    /// 客户端预测更新（包含 RTT 补偿和预测帧推进）
    /// </summary>
    void Tick();
    
    /// <summary>
    /// 设置玩家输入
    /// </summary>
    void SetPlayerInput(long playerId, LSInput input);
    
    /// <summary>
    /// 设置服务器广播的帧输入（包含回滚逻辑）
    /// </summary>
    void SetOneFrameInputs(OneFrameInputs inputs);
    
    /// <summary>
    /// 回滚到指定帧
    /// </summary>
    void Rollback(int frame);
    
    /// <summary>
    /// 获取当前预测帧对应的时间
    /// </summary>
    long GetCurrentPredictionFrameTime();
    
    /// <summary>
    /// 获取指定预测帧对应的时间
    /// </summary>
    long GetPredictionFrameTime(int predictionFrame);
}
```

### 4.3 IServerFrameSync（服务器接口）

```csharp
/// <summary>
/// 服务器帧同步接口 - 包含服务器权威帧推进功能
/// </summary>
public interface IServerFrameSync : ILSControllerBase
{
    /// <summary>
    /// 服务器权威帧更新（Tick() 方法实现）
    /// 推进权威帧、收集输入、执行逻辑、广播结果
    /// </summary>
    // Tick() 已在 ILSControllerBase 中定义，这里通过注释说明服务器实现
    
    /// <summary>
    /// 添加玩家输入到缓存
    /// </summary>
    /// <param name="frame">帧号</param>
    /// <param name="playerId">玩家ID</param>
    /// <param name="input">输入数据</param>
    void AddPlayerInput(int frame, long playerId, LSInput input);
    
    /// <summary>
    /// 收集指定帧的所有玩家输入（从输入缓存中）
    /// </summary>
    /// <param name="frame">帧号</param>
    /// <returns>该帧的所有玩家输入</returns>
    OneFrameInputs CollectFrameInputs(int frame);
}
```

---

## 5. 实现方案

### 5.1 ClientLSController

**职责**：
- 实现客户端预测帧同步逻辑
- 处理 RTT 补偿和预测帧推进
- 实现回滚和状态验证

**关键实现**：
```csharp
public class ClientLSController : ILSControllerBase, IClientFrameSync
{
    private LSInputSystem _inputSystem;
    public int PredictionFrame { get; set; } = -1;
    public int MaxPredictionFrames { get; set; } = 5;
    
    public void Tick()
    {
        if (!IsRunning || IsPaused || Room == null) return;
        
        // 客户端预测逻辑：RTT 补偿 + 预测帧推进
        long currentTime = TimeInfo.Instance.ServerNow() + TimeInfo.Instance.RTT / 2;
        
        while (true)
        {
            if (currentTime < CreationTime + (PredictionFrame + 1) * LSConstValue.UpdateInterval)
            {
                return;
            }
            
            if (PredictionFrame - AuthorityFrame > MaxPredictionFrames)
            {
                return;
            }
            
            ++PredictionFrame;
            
            OneFrameInputs inputs = _inputSystem.GetOneFrameMessages(PredictionFrame);
            Room.FrameTick(inputs);
            
            // 发布输入事件（客户端特有）
            if (Room.MainPlayerId > 0)
            {
                var eventData = new FrameDataUploadEventData(PredictionFrame, _inputSystem.ClientInput);
                EventSystem.Instance.Publish(eventData);
            }
        }
    }
    
    public void SetOneFrameInputs(OneFrameInputs inputs)
    {
        // 客户端回滚逻辑
        _inputSystem.FrameBuffer.MoveForward(AuthorityFrame);
        
        if (AuthorityFrame > PredictionFrame)
        {
            // 服务器帧超前，直接覆盖
            var aFrame = FrameBuffer.FrameInputs(AuthorityFrame);
            inputs.CopyTo(aFrame);
        }
        else
        {
            // 检查输入是否一致，不一致则回滚
            var pFrame = FrameBuffer.FrameInputs(AuthorityFrame);
            if (!inputs.Equal(pFrame))
            {
                Rollback(AuthorityFrame);
            }
        }
        
        var af = _inputSystem.FrameBuffer.FrameInputs(AuthorityFrame);
        inputs.CopyTo(af);
    }
    
    public void Rollback(int frame)
    {
        // 回滚实现
        var loadedWorld = LoadState(frame);
        if (loadedWorld == null) return;
        
        Room.MainWorld.Cleanup();
        Room.MainWorld = loadedWorld;
        
        var aInput = FrameBuffer.FrameInputs(frame);
        Room.FrameTick(aInput);
        
        // 重放预测帧
        for (int i = AuthorityFrame + 1; i <= PredictionFrame; ++i)
        {
            var pInput = FrameBuffer.FrameInputs(i);
            CopyOtherInputsTo(aInput, pInput);
            Room.FrameTick(pInput);
        }
    }
}
```

### 5.2 ServerLSController

**职责**：
- 实现服务器权威帧同步逻辑
- 收集所有玩家输入
- 推进权威帧并执行逻辑

**关键实现**：
```csharp
public class ServerLSController : ILSControllerBase, IServerFrameSync
{
    private readonly Dictionary<int, Dictionary<long, LSInput>> _frameInputs = new();
    
    /// <summary>
    /// 服务器权威帧更新（统一使用 Tick() 方法）
    /// </summary>
    public void Tick()
    {
        if (!IsRunning || IsPaused || Room == null) return;
        
        // 检查是否到达下一帧时间
        long currentTime = TimeInfo.Instance.ServerNow();
        long targetFrameTime = CreationTime + (AuthorityFrame + 1) * LSConstValue.UpdateInterval;
        
        if (currentTime < targetFrameTime)
        {
            return; // 还没到下一帧时间
        }
        
        // 推进权威帧
        AuthorityFrame++;
        
        // 收集当前帧的所有输入
        var frameInputs = CollectFrameInputs(AuthorityFrame);
        
        // 确保 FrameBuffer 已准备好
        FrameBuffer.MoveForward(AuthorityFrame);
        
        // 执行逻辑
        Room.FrameTick(frameInputs);
        
        // 广播帧数据给所有客户端（由 GameSession 处理，这里不直接发送）
        // 可以通过事件或回调通知 GameSession
    }
    
    public void AddPlayerInput(int frame, long playerId, LSInput input)
    {
        if (!_frameInputs.ContainsKey(frame))
        {
            _frameInputs[frame] = new Dictionary<long, LSInput>();
        }
        
        _frameInputs[frame][playerId] = input;
    }
    
    public OneFrameInputs CollectFrameInputs(int frame)
    {
        var frameInputs = OneFrameInputs.Create();
        
        if (_frameInputs.TryGetValue(frame, out var inputs))
        {
            foreach (var kvp in inputs)
            {
                frameInputs.Inputs[kvp.Key] = kvp.Value;
            }
        }
        
        return frameInputs;
    }
}
```

### 5.3 Room 类更新

**Room 创建时指定 LSController 类型**：
```csharp
public class Room
{
    /// <summary>
    /// 帧同步控制器（基础接口，客户端和服务器通用）
    /// </summary>
    public ILSControllerBase LSController { get; set; }
    
    /// <summary>
    /// 初始化房间
    /// </summary>
    /// <param name="controllerType">LSController 类型（"client" 或 "server"）</param>
    public virtual void Initialize(string controllerType = "client")
    {
        TotalTime = 0f;
        if (MainWorld == null)
        {
            ASLogger.Instance.Error($"Room {RoomId} has no MainWorld defined.");
        }
        MainWorld?.Initialize(0);
        
        // 根据类型创建对应的 LSController
        if (LSController == null)
        {
            if (controllerType == "server")
            {
                LSController = new ServerLSController { Room = this };
            }
            else
            {
                LSController = new ClientLSController { Room = this };
            }
        }
        
        // 初始化所有世界
        foreach (var world in Worlds)
        {
            world?.Initialize(0);
        }
    }
    
    /// <summary>
    /// 更新房间（客户端和服务器通用，统一调用 Tick()）
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!IsActive) return;
        
        TotalTime += deltaTime;
        
        // 统一使用 Tick() 方法更新（客户端预测或服务器权威）
        LSController?.Tick();
    }
    
    public void FrameTick(OneFrameInputs oneFrameInputs)
    {
        // 确保 FrameBuffer 已准备好
        if (LSController != null && LSController.AuthorityFrame >= 0)
        {
            LSController.FrameBuffer.MoveForward(LSController.AuthorityFrame);
        }
        
        // 保存状态（客户端和服务器都需要）
        LSController?.SaveState();
        
        // 处理输入并更新世界
        foreach (var pairs in oneFrameInputs.Inputs)
        {
            var input = pairs.Value;
            var entity = MainWorld.GetEntity(pairs.Key);
            if (entity != null)
            {
                var inputComponent = entity.GetComponent<LSInputComponent>();
                inputComponent?.SetInput(input);
            }
        }
        
        // 更新所有世界
        foreach (var world in Worlds)
        {
            world.Update();
        }
        
        TickSystems();
    }
}
```

---

## 6. 使用示例

### 6.1 客户端使用

```csharp
// 客户端代码 - Room 创建时指定使用 ClientLSController
public class GameMode
{
    public void InitializeRoom()
    {
        // 创建房间时指定使用客户端控制器
        MainRoom = new Room();
        MainRoom.Initialize("client"); // 指定使用 ClientLSController
        
        // 或者手动创建
        // MainRoom.LSController = new ClientLSController { Room = MainRoom };
    }
}

public class FrameSyncHandler
{
    public void OnFrameSyncStartNotification(FrameSyncStartNotification notification)
    {
        // 设置服务器时间
        if (_gameMode.MainRoom.LSController is IClientFrameSync clientSync)
        {
            clientSync.CreationTime = notification.startTime;
            clientSync.Start();
        }
    }
    
    public void OnFrameInputs(OneFrameInputs frameInputs)
    {
        if (_gameMode.MainRoom.LSController is IClientFrameSync clientSync)
        {
            clientSync.AuthorityFrame++;
            clientSync.SetOneFrameInputs(frameInputs);
        }
    }
}

// InputManager
public void Update()
{
    if (_gameMode.MainRoom.LSController is IClientFrameSync clientSync)
    {
        var input = LSInputAssembler.AssembleFromRawInput(...);
        clientSync.SetPlayerInput(playerId, input);
    }
}

// Room.Update() 统一调用 Tick()
// 客户端：Room.Update() -> LSController.Tick() -> ClientLSController.Tick() (预测更新)
```

### 6.2 服务器使用

```csharp
// 服务器代码 - Room 创建时指定使用 ServerLSController
public class GameSession
{
    private IServerFrameSync? _serverController;
    
    public void Start()
    {
        // 创建逻辑房间时指定使用服务器控制器
        LogicRoom = new Astrum.LogicCore.Core.Room();
        LogicRoom.Initialize("server"); // 指定使用 ServerLSController
        
        _serverController = LogicRoom.LSController as IServerFrameSync;
        
        if (_serverController != null)
        {
            _serverController.CreationTime = TimeInfo.Instance.ServerNow();
            _serverController.Start();
        }
    }
    
    public void Update()
    {
        // 统一使用 Tick() 方法更新
        // Room.Update() -> LSController.Tick() -> ServerLSController.Tick() (权威更新)
        LogicRoom?.Update(0.016f); // 固定帧率更新
        
        // Tick() 内部会推进权威帧、执行逻辑
        // 然后通过事件或回调通知 GameSession 广播结果
        if (_serverController != null && _serverController.IsRunning)
        {
            // 广播最新帧数据（在 Tick() 执行后）
            BroadcastLatestFrame();
        }
    }
    
    private void BroadcastLatestFrame()
    {
        if (_serverController == null) return;
        
        // 收集当前权威帧的输入
        var frameInputs = _serverController.CollectFrameInputs(_serverController.AuthorityFrame);
        
        // 广播给客户端
        SendFrameSyncData(_serverController.AuthorityFrame, frameInputs);
    }
    
    public void HandleInput(string userId, SingleInput input)
    {
        if (_serverController == null) return;
        
        var playerId = GetPlayerId(userId);
        if (playerId > 0)
        {
            // 添加输入到缓存（下一帧使用）
            _serverController.AddPlayerInput(_serverController.AuthorityFrame + 1, playerId, input.Input);
        }
    }
}
```

---

## 7. 迁移计划

### 7.1 步骤1：创建接口定义

1. 创建 `ILSControllerBase` 接口
2. 创建 `IClientFrameSync` 接口
3. 创建 `IServerFrameSync` 接口

### 7.2 步骤2：实现 ClientLSController

1. 将现有 `LSController` 重命名为 `ClientLSController`
2. 实现 `ILSControllerBase` 和 `IClientFrameSync`
3. 保留所有客户端预测逻辑

### 7.3 步骤3：实现 ServerLSController

1. 创建 `ServerLSController` 类
2. 实现 `ILSControllerBase` 和 `IServerFrameSync`
3. 简化逻辑（移除预测帧、RTT补偿等）

### 7.4 步骤4：更新 Room 类

1. 将 `Room.LSController` 类型改为 `ILSControllerBase`
2. 更新 `Room.Initialize()` 支持指定 LSController 类型（"client" 或 "server"）
3. 更新 `Room.Update()` 统一调用 `LSController.Tick()`
4. 更新 `Room.FrameTick()` 使用基础接口

### 7.5 步骤5：更新客户端代码

1. 更新 `FrameSyncHandler` 使用 `IClientFrameSync`
2. 更新 `InputManager` 使用 `IClientFrameSync`
3. 更新其他客户端调用代码

### 7.6 步骤6：更新服务器代码

1. 更新 `GameSession` 使用 `IServerFrameSync`
2. 更新服务器其他调用代码

---

## 8. 关键决策与取舍

### 8.1 为什么使用接口隔离而非单一基类？

**问题**：为什么不使用单一基类 + 虚方法？

**备选方案**：
1. 单一基类 + 虚方法（传统继承）
2. 接口隔离 + 组合模式（当前方案）

**选择**：接口隔离 + 组合模式

**原因**：
- 客户端代码不需要看到服务器接口（如 `CollectFrameInputs`）
- 服务器代码不需要看到客户端接口（如 `Rollback`）
- 编译期类型安全，避免误用
- 符合接口隔离原则（ISP）

**影响**：
- 需要显式接口转换（`is IClientFrameSync`），但提供了类型安全
- 代码更清晰，职责更明确

### 8.2 为什么 Room.LSController 保持为 ILSControllerBase？

**问题**：为什么不直接使用 `IClientFrameSync` 或 `IServerFrameSync`？

**选择**：保持为 `ILSControllerBase`，Room 创建时指定类型

**原因**：
- `Room` 类需要同时支持客户端和服务器
- `Room.FrameTick()` 和 `Room.Update()` 等共同方法只需要基础接口
- Room 创建时通过 `Initialize("client")` 或 `Initialize("server")` 指定类型
- 需要特定功能时通过接口转换访问

**影响**：
- Room 创建时明确指定类型，更清晰
- 需要接口转换访问特定功能，但提供了灵活性
- 保持了 `Room` 类的通用性

### 8.3 为什么两个 Controller 都使用 Tick()？

**问题**：为什么服务器也使用 `Tick()` 而不是 `ProcessAuthorityFrame()`？

**选择**：统一使用 `Tick()` 方法

**原因**：
- 统一接口更简洁，`Room.Update()` 可以统一调用 `LSController.Tick()`
- 客户端和服务器都通过 `Tick()` 更新，但实现逻辑不同
- 客户端 `Tick()` 包含预测逻辑（RTT补偿、预测帧推进）
- 服务器 `Tick()` 包含权威逻辑（推进权威帧、收集输入、执行逻辑）
- 符合多态设计原则，接口统一但实现不同

**影响**：
- 代码更统一，`Room.Update()` 逻辑更简洁
- 服务器和客户端都通过 `Tick()` 更新，但内部实现完全不同

---

## 9. 相关文档

- [帧同步机制](Frame-Sync-Mechanism%20帧同步机制.md) - 帧同步整体架构
- [帧同步状态同步与恢复机制设计](Frame-Sync-State-Sync-Design%20帧同步状态同步与恢复机制设计.md) - 状态快照和重连机制
- [房间系统重构设计](../../13-Server%20服务器/Room-System-Refactor-Design%20房间系统重构设计.md) - 服务器房间系统重构

---

*文档版本：v1.1*  
*创建时间：2025-01-27*  
*最后更新：2025-01-27*  
*状态：设计阶段*  
*Owner*: 帧同步开发团队  
*变更摘要*: 更新设计：Room 创建时指定 LSController 类型，统一使用 Tick() 方法更新

