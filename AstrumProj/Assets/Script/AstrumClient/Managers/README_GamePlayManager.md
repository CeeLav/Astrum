# GamePlayManager 游戏玩法管理器

## 概述

`GamePlayManager` 是一个用于管理一局游戏内容的核心管理器，负责处理游戏状态、玩家管理、回合控制、游戏逻辑等。

## 主要功能

### 1. 游戏状态管理
- **游戏状态枚举** (`GamePlayState`)
  - `None`: 无状态
  - `Preparing`: 准备中
  - `Playing`: 游戏中
  - `Paused`: 暂停
  - `RoundEnd`: 回合结束
  - `GameEnd`: 游戏结束
  - `Error`: 错误状态

### 2. 玩家管理
- 支持多玩家游戏（最多4个玩家）
- 玩家数据管理（血量、分数、位置等）
- 玩家加入/离开事件处理
- 本地玩家和AI玩家支持

### 3. 回合系统
- 多回合游戏支持
- 回合时间限制（默认5分钟）
- 回合间状态重置
- 回合结果统计

### 4. 事件系统
- 游戏状态变化事件
- 游戏时间变化事件
- 回合变化事件
- 玩家加入/离开事件
- 游戏结束事件
- 游戏暂停/恢复事件

## 使用方法

### 1. 基本初始化

```csharp
// 获取GamePlayManager实例
var gamePlayManager = GamePlayManager.Instance;

// 初始化
gamePlayManager.Initialize();
```

### 2. 开始新游戏

```csharp
// 开始单机游戏
gamePlayManager.StartNewGame("SinglePlayer", GameDifficulty.Normal, 3);
```

### 3. 订阅事件

```csharp
// 订阅游戏状态变化事件
gamePlayManager.OnGameStateChanged += (newState) => {
    Debug.Log($"游戏状态变化: {newState}");
};

// 订阅游戏结束事件
gamePlayManager.OnGameEnded += (gameResult) => {
    Debug.Log($"游戏结束，总时长: {gameResult.TotalDuration}秒");
};
```

### 4. 游戏控制

```csharp
// 暂停游戏
gamePlayManager.PauseGame();

// 恢复游戏
gamePlayManager.ResumeGame();

// 结束游戏
gamePlayManager.EndGame();
```

### 5. 玩家管理

```csharp
// 添加AI玩家
var aiPlayer = new PlayerData {
    Id = "AI_001",
    Name = "AI玩家1",
    IsLocalPlayer = false
};
gamePlayManager.AddPlayer(aiPlayer);

// 获取本地玩家
var localPlayer = gamePlayManager.GetLocalPlayer();

// 移除玩家
gamePlayManager.RemovePlayer("AI_001");
```

## 数据结构

### PlayerData（玩家数据）
```csharp
public class PlayerData {
    public string Id { get; set; }                    // 玩家ID
    public string Name { get; set; }                  // 玩家名称
    public bool IsLocalPlayer { get; set; }           // 是否为本地玩家
    public bool IsAlive { get; set; }                 // 是否存活
    public float Health { get; set; }                 // 当前血量
    public float MaxHealth { get; set; }              // 最大血量
    public Vector3 Position { get; set; }             // 位置
    public DateTime JoinTime { get; set; }            // 加入时间
    public DateTime? DeathTime { get; set; }          // 死亡时间
    public int Score { get; set; }                    // 分数
    public Dictionary<string, object> CustomData { get; set; } // 自定义数据
}
```

### GameSession（游戏会话）
```csharp
public class GameSession {
    public string SessionId { get; set; }             // 会话ID
    public string GameMode { get; set; }              // 游戏模式
    public GameDifficulty Difficulty { get; set; }    // 游戏难度
    public DateTime StartTime { get; set; }           // 开始时间
    public DateTime? EndTime { get; set; }            // 结束时间
}
```

### GameResult（游戏结果）
```csharp
public class GameResult {
    public string SessionId { get; set; }             // 会话ID
    public string GameMode { get; set; }              // 游戏模式
    public GameDifficulty Difficulty { get; set; }    // 游戏难度
    public int TotalRounds { get; set; }              // 总回合数
    public double TotalDuration { get; set; }         // 总时长
    public List<PlayerData> Players { get; set; }     // 玩家列表
    public Dictionary<string, object> Statistics { get; set; } // 统计数据
}
```

## 配置选项

### 游戏设置
- `gameTimeScale`: 游戏时间缩放（默认1.0）
- `maxPlayers`: 最大玩家数（默认4）
- `roundTimeLimit`: 回合时间限制（默认300秒）

### 日志设置
- `enableLogging`: 是否启用日志输出（默认true）

## 集成到GameApplication

`GamePlayManager` 已经集成到 `GameApplication` 中，会自动初始化和更新：

```csharp
// 在GameApplication中访问
var gamePlayManager = GameApplication.Instance.GamePlayManager;
```

## 示例代码

参考 `GamePlayManagerExample.cs` 文件，其中包含了完整的使用示例：

- 游戏启动和停止
- 事件订阅和处理
- 玩家管理
- 游戏状态控制
- 调试功能

## 注意事项

1. **协程支持**: `GamePlayManager` 使用协程来处理异步操作，确保在Unity环境中正常工作。

2. **单例模式**: `GamePlayManager` 使用单例模式，确保全局只有一个实例。

3. **事件清理**: 在组件销毁时记得取消订阅事件，避免内存泄漏。

4. **线程安全**: 所有操作都在主线程中执行，确保线程安全。

5. **错误处理**: 游戏启动失败时会自动设置错误状态，可以通过事件监听处理。

## 扩展建议

1. **自定义游戏逻辑**: 在 `UpdateGameSpecificLogic()` 方法中添加具体的游戏逻辑。

2. **胜利条件**: 在 `CheckVictoryCondition()` 方法中实现具体的胜利条件检查。

3. **数据持久化**: 在 `SaveGameData()` 方法中实现游戏数据的保存逻辑。

4. **网络同步**: 可以扩展支持网络多人游戏功能。

5. **AI行为**: 可以为AI玩家添加更复杂的行为逻辑。 