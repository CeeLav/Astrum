# MovementCapabilityV2 真实环境测试指南

**创建日期**: 2025-11-04  
**状态**: 🟢 已配置，可进行真实环境测试

---

## ✅ 配置完成

### 1. BaseUnitArchetype 已更新

**文件**: `AstrumProj/Assets/Script/AstrumLogic/Archetypes/Builtins/BaseUnitArchetype.cs`

```csharp
private static readonly Type[] _caps =
{
    typeof(MovementCapabilityV2)  // 使用新架构的 MovementCapability
};
```

### 2. 自动注册机制

`MovementCapabilityV2` 会在 World 初始化时自动注册到 `CapabilitySystem`：
- `CapabilitySystem.Initialize()` 会扫描所有 `ICapability` 实现
- 自动注册 `MovementCapabilityV2`
- 构建 Tag 映射（Movement、Control）

---

## 🧪 测试场景

### 场景 1: Role 实体（推荐）

**Archetype**: `Role` = `BaseUnit` + `Combat` + `Controllable`

**组件**:
- ✅ `TransComponent` (BaseUnit)
- ✅ `MovementComponent` (BaseUnit)
- ✅ `LSInputComponent` (Controllable) ← **必需**

**测试步骤**:
1. 创建 Role 实体（通过 EntityFactory）
2. 验证 `MovementCapabilityV2` 已注册到 `CapabilitySystem`
3. 验证 `Entity.CapabilityStates` 包含 `MovementCapabilityV2.TypeId`
4. 验证 `ShouldActivate` 返回 `true`（所有必需组件存在）
5. 运行游戏，验证移动功能正常

### 场景 2: BaseUnit 实体（无输入）

**Archetype**: `BaseUnit`

**组件**:
- ✅ `TransComponent`
- ✅ `MovementComponent`
- ❌ `LSInputComponent` ← **缺失**

**预期行为**:
- `MovementCapabilityV2` 会注册到实体
- `ShouldActivate` 返回 `false`（缺少 LSInputComponent）
- `IsActive` 保持为 `false`
- **不会执行移动逻辑**（符合预期，因为没有输入）

### 场景 3: Monster 实体（AI 控制）

**Archetype**: `Monster` = `BaseUnit` + `Combat` + `AI`

**组件**:
- ✅ `TransComponent` (BaseUnit)
- ✅ `MovementComponent` (BaseUnit)
- ❌ `LSInputComponent` ← **缺失**（Monster 使用 AI，不使用玩家输入）

**预期行为**:
- `MovementCapabilityV2` 会注册到实体
- `ShouldActivate` 返回 `false`（缺少 LSInputComponent）
- **需要为 AI 实体创建单独的移动 Capability**（或修改 `ShouldActivate` 逻辑）

---

## 🔍 验证检查点

### 1. 注册验证

```csharp
// 在 World 初始化后
var capability = CapabilitySystem.GetCapability(typeof(MovementCapabilityV2));
Assert.NotNull(capability);
Assert.Equal(MovementCapabilityV2.TypeId, capability.TypeId);
```

### 2. 实体状态验证

```csharp
// 创建 Role 实体后
var entity = EntityFactory.Instance.CreateEntity(roleId, world);

// 验证 CapabilityState 存在
Assert.True(entity.CapabilityStates.ContainsKey(MovementCapabilityV2.TypeId));

var state = entity.CapabilityStates[MovementCapabilityV2.TypeId];
Assert.False(state.IsActive); // 初始未激活

// 验证 CustomData 已初始化
Assert.NotNull(state.CustomData);
Assert.Contains("MovementThreshold", state.CustomData.Keys);
```

### 3. 激活验证

```csharp
// 验证所有必需组件存在
Assert.NotNull(entity.GetComponent<LSInputComponent>());
Assert.NotNull(entity.GetComponent<MovementComponent>());
Assert.NotNull(entity.GetComponent<TransComponent>());

// 验证 ShouldActivate
var capability = CapabilitySystem.GetCapability(typeof(MovementCapabilityV2));
Assert.True(capability.ShouldActivate(entity));
```

### 4. 更新循环验证

```csharp
// 运行一帧更新
world.Updater.UpdateWorld(world);

// 验证 CapabilitySystem 已更新
var state = entity.CapabilityStates[MovementCapabilityV2.TypeId];
// 如果所有组件存在且未被禁用，应该激活
Assert.True(state.IsActive);
```

### 5. 移动功能验证

```csharp
// 设置输入
var inputComponent = entity.GetComponent<LSInputComponent>();
var input = new LSInput { MoveX = 1000000, MoveY = 0 }; // 向右移动
inputComponent.SetInput(input);

// 记录初始位置
var initialPos = entity.GetComponent<TransComponent>().Position;

// 运行多帧更新
for (int i = 0; i < 60; i++) // 1秒（60帧）
{
    world.Updater.UpdateWorld(world);
}

// 验证位置已改变
var finalPos = entity.GetComponent<TransComponent>().Position;
Assert.NotEqual(initialPos, finalPos);
```

---

## 🐛 已知问题与注意事项

### 1. LSInputComponent 依赖

**问题**: `MovementCapabilityV2.ShouldActivate` 要求 `LSInputComponent` 存在。

**影响**:
- ✅ `Role` 实体：正常（有 Controllable）
- ❌ `BaseUnit` 单独使用：不会激活
- ❌ `Monster` 实体：不会激活（需要 AI 版本）

**解决方案**:
- 方案 A：为 AI 实体创建 `AIMovementCapability`（不依赖 LSInputComponent）
- 方案 B：修改 `MovementCapabilityV2.ShouldActivate`，支持 AI 输入源

### 2. 新旧系统并存

**当前状态**:
- ✅ 新系统已集成（`CapabilitySystem`）
- ✅ 旧系统仍保留（`Entity.Capabilities` 列表）
- ✅ `LSUpdater` 同时运行新旧两套更新逻辑

**验证**:
- 确保新系统正常工作
- 确保旧系统不影响新系统
- 确保没有重复执行移动逻辑

### 3. Tag 系统测试

```csharp
// 禁用 Movement Tag
world.CapabilitySystem.DisableCapabilitiesByTag(entity, CapabilityTag.Movement, instigatorId);

// 验证 Capability 被禁用
var state = entity.CapabilityStates[MovementCapabilityV2.TypeId];
// 即使组件完整，IsActive 也应该为 false
```

---

## 📊 测试清单

### 基础功能
- [ ] MovementCapabilityV2 自动注册
- [ ] Entity 创建时正确挂载
- [ ] OnAttached 正确调用
- [ ] CustomData 正确初始化
- [ ] ShouldActivate 正确判定
- [ ] CapabilitySystem.Update 正确执行

### 移动功能
- [ ] 输入响应正常
- [ ] 位置更新正确
- [ ] 旋转更新正确
- [ ] 物理世界同步

### Tag 系统
- [ ] Tag 禁用功能
- [ ] Tag 启用功能
- [ ] 多 Instigator 支持

### 性能
- [ ] 更新性能（对比旧系统）
- [ ] 内存占用（对比旧系统）
- [ ] 缓存命中率

---

## 🚀 快速测试步骤

1. **启动游戏**
2. **创建 Role 实体**（通过 EntityFactory）
3. **检查日志**：
   - `MovementCapabilityV2` 已注册
   - `OnAttached` 已调用
   - `ShouldActivate` 返回 true
4. **设置输入**：通过输入系统设置移动输入
5. **运行更新循环**：验证实体移动
6. **检查物理同步**：验证物理世界位置更新

---

## 📝 日志输出

### 成功标志

```
[CapabilitySystem] Registered Capability: MovementCapabilityV2, TypeId: <hash>
[CapabilitySystem] Entity 1 registered MovementCapabilityV2
[MovementCapabilityV2] OnAttached called for Entity 1
[MovementCapabilityV2] ShouldActivate: true for Entity 1
[CapabilitySystem] MovementCapabilityV2 activated for Entity 1
```

### 错误标志

```
[WARNING] Capability MovementCapabilityV2 not registered in CapabilitySystem
[ERROR] Failed to register Capability: MovementCapabilityV2
[MovementCapabilityV2] ShouldActivate: false (missing LSInputComponent)
```

---

**文档结束**

