# 帧同步状态同步与恢复机制设计 Frame-Sync-State-Sync-Design

> 📖 **版本**: v2.0 | 📅 **最后更新**: 2025-11-13  
> 👥 **面向读者**: 服务器/网络工程师、客户端帧同步开发  
> 🎯 **目标**: 统一的状态同步机制，支持首次连接、断线重连与战斗回放

**TL;DR**
- 服务器直接运行 `AstrumLogic`，维护权威 `Room/World` 状态
- 按固定帧率推进房间，同时记录输入历史与状态快照
- **首次连接**：服务器创建所有玩家实体→保存第0帧快照→客户端通过快照恢复状态
- **断线重连**：校验用户→加载最近快照→补发错过帧→恢复会话
- **回放机制**：按战斗记录生成回放包，支持快进/跳转/暂停
- 状态快照与帧历史采用增量+压缩策略，控制 IO 与内存
- `noEngineReferences=true` 仅阻止 UnityEngine 引入，必须彻底移除 Unity 类型
- 关键指标：状态一致性、恢复耗时、快照大小、帧延迟

---

## 1. 系统概述

| 角色 | 职责 | 说明 |
|------|------|------|
| 服务器 FrameSyncManager | 帧推进、状态快照、帧下发 | 权威逻辑所在 |
| 服务器 RoomManager | 房间生命周期、事件派发 | 管理 `Room` 实例 |
| 服务器 StateSnapshotManager | 快照存储/加载 | 支撑首次连接/重连/回放 |
| 客户端 LSController | 预测、回滚、回放 | 与服务器协议保持一致 |

**设计理念**
- 权威逻辑统一在服务器运行：客户端仅预测，避免作弊
- 首次连接、断线重连与回放共享同一套快照+帧历史能力
- 服务器是唯一创建源：所有玩家实体在服务器创建，客户端通过快照恢复
- 数据落地可水平扩展（Redis / Files / 数据库）

**系统边界**
- ✅ 负责：帧推进、权威逻辑、状态快照、首次连接、重连、回放
- ❌ 不负责：UI 表现层、资源加载、非战斗玩法逻辑

---

## 2. 架构设计

### 2.1 组件关系

```
┌─────────────────────────────────────────────────────────────┐
│                     Client (Unity)                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ AstrumView  │  │ AstrumClient│  │ AstrumLogic │         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
│         │                │                │                 │
└─────────┼────────────────┼────────────────┼─────────────────┘
          │                │                │
          │ Input/Prediction/Event          │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────┐
│                     Server (net9.0)                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ FrameSync   │  │ RoomManager │  │ AstrumLogic │         │
│  │ Manager     │  │             │  │ Runner      │         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
│         │                │                │                 │
│ Snapshot/FrameHistory    │                │                 │
│         ▼                ▼                ▼                 │
│  ┌──────────────────────────────────────────────┐          │
│  │ StateSnapshotManager / StorageAdapter        │          │
│  └──────────────────────────────────────────────┘          │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 数据流程

```
首次连接场景：
Client                     Server FrameSyncManager            Storage
  | 进入房间                  |                                 |
  |                           | StartRoomFrameSync()            |
  |                           | 创建所有玩家实体                |
  |                           | 保存第0帧快照 S(0) ──────────► |
  |◄──── FrameSyncStart ──────| 包含 worldSnapshot             |
  | 反序列化快照恢复状态        |                                 |
  |                           |                                 |

正常游戏场景：
  | 输入 I(t) ───────────────►|                                 |
  |                           | 帧推进 F(t)                     |
  |◄──────── 帧数据 D(t)      |                                 |
  |                           | 保存快照 S(t) ───────────────► |
  |                           | 保存输入历史 H(t) ───────────► |

断线重连场景：
  | 连接断开                  |                                 |
  | 重连请求 R ──────────────►|                                 |
  |                           | 加载快照 S(t-k) ◄────────────  |
  |                           | 补发缺失帧 H(t-k..t)            |
  |◄──── 重连响应 ────────────| 包含 worldSnapshot + 缺失帧     |
  | 恢复状态并回放缺失帧        |                                 |
```

---

## 3. 世界快照机制（统一设计）

### 3.1 快照内容与格式

**快照内容：**
- `World` 对象的完整序列化数据（使用 MemoryPack）
- 包含：所有实体（Entities）、组件（Components）、能力（Capabilities）、系统状态（Systems）
- 包含：第N帧的完整世界状态

**序列化方式：**
```csharp
// 序列化 World
byte[] worldSnapshotData = MemoryPackHelper.Serialize(world);

// 反序列化 World
World world = MemoryPackHelper.Deserialize(typeof(World), worldSnapshotData, 0, worldSnapshotData.Length) as World;
```

### 3.2 快照周期与存储

- **周期**：每 10 帧（可配置）
- **存储**：`StateSnapshotManager` 统一对接 Redis / File
- **压缩**：使用 GZip (MemoryPack → GZip)
- **保留策略**：最近 30 秒快照（约 60 份）

```csharp
public void SaveWorldSnapshot(string roomId, int frame, World world)
{
    var data = MemoryPackHelper.Serialize(world);
    var compressed = Compress(data);

    _snapshotStore.Save(new WorldSnapshot
    {
        RoomId = roomId,
        Frame = frame,
        Timestamp = DateTime.UtcNow,
        Data = compressed
    });
}
```

### 3.3 快照使用场景

1. **首次连接**：第0帧快照，包含所有玩家实体
2. **断线重连**：最近快照，恢复当前世界状态
3. **回放功能**：起始帧快照，作为回放起点

---

## 4. 首次连接（玩家创建）

### 4.1 核心设计

**核心思想：**
- **服务器在开始游戏时创建所有玩家实体**
- **服务器保存第0帧的世界快照**
- **客户端通过 FrameSyncStartNotification 接收世界快照并恢复状态**（一步到位）
- **PlayerId 就是 Entity.UniqueId，服务器创建后直接分配**

**方案说明：**
- 服务器在 `StartRoomFrameSync` 时创建所有玩家实体，保存第0帧快照
- 客户端收到 `FrameSyncStartNotification` 时，直接反序列化世界快照，恢复所有实体状态
- 无需通过帧输入创建实体，一步到位，状态完全一致
- 支持中途加入：服务器创建实体后发送快照，客户端直接恢复

### 4.2 服务器流程

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

### 4.3 客户端流程

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

### 4.4 协议定义

**FrameSyncStartNotification（新增字段）**
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

**关键数据结构**
```csharp
// 服务器端：玩家状态
public class RoomFrameSyncState
{
    // ... 现有字段 ...
    
    // UserId -> PlayerId 映射（实体创建后确定）
    public Dictionary<string, long> UserIdToPlayerId { get; set; } = new();
}
```

### 4.5 PlayerId 与 Entity.UniqueId 的关系

**核心设计：**
- **PlayerId 就是 Entity.UniqueId**
- 服务器在 `StartRoomFrameSync` 时创建所有玩家实体
- 服务器创建实体后，将 UniqueId 作为 PlayerId 分配给 UserId
- 客户端通过世界快照恢复实体状态，UniqueId 与服务器完全一致
- 这样 `Entity.UniqueId == PlayerId`，无需额外映射，也无需修改 Entity 类

**关键点：**
1. **服务器是唯一创建源**：服务器创建所有实体，客户端只恢复状态
2. **UniqueId 完全一致**：通过世界快照恢复，UniqueId 与服务器完全相同
3. **无需修改 Entity 类**：不需要支持指定 UniqueId，通过快照恢复即可
4. **一步到位**：客户端直接获得完整世界状态，无需通过帧输入创建实体

---

## 5. 断线重连流程

### 5.1 重连流程

1. 客户端发送 `ReconnectRequest(userId, roomId, lastFrame)`
2. 服务器校验用户、房间、状态
3. 加载最近快照（若最新快照失败，回退到上一份）
4. 生成错过的帧 `H(lastFrame+1 ... currentFrame)`
5. 返回 `ReconnectResponse(worldSnapshot, currentFrame, missedFrames)`
6. 客户端恢复状态并继续收发输入

```plaintext
Client → Server: ReconnectRequest
Server: Validate user/session/room
Server: Snapshot = LoadLatest(roomId)
Server: Missed = FrameHistory(lastFrame+1 .. current)
Server → Client: ReconnectResponse(WorldSnapshot, CurrentFrame, Missed)
Client: Restore World, Apply Missed Frames, Resume
```

### 5.2 服务器重连处理

**重连检测：**
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
        // ... 首次连接流程 ...
    }
}
```

### 5.3 客户端恢复流程

```csharp
public void OnReconnectResponse(ReconnectResponse resp)
{
    // 反序列化世界快照
    var world = MemoryPackHelper.Deserialize(typeof(World), Decompress(resp.WorldSnapshot), 0, resp.WorldSnapshot.Length) as World;
    
    // 替换 MainRoom.MainWorld
    MainRoom.MainWorld = world;
    
    // 回放缺失的帧
    foreach (var frame in resp.MissedFrames)
    {
        MainRoom.FrameTick(frame.FrameInputs);
    }
    
    // 更新帧号
    LSController.Instance.AuthorityFrame = resp.CurrentFrame;
    LSController.Instance.PredictionFrame = resp.CurrentFrame;
}
```

---

## 6. 回放机制

### 6.1 回放流程

- 服务器在战斗开始/结束时自动录制回放
- 回放包：`ReplayMetadata + WorldSnapshot(start) + FrameHistory(start..end)`
- 客户端加载回放包 → 反序列化世界快照 → 逐帧推进 → 渲染
- 支持：播放/暂停、1x/2x/4x、跳转帧（通过重新模拟到目标帧）

```csharp
public class ReplayPlayer
{
    public void Load(byte[] replayData)
    {
        _replay = MemoryPackSerializer.Deserialize<ReplayData>(replayData);
        // 反序列化起始帧的世界快照
        _world = MemoryPackSerializer.Deserialize<World>(Decompress(_replay.WorldSnapshotAtStartFrame));
        _currentFrame = _replay.Metadata.StartFrame;
    }

    public void PlayNextFrame()
    {
        var frameInputs = _replay.FrameInputs[_currentFrame];
        _world.Update(); // 执行帧逻辑
        Render(_world);
        _currentFrame++;
    }
}
```

### 6.2 回放数据结构

```csharp
public class ReplayData
{
    public int StartFrame { get; set; }
    public int EndFrame { get; set; }
    public byte[] WorldSnapshotAtStartFrame { get; set; } // 起始帧的世界快照（bytes）
    public Dictionary<int, OneFrameInputs> FrameInputs { get; set; } // 帧号 -> 输入数据
}
```

---

## 7. 输入历史

- 每帧保存 `OneFrameInputs`
- 使用 `ObjectPool<OneFrameInputs>` 减少 GC
- 历史长度：最近 15 秒（约 300 帧）

```csharp
public void RecordFrameInputs(string roomId, int frame, OneFrameInputs inputs)
{
    _frameHistoryStore.Save(roomId, frame, inputs);
    _frameHistoryStore.Trim(roomId, frame - MaxHistoryFrames);
}
```

---

## 8. 关键模块与接口

### 8.1 FrameSyncManager (服务器)

- `StartRoomFrameSync(roomId)`: 初始化状态、创建玩家实体、保存第0帧快照、开始记录
- `ProcessRoomFrame(roomId, frameState)`: 推进帧、分发结果
- `HandleSingleInput(roomId, SingleInput)`: 收集玩家输入
- `GetRoomState(roomId)`: 获取 Room 实例（用于快照/重连）
- `GetFrameHistory(roomId, start, end)`: 获取帧区间数据

### 8.2 StateSnapshotManager

- `Save(WorldSnapshot snapshot)`
- `WorldSnapshot? GetLatest(roomId)`
- `WorldSnapshot? GetPrevious(roomId, frame)`
- `DeleteOlderThan(roomId, TimeSpan window)`

### 8.3 Reconnect Handler

```csharp
private void HandleReconnect(Session session, ReconnectRequest req)
{
    var user = _userManager.GetUserBySessionId(session.Id.ToString());
    var roomState = _frameSyncManager.GetRoomState(req.RoomId);
    var snapshot = _snapshotStore.GetLatest(req.RoomId);
    var missedFrames = _frameHistoryStore.Fetch(req.RoomId, req.LastFrame + 1);

    var response = ReconnectResponse.Create();
    response.Success = true;
    response.WorldSnapshot = snapshot.Data;
    response.CurrentFrame = roomState.AuthorityFrame;
    response.MissedFrames = missedFrames;

    _networkManager.Send(session, response);
}
```

---

## 9. 数据存储策略

| 数据类型 | 频率 | 内容 | 存储 | 保留策略 |
|----------|------|------|------|-----------|
| 世界快照 | 每10帧 | World 序列化数据 | Redis/File | 最近30秒 |
| 输入历史 | 每帧 | OneFrameInputs | Redis | 最近15秒 |
| 回放包 | 战斗完成 | Metadata + Snapshot + Frames | 文件 | 运营策略 |

**压缩与增量**
- 快照采用定期完整快照 + 中间增量（待优化）
- 输入历史使用差分存储（仅变化的玩家输入）

---

## 10. 状态一致性与验证

- 状态哈希：`TSVector` + `CapabilityStates` + `SkillEffectSystem` Queue
- 客户端/服务器定期校验（可选）
- 断线重连完成后校验：若失败，强制重置到最新快照

```csharp
public long CalcStateHash(World world)
{
    using var hash = new XxHash64();
    foreach (var entity in world.Entities.Values)
    {
        var trans = entity.GetComponent<TransComponent>();
        if (trans == null) continue;

        hash.Add(trans.Position.x.RawValue);
        hash.Add(trans.Position.y.RawValue);
        hash.Add(trans.Position.z.RawValue);
    }
    return hash.ToHashCode();
}
```

---

## 11. 错误处理机制

### 11.1 世界快照恢复失败处理

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
        RequestWorldSnapshot(notification.roomId);
        return;
    }
    
    // 3. 验证世界状态和 PlayerId 映射
    // ... 验证逻辑 ...
    
    // 4. 恢复世界状态
    // ... 恢复逻辑 ...
}
```

### 11.2 世界快照的幂等性

**问题：**
- 如果客户端重复收到世界快照，可能会重复恢复世界状态
- 如果服务器重复发送快照，需要确保幂等性

**解决方案：**
```csharp
// 客户端：检查是否已经恢复过世界状态（幂等性检查）
if (MainRoom?.MainWorld != null && MainRoom.MainWorld.Entities.Count > 0)
{
    ASLogger.Instance.Warning("世界状态已存在，强制恢复（确保状态一致）");
}

// 替换 MainRoom.MainWorld（幂等操作）
MainRoom.MainWorld?.Cleanup();
MainRoom.MainWorld = world;
```

---

## 12. 性能与监控指标

| 指标 | 目标 | 监控方式 |
|------|------|----------|
| 快照大小 | < 200 KB/份 | 日志 & Prometheus |
| 快照写入耗时 | < 5 ms | Stopwatch | 
| 首次连接恢复耗时 | < 200 ms | 事件计时 |
| 重连恢复耗时 | < 200 ms | 事件计时 |
| 回放文件大小 | < 50 MB/战斗 | 文件大小统计 |
| 状态一致性失败率 | 0 | 状态哈希比对 |

---

## 13. 风险与对策

| 风险 | 描述 | 对策 |
|------|------|------|
| 快照损坏 | 存储失败导致状态无法恢复 | 多副本保存，快照回退 |
| 帧历史过大 | 长时间战斗导致内存上涨 | 定期清理，差分压缩 |
| 兼容性问题 | 版本升级造成回放不可用 | 回放包记录版本，并做兼容处理 |
| Unity 引用残留 | `noEngineReferences=true` 仍可能漏掉 Unity 类型 | 全面替换 Unity 类型为 TrueSync |
| 快照数据过大 | 首次连接时传输大量数据 | 使用 GZip 压缩，考虑分块传输 |

---

## 14. 开发计划

1. **Unity 依赖剥离**（已完成）
2. **AstrumLogic.dll 引入服务器**（已完成）
3. **首次连接：玩家创建与快照机制**
   - 修改 `StartRoomFrameSync` 创建所有玩家实体
   - 保存第0帧快照
   - 修改 `FrameSyncStartNotification` 协议
4. **FrameSyncManager 调整，运行 `Room.FrameTick`**
5. **状态快照 + 输入历史存储实现**
6. **断线重连协议（proto）与实现**
7. **回放录制/播放功能**
8. **性能测试与监控落地**

---

## 15. 相关文件

- `AstrumServer/AstrumServer/Managers/FrameSyncManager.cs`
- `AstrumServer/AstrumServer/Managers/RoomManager.cs`
- `AstrumServer/AstrumServer/Core/GameServer.cs`
- `AstrumProj/Assets/Script/AstrumLogic/Core/Room.cs`
- `AstrumProj/Assets/Script/AstrumLogic/Core/World.cs`
- `AstrumProj/Assets/Script/AstrumLogic/FrameSync/LSController.cs`
- `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/MultiplayerGameMode.cs`
- `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/Handlers/FrameSyncHandler.cs`
- `Docs/11-Network 网络系统/Frame-Sync-Mechanism 帧同步机制.md`

---

## 16. 元信息

- **Owner**: 网络组 @帧同步
- **上游任务**: Astrum 服务器运行 AstrumLogic、首次连接、断线重连、战斗回放
- **变更摘要**: 合并玩家创建架构分析，统一状态同步机制设计

---

*文档版本：v2.0*  
*创建时间：2025-11-13*  
*最后更新：2025-11-13*  
*状态：设计完成*

