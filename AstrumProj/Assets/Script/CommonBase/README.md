# CommonBase 程序集

## 概述

CommonBase是Astrum游戏的基础通用功能程序集，**与Unity完全解耦**，提供单例模式、事件系统、对象池、日志系统等基础功能。

## 设计理念

- **平台无关**: 不依赖Unity引擎，可在任何.NET环境中运行
- **高性能**: 线程安全，内存高效
- **易扩展**: 接口化设计，易于扩展和定制
- **类型安全**: 强类型设计，编译时检查

## 核心组件

### 1. Singleton (单例模式)
- **Singleton<T>**: 线程安全的单例基类
- **LazySingleton<T>**: 延迟初始化的高效单例
- **InitializableSingleton<T>**: 带初始化接口的单例
- **IInitializable**: 初始化接口
- **IDisposable**: 可释放接口

### 2. EventSystem (事件系统)
- **EventSystem**: 全局事件管理器
- **EventData**: 事件数据基类
- **GameEventData**: 游戏事件数据
- **PlayerEventData**: 玩家事件数据

### 3. ObjectPool (对象池)
- **IObjectPool<T>**: 对象池接口
- **IObjectPoolFactory<T>**: 对象池工厂接口
- **ObjectPool<T>**: 通用对象池实现
- **ObjectPoolManager**: 对象池管理器
- **IResettable**: 可重置接口
- **IDestroyable**: 可销毁接口

### 4. Logger (日志系统)
- **ILogger**: 日志接口
- **ILogHandler**: 日志处理器接口
- **ConsoleLogHandler**: 控制台日志处理器
- **FileLogHandler**: 文件日志处理器
- **Logger**: 日志管理器

## 文件结构

```
CommonBase/
├── CommonBase.asmdef      # Unity程序集定义文件
├── Singleton.cs           # 单例模式系统
├── EventSystem.cs         # 事件系统
├── ObjectPool.cs          # 对象池系统
├── Logger.cs              # 日志系统
└── README.md             # 说明文档
```

## 使用方法

### 1. 单例模式

```csharp
using Astrum.CommonBase;

// 基本单例
public class GameManager : Singleton<GameManager>
{
    public void Initialize()
    {
        Console.WriteLine("游戏管理器已初始化");
    }
}

// 使用单例
var gameManager = GameManager.Instance;
gameManager.Initialize();

// 延迟初始化单例
public class NetworkManager : LazySingleton<NetworkManager>
{
    protected override NetworkManager CreateInstance()
    {
        return new NetworkManager();
    }
}

// 带初始化的单例
public class ConfigManager : InitializableSingleton<ConfigManager>, IInitializable
{
    public bool IsInitialized { get; private set; }
    
    public void Initialize()
    {
        // 初始化逻辑
        IsInitialized = true;
    }
    
    public void Destroy()
    {
        // 清理逻辑
        IsInitialized = false;
    }
}

// 使用带初始化的单例
ConfigManager.Initialize();
```

### 2. 事件系统

```csharp
using Astrum.CommonBase;

// 获取事件系统
var eventSystem = EventSystem.Instance;

// 注册事件监听器
eventSystem.Subscribe<PlayerEventData>(OnPlayerEvent);
eventSystem.Subscribe("GameStart", OnGameStart);

// 发布事件
var playerEvent = new PlayerEventData
{
    PlayerId = "player1",
    PlayerName = "张三",
    EventType = "Join",
    Data = "玩家加入游戏"
};

eventSystem.Publish(playerEvent);
eventSystem.Publish("GameStart", new { Level = 1 });

// 事件处理器
private void OnPlayerEvent(PlayerEventData eventData)
{
    Console.WriteLine($"玩家事件: {eventData.PlayerName} - {eventData.EventType}");
}

private void OnGameStart(object data)
{
    Console.WriteLine("游戏开始");
}
```

### 3. 对象池

```csharp
using Astrum.CommonBase;

// 获取对象池管理器
var poolManager = ObjectPoolManager.Instance;

// 创建简单对象池
var bulletPool = poolManager.GetSimplePool<Bullet>(initialSize: 10, maxSize: 100);

// 获取和归还对象
var bullet = bulletPool.Get();
// 使用对象...
bulletPool.Return(bullet);

// 自定义对象池工厂
public class BulletFactory : IObjectPoolFactory<Bullet>
{
    public Bullet Create()
    {
        return new Bullet();
    }
    
    public void Reset(Bullet bullet)
    {
        bullet.Reset();
    }
    
    public void Destroy(Bullet bullet)
    {
        bullet.Destroy();
    }
}

var customBulletPool = poolManager.GetPool(new BulletFactory(), 10, 100);

// 获取统计信息
var stats = poolManager.GetPoolStatistics();
foreach (var stat in stats)
{
    Console.WriteLine($"对象池 {stat.Key}: 数量={stat.Value}");
}
```

### 4. 日志系统

```csharp
using Astrum.CommonBase;

// 获取日志管理器
var logger = Logger.Instance;

// 添加日志处理器
logger.AddHandler(new ConsoleLogHandler(useColors: true));
logger.AddHandler(new FileLogHandler("logs/game.log", maxFileSize: 10, maxFileCount: 5));

// 设置最小日志级别
logger.MinLevel = LogLevel.Info;

// 记录日志
logger.Debug("调试信息");
logger.Info("游戏启动");
logger.Warning("警告信息");
logger.Error("错误信息");
logger.Fatal("致命错误");

// 使用格式化字符串
logger.Info("玩家 {0} 加入了游戏", "张三");
logger.Error("连接失败: {0}", "超时");

// 记录异常
try
{
    // 可能出错的代码
}
catch (Exception ex)
{
    logger.LogException(ex, LogLevel.Error);
}

// 自定义日志处理器
public class CustomLogHandler : ILogHandler
{
    public void HandleLog(LogLevel level, string message, DateTime timestamp)
    {
        // 自定义日志处理逻辑
        var logMessage = $"[{timestamp:HH:mm:ss}] {level}: {message}";
        // 发送到远程服务器、数据库等
    }
}

logger.AddHandler(new CustomLogHandler());
```

## 高级用法

### 1. 组合使用

```csharp
// 在游戏管理器中使用所有基础功能
public class GameManager : InitializableSingleton<GameManager>, IInitializable
{
    private readonly EventSystem _eventSystem;
    private readonly Logger _logger;
    private readonly ObjectPoolManager _poolManager;
    
    public bool IsInitialized { get; private set; }
    
    public GameManager()
    {
        _eventSystem = EventSystem.Instance;
        _logger = Logger.Instance;
        _poolManager = ObjectPoolManager.Instance;
    }
    
    public void Initialize()
    {
        _logger.Info("游戏管理器初始化开始");
        
        // 设置日志
        _logger.AddHandler(new ConsoleLogHandler());
        _logger.AddHandler(new FileLogHandler("logs/game.log"));
        
        // 注册事件
        _eventSystem.Subscribe<GameEventData>(OnGameEvent);
        
        // 创建对象池
        var bulletPool = _poolManager.GetSimplePool<Bullet>(10, 100);
        
        IsInitialized = true;
        _logger.Info("游戏管理器初始化完成");
    }
    
    public void Destroy()
    {
        _logger.Info("游戏管理器销毁开始");
        
        // 清理资源
        _poolManager.ClearAllPools();
        _eventSystem.Clear();
        
        IsInitialized = false;
        _logger.Info("游戏管理器销毁完成");
    }
    
    private void OnGameEvent(GameEventData eventData)
    {
        _logger.Info("游戏事件: {0}", eventData.EventType);
    }
}
```

### 2. 线程安全

```csharp
// 所有组件都是线程安全的
Task.Run(() =>
{
    var logger = Logger.Instance;
    logger.Info("来自线程 {0} 的日志", Thread.CurrentThread.ManagedThreadId);
});

Task.Run(() =>
{
    var eventSystem = EventSystem.Instance;
    eventSystem.Publish("ThreadEvent", Thread.CurrentThread.ManagedThreadId);
});
```

### 3. 性能优化

```csharp
// 对象池减少GC压力
var bulletPool = ObjectPoolManager.Instance.GetSimplePool<Bullet>(100, 1000);

// 批量处理
for (int i = 0; i < 1000; i++)
{
    var bullet = bulletPool.Get();
    // 使用对象...
    bulletPool.Return(bullet);
}

// 事件系统避免频繁分配
var eventData = new PlayerEventData();
for (int i = 0; i < 100; i++)
{
    eventData.PlayerId = $"player{i}";
    eventData.EventType = "Move";
    EventSystem.Instance.Publish(eventData);
}
```

## 优势

1. **高性能**: 线程安全，内存高效，减少GC压力
2. **易用性**: 简单的API，快速上手
3. **可扩展**: 接口化设计，易于扩展和定制
4. **类型安全**: 强类型设计，编译时检查
5. **跨平台**: 不依赖Unity，可在任何.NET环境使用

## 注意事项

- 所有单例都是线程安全的
- 对象池需要正确归还对象
- 事件监听器需要及时取消注册
- 日志处理器需要正确释放资源
- 文件日志处理器实现了自动轮转

## 下一步开发

1. 添加配置管理系统
2. 实现缓存系统
3. 添加定时器系统
4. 实现任务调度系统
5. 添加序列化工具 