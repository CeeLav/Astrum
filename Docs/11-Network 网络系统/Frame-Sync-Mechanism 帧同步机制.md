# 🎮 帧同步机制详解

**版本**: v1.0.0  
**最后更新**: 2025-10-31

---

## 概述

Astrum使用**服务器权威帧同步**机制来实现多人游戏的确定性同步。帧同步确保所有客户端在相同的游戏帧上执行相同的逻辑，从而保证游戏状态的一致性。

---

## 设计原则

### 1. 服务器权威

- 服务器是游戏状态的唯一权威来源
- 所有关键逻辑在服务器执行
- 客户端仅负责输入收集和状态展示

### 2. 确定性逻辑

- 使用定点数（TrueSync）避免浮点数误差
- 避免非确定性随机数
- 逻辑执行顺序一致

### 3. 输入驱动

- 游戏状态完全由玩家输入驱动
- 相同的输入序列产生相同的游戏状态
- 状态不依赖时间，只依赖帧数

---

## 帧同步流程

### 完整流程

```
┌─────────────────────────────────────────────────────────┐
│                   客户端 A                               │
├─────────────────────────────────────────────────────────┤
│ 1. 收集输入 (InputManager)                              │
│ 2. 发送SingleInput到服务器                               │
│ 3. 本地预测执行 (可选)                                    │
│ 4. 接收OneFrameInputs                                   │
│ 5. 执行确定性逻辑                                        │
│ 6. 验证/纠正状态                                         │
│ 7. 更新显示                                              │
└─────────────────────────────────────────────────────────┘
                    ↓  ↑
              ┌─────┴──┴─────┐
              │    服务器     │
              └─────┬──┬─────┘
                    ↓  ↑
┌─────────────────────────────────────────────────────────┐
│                   客户端 B                               │
├─────────────────────────────────────────────────────────┤
│ 1. 收集输入 (InputManager)                              │
│ 2. 发送SingleInput到服务器                               │
│ 3. 本地预测执行 (可选)                                    │
│ 4. 接收OneFrameInputs                                   │
│ 5. 执行确定性逻辑                                        │
│ 6. 验证/纠正状态                                         │
│ 7. 更新显示                                              │
└─────────────────────────────────────────────────────────┘
```

### 单帧详细流程

```
时间轴:
T0 ─────────────────────────────────────────►

服务器:
  T0: 收集所有客户端输入 (Frame N)
  T1: 等待固定时间 (例如20ms)
  T2: 执行确定性逻辑 (Frame N)
  T3: 广播OneFrameInputs (Frame N)
  T4: 推进到Frame N+1

客户端:
  T0: 发送Frame N的输入
  T1-T2: 本地预测执行 (可选)
  T3: 接收OneFrameInputs (Frame N)
  T4: 执行确定性逻辑 (Frame N)
  T5: 显示结果
```

---

## 关键组件

### 客户端组件

#### 1. InputManager (输入管理器)

**位置**: `AstrumProj/Assets/Script/AstrumClient/Managers/InputManager.cs`

**职责**:
- 收集玩家输入（移动、技能等）
- 封装为LSInput格式
- 通过SingleInput发送到服务器

**输入收集**:
```csharp
public void CollectLSInput(long playerId)
{
    var input = new LSInput();
    
    // 收集移动输入
    input.MoveDirection = GetMoveInput();
    
    // 收集技能输入
    input.SkillId = GetSkillInput();
    
    // 发送到服务器
    var singleInput = SingleInput.Create();
    singleInput.PlayerID = playerId;
    singleInput.FrameID = currentFrame;
    singleInput.Input = input;
    
    NetworkManager.Instance.Send(singleInput);
}
```

#### 2. FrameSyncHandler (帧同步处理器)

**位置**: `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/Handlers/FrameSyncHandler.cs`

**职责**:
- 处理服务器发送的帧同步消息
- 管理帧同步控制器
- 处理帧输入数据

**关键方法**:
```csharp
// 帧同步开始
public void OnFrameSyncStartNotification(FrameSyncStartNotification notification)
{
    // 设置服务器时间
    _gameMode.MainRoom.SetServerCreationTime(notification.startTime);
    
    // 启动帧同步控制器
    _gameMode.MainRoom.LSController.Start();
}

// 处理帧输入
public void OnFrameInputs(OneFrameInputs frameInputs)
{
    // 处理所有玩家的输入
    DealNetFrameInputs(frameInputs);
}
```

#### 3. LSController (帧同步控制器)

**位置**: LogicCore中的帧同步实现

**职责**:
- 管理当前帧数
- 驱动游戏逻辑执行
- 处理输入和状态同步

### 服务器组件

#### 1. FrameSyncManager (帧同步管理器)

**位置**: `AstrumServer/AstrumServer/Managers/FrameSyncManager.cs`

**职责**:
- 管理所有房间的帧同步
- 收集客户端输入
- 定时广播帧输入
- 推进游戏帧

**关键方法**:
```csharp
// 启动房间帧同步
public void StartRoomFrameSync(string roomId)

// 收集输入
public void CollectRoomInputs(string roomId, long playerId, SingleInput input)

// 更新帧同步（定时调用）
public void Update()
```

#### 2. Room (游戏房间)

**位置**: `AstrumServer` 或 `LogicCore`

**职责**:
- 存储房间状态
- 执行游戏逻辑
- 管理玩家输入

---

## 帧同步消息

### SingleInput (客户端→服务器)

**定义**:
```protobuf
message SingleInput {
    int64 PlayerID = 1;
    int32 FrameID = 2;
    LSInput Input = 3;
}
```

**用途**: 客户端上报单帧的玩家输入

**发送时机**: 每帧收集到输入后立即发送

### OneFrameInputs (服务器→客户端)

**定义**:
```protobuf
message OneFrameInputs {
    int32 FrameID = 1;
    repeated PlayerInput Inputs = 2;
}

message PlayerInput {
    int64 PlayerID = 1;
    LSInput Input = 2;
}
```

**用途**: 服务器广播一帧的所有玩家输入

**发送时机**: 服务器收集完所有客户端输入后，定时广播

### FrameSyncStartNotification (服务器→客户端)

**定义**:
```protobuf
message FrameSyncStartNotification {
    string roomId = 1;
    int32 frameRate = 2;
    int64 startTime = 3;
}
```

**用途**: 通知客户端帧同步开始

**时机**: 游戏开始时发送

### FrameSyncEndNotification (服务器→客户端)

**定义**:
```protobuf
message FrameSyncEndNotification {
    string roomId = 1;
    int32 finalFrame = 2;
    string reason = 3;
}
```

**用途**: 通知客户端帧同步结束

**时机**: 游戏结束时发送

---

## 帧同步时序

### 理想时序

```
客户端A: [输入] ──┐
                  ├──► 服务器: [收集输入] ──► [执行逻辑] ──► [广播]
客户端B: [输入] ──┘                                    │
                                                        │
客户端A: ◄────────────────────────────────────────────┘
客户端B: ◄────────────────────────────────────────────┘
```

### 实际时序（考虑网络延迟）

```
客户端A发送输入: T0
  延迟: +50ms
服务器接收: T0+50ms

服务器收集所有输入: T0+50ms ~ T0+100ms
服务器执行逻辑: T0+100ms
服务器广播: T0+100ms
  延迟: +50ms
客户端接收: T0+150ms

总延迟: ~150ms (3-4帧)
```

### 延迟补偿策略

1. **客户端预测**: 本地立即执行，服务器验证后纠正
2. **延迟隐藏**: 缓冲几帧再执行，平滑延迟波动
3. **时间同步**: 客户端使用服务器时间，而非本地时间

---

## 实现细节

### 服务器帧同步流程

```csharp
// FrameSyncManager.Update()
public void Update()
{
    var now = TimeInfo.Instance.ClientNow();
    
    foreach (var room in activeRooms)
    {
        // 检查是否到达下一帧时间
        if (now >= room.NextFrameTime)
        {
            // 1. 收集所有玩家输入
            var frameInputs = CollectAllInputs(room);
            
            // 2. 执行游戏逻辑
            ExecuteGameLogic(room, frameInputs);
            
            // 3. 广播输入给所有客户端
            BroadcastFrameInputs(room, frameInputs);
            
            // 4. 推进到下一帧
            room.CurrentFrame++;
            room.NextFrameTime += FrameInterval;
        }
    }
}
```

### 客户端帧同步流程

```csharp
// 接收服务器广播的帧输入
public void OnFrameInputs(OneFrameInputs frameInputs)
{
    // 1. 更新当前帧
    var frameId = frameInputs.FrameID;
    
    // 2. 将所有输入应用到游戏逻辑
    foreach (var playerInput in frameInputs.Inputs)
    {
        var room = MainRoom;
        if (room != null)
        {
            // 应用输入到对应玩家
            room.ApplyPlayerInput(playerInput.PlayerID, playerInput.Input);
        }
    }
    
    // 3. 执行确定性逻辑（与服务器完全一致）
    room.ExecuteFrame(frameId);
    
    // 4. 更新显示
    UpdateVisuals();
}
```

---

## 输入处理

### 输入格式 (LSInput)

```csharp
public class LSInput
{
    public Vector3 MoveDirection { get; set; }  // 移动方向
    public int SkillId { get; set; }           // 技能ID
    public bool IsJumping { get; set; }        // 是否跳跃
    public int BornInfo { get; set; }          // 创建玩家时的额外信息
}
```

### 输入收集

```csharp
// InputManager中
private LSInput CollectInput()
{
    var input = new LSInput();
    
    // 从Unity输入系统收集
    input.MoveDirection = new Vector3(
        Input.GetAxis("Horizontal"),
        0,
        Input.GetAxis("Vertical")
    );
    
    // 收集技能输入
    if (Input.GetKeyDown(KeyCode.Space))
    {
        input.IsJumping = true;
    }
    
    return input;
}
```

---

## 状态同步

### 同步内容

#### 必须同步
- ✅ 玩家位置和速度
- ✅ 实体状态（HP、MP等）
- ✅ 技能释放和冷却
- ✅ 伤害和死亡事件

#### 不需要同步（本地表现）
- ❌ 动画播放
- ❌ 特效显示
- ❌ 音效播放
- ❌ UI更新（部分）

### 状态验证

```csharp
// 服务器定期发送权威状态（规划中）
// 客户端对比本地状态和服务器状态
// 如果差异超过阈值，进行纠正
if (stateDifference > threshold)
{
    // 回滚到服务器状态
    RollbackToServerState(serverState);
}
```

---

## 性能优化

### 1. 输入压缩

- 只发送有意义的输入（非零值）
- 使用增量编码
- 批量发送多个帧的输入

### 2. 帧率控制

- 服务器帧率: 20 FPS (50ms/帧)
- 客户端帧率: 60 FPS (16.67ms/帧)
- 客户端插值显示

### 3. 网络优化

- 使用UDP进行帧同步（规划中）
- 消息批处理
- 压缩帧数据

---

## 断线重连

### 重连策略（规划中）

1. **状态快照**: 服务器定期保存游戏状态快照
2. **状态同步**: 重连后发送完整状态给客户端
3. **快速追赶**: 客户端快速执行到当前帧
4. **平滑过渡**: 平滑过渡到实时同步

---

## 常见问题

### Q1: 如何保证确定性？

**A**: 
- 使用定点数（TrueSync）
- 避免浮点数计算
- 避免非确定性随机数
- 逻辑执行顺序一致

### Q2: 如何处理网络延迟？

**A**:
- 客户端预测执行
- 服务器验证纠正
- 延迟隐藏（缓冲）
- 平滑插值显示

### Q3: 如何防止作弊？

**A**:
- 服务器权威验证
- 输入合理性检查
- 状态验证
- 异常检测

---

## 总结

帧同步机制是多人游戏的核心：

✅ **确定性**: 保证所有客户端状态一致  
✅ **公平性**: 服务器权威，防止作弊  
✅ **可扩展**: 支持任意数量的玩家  
✅ **性能**: 高效的状态同步机制  
✅ **可靠性**: 支持断线重连（规划中）

