# Spec Delta: Capability System Performance Enhancements

## MODIFIED Requirements

### Requirement: Capability 组件查询应支持缓存机制

Capability SHALL 提供组件查询缓存机制，避免每帧重复查询相同组件。缓存 MUST 在组件变化时自动失效，确保数据一致性。

**Rationale**: 当前每帧重复查询相同组件导致不必要的性能开销。通过缓存频繁访问的组件可显著减少查询时间。

**Priority**: P0

#### Scenario: Capability 缓存组件查询结果

**Given** 一个 Capability 需要频繁访问实体的组件  
**When** Capability 多次调用 `GetComponent<T>()`  
**Then** 第一次查询后应缓存结果，后续调用直接返回缓存  
**And** 组件添加/移除时应自动失效相关缓存  
**And** 实体销毁时应清理该实体的所有缓存

#### Scenario: 缓存正确性验证

**Given** 一个实体的组件被修改或移除  
**When** Capability 通过缓存访问该组件  
**Then** 应返回最新的组件引用或 null  
**And** 不应返回过期的缓存数据

### Requirement: Capability 应支持对象池复用临时对象

Capability SHALL 使用对象池复用频繁创建的临时对象，避免每帧分配新对象。对象池 MUST 有大小限制，防止内存泄漏。

**Rationale**: 频繁创建临时对象（如 LSInput）导致大量 GC 分配。使用对象池可显著减少分配。

**Priority**: P0

#### Scenario: 使用对象池减少临时对象分配

**Given** 一个 Capability 需要创建临时数据对象  
**When** Capability 创建对象时  
**Then** 应从对象池获取而非直接 new  
**And** 使用完成后应归还到对象池  
**And** 归还前应重置对象状态

#### Scenario: 对象池大小限制

**Given** 对象池已达到最大容量  
**When** 尝试归还新对象时  
**Then** 应丢弃该对象而非无限增长  
**And** 池大小应有合理的上限（如 128 个对象）

### Requirement: Capability 应避免热路径中的 LINQ 操作

Capability 的 Tick() 方法 MUST 避免使用 LINQ 操作，改用手动循环和预分配缓冲区，消除临时集合分配。

**Rationale**: LINQ 操作会产生临时集合分配和额外的迭代开销。在性能关键路径应使用手动循环。

**Priority**: P1

#### Scenario: 使用预分配缓冲区替代 LINQ

**Given** 一个 Capability 需要过滤和排序集合  
**When** 执行过滤和排序操作时  
**Then** 应使用预分配的工作缓冲区  
**And** 应使用 foreach 循环替代 Where()  
**And** 应使用 List.Sort() 替代 OrderBy()  
**And** 不应产生临时集合分配

## ADDED Requirements

### Requirement: Capability 基类应提供性能优化基础设施

Capability<T> 基类 SHALL 提供统一的性能优化基础设施，包括组件缓存、预分配缓冲区等，让所有子类自动受益。

**Rationale**: 统一的性能优化基础设施可以让所有 Capability 受益，避免重复实现。

**Priority**: P0

#### Scenario: 基类提供组件缓存机制

**Given** 一个继承自 `Capability<T>` 的 Capability  
**When** Capability 使用 `GetComponentCached<T>()` 方法  
**Then** 基类应自动管理缓存的生命周期  
**And** 应在实体销毁时自动清理缓存  
**And** 应提供手动失效缓存的方法

#### Scenario: 基类提供预分配缓冲区支持

**Given** 一个需要临时集合的 Capability  
**When** Capability 初始化时  
**Then** 可以声明预分配的工作缓冲区  
**And** 缓冲区容量应根据典型使用场景设置  
**And** 应提供自动扩容机制（但尽量避免）

## Performance Targets

### Target: 单个 Capability.Tick() 性能预算

- **目标**: 单个 Capability 的 Tick() 方法应 < 0.5ms（在 100 实体场景）
- **测量**: 使用 Unity Profiler 深度监控
- **验证**: 所有优化后的 Capability 必须满足此目标

### Target: GC 分配预算

- **目标**: 单个 Capability 每帧 GC 分配 < 1KB
- **测量**: 使用 Unity Memory Profiler
- **验证**: 运行 1000 帧后总分配 < 100KB

### Target: 组件查询性能

- **目标**: 使用缓存后查询时间 < 0.01ms
- **基准**: 未缓存的查询时间作为基准
- **提升**: 至少 3x 性能提升

## Implementation Notes

### 注意事项

1. **缓存一致性**: 必须确保缓存与实际组件状态同步
2. **内存管理**: 对象池不应导致内存泄漏
3. **向后兼容**: 优化应完全向后兼容，不影响现有 API
4. **测试覆盖**: 每个优化必须有对应的性能和正确性测试
5. **ProfileScope**: 所有优化代码应添加性能监控点

### 示例代码

```csharp
// 使用组件缓存
public override void Tick(Entity entity)
{
    // ✅ 推荐：使用缓存
    var fsm = GetComponentCached<AIStateMachineComponent>(entity);
    
    // ❌ 避免：每帧重复查询
    // var fsm = GetComponent<AIStateMachineComponent>(entity);
}

// 使用对象池
private LSInput CreateInput(...)
{
    // ✅ 推荐：从对象池获取
    var input = LSInputPool.Rent();
    // 设置数据...
    return input;
    
    // ❌ 避免：直接分配
    // var input = LSInput.Create();
}

// 避免 LINQ
private void FilterAndSort(List<Item> items)
{
    // ✅ 推荐：手动循环 + 预分配缓冲区
    _workBuffer.Clear();
    foreach (var item in items)
    {
        if (item.IsValid)
            _workBuffer.Add(item);
    }
    _workBuffer.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    
    // ❌ 避免：LINQ
    // var filtered = items.Where(i => i.IsValid)
    //                     .OrderByDescending(i => i.Priority)
    //                     .ToList();
}
```

