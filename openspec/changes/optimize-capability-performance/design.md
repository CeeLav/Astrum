# Design: Capability 性能优化架构设计

## 概述

本设计文档详细说明如何优化高耗时 Capability 的性能，通过空间索引、对象池和查询缓存等技术将帧时间从 11.31ms 降至 < 3ms。

## 架构原则

1. **零拷贝原则** - 最小化对象分配和拷贝
2. **缓存优先** - 缓存频繁访问的数据
3. **空间换时间** - 使用索引结构加速查询
4. **渐进优化** - 分阶段实施，每阶段可独立验证

## Phase 1: 空间索引系统

### 问题分析

当前 `BattleStateCapability.FindNearestEnemy()` 每帧遍历所有实体：

```csharp
// 当前实现：O(n) 复杂度
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
- 每帧每个 AI 实体都执行一次，总复杂度 O(M*N)，M 为 AI 数量
- 100 个实体 × 50 个 AI = 5000 次迭代/帧

### 解决方案：空间哈希

使用 2D 空间哈希将世界划分为网格：

```
网格大小：10×10 单位
查询范围：仅检查相邻网格（最多 9 个格子）
```

```csharp
public class SpatialIndexSystem
{
    private const int CELL_SIZE = 10; // 网格大小
    private Dictionary<(int, int), HashSet<Entity>> _grid;
    
    // O(1) 插入
    public void Insert(Entity entity, TSVector position);
    
    // O(1) 移除
    public void Remove(Entity entity);
    
    // O(k) 查询，k << N（仅查询附近网格）
    public IEnumerable<Entity> QueryNearby(TSVector position, FP radius);
    
    // O(1) 更新位置
    public void UpdatePosition(Entity entity, TSVector oldPos, TSVector newPos);
}
```

### 集成方式

1. **World 初始化时创建空间索引**
```csharp
public class World
{
    public SpatialIndexSystem SpatialIndex { get; private set; }
    
    public void Initialize()
    {
        SpatialIndex = new SpatialIndexSystem();
        // ...
    }
}
```

2. **实体位置变化时更新索引**
```csharp
public class MovementCapability : Capability<MovementCapability>
{
    public override void Tick(Entity entity)
    {
        var trans = GetComponent<TransComponent>(entity);
        var oldPos = trans.Position;
        
        // 更新位置...
        trans.Position = newPos;
        
        // 通知空间索引
        entity.World?.SpatialIndex?.UpdatePosition(entity, oldPos, newPos);
    }
}
```

3. **BattleStateCapability 使用空间索引**
```csharp
private Entity? FindNearestEnemy(World world, TSVector selfPos, Entity selfEntity)
{
    // 优化后：仅查询附近实体
    var nearby = world.SpatialIndex.QueryNearby(selfPos, (FP)LoseTargetDistance);
    
    Entity? nearest = null;
    FP best = FP.MaxValue;
    
    foreach (var e in nearby)  // ← 数量大幅减少
    {
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
    return nearest;
}
```

### 性能分析

| 场景 | 优化前 | 优化后 | 提升 |
|------|--------|--------|------|
| 10 实体 | O(10) | O(1-2) | ~5x |
| 100 实体 | O(100) | O(3-5) | ~20x |
| 1000 实体 | O(1000) | O(5-10) | ~100x |

**预期效果**: BattleStateCapability 从 7.08ms 降至 < 1ms

## Phase 2: 对象池优化

### 问题分析

当前每帧创建新的 `LSInput` 对象：

```csharp
// BattleStateCapability.cs
private LSInput CreateInput(Entity entity, ...)
{
    var input = LSInput.Create();  // ← 每次分配新对象
    input.PlayerId = entity.UniqueId;
    // ...
    return input;
}
```

### 解决方案：LSInput 对象池

```csharp
public class LSInputPool
{
    private static readonly Stack<LSInput> _pool = new Stack<LSInput>(64);
    private const int MAX_POOL_SIZE = 128;
    
    public static LSInput Rent()
    {
        if (_pool.Count > 0)
            return _pool.Pop();
        return LSInput.Create();
    }
    
    public static void Return(LSInput input)
    {
        if (_pool.Count < MAX_POOL_SIZE)
        {
            // 重置对象状态
            input.Reset();
            _pool.Push(input);
        }
    }
}
```

### 使用方式

```csharp
// 使用对象池
private LSInput CreateInput(Entity entity, ...)
{
    var input = LSInputPool.Rent();  // ← 从池中获取
    input.PlayerId = entity.UniqueId;
    // ...
    return input;
}

// 使用完后归还
public void SetInput(LSInput input)
{
    // 复制数据...
    LSInputPool.Return(input);  // ← 归还到池
}
```

**预期效果**: 减少 ~600KB/s 的 GC 分配（60 FPS）

## Phase 3: 查询缓存

### 问题分析

当前每帧重复查询相同组件：

```csharp
public override void Tick(Entity entity)
{
    var fsm = GetComponent<AIStateMachineComponent>(entity);  // 第 1 次查询
    var trans = GetComponent<TransComponent>(entity);         // 第 2 次查询
    var inputComp = GetComponent<LSInputComponent>(entity);   // 第 3 次查询
    // ...
}
```

### 解决方案：组件缓存

```csharp
public abstract class Capability<T> where T : Capability<T>, new()
{
    // 缓存层
    private Dictionary<long, Dictionary<Type, IComponent>> _componentCache 
        = new Dictionary<long, Dictionary<Type, IComponent>>();
    
    protected TComponent GetComponentCached<TComponent>(Entity entity) 
        where TComponent : class, IComponent
    {
        if (!_componentCache.TryGetValue(entity.UniqueId, out var cache))
        {
            cache = new Dictionary<Type, IComponent>();
            _componentCache[entity.UniqueId] = cache;
        }
        
        var type = typeof(TComponent);
        if (!cache.TryGetValue(type, out var component))
        {
            component = entity.GetComponent<TComponent>();
            cache[type] = component;
        }
        
        return component as TComponent;
    }
    
    // 实体销毁时清除缓存
    public virtual void OnEntityDestroyed(Entity entity)
    {
        _componentCache.Remove(entity.UniqueId);
    }
}
```

### 使用方式

```csharp
public override void Tick(Entity entity)
{
    // 使用缓存版本
    var fsm = GetComponentCached<AIStateMachineComponent>(entity);
    var trans = GetComponentCached<TransComponent>(entity);
    var inputComp = GetComponentCached<LSInputComponent>(entity);
    // ...
}
```

**预期效果**: 减少 30-50% 的组件查询开销

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

