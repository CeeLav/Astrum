@帧同步

# 帧同步回放 GameMode 设计

> 📖 **版本**: v1.0 | 📅 **最后更新**: 2025-11-23  
> 👥 **面向读者**: 客户端帧同步开发、服务器战斗记录/状态快照开发  
> 🎯 **目标**: 基于现有帧同步与快照机制，实现可播放/暂停/拖动进度的战斗回放 GameMode

**TL;DR**
- 回放基于**世界快照 + 帧输入历史**，不重新发明状态保存机制，直接复用 `StateSnapshotManager` 产物  
- 服务器在战斗过程中统一记录：周期性世界快照 `S(n)` + 每帧输入 `H(n)`，战斗结束后打包成单一回放文件  
- 客户端增加 `ReplayGameMode`：加载指定回放文件，使用本地 `ClientLSController` 按录制输入推进逻辑  
- 播放/暂停：仅切换时间推进与否，不改变逻辑帧序列  
- 拖动/倒退：查找目标帧之前最近快照 `S(k)`，加载快照后从 `k+1` 快速重算到目标帧  
- 回放期间关闭网络依赖，所有帧输入来自本地文件，保证与实战结果**确定性一致**

---

## 1. 概述

战斗回放需要在客户端以**离线方式**重演一局战斗全过程，并支持：

- **指定文件回放**：从服务端下载或本地选择一份战斗记录文件进行回放  
- **基本控制**：播放 / 暂停 / 停止 / 拖动进度条（支持前后任意跳转）  
- **确定性一致**：同一回放文件在任意客户端播放，结果帧状态与服务器战斗结束状态一致  

本方案基于 `Frame-Sync-State-Sync-Design 帧同步状态同步与恢复机制设计.md` 中已经定义的：

- 服务器**权威世界快照**：`World` 的 MemoryPack 快照，周期性保存  
- 每帧输入历史 `H(t)`：用于断线重连和状态恢复  

回放仅在此基础上增加：

- 战斗期间在服务器侧**收集并持久化**该房间的快照与帧输入  
- 战斗结束时生成**单一回放文件**（可下载/分发）  
- 客户端增加 `ReplayGameMode`，不依赖网络、只消费回放文件数据驱动本地 `ClientLSController`。

---

## 2. 整体架构设计

### 2.1 组件关系（录制端 + 回放端）

```text
┌─────────────────────────────────────────────────────────────┐
│                      Server（权威）                        │
│                                                             │
│  FrameSyncManager      StateSnapshotManager                 │
│        │                         │                          │
│        └────► BattleReplayRecorder ◄────────────────────┐   │
│                              │                         │   │
│                              ▼                         │   │
│                      BattleReplayFile（内存结构）       │   │
│                              │                         │   │
│                              ▼                         │   │
│                        回放文件（磁盘） ────────────────┘   │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                      Client（Unity）                        │
│                                                             │
│  ReplayUI（进度条/按钮）                                    │
│        │                                                    │
│        ▼                                                    │
│  ReplayGameMode -----------------------------------------┐  │
│        │                                                 │  │
│        ▼                                                 │  │
│  ReplayTimeline（回放时间线/索引）                       │  │
│        │                                                 │  │
│        ▼                                                 │  │
│  ClientLSController（本地离线帧同步）                    │  │
│        │                                                 │  │
│        ▼                                                 │  │
│  World / ViewSync（逻辑执行 + 视图更新）                  │  │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 数据流（高层）

```text
【实战录制】
客户端输入 I(t) ─► 服务器 FrameSyncManager
                         │
                         ├─► StateSnapshotManager：按周期 SaveWorldSnapshot(S(t))
                         └─► BattleReplayRecorder：记录 S(t) & H(t)

战斗结束 ─► BattleReplayRecorder.Finish(roomId)
         └─► 生成 BattleReplayFile（内存）
             └─► 序列化到磁盘回放文件

【客户端回放】
选择回放文件 ─► ReplayGameMode.Load(filePath)
             └─► 反序列化 BattleReplayFile → ReplayTimeline
             └─► 从起始快照 S(start) 装载 World 到 ClientLSController
             └─► 本地 Tick() 读取 H(t) 驱动逻辑帧（无网络）
```

---

## 3. 服务器录制设计

### 3.1 录制内容

录制目标是，使客户端在**不依赖服务器**的情况下，仅凭回放文件即可恢复完整战斗过程：

- **世界快照序列**：`WorldSnapshot` 列表，至少包含：
  - 第 0 帧快照（战斗开始）  
  - 之后按配置周期（例如每 10 帧）生成的快照 `S(n)`  
- **帧输入历史**：每一逻辑帧的 `OneFrameInputs` 或等价结构 `H(n)`  
  - 包含所有玩家在该帧的输入  
  - 对应服务器权威帧推进使用的真实数据  

### 3.2 BattleReplayRecorder（服务器）

**职责**：绑定到单个房间，对战斗全程进行录制，并在战斗结束时生成一个 `BattleReplayFile`。

- **创建时机**：
  - `FrameSyncManager.StartRoomFrameSync(roomId)` 时，为该房间创建 `BattleReplayRecorder` 实例  
- **销毁时机**：
  - 房间战斗结束（结算完成/销毁 `Room`）时调用 `Finish()`，并释放引用  

**伪接口示例（逻辑）：**

```csharp
public sealed class BattleReplayRecorder
{
    public void OnWorldSnapshot(WorldSnapshot snapshot);   // 来自 StateSnapshotManager
    public void OnFrameInputs(int frame, OneFrameInputs inputs); // 来自 FrameSyncManager

    public BattleReplayFile Finish();                      // 战斗结束时生成最终文件结构
}
```

集成点：

- `StateSnapshotManager.SaveWorldSnapshot(roomId, frame, world)` 成功后回调 `OnWorldSnapshot`  
- `FrameSyncManager` 在广播某帧 `OneFrameInputs` 时，调用 `OnFrameInputs` 进行记录  

### 3.3 回放文件结构（逻辑层）

回放文件的逻辑结构仅在文档中约定，具体实现仍使用 MemoryPack/Proto 等现有序列化方案。

```csharp
public sealed class BattleReplayFile
{
    public int Version;              // 回放格式版本，支持后续兼容
    public string RoomId;
    public int TickRate;             // 帧率，保证播放节奏一致
    public int TotalFrames;          // 战斗结束时的最终帧
    public long StartTimestamp;      // 战斗起始 UTC 时间
    public int RandomSeed;           // 该战斗使用的随机种子（确保确定性）

    public List<ReplayPlayerInfo> Players;      // 玩家列表（名称/职业等展示用）
    public List<ReplaySnapshot> Snapshots;      // 世界快照 S(n)
    public List<ReplayFrameInputs> FrameInputs; // 帧输入序列 H(n)
}

public sealed class ReplaySnapshot
{
    public int Frame;                // 快照对应的逻辑帧号
    public byte[] WorldData;         // GZip(MemoryPack(World))，与 StateSnapshotManager 保持一致
}

public sealed class ReplayFrameInputs
{
    public int Frame;                // 帧号
    public byte[] InputsData;        // 对应 OneFrameInputs 的序列化结果（Proto/MemoryPack 均可）
}
```

**约束与约定：**

- `Snapshots` 按 `Frame` 升序存储，第一个必须是第 0 帧或起始帧  
- `FrameInputs` 至少覆盖 `[startFrame, TotalFrames]` 区间的所有帧  
- 回放文件仅作为**战斗记录快照**，不参与在线逻辑，避免影响生产链路  

---

## 4. 客户端回放 GameMode 设计

### 4.1 ReplayGameMode（核心入口）

**位置（规划）**：`AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/ReplayGameMode.cs`

**职责**：

- 从指定路径加载回放文件并构建 `ReplayTimeline`  
- 创建/初始化本地 `ClientLSController` 与逻辑世界 `World`  
- 驱动本地逻辑帧推进（不依赖网络），控制播放/暂停/跳转  
- 与 `ReplayUI` 对接：更新进度条、当前时间、状态（播放/暂停）  

**关键状态：**

- `BattleReplayFile _replayData`  
- `ReplayTimeline _timeline`  
- `ClientLSController _lsController`  
- `int _currentFrame`  
- `bool _isPlaying`  

### 4.2 ReplayTimeline（时间线与索引）

**职责**：封装 `BattleReplayFile`，提供按帧号查询输入与快照的能力。

- 根据 `ReplaySnapshot.Frame` 构建有序列表，支持二分查找最近快照：
  - `GetNearestSnapshot(frame)` → 返回 `frame0 <= frame` 且距离最近的快照  
- 为 `FrameInputs` 提供快速访问：
  - 内部使用 `Dictionary<int, ReplayFrameInputs>` 或稀疏数组  
- 提供基础信息：`TotalFrames`、`TickRate`、起始帧等  

### 4.3 回放播放循环

**逻辑帧推进**：

- `ReplayGameMode.Tick(deltaTime)` 中，根据 `TickRate` 将真实时间映射为目标逻辑帧：
  - `targetFrame = startFrame + floor(elapsedTime * TickRate)`  
- 当 `_isPlaying == true` 时：
  - 逐帧从 `_currentFrame+1` 推进到 `targetFrame`：
    - 对于每个 `frame`：
      - 从 `_timeline` 取出 `ReplayFrameInputs`，转为 `OneFrameInputs` 调用 `_lsController.SetOneFrameInputs(...)`  
      - 调用 `_lsController.Tick()` 推进一帧逻辑  
  - 逻辑帧推进完毕后，触发视图同步（沿用现有逻辑，例如 World→View 的同步系统）  

**暂停**：

- 将 `_isPlaying` 置为 `false`，`Tick()` 仍更新 UI 时间显示但不再推进逻辑帧。  

---

## 5. 拖动/回退设计（核心逻辑）

### 5.1 需求拆解

- **拖动进度条**：用户可将进度条拖到任意时间点/帧号  
- **回退**：从较大的帧跳回较小的帧，不允许直接“倒着运算”  
- 性能要求：拖动响应应在可接受时间内完成，不出现长时间卡死  

### 5.2 基本策略

拖动或回退时统一采用：**“最近快照 + 向前重放”** 策略：

1. 计算目标帧 `targetFrame`（由 UI 转换自进度条百分比）  
2. 使用 `ReplayTimeline.GetNearestSnapshot(targetFrame)` 找到 `snapshotFrame <= targetFrame` 的最近快照  
3. 调用 `_lsController.LoadState(snapshot.WorldData)` 恢复世界  
4. 将 `_currentFrame = snapshotFrame`  
5. 在单帧或多帧内快速执行：
   - 从 `snapshotFrame+1` 到 `targetFrame`：按录制的 `ReplayFrameInputs` 调用 `_lsController.SetOneFrameInputs()` + `_lsController.Tick()`  
   - 在这段“追帧”过程中可以：
     - 关闭中间帧的视图插值，仅在到达 `targetFrame` 时进行一次强制视图同步  
6. 更新进度条和当前时间显示  

### 5.3 伪流程

```text
OnSeek(float normalizedProgress)
    targetFrame = (int)(normalizedProgress * _timeline.TotalFrames)

    snapshot = _timeline.GetNearestSnapshot(targetFrame)
    LoadWorldFromSnapshot(snapshot)              // _lsController.LoadState(...)
    _currentFrame = snapshot.Frame

    for frame in (_currentFrame + 1) .. targetFrame:
        inputs = _timeline.GetFrameInputs(frame)
        _lsController.SetOneFrameInputs(inputs)
        _lsController.Tick()                     // 仅逻辑推进，可选关闭中间渲染

    _currentFrame = targetFrame
    ForceSyncView()                             // 将最终逻辑状态同步到可见表现
```

**说明：**

- 快照间隔（例如每 10 帧）会直接影响 Seek 性能：间隔越小，拖动时需要重算的帧数越少，但文件变大  
- 可在 `Frame-Sync-State-Sync-Design` 中统一配置快照周期，避免两套策略不一致  

---

## 6. UI 交互与 GameMode 集成

### 6.1 ReplayUI 与 ReplayGameMode 协议

UI 层只关心“时间”与“控制”，不直接接触具体帧同步细节：

- `Play()` / `Pause()` / `Stop()`  
- `Seek(float normalized)`：0~1，对应 0~TotalFrames  
- `OnLoaded(BattleReplayFileInfo info)`：显示对局时长、对局双方信息等  

`ReplayGameMode` 对外暴露：

- `float Progress { get; }`：当前进度 0~1  
- `float DurationSeconds { get; }`：总时长（基于 `TotalFrames / TickRate`）  
- `bool IsPlaying { get; }`  

### 6.2 与现有 GameMode/FrameSyncHandler 的关系

- 回放 GameMode 下：
  - 不再通过网络接收 `FrameSyncStartNotification` / `OneFrameInputs`  
  - `FrameSyncHandler` 不参与回放逻辑，避免与真实在线帧同步混用  
  - `ClientLSController` 的使用方式与在线模式保持一致，只是输入来源改为本地文件  
- 通过 GameModeFactory（若存在）新增一种模式：`Replay`，从回放入口场景或观战界面进入。  

---

## 7. 关键决策与取舍（简要）

- **问题**：拖动/回退如何保证性能与一致性？  
  - **备选**：
    - A. 每帧都做完整快照 → 拖动无重算，但文件巨大  
    - B. 固定周期快照 + 从 0 帧重算 → 文件可控，但拖动越靠后需要重算越久  
    - C. 固定周期快照 + 最近快照重算 → 在文件大小与拖动性能之间折中  
  - **选择**：C，采用周期性快照 + 最近快照重算策略，与断线重连设计保持一致  
  - **影响**：
    - 文件大小与已有快照策略一致，无额外数量级增长  
    - 拖动性能可通过统一调整快照周期与压缩策略进行控制  

- **问题**：回放逻辑放在客户端还是服务器？  
  - **选择**：完全在客户端本地复演，服务器只负责产生回放文件，不参与具体回放过程  
  - **影响**：减轻服务器压力，同时避免在线逻辑与回放逻辑耦合。  

---

## 8. 相关文档与追溯

- 上游设计：`Frame-Sync-Mechanism 帧同步机制.md`  
- 上游设计：`Frame-Sync-State-Sync-Design 帧同步状态同步与恢复机制设计.md`  
- 相关重构：`LSController-Refactor-Design LSController重构设计.md`  
- 计划落地代码（示例路径）：
  - 客户端：`AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/ReplayGameMode.cs`  
  - 客户端：`AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/ReplayTimeline.cs`  
  - 服务器：`AstrumServer/.../FrameSync/BattleReplayRecorder.cs`  

---

*文档版本：v1.0*  
*创建时间：2025-11-23*  
*最后更新：2025-11-23*  
*状态：策划案*  
*Owner*: 待定  
*变更摘要*: 初始创建帧同步回放 GameMode 技术设计文档。


