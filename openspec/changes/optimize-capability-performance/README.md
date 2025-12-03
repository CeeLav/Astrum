# Capability 性能优化提案

## 📊 性能问题分析

Unity Profiler 显示多个 Capability 在每帧产生显著的性能开销：

| Capability | 耗时 | GC 分配 | 占比 |
|-----------|------|---------|------|
| BattleStateCapability | **7.08ms** | **0.7MB** | 31.3% |
| ActionCapability | 2.40ms | 88.4KB | 10.6% |
| SkillDisplacementCapability | 1.05ms | 26.6KB | 4.6% |
| SkillExecutorCapability | 0.78ms | 42.1KB | 3.4% |
| **总计** | **11.31ms** | **~0.9MB** | **49.9%** |

这占用了 **50% 的帧预算**和产生 **0.9MB 的 GC 分配/帧**！

## 🎯 优化目标

- **性能**: 从 11.31ms 降至 **< 3ms** (73% 提升)
- **GC**: 从 0.9MB/帧 降至 **< 100KB/帧** (89% 减少)
- **帧率**: 稳定 60 FPS，即使 100+ 实体

## 📁 提案文件结构

```
optimize-capability-performance/
├── proposal.md          # 提案概述和变更说明
├── design.md            # 详细架构设计
├── tasks.md             # 分阶段实施任务清单
├── README.md            # 本文件
└── specs/               # 规范增量
    ├── capability-system/
    │   └── spec.md      # Capability 系统优化规范
    ├── world-management/
    │   └── spec.md      # 空间索引系统规范
    └── performance/
        └── spec.md      # 性能优化最佳实践（新增）
```

## 🔧 优化方案（4 个阶段）

### Phase 1: BattleStateCapability 目标缓存 ⭐ (优先级最高)

**问题**: `BattleStateCapability` 每帧遍历所有实体查找目标 (O(N))

**解决**: 目标缓存 + BEPU 物理查询

```csharp
// 优化前: 每帧都遍历所有实体
foreach (var e in world.Entities) { ... }  // ← 100 个实体，每帧执行

// 优化后: 90% 的帧使用缓存目标
if (_cachedTargets.TryGetValue(entityId, out var targetId))
{
    var target = world.GetEntity(targetId);
    if (target != null && distance < RetargetDistance)
    {
        // 直接使用缓存，无需查询！
        return target;
    }
}

// 仅 10% 的帧需要查询，且使用 BEPU 物理索引
var nearby = world.PhysicsWorld.QueryAABB(aabb);  // ← 仅查询附近实体
```

**预期效果**: BattleStateCapability 从 7.08ms → **<0.5ms** (93% 提升)

### Phase 2: 对象池优化

**问题**: 每帧创建新的 `LSInput` 对象

**解决**: LSInput 对象池复用

```csharp
// 优化前: 每次分配新对象
var input = LSInput.Create();

// 优化后: 从对象池租借
var input = LSInputPool.Rent();
// ... 使用后归还
LSInputPool.Return(input);
```

**预期效果**: 减少 ~600KB/s 的 GC 分配

### Phase 3: 监控 GetComponent 性能

**假设**: GetComponent 本身应该很快（Dictionary 查询）

**验证**: 添加 ProfileScope 监控

```csharp
// Capability 基类
protected TComponent GetComponent<TComponent>(Entity entity)
{
    #if ENABLE_PROFILER
    using (new ProfileScope($"GetComponent<{typeof(TComponent).Name}>"))
    #endif
    {
        return entity.GetComponent<TComponent>();
    }
}
```

**决策**: 
- ✅ 如果 < 0.5ms/帧：无需优化
- ⚠️ 如果 > 1ms/帧：考虑缓存方案

### Phase 4: LINQ 优化

**问题**: ActionCapability 中大量 LINQ 操作产生临时集合

**解决**: 手动循环 + 预分配缓冲区

```csharp
// 优化前: LINQ 产生临时 List
var sorted = items.Where(i => i.IsValid)
                  .OrderByDescending(i => i.Priority)
                  .ToList();

// 优化后: 预分配缓冲区
_buffer.Clear();
foreach (var item in items)
    if (item.IsValid) _buffer.Add(item);
_buffer.Sort((a, b) => b.Priority.CompareTo(a.Priority));
```

**预期效果**: 消除 ~50KB/帧 的 LINQ 分配

## ⏱️ 实施时间

| 阶段 | 时间 | 优先级 |
|------|------|--------|
| Phase 1: 空间索引 | 1-2 天 | 🔴 最高 |
| Phase 2: 对象池 | 0.5-1 天 | 🟡 高 |
| Phase 3: 查询缓存 | 0.5-1 天 | 🟡 高 |
| Phase 4: LINQ 优化 | 1-2 天 | 🟢 中 |
| 验证和测试 | 0.5-1 天 | 🔴 最高 |
| **总计** | **4-7 天** | |

## ✅ 验证标准

### 性能目标

- [ ] Unity Profiler: LSUpdater.UpdateWorld < 5ms
- [ ] Memory Profiler: GC 分配 < 100KB/帧
- [ ] 帧率: 60 FPS 稳定（100+ 实体场景）
- [ ] 内存稳定: 运行 30 分钟无泄漏

### 正确性测试

- [ ] 所有单元测试通过
- [ ] 游戏逻辑行为一致（优化前后）
- [ ] 回放测试：录像文件一致
- [ ] AI 行为测试：寻路、战斗正确

## 📝 下一步

1. **审批**: 等待提案审批
2. **实施**: 按 `tasks.md` 分阶段实施
3. **验证**: 每阶段完成后验证性能和正确性
4. **归档**: 完成后归档到 `openspec/changes/archive/`

## 📚 参考文档

- `proposal.md` - 提案详情
- `design.md` - 技术设计
- `tasks.md` - 任务清单
- `specs/` - 规范增量

---

**创建日期**: 2025-12-03  
**状态**: 🟡 待审批  
**预期收益**: 73% 性能提升 + 89% GC 减少

