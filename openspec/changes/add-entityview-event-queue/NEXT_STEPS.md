# 下一步工作

## 已完成 ✅

1. ✅ Entity.ViewEventQueue 基础设施
2. ✅ Stage 事件轮询和分层处理
3. ✅ World/Entity 事件发布迁移
4. ✅ 编译验证通过

## 待实现 🔨

### 1. ViewComponent 事件注册机制（参考 Capability）

需要实现：

#### 1.1 修改 ViewComponent.cs
```csharp
public abstract class ViewComponent
{
    // 事件处理器映射
    private Dictionary<Type, Delegate> _viewEventHandlers = new Dictionary<Type, Delegate>();
    
    /// <summary>
    /// 注册视图事件处理器（子类重写）
    /// </summary>
    protected virtual void RegisterViewEventHandlers()
    {
        // 子类实现
    }
    
    /// <summary>
    /// 注册单个事件处理器
    /// </summary>
    protected void RegisterViewEventHandler<TEvent>(Action<TEvent> handler)
        where TEvent : struct
    {
        _viewEventHandlers[typeof(TEvent)] = handler;
    }
    
    /// <summary>
    /// 获取事件处理器映射（供 EntityView 访问）
    /// </summary>
    public Dictionary<Type, Delegate> GetViewEventHandlers() => _viewEventHandlers;
}
```

#### 1.2 修改 EntityView.cs
```csharp
public class EntityView
{
    // 事件类型 -> ViewComponent 列表映射
    private Dictionary<Type, List<ViewComponent>> _viewEventToComponents 
        = new Dictionary<Type, List<ViewComponent>>();
    
    /// <summary>
    /// 注册 ViewComponent 的事件处理器
    /// </summary>
    private void RegisterViewComponentEventHandlers(ViewComponent component)
    {
        var handlers = component.GetViewEventHandlers();
        foreach (var kvp in handlers)
        {
            var eventType = kvp.Key;
            if (!_viewEventToComponents.ContainsKey(eventType))
                _viewEventToComponents[eventType] = new List<ViewComponent>();
            _viewEventToComponents[eventType].Add(component);
        }
    }
    
    /// <summary>
    /// 取消注册 ViewComponent 的事件处理器
    /// </summary>
    private void UnregisterViewComponentEventHandlers(ViewComponent component)
    {
        var handlers = component.GetViewEventHandlers();
        foreach (var kvp in handlers)
        {
            var eventType = kvp.Key;
            if (_viewEventToComponents.ContainsKey(eventType))
            {
                _viewEventToComponents[eventType].Remove(component);
                if (_viewEventToComponents[eventType].Count == 0)
                    _viewEventToComponents.Remove(eventType);
            }
        }
    }
    
    /// <summary>
    /// 分发自定义视图事件到 ViewComponent
    /// 类似 CapabilitySystem.DispatchEventToEntity
    /// </summary>
    public void DispatchViewEventToComponents(Type eventType, object eventData)
    {
        if (!_viewEventToComponents.TryGetValue(eventType, out var components))
            return; // 没有 ViewComponent 监听此事件
        
        foreach (var component in components)
        {
            if (!component.IsEnabled) continue;
            
            var handlers = component.GetViewEventHandlers();
            if (handlers.TryGetValue(eventType, out var handler))
            {
                handler.DynamicInvoke(eventData);
            }
        }
    }
    
    // 在 AddViewComponent 中调用注册
    public void AddViewComponent(ViewComponent component)
    {
        // ... 现有代码 ...
        RegisterViewComponentEventHandlers(component);
    }
    
    // 在 RemoveViewComponent 中取消注册
    private void RemoveViewComponent(ViewComponent component)
    {
        // ... 现有代码 ...
        UnregisterViewComponentEventHandlers(component);
    }
}
```

#### 1.3 调整 ProcessEvent() 处理自定义事件
```csharp
public void ProcessEvent(ViewEvent evt)
{
    switch (evt.EventType)
    {
        case ViewEventType.SubArchetypeChanged:
            // EntityView 级别：自己处理
            ProcessEntityViewEvent_SubArchetypeChanged(evt);
            break;
            
        default:
            // 自定义事件：查找 ViewComponent 并分发
            if (evt.EventData != null)
            {
                var eventDataType = evt.EventData.GetType();
                DispatchViewEventToComponents(eventDataType, evt.EventData);
            }
            break;
    }
}
```

### 2. 去掉 ComponentDirty 相关代码

#### 2.1 修改 ViewEvents.cs
- ❌ 删除 `ComponentDirty` 枚举值

#### 2.2 修改 EntityView.cs
- ❌ 删除 `ProcessViewComponentEvent_ComponentDirty()` 方法
- ✅ 保留 `SyncDirtyComponents()` 方法（脏组件同步机制独立存在）

### 3. 示例：AnimationViewComponent

```csharp
public class AnimationViewComponent : ViewComponent
{
    protected override void RegisterViewEventHandlers()
    {
        // 注册需要监听的视图事件
        RegisterViewEventHandler<HitAnimationEvent>(OnHitAnimation);
        RegisterViewEventHandler<SkillAnimationEvent>(OnSkillAnimation);
    }
    
    private void OnHitAnimation(HitAnimationEvent evt)
    {
        PlayAnimation(evt.AnimationName);
    }
    
    private void OnSkillAnimation(SkillAnimationEvent evt)
    {
        PlayAnimation(evt.AnimationName);
    }
}
```

## 设计对比

### 之前（混淆）
```
视图事件队列包含：
- EntityCreated ✅
- EntityDestroyed ✅
- SubArchetypeChanged ✅
- ComponentDirty ❌ （不应该）
- WorldRollback ✅
```

### 之后（清晰）
```
视图事件队列（状态变化通知）：
- EntityCreated（Stage 级别）
- EntityDestroyed（Stage 级别）
- SubArchetypeChanged（EntityView 级别）
- WorldRollback（Stage 级别）
- 自定义事件（ViewComponent 级别，通过注册机制）

脏组件同步（独立机制）：
- Stage.SyncDirtyComponents()
- EntityView.SyncDirtyComponents(dirtyIds)
- ViewComponent.SyncDataFromComponent(componentId)
- ViewComponent.GetWatchedComponentIds()
```

## 优势

1. **职责分离**：
   - 事件队列：状态变化通知
   - 脏组件：数据同步

2. **设计一致**：
   - ViewComponent 事件注册 ≈ Capability 事件注册
   - EntityView 事件分发 ≈ CapabilitySystem 事件分发

3. **灵活性**：
   - ViewComponent 可以监听自定义视图事件
   - 不限于预定义的事件类型

4. **性能**：
   - 高频数据同步：脏组件机制（轮询）
   - 低频状态通知：事件队列（异步）

## 预估工作量

- 修改 ViewComponent.cs：1 小时
- 修改 EntityView.cs：1-2 小时
- 更新文档：0.5 小时
- 测试验证：1 小时

**总计**：3.5-4.5 小时

