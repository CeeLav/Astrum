# ECC 对象池化设计文档

## Context

Astrum 项目的战斗逻辑系统采用 ECC（Entity-Component-Capability）架构，在高频战斗场景中频繁创建和销毁 Entity 和 Component，导致大量 GC 分配。目标是实现基本无 GC 的战斗逻辑系统，提升性能和稳定性。

**当前状况**：
- Entity 和 Component 通过 `new T()` 直接创建
- 销毁时直接丢弃对象，依赖 GC 回收
- 高频场景（技能特效、弹道、临时实体）产生大量临时对象

**约束条件**：
- 必须保持 MemoryPack 序列化的兼容性（序列化后的对象不能来自对象池）
- 必须支持确定性回滚（对象池不能影响帧同步）
- 必须保持现有 API 的兼容性，尽量降低迁移成本

## Goals / Non-Goals

### Goals
1. **零 GC 分配**：Entity 和 Component 的创建/销毁不产生托管堆分配
2. **生命周期清晰**：通过对象池统一管理对象的创建、使用、回收流程
3. **状态重置完整**：确保对象池回收后再次使用时状态干净
4. **性能提升显著**：战斗场景 GC 分配减少 80% 以上

### Non-Goals
1. **不改造 Capability**：Capability 已经是无状态的单例模式，无需对象池化
2. **不改造 Dictionary/List 内部结构**：优先使用预分配容量，仅在必要时使用专用集合池
3. **不改造序列化机制**：保持 MemoryPack 序列化方式不变

## Decisions

### Decision 1: 使用现有的 ObjectPool 系统

**选择**：扩展现有的 `Astrum.CommonBase.ObjectPool` 系统，而非创建新的专用对象池。

**理由**：
- 项目已有成熟的对象池实现，支持线程安全和无锁设计
- ObjectPool 已经支持 `IPool` 接口，可以自动标记对象来源
- 统一的对象池管理便于监控和调优

**替代方案**：
- 为 Entity 和 Component 创建专用对象池 → 增加代码复杂度，重复实现
- 使用第三方对象池库 → 增加外部依赖，迁移成本高

### Decision 2: 实现 IPool 接口但不影响序列化

**选择**：Entity 和 Component 实现 `IPool` 接口，但在序列化时确保对象不来自对象池。

**理由**：
- MemoryPack 序列化需要创建新对象，如果对象来自对象池会导致状态污染
- 序列化时的对象创建路径：`ObjectPool.Instance.Fetch<T>(isFromPool: false)`
- 运行时创建路径：`ObjectPool.Instance.Fetch<T>()`（默认 from pool）

**实现方式**：
```csharp
// 序列化时创建新对象
var entity = ObjectPool.Instance.Fetch<Entity>(isFromPool: false);

// 运行时创建使用对象池
var entity = ObjectPool.Instance.Fetch<Entity>();
```

### Decision 3: Reset 方法的设计

**选择**：在 Entity 和 BaseComponent 中添加 `Reset()` 方法，在对象池回收时调用。

**理由**：
- 对象池回收前需要重置所有状态，避免下次使用时残留数据
- Reset 方法集中管理重置逻辑，比分散在多个地方更清晰
- 可以支持继承类的扩展重置逻辑

**实现模式**：
```csharp
public virtual void Reset()
{
    // 重置基础字段
    UniqueId = 0;
    Name = string.Empty;
    // ...
    
    // 清理集合（不清空引用，只清理内容）
    Components.Clear();
    // ...
    
    // 调用基类重置
    base.Reset(); // 如果有基类
}
```

### Decision 4: 集合的预分配策略

**选择**：Entity 内部的 List 和 Dictionary 使用预分配容量，而非使用集合对象池。

**理由**：
- 集合对象的生命周期与 Entity 绑定，不需要单独管理
- 预分配容量可以避免频繁扩容，减少 GC 分配
- 集合对象池会增加复杂度，收益有限

**实现方式**：
```csharp
// 预分配常用容量
public List<BaseComponent> Components { get; private set; } 
    = new List<BaseComponent>(8); // 预分配 8 个元素

public Dictionary<int, CapabilityState> CapabilityStates { get; private set; } 
    = new Dictionary<int, CapabilityState>(4); // 预分配 4 个元素
```

### Decision 5: 对象池容量配置

**选择**：ObjectPool 默认容量为 1000，通过配置支持不同类型设置不同容量。

**理由**：
- Entity 通常数量较少（几十到几百），但 Component 数量较多
- 不同类型对象的使用频率不同，需要差异化配置
- 对象池容量过大占用内存，过小需要频繁创建新对象

**实现方式**：
```csharp
// 对象池配置
private readonly Func<Type, Pool> AddPoolFunc = type => 
{
    // 根据类型设置不同容量
    var capacity = GetPoolCapacity(type);
    return new Pool(type, capacity);
};

private int GetPoolCapacity(Type type)
{
    if (typeof(Entity).IsAssignableFrom(type))
        return 500; // Entity 池容量
    if (typeof(BaseComponent).IsAssignableFrom(type))
        return 1000; // Component 池容量
    return 1000; // 默认容量
}
```

## Risks / Trade-offs

### Risk 1: 序列化兼容性问题

**风险**：如果序列化时使用了对象池中的对象，可能导致状态污染或引用错误。

**缓解措施**：
- 明确区分序列化创建路径和运行时创建路径
- 在 MemoryPack 序列化代码中使用 `isFromPool: false`
- 添加单元测试验证序列化后对象状态正确

### Risk 2: Reset 方法遗漏字段

**风险**：Reset 方法可能遗漏某些字段，导致对象池回收后再次使用时状态不正确。

**缓解措施**：
- 在 Reset 方法中使用代码审查检查清单
- 添加单元测试验证 Reset 后的对象状态
- 考虑使用反射或代码生成自动生成 Reset 方法（未来优化）

### Risk 3: 性能回退

**风险**：对象池的 Get/Return 操作可能比直接创建更慢。

**缓解措施**：
- ObjectPool 使用无锁设计，性能开销极低
- 进行性能测试对比，确保对象池化后性能提升
- 如果出现性能回退，分析瓶颈并优化

### Risk 4: 内存占用增加

**风险**：对象池会预分配对象，占用更多内存。

**缓解措施**：
- 对象池容量可配置，根据实际使用情况调整
- 对象池使用最大容量限制，超过容量后丢弃多余对象
- 监控内存使用情况，必要时调整容量策略

## Migration Plan

### 阶段 1: 接口和基础设施
1. Entity 和 BaseComponent 实现 `IPool` 接口
2. 添加 Reset 方法（先实现空方法）
3. 扩展 ObjectPool 支持容量配置

### 阶段 2: 重置逻辑实现
1. 实现 Entity.Reset 方法，重置所有字段和集合
2. 实现 BaseComponent.Reset 方法
3. 为每个 Component 子类实现 Reset 方法
4. 添加单元测试验证重置逻辑

### 阶段 3: Factory 改造
1. EntityFactory 改用 ObjectPool 创建 Entity
2. ComponentFactory 改用 ObjectPool 创建 Component
3. 在回收时调用 Reset 方法
4. 添加单元测试验证创建和回收流程

### 阶段 4: 销毁逻辑改造
1. World.DestroyEntity 改为回收 Entity
2. EntityFactory.DestroyEntity 改为回收 Entity
3. Component 移除时改为回收
4. 添加单元测试验证销毁和回收流程

### 阶段 5: 集合优化
1. Entity 内部集合预分配容量
2. 测试验证 GC 分配减少情况
3. 性能测试和调优

### 阶段 6: 序列化兼容性
1. 验证 MemoryPack 序列化使用 `isFromPool: false`
2. 测试序列化/反序列化后对象状态
3. 添加集成测试验证完整流程

## Open Questions

1. **集合对象池化**：是否需要为 List/Dictionary 等集合类型单独创建对象池？目前倾向于预分配容量，但可以后续评估。
2. **预热策略**：是否需要在游戏启动时预热对象池？如果需要，预热哪些类型和数量？
3. **监控和调试**：是否需要添加对象池使用情况监控（如对象池命中率、容量使用率）？这有助于后续调优。
