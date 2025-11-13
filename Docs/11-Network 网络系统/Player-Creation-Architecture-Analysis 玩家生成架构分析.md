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

## 三、重构方案：世界快照一步到位

#### 核心思想
- **服务器在开始游戏时创建所有玩家实体**
- **服务器保存第0帧的世界快照**
- **客户端通过 FrameSyncStartNotification 接收世界快照并恢复状态**（一步到位）
- **PlayerId 就是 Entity.UniqueId，服务器创建后直接分配**

**方案说明：**
- 服务器在 `StartRoomFrameSync` 时创建所有玩家实体，保存第0帧快照
- 客户端收到 `FrameSyncStartNotification` 时，直接反序列化世界快照，恢复所有实体状态
- 无需通过帧输入创建实体，一步到位，状态完全一致
- 支持中途加入：服务器创建实体后发送快照，客户端直接恢复
- 兼容回放：快照机制与断线重连/回放一致

#### 详细流程设计

##### 3.1 客户端流程（世界快照方案）

**阶段1：游戏开始**
```
1. 进入房间后（收到 GameStartNotification）
   MultiplayerGameMode.OnGameStartNotification()
     ↓
   创建 Room 和 Stage
     ↓
   等待帧同步开始
```

**阶段2：帧同步开始并接收世界快照**
```
2. 收到 FrameSyncStartNotification
   FrameSyncHandler.OnFrameSyncStartNotification()
     ↓
   从通知中获取世界快照数据
   - notification.worldSnapshot (bytes)
   - notification.playerIdMapping (Dictionary<string, long>)
     ↓
   反序列化 World
   - 使用 MemoryPackHelper.Deserialize() 反序列化 worldSnapshot
   - 替换 MainRoom.MainWorld
   - 重建 World 的引用关系（Room、Systems等）
     ↓
   从 playerIdMapping 获取 PlayerId
   - 查找自己的 UserId 对应的 PlayerId
   - 设置 PlayerId 和 MainRoom.MainPlayerId
     ↓
   启动 LSController
   - 设置 CreationTime = notification.startTime
   - 将快照数据加载到 FrameBuffer（用于回滚）
   - LSController.Start()
     ↓
   Stage.OnEntityCreated()（自动监听 EntityCreatedEventData）
   - 遍历 World.Entities，为每个实体创建 EntityView
   - EntityViewFactory.CreateEntityView()
   - 创建 EntityView
```

**阶段3：后续帧处理**
```
3. 收到 FrameSyncData（后续帧）
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
   Room.FrameTick()
   - 正常处理帧输入
   - 更新实体输入组件
   - 实体已存在，只需更新状态
```

##### 3.2 服务器流程（世界快照方案）

**阶段1：开始游戏时创建所有玩家实体并保存快照**
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
   - 启动 LSController
     ↓
   创建所有玩家实体
   foreach (userId in playerNames.OrderBy(x => x))
   {
     var playerEntity = frameState.LogicRoom.MainWorld.CreateEntity(1003);
     var playerId = playerEntity.UniqueId; // PlayerId 就是 Entity.UniqueId
     
     // 记录 PlayerId 映射
     frameState.UserIdToPlayerId[userId] = playerId;
     frameState.LogicRoom.Players.Add(playerId);
     
     ASLogger.Instance.Info($"服务器：创建玩家实体 - UserId: {userId}, PlayerId: {playerId}");
   }
     ↓
   保存第0帧快照
   - frameState.LogicRoom.LSController.AuthorityFrame = 0
   - frameState.LogicRoom.LSController.FrameBuffer.MoveForward(0)
   - frameState.LogicRoom.LSController.SaveState()
   - 获取快照数据：snapshotBuffer = FrameBuffer.Snapshot(0)
   - 序列化为 bytes：worldSnapshotData
     ↓
   发送 FrameSyncStartNotification
   - 包含 worldSnapshot（世界快照数据）
   - 包含 playerIdMapping（UserId -> PlayerId 映射）
   - 包含 playerIds（UserId列表）
   - 发送给房间内所有玩家
```

**阶段2：后续帧处理**
```
2. ProcessRoomFrame()（后续帧）
   - AuthorityFrame++
   - CollectFrameInputs() 收集输入
   - Room.FrameTick() 执行帧逻辑
   - SendFrameSyncData() 发送帧同步数据
   - 实体已存在，只需更新状态
```

#### 3.3 关键数据结构

##### 协议：FrameSyncStartNotification（新增字段）
```protobuf
message FrameSyncStartNotification
{
    string roomId = 1;
    int32 frameRate = 2;
    int32 frameInterval = 3;
    int64 startTime = 4;
    repeated string playerIds = 5;           // 玩家ID列表（UserId）
    bytes worldSnapshot = 6;                // 世界快照数据（第0帧）
    map<string, int64> playerIdMapping = 7; // UserId -> PlayerId 映射
}
```

##### 服务器端：玩家状态
```csharp
public class RoomFrameSyncState
{
    // ... 现有字段 ...
    
    // UserId -> PlayerId 映射（实体创建后确定）
    public Dictionary<string, long> UserIdToPlayerId { get; set; } = new();
}
```

##### 世界快照数据
```csharp
// 世界快照就是 World 对象的序列化数据
// 使用 MemoryPack 序列化，包含：
// - World 的所有属性（Entities、Systems、状态等）
// - 所有实体的完整状态（Components、Capabilities等）
// - 第0帧的完整世界状态

// 序列化方式：
byte[] worldSnapshotData = MemoryPackHelper.Serialize(world);

// 反序列化方式：
World world = MemoryPackHelper.Deserialize(typeof(World), worldSnapshotData, 0, worldSnapshotData.Length) as World;
```

#### 3.4 断线重连支持

**客户端重连流程：**
```
1. 客户端断线后重连
   - 重新建立连接
   - 重新登录（获取UserId）
   - 重新加入房间（如果房间还在）
     ↓
2. 如果游戏已开始，服务器会重新发送 FrameSyncStartNotification
   - 通知中包含 worldSnapshot（当前帧的世界快照）
   - 通知中包含 playerIdMapping
   - 客户端从映射中获取自己的 PlayerId
     ↓
3. 恢复世界状态
   - 反序列化 worldSnapshot，恢复 World 状态
   - 替换 MainRoom.MainWorld
   - 重建所有实体的 EntityView
     ↓
4. 回放缺失的帧（可选）
   - 从快照帧开始，逐帧回放
   - 通过帧输入数据，更新实体状态
   - 同步到当前帧
```

**服务器重连处理：**
```csharp
// 在 StartRoomFrameSync 中处理重连
public void StartRoomFrameSync(string roomId)
{
    var roomInfo = _roomManager.GetRoom(roomId);
    var playerNames = roomInfo.PlayerNames;
    
    // 检查是否已有房间状态（重连情况）
    if (_roomFrameStates.TryGetValue(roomId, out var existingState))
    {
        // 重连：使用现有的世界状态和 PlayerId 映射
        // 保存当前帧的快照
        var currentFrame = existingState.AuthorityFrame;
        existingState.LogicRoom.LSController.SaveState();
        var snapshotBuffer = existingState.LogicRoom.LSController.FrameBuffer.Snapshot(currentFrame);
        byte[] worldSnapshotData = new byte[snapshotBuffer.Length];
        snapshotBuffer.Read(worldSnapshotData, 0, (int)snapshotBuffer.Length);
        
        // 发送 FrameSyncStartNotification，包含当前帧快照
        SendFrameSyncStartNotification(roomId, existingState, worldSnapshotData);
    }
    else
    {
        // 新游戏：创建所有玩家实体并保存快照
        // ... 正常流程 ...
    }
}
```

#### 3.5 回放功能支持

**回放流程：**
```
1. 客户端请求回放
   - 发送 ReplayRequest（指定起始帧和结束帧）
     ↓
2. 服务器返回回放数据
   - 返回起始帧的世界快照（worldSnapshot）
   - 返回指定帧范围内的所有帧输入数据（OneFrameInputs）
     ↓
3. 客户端回放
   - 反序列化起始帧的世界快照，恢复 World 状态
   - 从起始帧开始，逐帧执行
   - 通过帧输入数据，更新实体状态
   - 执行所有帧逻辑
   - 同步到结束帧
```

**回放数据结构：**
```csharp
public class ReplayData
{
    public int StartFrame { get; set; }
    public int EndFrame { get; set; }
    public byte[] WorldSnapshotAtStartFrame { get; set; } // 起始帧的世界快照（bytes）
    public Dictionary<int, OneFrameInputs> FrameInputs { get; set; } // 帧号 -> 输入数据
}
```

#### 3.6 优点
- **一步到位**：客户端直接获得完整世界状态，无需通过帧输入创建实体
- **状态一致**：服务器是唯一创建源，客户端只恢复状态，保证完全一致
- **支持中途加入**：服务器创建实体后发送快照，客户端直接恢复，无需依赖创建顺序
- **支持重连**：通过快照机制支持断线重连，恢复完整世界状态
- **支持回放**：快照机制与回放一致，可以完整回放
- **简化逻辑**：无需通过帧输入创建实体，逻辑更简单清晰

#### 3.7 需要实现的功能

1. **协议定义**
   - **修改**：在 `FrameSyncStartNotification` 中添加字段
     - `worldSnapshot` (bytes)：世界快照数据（第0帧）
     - `playerIdMapping` (map<string, int64>)：UserId -> PlayerId 映射
   - 用途：
     - `worldSnapshot`：客户端直接恢复世界状态
     - `playerIdMapping`：方便客户端业务系统使用，例如显示玩家列表、处理玩家交互等

2. **服务器端**
   - **修改**：在 `StartRoomFrameSync` 时创建所有玩家实体
   - **新增**：保存第0帧的世界快照
   - **修改**：在 `SendFrameSyncStartNotification` 中包含世界快照和 PlayerId 映射
   - 重连检测和处理（重连时发送当前帧快照）

3. **客户端端**
   - **修改**：在 `OnFrameSyncStartNotification` 中反序列化世界快照
   - **修改**：替换 `MainRoom.MainWorld` 为快照恢复的 World
   - **修改**：从 `playerIdMapping` 中获取 PlayerId
   - **修改**：将快照数据加载到 FrameBuffer（用于回滚）
   - **修改**：为快照中的所有实体创建 EntityView
   - 移除旧的 BornInfo 发送逻辑

4. **清理工作**
   - 移除旧的 BornInfo 发送逻辑（RequestCreatePlayer）
   - 清理客户端通过帧输入创建玩家的代码
   - **可选**：从 `LSInput` 协议中移除 `BornInfo` 字段（如果确定不再使用）

## 四、选定方案：世界快照一步到位

### 4.1 方案概述

**核心设计：**
- 服务器创建：服务器在 `StartRoomFrameSync` 时创建所有玩家实体
- 世界快照：服务器保存第0帧的世界快照（World 序列化数据）
- 状态恢复：客户端通过 `FrameSyncStartNotification` 接收世界快照并恢复状态
- PlayerId 分配：PlayerId 就是 Entity.UniqueId，服务器创建后直接分配

### 4.2 关键设计决策

#### 4.2.1 PlayerId 分配时机
- **时机**：在 `StartRoomFrameSync` 时创建实体后立即分配
- **方式**：PlayerId 就是 Entity.UniqueId，服务器创建实体后自动获得
- **存储**：服务器维护 `UserId -> PlayerId` 映射，支持重连

#### 4.2.2 创建时机
- **时机**：在 `StartRoomFrameSync` 时立即创建所有玩家实体
- **方式**：服务器按 UserId 顺序创建实体，保存第0帧快照
- **优势**：一步到位，无需通过帧输入创建

#### 4.2.3 世界快照机制
- **快照内容**：World 对象的完整序列化数据（使用 MemoryPack）
- **快照时机**：第0帧（所有玩家实体创建后）
- **快照大小**：可能较大，需要考虑网络传输和压缩
- **快照压缩**：可以使用 GZip 压缩（如断线重连文档中提到的）

#### 4.2.4 重连处理
- **检测**：服务器检查房间是否已有状态（`_roomFrameStates`）
- **处理**：如果已有状态，保存当前帧快照并发送给客户端
- **状态恢复**：客户端反序列化快照，恢复完整世界状态

#### 4.2.5 回放支持
- **数据来源**：起始帧的世界快照 + 帧输入数据
- **回放流程**：反序列化起始帧快照 → 逐帧执行 → 更新实体状态
- **完整性**：快照包含完整世界状态，可以完整回放

### 4.3 实现细节

#### 4.3.1 服务器端实现要点

**1. 创建所有玩家实体并保存快照**
```csharp
public class FrameSyncManager
{
    // 在开始帧同步时创建所有玩家实体并保存快照
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
            AuthorityFrame = 0, // 初始为 0
            IsActive = true,
            StartTime = startTime,
            PlayerIds = new List<string>(roomInfo.PlayerNames),
            UserIdToPlayerId = new Dictionary<string, long>(),
            LogicRoom = logicRoom
        };
        
        // 创建所有玩家实体（按 UserId 顺序，确保 UniqueId 一致）
        foreach (var userId in roomInfo.PlayerNames.OrderBy(x => x))
        {
            var playerEntity = logicRoom.MainWorld.CreateEntity(1003); // EntityConfigId=1003
            var playerId = playerEntity.UniqueId; // PlayerId 就是 Entity.UniqueId
            
            // 记录 PlayerId 映射
            frameState.UserIdToPlayerId[userId] = playerId;
            logicRoom.Players.Add(playerId);
            
            ASLogger.Instance.Info($"服务器：创建玩家实体 - UserId: {userId}, PlayerId: {playerId}");
        }
        
        // 启动帧同步控制器
        logicRoom.LSController?.Start();
        
        // 保存第0帧快照
        logicRoom.LSController.AuthorityFrame = 0;
        logicRoom.LSController.FrameBuffer.MoveForward(0);
        logicRoom.LSController.SaveState();
        
        // 获取快照数据
        var snapshotBuffer = logicRoom.LSController.FrameBuffer.Snapshot(0);
        byte[] worldSnapshotData = new byte[snapshotBuffer.Length];
        snapshotBuffer.Read(worldSnapshotData, 0, (int)snapshotBuffer.Length);
        
        _roomFrameStates[roomId] = frameState;
        
        // 发送帧同步开始通知（包含世界快照和 PlayerId 映射）
        SendFrameSyncStartNotification(roomId, frameState, worldSnapshotData);
        
        ASLogger.Instance.Info($"房间 {roomId} 开始帧同步，玩家数: {frameState.PlayerIds.Count}，快照大小: {worldSnapshotData.Length} bytes");
    }
    
    // 发送帧同步开始通知（包含世界快照）
    private void SendFrameSyncStartNotification(string roomId, RoomFrameSyncState frameState, byte[] worldSnapshotData)
    {
        var notification = FrameSyncStartNotification.Create();
        notification.roomId = roomId;
        notification.frameRate = FRAME_RATE;
        notification.frameInterval = FRAME_INTERVAL_MS;
        notification.startTime = frameState.StartTime;
        notification.playerIds = new List<string>(frameState.PlayerIds);
        notification.worldSnapshot = worldSnapshotData; // 世界快照数据
        notification.playerIdMapping = new Dictionary<string, long>(frameState.UserIdToPlayerId); // PlayerId 映射
        
        ASLogger.Instance.Info($"准备发送帧同步开始通知，包含 {notification.playerIdMapping.Count} 个玩家的 PlayerId 映射，快照大小: {worldSnapshotData.Length} bytes");
        
        // 发送给房间内所有玩家
        foreach (var userId in frameState.PlayerIds)
        {
            var sessionId = _userManager.GetSessionIdByUserId(userId);
            if (!string.IsNullOrEmpty(sessionId))
            {
                _networkManager.SendMessage(sessionId, notification);
                ASLogger.Instance.Debug($"已发送帧同步开始通知给玩家 - UserId: {userId}, PlayerId: {frameState.UserIdToPlayerId[userId]}");
            }
        }
        
        ASLogger.Instance.Info($"已发送帧同步开始通知给房间 {roomId} 的所有玩家（共 {frameState.PlayerIds.Count} 个）");
    }
}
```

**2. 帧处理流程（后续帧正常处理）**
```csharp
private void ProcessRoomFrame(string roomId, RoomFrameSyncState frameState)
{
    // 推进 AuthorityFrame
    frameState.AuthorityFrame++;
    
    // 收集当前帧的所有输入数据
    var frameInputs = frameState.CollectFrameInputs(frameState.AuthorityFrame);
    
    // 推进逻辑世界
    if (frameState.LogicRoom != null)
    {
        // 更新 LSController 的 AuthorityFrame
        var controller = frameState.LogicRoom.LSController;
        if (controller != null)
        {
            controller.AuthorityFrame = frameState.AuthorityFrame;
        }
        
        // 执行帧逻辑（实体已存在，只需更新状态）
        frameState.LogicRoom.FrameTick(frameInputs);
    }
    
    // 发送帧同步数据
    SendFrameSyncData(roomId, frameState.AuthorityFrame, frameInputs);
}
```

#### 4.3.2 客户端实现要点

**1. 从 FrameSyncStartNotification 接收世界快照并恢复状态**
```csharp
public class MultiplayerGameMode
{
    // 所有玩家的 PlayerId 映射（供业务系统使用）
    private Dictionary<string, long> _playerIdMapping = new();
    
    public void OnFrameSyncStartNotification(FrameSyncStartNotification notification)
    {
        // 检查世界快照数据
        if (notification.worldSnapshot == null || notification.worldSnapshot.Length == 0)
        {
            ASLogger.Instance.Error("世界快照数据为空，无法恢复世界状态");
            return;
        }
        
        // 反序列化 World
        var world = MemoryPackHelper.Deserialize(typeof(World), notification.worldSnapshot, 0, notification.worldSnapshot.Length) as World;
        if (world == null)
        {
            ASLogger.Instance.Error("世界快照反序列化失败");
            return;
        }
        
        ASLogger.Instance.Info($"世界快照反序列化成功，实体数量: {world.Entities?.Count ?? 0}");
        
        // 替换 MainRoom.MainWorld
        if (MainRoom != null)
        {
            // 清理旧的世界
            MainRoom.MainWorld?.Cleanup();
            
            // 设置新世界
            MainRoom.MainWorld = world;
            
            // 重建 World 的引用关系
            world.RoomId = MainRoom.RoomId;
            // 注意：World 的 Systems 等引用会在反序列化后自动重建（通过 MemoryPackConstructor）
        }
        
        // 保存 PlayerId 映射
        if (notification.playerIdMapping != null)
        {
            _playerIdMapping = new Dictionary<string, long>(notification.playerIdMapping);
            
            // 从映射中获取当前玩家的 PlayerId
            var userId = UserManager.Instance.UserId;
            if (_playerIdMapping.TryGetValue(userId, out var playerId))
            {
                PlayerId = playerId;
                if (MainRoom != null)
                {
                    MainRoom.MainPlayerId = playerId;
                }
                ASLogger.Instance.Info($"玩家注册成功 - UserId: {userId}, PlayerId: {playerId}");
            }
        }
        
        // 启动 LSController
        if (MainRoom?.LSController != null && !MainRoom.LSController.IsRunning)
        {
            MainRoom.LSController.CreationTime = notification.startTime;
            
            // 将快照数据加载到 FrameBuffer（用于回滚）
            var snapshotBuffer = MainRoom.LSController.FrameBuffer.Snapshot(0);
            snapshotBuffer.Seek(0, SeekOrigin.Begin);
            snapshotBuffer.SetLength(0);
            snapshotBuffer.Write(notification.worldSnapshot, 0, notification.worldSnapshot.Length);
            
            MainRoom.LSController.Start();
            ASLogger.Instance.Info($"LSController 已启动，世界快照已加载到 FrameBuffer");
        }
        
        // 为快照中的所有实体创建 EntityView
        if (world.Entities != null)
        {
            foreach (var entity in world.Entities.Values)
            {
                if (!entity.IsDestroyed)
                {
                    // 发布 EntityCreatedEventData 事件，触发 Stage 创建 EntityView
                    var eventData = new EntityCreatedEventData(entity);
                    EventSystem.Instance.Publish(eventData);
                }
            }
            ASLogger.Instance.Info($"已为 {world.Entities.Count} 个实体创建 EntityView");
        }
        
        ASLogger.Instance.Info($"帧同步已启动，世界状态已恢复");
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

**2. 帧处理逻辑（后续帧正常处理）**
```csharp
public void FrameTick(OneFrameInputs oneFrameInputs)
{
    // 正常处理帧输入
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
            // 实体不存在（理论上不应该发生，因为快照中已包含所有实体）
            ASLogger.Instance.Warning($"收到未创建实体的输入，忽略 - PlayerId: {playerId}");
        }
    }
    
    // ... 更新世界 ...
}
```

**3. 移除旧的创建逻辑**
- 移除 `RequestCreatePlayer()` 中发送 `BornInfo` 的逻辑
- 移除 `FrameTick()` 中检测 `BornInfo != 0` 的旧逻辑
- **可选**：从 `LSInput` 协议中移除 `BornInfo` 字段（如果确定不再使用）

### 4.4 待实现的功能清单

#### 4.4.1 协议层
- [ ] **修改**：在 `FrameSyncStartNotification` 中添加字段
  - `worldSnapshot` (bytes)：世界快照数据（第0帧）
  - `playerIdMapping` (map<string, int64>)：UserId -> PlayerId 映射
  - 用途：
    - `worldSnapshot`：客户端直接恢复世界状态
    - `playerIdMapping`：方便客户端业务系统使用

#### 4.4.2 服务器端
- [ ] **修改**：在 `StartRoomFrameSync()` 中创建所有玩家实体
- [ ] **新增**：保存第0帧的世界快照
- [ ] **修改**：在 `SendFrameSyncStartNotification()` 中包含世界快照和 PlayerId 映射
- [ ] 实现重连检测逻辑（重连时发送当前帧快照）

#### 4.4.3 客户端端
- [ ] **修改**：在 `OnFrameSyncStartNotification()` 中反序列化世界快照
- [ ] **修改**：替换 `MainRoom.MainWorld` 为快照恢复的 World
- [ ] **修改**：从 `playerIdMapping` 中获取 PlayerId
- [ ] **修改**：将快照数据加载到 FrameBuffer（用于回滚）
- [ ] **修改**：为快照中的所有实体创建 EntityView
- [ ] 移除旧的 `RequestCreatePlayer()` 逻辑
- [ ] 移除旧的 `BornInfo` 发送逻辑
- [ ] **可选**：从 `LSInput` 协议中移除 `BornInfo` 字段

#### 4.4.4 清理工作
- [ ] 移除客户端通过帧输入创建玩家的代码
- [ ] 清理 `BornInfo` 相关的旧逻辑
- [ ] **可选**：从 `LSInput` 协议中移除 `BornInfo` 字段
- [ ] 更新相关文档和注释

### 4.5 测试要点

1. **正常流程测试**
   - 服务器创建所有玩家实体 → 保存快照 → 客户端接收快照 → 恢复世界状态

2. **多玩家测试**
   - 多个玩家同时开始 → 服务器创建所有实体 → 客户端恢复所有实体状态

3. **重连测试**
   - 客户端断线 → 重连 → 接收当前帧快照 → 恢复世界状态

4. **回放测试**
   - 请求回放数据 → 反序列化起始帧快照 → 逐帧执行 → 正确更新实体状态

5. **边界情况**
   - 快照数据为空或损坏的处理
   - 世界快照反序列化失败的处理
   - 中途加入玩家的处理（服务器创建实体后发送快照）

## 六、实现细节补充

### 6.1 关键实现细节

#### 6.1.1 PlayerId 与 Entity.UniqueId 的关系

**核心设计：**
- **PlayerId 就是 Entity.UniqueId**
- 服务器在 `StartRoomFrameSync` 时创建所有玩家实体
- 服务器创建实体后，将 UniqueId 作为 PlayerId 分配给 UserId
- 客户端通过世界快照恢复实体状态，UniqueId 与服务器完全一致
- 这样 `Entity.UniqueId == PlayerId`，无需额外映射，也无需修改 Entity 类

**实现流程：**
```csharp
// 服务器端：创建所有玩家实体并保存快照
public void StartRoomFrameSync(string roomId)
{
    // ... 创建逻辑房间 ...
    
    // 创建所有玩家实体（按 UserId 顺序）
    foreach (var userId in roomInfo.PlayerNames.OrderBy(x => x))
    {
        var playerEntity = logicRoom.MainWorld.CreateEntity(1003);
        var playerId = playerEntity.UniqueId; // PlayerId 就是 Entity.UniqueId
        
        // 记录 PlayerId 映射
        frameState.UserIdToPlayerId[userId] = playerId;
        logicRoom.Players.Add(playerId);
    }
    
    // 保存第0帧快照
    logicRoom.LSController.SaveState();
    var snapshotBuffer = logicRoom.LSController.FrameBuffer.Snapshot(0);
    byte[] worldSnapshotData = new byte[snapshotBuffer.Length];
    snapshotBuffer.Read(worldSnapshotData, 0, (int)snapshotBuffer.Length);
    
    // 发送通知（包含快照和 PlayerId 映射）
    SendFrameSyncStartNotification(roomId, frameState, worldSnapshotData);
}

// 客户端：通过世界快照恢复实体状态
public void OnFrameSyncStartNotification(FrameSyncStartNotification notification)
{
    // 反序列化 World
    var world = MemoryPackHelper.Deserialize(typeof(World), notification.worldSnapshot, 0, notification.worldSnapshot.Length) as World;
    
    // 替换 MainRoom.MainWorld
    MainRoom.MainWorld = world;
    
    // 从 playerIdMapping 获取 PlayerId
    var userId = UserManager.Instance.UserId;
    if (notification.playerIdMapping.TryGetValue(userId, out var playerId))
    {
        PlayerId = playerId; // PlayerId 就是 Entity.UniqueId
    }
}
```

**关键点：**
1. **服务器是唯一创建源**：服务器创建所有实体，客户端只恢复状态
2. **UniqueId 完全一致**：通过世界快照恢复，UniqueId 与服务器完全相同
3. **无需修改 Entity 类**：不需要支持指定 UniqueId，通过快照恢复即可
4. **一步到位**：客户端直接获得完整世界状态，无需通过帧输入创建实体

#### 6.1.2 世界快照的幂等性

**问题：**
- 如果客户端重复收到世界快照，可能会重复恢复世界状态
- 如果服务器重复发送快照，需要确保幂等性

**解决方案：**
```csharp
// 客户端 OnFrameSyncStartNotification 中的检查
public void OnFrameSyncStartNotification(FrameSyncStartNotification notification)
{
    // 检查是否已经恢复过世界状态（幂等性检查）
    if (MainRoom?.MainWorld != null && MainRoom.MainWorld.Entities.Count > 0)
    {
        ASLogger.Instance.Warning("世界状态已存在，跳过快照恢复（可能是重复通知）");
        // 可以选择：
        // 1. 跳过恢复（如果当前状态有效）
        // 2. 强制恢复（如果快照更新）
        // 这里选择强制恢复，确保状态一致
    }
    
    // 反序列化 World
    var world = MemoryPackHelper.Deserialize(typeof(World), notification.worldSnapshot, 0, notification.worldSnapshot.Length) as World;
    
    // 替换 MainRoom.MainWorld（幂等操作）
    if (MainRoom != null)
    {
        MainRoom.MainWorld?.Cleanup();
        MainRoom.MainWorld = world;
    }
    
    // ... 其他恢复逻辑 ...
}

// 服务器端：检查是否已创建实体（防止重复创建）
public void StartRoomFrameSync(string roomId)
{
    // 检查是否已有房间状态（重连情况）
    if (_roomFrameStates.TryGetValue(roomId, out var existingState))
    {
        // 重连：使用现有的世界状态，发送当前帧快照
        // ... 重连逻辑 ...
        return;
    }
    
    // 新游戏：创建所有玩家实体
    foreach (var userId in roomInfo.PlayerNames.OrderBy(x => x))
    {
        // 检查实体是否已存在（防止重复创建）
        var existingEntity = logicRoom.MainWorld.GetEntityByUserId(userId);
        if (existingEntity != null)
        {
            ASLogger.Instance.Warning($"玩家实体已存在，跳过创建 - UserId: {userId}");
            continue;
        }
        
        // 创建实体
        var playerEntity = logicRoom.MainWorld.CreateEntity(1003);
        // ... 记录映射 ...
    }
}
```

### 6.2 错误处理机制

#### 6.2.1 世界快照恢复失败处理

**错误情况：**
1. 世界快照数据为空或损坏
2. 世界快照反序列化失败
3. 世界快照中的实体数量与预期不符
4. PlayerId 映射缺失或错误

**处理流程：**
```csharp
// 客户端 OnFrameSyncStartNotification 中的错误处理
public void OnFrameSyncStartNotification(FrameSyncStartNotification notification)
{
    // 1. 检查世界快照数据
    if (notification.worldSnapshot == null || notification.worldSnapshot.Length == 0)
    {
        ASLogger.Instance.Error("世界快照数据为空，无法恢复世界状态");
        // 可以选择：
        // 1. 请求服务器重新发送快照
        // 2. 等待下一帧数据
        // 3. 断开连接并重连
        RequestWorldSnapshot(notification.roomId);
        return;
    }
    
    // 2. 反序列化 World
    World world = null;
    try
    {
        world = MemoryPackHelper.Deserialize(typeof(World), notification.worldSnapshot, 0, notification.worldSnapshot.Length) as World;
    }
    catch (Exception ex)
    {
        ASLogger.Instance.Error($"世界快照反序列化失败: {ex.Message}");
        // 请求服务器重新发送快照
        RequestWorldSnapshot(notification.roomId);
        return;
    }
    
    if (world == null)
    {
        ASLogger.Instance.Error("世界快照反序列化结果为空");
        RequestWorldSnapshot(notification.roomId);
        return;
    }
    
    // 3. 验证世界状态
    if (world.Entities == null || world.Entities.Count == 0)
    {
        ASLogger.Instance.Warning("世界快照中没有实体，可能存在问题");
        // 可以选择继续或请求重新发送
    }
    
    // 4. 验证 PlayerId 映射
    if (notification.playerIdMapping == null || notification.playerIdMapping.Count == 0)
    {
        ASLogger.Instance.Warning("PlayerId 映射为空，无法获取 PlayerId");
        // 可以选择：
        // 1. 从世界中的实体推断 PlayerId
        // 2. 请求服务器重新发送映射
    }
    
    // 5. 恢复世界状态
    try
    {
        // ... 恢复逻辑 ...
    }
    catch (Exception ex)
    {
        ASLogger.Instance.Error($"恢复世界状态失败: {ex.Message}");
        // 请求服务器重新发送快照
        RequestWorldSnapshot(notification.roomId);
        return;
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

