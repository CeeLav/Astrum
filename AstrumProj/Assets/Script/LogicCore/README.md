# LogicCore 程序集

## 概述

LogicCore是Astrum游戏的核心逻辑程序集，**与Unity完全解耦**，可以在客户端和服务器端共享使用。

## 设计理念

- **平台无关**: 不依赖Unity引擎，可在任何.NET环境中运行
- **共享逻辑**: 客户端和服务器端使用相同的核心逻辑
- **事件驱动**: 使用事件系统进行组件间通信
- **配置驱动**: 通过配置文件控制游戏参数

## 核心组件

### 1. GameStateManager (游戏状态管理器)
- 管理游戏状态转换
- 提供状态改变事件
- 单例模式设计

### 2. GameConfig (游戏配置)
- 游戏参数配置
- JSON序列化支持
- 配置验证功能

### 3. PlayerManager (玩家管理器)
- 玩家数据管理
- 玩家输入处理
- 位置和状态更新

### 4. Vector3 (向量结构)
- 自定义Vector3实现
- 与Unity解耦
- 支持基本数学运算

## 文件结构

```
LogicCore/
├── LogicCore.asmdef          # Unity程序集定义文件
├── GameState.cs              # 游戏状态管理
├── GameConfig.cs             # 游戏配置
├── PlayerManager.cs          # 玩家管理
└── README.md                 # 说明文档
```

## 使用方法

### 1. 初始化游戏

```csharp
using Astrum.LogicCore;

// 获取游戏状态管理器
var gameManager = GameStateManager.Instance;

// 初始化游戏
gameManager.Initialize();

// 监听状态改变
gameManager.OnStateChanged += (previous, current) => {
    Console.WriteLine($"游戏状态从 {previous} 变为 {current}");
};
```

### 2. 管理玩家

```csharp
var playerManager = gameManager.PlayerManager;

// 添加玩家
bool success = playerManager.AddPlayer("player1", "张三");

// 处理玩家输入
playerManager.HandlePlayerInput("player1", new Vector3(1, 0, 0));

// 处理玩家跳跃
playerManager.HandlePlayerJump("player1");

// 监听玩家事件
playerManager.OnPlayerJoined += (player) => {
    Console.WriteLine($"玩家 {player.Name} 加入了游戏");
};
```

### 3. 配置管理

```csharp
var config = gameManager.Config;

// 修改配置
config.MaxPlayers = 8;
config.PlayerMoveSpeed = 6f;

// 验证配置
if (config.Validate())
{
    Console.WriteLine("配置有效");
}
```

## 服务器端集成

在服务器端项目中引用LogicCore：

```csharp
// 在AstrumServer项目中
using Astrum.LogicCore;

public class GameServer
{
    private GameStateManager _gameManager;
    
    public GameServer()
    {
        _gameManager = GameStateManager.Instance;
        _gameManager.Initialize();
        
        // 设置服务器配置
        _gameManager.Config.ServerPort = 8888;
        _gameManager.Config.MaxPlayers = 16;
    }
    
    public void HandleClientMessage(string clientId, string message)
    {
        // 处理客户端消息，更新游戏逻辑
        var input = ParseInput(message);
        _gameManager.PlayerManager.HandlePlayerInput(clientId, input);
    }
}
```

## 客户端集成

在Unity客户端中使用LogicCore：

```csharp
using Astrum.LogicCore;

public class UnityGameController : MonoBehaviour
{
    private GameStateManager _gameManager;
    
    void Start()
    {
        _gameManager = GameStateManager.Instance;
        _gameManager.Initialize();
        
        // 监听玩家位置更新
        _gameManager.PlayerManager.OnPlayerPositionChanged += OnPlayerPositionChanged;
    }
    
    void Update()
    {
        // 更新游戏逻辑
        _gameManager.Update(Time.deltaTime);
        
        // 处理Unity输入
        HandleUnityInput();
    }
    
    private void HandleUnityInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        var input = new Vector3(horizontal, 0, vertical);
        _gameManager.PlayerManager.HandlePlayerInput("localPlayer", input);
        
        if (Input.GetButtonDown("Jump"))
        {
            _gameManager.PlayerManager.HandlePlayerJump("localPlayer");
        }
    }
    
    private void OnPlayerPositionChanged(PlayerData player)
    {
        // 更新Unity GameObject位置
        // 这里需要将LogicCore的Vector3转换为Unity的Vector3
    }
}
```

## 优势

1. **代码复用**: 客户端和服务器端共享相同逻辑
2. **一致性**: 确保客户端和服务器端行为一致
3. **可测试性**: 核心逻辑可以独立测试
4. **可维护性**: 逻辑集中管理，易于维护
5. **扩展性**: 易于添加新功能和组件

## 注意事项

- LogicCore中的Vector3与Unity的Vector3不同，需要转换
- 所有时间相关操作使用标准.NET DateTime
- 事件系统用于组件间通信，避免直接依赖
- 配置验证确保数据完整性 