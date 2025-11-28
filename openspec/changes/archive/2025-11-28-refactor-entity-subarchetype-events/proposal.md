# Change: 重构 Entity 子原型事件系统

## Why

当前系统在 Entity 添加子原型时，通过新增组件事件（`EntityComponentChangedEvent`）通知 EntityView。这种方式存在以下问题：

1. **语义不匹配**：子原型的添加/移除是一个更高层次的抽象，不应该通过组件级别的变化事件来表达
2. **EntityView 缺少子原型支持**：EntityView 目前只支持基于主原型的 ViewComponents 装配，无法响应子原型的动态变化
3. **事件处理不完整**：`OnEntityComponentChanged` 方法中只有空的 switch case，没有实际处理逻辑

需要将事件系统从"组件变化"提升到"子原型变化"的层次，并让 EntityView 支持子原型的动态装配和卸载。

## What Changes

- **BREAKING**: 新增 `EntitySubArchetypeChangedEventData` 事件，完全替代通过组件事件通知子原型变化的方式
- **BREAKING**: Entity 的 `AttachSubArchetype` 和 `DetachSubArchetype` 方法改为发布子原型变化事件，不再发布组件变化事件（破坏性变更，不保留向后兼容）
- **ADDED**: EntityView 支持子原型机制，可以动态添加/移除子原型对应的 ViewComponents
- **MODIFIED**: Stage 移除 `OnEntityComponentChanged` 中对子原型的处理，新增 `OnEntitySubArchetypeChanged` 处理子原型变化事件
- **ADDED**: ViewArchetypeManager 支持查询子原型对应的 ViewComponents
- **ADDED**: EntityView 维护活跃子原型列表，支持子原型的挂载和卸载

## Impact

- 影响的规范：`entity-view` 功能
- 影响的代码：
  - `AstrumProj/Assets/Script/AstrumLogic/Core/Entity.cs` - 修改事件发布逻辑
  - `AstrumProj/Assets/Script/AstrumLogic/Events/EntityEvents.cs` - 新增子原型变化事件
  - `AstrumProj/Assets/Script/AstrumView/Core/EntityView.cs` - 添加子原型支持
  - `AstrumProj/Assets/Script/AstrumView/Core/Stage.cs` - 修改事件处理
  - `AstrumProj/Assets/Script/AstrumView/Archetypes/ViewArchetypeManager.cs` - 添加子原型查询支持
  - `AstrumProj/Assets/Script/AstrumView/Core/EntityViewFactory.cs` - 可能需要调整装配逻辑

