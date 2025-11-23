# 帧同步回放 GameMode 开发进展

> 📊 **当前版本**: v0.1.0  
> 📅 **最后更新**: 2025-01-27  
> 👤 **负责人**: 待定

## TL;DR（四象限）
- **状态/进度**：📝 策划案完成，准备开始开发
- **已达成**：技术设计文档完成，包含服务器录制、客户端回放、UI集成等完整方案
- **风险/阻塞**：无
- **下一步**：开始实现服务器端 BattleReplayRecorder，然后实现客户端 ReplayLSController 和 ReplayGameMode

---

## 版本历史

### v0.1.0 - 初始规划 (2025-01-27)
**状态**: 📝 策划案完成

**完成内容**:
- [x] 技术设计文档完成（`Frame-Sync-Replay-Design 帧同步回放GameMode设计.md`）
- [x] 回放机制架构设计
- [x] 服务器录制方案设计
- [x] 客户端回放 GameMode 设计
- [x] ReplayLSController 设计
- [x] Login 窗口回放入口设计

**待完成**:
- [ ] 服务器端：BattleReplayRecorder 实现
- [ ] 服务器端：回放文件序列化/反序列化
- [ ] 客户端：ReplayLSController 实现
- [ ] 客户端：ReplayTimeline 实现
- [ ] 客户端：ReplayGameMode 实现
- [ ] 客户端：Login 窗口回放入口实现
- [ ] 客户端：回放控制 UI（播放/暂停/拖动）

**预计工时**: 40-50 小时

---

## 当前阶段

**阶段名称**: 开发准备阶段

**完成度**: 10%

**下一步计划**:
1. **服务器端开发**（优先级：高）
   - 实现 `BattleReplayRecorder` 类
   - 集成到 `FrameSyncManager` 和 `StateSnapshotManager`
   - 实现回放文件序列化（MemoryPack）
   - 测试录制功能

2. **客户端核心实现**（优先级：高）
   - 实现 `ReplayLSController`（基于 `ILSControllerBase`）
   - 实现 `ReplayTimeline`（回放文件索引）
   - 实现 `ReplayGameMode`（回放主逻辑）

3. **客户端 UI 集成**（优先级：中）
   - Login 窗口添加回放文件地址输入框
   - 实现回放控制 UI（播放/暂停/进度条）
   - 实现地址缓存功能（PlayerPrefs）

4. **测试与优化**（优先级：中）
   - 回放功能测试
   - 跳转性能优化
   - 文件大小优化

---

## 开发任务清单

### 服务器端任务

#### 1. BattleReplayRecorder 实现
- [ ] 创建 `BattleReplayRecorder` 类
- [ ] 实现 `OnWorldSnapshot()` 方法
- [ ] 实现 `OnFrameInputs()` 方法
- [ ] 实现 `Finish()` 方法生成回放文件
- [ ] 集成到 `FrameSyncManager.StartRoomFrameSync()`
- [ ] 集成到 `StateSnapshotManager.SaveWorldSnapshot()`

**相关文档**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 3 节

#### 2. 回放文件序列化
- [ ] 定义 `BattleReplayFile` 数据结构（MemoryPack）
- [ ] 定义 `ReplaySnapshot` 数据结构
- [ ] 定义 `ReplayFrameInputs` 数据结构
- [ ] 实现序列化方法
- [ ] 实现反序列化方法（客户端使用）
- [ ] 文件压缩（GZip）支持

**相关文档**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 3.3 节

### 客户端任务

#### 3. ReplayLSController 实现
- [ ] 创建 `ReplayLSController` 类，实现 `ILSControllerBase`
- [ ] 实现 `Tick(float deltaTime)` 方法（本地时间推进）
- [ ] 实现 `SetFrameInputs()` 方法
- [ ] 实现 `FastForwardTo()` 方法（跳转功能）
- [ ] 实现 `LoadState()` 方法（快照加载）
- [ ] 实现 `SaveState()` 方法（快照保存）

**相关文档**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 4.3 节  
**相关代码**: `AstrumProj/Assets/Script/AstrumLogic/Core/ReplayLSController.cs`

#### 4. ReplayTimeline 实现
- [ ] 创建 `ReplayTimeline` 类
- [ ] 实现回放文件加载（反序列化）
- [ ] 实现 `GetNearestSnapshot(int frame)` 方法（二分查找）
- [ ] 实现 `GetFrameInputs(int frame)` 方法
- [ ] 提供基础信息访问（TotalFrames、TickRate 等）

**相关文档**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 4.2 节  
**相关代码**: `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/ReplayTimeline.cs`

#### 5. ReplayGameMode 实现
- [ ] 创建 `ReplayGameMode` 类，继承 `BaseGameMode`
- [ ] 实现 `Load(string filePath)` 方法（加载回放文件）
- [ ] 实现 `Tick(float deltaTime)` 方法（驱动回放）
- [ ] 实现播放/暂停控制
- [ ] 实现跳转功能（调用 `ReplayLSController.FastForwardTo()`）
- [ ] 集成视图同步

**相关文档**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 4.1、4.4 节  
**相关代码**: `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/ReplayGameMode.cs`

#### 6. Login 窗口回放入口
- [ ] 修改 Login Prefab，添加 `ReplayFilePathInputField`
- [ ] 重新生成 UI 代码（UI Generator）
- [ ] 在 `LoginView.cs` 实现地址缓存（PlayerPrefs）
- [ ] 实现 `OnReplayButtonClicked()` 方法
- [ ] 在 `LoginGameMode.cs` 实现 `StartReplay()` 方法

**相关文档**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 6.1 节  
**相关代码**: 
- `AstrumProj/Assets/Script/AstrumClient/UI/Generated/LoginView.cs`
- `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/LoginGameMode.cs`

#### 7. 回放控制 UI
- [ ] 创建 ReplayUI Prefab（播放/暂停按钮、进度条）
- [ ] 生成 UI 代码
- [ ] 实现播放/暂停控制
- [ ] 实现进度条拖动（调用 `ReplayGameMode.Seek()`）
- [ ] 显示当前时间/总时长

**相关文档**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 6.2 节

---

## 技术要点

### 关键实现细节

1. **快照理解**：快照保存的是该帧输入运算**前**的状态，加载后需要运行该帧输入才能得到运算后状态
2. **时间管理**：回放使用本地时间（`_replayElapsedTime`），通过 `deltaTime` 递增，不依赖 `TimeInfo`
3. **跳转优化**：使用最近快照 + 快速推进策略，支持关闭中间帧渲染
4. **文件格式**：使用 MemoryPack 序列化，GZip 压缩

### 依赖关系

- **服务器端**：依赖 `FrameSyncManager`、`StateSnapshotManager`、`AstrumLogic`
- **客户端**：依赖 `ILSControllerBase`、`Room`、`World`、`FrameBuffer`
- **UI**：依赖现有 UI 系统（UIRefs、UI Generator）

---

## 相关文档

- **技术设计**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md`
- **上游设计**: `Frame-Sync-Mechanism 帧同步机制.md`
- **上游设计**: `Frame-Sync-State-Sync-Design 帧同步状态同步与恢复机制设计.md`
- **相关重构**: `LSController-Refactor-Design LSController重构设计.md`

---

*文档版本：v0.1.0*  
*创建时间：2025-01-27*  
*最后更新：2025-01-27*  
*状态：开发准备阶段*

