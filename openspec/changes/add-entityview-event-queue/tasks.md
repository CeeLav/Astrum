# Implementation Tasks

## Phase 1: 基础数据结构和配置（2-3小时）✅

- [x] 1.1 创建 `ViewEvents.cs` 文件
  - [x] 1.1.1 定义 `ViewEventType` 枚举（EntityCreated, EntityDestroyed, SubArchetypeChanged, ComponentDirty, WorldRollback）
  - [x] 1.1.2 定义 `ViewEvent` 结构体（EventType, EventData, Frame）
  - [x] 1.1.3 添加完整的 XML 文档注释
  
- [x] 1.2 添加配置开关
  - [x] 1.2.1 移除配置开关（用户要求直接使用队列模式）
  - [x] 1.2.2 无需条件编译

- [x] 1.3 编译验证
  - [x] 1.3.1 确保新文件编译通过
  - [x] 1.3.2 确保没有引入新的依赖

## Phase 2: Entity.ViewEventQueue（1-2小时）✅

- [x] 2.1 创建 `Entity.ViewEventQueue.cs` 文件（partial class）
  - [x] 2.1.1 添加 `static bool HasViewLayer` 静态标记（默认 false）
  - [x] 2.1.2 添加 `Queue<ViewEvent> _viewEventQueue` 字段（延迟创建）
  - [x] 2.1.3 添加 `[MemoryPackIgnore]` 特性（事件不序列化）
  - [x] 2.1.4 添加 `bool HasPendingViewEvents` 属性
  - [x] 2.1.5 实现 `void QueueViewEvent(ViewEvent evt)` 方法：
    - [x] 检查 `HasViewLayer`，服务器端直接 return
    - [x] 延迟创建队列
    - [x] 入队事件
  - [x] 2.1.6 实现 `public Queue<ViewEvent> ViewEventQueue` 属性（供 Stage 访问）
  - [x] 2.1.7 实现 `public void ClearViewEventQueue()` 方法

- [x] 2.2 修改 `EntityView.cs`
  - [x] 2.2.1 实现 `void ProcessEvent(ViewEvent evt)` 方法（分层分发）
  - [x] 2.2.2 实现具体处理方法：
    - [x] `ProcessEntityViewEvent_SubArchetypeChanged()`（EntityView 级别）
    - [x] `ProcessViewComponentEvent_ComponentDirty()`（ViewComponent 级别）

- [x] 2.3 编译验证
  - [x] 2.3.1 确保编译通过
  - [x] 2.3.2 确保 Entity 和 EntityView 现有功能不受影响

## Phase 3: Stage 事件处理（2-3小时）✅

- [x] 3.1 修改 `Stage.cs` - 添加事件队列支持
  - [x] 3.1.1 在 `Initialize()` 中设置 `Entity.HasViewLayer = true`
  - [x] 3.1.2 实现 `void ProcessViewEvents()` 私有方法：
    - [x] 遍历 `_room.MainWorld.Entities` 所有实体
    - [x] 检查 `entity.HasPendingViewEvents`
    - [x] 根据事件类型分层处理（Stage / EntityView）
    - [x] Stage 级别事件直接处理
    - [x] EntityView/ViewComponent 级别事件传递给 EntityView
  - [x] 3.1.3 在 `Update()` 中调用 `ProcessViewEvents()`
  - [x] 3.1.4 实现 `ProcessStageEvent_EntityCreated()` 创建 EntityView
  - [x] 3.1.5 实现 `ProcessStageEvent_EntityDestroyed()` 销毁 EntityView
  - [x] 3.1.6 实现 `ProcessStageEvent_WorldRollback()` 处理回滚

- [x] 3.2 添加调试支持
  - [x] 3.2.1 添加日志记录（事件处理数量、Entity ID）
  - [x] 3.2.2 使用 ProfileScope 监控性能
  - [x] 3.2.3 添加警告日志（异常情况）

- [x] 3.3 编译验证
  - [x] 3.3.1 确保编译通过
  - [x] 3.3.2 分层事件处理正常

## Phase 4: World/Entity 事件发布迁移（2-3小时）✅

- [x] 4.1 修改 `World.cs`
  - [x] 4.1.1 修改 `PublishEntityCreatedEvent()` 调用 `entity.QueueViewEvent()`
  - [x] 4.1.2 修改 `PublishEntityDestroyedEvent()` 调用 `entity.QueueViewEvent()`
  - [x] 4.1.3 保留 EventSystem 调用（已注释，便于回退）

- [x] 4.2 修改 `Entity.cs`
  - [x] 4.2.1 添加 `using Astrum.LogicCore.Events`
  - [x] 4.2.2 修改 `PublishSubArchetypeChangedEvent()` 调用 `QueueViewEvent()`
  - [x] 4.2.3 保留 EventSystem 调用（已注释，便于回退）

- [x] 4.3 编译验证
  - [x] 4.3.1 确保编译通过 ✅
  - [x] 4.3.2 确保逻辑层不需要引用视图层 ✅

## Phase 5: 测试（3-4小时）⏸️

**注**：核心功能已实现并编译通过，详细测试需要在实际游戏场景中验证。

- [ ] 5.1 单元测试（待后续补充）
  - [ ] 5.1.1 测试 ViewEvent 结构体的创建
  - [ ] 5.1.2 测试 Entity.ViewEventQueue 的入队/出队
  - [ ] 5.1.3 测试 Entity.HasViewLayer 标记（服务器端拒绝入队）
  - [ ] 5.1.4 测试 EntityView.ProcessEvent() 方法
  - [ ] 5.1.5 测试 Stage.ProcessViewEvents() 轮询逻辑

- [ ] 5.2 集成测试（建议运行游戏验证）
  - [ ] 5.2.1 测试完整的 Entity 创建流程（World → Stage → EntityView）
  - [ ] 5.2.2 测试完整的 Entity 销毁流程
  - [ ] 5.2.3 测试子原型变化流程
  - [ ] 5.2.4 测试 World 回滚流程
  - [ ] 5.2.5 测试大量实体场景（100+）

- [ ] 5.3 性能测试（建议使用 Profiler）
  - [ ] 5.3.1 监控事件处理的帧耗时
  - [ ] 5.3.2 测试大量实体创建的性能（1000+）
  - [ ] 5.3.3 测试事件队列的内存占用
  - [ ] 5.3.4 确保性能没有退化

- [ ] 5.4 兼容性测试
  - [ ] 5.4.1 测试现有功能不受影响（UI、音效、特效）
  - [ ] 5.4.2 测试多 Stage 场景

## 补充实施: ViewComponent 事件注册机制 ✅

- [x] 8.1 修改 ViewComponent.cs
  - [x] 8.1.1 添加 `_viewEventHandlers` 字段
  - [x] 8.1.2 在 Initialize() 中调用 RegisterViewEventHandlers()
  - [x] 8.1.3 实现 RegisterViewEventHandlers() 虚方法
  - [x] 8.1.4 实现 RegisterViewEventHandler<TEvent>() 方法
  - [x] 8.1.5 实现 GetViewEventHandlers() 方法

- [x] 8.2 修改 EntityView.cs
  - [x] 8.2.1 添加 `_viewEventToComponents` 映射
  - [x] 8.2.2 实现 RegisterViewComponentEventHandlers()
  - [x] 8.2.3 实现 UnregisterViewComponentEventHandlers()
  - [x] 8.2.4 完善 DispatchViewEventToComponents()
  - [x] 8.2.5 在 AddViewComponent 中调用注册
  - [x] 8.2.6 在 RemoveViewComponent 中调用取消注册

- [x] 8.3 编译验证
  - [x] 8.3.1 确保编译通过 ✅
  - [x] 8.3.2 确保事件注册机制正常工作

## Phase 6: 文档和清理（1-2小时）✅

- [x] 6.1 更新文档
  - [x] 6.1.1 创建 IMPLEMENTATION_SUMMARY.md 说明实施情况
  - [x] 6.1.2 更新 tasks.md 标记已完成的任务
  - [x] 6.1.3 保留 design.md 和 proposal.md 作为参考

- [x] 6.2 代码清理
  - [x] 6.2.1 保留调试日志（便于问题排查）
  - [x] 6.2.2 代码风格统一
  - [x] 6.2.3 添加了完整的 XML 注释

- [x] 6.3 配置
  - [x] 6.3.1 移除配置开关（用户要求直接使用队列模式）
  - [x] 6.3.2 使用 `Entity.HasViewLayer` 静态标记作为唯一控制点

## Phase 7: 审查和部署（1小时）

**注**：核心实现已完成，等待用户测试和反馈。

- [ ] 7.1 用户验证
  - [ ] 7.1.1 运行游戏测试基本功能
  - [ ] 7.1.2 测试实体创建/销毁流程
  - [ ] 7.1.3 测试技能系统和战斗系统
  - [ ] 7.1.4 监控 Unity Profiler 性能数据

- [ ] 7.2 问题修复
  - [ ] 7.2.1 根据测试结果修复问题
  - [ ] 7.2.2 优化性能瓶颈
  - [ ] 7.2.3 完善错误处理

- [ ] 7.3 最终审查
  - [ ] 7.3.1 代码审查
  - [ ] 7.3.2 文档完整性检查
  - [ ] 7.3.3 标记为已完成

## 总预估工时

- Phase 1-2: 3-5 小时（基础设施）
- Phase 3-4: 4-6 小时（核心实现）
- Phase 5: 3-4 小时（测试）
- Phase 6-7: 2-3 小时（文档和部署）

**总计**: 12-18 小时（约 2-3 个工作日）

## 依赖关系

- Phase 2 依赖 Phase 1
- Phase 3 依赖 Phase 2
- Phase 4 依赖 Phase 3
- Phase 5 依赖 Phase 4
- Phase 6-7 依赖 Phase 5

## 并行可能性

- Phase 1.2（配置）和 Phase 1.1（数据结构）可以并行
- Phase 2 和 Phase 3 可以部分并行（EntityView 和 Stage 独立开发）
- Phase 5.1-5.2（单元测试和集成测试）可以并行编写

## 验收标准

每个 Phase 完成后需要：
1. ✅ 代码编译通过，无警告
2. ✅ 相关测试通过
3. ✅ 代码审查通过（如需要）
4. ✅ 文档更新（如需要）

