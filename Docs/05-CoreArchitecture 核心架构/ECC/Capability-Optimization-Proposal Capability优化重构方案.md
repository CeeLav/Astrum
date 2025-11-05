# Capability 系统优化重构方案

> 作者：AI Assistant  
> 日期：2025-11-04  
> 版本：v1.6  
> 适用范围：Astrum 客户端 ECC 架构

## 1. 方案概述

### 1.1 背景与动机

当前 Capability 系统采用"实例化对象"设计，每个实体持有独立的 Capability 实例。虽然这种设计简单直观，但在性能和架构上存在以下问题：

1. **内存开销大**：每个实体都持有完整的 Capability 实例，即使逻辑相同，也需要重复分配内存
2. **缓存不友好**：Capability 实例散布在内存中，遍历执行时缓存命中率低
3. **与 ECS 理念不符**：ECC 架构中 Component 已经是纯数据，但 Capability 仍是有状态对象，未充分发挥数据驱动优势
4. **扩展性受限**：难以实现批量处理、SIMD 优化等高级性能优化
5. **状态管理复杂**：激活/禁用逻辑分散在各个 Capability 实例中，难以统一管理

### 1.2 优化目标

**核心理念**：将 Capability 从"有状态的行为对象"转变为"无状态的逻辑处理器"，状态完全由 Entity 持有，向纯 ECS 架构靠拢。

**具体目标**：

1. **性能优化**：减少内存占用，提升缓存命中率，为未来的并行/批量处理铺路
2. **状态集中化**：所有 Capability 状态（是否拥有、是否激活、禁用来源）统一存储在 Entity 上
3. **统一更新机制**：由单一系统统一调度所有 Capability 的激活判定与 Tick 执行
4. **灵活的禁用机制**：通过 Tag 系统实现细粒度的 Capability 控制，支持溯源
5. **完整生命周期**：新增 `ShouldActivate`、`ShouldDeactivate`、`OnDetached` 等接口，规范激活逻辑

### 1.3 设计原则

1. **数据与逻辑分离**：Capability 只包含逻辑，不持有状态；状态存储在 Entity 的 Component 中
2. **确定性优先**：所有状态变更可序列化、可回放，满足帧同步要求
3. **向后兼容**：优先考虑平滑迁移，降低现有代码改动量
4. **可扩展性**：为未来的并行处理、Job System 等优化预留接口

---

## 2. 架构设计

### 2.1 整体架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    CapabilitySystem                        │
│  （World 成员变量，统一调度所有 Capability）                   │
│                                                             │
│  - 注册所有 Capability 类型                                  │
│  - 按优先级遍历所有 Capability                                │
│  - 对每个 Capability：                                       │
│    遍历所有实体，对拥有此 Capability 的实体：                 │
│    1. 检查 ShouldActivate（判定激活条件）                     │
│    2. 更新激活状态（OnActivate/OnDeactivate）                │
│    3. 更新持续时间（ActiveDuration/DeactiveDuration）         │
│    4. 对激活的 Capability 调用 Tick(Entity)                  │
└─────────────────────────────────────────────────────────────┘
                            ▲
                            │ 统一调度
                            │
┌─────────────────────────────────────────────────────────────┐
│                        Entity                              │
│                                                             │
│  【Capability 状态数据】                                      │
│  - CapabilityStates: Dictionary<int, CapabilityState>      │
│    └─ Key: TypeId (基于 TypeHash 的 int)                   │
│    └─ 存在 = 拥有此 Capability，不存在 = 未拥有              │
│    └─ IsActive: bool        // 是否激活                      │
│                                                             │
│  【Capability Tag 注册】                                     │
│  - CapabilityTags: Dictionary<int, HashSet<CapabilityTag>> │
│    └─ Key: TypeId                                           │
│    └─ 每个 Capability 类型对应的 Tag 集合                     │
│                                                             │
│  【禁用 Tag 记录】                                           │
│  - DisabledTags: Dictionary<CapabilityTag, HashSet<long>>  │
│    └─ Tag -> 禁用发起者实体ID集合                             │
└─────────────────────────────────────────────────────────────┘
                            ▲
                            │ 访问状态
                            │
┌─────────────────────────────────────────────────────────────┐
│               Capability（静态类/单例）                        │
│                                                             │
│  【核心接口】                                                 │
│  + ShouldActivate(Entity): bool    // 判定是否应激活          │
│  + ShouldDeactivate(Entity): bool  // 判定是否应停用          │
│  + OnActivate(Entity): void        // 激活时回调              │
│  + OnDeactivate(Entity): void      // 停用时回调              │
│  + OnAttached(Entity): void        // 首次挂载时回调          │
│  + OnDetached(Entity): void        // 完全卸载时回调          │
│  + Tick(Entity): void              // 每帧逻辑                │
│                                                             │
│  【元数据】                                                   │
│  + TypeId: int                     // 类型 ID（基于 TypeHash）│
│  + Priority: int                   // 执行优先级              │
│  + Tags: HashSet<CapabilityTag>   // 此 Capability 的 Tag 集合 │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 核心数据结构

#### 2.2.1 Entity 新增字段

```csharp
/// <summary>
/// Capability 状态管理
/// </summary>
[MemoryPackable]
public partial class Entity
{
    // ====== 新增字段 ======
    
    /// <summary>
    /// Capability 状态字典（TypeId -> 状态）
    /// 使用 int 类型的 TypeId 作为 Key，基于 TypeHash 生成，性能更高
    /// </summary>
    public Dictionary<int, CapabilityState> CapabilityStates { get; private set; } 
        = new Dictionary<int, CapabilityState>();
    
    /// <summary>
    /// 被禁用的 Tag 集合（Tag -> 禁用发起者实体ID集合）
    /// 用于支持基于 Tag 的 Capability 批量禁用
    /// </summary>
    public Dictionary<CapabilityTag, HashSet<long>> DisabledTags { get; private set; } 
        = new Dictionary<CapabilityTag, HashSet<long>>();
    
    // ====== 移除字段 ======
    // public List<Capability> Capabilities { get; private set; } = new List<Capability>();
    // ↑ 此字段将被废弃，Capability 不再以实例形式存在
}
```

#### 2.2.2 CapabilityState 数据结构

```csharp
/// <summary>
/// Capability 在实体上的状态信息
/// </summary>
[MemoryPackable]
public partial struct CapabilityState
{
    /// <summary>
    /// 是否激活（满足激活条件且未被禁用）
    /// 注意：CapabilityState 在字典中存在即表示实体拥有此 Capability
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// 已激活持续时间（帧数，仅在 TrackActiveDuration 为 true 时更新）
    /// 当 IsActive 为 true 时，每帧递增 1
    /// </summary>
    public int ActiveDuration { get; set; }
    
    /// <summary>
    /// 已禁用持续时间（帧数，仅在 TrackDeactiveDuration 为 true 时更新）
    /// 当 IsActive 为 false 时，每帧递增 1
    /// </summary>
    public int DeactiveDuration { get; set; }
    
    /// <summary>
    /// 自定义数据（预留字段，用于存储 Capability 特定的简单状态）
    /// 例如：冷却时间、计数器等
    /// </summary>
    public Dictionary<string, object> CustomData { get; set; }
}
```

### 2.3 CapabilityTag 枚举定义

```csharp
/// <summary>
/// Capability 标签枚举
/// 普通枚举，每个 Capability 可以拥有多个 Tag（使用 HashSet 存储）
/// </summary>
public enum CapabilityTag
{
    /// <summary>
    /// 移动相关（位移、旋转等）
    /// </summary>
    Movement,
    
    /// <summary>
    /// 玩家控制相关（输入响应、指令处理等）
    /// </summary>
    Control,
    
    /// <summary>
    /// 攻击相关（普通攻击、连击等）
    /// </summary>
    Attack,
    
    /// <summary>
    /// 技能相关（技能释放、技能效果等）
    /// </summary>
    Skill,
    
    /// <summary>
    /// AI 相关（AI 决策、状态机等）
    /// </summary>
    AI,
    
    /// <summary>
    /// 动画相关（动画播放、状态同步等，通常不禁用）
    /// </summary>
    Animation,
    
    /// <summary>
    /// 物理相关（碰撞、物理模拟等）
    /// </summary>
    Physics,
    
    /// <summary>
    /// 战斗相关（伤害计算、状态效果等）
    /// </summary>
    Combat,
    
    /// <summary>
    /// 交互相关（拾取、对话、使用物品等）
    /// </summary>
    Interaction
}
```

### 2.4 TypeHash 工具类

```csharp
/// <summary>
/// 字符串稳定哈希工具类
/// 使用 FNV-1a 算法生成稳定的哈希值
/// </summary>
public static class StringHashUtility
{
    /// <summary>
    /// 获取字符串的稳定哈希值
    /// </summary>
    public static int GetStableHash(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return 0;
        
        unchecked
        {
            const int fnvPrime = 16777619;
            int hash = (int)2166136261;
            for (int i = 0; i < str.Length; i++)
                hash = (hash ^ str[i]) * fnvPrime;
            return hash;
        }
    }
}

/// <summary>
/// 类型哈希工具类
/// 为每个类型生成唯一的稳定哈希 ID
/// </summary>
public static class TypeHash<T>
{
    /// <summary>
    /// 类型的稳定哈希 ID（编译期常量）
    /// </summary>
    public static readonly int Hash = typeof(T).FullName?.GetStableHash() ?? 0;
    
    /// <summary>
    /// 获取类型的哈希 ID
    /// </summary>
    public static int GetHash() => Hash;
}
```

### 2.5 Capability 新接口定义

#### 2.4.1 ICapability 接口

```csharp
/// <summary>
/// Capability 核心接口
/// 所有 Capability 必须实现此接口
/// </summary>
public interface ICapability
{
    /// <summary>
    /// 类型 ID（基于 TypeHash 的稳定哈希值）
    /// </summary>
    int TypeId { get; }
    
    /// <summary>
    /// 执行优先级（数值越大越优先）
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// 此 Capability 的 Tag 集合
    /// 用于支持基于 Tag 的批量禁用
    /// </summary>
    IReadOnlySet<CapabilityTag> Tags { get; }
    
    /// <summary>
    /// 是否跟踪激活持续时间（ActiveDuration）
    /// 如果为 true，系统会在每帧更新 ActiveDuration
    /// </summary>
    bool TrackActiveDuration { get; }
    
    /// <summary>
    /// 是否跟踪禁用持续时间（DeactiveDuration）
    /// 如果为 true，系统会在每帧更新 DeactiveDuration
    /// </summary>
    bool TrackDeactiveDuration { get; }
    
    /// <summary>
    /// 判定此 Capability 是否应该激活
    /// 每帧调用，用于检查激活条件（如：所需组件是否存在、外部条件是否满足）
    /// </summary>
    bool ShouldActivate(Entity entity);
    
    /// <summary>
    /// 判定此 Capability 是否应该停用
    /// 每帧调用，用于检查停用条件（如：所需组件被移除、外部条件不再满足）
    /// </summary>
    bool ShouldDeactivate(Entity entity);
    
    /// <summary>
    /// 首次挂载到实体时调用（在 Archetype 装配时）
    /// 用于初始化 Entity 上的状态数据
    /// 注意：此方法替代了原来的 Initialize() 方法
    /// </summary>
    void OnAttached(Entity entity);
    
    /// <summary>
    /// 从实体上完全卸载时调用（在 SubArchetype 卸载时）
    /// 用于清理 Entity 上的状态数据
    /// </summary>
    void OnDetached(Entity entity);
    
    /// <summary>
    /// 激活时调用（当 IsActive 从 false 变为 true）
    /// </summary>
    void OnActivate(Entity entity);
    
    /// <summary>
    /// 停用时调用（当 IsActive 从 true 变为 false）
    /// </summary>
    void OnDeactivate(Entity entity);
    
    /// <summary>
    /// 每帧更新（仅在激活状态下调用）
    /// </summary>
    void Tick(Entity entity);
}
```

#### 2.4.2 CapabilityBase 抽象基类

```csharp
/// <summary>
/// Capability 抽象基类
/// 使用泛型自动生成 TypeId，提供默认实现和辅助方法
/// </summary>
/// <typeparam name="T">Capability 自身类型</typeparam>
public abstract class Capability<T> : ICapability where T : ICapability
{
    /// <summary>
    /// 类型 ID（基于 TypeHash 的稳定哈希值，编译期常量）
    /// </summary>
    public static readonly int TypeId = TypeHash<T>.GetHash();
    
    /// <summary>
    /// 类型 ID（接口实现）
    /// </summary>
    int ICapability.TypeId => TypeId;
    
    /// <summary>
    /// 执行优先级（默认值，子类可重写）
    /// </summary>
    public virtual int Priority => 0;
    
    /// <summary>
    /// Tag 集合（默认空集合，子类可重写）
    /// </summary>
    public virtual IReadOnlySet<CapabilityTag> Tags => EmptyTags;
    
    /// <summary>
    /// 是否跟踪激活持续时间（默认 false）
    /// </summary>
    public virtual bool TrackActiveDuration => false;
    
    /// <summary>
    /// 是否跟踪禁用持续时间（默认 false）
    /// </summary>
    public virtual bool TrackDeactiveDuration => false;
    
    private static readonly HashSet<CapabilityTag> EmptyTags = new HashSet<CapabilityTag>();
    
    /// <summary>
    /// 判定此 Capability 是否应该激活
    /// </summary>
    public virtual bool ShouldActivate(Entity entity)
    {
        // 默认实现：检查是否存在且未被禁用
        // CapabilityState 存在即表示拥有此 Capability
        return HasCapabilityState(entity) && !IsCapabilityDisabled(entity);
    }
    
    /// <summary>
    /// 判定此 Capability 是否应该停用
    /// </summary>
    public virtual bool ShouldDeactivate(Entity entity)
    {
        // 默认实现：检查是否被禁用或不再存在
        return !HasCapabilityState(entity) || IsCapabilityDisabled(entity);
    }
    
    /// <summary>
    /// 首次挂载到实体时调用
    /// </summary>
    public virtual void OnAttached(Entity entity)
    {
        // 默认不执行任何操作
    }
    
    /// <summary>
    /// 从实体上完全卸载时调用
    /// </summary>
    public virtual void OnDetached(Entity entity)
    {
        // 默认清理状态
        RemoveCapabilityState(entity);
    }
    
    /// <summary>
    /// 激活时调用
    /// </summary>
    public virtual void OnActivate(Entity entity)
    {
        // 默认不执行任何操作
    }
    
    /// <summary>
    /// 停用时调用
    /// </summary>
    public virtual void OnDeactivate(Entity entity)
    {
        // 默认不执行任何操作
    }
    
    /// <summary>
    /// 每帧更新（抽象方法，子类必须实现）
    /// </summary>
    public abstract void Tick(Entity entity);
    
    // ====== 辅助方法 ======
    
    /// <summary>
    /// 获取此 Capability 在实体上的状态
    /// </summary>
    protected CapabilityState GetCapabilityState(Entity entity)
    {
        if (entity.CapabilityStates.TryGetValue(TypeId, out var state))
            return state;
        return default;
    }
    
    /// <summary>
    /// 设置此 Capability 在实体上的状态
    /// </summary>
    protected void SetCapabilityState(Entity entity, CapabilityState state)
    {
        entity.CapabilityStates[TypeId] = state;
    }
    
    /// <summary>
    /// 移除此 Capability 在实体上的状态
    /// </summary>
    protected void RemoveCapabilityState(Entity entity)
    {
        entity.CapabilityStates.Remove(TypeId);
    }
    
    /// <summary>
    /// 检查此 Capability 是否在实体上存在（字典中存在即表示拥有）
    /// </summary>
    protected bool HasCapabilityState(Entity entity)
    {
        return entity.CapabilityStates.ContainsKey(TypeId);
    }
    
    /// <summary>
    /// 检查此 Capability 是否被禁用
    /// 通过检查 Entity.DisabledTags 中是否有此 Capability 的 Tag 被禁用
    /// </summary>
    protected bool IsCapabilityDisabled(Entity entity)
    {
        // 使用静态方法获取 Capability
        var capability = CapabilitySystem.GetCapability(TypeId);
        if (capability == null)
            return false;
        
        // 检查此 Capability 的任何一个 Tag 是否在 DisabledTags 中
        foreach (var tag in capability.Tags)
        {
            if (entity.DisabledTags.ContainsKey(tag) && entity.DisabledTags[tag].Count > 0)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查实体是否拥有指定组件
    /// </summary>
    protected bool HasComponent<TComponent>(Entity entity) where TComponent : BaseComponent
    {
        return entity.HasComponent<TComponent>();
    }
    
    /// <summary>
    /// 获取实体的组件
    /// </summary>
    protected TComponent GetComponent<TComponent>(Entity entity) where TComponent : BaseComponent
    {
        return entity.GetComponent<TComponent>();
    }
}
```

### 2.6 CapabilitySystem 统一调度系统

```csharp
/// <summary>
/// Capability 统一调度系统
/// 负责所有 Capability 的激活判定和 Tick 执行
/// World 的成员变量，支持序列化用于帧同步回滚
/// </summary>
[MemoryPackable]
public partial class CapabilitySystem
{
    // ====== 静态注册信息（不序列化，避免回滚） ======
    
    /// <summary>
    /// 已注册的 Capability 列表（静态，所有 World 共享）
    /// Key: Capability 类型
    /// Value: Capability 实例（单例）
    /// </summary>
    [MemoryPackIgnore]
    private static readonly Dictionary<Type, ICapability> _registeredCapabilities 
        = new Dictionary<Type, ICapability>();
    
    /// <summary>
    /// 按优先级排序的 Capability 列表（静态，所有 World 共享）
    /// </summary>
    [MemoryPackIgnore]
    private static readonly List<ICapability> _sortedCapabilities = new List<ICapability>();
    
    /// <summary>
    /// Tag 到 Capability TypeId 集合的映射（静态，所有 World 共享）
    /// </summary>
    [MemoryPackIgnore]
    private static readonly Dictionary<CapabilityTag, HashSet<int>> _tagToCapabilityTypeIds 
        = new Dictionary<CapabilityTag, HashSet<int>>();
    
    /// <summary>
    /// TypeId 到 Capability 实例的映射（静态，所有 World 共享）
    /// </summary>
    [MemoryPackIgnore]
    private static readonly Dictionary<int, ICapability> _typeIdToCapability 
        = new Dictionary<int, ICapability>();
    
    /// <summary>
    /// 静态初始化标志（确保只初始化一次）
    /// </summary>
    [MemoryPackIgnore]
    private static bool _isStaticInitialized = false;
    
    // ====== 实例数据（序列化，支持回滚） ======
    
    /// <summary>
    /// Capability TypeId 到拥有此 Capability 的 Entity ID 集合的映射
    /// Key: Capability TypeId
    /// Value: 拥有此 Capability 的 Entity ID 集合
    /// </summary>
    private Dictionary<int, HashSet<long>> _typeIdToEntityIds 
        = new Dictionary<int, HashSet<long>>();
    
    /// <summary>
    /// 所属 World（不序列化，由 World 设置）
    /// </summary>
    [MemoryPackIgnore]
    public World World { get; set; }
    
    public CapabilitySystem() { }
    
    /// <summary>
    /// MemoryPack 构造函数
    /// </summary>
    [MemoryPackConstructor]
    public CapabilitySystem(Dictionary<int, HashSet<long>> typeIdToEntityIds)
    {
        _typeIdToEntityIds = typeIdToEntityIds ?? new Dictionary<int, HashSet<long>>();
        
        // 确保静态数据已初始化
        EnsureStaticInitialized();
    }
    
    /// <summary>
    /// 确保静态数据已初始化（线程安全）
    /// </summary>
    private static void EnsureStaticInitialized()
    {
        if (_isStaticInitialized)
            return;
        
        lock (_registeredCapabilities)
        {
            if (_isStaticInitialized)
                return;
            
            // 自动扫描并注册所有 Capability
            RegisterAllCapabilities();
            
            // 按优先级排序
            SortCapabilities();
            
            // 构建 Tag 映射
            BuildTagMapping();
            
            _isStaticInitialized = true;
        }
    }
    
    /// <summary>
    /// 初始化系统（在游戏启动时调用）
    /// </summary>
    public void Initialize()
    {
        EnsureStaticInitialized();
    }
    
    /// <summary>
    /// 注册 Capability
    /// </summary>
    public static void RegisterCapability<T>() where T : ICapability, new()
    {
        var capability = new T();
        var type = typeof(T);
        
        lock (_registeredCapabilities)
        {
            if (!_registeredCapabilities.ContainsKey(type))
            {
                _registeredCapabilities[type] = capability;
                _sortedCapabilities.Add(capability);
                
                // 同时注册到 TypeId 映射
                _typeIdToCapability[capability.TypeId] = capability;
            }
        }
    }
    
    /// <summary>
    /// 注册 Capability 实例（用于单例）
    /// </summary>
    private static void RegisterCapability(Type type, ICapability capability)
    {
        lock (_registeredCapabilities)
        {
            if (!_registeredCapabilities.ContainsKey(type))
            {
                _registeredCapabilities[type] = capability;
                _sortedCapabilities.Add(capability);
                
                // 同时注册到 TypeId 映射
                _typeIdToCapability[capability.TypeId] = capability;
            }
        }
    }
    
    /// <summary>
    /// 自动扫描并注册所有 Capability
    /// </summary>
    private static void RegisterAllCapabilities()
    {
        var assembly = typeof(ICapability).Assembly;
        var capabilityTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(ICapability).IsAssignableFrom(t));
        
        foreach (var type in capabilityTypes)
        {
            try
            {
                var capability = (ICapability)Activator.CreateInstance(type);
                RegisterCapability(type, capability);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Failed to register Capability: {type.Name}, Error: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 按优先级排序 Capability
    /// </summary>
    private static void SortCapabilities()
    {
        _sortedCapabilities.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }
    
    /// <summary>
    /// 构建 Tag 到 Capability TypeId 的映射
    /// </summary>
    private static void BuildTagMapping()
    {
        _tagToCapabilityTypeIds.Clear();
        
        // 遍历所有已注册的 Capability
        foreach (var capability in _registeredCapabilities.Values)
        {
            var typeId = capability.TypeId;
            
            // 遍历此 Capability 的所有 Tag
            foreach (var tag in capability.Tags)
            {
                if (!_tagToCapabilityTypeIds.TryGetValue(tag, out var typeIds))
                {
                    typeIds = new HashSet<int>();
                    _tagToCapabilityTypeIds[tag] = typeIds;
                }
                typeIds.Add(typeId);
            }
        }
    }
    
    /// <summary>
    /// 根据 TypeId 获取 Capability
    /// </summary>
    public static ICapability GetCapability(int typeId)
    {
        return _typeIdToCapability.TryGetValue(typeId, out var capability) ? capability : null;
    }
    
    /// <summary>
    /// 根据 Type 获取 Capability
    /// </summary>
    public static ICapability GetCapability(Type type)
    {
        return _registeredCapabilities.TryGetValue(type, out var capability) ? capability : null;
    }
    
    // ====== Entity Capability 注册管理（实例方法，支持回滚） ======
    
    /// <summary>
    /// 注册 Entity 拥有某个 Capability
    /// 在 Entity 添加 Capability 时调用
    /// </summary>
    public void RegisterEntityCapability(long entityId, int typeId)
    {
        if (!_typeIdToEntityIds.TryGetValue(typeId, out var entityIds))
        {
            entityIds = new HashSet<long>();
            _typeIdToEntityIds[typeId] = entityIds;
        }
        entityIds.Add(entityId);
    }
    
    /// <summary>
    /// 注销 Entity 的某个 Capability
    /// 在 Entity 移除 Capability 时调用
    /// </summary>
    public void UnregisterEntityCapability(long entityId, int typeId)
    {
        if (_typeIdToEntityIds.TryGetValue(typeId, out var entityIds))
        {
            entityIds.Remove(entityId);
            
            // 如果集合为空，可以选择移除（或者保留，等待垃圾回收）
            if (entityIds.Count == 0)
            {
                _typeIdToEntityIds.Remove(typeId);
            }
        }
    }
    
    /// <summary>
    /// 清理已销毁实体的 Capability 注册
    /// 在 Entity 销毁时调用
    /// </summary>
    public void UnregisterEntity(long entityId)
    {
        foreach (var kvp in _typeIdToEntityIds.ToList())
        {
            kvp.Value.Remove(entityId);
            
            // 如果集合为空，移除该 TypeId 的映射
            if (kvp.Value.Count == 0)
            {
                _typeIdToEntityIds.Remove(kvp.Key);
            }
        }
    }
    
    /// <summary>
    /// 更新所有 Capability（按 Capability 遍历，只更新拥有该 Capability 的实体）
    /// </summary>
    public void Update(World world)
    {
        if (world == null || world.Entities == null)
            return;
        
        // 按优先级遍历所有 Capability
        foreach (var capability in _sortedCapabilities)
        {
            var typeId = capability.TypeId;
            
            // 只遍历拥有此 Capability 的实体（避免无效更新）
            if (!_typeIdToEntityIds.TryGetValue(typeId, out var entityIds))
                continue;
            
            // 遍历拥有此 Capability 的实体 ID 集合
            foreach (var entityId in entityIds.ToList()) // ToList() 避免迭代时修改集合
            {
                // 获取实体（可能已被销毁）
                if (!world.Entities.TryGetValue(entityId, out var entity))
                {
                    // 实体已被销毁，清理注册
                    UnregisterEntityCapability(entityId, typeId);
                    continue;
                }
                
                if (entity == null || !entity.IsActive || entity.IsDestroyed)
                {
                    // 实体已销毁或未激活，清理注册
                    UnregisterEntityCapability(entityId, typeId);
                    continue;
                }
                
                // 检查此 Capability 是否仍然存在于实体上（双重检查，防止状态不一致）
                if (!entity.CapabilityStates.TryGetValue(typeId, out var state))
                {
                    // 状态不一致，清理注册
                    UnregisterEntityCapability(entityId, typeId);
                    continue;
                }
                
                // 1. 更新激活状态
                UpdateActivationState(capability, entity, ref state);
                
                // 2. 更新持续时间
                UpdateDuration(capability, entity, ref state);
                
                // 3. 执行激活的 Capability 的 Tick
                if (state.IsActive)
                {
                    try
                    {
                        capability.Tick(entity);
                    }
                    catch (Exception ex)
                    {
                        ASLogger.Instance.Error($"Error executing Capability {capability.GetType().Name} on entity {entity.UniqueId}: {ex.Message}");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 更新单个 Capability 在单个实体上的激活状态
    /// </summary>
    private void UpdateActivationState(ICapability capability, Entity entity, ref CapabilityState state)
    {
        var typeId = capability.TypeId;
        
        // 检查 Tag 是否被禁用
        bool isDisabled = IsCapabilityDisabledByTag(capability, entity);
        
        // 如果当前未激活，检查是否应该激活
        if (!state.IsActive)
        {
            // 只有在未被禁用的情况下才检查激活条件
            if (!isDisabled && capability.ShouldActivate(entity))
            {
                state.IsActive = true;
                state.ActiveDuration = 0; // 重置激活持续时间
                entity.CapabilityStates[typeId] = state;
                capability.OnActivate(entity);
                return; // 成功激活后直接返回，避免无效的停用检查
            }
        }
        else // 当前已激活
        {
            // 检查是否应该停用（被禁用或 ShouldDeactivate 返回 true）
            if (isDisabled || capability.ShouldDeactivate(entity))
            {
                state.IsActive = false;
                state.DeactiveDuration = 0; // 重置禁用持续时间
                entity.CapabilityStates[typeId] = state;
                capability.OnDeactivate(entity);
            }
        }
    }
    
    /// <summary>
    /// 检查 Capability 是否被 Tag 禁用
    /// </summary>
    private bool IsCapabilityDisabledByTag(ICapability capability, Entity entity)
    {
        // 检查此 Capability 的任何一个 Tag 是否在 DisabledTags 中
        foreach (var tag in capability.Tags)
        {
            if (entity.DisabledTags.ContainsKey(tag) && entity.DisabledTags[tag].Count > 0)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// 更新 Capability 的持续时间
    /// </summary>
    private void UpdateDuration(ICapability capability, Entity entity, ref CapabilityState state)
    {
        var typeId = capability.TypeId;
        
        if (state.IsActive && capability.TrackActiveDuration)
        {
            state.ActiveDuration++;
            entity.CapabilityStates[typeId] = state;
        }
        else if (!state.IsActive && capability.TrackDeactiveDuration)
        {
            state.DeactiveDuration++;
            entity.CapabilityStates[typeId] = state;
        }
    }
    
    
    // ====== 外部接口：Tag 禁用/启用 ======
    
    /// <summary>
    /// 禁用实体上所有匹配指定 Tag 的 Capability
    /// </summary>
    public void DisableCapabilitiesByTag(Entity entity, CapabilityTag tag, long instigatorId, string reason = null)
    {
        // 查找所有匹配此 Tag 的 Capability
        if (!_tagToCapabilityTypeIds.TryGetValue(tag, out var typeIds))
            return;
        
        // 记录到 DisabledTags（用于快速检查）
        if (!entity.DisabledTags.TryGetValue(tag, out var instigators))
        {
            instigators = new HashSet<long>();
            entity.DisabledTags[tag] = instigators;
        }
        instigators.Add(instigatorId);
        
        // 注意：不需要在 CapabilityState 中存储 Disabler，因为禁用状态可以从 DisabledTags 中查询
    }
    
    /// <summary>
    /// 启用实体上所有匹配指定 Tag 的 Capability
    /// </summary>
    public void EnableCapabilitiesByTag(Entity entity, CapabilityTag tag, long instigatorId)
    {
        // 从 DisabledTags 中移除
        if (entity.DisabledTags.TryGetValue(tag, out var instigators))
        {
            instigators.Remove(instigatorId);
            if (instigators.Count == 0)
                entity.DisabledTags.Remove(tag);
        }
    }
    
    /// <summary>
    /// 检查实体上是否有指定 Tag 的 Capability 被禁用
    /// </summary>
    public bool IsTagDisabled(Entity entity, CapabilityTag tag)
    {
        return entity.DisabledTags.ContainsKey(tag) && entity.DisabledTags[tag].Count > 0;
    }
}
```

---

## 3. 迁移方案与开发进展

详细的迁移方案和实施计划请参考：[开发进展文档](../开发进展/Capability-Optimization-Progress%20Capability优化重构开发进展.md)

---

## 4. 具体示例：MovementCapability 迁移

### 4.1 迁移前（旧实现）

```csharp
[MemoryPackable]
public partial class MovementCapability : Capability
{
    public FP MovementThreshold { get; set; } = (FP)0.1f;
    
    public MovementCapability()
    {
        Priority = 100;
    }
    
    public override void Tick()
    {
        if (!CanExecute()) return;
        
        var inputComponent = GetOwnerComponent<LSInputComponent>();
        var movementComponent = GetOwnerComponent<MovementComponent>();
        var transComponent = GetOwnerComponent<TransComponent>();
        
        // ... 移动逻辑 ...
    }
    
    public override bool CanExecute()
    {
        if (!base.CanExecute()) return false;
        return OwnerHasComponent<LSInputComponent>() && 
               OwnerHasComponent<MovementComponent>() && 
               OwnerHasComponent<TransComponent>();
    }
}
```

### 4.2 迁移后（新实现）

```csharp
/// <summary>
/// 移动能力（静态/单例模式）
/// </summary>
public class MovementCapability : Capability<MovementCapability>
{
    // ====== 元数据 ======
    public override int Priority => 100;
    
    public override IReadOnlySet<CapabilityTag> Tags => _tags;
    private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag> 
    { 
        CapabilityTag.Movement, 
        CapabilityTag.Control 
    };
    
    // ====== 常量配置（可移至配置文件） ======
    private const string KEY_MOVEMENT_THRESHOLD = "MovementThreshold";
    private static readonly FP DEFAULT_MOVEMENT_THRESHOLD = (FP)0.1f;
    
    // ====== 生命周期 ======
    
    public override void OnAttached(Entity entity)
    {
        base.OnAttached(entity);
        
        // 初始化自定义数据
        var state = GetCapabilityState(entity);
        if (state.CustomData == null)
            state.CustomData = new Dictionary<string, object>();
        
        state.CustomData[KEY_MOVEMENT_THRESHOLD] = DEFAULT_MOVEMENT_THRESHOLD;
        SetCapabilityState(entity, state);
    }
    
    public override bool ShouldActivate(Entity entity)
    {
        // 检查必需组件是否存在
        return base.ShouldActivate(entity) &&
               HasComponent<LSInputComponent>(entity) &&
               HasComponent<MovementComponent>(entity) &&
               HasComponent<TransComponent>(entity);
    }
    
    public override bool ShouldDeactivate(Entity entity)
    {
        // 缺少任何必需组件则停用
        return base.ShouldDeactivate(entity) ||
               !HasComponent<LSInputComponent>(entity) ||
               !HasComponent<MovementComponent>(entity) ||
               !HasComponent<TransComponent>(entity);
    }
    
    // ====== 每帧逻辑 ======
    
    public override void Tick(Entity entity)
    {
        // 获取组件
        var inputComponent = GetComponent<LSInputComponent>(entity);
        var movementComponent = GetComponent<MovementComponent>(entity);
        var transComponent = GetComponent<TransComponent>(entity);
        
        if (inputComponent?.CurrentInput == null || movementComponent == null || transComponent == null)
            return;
        
        // 获取移动阈值
        var state = GetCapabilityState(entity);
        var threshold = (FP)(state.CustomData?.TryGetValue(KEY_MOVEMENT_THRESHOLD, out var value) == true 
            ? value 
            : DEFAULT_MOVEMENT_THRESHOLD);
        
        // 原有移动逻辑（不变）
        var input = inputComponent.CurrentInput;
        FP moveX = (FP)(input.MoveX / (double)(1L << 32));
        FP moveY = (FP)(input.MoveY / (double)(1L << 32));
        
        FP inputMagnitude = FP.Sqrt(moveX * moveX + moveY * moveY);
        
        if (inputMagnitude > FP.One)
        {
            moveX /= inputMagnitude;
            moveY /= inputMagnitude;
            inputMagnitude = FP.One;
        }
        
        var deltaTime = LSConstValue.UpdateInterval / 1000f;
        
        // 更新朝向
        if (inputMagnitude > threshold)
        {
            TSVector inputDirection = new TSVector(moveX, FP.Zero, moveY);
            if (inputDirection.sqrMagnitude > FP.EN4)
            {
                transComponent.Rotation = TSQuaternion.LookRotation(inputDirection, TSVector.up);
            }
        }
        
        // 处理移动
        if (inputMagnitude > threshold && movementComponent.CanMove)
        {
            FP speed = movementComponent.Speed;
            FP dt = (FP)deltaTime;
            FP deltaX = moveX * speed * dt;
            FP deltaY = moveY * speed * dt;
            
            var pos = transComponent.Position;
            transComponent.Position = new TSVector(pos.x + deltaX, pos.y, pos.z + deltaY);
            
            entity.World?.HitSystem?.UpdateEntityPosition(entity);
        }
    }
}
```

---

## 5. Tag 系统应用示例

### 5.1 技能释放时禁用移动

```csharp
// SkillExecutorCapability.cs
public override void OnActivate(Entity entity)
{
    base.OnActivate(entity);
    
    // 禁用所有标记为 Movement 的 Capability
    entity.World?.CapabilitySystem?.DisableCapabilitiesByTag(
        entity, 
        CapabilityTag.Movement, 
        entity.UniqueId, 
        "SkillCasting"
    );
}

public override void OnDeactivate(Entity entity)
{
    base.OnDeactivate(entity);
    
    // 恢复移动能力
    entity.World?.CapabilitySystem?.EnableCapabilitiesByTag(
        entity, 
        CapabilityTag.Movement, 
        entity.UniqueId
    );
}
```

### 5.2 死亡时禁用所有控制

```csharp
// DeadCapability.cs
private void OnEntityDied(EntityDiedEventData eventData)
{
    if (eventData.EntityId != OwnerEntity.UniqueId)
        return;
    
    var entity = OwnerEntity;
    
    // 禁用所有标记为 Control 的 Capability
    entity.World?.CapabilitySystem?.DisableCapabilitiesByTag(
        entity, 
        CapabilityTag.Control, 
        entity.UniqueId, 
        "Death"
    );
}

private void OnEntityRevived(EntityRevivedEventData eventData)
{
    if (eventData.EntityId != OwnerEntity.UniqueId)
        return;
    
    var entity = OwnerEntity;
    
    // 恢复所有控制能力
    entity.World?.CapabilitySystem?.EnableCapabilitiesByTag(
        entity, 
        CapabilityTag.Control, 
        entity.UniqueId
    );
}
```

### 5.3 常用 Tag 枚举值

| Tag 枚举值 | 含义 | 应用场景 |
|-----------|------|---------|
| `CapabilityTag.Movement` | 移动相关 | 技能施放、眩晕、冰冻、死亡时禁用 |
| `CapabilityTag.Control` | 玩家控制相关 | 死亡、剧情接管时禁用 |
| `CapabilityTag.Attack` | 攻击相关 | 缴械、眩晕、死亡时禁用 |
| `CapabilityTag.Skill` | 技能相关 | 沉默、眩晕、死亡时禁用 |
| `CapabilityTag.AI` | AI 相关 | 玩家接管、特殊剧情时禁用 |
| `CapabilityTag.Animation` | 动画相关 | 通常不禁用（视觉表现层） |
| `CapabilityTag.Physics` | 物理相关 | 死亡、传送时禁用 |
### 5.4 多 Tag 禁用示例

```csharp
// 示例1：禁用多个 Tag（需要分别调用）
entity.World?.CapabilitySystem?.DisableCapabilitiesByTag(entity, CapabilityTag.Movement, entity.UniqueId, "Stun");
entity.World?.CapabilitySystem?.DisableCapabilitiesByTag(entity, CapabilityTag.Control, entity.UniqueId, "Stun");

// 示例2：禁用所有战斗相关能力（需要分别禁用）
entity.World?.CapabilitySystem?.DisableCapabilitiesByTag(entity, CapabilityTag.Attack, entity.UniqueId, "Silence");
entity.World?.CapabilitySystem?.DisableCapabilitiesByTag(entity, CapabilityTag.Skill, entity.UniqueId, "Silence");
entity.World?.CapabilitySystem?.DisableCapabilitiesByTag(entity, CapabilityTag.Combat, entity.UniqueId, "Silence");
```

---

## 6. 性能分析

### 6.1 内存占用对比

#### 旧方案（实例模式）

假设一个实体有 5 个 Capability：

```
单个 Capability 实例大小：
- CapabilityId (int): 4 bytes
- IsActive (bool): 1 byte
- Priority (int): 4 bytes
- OwnerEntity (引用): 8 bytes
- 虚函数表指针: 8 bytes
- 子类字段: 平均 16 bytes
= 约 41 bytes/实例

5 个 Capability = 5 * 41 = 205 bytes
1000 个实体 = 1000 * 205 = 205 KB
```

#### 新方案（状态模式）

```
单个 CapabilityState 大小：
- IsActive (bool): 1 byte
- ActiveDuration (int): 4 bytes（仅当 TrackActiveDuration 为 true 时使用）
- DeactiveDuration (int): 4 bytes（仅当 TrackDeactiveDuration 为 true 时使用）
- CustomData (Dictionary): 8 bytes（空字典）
= 约 17 bytes/状态（假设两个 Duration 字段都使用）

实际使用中，大多数 Capability 不需要跟踪持续时间，实际大小约为：
- IsActive (bool): 1 byte
- ActiveDuration (int): 4 bytes（默认值，即使不跟踪也会占用）
- DeactiveDuration (int): 4 bytes（默认值，即使不跟踪也会占用）
- CustomData (Dictionary): 8 bytes（空字典）
= 约 17 bytes/状态

5 个 Capability 状态 = 5 * 17 = 85 bytes
1000 个实体 = 1000 * 85 = 85 KB

节省内存：(205 - 85) / 205 = 59% ✓
（相比旧方案仍有显著优势）
```

### 6.2 更新性能优化

#### 旧方案（遍历所有 Entity）

```
假设场景：1000 个实体，其中 100 个拥有 MovementCapability
更新 MovementCapability 时需要遍历 1000 个实体
→ 无效遍历：900 次（90%）
→ 性能开销：O(EntityCount * CapabilityCount)
```

#### 新方案（使用 Entity 映射）

```
假设场景：1000 个实体，其中 100 个拥有 MovementCapability
更新 MovementCapability 时只遍历 100 个实体 ID（从 _typeIdToEntityIds 获取）
→ 无效遍历：0 次（0%）
→ 性能开销：O(拥有该Capability的EntityCount * CapabilityCount)
→ 性能提升：约 10 倍（在大部分实体没有该 Capability 的情况下）
```

### 6.3 缓存命中率提升

#### 旧方案

```
Capability 实例散布在内存中（不连续）
→ 缓存行利用率低（约 30-40%）
→ 频繁的 Cache Miss
```

#### 新方案

```
CapabilityState 连续存储在 Dictionary 中
→ 缓存行利用率高（约 70-80%）
→ 更少的 Cache Miss
→ 理论性能提升：20-30%
```

### 6.4 静态数据优化

#### 注册信息静态化

```
将 Capability 注册信息改为静态，所有 World 共享
→ 避免序列化和回滚时重复初始化
→ 减少内存占用（多个 World 共享同一份注册数据）
→ 提升初始化速度（只需初始化一次）
→ 避免回滚时重置注册信息
```

### 6.5 批量处理潜力

新方案为未来的并行优化铺路：

```csharp
// 示例：并行更新所有实体的 MovementCapability
public void UpdateMovementCapability_Parallel(World world, List<Entity> entities)
{
    var typeId = MovementCapability.TypeId;
    var capability = world?.CapabilitySystem?.GetCapability(typeId);
    
    Parallel.ForEach(entities, entity =>
    {
        // 检查 Capability 是否存在且激活（存在即表示拥有）
        if (!entity.CapabilityStates.TryGetValue(typeId, out var state))
            return;
        if (!state.IsActive)
            return;
        
        // 并行执行移动逻辑
        capability.Tick(entity);
    });
}
```

---

## 7. 测试计划

### 7.1 单元测试

#### 测试 1：CapabilityState 管理

```csharp
[Test]
public void Test_CapabilityState_EnableDisable()
{
    var entity = new Entity();
    var typeId = MovementCapability.TypeId; // 使用 TypeId
    
    // 启用 Capability（添加状态即表示拥有）
    entity.CapabilityStates[typeId] = new CapabilityState
    {
        IsActive = false
    };
    
    Assert.IsTrue(entity.CapabilityStates.ContainsKey(typeId));
    Assert.IsFalse(entity.CapabilityStates[typeId].IsActive);
    
    // 激活 Capability
    var state = entity.CapabilityStates[typeId];
    state.IsActive = true;
    entity.CapabilityStates[typeId] = state;
    
    Assert.IsTrue(entity.CapabilityStates[typeId].IsActive);
}
```

#### 测试 2：Tag 禁用/启用

```csharp
[Test]
public void Test_TagDisable_Enable()
{
    var world = new World();
    world.CapabilitySystem = new CapabilitySystem();
    world.CapabilitySystem.Initialize();
    world.CapabilitySystem.World = world;
    
    var entity = new Entity();
    entity.World = world;
    
    // 注册 MovementCapability（Tag: "Movement"）
    var typeId = MovementCapability.TypeId; // 使用 TypeId
    entity.CapabilityStates[typeId] = new CapabilityState
    {
        IsActive = true,
        ActiveDuration = 0,
        DeactiveDuration = 0
    };
    
    // 禁用 Movement Tag
    world.CapabilitySystem.DisableCapabilitiesByTag(
        entity, CapabilityTag.Movement, 999, "Test"
    );
    
    Assert.IsTrue(entity.DisabledTags.ContainsKey(CapabilityTag.Movement));
    
    // 启用 Movement Tag
    world.CapabilitySystem.EnableCapabilitiesByTag(
        entity, CapabilityTag.Movement, 999
    );
    
    Assert.IsFalse(entity.DisabledTags.ContainsKey(CapabilityTag.Movement));
}
```

#### 测试 3：ShouldActivate/Deactivate

```csharp
[Test]
public void Test_ShouldActivate_Deactivate()
{
    var entity = new Entity();
    var capability = new MovementCapability();
    
    // 添加必需组件
    entity.AddComponent(new LSInputComponent());
    entity.AddComponent(new MovementComponent());
    entity.AddComponent(new TransComponent());
    
    // 启用 Capability（使用 TypeId，存在即表示拥有）
    var typeId = MovementCapability.TypeId;
    entity.CapabilityStates[typeId] = new CapabilityState
    {
        IsActive = false
    };
    
    // 测试激活条件
    Assert.IsTrue(capability.ShouldActivate(entity));
    
    // 移除组件后测试停用条件
    entity.RemoveComponent<LSInputComponent>();
    Assert.IsTrue(capability.ShouldDeactivate(entity));
}
```

### 7.2 集成测试

#### 测试 4：完整的 Capability 生命周期

```csharp
[Test]
public void Test_FullCapabilityLifecycle()
{
    var world = new World();
    var entity = world.CreateEntityByConfig(1001); // Role 原型
    
    // 1. 检查 Capability 是否正确装配（使用 TypeId）
    var movementTypeId = MovementCapability.TypeId;
    Assert.IsTrue(entity.CapabilityStates.ContainsKey(movementTypeId));
    
    // 2. 测试 Capability 更新
    world.CapabilitySystem.Update(world);
    
    // 3. 测试 SubArchetype 挂载/卸载（使用 TypeId）
    entity.AttachSubArchetype("AI", out var reason);
    var aiTypeId = AIFSMCapability.TypeId;
    Assert.IsTrue(entity.CapabilityStates.ContainsKey(aiTypeId));
    
    entity.DetachSubArchetype("AI", out reason);
    Assert.IsFalse(entity.CapabilityStates.ContainsKey(aiTypeId));
}
```

### 7.3 性能测试

#### 测试 5：大规模实体更新性能

```csharp
[Test]
public void Test_PerformanceComparison()
{
    const int entityCount = 1000;
    var world = new World();
    var entities = new List<Entity>();
    
    // 创建 1000 个实体
    for (int i = 0; i < entityCount; i++)
    {
        entities.Add(world.CreateEntityByConfig(1001));
    }
    
    // 测试旧方案（如果保留）
    var sw1 = Stopwatch.StartNew();
    foreach (var entity in entities)
    {
        UpdateEntityCapabilities_Legacy(entity);
    }
    sw1.Stop();
    
    // 测试新方案
    var sw2 = Stopwatch.StartNew();
    world.CapabilitySystem.Update(world);
    sw2.Stop();
    
    Console.WriteLine($"Legacy: {sw1.ElapsedMilliseconds}ms");
    Console.WriteLine($"New: {sw2.ElapsedMilliseconds}ms");
    Console.WriteLine($"Improvement: {(1 - sw2.ElapsedMilliseconds / (double)sw1.ElapsedMilliseconds) * 100:F2}%");
}
```

---

## 8. 风险评估与应对

### 8.1 风险列表

| 风险 | 等级 | 影响 | 应对措施 |
|------|------|------|---------|
| 迁移过程中引入 Bug | 高 | 游戏逻辑错误 | 分批迁移，每个 Capability 迁移后进行充分测试 |
| 性能不达预期 | 中 | 无法达到优化目标 | 保留性能测试对比，必要时回滚 |
| 序列化兼容性问题 | 高 | 存档无法加载 | 编写存档升级工具，保留旧版本兼容代码 |
| 代码量大导致延期 | 中 | 开发周期延长 | 采用双轨制，分阶段迁移 |
| Tag 系统误用 | 低 | 逻辑错误 | 编写 Tag 使用规范文档，代码审查 |

### 8.2 回滚方案

如果新方案出现严重问题，可以快速回滚：

1. **保留旧代码**：在兼容期不删除旧的 Capability 实例模式
2. **开关控制**：通过配置开关在两种模式间切换
3. **存档兼容**：序列化时同时保存两种格式

```csharp
// 配置开关
public static class CapabilityConfig
{
    public static bool UseNewCapabilitySystem = true; // 可在运行时切换
}

// LSUpdater.cs - 可切换的更新方式
public void Update()
{
    foreach (var entity in GetActiveEntities())
    {
        if (CapabilityConfig.UseNewCapabilitySystem)
        {
            CapabilitySystem.Instance.UpdateEntity(entity);
        }
        else
        {
            UpdateEntityCapabilities_Legacy(entity);
        }
    }
}
```

---

## 10. 版本历史

### v1.5 (2025-11-04)
- ✅ 添加 `ActiveDuration` 和 `DeactiveDuration` 字段到 `CapabilityState`
- ✅ 添加 `TrackActiveDuration` 和 `TrackDeactiveDuration` 配置属性到 `ICapability`
- ✅ 将 `Initialize()` 方法统一改为 `OnAttached(Entity entity)`
- ✅ 移除兼容接口（无参数的 `OnActivate()` 和 `OnDeactivate()`）
- ✅ 将 `CapabilitySystem` 从单例改为 `World` 的成员变量
- ✅ 添加 `CapabilitySystem` 的 `MemoryPackable` 支持，用于帧同步回滚
- ✅ 更新顺序改为按 Capability 遍历（每个 Capability 更新所有拥有它的实体）

### v1.4 (2025-11-04)
- ✅ 移除 `CapabilityState.IsEnabled` 字段（字典存在即表示拥有）
- ✅ 移除 `CapabilityState.Disablers` 字段（使用 `Entity.DisabledTags` 统一管理）
- ✅ 将 `CapabilityTag` 从 `[Flags]` 枚举改为普通枚举

### v1.3 (2025-11-04)
- ✅ 将 `CapabilityTag` 从字符串改为枚举类型
- ✅ 将 `ICapability.Tags` 从 `IReadOnlySet<string>` 改为 `IReadOnlySet<CapabilityTag>`

### v1.2 (2025-11-04)
- ✅ 将 Capability 的 Key 从字符串改为 `TypeId`（基于 `TypeHash` 的整数）
- ✅ 添加 `TypeHash<T>` 和 `StringHashUtility` 工具类

### v1.1 (2025-11-04)
- ✅ 初始版本，包含完整的架构设计和迁移方案

---

## 11. 附录

### 11.1 术语表

| 术语 | 含义 |
|------|------|
| Capability | 能力，ECC 架构中的行为处理单元 |
| CapabilityState | Capability 在实体上的状态信息 |
| CapabilityTag | Capability 的标签枚举 |
| Instigator | 发起者，指禁用 Capability 的实体 |
| ShouldActivate | 判定 Capability 是否应该激活的接口方法 |
| ShouldDeactivate | 判定 Capability 是否应该停用的接口方法 |

### 11.2 参考文档

- [ECC-System ECC结构说明.md](./ECC-System%20ECC结构说明.md)
- [Archetype-System Archetype结构说明.md](./Archetype-System%20Archetype结构说明.md)
- [Serialization-Best-Practices 序列化最佳实践.md](./Serialization-Best-Practices%20序列化最佳实践.md)

### 11.3 变更记录

（已移至"版本历史"章节）

---

**文档结束**


