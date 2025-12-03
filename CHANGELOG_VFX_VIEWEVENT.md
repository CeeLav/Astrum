# VFX 特效系统迁移到 ViewEvent 机制

**日期**：2025-12-03  
**状态**：✅ 完成并编译通过

---

## 改动概述

将受击特效（VFX）的播放从 EventSystem（同步）改为 ViewEvent 队列（异步），进一步解耦逻辑层和视图层。

---

## 修改文件

### 1. ViewEvents.cs（新增事件类型）
**路径**：`AstrumProj/Assets/Script/AstrumLogic/Events/ViewEvents.cs`

**新增**：
```csharp
/// <summary>
/// VFX 触发事件数据
/// 用于在视图层播放视觉特效
/// </summary>
public struct VFXTriggerEvent
{
    public string ResourcePath;           // 特效资源路径
    public TSVector PositionOffset;       // 位置偏移
    public TSVector Rotation;             // 旋转
    public float Scale;                   // 缩放
    public float PlaybackSpeed;           // 播放速度
    public bool FollowCharacter;          // 是否跟随角色
    public bool Loop;                     // 是否循环播放
}
```

**改动**：
- 添加 `using TrueSync;` 支持 TSVector

---

### 2. VFXViewComponent.cs（新增视图组件）
**路径**：`AstrumProj/Assets/Script/AstrumView/Components/VFXViewComponent.cs`

**功能**：
- 管理实体上的视觉特效播放
- 通过 ViewEvent 机制接收逻辑层的特效触发请求
- 自动清理已完成的特效实例

**核心特性**：
```csharp
// 静态注册（类型级，只执行一次）
static VFXViewComponent()
{
    ViewComponentEventRegistry.Instance.RegisterEventHandler(
        typeof(VFXTriggerEvent), 
        typeof(VFXViewComponent));
}

// 实例注册（实例级，第一次初始化时执行）
protected override void RegisterViewEventHandlers()
{
    RegisterViewEventHandler<VFXTriggerEvent>(OnVFXTrigger);
}

// 事件处理器
private void OnVFXTrigger(VFXTriggerEvent evt)
{
    PlayVFX(evt);  // 播放特效
}
```

**生命周期管理**：
- `OnUpdate()`: 清理已完成的特效实例
- `OnDestroy()`: 清理所有特效实例
- `StopAllLoopingVFX()`: 停止所有循环特效

**VFX 播放逻辑**：
- ✅ 通过 VFXManager 统一管理特效
- ✅ 使用 VFXTriggerEventData 构造事件
- ✅ VFXManager 自动处理加载、实例化、生命周期
- ✅ 支持跟随角色、位置偏移、旋转、缩放、播放速度
- ✅ 使用 instanceId 跟踪特效，便于停止和清理

---

### 3. HitReactionCapability.cs（改用 ViewEvent）
**路径**：`AstrumProj/Assets/Script/AstrumLogic/Capabilities/HitReactionCapability.cs`

**之前（同步，使用 EventSystem）**：
```csharp
var triggerData = new VFXTriggerEventData
{
    EntityId = entity.UniqueId,
    ResourcePath = evt.VisualEffectPath,
    PositionOffset = positionOffset,
    // ...
};

EventSystem.Instance.Publish(triggerData);  // 同步调用
```

**之后（异步，使用 ViewEvent）**：
```csharp
var vfxEvent = new VFXTriggerEvent
{
    ResourcePath = evt.VisualEffectPath,
    PositionOffset = positionOffset,
    Rotation = TSVector.zero,
    Scale = 1f,
    PlaybackSpeed = 1f,
    FollowCharacter = true,
    Loop = false
};

// 通过 ViewEvent 队列传递到视图层（异步，不阻塞逻辑层）
entity.QueueViewEvent(new ViewEvent(
    ViewEventType.CustomViewEvent, 
    vfxEvent, 
    entity.World.CurFrame
));
```

**优势**：
- ✅ 不阻塞逻辑层
- ✅ 不直接依赖 EventSystem
- ✅ 统一使用 ViewEvent 机制
- ✅ 为多线程做准备

---

## 架构改进

### 之前的流程（同步）
```
HitReactionCapability.PlayHitVFX()
  ↓ (同步调用)
EventSystem.Publish(VFXTriggerEventData)
  ↓ (立即回调)
某个视图层监听器接收
  ↓
播放特效
```

**问题**：
- ❌ 逻辑层直接调用视图层
- ❌ 阻塞逻辑层执行
- ❌ 不利于多线程

### 现在的流程（异步）
```
HitReactionCapability.PlayHitVFX()
  ↓ (异步入队)
Entity.QueueViewEvent(VFXTriggerEvent)
  ↓ [队列，不阻塞]
[等待下一帧]
  ↓
Stage.ProcessViewEvents()
  ↓
EntityView.ProcessEvent(CustomViewEvent)
  ↓
EntityView.DispatchViewEventToComponents()
  ↓ (查询全局映射)
VFXViewComponent.OnVFXTrigger(evt)
  ↓
PlayVFX() - 播放特效
```

**优势**：
- ✅ 完全解耦逻辑层和视图层
- ✅ 异步处理，不阻塞逻辑层
- ✅ 统一使用 ViewEvent 机制
- ✅ 符合 ViewComponent 事件注册模式
- ✅ 为多线程做准备

---

## 使用示例

### 逻辑层触发特效
```csharp
// 在 Capability 中触发特效
var vfxEvent = new VFXTriggerEvent
{
    ResourcePath = "VFX/Hit/Blood_Splash",
    PositionOffset = new TSVector(0, 1, 0),
    Rotation = TSVector.zero,
    Scale = 1.2f,
    PlaybackSpeed = 1.0f,
    FollowCharacter = false,
    Loop = false
};

entity.QueueViewEvent(new ViewEvent(
    ViewEventType.CustomViewEvent, 
    vfxEvent, 
    entity.World.CurFrame
));
```

### 视图层自动处理
```csharp
// VFXViewComponent 自动接收事件并播放特效
// 无需手动订阅，通过事件注册机制自动分发
```

---

## 设计一致性

### 与 EntityView Event Queue 完全一致

| 层级 | 事件类型 | 处理方式 |
|------|---------|----------|
| Stage | EntityCreated, EntityDestroyed, WorldRollback | Stage 直接处理 |
| EntityView | SubArchetypeChanged | EntityView 处理 |
| **ViewComponent** | **VFXTriggerEvent**（CustomViewEvent） | **通过事件注册机制分发** |

### 与 Capability 事件注册模式一致

| 机制 | Capability | ViewComponent |
|------|-----------|---------------|
| **静态注册** | Capability 类型注册事件 | ViewComponent 类型注册事件 |
| **全局映射** | CapabilitySystem._eventToHandlers | ViewComponentEventRegistry |
| **实例处理** | Capability.OnEvent() | ViewComponent.OnVFXTrigger() |

---

## 性能影响

### 延迟
- **影响**：特效播放延迟 1 帧（~16ms @ 60fps）
- **可接受性**：✅ 特效本身有延迟，1 帧不可察觉

### 内存
- **新增**：VFXViewComponent 实例（每个需要播放特效的实体）
- **优化**：对象池优化，事件回调只注册一次

### CPU
- **节省**：不阻塞逻辑层
- **新增**：事件队列处理（已优化，批量处理）

---

## 迁移检查清单

- [x] 定义 VFXTriggerEvent 事件数据结构
- [x] 创建 VFXViewComponent 视图组件
- [x] 实现事件注册机制（静态 + 实例）
- [x] 实现 VFX 播放逻辑
- [x] 修改 HitReactionCapability 使用 ViewEvent
- [x] 编译验证通过
- [ ] 运行游戏测试特效播放
- [ ] 验证特效生命周期管理
- [ ] 检查性能影响

---

## 后续工作

### 其他特效系统迁移
可以用类似方式迁移其他特效系统：
- 技能特效
- 死亡特效
- Buff/Debuff 特效
- 环境特效

### 示例
```csharp
// 定义新的事件类型
public struct SkillVFXEvent { /* ... */ }

// 在 VFXViewComponent 中注册
static VFXViewComponent()
{
    ViewComponentEventRegistry.Instance.RegisterEventHandler(
        typeof(VFXTriggerEvent), typeof(VFXViewComponent));
    ViewComponentEventRegistry.Instance.RegisterEventHandler(
        typeof(SkillVFXEvent), typeof(VFXViewComponent));  // 新增
}

protected override void RegisterViewEventHandlers()
{
    RegisterViewEventHandler<VFXTriggerEvent>(OnVFXTrigger);
    RegisterViewEventHandler<SkillVFXEvent>(OnSkillVFX);  // 新增
}
```

---

## 总结

🎉 **VFX 特效系统成功迁移到 ViewEvent 机制！**

**主要成就**：
- ✅ 逻辑层和视图层完全解耦（特效部分）
- ✅ 异步处理，不阻塞逻辑层
- ✅ 统一使用 ViewEvent 队列机制
- ✅ 符合 ViewComponent 事件注册模式
- ✅ 向后兼容，易于扩展

**设计一致性**：
- ✅ 与 EntityView Event Queue 完全一致
- ✅ 与 Capability 事件注册模式一致
- ✅ 与对象池优化模式一致

**编译状态**：✅ 成功

**下一步**：运行游戏测试特效播放

---

**文件数量**：
- 新增：1 个（VFXViewComponent.cs）
- 修改：2 个（ViewEvents.cs, HitReactionCapability.cs）

**代码行数**：
- 新增：~300 行
- 修改：~50 行

