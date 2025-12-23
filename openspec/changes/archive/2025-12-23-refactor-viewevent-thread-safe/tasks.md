## 1. 基础结构改造

- [x] 1.1 修改 `Entity.ViewEventQueue.cs`
  - [x] 1.1.1 将 `_viewEventQueue` 字段类型从 `Queue<ViewEvent>` 改为 `ConcurrentQueue<ViewEvent>`
  - [x] 1.1.2 实现线程安全的延迟初始化（使用 `Interlocked.CompareExchange`）
  - [x] 1.1.3 更新 `ViewEventQueue` 属性返回类型
  - [x] 1.1.4 更新 `HasPendingViewEvents` 使用 `IsEmpty` 属性
  - [x] 1.1.5 更新 `ClearViewEventQueue()` 使用 `TryDequeue` 循环清空
  - [x] 1.1.6 添加代码注释说明线程安全性和使用场景

- [x] 1.2 修改 `GlobalEventQueue.cs`
  - [x] 1.2.1 将 `_globalEvents` 字段类型从 `Queue<EntityEvent>` 改为 `ConcurrentQueue<EntityEvent>`
  - [x] 1.2.2 更新 `QueueGlobalEvent()` 方法（已使用 `Enqueue`，无需修改）
  - [x] 1.2.3 更新 `GetEvents()` 方法返回类型
  - [x] 1.2.4 更新 `ClearAll()` 使用 `TryDequeue` 循环清空
  - [x] 1.2.5 更新 `HasPendingEvents` 使用 `IsEmpty` 属性
  - [x] 1.2.6 添加代码注释说明线程安全性

- [x] 1.3 修改 `Stage.ProcessViewEvents()`
  - [x] 1.3.1 更新队列类型声明为 `ConcurrentQueue<ViewEvent>`
  - [x] 1.3.2 使用 `TryDequeue(out var evt)` 替代 `Dequeue()`
  - [x] 1.3.3 移除 `eventQueue.Count > 0` 的二次检查（`TryDequeue` 自然处理空队列）
  - [x] 1.3.4 添加事件处理数量限制（可选，防止单帧卡顿）

- [x] 1.4 检查其他引用点
  - [x] 1.4.1 搜索所有使用 `ViewEventQueue` 的代码
  - [x] 1.4.2 确保没有直接访问 `.Count` 或其他 `Queue<T>` 特有方法
  - [x] 1.4.3 更新为使用 `ConcurrentQueue<T>` 的 API（`IsEmpty`, `TryDequeue` 等）
  - [x] 1.4.4 修复 `CapabilitySystem.cs` 中的 `GlobalEventQueue` 使用

## 2. 编译验证

- [x] 2.1 编译 AstrumLogic 项目
  - [x] 2.1.1 执行 `dotnet build AstrumLogic.csproj -v q`
  - [x] 2.1.2 修复所有编译错误（类型不匹配、方法调用等）

- [x] 2.2 编译 AstrumView 项目
  - [x] 2.2.1 执行 `dotnet build AstrumView.csproj -v q`
  - [x] 2.2.2 修复所有编译错误

- [x] 2.3 编译完整解决方案
  - [x] 2.3.1 执行 `dotnet build AstrumProj.sln -v q`
  - [x] 2.3.2 确保无警告和错误

- [ ] 2.4 在 Unity 中执行 `Assets/Refresh` 刷新资源

## 3. 运行时验证

- [ ] 3.1 单线程模式测试（当前环境）
  - [ ] 3.1.1 运行游戏，测试基本功能（移动、攻击、技能）
  - [ ] 3.1.2 验证特效播放正常（VFXViewComponent 的事件）
  - [ ] 3.1.3 验证实体创建/销毁事件处理正常
  - [ ] 3.1.4 检查 Console 无异常或错误日志

- [ ] 3.2 事件时序验证
  - [ ] 3.2.1 测试连续触发多个事件（如快速攻击）
  - [ ] 3.2.2 验证事件按顺序处理（先入队的先播放）
  - [ ] 3.2.3 验证无事件丢失（所有特效都正常播放）

- [ ] 3.3 性能测试
  - [ ] 3.3.1 使用 Unity Profiler 测量 `Stage.ProcessViewEvents()` 耗时
  - [ ] 3.3.2 对比改造前后的性能数据
  - [ ] 3.3.3 测试大量实体场景（100+ 实体同时产生事件）
  - [ ] 3.3.4 确认无明显性能回退

- [ ] 3.4 边界情况测试
  - [ ] 3.4.1 测试实体快速创建/销毁（事件队列生命周期）
  - [ ] 3.4.2 测试队列为空时的行为（无多余日志或警告）
  - [ ] 3.4.3 测试大量事件积累后的处理（如暂停后恢复）

## 4. 文档和清理

- [ ] 4.1 更新代码注释
  - [ ] 4.1.1 在 `Entity.ViewEventQueue.cs` 添加线程安全说明
  - [ ] 4.1.2 在 `GlobalEventQueue.cs` 添加线程安全说明
  - [ ] 4.1.3 在 `Stage.ProcessViewEvents()` 添加并发处理说明

- [ ] 4.2 更新相关变更提案
  - [ ] 4.2.1 标记 `refactor-ecc-viewread-snapshots` 的相关任务为完成
  - [ ] 4.2.2 记录 ViewEvent 线程安全改造完成

- [ ] 4.3 清理调试代码
  - [ ] 4.3.1 移除添加的临时调试日志（如有）
  - [ ] 4.3.2 确保代码风格一致

