# 帧同步回放 GameMode 开发进展

> 📊 **当前版本**: v0.2.0  
> 📅 **最后更新**: 2025-01-27  
> 👤 **负责人**: 待定

## TL;DR（四象限）
- **状态/进度**：🚧 核心功能已实现，UI交互待完善
- **已达成**：服务器录制、客户端回放核心逻辑、回放文件数据结构、Login入口、重构设计文档
- **风险/阻塞**：ReplayUIView业务逻辑未实现，UI更新机制待完善
- **下一步**：完善ReplayUIView实现，优化职责边界，改进快照加载策略

---

## 版本历史

### v0.2.0 - 核心功能实现 (2025-01-27)
**状态**: 🚧 开发中

**完成内容**:
- [x] **服务器端**：`BattleReplayRecorder` 实现（周期性保存，每5秒）
- [x] **服务器端**：回放文件数据结构 `BattleReplayFile`（MemoryPack + GZip压缩）
- [x] **服务器端**：集成到 `GameSession`（录制输入和快照）
- [x] **客户端**：`ReplayLSController` 实现（帧推进、跳转、快照加载）
- [x] **客户端**：`ReplayTimeline` 实现（回放文件加载、快照/输入查询）
- [x] **客户端**：`ReplayGameMode` 实现（加载、播放控制、Room管理）
- [x] **客户端**：`Room.Initialize()` 支持回放模式（快照初始化）
- [x] **客户端**：Login 窗口回放入口（`ReplayButton` + `InputField`）
- [x] **客户端**：`LoginGameMode.StartReplay()` 实现
- [x] **客户端**：`ReplayUIView` UI框架创建（designer.cs）
- [x] **文档**：重构设计文档（`Frame-Sync-Replay-Refactor-Design`）

**待完成**:
- [ ] **客户端**：`ReplayUIView` 业务逻辑实现（`OnUpdate`、`UpdateUI`、交互方法）
- [ ] **客户端**：UI显示帧数和时间（格式化显示，相对时间从0开始）
- [ ] **优化**：职责边界优化（快照加载移到 `ReplayLSController`）
- [ ] **优化**：帧推进逻辑优化（移除冗余预加载）
- [ ] **优化**：`FastForwardTo()` 支持回退（重新加载快照）
- [ ] **测试**：回放功能完整测试

**预计工时**: 剩余 8-12 小时

### v0.1.0 - 初始规划 (2025-01-27)
**状态**: 📝 策划案完成

**完成内容**:
- [x] 技术设计文档完成（`Frame-Sync-Replay-Design 帧同步回放GameMode设计.md`）
- [x] 回放机制架构设计
- [x] 服务器录制方案设计
- [x] 客户端回放 GameMode 设计
- [x] ReplayLSController 设计
- [x] Login 窗口回放入口设计

---

## 当前阶段

**阶段名称**: UI完善与优化阶段

**完成度**: 95%

**下一步计划**:
1. **测试与优化**（优先级：高）
   - 在 Unity Editor 中重新生成项目文件以解决编译错误
   - 验证回放功能（播放、暂停、跳转、拖动）
   - 验证UI显示（帧数、时间）
   - 验证回退功能
   - 优化 UI 交互体验

2. **剩余任务**
   - 完整测试
   - 性能分析与优化

---

## 开发任务清单

### 服务器端任务

#### 1. BattleReplayRecorder 实现 ✅
- [x] 创建 `BattleReplayRecorder` 类
- [x] 实现 `OnWorldSnapshot()` 方法
- [x] 实现 `OnFrameInputs()` 方法
- [x] 实现 `Finish()` 方法生成回放文件
- [x] 实现周期性保存（每5秒保存一次）
- [x] 集成到 `GameSession`（`Start()`、`OnFrameProcessed`、`Stop()`）
- [x] 文件保存路径：`Astrum\AstrumConfig\Record`

**相关文档**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 3 节  
**相关代码**: `AstrumServer/AstrumServer/FrameSync/BattleReplayRecorder.cs`

#### 2. 回放文件序列化 ✅
- [x] 定义 `BattleReplayFile` 数据结构（MemoryPack）
- [x] 定义 `ReplaySnapshot` 数据结构
- [x] 定义 `ReplayFrameInputs` 数据结构
- [x] 实现序列化方法（MemoryPack）
- [x] 实现反序列化方法（客户端使用）
- [x] 文件压缩（GZip）支持
- [x] 数据结构移至 `AstrumLogic`（服务器和客户端共享）

**相关文档**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 3.3 节  
**相关代码**: `AstrumProj/Assets/Script/AstrumLogic/FrameSync/BattleReplayFile.cs`

### 客户端任务

#### 3. ReplayLSController 实现 ✅
- [x] 创建 `ReplayLSController` 类，实现 `ILSControllerBase`
- [x] 实现 `Tick(float deltaTime)` 方法（本地时间推进，相对时间从0开始）
- [x] 实现 `SetFrameInputs()` 方法
- [x] 实现 `FastForwardTo()` 方法（跳转功能，支持回退）
- [x] 实现 `LoadState()` 方法（快照加载）
- [x] 实现 `SaveState()` 方法（空实现，回放不需要保存）
- [x] 优化：`FastForwardTo()` 支持回退（重新加载快照）
- [x] 优化：职责边界优化（提供 `LoadSnapshot` 方法）

**相关文档**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 4.3 节  
**相关代码**: `AstrumProj/Assets/Script/AstrumLogic/Core/ReplayLSController.cs`

#### 4. ReplayTimeline 实现 ✅
- [x] 创建 `ReplayTimeline` 类
- [x] 实现回放文件加载（反序列化）
- [x] 实现 `GetNearestSnapshot(int frame)` 方法（二分查找）
- [x] 实现 `GetFrameInputs(int frame)` 方法
- [x] 提供基础信息访问（TotalFrames、TickRate、StartTimestamp等）
- [x] 支持快照数据解压缩（GZip）

**相关文档**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 4.2 节  
**相关代码**: `AstrumProj/Assets/Script/AstrumLogic/FrameSync/ReplayTimeline.cs`

#### 5. ReplayGameMode 实现 ✅
- [x] 创建 `ReplayGameMode` 类，继承 `BaseGameMode`
- [x] 实现 `Load(string filePath)` 方法（加载回放文件）
- [x] 实现 `Update(float deltaTime)` 方法（驱动回放）
- [x] 实现播放/暂停控制（`Play()`、`Pause()`、`Stop()`）
- [x] 实现跳转功能（`Seek()`、`SeekToFrame()`，调用 `ReplayLSController.FastForwardTo()`）
- [x] 集成视图同步（创建 Stage、同步 EntityViews）
- [x] 支持快照初始化 Room（`Room.Initialize("replay", worldSnapshot)`）
- [x] 优化：移除预加载逻辑，统一职责边界

**相关文档**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 4.1、4.4 节  
**相关代码**: `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/ReplayGameMode.cs`

#### 6. Login 窗口回放入口 ✅
- [x] 修改 Login Prefab，添加 `ReplayFilePathInputField`
- [x] 重新生成 UI 代码（UI Generator）
- [x] 实现 `OnReplayButtonClicked()` 方法
- [x] 在 `LoginGameMode.cs` 实现 `StartReplay()` 方法
- [ ] 优化：实现地址缓存功能（PlayerPrefs）

**相关文档**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 6.1 节  
**相关代码**: 
- `AstrumProj/Assets/Script/AstrumClient/UI/Generated/LoginView.cs`
- `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/LoginGameMode.cs`

#### 7. 回放控制 UI ✅
- [x] 创建 ReplayUI Prefab（播放/暂停按钮、进度条、帧/时间显示）
- [x] 生成 UI 代码（designer.cs）
- [x] 创建 `UIBase` 基类并继承
- [x] 实现 `ReplayUIView.Update()` 方法（由 `UIManager` 驱动）
- [x] 实现 `UpdateUI()` 方法（更新播放/暂停按钮、进度条、帧/时间显示）
- [x] 实现帧数显示格式化（`"1234 / 5000"`）
- [x] 实现时间显示格式化（`"00:20 / 01:23"`，相对时间从0开始）
- [x] 实现 `OnPlayButtonClicked()`、`OnPauseButtonClicked()` 方法
- [x] 实现 `OnSliderValueChanged()`、`OnSliderDragEnd()` 方法（EventTrigger 支持）
- [x] 实现 `RefreshReplayGameMode()` 方法（从 GameDirector 获取）

**相关文档**: 
- `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md` 第 6.2 节
- `Frame-Sync-Replay-Refactor-Design 回放系统重构设计.md` 第 4.2 节  
**相关代码**: 
- `AstrumProj/Assets/Script/AstrumClient/UI/Generated/ReplayUIView.cs`
- `AstrumProj/Assets/Script/AstrumClient/UI/Generated/ReplayUIView.designer.cs`
- `AstrumProj/Assets/Script/AstrumClient/UI/Core/UIBase.cs`

---

## 技术要点

### 关键实现细节

1. **快照理解**：快照保存的是该帧输入运算**前**的状态，加载后需要运行该帧输入才能得到运算后状态
2. **时间管理**：回放使用**相对时间**（从0开始），`_replayElapsedTime` 通过 `deltaTime` 递增，不依赖 `TimeInfo` 或绝对时间戳
3. **跳转优化**：使用最近快照 + 快速推进策略，支持关闭中间帧渲染（暂不支持回退）
4. **文件格式**：使用 MemoryPack 序列化，GZip 压缩，数据结构在 `AstrumLogic` 中共享
5. **录制策略**：服务器每5秒保存一次回放文件，战斗结束时执行最终保存
6. **UI更新机制**：`UIManager.Update()` 统一驱动所有 UI 的 `Update()`，`ReplayUIView` 从 `GameDirector` 获取 `ReplayGameMode`

### 依赖关系

- **服务器端**：依赖 `FrameSyncManager`、`StateSnapshotManager`、`AstrumLogic`
- **客户端**：依赖 `ILSControllerBase`、`Room`、`World`、`FrameBuffer`
- **UI**：依赖现有 UI 系统（UIRefs、UI Generator）

---

## 相关文档

- **技术设计**: `Frame-Sync-Replay-Design 帧同步回放GameMode设计.md`
- **重构设计**: `Frame-Sync-Replay-Refactor-Design 回放系统重构设计.md` ⭐ **新增**
- **上游设计**: `Frame-Sync-Mechanism 帧同步机制.md`
- **上游设计**: `Frame-Sync-State-Sync-Design 帧同步状态同步与恢复机制设计.md`
- **相关重构**: `LSController-Refactor-Design LSController重构设计.md`

---

*文档版本：v0.2.0*  
*创建时间：2025-01-27*  
*最后更新：2025-01-27*  
*状态：UI完善与优化阶段*

