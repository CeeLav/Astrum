# Unity LogHandler 使用指南

## 概述

UnityLogHandler 是一个专为Unity设计的日志处理器，它将ASLogger的日志输出到Unity控制台，提供丰富的配置选项和功能特性。

## 核心组件

### 1. UnityLogHandler
- **功能**: 将ASLogger的日志输出到Unity控制台
- **特性**: 
  - 支持颜色显示
  - 日志级别过滤
  - 堆栈跟踪
  - 日志缓存和统计
  - 时间戳显示

### 2. LogManager
- **功能**: 管理日志处理器的生命周期
- **特性**:
  - 自动初始化
  - 支持多种日志处理器
  - 配置管理
  - 统计信息

## 安装和配置

### 1. 自动初始化
LogManager 会在游戏启动时自动初始化，无需手动配置：

```csharp
// GameApplication 会自动调用
LogManager.Instance.Initialize();
```

### 2. 手动初始化
如果需要手动控制初始化时机：

```csharp
var logManager = LogManager.Instance;
if (!logManager.IsInitialized())
{
    logManager.Initialize();
}
```

## 基本使用

### 1. 直接使用ASLogger
```csharp
// 调试日志
ASLogger.Instance.Debug("这是一条调试日志");

// 信息日志
ASLogger.Instance.Info("这是一条信息日志");

// 警告日志
ASLogger.Instance.Warning("这是一条警告日志");

// 错误日志
ASLogger.Instance.Error("这是一条错误日志");

// 致命错误日志
ASLogger.Instance.Fatal("这是一条致命错误日志");
```

### 2. 使用LogManager
```csharp
var logManager = LogManager.Instance;

// 便捷的日志方法
logManager.Debug("调试日志");
logManager.Info("信息日志");
logManager.Warning("警告日志");
logManager.Error("错误日志");
logManager.Fatal("致命错误日志");
```

### 3. 异常日志
```csharp
try
{
    // 可能抛出异常的代码
    throw new Exception("测试异常");
}
catch (Exception ex)
{
    // 记录异常日志
    ASLogger.Instance.LogException(ex);
    
    // 或者指定日志级别
    ASLogger.Instance.LogException(ex, LogLevel.Error);
}
```

## 配置选项

### 1. 日志级别设置
```csharp
var logManager = LogManager.Instance;

// 设置最小日志级别
logManager.SetMinLogLevel(LogLevel.Warning);

// 获取当前最小日志级别
LogLevel currentLevel = logManager.GetMinLogLevel();
```

### 2. Unity控制台配置
```csharp
var unityHandler = logManager.GetUnityLogHandler();

// 启用/禁用颜色显示
unityHandler.SetUnityConsoleColorsEnabled(true);

// 启用/禁用堆栈跟踪
unityHandler.SetStackTraceEnabled(false);

// 设置最大日志缓存数量
unityHandler.SetMaxLogCount(1000);
```

### 3. 文件日志配置
```csharp
var logManager = LogManager.Instance;

// 启用文件日志
logManager.SetFileLogHandlerEnabled(true);

// 获取日志文件路径
string logPath = logManager.GetLogFilePath();
```

## 高级功能

### 1. 日志统计
```csharp
var logManager = LogManager.Instance;

// 获取日志统计信息
var stats = logManager.GetLogStatistics();
foreach (var kvp in stats)
{
    Debug.Log($"{kvp.Key}: {kvp.Value}");
}

// 输出统计信息
logManager.OutputLogStatistics();
```

### 2. 最近日志查看
```csharp
var logManager = LogManager.Instance;

// 输出最近10条日志
logManager.OutputRecentLogs(10);
```

### 3. 日志缓存管理
```csharp
var logManager = LogManager.Instance;

// 清空日志缓存
logManager.ClearLogCache();

// 重置统计信息
logManager.ResetLogStatistics();
```

## 编辑器集成

### 1. ContextMenu 方法
LogManager 提供了多个 ContextMenu 方法，可以在Unity编辑器中直接调用：

- `初始化日志管理器`
- `关闭日志管理器`
- `重新初始化`
- `输出日志统计`
- `输出最近日志`
- `清空日志缓存`
- `重置日志统计`
- `测试日志功能`

### 2. 示例脚本
使用 `LogHandlerExample.cs` 来测试各种功能：

```csharp
// 在场景中添加 LogHandlerExample 组件
// 然后通过 ContextMenu 测试各种功能
```

## 性能考虑

### 1. 日志级别过滤
- 设置合适的最小日志级别，避免输出过多调试信息
- 生产环境建议设置为 `LogLevel.Warning` 或 `LogLevel.Error`

### 2. 堆栈跟踪
- 堆栈跟踪会显著影响性能，仅在调试时启用
- 生产环境建议禁用堆栈跟踪

### 3. 日志缓存
- 合理设置最大日志数量，避免内存占用过多
- 定期清理日志缓存

## 最佳实践

### 1. 日志级别使用
```csharp
// Debug: 详细的调试信息
ASLogger.Instance.Debug("进入方法 X，参数: {0}", param);

// Info: 重要的状态信息
ASLogger.Instance.Info("游戏状态改变: {0} -> {1}", oldState, newState);

// Warning: 潜在问题
ASLogger.Instance.Warning("资源加载失败，使用默认值: {0}", resourceName);

// Error: 错误但可恢复
ASLogger.Instance.Error("网络连接失败，尝试重连");

// Fatal: 致命错误
ASLogger.Instance.Fatal("游戏初始化失败，无法继续");
```

### 2. 异常处理
```csharp
try
{
    // 业务逻辑
}
catch (Exception ex)
{
    // 记录异常
    ASLogger.Instance.LogException(ex);
    
    // 处理异常
    HandleException(ex);
}
```

### 3. 格式化日志
```csharp
// 使用格式化字符串
ASLogger.Instance.Info("玩家 {0} 获得了 {1} 个 {2}", playerName, count, itemName);

// 使用字符串插值
ASLogger.Instance.Info($"玩家 {playerName} 获得了 {count} 个 {itemName}");
```

## 故障排除

### 1. 日志不显示
- 检查最小日志级别设置
- 确认UnityLogHandler已正确注册
- 检查Unity控制台是否启用

### 2. 性能问题
- 降低日志级别
- 禁用堆栈跟踪
- 减少日志缓存大小

### 3. 文件日志问题
- 检查文件路径权限
- 确认磁盘空间充足
- 检查文件大小限制

## 扩展开发

### 1. 自定义日志处理器
```csharp
public class CustomLogHandler : ILogHandler
{
    public void HandleLog(LogLevel level, string message, DateTime timestamp)
    {
        // 自定义处理逻辑
    }
}

// 注册自定义处理器
ASLogger.Instance.AddHandler(new CustomLogHandler());
```

### 2. 集成第三方日志系统
```csharp
public class ThirdPartyLogHandler : ILogHandler
{
    public void HandleLog(LogLevel level, string message, DateTime timestamp)
    {
        // 转发到第三方日志系统
        ThirdPartyLogger.Log(level.ToString(), message);
    }
}
```

## 示例代码

参考 `LogHandlerExample.cs` 文件，其中包含了完整的使用示例：

- 基本日志功能测试
- 日志级别过滤测试
- 日志统计功能测试
- 配置选项测试
- 性能测试
- 异常处理测试

## 注意事项

1. **初始化顺序**: LogManager 应该在游戏启动时尽早初始化
2. **线程安全**: 日志处理器是线程安全的，可以在多线程环境中使用
3. **内存管理**: 注意日志缓存的内存占用
4. **性能影响**: 在生产环境中合理配置日志级别
5. **文件权限**: 确保有足够的文件系统权限来创建日志文件 