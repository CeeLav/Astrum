# Change: Add EntityView Event Queue

## Why

当前逻辑层（Entity/Capability）通过 EventSystem 同步调用视图层（Stage/EntityView），存在以下问题：

1. **耦合度高**：逻辑层直接触发视图层回调，导致逻辑层和视图层紧密耦合
2. **无法多线程化**：同步调用阻止了逻辑层在独立线程中运行的可能性
3. **性能瓶颈**：逻辑层的操作会立即触发视图层更新，增加了帧耗时
4. **不确定性**：回调执行时机不可控，可能在逻辑层执行过程中触发

**目标**：仿照 Entity.EventQueue，为 EntityView 添加异步事件队列机制，使逻辑层和视图层彻底解耦，为未来的逻辑层多线程化做准备。

## What Changes

### 1. 新增 ViewEvent 事件队列系统
- 添加 `ViewEvent` 结构体，封装视图层事件数据
- 添加 `ViewEventType` 枚举，定义视图事件类型（Created, Destroyed, ComponentChanged 等）
- 在 `Entity` 中添加 `ViewEventQueue` 队列，存储待处理的视图事件（与 Entity.EventQueue 并列）
- 添加 `Entity.HasViewLayer` 静态标记，服务器端拒绝入队避免内存泄漏

### 2. 修改 Stage 事件处理机制
- Stage 不再通过 EventSystem 同步订阅事件
- Stage 在 Update() 中轮询所有 Entity 的 ViewEventQueue
- 获取或创建对应的 EntityView，并分发事件处理
- 批量处理事件，提升性能

### 3. 修改 World/Entity 事件发布机制
- World 创建/销毁 Entity 时，调用 `entity.QueueViewEvent()` 将事件入队
- Entity 组件变化时，将事件入队而非同步发布
- 无需额外缓存，Entity 总是先于 EntityView 创建

### 4. 保持兼容性
- EventSystem 保持不变，用于其他系统级事件（UI、音效、特效等）
- 仅实体相关的视图事件使用队列机制
- 提供配置选项，允许在调试时切换回同步模式

## Impact

- **影响的规范**：
  - `entity-view` - EntityView 的事件处理机制
  
- **影响的代码**：
  - `AstrumProj/Assets/Script/AstrumView/Events/ViewEvents.cs` - 新增视图事件定义（NEW）
  - `AstrumProj/Assets/Script/AstrumLogic/Core/Entity.ViewEventQueue.cs` - Entity 添加 ViewEventQueue（NEW）
  - `AstrumProj/Assets/Script/AstrumView/Core/EntityView.cs` - 添加事件处理方法
  - `AstrumProj/Assets/Script/AstrumView/Core/Stage.cs` - 修改事件处理逻辑（轮询 Entity）
  - `AstrumProj/Assets/Script/AstrumLogic/Core/World.cs` - 修改事件发布方式
  - `AstrumProj/Assets/Script/AstrumLogic/Core/Entity.cs` - 子原型变化事件入队

- **破坏性变更**：
  - ❌ 无破坏性变更（向后兼容，EventSystem 保持不变）
  
- **性能影响**：
  - ✅ 正面影响：批量处理事件，减少逻辑层阻塞
  - ✅ 正面影响：为多线程化铺路

- **测试需求**：
  - 单元测试：事件队列的入队/出队逻辑
  - 集成测试：Entity 创建/销毁/组件变化的事件流程
  - 性能测试：大量实体场景下的事件处理性能

