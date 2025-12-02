# Change: 为逻辑层和表现层添加 ASProfiler 性能监控系统

## Why

当前项目缺乏统一的性能监控机制，无法有效追踪逻辑层（AstrumLogic）和表现层（AstrumView）的性能瓶颈。根据项目约定文档，游戏必须满足严格的性能预算：

- **帧率要求**: 95% 帧内总耗时 < 16ms，99% 帧内 < 22ms（60 FPS）
- **GC 要求**: 热路径在 10 秒窗口内托管堆分配接近 0
- **验证要求**: 所有性能敏感模块在合并前必须通过 Unity Profiler/分析工具验证

由于逻辑层（AstrumLogic）不依赖 Unity，无法直接使用 Unity Profiler API。需要创建一个类似 ASLogger 的跨平台性能监控系统 ASProfiler，支持：

1. **逻辑层监控**: World.Update()、各种 System.Tick()、Capability.Update() 等
2. **表现层监控**: Stage.Update()、EntityView.UpdateView()、动画系统等
3. **环境适配**: 不同环境（Unity、Server、Test）注册不同的监控实现
4. **零开销**: 在 Release 构建中可完全禁用，无性能损耗

## What Changes

- **ADDED**: 在 CommonBase 中创建 `ASProfiler` 类，提供跨平台性能监控接口
- **ADDED**: `IProfilerHandler` 接口，支持不同环境注册监控函数
- **ADDED**: Unity 环境的 `UnityProfilerHandler`，使用 Unity Profiler API
- **ADDED**: 服务器环境的 `ServerProfilerHandler`，使用 Stopwatch 记录耗时
- **ADDED**: 测试环境的 `TestProfilerHandler`，用于单元测试验证
- **ADDED**: 逻辑层关键路径的性能监控点（World.Update、各 System、Capability）
- **ADDED**: 表现层关键路径的性能监控点（Stage.Update、EntityView、动画系统）
- **ADDED**: 条件编译支持（`#if ENABLE_PROFILER`），Release 构建零开销
- **ADDED**: 性能报告生成功能，支持导出性能数据

## Impact

- 影响的规范：`profiling` 功能（新增规范）
- 影响的代码：
  - `AstrumProj/Assets/Script/CommonBase/ASProfiler.cs` - 新增核心 Profiler 类
  - `AstrumProj/Assets/Script/CommonBase/IProfilerHandler.cs` - 新增 Handler 接口
  - `AstrumProj/Assets/Script/AstrumClient/Profiling/UnityProfilerHandler.cs` - Unity 实现
  - `AstrumServer/AstrumServer/Profiling/ServerProfilerHandler.cs` - 服务器实现
  - `AstrumTest/Shared/TestProfilerHandler.cs` - 测试实现
  - `AstrumProj/Assets/Script/AstrumLogic/Core/World.cs` - 添加监控点
  - `AstrumProj/Assets/Script/AstrumLogic/Core/LSUpdater.cs` - 添加监控点
  - `AstrumProj/Assets/Script/AstrumLogic/Systems/CapabilitySystem.cs` - 添加监控点
  - `AstrumProj/Assets/Script/AstrumView/Core/Stage.cs` - 添加监控点
  - `AstrumProj/Assets/Script/AstrumView/Core/EntityView.cs` - 添加监控点
  - `AstrumProj/Assets/Script/AstrumClient/Managers/GameApplication.cs` - 初始化 Profiler
- 性能影响：
  - Debug 构建：每个监控点增加 < 0.01ms 开销（可接受）
  - Release 构建：完全编译移除，零开销
- 兼容性：完全向后兼容，不影响现有代码逻辑

