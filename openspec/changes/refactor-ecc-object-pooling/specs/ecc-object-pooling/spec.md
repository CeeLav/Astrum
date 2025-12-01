## ADDED Requirements

### Requirement: Entity 对象池支持

Entity SHALL 实现 `IPool` 接口，支持通过 ObjectPool 进行对象池管理。Entity 的创建和销毁应通过对象池进行，以减少 GC 分配。

#### Scenario: 从对象池获取 Entity
- **WHEN** EntityFactory.CreateByArchetype 被调用
- **THEN** 通过 `ObjectPool.Instance.Fetch<Entity>()` 获取 Entity 实例
- **AND** Entity 的 IsFromPool 属性为 true
- **AND** Entity 的所有字段和集合处于初始化状态

#### Scenario: Entity 回收至对象池
- **WHEN** World.DestroyEntity 或 EntityFactory.DestroyEntity 被调用
- **THEN** Entity 调用 Reset 方法重置所有状态
- **AND** Entity 通过 `ObjectPool.Instance.Recycle(entity)` 回收至对象池
- **AND** Entity 的 IsFromPool 属性在回收时被设置为 false

#### Scenario: Entity Reset 重置状态
- **WHEN** Entity 从对象池回收时
- **THEN** Reset 方法被调用，重置所有字段（UniqueId、Name、IsDestroyed 等）
- **AND** 所有集合（Components、ChildrenIds、CapabilityStates 等）被清空
- **AND** 所有引用类型字段被重置为默认值或空集合

#### Scenario: 序列化时创建新 Entity
- **WHEN** MemoryPack 序列化或反序列化 Entity
- **THEN** 使用 `ObjectPool.Instance.Fetch<Entity>(isFromPool: false)` 创建新对象
- **AND** 创建的 Entity 不来自对象池，IsFromPool 为 false
- **AND** 序列化后的对象不会影响对象池状态

### Requirement: Component 对象池支持

BaseComponent 及其所有子类 SHALL 实现 `IPool` 接口，支持通过 ObjectPool 进行对象池管理。Component 的创建和销毁应通过对象池进行。

#### Scenario: 从对象池获取 Component
- **WHEN** ComponentFactory.CreateComponentFromType 被调用
- **THEN** 通过 `ObjectPool.Instance.Fetch<BaseComponent>(componentType)` 获取 Component 实例
- **AND** Component 的 IsFromPool 属性为 true
- **AND** Component 的所有字段处于初始化状态

#### Scenario: Component 回收至对象池
- **WHEN** Entity.RemoveComponent 被调用或 Entity 销毁时
- **THEN** Component 调用 Reset 方法重置所有状态
- **AND** Component 通过 `ObjectPool.Instance.Recycle(component)` 回收至对象池
- **AND** Component 的 IsFromPool 属性在回收时被设置为 false

#### Scenario: Component Reset 重置状态
- **WHEN** Component 从对象池回收时
- **THEN** Reset 方法被调用，重置 EntityId 等基础字段
- **AND** Component 子类的所有自定义字段被重置
- **AND** 所有集合类型字段被清空或重置为默认值

#### Scenario: 序列化时创建新 Component
- **WHEN** MemoryPack 序列化或反序列化 Component
- **THEN** 使用 `ObjectPool.Instance.Fetch<BaseComponent>(componentType, isFromPool: false)` 创建新对象
- **AND** 创建的 Component 不来自对象池，IsFromPool 为 false

### Requirement: 对象池容量配置

ObjectPool SHALL 支持为不同类型的对象配置不同的池容量，以优化内存使用和性能。

#### Scenario: Entity 对象池容量配置
- **WHEN** ObjectPool 初始化 Entity 类型的对象池
- **THEN** Entity 类型的池容量设置为 500（可配置）
- **AND** 当池中对象数量超过容量时，多余的回收对象被丢弃

#### Scenario: Component 对象池容量配置
- **WHEN** ObjectPool 初始化 Component 类型的对象池
- **THEN** Component 类型的池容量设置为 1000（可配置）
- **AND** 不同类型的 Component 可以设置不同的容量

#### Scenario: 对象池容量超限处理
- **WHEN** 回收对象时，对象池已达到最大容量
- **THEN** 多余的回收对象被丢弃，不进入对象池
- **AND** 对象可以正常被 GC 回收

### Requirement: Entity 集合预分配

Entity 内部的集合类型（List、Dictionary）SHALL 使用预分配容量，以减少扩容时的 GC 分配。

#### Scenario: Components 列表预分配
- **WHEN** Entity 被创建
- **THEN** Components 列表预分配容量为 8（可配置）
- **AND** 添加组件时，如果列表未满，不产生 GC 分配

#### Scenario: CapabilityStates 字典预分配
- **WHEN** Entity 被创建
- **THEN** CapabilityStates 字典预分配容量为 4（可配置）
- **AND** 添加状态时，如果字典未满，不产生 GC 分配

#### Scenario: 集合重置而非重新创建
- **WHEN** Entity.Reset 被调用
- **THEN** 集合调用 Clear 方法清空内容
- **AND** 集合对象本身不被销毁，保留预分配的容量

## MODIFIED Requirements

### Requirement: EntityFactory 创建 Entity

EntityFactory SHALL 使用 ObjectPool 创建 Entity，而非直接使用 new 关键字。

#### Scenario: 通过对象池创建 Entity
- **WHEN** EntityFactory.CreateByArchetype 被调用
- **THEN** 使用 `ObjectPool.Instance.Fetch<Entity>()` 或 `ObjectPool.Instance.Fetch<T>()` 获取 Entity
- **AND** 获取的 Entity 已重置所有状态，可以直接使用
- **AND** Entity 设置基本属性（Name、Archetype、EntityConfigId 等）后返回

### Requirement: ComponentFactory 创建 Component

ComponentFactory SHALL 使用 ObjectPool 创建 Component，而非直接使用 new 或 Activator.CreateInstance。

#### Scenario: 通过对象池创建 Component
- **WHEN** ComponentFactory.CreateComponentFromType 被调用
- **THEN** 使用 `ObjectPool.Instance.Fetch(type)` 获取 Component 实例
- **AND** 如果对象池为空，ObjectPool 自动创建新实例
- **AND** 获取的 Component 已重置所有状态，EntityId 为 0

### Requirement: Entity 销毁流程

Entity 的销毁流程 SHALL 改为回收至对象池，而非直接丢弃。

#### Scenario: Entity 销毁并回收
- **WHEN** World.DestroyEntity 或 EntityFactory.DestroyEntity 被调用
- **THEN** Entity 调用 Reset 方法重置所有状态
- **AND** Entity 的所有 Component 被回收至对象池
- **AND** Entity 从 World.Entities 字典中移除
- **AND** Entity 通过 ObjectPool.Instance.Recycle 回收至对象池
- **AND** Entity 不再被引用，可以被对象池复用

#### Scenario: Entity 销毁时清理关系
- **WHEN** Entity 被销毁时
- **THEN** 处理父子关系（从父实体移除子实体引用，或从子实体移除父实体引用）
- **AND** 递归销毁所有子实体
- **AND** 清理所有事件订阅和回调

## REMOVED Requirements

无删除的需求。

