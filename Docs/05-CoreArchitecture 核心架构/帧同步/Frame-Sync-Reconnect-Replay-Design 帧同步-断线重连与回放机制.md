# 帧同步断线重连与回放机制设计 Frame-Sync-Reconnect-Replay-Design

> 📖 **版本**: v1.0 | 📅 **最后更新**: 2025-11-13  
> 👥 **面向读者**: 服务器/网络工程师、客户端帧同步开发  
> 🎯 **目标**: 服务器运行 AstrumLogic 后，支持断线重连与战斗回放

**TL;DR**
- 服务器直接运行 `AstrumLogic`，维护权威 `Room/World` 状态
- 按固定帧率推进房间，同时记录输入历史与状态快照
- 断线重连：校验用户→加载最近快照→补发错过帧→恢复会话
- 回放机制：按战斗记录生成回放包，支持快进/跳转/暂停
- 状态快照与帧历史采用增量+压缩策略，控制 IO 与内存
- `noEngineReferences=true` 仅阻止 UnityEngine 引入，必须彻底移除 Unity 类型
- 关键指标：状态一致性、恢复耗时、快照大小、帧延迟

---

## 1. 系统概述

| 角色 | 职责 | 说明 |
|------|------|------|
| 服务器 FrameSyncManager | 帧推进、状态快照、帧下发 | 权威逻辑所在 |
| 服务器 RoomManager | 房间生命周期、事件派发 | 管理 `Room` 实例 |
| 服务器 StateSnapshotManager | 快照存储/加载 | 支撑断线重连/回放 |
| 客户端 LSController | 预测、回滚、回放 | 与服务器协议保持一致 |

**设计理念**
- 权威逻辑统一在服务器运行：客户端仅预测，避免作弊
- 断线重连与回放共享同一套快照+帧历史能力
- 数据落地可水平扩展（Redis / Files / 数据库）

**系统边界**
- ✅ 负责：帧推进、权威逻辑、状态快照、重连、回放
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
Client                     Server FrameSyncManager            Storage
  | 输入 I(t) ───────────────►|                                 |
  |                           | 帧推进 F(t)                     |
  |◄──────── 帧数据 D(t)      |                                 |
  |                           | 保存快照 S(t) ───────────────► |
  |                           | 保存输入历史 H(t) ───────────► |
断线                        重连                              |
  | 连接断开                  |                                 |
  |                           | 加载快照 S(t-k) ◄────────────  |
  | 重连请求 R                | 补发缺失帧 H(t-k..t)            |
  |◄──────── 重连响应         |                                 |
```

---

## 3. 功能设计

### 3.1 服务器运行 AstrumLogic

- 将 `AstrumProj/Library/ScriptAssemblies/AstrumLogic.dll` 引入服务器 `AstrumServer.csproj`
- `FrameSyncManager` 在 `ProcessRoomFrame` 中调用 `Room.FrameTick()`
- `Room` 内部使用 `World.Update()`、`SkillEffectSystem`、`HitSystem`
- `noEngineReferences = true`（`AstrumLogic.asmdef`）确保不引用 UnityEngine
- 所有 `UnityEngine.Vector3` 等类型替换为 `TrueSync.TSVector`

### 3.2 状态快照

- 快照内容：`Room` 序列化数据（包括 World、Entities、系统状态）
- 周期：每 10 帧（可配置）
- 存储：`StateSnapshotManager` 统一对接 Redis / File
- 压缩：使用 GZip (MemoryPack → GZip)
- 保留策略：最近 30 秒快照（约 60 份）

```csharp
public void SaveRoomSnapshot(string roomId, int frame)
{
    var room = _roomManager.GetRoom(roomId);
    if (room == null) return;

    var data = MemoryPackSerializer.Serialize(room);
    var compressed = Compress(data);

    _snapshotStore.Save(new RoomSnapshot
    {
        RoomId = roomId,
        Frame = frame,
        Timestamp = DateTime.UtcNow,
        Data = compressed
    });
}
```

### 3.3 输入历史

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

### 3.4 断线重连流程

1. 客户端发送 `ReconnectRequest(userId, roomId, lastFrame)`
2. 服务器校验用户、房间、状态
3. 加载最近快照（若最新快照失败，回退到上一份）
4. 生成错过的帧 `H(lastFrame+1 ... currentFrame)`
5. 返回 `ReconnectResponse(state, currentFrame, missedFrames)`
6. 客户端恢复状态并继续收发输入

```plaintext
Client → Server: ReconnectRequest
Server: Validate user/session/room
Server: Snapshot = LoadLatest(roomId)
Server: Missed = FrameHistory(lastFrame+1 .. current)
Server → Client: ReconnectResponse(Snapshot, CurrentFrame, Missed)
Client: Restore Room, Apply Missed Frames, Resume
```

### 3.5 回放机制

- 服务器在战斗开始/结束时自动录制回放
- 回放包：`ReplayMetadata + RoomSnapshot(start) + FrameHistory(start..end)`
- 客户端加载回放包 → 反序列化房间 → 逐帧推进 → 渲染
- 支持：播放/暂停、1x/2x/4x、跳转帧（通过重新模拟到目标帧）

```csharp
public class ReplayPlayer
{
    public void Load(byte[] replayData)
    {
        _replay = MemoryPackSerializer.Deserialize<ReplayData>(replayData);
        _room = MemoryPackSerializer.Deserialize<Room>(_replay.InitialState);
        _currentFrame = _replay.Metadata.StartFrame;
    }

    public void PlayNextFrame()
    {
        var frameInputs = _replay.FrameHistory[_currentFrame];
        _room.FrameTick(frameInputs);
        Render(_room);
        _currentFrame++;
    }
}
```

---

## 4. 关键模块与接口

### 4.1 FrameSyncManager (服务器)

- `StartRoomFrameSync(roomId)`: 初始化状态、开始记录
- `ProcessRoomFrame(roomId, frameState)`: 推进帧、分发结果
- `HandleSingleInput(roomId, SingleInput)`: 收集玩家输入
- `GetRoomState(roomId)`: 获取 Room 实例（用于快照/重连）
- `GetFrameHistory(roomId, start, end)`: 获取帧区间数据

### 4.2 StateSnapshotManager

- `Save(RoomSnapshot snapshot)`
- `RoomSnapshot? GetLatest(roomId)`
- `RoomSnapshot? GetPrevious(roomId, frame)`
- `DeleteOlderThan(roomId, TimeSpan window)`

### 4.3 Reconnect Handler

```csharp
private void HandleReconnect(Session session, ReconnectRequest req)
{
    var user = _userManager.GetUserBySessionId(session.Id.ToString());
    var roomState = _frameSyncManager.GetRoomState(req.RoomId);
    var snapshot = _snapshotStore.GetLatest(req.RoomId);
    var missedFrames = _frameHistoryStore.Fetch(req.RoomId, req.LastFrame + 1);

    var response = ReconnectResponse.Create();
    response.Success = true;
    response.RoomState = snapshot.Data;
    response.CurrentFrame = roomState.AuthorityFrame;
    response.MissedFrames = missedFrames;

    _networkManager.Send(session, response);
}
```

### 4.4 客户端恢复流程

```csharp
public void OnReconnectResponse(ReconnectResponse resp)
{
    var room = MemoryPackSerializer.Deserialize<Room>(Decompress(resp.RoomState));
    GameRuntime.SetRoom(room);

    foreach (var frame in resp.MissedFrames)
    {
        room.FrameTick(frame.FrameInputs);
    }

    LSController.Instance.AuthorityFrame = resp.CurrentFrame;
    LSController.Instance.PredictionFrame = resp.CurrentFrame;
}
```

---

## 5. 数据存储策略

| 数据类型 | 频率 | 内容 | 存储 | 保留策略 |
|----------|------|------|------|-----------|
| 状态快照 | 每10帧 | Room 序列化数据 | Redis/File | 最近30秒 |
| 输入历史 | 每帧 | OneFrameInputs | Redis | 最近15秒 |
| 回放包 | 战斗完成 | Metadata + Snapshot + Frames | 文件 | 运营策略 |

**压缩与增量**
- 快照采用定期完整快照 + 中间增量（待优化）
- 输入历史使用差分存储（仅变化的玩家输入）

---

## 6. 状态一致性与验证

- 状态哈希：`TSVector` + `CapabilityStates` + `SkillEffectSystem` Queue
- 客户端/服务器定期校验（可选）
- 断线重连完成后校验：若失败，强制重置到最新快照

```csharp
public long CalcStateHash(Room room)
{
    using var hash = new XxHash64();
    foreach (var entity in room.MainWorld.Entities.Values)
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

## 7. 性能与监控指标

| 指标 | 目标 | 监控方式 |
|------|------|----------|
| 快照大小 | < 200 KB/份 | 日志 & Prometheus |
| 快照写入耗时 | < 5 ms | Stopwatch | 
| 重连恢复耗时 | < 200 ms | 事件计时 |
| 回放文件大小 | < 50 MB/战斗 | 文件大小统计 |
| 状态一致性失败率 | 0 | 状态哈希比对 |

---

## 8. 风险与对策

| 风险 | 描述 | 对策 |
|------|------|------|
| 快照损坏 | 存储失败导致状态无法恢复 | 多副本保存，快照回退 |
| 帧历史过大 | 长时间战斗导致内存上涨 | 定期清理，差分压缩 |
| 兼容性问题 | 版本升级造成回放不可用 | 回放包记录版本，并做兼容处理 |
| Unity 引用残留 | `noEngineReferences=true` 仍可能漏掉 Unity 类型 | 全面替换 Unity 类型为 TrueSync |

---

## 9. 开发计划

1. **Unity 依赖剥离**（已完成）
2. **AstrumLogic.dll 引入服务器**
3. FrameSyncManager 调整，运行 `Room.FrameTick`
4. 状态快照 + 输入历史存储实现
5. 断线重连协议（proto）与实现
6. 回放录制/播放功能
7. 性能测试与监控落地

---

## 10. 相关文件

- `AstrumServer/AstrumServer/Managers/FrameSyncManager.cs`
- `AstrumServer/AstrumServer/Managers/RoomManager.cs`
- `AstrumServer/AstrumServer/Core/GameServer.cs`
- `AstrumProj/Assets/Script/AstrumLogic/Core/Room.cs`
- `AstrumProj/Assets/Script/AstrumLogic/Core/World.cs`
- `AstrumProj/Assets/Script/AstrumLogic/FrameSync/LSController.cs`
- `Docs/11-Network 网络系统/Frame-Sync-Mechanism 帧同步机制.md`

---

## 11. 元信息

- **Owner**: 网络组 @帧同步
- **上游任务**: Astrum 服务器运行 AstrumLogic、断线重连、战斗回放
- **变更摘要**: 初版断线重连与回放能力设计文档

---

*文档版本：v1.0*  
*创建时间：2025-11-13*  
*最后更新：2025-11-13*  
*状态：策划案*
