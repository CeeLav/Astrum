# GameDirector 重构实施计划

## 概述

本文档提供了将现有游戏核心模块重构为 GameDirector 架构的详细实施计划，包括具体的代码迁移步骤、时间安排和风险评估。

## 重构目标

1. **集中管理**：通过 GameDirector 统一管理所有游戏子系统
2. **状态驱动**：基于游戏状态进行系统调度和切换
3. **职责分离**：明确各组件职责，降低耦合度
4. **向后兼容**：保持现有 API 的兼容性
5. **易于扩展**：支持新游戏模式和子系统的轻松添加

## 实施阶段

### 阶段一：基础架构搭建（第1-2周）

#### 1.1 创建核心类文件

**文件结构**：
```
AstrumProj/Assets/Script/AstrumClient/Core/
├── GameDirector.cs                    # 游戏导演主类
├── GameState.cs                       # 游戏状态枚举
├── GameStateMachine.cs                # 状态机实现
├── IGameSubsystem.cs                  # 子系统接口
└── GameEvent.cs                       # 游戏事件系统
```

**实施步骤**：

1. **创建 GameDirector 基础类**
```csharp
// 文件：AstrumProj/Assets/Script/AstrumClient/Core/GameDirector.cs
namespace Astrum.Client.Core
{
    public class GameDirector : Singleton<GameDirector>
    {
        // 基础属性
        private GameState _currentState = GameState.ApplicationStarting;
        private IGameMode _currentGameMode;
        private Dictionary<Type, IGameSubsystem> _subsystems;
        private GameStateMachine _stateMachine;
        
        // 公共属性
        public GameState CurrentState => _currentState;
        public IGameMode CurrentGameMode => _currentGameMode;
        
        // 核心方法
        public void Initialize() { /* 实现 */ }
        public void Update(float deltaTime) { /* 实现 */ }
        public void Shutdown() { /* 实现 */ }
        
        // 状态管理
        public void ChangeGameState(GameState newState) { /* 实现 */ }
        public void SwitchGameMode(IGameMode newMode) { /* 实现 */ }
    }
}
```

2. **创建游戏状态枚举**
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

3. **创建状态机**
```csharp
// 文件：AstrumProj/Assets/Script/AstrumClient/Core/GameStateMachine.cs
namespace Astrum.Client.Core
{
    public class GameStateMachine
    {
        private GameState _currentState;
        private Dictionary<GameState, List<GameState>> _transitions;
        
        public GameState CurrentState => _currentState;
        
        public void Initialize() { /* 实现 */ }
        public void Update(float deltaTime) { /* 实现 */ }
        public bool ChangeState(GameState newState) { /* 实现 */ }
    }
}
```

#### 1.2 创建子系统接口

```csharp
// 文件：AstrumProj/Assets/Script/AstrumClient/Core/IGameSubsystem.cs
namespace Astrum.Client.Core
{
    public interface IGameSubsystem
    {
        void Initialize();
        void Update(float deltaTime);
        void Shutdown();
        string SubsystemName { get; }
        bool IsInitialized { get; }
    }
}
```

#### 1.3 扩展现有事件系统

```csharp
// 文件：AstrumProj/Assets/Script/AstrumClient/Core/GameDirectorEvents.cs
namespace Astrum.Client.Core
{
    // 使用现有的 EventData 基类
    public class GameStateChangedEventData : EventData
    {
        public GameState PreviousState { get; set; }
        public GameState NewState { get; set; }
        
        public GameStateChangedEventData(GameState previousState, GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }
    
    public class GameModeChangedEventData : EventData
    {
        public IGameMode PreviousMode { get; set; }
        public IGameMode NewMode { get; set; }
        
        public GameModeChangedEventData(IGameMode previousMode, IGameMode newMode)
        {
            PreviousMode = previousMode;
            NewMode = newMode;
        }
    }
}
```

#### 1.4 测试基础架构

- 创建单元测试
- 验证单例模式
- 测试基础状态切换

### 阶段二：GameMode 系统扩展（第3-4周）

#### 2.1 扩展 IGameMode 接口

**文件**：`AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/IGameMode.cs`

```csharp
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
        bool CanTransitionTo(GameModeState targetState);
        
        // 新增事件处理
        void OnGameEvent(GameEvent gameEvent);
        void RegisterEventHandler<T>(Action<T> handler) where T : GameEvent;
        void UnregisterEventHandler<T>(Action<T> handler) where T : GameEvent;
        
        // 新增配置管理
        GameModeConfig GetConfig();
        void ApplyConfig(GameModeConfig config);
        void SaveConfig();
        void LoadConfig();
        
        // 原有属性
        Room MainRoom { get; }
        Stage MainStage { get; }
        long PlayerId { get; }
        string ModeName { get; }
        bool IsRunning { get; }
    }
}
```

#### 2.2 创建 BaseGameMode 抽象类

**文件**：`AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/BaseGameMode.cs`

```csharp
namespace Astrum.Client.Managers.GameModes
{
    public abstract class BaseGameMode : IGameMode
    {
        protected GameModeState _currentState = GameModeState.Initializing;
        protected GameModeConfig _config;
        protected Dictionary<Type, Delegate> _eventHandlers = new();
        
        // 实现 IGameMode 接口
        public abstract void Initialize();
        public abstract void StartGame(string sceneName);
        public abstract void Update(float deltaTime);
        public abstract void Shutdown();
        public abstract Room MainRoom { get; }
        public abstract Stage MainStage { get; }
        public abstract long PlayerId { get; }
        public abstract string ModeName { get; }
        public abstract bool IsRunning { get; }
        
        // 状态管理实现
        public GameModeState CurrentState => _currentState;
        
        protected virtual void ChangeState(GameModeState newState)
        {
            if (!CanTransitionTo(newState)) return;
            
            var previousState = _currentState;
            OnStateExit(previousState);
            _currentState = newState;
            OnStateEnter(newState);
            
            OnGameEvent(new GameModeStateChangedEvent(previousState, newState));
        }
        
        public virtual bool CanTransitionTo(GameModeState targetState)
        {
            // 实现状态转换规则
            return _currentState switch
            {
                GameModeState.Initializing => targetState == GameModeState.Loading,
                GameModeState.Loading => targetState == GameModeState.Ready,
                GameModeState.Ready => targetState == GameModeState.Playing,
                GameModeState.Playing => targetState == GameModeState.Paused || targetState == GameModeState.Ending,
                GameModeState.Paused => targetState == GameModeState.Playing || targetState == GameModeState.Ending,
                GameModeState.Ending => targetState == GameModeState.Finished,
                GameModeState.Finished => false,
                _ => false
            };
        }
        
        // 事件处理实现
        public virtual void OnGameEvent(GameEvent gameEvent)
        {
            var eventType = gameEvent.GetType();
            if (_eventHandlers.TryGetValue(eventType, out var handler))
            {
                handler.DynamicInvoke(gameEvent);
            }
        }
        
        public void RegisterEventHandler<T>(Action<T> handler) where T : GameEvent
        {
            _eventHandlers[typeof(T)] = handler;
        }
        
        public void UnregisterEventHandler<T>(Action<T> handler) where T : GameEvent
        {
            _eventHandlers.Remove(typeof(T));
        }
        
        // 配置管理实现
        public virtual GameModeConfig GetConfig()
        {
            return _config ??= CreateDefaultConfig();
        }
        
        public virtual void ApplyConfig(GameModeConfig config)
        {
            _config = config;
            OnGameEvent(new GameModeConfigChangedEvent(config));
        }
        
        protected abstract GameModeConfig CreateDefaultConfig();
        
        // 虚方法，子类可以重写
        public virtual void OnStateEnter(GameModeState state) { }
        public virtual void OnStateExit(GameModeState state) { }
        public virtual void SaveConfig() { }
        public virtual void LoadConfig() { }
    }
}
```

#### 2.3 更新现有 GameMode 实现

**更新 SinglePlayerGameMode**：
- 继承 BaseGameMode
- 实现状态管理
- 保持原有功能

**更新 MultiplayerGameMode**：
- 继承 BaseGameMode
- 实现状态管理
- 保持网络功能

#### 2.4 测试 GameMode 扩展

- 单元测试新功能
- 集成测试状态管理
- 性能测试事件系统

### 阶段三：GameDirector 核心实现（第5-6周）

#### 3.1 实现 GameDirector 核心功能

```csharp
public class GameDirector : Singleton<GameDirector>
{
    private GameState _currentState = GameState.ApplicationStarting;
    private IGameMode _currentGameMode;
    private Dictionary<Type, IGameSubsystem> _subsystems;
    private GameStateMachine _stateMachine;
    
    public void Initialize()
    {
        ASLogger.Instance.Info("GameDirector: 初始化游戏导演");
        
        // 初始化状态机
        InitializeStateMachine();
        
        // 初始化子系统
        InitializeSubsystems();
        
        // 设置初始状态
        ChangeGameState(GameState.ApplicationReady);
        
        ASLogger.Instance.Info("GameDirector: 初始化完成");
    }
    
    private void InitializeSubsystems()
    {
        _subsystems = new Dictionary<Type, IGameSubsystem>();
        
        // 注册所有子系统
        RegisterSubsystem<ResourceManager>(ResourceManager.Instance);
        RegisterSubsystem<SceneManager>(SceneManager.Instance);
        RegisterSubsystem<NetworkManager>(NetworkManager.Instance);
        RegisterSubsystem<UIManager>(UIManager.Instance);
        RegisterSubsystem<AudioManager>(AudioManager.Instance);
        RegisterSubsystem<InputManager>(InputManager.Instance);
        RegisterSubsystem<CameraManager>(CameraManager.Instance);
        RegisterSubsystem<GamePlayManager>(GamePlayManager.Instance);
        
        // 初始化所有子系统
        foreach (var subsystem in _subsystems.Values)
        {
            subsystem.Initialize();
        }
    }
    
    public void Update(float deltaTime)
    {
        // 更新状态机
        _stateMachine.Update(deltaTime);
        
        // 更新当前游戏模式
        _currentGameMode?.Update(deltaTime);
        
        // 更新子系统
        UpdateSubsystems(deltaTime);
    }
    
    private void UpdateSubsystems(float deltaTime)
    {
        foreach (var subsystem in _subsystems.Values)
        {
            if (subsystem.IsInitialized)
            {
                subsystem.Update(deltaTime);
            }
        }
    }
    
    public void ChangeGameState(GameState newState)
    {
        if (_currentState == newState) return;
        
        var previousState = _currentState;
        _currentState = newState;
        
        // 触发状态变化事件（使用现有 EventSystem）
        EventSystem.Instance.Publish(new GameStateChangedEventData(previousState, newState));
        
        ASLogger.Instance.Info($"GameDirector: 游戏状态从 {previousState} 变为 {newState}");
    }
    
    public void SwitchGameMode(IGameMode newMode)
    {
        if (_currentGameMode != null)
        {
            _currentGameMode.Shutdown();
        }
        
        var previousMode = _currentGameMode;
        _currentGameMode = newMode;
        _currentGameMode?.Initialize();
        
        // 触发游戏模式变化事件
        EventSystem.Instance.Publish(new GameModeChangedEventData(previousMode, newMode));
        
        ASLogger.Instance.Info($"GameDirector: 切换到游戏模式 {newMode?.ModeName}");
    }
}
```

#### 3.2 实现子系统管理

```csharp
public class GameDirector : Singleton<GameDirector>
{
    private void RegisterSubsystem<T>(IGameSubsystem subsystem) where T : IGameSubsystem
    {
        _subsystems[typeof(T)] = subsystem;
    }
    
    public T GetSubsystem<T>() where T : class, IGameSubsystem
    {
        if (_subsystems.TryGetValue(typeof(T), out var subsystem))
        {
            return subsystem as T;
        }
        return null;
    }
    
    public void Shutdown()
    {
        ASLogger.Instance.Info("GameDirector: 关闭游戏导演");
        
        // 关闭当前游戏模式
        _currentGameMode?.Shutdown();
        _currentGameMode = null;
        
        // 关闭所有子系统
        foreach (var subsystem in _subsystems.Values)
        {
            subsystem.Shutdown();
        }
        _subsystems.Clear();
        
        // 设置关闭状态
        ChangeGameState(GameState.SystemShutdown);
    }
}
```

#### 3.3 测试 GameDirector 核心功能

- 测试子系统管理
- 测试状态切换
- 测试游戏模式切换

### 阶段四：迁移现有代码（第7-8周）

#### 4.1 修改 GameApplication

```csharp
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
        Debug.Log("GameApplication: 初始化开始...");
        
        try
        {
            Application.runInBackground = true;
            
            // 初始化 GameDirector
            GameDirector.Instance.Initialize();
            
            Debug.Log("GameApplication: 初始化完成");
        }
        catch (Exception ex)
        {
            Debug.LogError($"GameApplication: 初始化失败 - {ex.Message}");
            Shutdown();
        }
    }
    
    private void Update()
    {
        if (!isRunning) return;
        
        // 委托给 GameDirector 更新
        GameDirector.Instance.Update(Time.deltaTime);
    }
    
    public void StartGame()
    {
        // 设置目标帧率
        Application.targetFrameRate = targetFrameRate;
        
        // 委托给 GameDirector
        GameDirector.Instance.StartGame();
    }
    
    private void Shutdown()
    {
        Debug.Log("GameApplication: 关闭应用程序...");
        
        // 委托给 GameDirector
        GameDirector.Instance.Shutdown();
        
        isRunning = false;
    }
}
```

#### 4.2 简化 GamePlayManager

```csharp
public class GamePlayManager : Singleton<GamePlayManager>, IGameSubsystem
{
    public string SubsystemName => "GamePlayManager";
    public bool IsInitialized { get; private set; }
    
    public void Initialize()
    {
        ASLogger.Instance.Info("GamePlayManager: 初始化游戏玩法管理器");
        
        // 初始化用户管理器和房间系统管理器
        UserManager.Instance?.Initialize();
        RoomSystemManager.Instance?.Initialize();
        
        // 注册网络消息处理器
        RegisterNetworkMessageHandlers();
        
        IsInitialized = true;
        ASLogger.Instance.Info("GamePlayManager: 初始化完成");
    }
    
    public void Update(float deltaTime)
    {
        // 简化的更新逻辑，主要逻辑已迁移到 GameDirector
    }
    
    public void Shutdown()
    {
        ASLogger.Instance.Info("GamePlayManager: 关闭游戏玩法管理器");
        
        // 取消注册网络消息处理器
        UnregisterNetworkMessageHandlers();
        
        // 清理网络游戏状态
        OnlineUsers.Clear();
        
        IsInitialized = false;
    }
    
    // 保持原有方法，但委托给 GameDirector
    public void StartGame(string gameSceneName)
    {
        GameDirector.Instance.StartGame(gameSceneName);
    }
}
```

#### 4.3 创建子系统适配器

```csharp
// 为现有管理器创建适配器
public class ResourceManagerAdapter : IGameSubsystem
{
    private ResourceManager _resourceManager;
    
    public string SubsystemName => "ResourceManager";
    public bool IsInitialized => _resourceManager != null;
    
    public void Initialize()
    {
        _resourceManager = ResourceManager.Instance;
        _resourceManager.Initialize();
    }
    
    public void Update(float deltaTime)
    {
        // ResourceManager 通常不需要每帧更新
    }
    
    public void Shutdown()
    {
        _resourceManager?.Shutdown();
        _resourceManager = null;
    }
}
```

#### 4.4 测试迁移后的代码

- 测试向后兼容性
- 测试功能完整性
- 性能对比测试

### 阶段五：优化和清理（第9-10周）

#### 5.1 性能优化

```csharp
public class GameDirector : Singleton<GameDirector>
{
    private readonly Dictionary<Type, IGameSubsystem> _subsystems;
    private readonly List<IGameSubsystem> _updateableSubsystems;
    
    private void InitializeSubsystems()
    {
        // 分离需要更新的子系统
        _updateableSubsystems = new List<IGameSubsystem>();
        
        foreach (var subsystem in _subsystems.Values)
        {
            if (subsystem is IUpdateableSubsystem updateable)
            {
                _updateableSubsystems.Add(updateable);
            }
        }
    }
    
    private void UpdateSubsystems(float deltaTime)
    {
        // 只更新需要更新的子系统
        foreach (var subsystem in _updateableSubsystems)
        {
            if (subsystem.IsInitialized)
            {
                subsystem.Update(deltaTime);
            }
        }
    }
}

public interface IUpdateableSubsystem : IGameSubsystem
{
    bool ShouldUpdate { get; }
    int UpdatePriority { get; }
}
```

#### 5.2 内存管理优化

```csharp
public class GameDirector : Singleton<GameDirector>
{
    private readonly ObjectPool<GameEvent> _eventPool;
    
    public void OnGameEvent(GameEvent gameEvent)
    {
        // 使用对象池管理事件
        ProcessEvent(gameEvent);
        
        // 回收事件对象
        if (gameEvent is IPoolable poolable)
        {
            _eventPool.Return(poolable);
        }
    }
}
```

#### 5.3 清理旧代码

- 移除未使用的方法
- 清理注释掉的代码
- 更新文档

### 阶段六：测试和验证（第11-12周）

#### 6.1 全面测试

**单元测试**：
- GameDirector 核心功能
- GameMode 状态管理
- 事件系统
- 配置管理

**集成测试**：
- 完整游戏流程
- 模式切换
- 状态转换
- 错误处理

**性能测试**：
- 内存使用
- CPU 使用率
- 帧率稳定性
- 启动时间

#### 6.2 用户验收测试

- 功能完整性验证
- 性能对比测试
- 稳定性测试
- 用户体验测试

## 风险评估和缓解策略

### 高风险项

1. **破坏性更改**
   - 风险：现有代码可能无法正常工作
   - 缓解：保持向后兼容性，渐进式迁移

2. **性能下降**
   - 风险：新架构可能影响性能
   - 缓解：性能监控，优化关键路径

3. **状态管理复杂性**
   - 风险：状态机可能过于复杂
   - 缓解：简化状态转换，充分测试

### 中风险项

1. **学习成本**
   - 风险：团队需要时间适应新架构
   - 缓解：提供详细文档和培训

2. **集成问题**
   - 风险：与现有系统集成可能有问题
   - 缓解：充分的集成测试

### 低风险项

1. **代码质量**
   - 风险：新代码可能存在 bug
   - 缓解：代码审查，单元测试

## 成功标准

### 功能标准

1. **完整性**：所有现有功能正常工作
2. **扩展性**：可以轻松添加新游戏模式
3. **稳定性**：系统运行稳定，无崩溃
4. **性能**：性能不低于现有系统

### 技术标准

1. **代码质量**：代码清晰，易于维护
2. **测试覆盖**：单元测试覆盖率 > 80%
3. **文档完整**：架构文档和 API 文档完整
4. **向后兼容**：现有 API 保持兼容

## 总结

这个重构计划为 Astrum 游戏提供了：

1. **清晰的实施路径**：分阶段实施，降低风险
2. **详细的实施步骤**：每个阶段都有具体的任务
3. **风险控制**：识别和缓解潜在风险
4. **成功标准**：明确的成功标准

通过这个计划，我们可以安全、高效地将现有架构升级为更强大、更灵活的 GameDirector 架构，为未来的功能扩展奠定坚实的基础。
