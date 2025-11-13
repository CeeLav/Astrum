# 帧同步状态同步与恢复机制 - 开发进展

**项目**: 帧同步状态同步与恢复机制（首次连接、断线重连、回放）  
**创建日期**: 2025-11-13  
**最后更新**: 2025-11-13  
**版本**: v1.0  
**技术方案**: [Frame-Sync-State-Sync-Design 帧同步状态同步与恢复机制设计.md](../帧同步/Frame-Sync-State-Sync-Design%20帧同步状态同步与恢复机制设计.md)

---

## 📋 目录

1. [开发状态总览](#开发状态总览)
2. [阶段划分](#阶段划分)
3. [详细任务清单](#详细任务清单)
4. [技术债务](#技术债务)
5. [测试计划](#测试计划)

---

## 开发状态总览

### 当前版本
- **版本号**: v1.0
- **状态**: 🟡 设计完成，开发中
- **功能完成度**: 20% (协议定义 100%，服务器端 0%，客户端 0%)

### 阶段划分
- ✅ **Phase 0**: 技术方案设计 - **已完成**
  - ✅ 架构设计
  - ✅ 协议设计
  - ✅ 流程设计
  - ✅ 文档合并
- ✅ **Phase 1**: 协议层实现 - **已完成**
  - ✅ 修改协议定义（添加 worldSnapshot 和 playerIdMapping）
  - ✅ 重新生成协议代码
- ⏳ **Phase 2**: 服务器端实现 - **待开发**
  - ⏳ 创建所有玩家实体
  - ⏳ 保存第0帧快照
  - ⏳ 发送快照和映射
  - ⏳ 重连检测和处理
- ⏳ **Phase 3**: 客户端实现 - **待开发**
  - ⏳ 反序列化世界快照
  - ⏳ 恢复世界状态
  - ⏳ 创建 EntityView
  - ⏳ 加载快照到 FrameBuffer
- ⏳ **Phase 4**: 测试与优化 - **待开发**
  - ⏳ 单元测试
  - ⏳ 集成测试
  - ⏳ 性能测试
  - ⏳ 错误处理测试

---

## 阶段划分

### Phase 0: 技术方案设计 ✅

**目标**: 完成技术方案设计和文档编写

**完成内容**:
- ✅ 分析当前架构问题
- ✅ 设计世界快照方案
- ✅ 统一首次连接、断线重连、回放机制
- ✅ 合并玩家创建架构分析和断线重连文档
- ✅ 完成统一设计文档

**文档**:
- `Frame-Sync-State-Sync-Design 帧同步状态同步与恢复机制设计.md`

---

### Phase 1: 协议层实现 🟡

**目标**: 完成协议定义和代码生成

#### 1.1 修改协议定义 ✅

**任务**: 在 `FrameSyncStartNotification` 中添加字段

**文件**: `AstrumConfig/Proto/gamemessages_C_2000.proto`

**修改内容**:
```protobuf
message FrameSyncStartNotification
{
    string roomId = 1;
    int32 frameRate = 2;
    int32 frameInterval = 3;
    int64 startTime = 4;
    repeated string playerIds = 5;           // 玩家ID列表（UserId）
    bytes worldSnapshot = 6;                // 世界快照数据（第0帧）【新增】
    map<string, int64> playerIdMapping = 7; // UserId -> PlayerId 映射【新增】
}
```

**状态**: ✅ 已完成

#### 1.2 重新生成协议代码 ✅

**任务**: 运行 Proto2CS 工具重新生成 C# 代码

**步骤**:
1. ✅ 运行 `cd AstrumTool/Proto2CS && dotnet run`
2. ✅ 检查生成的代码是否正确
3. ✅ 验证新字段是否可用

**验证结果**:
- ✅ `worldSnapshot` 字段已生成（类型：`byte[]`）
- ✅ `playerIdMapping` 字段已生成（类型：`Dictionary<string, long>`）
- ✅ 客户端和服务端代码都已更新

**状态**: ✅ 已完成

---

### Phase 2: 服务器端实现 ⏳

**目标**: 实现服务器端创建玩家实体、保存快照、发送通知的逻辑

#### 2.1 修改 StartRoomFrameSync 创建所有玩家实体 ⏳

**文件**: `AstrumServer/AstrumServer/Managers/FrameSyncManager.cs`

**任务**:
- [ ] 在 `StartRoomFrameSync` 中创建所有玩家实体
- [ ] 按 UserId 顺序创建，确保 UniqueId 一致
- [ ] 记录 UserId -> PlayerId 映射
- [ ] 添加到 `RoomFrameSyncState.UserIdToPlayerId`

**代码位置**: `StartRoomFrameSync()` 方法

**关键逻辑**:
```csharp
// 创建所有玩家实体（按 UserId 顺序，确保 UniqueId 一致）
foreach (var userId in roomInfo.PlayerNames.OrderBy(x => x))
{
    var playerEntity = logicRoom.MainWorld.CreateEntity(1003); // EntityConfigId=1003
    var playerId = playerEntity.UniqueId; // PlayerId 就是 Entity.UniqueId
    
    // 记录 PlayerId 映射
    frameState.UserIdToPlayerId[userId] = playerId;
    logicRoom.Players.Add(playerId);
}
```

**状态**: ⏳ 待开发

#### 2.2 保存第0帧快照 ⏳

**文件**: `AstrumServer/AstrumServer/Managers/FrameSyncManager.cs`

**任务**:
- [ ] 在创建完所有玩家实体后，保存第0帧快照
- [ ] 设置 `LSController.AuthorityFrame = 0`
- [ ] 调用 `FrameBuffer.MoveForward(0)`
- [ ] 调用 `LSController.SaveState()`
- [ ] 从 `FrameBuffer.Snapshot(0)` 获取快照数据
- [ ] 转换为 `byte[]`

**代码位置**: `StartRoomFrameSync()` 方法，在创建实体之后

**关键逻辑**:
```csharp
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
```

**状态**: ⏳ 待开发

#### 2.3 修改 SendFrameSyncStartNotification 包含快照和映射 ⏳

**文件**: `AstrumServer/AstrumServer/Managers/FrameSyncManager.cs`

**任务**:
- [ ] 修改 `SendFrameSyncStartNotification` 方法签名，添加 `worldSnapshotData` 参数
- [ ] 在通知中设置 `worldSnapshot` 字段
- [ ] 在通知中设置 `playerIdMapping` 字段
- [ ] 更新调用处，传入快照数据

**代码位置**: `SendFrameSyncStartNotification()` 方法

**关键逻辑**:
```csharp
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
    
    // 发送给房间内所有玩家
    // ...
}
```

**状态**: ⏳ 待开发

#### 2.4 实现重连检测和处理 ⏳

**文件**: `AstrumServer/AstrumServer/Managers/FrameSyncManager.cs`

**任务**:
- [ ] 在 `StartRoomFrameSync` 中检测是否已有房间状态（重连情况）
- [ ] 如果已有状态，保存当前帧快照
- [ ] 发送包含当前帧快照的通知

**代码位置**: `StartRoomFrameSync()` 方法开头

**关键逻辑**:
```csharp
// 检查是否已有房间状态（重连情况）
if (_roomFrameStates.TryGetValue(roomId, out var existingState))
{
    // 重连：保存当前帧快照并发送
    var currentFrame = existingState.AuthorityFrame;
    existingState.LogicRoom.LSController.SaveState();
    var snapshotBuffer = existingState.LogicRoom.LSController.FrameBuffer.Snapshot(currentFrame);
    byte[] worldSnapshotData = new byte[snapshotBuffer.Length];
    snapshotBuffer.Read(worldSnapshotData, 0, (int)snapshotBuffer.Length);
    
    SendFrameSyncStartNotification(roomId, existingState, worldSnapshotData);
    return;
}
```

**状态**: ⏳ 待开发

#### 2.5 添加 UserIdToPlayerId 字段到 RoomFrameSyncState ⏳

**文件**: `AstrumServer/AstrumServer/Managers/FrameSyncManager.cs`

**任务**:
- [ ] 在 `RoomFrameSyncState` 类中添加 `UserIdToPlayerId` 字段
- [ ] 初始化字典

**代码位置**: `RoomFrameSyncState` 类定义

**关键逻辑**:
```csharp
public class RoomFrameSyncState
{
    // ... 现有字段 ...
    
    // UserId -> PlayerId 映射（实体创建后确定）
    public Dictionary<string, long> UserIdToPlayerId { get; set; } = new();
}
```

**状态**: ⏳ 待开发

---

### Phase 3: 客户端实现 ⏳

**目标**: 实现客户端接收快照、恢复状态、创建视图的逻辑

#### 3.1 修改 OnFrameSyncStartNotification 反序列化世界快照 ⏳

**文件**: `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/Handlers/FrameSyncHandler.cs`

**任务**:
- [ ] 检查世界快照数据是否为空
- [ ] 使用 `MemoryPackHelper.Deserialize` 反序列化 World
- [ ] 错误处理和日志记录

**代码位置**: `OnFrameSyncStartNotification()` 方法

**关键逻辑**:
```csharp
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
```

**状态**: ⏳ 待开发

#### 3.2 替换 MainRoom.MainWorld 为快照恢复的 World ⏳

**文件**: `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/Handlers/FrameSyncHandler.cs`

**任务**:
- [ ] 清理旧的世界（调用 `Cleanup()`）
- [ ] 设置新世界到 `MainRoom.MainWorld`
- [ ] 重建 World 的引用关系（RoomId、Systems等）

**代码位置**: `OnFrameSyncStartNotification()` 方法

**关键逻辑**:
```csharp
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
```

**状态**: ⏳ 待开发

#### 3.3 从 playerIdMapping 获取 PlayerId ⏳

**文件**: `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/MultiplayerGameMode.cs`

**任务**:
- [ ] 保存 `playerIdMapping` 到 `MultiplayerGameMode`
- [ ] 从映射中获取当前玩家的 PlayerId
- [ ] 设置 `PlayerId` 和 `MainRoom.MainPlayerId`

**代码位置**: `OnFrameSyncStartNotification()` 方法或 `MultiplayerGameMode` 相关方法

**关键逻辑**:
```csharp
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
    }
}
```

**状态**: ⏳ 待开发

#### 3.4 将快照数据加载到 FrameBuffer ⏳

**文件**: `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/Handlers/FrameSyncHandler.cs`

**任务**:
- [ ] 获取 `FrameBuffer.Snapshot(0)`
- [ ] 清空并写入快照数据
- [ ] 用于后续回滚

**代码位置**: `OnFrameSyncStartNotification()` 方法

**关键逻辑**:
```csharp
// 将快照数据加载到 FrameBuffer（用于回滚）
var snapshotBuffer = MainRoom.LSController.FrameBuffer.Snapshot(0);
snapshotBuffer.Seek(0, SeekOrigin.Begin);
snapshotBuffer.SetLength(0);
snapshotBuffer.Write(notification.worldSnapshot, 0, notification.worldSnapshot.Length);
```

**状态**: ⏳ 待开发

#### 3.5 为快照中的所有实体创建 EntityView ⏳

**文件**: `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/Handlers/FrameSyncHandler.cs`

**任务**:
- [ ] 遍历 `World.Entities`
- [ ] 为每个实体发布 `EntityCreatedEventData` 事件
- [ ] 触发 `Stage` 创建 `EntityView`

**代码位置**: `OnFrameSyncStartNotification()` 方法

**关键逻辑**:
```csharp
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
}
```

**状态**: ⏳ 待开发

#### 3.6 移除旧的 BornInfo 发送逻辑 ⏳

**文件**: `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/MultiplayerGameMode.cs`

**任务**:
- [ ] 移除 `RequestCreatePlayer()` 方法或移除其中的 `BornInfo` 发送逻辑
- [ ] 清理相关调用

**状态**: ⏳ 待开发

---

### Phase 4: 测试与优化 ⏳

**目标**: 完成功能测试、性能测试和错误处理测试

#### 4.1 单元测试 ⏳

**任务**:
- [ ] 测试世界快照序列化/反序列化
- [ ] 测试 PlayerId 映射
- [ ] 测试快照恢复逻辑

**状态**: ⏳ 待开发

#### 4.2 集成测试 ⏳

**任务**:
- [ ] 测试首次连接流程（服务器创建实体 → 客户端恢复）
- [ ] 测试多玩家场景
- [ ] 测试重连流程
- [ ] 测试中途加入玩家

**状态**: ⏳ 待开发

#### 4.3 性能测试 ⏳

**任务**:
- [ ] 测试快照大小（目标 < 200 KB）
- [ ] 测试快照序列化/反序列化耗时（目标 < 5 ms）
- [ ] 测试首次连接恢复耗时（目标 < 200 ms）
- [ ] 测试重连恢复耗时（目标 < 200 ms）

**状态**: ⏳ 待开发

#### 4.4 错误处理测试 ⏳

**任务**:
- [ ] 测试快照数据为空的情况
- [ ] 测试快照反序列化失败的情况
- [ ] 测试 PlayerId 映射缺失的情况
- [ ] 测试重复收到快照的情况（幂等性）

**状态**: ⏳ 待开发

---

## 详细任务清单

### 协议层 (Phase 1)

- [x] 修改协议定义：添加 `worldSnapshot` 和 `playerIdMapping` 字段
- [x] 重新生成协议代码（Proto2CS）
- [x] 验证生成的代码是否正确

### 服务器端 (Phase 2)

- [ ] 添加 `UserIdToPlayerId` 字段到 `RoomFrameSyncState`
- [ ] 修改 `StartRoomFrameSync` 创建所有玩家实体
- [ ] 保存第0帧快照
- [ ] 修改 `SendFrameSyncStartNotification` 包含快照和映射
- [ ] 实现重连检测和处理
- [ ] 添加日志记录

### 客户端 (Phase 3)

- [ ] 修改 `OnFrameSyncStartNotification` 反序列化世界快照
- [ ] 替换 `MainRoom.MainWorld` 为快照恢复的 World
- [ ] 从 `playerIdMapping` 获取 PlayerId
- [ ] 将快照数据加载到 FrameBuffer
- [ ] 为快照中的所有实体创建 EntityView
- [ ] 移除旧的 `BornInfo` 发送逻辑
- [ ] 添加错误处理和日志记录

### 测试 (Phase 4)

- [ ] 单元测试
- [ ] 集成测试
- [ ] 性能测试
- [ ] 错误处理测试

---

## 技术债务

### 待优化项

1. **快照压缩**
   - 当前：未压缩
   - 目标：使用 GZip 压缩，减少网络传输
   - 优先级：中

2. **快照增量存储**
   - 当前：完整快照
   - 目标：增量快照（仅存储变化）
   - 优先级：低

3. **快照分块传输**
   - 当前：一次性传输
   - 目标：如果快照过大，考虑分块传输
   - 优先级：低

4. **清理旧的 BornInfo 字段**
   - 当前：保留在协议中
   - 目标：从 `LSInput` 协议中移除 `BornInfo` 字段
   - 优先级：低

---

## 测试计划

### 测试场景

1. **首次连接场景**
   - 服务器创建所有玩家实体
   - 客户端接收快照并恢复状态
   - 验证所有实体都正确创建

2. **多玩家场景**
   - 多个玩家同时连接
   - 验证每个玩家都能正确恢复状态
   - 验证 PlayerId 映射正确

3. **重连场景**
   - 客户端断线后重连
   - 服务器发送当前帧快照
   - 客户端恢复状态并继续游戏

4. **中途加入场景**
   - 游戏进行中，新玩家加入
   - 服务器创建新玩家实体
   - 发送快照给新玩家

5. **错误场景**
   - 快照数据为空
   - 快照反序列化失败
   - PlayerId 映射缺失
   - 重复收到快照

### 性能指标

| 指标 | 目标 | 测试方法 |
|------|------|----------|
| 快照大小 | < 200 KB | 记录快照字节数 |
| 快照序列化耗时 | < 5 ms | Stopwatch |
| 快照反序列化耗时 | < 5 ms | Stopwatch |
| 首次连接恢复耗时 | < 200 ms | 事件计时 |
| 重连恢复耗时 | < 200 ms | 事件计时 |

---

## 更新日志

### 2025-11-13
- ✅ 创建开发进展文档
- ✅ 完成 Phase 0（技术方案设计）
- ✅ 完成 Phase 1（协议层实现）
  - ✅ 修改协议定义（添加 worldSnapshot 和 playerIdMapping）
  - ✅ 重新生成协议代码并验证
- ⏳ 开始 Phase 2（服务器端实现）

---

*文档版本：v1.0*  
*最后更新：2025-11-13*

