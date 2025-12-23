# SkillExecutorCapability ProfileScope 监控已添加

**日期**: 2025-12-05  
**状态**: ✅ 已完成并通过编译  

---

## 📊 添加的 ProfileScope 监控

为了帮助定位 GC 来源，已为 `SkillExecutorCapability` 的所有关键方法添加详细的 ProfileScope 监控。

### 监控层级结构

```
SkillExecutorCapability.Tick
├─ SkillExec.ProcessFrame
│  ├─ ProcessFrame.ClearBuffer
│  ├─ ProcessFrame.FilterTriggers
│  └─ ProcessFrame.ProcessTriggers
│     └─ SkillExec.ProcessTrigger
│        ├─ Trigger.SkillEffect
│        │  ├─ SkillEffect.Collision
│        │  │  ├─ Collision.SetupFilter
│        │  │  ├─ Collision.QueryHits
│        │  │  └─ Collision.TriggerEffects
│        │  │     └─ SkillExec.TriggerEffect
│        │  │        ├─ Effect.Projectile
│        │  │        └─ Effect.CreateData
│        │  │           └─ Effect.QueueEffect
│        │  ├─ SkillEffect.Direct
│        │  │  └─ SkillExec.TriggerEffect (同上)
│        │  └─ SkillEffect.Condition
│        │     └─ SkillExec.TriggerEffect (同上)
│        └─ Trigger.VFX
│           └─ VFX.BuildEventData
│              ├─ VFX.CreateEvent
│              └─ VFX.QueueEvent
```

### 详细监控点

| 作用域名称 | 监控内容 | 说明 |
|-----------|---------|------|
| **SkillExecutorCapability.Tick** | 整个 Tick 方法 | 顶层监控 |
| **SkillExec.ProcessFrame** | ProcessFrame 方法 | 处理当前帧触发事件 |
| **ProcessFrame.ClearBuffer** | 清空缓冲区 | 监控 List.Clear() |
| **ProcessFrame.FilterTriggers** | 过滤触发事件 | 监控 for 循环过滤 |
| **ProcessFrame.ProcessTriggers** | 遍历触发事件 | 监控 for 循环处理 |
| **SkillExec.ProcessTrigger** | 单个触发事件分发 | 监控类型分发 |
| **Trigger.SkillEffect** | 技能效果触发 | 监控技能效果处理 |
| **Trigger.VFX** | VFX 触发 | 监控 VFX 处理 |
| **SkillEffect.Collision** | 碰撞触发 | 监控碰撞检测 |
| **Collision.SetupFilter** | 设置碰撞过滤器 | 监控 filter 复用 |
| **Collision.QueryHits** | 查询碰撞命中 | 监控物理查询 |
| **Collision.TriggerEffects** | 触发碰撞效果 | 监控 for 循环 |
| **SkillEffect.Direct** | 直接触发 | 监控直接效果 |
| **SkillEffect.Condition** | 条件触发 | 监控条件效果 |
| **SkillExec.TriggerEffect** | 触发技能效果 | 监控效果入队 |
| **Effect.Projectile** | 处理投射物效果 | 监控投射物创建 |
| **Effect.CreateData** | 创建效果数据 | 监控数据构建 |
| **Effect.QueueEffect** | 入队效果 | 监控效果队列 |
| **VFX.BuildEventData** | 构建 VFX 事件数据 | 监控 VFX 数据创建 |
| **VFX.CreateEvent** | 创建 VFX 事件 | 监控事件对象创建 |
| **VFX.QueueEvent** | 入队 VFX 事件 | 监控事件队列 |

---

## 🔍 如何使用 Unity Profiler 定位 GC

### 1. 激活 Unity 并运行游戏

```bash
# 刷新 Unity 项目
Assets/Refresh
```

### 2. 打开 Unity Profiler

- 菜单：Window → Analysis → Profiler
- 或快捷键：Ctrl+7

### 3. 启用 Deep Profile

- 在 Profiler 窗口点击 "Deep Profile"
- 这样可以看到所有 ProfileScope 的详细信息

### 4. 查看 GC Alloc 列

在 Profiler 中找到 `SkillExecutorCapability.Tick`，展开查看子节点：

```
SkillExecutorCapability.Tick  [3.09ms, 125.4 KB]  ← 总耗时和 GC
├─ SkillExec.ProcessFrame  [2.5ms, 100 KB]
│  ├─ ProcessFrame.ClearBuffer  [0.01ms, 0 B]      ← 这个没有 GC ✅
│  ├─ ProcessFrame.FilterTriggers  [0.5ms, 80 KB]  ← 这里有 GC ❌
│  └─ ProcessFrame.ProcessTriggers  [2ms, 20 KB]
│     └─ SkillExec.ProcessTrigger  [1.5ms, 15 KB]
│        └─ Trigger.VFX  [1ms, 10 KB]              ← VFX 有 GC ❌
│           └─ VFX.BuildEventData  [0.8ms, 10 KB]  ← 找到问题！
```

### 5. 重点关注的指标

| 指标 | 说明 |
|------|------|
| **GC Alloc** | 每帧分配的内存（应该 < 1 KB） |
| **GC.Alloc 次数** | 每帧分配的对象数量（应该 < 50） |
| **Time ms** | 耗时（应该 < 2ms） |

---

## 🎯 预期发现

根据优化前的分析，预期会在以下地方发现 GC：

### ❌ 已优化但仍可能有 GC 的地方

1. **ProcessFrame.FilterTriggers** (~80 KB)
   - 虽然使用了 for 循环和预分配缓冲区
   - 但 `trigger.IsFrameInRange()` 内部可能有 GC
   - 需要进一步检查 `TriggerFrameInfo.IsFrameInRange()` 实现

2. **VFX.BuildEventData** (~10 KB)
   - `new VFXTriggerEventData { ... }` 创建新对象
   - `new VFXTriggerEvent { ... }` 创建新对象
   - 这是 Phase 4 的优化目标（VFX 对象池）

3. **Collision.QueryHits** (~20 KB)
   - `hitSystem.QueryHits()` 可能返回新的 List
   - 需要检查 HitSystem 的实现

4. **Effect.CreateData** (~5 KB)
   - `new SkillEffectData { ... }` 创建新对象
   - 可能需要对象池

### ✅ 应该没有 GC 的地方

1. **ProcessFrame.ClearBuffer** - List.Clear() 不产生 GC
2. **Collision.SetupFilter** - 复用现有对象
3. **Collision.TriggerEffects** - for 循环无枚举器 GC

---

## 📝 下一步行动

1. **运行游戏并查看 Profiler**
   - 激活 Unity
   - 运行游戏
   - 打开 Profiler 并启用 Deep Profile
   - 查看 SkillExecutorCapability.Tick 的详细信息

2. **定位主要 GC 来源**
   - 找到 GC Alloc 最大的子节点
   - 记录具体数值

3. **根据 Profiler 结果决定进一步优化**
   - 如果 VFX.BuildEventData 有显著 GC → 实施 Phase 4（VFX 对象池）
   - 如果其他地方有 GC → 分析具体原因并优化

---

## ✅ 编译验证

- **编译状态**: ✅ 成功
- **编译时间**: 7.6 秒
- **错误数**: 0
- **警告数**: 128（均为无关警告）

---

## 📋 修改文件

```
AstrumProj/Assets/Script/AstrumLogic/Capabilities/SkillExecutorCapability.cs
  - 添加 21 个 ProfileScope 监控点
  - 覆盖所有关键方法和代码块
```

---

**所有 ProfileScope 已添加完成！等待 Unity Profiler 测试！** 🔍

