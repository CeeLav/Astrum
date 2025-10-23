# GameMode 系统扩展计划

## 概述

本文档详细描述了如何扩展现有的 GameMode 系统，增强游戏状态管理和模式切换能力，以支持更复杂的游戏场景和更好的架构设计。

## 当前 GameMode 系统分析

### 现有实现

1. **IGameMode 接口**：定义了基础的游戏模式接口
2. **SinglePlayerGameMode**：单机游戏模式实现
3. **MultiplayerGameMode**：联机游戏模式实现
4. **GamePlayManager**：负责 GameMode 的创建和切换

### 现有问题

1. **状态管理简单**：只有基本的 IsRunning 状态
2. **事件处理缺失**：缺乏统一的事件处理机制
3. **配置管理不足**：没有配置系统支持
4. **扩展性有限**：难以添加新的游戏模式特性

## 扩展方案

### 1. 增强 IGameMode 接口

#### 新增状态管理

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
    
    // 新增生命周期钩子
    void OnGameStart();
    void OnGamePause();
    void OnGameResume();
    void OnGameEnd();
    
    // 原有属性
    Room MainRoom { get; }
    Stage MainStage { get; }
    long PlayerId { get; }
    string ModeName { get; }
    bool IsRunning { get; }
}
```

#### 新增事件系统

```csharp
// 使用现有的 EventData 基类，而不是创建新的 GameEvent
public class GameModeStateChangedEventData : EventData
{
    public GameModeState PreviousState { get; set; }
    public GameModeState NewState { get; set; }
    
    public GameModeStateChangedEventData(GameModeState previousState, GameModeState newState)
    {
        PreviousState = previousState;
        NewState = newState;
    }
}

public class GameModeConfigChangedEventData : EventData
{
    public GameModeConfig Config { get; set; }
    
    public GameModeConfigChangedEventData(GameModeConfig config)
    {
        Config = config;
    }
}
```

#### 新增配置系统

```csharp
[Serializable]
public class GameModeConfig
{
    public string ModeName { get; set; }
    public bool AutoSave { get; set; } = true;
    public float UpdateInterval { get; set; } = 0.016f; // 60 FPS
    public Dictionary<string, object> CustomSettings { get; set; } = new();
    
    public T GetCustomSetting<T>(string key, T defaultValue = default)
    {
        if (CustomSettings.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
    
    public void SetCustomSetting<T>(string key, T value)
    {
        CustomSettings[key] = value;
    }
}
```

### 2. 创建基础 GameMode 实现

#### 抽象基类

```csharp
public abstract class BaseGameMode : IGameMode
{
    protected GameModeState _currentState = GameModeState.Initializing;
    protected GameModeConfig _config;
    protected Dictionary<Type, Delegate> _eventHandlers = new();
    
    // 状态管理
    public GameModeState CurrentState => _currentState;
    
    protected virtual void ChangeState(GameModeState newState)
    {
        if (!CanTransitionTo(newState))
        {
            ASLogger.Instance.Warning($"GameMode: 无法从 {_currentState} 转换到 {newState}");
            return;
        }
        
        var previousState = _currentState;
        OnStateExit(previousState);
        _currentState = newState;
        OnStateEnter(newState);
        
        // 触发状态变化事件
            OnGameEvent(new GameModeStateChangedEventData(previousState, newState));
    }
    
    protected virtual bool CanTransitionTo(GameModeState targetState)
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
    
    // 事件处理
    public virtual void OnGameEvent(EventData eventData)
    {
        // 使用现有的 EventSystem 发布事件
        EventSystem.Instance.Publish(eventData);
    }
    
    public void RegisterEventHandler<T>(Action<T> handler) where T : EventData
    {
        // 使用现有的 EventSystem 订阅事件
        EventSystem.Instance.Subscribe(handler);
    }
    
    public void UnregisterEventHandler<T>(Action<T> handler) where T : EventData
    {
        // 使用现有的 EventSystem 取消订阅事件
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
    public virtual void OnGameStart() { }
    public virtual void OnGamePause() { }
    public virtual void OnGameResume() { }
    public virtual void OnGameEnd() { }
    public virtual void SaveConfig() { }
    public virtual void LoadConfig() { }
}
```

### 3. 更新现有 GameMode 实现

#### 更新 SinglePlayerGameMode

```csharp
public class SinglePlayerGameMode : BaseGameMode
{
    // 原有属性
    public Room MainRoom { get; private set; }
    public Stage MainStage { get; private set; }
    public long PlayerId { get; private set; }
    public string ModeName => "SinglePlayer";
    public bool IsRunning { get; private set; }
    
    public override void Initialize()
    {
        ASLogger.Instance.Info("SinglePlayerGameMode: 初始化单机游戏模式");
        ChangeState(GameModeState.Initializing);
        
        // 注册事件处理器（使用现有 EventSystem）
        EventSystem.Instance.Subscribe<GameModeStateChangedEventData>(OnStateChanged);
        
        ChangeState(GameModeState.Ready);
    }
    
    public override void StartGame(string sceneName)
    {
        ASLogger.Instance.Info($"SinglePlayerGameMode: 启动单机游戏 - 场景: {sceneName}");
        
        try
        {
            ChangeState(GameModeState.Loading);
            
            // 原有启动逻辑
            CreateRoom();
            CreateStage();
            SwitchToGameScene(sceneName);
            
            ChangeState(GameModeState.Playing);
            IsRunning = true;
            OnGameStart();
            
            ASLogger.Instance.Info("SinglePlayerGameMode: 单机游戏启动成功");
        }
        catch (Exception ex)
        {
            ASLogger.Instance.Error($"SinglePlayerGameMode: 启动游戏失败 - {ex.Message}");
            ChangeState(GameModeState.Finished);
            throw;
        }
    }
    
    public override void Update(float deltaTime)
    {
        if (!IsRunning || CurrentState != GameModeState.Playing) return;
        
        // 原有更新逻辑
        if (MainRoom?.LSController != null && MainRoom.LSController.IsRunning)
        {
            MainRoom.LSController.AuthorityFrame = MainRoom.LSController.PredictionFrame;
        }
        
        MainRoom?.Update(deltaTime);
        MainStage?.Update(deltaTime);
    }
    
    public override void Shutdown()
    {
        ASLogger.Instance.Info("SinglePlayerGameMode: 关闭单机游戏模式");
        
        ChangeState(GameModeState.Ending);
        
        // 原有关闭逻辑
        if (MainStage != null)
        {
            MainStage.OnEntityViewAdded -= OnEntityViewAdded;
        }
        
        OnGameEnd();
        
        // 清理资源
        MainRoom = null;
        MainStage = null;
        PlayerId = -1;
        IsRunning = false;
        
        ChangeState(GameModeState.Finished);
    }
    
    // 状态变化处理
    protected override void OnStateEnter(GameModeState state)
    {
        base.OnStateEnter(state);
        
        switch (state)
        {
            case GameModeState.Playing:
                OnGameStart();
                break;
            case GameModeState.Paused:
                OnGamePause();
                break;
        }
    }
    
    protected override void OnStateExit(GameModeState state)
    {
        base.OnStateExit(state);
        
        switch (state)
        {
            case GameModeState.Playing:
                OnGameEnd();
                break;
        }
    }
    
    // 事件处理
    private void OnStateChanged(GameModeStateChangedEventData evt)
    {
        ASLogger.Instance.Info($"SinglePlayerGameMode: 状态从 {evt.PreviousState} 变为 {evt.NewState}");
    }
    
    // 配置管理
    protected override GameModeConfig CreateDefaultConfig()
    {
        return new GameModeConfig
        {
            ModeName = "SinglePlayer",
            AutoSave = true,
            UpdateInterval = 0.016f
        };
    }
    
    // 原有私有方法保持不变
    private void CreateRoom() { /* 原有实现 */ }
    private void CreateStage() { /* 原有实现 */ }
    private void SwitchToGameScene(string sceneName) { /* 原有实现 */ }
    private void OnEntityViewAdded(EntityView entityView) { /* 原有实现 */ }
}
```

#### 更新 MultiplayerGameMode

```csharp
public class MultiplayerGameMode : BaseGameMode
{
    // 原有属性
    public Room MainRoom { get; private set; }
    public Stage MainStage { get; private set; }
    public long PlayerId { get; private set; }
    public string ModeName => "Multiplayer";
    public bool IsRunning { get; private set; }
    
    // 辅助处理器
    private NetworkGameHandler _networkHandler;
    private FrameSyncHandler _frameSyncHandler;
    
    public override void Initialize()
    {
        ASLogger.Instance.Info("MultiplayerGameMode: 初始化联机游戏模式");
        ChangeState(GameModeState.Initializing);
        
        // 创建辅助处理器
        _networkHandler = new NetworkGameHandler(this);
        _frameSyncHandler = new FrameSyncHandler(this);
        
        // 注册网络消息处理器
        RegisterNetworkHandlers();
        
        // 注册事件
        EventSystem.Instance.Subscribe<FrameDataUploadEventData>(FrameDataUpload);
        EventSystem.Instance.Subscribe<NewPlayerEventData>(OnPlayerCreated);
        
        // 注册状态变化处理器（使用现有 EventSystem）
        EventSystem.Instance.Subscribe<GameModeStateChangedEventData>(OnStateChanged);
        
        ChangeState(GameModeState.Ready);
    }
    
    public override void StartGame(string sceneName)
    {
        ASLogger.Instance.Info("MultiplayerGameMode: 联机模式等待服务器游戏开始通知");
        // 联机模式不主动启动，等待服务器通知
    }
    
    public override void Update(float deltaTime)
    {
        if (!IsRunning || CurrentState != GameModeState.Playing) return;
        
        // 原有更新逻辑
        MainRoom?.Update(deltaTime);
        MainStage?.Update(deltaTime);
    }
    
    public override void Shutdown()
    {
        ASLogger.Instance.Info("MultiplayerGameMode: 关闭联机游戏模式");
        
        ChangeState(GameModeState.Ending);
        
        // 原有关闭逻辑
        UnregisterNetworkHandlers();
        EventSystem.Instance.Unsubscribe<FrameDataUploadEventData>(FrameDataUpload);
        EventSystem.Instance.Unsubscribe<NewPlayerEventData>(OnPlayerCreated);
        
        if (MainStage != null)
        {
            MainStage.OnEntityViewAdded -= OnEntityViewAdded;
        }
        
        OnGameEnd();
        
        // 清理资源
        MainRoom = null;
        MainStage = null;
        PlayerId = -1;
        IsRunning = false;
        
        ChangeState(GameModeState.Finished);
    }
    
    // 状态变化处理
    protected override void OnStateEnter(GameModeState state)
    {
        base.OnStateEnter(state);
        
        switch (state)
        {
            case GameModeState.Playing:
                OnGameStart();
                break;
            case GameModeState.Paused:
                OnGamePause();
                break;
        }
    }
    
    // 事件处理
    private void OnStateChanged(GameModeStateChangedEventData evt)
    {
        ASLogger.Instance.Info($"MultiplayerGameMode: 状态从 {evt.PreviousState} 变为 {evt.NewState}");
    }
    
    // 配置管理
    protected override GameModeConfig CreateDefaultConfig()
    {
        return new GameModeConfig
        {
            ModeName = "Multiplayer",
            AutoSave = false, // 联机模式不自动保存
            UpdateInterval = 0.016f
        };
    }
    
    // 原有方法保持不变
    private void RegisterNetworkHandlers() { /* 原有实现 */ }
    private void UnregisterNetworkHandlers() { /* 原有实现 */ }
    private void FrameDataUpload(FrameDataUploadEventData eventData) { /* 原有实现 */ }
    private void OnPlayerCreated(NewPlayerEventData eventData) { /* 原有实现 */ }
}
```

### 4. 专注于核心架构重构

**注意**：当前阶段专注于核心架构重构，不创建新的游戏模式。新游戏模式的创建将在核心架构稳定后进行。

**现有游戏模式优化**：
- 完善 SinglePlayerGameMode 的状态管理
- 完善 MultiplayerGameMode 的状态管理
- 确保与现有事件体系的完全兼容
- 优化配置管理功能

## 实施计划

### 阶段一：基础扩展（1-2周）

1. **创建基础类**
   - 实现 BaseGameMode 抽象类
   - 创建 GameModeState 枚举
   - 实现 GameEvent 系统

2. **更新现有接口**
   - 扩展 IGameMode 接口
   - 保持向后兼容性
   - 添加默认实现

### 阶段二：现有模式更新（2-3周）

1. **更新 SinglePlayerGameMode**
   - 继承 BaseGameMode
   - 实现状态管理
   - 添加事件处理

2. **更新 MultiplayerGameMode**
   - 继承 BaseGameMode
   - 实现状态管理
   - 保持网络功能

### 阶段三：核心架构集成（3-4周）

1. **集成到 GameDirector**
   - 更新 GameDirector 以支持 GameMode 管理
   - 实现模式切换逻辑
   - 添加配置管理

2. **优化现有 GameMode**
   - 完善状态管理功能
   - 优化事件处理机制
   - 测试核心功能

### 阶段四：优化和测试（1-2周）

1. **性能优化**
   - 优化事件系统
   - 优化状态切换
   - 内存管理优化

2. **全面测试**
   - 单元测试
   - 集成测试
   - 性能测试

## 总结

通过这个扩展计划，GameMode 系统将获得：

1. **强大的状态管理**：完整的状态机支持
2. **兼容的事件系统**：基于现有 EventSystem 的事件处理
3. **配置管理**：支持模式特定的配置
4. **向后兼容**：现有代码无需大幅修改
5. **核心架构优化**：为 GameDirector 集成做好准备

这个扩展专注于核心架构重构，确保与现有事件体系的完全兼容，为 Astrum 游戏提供了更强大、更灵活的游戏模式管理能力，为未来的功能扩展奠定了坚实的基础。
