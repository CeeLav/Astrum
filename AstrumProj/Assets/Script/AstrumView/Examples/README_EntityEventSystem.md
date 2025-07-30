# 实体事件系统

## 概述

实体事件系统实现了逻辑层（LogicCore）和视图层（View）之间的解耦通信。当 `World` 创建实体后，会抛出事件，`Stage` 层监听到这个事件后自动创建对应的 `EntityView`。

## 系统架构

### 1. 事件数据类 (`EntityEvents.cs`)

定义了各种实体相关的事件数据：

- `EntityCreatedEventData`: 实体创建事件
- `EntityDestroyedEventData`: 实体销毁事件
- `EntityUpdatedEventData`: 实体更新事件
- `EntityActiveStateChangedEventData`: 实体激活状态变化事件
- `EntityComponentChangedEventData`: 实体组件变化事件

### 2. 逻辑层事件发布

#### World 类
- 在 `CreateEntity<T>()` 方法中发布 `EntityCreatedEventData` 事件
- 在 `DestroyEntity()` 方法中发布 `EntityDestroyedEventData` 事件
- 提供 `PublishEntityUpdatedEvent()` 和 `PublishEntityActiveStateChangedEvent()` 方法

#### Entity 类
- 在 `SetActive()` 方法中发布激活状态变化事件
- 在 `AddComponent()` 和 `RemoveComponent()` 方法中发布组件变化事件

### 3. 视图层事件监听

#### Stage 类
- 在 `Initialize()` 方法中订阅实体事件
- 在 `Destroy()` 方法中取消订阅事件
- 实现各种事件处理方法：
  - `OnEntityCreated()`: 创建对应的 EntityView
  - `OnEntityDestroyed()`: 销毁对应的 EntityView
  - `OnEntityUpdated()`: 同步 EntityView 数据
  - `OnEntityActiveStateChanged()`: 更新 EntityView 激活状态
  - `OnEntityComponentChanged()`: 处理组件变化

### 4. EntityView 工厂

#### EntityViewFactory 类
- 根据实体类型创建对应的 EntityView
- 支持为 EntityView 添加视图组件
- 提供统一的 EntityView 创建和销毁接口

## 工作流程

### 1. 实体创建流程

```
World.CreateEntity() 
    ↓
发布 EntityCreatedEventData 事件
    ↓
Stage.OnEntityCreated() 监听事件
    ↓
调用 CreateEntityView() 创建 EntityView
    ↓
使用 EntityViewFactory 创建具体类型的 EntityView
    ↓
EntityView 初始化并添加到 Stage 的 EntityViews 字典
```

### 2. 实体销毁流程

```
World.DestroyEntity()
    ↓
发布 EntityDestroyedEventData 事件
    ↓
Stage.OnEntityDestroyed() 监听事件
    ↓
从 EntityViews 字典中获取对应的 EntityView
    ↓
调用 EntityView.Destroy() 销毁视图
    ↓
从 EntityViews 字典中移除
```

### 3. 实体更新流程

```
Entity 状态变化
    ↓
发布 EntityUpdatedEventData 事件
    ↓
Stage.OnEntityUpdated() 监听事件
    ↓
调用 EntityView.SyncWithEntity() 同步数据
    ↓
根据更新类型进行特殊处理
```

## 使用方法

### 1. 基本使用

```csharp
// 创建世界
var world = new World { WorldId = 1, RoomId = 1 };
world.Initialize();

// 创建Stage
var stage = new GameStage();
stage.SetRoom(new Room(1, "测试房间"));
stage.Initialize();

// 创建实体（会自动触发事件并创建EntityView）
var entity = world.CreateEntity<Entity>("Player");
```

### 2. 手动发布事件

```csharp
// 发布实体更新事件
world.PublishEntityUpdatedEvent(entity, "position", new Vector3(1f, 0f, 1f));

// 发布激活状态变化事件
world.PublishEntityActiveStateChangedEvent(entity, false);
```

### 3. 自定义事件处理

```csharp
// 在Stage中重写CreateEntityView方法
protected override EntityView CreateEntityView(long entityId)
{
    // 获取实体类型
    string entityType = GetEntityType(entityId);
    
    // 使用工厂创建EntityView
    return EntityViewFactory.Instance.CreateEntityView(entityType, entityId);
}
```

## 扩展指南

### 1. 添加新的事件类型

1. 在 `EntityEvents.cs` 中定义新的事件数据类
2. 在 `World` 或 `Entity` 类中添加事件发布方法
3. 在 `Stage` 类中添加事件处理方法

### 2. 添加新的 EntityView 类型

1. 继承 `EntityView` 类创建新的视图类型
2. 在 `EntityViewFactory` 中添加创建逻辑
3. 在 `Stage.CreateEntityView()` 中添加类型判断

### 3. 添加新的视图组件

1. 继承 `ViewComponent` 类创建新的组件
2. 在 `EntityViewFactory.AddViewComponent()` 中添加创建逻辑
3. 在 `EntityView` 中实现组件的使用

## 示例代码

参考 `EntityEventSystemExample.cs` 文件，其中包含了完整的使用示例：

- 创建测试世界和Stage
- 创建和销毁实体
- 更新实体状态
- 测试事件系统
- 显示当前状态

## 注意事项

1. **事件订阅**: Stage 在初始化时会自动订阅事件，销毁时会自动取消订阅

2. **Room ID 过滤**: Stage 只会处理属于自己 Room 的实体事件

3. **线程安全**: 事件系统使用锁机制确保线程安全

4. **内存管理**: 确保在 Stage 销毁时正确清理 EntityView 和取消事件订阅

5. **错误处理**: 事件处理过程中出现异常不会影响其他事件的处理

## 性能考虑

1. **事件过滤**: 通过 Room ID 过滤减少不必要的事件处理

2. **延迟处理**: 可以考虑将事件处理放在主线程中，避免线程安全问题

3. **对象池**: 对于频繁创建销毁的 EntityView，可以考虑使用对象池

4. **批量处理**: 对于大量实体的创建，可以考虑批量处理事件 