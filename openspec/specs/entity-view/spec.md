# Entity View Specification

## Purpose

EntityView 系统负责将逻辑层的 Entity 映射到视图层的视觉表现，管理 ViewComponents 的装配和同步。
## Requirements
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

