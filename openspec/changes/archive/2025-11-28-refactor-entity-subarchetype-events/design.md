# Design: Entity 子原型事件系统重构

## Context

当前系统架构：
- Entity 支持子原型（SubArchetype）机制，可以运行时动态添加/移除
- EntityView 基于主原型（Archetype）装配 ViewComponents，在创建时一次性装配
- 子原型变化时，Entity 通过组件变化事件通知，但 EntityView 没有响应

问题：
- 子原型是一个逻辑概念，不应该通过组件级别的变化来表达
- EntityView 无法响应子原型的动态变化，导致视图层与逻辑层不同步

## Goals / Non-Goals

### Goals
- 建立子原型级别的事件系统，替代组件级别的事件
- EntityView 支持子原型的动态挂载和卸载
- 保持视图层与逻辑层的同步

### Non-Goals
- 不改变 Entity 的子原型机制本身
- 不改变 ViewArchetype 的静态声明方式
- 不改变主原型的装配逻辑

## Decisions

### Decision 1: 新增子原型变化事件

**选择**: 创建 `EntitySubArchetypeChangedEventData` 事件，包含子原型类型和变化类型（Attach/Detach）

**理由**:
- 语义更清晰，直接表达子原型的变化
- 避免通过组件变化间接表达，减少耦合
- 便于后续扩展（如子原型优先级、依赖关系等）

**替代方案考虑**:
- 继续使用组件变化事件：语义不清晰，难以区分是子原型变化还是直接组件操作
- 使用通用实体更新事件：过于宽泛，缺少类型安全

### Decision 2: EntityView 维护活跃子原型列表

**选择**: EntityView 维护 `ActiveSubArchetypes` 列表，类似 Entity 的设计

**理由**:
- 与 Entity 的设计保持一致，便于理解和维护
- 支持查询和状态检查
- 便于实现引用计数和去重逻辑

**替代方案考虑**:
- 每次从 Entity 查询：性能较差，需要频繁访问逻辑层
- 只维护 ViewComponents：丢失子原型语义

### Decision 3: ViewArchetypeManager 支持子原型查询

**选择**: 扩展 `ViewArchetypeManager.TryGetComponents` 方法，支持查询子原型对应的 ViewComponents

**理由**:
- 复用现有的 ViewArchetype 注册机制
- 保持查询接口的一致性
- 支持子原型的 ViewComponents 声明

**实现方式**:
- 子原型使用与主原型相同的 `ViewArchetypeAttribute` 标记
- 查询时直接通过 `EArchetype` 枚举值查找

### Decision 4: 子原型 ViewComponents 的装配时机

**选择**: 在收到子原型 Attach 事件时立即装配对应的 ViewComponents

**理由**:
- 保持视图层与逻辑层的实时同步
- 避免延迟导致的视觉不一致
- 简化状态管理

**注意事项**:
- 需要处理 ViewComponents 的去重（同一类型只装配一次）
- 需要处理装配失败的情况（记录错误但不影响其他组件）
- ViewComponents 之间没有依赖关系，装配顺序无关

## Risks / Trade-offs

### Risk 1: 事件顺序问题
**风险**: 如果子原型变化事件在 EntityView 创建之前发布，可能丢失事件

**缓解措施**: 
- Stage 在创建 EntityView 后，主动同步 Entity 的活跃子原型列表
- 或者在 EntityView 初始化时检查并装配已有的子原型

### Risk 2: 性能影响
**风险**: 频繁的子原型变化可能导致频繁的 ViewComponents 装配/卸载

**缓解措施**:
- 使用引用计数避免重复装配
- 缓存 ViewComponents 类型查询结果
- 批量处理多个子原型变化


## Migration Plan

### 阶段 1: 新增事件和接口
1. 创建 `EntitySubArchetypeChangedEventData` 事件类
2. 在 Entity 中添加发布子原型变化事件的方法
3. 扩展 ViewArchetypeManager 支持子原型查询

### 阶段 2: EntityView 子原型支持
1. 在 EntityView 中添加 `ActiveSubArchetypes` 列表
2. 实现 `AttachSubArchetype` 和 `DetachSubArchetype` 方法
3. 实现 ViewComponents 的引用计数和去重逻辑

### 阶段 3: 事件处理迁移
1. 修改 Entity 的 `AttachSubArchetype` 和 `DetachSubArchetype` 发布子原型事件（不再发布组件变化事件）
2. 在 Stage 中添加 `OnEntitySubArchetypeChanged` 处理方法
3. 完全移除 `OnEntityComponentChanged` 中对子原型的处理（子原型变化不再触发组件事件）

### 阶段 4: 初始同步
1. 在 EntityView 初始化时，同步 Entity 的活跃子原型列表
2. 确保创建时已有的子原型也能正确装配

### 迁移注意事项
- 这是破坏性变更，所有依赖子原型通过组件事件通知的代码都需要更新
- 子原型变化将不再触发 `EntityComponentChangedEvent` 事件

### Decision 5: ViewComponents 无依赖关系

**选择**: ViewComponents 之间没有依赖关系，装配和卸载顺序无关

**理由**:
- 简化实现，避免复杂的依赖管理
- ViewComponents 是独立的视图表现，不应相互依赖
- 卸载时按照正常的 ViewComponent 卸载流程即可

### Decision 6: 子原型无加载顺序

**选择**: 子原型之间不设加载顺序，可以任意顺序挂载/卸载

**理由**:
- 简化实现，避免顺序管理复杂性
- 子原型是独立的逻辑单元，不应相互依赖
- 保持与 Entity 层设计的一致性

### Decision 7: ViewComponent 卸载流程

**选择**: ViewComponent 卸载时按照正常的卸载流程，子原型层不过多处理

**理由**:
- ViewComponent 已有完善的卸载机制
- 子原型层只需管理引用计数和列表状态
- 避免重复的清理逻辑

