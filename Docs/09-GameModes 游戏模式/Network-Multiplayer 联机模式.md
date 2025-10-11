# 🌐 联机模式设计文档

**版本**: v1.0.0  
**状态**: ✅ 基础完成，🚧 持续优化  
**最后更新**: 2025-10-10

---

## 📋 目录

1. [概述](#概述)
2. [设计目标](#设计目标)
3. [网络架构](#网络架构)
4. [连接流程](#连接流程)
5. [房间系统](#房间系统)
6. [帧同步机制](#帧同步机制)
7. [消息协议](#消息协议)
8. [状态同步](#状态同步)
9. [与单机模式的区别](#与单机模式的区别)

---

## 概述

联机模式是 Astrum 游戏的在线多人对战方式，玩家通过连接到服务器与其他玩家实时对战。该模式采用**客户端-服务器架构** + **帧同步**，确保所有客户端的游戏状态保持一致。

### 核心特性

- **实时对战** - 支持多人同时游戏
- **帧同步** - 服务器权威，确保公平性
- **房间匹配** - 房间创建、加入、离开
- **用户管理** - 登录、状态管理
- **断线重连** - 支持短暂掉线后重连（规划中）
- **观战模式** - 支持观战（规划中）

---

## 设计目标

### 游戏体验目标

1. **低延迟** 🚀
   - 目标延迟: < 100ms
   - 网络优化: 数据压缩、增量更新
   - 客户端预测: 减少操作延迟感

2. **一致性** 🎯
   - 服务器权威
   - 确定性逻辑
   - 状态验证和纠正

3. **公平性** ⚖️
   - 防作弊机制
   - 输入验证
   - 服务器仲裁

4. **稳定性** 🛡️
   - 断线重连
   - 异常处理
   - 房间恢复

### 技术目标

1. **可扩展性** 📈
   - 支持大量并发房间
   - 动态负载均衡
   - 水平扩展能力

2. **可维护性** 🔧
   - 清晰的协议定义
   - 日志和监控
   - 版本兼容性

---

## 网络架构

### 总体架构

```
┌─────────────┐         TCP/IP          ┌─────────────┐
│   Client A  │◄──────────────────────►│             │
│   Unity     │                         │             │
└─────────────┘                         │             │
                                        │   Server    │
┌─────────────┐         TCP/IP          │   .NET 9.0  │
│   Client B  │◄──────────────────────►│             │
│   Unity     │                         │             │
└─────────────┘                         │             │
                                        │             │
┌─────────────┐         TCP/IP          │             │
│   Client C  │◄──────────────────────►│             │
│   Unity     │                         └─────────────┘
└─────────────┘
```

### 通信协议

- **传输层**: TCP/IP
- **序列化**: Protocol Buffers + MemoryPack
- **消息格式**: 
  ```
  [2 bytes: Length] [2 bytes: OpCode] [N bytes: Payload]
  ```

### 核心组件

#### 客户端 (Unity)

1. **NetworkManager** - 网络管理器
   - 连接管理
   - 消息发送/接收
   - 事件分发

2. **UserManager** - 用户管理器
   - 登录/登出
   - 用户信息管理
   - 心跳维持

3. **RoomSystemManager** - 房间系统管理器
   - 房间列表
   - 创建/加入/离开房间
   - 房间状态更新

4. **GamePlayManager** - 游戏管理器
   - 游戏生命周期
   - 帧同步接收
   - 输入上报

#### 服务器 (.NET)

1. **GameServer** - 游戏服务器主体
   - 监听连接
   - 会话管理

2. **UserManager** - 用户管理
   - 用户注册
   - 会话映射

3. **RoomManager** - 房间管理
   - 房间创建/销毁
   - 玩家管理
   - 帧同步调度

4. **MessageHandler** - 消息处理器
   - 协议解析
   - 业务逻辑
   - 响应发送

---

## 连接流程

### 完整连接流程图

```
[1] 客户端启动
    ↓
[2] NetworkManager.Initialize()
    ↓
[3] NetworkManager.ConnectAsync(serverIP, port)
    ↓
[4] TCP连接建立
    ↓ 
    服务器响应: ConnectResponse
    ↓
[5] 客户端发送: LoginRequest(displayName)
    ↓
    服务器验证 → 创建User → 分配UserId
    ↓
    服务器响应: LoginResponse(userInfo)
    ↓
[6] 客户端保存 UserInfo
    ↓
[7] 客户端发送: GetRoomListRequest
    ↓
    服务器响应: GetRoomListResponse(roomList)
    ↓
[8] 显示房间列表UI
    ↓
[9] 玩家选择：创建房间 / 加入房间
    ↓
    ┌───────────────────┬───────────────────┐
    ↓                   ↓                   ↓
[10A] CreateRoomRequest [10B] JoinRoomRequest
    ↓                   ↓
[11A] 服务器创建房间    [11B] 服务器加入房间
    ↓                   ↓
    └───────────────────┴───────────────────┘
                    ↓
[12] 服务器响应: CreateRoomResponse / JoinRoomResponse
    ↓
[13] 服务器广播: RoomUpdateNotification (to all in room)
    ↓
[14] 客户端显示房间内UI（玩家列表、准备状态）
    ↓
[15] 房主点击"开始游戏"
    ↓
[16] 客户端发送: GameRequest(action="StartGame")
    ↓
[17] 服务器验证所有玩家准备
    ↓
[18] 服务器广播: GameStartNotification
    ↓
[19] 客户端加载游戏场景
    ↓
[20] 客户端通知服务器: GameResponse(ready=true)
    ↓
[21] 服务器等待所有客户端准备完成
    ↓
[22] 服务器广播: FrameSyncStartNotification
    ↓
[23] 开始帧同步循环
    ↓
游戏进行中...
```

### 详细步骤说明

#### Step 1-4: 建立网络连接

```csharp
// 客户端
await NetworkManager.Instance.ConnectAsync("127.0.0.1", 8080);

// 服务器自动响应 ConnectResponse
```

#### Step 5-6: 用户登录

```csharp
// 客户端发送登录请求
var loginRequest = new LoginRequest 
{ 
    DisplayName = "PlayerName"
};
NetworkManager.Instance.Send(loginRequest);

// 服务器响应
NetworkManager.Instance.OnLoginResponse += (response) => 
{
    if (response.Success)
    {
        UserManager.Instance.SetCurrentUser(response.UserInfo);
        ASLogger.Instance.Info($"登录成功: {response.UserInfo.DisplayName}");
    }
};
```

**服务器处理**:
```csharp
// 创建用户
var user = new UserInfo
{
    Id = GenerateUserId(),
    DisplayName = request.DisplayName,
    LastLoginAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};

userManager.AddUser(user, session);

// 响应
var response = new LoginResponse
{
    Success = true,
    UserInfo = user
};
session.Send(response);
```

#### Step 7-8: 获取房间列表

```csharp
// 客户端请求
var getRoomListRequest = new GetRoomListRequest
{
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};
NetworkManager.Instance.Send(getRoomListRequest);

// 客户端接收
NetworkManager.Instance.OnGetRoomListResponse += (response) => 
{
    RoomSystemManager.Instance.UpdateRoomList(response.Rooms);
    // 刷新UI显示房间列表
};
```

#### Step 10-14: 房间操作

**创建房间**:
```csharp
// 客户端
var createRoomRequest = new CreateRoomRequest
{
    RoomName = "My Room",
    MaxPlayers = 4,
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};
NetworkManager.Instance.Send(createRoomRequest);

// 响应
NetworkManager.Instance.OnCreateRoomResponse += (response) => 
{
    if (response.Success)
    {
        RoomSystemManager.Instance.JoinRoom(response.RoomInfo);
        // 进入房间UI
    }
};
```

**加入房间**:
```csharp
// 客户端
var joinRoomRequest = new JoinRoomRequest
{
    RoomId = selectedRoom.Id,
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};
NetworkManager.Instance.Send(joinRoomRequest);
```

**房间更新通知** (服务器广播):
```csharp
// 服务器
var notification = new RoomUpdateNotification
{
    RoomInfo = updatedRoomInfo,
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};

// 广播给房间内所有玩家
room.BroadcastToAll(notification);
```

#### Step 15-22: 开始游戏

```csharp
// 房主发起
var gameRequest = new GameRequest
{
    RequestId = GenerateRequestId(),
    Action = "StartGame"
};
NetworkManager.Instance.Send(gameRequest);

// 服务器验证并广播
var startNotification = new GameStartNotification
{
    RoomId = room.Id,
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};
room.BroadcastToAll(startNotification);

// 客户端加载场景
NetworkManager.Instance.OnGameStartNotification += (notification) => 
{
    GamePlayManager.Instance.LoadGameScene();
};
```

---

## 房间系统

### 房间状态机

```
[Idle 空闲]
    ↓ CreateRoom
[Waiting 等待]
    ↓ AllPlayersReady
[Starting 启动中]
    ↓ AllClientsLoaded
[Running 运行中]
    ↓ GameEnd
[Ending 结束中]
    ↓ Cleanup
[Idle 空闲]
```

### 房间数据结构

```csharp
public class Room
{
    public string RoomId { get; set; }
    public string RoomName { get; set; }
    public int MaxPlayers { get; set; }
    public List<UserInfo> Players { get; set; }
    public RoomState State { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // 帧同步相关
    public int CurrentFrame { get; set; }
    public FrameSyncManager FrameSyncManager { get; set; }
    
    // 游戏逻辑
    public World MainWorld { get; set; }
}

public enum RoomState
{
    Idle,       // 空闲
    Waiting,    // 等待玩家
    Starting,   // 启动中
    Running,    // 运行中
    Ending      // 结束中
}
```

### 房间管理器

```csharp
public class RoomManager
{
    private Dictionary<string, Room> _rooms = new();
    
    // 创建房间
    public Room CreateRoom(string roomName, int maxPlayers, UserInfo creator);
    
    // 加入房间
    public bool JoinRoom(string roomId, UserInfo user);
    
    // 离开房间
    public void LeaveRoom(string roomId, UserInfo user);
    
    // 获取房间列表
    public List<RoomInfo> GetRoomList();
    
    // 销毁房间
    public void DestroyRoom(string roomId);
}
```

---

## 帧同步机制

### 帧同步原理

```
服务器主循环 (每16.67ms, 60 FPS)
    ↓
[1] 收集所有客户端的输入
    ↓
[2] 组装当前帧输入数据
    ↓
[3] 广播给房间内所有客户端
    ↓
[4] 客户端执行相同帧
    ↓
[5] 状态保持同步
```

### 输入上报流程

```
客户端每帧:
    ↓
[1] InputManager.GetInput()
    - 读取键盘/鼠标输入
    ↓
[2] 构造 SingleInput 消息
    SingleInput {
        PlayerId = localPlayerId,
        Input = {
            MoveX, MoveY,
            Attack, Skill, ...
        },
        Frame = currentFrame
    }
    ↓
[3] NetworkManager.Send(SingleInput)
    - 发送到服务器
    ↓
服务器收到:
    ↓
[4] FrameSyncManager.AddInput(playerId, input)
    - 存储到当前帧输入缓冲区
    ↓
[5] 等待所有玩家输入或超时
    ↓
[6] 组装 OneFrameInputs
    OneFrameInputs {
        Frame = currentFrame,
        Inputs = [player1Input, player2Input, ...]
    }
    ↓
[7] Room.BroadcastToAll(OneFrameInputs)
    - 广播给所有客户端
    ↓
客户端收到:
    ↓
[8] GamePlayManager.DealNetFrameInputs(inputs)
    ↓
[9] Room.Tick(inputs)
    - 执行逻辑帧
    ↓
[10] 所有客户端保持同步
```

### 帧同步数据结构

```protobuf
// 单个玩家的输入
message SingleInput
{
    int64 PlayerId = 1;
    Input Input = 2;
    int32 Frame = 3;
}

// 一帧的所有输入
message OneFrameInputs
{
    int32 Frame = 1;
    repeated Input Inputs = 2;
}

// 帧同步数据 (服务器广播)
message FrameSyncData
{
    int32 Frame = 1;
    repeated Input PlayerInputs = 2;
    int64 Timestamp = 3;
}
```

### 关键代码

**客户端输入上报**:
```csharp
void Update()
{
    // 收集输入
    var input = InputManager.Instance.GetCurrentInput();
    input.PlayerId = GamePlayManager.Instance.PlayerId;
    input.Frame = currentFrame;
    
    // 上报服务器
    var singleInput = new SingleInput
    {
        PlayerId = input.PlayerId,
        Input = input,
        Frame = currentFrame
    };
    NetworkManager.Instance.Send(singleInput);
}
```

**客户端接收帧数据**:
```csharp
NetworkManager.Instance.OnFrameInputs += (frameInputs) => 
{
    GamePlayManager.Instance.DealNetFrameInputs(frameInputs);
};

public void DealNetFrameInputs(OneFrameInputs frameInputs)
{
    if (MainRoom == null) return;
    
    // 执行逻辑帧
    MainRoom.Tick(frameInputs.Inputs);
    
    // 同步到表现层
    MainStage.Tick();
}
```

**服务器帧循环**:
```csharp
// 服务器主循环 (60 FPS)
while (room.State == RoomState.Running)
{
    // 1. 收集输入（带超时）
    var inputs = frameSyncManager.CollectInputs(currentFrame, timeoutMs: 50);
    
    // 2. 组装帧数据
    var frameInputs = new OneFrameInputs
    {
        Frame = currentFrame,
        Inputs = inputs
    };
    
    // 3. 广播
    room.BroadcastToAll(frameInputs);
    
    // 4. 执行服务器逻辑帧（如需要）
    // room.Tick(inputs);
    
    currentFrame++;
    await Task.Delay(16); // 60 FPS ≈ 16.67ms
}
```

---

## 消息协议

### 协议分类

#### 1. 连接协议
| 消息 | 方向 | 说明 |
|------|------|------|
| ConnectResponse | S→C | 连接确认 |
| HeartbeatRequest | C→S | 心跳请求 |
| HeartbeatResponse | S→C | 心跳响应 |
| C2G_Ping | C→S | Ping测试 |
| G2C_Ping | S→C | Pong响应 |

#### 2. 用户协议
| 消息 | 方向 | 说明 |
|------|------|------|
| LoginRequest | C→S | 登录请求 |
| LoginResponse | S→C | 登录响应 |

#### 3. 房间协议
| 消息 | 方向 | 说明 |
|------|------|------|
| GetRoomListRequest | C→S | 获取房间列表 |
| GetRoomListResponse | S→C | 房间列表响应 |
| CreateRoomRequest | C→S | 创建房间 |
| CreateRoomResponse | S→C | 创建房间响应 |
| JoinRoomRequest | C→S | 加入房间 |
| JoinRoomResponse | S→C | 加入房间响应 |
| LeaveRoomRequest | C→S | 离开房间 |
| LeaveRoomResponse | S→C | 离开房间响应 |
| RoomUpdateNotification | S→C | 房间更新通知 (广播) |

#### 4. 游戏协议
| 消息 | 方向 | 说明 |
|------|------|------|
| GameRequest | C→S | 游戏请求（开始、暂停等） |
| GameResponse | S→C | 游戏响应 |
| GameStartNotification | S→C | 游戏开始通知 (广播) |
| GameEndNotification | S→C | 游戏结束通知 (广播) |
| GameStateUpdate | S→C | 游戏状态更新 |

#### 5. 帧同步协议
| 消息 | 方向 | 说明 |
|------|------|------|
| SingleInput | C→S | 单个玩家输入 |
| OneFrameInputs | S→C | 一帧的所有输入 (广播) |
| FrameSyncStartNotification | S→C | 帧同步开始 |
| FrameSyncEndNotification | S→C | 帧同步结束 |
| FrameSyncData | S→C | 帧同步数据 (备用) |

### 消息处理流程

```
客户端发送消息:
    ↓
NetworkManager.Send(message)
    ↓
Session.Send(bytes)
    ↓
TService.Send(bytes)
    ↓
TCP发送
    ↓
=== 网络传输 ===
    ↓
服务器接收:
    ↓
TService.OnRead(bytes)
    ↓
Session.OnRead(message)
    ↓
MessageHandler.Handle(message)
    ↓
业务逻辑处理
    ↓
构造响应消息
    ↓
Session.Send(response)
    ↓
=== 网络传输 ===
    ↓
客户端接收:
    ↓
TService.OnRead(bytes)
    ↓
Session.OnRead(message)
    ↓
NetworkManager.OnMessage(message)
    ↓
触发对应事件
    ↓
业务层处理
```

---

## 状态同步

### 同步内容

#### 必须同步
- ✅ 玩家输入
- ✅ 实体位置（通过帧同步保证）
- ✅ 实体状态（HP、技能CD等）
- ✅ 技能触发
- ✅ 伤害事件

#### 不需要同步（本地表现）
- ❌ 动画播放
- ❌ 特效显示
- ❌ 音效播放
- ❌ UI更新

### 一致性保证

1. **确定性逻辑**
   - 使用 TrueSync (FP Math)
   - 避免浮点数误差
   - 避免非确定性随机

2. **服务器权威**
   - 服务器驱动帧
   - 输入验证
   - 状态仲裁

3. **客户端预测** (规划中)
   - 本地预测执行
   - 服务器验证
   - 状态回滚纠正

---

## 与单机模式的区别

### 核心差异

| 特性 | 单机模式 | 联机模式 |
|------|----------|----------|
| **架构** | 本地 | 客户端-服务器 |
| **Room** | 本地创建 | 服务器创建 |
| **帧驱动** | Unity Update() | 服务器推送 |
| **输入** | 直接应用 | 上报→广播 |
| **延迟** | 0ms | 10-100ms |
| **对手** | AI | 真实玩家 |
| **暂停** | ✅ | ❌ |
| **作弊** | 无影响 | 需防范 |
| **调试** | 简单 | 复杂 |

### 代码复用

**100%复用的逻辑层**:
- ✅ Entity-Component-Capability
- ✅ 战斗系统
- ✅ 物理系统
- ✅ 技能系统
- ✅ 帧同步逻辑核心

**不同的部分**:
- ❌ 输入来源 (本地 vs 网络)
- ❌ 帧驱动 (Update vs 服务器推送)
- ❌ Room管理 (本地 vs 服务器)

---

## 优化策略

### 网络优化

1. **消息压缩**
   - MemoryPack二进制序列化
   - 差量编码（规划中）
   - 批量打包（规划中）

2. **带宽优化**
   - 输入数据压缩（使用byte而非int）
   - 只同步变化的数据
   - 优先级队列

3. **延迟优化**
   - 客户端预测（规划中）
   - 插值平滑
   - UDP优化（规划中）

### 服务器优化

1. **并发优化**
   - 异步I/O
   - 房间独立tick
   - 无锁队列

2. **内存优化**
   - 对象池
   - 增量GC
   - 内存预分配

---

## 已知限制

### 当前限制

1. **房间容量** - 单房间建议 ≤ 4人
2. **断线重连** - 未实现
3. **观战模式** - 未实现
4. **客户端预测** - 未实现
5. **状态回滚** - 未实现

### 技术债务

1. **UDP支持** - 当前仅TCP
2. **负载均衡** - 未实现
3. **房间持久化** - 未实现
4. **录像回放** - 未实现

---

## 相关文档

- [联机模式开发进展](_status%20开发进展/Network-Multiplayer-Progress%20联机模式开发进展.md) - 详细开发记录
- [单机模式](Single-Player%20单机模式.md) - 对比参考
- [房间系统](../01-GameDesign%20游戏设计/Room-System%20房间系统.md) - 房间系统详细设计
- [服务器配置](../07-Development%20开发指南/Server-Setup%20服务器配置.md) - 服务器部署

---

**最后更新**: 2025-10-10  
**维护者**: 开发团队



