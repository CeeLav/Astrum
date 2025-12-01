# Change: 将 ECC 体系改为对象池管理以实现无 GC 目标

## Why

当前 AstrumLogic 战斗逻辑在 Entity、Component 的创建和销毁过程中产生大量 GC 分配，影响战斗系统的性能表现。特别是在高频创建/销毁场景（如技能特效、弹道、临时实体等）中，GC 压力会导致帧率不稳定。

通过将整个 ECC 体系（Entity、Component 及相关数据结构）纳入对象池管理，可以实现：

1. **消除高频对象的 GC 分配**：Entity 和 Component 的创建/销毁不再产生托管堆分配
2. **提升缓存友好性**：对象池复用对象，内存布局更紧凑，提升缓存命中率
3. **减少 GC 暂停时间**：大幅降低 GC 频率和暂停时间，提升战斗流畅度
4. **统一生命周期管理**：通过对象池统一管理对象的创建、回收和重置，降低出错风险

## What Changes

- **ADDED**: Entity 实现 `IPool` 接口，支持对象池管理
- **ADDED**: BaseComponent 实现 `IPool` 接口，支持对象池管理
- **MODIFIED**: EntityFactory 使用 ObjectPool 获取和回收 Entity 实例
- **MODIFIED**: ComponentFactory 使用 ObjectPool 获取和回收 Component 实例
- **ADDED**: Entity 和 Component 添加 `Reset()` 方法用于对象池回收时的状态重置
- **MODIFIED**: World.DestroyEntity 和 EntityFactory.DestroyEntity 改为回收对象而非直接销毁
- **MODIFIED**: Entity 内部集合（List、Dictionary）使用对象池管理或预分配容量
- **ADDED**: 对象池配置和预热机制，支持游戏启动时预分配常用对象

## Impact

- 影响的规范：`ecc-object-pooling` 功能（需要创建新规范）
- 影响的代码：
  - `AstrumProj/Assets/Script/AstrumLogic/Core/Entity.cs` - 实现 IPool，添加 Reset 方法
  - `AstrumProj/Assets/Script/AstrumLogic/Components/BaseComponent.cs` - 实现 IPool，添加 Reset 方法
  - `AstrumProj/Assets/Script/AstrumLogic/Factories/EntityFactory.cs` - 改用 ObjectPool 创建/回收 Entity
  - `AstrumProj/Assets/Script/AstrumLogic/Factories/ComponentFactory.cs` - 改用 ObjectPool 创建/回收 Component
  - `AstrumProj/Assets/Script/AstrumLogic/Core/World.cs` - 销毁逻辑改为回收
  - `AstrumProj/Assets/Script/CommonBase/ObjectPool.cs` - 可能需要扩展容量配置
  - 所有 Component 子类 - 需要实现 Reset 方法重置状态
  - 所有 Entity 子类 - 需要实现 Reset 方法重置状态（如果有）
- 性能影响：预期在战斗场景中 GC 分配减少 80% 以上，GC 暂停时间减少 70% 以上
- 兼容性：需要确保 MemoryPack 序列化与对象池兼容（序列化后的对象不来自对象池）
