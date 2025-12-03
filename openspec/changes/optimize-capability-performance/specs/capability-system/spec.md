# Spec Delta: Capability System Performance Enhancements

## MODIFIED Requirements

### Requirement: AIStateMachineComponent 应存储当前攻击目标用于缓存

AIStateMachineComponent SHALL 存储当前攻击目标 ID，BattleStateCapability 应优先使用该缓存目标，仅在目标无效或超出范围时重新查询。

**Rationale**: 战斗目标不会频繁变化，数据应存储在 Component 中（符合 ECC 架构）。缓存目标可消除 90% 的查询开销。

**Priority**: P0

#### Scenario: 使用 Component 中缓存的攻击目标

**Given** 一个 AI 实体的 AIStateMachineComponent.CurrentTargetId > 0  
**When** BattleStateCapability.Tick() 执行  
**Then** 应先从 World 获取缓存的目标实体  
**And** 目标存在、未死亡且距离 < RetargetDistance 时，直接使用该目标  
**And** 不应重新遍历所有实体查找目标  
**And** 应更新 AIStateMachineComponent.LastTargetValidationFrame 记录验证帧号

#### Scenario: 目标超出范围时重新查找并更新 Component

**Given** 一个 AI 实体的缓存目标距离 > RetargetDistance  
**When** BattleStateCapability 检测到目标超出范围  
**Then** 应调用 FindNearestEnemy() 重新查找  
**And** 将新目标 ID 存储到 AIStateMachineComponent.CurrentTargetId  
**And** 更新 LastTargetValidationFrame  
**And** 新目标查找应使用物理引擎的空间查询

#### Scenario: 目标死亡或销毁时清除缓存

**Given** AIStateMachineComponent.CurrentTargetId 指向的实体已死亡或销毁  
**When** BattleStateCapability.Tick() 检查缓存目标  
**Then** 应检测到目标无效（实体为 null 或 IsDead）  
**And** 将 CurrentTargetId 设为 -1（清除缓存）  
**And** 重新查找新目标或切换到 Idle 状态

### Requirement: Capability.GetComponent 应添加性能监控

Capability 基类的 GetComponent() 方法 SHALL 添加 ProfileScope 监控，用于验证组件查询性能是否足够快。

**Rationale**: 在实施缓存优化前，应先验证 GetComponent 是否真的是性能瓶颈。如果查询本身很快（< 0.01ms），则无需缓存。

**Priority**: P1

#### Scenario: 监控 GetComponent 调用性能

**Given** Capability 调用 GetComponent<T>()  
**When** 启用 ENABLE_PROFILER 编译标志  
**Then** GetComponent 调用 SHALL 被 ProfileScope 包裹  
**And** Unity Profiler 应显示每个组件类型的查询耗时  
**And** 应能统计总调用次数和总耗时

#### Scenario: 根据监控数据决定是否需要缓存

**Given** GetComponent 的性能监控数据  
**When** 单次调用 < 0.01ms 且总耗时 < 0.5ms  
**Then** 无需实施缓存优化  
**When** 单次调用 > 0.05ms 或总耗时 > 1ms  
**Then** 应考虑实施组件缓存方案

### Requirement: LSInput 创建应使用已有对象池

所有创建 LSInput 的代码 MUST 调用 `LSInput.Create(isFromPool: true)` 使用 ObjectPool.Instance 复用对象。使用完后 SHALL 调用 `ObjectPool.Instance.Recycle()` 归还。

**Rationale**: 项目已有全局对象池 ObjectPool.Instance，LSInput.Create() 已支持对象池参数。启用该参数可消除临时对象分配。

**Priority**: P0

#### Scenario: 创建 LSInput 时启用对象池

**Given** Capability 需要创建 LSInput 对象  
**When** 调用 LSInput.Create()  
**Then** MUST 传入 `isFromPool: true` 参数  
**And** 返回的对象应从 ObjectPool.Instance 获取  
**And** 对象的 IsFromPool 属性应为 true

#### Scenario: 使用完 LSInput 后归还对象池

**Given** LSInput 对象已使用完毕（数据已复制到 Component）  
**When** LSInputComponent.SetInput() 执行完毕  
**Then** SHALL 调用 `ObjectPool.Instance.Recycle(input)` 归还对象  
**And** 归还前 MUST 确保数据已完全复制，无悬空引用  
**And** ObjectPool 会自动检查 IsFromPool 标记，避免重复归还

#### Scenario: ObjectPool 自动管理容量

**Given** ObjectPool.Instance 管理 LSInput 类型的池  
**When** 池中对象数量达到上限（默认 1000）  
**Then** 多余的归还对象会被自动丢弃  
**And** 不会造成内存无限增长  
**And** 池容量可通过 ObjectPool.GetPoolCapacity() 配置

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

