# Spec: Performance Optimization Best Practices

> **Status**: New Specification  
> **Category**: Non-Functional Requirements  
> **Version**: 1.0

## Overview

本规范定义 Astrum 项目中性能优化的最佳实践、性能预算和验证方法。所有性能关键代码必须遵守这些准则。

## ADDED Requirements

### Requirement: 热路径代码必须满足性能预算

所有性能关键代码（Capability.Tick, System.Update 等）MUST 满足预定义的性能预算。单帧总时间 95% SHALL < 16ms，99% SHALL < 22ms。

**Rationale**: 性能预算确保游戏在目标硬件上流畅运行。超出预算的代码必须优化。

**Priority**: P0

#### Scenario: 单帧总时间预算（60 FPS）

**Given** 游戏运行在目标硬件上  
**When** 测量连续 1000 帧的性能  
**Then** 95% 的帧总时间应 < 16ms  
**And** 99% 的帧总时间应 < 22ms  
**And** 平均帧时间应 < 14ms

#### Scenario: 各系统时间预算分配

**Given** 单帧总预算 16ms  
**Then** 应按以下分配：
- **Logic (AstrumLogic)**: < 5ms (30%)
  - World.Update: < 3ms
  - LSUpdater: < 3ms (包含所有 Capability)
  - Systems: < 2ms
- **View (AstrumView)**: < 6ms (40%)
  - Stage.Update: < 2ms
  - EntityView.UpdateView: < 2ms
  - Animation: < 1ms
  - VFX: < 1ms
- **Other (Rendering, Physics, etc.)**: < 5ms (30%)

#### Scenario: 单个 Capability 性能预算

**Given** 一个 Capability 的 Tick() 方法在 100 实体场景  
**Then** 单次调用应 < 0.5ms  
**And** 如果所有实体都有该 Capability，总计 < 5ms  
**And** 超出预算时应优化或拆分为多个 Capability

### Requirement: 热路径代码必须最小化 GC 分配

热路径代码 MUST 最小化托管堆分配，10 秒窗口内总分配 SHALL < 100KB。MUST NOT 使用 LINQ、string 拼接、临时集合等产生分配的操作。

**Rationale**: GC 会导致帧率波动和卡顿。热路径应接近零分配。

**Priority**: P0

#### Scenario: 每帧 GC 分配预算

**Given** 游戏正常运行  
**When** 测量 10 秒（600 帧）的 GC 分配  
**Then** 总分配应 < 100KB（平均 < 170 bytes/帧）  
**And** 单帧峰值分配应 < 1KB  
**And** 不应有持续增长的内存泄漏

#### Scenario: 热路径中禁止的分配模式

**Given** 性能关键代码（Capability.Tick, System.Update 等）  
**Then** 禁止以下操作：
- ❌ LINQ 操作（Where, Select, OrderBy 等）
- ❌ string 拼接和格式化（string.Format, $"..."）
- ❌ 临时数组/List 分配（new T[], new List<T>()）
- ❌ 装箱操作（值类型转 object）
- ❌ 闭包捕获（lambda 捕获外部变量）
- ❌ foreach 遍历会产生枚举器的集合（Dictionary 等）

#### Scenario: 推荐的零分配模式

**Given** 需要临时存储数据  
**Then** 应使用以下模式：
- ✅ 对象池（ObjectPool）
- ✅ 预分配缓冲区（预分配 List,重复使用）
- ✅ struct 而非 class（值类型）
- ✅ Span<T>/ArraySegment<T>（无复制切片）
- ✅ for 循环替代 foreach（避免枚举器）
- ✅ StringBuilder 替代 string 拼接

### Requirement: 频繁查询应使用缓存或索引结构

频繁的数据查询 SHALL 使用缓存或索引结构优化。空间查询 MUST 使用空间索引，组件查询 MUST 使用缓存，集合查询 MUST 使用哈希表或字典。

**Rationale**: O(N) 查询在大量数据时性能极差。使用索引可将复杂度降至 O(1) 或 O(log N)。

**Priority**: P0

#### Scenario: 空间查询使用空间索引

**Given** 需要查询某位置附近的实体  
**When** 实体数量 > 20  
**Then** 必须使用空间索引（空间哈希、四叉树等）  
**And** 查询时间应与总实体数无关  
**And** 查询时间应 < 0.01ms（在 100 实体场景）

#### Scenario: 组件查询使用缓存

**Given** 同一个 Capability 多次访问同一组件  
**When** 在单帧内重复调用 GetComponent<T>()  
**Then** 应缓存第一次查询的结果  
**And** 后续访问应 < 0.001ms（几乎零开销）  
**And** 缓存应在组件变化时自动失效

#### Scenario: 集合查询使用哈希表或字典

**Given** 需要频繁查找集合中的元素  
**When** 集合大小 > 10  
**Then** 应使用 Dictionary 或 HashSet 而非 List  
**And** 查找时间应为 O(1) 而非 O(N)  
**And** 使用 `TryGetValue()` 避免两次查找

### Requirement: 所有优化必须经过性能验证

所有性能优化 MUST 使用 Unity Profiler 或自动化测试验证效果。SHALL 记录优化前后的性能数据，目标提升 > 30%。

**Rationale**: 没有测量的优化可能无效甚至负优化。必须用数据证明优化效果。

**Priority**: P0

#### Scenario: 优化前后性能对比

**Given** 一段需要优化的代码  
**When** 实施优化  
**Then** 必须记录优化前的性能基准  
**And** 优化后必须再次测量性能  
**And** 应记录提升百分比（目标 > 30%）  
**And** 如果提升 < 10%，应考虑是否值得增加复杂度

#### Scenario: 使用 Unity Profiler 验证

**Given** 优化的代码  
**When** 在 Unity Editor 中运行  
**Then** 必须在 Unity Profiler 中验证：
- Deep Profile 模式查看详细调用栈
- 确认目标方法的 Self Time 减少
- 确认 GC Alloc 减少（Memory Profiler）
- 运行至少 1000 帧获取稳定数据

#### Scenario: 性能测试自动化

**Given** 性能关键的系统  
**Then** 应创建自动化性能测试：
```csharp
[Test]
[Performance]
public void TestCapabilityPerformance()
{
    // Arrange: 创建测试场景
    var world = CreateWorldWith100Entities();
    
    // Act: 预热 + 测量
    for (int i = 0; i < 100; i++) world.Update(); // 预热
    var stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < 1000; i++) world.Update();
    stopwatch.Stop();
    
    // Assert: 验证性能目标
    var avgMs = stopwatch.ElapsedMilliseconds / 1000.0;
    Assert.Less(avgMs, 16.0, "平均帧时间应 < 16ms");
}
```

### Requirement: 优化不应降低代码可读性和可维护性

性能优化 SHALL 保持代码可读性，MUST 添加清晰注释说明优化原因和权衡。复杂优化 SHALL 提供禁用开关用于调试。

**Rationale**: 过度优化会导致代码难以理解和维护。需要在性能和可读性之间平衡。

**Priority**: P1

#### Scenario: 优化代码应有清晰的注释

**Given** 一段性能优化的代码  
**Then** 应包含以下注释：
- 为什么需要这样优化（性能瓶颈）
- 优化前后的性能数据
- 优化的权衡和限制
- 如果有非直观的实现，解释原理

#### Scenario: 优化应有禁用选项

**Given** 复杂的性能优化（如缓存、对象池）  
**Then** 应提供禁用开关用于调试：
```csharp
public class Capability<T>
{
    #if ENABLE_COMPONENT_CACHE
    protected TComponent GetComponentCached<TComponent>(...) { }
    #else
    protected TComponent GetComponentCached<TComponent>(Entity entity)
        => entity.GetComponent<TComponent>();  // 回退到简单实现
    #endif
}
```

#### Scenario: 避免过早优化

**Given** 新功能开发  
**Then** 应遵循以下原则：
1. **首先**: 实现正确的功能
2. **其次**: Profile 找出真正的瓶颈
3. **最后**: 只优化瓶颈（不优化非瓶颈）
4. **验证**: 优化后重新测试正确性

## Performance Measurement Tools

### Unity Profiler

- **Deep Profiling**: 查看详细调用栈
- **Hierarchy View**: 分析时间分布
- **Timeline View**: 查看帧时间波动
- **Memory Profiler**: 分析 GC 分配和内存泄漏

### ASProfiler

- **逻辑层监控**: 不依赖 Unity 的性能监控
- **ProfileScope**: 自定义监控点
```csharp
using (new ProfileScope("MyMethod"))
{
    // 被监控的代码
}
```

### Benchmark.NET (Optional)

- **微基准测试**: 测试小段代码的性能
- **统计分析**: 平均值、中位数、标准差
- **对比测试**: 优化前后对比

## Common Performance Anti-Patterns

### Anti-Pattern 1: 全量遍历查找

```csharp
// ❌ 错误：O(N) 复杂度
Entity FindEntity(long id)
{
    foreach (var entity in world.Entities.Values)
        if (entity.UniqueId == id) return entity;
    return null;
}

// ✅ 正确：O(1) 复杂度
Entity FindEntity(long id)
{
    world.Entities.TryGetValue(id, out var entity);
    return entity;
}
```

### Anti-Pattern 2: 热路径中的 LINQ

```csharp
// ❌ 错误：每次都分配新 List
var sorted = items.Where(i => i.IsValid)
                  .OrderByDescending(i => i.Priority)
                  .ToList();

// ✅ 正确：重用预分配缓冲区
_buffer.Clear();
foreach (var item in items)
    if (item.IsValid) _buffer.Add(item);
_buffer.Sort((a, b) => b.Priority.CompareTo(a.Priority));
```

### Anti-Pattern 3: 重复的组件查询

```csharp
// ❌ 错误：每帧 3 次查询
public override void Tick(Entity entity)
{
    var trans = entity.GetComponent<TransComponent>();  // 查询 1
    var pos = entity.GetComponent<TransComponent>().Position;  // 查询 2
    Move(entity.GetComponent<TransComponent>());  // 查询 3
}

// ✅ 正确：缓存查询结果
public override void Tick(Entity entity)
{
    var trans = GetComponentCached<TransComponent>(entity);  // 仅查询 1 次
    var pos = trans.Position;
    Move(trans);
}
```

### Anti-Pattern 4: 字符串拼接

```csharp
// ❌ 错误：每次都分配新字符串
string log = "Entity " + id + " at " + pos.ToString();

// ✅ 正确：使用 StringBuilder 或避免字符串操作
// 方案 1: StringBuilder
var sb = StringBuilderPool.Rent();
sb.Append("Entity ").Append(id).Append(" at ").Append(pos.x);
var log = sb.ToString();
StringBuilderPool.Return(sb);

// 方案 2: 结构化日志（推荐）
ASLogger.Instance.Debug("Entity {0} at ({1}, {2})", id, pos.x, pos.z);
```

## Success Metrics

### 性能目标达成标准

- [ ] 95% 帧时间 < 16ms
- [ ] GC 分配 < 100KB/帧
- [ ] 无内存泄漏（30 分钟测试）
- [ ] Unity Profiler 验证通过
- [ ] 性能测试套件通过

### 代码质量标准

- [ ] 所有优化有性能数据支撑
- [ ] 所有优化有正确性测试
- [ ] 优化代码有清晰注释
- [ ] 代码审查通过
- [ ] 符合项目编码规范

