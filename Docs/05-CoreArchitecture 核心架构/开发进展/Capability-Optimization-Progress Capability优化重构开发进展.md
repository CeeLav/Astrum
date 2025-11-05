# Capability 优化重构 - 开发进展

**项目**: Capability 系统优化重构（状态元化，向 ECS 靠拢）  
**创建日期**: 2025-11-04  
**最后更新**: 2025-11-04  
**版本**: v1.6  
**技术方案**: [Capability-Optimization-Proposal Capability优化重构方案.md](../ECC/Capability-Optimization-Proposal%20Capability优化重构方案.md)

---

## 📋 目录

1. [开发状态总览](#开发状态总览)
2. [迁移方案](#迁移方案)
3. [实施计划](#实施计划)
4. [待完成功能](#待完成功能)
5. [技术债务](#技术债务)

---

## 开发状态总览

### 当前版本
- **版本号**: v1.6 (基础架构完成，开始迁移)
- **状态**: 🟢 基础架构完成，开始 Capability 迁移
- **功能完成度**: 40% (基础架构 100%，系统集成 100%，Capability 迁移 10%)

### 阶段划分
- ✅ **Phase 0**: 技术方案设计 - **已完成**
  - ✅ 架构设计
  - ✅ 数据结构定义
  - ✅ 接口设计
  - ✅ 迁移方案
  - ✅ 性能分析
- ✅ **Phase 1**: 基础架构 - **已完成**
  - ✅ ICapability 接口定义
  - ✅ Capability<T> 抽象基类
  - ✅ CapabilitySystem 调度系统
  - ✅ Entity 新增字段（CapabilityStates、DisabledTags）
  - ✅ CapabilityTag 枚举
  - ✅ TypeHash<T> 工具类
  - ✅ Tag 禁用/启用系统
  - ✅ World 类集成 CapabilitySystem
  - ✅ EntityFactory 和 Archetype 装配流程调整
  - ✅ LSUpdater 更新逻辑调整
  - ✅ 序列化兼容性（MemoryPack 支持）
- 🟡 **Phase 2**: 简单 Capability 迁移 - **进行中**
  - ✅ MovementCapabilityV2 实现（新架构示例）
  - ⏳ 迁移 DeadCapability
  - ⏳ 迁移 SkillDisplacementCapability
- ⏳ **Phase 3**: 复杂 Capability 迁移 - **待开发**
- ⏳ **Phase 4**: 集成与优化 - **待开发**
- ⏳ **Phase 5**: 清理与文档 - **待开发**

---

## 迁移方案

### 向后兼容策略

为了平滑迁移，采用"双轨制"过渡方案：

#### 阶段 1：保留旧接口（兼容期）

1. **保留 Entity.Capabilities 字段**（标记为 `[Obsolete]`）
2. **Capability 基类同时支持实例模式和静态模式**
3. **LSUpdater 同时支持两种更新方式**

```csharp
// Entity.cs - 兼容旧代码
[Obsolete("Use CapabilityStates instead")]
public List<Capability> Capabilities { get; private set; } = new List<Capability>();

// LSUpdater.cs - 新方式更新
public void Update()
{
    // 新方式：统一调度（按 Capability 遍历，每个 Capability 更新所有拥有它的实体）
    if (CurrentWorld?.CapabilitySystem != null)
    {
        CurrentWorld.CapabilitySystem.Update(CurrentWorld);
    }
    
    // 旧方式：实例更新（兼容期保留，逐步迁移后移除）
    foreach (var entity in GetActiveEntities())
    {
        UpdateEntityCapabilities_Legacy(entity);
    }
}
```

#### 阶段 2：逐步迁移（分批重构）

1. **优先迁移简单的 Capability**（如 `MovementCapability`、`DeadCapability`）
2. **逐步迁移复杂的 Capability**（如 `ActionCapability`、`AIFSMCapability`）
3. **每个 Capability 迁移后进行单元测试**

#### 阶段 3：完全移除旧代码（清理期）

1. **移除 `Entity.Capabilities` 字段**
2. **移除 Capability 实例模式相关代码**
3. **更新所有文档和示例**

### World 类调整

```csharp
// World.cs - 添加 CapabilitySystem 成员变量
public partial class World
{
    /// <summary>
    /// Capability 统一调度系统
    /// </summary>
    public CapabilitySystem CapabilitySystem { get; set; }
    
    public World()
    {
        Entities = new Dictionary<long, Entity>();
        HitSystem = new HitSystem();
        SkillEffectSystem = new SkillEffectSystem();
        CapabilitySystem = new CapabilitySystem();
        CapabilitySystem.World = this;
        CapabilitySystem.Initialize();
    }
    
    /// <summary>
    /// MemoryPack 构造函数
    /// </summary>
    [MemoryPackConstructor]
    public World(/* ... 其他参数 ... */, CapabilitySystem capabilitySystem)
    {
        // ... 其他初始化 ...
        CapabilitySystem = capabilitySystem ?? new CapabilitySystem();
        CapabilitySystem.World = this;
        
        // 反序列化后需要重建 CapabilitySystem 的内部映射
        if (CapabilitySystem != null)
        {
            CapabilitySystem.Initialize();
        }
    }
}
```

### Archetype 装配流程调整

```csharp
// EntityFactory.cs - 新的装配流程
public static Entity CreateByArchetype(string archetypeName, World world)
{
    var entity = new Entity();
    var archetypeInfo = ArchetypeManager.Instance.Get(archetypeName);
    
    // 1. 装配 Components（不变）
    foreach (var componentType in archetypeInfo.Components)
    {
        var component = (BaseComponent)Activator.CreateInstance(componentType);
        entity.AddComponent(component);
    }
    
    // 2. 装配 Capabilities（新方式）
    foreach (var capabilityType in archetypeInfo.Capabilities)
    {
        // 获取 Capability 实例以获取 TypeId（静态方法）
        var capability = CapabilitySystem.GetCapability(capabilityType);
        if (capability == null)
        {
            ASLogger.Instance.Warning($"Capability {capabilityType.Name} not registered in CapabilitySystem");
            continue;
        }
        
        // 启用此 Capability（使用 TypeId 作为 Key，存在即表示拥有）
        entity.CapabilityStates[capability.TypeId] = new CapabilityState
        {
            IsActive = false, // 初始未激活，等待 ShouldActivate 判定
            ActiveDuration = 0,
            DeactiveDuration = 0,
            CustomData = new Dictionary<string, object>()
        };
        
        // 注册到 CapabilitySystem
        world.CapabilitySystem?.RegisterEntityCapability(entity.UniqueId, capability.TypeId);
        
        // 调用 OnAttached 回调
        capability.OnAttached(entity);
    }
    
    return entity;
}
```

### SubArchetype 装配流程调整

```csharp
// Entity.cs - AttachSubArchetype 调整
public bool AttachSubArchetype(string subArchetypeName, out string reason)
{
    // ... 原有逻辑 ...
    
    // 装配 Capabilities（新方式）
    foreach (var capabilityType in subInfo.Capabilities)
    {
        // 获取 Capability 实例以获取 TypeId（静态方法）
        var capability = CapabilitySystem.GetCapability(capabilityType);
        if (capability == null)
            continue;
        
        var typeId = capability.TypeId;
        var key = GetTypeKey(capabilityType); // 保留字符串 Key 用于引用计数
        
        // 引用计数（使用字符串 Key）
        if (!CapabilityRefCounts.TryGetValue(key, out var count))
            count = 0;
        CapabilityRefCounts[key] = count + 1;
        
        // 首次添加：启用 Capability（使用 TypeId，存在即表示拥有）
        if (count == 0)
        {
            CapabilityStates[typeId] = new CapabilityState
            {
                IsActive = false,
                ActiveDuration = 0,
                DeactiveDuration = 0,
                CustomData = new Dictionary<string, object>()
            };
            
            // 注册到 CapabilitySystem
            World?.CapabilitySystem?.RegisterEntityCapability(UniqueId, typeId);
            
            // 调用 OnAttached
            capability.OnAttached(this);
        }
    }
    
    return true;
}

// Entity.cs - DetachSubArchetype 调整
public bool DetachSubArchetype(string subArchetypeName, out string reason)
{
    // ... 原有逻辑 ...
    
    // 卸载 Capabilities（新方式）
    foreach (var capabilityType in subInfo.Capabilities)
    {
        // 获取 Capability 实例以获取 TypeId（静态方法）
        var capability = CapabilitySystem.GetCapability(capabilityType);
        if (capability == null)
            continue;
        
        var typeId = capability.TypeId;
        var key = GetTypeKey(capabilityType); // 保留字符串 Key 用于引用计数
        
        if (!CapabilityRefCounts.TryGetValue(key, out var count))
            count = 0;
        
        if (count > 0)
            count--;
        
        CapabilityRefCounts[key] = count;
        
        // 引用计数归零：移除 Capability（使用 TypeId）
        if (count == 0)
        {
            // 调用 OnDetached
            capability.OnDetached(this);
            
            // 移除状态（使用 TypeId）
            CapabilityStates.Remove(typeId);
            
            // 从 CapabilitySystem 注销
            World?.CapabilitySystem?.UnregisterEntityCapability(UniqueId, typeId);
        }
    }
    
    return true;
}

// World.cs - 销毁实体时清理 Capability 注册
public void DestroyEntity(long entityId)
{
    if (!Entities.TryGetValue(entityId, out var entity))
        return;
    
    // 清理 CapabilitySystem 中的注册
    CapabilitySystem?.UnregisterEntity(entityId);
    
    // ... 其他销毁逻辑 ...
    
    Entities.Remove(entityId);
}
```

---

## 实施计划

### 开发阶段

#### 第 1 阶段：基础架构（预计 1-2 周）

- [ ] 定义 `ICapability` 接口
- [ ] 实现 `CapabilityBase` 抽象基类（`Capability<T>`）
- [ ] 实现 `CapabilitySystem` 调度系统
- [ ] 在 `Entity` 中添加 `CapabilityStates` 字段
- [ ] 实现 `CapabilityTag` 枚举
- [ ] 实现 `TypeHash<T>` 工具类
- [ ] 实现 Tag 系统核心逻辑
- [ ] 编写基础单元测试

#### 第 2 阶段：迁移简单 Capability（预计 2-3 周）

- [ ] 迁移 `MovementCapability`
- [ ] 迁移 `DeadCapability`
- [ ] 迁移 `SkillDisplacementCapability`
- [ ] 每个 Capability 迁移后进行单元测试和集成测试

#### 第 3 阶段：迁移复杂 Capability（预计 3-4 周）

- [ ] 迁移 `ActionCapability`
- [ ] 迁移 `SkillCapability`
- [ ] 迁移 `SkillExecutorCapability`
- [ ] 迁移 `AIFSMCapability` 及相关状态 Capability
- [ ] 进行性能对比测试

#### 第 4 阶段：集成与优化（预计 1-2 周）

- [ ] 调整 `EntityFactory` 和 `Archetype` 装配流程
- [ ] 实现序列化兼容性
- [ ] 进行大规模实体压力测试
- [ ] 修复发现的 Bug

#### 第 5 阶段：清理与文档（预计 1 周）

- [ ] 移除旧的 Capability 实例模式代码
- [ ] 更新 ECC 架构文档
- [ ] 编写 Capability 开发指南
- [ ] 编写 Tag 系统使用规范

### 里程碑

| 里程碑 | 目标 | 验收标准 |
|--------|------|---------|
| M1: 基础架构完成 | `CapabilitySystem` 可用 | 通过所有基础单元测试 |
| M2: 简单 Capability 迁移完成 | 至少 3 个 Capability 迁移 | 原有功能无损失 |
| M3: 复杂 Capability 迁移完成 | 所有 Capability 迁移 | 通过所有集成测试 |
| M4: 性能验证通过 | 内存减少 >50%，性能提升 >20% | 性能测试达标 |
| M5: 正式发布 | 移除旧代码，文档更新 | 代码审查通过 |

---

## 待完成功能

### 基础架构
- [ ] `ICapability` 接口定义
- [ ] `Capability<T>` 抽象基类实现
- [ ] `CapabilitySystem` 调度系统实现
- [ ] `Entity.CapabilityStates` 字段添加
- [ ] `CapabilityTag` 枚举定义
- [ ] `TypeHash<T>` 工具类实现
- [ ] Tag 禁用/启用系统实现

### Capability 迁移
- [ ] `MovementCapability` 迁移
- [ ] `DeadCapability` 迁移
- [ ] `SkillDisplacementCapability` 迁移
- [ ] `ActionCapability` 迁移
- [ ] `SkillCapability` 迁移
- [ ] `SkillExecutorCapability` 迁移
- [ ] `AIFSMCapability` 迁移
- [ ] 其他 Capability 迁移

### 系统集成
- [ ] `World` 类集成 `CapabilitySystem`
- [ ] `EntityFactory` 装配流程调整
- [ ] `Entity.AttachSubArchetype` 调整
- [ ] `Entity.DetachSubArchetype` 调整
- [ ] `World.DestroyEntity` 清理逻辑
- [ ] `LSUpdater` 更新逻辑调整

### 测试与验证
- [ ] 基础单元测试
- [ ] 集成测试
- [ ] 性能测试
- [ ] 序列化兼容性测试

### 文档与清理
- [ ] 移除旧代码
- [ ] 更新 ECC 架构文档
- [ ] 编写 Capability 开发指南
- [ ] 编写 Tag 系统使用规范

---

## 技术债务

### 当前技术债务
- 暂无

### 未来优化方向
- 并行处理优化（Job System）
- SIMD 优化
- 批量处理优化
- 缓存优化

---

## 变更记录

### 2025-11-04 (下午)
- ✅ 完成基础架构实现
  - ✅ ICapability 接口、Capability<T> 基类
  - ✅ CapabilitySystem 调度系统
  - ✅ Entity 新增字段和序列化支持
  - ✅ World 类集成 CapabilitySystem
  - ✅ EntityFactory 和 LSUpdater 调整
- ✅ 实现 MovementCapabilityV2（新架构示例）
- ✅ 修复序列化兼容性（CustomDataSerialized）
- ✅ 编译通过，0 个错误

### 2025-11-04 (上午)
- ✅ 创建开发进展文档
- ✅ 提取迁移方案和实施计划
- ✅ 生成初始待完成功能清单

---

**文档结束**

