## ADDED Requirements

### Requirement: 跨平台性能监控抽象层
系统 SHALL 提供一个跨平台的性能监控抽象层 ASProfiler，类似于 ASLogger 的架构模式，支持在不依赖 Unity 的逻辑层中进行性能监控。

#### Scenario: 逻辑层使用 ASProfiler
- **WHEN** 逻辑层代码（如 World.Update）调用 ASProfiler.BeginSample("World.Update")
- **THEN** 系统应记录该作用域的执行时间，且不依赖任何 Unity API

#### Scenario: 不同环境注册不同 Handler
- **WHEN** Unity 客户端启动时注册 UnityProfilerHandler
- **AND** 服务器启动时注册 ServerProfilerHandler
- **THEN** 相同的 ASProfiler 调用应在不同环境产生不同的监控行为（Unity Profiler vs Stopwatch）

#### Scenario: Release 构建零开销
- **WHEN** 使用 Release 配置编译项目（不定义 ENABLE_PROFILER）
- **THEN** 所有 ASProfiler 调用应被编译器完全移除，不产生任何运行时开销

### Requirement: 性能监控 Handler 接口
系统 SHALL 定义 IProfilerHandler 接口，允许不同环境实现自己的性能监控逻辑。

#### Scenario: Unity 环境使用 Unity Profiler API
- **WHEN** UnityProfilerHandler.BeginSample("SampleName") 被调用
- **THEN** 系统应调用 UnityEngine.Profiling.Profiler.BeginSample("SampleName")
- **AND** 监控数据应出现在 Unity Profiler 窗口中

#### Scenario: 服务器环境使用 Stopwatch 记录
- **WHEN** ServerProfilerHandler.BeginSample("SampleName") 被调用
- **THEN** 系统应使用 Stopwatch.GetTimestamp() 记录开始时间
- **AND** EndSample 时计算耗时，如果超过阈值（如 5ms）则输出警告日志

#### Scenario: 测试环境收集性能数据
- **WHEN** TestProfilerHandler 被注册
- **THEN** 系统应收集所有监控点的耗时数据
- **AND** 测试代码可查询和验证性能指标（如 GetSampleTime("World.Update")）

### Requirement: 自动作用域管理
系统 SHALL 提供 ProfileScope 结构体，使用 IDisposable 模式自动管理 BeginSample/EndSample 配对。

#### Scenario: 使用 using 语句自动管理
- **WHEN** 代码使用 `using (new ProfileScope("World.Update")) { ... }`
- **THEN** 进入作用域时应自动调用 BeginSample("World.Update")
- **AND** 退出作用域时应自动调用 EndSample()
- **AND** 即使抛出异常也应正确调用 EndSample()

#### Scenario: 嵌套监控作用域
- **WHEN** 代码使用嵌套的 ProfileScope
```csharp
using (new ProfileScope("World.Update"))
{
    using (new ProfileScope("CapabilitySystem.Update"))
    {
        // ...
    }
}
```
- **THEN** 系统应正确记录嵌套关系和各自的耗时
- **AND** Unity Profiler 应显示正确的层级结构

### Requirement: 逻辑层关键路径监控
系统 SHALL 在逻辑层（AstrumLogic）的关键性能路径添加监控点。

#### Scenario: World.Update 监控
- **WHEN** World.Update() 被调用
- **THEN** 系统应监控整个 Update 方法的执行时间
- **AND** 应分别监控子步骤（Updater.UpdateWorld、CapabilitySystem.ProcessEntityEvents、HitSystem.StepPhysics）

#### Scenario: CapabilitySystem.Update 监控
- **WHEN** CapabilitySystem.Update() 被调用
- **THEN** 系统应监控整个 Update 方法的执行时间
- **AND** 应监控每个 Capability 类型的 Update 耗时（如 MovementCapability.Update）

#### Scenario: System.Tick 监控
- **WHEN** 各种 System（如 SkillEffectSystem、HitSystem）的 Tick/Update 方法被调用
- **THEN** 系统应监控每个 System 的执行时间

### Requirement: 表现层关键路径监控
系统 SHALL 在表现层（AstrumView）的关键性能路径添加监控点。

#### Scenario: Stage.Update 监控
- **WHEN** Stage.Update() 被调用
- **THEN** 系统应监控整个 Update 方法的执行时间
- **AND** 应分别监控子步骤（SyncDirtyComponents、EntityView.UpdateView）

#### Scenario: EntityView.UpdateView 监控
- **WHEN** EntityView.UpdateView() 被调用
- **THEN** 系统应监控每个 EntityView 的更新耗时
- **AND** 应支持按 EntityView 类型分组统计（如 PlayerEntityView、MonsterEntityView）

#### Scenario: 动画系统监控
- **WHEN** 动画系统更新动画状态
- **THEN** 系统应监控动画更新的总耗时
- **AND** 应支持监控单个动画状态机的耗时（可选）

### Requirement: 条件编译支持
系统 SHALL 使用条件编译符号 ENABLE_PROFILER 控制监控代码的编译。

#### Scenario: Debug 构建启用监控
- **WHEN** 项目使用 Debug 配置编译（定义 ENABLE_PROFILER）
- **THEN** 所有 ASProfiler 调用应正常执行
- **AND** 监控数据应正确记录和显示

#### Scenario: Release 构建禁用监控
- **WHEN** 项目使用 Release 配置编译（不定义 ENABLE_PROFILER）
- **THEN** 所有 ASProfiler 调用应被编译器移除
- **AND** 生成的 IL 代码中不应包含任何监控相关代码

#### Scenario: 使用 Conditional 特性
- **WHEN** ASProfiler 方法使用 [Conditional("ENABLE_PROFILER")] 特性
- **THEN** 编译器应在未定义 ENABLE_PROFILER 时完全移除方法调用
- **AND** 包括方法参数的计算也应被移除（如字符串拼接）

### Requirement: 性能开销控制
系统 SHALL 确保性能监控本身的开销在可接受范围内。

#### Scenario: Debug 构建开销验证
- **WHEN** 在 Debug 构建中启用所有监控点
- **THEN** 监控开销应小于总帧时间的 1%
- **AND** 单个监控点的开销应小于 0.01ms

#### Scenario: Release 构建零开销验证
- **WHEN** 在 Release 构建中运行性能测试
- **THEN** 启用/禁用监控代码的性能差异应为 0
- **AND** 通过 IL 反编译验证监控代码已完全移除

#### Scenario: 字符串分配优化
- **WHEN** 监控点使用字符串常量作为名称
- **THEN** 系统应复用字符串常量，不产生额外 GC 分配
- **AND** 建议使用 nameof() 表达式避免硬编码字符串

### Requirement: 初始化和配置
系统 SHALL 在应用启动时初始化 ASProfiler 并注册对应环境的 Handler。

#### Scenario: Unity 客户端初始化
- **WHEN** GameApplication.Awake() 被调用
- **THEN** 系统应创建 UnityProfilerHandler 实例
- **AND** 调用 ASProfiler.Instance.RegisterHandler(unityHandler)
- **AND** 后续所有监控调用应使用 Unity Profiler API

#### Scenario: 服务器初始化
- **WHEN** 服务器主程序启动
- **THEN** 系统应创建 ServerProfilerHandler 实例
- **AND** 调用 ASProfiler.Instance.RegisterHandler(serverHandler)
- **AND** 配置性能阈值（如超过 5ms 记录日志）

#### Scenario: 单元测试初始化
- **WHEN** 单元测试 Setup 方法被调用
- **THEN** 系统应创建 TestProfilerHandler 实例
- **AND** 调用 ASProfiler.Instance.RegisterHandler(testHandler)
- **AND** 测试结束后可查询收集的性能数据

### Requirement: 线程安全性
系统 SHALL 明确说明 ASProfiler 的线程安全性限制。

#### Scenario: 单线程环境正常工作
- **WHEN** 逻辑层和表现层在单线程中运行（当前架构）
- **THEN** ASProfiler 应正常工作，无需额外同步

#### Scenario: 多线程环境不支持
- **WHEN** 尝试在多线程中使用 ASProfiler（如 Job System）
- **THEN** 系统应在文档中明确说明不支持多线程
- **AND** 可选：在 Debug 构建中检测多线程使用并输出警告

#### Scenario: 未来多线程支持扩展
- **WHEN** 未来需要支持多线程监控
- **THEN** 系统应使用 ThreadLocal<Stack> 或 AsyncLocal 存储嵌套关系
- **AND** 每个线程独立管理自己的监控栈

