# 序列化最佳实践与规范

## 核心原则

### 1. ID引用 > 对象引用
**规则**：对于需要序列化的数据结构，优先使用ID引用，而不是直接引用对象。

```csharp
// ❌ 不推荐：直接引用，可能导致重复序列化
public ActionInfo CurrentAction { get; set; }
public List<ActionInfo> AvailableActions { get; set; }

// ✅ 推荐：ID引用 + 访问器
public int CurrentActionId { get; set; }  // 序列化
[MemoryPackIgnore]
public ActionInfo CurrentAction => GetAction(CurrentActionId);  // 不序列化
public Dictionary<int, ActionInfo> AvailableActions { get; set; }
```

### 2. 字典 > 列表（对于可通过ID查找的数据）
**原因**：
- 查找性能：O(1) vs O(n)
- 避免重复：自然防止同一ID的对象多次添加
- 引用一致性：同一ID始终指向同一对象

```csharp
// ❌ 不推荐
public List<ActionInfo> AvailableActions { get; set; }
var action = AvailableActions.Find(a => a.Id == actionId);  // O(n)

// ✅ 推荐
public Dictionary<int, ActionInfo> AvailableActions { get; set; }
if (AvailableActions.TryGetValue(actionId, out var action))  // O(1)
```

### 3. 标记不序列化的属性
**规则**：所有计算属性、缓存属性、临时属性都应该标记为不序列化。

```csharp
[MemoryPackIgnore]  // 或 [JsonIgnore]
public ActionInfo CurrentAction => GetAction(CurrentActionId);

[MemoryPackIgnore]
public bool IsIdle => CurrentActionId == 0;
```

---

## 常见陷阱与解决方案

### 陷阱 1：同一对象在多个地方引用
**问题场景**：
```csharp
actionComponent.CurrentAction = skillAction;  // 引用1
actionComponent.AvailableActions.Add(skillAction);  // 引用2（同一对象！）
```

**序列化结果**：
- 第一次序列化：完整对象（所有字段）
- 第二次序列化：可能为空或部分字段

**解决方案**：
```csharp
// 方案A：确保从同一数据源获取
actionComponent.AvailableActions[actionId] = skillAction;
actionComponent.CurrentActionId = actionId;  // 自动通过访问器查找

// 方案B：使用工厂方法确保单例
var action = ActionFactory.GetOrCreate(actionId);
```

### 陷阱 2：集合中混用引用和ID
**问题场景**：
```csharp
public List<Entity> Targets { get; set; }  // 序列化整个Entity！
public int TargetEntityId { get; set; }  // 也在序列化
```

**解决方案**：
```csharp
// 只序列化ID列表
public List<long> TargetEntityIds { get; set; }

// 运行时动态查找
[MemoryPackIgnore]
public IEnumerable<Entity> Targets => TargetEntityIds.Select(id => World.GetEntity(id));
```

### 陷阱 3：循环引用
**问题场景**：
```csharp
public class Entity
{
    public ActionComponent ActionComp { get; set; }  // 引用组件
}
public class ActionComponent
{
    public Entity Owner { get; set; }  // 引用实体 ← 循环！
}
```

**解决方案**：
```csharp
public class ActionComponent
{
    public long EntityId { get; set; }  // 只存ID
    
    [MemoryPackIgnore]
    public Entity Owner => World.GetEntity(EntityId);  // 动态查找
}
```

---

## 代码审查检查清单

在提交包含序列化类的代码前，检查以下项目：

- [ ] 是否存在对象的多重引用？
- [ ] 是否可以用ID替代对象引用？
- [ ] 计算属性是否标记了 `[MemoryPackIgnore]`？
- [ ] 集合类型是否使用了合适的数据结构（Dictionary vs List）？
- [ ] 是否存在循环引用？
- [ ] 序列化后的数据大小是否合理？

---

## 单元测试模板

### 测试：序列化/反序列化一致性
```csharp
[Fact]
public void TestSerializationConsistency()
{
    // Arrange
    var original = new ActionComponent();
    original.CurrentActionId = 3001;
    original.AvailableActions[3001] = CreateTestAction(3001);
    
    // Act
    var bytes = MemoryPackSerializer.Serialize(original);
    var deserialized = MemoryPackSerializer.Deserialize<ActionComponent>(bytes);
    
    // Assert
    Assert.Equal(original.CurrentActionId, deserialized.CurrentActionId);
    Assert.Equal(original.AvailableActions.Count, deserialized.AvailableActions.Count);
    Assert.NotNull(deserialized.CurrentAction);  // 访问器应该工作
    Assert.Equal(original.CurrentAction.Id, deserialized.CurrentAction.Id);
}
```

### 测试：检测重复引用
```csharp
[Fact]
public void TestNoDuplicateReferences()
{
    var component = new ActionComponent();
    var action = CreateTestAction(3001);
    
    component.AvailableActions[3001] = action;
    component.CurrentActionId = 3001;
    
    // 验证是同一个引用
    Assert.Same(action, component.CurrentAction);
}
```

---

## 调试技巧

### 1. 添加序列化日志
```csharp
[MemoryPackConstructor]
public SkillActionInfo(...)
{
    // ... 赋值 ...
    
    #if DEBUG
    var callerStack = new System.Diagnostics.StackTrace(1, false);
    ASLogger.Instance.Debug($"[Serialization] SkillActionInfo.ctor " +
        $"ActionId={id} TriggerEffects={triggerEffects?.Count ?? 0} " +
        $"Caller={callerStack.GetFrame(0)?.GetMethod()?.Name}");
    #endif
}
```

### 2. 序列化大小检查
```csharp
var bytes = MemoryPackSerializer.Serialize(entity);
if (bytes.Length > 10_000)  // 10KB
{
    ASLogger.Instance.Warning($"Entity {entity.UniqueId} serialization size: {bytes.Length} bytes");
}
```

---

## 工具集成建议

### 1. 静态分析规则
创建 Roslyn Analyzer 检测：
- 同一对象被添加到多个集合
- 未标记 `[MemoryPackIgnore]` 的计算属性
- 可能的循环引用

### 2. 性能监控
```csharp
// 在回滚时监控序列化大小
var bytes = SaveState(frame);
Telemetry.RecordSerializationSize(frame, bytes.Length);
if (bytes.Length > threshold)
{
    ASLogger.Instance.Warning($"Frame {frame} state size: {bytes.Length} bytes");
}
```

---

## 参考示例

### 正确的组件设计
```csharp
[MemoryPackable]
public partial class ActionComponent : BaseComponent
{
    // ✅ 序列化字段：只存储ID
    public int CurrentActionId { get; set; }
    
    // ✅ 计算属性：标记不序列化
    [MemoryPackIgnore]
    public ActionInfo? CurrentAction 
    {
        get => CurrentActionId > 0 && AvailableActions.ContainsKey(CurrentActionId)
            ? AvailableActions[CurrentActionId] : null;
        set => CurrentActionId = value?.Id ?? 0;
    }
    
    // ✅ 使用字典：高效查找 + 自然去重
    [MemoryPackAllowSerialize]
    public Dictionary<int, ActionInfo> AvailableActions { get; set; } = new();
}
```

---

## 总结

**3个核心原则**：
1. **ID引用优于对象引用**（序列化层面）
2. **字典优于列表**（对于可ID查找的数据）
3. **显式标记不序列化属性**（避免意外序列化）

**记住**：回滚系统对数据一致性要求极高，任何重复引用都可能导致状态不一致！

