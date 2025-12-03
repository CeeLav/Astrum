# 全局注册机制优化

**优化日期**：2025-12-03  
**优化原因**：用户反馈每个 EntityView 创建时都需要重新建立映射太麻烦

---

## 优化前的问题

**原始设计**：
```csharp
// EntityView.cs
private Dictionary<Type, List<ViewComponent>> _viewEventToComponents;

private void RegisterViewComponentEventHandlers(ViewComponent component)
{
    // 每个 EntityView 创建时都要重新建立映射
    // 100 个 EntityView = 100 次建立映射
    // 内存浪费，性能开销
}
```

**问题**：
- ❌ 每个 EntityView 都要维护一份映射副本
- ❌ EntityView 创建时都要遍历 ViewComponent 建立映射
- ❌ 内存浪费（N 个 EntityView = N 份映射）

---

## 优化后的方案

**全局注册机制**（参考 CapabilitySystem）：

### 1. ViewComponentEventRegistry（全局单例）

```csharp
public class ViewComponentEventRegistry
{
    // 全局映射：事件类型 -> ViewComponent 类型列表
    // 所有 EntityView 共享，只建立一次
    private Dictionary<Type, List<Type>> _eventTypeToComponentTypes;
    
    public void RegisterEventHandler(Type eventType, Type componentType)
    {
        // ViewComponent 类型在静态构造函数中注册
    }
    
    public List<Type> GetComponentTypesForEvent(Type eventType)
    {
        // EntityView 分发时查询全局映射
    }
}
```

### 2. ViewComponent 静态注册

```csharp
public class AnimationViewComponent : ViewComponent
{
    // 静态注册（类型级，只执行一次）
    static AnimationViewComponent()
    {
        ViewComponentEventRegistry.Instance.RegisterEventHandler(
            typeof(HitAnimationEvent), 
            typeof(AnimationViewComponent));
        ViewComponentEventRegistry.Instance.RegisterEventHandler(
            typeof(SkillAnimationEvent), 
            typeof(AnimationViewComponent));
    }
    
    // 实例注册（实例级，每个实例都要注册）
    protected override void RegisterViewEventHandlers()
    {
        RegisterViewEventHandler<HitAnimationEvent>(OnHitAnimation);
        RegisterViewEventHandler<SkillAnimationEvent>(OnSkillAnimation);
    }
    
    private void OnHitAnimation(HitAnimationEvent evt) { /* ... */ }
}
```

### 3. EntityView 分发逻辑

```csharp
public class EntityView
{
    // 不再维护实例级映射！
    
    private void DispatchViewEventToComponents(Type eventType, object eventData)
    {
        // 1. 查询全局映射：哪些 ViewComponent 类型监听此事件
        var componentTypes = ViewComponentEventRegistry.Instance
            .GetComponentTypesForEvent(eventType);
        
        if (componentTypes == null) return;
        
        // 2. 检查当前 EntityView 是否有对应的 ViewComponent 实例
        foreach (var componentType in componentTypes)
        {
            var component = _viewComponents
                .FirstOrDefault(c => c.GetType() == componentType);
            
            if (component != null && component.IsEnabled)
            {
                // 3. 调用实例的事件处理器
                component.InvokeEventHandler(eventType, eventData);
            }
        }
    }
}
```

---

## 对比 CapabilitySystem

**完全一致的设计模式**：

```
CapabilitySystem:
- 全局映射：_eventToHandlers (EventType -> List<CapabilityType>)
- 静态注册：Capability 类型注册事件处理器
- 实例分发：检查 entity.CapabilityStates，调用对应 Capability

ViewComponentEventRegistry:
- 全局映射：_eventTypeToComponentTypes (EventType -> List<ComponentType>)
- 静态注册：ViewComponent 类型注册事件处理器
- 实例分发：检查 entityView._viewComponents，调用对应 ViewComponent
```

---

## 性能对比

### 内存开销

| 方案 | 100 个 EntityView | 说明 |
|------|-------------------|------|
| **优化前** | 100 × 映射大小 | 每个 EntityView 维护映射副本 |
| **优化后** | 1 × 映射大小 | 全局共享，所有 EntityView 共享一份 |

**节省**：~99% 内存（假设每个映射 1KB，节省 ~99KB）

### CPU 开销

| 操作 | 优化前 | 优化后 |
|------|--------|--------|
| **EntityView 创建** | O(M×N) | O(1) |
| | M = 事件类型数，N = ViewComponent 数 | 无需建立映射 |
| **事件分发** | O(1) | O(N) |
| | 直接查本地映射 | 查询全局映射 + 遍历本地实例 |

**权衡**：
- ✅ EntityView 创建更高效（更常见）
- ⚠️ 事件分发略微慢一点（但可接受，N 通常很小）

---

## 使用示例

### 示例 1：动画 ViewComponent

```csharp
public class AnimationViewComponent : ViewComponent
{
    // ====== 静态注册（类型级，只执行一次）======
    static AnimationViewComponent()
    {
        var registry = ViewComponentEventRegistry.Instance;
        registry.RegisterEventHandler(typeof(HitAnimationEvent), 
            typeof(AnimationViewComponent));
        registry.RegisterEventHandler(typeof(SkillAnimationEvent), 
            typeof(AnimationViewComponent));
        registry.RegisterEventHandler(typeof(DeathAnimationEvent), 
            typeof(AnimationViewComponent));
    }
    
    // ====== 实例注册（实例级，每个实例都要注册）======
    protected override void RegisterViewEventHandlers()
    {
        RegisterViewEventHandler<HitAnimationEvent>(OnHitAnimation);
        RegisterViewEventHandler<SkillAnimationEvent>(OnSkillAnimation);
        RegisterViewEventHandler<DeathAnimationEvent>(OnDeathAnimation);
    }
    
    // ====== 事件处理器 ======
    private void OnHitAnimation(HitAnimationEvent evt)
    {
        PlayAnimation(evt.AnimationName);
        Debug.Log($"播放受击动画: {evt.AnimationName}");
    }
    
    private void OnSkillAnimation(SkillAnimationEvent evt)
    {
        PlayAnimation(evt.AnimationName);
        Debug.Log($"播放技能动画: {evt.AnimationName}");
    }
    
    private void OnDeathAnimation(DeathAnimationEvent evt)
    {
        PlayAnimation(evt.AnimationName);
        Debug.Log($"播放死亡动画: {evt.AnimationName}");
    }
}
```

### 示例 2：触发事件

```csharp
// 逻辑层
public class ActionCapability : Capability<ActionCapability>
{
    private void OnActionStart(string animationName)
    {
        // 触发视图事件
        var animEvent = new SkillAnimationEvent
        {
            AnimationName = animationName,
            PlaySpeed = 1.0f
        };
        
        Entity.QueueViewEvent(new ViewEvent(
            ViewEventType.CustomViewEvent, 
            animEvent, 
            World.CurFrame
        ));
    }
}
```

**事件流**：
```
ActionCapability.OnActionStart()
  → Entity.QueueViewEvent(CustomViewEvent, SkillAnimationEvent)
  → [异步队列]
  → Stage.ProcessViewEvents()
  → EntityView.ProcessEvent(CustomViewEvent)
  → EntityView.DispatchViewEventToComponents(typeof(SkillAnimationEvent), eventData)
    → 查询全局映射: AnimationViewComponent 监听 SkillAnimationEvent
    → 检查本地实例: entityView 是否有 AnimationViewComponent?
    → 调用: AnimationViewComponent.OnSkillAnimation(evt)
```

---

## 设计原则

### 1. 两层注册

**类型级注册**（静态）：
- 在静态构造函数中调用 `ViewComponentEventRegistry.RegisterEventHandler`
- 声明：**哪些 ViewComponent 类型**监听**哪些事件类型**
- 作用：全局映射，所有 EntityView 共享

**实例级注册**（实例）：
- 在 `RegisterViewEventHandlers()` 中调用 `RegisterViewEventHandler<TEvent>`
- 声明：**这个 ViewComponent 实例**的**事件处理器方法**
- 作用：实例映射，每个 ViewComponent 实例维护自己的处理器

### 2. 分发流程

```
事件到达 EntityView
  ↓
查询全局映射（EventType → List<ComponentType>）
  ↓
遍历 ComponentType 列表
  ↓
检查 EntityView 是否有对应实例
  ↓
调用实例的事件处理器
```

### 3. 与 CapabilitySystem 一致

| CapabilitySystem | ViewComponentEventRegistry |
|------------------|---------------------------|
| `_eventToHandlers` | `_eventTypeToComponentTypes` |
| `Capability.RegisterEventHandlers()` | `ViewComponent.RegisterViewEventHandlers()` |
| `DispatchEventToEntity()` | `DispatchViewEventToComponents()` |
| 检查 `entity.CapabilityStates` | 检查 `entityView._viewComponents` |

---

## 总结

✅ **优势**：
1. **内存优化**：全局映射只建立一次，所有 EntityView 共享
2. **性能优化**：EntityView 创建时无需建立映射
3. **设计一致**：与 CapabilitySystem 完全一致
4. **代码简洁**：EntityView 不再维护映射，逻辑更清晰

⚠️ **权衡**：
- 事件分发时需要遍历 ViewComponent 实例（但通常数量很少，可接受）

🎯 **结论**：这是一个更优雅、更高效的设计！完美契合项目架构风格。

