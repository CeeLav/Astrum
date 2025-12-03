# CapabilitySystem 细分监控说明

## 📊 监控层次结构

### 顶层监控 (World.Update)
```
World.Update
├── World.UpdateWorld → LSUpdater.UpdateWorld → CapabilitySystem.Update
├── World.ProcessEntityEvents → CapabilitySystem.ProcessEntityEvents
└── World.StepPhysics
```

### CapabilitySystem 细分监控

#### 1. Capability 更新监控 (World.UpdateWorld)
```
World.UpdateWorld
└── LSUpdater.UpdateWorld
    └── CapabilitySystem.Update
        ├── Cap.MovementCapability.Update
        ├── Cap.ActionCapability.Update
        ├── Cap.SkillCapability.Update
        ├── Cap.DamageCapability.Update
        ├── Cap.StateCapability.Update
        └── Cap.{OtherCapability}.Update
```

**监控内容**:
- 每个 Capability 类型的完整更新循环
- 包括激活状态检查、持续时间更新、Tick 执行
- 遍历所有拥有该 Capability 的实体

**命名格式**: `Cap.{CapabilityTypeName}.Update`

#### 2. 事件处理监控 (World.ProcessEntityEvents)
```
World.ProcessEntityEvents
├── CapSys.ProcessTargetedEvents
│   └── 遍历每个实体的事件队列
│       ├── Cap.ActionCapability.OnEvent
│       ├── Cap.DamageCapability.OnEvent
│       └── Cap.{OtherCapability}.OnEvent
│
└── CapSys.ProcessBroadcastEvents
    └── 广播事件到所有实体
        ├── Cap.ActionCapability.OnEvent
        ├── Cap.StateCapability.OnEvent
        └── Cap.{OtherCapability}.OnEvent
```

**监控内容**:
- **ProcessTargetedEvents**: 处理发送给特定实体的事件
- **ProcessBroadcastEvents**: 处理广播给所有实体的事件
- **Cap.{CapabilityTypeName}.OnEvent**: 每个 Capability 处理事件的耗时

**命名格式**: 
- `CapSys.ProcessTargetedEvents` - 个体事件处理
- `CapSys.ProcessBroadcastEvents` - 全体事件处理
- `Cap.{CapabilityTypeName}.OnEvent` - 单个事件处理

## 🎯 实际应用场景

### 场景 1: 定位性能瓶颈
**问题**: 游戏在有大量实体时帧率下降

**使用 Profiler 分析**:
```
World.Update (15.2ms) ⚠️
├── World.UpdateWorld (12.8ms) ⚠️
│   └── LSUpdater.UpdateWorld (12.8ms)
│       └── CapabilitySystem.Update (12.5ms)
│           ├── Cap.MovementCapability.Update (0.8ms) ✓
│           ├── Cap.ActionCapability.Update (9.2ms) ⚠️ 瓶颈！
│           ├── Cap.SkillCapability.Update (1.5ms) ✓
│           └── Cap.DamageCapability.Update (0.5ms) ✓
│
└── World.ProcessEntityEvents (1.8ms) ✓
    ├── CapSys.ProcessTargetedEvents (1.2ms)
    └── CapSys.ProcessBroadcastEvents (0.6ms)
```

**结论**: `ActionCapability.Update` 是主要瓶颈，需要优化

### 场景 2: 事件处理性能分析
**问题**: 战斗时偶尔出现卡顿

**使用 Profiler 分析**:
```
World.ProcessEntityEvents (8.5ms) ⚠️
├── CapSys.ProcessTargetedEvents (0.8ms) ✓
└── CapSys.ProcessBroadcastEvents (7.7ms) ⚠️ 瓶颈！
    ├── Cap.DamageCapability.OnEvent (6.2ms) ⚠️
    ├── Cap.StateCapability.OnEvent (0.8ms)
    └── Cap.ActionCapability.OnEvent (0.7ms)
```

**结论**: 广播事件中 `DamageCapability` 处理耗时过长，可能是AOE伤害计算问题

### 场景 3: 对比不同 Capability 的性能
**需求**: 评估新增 Capability 的性能影响

**使用 Profiler 对比**:
```
添加前:
CapabilitySystem.Update (10.2ms)
├── Cap.MovementCapability.Update (2.1ms)
├── Cap.ActionCapability.Update (5.8ms)
└── Cap.SkillCapability.Update (2.3ms)

添加后:
CapabilitySystem.Update (14.5ms) ⚠️ +4.3ms
├── Cap.MovementCapability.Update (2.1ms)
├── Cap.ActionCapability.Update (5.8ms)
├── Cap.SkillCapability.Update (2.3ms)
└── Cap.NewAICapability.Update (4.3ms) ⚠️ 新增开销
```

**结论**: 新增的 AI Capability 开销较大，需要优化算法

## 📈 性能预期

### Debug 构建 (ENABLE_PROFILER 启用)
- **单个 Capability 监控开销**: < 0.01ms
- **总监控开销**: < 0.5% (假设 10 个 Capability)
- **字符串拼接**: 使用字符串插值，有少量 GC

### Release 构建 (ENABLE_PROFILER 禁用)
- **监控代码**: 完全移除（条件编译）
- **性能开销**: 0ms
- **GC 分配**: 0

## 🔍 Unity Profiler 中的显示

### 层级视图 (Hierarchy View)
```
World.Update
│
├─ World.UpdateWorld
│  └─ LSUpdater.UpdateWorld
│     └─ CapabilitySystem.Update (可展开)
│        ├─ Cap.MovementCapability.Update
│        ├─ Cap.ActionCapability.Update
│        ├─ Cap.SkillCapability.Update
│        └─ ...
│
└─ World.ProcessEntityEvents (可展开)
   ├─ CapSys.ProcessTargetedEvents
   │  ├─ Cap.ActionCapability.OnEvent
   │  └─ Cap.DamageCapability.OnEvent
   │
   └─ CapSys.ProcessBroadcastEvents
      └─ Cap.DamageCapability.OnEvent
```

### Timeline 视图
可以看到各 Capability 的执行时间轴，分析并发和依赖关系。

## 💡 优化建议

### 1. 识别热点 Capability
查看 Profiler 数据，找出耗时最多的 Capability：
- 如果某个 Capability 持续占用 > 20% 时间 → 优先优化
- 如果多个 Capability 平均分布 → 考虑整体优化策略

### 2. 优化事件处理
- **减少事件数量**: 合并多个小事件
- **延迟处理**: 非紧急事件可延迟到下一帧
- **批量处理**: 相同类型事件批量处理

### 3. 优化 Capability.Tick()
- **提前退出**: 无需更新时提前返回
- **缓存计算结果**: 避免重复计算
- **减少组件查询**: 缓存常用组件引用

### 4. 控制实体数量
- 每种 Capability 会遍历所有拥有它的实体
- 实体越多，开销越大
- 建议: 单个 Capability 的实体数 < 100

## 📝 代码示例

### 查看特定 Capability 的性能
```csharp
// 在单元测试中
var testHandler = new TestProfilerHandler();
ASProfiler.Instance.RegisterHandler(testHandler);

// 运行游戏逻辑
world.Update();

// 查询性能数据
var movementTime = testHandler.GetAverageSampleTime("Cap.MovementCapability.Update");
var actionTime = testHandler.GetAverageSampleTime("Cap.ActionCapability.Update");

Assert.Less(movementTime, 1.0, "MovementCapability should be < 1ms");
Assert.Less(actionTime, 2.0, "ActionCapability should be < 2ms");
```

### 性能对比测试
```csharp
// 测试前后性能差异
var handlerBefore = new TestProfilerHandler();
ASProfiler.Instance.RegisterHandler(handlerBefore);
RunGameLogic(); // 运行基准测试

var beforeTime = handlerBefore.GetAverageSampleTime("CapabilitySystem.Update");

// 添加新功能
AddNewFeature();

var handlerAfter = new TestProfilerHandler();
ASProfiler.Instance.RegisterHandler(handlerAfter);
RunGameLogic(); // 运行对比测试

var afterTime = handlerAfter.GetAverageSampleTime("CapabilitySystem.Update");
var overhead = afterTime - beforeTime;

Assert.Less(overhead, 0.5, "New feature overhead should be < 0.5ms");
```

## ⚠️ 注意事项

### 1. 字符串拼接开销
```csharp
// ❌ 不推荐：每次都拼接字符串
using (new ProfileScope($"Cap.{capability.GetType().Name}.Update"))

// ✅ 推荐：缓存 Capability 名称
private Dictionary<ICapability, string> _capabilityNames = new();
var name = _capabilityNames.GetOrAdd(capability, c => $"Cap.{c.GetType().Name}.Update");
using (new ProfileScope(name))
```

但当前实现为了代码简洁性，使用了字符串插值。在 Release 构建中会完全移除，Debug 构建中的少量 GC 可以接受。

### 2. 监控粒度权衡
- **过粗**: 无法定位具体问题
- **过细**: 监控开销增加
- **当前设计**: 按 Capability 类型监控（推荐粒度）

### 3. 嵌套监控深度
当前监控层次为 3 层：
```
World.Update (1层)
└── World.ProcessEntityEvents (2层)
    └── Cap.{Name}.OnEvent (3层)
```

建议不要超过 5 层，否则会影响 Profiler 可读性。

## 🎉 总结

通过为 CapabilitySystem 添加细分监控，我们现在可以：

✅ **精确定位**: 快速找到性能瓶颈的具体 Capability  
✅ **量化分析**: 用数据说话，而不是猜测  
✅ **对比测试**: 评估优化效果和新功能影响  
✅ **持续监控**: 在开发过程中持续关注性能  

**下一步**: 刷新 Unity，查看 Profiler 中的监控数据！

---

**创建时间**: 2025-12-02  
**文档版本**: 1.0  
**相关提案**: add-asprofiler-system


