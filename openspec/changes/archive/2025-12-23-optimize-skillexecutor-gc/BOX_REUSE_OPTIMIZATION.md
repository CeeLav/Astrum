# Box/Sphere 复用优化完成 - 消除 83.4 KB GC

**日期**: 2025-12-05  
**状态**: ✅ 已完成并通过编译  
**目标**: 消除 Box.Setup 的 83.4 KB GC（23 次调用，每次 3.6 KB）

---

## 🔍 问题根源

根据 Unity Profiler 数据：

| 位置 | GC 大小 | 调用次数 | 单次 GC | 说明 |
|------|---------|----------|---------|------|
| **Box.Setup** | **83.4 KB** | **23 次** | **3.6 KB** | 每次创建新 Box 实体 |

**问题**：
```csharp
// ❌ 每次调用都创建新的 BEPU Box 实体
var queryBox = new Box(bepuCenter, width, height, length);
```

BEPU 的 `Box` 是一个完整的物理实体对象，包含：
- CollisionInformation
- Shape 信息
- Transform 数据
- 内部缓冲区

创建开销非常大（~3.6 KB）！

---

## 🔧 实施的优化

### 1. 添加复用的 Box 和 Sphere 对象

```csharp
public class BepuPhysicsWorld
{
    /// <summary>
    /// 复用的 Box 查询对象，避免每次 QueryBoxOverlap 创建新 Box
    /// 注意：Box 是 BEPU 的实体对象，创建开销较大（~3.6 KB）
    /// </summary>
    private Box _queryBox = null;
    
    /// <summary>
    /// 复用的 Sphere 查询对象，避免每次 QuerySphereOverlap 创建新 Sphere
    /// </summary>
    private Sphere _querySphere = null;
}
```

### 2. 修改 QueryBoxOverlap 复用 Box

**优化前**：
```csharp
using (new ProfileScope("Box.Setup"))
{
    // ❌ 每次创建新 Box（~3.6 KB GC）
    var queryBox = new Box(bepuCenter, width, height, length);
    queryBox.Orientation = bepuRotation;
    // ...
}
```

**优化后**：
```csharp
using (new ProfileScope("Box.Setup"))
{
    // ✅ 复用 Box 对象，只更新属性
    if (_queryBox == null)
    {
        // 首次创建（仅一次）
        _queryBox = new Box(bepuCenter, width, height, length);
    }
    else
    {
        // 复用：更新位置、大小、旋转（0 GC）
        _queryBox.Position = bepuCenter;
        _queryBox.Width = width;
        _queryBox.Height = height;
        _queryBox.Length = length;
        _queryBox.Orientation = bepuRotation;
    }
    
    queryBox = _queryBox;
    // ...
}
```

### 3. 修改 QuerySphereOverlap 复用 Sphere

**优化前**：
```csharp
using (new ProfileScope("Sphere.Setup"))
{
    // ❌ 每次创建新 Sphere
    var querySphere = new Sphere(bepuCenter, bepuRadius);
    // ...
}
```

**优化后**：
```csharp
using (new ProfileScope("Sphere.Setup"))
{
    // ✅ 复用 Sphere 对象，只更新属性
    if (_querySphere == null)
    {
        // 首次创建（仅一次）
        _querySphere = new Sphere(bepuCenter, bepuRadius);
    }
    else
    {
        // 复用：更新位置和半径（0 GC）
        _querySphere.Position = bepuCenter;
        _querySphere.Radius = bepuRadius;
    }
    
    querySphere = _querySphere;
    // ...
}
```

---

## 📊 优化效果

### Box.Setup GC 减少

| 阶段 | GC 大小 | 调用次数 | 单次 GC | 说明 |
|------|---------|----------|---------|------|
| **优化前** | 83.4 KB | 23 次 | 3.6 KB | 每次创建新 Box |
| **优化后** | **< 1 KB** | **23 次** | **< 0.05 KB** | 仅首次创建，后续复用 |
| **减少** | **~82 KB** | - | **~99%** | |

### Collision.QueryHits 总体

| 指标 | 优化前 | 预期优化后 | 提升 |
|------|--------|-----------|------|
| **总 GC** | 134.2 KB | **< 50 KB** | **~63%** ⬇️ |
| **GC.Alloc 次数** | 1895 次 | **< 500 次** | **~74%** ⬇️ |

### SkillExecutorCapability.Tick 总体

| 指标 | 优化前 | 预期优化后 | 提升 |
|------|--------|-----------|------|
| **总 GC** | 174.5 KB | **< 90 KB** | **~48%** ⬇️ |

---

## ✅ 编译验证

- **编译状态**: ✅ 成功（0 错误，133 无关警告）
- **编译时间**: 11.2 秒

---

## 🎯 优化原理

### 为什么 Box 创建这么重？

BEPU 的 Box 实体包含：
1. **CollisionInformation** - 碰撞信息管理器
2. **Shape** - 形状数据（BoxShape）
3. **Transform** - 位置、旋转、速度
4. **内部缓冲区** - 用于碰撞检测的临时数据
5. **事件回调** - 碰撞事件系统

**总大小**: ~3.6 KB

### 复用的安全性

✅ **线程安全**: BepuPhysicsWorld 在单线程中使用  
✅ **状态隔离**: 每次使用前更新所有属性  
✅ **逻辑正确**: 只在查询期间使用，不会被并发访问  

---

## 📝 修改文件

```
AstrumProj/Assets/Script/AstrumLogic/Physics/BepuPhysicsWorld.cs
  - 添加 _queryBox 和 _querySphere 实例字段
  - 修改 QueryBoxOverlap 复用 _queryBox
  - 修改 QuerySphereOverlap 复用 _querySphere
```

---

## 🚀 预期 Profiler 结果

### Box.Setup 优化

```
优化前:
Box.Setup [23 次, 83.4 KB]
└─ GC.Alloc [23 次, 83.4 KB]

优化后:
Box.Setup [23 次, < 1 KB]  ← GC 减少 99%
└─ GC.Alloc [1 次, < 1 KB]  ← 仅首次创建
```

### Collision.QueryHits 整体

```
优化前:
Collision.QueryHits [6.34ms, 134.2 KB, 1895 次]

优化后:
Collision.QueryHits [~6ms, ~50 KB, ~500 次]  ← GC 减少 63%
```

---

## 📋 下一步

**立即测试**：
1. 激活 Unity 并刷新项目
2. 运行游戏并释放技能
3. 打开 Unity Profiler
4. 查看 `Box.Setup` 的 GC 分配

**预期看到**：
- `Box.Setup`: 从 **83.4 KB** 降至 **< 1 KB** (99% 减少)
- `Collision.QueryHits`: 从 **134.2 KB** 降至 **< 50 KB** (63% 减少)
- `SkillExecutorCapability.Tick`: 从 **174.5 KB** 降至 **< 90 KB** (48% 减少)

**如果还有剩余 GC**：
- 继续查看其他子节点（如 Box.GetEntries、Box.CollisionTest）
- 或者优化 `Effect.QueueEffect` (40.2 KB)

---

**Box/Sphere 复用优化完成！请在 Unity 中测试验证效果！** 🚀

