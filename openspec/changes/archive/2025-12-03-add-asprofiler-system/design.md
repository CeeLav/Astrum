# ASProfiler 系统技术设计

## Context

Astrum 项目采用分层架构，逻辑层（AstrumLogic）完全独立于 Unity，表现层（AstrumView）依赖 Unity。当前缺乏统一的性能监控机制，无法有效追踪性能瓶颈。

**背景约束**:
- 逻辑层不能依赖 Unity API（包括 Unity Profiler）
- 必须满足严格的性能预算（60 FPS，95% 帧 < 16ms）
- Release 构建需要零性能开销
- 需要支持多环境（Unity 客户端、服务器、单元测试）

**利益相关者**:
- 开发者：需要快速定位性能瓶颈
- 测试团队：需要验证性能指标
- 运维团队：需要监控服务器性能

## Goals / Non-Goals

**Goals**:
- ✅ 提供跨平台的性能监控抽象层（类似 ASLogger）
- ✅ 支持逻辑层和表现层的关键路径监控
- ✅ 不同环境可注册不同的监控实现
- ✅ Release 构建零开销（条件编译）
- ✅ 支持嵌套监控（子作用域）
- ✅ 支持性能数据导出和分析

**Non-Goals**:
- ❌ 不替代 Unity Profiler（而是集成）
- ❌ 不进行自动性能优化
- ❌ 不支持远程性能监控（可在后续扩展）
- ❌ 不监控内存分配（由 Unity Profiler 负责）

## Decisions

### Decision 1: 采用 ASLogger 相似的架构模式

**选择**: 使用 Singleton + Handler 注册模式

**理由**:
- ASLogger 已验证该模式在项目中运行良好
- 逻辑层通过接口调用，不依赖具体实现
- 不同环境可注册不同 Handler（Unity、Server、Test）
- 易于理解和维护（团队已熟悉）

**实现**:
```csharp
// CommonBase/ASProfiler.cs
public class ASProfiler : Singleton<ASProfiler>
{
    private IProfilerHandler _handler;
    
    public void RegisterHandler(IProfilerHandler handler)
    {
        _handler = handler;
    }
    
    public void BeginSample(string name)
    {
        #if ENABLE_PROFILER
        _handler?.BeginSample(name);
        #endif
    }
    
    public void EndSample()
    {
        #if ENABLE_PROFILER
        _handler?.EndSample();
        #endif
    }
}
```

**Alternatives considered**:
- **方案 A**: 直接在逻辑层使用 Stopwatch
  - ❌ 无法集成 Unity Profiler
  - ❌ Release 构建仍有开销
  - ❌ 无法统一管理监控点

- **方案 B**: 使用 AOP（面向切面编程）自动注入
  - ❌ 增加编译复杂度
  - ❌ 难以调试和维护
  - ❌ 不符合项目简单优先原则

### Decision 2: 使用 IDisposable 模式简化调用

**选择**: 提供 `ProfileScope` 结构体，使用 `using` 语句自动管理

**理由**:
- 自动配对 BeginSample/EndSample，防止遗漏
- 代码更简洁易读
- 异常安全（即使抛异常也会调用 EndSample）
- C# 标准模式，团队熟悉

**实现**:
```csharp
public readonly struct ProfileScope : IDisposable
{
    public ProfileScope(string name)
    {
        ASProfiler.Instance.BeginSample(name);
    }
    
    public void Dispose()
    {
        ASProfiler.Instance.EndSample();
    }
}

// 使用示例
public void Update()
{
    using (new ProfileScope("World.Update"))
    {
        // ... 逻辑代码
    }
}
```

**Alternatives considered**:
- **方案 A**: 手动调用 BeginSample/EndSample
  - ❌ 容易忘记调用 EndSample
  - ❌ 异常时可能导致监控栈错乱
  - ❌ 代码冗长

### Decision 3: 使用条件编译控制开销

**选择**: 使用 `#if ENABLE_PROFILER` 条件编译

**理由**:
- Release 构建完全移除监控代码，零开销
- 符合项目性能预算要求
- 编译器优化可完全内联空方法
- 可通过编译符号灵活控制

**实现**:
```csharp
// 定义编译符号
// Debug: ENABLE_PROFILER
// Release: 不定义

[Conditional("ENABLE_PROFILER")]
public void BeginSample(string name)
{
    _handler?.BeginSample(name);
}
```

**Alternatives considered**:
- **方案 A**: 运行时开关（bool IsEnabled）
  - ❌ Release 构建仍有分支判断开销
  - ❌ 无法完全优化掉字符串参数

- **方案 B**: 完全移除监控代码
  - ❌ 无法在需要时快速启用
  - ❌ 不便于性能调试

### Decision 4: Unity 环境使用 Profiler.BeginSample

**选择**: UnityProfilerHandler 直接调用 Unity Profiler API

**理由**:
- 与 Unity Profiler 无缝集成，数据统一显示
- 支持 Deep Profiling 和 Timeline
- 支持 Profiler Marker（高性能）
- Unity 官方推荐方式

**实现**:
```csharp
public class UnityProfilerHandler : IProfilerHandler
{
    public void BeginSample(string name)
    {
        UnityEngine.Profiling.Profiler.BeginSample(name);
    }
    
    public void EndSample()
    {
        UnityEngine.Profiling.Profiler.EndSample();
    }
}
```

**Alternatives considered**:
- **方案 A**: 自己实现计时和可视化
  - ❌ 重复造轮子
  - ❌ 无法与 Unity Profiler 集成
  - ❌ 开发和维护成本高

### Decision 5: 服务器环境使用 Stopwatch + 日志

**选择**: ServerProfilerHandler 使用 Stopwatch 记录耗时，超阈值输出日志

**理由**:
- 服务器无 GUI，日志是最佳输出方式
- Stopwatch 高精度，开销低
- 可配置阈值，只记录慢操作
- 可集成到监控系统（如 Prometheus）

**实现**:
```csharp
public class ServerProfilerHandler : IProfilerHandler
{
    private Stack<(string name, long startTicks)> _stack = new();
    private long _thresholdMs = 5; // 超过 5ms 记录
    
    public void BeginSample(string name)
    {
        _stack.Push((name, Stopwatch.GetTimestamp()));
    }
    
    public void EndSample()
    {
        var (name, startTicks) = _stack.Pop();
        var elapsedMs = (Stopwatch.GetTimestamp() - startTicks) * 1000.0 / Stopwatch.Frequency;
        
        if (elapsedMs > _thresholdMs)
        {
            ASLogger.Instance.Warning($"[Profiler] {name} took {elapsedMs:F2}ms");
        }
    }
}
```

## Risks / Trade-offs

### Risk 1: 字符串分配开销

**风险**: 每次调用 BeginSample 传递字符串可能产生 GC 分配

**缓解措施**:
- 使用字符串常量（编译器会复用）
- 考虑使用 `nameof()` 表达式
- 考虑使用 Profiler.BeginSample 的重载（CustomSampler）
- Release 构建完全移除，无影响

**决策**: 接受 Debug 构建的少量 GC，优先保证易用性

### Risk 2: 监控点过多导致性能下降

**风险**: 如果添加过多监控点，即使在 Debug 构建也可能影响性能

**缓解措施**:
- 只监控关键路径（World.Update、System.Tick、Stage.Update）
- 避免在循环内部添加监控点
- 提供分级监控（Normal、Detailed、Deep）
- 性能测试验证开销 < 1%

**决策**: 初期只添加核心监控点，后续按需扩展

### Risk 3: 跨线程监控

**风险**: 当前设计使用 Stack 存储嵌套关系，不支持多线程

**缓解措施**:
- 当前逻辑层和表现层都是单线程，暂不支持多线程
- 如需支持，使用 ThreadLocal<Stack> 或 AsyncLocal
- 文档明确说明线程安全性

**决策**: 初期不支持多线程，后续按需扩展

## Migration Plan

### Phase 1: 基础设施（1-2 天）
1. 实现 ASProfiler 核心类（CommonBase）
2. 实现 IProfilerHandler 接口
3. 实现 ProfileScope 结构体
4. 添加单元测试验证基本功能

### Phase 2: 环境适配（1-2 天）
1. 实现 UnityProfilerHandler（Unity 客户端）
2. 实现 ServerProfilerHandler（服务器）
3. 实现 TestProfilerHandler（单元测试）
4. 在 GameApplication 中注册 Handler

### Phase 3: 逻辑层集成（2-3 天）
1. World.Update() 添加监控点
2. LSUpdater.UpdateWorld() 添加监控点
3. CapabilitySystem.Update() 添加监控点
4. 各 System.Tick() 添加监控点
5. 验证性能开销 < 1%

### Phase 4: 表现层集成（2-3 天）
1. Stage.Update() 添加监控点
2. EntityView.UpdateView() 添加监控点
3. 动画系统添加监控点
4. UI 系统添加监控点（可选）
5. 验证性能开销 < 1%

### Phase 5: 测试和文档（1-2 天）
1. 编写单元测试
2. 编写集成测试
3. 性能测试验证
4. 编写使用文档
5. 代码审查

**总计**: 7-12 天

### Rollback Plan
如果发现严重性能问题或 Bug：
1. 移除所有监控点调用（保留基础设施）
2. 或完全禁用 ENABLE_PROFILER 编译符号
3. 代码已模块化，可快速回滚

## Open Questions

1. **是否需要支持自定义性能指标？**（如 Entity 数量、内存使用）
   - 建议：初期不支持，专注于时间监控

2. **是否需要性能数据持久化？**（导出到文件）
   - 建议：Phase 2 添加，用于自动化性能测试

3. **是否需要远程性能监控？**（服务器实时查看客户端性能）
   - 建议：后续扩展，当前不是核心需求

4. **是否需要与 Unity Profiler Marker 集成？**（更高性能）
   - 建议：Phase 2 优化，当前使用 BeginSample 即可

