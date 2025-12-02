# Implementation Tasks

## Phase 1: 基础数据结构和配置（2-3小时）

- [ ] 1.1 创建 `ViewEvents.cs` 文件
  - [ ] 1.1.1 定义 `ViewEventType` 枚举（EntityCreated, EntityDestroyed, SubArchetypeChanged, ComponentDirty, WorldRollback）
  - [ ] 1.1.2 定义 `ViewEvent` 结构体（EventType, EventData, TargetEntityId, Frame）
  - [ ] 1.1.3 添加完整的 XML 文档注释
  
- [ ] 1.2 添加配置开关
  - [ ] 1.2.1 在项目配置中添加 `USE_VIEW_EVENT_QUEUE` 开关（默认 false）
  - [ ] 1.2.2 添加条件编译支持（用于性能测试）

- [ ] 1.3 编译验证
  - [ ] 1.3.1 确保新文件编译通过
  - [ ] 1.3.2 确保没有引入新的依赖

## Phase 2: Entity.ViewEventQueue（1-2小时）

- [ ] 2.1 创建 `Entity.ViewEventQueue.cs` 文件（partial class）
  - [ ] 2.1.1 添加 `static bool HasViewLayer` 静态标记（默认 false）
  - [ ] 2.1.2 添加 `Queue<ViewEvent> _viewEventQueue` 字段（延迟创建）
  - [ ] 2.1.3 添加 `[MemoryPackIgnore]` 特性（事件不序列化）
  - [ ] 2.1.4 添加 `bool HasPendingViewEvents` 属性
  - [ ] 2.1.5 实现 `void QueueViewEvent(ViewEvent evt)` 方法：
    - [ ] 检查 `HasViewLayer`，服务器端直接 return
    - [ ] 延迟创建队列
    - [ ] 入队事件
  - [ ] 2.1.6 实现 `internal Queue<ViewEvent> ViewEventQueue` 属性（供 Stage 访问）
  - [ ] 2.1.7 实现 `internal void ClearViewEventQueue()` 方法

- [ ] 2.2 修改 `EntityView.cs`
  - [ ] 2.2.1 实现 `void ProcessEvent(ViewEvent evt)` 方法（分发到具体处理方法）
  - [ ] 2.2.2 实现具体处理方法：
    - [ ] `ProcessEntityCreatedEvent()`
    - [ ] `ProcessEntityDestroyedEvent()`
    - [ ] `ProcessSubArchetypeChangedEvent()`
    - [ ] `ProcessComponentDirtyEvent()`
    - [ ] `ProcessWorldRollbackEvent()`

- [ ] 2.3 编译验证
  - [ ] 2.3.1 确保编译通过
  - [ ] 2.3.2 确保 Entity 和 EntityView 现有功能不受影响

## Phase 3: Stage 事件处理（2-3小时）

- [ ] 3.1 修改 `Stage.cs` - 添加事件队列支持
  - [ ] 3.1.1 在 `Initialize()` 中设置 `Entity.HasViewLayer = true`
  - [ ] 3.1.2 实现 `void ProcessViewEvents()` 私有方法：
    - [ ] 遍历 `_room.MainWorld.Entities` 所有实体
    - [ ] 检查 `entity.HasPendingViewEvents`
    - [ ] 获取或创建对应的 EntityView
    - [ ] 处理 `entity.ViewEventQueue` 中的所有事件
  - [ ] 3.1.3 在 `Update()` 中调用 `ProcessViewEvents()`（基于配置开关）
  - [ ] 3.1.4 处理 EntityCreated 事件时创建 EntityView
  - [ ] 3.1.5 添加错误处理（EntityView 不存在且第一个事件不是 Created）
  - [ ] 3.1.6 在 `Destroy()` 中重置 `Entity.HasViewLayer = false`（可选）

- [ ] 3.2 添加调试支持
  - [ ] 3.2.1 添加日志记录（事件处理数量、Entity ID）
  - [ ] 3.2.2 添加性能监控（事件处理耗时统计）
  - [ ] 3.2.3 添加警告日志（异常情况）

- [ ] 3.3 编译验证
  - [ ] 3.3.1 确保编译通过
  - [ ] 3.3.2 确保双模式切换正常

## Phase 4: World/Entity 事件发布迁移（2-3小时）

- [ ] 4.1 修改 `World.cs`
  - [ ] 4.1.1 修改 `CreateEntity()` 方法：
    - [ ] 保留 EventSystem.Publish（配置开关关闭时使用）
    - [ ] 添加 `entity.QueueViewEvent()`（配置开关开启时使用）
  - [ ] 4.1.2 修改 `DestroyEntity()` 方法（同上）
  - [ ] 4.1.3 修改 `OnWorldRollback()` 相关代码（同上）

- [ ] 4.2 修改 `Entity.cs`
  - [ ] 4.2.1 修改 `AttachSubArchetype()` 调用 `QueueViewEvent()`
  - [ ] 4.2.2 修改 `DetachSubArchetype()` 调用 `QueueViewEvent()`
  - [ ] 4.2.3 保留 EventSystem 调用作为 fallback（配置开关控制）

- [ ] 4.3 编译验证
  - [ ] 4.3.1 确保编译通过
  - [ ] 4.3.2 确保逻辑层不需要引用 Stage（事件存储在 Entity 上）

## Phase 5: 测试（3-4小时）

- [ ] 5.1 单元测试
  - [ ] 5.1.1 测试 ViewEvent 结构体的创建
  - [ ] 5.1.2 测试 Entity.ViewEventQueue 的入队/出队
  - [ ] 5.1.3 测试 Entity.HasViewLayer 标记（服务器端拒绝入队）
  - [ ] 5.1.4 测试 EntityView.ProcessEvent() 方法
  - [ ] 5.1.5 测试 Stage.ProcessViewEvents() 轮询逻辑

- [ ] 5.2 集成测试
  - [ ] 5.2.1 测试完整的 Entity 创建流程（World → Stage → EntityView）
  - [ ] 5.2.2 测试完整的 Entity 销毁流程
  - [ ] 5.2.3 测试子原型变化流程
  - [ ] 5.2.4 测试 World 回滚流程
  - [ ] 5.2.5 测试大量实体场景（100+）

- [ ] 5.3 性能测试
  - [ ] 5.3.1 对比同步模式和队列模式的帧耗时
  - [ ] 5.3.2 测试大量实体创建的性能（1000+）
  - [ ] 5.3.3 测试事件队列的内存占用
  - [ ] 5.3.4 确保性能没有退化

- [ ] 5.4 兼容性测试
  - [ ] 5.4.1 测试配置开关切换（同步 ↔ 队列）
  - [ ] 5.4.2 测试现有功能不受影响（UI、音效、特效）
  - [ ] 5.4.3 测试多 Stage 场景

## Phase 6: 文档和清理（1-2小时）

- [ ] 6.1 更新文档
  - [ ] 6.1.1 更新 EntityView 文档，说明事件队列机制
  - [ ] 6.1.2 更新 Stage 文档，说明事件处理流程
  - [ ] 6.1.3 更新开发指南，说明如何使用事件队列
  - [ ] 6.1.4 添加迁移指南（从同步模式到队列模式）

- [ ] 6.2 代码清理
  - [ ] 6.2.1 移除调试日志（或改为 DEBUG 级别）
  - [ ] 6.2.2 统一代码风格
  - [ ] 6.2.3 添加必要的注释

- [ ] 6.3 配置默认值
  - [ ] 6.3.1 将 `USE_VIEW_EVENT_QUEUE` 改为 true（经过充分测试后）
  - [ ] 6.3.2 考虑是否保留配置开关（用于调试）

## Phase 7: 审查和部署（1小时）

- [ ] 7.1 代码审查
  - [ ] 7.1.1 提交 Pull Request
  - [ ] 7.1.2 通过代码审查
  - [ ] 7.1.3 处理审查意见

- [ ] 7.2 部署验证
  - [ ] 7.2.1 在开发环境验证
  - [ ] 7.2.2 在测试环境验证
  - [ ] 7.2.3 确认所有测试通过

- [ ] 7.3 监控
  - [ ] 7.3.1 监控性能指标（帧率、内存）
  - [ ] 7.3.2 监控错误日志
  - [ ] 7.3.3 收集反馈

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

