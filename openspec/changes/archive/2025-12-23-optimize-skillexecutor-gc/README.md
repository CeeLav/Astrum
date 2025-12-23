# SkillExecutorCapability 0 GC 优化提案

## 📋 提案概述

本提案旨在优化 `SkillExecutorCapability` 的 GC 分配问题，将 GC 从 **125.4 KB/帧** 降至 **< 1 KB/帧**，实现接近 0 GC 的性能目标。

## 🎯 性能目标

| 指标 | 当前值 | 目标值 | 提升 |
|------|--------|--------|------|
| GC 分配 | 125.4 KB/帧 | < 1 KB/帧 | ~99% |
| GC.Alloc 次数 | 2052 次/帧 | < 50 次/帧 | ~98% |
| 耗时 | 3.09ms | < 2ms | ~35% |

## 📊 问题分析

根据 Unity Profiler 数据和代码审查，主要 GC 来源：

1. **LINQ ToList() 操作** (~80 KB/帧，60-70%)
   - 第 90-92 行：每帧创建新 List 存储触发事件
   
2. **CollisionFilter 分配** (~25 KB/帧，15-20%)
   - 第 258-262 行：每次碰撞检测创建新对象和 HashSet
   
3. **foreach 枚举器** (~5 KB/帧，3-5%)
   - 多处使用 foreach 循环可能产生枚举器 GC
   
4. **VFX 事件对象** (~10 KB/帧，5-10%)
   - 第 190-231 行：每次 VFX 触发创建新对象

## 🔧 优化方案

### Phase 1: 消除 LINQ ToList() 分配（主要优化）

**方案**: 使用预分配缓冲区替代 ToList()

```csharp
// ❌ 优化前
var triggersAtFrame = triggerEffects
    .Where(t => t.IsFrameInRange(currentFrame))
    .ToList();  // ← 每帧创建新 List

// ✅ 优化后
private List<TriggerFrameInfo> _triggerBuffer = new List<TriggerFrameInfo>(16);

_triggerBuffer.Clear();
for (int i = 0; i < triggerEffects.Count; i++)
{
    if (triggerEffects[i].IsFrameInRange(currentFrame))
        _triggerBuffer.Add(triggerEffects[i]);
}
```

**预期效果**: ~80 KB/帧 → 0 B

### Phase 2: 优化循环遍历

**方案**: 使用 for 循环替代 foreach

```csharp
// ❌ 优化前
foreach (var trigger in triggersAtFrame)
{
    ProcessTrigger(caster, actionInfo, trigger);
}

// ✅ 优化后
for (int i = 0; i < _triggerBuffer.Count; i++)
{
    ProcessTrigger(caster, actionInfo, _triggerBuffer[i]);
}
```

**预期效果**: ~5 KB/帧 → 0 B

### Phase 3: 复用 CollisionFilter 对象

**方案**: 使用实例字段复用 CollisionFilter

```csharp
// ❌ 优化前
var filter = new CollisionFilter
{
    ExcludedEntityIds = new HashSet<long> { caster.UniqueId },
    OnlyEnemies = false
};

// ✅ 优化后
private CollisionFilter _collisionFilter = new CollisionFilter
{
    ExcludedEntityIds = new HashSet<long>(),
    OnlyEnemies = false
};

_collisionFilter.ExcludedEntityIds.Clear();
_collisionFilter.ExcludedEntityIds.Add(caster.UniqueId);
```

**预期效果**: ~25 KB/帧 → 0 B

### Phase 4: VFX 事件对象池（可选）

**方案**: 实现 IPool 接口，使用对象池

**决策**: 先完成 Phase 1-3，根据实际 VFX 触发频率决定是否实施

**预期效果**: ~10 KB/帧 → 0 B（如果实施）

## 📁 文件结构

```
openspec/changes/optimize-skillexecutor-gc/
├── proposal.md                          # 提案说明
├── design.md                            # 详细设计文档
├── tasks.md                             # 实施任务清单
├── README.md                            # 本文件
└── specs/
    └── capability-system/
        └── spec.md                      # 规范增量
```

## 🚀 实施计划

| 阶段 | 工作内容 | 预计时间 | 优先级 |
|------|---------|---------|--------|
| Phase 1 | ToList() 优化 | 0.5 天 | 高 |
| Phase 2 | 循环优化 | 0.5 天 | 高 |
| Phase 3 | CollisionFilter 复用 | 0.5 天 | 中 |
| Phase 4 | VFX 对象池（可选）| 1 天 | 低 |
| 测试验证 | 性能和功能测试 | 0.5 天 | 高 |
| 文档更新 | 更新文档 | 0.5 天 | 中 |
| **总计** | - | **2-3.5 天** | - |

## ✅ 验证标准

### 性能验证
- [ ] Unity Profiler 显示 GC < 1 KB/帧
- [ ] GC.Alloc 次数 < 50 次/帧
- [ ] 耗时 < 2ms/帧

### 功能验证
- [ ] 技能释放正常
- [ ] VFX 触发正常
- [ ] 碰撞检测正常
- [ ] 效果触发正常
- [ ] 所有单元测试通过

### 稳定性验证
- [ ] 运行 30 分钟后内存稳定
- [ ] 无内存泄漏
- [ ] 无性能退化

## 📚 参考资料

- **优化模式**: `openspec/changes/archive/2025-12-05-optimize-capability-performance/ZERO_GC_OPTIMIZATION.md`
- **对象池系统**: `AstrumProj/Assets/Script/CommonBase/ObjectPool/ObjectPool.cs`
- **源代码**: `AstrumProj/Assets/Script/AstrumLogic/Capabilities/SkillExecutorCapability.cs`

## 🔍 关键决策

1. **为什么使用预分配缓冲区而非对象池？**
   - List<T> 的 Clear() 不释放容量，可以直接复用
   - 比对象池更简单，性能相当
   - 参考 ActionCapability 的成功案例

2. **为什么 Phase 4 是可选的？**
   - VFX 触发频率相对较低（< 20 次/帧）
   - Phase 1-3 已经能达到 > 95% 的 GC 减少
   - 可以根据实际测量结果决定是否实施

3. **为什么使用 for 循环而非 foreach？**
   - foreach 可能产生枚举器 GC（即使是 List<T>）
   - for 循环直接索引访问，零开销
   - 参考 ActionCapability 的成功案例

## 📝 状态

- **当前状态**: 🟡 待审批
- **验证状态**: ✅ 通过 `openspec-chinese validate --strict`
- **创建日期**: 2025-12-05
- **预计完成**: 2025-12-07 ~ 2025-12-09

## 🎉 预期成果

完成本提案后，SkillExecutorCapability 将实现：

- **GC 减少 ~99%**: 从 125.4 KB/帧 → < 1 KB/帧
- **性能提升 ~35%**: 从 3.09ms → < 2ms
- **代码质量提升**: 遵循 0 GC 最佳实践
- **维护性提升**: 代码更清晰，注释更完善

---

**下一步**: 等待用户审批，批准后开始实施

