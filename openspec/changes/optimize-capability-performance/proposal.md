# Change: 优化高耗时 Capability 的性能和 GC 分配

**状态**: 🟡 待审批

## Why

Unity Profiler 显示多个 Capability 在每帧产生显著的性能开销和 GC 分配：

| Capability | 耗时 | GC 分配 | 占比 |
|-----------|------|---------|------|
| BattleStateCapability | 7.08ms | 0.7MB | 31.3% |
| ActionCapability | 2.40ms | 88.4KB | 10.6% |
| SkillDisplacementCapability | 1.05ms | 26.6KB | 4.6% |
| SkillExecutorCapability | 0.78ms | 42.1KB | 3.4% |
| **总计** | **11.31ms** | **~0.9MB** | **49.9%** |

这些 Capability 在 LSUpdater.UpdateWorld 中占据了约 **50% 的帧时间**和 **0.9MB 的 GC 分配**，严重影响游戏性能。

### 根本原因

1. **BattleStateCapability** (最严重):
   - 每帧遍历 `world.Entities` 字典寻找最近敌人（O(n) 复杂度）
   - 每帧创建新的 `LSInput` 对象
   - 缺少空间索引结构（如空间哈希、四叉树）

2. **ActionCapability**:
   - 大量 LINQ 操作和集合遍历
   - 动作候选列表频繁重建
   - 字符串比较和临时集合分配

3. **通用问题**:
   - 缺少对象池复用机制
   - 未缓存频繁查询的结果
   - 热路径中的装箱/拆箱操作
   - 过度的组件查询

### 性能目标

根据项目约定（`openspec/project.md`）：
- **帧率要求**: 95% 帧内总耗时 < 16ms，99% 帧内 < 22ms
- **GC 要求**: 热路径在 10 秒窗口内托管堆分配接近 0
- **目标**: 将这些 Capability 的总耗时降至 < 3ms，GC 分配 < 100KB/帧

## What Changes

### Phase 1: 优化 BattleStateCapability 目标查询（优先级最高）

- **MODIFIED**: `BattleStateCapability` - 添加目标缓存，避免每帧查询
- **MODIFIED**: `BattleStateCapability` - 使用 BEPU 物理引擎的空间索引替代全量遍历
- **MODIFIED**: `BepuPhysicsWorld` - 添加 AABB 范围查询方法（如需）

### Phase 2: 启用 LSInput 对象池

- **MODIFIED**: 所有 `LSInput.Create()` 调用 - 启用 `isFromPool: true` 参数
- **MODIFIED**: `LSInputComponent.SetInput()` - 使用完后归还对象池
- **ADDED**: `LSInput.Reset()` 方法（如未实现） - 清空字段用于对象池复用

### Phase 3: 监控 GetComponent 性能

- **MODIFIED**: `Capability<T>.GetComponent()` - 添加 ProfileScope 监控查询性能
- **ADDED**: 性能监控数据收集 - 验证 GetComponent 是否足够快

### Phase 4: LINQ 优化

- **MODIFIED**: `ActionCapability` - 用 for/foreach 替代 LINQ
- **MODIFIED**: 相关 Capability - 消除热路径中的 LINQ 操作
- **ADDED**: 预分配的工作缓冲区，避免临时集合分配

## Impact

### 影响的规范

- `capability-system` (修改) - 增加对象池和缓存支持
- `world-management` (修改) - 增加空间索引系统
- `performance` (新增) - 定义性能优化最佳实践

### 影响的代码

**新增文件**:
- `AstrumLogic/Systems/SpatialIndexSystem.cs` - 空间索引实现
- `AstrumLogic/Core/CapabilityQueryCache.cs` - 查询缓存
- `CommonBase/ObjectPool/LSInputPool.cs` - LSInput 对象池

**修改文件**:
- `AstrumLogic/Capabilities/BattleStateCapability.cs` - 使用空间索引
- `AstrumLogic/Capabilities/ActionCapability.cs` - 消除 LINQ，使用缓存
- `AstrumLogic/Capabilities/SkillDisplacementCapability.cs` - 对象池优化
- `AstrumLogic/Capabilities/SkillExecutorCapability.cs` - 查询缓存
- `AstrumLogic/Core/World.cs` - 集成空间索引
- `AstrumLogic/Core/Capability.cs` - 添加缓存支持

### 预期性能提升

| Capability | 优化前 | 预期优化后 | 提升 |
|-----------|--------|-----------|------|
| BattleStateCapability | 7.08ms / 0.7MB | <0.5ms / <10KB | **~93%** |
| ActionCapability | 2.40ms / 88.4KB | <1ms / <20KB | **~60%** |
| 其他 | 1.83ms / ~100KB | <1ms / <30KB | **~50%** |
| **总计** | 11.31ms / 0.9MB | **<2.5ms / <60KB** | **~78%** |

### 兼容性

- ✅ 向后兼容 - 不改变公开 API
- ✅ 不影响游戏逻辑 - 纯性能优化
- ⚠️ 需要性能测试验证 - 确保优化有效
- ⚠️ 需要内存测试 - 确保对象池不泄漏

### 风险

1. **空间索引维护成本** - 需要在实体移动时更新索引
2. **对象池复杂度** - 需要正确管理生命周期
3. **缓存一致性** - 需要在组件变化时失效缓存
4. **测试覆盖** - 需要全面的性能和正确性测试

## Dependencies

- 无外部依赖
- 建议先完成 Phase 1（空间索引），影响最大
- Phase 2-4 可并行进行

## Success Criteria

- [ ] Unity Profiler 显示 Capability 总耗时 < 3ms
- [ ] GC 分配 < 100KB/帧
- [ ] 所有现有单元测试通过
- [ ] 性能测试显示稳定的帧率提升
- [ ] 无内存泄漏（运行 30 分钟后内存稳定）
- [ ] 游戏逻辑行为完全一致（与优化前）

