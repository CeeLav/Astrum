# Change: 优化 SkillExecutorCapability 实现 0 GC

**状态**: 🟢 已实施（Phase 1-3 完成，待 Unity 测试验证）

## Why

根据 Unity Profiler 数据，`SkillExecutorCapability` 在每帧产生 **125.4 KB** 的 GC 分配，占 `Cap.SkillExecutorCapability` 总耗时的 **15.5%**（3.09ms）。这是当前最主要的 GC 来源之一。

### 性能数据

| Capability | 耗时 | GC 分配 | GC.Alloc 次数 |
|-----------|------|---------|--------------|
| SkillExecutorCapability | 3.09ms | 125.4 KB | 2052 次 |
| CapSys.UpdateActivation | 0.06ms | 0 B | 101 次 |

### 根本原因

通过代码审查，发现主要 GC 来源：

1. **LINQ ToList() 操作** (第 90-92 行):
   ```csharp
   var triggersAtFrame = triggerEffects
       .Where(t => t.IsFrameInRange(currentFrame))
       .ToList();  // ← 每帧创建新 List，产生 GC
   ```
   - 每个技能动作每帧都会执行
   - 即使没有触发事件，也会创建空 List
   - 多个实体同时释放技能时，GC 成倍增长

2. **foreach 枚举器分配** (第 100-103 行):
   ```csharp
   foreach (var trigger in triggersAtFrame)  // ← 可能产生枚举器 GC
   {
       ProcessTrigger(caster, actionInfo, trigger);
   }
   ```

3. **VFX 事件数据分配** (第 190-231 行):
   ```csharp
   var eventData = new VFXTriggerEventData { ... };  // ← 每次创建新对象
   var vfxEvent = new VFXTriggerEvent { ... };       // ← 每次创建新对象
   ```

4. **CollisionFilter 分配** (第 258-262 行):
   ```csharp
   var filter = new CollisionFilter
   {
       ExcludedEntityIds = new HashSet<long> { caster.UniqueId },  // ← 每次创建 HashSet
       OnlyEnemies = false
   };
   ```

### 性能目标

根据项目约定和已完成的优化案例：
- **目标**: 将 SkillExecutorCapability 的 GC 分配降至 **< 1 KB/帧**
- **GC.Alloc 次数**: 从 2052 次降至 **< 50 次/帧**
- **耗时**: 保持或优化至 **< 2ms/帧**

## What Changes

### Phase 1: 消除 LINQ ToList() 分配（主要优化）

- **MODIFIED**: `SkillExecutorCapability.ProcessFrame()` - 使用预分配缓冲区替代 ToList()
- **ADDED**: 实例字段 `_triggerBuffer` - 预分配的 List<TriggerFrameInfo>

### Phase 2: 优化循环遍历

- **MODIFIED**: `SkillExecutorCapability.ProcessFrame()` - 使用 for 循环替代 foreach
- **MODIFIED**: `SkillExecutorCapability.HandleCollisionTrigger()` - 使用 for 循环遍历 hits

### Phase 3: 复用 CollisionFilter 对象

- **ADDED**: 实例字段 `_collisionFilter` - 复用的 CollisionFilter 对象
- **MODIFIED**: `SkillExecutorCapability.HandleCollisionTrigger()` - 复用 filter 而非每次创建

### Phase 4: VFX 事件对象池（可选）

- **MODIFIED**: `VFXTriggerEventData` - 实现 IPool 接口（如需要）
- **MODIFIED**: `VFXTriggerEvent` - 实现 IPool 接口（如需要）
- **MODIFIED**: `SkillExecutorCapability.ProcessVFXTrigger()` - 使用对象池

## Impact

### 影响的规范

- `capability-system` (修改) - 增加 0 GC 优化最佳实践

### 影响的代码

**修改文件**:
- `AstrumLogic/Capabilities/SkillExecutorCapability.cs` - 主要优化目标
- `AstrumLogic/Events/VFXTriggerEventData.cs` - 可能需要对象池支持
- `AstrumLogic/Events/VFXTriggerEvent.cs` - 可能需要对象池支持
- `AstrumLogic/Physics/CollisionFilter.cs` - 可能需要 Reset() 方法

### 预期性能提升

| 指标 | 优化前 | 预期优化后 | 提升 |
|------|--------|-----------|------|
| GC 分配 | 125.4 KB/帧 | **< 1 KB/帧** | **~99%** |
| GC.Alloc 次数 | 2052 次/帧 | **< 50 次/帧** | **~98%** |
| 耗时 | 3.09ms | **< 2ms** | **~35%** |

### 兼容性

- ✅ 向后兼容 - 不改变公开 API
- ✅ 不影响游戏逻辑 - 纯性能优化
- ✅ 遵循已有优化模式 - 参考 ActionCapability 优化案例
- ⚠️ 需要性能测试验证 - 确保优化有效

### 风险

1. **对象池复杂度** - VFX 事件对象池需要正确管理生命周期
2. **CollisionFilter 复用** - 需要确保每次使用前正确重置状态
3. **缓冲区大小** - 需要合理设置 _triggerBuffer 的初始容量

## Dependencies

- 无外部依赖
- 参考已完成的优化案例：
  - `2025-12-05-optimize-capability-performance` - ActionCapability 0 GC 优化
  - `2025-12-03-refactor-ecc-object-pooling` - 对象池系统

## Success Criteria

- [ ] Unity Profiler 显示 SkillExecutorCapability GC < 1 KB/帧
- [ ] GC.Alloc 次数 < 50 次/帧
- [ ] 所有现有单元测试通过
- [ ] 技能释放功能正常（VFX、碰撞检测、效果触发）
- [ ] 无内存泄漏（运行 30 分钟后内存稳定）
- [ ] 游戏逻辑行为完全一致（与优化前）

## References

- 性能数据来源：Unity Profiler 截图（用户提供）
- 优化模式参考：`openspec/changes/archive/2025-12-05-optimize-capability-performance/ZERO_GC_OPTIMIZATION.md`
- 对象池系统：`AstrumProj/Assets/Script/CommonBase/ObjectPool/ObjectPool.cs`

