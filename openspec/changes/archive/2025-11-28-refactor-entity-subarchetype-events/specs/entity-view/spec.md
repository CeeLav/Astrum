## ADDED Requirements

### Requirement: 子原型变化事件
系统 SHALL 提供 `EntitySubArchetypeChangedEventData` 事件，用于通知 EntityView 子原型的添加或移除。

#### Scenario: Entity 添加子原型时发布事件
- **WHEN** Entity 成功调用 `AttachSubArchetype` 添加子原型
- **THEN** 系统发布 `EntitySubArchetypeChangedEventData` 事件，包含子原型类型和 "Attach" 变化类型
- **AND** 事件包含正确的 EntityId、WorldId 和 RoomId

#### Scenario: Entity 移除子原型时发布事件
- **WHEN** Entity 成功调用 `DetachSubArchetype` 移除子原型
- **THEN** 系统发布 `EntitySubArchetypeChangedEventData` 事件，包含子原型类型和 "Detach" 变化类型
- **AND** 事件包含正确的 EntityId、WorldId 和 RoomId

### Requirement: EntityView 子原型支持
EntityView SHALL 支持子原型的动态挂载和卸载，维护活跃子原型列表并装配对应的 ViewComponents。

#### Scenario: EntityView 挂载子原型
- **WHEN** EntityView 收到子原型 Attach 事件
- **THEN** EntityView 查询子原型对应的 ViewComponents
- **AND** EntityView 使用引用计数装配 ViewComponents（避免重复装配）
- **AND** EntityView 将子原型添加到活跃列表
- **AND** ViewComponents 的装配顺序无关（无依赖关系）

#### Scenario: EntityView 卸载子原型
- **WHEN** EntityView 收到子原型 Detach 事件
- **THEN** EntityView 使用引用计数卸载 ViewComponents（引用计数归零时移除）
- **AND** EntityView 从活跃列表移除子原型
- **AND** ViewComponents 按照正常的卸载流程处理，子原型层不过多干预

#### Scenario: EntityView 查询子原型状态
- **WHEN** 查询 EntityView 是否激活某个子原型
- **THEN** EntityView 返回子原型是否在活跃列表中

#### Scenario: EntityView 初始化时同步子原型
- **WHEN** EntityView 初始化时
- **THEN** EntityView 检查 Entity 的活跃子原型列表
- **AND** EntityView 自动装配所有已激活子原型对应的 ViewComponents
- **AND** 子原型的装配顺序无关（无加载顺序要求）

### Requirement: ViewArchetypeManager 子原型查询
ViewArchetypeManager SHALL 支持查询子原型对应的 ViewComponents。

#### Scenario: 查询子原型 ViewComponents
- **WHEN** 查询某个子原型（EArchetype）对应的 ViewComponents
- **THEN** ViewArchetypeManager 返回该子原型声明的所有 ViewComponent 类型
- **AND** 如果子原型未注册，返回空数组

## ADDED Requirements

### Requirement: Stage 子原型事件处理
Stage SHALL 订阅并处理子原型变化事件，将事件转发给对应的 EntityView。

#### Scenario: Stage 处理子原型 Attach 事件
- **WHEN** Stage 收到 `EntitySubArchetypeChangedEventData` 事件，变化类型为 "Attach"
- **THEN** Stage 检查 RoomId 是否匹配
- **AND** Stage 获取对应的 EntityView
- **AND** Stage 调用 EntityView 的 `AttachSubArchetype` 方法

#### Scenario: Stage 处理子原型 Detach 事件
- **WHEN** Stage 收到 `EntitySubArchetypeChangedEventData` 事件，变化类型为 "Detach"
- **THEN** Stage 检查 RoomId 是否匹配
- **AND** Stage 获取对应的 EntityView
- **AND** Stage 调用 EntityView 的 `DetachSubArchetype` 方法

### Requirement: Entity 子原型事件发布
Entity SHALL 在子原型变化时发布子原型变化事件，而不是组件变化事件。

#### Scenario: Entity 添加子原型时发布事件
- **WHEN** Entity 的 `AttachSubArchetype` 方法成功添加子原型
- **THEN** Entity 发布 `EntitySubArchetypeChangedEventData` 事件（而不是 `EntityComponentChangedEvent`）
- **AND** 事件包含子原型类型和 "Attach" 变化类型

#### Scenario: Entity 移除子原型时发布事件
- **WHEN** Entity 的 `DetachSubArchetype` 方法成功移除子原型
- **THEN** Entity 发布 `EntitySubArchetypeChangedEventData` 事件（而不是 `EntityComponentChangedEvent`）
- **AND** 事件包含子原型类型和 "Detach" 变化类型

