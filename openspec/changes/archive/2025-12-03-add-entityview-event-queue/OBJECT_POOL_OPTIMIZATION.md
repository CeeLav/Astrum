# 对象池优化：避免重复注册事件回调

**优化日期**：2025-12-03  
**优化原因**：用户反馈 RegisterViewEventHandlers 有开销，应避免重复注册

---

## 优化前的问题

**原始逻辑**：
```csharp
public virtual void Initialize()
{
    // 每次初始化都重新注册
    RegisterViewEventHandlers();
}

public virtual void Destroy()
{
    // 返回对象池时清空注册
    _viewEventHandlers.Clear();
}
```

**问题**：
- ❌ 每次从对象池取出 ViewComponent 时都重新注册回调
- ❌ RegisterViewEventHandlers() 有开销（创建 Delegate，字典操作）
- ❌ 不符合对象池最佳实践（应尽量保留可复用状态）

---

## 优化后的方案

**核心思想**：
- ✅ 只在第一次注册回调
- ✅ 返回对象池时不清空回调
- ✅ 使用标志位 `_eventHandlersRegistered` 避免重复注册

### 实现

```csharp
public abstract class ViewComponent
{
    // 事件处理器映射
    private Dictionary<Type, Delegate> _viewEventHandlers = new Dictionary<Type, Delegate>();
    
    // 事件处理器是否已注册标志（对象池优化）
    private bool _eventHandlersRegistered = false;
    
    public virtual void Initialize()
    {
        // ... 其他初始化 ...
        
        // 对象池优化：只在第一次注册，避免重复注册开销
        if (!_eventHandlersRegistered)
        {
            RegisterViewEventHandlers();
            _eventHandlersRegistered = true;
        }
        
        // ... 子类初始化 ...
    }
    
    public virtual void Destroy()
    {
        // ... 子类销毁 ...
        
        // 注意：不清空 _viewEventHandlers 和 _eventHandlersRegistered
        // 对象池优化：避免下次初始化时重新注册回调
    }
}
```

---

## 生命周期示例

### 场景：ViewComponent 从对象池多次使用

```
第一次使用：
1. 从对象池取出（或新建）
2. Initialize()
   → _eventHandlersRegistered = false
   → RegisterViewEventHandlers() ✅ 执行
   → _eventHandlersRegistered = true
3. 使用...
4. Destroy()
   → 不清空 _viewEventHandlers ✅
   → 不重置 _eventHandlersRegistered ✅
5. 返回对象池

第二次使用：
1. 从对象池取出（同一个实例）
2. Initialize()
   → _eventHandlersRegistered = true
   → RegisterViewEventHandlers() ❌ 跳过
3. 使用...
4. Destroy()
   → 不清空 _viewEventHandlers ✅
5. 返回对象池

第N次使用：
1. 从对象池取出
2. Initialize()
   → RegisterViewEventHandlers() ❌ 跳过（标志位保护）
3. 使用...
```

---

## 性能对比

### 假设场景
- 100 个 EntityView，每个平均 3 个 ViewComponent
- 每个 ViewComponent 注册 2 个事件回调
- 战斗场景中 EntityView 平均重复使用 5 次

### 优化前
```
总注册次数 = 100 × 3 × 5 = 1500 次
每次注册开销 = 创建 Delegate (2个) + 字典操作 (2次)
总开销 = 1500 × 开销
```

### 优化后
```
总注册次数 = 100 × 3 × 1 = 300 次
（只在第一次注册，后续复用）
总开销 = 300 × 开销

节省 = 1200 次注册 = 80% 开销
```

---

## 对比其他对象池场景

### Unity GameObject Pool
```csharp
// 不重新设置固定属性
var obj = pool.Get();
obj.transform.localPosition = newPos;  // 重置可变状态
// obj.layer = ...;  // 不重置固定属性
```

### Entity Pool（Astrum）
```csharp
// 不重新创建 Capability（已经创建好）
var entity = entityPool.Get();
entity.Reset();  // 重置数据，不重新创建 Capability
```

### ViewComponent Pool（优化后）
```csharp
// 不重新注册事件回调（已经注册好）
var component = componentPool.Get();
component.Initialize();  // 重置状态，不重新注册回调
```

**一致的优化原则**：
- ✅ 固定的、可复用的状态保留
- ✅ 可变的、需重置的状态清空
- ✅ 避免重复创建/注册/初始化

---

## 注意事项

### 1. 标志位的作用
```csharp
_eventHandlersRegistered = false/true
```
- `false`：从未注册过，需要调用 RegisterViewEventHandlers()
- `true`：已经注册过，跳过注册

### 2. 何时重置标志位？
**不需要重置！**
- ViewComponent 实例在整个生命周期中只注册一次
- 即使返回对象池，下次取出仍然可用

### 3. 如果需要修改回调怎么办？
**场景：热更新、配置变更**

方案1：创建新的 ViewComponent 实例
```csharp
// 不推荐：修改现有实例的回调
// 推荐：创建新实例
```

方案2：提供强制重新注册方法（如果真的需要）
```csharp
public void ForceReregisterEventHandlers()
{
    _viewEventHandlers.Clear();
    _eventHandlersRegistered = false;
    RegisterViewEventHandlers();
    _eventHandlersRegistered = true;
}
```

---

## 使用示例

### 正常使用（无需改动）

```csharp
public class AnimationViewComponent : ViewComponent
{
    // 静态注册（类型级）
    static AnimationViewComponent()
    {
        ViewComponentEventRegistry.Instance.RegisterEventHandler(
            typeof(HitAnimationEvent), typeof(AnimationViewComponent));
    }
    
    // 实例注册（实例级）
    // 这个方法现在只会在第一次 Initialize 时调用
    protected override void RegisterViewEventHandlers()
    {
        RegisterViewEventHandler<HitAnimationEvent>(OnHitAnimation);
        RegisterViewEventHandler<SkillAnimationEvent>(OnSkillAnimation);
    }
    
    private void OnHitAnimation(HitAnimationEvent evt)
    {
        PlayAnimation(evt.AnimationName);
    }
}
```

**使用流程**：
```csharp
// 第一次
var anim = pool.Get<AnimationViewComponent>();
anim.Initialize();  // RegisterViewEventHandlers() ✅ 执行
anim.Destroy();
pool.Return(anim);

// 第二次（同一个实例）
var anim = pool.Get<AnimationViewComponent>();
anim.Initialize();  // RegisterViewEventHandlers() ❌ 跳过
anim.Destroy();
pool.Return(anim);
```

---

## 调试建议

### 验证优化效果

在 `RegisterViewEventHandlers()` 中添加日志：
```csharp
protected override void RegisterViewEventHandlers()
{
    Debug.Log($"[{GetType().Name}] 注册事件回调（应该只调用一次）");
    RegisterViewEventHandler<HitAnimationEvent>(OnHitAnimation);
}
```

**预期输出**（每个 ViewComponent 实例只输出一次）：
```
[AnimationViewComponent] 注册事件回调（应该只调用一次）
// ... 使用 ...
// ... 返回对象池 ...
// ... 再次使用 ...
// （不应该再次输出注册日志）
```

---

## 总结

✅ **优化效果**：
1. **性能提升**：减少 80% 的事件回调注册开销（假设平均复用 5 次）
2. **内存稳定**：避免重复创建 Delegate 和字典操作
3. **符合最佳实践**：与 Unity 对象池、Entity Pool 一致

✅ **实现简单**：
- 添加 1 个标志位 `_eventHandlersRegistered`
- 添加 1 个 if 判断
- 移除 Destroy() 中的清空逻辑

✅ **向后兼容**：
- 不影响现有代码
- 子类无需修改

🎯 **对象池黄金法则**：
> 固定的、可复用的状态保留；可变的、需重置的状态清空。

事件回调属于"固定的、可复用的状态"，应该保留！✨

