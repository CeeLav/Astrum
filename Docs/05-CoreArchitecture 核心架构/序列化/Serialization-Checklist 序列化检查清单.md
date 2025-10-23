# 序列化代码审查检查清单

## 🚦 快速检查（3分钟）

在提交任何包含 `[MemoryPackable]` 的代码前，快速检查：

### ✅ 必检项
- [ ] **无重复对象引用**：同一对象是否在多个字段/集合中？
- [ ] **ID优于对象**：能用ID引用就不用对象引用
- [ ] **计算属性已标记**：所有 `get => ...` 属性是否有 `[MemoryPackIgnore]`？
- [ ] **合理的集合类型**：能用 `Dictionary<int, T>` 就不用 `List<T>`

### ⚠️ 高风险场景
- [ ] **CurrentXXX + List<XXX>**：是否同一对象？→ 改用 ID + Dictionary
- [ ] **Entity/Component互相引用**：是否循环？→ 其中一方用ID
- [ ] **聚合根**：子对象是否引用父对象？→ 只在父保存引用

---

## 🔍 深度检查（10分钟）

### 1. 序列化字段检查
```csharp
// 检查所有 public 字段/属性
public ActionInfo CurrentAction { get; set; }  // ⚠️ 检查是否重复
public int CurrentActionId { get; set; }        // ✅ 使用ID
[MemoryPackIgnore]
public ActionInfo CurrentAction => ...;         // ✅ 计算属性
```

### 2. 引用一致性检查
```csharp
// 搜索代码，确认同一对象不会多次赋值
CurrentAction = action;           // 位置1
AvailableActions.Add(action);     // 位置2 ← 检查是否同一个action！
```

### 3. 序列化测试
```csharp
[Fact]
public void TestRoundtripSerialization()
{
    var original = CreateTestObject();
    var bytes = MemoryPackSerializer.Serialize(original);
    var restored = MemoryPackSerializer.Deserialize<T>(bytes);
    
    // 验证关键数据
    Assert.Equal(original.Key, restored.Key);
    Assert.Equal(original.Data.Count, restored.Data.Count);
}
```

---

## 🛠️ 快速修复模板

### 场景1：当前对象 + 列表包含同一对象
```csharp
// Before ❌
public ActionInfo CurrentAction { get; set; }
public List<ActionInfo> AvailableActions { get; set; }

// After ✅
public int CurrentActionId { get; set; }
[MemoryPackIgnore]
public ActionInfo CurrentAction 
    => AvailableActions.TryGetValue(CurrentActionId, out var a) ? a : null;
public Dictionary<int, ActionInfo> AvailableActions { get; set; }
```

### 场景2：父子循环引用
```csharp
// Before ❌
class Parent { public Child TheChild { get; set; } }
class Child { public Parent Owner { get; set; } }

// After ✅
class Child 
{ 
    public long OwnerId { get; set; }
    [MemoryPackIgnore]
    public Parent Owner => World.GetParent(OwnerId);
}
```

### 场景3：计算属性未标记
```csharp
// Before ❌
public bool IsIdle => CurrentActionId == 0;  // 会被序列化！

// After ✅
[MemoryPackIgnore]
public bool IsIdle => CurrentActionId == 0;
```

---

## 📊 自动化检测

### Git Pre-commit Hook
```bash
#!/bin/bash
# .git/hooks/pre-commit

# 检查是否有未标记的计算属性
git diff --cached --name-only | grep ".cs$" | while read file; do
    if grep -P "public .* => .*;" "$file" | grep -v "MemoryPackIgnore"; then
        echo "⚠️  Found unmarked computed property in $file"
        exit 1
    fi
done
```

### CI/CD 检查
```yaml
# .github/workflows/check-serialization.yml
- name: Check Serialization
  run: |
    # 检查 MemoryPackable 类是否有重复引用风险
    dotnet build --no-restore
    dotnet test SerializationTests --filter Category=Serialization
```

---

## 📝 代码审查问题清单

审查包含序列化代码时问问自己：

1. **这个对象会被序列化几次？**
   - 1次 ✅
   - 2次+ ⚠️ 需要优化

2. **如果回滚，这个数据会一致吗？**
   - 肯定会 ✅
   - 不确定 ⚠️ 需要测试

3. **序列化后的大小合理吗？**
   - <1KB ✅
   - 1-10KB ⚠️ 检查是否有冗余
   - >10KB ❌ 必须优化

4. **计算属性都标记了吗？**
   - 都标记了 ✅
   - 漏了几个 ❌ 补充标记

---

## 🎯 一句话总结

**"能用ID就不用对象，能用字典就不用列表，计算属性必须标记！"**

