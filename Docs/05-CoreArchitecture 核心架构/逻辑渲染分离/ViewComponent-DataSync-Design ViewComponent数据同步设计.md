# ViewComponent 数据同步设计

> 📖 **版本**: v1.0 | 📅 **创建日期**: 2025-01-XX  
> 👥 **面向读者**: 视图层开发人员、逻辑层开发人员  
> 🎯 **目标**: 实现 ViewComponent 自动监听 BaseComponent 数据变化并同步的机制

**TL;DR**
- ViewComponent 通过 `GetWatchedComponentTypes()` 声明需要监听的 BaseComponent 类型
- BaseComponent 使用脏标记机制，子类在数据变化时调用 `MarkDirty()` 设置脏标记
- Entity 维护组件的脏标记状态，在查询时提供脏标记信息
- Stage 在更新时查询所有 Entity 的脏组件，通知对应的 EntityView
- EntityView 建立 ViewComponent 与 BaseComponent 的映射关系，被动接收同步请求
- 采用混合方案：重要变化使用脏标记查询，频繁变化在 Update 中主动拉取

---

## 1. 概述

当前 ViewComponent 的 `OnSyncData` 接口存在以下问题：

1. **调用方式不统一**：部分 ViewComponent（如 `HUDViewComponent`）在 `OnUpdate` 中主动拉取数据，部分（如 `TransViewComponent`）的 `OnSyncData` 未被使用
2. **缺少自动通知机制**：ViewComponent 无法声明需要监听的 BaseComponent，BaseComponent 数据变化时无法自动通知 ViewComponent
3. **性能问题**：每帧主动拉取数据导致不必要的性能开销

**设计目标**：
- ViewComponent 可以声明需要监听的 BaseComponent 类型
- BaseComponent 数据变化时自动通知对应的 ViewComponent
- 统一数据同步机制，提升可维护性和性能

**系统边界**：
- ✅ 负责：ViewComponent 与 BaseComponent 之间的数据同步机制
- ✅ 负责：BaseComponent 数据变化通知机制
- ❌ 不负责：具体的视图渲染逻辑、动画播放、特效显示

---

## 2. 架构设计

### 2.1 整体架构

```
┌─────────────────┐
│ BaseComponent   │
│                 │
│ ComponentId     │──┐
│ EntityId        │  │
└────────┬────────┘  │
         │           │
         │ 数据变化   │
         │ 通过EntityId│
         │ 获取Entity │
         │           │
┌────────▼────────┐  │
│     Entity      │  │
│                 │  │
│ HashSet<int>    │  │
│ _dirtyComponentIds│ │
│ MarkComponentDirty│ │
│ GetDirtyComponents│ │
└────────┬────────┘  │
         │           │
         │           │
┌────────▼────────┐  │
│     Stage      │  │
│                 │  │
│ Update()        │  │
│ 查询脏组件ID     │──┼──┐
│ 通知EntityView   │  │  │
└────────┬────────┘  │  │  │
         │           │  │  │
         │           │  │  │
┌────────▼────────┐  │  │  │
│   EntityView    │  │  │  │
│                 │  │  │  │
│ 建立映射关系     │  │  │  │
│ 接收同步请求     │  │  │  │
│ 触发同步         │──┼──┼──┼──┐
└────────┬────────┘  │  │  │  │
         │           │  │  │  │
         │           │  │  │  │
┌────────▼────────┐  │  │  │  │
│ ViewComponent   │  │  │  │  │
│                 │  │  │  │  │
│ GetWatchedIds   │  │  │  │  │
│ SyncDataFromComp│  │  │  │  │
│ (componentId)   │  │  │  │  │
│ OnSyncData      │◄─┘  │  │  │
└─────────────────┘     │  │  │
                         │  │  │
                    ┌────┘  │  │
                    │       │  │
                    └───────┘  │
                               │
                               └── 脏标记查询同步流程
```

### 2.2 核心组件

#### 2.2.1 BaseComponent

**文件**: `AstrumProj/Assets/Script/AstrumLogic/Components/BaseComponent.cs`

**说明**:
- BaseComponent 不需要添加任何新方法
- 子类在数据变化时，需要通过 EntityId 获取 Entity，然后调用 Entity 的方法标记组件为脏
- 对于频繁变化的组件（如 `TransComponent`），可以不使用脏标记，由 ViewComponent 在 Update 中主动拉取
- 脏标记由 Entity 统一管理，使用 ComponentId 作为唯一标识

#### 2.2.2 Entity 脏标记管理

**文件**: `AstrumProj/Assets/Script/AstrumLogic/Core/Entity.cs`

**修改内容**:
- 维护脏组件 ID 集合：`HashSet<int> _dirtyComponentIds`
- 提供 `GetDirtyComponentIds()` 方法：返回当前所有脏组件的 ComponentId
- 提供 `GetDirtyComponents()` 方法：根据 ComponentId 返回对应的组件实例
- 提供 `ClearDirtyComponents()` 方法：清除所有脏标记
- 在组件调用 `MarkDirty()` 时，将 ComponentId 添加到脏组件集合

**设计要点**:
- Entity 统一管理所有组件的脏标记状态，使用 ComponentId 作为唯一标识
- 使用 HashSet<int> 存储 ComponentId，性能高效
- 提供批量查询接口，便于 Stage 统一处理
- 脏标记在查询后统一清除，避免重复处理

#### 2.2.3 Stage 查询处理

**文件**: `AstrumProj/Assets/Script/AstrumView/Core/Stage.cs`

**修改内容**:
- 在 `Update()` 方法中，遍历所有 Entity，查询脏组件
- 对于有脏组件的 Entity，通知对应的 EntityView 进行同步
- 同步完成后，清除 Entity 的脏标记

**设计要点**:
- Stage 作为协调层，统一处理所有 Entity 的脏组件查询
- 批量处理，减少遍历次数
- 在视图层更新时处理，确保逻辑层和视图层的同步时机

#### 2.2.3 ViewComponent 增强

**文件**: `AstrumProj/Assets/Script/AstrumView/Components/ViewComponent.cs`

**新增内容**:
- `GetWatchedComponentIds()` 虚方法：返回需要监听的 BaseComponent 的 ComponentId 数组
- `SyncDataFromComponent(int componentId)` 虚方法：根据 ComponentId 从 OwnerEntity 获取组件并同步数据

**设计要点**:
- 默认不监听任何组件，子类重写 `GetWatchedComponentIds()` 声明需要监听的 ComponentId
- `SyncDataFromComponent` 由子类实现，根据 ComponentId 从 OwnerEntity 获取组件并提取数据
- 保持现有的 `OnSyncData` 抽象方法，确保向后兼容

#### 2.2.4 EntityView 协调机制

**文件**: `AstrumProj/Assets/Script/AstrumView/Core/EntityView.cs`

**新增内容**:
- `_componentIdToViewComponentsMap` 字典：维护 ComponentId 到 ViewComponent 列表的映射
- `RegisterViewComponentWatchedIds()` 方法：建立映射关系
- `UnregisterViewComponentWatchedIds()` 方法：清理映射关系
- `SyncDirtyComponents()` 方法：接收 Stage 的同步请求，处理脏组件

**设计要点**:
- EntityView 作为协调层，负责建立和维护 ViewComponent 与 ComponentId 的映射关系
- 被动接收 Stage 的同步请求，避免主动轮询带来的性能开销
- 根据 ViewComponent 的监听声明（ComponentId），只处理需要同步的组件

---

## 3. 实现细节

### 3.1 BaseComponent 使用说明

#### 3.1.1 BaseComponent 不需要修改

BaseComponent 不需要添加任何新方法，保持现有实现即可。

#### 3.1.2 子类使用示例

子类在数据变化时，需要通过 EntityId 获取 Entity，然后调用 Entity 的方法标记组件为脏：

```csharp
// DynamicStatsComponent.cs 示例
public partial class DynamicStatsComponent : BaseComponent
{
    private Dictionary<DynamicResourceType, FP> _resources = new Dictionary<DynamicResourceType, FP>();
    
    public void Set(DynamicResourceType type, FP value)
    {
        if (!_resources.ContainsKey(type) || _resources[type] != value)
        {
            _resources[type] = value;
            
            // 对于重要的资源变化（如血量），标记组件为脏
            if (type == DynamicResourceType.CURRENT_HP)
            {
                // 通过 EntityId 获取 Entity，然后调用 Entity 的方法
                // 具体实现取决于项目的架构，可能需要：
                // 1. 通过 World 管理器获取 Entity
                // 2. 通过 Entity 管理器获取 Entity
                // 3. 通过静态方法获取 Entity
                // 
                // 示例（需要根据实际项目架构调整）：
                // var entity = GetEntityById(EntityId);
                // entity?.MarkComponentDirty(ComponentId);
            }
        }
    }
    
    // 注意：这里需要根据实际项目架构提供获取 Entity 的方法
    // 例如：通过 World.GetEntity(EntityId) 获取
}
```

### 3.2 Entity 脏标记管理

```csharp
// Entity.cs
public partial class Entity
{
    // 脏组件 ID 集合（使用 ComponentId 作为唯一标识）
    private HashSet<int> _dirtyComponentIds = new HashSet<int>();
    
    // 在 AddComponent 方法中
    public T AddComponent<T>(T component) where T : BaseComponent
    {
        // 现有代码...
        // 注意：BaseComponent 已经有 EntityId 属性，在 AddComponent 时会设置
        // component.EntityId = UniqueId; （如果还没有设置的话）
        
        // 发布组件添加事件（保持现有机制）
        PublishComponentChangedEvent(component, "add");
        
        return component;
    }
    
    // 在 RemoveComponent 方法中
    public bool RemoveComponent<T>() where T : BaseComponent
    {
        var component = GetComponent<T>();
        if (component != null)
        {
            // 从脏组件 ID 集合中移除
            _dirtyComponentIds.Remove(component.ComponentId);
            
            // 发布组件移除事件（保持现有机制）
            PublishComponentChangedEvent(component, "remove");
            
            // 现有代码...
        }
        
        return component != null;
    }
    
    /// <summary>
    /// 记录组件为脏（由 BaseComponent.MarkDirty() 调用）
    /// </summary>
    /// <param name="componentId">组件的 ComponentId</param>
    public void MarkComponentDirty(int componentId)
    {
        _dirtyComponentIds.Add(componentId);
    }
    
    /// <summary>
    /// 获取所有脏组件的 ComponentId
    /// </summary>
    public IReadOnlyCollection<int> GetDirtyComponentIds()
    {
        return _dirtyComponentIds;
    }
    
    /// <summary>
    /// 根据 ComponentId 获取对应的组件实例
    /// </summary>
    public BaseComponent GetComponentById(int componentId)
    {
        foreach (var component in Components.Values)
        {
            if (component.ComponentId == componentId)
            {
                return component;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 获取所有脏组件实例
    /// </summary>
    public List<BaseComponent> GetDirtyComponents()
    {
        var dirtyComponents = new List<BaseComponent>();
        foreach (var componentId in _dirtyComponentIds)
        {
            var component = GetComponentById(componentId);
            if (component != null)
            {
                dirtyComponents.Add(component);
            }
        }
        return dirtyComponents;
    }
    
    /// <summary>
    /// 清除所有脏标记
    /// </summary>
    public void ClearDirtyComponents()
    {
        _dirtyComponentIds.Clear();
    }
}
```

### 3.3 ViewComponent 声明机制

```csharp
// ViewComponent.cs
public abstract class ViewComponent
{
    // 现有代码...
    
    /// <summary>
    /// 获取需要监听的 BaseComponent 的 ComponentId 列表
    /// 子类重写此方法以声明需要监听的组件 ID
    /// </summary>
    /// <returns>需要监听的 ComponentId 数组，如果不需要监听则返回 null</returns>
    public virtual int[] GetWatchedComponentIds()
    {
        return null; // 默认不监听任何组件
    }
    
    /// <summary>
    /// 根据 ComponentId 从 OwnerEntity 获取组件并同步数据
    /// 子类重写此方法以自定义数据提取逻辑
    /// </summary>
    /// <param name="componentId">BaseComponent 的 ComponentId</param>
    protected virtual void SyncDataFromComponent(int componentId)
    {
        // 默认实现：子类可以重写以自定义数据提取逻辑
        // 例如：从 OwnerEntity 根据 ComponentId 获取组件，提取数据，构造数据对象，然后调用 OnSyncData
        if (OwnerEntity == null) return;
        
        var component = OwnerEntity.GetComponentById(componentId);
        if (component != null)
        {
            // 子类应该重写此方法，从 component 提取数据并调用 OnSyncData
        }
    }
    
    // 现有的抽象方法
    protected abstract void OnSyncData(object data);
}
```

### 3.4 Stage 查询处理

```csharp
// Stage.cs
public class Stage
{
    // 现有代码...
    
    /// <summary>
    /// 更新 Stage
    /// </summary>
    public virtual void Update(float deltaTime)
    {
        // 现有更新逻辑...
        
        // 处理脏组件同步
        SyncDirtyComponents();
        
        // 其他更新逻辑...
    }
    
    /// <summary>
    /// 同步所有 Entity 的脏组件
    /// </summary>
    private void SyncDirtyComponents()
    {
        if (_room?.MainWorld == null) return;
        
        // 遍历所有 Entity
        foreach (var entity in _room.MainWorld.GetAllEntities())
        {
            var dirtyComponentIds = entity.GetDirtyComponentIds();
            if (dirtyComponentIds.Count > 0)
            {
                // 获取对应的 EntityView
                if (_entityViews.TryGetValue(entity.UniqueId, out var entityView))
                {
                    // 通知 EntityView 同步脏组件（传入 ComponentId 集合）
                    entityView.SyncDirtyComponents(dirtyComponentIds);
                }
                
                // 清除脏标记
                entity.ClearDirtyComponents();
            }
        }
    }
}
```

### 3.5 EntityView 协调机制

```csharp
// EntityView.cs
public class EntityView
{
    // 现有代码...
    
    // ComponentId 到 ViewComponent 列表的映射
    private Dictionary<int, List<ViewComponent>> _componentIdToViewComponentsMap = new Dictionary<int, List<ViewComponent>>();
    
    /// <summary>
    /// 添加视图组件
    /// </summary>
    public virtual void AddViewComponent(ViewComponent component)
    {
        // 现有代码...
        
        // 建立映射关系
        RegisterViewComponentWatchedIds(component);
    }
    
    /// <summary>
    /// 移除视图组件
    /// </summary>
    public virtual void RemoveViewComponent<T>() where T : ViewComponent
    {
        var component = GetViewComponent<T>();
        if (component != null)
        {
            // 清理映射关系
            UnregisterViewComponentWatchedIds(component);
            
            // 现有代码...
        }
    }
    
    /// <summary>
    /// 注册 ViewComponent 监听的组件 ID
    /// </summary>
    private void RegisterViewComponentWatchedIds(ViewComponent viewComponent)
    {
        var watchedIds = viewComponent.GetWatchedComponentIds();
        if (watchedIds == null || watchedIds.Length == 0)
        {
            return; // 没有需要监听的组件
        }
        
        foreach (var componentId in watchedIds)
        {
            if (!_componentIdToViewComponentsMap.ContainsKey(componentId))
            {
                _componentIdToViewComponentsMap[componentId] = new List<ViewComponent>();
            }
            if (!_componentIdToViewComponentsMap[componentId].Contains(viewComponent))
            {
                _componentIdToViewComponentsMap[componentId].Add(viewComponent);
            }
        }
    }
    
    /// <summary>
    /// 取消注册 ViewComponent 监听的组件 ID
    /// </summary>
    private void UnregisterViewComponentWatchedIds(ViewComponent viewComponent)
    {
        var watchedIds = viewComponent.GetWatchedComponentIds();
        if (watchedIds == null || watchedIds.Length == 0)
        {
            return; // 没有需要取消监听的组件
        }
        
        foreach (var componentId in watchedIds)
        {
            if (_componentIdToViewComponentsMap.ContainsKey(componentId))
            {
                _componentIdToViewComponentsMap[componentId].Remove(viewComponent);
                if (_componentIdToViewComponentsMap[componentId].Count == 0)
                {
                    _componentIdToViewComponentsMap.Remove(componentId);
                }
            }
        }
    }
    
    /// <summary>
    /// 同步脏组件（由 Stage 调用）
    /// </summary>
    public void SyncDirtyComponents(IReadOnlyCollection<int> dirtyComponentIds)
    {
        if (dirtyComponentIds == null || dirtyComponentIds.Count == 0)
        {
            return;
        }
        
        // 遍历所有脏组件 ID
        foreach (var componentId in dirtyComponentIds)
        {
            // 检查是否有 ViewComponent 监听此组件 ID
            if (!_componentIdToViewComponentsMap.ContainsKey(componentId))
            {
                continue;
            }
            
            // 获取对应的 ViewComponent 列表
            var viewComponents = _componentIdToViewComponentsMap[componentId];
            
            // 通知所有监听的 ViewComponent
            foreach (var viewComponent in viewComponents)
            {
                if (viewComponent != null && viewComponent.IsEnabled)
                {
                    // 调用 ViewComponent 的数据同步方法（传入 ComponentId）
                    viewComponent.SyncDataFromComponent(componentId);
                }
            }
        }
    }
    
    /// <summary>
    /// 销毁实体视图
    /// </summary>
    public virtual void Destroy()
    {
        // 清理映射关系
        _componentIdToViewComponentsMap.Clear();
        
        // 现有代码...
    }
}
```

### 3.5 ViewComponent 子类实现示例

#### 3.5.1 HealthViewComponent 示例

```csharp
// HealthViewComponent.cs
public class HealthViewComponent : ViewComponent
{
    // 现有代码...
    
    // 需要监听的组件 ID（在初始化时获取）
    private int _dynamicStatsComponentId;
    private int _derivedStatsComponentId;
    
    protected override void OnInitialize()
    {
        // 现有初始化代码...
        
        // 获取需要监听的组件 ID
        if (OwnerEntity != null)
        {
            var dynamicStats = OwnerEntity.GetComponent<DynamicStatsComponent>();
            var derivedStats = OwnerEntity.GetComponent<DerivedStatsComponent>();
            
            if (dynamicStats != null)
            {
                _dynamicStatsComponentId = dynamicStats.ComponentId;
            }
            if (derivedStats != null)
            {
                _derivedStatsComponentId = derivedStats.ComponentId;
            }
        }
    }
    
    /// <summary>
    /// 声明需要监听的组件 ID
    /// </summary>
    public override int[] GetWatchedComponentIds()
    {
        var ids = new List<int>();
        if (_dynamicStatsComponentId != 0)
        {
            ids.Add(_dynamicStatsComponentId);
        }
        if (_derivedStatsComponentId != 0)
        {
            ids.Add(_derivedStatsComponentId);
        }
        return ids.ToArray();
    }
    
    /// <summary>
    /// 根据 ComponentId 同步数据
    /// </summary>
    protected override void SyncDataFromComponent(int componentId)
    {
        if (OwnerEntity == null) return;
        
        // 根据 ComponentId 获取组件
        var component = OwnerEntity.GetComponentById(componentId);
        if (component == null) return;
        
        // 获取相关组件（可能需要多个组件的数据）
        var dynamicStats = OwnerEntity.GetComponent<DynamicStatsComponent>();
        var derivedStats = OwnerEntity.GetComponent<DerivedStatsComponent>();
        
        if (dynamicStats != null && derivedStats != null)
        {
            // 构造数据对象
            var healthData = new HealthData(
                (float)dynamicStats.Get(DynamicResourceType.CURRENT_HP),
                (float)derivedStats.Get(StatType.HP),
                dynamicStats.Get(DynamicResourceType.CURRENT_HP) > 0
            );
            
            // 直接调用 OnSyncData
            OnSyncData(healthData);
        }
    }
    
    // 现有的 OnSyncData 实现保持不变
    protected override void OnSyncData(object data)
    {
        if (data is HealthData healthData)
        {
            // 现有实现...
        }
    }
}
```

#### 3.5.2 TransViewComponent 示例（频繁变化，不使用脏标记）

```csharp
// TransViewComponent.cs
public class TransViewComponent : ViewComponent
{
    // 对于频繁变化的组件，可以不声明监听，在 Update 中主动拉取
    public override int[] GetWatchedComponentIds()
    {
        return null; // 不监听，在 Update 中主动拉取
    }
    
    protected override void OnUpdate(float deltaTime)
    {
        // 在 Update 中主动从 OwnerEntity 获取 TransComponent 数据
        var ownerEntity = OwnerEntity;
        if (ownerEntity != null)
        {
            var transComponent = ownerEntity.GetComponent<TransComponent>();
            if (transComponent != null)
            {
                // 直接使用数据，不需要通过脏标记通知
                // 现有实现...
            }
        }
    }
}
```

---

## 4. 数据流

### 4.1 数据变化通知流程

```
1. BaseComponent 数据变化
   ↓
2. 子类通过 EntityId 获取 Entity
   ↓
3. 调用 Entity.MarkComponentDirty(ComponentId) 记录脏组件 ID
   ↓
4. Entity 将 ComponentId 添加到 _dirtyComponentIds 集合
   ↓
5. Stage.Update() 调用 SyncDirtyComponents()
   ↓
6. Stage 遍历所有 Entity，查询脏组件 ID
   ↓
7. Entity.GetDirtyComponents() 根据 ID 获取组件实例
   ↓
8. 对于有脏组件的 Entity，调用 EntityView.SyncDirtyComponents()
   ↓
9. EntityView 查找对应的 ViewComponent
   ↓
10. 调用 ViewComponent.SyncDataFromComponent(componentId)
    ↓
11. ViewComponent 根据 ComponentId 获取组件并提取数据
    ↓
12. ViewComponent.OnSyncData() 执行同步逻辑
    ↓
13. Entity.ClearDirtyComponents() 清除脏标记
```

### 4.2 初始化流程

```
1. EntityView.Initialize()
   ↓
2. EntityView.AddViewComponent()
   ↓
3. ViewComponent.Initialize()
   ↓
4. EntityView.RegisterViewComponentWatchedTypes()
   ↓
5. 建立 BaseComponent 类型到 ViewComponent 的映射
```

---

## 5. 关键决策

### 5.1 为什么采用混合方案？

**问题**: BaseComponent 数据变化如何通知 ViewComponent？

**备选方案**:
- 方案A：所有组件都使用事件通知
- 方案B：所有组件都在 Update 中主动拉取
- 方案C：混合方案（重要变化用事件，频繁变化主动拉取）

**选择**: 方案C

**理由**:
- 性能考虑：频繁变化的组件（如位置、旋转）每帧都变化，使用事件会产生大量事件，反而影响性能
- 实时性考虑：重要但不频繁的变化（如血量、状态）需要及时响应，使用事件更合适
- 灵活性：子类可以根据实际情况选择合适的方式

**影响**:
- ViewComponent 需要明确哪些组件使用事件，哪些组件主动拉取
- 需要在文档中说明使用场景

### 5.2 为什么使用脏标记而不是事件？

**问题**: BaseComponent 的数据变化应该如何通知 ViewComponent？

**备选方案**:
- 方案A：使用事件机制（OnDataChanged）
- 方案B：使用脏标记机制（IsDirty）
- 方案C：在 Stage 更新时统一查询

**选择**: 方案B + 方案C（脏标记 + Stage 查询）

**理由**:
- 性能：避免事件系统的开销，使用 HashSet<int> 存储 ComponentId，性能高效
- 批量处理：Stage 可以批量查询所有 Entity 的脏组件，减少遍历次数
- 可控性：在 Stage 更新时统一处理，时机可控
- 简单性：不需要维护事件订阅关系，减少内存开销
- 集中管理：Entity 统一管理脏标记，BaseComponent 不需要存储状态

**影响**:
- Entity 需要维护脏组件 ID 集合（HashSet<int>）
- BaseComponent 需要保存 OwnerEntity 引用，用于通知
- Stage 需要在更新时查询脏组件
- 需要在同步后清除脏标记，避免重复处理

### 5.3 为什么在 Stage 层面查询？

**问题**: 脏组件的查询应该在哪里进行？

**备选方案**:
- 方案A：在 EntityView 中主动轮询
- 方案B：在 Stage 中统一查询

**选择**: 方案B

**理由**:
- 集中管理：Stage 统一管理所有 EntityView，便于批量处理
- 性能优化：一次遍历所有 Entity，比每个 EntityView 单独查询更高效
- 时机控制：在视图层更新时处理，确保逻辑层和视图层的同步时机

**影响**:
- Stage 需要维护 EntityView 的引用
- 需要在 Stage.Update() 中添加查询逻辑

---

## 6. 迁移指南

### 6.1 现有 ViewComponent 迁移步骤

1. **确定监听策略**
   - 分析组件数据变化频率
   - 频繁变化：不声明监听，在 Update 中主动拉取
   - 重要变化：声明监听，使用脏标记机制

2. **在 OnInitialize() 中获取 ComponentId**
   - 从 OwnerEntity 获取需要监听的组件实例
   - 保存组件的 ComponentId

3. **实现 GetWatchedComponentIds()**
   - 返回需要监听的 ComponentId 数组
   - 如果不需要监听，返回 null

4. **实现 SyncDataFromComponent(int componentId)**
   - 根据 ComponentId 从 OwnerEntity 获取组件
   - 提取数据并构造数据对象
   - 直接调用 OnSyncData(data)

5. **保持 OnSyncData() 实现**
   - 现有的 OnSyncData 实现保持不变
   - 确保数据格式兼容

### 6.2 现有 BaseComponent 迁移步骤

1. **识别关键数据变化点**
   - 找出需要通知 ViewComponent 的数据变化点
   - 例如：血量变化、状态变化、属性变化

2. **添加脏标记调用**
   - 在属性 setter 或关键方法中，通过 EntityId 获取 Entity
   - 调用 Entity.MarkComponentDirty(ComponentId) 标记组件为脏
   - 确保只在数据真正变化时调用

3. **测试验证**
   - 验证数据变化能正确设置脏标记
   - 验证 Stage 能正确查询并处理脏组件
   - 验证性能影响在可接受范围内

---

## 7. 典型示例

### 7.1 完整示例：HealthViewComponent

```csharp
public class HealthViewComponent : ViewComponent
{
    private float _currentHealth = 100f;
    private float _maxHealth = 100f;
    private bool _isAlive = true;
    
    // 需要监听的组件 ID
    private int _dynamicStatsComponentId;
    private int _derivedStatsComponentId;
    
    protected override void OnInitialize()
    {
        // 现有初始化代码...
        
        // 获取需要监听的组件 ID
        if (OwnerEntity != null)
        {
            var dynamicStats = OwnerEntity.GetComponent<DynamicStatsComponent>();
            var derivedStats = OwnerEntity.GetComponent<DerivedStatsComponent>();
            
            if (dynamicStats != null)
            {
                _dynamicStatsComponentId = dynamicStats.ComponentId;
            }
            if (derivedStats != null)
            {
                _derivedStatsComponentId = derivedStats.ComponentId;
            }
        }
    }
    
    // 声明需要监听的组件 ID
    public override int[] GetWatchedComponentIds()
    {
        var ids = new List<int>();
        if (_dynamicStatsComponentId != 0)
        {
            ids.Add(_dynamicStatsComponentId);
        }
        if (_derivedStatsComponentId != 0)
        {
            ids.Add(_derivedStatsComponentId);
        }
        return ids.ToArray();
    }
    
    // 根据 ComponentId 同步数据
    protected override void SyncDataFromComponent(int componentId)
    {
        if (OwnerEntity == null) return;
        
        // 根据 ComponentId 获取组件（虽然这里可能不需要，因为需要多个组件的数据）
        var component = OwnerEntity.GetComponentById(componentId);
        if (component == null) return;
        
        // 获取相关组件
        var dynamicStats = OwnerEntity.GetComponent<DynamicStatsComponent>();
        var derivedStats = OwnerEntity.GetComponent<DerivedStatsComponent>();
        
        if (dynamicStats != null && derivedStats != null)
        {
            var healthData = new HealthData(
                (float)dynamicStats.Get(DynamicResourceType.CURRENT_HP),
                (float)derivedStats.Get(StatType.HP),
                dynamicStats.Get(DynamicResourceType.CURRENT_HP) > 0
            );
            
            // 直接调用 OnSyncData
            OnSyncData(healthData);
        }
    }
    
    // 数据同步逻辑
    protected override void OnSyncData(object data)
    {
        if (data is HealthData healthData)
        {
            float previousHealth = _currentHealth;
            
            _currentHealth = healthData.CurrentHealth;
            _maxHealth = healthData.MaxHealth;
            _isAlive = healthData.IsAlive;
            
            // 处理血量变化逻辑...
        }
    }
}
```

### 7.2 BaseComponent 脏标记示例

```csharp
public partial class DynamicStatsComponent : BaseComponent
{
    private Dictionary<DynamicResourceType, FP> _resources = new Dictionary<DynamicResourceType, FP>();
    
    public void Set(DynamicResourceType type, FP value)
    {
        if (!_resources.ContainsKey(type) || _resources[type] != value)
        {
            _resources[type] = value;
            
            // 对于重要的资源变化（如血量），标记组件为脏
            if (type == DynamicResourceType.CURRENT_HP)
            {
                // 通过 EntityId 获取 Entity，然后调用 Entity 的方法
                // 具体实现取决于项目的架构
                // 示例：var entity = WorldManager.Instance?.GetEntity(EntityId);
                //       entity?.MarkComponentDirty(ComponentId);
            }
        }
    }
}
```

---

## 8. 注意事项

### 8.1 性能考虑

- **脏标记存储**：使用 HashSet<int> 存储 ComponentId，查找和插入都是 O(1)
- **脏标记查询频率**：Stage 每帧查询一次，避免过度查询
- **映射查找优化**：EntityView 使用 ComponentId 作为键，映射查找使用字典，时间复杂度 O(1)
- **批量处理**：Stage 批量查询所有 Entity 的脏组件，减少遍历次数
- **脏标记清除**：同步后立即清除，避免重复处理
- **组件查找优化**：Entity 根据 ComponentId 查找组件，需要遍历 Components，但脏组件数量通常较少

### 8.2 生命周期管理

- **脏组件集合清理**：确保在 Entity 销毁时清理脏组件集合
- **映射关系清理**：确保在 ViewComponent 移除时清理映射关系
- **空引用检查**：在同步处理中检查 OwnerEntity 是否为 null
- **脏标记重置**：确保在同步后清除脏标记，避免重复处理

### 8.3 向后兼容

- **现有代码兼容**：保持现有的 `OnSyncData` 抽象方法，确保现有代码不受影响
- **可选机制**：事件通知为可选机制，子类可以选择不使用
- **渐进式迁移**：可以逐步迁移现有 ViewComponent，不需要一次性全部修改

---

**返回**: [核心架构文档](../README.md)

