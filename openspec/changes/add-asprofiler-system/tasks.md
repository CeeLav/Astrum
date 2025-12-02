## 1. 基础设施实现

- [x] 1.1 在 CommonBase 中创建 ASProfiler.cs 核心类
  - [x] 实现 Singleton 模式
  - [x] 实现 RegisterHandler 方法
  - [x] 实现 BeginSample/EndSample 方法（带 Conditional 特性）
  - [x] 添加 XML 注释文档

- [x] 1.2 在 CommonBase 中创建 IProfilerHandler.cs 接口
  - [x] 定义 BeginSample(string name) 方法
  - [x] 定义 EndSample() 方法
  - [x] 添加 XML 注释文档

- [x] 1.3 在 CommonBase 中创建 ProfileScope.cs 结构体
  - [x] 实现 IDisposable 接口
  - [x] 构造函数调用 BeginSample
  - [x] Dispose 方法调用 EndSample
  - [x] 添加 readonly 修饰符（避免拷贝）
  - [x] 添加 XML 注释文档

- [ ] 1.4 编写基础设施单元测试（待 Unity 刷新后）
  - [ ] 测试 ASProfiler Singleton 模式
  - [ ] 测试 Handler 注册和调用
  - [ ] 测试 ProfileScope 自动管理
  - [ ] 测试嵌套 ProfileScope

## 2. 环境适配实现

- [x] 2.1 实现 UnityProfilerHandler（Unity 客户端）
  - [x] 在 AstrumClient/Profiling/ 目录创建 UnityProfilerHandler.cs
  - [x] 实现 IProfilerHandler 接口
  - [x] BeginSample 调用 UnityEngine.Profiling.Profiler.BeginSample
  - [x] EndSample 调用 UnityEngine.Profiling.Profiler.EndSample
  - [x] 添加 XML 注释文档

- [x] 2.2 实现 ServerProfilerHandler（服务器）
  - [x] 在 AstrumServer/Profiling/ 目录创建 ServerProfilerHandler.cs
  - [x] 实现 IProfilerHandler 接口
  - [x] 使用 Stack<(string, long)> 存储嵌套关系
  - [x] BeginSample 使用 Stopwatch.GetTimestamp() 记录开始时间
  - [x] EndSample 计算耗时，超过阈值输出日志
  - [x] 添加可配置的阈值参数（默认 5ms）
  - [x] 添加 XML 注释文档

- [x] 2.3 实现 TestProfilerHandler（单元测试）
  - [x] 在 AstrumTest/Shared/ 目录创建 TestProfilerHandler.cs
  - [x] 实现 IProfilerHandler 接口
  - [x] 使用 Dictionary<string, List<double>> 存储所有样本耗时
  - [x] 提供 GetSampleTimes(string name) 查询方法
  - [x] 提供 GetAverageSampleTime(string name) 统计方法
  - [x] 提供 Clear() 清空数据方法
  - [x] 添加 XML 注释文档

- [x] 2.4 在 GameApplication 中注册 UnityProfilerHandler
  - [x] 在 Awake() 方法中创建 UnityProfilerHandler
  - [x] 调用 ASProfiler.Instance.RegisterHandler()
  - [x] 添加日志记录初始化成功

- [ ] 2.5 在服务器主程序中注册 ServerProfilerHandler（待实施）
  - [ ] 在 Main() 或启动方法中创建 ServerProfilerHandler
  - [ ] 调用 ASProfiler.Instance.RegisterHandler()
  - [ ] 配置性能阈值（可从配置文件读取）

## 3. 逻辑层监控点集成

- [x] 3.1 在 World.Update() 添加监控
  - [x] 整个方法使用 ProfileScope("World.Update")
  - [x] Updater.UpdateWorld() 使用 ProfileScope("World.UpdateWorld")
  - [x] CapabilitySystem.ProcessEntityEvents() 使用 ProfileScope("World.ProcessEntityEvents")
  - [x] HitSystem.StepPhysics() 使用 ProfileScope("World.StepPhysics")

- [x] 3.2 在 LSUpdater.UpdateWorld() 添加监控
  - [x] 整个方法使用 ProfileScope("LSUpdater.UpdateWorld")

- [ ] 3.3 在 CapabilitySystem.Update() 添加监控（待实施）
  - [ ] 整个方法使用 ProfileScope("CapabilitySystem.Update")
  - [ ] 每个 Capability 类型的更新使用 ProfileScope($"Capability.{capabilityType}")

- [ ] 3.4 在各 System.Tick() 添加监控（待实施）
  - [ ] SkillEffectSystem.Update() 使用 ProfileScope("SkillEffectSystem.Update")
  - [ ] 其他 System 按需添加

- [x] 3.5 在 Room.FrameTick() 添加监控
  - [x] 整个方法使用 ProfileScope("Room.FrameTick")
  - [x] World.Update() 循环使用 ProfileScope("Room.UpdateWorlds")
  - [x] TickSystems() 使用 ProfileScope("Room.TickSystems")

- [ ] 3.6 验证逻辑层监控开销（需要 Unity 刷新后编译通过才能验证）
  - [ ] 运行性能测试，测量添加监控前后的帧时间
  - [ ] 确保开销 < 1%（Debug 构建）
  - [ ] 确保 Release 构建无性能差异

## 4. 表现层监控点集成

- [x] 4.1 在 Stage.Update() 添加监控
  - [x] 整个方法使用 ProfileScope("Stage.Update")
  - [x] SyncDirtyComponents() 使用 ProfileScope("Stage.SyncDirtyComponents")
  - [x] EntityView.UpdateView() 循环使用 ProfileScope("Stage.UpdateEntityViews")

- [ ] 4.2 在 EntityView.UpdateView() 添加监控（待实施）
  - [ ] 整个方法使用 ProfileScope($"EntityView.Update.{GetType().Name}")
  - [ ] 或使用基类统一添加，子类自动继承

- [ ] 4.3 在动画系统添加监控（可选，按需添加）
  - [ ] 如果有独立的动画更新方法，添加 ProfileScope
  - [ ] 或在 EntityView 的动画更新部分添加

- [ ] 4.4 在 UI 系统添加监控（可选，按需添加）
  - [ ] UIManager.Update() 使用 ProfileScope("UIManager.Update")
  - [ ] 各 UIView 的 Update 方法添加监控（如需要）

- [ ] 4.5 验证表现层监控开销（需要 Unity 刷新后编译通过才能验证）
  - [ ] 运行性能测试，测量添加监控前后的帧时间
  - [ ] 确保开销 < 1%（Debug 构建）
  - [ ] 确保 Release 构建无性能差异

## 5. 条件编译配置

- [ ] 5.1 配置 Unity 项目编译符号
  - [ ] 在 Unity Player Settings 中添加 ENABLE_PROFILER 到 Debug 配置
  - [ ] 确保 Release 配置不包含 ENABLE_PROFILER
  - [ ] 验证不同配置的编译结果

- [ ] 5.2 配置服务器项目编译符号
  - [ ] 在 AstrumServer.csproj 中添加条件编译配置
  - [ ] Debug 配置定义 ENABLE_PROFILER
  - [ ] Release 配置不定义 ENABLE_PROFILER

- [ ] 5.3 验证条件编译效果
  - [ ] 使用 IL 反编译工具查看 Release 构建
  - [ ] 确认所有 ASProfiler 调用已被移除
  - [ ] 确认字符串参数计算也被移除

## 6. 测试和验证

- [ ] 6.1 编写单元测试
  - [ ] 测试 ASProfiler 基本功能
  - [ ] 测试 ProfileScope 自动管理
  - [ ] 测试嵌套监控
  - [ ] 测试异常安全性（抛异常时仍调用 EndSample）
  - [ ] 测试 TestProfilerHandler 数据收集

- [ ] 6.2 编写集成测试
  - [ ] 测试逻辑层监控点正常工作
  - [ ] 测试表现层监控点正常工作
  - [ ] 测试 Unity Profiler 显示正确数据
  - [ ] 测试服务器日志输出正确

- [ ] 6.3 性能测试
  - [ ] 测试 Debug 构建监控开销 < 1%
  - [ ] 测试 Release 构建零开销
  - [ ] 测试大量监控点场景（100+ Entity）
  - [ ] 测试嵌套监控性能影响

- [ ] 6.4 手动测试
  - [ ] 在 Unity Editor 中运行游戏，打开 Profiler 窗口
  - [ ] 验证所有监控点正确显示
  - [ ] 验证嵌套关系正确
  - [ ] 验证耗时数据准确

- [ ] 6.5 服务器测试
  - [ ] 启动服务器，触发慢操作
  - [ ] 验证日志输出性能警告
  - [ ] 验证阈值配置生效
  - [ ] 验证嵌套监控正确

## 7. 文档和代码审查

- [ ] 7.1 编写使用文档
  - [ ] 在 Docs/ 目录创建 ASProfiler 使用文档
  - [ ] 说明如何添加监控点
  - [ ] 说明如何配置编译符号
  - [ ] 说明如何查看监控数据
  - [ ] 说明性能开销和最佳实践

- [ ] 7.2 更新项目文档
  - [ ] 更新 QUICK_START.md 提及性能监控
  - [ ] 更新开发规范文档（如有）

- [ ] 7.3 代码审查
  - [ ] 检查所有代码符合命名规范
  - [ ] 检查所有公共 API 有 XML 注释
  - [ ] 检查无编译警告
  - [ ] 检查无 TODO 标记

- [ ] 7.4 性能报告
  - [ ] 记录添加监控前后的性能对比数据
  - [ ] 记录 Debug/Release 构建的性能差异
  - [ ] 记录典型场景的监控开销

## 8. 部署和清理

- [ ] 8.1 合并代码
  - [ ] 创建 PR 并提交代码审查
  - [ ] 解决审查意见
  - [ ] 合并到主分支

- [ ] 8.2 验证部署
  - [ ] 验证 CI/CD 构建通过
  - [ ] 验证所有测试通过
  - [ ] 验证 Release 构建正常

- [ ] 8.3 清理临时文件
  - [ ] 删除测试用的临时脚本（如有）
  - [ ] 清理调试日志输出

- [ ] 8.4 归档 OpenSpec 变更
  - [ ] 使用 openspec archive add-asprofiler-system 归档提案
  - [ ] 更新 specs/profiling/spec.md 规范文件
  - [ ] 验证归档后的规范正确

