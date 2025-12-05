# BepuPhysicsWorld 和 HitSystem 详细 ProfileScope 监控已添加

**日期**: 2025-12-05  
**状态**: ✅ 已完成并通过编译  
**目标**: 为物理查询添加细粒度监控，定位具体 GC 来源

---

## 📊 添加的 ProfileScope 监控

### BepuPhysicsWorld.QueryBoxOverlap()

```
Physics.QueryBoxOverlap
├─ Box.Setup
│  ├─ 创建 queryBox
│  ├─ 计算 boundingBox
│  └─ UpdateBoundingBox()
├─ Box.BroadPhaseUpdate
│  └─ _space.BroadPhase.Update()
├─ Box.GetEntries
│  ├─ _candidatesBuffer.Clear()
│  └─ QueryAccelerator.GetEntries()
└─ Box.NarrowPhase
   ├─ Box.ExtractEntity
   │  └─ 提取 AstrumEntity from Tag
   ├─ Box.CollisionTest
   │  ├─ Box.BoxBoxCollider (Box vs Box)
   │  ├─ Box.GJK_Capsule (Box vs Capsule)
   │  └─ Box.GJK_Convex (Box vs 其他凸体)
   └─ Box.AddResult
      └─ outResults.Add()
```

### BepuPhysicsWorld.QuerySphereOverlap()

```
Physics.QuerySphereOverlap
├─ Sphere.Setup
│  ├─ 创建 querySphere
│  ├─ 计算 boundingBox
│  └─ UpdateBoundingBox()
├─ Sphere.GetEntries
│  ├─ _candidatesBuffer.Clear()
│  └─ QueryAccelerator.GetEntries()
└─ Sphere.ExtractEntities
   └─ 提取 AstrumEntity from Tag
```

### HitSystem 过滤监控

```
HitSys.ApplyFilter
├─ Filter.DefaultFilter (filter == null)
│  └─ 反向遍历移除施法者
└─ Filter.CustomFilter (filter != null)
   └─ 反向遍历应用过滤规则

HitSys.ApplyDedup
├─ Dedup.GetCache
│  └─ HitCache.TryGetValue()
└─ Dedup.RemoveDuplicates
   └─ 反向遍历移除重复命中
```

---

## 🔍 监控层级结构

### 完整调用链

```
SkillExecutorCapability.Tick
└─ SkillExec.ProcessFrame
   └─ ProcessFrame.ProcessTriggers
      └─ SkillExec.ProcessTrigger
         └─ Trigger.SkillEffect
            └─ SkillEffect.Collision
               ├─ Collision.SetupFilter
               ├─ Collision.QueryHits
               │  └─ Physics.QueryBoxOverlap / Physics.QuerySphereOverlap
               │     ├─ Box/Sphere.Setup
               │     ├─ Box.BroadPhaseUpdate (仅 Box)
               │     ├─ Box/Sphere.GetEntries
               │     ├─ Box.NarrowPhase / Sphere.ExtractEntities
               │     │  ├─ Box.ExtractEntity
               │     │  ├─ Box.CollisionTest
               │     │  │  ├─ Box.BoxBoxCollider
               │     │  │  ├─ Box.GJK_Capsule
               │     │  │  └─ Box.GJK_Convex
               │     │  └─ Box.AddResult
               │     └─ HitSys.ApplyFilter
               │        ├─ Filter.DefaultFilter
               │        └─ Filter.CustomFilter
               └─ Collision.TriggerEffects
```

---

## 🎯 重点监控目标

### 预期发现的 GC 来源

根据之前的 Profiler 数据（134.2 KB GC，1895 次分配），预期会在以下地方发现 GC：

| 位置 | 预期 GC | 说明 |
|------|---------|------|
| **Box.Setup** | ~20 KB | 创建 queryBox 和 boundingBox |
| **Box.BroadPhaseUpdate** | ~30 KB | BroadPhase.Update() 内部分配 |
| **Box.GetEntries** | ~50 KB | QueryAccelerator.GetEntries() |
| **Box.NarrowPhase** | ~20 KB | 碰撞检测算法 |
| **Box.CollisionTest** | ~10 KB | RigidTransform 创建 |
| **HitSys.ApplyFilter** | ~4 KB | RemoveAt() 可能的内部分配 |

### 关键问题

1. **RawList.Clear()** - BEPU 的 RawList 是否真的能复用？
2. **QueryAccelerator.GetEntries()** - 这个方法内部是否有分配？
3. **new Box() / new Sphere()** - 临时对象创建
4. **new RigidTransform()** - 每次碰撞检测创建
5. **CollisionInformation.UpdateBoundingBox()** - 是否有内部分配？

---

## 📋 Unity Profiler 测试指南

### 1. 查看物理查询详情

展开 `Collision.QueryHits`，应该能看到：

```
Collision.QueryHits
└─ Physics.QueryBoxOverlap / Physics.QuerySphereOverlap
   ├─ Box/Sphere.Setup [GC: ?]
   ├─ Box.BroadPhaseUpdate [GC: ?]
   ├─ Box/Sphere.GetEntries [GC: ?]
   └─ Box.NarrowPhase / Sphere.ExtractEntities [GC: ?]
```

### 2. 重点关注

- **哪个子节点的 GC 最大？**
- **GC.Alloc 次数最多的是哪个？**
- **是否有意外的分配？**

### 3. 记录数据

请记录以下数据：
- Box.Setup: ? KB, ? 次
- Box.BroadPhaseUpdate: ? KB, ? 次
- Box.GetEntries: ? KB, ? 次
- Box.NarrowPhase: ? KB, ? 次
- Box.CollisionTest: ? KB, ? 次

---

## ✅ 编译验证

- **编译状态**: ✅ 成功
- **添加的监控点**: 15 个
- **覆盖范围**: 
  - BepuPhysicsWorld.QueryBoxOverlap (8 个监控点)
  - BepuPhysicsWorld.QuerySphereOverlap (3 个监控点)
  - HitSystem.ApplyFilterInPlace (2 个监控点)
  - HitSystem.ApplyDeduplication (2 个监控点)

---

## 📝 修改文件

```
AstrumProj/Assets/Script/AstrumLogic/Physics/BepuPhysicsWorld.cs
  - QueryBoxOverlap: 添加 8 个 ProfileScope
  - QuerySphereOverlap: 添加 3 个 ProfileScope

AstrumProj/Assets/Script/AstrumLogic/Systems/HitSystem.cs
  - ApplyFilterInPlace: 添加 2 个 ProfileScope
  - ApplyDeduplication: 添加 2 个 ProfileScope
  - QueryHits: 添加 2 个 ProfileScope
```

---

## 🚀 下一步

**在 Unity Profiler 中查看**：

1. 展开 `Collision.QueryHits`
2. 展开 `Physics.QueryBoxOverlap` 或 `Physics.QuerySphereOverlap`
3. 查看每个子节点的 GC 分配
4. 找到 GC 最大的节点

**根据结果决定优化方向**：
- 如果 `Box.Setup` 有大量 GC → 考虑复用 queryBox
- 如果 `Box.GetEntries` 有大量 GC → 检查 BEPU 内部实现
- 如果 `Box.CollisionTest` 有大量 GC → 考虑缓存 RigidTransform

---

**所有 ProfileScope 已添加完成！请在 Unity 中测试并告诉我结果！** 🔍

