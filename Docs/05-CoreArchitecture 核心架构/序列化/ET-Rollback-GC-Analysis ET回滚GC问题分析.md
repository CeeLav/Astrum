# ET 回滚机制中的 GC 问题分析

## 问题描述

在 ET 框架的回滚机制中，如果快照恢复出来的实体 `isFromPool == false`，这些实体不会被对象池回收。当频繁回滚时，会产生大量临时对象，导致 GC 压力。

## 问题分析

### 1. 对象池回收机制

```61:77:AstrumProj/Assets/Script/CommonBase/ObjectPool.cs
public void Recycle(object obj)
{
    if (obj is IPool p)
    {
        if (!p.IsFromPool)
        {
            return;  // ⚠️ 关键：isFromPool == false 的对象不会被回收
        }

        // 防止多次入池
        p.IsFromPool = false;
    }

    Type type = obj.GetType();
    Pool pool = GetPool(type);
    pool.Return(obj);
}
```

**关键逻辑**：
- 如果对象的 `IsFromPool == false`，`Recycle()` 会直接返回
- 这些对象不会被回收到对象池
- 对象会被丢弃，依赖 GC 回收

### 2. 反序列化创建的对象

```28:35:AstrumProj/Assets/Script/CommonBase/Serialize/MemoryPackHelper.cs
public static object Deserialize(Type type, byte[] bytes, int index, int count)
{
    object o = MemoryPackSerializer.Deserialize(type, bytes.AsSpan(index, count));
    // ⚠️ 直接创建新对象，不经过对象池
    // 如果对象实现了 IPool，IsFromPool 默认为 false
    if (o is ISupportInitialize supportInitialize)
    {
        supportInitialize.EndInit();
    }
    return o;
}
```

**问题**：
- `MemoryPackSerializer.Deserialize` 直接创建新对象
- 不经过 `ObjectPool.Fetch()`，所以 `IsFromPool` 默认为 `false`
- 这些对象在销毁时不会被回收到对象池

### 3. 回滚流程中的对象生命周期

```144:169:AstrumProj/Assets/Script/AstrumLogic/Core/ClientLSController.cs
public void Rollback(int frame)
{
    var loadedWorld = LoadState(frame);  // 反序列化创建新 World（isFromPool == false）
    if (loadedWorld == null)
    {
        return;
    }
    
    Room.MainWorld.Cleanup();  // 清理旧世界
    Room.MainWorld = loadedWorld;  // 设置新世界
    // ...
}
```

```376:396:AstrumProj/Assets/Script/AstrumLogic/Core/World.cs
public virtual void Cleanup()
{
    // 清理所有全局事件
    GlobalEventQueue?.ClearAll();
    
    // 清理所有实体的个体事件
    foreach (var entity in Entities.Values)
    {
        entity?.ClearEventQueue();
    }
    
    foreach (var entity in Entities.Values)
    {
        entity.Destroy();  // ⚠️ 如果 entity.IsFromPool == false，不会被回收
    }
    Entities.Clear();
    
    // 清理系统资源
    HitSystem?.Dispose();
    SkillEffectSystem?.ClearQueue();
}
```

**问题流程**：
1. 回滚时，`LoadState()` 反序列化创建新的 World 和所有 Entity
2. 这些对象的 `IsFromPool == false`（因为不经过对象池）
3. `Cleanup()` 销毁旧世界的 Entity
4. 如果 Entity 实现了 `IPool` 且 `IsFromPool == false`，`Recycle()` 会直接返回
5. 这些 Entity 对象被丢弃，等待 GC 回收
6. 频繁回滚会产生大量临时对象，导致 GC 压力

## GC 影响分析

### 场景假设

假设一个战斗场景：
- 每帧有 100 个 Entity
- 每个 Entity 平均 1KB 内存
- 每秒回滚 10 次
- 每次回滚创建 100 个新 Entity

**计算**：
- 每次回滚：100 个 Entity × 1KB = 100KB
- 每秒回滚：100KB × 10 = 1MB
- 每分钟回滚：1MB × 60 = 60MB

**GC 影响**：
- 大量临时对象产生，增加 GC 频率
- GC 暂停时间增加，影响游戏流畅度
- 内存碎片增加，可能导致内存不足

### 实际影响

1. **GC 频率增加**：频繁回滚会产生大量临时对象，触发更频繁的 GC
2. **GC 暂停时间增加**：大量对象需要回收，GC 暂停时间变长
3. **内存碎片**：频繁分配和释放对象，导致内存碎片
4. **性能下降**：GC 暂停会影响游戏帧率，特别是在移动设备上

## 解决方案

### 方案 1：强制回收反序列化对象（推荐）

修改 `World.Cleanup()` 或 `EntityFactory.DestroyEntity()`，强制回收所有 Entity，即使 `IsFromPool == false`：

```csharp
public void DestroyEntity(Entity entity, World world)
{
    if (entity == null) return;

    // ... 销毁逻辑 ...

    // 强制回收 Entity（即使 isFromPool == false）
    if (entity is IPool poolEntity)
    {
        // 临时标记为来自对象池，以便回收
        bool originalIsFromPool = poolEntity.IsFromPool;
        poolEntity.IsFromPool = true;
        ObjectPool.Instance.Recycle(entity);
        // 注意：Recycle 内部会将 IsFromPool 设置为 false，所以不需要恢复
    }
    else
    {
        // 如果没有实现 IPool，直接丢弃（依赖 GC）
    }
}
```

**优点**：
- ✅ 反序列化创建的对象也能被回收
- ✅ 减少 GC 压力
- ✅ 实现简单

**缺点**：
- ⚠️ 需要确保对象状态已完全重置（通过 Reset 方法）
- ⚠️ 需要对象实现 IPool 接口

### 方案 2：反序列化时使用对象池

修改 `MemoryPackHelper.Deserialize()`，让反序列化时也尝试从对象池获取对象：

```csharp
public static object Deserialize(Type type, byte[] bytes, int index, int count)
{
    // 尝试从对象池获取对象
    object o = null;
    if (typeof(IPool).IsAssignableFrom(type))
    {
        // 如果类型实现了 IPool，尝试从对象池获取
        o = ObjectPool.Instance.Fetch(type, isFromPool: true);
    }
    
    // 如果对象池中没有，或者类型未实现 IPool，创建新对象
    if (o == null)
    {
        o = MemoryPackSerializer.Deserialize(type, bytes.AsSpan(index, count));
    }
    else
    {
        // 反序列化到现有对象
        MemoryPackSerializer.Deserialize(type, bytes.AsSpan(index, count), ref o);
    }
    
    if (o is ISupportInitialize supportInitialize)
    {
        supportInitialize.EndInit();
    }
    return o;
}
```

**优点**：
- ✅ 从源头解决问题，反序列化时就使用对象池
- ✅ 对象自动标记为 `IsFromPool == true`
- ✅ 销毁时自动回收

**缺点**：
- ⚠️ 需要确保对象状态已重置（MemoryPack 会覆盖，但集合等可能需要清理）
- ⚠️ 需要 MemoryPack 支持反序列化到现有对象（需要检查是否支持）

### 方案 3：添加专用的回收方法

在 `ObjectPool` 中添加一个强制回收方法，忽略 `IsFromPool` 标志：

```csharp
/// <summary>
/// 强制回收对象（忽略 IsFromPool 标志）
/// 用于回收反序列化创建的对象
/// </summary>
public void ForceRecycle(object obj)
{
    if (obj is IPool p)
    {
        // 临时设置为 true，以便回收
        bool originalIsFromPool = p.IsFromPool;
        p.IsFromPool = true;
        
        Type type = obj.GetType();
        Pool pool = GetPool(type);
        pool.Return(obj);
        
        // 回收后设置为 false（防止重复入池）
        p.IsFromPool = false;
    }
    else
    {
        // 如果没有实现 IPool，直接回收
        Type type = obj.GetType();
        Pool pool = GetPool(type);
        pool.Return(obj);
    }
}
```

然后在 `World.Cleanup()` 中使用：

```csharp
public virtual void Cleanup()
{
    // ... 清理逻辑 ...
    
    foreach (var entity in Entities.Values)
    {
        entity.Destroy();
        // 强制回收 Entity
        ObjectPool.Instance.ForceRecycle(entity);
    }
    Entities.Clear();
}
```

**优点**：
- ✅ 明确区分正常回收和强制回收
- ✅ 不影响现有的回收逻辑
- ✅ 可以处理未实现 IPool 的对象

**缺点**：
- ⚠️ 需要确保对象状态已重置
- ⚠️ 需要额外的代码维护

### 方案 4：混合方案（推荐用于生产环境）

结合方案 1 和方案 2：

1. **反序列化时使用对象池**（方案 2）
   - 从源头解决问题
   - 对象自动标记为 `IsFromPool == true`

2. **清理时强制回收**（方案 1）
   - 作为兜底方案
   - 确保所有对象都能被回收

3. **添加 Reset 方法**
   - 确保对象状态完全重置
   - 避免状态污染

## 实现建议

### 步骤 1：实现 Reset 方法

在 Entity 和 Component 中实现 `Reset()` 方法：

```csharp
public virtual void Reset()
{
    UniqueId = 0;
    Name = string.Empty;
    IsDestroyed = false;
    Components.Clear();
    CapabilityStates.Clear();
    ChildrenIds.Clear();
    ParentId = -1;
    // ... 重置所有字段 ...
}
```

### 步骤 2：修改反序列化逻辑

修改 `MemoryPackHelper.Deserialize()`，支持从对象池获取对象。

### 步骤 3：修改清理逻辑

修改 `World.Cleanup()` 和 `EntityFactory.DestroyEntity()`，确保所有对象都被回收。

### 步骤 4：添加验证

添加单元测试，验证：
- 反序列化创建的对象能被正确回收
- 对象状态被正确重置
- GC 分配减少

## 总结

**问题**：ET 框架回滚时，反序列化创建的对象 `isFromPool == false`，不会被对象池回收，导致大量 GC。

**影响**：
- 频繁回滚会产生大量临时对象
- GC 频率和暂停时间增加
- 游戏性能下降

**解决方案**：
1. 反序列化时使用对象池（推荐）
2. 清理时强制回收对象
3. 实现 Reset 方法确保状态重置

**建议**：采用混合方案，从源头解决问题，同时添加兜底机制，确保所有对象都能被正确回收。






