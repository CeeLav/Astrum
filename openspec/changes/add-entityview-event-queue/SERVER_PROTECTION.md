# 服务器端防护机制

## 问题分析

### 内存泄漏风险
服务器端（AstrumServer）没有视图层，但 Entity 是共享代码：

```
客户端（AstrumProj）: Entity + EntityView + Stage ✅
服务器端（AstrumServer）: Entity（没有 EntityView/Stage）❌
```

**如果不加防护**：
1. World.CreateEntity() 调用 entity.QueueViewEvent()
2. Entity._viewEventQueue 创建并入队事件
3. **服务器端没有 Stage 消费事件**
4. 每个 Entity 都积累未消费的事件
5. 内存持续增长，最终 OOM

### 泄漏规模估算
```
假设：
- 1000 个在线玩家
- 每个玩家 10 个 Entity（玩家 + 技能 + 怪物等）
- 每个 Entity 平均 100 个生命周期事件（创建、更新、销毁等）

泄漏内存：
- 10,000 个 Entity
- 每个 Entity 的 Queue：~80 bytes
- 每个 ViewEvent：~40 bytes
- 总内存：10,000 × (80 + 100 × 40) = 40.8 MB

如果服务器长时间运行，Entity 不断创建销毁：
- 1小时后：数百 MB
- 1天后：数 GB
- 最终：OOM 崩溃
```

## 解决方案

### 静态标记机制

```csharp
// Entity.ViewEventQueue.cs
public partial class Entity
{
    /// <summary>
    /// 静态标记：当前环境是否有视图层
    /// - 客户端：true（有 Stage/EntityView）
    /// - 服务器：false（没有视图层）
    /// </summary>
    public static bool HasViewLayer { get; set; } = false;
    
    public void QueueViewEvent(ViewEvent evt)
    {
        // 服务器端直接返回，不入队
        if (!HasViewLayer)
            return;
        
        // 客户端正常入队
        if (_viewEventQueue == null)
            _viewEventQueue = new Queue<ViewEvent>(4);
        _viewEventQueue.Enqueue(evt);
    }
}
```

### 初始化

```csharp
// 客户端（AstrumProj）
// Stage.cs
public void Initialize()
{
    if (_isInited) return;
    
    // 启用视图层标记
    Entity.HasViewLayer = true;
    
    // 其他初始化...
    _isInited = true;
}

// 服务器端（AstrumServer）
// 无需任何代码，默认 false
```

## 优势分析

### 1. 零性能开销
```csharp
// 仅一次静态 bool 检查，现代 CPU 分支预测几乎零成本
if (!HasViewLayer)  // ~1 CPU cycle
    return;
```

### 2. 简单可靠
- 不需要条件编译（`#if UNITY_CLIENT`）
- 不需要依赖注入
- 不需要配置文件
- 一行代码解决问题

### 3. 安全兜底
即使逻辑层代码错误地调用 `QueueViewEvent()`：
```csharp
// 服务器端错误调用
entity.QueueViewEvent(new ViewEvent { ... });  // ✅ 安全，直接返回

// 不会：
// ❌ 创建队列
// ❌ 占用内存
// ❌ 导致泄漏
```

### 4. 调试友好
```csharp
// 开发阶段可以强制启用（测试）
#if DEBUG
Entity.HasViewLayer = true;  // 测试视图事件逻辑
#endif
```

## 对比其他方案

### 方案 A：条件编译
```csharp
#if UNITY_CLIENT
entity.QueueViewEvent(...);
#endif
```
**问题**：
- 需要在所有调用点添加宏
- 服务器端可能也用 Unity（无头模式）
- 难以维护

### 方案 B：抽象接口
```csharp
interface IViewEventSink
{
    void QueueViewEvent(ViewEvent evt);
}
```
**问题**：
- 复杂度高
- 需要依赖注入
- 性能开销（虚方法调用）

### 方案 C：配置项
```csharp
if (Config.HasViewLayer)
    entity.QueueViewEvent(...);
```
**问题**：
- 需要在所有调用点检查
- 配置加载开销
- 容易遗漏

## 实现清单

- [x] 在 Entity.ViewEventQueue 中添加 `HasViewLayer` 静态属性
- [x] 在 `QueueViewEvent()` 中添加检查
- [x] 在 Stage.Initialize() 中设置为 true
- [x] 在 tasks.md 中添加实施步骤
- [x] 在 spec.md 中添加场景
- [x] 更新所有文档

## 测试验证

### 单元测试
```csharp
[Test]
public void Entity_ViewEventQueue_ServerSide_RejectsEvents()
{
    // Arrange
    Entity.HasViewLayer = false;  // 模拟服务器端
    var entity = new Entity();
    
    // Act
    entity.QueueViewEvent(new ViewEvent { EventType = ViewEventType.EntityCreated });
    
    // Assert
    Assert.IsFalse(entity.HasPendingViewEvents);  // 队列应为空
    Assert.IsNull(entity.ViewEventQueue);         // 队列未创建
}

[Test]
public void Entity_ViewEventQueue_ClientSide_AcceptsEvents()
{
    // Arrange
    Entity.HasViewLayer = true;  // 模拟客户端
    var entity = new Entity();
    
    // Act
    entity.QueueViewEvent(new ViewEvent { EventType = ViewEventType.EntityCreated });
    
    // Assert
    Assert.IsTrue(entity.HasPendingViewEvents);   // 队列有事件
    Assert.AreEqual(1, entity.ViewEventQueue.Count);
}
```

### 集成测试
- 服务器端长时间运行，监控内存增长
- 客户端正常功能验证
- 切换 HasViewLayer 标记测试

## 性能影响

### 服务器端
- **内存节省**：100%（不创建任何队列）
- **CPU 节省**：几乎 100%（仅 1 次 bool 检查）

### 客户端
- **额外开销**：~1 CPU cycle（静态 bool 检查）
- **可忽略不计**

## 总结

通过添加 `Entity.HasViewLayer` 静态标记：

✅ **彻底解决**服务器端内存泄漏问题  
✅ **零性能开销**（仅静态 bool 检查）  
✅ **简单可靠**（一行代码防护）  
✅ **安全兜底**（即使代码错误也不会泄漏）  
✅ **易于测试**（可模拟客户端/服务器端）

这是一个非常优雅和实用的优化！🎯

