# GameDirector 游戏导演架构设计

## 概述

本文档描述了 Astrum 游戏核心模块的抽象优化升级方案，通过引入 `GameDirector` 单例来集中管理游戏子系统的初始化、调度和状态切换，同时扩展现有的 `GameMode` 系统以提供更强大的游戏状态管理能力。

## 当前架构分析

### 现有架构问题

1. **职责分散**：游戏子系统的初始化逻辑分散在 `GameApplication` 和 `GamePlayManager` 中
2. **耦合度高**：`GameApplication` 直接管理所有管理器，职责过重
3. **状态管理混乱**：游戏状态切换逻辑分散，缺乏统一的状态管理
4. **扩展性差**：添加新的游戏模式或子系统需要修改多个地方

### 当前核心组件

- **GameApplication**: 游戏应用程序主控制器，负责管理器初始化和更新循环
- **GamePlayManager**: 游戏玩法管理器，负责 GameMode 的创建和切换
- **IGameMode**: 游戏模式接口，定义了单机和联机两种模式
- **ApplicationState**: 应用程序状态枚举（INITIALIZING, LOADING, GAME_PLAYING, PAUSED, SHUTDOWN）

## 目标架构设计

### 核心设计原则

1. **单一职责**：每个组件只负责自己的核心功能
2. **集中管理**：通过 GameDirector 统一管理游戏子系统
3. **状态驱动**：基于游戏状态进行系统调度
4. **可扩展性**：支持新游戏模式和子系统的轻松添加

### 新架构组件

#### 1. GameDirector（游戏导演）

**职责**：
- 统一管理所有游戏子系统的初始化
- 控制游戏状态切换和流程
- 协调各个管理器之间的交互
- 提供游戏生命周期管理

**核心功能**：
```csharp
public class GameDirector : Singleton<GameDirector>
{
    // 游戏状态管理
    public GameState CurrentState { get; private set; }
    public IGameMode CurrentGameMode { get; private set; }
    
    // 子系统管理
    public void InitializeSubsystems();
    public void UpdateSubsystems(float deltaTime);
    public void ShutdownSubsystems();
    
    // 状态切换
    public void ChangeGameState(GameState newState);
    public void SwitchGameMode(IGameMode newMode);
    
    // 生命周期管理
    public void StartGame();
    public void PauseGame();
    public void ResumeGame();
    public void EndGame();
}
```

#### 2. 扩展的 GameMode 系统

**增强的 IGameMode 接口**：
```csharp
public interface IGameMode
{
    // 基础生命周期
    void Initialize();
    void StartGame(string sceneName);
    void Update(float deltaTime);
    void Shutdown();
    
    // 新增：状态管理
    GameModeState CurrentState { get; }
    void OnStateEnter(GameModeState state);
    void OnStateExit(GameModeState state);
    
    // 新增：事件处理
    void OnGameEvent(GameEvent gameEvent);
    
    // 新增：配置管理
    GameModeConfig GetConfig();
    void ApplyConfig(GameModeConfig config);
    
    // 原有属性
    Room MainRoom { get; }
    Stage MainStage { get; }
    long PlayerId { get; }
    string ModeName { get; }
    bool IsRunning { get; }
}
```

**新增游戏模式状态**：
```csharp
public enum GameModeState
{
    Initializing,    // 初始化中
    Loading,         // 加载中
    Ready,          // 准备就绪
    Playing,        // 游戏中
    Paused,         // 暂停
    Ending,         // 结束中
    Finished        // 已结束
}
```

#### 3. 游戏状态系统

**扩展的游戏状态**：
```csharp
public enum GameState
{
    // 应用级状态
    ApplicationStarting,    // 应用启动中
    ApplicationReady,      // 应用就绪
    
    // 游戏级状态
    GameMenu,              // 游戏菜单
    GameLoading,           // 游戏加载中
    GamePlaying,           // 游戏进行中
    GamePaused,            // 游戏暂停
    GameEnding,            // 游戏结束中
    
    // 系统级状态
    SystemShutdown         // 系统关闭
}
```

## 架构重构计划

### 阶段一：创建 GameDirector 基础架构

1. **创建 GameDirector 类**
   - 实现单例模式
   - 定义核心接口和属性
   - 实现基础的生命周期管理

2. **扩展 GameMode 系统**
   - 增强 IGameMode 接口
   - 添加 GameModeState 枚举
   - 更新现有 GameMode 实现

3. **创建游戏状态管理**
   - 定义 GameState 枚举
   - 实现状态切换逻辑
   - 添加状态事件系统

### 阶段二：迁移初始化逻辑

1. **从 GameApplication 迁移管理器初始化**
   - 将 `InitializeManagers()` 逻辑迁移到 GameDirector
   - 保持 GameApplication 作为 Unity MonoBehaviour 入口
   - 建立 GameApplication -> GameDirector 的调用关系

2. **从 GamePlayManager 迁移 GameMode 管理**
   - 将 GameMode 创建和切换逻辑迁移到 GameDirector
   - 简化 GamePlayManager 的职责
   - 保持向后兼容性

### 阶段三：实现状态驱动架构

1. **实现状态机系统**
   - 创建 GameStateMachine 类
   - 实现状态转换规则
   - 添加状态事件处理

2. **集成 GameMode 状态管理**
   - 将 GameMode 状态与游戏状态关联
   - 实现状态同步机制
   - 添加状态验证逻辑

### 阶段四：优化和扩展

1. **性能优化**
   - 优化子系统更新顺序
   - 实现按需更新机制
   - 添加性能监控

2. **功能扩展**
   - 支持新的游戏模式
   - 添加配置系统
   - 实现插件架构

## 详细实施步骤

### 步骤 1：创建 GameDirector 基础类

```csharp
// 文件：AstrumProj/Assets/Script/AstrumClient/Core/GameDirector.cs
namespace Astrum.Client.Core
{
    public class GameDirector : Singleton<GameDirector>
    {
        // 游戏状态
        private GameState _currentState = GameState.ApplicationStarting;
        private IGameMode _currentGameMode;
        
        // 子系统管理器
        private Dictionary<Type, IGameSubsystem> _subsystems;
        
        // 状态机
        private GameStateMachine _stateMachine;
        
        // 初始化
        public void Initialize()
        {
            InitializeSubsystems();
            InitializeStateMachine();
        }
        
        // 更新循环
        public void Update(float deltaTime)
        {
            _stateMachine.Update(deltaTime);
            UpdateSubsystems(deltaTime);
        }
        
        // 状态切换
        public void ChangeGameState(GameState newState)
        {
            _stateMachine.ChangeState(newState);
        }
        
        // GameMode 切换
        public void SwitchGameMode(IGameMode newMode)
        {
            if (_currentGameMode != null)
            {
                _currentGameMode.Shutdown();
            }
            
            _currentGameMode = newMode;
            _currentGameMode.Initialize();
        }
    }
}
```

### 步骤 2：扩展 IGameMode 接口

```csharp
// 文件：AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/IGameMode.cs
namespace Astrum.Client.Managers.GameModes
{
    public interface IGameMode
    {
        // 原有接口保持不变
        void Initialize();
        void StartGame(string sceneName);
        void Update(float deltaTime);
        void Shutdown();
        
        // 新增状态管理
        GameModeState CurrentState { get; }
        void OnStateEnter(GameModeState state);
        void OnStateExit(GameModeState state);
        
        // 新增事件处理
        void OnGameEvent(GameEvent gameEvent);
        
        // 新增配置管理
        GameModeConfig GetConfig();
        void ApplyConfig(GameModeConfig config);
        
        // 原有属性
        Room MainRoom { get; }
        Stage MainStage { get; }
        long PlayerId { get; }
        string ModeName { get; }
        bool IsRunning { get; }
    }
}
```

### 步骤 3：创建游戏状态系统

```csharp
// 文件：AstrumProj/Assets/Script/AstrumClient/Core/GameState.cs
namespace Astrum.Client.Core
{
    public enum GameState
    {
        ApplicationStarting,
        ApplicationReady,
        GameMenu,
        GameLoading,
        GamePlaying,
        GamePaused,
        GameEnding,
        SystemShutdown
    }
    
    public enum GameModeState
    {
        Initializing,
        Loading,
        Ready,
        Playing,
        Paused,
        Ending,
        Finished
    }
}
```

### 步骤 4：修改 GameApplication

```csharp
// 修改 GameApplication.cs
public class GameApplication : MonoBehaviour
{
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Initialize()
    {
        // 初始化 GameDirector
        GameDirector.Instance.Initialize();
        
        // 设置目标帧率
        Application.targetFrameRate = targetFrameRate;
    }
    
    private void Update()
    {
        if (!isRunning) return;
        
        // 委托给 GameDirector 更新
        GameDirector.Instance.Update(Time.deltaTime);
    }
}
```

### 步骤 5：更新 GamePlayManager

```csharp
// 修改 GamePlayManager.cs
public class GamePlayManager : Singleton<GamePlayManager>
{
    public void Initialize()
    {
        // 简化初始化，主要逻辑迁移到 GameDirector
        UserManager.Instance?.Initialize();
        RoomSystemManager.Instance?.Initialize();
        RegisterNetworkMessageHandlers();
    }
    
    public void StartGame(string gameSceneName)
    {
        // 委托给 GameDirector
        GameDirector.Instance.StartGame(gameSceneName);
    }
}
```

## 架构优势

### 1. 职责清晰
- **GameApplication**: 仅作为 Unity 入口点，负责基础设置
- **GameDirector**: 统一管理游戏核心逻辑和状态
- **GamePlayManager**: 专注于网络和用户管理
- **GameMode**: 专注于具体游戏模式实现

### 2. 状态管理统一
- 集中的状态机管理
- 清晰的状态转换规则
- 统一的状态事件处理

### 3. 易于扩展
- 新游戏模式只需实现 IGameMode 接口
- 新子系统可以轻松集成到 GameDirector
- 支持插件化架构

### 4. 向后兼容
- 保持现有 API 不变
- 渐进式迁移
- 最小化破坏性更改

## 迁移策略

### 渐进式迁移
1. **第一阶段**：创建 GameDirector，但不改变现有调用
2. **第二阶段**：逐步迁移初始化逻辑
3. **第三阶段**：实现状态驱动架构
4. **第四阶段**：优化和清理旧代码

### 兼容性保证
- 保持所有现有公共 API
- 添加新的扩展方法
- 使用适配器模式处理旧接口

## 总结

通过引入 GameDirector 和扩展 GameMode 系统，我们可以实现：

1. **集中管理**：所有游戏子系统由 GameDirector 统一管理
2. **状态驱动**：基于游戏状态进行系统调度和切换
3. **职责分离**：每个组件职责清晰，易于维护
4. **易于扩展**：支持新游戏模式和子系统的轻松添加
5. **向后兼容**：保持现有代码的兼容性

这个架构设计为 Astrum 游戏提供了更强大、更灵活的核心管理系统，为未来的功能扩展奠定了坚实的基础。
