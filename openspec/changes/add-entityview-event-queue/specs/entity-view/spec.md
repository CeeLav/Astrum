# Entity View Specification - Add Event Queue

## ADDED Requirements

### Requirement: Entity.ViewEventQueue
Entity SHALL 维护一个视图事件队列，用于存储待处理的视图层事件。

#### Scenario: Entity 静态标记视图层存在性
- **GIVEN** Entity 有静态属性 `HasViewLayer`（默认 false）
- **WHEN** 客户端启动时，Stage 初始化设置 `Entity.HasViewLayer = true`
- **THEN** Entity 允许入队视图事件

#### Scenario: 服务器端拒绝入队视图事件
- **GIVEN** `Entity.HasViewLayer` 为 false（服务器端）
- **WHEN** 调用 `entity.QueueViewEvent(ViewEvent)`
- **THEN** Entity 立即返回，不入队事件
- **AND** 不创建队列实例，避免内存泄漏

#### Scenario: 客户端接收视图事件
- **GIVEN** `Entity.HasViewLayer` 为 true（客户端）
- **WHEN** 调用 `entity.QueueViewEvent(ViewEvent)`
- **THEN** Entity 将事件添加到本地队列 `_viewEventQueue`
- **AND** `HasPendingViewEvents` 属性返回 true

#### Scenario: Entity.ViewEventQueue 延迟创建
- **GIVEN** Entity 初始化时 `_viewEventQueue` 为 null
- **AND** `Entity.HasViewLayer` 为 true
- **WHEN** 第一次调用 `QueueViewEvent()` 时
- **THEN** Entity 创建新的 `Queue<ViewEvent>` 实例
- **AND** 事件被添加到队列

#### Scenario: Entity.ViewEventQueue 不被序列化
- **GIVEN** Entity.ViewEventQueue 标记为 `[MemoryPackIgnore]`
- **WHEN** Entity 被序列化时
- **THEN** ViewEventQueue 不被包含在序列化数据中

### Requirement: EntityView 事件处理和分发
EntityView SHALL 提供事件处理方法，用于处理来自 Entity 的视图事件，并根据事件类型分层处理或分发给 ViewComponent。

#### Scenario: EntityView 处理 EntityCreated 事件
- **WHEN** EntityView 处理 `ViewEventType.EntityCreated` 事件
- **THEN** EntityView 执行初始化逻辑（等同于当前的 OnEntityCreated）
- **AND** 创建必要的 ViewComponents

#### Scenario: EntityView 处理 EntityDestroyed 事件
- **WHEN** EntityView 处理 `ViewEventType.EntityDestroyed` 事件
- **THEN** EntityView 执行销毁逻辑（等同于当前的 OnEntityDestroyed）
- **AND** 清理所有资源和组件

#### Scenario: EntityView 处理 SubArchetypeChanged 事件
- **WHEN** EntityView 处理 `ViewEventType.SubArchetypeChanged` 事件
- **THEN** EntityView 根据事件数据添加或移除子原型的 ViewComponents
- **AND** 更新内部状态

#### Scenario: EntityView 分发自定义事件到 ViewComponent
- **WHEN** EntityView 收到自定义视图事件（非预定义的 Stage/EntityView 级别事件）
- **THEN** EntityView 查询 `_viewEventToComponents` 映射
- **AND** 找到注册了该事件类型的所有 ViewComponent
- **AND** 调用每个 ViewComponent 的事件处理器

### Requirement: Stage 事件轮询机制
Stage SHALL 在 Update 时轮询所有 Entity 的视图事件队列，并分发到对应的 EntityView 进行处理。

#### Scenario: Stage 初始化时启用视图层
- **WHEN** Stage.Initialize() 被调用
- **THEN** Stage 设置 `Entity.HasViewLayer = true`
- **AND** 允许 Entity 入队视图事件

#### Scenario: Stage 轮询 Entity 的视图事件
- **WHEN** Stage.Update() 调用 `ProcessViewEvents()`
- **THEN** Stage 遍历 `_room.MainWorld.Entities` 所有实体
- **AND** 对于每个实体，检查 `entity.HasPendingViewEvents`

#### Scenario: Stage 处理有待处理事件的 Entity
- **GIVEN** Entity 的 `HasPendingViewEvents` 为 true
- **WHEN** Stage 处理该 Entity
- **THEN** Stage 获取或创建对应的 EntityView
- **AND** 循环从 `entity.ViewEventQueue` 出队事件
- **AND** 调用 `entityView.ProcessEvent(evt)` 处理每个事件

#### Scenario: Stage 创建 EntityView
- **GIVEN** Entity 有待处理视图事件
- **AND** 对应的 EntityView 不存在
- **AND** 第一个事件类型为 `ViewEventType.EntityCreated`
- **WHEN** Stage 处理该 Entity
- **THEN** Stage 调用 `CreateEntityViewInternal()` 创建 EntityView
- **AND** 将 EntityView 添加到 `_entityViews` 字典

#### Scenario: Stage 处理 EntityView 不存在且第一个事件不是 Created
- **GIVEN** Entity 有待处理视图事件
- **AND** 对应的 EntityView 不存在
- **AND** 第一个事件类型不是 `ViewEventType.EntityCreated`
- **WHEN** Stage 处理该 Entity
- **THEN** Stage 记录警告日志
- **AND** 跳过该 Entity，不处理事件

#### Scenario: Stage 支持配置开关
- **WHEN** 配置项 `USE_VIEW_EVENT_QUEUE` 为 true
- **THEN** Stage 使用事件队列机制
- **WHEN** 配置项 `USE_VIEW_EVENT_QUEUE` 为 false
- **THEN** Stage 使用传统的 EventSystem 订阅机制（向后兼容）

### Requirement: 视图事件数据结构
系统 SHALL 定义视图事件的数据结构，封装事件类型和相关数据。

#### Scenario: ViewEvent 结构定义
- **GIVEN** ViewEvent 结构体包含以下字段：
  - `ViewEventType EventType`: 事件类型
  - `object EventData`: 事件数据（类型根据 EventType 确定）
  - `int Frame`: 事件发生的帧号
- **THEN** ViewEvent 可以被正确创建

#### Scenario: ViewEventType 枚举定义
- **GIVEN** ViewEventType 枚举包含以下值：
  - `EntityCreated`: 实体创建（Stage 级别）
  - `EntityDestroyed`: 实体销毁（Stage 级别）
  - `SubArchetypeChanged`: 子原型变化（EntityView 级别）
  - `WorldRollback`: 世界回滚（Stage 级别）
- **THEN** 所有预定义的视图事件都有对应的类型
- **AND** 自定义视图事件通过 EventData 的类型区分

### Requirement: ViewComponent 事件注册机制
ViewComponent SHALL 提供事件注册机制，允许子类声明需要监听的自定义视图事件。

#### Scenario: ViewComponent 注册事件处理器
- **WHEN** ViewComponent 初始化时
- **THEN** 调用 `RegisterViewEventHandlers()` 虚方法
- **AND** 子类可以调用 `RegisterViewEventHandler<TEvent>(handler)` 注册事件

#### Scenario: EntityView 维护事件映射
- **WHEN** EntityView 添加 ViewComponent 时
- **THEN** 调用 `RegisterViewComponentEventHandlers(component)`
- **AND** 从 ViewComponent 获取事件处理器映射
- **AND** 建立 `事件类型 -> ViewComponent 列表` 映射

#### Scenario: EntityView 分发事件到 ViewComponent
- **WHEN** EntityView 收到自定义视图事件
- **THEN** 查询 `_viewEventToComponents` 映射
- **AND** 获取注册了该事件类型的 ViewComponent 列表
- **AND** 调用每个 ViewComponent 的事件处理器（DynamicInvoke）

### Requirement: 脏组件同步独立性
脏组件同步机制 SHALL 保持独立，不通过视图事件队列处理。

#### Scenario: 脏组件同步机制不变
- **WHEN** Stage.Update() 执行
- **THEN** Stage 先调用 `ProcessViewEvents()` 处理视图事件
- **AND** 然后调用 `SyncDirtyComponents()` 同步脏组件
- **AND** 两个机制相互独立，不互相干扰

#### Scenario: ViewComponent 同时支持事件和数据同步
- **GIVEN** ViewComponent 可以通过 `GetWatchedComponentIds()` 监听脏组件
- **AND** ViewComponent 可以通过 `RegisterViewEventHandlers()` 监听视图事件
- **WHEN** 数据变化时
- **THEN** 脏组件机制触发 `SyncDataFromComponent()`
- **WHEN** 状态变化时
- **THEN** 事件机制触发注册的事件处理器

## MODIFIED Requirements

### Requirement: Stage 初始化和销毁
Stage SHALL 在初始化时准备事件处理基础设施，在销毁时清理所有资源。

**修改说明**：添加事件队列的初始化和清理逻辑。

#### Scenario: Stage 初始化（已有场景，增强）
- **WHEN** Stage.Initialize() 被调用
- **THEN** Stage 创建 StageRoot GameObject
- **AND** 根据配置项决定是否订阅 EventSystem 事件（向后兼容）
- **AND** 初始化 `_pendingViewEvents` 字典
- **AND** 设置 `_isInited` 为 true

#### Scenario: Stage 销毁（已有场景，增强）
- **WHEN** Stage.Destroy() 被调用
- **THEN** Stage 清空所有待处理的视图事件队列 `_pendingViewEvents`
- **AND** 销毁所有 EntityView
- **AND** 取消 EventSystem 事件订阅（如果使用）
- **AND** 销毁 StageRoot GameObject

#### Scenario: Stage 更新（已有场景，增强）
- **WHEN** Stage.Update(deltaTime) 被调用
- **THEN** Stage 首先调用 `ProcessViewEvents()` 处理待处理的视图事件
- **AND** 然后调用 `SyncDirtyComponents()` 同步脏组件
- **AND** 最后更新所有 EntityView

### Requirement: World 实体生命周期事件发布
World SHALL 在实体生命周期变化时发布事件通知视图层。

**修改说明**：将同步事件发布改为在 Entity 上入队事件。

#### Scenario: World 创建实体后发布事件（修改）
- **WHEN** World.CreateEntity() 成功创建实体
- **THEN** World 调用 `entity.QueueViewEvent()` 将 `EntityCreated` 事件入队（如果 USE_VIEW_EVENT_QUEUE=true）
- **OR** World 通过 `EventSystem.Publish()` 发布 `EntityCreatedEventData` 事件（如果 USE_VIEW_EVENT_QUEUE=false）

#### Scenario: World 销毁实体后发布事件（修改）
- **WHEN** World.DestroyEntity() 销毁实体
- **THEN** World 调用 `entity.QueueViewEvent()` 将 `EntityDestroyed` 事件入队（如果 USE_VIEW_EVENT_QUEUE=true）
- **OR** World 通过 `EventSystem.Publish()` 发布 `EntityDestroyedEventData` 事件（如果 USE_VIEW_EVENT_QUEUE=false）

#### Scenario: World 回滚后发布事件（修改）
- **WHEN** World 执行回滚操作后
- **THEN** World 遍历所有实体，调用 `entity.QueueViewEvent()` 将 `WorldRollback` 事件入队（如果 USE_VIEW_EVENT_QUEUE=true）
- **OR** World 通过 `EventSystem.Publish()` 发布 `WorldRollbackEventData` 事件（如果 USE_VIEW_EVENT_QUEUE=false）

### Requirement: Entity 子原型变化事件发布
Entity SHALL 在子原型变化时发布事件通知视图层。

**修改说明**：将同步事件发布改为在自身上入队事件。

#### Scenario: Entity 添加子原型时发布事件（修改）
- **WHEN** Entity.AttachSubArchetype() 成功添加子原型
- **THEN** Entity 调用 `this.QueueViewEvent()` 将 `SubArchetypeChanged` 事件入队（如果 USE_VIEW_EVENT_QUEUE=true）
- **OR** Entity 通过 `EventSystem.Publish()` 发布 `EntitySubArchetypeChangedEventData` 事件（如果 USE_VIEW_EVENT_QUEUE=false）

#### Scenario: Entity 移除子原型时发布事件（修改）
- **WHEN** Entity.DetachSubArchetype() 成功移除子原型
- **THEN** Entity 调用 `this.QueueViewEvent()` 将 `SubArchetypeChanged` 事件入队（如果 USE_VIEW_EVENT_QUEUE=true）
- **OR** Entity 通过 `EventSystem.Publish()` 发布 `EntitySubArchetypeChangedEventData` 事件（如果 USE_VIEW_EVENT_QUEUE=false）

## REMOVED Requirements

无。本变更不移除任何现有需求，而是增强现有机制并添加新的异步处理能力。

