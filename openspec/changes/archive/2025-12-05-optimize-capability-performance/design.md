# Design: Capability 性能优化架构设计

## 概述

本设计文档详细说明如何优化高耗时 Capability 的性能，通过空间索引、对象池和查询缓存等技术将帧时间从 11.31ms 降至 < 3ms。

## 架构原则

1. **零拷贝原则** - 最小化对象分配和拷贝
2. **缓存优先** - 缓存频繁访问的数据
3. **空间换时间** - 使用索引结构加速查询
4. **渐进优化** - 分阶段实施，每阶段可独立验证

## Phase 1: 优化目标查询策略

### 问题分析

当前 `BattleStateCapability.FindNearestEnemy()` 每帧遍历所有实体：

```csharp
// 当前实现：O(n) 复杂度，每帧都执行
foreach (var kv in world.Entities)  // ← 每帧遍历所有实体
{
    var e = kv.Value;
    if (e == null || e.UniqueId == selfEntity.UniqueId) continue;
    if (e.GetComponent<RoleInfoComponent>() == null) continue;
    var pos = e.GetComponent<TransComponent>()?.Position ?? TSVector.zero;
    var d = (pos - selfPos).sqrMagnitude;
    if (d < best)
    {
        best = d;
        nearest = e;
    }
}
```

**性能瓶颈**:
- 有 N 个实体时，复杂度 O(N)
- **每帧**每个 AI 实体都执行一次，总复杂度 O(M*N)，M 为 AI 数量
- 100 个实体 × 50 个 AI = **5000 次迭代/帧**
- **根本问题**: 目标不会频繁变化，没必要每帧查询

### 解决方案：目标缓存 + BEPU 物理查询

采用两个优化策略：

#### 策略 1: 在 AIStateMachineComponent 中缓存当前目标（主要优化）

```csharp
// AIStateMachineComponent.cs - Component 存储数据
public class AIStateMachineComponent : IComponent
{
    // 已有字段
    public long CurrentTargetId { get; set; }
    
    // 新增：上次验证目标时的帧号（用于判断是否需要重新验证）
    public int LastTargetValidationFrame { get; set; } = -1;
}

// BattleStateCapability.cs - Capability 处理逻辑
public override void Tick(Entity entity)
{
    var fsm = GetComponent<AIStateMachineComponent>(entity);
    var currentFrame = entity.World?.CurFrame ?? 0;
    
    // 1. 检查是否有缓存的有效目标
    if (fsm.CurrentTargetId > 0)
    {
        var cachedTarget = entity.World.GetEntity(fsm.CurrentTargetId);
        if (cachedTarget != null && !cachedTarget.IsDead)
        {
            var dist = GetDistance(entity, cachedTarget);
            
            // 目标在合理范围内，继续使用（无需查询！）
            if (dist < RetargetDistance)
            {
                AttackTarget(entity, cachedTarget);
                fsm.LastTargetValidationFrame = currentFrame;
                return;  // ← 90% 的帧直接返回，零查询开销
            }
        }
    }
    
    // 2. 只在必要时重新查询（目标丢失或超出范围）
    var newTarget = FindNearestEnemy(...);
    fsm.CurrentTargetId = newTarget?.UniqueId ?? -1;  // ← 更新 Component 中的缓存
    fsm.LastTargetValidationFrame = currentFrame;
}
```

**效果**: 
- 90% 的帧只需检查距离，无需遍历实体
- 符合 ECC 架构：数据在 Component，逻辑在 Capability

#### 策略 2: 使用 BEPU 物理引擎的空间索引

项目已有 `BepuPhysicsWorld`，其内部使用 BEPU 的 `Space.BroadPhase` 作为空间索引：

```csharp
// 使用 BEPU 的空间查询（已有功能）
private Entity? FindNearestEnemy(World world, TSVector selfPos, Entity selfEntity)
{
    var physicsWorld = world.PhysicsWorld;
    if (physicsWorld == null) return null;
    
    // 使用 BEPU 的 BoundingBox 查询（O(k) 复杂度）
    var searchRadius = (FP)RetargetDistance;
    var aabb = new BoundingBox(
        min: selfPos - TSVector.one * searchRadius,
        max: selfPos + TSVector.one * searchRadius
    );
    
    // BEPU 的 BroadPhase 已经是空间索引（通常是 DynamicHierarchy）
    var nearby = physicsWorld.QueryAABB(aabb);
    
    Entity? nearest = null;
    FP bestDist = FP.MaxValue;
    
    foreach (var bepuEntity in nearby)
    {
        // 从 Tag 获取 Astrum Entity
        if (bepuEntity.Tag is Entity e && e.UniqueId != selfEntity.UniqueId)
        {
            var dist = (e.Position - selfPos).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                nearest = e;
            }
        }
    }
    
    return nearest;
}
```

### 实施步骤

1. **添加目标缓存机制** (主要优化)
   - 在 `BattleStateCapability` 添加 `_cachedTargets` 字典
   - 添加 `RetargetDistance` 常量（如 8.0 单位）
   - 修改 `Tick()` 逻辑：先检查缓存，仅在必要时查询

2. **使用 BEPU 物理查询** (次要优化)
   - 在 `BepuPhysicsWorld` 添加 `QueryAABB()` 方法
   - `FindNearestEnemy()` 改用物理查询替代遍历
   - 确保所有战斗实体已注册到物理世界

3. **清理缓存**
   - 实体销毁时从 `_cachedTargets` 移除
   - 切换状态时清理目标缓存

### 性能分析

| 场景 | 优化前 | 优化后 | 提升 |
|------|--------|--------|------|
| **缓存命中** (90% 的帧) | 遍历 100 实体 | **仅距离检查** | **~100x** |
| **缓存未命中** (10% 的帧) | O(100) 遍历 | O(5-10) BEPU 查询 | ~10-20x |
| **总体** | 7.08ms/帧 | **< 0.5ms/帧** | **~14x** |

### 关键改进点

✅ **无需创建新的空间索引系统** - 复用 BEPU 现有功能  
✅ **目标缓存是主要优化** - 90% 帧无需任何查询  
✅ **实现简单** - 约 50 行代码，无需改动 World  
✅ **内存开销小** - 仅一个 Dictionary<long, long>  

**预期效果**: BattleStateCapability 从 7.08ms 降至 **< 0.5ms** (14x 提升)

## Phase 2: 使用已有对象池复用 LSInput

### 问题分析

当前创建 `LSInput` 时没有使用对象池：

```csharp
// BattleStateCapability.cs
private LSInput CreateInput(Entity entity, ...)
{
    var input = LSInput.Create();  // ← isFromPool 默认 false，每次分配新对象
    input.PlayerId = entity.UniqueId;
    // ...
    return input;
}
```

### 解决方案：使用 ObjectPool.Instance

项目已有全局对象池 `ObjectPool.Instance`，`LSInput.Create()` 已支持对象池参数：

```csharp
// Generated/Message/gamemessages_C_2000.cs
public partial class LSInput : MessageObject
{
    public static LSInput Create(bool isFromPool = false)
    {
        return ObjectPool.Instance.Fetch(typeof(LSInput), isFromPool) as LSInput;
    }
}
```

### 使用方式

**方案 1：修改 Create 调用**（推荐）
```csharp
// 从对象池获取
private LSInput CreateInput(Entity entity, ...)
{
    var input = LSInput.Create(isFromPool: true);  // ← 启用对象池
    input.PlayerId = entity.UniqueId;
    // ...
    return input;
}

// 使用完后归还
public void SetInput(LSInput input)
{
    // 复制数据...
    ObjectPool.Instance.Recycle(input);  // ← 归还到池
}
```

**方案 2：修改默认参数**（如果适用所有场景）
```csharp
// 修改生成的代码模板，让 isFromPool 默认为 true
public static LSInput Create(bool isFromPool = true)  // ← 改为默认启用
```

### 需要修改的地方

所有调用 `LSInput.Create()` 的地方：
- `BattleStateCapability.CreateInput()`
- `MoveStateCapability.CreateInput()`
- `IdleStateCapability.CreateInput()`
- `ServerLSController.CreateDefaultInput()`
- 其他创建 LSInput 的地方

### 对象池回收

使用完 LSInput 后需要回收：
```csharp
// 在 LSInputComponent.SetInput() 中
public void SetInput(LSInput input)
{
    // 复制数据到内部字段
    this.PlayerId = input.PlayerId;
    this.Frame = input.Frame;
    // ...
    
    // 归还到对象池
    if (input is IPool && ((IPool)input).IsFromPool)
    {
        ObjectPool.Instance.Recycle(input);
    }
}
```

**预期效果**: 减少 ~600KB/s 的 GC 分配（60 FPS）

## Phase 3: 监控 GetComponent 性能

### 问题验证

需要验证 `GetComponent()` 是否真的是性能瓶颈：

```csharp
public override void Tick(Entity entity)
{
    var fsm = GetComponent<AIStateMachineComponent>(entity);  // 是否慢？
    var trans = GetComponent<TransComponent>(entity);         // 是否慢？
    var inputComp = GetComponent<LSInputComponent>(entity);   // 是否慢？
}
```

**假设**: GetComponent 本身应该很快（Dictionary 查询，O(1)），可能不是瓶颈。

### 解决方案：添加性能监控验证假设

在 `Capability<T>` 基类的 `GetComponent()` 中添加 ProfileScope：

```csharp
public abstract class Capability<T> where T : Capability<T>, new()
{
    protected TComponent GetComponent<TComponent>(Entity entity) 
        where TComponent : class, IComponent
    {
        #if ENABLE_PROFILER
        using (new ProfileScope($"GetComponent<{typeof(TComponent).Name}>"))
        #endif
        {
            return entity.GetComponent<TComponent>();
        }
    }
}
```

### 性能监控目标

通过 Unity Profiler 深度分析验证：

| 指标 | 预期值 | 说明 |
|------|--------|------|
| 单次调用 | < 0.01ms | 如果满足，无需缓存 |
| 总调用次数 | ~100-200/帧 | 统计实际调用频率 |
| 总耗时 | < 0.5ms | 所有 GetComponent 总计 |

### 决策树

```
收集性能数据
├─ GetComponent 总耗时 < 0.5ms
│  └─ ✅ 无需优化，移除此 Phase
└─ GetComponent 总耗时 > 1ms
   └─ ⚠️ 考虑缓存方案（重新评估）
```

**预期结论**: GetComponent 本身很快，无需缓存，此 Phase 可能直接跳过

## Phase 4: LINQ 优化

### 问题分析

`ActionCapability` 中大量使用 LINQ：

```csharp
// 产生临时集合分配
var candidates = actionComponent.ActionCandidates
    .Where(a => a.Priority > 0)
    .OrderByDescending(a => a.Priority)
    .ToList();  // ← 临时 List 分配
```

### 解决方案：手动循环 + 预分配缓冲区

```csharp
public class ActionCapability : Capability<ActionCapability>
{
    // 预分配工作缓冲区（避免重复分配）
    private List<ActionCandidate> _candidateBuffer = new List<ActionCandidate>(16);
    
    private void SelectActionFromCandidates(Entity entity)
    {
        var actionComponent = GetComponent<ActionComponent>(entity);
        
        // 清空缓冲区（不释放容量）
        _candidateBuffer.Clear();
        
        // 手动过滤和排序
        foreach (var candidate in actionComponent.ActionCandidates)
        {
            if (candidate.Priority > 0)
                _candidateBuffer.Add(candidate);
        }
        
        // 手动排序（避免 OrderByDescending 的分配）
        _candidateBuffer.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        
        // 处理结果...
    }
}
```

**预期效果**: 消除 ActionCapability 中 ~50KB/帧 的 LINQ 分配

## 实施顺序

1. ✅ **Phase 1: 空间索引** (1-2 天)
   - 影响最大，优先级最高
   - 独立模块，风险可控

2. ✅ **Phase 2: 对象池** (0.5-1 天)
   - 简单直接，立即见效
   - 可与 Phase 3 并行

3. ✅ **Phase 3: 查询缓存** (0.5-1 天)
   - 基础设施改进
   - 可与 Phase 2 并行

4. ✅ **Phase 4: LINQ 优化** (1-2 天)
   - 代码量较大
   - 需要仔细测试

**总计**: 3-6 天

## 测试策略

### 性能测试

```csharp
[Test]
public void TestCapabilityPerformance()
{
    // 1. 创建测试场景（100 个实体）
    var world = CreateTestWorld(entityCount: 100);
    
    // 2. 预热
    for (int i = 0; i < 100; i++)
        world.Update();
    
    // 3. 测量性能
    var stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < 1000; i++)
        world.Update();
    stopwatch.Stop();
    
    // 4. 验证性能目标
    var avgFrameTime = stopwatch.ElapsedMilliseconds / 1000.0;
    Assert.Less(avgFrameTime, 16.0, "平均帧时间应 < 16ms");
}
```

### 正确性测试

确保优化不改变游戏逻辑：

```csharp
[Test]
public void TestOptimizationCorrectness()
{
    // 1. 记录优化前的行为
    var world1 = CreateTestWorld();
    var results1 = SimulateFrames(world1, 1000);
    
    // 2. 应用优化
    EnableOptimizations(world1);
    
    // 3. 记录优化后的行为
    var world2 = CreateTestWorld();
    var results2 = SimulateFrames(world2, 1000);
    
    // 4. 验证结果一致
    Assert.AreEqual(results1, results2, "优化前后行为应完全一致");
}
```

## 风险缓解

### 风险 1: 空间索引维护成本

**缓解措施**:
- 仅在位置实际改变时更新索引
- 使用脏标记避免重复更新
- Profile 索引维护开销，确保 < 0.5ms

### 风险 2: 对象池内存泄漏

**缓解措施**:
- 限制池大小（MAX_POOL_SIZE）
- 定期检查池中对象数量
- 添加内存泄漏检测

### 风险 3: 缓存一致性

**缓解措施**:
- 在组件添加/移除时失效缓存
- 使用代理模式自动检测变化
- 严格测试缓存失效逻辑

## 性能预算

| 系统 | 优化前 | 目标 | 预算 |
|------|--------|------|------|
| BattleStateCapability | 7.08ms | <1ms | 6% |
| ActionCapability | 2.40ms | <1ms | 6% |
| 其他 Capability | 1.83ms | <1ms | 6% |
| **LSUpdater 总计** | 13.23ms | **<5ms** | **30%** |

总帧预算：16ms (60 FPS)
- Logic: 5ms (30%)
- View: 6ms (40%)
- Other: 5ms (30%)

