# 设计优化：事件机制和脏组件分离

## 设计调整原因

用户反馈后识别的问题：
1. ❌ **混淆职责**：将脏组件同步和事件通知混在一起
2. ❌ **缺少注册机制**：ViewComponent 没有像 Capability 一样的事件注册机制
3. ❌ **分发不清晰**：ViewComponent 如何接收事件不明确

## 优化后的设计

### 1. 两个独立机制

#### 机制 A：脏组件同步（数据同步，高频）
```
职责：Component 数据变化时同步到 ViewComponent
频率：每帧可能有大量变化
机制：轮询 + 脏标记

流程：
Stage.Update()
  → SyncDirtyComponents()
    → 遍历 Entity.GetDirtyComponentIds()
    → EntityView.SyncDirtyComponents(dirtyIds)
      → ViewComponent.SyncDataFromComponent(componentId)

ViewComponent API：
- GetWatchedComponentIds()：声明监听哪些 Component
- SyncDataFromComponent(componentId)：同步数据
```

#### 机制 B：视图事件队列（状态通知，低频）
```
职责：实体状态变化、子原型变化等通知
频率：低频，事件驱动
机制：异步队列 + 事件注册

流程：
Entity.QueueViewEvent()
  → [异步队列]
  → Stage.ProcessViewEvents()
    → 分层处理：
      - Stage 级别：EntityCreated, EntityDestroyed, WorldRollback
      - EntityView 级别：SubArchetypeChanged
      - ViewComponent 级别：CustomViewEvent（通过注册机制分发）

ViewComponent API（新增）：
- RegisterViewEventHandlers()：注册事件处理器
- RegisterViewEventHandler<TEvent>(handler)：注册单个事件
```

### 2. ViewComponent 事件注册机制

**参考设计**：Capability 的事件注册

#### Capability 的实现（参考）
```csharp
// HitReactionCapability.cs
protected override void RegisterEventHandlers()
{
    RegisterEventHandler<HitReactionEvent>(OnHitReaction);
}

private void OnHitReaction(Entity entity, HitReactionEvent evt)
{
    // 处理受击事件
}
```

#### ViewComponent 的实现（新增）
```csharp
// AnimationViewComponent.cs
protected override void RegisterViewEventHandlers()
{
    // 注册需要监听的视图事件
    RegisterViewEventHandler<HitAnimationEvent>(OnHitAnimation);
    RegisterViewEventHandler<SkillAnimationEvent>(OnSkillAnimation);
}

private void OnHitAnimation(HitAnimationEvent evt)
{
    // 处理受击动画
    PlayAnimation(evt.AnimationName);
}

private void OnSkillAnimation(SkillAnimationEvent evt)
{
    // 处理技能动画
    PlayAnimation(evt.AnimationName);
}
```

#### EntityView 的分发逻辑（参考 CapabilitySystem）
```csharp
// EntityView.cs
// 事件类型 -> ViewComponent 列表映射
private Dictionary<Type, List<ViewComponent>> _viewEventToComponents 
    = new Dictionary<Type, List<ViewComponent>>();

private void DispatchViewEventToComponents(Type eventType, object eventData)
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
```

## 对比

### 之前的设计（混淆）

```
ViewEventType:
- EntityCreated ✅
- EntityDestroyed ✅
- SubArchetypeChanged ✅
- ComponentDirty ❌（错误，这是脏组件的职责）
- WorldRollback ✅

ViewComponent 接收事件：
- ❌ 没有明确的机制
- ❌ 通过 ProcessViewComponentEvent_ComponentDirty？
```

### 优化后的设计（清晰）

```
ViewEventType:
- EntityCreated（Stage 级别）
- EntityDestroyed（Stage 级别）
- SubArchetypeChanged（EntityView 级别）
- WorldRollback（Stage 级别）
- CustomViewEvent（ViewComponent 级别，通过 EventData 类型区分）

ViewComponent 接收事件：
- ✅ RegisterViewEventHandlers() 注册
- ✅ RegisterViewEventHandler<TEvent>(handler) 类型安全
- ✅ EntityView 维护映射并分发
- ✅ 与 Capability 设计一致
```

## 优势

### 1. 职责清晰
- **脏组件**：高频数据同步，轮询机制，ViewComponent.GetWatchedComponentIds()
- **视图事件**：低频状态通知，异步队列，ViewComponent.RegisterViewEventHandlers()

### 2. 设计一致
```
Capability 事件处理：
- RegisterEventHandlers()
- CapabilitySystem.DispatchEventToEntity()
- _eventToHandlers 映射

ViewComponent 事件处理（新增）：
- RegisterViewEventHandlers()
- EntityView.DispatchViewEventToComponents()
- _viewEventToComponents 映射
```

### 3. 类型安全
```csharp
// 编译期检查
RegisterViewEventHandler<HitAnimationEvent>(OnHitAnimation);

private void OnHitAnimation(HitAnimationEvent evt)
{
    // evt 类型明确，无需类型转换
}
```

### 4. 灵活扩展
- ViewComponent 可以监听任意自定义事件类型
- 不限于预定义的 ViewEventType
- 通过 CustomViewEvent + EventData 类型实现

## 实施计划

### 已完成 ✅
1. Entity.ViewEventQueue 基础设施
2. Stage 分层事件处理
3. World/Entity 事件发布迁移
4. 去掉 ComponentDirty 事件类型
5. EntityView.DispatchViewEventToComponents() 骨架

### 待实现 🔨
1. ViewComponent.RegisterViewEventHandlers() 机制
2. ViewComponent.RegisterViewEventHandler<TEvent>() 方法
3. ViewComponent.GetViewEventHandlers() 方法
4. EntityView 注册/取消注册逻辑
5. EntityView._viewEventToComponents 映射
6. 完善 DispatchViewEventToComponents() 实现

### 预估工作量
- ViewComponent 基类修改：1 小时
- EntityView 映射和分发：1-2 小时
- 测试验证：1 小时
- 文档更新：0.5 小时

**总计**：3.5-4.5 小时

## 示例场景

### 场景 1：受击动画
```
逻辑层：
HitReactionCapability 处理受击
  → entity.QueueViewEvent(CustomViewEvent, new HitAnimationEvent { ... })

视图层：
Stage.ProcessViewEvents()
  → EntityView.ProcessEvent(CustomViewEvent)
    → DispatchViewEventToComponents(typeof(HitAnimationEvent), eventData)
      → AnimationViewComponent.OnHitAnimation(evt)
        → 播放受击动画
```

### 场景 2：血量变化
```
逻辑层：
DynamicStatsComponent.Set(HP, newValue)
  → entity.MarkComponentDirty(componentId)

视图层：
Stage.SyncDirtyComponents()
  → EntityView.SyncDirtyComponents(dirtyIds)
    → HealthViewComponent.SyncDataFromComponent(componentId)
      → 更新血条显示
```

**分离明确**：
- 动画播放：事件机制（低频，状态变化）
- 血条更新：脏组件机制（高频，数据同步）

## 总结

通过分离事件机制和脏组件同步：

✅ **职责清晰**：两个机制各司其职  
✅ **设计一致**：ViewComponent 事件注册 ≈ Capability 事件注册  
✅ **性能优化**：高频用轮询，低频用事件  
✅ **灵活扩展**：自定义事件类型  
✅ **类型安全**：编译期检查

这是一个更优雅、更符合项目架构的设计！🎯

