## 1. 实施

### 1.1 事件系统
- [x] 1.1.1 创建 `EntitySubArchetypeChangedEventData` 事件类（`AstrumProj/Assets/Script/AstrumLogic/Events/EntityEvents.cs`）
- [x] 1.1.2 添加子原型类型（`EArchetype`）和变化类型（Attach/Detach）字段
- [x] 1.1.3 添加构造函数，接受 Entity、WorldId、RoomId、子原型类型和变化类型

### 1.2 Entity 事件发布
- [x] 1.2.1 在 Entity 中添加 `PublishSubArchetypeChangedEvent` 私有方法
- [x] 1.2.2 修改 `AttachSubArchetype` 方法，在成功添加后发布 Attach 事件
- [x] 1.2.3 修改 `DetachSubArchetype` 方法，在成功移除后发布 Detach 事件
- [x] 1.2.4 确保事件包含正确的 WorldId 和 RoomId（从 World 获取）

### 1.3 ViewArchetypeManager 扩展
- [x] 1.3.1 验证 ViewArchetypeManager 是否已支持子原型查询（通过 `TryGetComponents` 方法）
- [x] 1.3.2 如果支持，确保子原型可以正确注册 ViewComponents
- [x] 1.3.3 如果不支持，扩展 `TryGetComponents` 方法支持子原型查询

### 1.4 EntityView 子原型支持
- [x] 1.4.1 在 EntityView 中添加 `ActiveSubArchetypes` 列表（`List<EArchetype>`）
- [x] 1.4.2 添加 ViewComponents 引用计数字典（`Dictionary<Type, int>`）
- [x] 1.4.3 实现 `AttachSubArchetype` 方法：
  - 检查子原型是否已激活
  - 查询子原型对应的 ViewComponents
  - 使用引用计数装配 ViewComponents
  - 添加到活跃列表
- [x] 1.4.4 实现 `DetachSubArchetype` 方法：
  - 检查子原型是否已激活
  - 使用引用计数卸载 ViewComponents
  - 从活跃列表移除
- [x] 1.4.5 实现 `IsSubArchetypeActive` 查询方法
- [x] 1.4.6 实现 `ListActiveSubArchetypes` 查询方法

### 1.5 Stage 事件处理
- [x] 1.5.1 在 Stage 的 `SubscribeToEntityEvents` 中订阅 `EntitySubArchetypeChangedEventData`
- [x] 1.5.2 在 Stage 的 `UnsubscribeFromEntityEvents` 中取消订阅
- [x] 1.5.3 实现 `OnEntitySubArchetypeChanged` 方法：
  - 检查 RoomId 匹配
  - 获取对应的 EntityView
  - 根据变化类型调用 EntityView 的 Attach/Detach 方法
- [x] 1.5.4 完全移除 `OnEntityComponentChanged` 中对子原型的处理逻辑（子原型变化不再触发组件事件）

### 1.6 初始同步
- [x] 1.6.1 在 EntityViewFactory 的 `AssembleViewComponents` 方法中，在装配主原型 ViewComponents 后，同步 Entity 的活跃子原型
- [x] 1.6.2 确保创建时已有的子原型也能正确装配 ViewComponents

## 2. 测试

- [ ] 2.1 单元测试：Entity 发布子原型变化事件
- [ ] 2.2 单元测试：EntityView 的 AttachSubArchetype 和 DetachSubArchetype
- [ ] 2.3 单元测试：ViewComponents 的引用计数和去重
- [ ] 2.4 集成测试：完整的子原型挂载/卸载流程
- [ ] 2.5 集成测试：EntityView 初始化时同步已有子原型

## 3. 验证

- [ ] 3.1 编译检查：确保所有代码编译通过
- [ ] 3.2 运行时测试：在 Unity 中测试子原型的动态添加和移除
- [ ] 3.3 验证 ViewComponents 正确装配和卸载
- [ ] 3.4 验证事件系统正常工作

