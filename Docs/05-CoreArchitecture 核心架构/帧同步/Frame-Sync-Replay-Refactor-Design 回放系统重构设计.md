@帧同步

# 回放系统重构设计

> 📖 **版本**: v1.1 | 📅 **最后更新**: 2025-01-27  
> 👥 **面向读者**: 客户端帧同步开发、回放系统维护  
> 🎯 **目标**: 梳理当前回放系统实现，识别问题并提出重构方案

**TL;DR**
- 当前实现：`ReplayGameMode` 负责加载回放文件、创建 Room、管理播放状态；`ReplayLSController` 负责逻辑推进；`ReplayUIView` 负责 UI 交互
- 核心问题：职责边界不清、UI 更新机制不完整、帧推进逻辑存在冗余、快照加载时机不当
- 重构方向：明确职责边界、完善 UI 更新机制、优化帧推进流程、改进快照加载策略
- **时间概念**：回放使用**相对时间**（从0开始），不显示绝对时间戳；UI 需要显示当前帧数和时间

---

## 1. 当前架构概述

### 1.1 组件关系

```text
┌─────────────────────────────────────────────────────────────┐
│                    ReplayGameMode                            │
│  - 加载回放文件（ReplayTimeline）                           │
│  - 创建 Room（使用快照初始化）                              │
│  - 管理播放状态（Play/Pause/Seek）                          │
│  - 控制 UI 显示/隐藏                                         │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                    Room                                      │
│  - 管理 MainWorld                                           │
│  - 持有 ReplayLSController                                  │
│  - Update() 调用 ReplayLSController.Tick(deltaTime)       │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              ReplayLSController                             │
│  - 管理 FrameBuffer（输入和快照）                           │
│  - Tick(deltaTime) 推进逻辑帧                               │
│  - FastForwardTo() 支持跳转                                 │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              ReplayTimeline                                 │
│  - 管理回放文件数据（BattleReplayFile）                     │
│  - 提供按帧查询输入和快照的能力                             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│              ReplayUIView                                    │
│  - 显示播放/暂停按钮、进度条、帧/时间显示                   │
│  - 通过 GameDirector 获取 ReplayGameMode                    │
│  - UIManager.Update() 驱动 UI 更新                          │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 数据流

```text
【加载阶段】
ReplayGameMode.Load(filePath)
  └─► ReplayTimeline.LoadFromFile()
  └─► 获取起始快照 S(0)
  └─► 解压缩快照数据
  └─► Room.Initialize("replay", worldSnapshot)
      └─► LoadWorldFromSnapshot() 创建 MainWorld
      └─► 创建 ReplayLSController
  └─► SetupSnapshotInFrameBuffer() 设置快照到 FrameBuffer
  └─► 打开 ReplayUI

【播放阶段】
ReplayGameMode.Update(deltaTime)
  └─► 预加载下一帧输入到 FrameBuffer
  └─► Room.Update(deltaTime)
      └─► ReplayLSController.Tick(deltaTime)
          └─► 从 FrameBuffer 读取输入
          └─► Room.FrameTick(inputs)
              └─► 更新实体输入组件
              └─► World.Update()
              └─► SkillEffectSystem.Update()

【UI 更新】
UIManager.Update()
  └─► ReplayUIView.Update()
      └─► 从 GameDirector 获取 ReplayGameMode
      └─► 更新播放/暂停按钮、进度条、帧/时间显示
```

---

## 2. 当前实现细节

### 2.1 ReplayGameMode

**职责**:
- 加载回放文件并创建 `ReplayTimeline`
- 创建 `Room` 并使用起始快照初始化
- 管理播放状态（`_isPlaying`、`_currentFrame`）
- 提供播放控制接口（`Play()`、`Pause()`、`Seek()`、`SeekToFrame()`）
- 管理 UI 显示/隐藏

**关键方法**:
- `Load(string filePath)`: 加载回放文件，创建 Room，设置快照
- `Update(float deltaTime)`: 预加载输入，更新 Room
- `CreateRoom(byte[] worldSnapshot)`: 创建 Room 并初始化
- `SetupSnapshotInFrameBuffer()`: 将快照数据设置到 FrameBuffer

**状态管理**:
- `_isPlaying`: 是否正在播放
- `_currentFrame`: 当前帧号（从 `_lsController.AuthorityFrame` 获取）

**时间属性**:
- `CurrentTimeSeconds`: 当前时间（秒），**相对时间，从0开始** = `CurrentFrame / TickRate`
- `DurationSeconds`: 总时长（秒） = `TotalFrames / TickRate`
- `CurrentFrame`: 当前帧号
- `TotalFrames`: 总帧数

**时间概念说明**:
- **回放使用相对时间**：从回放开始（第0帧）计时，不显示录制时的绝对时间戳
- 实时游戏逻辑中会记录时间戳（`StartTimestamp`），但回放关注的是**相对时间**
- 时间计算：`时间(秒) = 帧号 / 帧率`

### 2.2 ReplayLSController

**职责**:
- 管理 `FrameBuffer`（存储输入和快照）
- 推进逻辑帧（基于 `deltaTime` 和 `TickRate`）
- 支持快速跳转（`FastForwardTo()`）

**关键方法**:
- `Tick(float deltaTime)`: 基于本地时间推进帧
- `SetFrameInputs(int frame, OneFrameInputs inputs)`: 设置帧输入到 FrameBuffer
- `FastForwardTo(int targetFrame, Func<int, OneFrameInputs> getFrameInputs)`: 快速跳转到目标帧

**时间管理**:
- `_replayElapsedTime`: 回放本地时间（秒），**相对时间，从0开始**，通过 `deltaTime` 递增
- `TickRate`: 帧率（如 60 FPS）
- `AuthorityFrame`: 当前权威帧号

**时间概念说明**:
- **回放使用相对时间**：`_replayElapsedTime` 从0开始累计，不依赖 `TimeInfo` 或绝对时间戳
- 时间与帧的对应关系：`帧号 = floor(时间(秒) * TickRate)`
- `CreationTime` 字段保留用于兼容接口，但回放逻辑不依赖它

**帧推进逻辑**:
```csharp
// 计算期望到达的帧：floor(elapsedTime * TickRate)
int expectedFrame = (int)(_replayElapsedTime * TickRate);

// 推进到期望帧（单次最多推进若干帧，避免卡顿）
while (AuthorityFrame < expectedFrame && steps < MAX_FRAMES_PER_TICK)
{
    int nextFrame = AuthorityFrame + 1;
    OneFrameInputs inputs = _frameBuffer.FrameInputs(nextFrame);
    if (inputs != null)
    {
        AuthorityFrame = nextFrame;
        Room.FrameTick(inputs);
    }
}
```

### 2.3 ReplayUIView

**当前状态**: 业务逻辑部分（`ReplayUIView.cs`）只有框架代码，缺少实际实现

**预期职责**:
- 从 `GameDirector` 获取 `ReplayGameMode`
- 更新播放/暂停按钮状态
- 更新进度条（`Slider`）
- **更新帧数显示**（`frameText`）：显示当前帧/总帧数，格式如 "1234 / 5000"
- **更新时间显示**（`timeText`）：显示当前时间/总时长，格式如 "00:20 / 01:23"
- 处理用户交互（播放/暂停/拖动进度条）

**UI 元素**:
- `playButton` / `pauseButton`: 播放/暂停按钮
- `sliderSlider`: 进度条
- `frameText`: 帧数显示（当前帧 / 总帧数）
- `timeText`: 时间显示（当前时间 / 总时长）

**时间显示说明**:
- **回放使用相对时间**：从 0 开始计时，不显示绝对时间戳
- 当前时间 = `CurrentFrame / TickRate`（秒）
- 总时长 = `TotalFrames / TickRate`（秒）
- 时间格式：`mm:ss` 或 `hh:mm:ss`（根据时长自动选择）

### 2.4 Room

**回放相关修改**:
- `Initialize(string controllerType, byte[] worldSnapshot)`: 支持 `"replay"` 类型和快照初始化
- `Update(float deltaTime)`: 检查 `LSController` 类型，调用 `ReplayLSController.Tick(deltaTime)`
- `LoadWorldFromSnapshot(byte[] worldSnapshot)`: 从快照数据加载 World

---

## 3. 存在的问题

### 3.1 职责边界不清

**问题描述**:
- `ReplayGameMode.Update()` 中预加载输入的逻辑与 `ReplayLSController` 的职责重叠
- `ReplayGameMode` 同时管理播放状态（`_isPlaying`）和 `ReplayLSController.IsPaused`，存在两处状态
- `SetupSnapshotInFrameBuffer()` 在 `ReplayGameMode` 中，但快照管理应该是 `ReplayLSController` 的职责

**影响**:
- 代码耦合度高，难以维护
- 状态同步容易出错
- 职责不清晰，增加理解成本

### 3.2 UI 更新机制不完整

**问题描述**:
- `ReplayUIView.cs` 只有框架代码，缺少实际实现
- `OnUpdate()` 方法未实现，UI 无法更新
- 缺少 `RefreshReplayGameMode()`、`UpdateUI()`、`OnPlayButtonClicked()` 等方法
- **缺少帧数和时间的格式化显示逻辑**

**影响**:
- UI 无法正常显示和交互
- 用户无法控制回放播放
- 无法查看当前播放进度（帧数和时间）

**UI 显示需求**:
- **帧数显示**：格式 "当前帧 / 总帧数"，如 "1234 / 5000"
- **时间显示**：格式 "当前时间 / 总时长"，如 "00:20 / 01:23"
- **时间格式**：根据时长自动选择 `mm:ss` 或 `hh:mm:ss`
- **时间概念**：使用相对时间（从0开始），不显示绝对时间戳

### 3.3 帧推进逻辑存在冗余

**问题描述**:
- `ReplayGameMode.Update()` 中预加载下一帧输入，但 `ReplayLSController.Tick()` 也会从 `FrameBuffer` 读取输入
- `ReplayGameMode` 中注释掉了 `_lsController.Tick(deltaTime)` 调用，但 `Room.Update()` 中会调用
- `_currentFrame` 从 `_lsController.AuthorityFrame` 获取，存在数据同步问题

**影响**:
- 代码逻辑混乱，难以理解
- 可能存在帧推进不一致的问题

### 3.4 快照加载时机不当

**问题描述**:
- `SetupSnapshotInFrameBuffer()` 在 `CreateRoom()` 之后调用，但此时 `FrameBuffer` 可能还未准备好
- 快照数据同时用于 `Room.Initialize()` 和 `FrameBuffer`，但两者使用方式不同
- `FastForwardTo()` 需要从 `FrameBuffer` 读取快照，但快照可能未正确设置

**影响**:
- 快照加载可能失败
- `FastForwardTo()` 可能无法正常工作

### 3.5 状态同步问题

**问题描述**:
- `ReplayGameMode._isPlaying` 和 `ReplayLSController.IsPaused` 需要手动同步
- `ReplayGameMode._currentFrame` 和 `ReplayLSController.AuthorityFrame` 需要手动同步
- `Play()` 和 `Pause()` 方法需要同时更新两处状态

**影响**:
- 容易出现状态不一致
- 代码维护成本高

### 3.6 缺少错误处理

**问题描述**:
- `ReplayLSController.Tick()` 中如果 `FrameBuffer` 没有输入数据，只是 `break`，没有错误提示
- `FastForwardTo()` 不支持回退（`AuthorityFrame > targetFrame`），但缺少重新加载快照的逻辑
- `ReplayGameMode.Load()` 中如果快照加载失败，没有回滚机制

**影响**:
- 错误难以定位和修复
- 用户体验差

---

## 4. 重构方案

### 4.1 明确职责边界

**原则**:
- `ReplayGameMode`: 负责回放文件加载、Room 创建、UI 管理、播放控制接口
- `ReplayLSController`: 负责帧推进逻辑、FrameBuffer 管理、快照加载
- `ReplayUIView`: 负责 UI 显示和用户交互

**具体调整**:
1. 将 `SetupSnapshotInFrameBuffer()` 移到 `ReplayLSController`，作为 `LoadSnapshot()` 方法
2. 移除 `ReplayGameMode.Update()` 中的预加载逻辑，由 `ReplayLSController` 内部处理
3. 统一播放状态管理：`ReplayGameMode` 只管理 `_isPlaying`，通过 `ReplayLSController.IsPaused` 控制实际播放

### 4.2 完善 UI 更新机制

**实现内容**:
1. 实现 `ReplayUIView.OnUpdate()`，从 `GameDirector` 获取 `ReplayGameMode` 并更新 UI
2. 实现 `UpdateUI()` 方法，更新播放/暂停按钮、进度条、帧/时间显示
3. **实现帧数显示格式化**：
   - 格式：`"{CurrentFrame} / {TotalFrames}"`
   - 示例：`"1234 / 5000"`
4. **实现时间显示格式化**：
   - 格式：`"{CurrentTime} / {Duration}"`
   - 时间格式：`mm:ss`（时长 < 1小时）或 `hh:mm:ss`（时长 >= 1小时）
   - 示例：`"00:20 / 01:23"` 或 `"01:05:30 / 02:15:45"`
   - **使用相对时间**：从0开始，不显示绝对时间戳
5. 实现 `OnPlayButtonClicked()`、`OnPauseButtonClicked()`、`OnSliderValueChanged()` 等交互方法
6. 添加 `RefreshReplayGameMode()` 方法，缓存 `ReplayGameMode` 引用

**时间格式化辅助方法**:
```csharp
private string FormatTime(float seconds)
{
    int totalSeconds = (int)seconds;
    int hours = totalSeconds / 3600;
    int minutes = (totalSeconds % 3600) / 60;
    int secs = totalSeconds % 60;
    
    if (hours > 0)
        return $"{hours:D2}:{minutes:D2}:{secs:D2}";
    else
        return $"{minutes:D2}:{secs:D2}";
}
```

### 4.3 优化帧推进流程

**调整方案**:
1. 移除 `ReplayGameMode.Update()` 中的预加载逻辑
2. `ReplayLSController.Tick()` 内部处理输入预加载（如果需要）
3. `ReplayGameMode._currentFrame` 直接从 `ReplayLSController.AuthorityFrame` 获取，不单独维护
4. 确保 `Room.Update()` 正确调用 `ReplayLSController.Tick(deltaTime)`

### 4.4 改进快照加载策略

**调整方案**:
1. `ReplayLSController` 添加 `LoadSnapshot(int frame, byte[] snapshotData)` 方法
2. `ReplayGameMode.Load()` 中调用 `_lsController.LoadSnapshot(0, worldData)` 设置起始快照
3. `FastForwardTo()` 支持回退：如果 `AuthorityFrame > targetFrame`，查找目标帧之前的快照并重新加载

### 4.5 统一状态管理

**调整方案**:
1. `ReplayGameMode.Play()` 和 `Pause()` 只更新 `_isPlaying`，然后设置 `_lsController.IsPaused = !_isPlaying`
2. `ReplayGameMode.CurrentFrame` 属性直接返回 `_lsController.AuthorityFrame`
3. 移除 `ReplayGameMode._currentFrame` 字段

### 4.6 增强错误处理

**调整方案**:
1. `ReplayLSController.Tick()` 中如果缺少输入数据，记录警告日志
2. `FastForwardTo()` 支持回退：查找目标帧之前的快照，重新加载 World，然后推进到目标帧
3. `ReplayGameMode.Load()` 中添加回滚机制：如果任何步骤失败，清理已创建的资源

---

## 5. 重构后的架构

### 5.1 组件职责

**ReplayGameMode**:
- ✅ 加载回放文件（`ReplayTimeline`）
- ✅ 创建 Room 并初始化
- ✅ 管理 UI 显示/隐藏
- ✅ 提供播放控制接口（委托给 `ReplayLSController`）
- ❌ 不再管理 FrameBuffer
- ❌ 不再预加载输入

**ReplayLSController**:
- ✅ 管理 FrameBuffer（输入和快照）
- ✅ 推进逻辑帧
- ✅ 支持快速跳转（包括回退）
- ✅ 加载和管理快照
- ✅ 内部处理输入预加载（如果需要）

**ReplayUIView**:
- ✅ 显示播放/暂停按钮、进度条、帧/时间显示
- ✅ **格式化显示帧数**：`"当前帧 / 总帧数"`
- ✅ **格式化显示时间**：`"当前时间 / 总时长"`（相对时间，从0开始）
- ✅ 处理用户交互
- ✅ 从 `GameDirector` 获取 `ReplayGameMode` 并更新 UI

### 5.2 数据流（重构后）

```text
【加载阶段】
ReplayGameMode.Load(filePath)
  └─► ReplayTimeline.LoadFromFile()
  └─► 获取起始快照 S(0)
  └─► 解压缩快照数据
  └─► Room.Initialize("replay", worldSnapshot)
      └─► LoadWorldFromSnapshot() 创建 MainWorld
      └─► 创建 ReplayLSController
  └─► _lsController.LoadSnapshot(0, worldData)
      └─► 设置快照到 FrameBuffer
  └─► 打开 ReplayUI

【播放阶段】
ReplayGameMode.Update(deltaTime)
  └─► Room.Update(deltaTime)
      └─► ReplayLSController.Tick(deltaTime)
          └─► 内部预加载输入（如果需要）
          └─► 从 FrameBuffer 读取输入
          └─► Room.FrameTick(inputs)

【UI 更新】
UIManager.Update()
  └─► ReplayUIView.Update()
      └─► RefreshReplayGameMode()
      └─► UpdateUI()
          └─► 更新播放/暂停按钮（基于 _isPlaying）
          └─► 更新进度条（基于 CurrentFrame / TotalFrames）
          └─► 更新帧数显示（格式："1234 / 5000"）
          └─► 更新时间显示（格式："00:20 / 01:23"，相对时间从0开始）
```

---

## 6. 关键决策与取舍

### 6.1 职责划分

**问题**: `ReplayGameMode` 和 `ReplayLSController` 的职责边界如何划分？

**备选方案**:
1. `ReplayGameMode` 负责所有回放相关逻辑，`ReplayLSController` 只负责帧推进
2. `ReplayGameMode` 只负责文件加载和 UI 管理，`ReplayLSController` 负责所有逻辑相关

**选择**: 方案 2 - `ReplayGameMode` 作为协调者，`ReplayLSController` 负责核心逻辑

**影响**: 
- ✅ 职责清晰，易于维护
- ✅ `ReplayLSController` 可以独立测试
- ⚠️ `ReplayGameMode` 需要更多委托调用

### 6.2 UI 更新机制

**问题**: UI 更新应该由谁驱动？

**备选方案**:
1. `ReplayUIView` 使用协程或 `Update()` 主动更新
2. `UIManager.Update()` 统一驱动所有 UI 的 `Update()`

**选择**: 方案 2 - `UIManager` 统一驱动

**影响**:
- ✅ 统一管理，性能可控
- ✅ 符合现有 UI 架构
- ⚠️ 需要确保 `UIBase` 基类正确实现

### 6.3 快照加载策略

**问题**: 快照应该在哪里加载和管理？

**备选方案**:
1. `ReplayGameMode` 加载快照，然后传递给 `ReplayLSController`
2. `ReplayLSController` 自己管理快照加载

**选择**: 方案 1 - `ReplayGameMode` 负责加载，`ReplayLSController` 负责存储和使用

**影响**:
- ✅ `ReplayGameMode` 可以控制加载时机
- ✅ `ReplayLSController` 专注于逻辑处理
- ⚠️ 需要明确接口定义

---

---

## 8. 相关文档

- [帧同步回放GameMode设计](Frame-Sync-Replay-Design%20帧同步回放GameMode设计.md)
- [帧同步状态同步与恢复机制设计](../帧同步/Frame-Sync-State-Sync-Design%20帧同步状态同步与恢复机制设计.md)

---

*文档版本：v1.1*  
*创建时间：2025-01-27*  
*最后更新：2025-01-27*  
*Owner*: AI Assistant  
*变更摘要*: 补充UI显示帧数和时间的说明，明确回放使用相对时间（从0开始）的概念

