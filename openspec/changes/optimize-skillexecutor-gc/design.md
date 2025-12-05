# Design: SkillExecutorCapability 0 GC 优化架构设计

## 概述

本设计文档详细说明如何将 `SkillExecutorCapability` 的 GC 分配从 125.4 KB/帧 优化到 < 1 KB/帧，参考已完成的 ActionCapability 优化案例。

## 架构原则

1. **零分配原则** - 热路径中避免任何对象分配
2. **对象复用** - 使用对象池和预分配缓冲区
3. **避免 LINQ** - 使用 for 循环替代 LINQ 操作
4. **参考已有模式** - 遵循项目中已验证的优化模式

## Phase 1: 消除 LINQ ToList() 分配

### 问题分析

当前实现每帧都会创建新的 List：

```csharp
// SkillExecutorCapability.cs:90-92
var triggersAtFrame = triggerEffects
    .Where(t => t.IsFrameInRange(currentFrame))
    .ToList();  // ← 每帧创建新 List，产生大量 GC
```

**性能瓶颈**:
- 有 N 个触发事件时，每帧创建新 List
- 即使没有触发事件，也会创建空 List
- 多个实体同时释放技能时，GC 成倍增长
- **预估**: 这个操作占 GC 分配的 **60-70%**

### 解决方案：预分配缓冲区

参考 ActionCapability 的优化模式：

```csharp
public class SkillExecutorCapability : Capability<SkillExecutorCapability>
{
    // 预分配工作缓冲区（避免重复分配）
    private List<TriggerFrameInfo> _triggerBuffer = new List<TriggerFrameInfo>(16);
    
    private void ProcessFrame(Entity caster, ActionInfo actionInfo, int currentFrame)
    {
        var triggerEffects = actionInfo.GetTriggerEffects();
        if (triggerEffects == null || triggerEffects.Count == 0)
            return;
        
        // 清空缓冲区（不释放容量）
        _triggerBuffer.Clear();
        
        // 手动过滤（避免 LINQ）
        int count = triggerEffects.Count;
        for (int i = 0; i < count; i++)
        {
            var trigger = triggerEffects[i];
            if (trigger.IsFrameInRange(currentFrame))
            {
                _triggerBuffer.Add(trigger);
            }
        }
        
        if (_triggerBuffer.Count == 0)
            return;
        
        ASLogger.Instance.Debug($"Processing {_triggerBuffer.Count} triggers at frame {currentFrame}");
        
        // 使用 for 循环遍历（避免枚举器 GC）
        int triggerCount = _triggerBuffer.Count;
        for (int i = 0; i < triggerCount; i++)
        {
            ProcessTrigger(caster, actionInfo, _triggerBuffer[i]);
        }
    }
}
```

**效果**:
- **ToList() GC**: 从 ~80 KB/帧 → **0 B**
- **枚举器 GC**: 从 ~10 KB/帧 → **0 B**
- **总计**: ~90 KB/帧 减少

### 缓冲区容量选择

根据典型技能配置：
- 大多数技能有 1-5 个触发事件
- 复杂技能最多 10-15 个触发事件
- **初始容量**: 16（2^4，内存对齐友好）
- **自动扩容**: 超过 16 时自动扩容（List 内部机制）

## Phase 2: 优化循环遍历

### 问题分析

当前使用 foreach 遍历，可能产生枚举器 GC：

```csharp
// SkillExecutorCapability.cs:286-295
foreach (var target in hits)  // ← 可能产生枚举器 GC
{
    if (trigger.EffectIds != null)
    {
        foreach (var effectId in trigger.EffectIds)  // ← 可能产生枚举器 GC
        {
            TriggerSkillEffect(caster, target, effectId, trigger);
        }
    }
}
```

### 解决方案：使用 for 循环

参考 ActionCapability 的优化模式：

```csharp
private void HandleCollisionTrigger(Entity caster, ActionInfo actionInfo, TriggerFrameInfo trigger)
{
    // ... 碰撞检测逻辑 ...
    
    var hits = hitSystem.QueryHits(caster, shape, filter);
    
    if (hits.Count == 0)
    {
        ASLogger.Instance.Debug($"[SkillExecutorCapability] Collision trigger hit 0 targets");
        return;
    }
    
    // ✅ 使用 for 循环遍历 hits
    int hitCount = hits.Count;
    for (int i = 0; i < hitCount; i++)
    {
        var target = hits[i];
        
        if (trigger.EffectIds != null)
        {
            // ✅ 使用 for 循环遍历 effectIds
            int effectCount = trigger.EffectIds.Length;
            for (int j = 0; j < effectCount; j++)
            {
                var effectId = trigger.EffectIds[j];
                TriggerSkillEffect(caster, target, effectId, trigger);
            }
        }
    }
}
```

**效果**:
- **枚举器 GC**: 从 ~5 KB/帧 → **0 B**

## Phase 3: 复用 CollisionFilter 对象

### 问题分析

每次碰撞检测都创建新的 CollisionFilter 和 HashSet：

```csharp
// SkillExecutorCapability.cs:258-262
var filter = new CollisionFilter
{
    ExcludedEntityIds = new HashSet<long> { caster.UniqueId },  // ← 每次创建新 HashSet
    OnlyEnemies = false
};
```

**性能瓶颈**:
- 每次碰撞触发都创建新对象
- HashSet 初始化有额外开销
- **预估**: 占 GC 分配的 **15-20%**

### 解决方案：实例字段复用

```csharp
public class SkillExecutorCapability : Capability<SkillExecutorCapability>
{
    // 预分配工作缓冲区
    private List<TriggerFrameInfo> _triggerBuffer = new List<TriggerFrameInfo>(16);
    
    // 复用的 CollisionFilter（避免重复分配）
    private CollisionFilter _collisionFilter = new CollisionFilter
    {
        ExcludedEntityIds = new HashSet<long>(),
        OnlyEnemies = false
    };
    
    private void HandleCollisionTrigger(Entity caster, ActionInfo actionInfo, TriggerFrameInfo trigger)
    {
        // ... 碰撞形状逻辑 ...
        
        // ✅ 复用 CollisionFilter，只更新排除列表
        _collisionFilter.ExcludedEntityIds.Clear();
        _collisionFilter.ExcludedEntityIds.Add(caster.UniqueId);
        _collisionFilter.OnlyEnemies = false;
        
        var hits = hitSystem.QueryHits(caster, shape, _collisionFilter);
        
        // ... 处理命中逻辑 ...
    }
}
```

**效果**:
- **CollisionFilter GC**: 从 ~20 KB/帧 → **0 B**
- **HashSet GC**: 从 ~5 KB/帧 → **0 B**
- **总计**: ~25 KB/帧 减少

### 注意事项

1. **线程安全**: SkillExecutorCapability 在单线程中执行，无需考虑线程安全
2. **状态重置**: 每次使用前必须清空 ExcludedEntityIds
3. **多实例**: 每个 SkillExecutorCapability 实例有独立的 _collisionFilter

## Phase 4: VFX 事件对象池（可选优化）

### 问题分析

VFX 触发时创建两个事件对象：

```csharp
// SkillExecutorCapability.cs:190-231
var eventData = new VFXTriggerEventData { ... };  // ← 创建新对象
var vfxEvent = new VFXTriggerEvent { ... };       // ← 创建新对象
```

**性能瓶颈**:
- 每次 VFX 触发都创建新对象
- **预估**: 占 GC 分配的 **5-10%**
- **优先级**: 较低（VFX 触发频率相对较低）

### 解决方案：实现对象池

参考 ActionCommand 的对象池实现：

```csharp
// VFXTriggerEventData.cs
public partial class VFXTriggerEventData : IPool
{
    [MemoryPackIgnore]
    public bool IsFromPool { get; set; }
    
    public static VFXTriggerEventData Create()
    {
        var instance = ObjectPool.Instance.Fetch<VFXTriggerEventData>();
        return instance;
    }
    
    public void Reset()
    {
        EntityId = 0;
        ResourcePath = string.Empty;
        PositionOffset = TSVector.zero;
        Rotation = TSVector.zero;
        Scale = 1.0f;
        PlaybackSpeed = 1.0f;
        FollowCharacter = false;
        Loop = false;
        InstanceId = 0;
    }
}

// VFXTriggerEvent.cs
public partial class VFXTriggerEvent : IPool
{
    [MemoryPackIgnore]
    public bool IsFromPool { get; set; }
    
    public static VFXTriggerEvent Create()
    {
        var instance = ObjectPool.Instance.Fetch<VFXTriggerEvent>();
        return instance;
    }
    
    public void Reset()
    {
        ResourcePath = string.Empty;
        PositionOffset = TSVector.zero;
        Rotation = TSVector.zero;
        Scale = 1.0f;
        PlaybackSpeed = 1.0f;
        FollowCharacter = false;
        Loop = false;
    }
}
```

### 使用对象池

```csharp
private void ProcessVFXTrigger(Entity caster, TriggerFrameInfo trigger)
{
    // ... 检查是否应该触发 ...
    
    // ✅ 从对象池获取
    var eventData = VFXTriggerEventData.Create();
    eventData.EntityId = caster.UniqueId;
    eventData.ResourcePath = trigger.VFXResourcePath ?? string.Empty;
    // ... 设置其他字段 ...
    
    var vfxEvent = VFXTriggerEvent.Create();
    vfxEvent.ResourcePath = eventData.ResourcePath;
    // ... 设置其他字段 ...
    
    // 通过 ViewEvent 队列传递到视图层
    caster.QueueViewEvent(new ViewEvent(
        ViewEventType.CustomViewEvent,
        vfxEvent,
        caster.World.CurFrame
    ));
    
    // ✅ 归还到对象池（eventData 不再需要）
    if (eventData.IsFromPool)
    {
        ObjectPool.Instance.Recycle(eventData);
    }
    
    // 注意：vfxEvent 由 ViewEvent 系统负责回收
}
```

**效果**:
- **VFX 事件 GC**: 从 ~10 KB/帧 → **0 B**

### 决策树

```
VFX 触发频率分析
├─ 频率 < 10 次/帧
│  └─ ⚠️ 优化收益较小，可以跳过
└─ 频率 > 20 次/帧
   └─ ✅ 实施对象池优化
```

**建议**: 先完成 Phase 1-3，测试后根据实际 VFX 触发频率决定是否实施 Phase 4

## 实施顺序

1. ✅ **Phase 1: ToList() 优化** (0.5 天)
   - 影响最大，优先级最高
   - 预期减少 ~90 KB/帧 GC

2. ✅ **Phase 2: 循环优化** (0.5 天)
   - 简单直接，立即见效
   - 预期减少 ~5 KB/帧 GC

3. ✅ **Phase 3: CollisionFilter 复用** (0.5 天)
   - 中等收益
   - 预期减少 ~25 KB/帧 GC

4. ⚠️ **Phase 4: VFX 对象池** (1 天，可选)
   - 根据实际需求决定
   - 预期减少 ~10 KB/帧 GC

**总计**: 1.5-2.5 天

## 测试策略

### 性能测试

```csharp
[Test]
public void TestSkillExecutorPerformance()
{
    // 1. 创建测试场景（10 个实体释放技能）
    var world = CreateTestWorld();
    var entities = CreateEntitiesWithSkills(world, count: 10);
    
    // 2. 预热
    for (int i = 0; i < 100; i++)
        world.Update();
    
    // 3. 测量 GC 分配
    long gcBefore = GC.GetTotalMemory(true);
    
    for (int i = 0; i < 1000; i++)
        world.Update();
    
    long gcAfter = GC.GetTotalMemory(false);
    long gcDelta = gcAfter - gcBefore;
    
    // 4. 验证 GC 目标
    Assert.Less(gcDelta / 1000.0, 1024, "平均 GC 应 < 1 KB/帧");
}
```

### 正确性测试

```csharp
[Test]
public void TestSkillExecutorCorrectness()
{
    // 1. 创建测试场景
    var world = CreateTestWorld();
    var caster = CreateEntityWithSkill(world);
    var target = CreateTargetEntity(world);
    
    // 2. 释放技能
    caster.GetComponent<ActionComponent>().PlayAction("Skill_Attack");
    
    // 3. 模拟多帧
    for (int i = 0; i < 60; i++)
    {
        world.Update();
    }
    
    // 4. 验证效果触发
    var damageComp = target.GetComponent<DamageComponent>();
    Assert.IsNotNull(damageComp, "目标应该受到伤害");
    Assert.Greater(damageComp.TotalDamage, 0, "伤害应该 > 0");
}
```

### 内存泄漏测试

```csharp
[Test]
public void TestNoMemoryLeak()
{
    var world = CreateTestWorld();
    var entities = CreateEntitiesWithSkills(world, count: 10);
    
    // 运行 10000 帧（约 3 分钟）
    for (int i = 0; i < 10000; i++)
    {
        world.Update();
        
        // 每 1000 帧检查一次内存
        if (i % 1000 == 0)
        {
            GC.Collect();
            long memory = GC.GetTotalMemory(true);
            ASLogger.Instance.Debug($"Frame {i}: Memory = {memory / 1024} KB");
        }
    }
    
    // 验证内存稳定
    GC.Collect();
    long finalMemory = GC.GetTotalMemory(true);
    Assert.Less(finalMemory, 100 * 1024 * 1024, "最终内存应 < 100 MB");
}
```

## 风险缓解

### 风险 1: 缓冲区容量不足

**缓解措施**:
- 初始容量设为 16（覆盖 95% 的情况）
- List 会自动扩容（2x 增长）
- 添加日志监控最大使用量

### 风险 2: CollisionFilter 状态污染

**缓解措施**:
- 每次使用前强制清空 ExcludedEntityIds
- 添加 Debug 断言检查状态
- 单元测试覆盖多次调用场景

### 风险 3: 对象池回收错误

**缓解措施**:
- 严格检查 IsFromPool 标志
- 添加对象池泄漏检测
- 限制对象池最大容量

## 性能预算

| 系统 | 优化前 | 目标 | 预算 |
|------|--------|------|------|
| SkillExecutorCapability | 3.09ms / 125.4 KB | <2ms / <1 KB | 12% |
| 其他 Capability | ~10ms / ~50 KB | <8ms / <50 KB | 50% |
| **LSUpdater 总计** | ~13ms / ~200 KB | **<10ms / <60 KB** | **62%** |

总帧预算：16ms (60 FPS)
- Logic: 10ms (62%)
- View: 4ms (25%)
- Other: 2ms (13%)

## 关键改进点

✅ **无需引入新系统** - 使用现有对象池和预分配模式  
✅ **ToList() 优化是主要收益** - 预期减少 70% GC  
✅ **实现简单** - 约 100 行代码修改  
✅ **内存开销小** - 仅增加两个实例字段  
✅ **参考已验证模式** - 遵循 ActionCapability 优化案例  

**预期效果**: SkillExecutorCapability 从 125.4 KB 降至 **< 1 KB** (~99% 减少)

