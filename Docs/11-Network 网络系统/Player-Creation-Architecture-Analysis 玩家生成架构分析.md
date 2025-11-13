# 玩家生成架构分析

## 一、当前架构总结

### 1.1 客户端流程

#### 1.1.1 玩家创建请求
```
MultiplayerGameMode.RequestCreatePlayer()
  ↓
发送 SingleInput 消息
  - FrameID: LSController.PredictionFrame
  - Input.BornInfo: UserId.GetHashCode()
  - Input.PlayerId: 0 (初始值)
```

#### 1.1.2 帧同步数据接收
```
FrameSyncHandler.OnFrameSyncData()
  ↓
DealNetFrameInputs()
  - 更新 AuthorityFrame
  - 调用 LSController.SetOneFrameInputs() 存储到 FrameBuffer
```

#### 1.1.3 游戏逻辑更新
```
MultiplayerGameMode.Update()
  ↓
Room.Update()
  ↓
LSController.Tick()
  - 基于时间推进 PredictionFrame
  - 调用 GetOneFrameMessages() 从 FrameBuffer 获取输入
  - 调用 Room.FrameTick()
```

#### 1.1.4 玩家实体创建（客户端）
```
Room.FrameTick()
  ↓
检测到 Input.BornInfo != 0
  ↓
Room.AddPlayer()
  - MainWorld.CreateEntity(1003)  // EntityConfigId=1003
  - Players.Add(player.UniqueId)
  ↓
发布 NewPlayerEventData
  ↓
MultiplayerGameMode.OnPlayerCreated()
  - 设置 PlayerId
  - 设置 MainRoom.MainPlayerId
```

#### 1.1.5 实体视图创建
```
Room.FrameTick() 中调用 MainWorld.CreateEntity()
  ↓
World.CreateEntity() 自动发布 EntityCreatedEventData 事件
  ↓
Stage.OnEntityCreated()（自动监听事件）
  - EntityViewFactory.CreateEntityView()
  - 创建 EntityView 并添加到 Stage
```

### 1.2 服务器流程

#### 1.2.1 玩家创建请求接收
```
GameServer.HandleSingleInput()
  ↓
FrameSyncManager.HandleSingleInput()
  - 将 SingleInput 转换为 LSInput
  - 分配 PlayerId（如果为0则自增）
  - 存储到 FrameBuffer（StoreFrameInput）
```

#### 1.2.2 帧处理
```
FrameSyncManager.ProcessRoomFrame()
  - AuthorityFrame++
  - CollectFrameInputs() 收集所有玩家输入
  - 更新 LSController.AuthorityFrame
  - 调用 Room.FrameTick()
```

#### 1.2.3 玩家实体创建（服务器）
```
Room.FrameTick()
  ↓
检测到 Input.BornInfo != 0
  ↓
Room.AddPlayer()
  - MainWorld.CreateEntity(1003)
  - Players.Add(player.UniqueId)
  ↓
发布 NewPlayerEventData（服务器端事件，不传播到客户端）
  ↓
pairs.Value.BornInfo = 0
pairs.Value.PlayerId = playerId
```

#### 1.2.4 状态同步
```
ProcessRoomFrame() 继续
  ↓
SendFrameSyncData()
  - 序列化 OneFrameInputs（包含所有玩家的输入）
  - 发送给房间内所有客户端
```

### 1.3 关键数据结构

#### 1.3.1 SingleInput 消息
```csharp
class SingleInput {
    int PlayerID;      // 客户端发送时为0，服务器分配后更新
    int FrameID;       // 客户端预测帧号
    LSInput Input;     // 输入数据
}

class LSInput {
    int PlayerId;      // 玩家ID（服务器分配）
    int BornInfo;      // 创建玩家标识（UserId.GetHashCode()）
    int MoveX, MoveY; // 移动输入
    // ... 其他输入
}
```

#### 1.3.2 OneFrameInputs（帧同步数据）
```csharp
class OneFrameInputs {
    Dictionary<long, LSInput> Inputs;  // Key: PlayerId, Value: LSInput
}
```

## 二、当前架构存在的问题

### 2.1 职责混乱

1. **玩家创建逻辑分散**
   - 客户端和服务器都在 `Room.FrameTick()` 中检测 `BornInfo` 并创建玩家
   - 创建逻辑耦合在帧同步逻辑中，难以维护

2. **PlayerId 分配不清晰**
   - 客户端初始发送 `PlayerId = 0`
   - 服务器在 `HandleSingleInput` 中分配 PlayerId
   - 但客户端在 `FrameTick` 中也会创建玩家，可能导致 ID 不一致

3. **BornInfo 机制不合理**
   - 通过输入消息的 `BornInfo` 字段触发创建，语义不清晰
   - `BornInfo` 需要手动清零，容易出错
   - 创建逻辑与输入处理混在一起

### 2.2 同步问题

1. **事件系统分离**
   - 服务器的事件不会传播到客户端
   - 客户端通过帧同步数据接收实体状态，`World.CreateEntity()` 会自动发布事件
   - 事件系统正常工作，无需额外检测

2. **时序问题**
   - 客户端和服务器在同一帧创建玩家，但 PlayerId 可能不一致
   - 客户端可能先于服务器创建，导致状态不同步

3. **状态同步**
   - 服务器创建的实体通过帧同步数据同步
   - 客户端通过 `World.CreateEntity()` 创建实体时，会自动发布 `EntityCreatedEventData` 事件
   - `Stage` 监听事件并自动创建 `EntityView`，无需额外检测

### 2.3 扩展性问题

1. **难以支持断线重连**
   - 玩家创建逻辑分散，难以在重连时恢复状态
   - 没有明确的玩家注册/注销机制

2. **难以支持回放**
   - 创建逻辑耦合在帧同步中，难以单独回放创建过程

3. **难以支持其他实体类型**
   - 当前逻辑只处理玩家创建，其他实体创建需要类似机制

## 三、重构方案：玩家注册系统

#### 核心思想（优化版）
- **服务器在开始游戏时自动为房间内所有玩家分配PlayerId**
- **服务器维护玩家注册状态**
- **客户端通过 FrameSyncStartNotification 获取PlayerId**（无需发送注册请求）
- **服务器通过帧输入下发创建玩家指令**（支持断线重连和回放）

**优化说明：**
- 由于玩家先登录、进房间，然后才开始游戏，服务器在开始游戏时已经知道房间内的所有玩家
- 因此可以在 `StartRoomFrameSync` 时自动为所有玩家分配 PlayerId，无需客户端发送注册请求
- 客户端通过 `FrameSyncStartNotification` 中的 `playerIdMapping` 字段获取自己的 PlayerId
- **重要**：`playerIdMapping` 包含房间内所有玩家的映射关系（UserId -> PlayerId），方便后续业务系统使用，例如：
  - 显示玩家列表时，可以通过 UserId 查找对应的 PlayerId
  - 处理玩家交互时，可以通过 PlayerId 查找对应的 UserId
  - 统计和分析时，可以关联 UserId 和 PlayerId

#### 详细流程设计

##### 3.1 客户端流程（优化版：自动注册）

**阶段1：游戏开始**
```
1. 进入房间后（收到 GameStartNotification）
   MultiplayerGameMode.OnGameStartNotification()
     ↓
   创建 Room 和 Stage
     ↓
   等待帧同步开始（不需要发送注册请求）
```

**阶段2：帧同步开始并获取 PlayerId，创建玩家实体**
```
2. 收到 FrameSyncStartNotification
   FrameSyncHandler.OnFrameSyncStartNotification()
     ↓
   从通知中获取 PlayerId 和所有玩家的映射
   - 通知中包含 playerIdMapping（Dictionary<string, long>，UserId -> PlayerId（UniqueId））
   - 包含房间内所有玩家的映射关系，方便后续业务系统使用
   - 客户端从映射中获取自己的 PlayerId：playerIdMapping[UserId]
     ↓
   MultiplayerGameMode.OnFrameSyncStart()
   - 保存 PlayerId（从 playerIdMapping 中获取）
   - 保存所有玩家的映射关系（供业务系统使用）
   - 设置 MainRoom.MainPlayerId = PlayerId
     ↓
   根据 playerIdMapping 创建所有玩家实体
   foreach (kvp in playerIdMapping)
   {
     var userId = kvp.Key;
     var playerId = kvp.Value; // PlayerId 就是服务器端实体的 UniqueId
     
     // 创建实体，使用指定的 UniqueId（PlayerId）
     // 注意：需要修改 Entity 类支持在创建时指定 UniqueId
     var entity = MainWorld.CreateEntity(1003, playerId); // 使用指定的 UniqueId
     // 或者：先创建实体，然后设置 UniqueId（需要 Entity 类支持）
   }
     ↓
   启动 LSController
   - 设置 CreationTime = notification.startTime
   - LSController.Start()
```

**阶段3：接收帧同步数据并更新实体状态**
```
3. 收到 FrameSyncData
   FrameSyncHandler.OnFrameSyncData()
     ↓
   DealNetFrameInputs()
   - 更新 AuthorityFrame
   - 存储到 FrameBuffer
     ↓
   LSController.Tick() (在 Update 循环中)
   - 推进 PredictionFrame
   - 调用 Room.FrameTick()
     ↓
   Room.FrameTick()（方案B：去掉 BornInfo）
   - 遍历所有输入
   - 如果输入中有 PlayerId，但 World 中没有对应实体（理论上不应该发生，实体已在阶段2创建）
   - 调用 Room.AddPlayer(playerId) 创建实体（兜底逻辑）
   - 更新实体的输入组件
   - MainWorld.CreateEntity() 会自动发布 EntityCreatedEventData 事件
     ↓
   Stage.OnEntityCreated()（自动监听 EntityCreatedEventData）
   - EntityViewFactory.CreateEntityView()
   - 创建 EntityView
```

##### 3.2 服务器流程（优化版：自动注册）

**阶段1：开始游戏时自动创建实体并分配 PlayerId**
```
1. StartRoomFrameSync(roomId)
   FrameSyncManager.StartRoomFrameSync()
     ↓
   获取房间信息
   - roomInfo = RoomManager.GetRoom(roomId)
   - playerNames = roomInfo.PlayerNames（房间内所有玩家UserId列表）
     ↓
   创建逻辑房间
   - frameState.LogicRoom = CreateLogicRoom(roomId, roomInfo)
     ↓
   先创建所有玩家实体，获得 UniqueId
   foreach (userId in playerNames)
   {
     // 先创建实体，获得 UniqueId
     var entity = frameState.LogicRoom.MainWorld.CreateEntity(1003); // EntityConfigId=1003
     var playerId = entity.UniqueId; // PlayerId 就是 Entity 的 UniqueId
     
     // 将 UniqueId 作为 PlayerId 分配给 UserId
     RegisterPlayer(userId, roomId, playerId);
     - 注册到 RoomFrameSyncState.RegisteredPlayers
     - 记录 UserId -> PlayerId（UniqueId）映射
     - 标记 EntityCreated = true（实体已创建）
   }
     ↓
   创建 RoomFrameSyncState
   - PlayerIds = playerNames（UserId列表）
   - RegisteredPlayers = 已注册的玩家列表（包含已创建的实体）
     ↓
   发送 FrameSyncStartNotification
   - 包含 playerIds（UserId列表）
   - 包含 playerIdMapping（Dictionary<string, long>，UserId -> PlayerId（UniqueId）映射）
   - playerIdMapping 包含房间内所有玩家的映射关系，方便客户端业务系统使用
   - 所有玩家都收到相同的通知，包含完整的映射关系
```

**阶段2：帧处理 - 同步实体状态到客户端**
```
2. ProcessRoomFrame()
   - AuthorityFrame++
   - CollectFrameInputs() 收集输入
     ↓
   确保所有已创建玩家的输入出现在帧数据中
   foreach (registeredPlayer in RegisteredPlayers)
   {
     // 实体已在 StartRoomFrameSync 时创建，确保其输入出现在帧数据中
     if (!frameInputs.Inputs.ContainsKey(registeredPlayer.PlayerId))
     {
       // 如果该 PlayerId 还没有输入，创建一个空输入
       var emptyInput = new LSInput {
         PlayerId = registeredPlayer.PlayerId,
         Frame = AuthorityFrame,
         // 其他字段保持默认值（空输入）
       };
       frameInputs.Inputs[registeredPlayer.PlayerId] = emptyInput;
     }
   }
     ↓
   Room.FrameTick(frameInputs)
   - 遍历所有输入
   - 如果输入中有 PlayerId，但 World 中没有对应实体（客户端情况）
   - 调用 Room.AddPlayer(playerId) 创建实体
   - 注意：服务器端实体已在 StartRoomFrameSync 时创建，这里主要是更新输入
   - 实体状态通过帧同步数据同步到客户端
```

**阶段3：帧同步数据下发**
```
3. SendFrameSyncData()
   - 序列化 OneFrameInputs（包含创建指令）
   - 发送给房间内所有客户端
```

#### 3.3 关键数据结构

##### 服务器端：玩家注册状态
```csharp
public class RegisteredPlayer
{
    public string UserId { get; set; }        // 用户ID
    public long PlayerId { get; set; }        // 分配的玩家ID
    public string RoomId { get; set; }        // 房间ID
    public long RegisterTime { get; set; }   // 注册时间
    public bool EntityCreated { get; set; }  // 实体是否已创建
    public int CreateFrame { get; set; }     // 创建实体时的帧号（用于回放）
}

public class RoomFrameSyncState
{
    // ... 现有字段 ...
    
    // 新增：注册的玩家列表
    public Dictionary<string, RegisteredPlayer> RegisteredPlayers { get; set; } = new();
    
    // 新增：UserId -> PlayerId 映射
    public Dictionary<string, long> UserIdToPlayerId { get; set; } = new();
}
```

##### 帧输入中的创建指令（方案A：使用 BornInfo）

```csharp
// LSInput.BornInfo 字段的语义：
// - 0: 正常输入，不创建玩家
// - > 0: 创建玩家指令，值为 PlayerId
//   服务器在帧处理时，如果检测到 BornInfo == PlayerId 且该PlayerId的实体不存在，
//   则创建该PlayerId对应的玩家实体
```

##### 帧输入中的创建指令（方案B：去掉 BornInfo，推荐）

```csharp
// 去掉 BornInfo 字段，直接通过 PlayerId 是否存在于 World 来判断是否需要创建
// 
// 逻辑：
// - 如果输入中有某个 PlayerId，但 World 中没有对应实体，则创建该玩家实体
// - 服务器端：只创建已注册的玩家（通过 RegisteredPlayers 验证）
// - 客户端：创建帧数据中出现的所有 PlayerId 对应的实体
//
// 优点：
// 1. 不需要额外的 BornInfo 字段，简化协议
// 2. 逻辑更简单：实体存在与否就是判断依据
// 3. 语义更清晰：PlayerId 的存在即表示需要创建
// 4. 支持回放：回放时通过帧数据中的 PlayerId 重建所有实体
//
// 实现要点：
// 1. 服务器端：在 ProcessPendingPlayerCreations 中，对于已注册但未创建的玩家，
//    添加一个"空输入"条目到 frameInputs，确保该 PlayerId 出现在帧数据中
// 2. Room.FrameTick：如果输入中有 PlayerId，但 World 中没有对应实体，则创建
// 3. 幂等性：创建前检查实体是否已存在，防止重复创建
```

#### 3.4 断线重连支持

**客户端重连流程（优化版）：**
```
1. 客户端断线后重连
   - 重新建立连接
   - 重新登录（获取UserId）
   - 重新加入房间（如果房间还在）
     ↓
2. 如果游戏已开始，服务器会重新发送 FrameSyncStartNotification
   - 通知中包含 playerIdMapping
   - 客户端从映射中获取自己的 PlayerId（重连时使用已分配的 PlayerId）
     ↓
3. 请求帧同步状态快照
   - 发送 GetFrameSyncSnapshotRequest（新消息）
   - 服务器返回当前帧号和世界状态
     ↓
4. 回放缺失的帧
   - 从快照帧开始，逐帧回放
   - 通过帧输入中的创建指令，重建所有玩家实体
   - 同步到当前帧
```

**服务器重连处理（优化版）：**
```csharp
// 在 StartRoomFrameSync 中处理重连
public void StartRoomFrameSync(string roomId)
{
    var roomInfo = _roomManager.GetRoom(roomId);
    var playerNames = roomInfo.PlayerNames;
    
    // 为所有玩家分配 PlayerId（包括重连的玩家）
    foreach (var userId in playerNames)
    {
        // 检查是否已注册（重连情况）
        if (!UserIdToPlayerId.TryGetValue(userId, out var existingPlayerId))
        {
            // 新玩家，分配新 PlayerId
            var newPlayerId = AllocatePlayerId(roomId);
            RegisterPlayer(userId, roomId, newPlayerId);
        }
        else
        {
            // 重连玩家，使用已分配的 PlayerId
            var registeredPlayer = RegisteredPlayers[userId];
            registeredPlayer.IsReconnected = true;
            registeredPlayer.ReconnectTime = TimeInfo.Instance.ClientNow();
        }
    }
    
    // 发送 FrameSyncStartNotification，包含 playerIdMapping
    SendFrameSyncStartNotification(roomId, frameState);
}
```

#### 3.5 回放功能支持

**回放流程：**
```
1. 客户端请求回放
   - 发送 ReplayRequest（指定起始帧和结束帧）
     ↓
2. 服务器返回回放数据
   - 返回指定帧范围内的所有帧输入数据
   - 包含每帧的 OneFrameInputs（包含创建指令）
     ↓
3. 客户端回放
   - 从起始帧开始，逐帧执行
   - 检测到创建指令（BornInfo > 0）时创建实体
   - 执行所有帧逻辑
   - 同步到结束帧
```

**回放数据结构：**
```csharp
public class ReplayData
{
    public int StartFrame { get; set; }
    public int EndFrame { get; set; }
    public Dictionary<int, OneFrameInputs> FrameInputs { get; set; } // 帧号 -> 输入数据
    public World SnapshotAtStartFrame { get; set; } // 起始帧的世界快照（可选）
}
```

#### 3.6 优点
- **职责清晰**：注册和创建分离，逻辑清晰
- **易于管理**：服务器维护玩家注册状态，便于管理
- **支持重连**：通过注册机制支持断线重连
- **支持回放**：创建指令通过帧输入下发，可以完整回放
- **状态一致**：服务器是唯一权威源，保证状态一致

#### 3.7 需要实现的功能

1. **协议定义**
   - ~~PlayerRegisterRequest / PlayerRegisterResponse~~（优化后不需要）
   - **新增**：在 `FrameSyncStartNotification` 中添加 `playerIdMapping` 字段
     - 类型：`Dictionary<string, long>`（UserId -> PlayerId）
     - 内容：包含房间内所有玩家的映射关系
     - 用途：方便客户端业务系统使用，例如显示玩家列表、处理玩家交互等

2. **服务器端**
   - **修改**：在 `StartRoomFrameSync` 时自动为房间内所有玩家分配 PlayerId
   - 维护 RegisteredPlayers 状态
   - 在帧处理中检查待创建玩家并下发创建指令
   - 重连检测和处理（重连时返回已分配的 PlayerId）

3. **客户端端**
   - ~~发送注册请求~~（优化后不需要）
   - ~~处理注册响应~~（优化后不需要）
   - **修改**：从 `FrameSyncStartNotification` 中获取 PlayerId
   - 修改 FrameTick 逻辑，通过 PlayerId 存在性判断创建玩家
   - 移除旧的 BornInfo 发送逻辑

4. **清理工作**
   - 移除旧的 BornInfo 发送逻辑（RequestCreatePlayer）
   - 清理客户端直接创建玩家的代码
   - **可选**：移除 PlayerRegisterRequest/Response 协议定义（如果确定不再使用）

## 四、选定方案：方案三（玩家注册系统）

### 4.1 方案概述

**核心设计：**
- 玩家注册：客户端通过 `PlayerRegisterRequest` 获取 `PlayerId`
- 创建指令：服务器通过帧输入中的 `BornInfo` 字段下发创建玩家指令
- 状态同步：客户端通过帧同步数据接收创建指令并创建实体

### 4.2 关键设计决策

#### 4.2.1 PlayerId 分配时机
- **时机**：在收到 `PlayerRegisterRequest` 时立即分配
- **方式**：从1开始递增，每个房间独立分配
- **存储**：服务器维护 `UserId -> PlayerId` 映射，支持重连

#### 4.2.2 创建时机
- **时机**：在帧同步开始后的第一帧创建
- **方式**：服务器在 `ProcessRoomFrame()` 中检查待创建玩家，在帧输入中添加创建指令
- **延迟**：可以延迟创建（例如等待所有玩家注册完成），但必须在帧同步开始后

#### 4.2.3 创建指令识别方式

**方案A：使用 BornInfo 字段**
- **语义**：
  - `BornInfo == 0`：正常输入，不创建玩家
  - `BornInfo == PlayerId`：创建玩家指令，值为要创建的 PlayerId
- **优势**：
  - 通过帧输入下发，支持回放
  - 语义清晰，值即为 PlayerId
  - 不需要额外的消息类型

**方案B：去掉 BornInfo，通过 PlayerId 存在性判断（推荐）**
- **语义**：
  - 如果输入中有某个 `PlayerId`，但 `World` 中没有对应实体，则创建该玩家实体
  - 服务器端：只创建已注册的玩家（通过 `RegisteredPlayers` 验证）
  - 客户端：创建帧数据中出现的所有 `PlayerId` 对应的实体
- **优势**：
  - **不需要额外的字段**，简化协议和数据结构
  - **逻辑更简单**：实体存在与否就是判断依据
  - **语义更清晰**：`PlayerId` 的存在即表示需要创建
  - **支持回放**：回放时通过帧数据中的 `PlayerId` 重建所有实体
  - **减少字段占用**：`BornInfo` 字段可以完全移除
- **实现要点**：
  - 服务器端：在 `ProcessPendingPlayerCreations` 中，对于已注册但未创建的玩家，添加一个"空输入"条目到 `frameInputs`
  - `Room.FrameTick`：如果输入中有 `PlayerId`，但 `World` 中没有对应实体，则创建
  - 幂等性：创建前检查实体是否已存在，防止重复创建

#### 4.2.4 重连处理
- **检测**：服务器在收到注册请求时检查 `UserId` 是否已注册
- **处理**：如果已注册，返回现有 `PlayerId`，标记为已重连
- **状态恢复**：客户端需要请求帧同步快照并回放缺失的帧

#### 4.2.5 回放支持
- **数据来源**：帧输入中的创建指令（`BornInfo`）
- **回放流程**：从起始帧开始，逐帧执行，检测到创建指令时创建实体
- **完整性**：所有创建指令都记录在帧输入中，可以完整回放

### 4.3 实现细节

#### 4.3.1 服务器端实现要点

**1. 玩家注册管理（优化版：自动注册）**
```csharp
public class FrameSyncManager
{
    // 在开始帧同步时自动创建实体并分配 PlayerId
    public void StartRoomFrameSync(string roomId)
    {
        var roomInfo = _roomManager.GetRoom(roomId);
        if (roomInfo == null) return;
        
        // 初始化逻辑环境
        EnsureLogicEnvironmentInitialized();
        
        // 创建逻辑房间
        var startTime = TimeInfo.Instance.ClientNow();
        var logicRoom = CreateLogicRoom(roomId, roomInfo, startTime);
        
        var frameState = new RoomFrameSyncState
        {
            RoomId = roomId,
            AuthorityFrame = 0,
            IsActive = true,
            StartTime = startTime,
            PlayerIds = new List<string>(roomInfo.PlayerNames),
            RegisteredPlayers = new Dictionary<string, RegisteredPlayer>(),
            UserIdToPlayerId = new Dictionary<string, long>(),
            LogicRoom = logicRoom
        };
        
        // 先创建所有玩家实体，获得 UniqueId，然后作为 PlayerId 分配给 UserId
        foreach (var userId in roomInfo.PlayerNames)
        {
            // 检查是否已注册（重连情况）
            if (!frameState.UserIdToPlayerId.TryGetValue(userId, out var existingPlayerId))
            {
                // 新玩家：先创建实体，获得 UniqueId
                var entity = logicRoom.MainWorld.CreateEntity(1003); // EntityConfigId=1003
                var playerId = entity.UniqueId; // PlayerId 就是 Entity 的 UniqueId
                
                // 将 UniqueId 作为 PlayerId 分配给 UserId
                var registeredPlayer = new RegisteredPlayer
                {
                    UserId = userId,
                    PlayerId = playerId, // 使用实体的 UniqueId
                    RoomId = roomId,
                    RegisterTime = TimeInfo.Instance.ClientNow(),
                    EntityCreated = true, // 实体已创建
                    CreateFrame = 0 // 在第0帧创建
                };
                
                frameState.RegisteredPlayers[userId] = registeredPlayer;
                frameState.UserIdToPlayerId[userId] = playerId;
                
                ASLogger.Instance.Info($"服务器：创建玩家实体并分配 PlayerId - UserId: {userId}, PlayerId: {playerId} (UniqueId: {entity.UniqueId})");
            }
            else
            {
                // 重连玩家，使用已分配的 PlayerId（实体可能已存在）
                var registeredPlayer = frameState.RegisteredPlayers[userId];
                registeredPlayer.IsReconnected = true;
                registeredPlayer.ReconnectTime = TimeInfo.Instance.ClientNow();
                
                // 检查实体是否还存在
                var existingEntity = logicRoom.MainWorld.GetEntity(existingPlayerId);
                if (existingEntity == null)
                {
                    // 实体不存在，重新创建
                    var entity = logicRoom.MainWorld.CreateEntity(1003);
                    // 注意：新创建的实体 UniqueId 可能与 existingPlayerId 不同
                    // 需要确保使用 existingPlayerId，或者更新映射关系
                    ASLogger.Instance.Warning($"重连玩家实体不存在，重新创建 - UserId: {userId}, PlayerId: {existingPlayerId}");
                }
            }
        }
        
        // 启动帧同步控制器
        logicRoom.LSController?.Start();
        
        _roomFrameStates[roomId] = frameState;
        
        // 发送帧同步开始通知，包含 playerIdMapping
        SendFrameSyncStartNotification(roomId, frameState);
    }
    
    // 发送帧同步开始通知（优化版：包含所有玩家的 playerIdMapping）
    private void SendFrameSyncStartNotification(string roomId, RoomFrameSyncState frameState)
    {
        var notification = FrameSyncStartNotification.Create();
        notification.roomId = roomId;
        notification.frameRate = FRAME_RATE;
        notification.frameInterval = FRAME_INTERVAL_MS;
        notification.startTime = frameState.StartTime;
        notification.playerIds = new List<string>(frameState.PlayerIds);
        
        // 包含所有玩家的 playerIdMapping（UserId -> PlayerId）
        // 所有玩家都收到相同的完整映射关系，方便业务系统使用
        notification.playerIdMapping = new Dictionary<string, long>();
        foreach (var kvp in frameState.UserIdToPlayerId)
        {
            notification.playerIdMapping[kvp.Key] = kvp.Value;
        }
        
        ASLogger.Instance.Info($"准备发送帧同步开始通知，包含 {notification.playerIdMapping.Count} 个玩家的 PlayerId 映射");
        
        // 发送给房间内所有玩家（所有玩家都收到相同的完整映射关系）
        foreach (var userId in frameState.PlayerIds)
        {
            var sessionId = _userManager.GetSessionIdByUserId(userId);
            if (!string.IsNullOrEmpty(sessionId))
            {
                _networkManager.SendMessage(sessionId, notification);
                ASLogger.Instance.Debug($"已发送帧同步开始通知给玩家 - UserId: {userId}, PlayerId: {frameState.UserIdToPlayerId[userId]}");
            }
        }
        
        ASLogger.Instance.Info($"已发送帧同步开始通知给房间 {roomId} 的所有玩家（共 {frameState.PlayerIds.Count} 个），包含完整的 PlayerId 映射关系");
    }
    
    // 注册玩家（内部方法，用于 StartRoomFrameSync）
    private void RegisterPlayer(string userId, string roomId, long playerId, RoomFrameSyncState frameState)
    {
        var registeredPlayer = new RegisteredPlayer
        {
            UserId = userId,
            PlayerId = playerId,
            RoomId = roomId,
            RegisterTime = TimeInfo.Instance.ClientNow(),
            EntityCreated = false
        };
        
        frameState.RegisteredPlayers[userId] = registeredPlayer;
        frameState.UserIdToPlayerId[userId] = playerId;
    }
    
    // 确保所有已创建玩家的输入出现在帧数据中（方案B：去掉 BornInfo）
    private void EnsurePlayerInputsInFrame(RoomFrameSyncState frameState, OneFrameInputs frameInputs)
    {
        foreach (var kvp in frameState.RegisteredPlayers)
        {
            var player = kvp.Value;
            // 确保该 PlayerId 出现在帧输入中（即使玩家还没有发送输入）
            // 实体已在 StartRoomFrameSync 时创建，这里只是确保输入数据存在
            if (!frameInputs.Inputs.ContainsKey(player.PlayerId))
            {
                var emptyInput = LSInput.Create();
                emptyInput.PlayerId = player.PlayerId;
                emptyInput.Frame = frameState.AuthorityFrame;
                // 其他字段保持默认值（空输入）
                
                frameInputs.Inputs[player.PlayerId] = emptyInput;
                
                ASLogger.Instance.Debug($"服务器：在帧 {frameState.AuthorityFrame} 为玩家添加空输入 - PlayerId: {player.PlayerId}, UserId: {player.UserId}");
            }
        }
    }
}
```

**2. 帧处理流程修改**
```csharp
private void ProcessRoomFrame(string roomId, RoomFrameSyncState frameState)
{
    // ... 现有逻辑 ...
    
    // 收集当前帧的所有输入数据
    var frameInputs = frameState.CollectFrameInputs(frameState.AuthorityFrame);
    
    // 确保所有已创建玩家的输入出现在帧数据中
    // 注意：实体已在 StartRoomFrameSync 时创建，这里只是确保输入数据存在
    EnsurePlayerInputsInFrame(frameState, frameInputs);
    
    // 推进逻辑世界
    if (frameState.LogicRoom != null)
    {
        // ... 更新 AuthorityFrame ...
        frameState.LogicRoom.FrameTick(frameInputs);
    }
    
    // 发送帧同步数据
    SendFrameSyncData(roomId, frameState.AuthorityFrame, frameInputs);
}
```

#### 4.3.2 客户端实现要点（优化版：自动注册）

**1. 从 FrameSyncStartNotification 获取 PlayerId 和所有玩家映射**
```csharp
public class MultiplayerGameMode
{
    // 所有玩家的 UserId -> PlayerId 映射（供业务系统使用）
    private Dictionary<string, long> _playerIdMapping = new();
    
    public void OnFrameSyncStartNotification(FrameSyncStartNotification notification)
    {
        // 保存所有玩家的映射关系（供业务系统使用）
        if (notification.playerIdMapping != null)
        {
            _playerIdMapping = new Dictionary<string, long>(notification.playerIdMapping);
            ASLogger.Instance.Info($"收到所有玩家的 PlayerId 映射，共 {_playerIdMapping.Count} 个玩家");
            
            // 打印所有玩家的映射关系（调试用）
            foreach (var kvp in _playerIdMapping)
            {
                ASLogger.Instance.Debug($"  PlayerId映射 - UserId: {kvp.Key}, PlayerId: {kvp.Value}");
            }
        }
        
        // 从映射中获取当前玩家的 PlayerId
        var userId = UserManager.Instance.UserId;
        if (!_playerIdMapping.TryGetValue(userId, out var playerId))
        {
            ASLogger.Instance.Error($"无法从 playerIdMapping 获取 PlayerId - UserId: {userId}");
            return;
        }
        
        // 保存 PlayerId
        PlayerId = playerId;
        if (MainRoom != null)
        {
            MainRoom.MainPlayerId = playerId;
        }
        
        // 根据 playerIdMapping 创建所有玩家实体
        // 注意：实体需要在收到通知时创建，使用服务器端分配的 UniqueId（PlayerId）
        if (MainRoom?.MainWorld != null)
        {
            foreach (var kvp in _playerIdMapping)
            {
                var mappedUserId = kvp.Key;
                var mappedPlayerId = kvp.Value; // 服务器端实体的 UniqueId
                
                // 检查实体是否已存在
                var existingEntity = MainRoom.MainWorld.GetEntity(mappedPlayerId);
                if (existingEntity == null)
                {
                    // 创建实体，使用指定的 UniqueId（PlayerId）
                    // 注意：需要修改 Entity 类或 EntityFactory 支持指定 UniqueId
                    var entity = MainRoom.MainWorld.CreateEntity(1003, mappedPlayerId);
                    if (entity != null && entity.UniqueId == mappedPlayerId)
                    {
                        ASLogger.Instance.Info($"客户端：创建玩家实体 - UserId: {mappedUserId}, PlayerId: {mappedPlayerId}");
                    }
                    else
                    {
                        ASLogger.Instance.Error($"客户端：创建玩家实体失败，UniqueId 不匹配 - UserId: {mappedUserId}, 期望: {mappedPlayerId}, 实际: {entity?.UniqueId ?? -1}");
                    }
                }
                else
                {
                    ASLogger.Instance.Debug($"客户端：玩家实体已存在 - UserId: {mappedUserId}, PlayerId: {mappedPlayerId}");
                }
            }
        }
        
        // 启动 LSController
        if (MainRoom?.LSController != null && !MainRoom.LSController.IsRunning)
        {
            MainRoom.LSController.CreationTime = notification.startTime;
            MainRoom.LSController.Start();
            ASLogger.Instance.Info($"LSController 已启动 - PlayerId: {playerId}");
        }
        
        ASLogger.Instance.Info($"玩家注册成功（自动注册） - UserId: {userId}, PlayerId: {playerId}");
    }
    
    /// <summary>
    /// 通过 UserId 获取 PlayerId（供业务系统使用）
    /// </summary>
    public long GetPlayerIdByUserId(string userId)
    {
        return _playerIdMapping.TryGetValue(userId, out var playerId) ? playerId : -1;
    }
    
    /// <summary>
    /// 通过 PlayerId 获取 UserId（供业务系统使用）
    /// </summary>
    public string GetUserIdByPlayerId(long playerId)
    {
        return _playerIdMapping.FirstOrDefault(kvp => kvp.Value == playerId).Key;
    }
    
    /// <summary>
    /// 获取所有玩家的映射关系（供业务系统使用）
    /// </summary>
    public Dictionary<string, long> GetAllPlayerIdMapping()
    {
        return new Dictionary<string, long>(_playerIdMapping);
    }
}
```

**2. 帧处理逻辑修改（方案B：去掉 BornInfo）**
```csharp
public void FrameTick(OneFrameInputs oneFrameInputs)
{
    // ... 现有逻辑 ...
    
    foreach (var pairs in oneFrameInputs.Inputs)
    {
        var input = pairs.Value;
        var playerId = pairs.Key;
        var entity = MainWorld.GetEntity(playerId);
        
        if (entity != null)
        {
            // 实体已存在，更新输入
            var inputComponent = entity.GetComponent<LSInputComponent>();
            if (inputComponent != null)
            {
                inputComponent.SetInput(input);
            }
        }
        else
        {
            // 实体不存在，且输入中有该 PlayerId，创建实体
            // 服务器端：需要验证该 PlayerId 是否已注册
            // 客户端：直接创建（帧数据中的 PlayerId 都是服务器下发的，应该是有效的）
            
            #if SERVER
            // 服务器端：验证 PlayerId 是否已注册
            if (!IsPlayerRegistered(playerId))
            {
                ASLogger.Instance.Warning($"服务器：收到未注册玩家的输入，忽略 - PlayerId: {playerId}");
                continue;
            }
            #endif
            
            ASLogger.Instance.Info($"检测到新玩家，创建实体 - PlayerId: {playerId}, Frame: {input.Frame}");
            
            var createdPlayerId = AddPlayer(playerId);
            if (createdPlayerId == playerId)
            {
                // 创建成功，发布事件
                var newPlayerEventData = new NewPlayerEventData(playerId, 0);
                EventSystem.Instance.Publish(newPlayerEventData);
                
                #if SERVER
                // 服务器端：标记为已创建
                MarkPlayerEntityCreated(playerId);
                #endif
                
                ASLogger.Instance.Info($"玩家实体创建成功 - PlayerId: {playerId}");
            }
            else
            {
                ASLogger.Instance.Error($"玩家实体创建失败，PlayerId不匹配 - 期望: {playerId}, 实际: {createdPlayerId}");
            }
        }
    }
    
    // ... 更新世界 ...
    // 注意：World.CreateEntity() 会自动发布 EntityCreatedEventData 事件
    // Stage 会监听该事件并自动创建 EntityView，无需手动调用 SyncNewEntities()
}
```

**3. 移除旧的创建逻辑**
- 移除 `RequestCreatePlayer()` 中发送 `BornInfo` 的逻辑
- 移除 `FrameTick()` 中检测 `BornInfo != 0` 的旧逻辑（改为检测实体是否存在）
- **可选**：从 `LSInput` 协议中移除 `BornInfo` 字段（如果确定不再使用）

### 4.4 待实现的功能清单

#### 4.4.1 协议层
- [ ] **修改**：在 `FrameSyncStartNotification` 中添加 `playerIdMapping` 字段
  - 类型：`Dictionary<string, long>`（UserId -> PlayerId）
  - 内容：包含房间内所有玩家的完整映射关系
  - 用途：
    - 客户端获取自己的 PlayerId
    - 业务系统通过 UserId 查找 PlayerId
    - 业务系统通过 PlayerId 查找 UserId
    - 显示玩家列表、处理玩家交互等
- [ ] ~~定义 `PlayerRegisterRequest` 消息~~（优化后不需要）
- [ ] ~~定义 `PlayerRegisterResponse` 消息~~（优化后不需要）

#### 4.4.2 服务器端
- [ ] **修改**：在 `StartRoomFrameSync()` 中自动为房间内所有玩家分配 PlayerId
- [ ] 实现 `RegisterPlayer()` 分配 PlayerId 并维护注册状态
- [ ] 修改 `RoomFrameSyncState` 添加 `RegisteredPlayers` 字段
- [ ] 实现 `ProcessPendingPlayerCreations()` 检查待创建玩家
- [ ] 修改 `ProcessRoomFrame()` 在帧处理中添加创建指令
- [ ] 实现重连检测逻辑（重连时返回已分配的 PlayerId）
- [ ] **修改**：在 `SendFrameSyncStartNotification()` 中包含 `playerIdMapping`

#### 4.4.3 客户端端
- [ ] ~~实现 `RequestPlayerRegister()` 发送注册请求~~（优化后不需要）
- [ ] ~~实现 `OnPlayerRegisterResponse()` 处理注册响应~~（优化后不需要）
- [ ] **修改**：在 `OnFrameSyncStartNotification()` 中从通知获取 PlayerId
- [ ] 修改 `FrameTick()` 检测创建指令（通过 `PlayerId` 是否存在判断）
- [ ] 移除旧的 `RequestCreatePlayer()` 逻辑
- [ ] 移除旧的 `BornInfo` 发送逻辑
- [ ] **可选**：从 `LSInput` 协议中移除 `BornInfo` 字段

#### 4.4.4 清理工作
- [ ] 移除客户端直接创建玩家的代码
- [ ] 清理 `BornInfo` 相关的旧逻辑
- [ ] **可选**：从 `LSInput` 协议中移除 `BornInfo` 字段
- [ ] 更新相关文档和注释

### 4.5 测试要点

1. **正常流程测试**
   - 客户端注册 → 获取 PlayerId → 收到创建指令 → 创建实体

2. **多玩家测试**
   - 多个玩家同时注册 → 服务器正确分配不同 PlayerId → 所有玩家都能创建

3. **重连测试**
   - 客户端断线 → 重连 → 获取相同 PlayerId → 状态恢复

4. **回放测试**
   - 请求回放数据 → 逐帧执行 → 检测创建指令 → 正确创建所有实体

5. **边界情况**
   - 房间已满时的注册处理
   - 帧同步开始前注册的处理
   - 创建指令丢失的处理

## 六、实现细节补充

### 6.1 关键实现细节

#### 6.1.1 PlayerId 与 Entity.UniqueId 的关系

**核心设计：**
- **PlayerId 就是 Entity.UniqueId**
- 先创建实体，获得 UniqueId，然后将 UniqueId 作为 PlayerId 分配给 UserId
- 这样 `Entity.UniqueId == PlayerId`，无需额外映射

**实现流程：**
```csharp
// 服务器端：在 StartRoomFrameSync 时创建实体
public void StartRoomFrameSync(string roomId)
{
    // ... 创建逻辑房间 ...
    
    foreach (var userId in playerNames)
    {
        // 1. 先创建实体，获得 UniqueId
        var entity = logicRoom.MainWorld.CreateEntity(1003);
        var uniqueId = entity.UniqueId; // Entity 自动生成的 UniqueId
        
        // 2. 将 UniqueId 作为 PlayerId 分配给 UserId
        var registeredPlayer = new RegisteredPlayer
        {
            UserId = userId,
            PlayerId = uniqueId, // PlayerId = UniqueId
            EntityCreated = true
        };
        
        frameState.RegisteredPlayers[userId] = registeredPlayer;
        frameState.UserIdToPlayerId[userId] = uniqueId;
    }
    
    // 3. 发送 FrameSyncStartNotification，包含 UserId -> PlayerId（UniqueId）映射
    SendFrameSyncStartNotification(roomId, frameState);
}

// 客户端：通过帧输入创建实体
public void FrameTick(OneFrameInputs oneFrameInputs)
{
    foreach (var pairs in oneFrameInputs.Inputs)
    {
        var playerId = pairs.Key; // 从服务器获取的 PlayerId
        var entity = MainWorld.GetEntity(playerId);
        
        if (entity == null)
        {
            // 创建实体，实体的 UniqueId 应该等于 playerId
            // 注意：客户端创建实体时，UniqueId 是自动生成的
            // 需要确保客户端和服务器使用相同的 UniqueId
            // 这需要在帧同步开始时，服务器先创建实体并告知客户端
            var createdEntity = MainWorld.CreateEntity(1003);
            // 问题：createdEntity.UniqueId 可能与 playerId 不一致
        }
    }
}
```

**关键问题：**
- 服务器端：实体在 `StartRoomFrameSync` 时创建，UniqueId 是自动生成的
- 客户端：实体通过帧输入创建，UniqueId 也是自动生成的
- **问题**：客户端和服务器端的 UniqueId 生成是独立的，可能不一致

**解决方案：**
1. **客户端在收到 FrameSyncStartNotification 时创建实体**（推荐）
   - 服务器在 `StartRoomFrameSync` 时创建所有玩家实体，获得 UniqueId
   - 服务器在 `FrameSyncStartNotification` 中包含 `playerIdMapping`（UserId -> UniqueId）
   - 客户端收到通知后，根据 `playerIdMapping` 创建对应实体，使用指定的 UniqueId
   - 需要修改 `Entity` 类或 `EntityFactory`，支持在创建时指定 UniqueId

2. **修改 Entity 类支持指定 UniqueId**
   ```csharp
   // 方案1：修改 Entity 构造函数
   public Entity(long? specifiedUniqueId = null)
   {
       if (specifiedUniqueId.HasValue)
       {
           UniqueId = specifiedUniqueId.Value;
           // 更新 _nextId 以确保后续生成的 ID 不会冲突
           if (_nextId <= specifiedUniqueId.Value)
           {
               _nextId = specifiedUniqueId.Value + 1;
           }
       }
       else
       {
           UniqueId = _nextId++;
       }
       CreationTime = DateTime.Now;
   }
   
   // 方案2：修改 EntityFactory.CreateEntity 支持指定 UniqueId
   public Entity CreateEntity(int entityConfigId, World world, long? specifiedUniqueId = null)
   {
       var entity = new Entity(specifiedUniqueId);
       // ... 其他初始化逻辑 ...
       return entity;
   }
   ```

3. **客户端创建实体时使用指定的 UniqueId**
   ```csharp
   // 在收到 FrameSyncStartNotification 时
   foreach (var kvp in notification.playerIdMapping)
   {
       var userId = kvp.Key;
       var playerId = kvp.Value; // 服务器端实体的 UniqueId
       
       // 创建实体，使用指定的 UniqueId
       var entity = MainWorld.CreateEntity(1003, playerId);
       // 确保 entity.UniqueId == playerId
   }
   ```

#### 6.1.3 创建指令的幂等性

**问题：**
- 如果客户端重复收到创建指令，可能会重复创建实体
- 如果服务器重复下发创建指令，需要确保幂等性

**解决方案：**
```csharp
// 客户端 FrameTick 中的检查（方案B：去掉 BornInfo）
else // 实体不存在
{
    // 检查实体是否已存在（幂等性检查，虽然理论上不会走到这里）
    var existingEntity = MainWorld.GetEntity(playerId);
    if (existingEntity != null)
    {
        ASLogger.Instance.Warning($"玩家实体已存在，跳过创建 - PlayerId: {playerId}");
        continue; // 跳过，不重复创建
    }
    
    // 创建实体
    var createdPlayerId = AddPlayer(playerId);
    // ...
}

// 服务器端 ProcessPendingPlayerCreations 中的检查
if (!player.EntityCreated)
{
    // 检查实体是否已存在（防止重复创建）
    var existingEntity = frameState.LogicRoom?.MainWorld?.GetEntity(player.PlayerId);
    if (existingEntity != null)
    {
        ASLogger.Instance.Warning($"服务器：玩家实体已存在，标记为已创建 - PlayerId: {player.PlayerId}");
        player.EntityCreated = true;
        continue;
    }
    
    // 添加创建指令
    // ...
}
```

### 6.2 错误处理机制

#### 6.2.1 注册失败处理

**错误情况：**
1. 房间不存在或已关闭
2. 房间已满
3. 用户未登录
4. 用户已在其他房间注册

**处理流程：**
```csharp
public PlayerRegisterResponse HandlePlayerRegisterRequest(PlayerRegisterRequest request)
{
    // 1. 验证用户登录状态
    if (!_userManager.IsUserLoggedIn(request.UserId))
    {
        return new PlayerRegisterResponse
        {
            Success = false,
            Message = "用户未登录",
            PlayerId = 0
        };
    }
    
    // 2. 验证房间存在
    var roomInfo = _roomManager.GetRoom(request.RoomId);
    if (roomInfo == null)
    {
        return new PlayerRegisterResponse
        {
            Success = false,
            Message = "房间不存在",
            PlayerId = 0
        };
    }
    
    // 3. 检查房间状态
    if (roomInfo.Status != RoomStatus.Playing)
    {
        return new PlayerRegisterResponse
        {
            Success = false,
            Message = "房间未开始或已结束",
            PlayerId = 0
        };
    }
    
    // 4. 检查房间是否已满
    var frameState = GetRoomFrameState(request.RoomId);
    if (frameState != null && frameState.RegisteredPlayers.Count >= roomInfo.MaxPlayers)
    {
        return new PlayerRegisterResponse
        {
            Success = false,
            Message = "房间已满",
            PlayerId = 0
        };
    }
    
    // 5. 检查是否已在其他房间注册
    if (IsUserRegisteredInOtherRoom(request.UserId, request.RoomId))
    {
        return new PlayerRegisterResponse
        {
            Success = false,
            Message = "用户已在其他房间注册",
            PlayerId = 0
        };
    }
    
    // 6. 注册玩家
    try
    {
        var playerId = RegisterPlayer(request.UserId, request.RoomId);
        return new PlayerRegisterResponse
        {
            Success = true,
            Message = "注册成功",
            PlayerId = playerId,
            RoomId = request.RoomId,
            Timestamp = TimeInfo.Instance.ClientNow()
        };
    }
    catch (Exception ex)
    {
        ASLogger.Instance.Error($"注册玩家失败: {ex.Message}");
        return new PlayerRegisterResponse
        {
            Success = false,
            Message = $"注册失败: {ex.Message}",
            PlayerId = 0
        };
    }
}
```

#### 6.2.2 创建实体失败处理

**错误情况：**
1. 实体配置不存在（EntityConfigId=1003）
2. 世界未初始化
3. 实体创建过程中异常

**处理流程：**
```csharp
// 服务器端
private void ProcessPendingPlayerCreations(RoomFrameSyncState frameState, OneFrameInputs frameInputs)
{
    foreach (var kvp in frameState.RegisteredPlayers.ToList()) // 使用 ToList 避免迭代时修改
    {
        var player = kvp.Value;
        if (!player.EntityCreated)
        {
            try
            {
                // 检查实体是否已存在
                var existingEntity = frameState.LogicRoom?.MainWorld?.GetEntity(player.PlayerId);
                if (existingEntity != null)
                {
                    player.EntityCreated = true;
                    continue;
                }
                
                // 添加创建指令
                var createInput = LSInput.Create();
                createInput.PlayerId = player.PlayerId;
                createInput.BornInfo = player.PlayerId;
                createInput.Frame = frameState.AuthorityFrame;
                
                frameInputs.Inputs[player.PlayerId] = createInput;
                
                // 标记为已创建（在 FrameTick 执行后确认）
                // 注意：这里只是标记指令已下发，实际创建在 FrameTick 中
                player.CreateFrame = frameState.AuthorityFrame;
                
                ASLogger.Instance.Info($"服务器：下发创建玩家指令 - PlayerId: {player.PlayerId}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理玩家创建指令失败 - PlayerId: {player.PlayerId}, 错误: {ex.Message}");
                // 不标记为已创建，下次帧处理时重试
            }
        }
    }
}

// Room.FrameTick 中的创建逻辑（方案B：去掉 BornInfo）
else // 实体不存在
{
    try
    {
        #if SERVER
        // 服务器端：验证 PlayerId 是否已注册
        if (!IsPlayerRegistered(playerId))
        {
            ASLogger.Instance.Warning($"收到未注册玩家的输入，忽略 - PlayerId: {playerId}");
            continue;
        }
        #endif
        
        var createdPlayerId = AddPlayer(playerId);
        if (createdPlayerId == playerId)
        {
            // 创建成功
            var newPlayerEventData = new NewPlayerEventData(playerId, 0);
            EventSystem.Instance.Publish(newPlayerEventData);
            
            // 服务器端：标记为已创建
            #if SERVER
            MarkPlayerEntityCreated(playerId);
            #endif
        }
        else
        {
            ASLogger.Instance.Error($"创建玩家失败 - 期望PlayerId: {playerId}, 实际: {createdPlayerId}");
        }
    }
    catch (Exception ex)
    {
        ASLogger.Instance.Error($"创建玩家实体异常 - PlayerId: {playerId}, 错误: {ex.Message}");
        // 不处理，下次帧处理时重试（如果 PlayerId 还在输入中）
    }
}
```

### 6.3 边界情况处理

#### 6.3.1 房间已满

**场景：**
- 客户端发送注册请求时，房间刚好满了

**处理：**
```csharp
// 在 HandlePlayerRegisterRequest 中检查
if (frameState.RegisteredPlayers.Count >= roomInfo.MaxPlayers)
{
    return new PlayerRegisterResponse
    {
        Success = false,
        Message = "房间已满",
        PlayerId = 0
    };
}
```

#### 6.3.2 帧同步开始前注册

**场景：**
- 客户端在帧同步开始前就注册了
- 服务器在帧同步开始后才开始处理创建指令

**处理：**
```csharp
// 服务器端：在 StartRoomFrameSync 时检查已注册的玩家
public void StartRoomFrameSync(string roomId)
{
    // ... 启动帧同步 ...
    
    // 检查是否有待创建的玩家
    var frameState = GetRoomFrameState(roomId);
    if (frameState != null && frameState.RegisteredPlayers.Count > 0)
    {
        ASLogger.Instance.Info($"房间 {roomId} 启动帧同步，已有 {frameState.RegisteredPlayers.Count} 个玩家待创建");
    }
}

// 在 ProcessRoomFrame 中，第一帧就会处理待创建的玩家
private void ProcessRoomFrame(string roomId, RoomFrameSyncState frameState)
{
    // 第一帧或后续帧都会检查待创建的玩家
    ProcessPendingPlayerCreations(frameState, frameInputs);
    // ...
}
```

#### 6.3.3 创建指令丢失

**场景：**
- 网络问题导致创建指令丢失
- 客户端未收到创建指令

**处理：**
```csharp
// 方案1：服务器在后续帧中重试（不推荐，会导致重复创建）
// 方案2：客户端超时后重新请求注册（推荐）

// 客户端：注册后设置超时
private long _registerTime = 0;
private const long REGISTER_TIMEOUT = 5000; // 5秒超时

public void OnPlayerRegistered(long playerId)
{
    PlayerId = playerId;
    _registerTime = TimeInfo.Instance.ClientNow();
    // 启动超时检查
    StartCoroutine(CheckPlayerCreationTimeout());
}

private IEnumerator CheckPlayerCreationTimeout()
{
    while (true)
    {
        yield return new WaitForSeconds(1f);
        
        // 检查是否超时
        if (TimeInfo.Instance.ClientNow() - _registerTime > REGISTER_TIMEOUT)
        {
            // 检查实体是否已创建
            var entity = MainRoom?.MainWorld?.GetEntity(PlayerId);
            if (entity == null)
            {
                ASLogger.Instance.Warning($"玩家创建超时，重新请求注册 - PlayerId: {PlayerId}");
                // 重新发送注册请求
                RequestPlayerRegister();
                yield break;
            }
            else
            {
                // 实体已创建，停止检查
                yield break;
            }
        }
        
        // 检查实体是否已创建
        var checkEntity = MainRoom?.MainWorld?.GetEntity(PlayerId);
        if (checkEntity != null)
        {
            // 实体已创建，停止检查
            yield break;
        }
    }
}
```

#### 6.3.4 玩家离开/注销

**场景：**
- 玩家主动离开房间
- 玩家断线
- 服务器踢出玩家

**处理流程：**
```csharp
// 1. 定义玩家注销协议（可选，或使用现有协议）
message PlayerUnregisterRequest
{
    string UserId = 1;
    string RoomId = 2;
}

// 2. 服务器端处理注销
public void UnregisterPlayer(string userId, string roomId)
{
    var frameState = GetRoomFrameState(roomId);
    if (frameState == null) return;
    
    // 移除注册信息
    if (frameState.RegisteredPlayers.TryGetValue(userId, out var player))
    {
        // 销毁玩家实体（如果已创建）
        if (player.EntityCreated && frameState.LogicRoom != null)
        {
            var entity = frameState.LogicRoom.MainWorld?.GetEntity(player.PlayerId);
            if (entity != null)
            {
                frameState.LogicRoom.MainWorld.DestroyEntity(player.PlayerId);
            }
        }
        
        // 移除注册信息
        frameState.RegisteredPlayers.Remove(userId);
        frameState.UserIdToPlayerId.Remove(userId);
        
        ASLogger.Instance.Info($"玩家注销 - UserId: {userId}, PlayerId: {player.PlayerId}");
    }
}

// 3. 客户端断线检测
// 在 NetworkManager 中检测断线，自动清理本地状态
public void OnDisconnected()
{
    // 清理玩家状态
    if (MultiplayerGameMode.Instance != null)
    {
        MultiplayerGameMode.Instance.OnPlayerDisconnected();
    }
}
```

### 6.4 时序图

#### 6.4.1 正常流程时序

```
客户端                          服务器
  |                              |
  |-- PlayerRegisterRequest ---->|
  |                              |-- RegisterPlayer()
  |                              |  分配 PlayerId
  |<-- PlayerRegisterResponse ---|
  |  (PlayerId = 1)              |
  |                              |
  |-- 等待帧同步开始              |
  |                              |
  |<-- FrameSyncStartNotification|
  |  启动 LSController           |
  |                              |
  |<-- FrameSyncData (Frame 0) ---|
  |  包含创建指令                 |
  |  (BornInfo = 1)              |
  |                              |
  |-- FrameTick()                |
  |  检测到创建指令               |
  |  创建实体 (UniqueId = 1)     |
  |  发布 EntityCreatedEventData |
  |                              |
  |-- Stage 创建 EntityView      |
  |                              |
```

#### 6.4.2 重连流程时序

```
客户端                          服务器
  |                              |
  |-- 断线                        |
  |                              |
  |-- 重连                        |
  |-- 重新登录                    |
  |                              |
  |-- PlayerRegisterRequest ---->|
  |  (UserId = "user123")        |
  |                              |-- 检测到已注册
  |                              |  返回现有 PlayerId
  |<-- PlayerRegisterResponse ---|
  |  (PlayerId = 1)              |
  |                              |
  |-- 请求快照                    |
  |<-- FrameSyncSnapshot --------|
  |  (Frame = 100, World State)  |
  |                              |
  |-- 回放缺失的帧                |
  |  从 Frame 0 到 Frame 100     |
  |  检测创建指令，重建实体       |
  |                              |
```

### 6.5 性能考虑

#### 6.5.1 注册状态存储

**当前设计：**
- 使用 `Dictionary<string, RegisteredPlayer>` 存储注册信息
- Key: UserId, Value: RegisteredPlayer

**优化建议：**
- 如果房间玩家数量固定且较小（< 100），可以使用数组
- 如果需要频繁查询 PlayerId，可以维护反向索引 `Dictionary<long, string>` (PlayerId -> UserId)

#### 6.5.2 创建指令检查

**当前设计：**
- 每帧都遍历所有注册玩家检查是否已创建

**优化建议：**
- 使用队列存储待创建玩家，创建后移除
- 或者：使用标志位快速跳过已创建的玩家

```csharp
// 优化版本
private Queue<RegisteredPlayer> _pendingCreations = new();

public void RegisterPlayer(...)
{
    // ...
    _pendingCreations.Enqueue(registeredPlayer);
}

private void ProcessPendingPlayerCreations(...)
{
    while (_pendingCreations.Count > 0)
    {
        var player = _pendingCreations.Peek();
        if (player.EntityCreated)
        {
            _pendingCreations.Dequeue();
            continue;
        }
        
        // 处理创建指令
        // ...
        _pendingCreations.Dequeue();
    }
}
```

### 6.6 调试和日志

#### 6.6.1 关键日志点

**服务器端：**
```csharp
// 注册时
ASLogger.Instance.Info($"玩家注册 - UserId: {userId}, PlayerId: {playerId}, RoomId: {roomId}");

// 下发创建指令时
ASLogger.Instance.Info($"下发创建玩家指令 - PlayerId: {playerId}, Frame: {frame}");

// 实体创建成功时
ASLogger.Instance.Info($"玩家实体创建成功 - PlayerId: {playerId}, EntityId: {entity.UniqueId}");
```

**客户端：**
```csharp
// 发送注册请求时
ASLogger.Instance.Info($"发送玩家注册请求 - UserId: {userId}, RoomId: {roomId}");

// 收到注册响应时
ASLogger.Instance.Info($"玩家注册成功 - PlayerId: {playerId}");

// 收到创建指令时
ASLogger.Instance.Info($"收到创建玩家指令 - PlayerId: {playerId}, Frame: {frame}");

// 实体创建成功时
ASLogger.Instance.Info($"玩家实体创建成功 - PlayerId: {playerId}");
```

#### 6.6.2 调试工具

**建议添加：**
1. 服务器端：`/debug players <roomId>` 命令，显示房间内所有注册玩家
2. 客户端：Unity Editor 窗口显示当前 PlayerId 和实体状态
3. 网络消息监控：记录所有 PlayerRegisterRequest/Response 消息

