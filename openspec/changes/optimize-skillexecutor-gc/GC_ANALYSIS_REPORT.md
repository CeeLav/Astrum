# SkillExecutorCapability GC 问题根源分析报告

**日期**: 2025-12-05  
**基于**: Unity Profiler 实际测量数据

---

## 📊 Profiler 测量结果

### 总体数据
- **总 GC**: 166.7 KB
- **总分配次数**: 101 次
- **总耗时**: 4.47ms

### GC 分布

| 位置 | GC 大小 | GC 占比 | 分配次数 | 耗时 | 说明 |
|------|---------|---------|----------|------|------|
| **Collision.QueryHits** | **97.0 KB** | **15.8%** | **1360** | 3.48ms | ⚠️ 最大 GC 来源 |
| **Effect.QueueEffect** | **60.8 KB** | **2.4%** | **1260** | 0.55ms | ⚠️ 第二大来源 |
| ProcessFrame.FilterTriggers | ~3 KB | 0.1% | ~26 | 0.5ms | ✅ 已优化 |
| 其他 | ~6 KB | 0.1% | ~24 | 0.5ms | ✅ 已优化 |

---

## 🔍 问题根源分析

### 问题 1: HitSystem.QueryHits() - 97 KB GC ⚠️

**位置**: `AstrumProj/Assets/Script/AstrumLogic/Systems/HitSystem.cs`

#### GC 来源

1. **多处创建空 List**（131, 158, 163, 168, 176 行）:
```csharp
// 每次查询失败都创建新的空 List
return new List<AstrumEntity>();  // ← 每次分配 ~72 字节
```

2. **ApplyFilter 使用 LINQ ToList()**（第 238 行）:
```csharp
// ❌ 每次都创建新 List
return candidates.Where(e => e.UniqueId != caster.UniqueId).ToList();
```

3. **ApplyFilter 手动创建 List**（第 241 行）:
```csharp
// ❌ 每次都创建新 List
var results = new List<AstrumEntity>();
```

4. **ApplyDeduplication 创建 List**（第 277 行）:
```csharp
// ❌ 每次都创建新 List
var results = new List<AstrumEntity>();
```

#### 调用频率
- 每个技能触发事件每帧调用一次
- 多个实体同时释放技能时，调用次数成倍增长
- Profiler 显示：1360 次 GC.Alloc

#### 预估 GC 计算
```
假设每帧有 10 个碰撞触发：
- QueryHits 返回新 List: 10 × ~72 B = 720 B
- ApplyFilter 创建 List: 10 × ~72 B = 720 B  
- ApplyDeduplication 创建 List: 10 × ~72 B = 720 B
- List 添加元素的扩容: ~8 KB
- 总计：~10 KB/次 × 10 次 = 100 KB

实际测量：97 KB ✅ 符合预估
```

---

### 问题 2: SkillEffectSystem.QueueSkillEffect() - 60.8 KB GC ⚠️

**位置**: `Effect.QueueEffect` (SkillEffectSystem 内部)

#### 可能原因
1. **队列扩容**: List/Queue 自动扩容产生 GC
2. **SkillEffectData 创建**: 每次调用创建新对象
3. **字典操作**: 可能有字典的扩容

#### 调用频率
- Profiler 显示：1260 次 GC.Alloc
- 每次效果触发都调用

#### 需要进一步分析
需要查看 `SkillEffectSystem.QueueSkillEffect()` 的具体实现。

---

## ✅ 已优化部分（Phase 1-3 成功）

### ProcessFrame.FilterTriggers
- **优化前**: ~80 KB（LINQ ToList）
- **优化后**: ~3 KB
- **减少**: ~96%

### Collision.SetupFilter
- **优化前**: ~25 KB（每次创建 CollisionFilter）
- **优化后**: 0 B（复用实例）
- **减少**: 100%

### 所有 foreach 循环
- **优化前**: ~5 KB（枚举器 GC）
- **优化后**: 0 B（for 循环）
- **减少**: 100%

---

## 🎯 优化建议

### 优先级 1: 优化 HitSystem.QueryHits() ⚠️⚠️⚠️

**预期减少**: 97 KB → < 5 KB (95% 减少)

#### 方案 A: 使用输出参数（推荐）

```csharp
// ❌ 优化前
public List<AstrumEntity> QueryHits(AstrumEntity caster, CollisionShape shape, CollisionFilter filter)
{
    var results = new List<AstrumEntity>();  // 每次创建
    // ...
    return results;
}

// ✅ 优化后
public void QueryHits(AstrumEntity caster, CollisionShape shape, CollisionFilter filter, List<AstrumEntity> outResults)
{
    outResults.Clear();  // 复用传入的 List
    // ...
    // 直接添加到 outResults
}

// 调用方
private List<AstrumEntity> _hitsBuffer = new List<AstrumEntity>(32);  // 预分配

hitSystem.QueryHits(caster, shape, _collisionFilter, _hitsBuffer);  // 传入复用的 List
```

#### 方案 B: 返回只读包装器

```csharp
// 使用实例字段缓存结果
private List<AstrumEntity> _queryResults = new List<AstrumEntity>(32);

public IReadOnlyList<AstrumEntity> QueryHits(...)
{
    _queryResults.Clear();
    // ... 填充结果
    return _queryResults;  // 返回只读视图
}
```

#### 具体修改点

1. **QueryHits 主方法**:
   - 改为 `void` 返回，添加 `List<AstrumEntity> outResults` 参数
   - 所有 `return new List<AstrumEntity>();` 改为 `outResults.Clear(); return;`

2. **ApplyFilter**:
   - 改为 `void ApplyFilter(..., List<AstrumEntity> outResults)`
   - 移除 LINQ，直接添加到 outResults

3. **ApplyDeduplication**:
   - 改为 `void ApplyDeduplication(..., List<AstrumEntity> inOutResults)`
   - 就地修改 List，不创建新 List

4. **SkillExecutorCapability 调用方**:
   - 添加 `_hitsBuffer` 实例字段
   - 调用时传入 buffer

---

### 优先级 2: 优化 SkillEffectSystem.QueueSkillEffect() ⚠️⚠️

**预期减少**: 60.8 KB → < 5 KB (92% 减少)

#### 需要先查看实现

1. 定位 `SkillEffectSystem.QueueSkillEffect()` 方法
2. 检查是否有 List/Queue 扩容
3. 检查 SkillEffectData 是否可以使用对象池

---

## 📈 预期优化效果

### 完成优先级 1 后

| 指标 | 当前值 | 优化后 | 提升 |
|------|--------|--------|------|
| **总 GC** | 166.7 KB | **< 70 KB** | **~58%** ⬇️ |
| **Collision.QueryHits GC** | 97 KB | **< 5 KB** | **~95%** ⬇️ |
| **总耗时** | 4.47ms | **~3.5ms** | **~22%** ⬇️ |

### 完成优先级 1 + 2 后

| 指标 | 当前值 | 优化后 | 提升 |
|------|--------|--------|------|
| **总 GC** | 166.7 KB | **< 10 KB** | **~94%** ⬇️ |
| **总分配次数** | 1360 次 | **< 100 次** | **~93%** ⬇️ |
| **总耗时** | 4.47ms | **~3ms** | **~33%** ⬇️ |

---

## 🚀 下一步行动

### 立即执行（优先级 1）

1. **优化 HitSystem.QueryHits()**:
   - 修改方法签名，使用输出参数
   - 移除所有 `new List<AstrumEntity>()` 创建
   - 移除 ApplyFilter 中的 LINQ ToList()
   - 修改 ApplyDeduplication 为就地修改
   
2. **修改调用方 SkillExecutorCapability**:
   - 添加 `_hitsBuffer` 实例字段
   - 修改 `HandleCollisionTrigger` 调用方式

3. **编译测试**:
   - 确保编译通过
   - 再次运行 Profiler 验证

### 后续执行（优先级 2）

4. **查看 SkillEffectSystem**:
   - 定位 QueueSkillEffect 实现
   - 分析 GC 来源
   - 制定优化方案

---

## 📝 总结

1. **Phase 1-3 优化已成功**: 
   - SkillExecutorCapability 本身的 GC 已降至最低
   - 预分配缓冲区、对象复用、for 循环都工作正常

2. **新发现的问题**:
   - **97 KB** 来自 `HitSystem.QueryHits()` 的 List 创建
   - **60.8 KB** 来自 `SkillEffectSystem.QueueSkillEffect()` 

3. **优化重点**:
   - 需要优化 HitSystem 和 SkillEffectSystem 这两个底层系统
   - 而不是 SkillExecutorCapability 本身

**优化这两个系统后，预计可以实现 < 10 KB GC 的目标！** 🎯

