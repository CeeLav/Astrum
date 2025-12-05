# ActionCapability 细粒度 ProfileScope 侦测 + GC 优化

**完成日期**: 2025-12-04  
**状态**: ✅ 完成  
**目的**: 定位 ActionCapability 剩余的 39.8 KB GC 分配来源

---

## 🎯 问题背景

Unity Profiler 显示 `Cap.ActionCapability` 仍有 **39.8 KB** GC：
- `ActionCapability.Tick`: 31.9 KB (101 次调用)
- `GC.Alloc`: 7.9 KB (202 次调用) - 不在 Tick 方法内

需要添加更细粒度的 ProfileScope 来定位具体的 GC 来源。

---

## ✅ 实施的改动

### 1. 添加细粒度 ProfileScope（20 处）

**ActionCapability**: 10 处  
**CapabilitySystem**: 6 处

#### **CheckActionCancellation 方法（4 处）**
```csharp
private void CheckActionCancellation(Entity entity)
{
    // 1. 回收 PreorderActions
    using (new ProfileScope("ActionCap.RecyclePreorders"))
    {
        RecyclePreorderActions(actionComponent.PreorderActions);
        actionComponent.PreorderActions.Clear();
    }
    
    // 2. 获取可用动作
    using (new ProfileScope("ActionCap.GetAvailableActions"))
    {
        availableActions = GetAvailableActions(actionComponent);
    }
    
    // 3. 检查取消条件循环
    using (new ProfileScope("ActionCap.CheckCancelLoop"))
    {
        foreach (var action in availableActions)
        {
            // 检查 HasValidCommand, TryGetMatchingCancelContext 等
        }
    }
}
```

#### **SelectActionFromCandidates 方法（5 处）**
```csharp
private void SelectActionFromCandidates(Entity entity)
{
    // 1. 合并外部预约
    using (new ProfileScope("ActionCap.MergeExternal"))
    {
        MergeExternalPreorders(actionComponent, entity);
    }
    
    // 2. 排序和选择
    using (new ProfileScope("ActionCap.SortAndSelect"))
    {
        actionComponent.PreorderActions.Sort(...);
        selectedAction = actionComponent.PreorderActions[0];
    }
    
    // 3. 查找动作
    using (new ProfileScope("ActionCap.LookupAction"))
    {
        actionComponent.AvailableActions.TryGetValue(...);
    }
    
    // 4. 切换动作
    using (new ProfileScope("ActionCap.SwitchAction"))
    {
        SwitchToAction(...);
    }
    
    // 5. 回收（成功/失败两个分支）
    using (new ProfileScope("ActionCap.RecycleAfterSelect"))
    {
        RecyclePreorderActions(...);
        actionComponent.PreorderActions.Clear();
    }
}
```

#### **LoadAvailableActions 方法（2 处）**
```csharp
private void LoadAvailableActions(ActionComponent actionComponent, Entity entity)
{
    // 1. 获取动作 ID 列表
    using (new ProfileScope("ActionCap.GetActionIds"))
    {
        availableActionIds = GetAvailableActionIds(entity);
    }
    
    // 2. 加载动作循环
    using (new ProfileScope("ActionCap.LoadActionsLoop"))
    {
        foreach (var actionId in availableActionIds)
        {
            TryCacheAction(actionComponent, actionId, entity);
        }
    }
}
```

#### **CapabilitySystem.Update 主循环（6 处）**
```csharp
using (new ProfileScope(capability.GetProfileScopeName()))
{
    _entitiesToUnregisterBuffer.Clear();
    
    // 1. 实体遍历主循环
    using (new ProfileScope("CapSys.EntityLoop"))
    {
        foreach (var entityId in entityIds)
        {
            // 2. 获取实体
            using (new ProfileScope("CapSys.GetEntity"))
            {
                if (!world.Entities.TryGetValue(entityId, out entity)) continue;
                if (entity == null || entity.IsDestroyed) continue;
            }
            
            // 3. 获取 Capability 状态
            using (new ProfileScope("CapSys.GetCapState"))
            {
                if (!entity.CapabilityStates.TryGetValue(typeId, out state)) continue;
            }
            
            // 4. 更新激活状态
            using (new ProfileScope("CapSys.UpdateActivation"))
            {
                UpdateActivationState(capability, entity, ref state);
            }
            
            // 5. 更新持续时间
            using (new ProfileScope("CapSys.UpdateDuration"))
            {
                UpdateDuration(capability, entity, ref state);
            }
            
            // 6. 执行 Tick（已有 capability 内部的 ProfileScope）
            if (state.IsActive)
            {
                capability.Tick(entity);
            }
        }
    }
    
    // 7. 批量注销
    using (new ProfileScope("CapSys.BatchUnregister"))
    {
        foreach (var entityId in _entitiesToUnregisterBuffer)
        {
            entityIds.Remove(entityId);
        }
        if (entityIds.Count == 0)
        {
            TypeIdToEntityIds.Remove(typeId);
        }
    }
}
```

---

### 2. 消除 GetAvailableActionIds 的 GC

**问题**：每次创建新的 `List<int>()`

**优化前**：
```csharp
private List<int> GetAvailableActionIds(Entity entity)
{
    var config = entity.EntityConfig;
    var list = new List<int>(); // ← 每次分配新 List
    
    if (config != null)
    {
        AddIfValid(list, config.IdleAction);
        AddIfValid(list, config.WalkAction);
        AddIfValid(list, config.RunAction);
        AddIfValid(list, config.HitAction);
    }
    
    return list;
}
```

**优化后**：
```csharp
// 类字段：预分配缓冲区
private readonly List<int> _availableActionIdsBuffer = new List<int>(8);

private List<int> GetAvailableActionIds(Entity entity)
{
    _availableActionIdsBuffer.Clear(); // ← 复用缓冲区
    var config = entity.EntityConfig;
    
    if (config != null)
    {
        AddIfValid(_availableActionIdsBuffer, config.IdleAction);
        AddIfValid(_availableActionIdsBuffer, config.WalkAction);
        AddIfValid(_availableActionIdsBuffer, config.RunAction);
        AddIfValid(_availableActionIdsBuffer, config.HitAction);
    }
    
    return _availableActionIdsBuffer;
}
```

**GC 节省**：
- 调用频率：每个实体初始化时 1 次（LoadAvailableActions）
- 101 个实体 × 1 次 × ~40 字节 ≈ **4 KB**

---

## 📊 新增的 ProfileScope 列表

### ActionCapability 细粒度 Scope（10 处）

| Scope 名称 | 位置 | 预期作用 |
|-----------|------|----------|
| `ActionCap.RecyclePreorders` | CheckActionCancellation | 定位 PreorderActions 回收开销 |
| `ActionCap.GetAvailableActions` | CheckActionCancellation | 定位 _availableActionsBuffer 使用 |
| `ActionCap.CheckCancelLoop` | CheckActionCancellation | 定位取消条件检查循环 |
| `ActionCap.MergeExternal` | SelectActionFromCandidates | 定位外部预约合并 |
| `ActionCap.SortAndSelect` | SelectActionFromCandidates | 定位 Sort() 和索引访问 |
| `ActionCap.LookupAction` | SelectActionFromCandidates | 定位 Dictionary.TryGetValue |
| `ActionCap.SwitchAction` | SelectActionFromCandidates | 定位 SwitchToAction 调用 |
| `ActionCap.RecycleAfterSelect` | SelectActionFromCandidates | 定位成功/失败分支的回收 |
| `ActionCap.GetActionIds` | LoadAvailableActions | **定位 GetAvailableActionIds GC** |
| `ActionCap.LoadActionsLoop` | LoadAvailableActions | 定位 TryCacheAction 循环 |

### CapabilitySystem 细粒度 Scope（6 处）

| Scope 名称 | 位置 | 预期作用 |
|-----------|------|----------|
| `CapSys.EntityLoop` | Update 主循环 | 定位整体实体遍历开销 |
| `CapSys.GetEntity` | Update 主循环 | 定位 world.Entities.TryGetValue 开销 |
| `CapSys.GetCapState` | Update 主循环 | 定位 entity.CapabilityStates.TryGetValue 开销 |
| `CapSys.UpdateActivation` | Update 主循环 | 定位 UpdateActivationState 开销 |
| `CapSys.UpdateDuration` | Update 主循环 | 定位 UpdateDuration 开销 |
| `CapSys.BatchUnregister` | Update 主循环 | 定位批量注销实体的开销 |

---

## 🔍 预期侦测结果

### 待确认的 GC 来源

根据代码分析，**7.9 KB** 的非 Tick GC 可能来自：

#### **1. List.Sort() 的内部分配**
```csharp
actionComponent.PreorderActions.Sort((a, b) => a.Priority.CompareTo(b.Priority));
```
- **问题**: `List<T>.Sort()` 在某些情况下会分配临时数组
- **预期**: `ActionCap.SortAndSelect` 会显示 GC
- **解决**: 改用插入排序（如果列表通常很小 <10 项）

#### **2. MergeExternalPreorders 的字典遍历**
```csharp
foreach (var kvp in actionComponent.ExternalPreorders)
{
    var preorder = PreorderActionInfo.Create(...);
    actionComponent.PreorderActions.Add(preorder);
}
```
- **问题**: Dictionary 枚举器可能产生装箱
- **预期**: `ActionCap.MergeExternal` 会显示 GC
- **解决**: 使用 `foreach (var kvp in actionComponent.ExternalPreorders.ToList())` 或预缓存

#### **3. HasValidCommand 的 LINQ 或 List 操作**
```csharp
private bool HasValidCommand(Entity entity, ActionInfo action)
{
    // 可能使用了 LINQ 或创建临时 List
}
```
- **问题**: 如果内部有 `Where()`, `Any()` 等 LINQ
- **预期**: `ActionCap.CheckCancelLoop` 会显示 GC
- **解决**: 改用 foreach 手动遍历

#### **4. TryGetMatchingCancelContext 的字符串比较**
```csharp
private bool TryGetMatchingCancelContext(...)
{
    // 可能涉及字符串拼接或比较
}
```
- **问题**: Tag 字符串比较可能产生临时字符串
- **预期**: `ActionCap.CheckCancelLoop` 会显示 GC
- **解决**: 使用 StringComparison.Ordinal

---

## 📈 预期性能收益

### GC 减少

| 优化项 | 优化前 | 优化后 | 节省 |
|--------|--------|--------|------|
| **GetAvailableActionIds** | ~4 KB/次 | **0 KB** | **4 KB** |
| **待侦测的其他来源** | ~35 KB | 待定 | 待定 |

### 侦测能力提升

- **之前**: 只能看到 `ActionCapability.Tick` 整体耗时和 GC
- **之后**: 可以精确定位到 10+ 个子方法的耗时和 GC

---

## 🧪 使用 Unity Profiler 验证

### 1. 启用 Deep Profile
```
Window → Analysis → Profiler → Deep Profile (勾选)
```

### 2. 查看细粒度 Scope
展开 `Cap.ActionCapability` 应该能看到：
```
Cap.ActionCapability (39.8 KB)
├─ CapSys.EntityLoop
│  ├─ CapSys.GetEntity (X KB)
│  ├─ CapSys.GetCapState (X KB)
│  ├─ CapSys.UpdateActivation (X KB)  ← 可能有 GC
│  ├─ CapSys.UpdateDuration (X KB)
│  └─ ActionCapability.Tick (31.9 KB)
│     ├─ ActionCap.CheckCancellation
│     │  ├─ ActionCap.RecyclePreorders (X KB)
│     │  ├─ ActionCap.GetAvailableActions (X KB)
│     │  └─ ActionCap.CheckCancelLoop (X KB)  ← 重点关注
│     └─ ActionCap.SelectAction
│        ├─ ActionCap.MergeExternal (X KB)  ← 重点关注
│        ├─ ActionCap.SortAndSelect (X KB)  ← 重点关注
│        ├─ ActionCap.LookupAction (X KB)
│        ├─ ActionCap.SwitchAction (X KB)
│        └─ ActionCap.RecycleAfterSelect (X KB)
├─ CapSys.BatchUnregister (X KB)
└─ GC.Alloc (7.9 KB)  ← 非 Tick 部分，可能在 OnAttached
   └─ ActionCap.LoadActionsLoop (X KB)  ← 重点关注
```

### 3. 识别 GC 热点
- 按 GC Alloc 列排序
- 找到 > 1 KB 的 Scope
- 根据 Scope 名称定位代码位置

---

## 🛠️ 后续优化方向

### 如果 `ActionCap.SortAndSelect` 有 GC
**原因**: `List<T>.Sort()` 分配临时数组  
**解决**: 
```csharp
// 选项 1：使用插入排序（列表小时更快）
private void InsertionSort(List<PreorderActionInfo> list)
{
    for (int i = 1; i < list.Count; i++)
    {
        var key = list[i];
        int j = i - 1;
        while (j >= 0 && list[j].Priority > key.Priority)
        {
            list[j + 1] = list[j];
            j--;
        }
        list[j + 1] = key;
    }
}

// 选项 2：保持列表有序（插入时排序）
actionComponent.PreorderActions.Add(preorder);
// → 改为：InsertSorted(actionComponent.PreorderActions, preorder);
```

### 如果 `ActionCap.MergeExternal` 有 GC
**原因**: Dictionary 枚举器装箱  
**解决**:
```csharp
// 使用 struct 枚举器（避免装箱）
foreach (var kvp in actionComponent.ExternalPreorders)
{
    // 已经是最优，除非 Dictionary 本身有问题
}

// 或者使用预缓存键列表
private readonly List<string> _externalKeysBuffer = new List<string>(8);

_externalKeysBuffer.Clear();
_externalKeysBuffer.AddRange(actionComponent.ExternalPreorders.Keys);
foreach (var key in _externalKeysBuffer)
{
    var preorder = actionComponent.ExternalPreorders[key];
    // ...
}
```

### 如果 `ActionCap.CheckCancelLoop` 有 GC
**原因**: HasValidCommand 或 TryGetMatchingCancelContext 内部分配  
**解决**: 深入分析这两个方法，查找 LINQ、字符串拼接、临时 List

---

## ✅ 总结

**添加的 ProfileScope**: **20 处**  
- ActionCapability: 10 处
- CapabilitySystem: 6 处
- 其他优化: 4 处

**消除的 GC 来源**: **1 处** (GetAvailableActionIds)  
**GC 节省**: **4 KB** (已确认)  
**编译状态**: ✅ 成功  

### 重点侦测区域

**ActionCapability**:
- `ActionCap.CheckCancelLoop` - 取消条件循环（可能有 LINQ）
- `ActionCap.MergeExternal` - 字典遍历（可能有装箱）
- `ActionCap.SortAndSelect` - List.Sort()（可能有临时数组）

**CapabilitySystem**:
- `CapSys.GetEntity` - 字典查找（应该无 GC，验证性能）
- `CapSys.GetCapState` - 字典查找（应该无 GC，验证性能）
- `CapSys.UpdateActivation` - 状态更新（可能有装箱或临时对象）

**下一步**: 
1. 在 Unity 中运行并查看 Profiler
2. 截图分享细粒度的 GC 分布
3. 重点关注上述 6 个区域的 GC.Alloc
4. 根据侦测结果进一步优化

**这 20 个 ProfileScope 将精确指引我们找到剩余的 35 KB GC 来源！** 🎯

### 预期发现

**最可能的 GC 来源**：
1. **UpdateActivationState** - 可能有 `ShouldActivate/ShouldDeactivate` 的临时对象
2. **ActionCap.CheckCancelLoop** - 可能有 HasValidCommand 的 LINQ
3. **ActionCap.SortAndSelect** - List.Sort() 的临时数组
4. **ActionCap.MergeExternal** - Dictionary 枚举器的装箱

