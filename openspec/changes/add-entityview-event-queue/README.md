# Add EntityView Event Queue

**Change ID**: `add-entityview-event-queue`  
**Status**: 📝 提案中  
**预估工时**: 12-18 小时（2-3 个工作日）

## 快速概览

为 EntityView 添加类似 Entity.EventQueue 的异步事件队列机制，实现逻辑层和视图层的彻底解耦，为未来的逻辑层多线程化做准备。

### 核心改动

1. **新增 ViewEvent 事件队列系统**
   - `ViewEvent` 结构体 + `ViewEventType` 枚举
   - Entity 维护视图事件队列（与 Entity.EventQueue 并列）
   - `Entity.HasViewLayer` 静态标记，服务器端拒绝入队

2. **修改 Stage 事件处理机制**
   - 从同步订阅改为批量处理队列
   - 在 Update() 中统一处理所有待处理事件

3. **修改 World/Entity 事件发布**
   - 从 EventSystem.Publish() 改为 Stage.QueueViewEvent()
   - 保持向后兼容（配置开关）

### 架构变化

**之前（同步）**：
```
World.CreateEntity()
  ↓
EventSystem.Publish(EntityCreatedEventData)
  ↓ (立即回调)
Stage.OnEntityCreated()
  ↓
CreateEntityView()
```

**之后（异步）**：
```
World.CreateEntity()
  ↓
entity.QueueViewEvent(ViewEvent)
  ↓ (入队到 Entity，不阻塞)
[等待下一帧]
  ↓
Stage.Update() → ProcessViewEvents()
  ↓ (遍历所有 Entity，检查 HasPendingViewEvents)
获取或创建 EntityView
  ↓
EntityView.ProcessEvent(entity.ViewEventQueue)
```

## 文件导航

- **[proposal.md](./proposal.md)** - 为什么需要这个变更以及影响范围
- **[design.md](./design.md)** - 技术设计决策和架构方案
- **[tasks.md](./tasks.md)** - 详细的实施任务列表
- **[specs/entity-view/spec.md](./specs/entity-view/spec.md)** - 规范变更（ADDED + MODIFIED）

## 关键决策

1. **事件队列设计**：放在 Entity vs EntityView vs Stage
   - ✅ 选择：放在 Entity 上（与 Entity.EventQueue 并列）
   - 理由：Entity 先于 EntityView 创建，天然解决事件缓存问题

2. **事件路由机制**：Stage 轮询 vs 逻辑层推送
   - ✅ 选择：Stage 轮询所有 Entity
   - 理由：与 CapabilitySystem 轮询 Entity.EventQueue 保持一致

3. **服务器端防护**：条件编译 vs 静态标记
   - ✅ 选择：`Entity.HasViewLayer` 静态标记
   - 理由：简单可靠，零开销，防止服务器端内存泄漏

4. **向后兼容**：保留同步模式 vs 完全切换
   - ✅ 选择：保留配置开关 `USE_VIEW_EVENT_QUEUE`
   - 理由：便于调试和回滚，逐步迁移

## 影响范围

### 新增文件
- `AstrumProj/Assets/Script/AstrumView/Events/ViewEvents.cs` - ViewEvent 和 ViewEventType 定义
- `AstrumProj/Assets/Script/AstrumLogic/Core/Entity.ViewEventQueue.cs` - Entity 的 ViewEventQueue 扩展

### 修改文件
- `AstrumProj/Assets/Script/AstrumView/Core/EntityView.cs` - 添加事件处理方法
- `AstrumProj/Assets/Script/AstrumView/Core/Stage.cs` - 轮询 Entity 并处理视图事件
- `AstrumProj/Assets/Script/AstrumLogic/Core/World.cs` - 调用 entity.QueueViewEvent()
- `AstrumProj/Assets/Script/AstrumLogic/Core/Entity.cs` - 子原型变化时入队事件

### 影响的规范
- `entity-view` - EntityView 的事件处理机制

## 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 事件延迟 | 视图层滞后 1 帧 | 可接受，特效本身有延迟 |
| 事件顺序 | 顺序错误导致状态不一致 | 保证 FIFO，与现有顺序一致 |
| 调试难度 | 异步处理增加调试难度 | 详细日志 + 同步模式开关 |
| 内存开销 | 每个 EntityView 额外 ~80 bytes | 可接受，换取性能提升 |

## 测试策略

1. **单元测试**：事件队列的入队/出队逻辑
2. **集成测试**：完整的实体生命周期流程
3. **性能测试**：大量实体场景（1000+）
4. **兼容性测试**：同步模式 ↔ 队列模式切换

## 迁移计划

### Phase 1: 基础设施（不影响现有功能）
- 添加事件队列数据结构
- 添加配置开关（默认 false）

### Phase 2-3: 实现队列机制
- EntityView 事件队列
- Stage 事件分发

### Phase 4: 迁移事件发布点
- World/Entity 使用队列

### Phase 5-7: 测试、文档、部署
- 全面测试验证
- 切换默认值为 true

## 下一步

1. 阅读并审查 `proposal.md` 和 `design.md`
2. 确认技术方案和架构决策
3. 如有疑问或建议，提出讨论
4. 获得批准后，按照 `tasks.md` 开始实施

## 参考资料

- [Entity.EventQueue 实现](../../AstrumProj/Assets/Script/AstrumLogic/Core/Entity.EventQueue.cs)
- [ViewComponent 数据同步设计](../../Docs/05-CoreArchitecture%20核心架构/逻辑渲染分离/ViewComponent-DataSync-Design%20ViewComponent数据同步设计.md)
- [事件队列系统设计](../../Docs/05-CoreArchitecture%20核心架构/事件/Event-Queue-System%20事件队列系统.md)

