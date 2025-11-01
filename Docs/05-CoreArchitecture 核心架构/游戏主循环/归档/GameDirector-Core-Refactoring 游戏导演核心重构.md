# GameDirector 核心重构方案

## 概述

本文档提供了基于现有事件体系的 GameDirector 核心重构方案，专注于核心架构优化，确保与现有系统的完全兼容。

## 核心设计原则

1. **兼容现有事件体系**：使用现有的 `EventSystem` 和 `EventData` 基类
2. **渐进式重构**：不破坏现有功能，逐步迁移
3. **专注核心架构**：先完成核心重构，再考虑新功能
4. **向后兼容**：保持现有 API 的兼容性

## 核心组件设计

### 1. GameDirector 核心类

```csharp
// 文件：AstrumProj/Assets/Script/AstrumClient/Core/GameDirector.cs
namespace Astrum.Client.Core
{
    public class GameDirector : Singleton<GameDirector>
    {
        // 游戏状态
        private GameState _currentState = GameState.ApplicationStarting;
        private IGameMode _currentGameMode;
        
        // Manager 引用
        private ResourceManager _resourceManager;
        private SceneManager _sceneManager;
        private NetworkManager _networkManager;
        private UIManager _uiManager;
        private AudioManager _audioManager;
        private InputManager _inputManager;
        private CameraManager _cameraManager;
        private GamePlayManager _gamePlayManager;
        
        // 公共属性
        public GameState CurrentState => _currentState;
        public IGameMode CurrentGameMode => _currentGameMode;
        
        // 核心方法
        public void Initialize()
        {
            ASLogger.Instance.Info("GameDirector: 初始化游戏导演");
            
            // 初始化子系统
            InitializeSubsystems();
            
            // 设置初始状态
            ChangeGameState(GameState.ApplicationReady);
            
            ASLogger.Instance.Info("GameDirector: 初始化完成");
        }
        
        public void Update(float deltaTime)
        {
            // 更新当前游戏模式
            _currentGameMode?.Update(deltaTime);
            
            // 更新子系统
            UpdateSubsystems(deltaTime);
        }
        
        public void ChangeGameState(GameState newState)
        {
            if (_currentState == newState) return;
            
            var previousState = _currentState;
            _currentState = newState;
            
            // 使用现有 EventSystem 发布事件
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
            
            // 使用现有 EventSystem 发布事件
            EventSystem.Instance.Publish(new GameModeChangedEventData(previousMode, newMode));
            
            ASLogger.Instance.Info($"GameDirector: 切换到游戏模式 {newMode?.ModeName}");
        }
        
        private void InitializeManagers()
        {
            ASLogger.Instance.Info("GameDirector: 初始化各个 Manager");
            
            // 直接初始化各个 Manager
            _resourceManager = ResourceManager.Instance;
            _resourceManager.Initialize();
            
            _sceneManager = SceneManager.Instance;
            _sceneManager.Initialize();
            
            _networkManager = NetworkManager.Instance;
            _networkManager.Initialize();
            
            _uiManager = UIManager.Instance;
            _uiManager.Initialize();
            
            _audioManager = AudioManager.Instance;
            _audioManager.Initialize();
            
            _inputManager = InputManager.Instance;
            _inputManager.Initialize();
            
            _cameraManager = CameraManager.Instance;
            _cameraManager.Initialize();
            
            _gamePlayManager = GamePlayManager.Instance;
            _gamePlayManager.Initialize();
            
            ASLogger.Instance.Info("GameDirector: 所有 Manager 初始化完成");
        }
        
        private void UpdateManagers(float deltaTime)
        {
            // 更新各个 Manager
            _inputManager?.Update();
            _networkManager?.Update();
            _uiManager?.Update();
            _audioManager?.Update();
            _cameraManager?.Update();
            _gamePlayManager?.Update(deltaTime);
        }
        
        public void Shutdown()
        {
            ASLogger.Instance.Info("GameDirector: 关闭游戏导演");
            
            // 关闭当前游戏模式
            _currentGameMode?.Shutdown();
            _currentGameMode = null;
            
            // 关闭所有 Manager
            _resourceManager?.Shutdown();
            _sceneManager?.Shutdown();
            _networkManager?.Shutdown();
            _uiManager?.Shutdown();
            _audioManager?.Shutdown();
            _inputManager?.Shutdown();
            _cameraManager?.Shutdown();
            _gamePlayManager?.Shutdown();
            
            // 设置关闭状态
            ChangeGameState(GameState.SystemShutdown);
        }
    }
}
```

### 2. 游戏状态枚举

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

### 3. 事件数据类（基于现有 EventData）

```csharp
// 文件：AstrumProj/Assets/Script/AstrumClient/Core/GameDirectorEvents.cs
namespace Astrum.Client.Core
{
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

### 4. 简化的 Manager 管理

GameDirector 直接管理各个 Manager，不需要额外的接口层：

```csharp
// GameDirector 中直接初始化各个 Manager
private void InitializeManagers()
{
    // 直接初始化各个 Manager
    ResourceManager.Instance.Initialize();
    SceneManager.Instance.Initialize();
    NetworkManager.Instance.Initialize();
    UIManager.Instance.Initialize();
    AudioManager.Instance.Initialize();
    InputManager.Instance.Initialize();
    CameraManager.Instance.Initialize();
    GamePlayManager.Instance.Initialize();
}
```

## GameMode 系统扩展

### 1. 扩展 IGameMode 接口

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
        bool CanTransitionTo(GameModeState targetState);
        
        // 新增事件处理（基于现有 EventSystem）
        void OnGameEvent(EventData eventData);
        void RegisterEventHandler<T>(Action<T> handler) where T : EventData;
        void UnregisterEventHandler<T>(Action<T> handler) where T : EventData;
        
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

### 2. 创建 BaseGameMode 抽象类

```csharp
// 文件：AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/BaseGameMode.cs
namespace Astrum.Client.Managers.GameModes
{
    public abstract class BaseGameMode : IGameMode
    {
        protected GameModeState _currentState = GameModeState.Initializing;
        protected GameModeConfig _config;
        
        // 状态管理
        public GameModeState CurrentState => _currentState;
        
        protected virtual void ChangeState(GameModeState newState)
        {
            if (!CanTransitionTo(newState)) return;
            
            var previousState = _currentState;
            OnStateExit(previousState);
            _currentState = newState;
            OnStateEnter(newState);
            
            // 使用现有 EventSystem 发布事件
            OnGameEvent(new GameModeStateChangedEventData(previousState, newState));
        }
        
        public virtual bool CanTransitionTo(GameModeState targetState)
        {
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
        
        // 事件处理（基于现有 EventSystem）
        public virtual void OnGameEvent(EventData eventData)
        {
            EventSystem.Instance.Publish(eventData);
        }
        
        public void RegisterEventHandler<T>(Action<T> handler) where T : EventData
        {
            EventSystem.Instance.Subscribe(handler);
        }
        
        public void UnregisterEventHandler<T>(Action<T> handler) where T : EventData
        {
            EventSystem.Instance.Unsubscribe(handler);
        }
        
        // 配置管理
        public virtual GameModeConfig GetConfig()
        {
            return _config ??= CreateDefaultConfig();
        }
        
        public virtual void ApplyConfig(GameModeConfig config)
        {
            _config = config;
            OnGameEvent(new GameModeConfigChangedEventData(config));
        }
        
        protected abstract GameModeConfig CreateDefaultConfig();
        
        // 抽象方法，子类必须实现
        public abstract void Initialize();
        public abstract void StartGame(string sceneName);
        public abstract void Update(float deltaTime);
        public abstract void Shutdown();
        public abstract Room MainRoom { get; }
        public abstract Stage MainStage { get; }
        public abstract long PlayerId { get; }
        public abstract string ModeName { get; }
        public abstract bool IsRunning { get; }
        
        // 虚方法，子类可以重写
        public virtual void OnStateEnter(GameModeState state) { }
        public virtual void OnStateExit(GameModeState state) { }
        public virtual void SaveConfig() { }
        public virtual void LoadConfig() { }
    }
}
```

### 3. 更新现有 GameMode 实现

**更新 SinglePlayerGameMode**：
- 继承 BaseGameMode
- 使用现有 EventSystem 处理事件
- 保持原有功能不变

**更新 MultiplayerGameMode**：
- 继承 BaseGameMode
- 使用现有 EventSystem 处理事件
- 保持网络功能不变

## 实施步骤

### 阶段一：基础架构搭建（第1-2周）

1. **创建核心类文件**
   - GameDirector.cs
   - GameState.cs
   - GameDirectorEvents.cs
   - IGameSubsystem.cs

2. **扩展 IGameMode 接口**
   - 添加状态管理方法
   - 添加事件处理方法
   - 添加配置管理方法

3. **创建 BaseGameMode 抽象类**
   - 实现通用状态管理
   - 实现事件处理机制
   - 实现配置管理

### 阶段二：现有 GameMode 更新（第3-4周）

1. **更新 SinglePlayerGameMode**
   - 继承 BaseGameMode
   - 实现状态管理
   - 保持原有功能

2. **更新 MultiplayerGameMode**
   - 继承 BaseGameMode
   - 实现状态管理
   - 保持网络功能

3. **测试现有功能**
   - 确保单机模式正常工作
   - 确保联机模式正常工作
   - 验证事件系统兼容性

### 阶段三：GameDirector 集成（第5-6周）

1. **实现 GameDirector 核心功能**
   - 子系统管理
   - 状态管理
   - 游戏模式切换

2. **修改 GameApplication**
   - 简化初始化逻辑
   - 委托给 GameDirector
   - 保持向后兼容

3. **简化 GamePlayManager**
   - 移除重复逻辑
   - 专注于网络管理
   - 委托给 GameDirector

### 阶段四：手动测试和优化（第7-8周）

1. **Unity 编辑器手动测试**
   - 测试 GameDirector 初始化
   - 测试单机模式完整流程
   - 测试联机模式完整流程
   - 测试游戏模式切换
   - 测试状态转换

2. **优化和清理**
   - 性能优化
   - 代码清理
   - 文档更新

## 兼容性保证

### 1. 现有 API 兼容性
- 保持所有现有公共方法
- 保持现有调用方式
- 添加新的扩展方法

### 2. 事件系统兼容性
- 使用现有 EventSystem
- 使用现有 EventData 基类
- 保持现有事件处理方式

### 3. 渐进式迁移
- 分阶段实施
- 最小化破坏性更改
- 保持功能完整性

## 总结

这个核心重构方案专注于：

1. **兼容现有事件体系**：完全基于现有 EventSystem
2. **渐进式重构**：不破坏现有功能
3. **专注核心架构**：先完成基础重构
4. **向后兼容**：保持现有 API 不变

通过这个方案，我们可以安全、高效地将现有架构升级为更强大、更灵活的 GameDirector 架构，为未来的功能扩展奠定坚实的基础。
