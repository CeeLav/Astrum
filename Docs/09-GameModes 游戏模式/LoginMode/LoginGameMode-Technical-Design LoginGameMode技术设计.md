# LoginGameMode 技术设计方案

## 1. 概述

### 1.1 背景

当前登录相关的逻辑全部集中在 `LoginView` 中，包括：
- 网络连接管理
- 登录流程控制
- 匹配状态管理
- UI 状态更新

这违反了 MVC/MVP 架构中的职责分离原则，View 层应该只负责 UI 展示，而业务逻辑应该由控制层（GameMode）管理。

### 1.2 目标

创建 `LoginGameMode` 作为登录状态的专用 GameMode，负责：
- **登录流程管理**：连接、登录、匹配等业务逻辑
- **状态管理**：登录状态的生命周期管理
- **事件协调**：网络事件、用户事件的统一处理
- **UI 协调**：与 LoginView 的交互，但不直接操作 UI

将 `LoginView` 重构为纯 UI 层，只负责：
- UI 元素的显示/隐藏
- 用户输入的捕获和传递
- 接收 LoginGameMode 的指令更新 UI

### 1.3 设计原则

1. **关注点分离**：业务逻辑与 UI 展示分离
2. **单一职责**：LoginGameMode 专注于登录状态管理
3. **状态驱动**：基于 GameModeState 进行流程控制
4. **事件驱动**：通过 EventSystem 进行组件间通信
5. **向后兼容**：保持现有登录流程不变

---

## 2. 架构设计

### 2.1 类图

```
┌─────────────────────────────────────────────────────────┐
│                    GameDirector                         │
│  - CurrentGameMode: IGameMode                           │
│  + SwitchGameMode(IGameMode)                            │
└────────────────────┬────────────────────────────────────┘
                     │
                     │ manages
                     ▼
┌─────────────────────────────────────────────────────────┐
│                  BaseGameMode                           │
│  # _currentState: GameModeState                         │
│  # ChangeState(GameModeState)                           │
│  + OnGameEvent(EventData)                               │
└────────────────────┬────────────────────────────────────┘
                     │
                     │ inherits
                     ▼
┌─────────────────────────────────────────────────────────┐
│                 LoginGameMode                           │
│  - _connectionState: ConnectionState                    │
│  - _matchState: MatchState                              │
│  - _serverAddress: string                               │
│  - _serverPort: int                                     │
│                                                          │
│  + Initialize()                                         │
│  + StartGame(string)                                    │
│  + Update(float)                                        │
│  + Shutdown()                                           │
│                                                          │
│  + ConnectToServer()                                    │
│  + SendLoginRequest()                                   │
│  + SendQuickMatchRequest()                              │
│  + SendCancelMatchRequest()                             │
│  + StartSinglePlayerGame()                              │
│                                                          │
│  - OnConnectResponse(ConnectResponseEventData)          │
│  - OnUserLoggedIn(UserInfo)                             │
│  - OnLoginError(string)                                 │
│  - OnQuickMatchResponse(QuickMatchResponse)             │
│  - OnMatchFoundNotification(MatchFoundNotification)     │
└────────────────────┬────────────────────────────────────┘
                     │
                     │ notifies
                     ▼
┌─────────────────────────────────────────────────────────┐
│                   LoginView                             │
│  - connectButtonButton: Button                          │
│  - connectionStatusText: Text                           │
│                                                          │
│  + UpdateConnectionStatus(string)                       │
│  + UpdateConnectButtonText(string)                      │
│  + SetConnectButtonInteractable(bool)                   │
│                                                          │
│  - OnConnectButtonClicked()                             │
│  - OnSinglePlayButtonClicked()                          │
└─────────────────────────────────────────────────────────┘
```

### 2.2 状态管理

#### 2.2.1 GameModeState（继承自 BaseGameMode）

```csharp
public enum GameModeState
{
    Initializing,    // 初始化中
    Loading,         // 加载中（保留，暂不使用）
    Ready,          // 准备就绪（显示登录界面）
    Playing,        // 游戏中（保留，暂不使用）
    Paused,         // 暂停（保留，暂不使用）
    Ending,         // 结束中
    Finished        // 已结束
}
```

**LoginGameMode 使用的状态**：
- `Initializing` → `Ready`：初始化完成，显示登录界面
- `Ready` → `Ending`：用户选择进入游戏，准备切换到其他 GameMode
- `Ending` → `Finished`：清理完成

#### 2.2.2 ConnectionState（LoginGameMode 内部状态）

```csharp
private enum ConnectionState
{
    Disconnected,    // 未连接
    Connecting,      // 连接中
    Connected,       // 已连接
    LoggingIn,       // 登录中
    LoggedIn         // 已登录
}
```

#### 2.2.3 MatchState（LoginGameMode 内部状态）

```csharp
private enum MatchState
{
    None,           // 未匹配
    Matching,       // 匹配中
    MatchFound      // 匹配成功
}
```

### 2.3 事件流设计

#### 2.3.1 登录流程事件

```
用户点击"连接服务器"
    ↓
LoginView.OnConnectButtonClicked()
    ↓
LoginGameMode.ConnectToServer()
    ↓
NetworkManager.ConnectAsync()
    ↓
[网络层] ConnectResponseEventData
    ↓
LoginGameMode.OnConnectResponse()
    ↓
LoginGameMode.SendLoginRequest()
    ↓
[网络层] LoginResponse
    ↓
UserManager.HandleLoginResponse()
    ↓
UserManager.OnUserLoggedIn 事件
    ↓
LoginGameMode.OnUserLoggedIn()
    ↓
[发布] LoginStateChangedEventData
    ↓
LoginView.OnLoginStateChanged()
    ↓
LoginView.UpdateUI()
```

#### 2.3.2 匹配流程事件

```
用户点击"快速联机"
    ↓
LoginView.OnConnectButtonClicked()
    ↓
LoginGameMode.SendQuickMatchRequest()
    ↓
[网络层] QuickMatchResponse
    ↓
LoginGameMode.OnQuickMatchResponse()
    ↓
[发布] MatchStateChangedEventData
    ↓
LoginView.OnMatchStateChanged()
    ↓
LoginView.UpdateUI()
    ↓
[网络层] MatchFoundNotification
    ↓
LoginGameMode.OnMatchFoundNotification()
    ↓
LoginGameMode.SwitchToMultiplayerMode()
    ↓
GameDirector.SwitchGameMode(MultiplayerGameMode)
```

#### 2.3.3 单机游戏流程事件

```
用户点击"单机游戏"
    ↓
LoginView.OnSinglePlayButtonClicked()
    ↓
LoginGameMode.StartSinglePlayerGame()
    ↓
GameDirector.SwitchGameMode(SinglePlayerGameMode)
    ↓
GameDirector.StartGame("DungeonsGame")
```

---

## 3. 详细设计

### 3.1 LoginGameMode 核心属性

```csharp
public class LoginGameMode : BaseGameMode
{
    // 继承自 BaseGameMode
    public override Room MainRoom { get; set; }        // 登录模式不使用
    public override Stage MainStage { get; set; }      // 登录模式不使用
    public override long PlayerId { get; set; }        // 登录模式不使用
    public override string ModeName => "Login";
    public override bool IsRunning { get; set; }
    
    // 内部状态
    private ConnectionState _connectionState = ConnectionState.Disconnected;
    private MatchState _matchState = MatchState.None;
    
    // 服务器配置
    private string _serverAddress = "127.0.0.1";
    private int _serverPort = 8888;
    
    // 引用
    private LoginView _loginView;  // UI 引用（可选，用于直接通信）
}
```

### 3.2 生命周期方法

#### 3.2.1 Initialize()

```csharp
public override void Initialize()
{
    ASLogger.Instance.Info("LoginGameMode: 初始化登录模式");
    ChangeState(GameModeState.Initializing);
    
    // 订阅网络事件
    SubscribeToNetworkEvents();
    
    // 订阅用户事件
    SubscribeToUserEvents();
    
    // 显示登录 UI
    ShowLoginUI();
    
    ChangeState(GameModeState.Ready);
    ASLogger.Instance.Info("LoginGameMode: 初始化完成");
}
```

#### 3.2.2 StartGame()

```csharp
public override void StartGame(string sceneName)
{
    // LoginGameMode 不直接启动游戏
    // 游戏启动由 SinglePlayerGameMode 或 MultiplayerGameMode 负责
    ASLogger.Instance.Warning("LoginGameMode: 登录模式不支持直接启动游戏");
}
```

#### 3.2.3 Update()

```csharp
public override void Update(float deltaTime)
{
    // LoginGameMode 通常不需要每帧更新
    // 所有逻辑都是事件驱动的
}
```

#### 3.2.4 Shutdown()

```csharp
public override void Shutdown()
{
    ASLogger.Instance.Info("LoginGameMode: 关闭登录模式");
    ChangeState(GameModeState.Ending);
    
    // 取消订阅事件
    UnsubscribeFromNetworkEvents();
    UnsubscribeFromUserEvents();
    
    // 清理状态
    _connectionState = ConnectionState.Disconnected;
    _matchState = MatchState.None;
    _loginView = null;
    
    ChangeState(GameModeState.Finished);
}
```

### 3.3 核心业务方法

#### 3.3.1 连接服务器

```csharp
/// <summary>
/// 连接到服务器
/// </summary>
public async void ConnectToServer()
{
    if (_connectionState != ConnectionState.Disconnected)
    {
        ASLogger.Instance.Warning("LoginGameMode: 已在连接或已连接状态");
        return;
    }
    
    try
    {
        _connectionState = ConnectionState.Connecting;
        PublishLoginStateChanged();
        
        ASLogger.Instance.Info($"LoginGameMode: 开始连接到服务器 {_serverAddress}:{_serverPort}");
        
        var networkManager = NetworkManager.Instance;
        if (networkManager == null)
        {
            throw new Exception("NetworkManager 不存在");
        }
        
        // 连接到服务器
        var channelId = await networkManager.ConnectAsync(_serverAddress, _serverPort);
        ASLogger.Instance.Info($"LoginGameMode: 连接请求已发送，ChannelId: {channelId}");
        
        // 发送连接请求
        var connectRequest = ConnectRequest.Create();
        networkManager.Send(connectRequest);
        ASLogger.Instance.Info("LoginGameMode: 连接请求已发送");
    }
    catch (Exception ex)
    {
        ASLogger.Instance.Error($"LoginGameMode: 连接失败 - {ex.Message}");
        _connectionState = ConnectionState.Disconnected;
        PublishLoginStateChanged();
        PublishLoginError($"连接失败: {ex.Message}");
    }
}
```

#### 3.3.2 发送登录请求

```csharp
/// <summary>
/// 发送登录请求
/// </summary>
private void SendLoginRequest()
{
    if (_connectionState != ConnectionState.Connected)
    {
        ASLogger.Instance.Warning("LoginGameMode: 未连接，无法发送登录请求");
        return;
    }
    
    try
    {
        _connectionState = ConnectionState.LoggingIn;
        PublishLoginStateChanged();
        
        var networkManager = NetworkManager.Instance;
        if (networkManager == null)
        {
            throw new Exception("NetworkManager 不存在");
        }
        
        // 创建登录请求
        var loginRequest = LoginRequest.Create();
        loginRequest.DisplayName = "Player_" + UnityEngine.Random.Range(1000, 9999);
        
        // 发送登录请求
        networkManager.Send(loginRequest);
        ASLogger.Instance.Info($"LoginGameMode: 登录请求已发送，显示名称: {loginRequest.DisplayName}");
    }
    catch (Exception ex)
    {
        ASLogger.Instance.Error($"LoginGameMode: 发送登录请求失败 - {ex.Message}");
        _connectionState = ConnectionState.Connected;
        PublishLoginStateChanged();
        PublishLoginError($"登录请求失败: {ex.Message}");
    }
}
```

#### 3.3.3 发送快速匹配请求

```csharp
/// <summary>
/// 发送快速匹配请求
/// </summary>
public void SendQuickMatchRequest()
{
    if (_connectionState != ConnectionState.LoggedIn)
    {
        ASLogger.Instance.Warning("LoginGameMode: 未登录，无法发送匹配请求");
        return;
    }
    
    if (_matchState != MatchState.None)
    {
        ASLogger.Instance.Warning("LoginGameMode: 已在匹配状态");
        return;
    }
    
    try
    {
        var networkManager = NetworkManager.Instance;
        if (networkManager == null)
        {
            throw new Exception("NetworkManager 不存在");
        }
        
        // 创建快速匹配请求
        var quickMatchRequest = QuickMatchRequest.Create();
        quickMatchRequest.Timestamp = TimeInfo.Instance.ClientNow();
        
        // 发送请求
        networkManager.Send(quickMatchRequest);
        ASLogger.Instance.Info("LoginGameMode: 快速匹配请求已发送");
        
        // 注意：不立即更新状态，等待服务器响应
    }
    catch (Exception ex)
    {
        ASLogger.Instance.Error($"LoginGameMode: 发送快速匹配请求失败 - {ex.Message}");
        PublishLoginError($"匹配请求失败: {ex.Message}");
    }
}
```

#### 3.3.4 启动单机游戏

```csharp
/// <summary>
/// 启动单机游戏
/// </summary>
public void StartSinglePlayerGame()
{
    try
    {
        ASLogger.Instance.Info("LoginGameMode: 启动单机游戏");
        
        // 1. 设置单机模式
        GameConfig.Instance.SetSinglePlayerMode(true);
        
        // 2. 隐藏登录 UI
        HideLoginUI();
        
        // 3. 创建单机游戏模式并切换
        var singlePlayerMode = new SinglePlayerGameMode();
        GameDirector.Instance.SwitchGameMode(singlePlayerMode);
        
        // 4. 启动游戏
        GameDirector.Instance.StartGame("DungeonsGame");
        
        ASLogger.Instance.Info("LoginGameMode: 单机游戏启动成功");
    }
    catch (Exception ex)
    {
        ASLogger.Instance.Error($"LoginGameMode: 启动单机游戏失败 - {ex.Message}");
        PublishLoginError($"启动游戏失败: {ex.Message}");
    }
}
```

### 3.4 事件处理方法

#### 3.4.1 连接响应

```csharp
/// <summary>
/// 处理连接响应事件
/// </summary>
private void OnConnectResponse(ConnectResponseEventData eventData)
{
    ASLogger.Instance.Info($"LoginGameMode: 收到连接响应 - Success: {eventData.Success}, Message: {eventData.Message}");
    
    if (eventData.Success)
    {
        _connectionState = ConnectionState.Connected;
        PublishLoginStateChanged();
        
        // 自动发送登录请求
        SendLoginRequest();
    }
    else
    {
        _connectionState = ConnectionState.Disconnected;
        PublishLoginStateChanged();
        PublishLoginError($"连接失败: {eventData.Message}");
    }
}
```

#### 3.4.2 用户登录成功

```csharp
/// <summary>
/// 用户登录成功事件处理
/// </summary>
private void OnUserLoggedIn(UserInfo userInfo)
{
    ASLogger.Instance.Info($"LoginGameMode: 用户登录成功 - ID: {userInfo.Id}, Name: {userInfo.DisplayName}");
    
    _connectionState = ConnectionState.LoggedIn;
    PublishLoginStateChanged();
}
```

#### 3.4.3 登录错误

```csharp
/// <summary>
/// 登录错误事件处理
/// </summary>
private void OnLoginError(string errorMessage)
{
    ASLogger.Instance.Error($"LoginGameMode: 登录失败 - {errorMessage}");
    
    _connectionState = ConnectionState.Connected;
    PublishLoginStateChanged();
    PublishLoginError(errorMessage);
}
```

#### 3.4.4 快速匹配响应

```csharp
/// <summary>
/// 快速匹配响应事件
/// </summary>
private void OnQuickMatchResponse(QuickMatchResponse response)
{
    ASLogger.Instance.Info($"LoginGameMode: 收到快速匹配响应 - Success: {response.Success}, Message: {response.Message}");
    
    if (response.Success)
    {
        _matchState = MatchState.Matching;
        PublishMatchStateChanged(response.QueuePosition, response.QueueSize);
    }
    else
    {
        _matchState = MatchState.None;
        PublishLoginError($"匹配失败: {response.Message}");
    }
}
```

#### 3.4.5 匹配成功通知

```csharp
/// <summary>
/// 匹配成功通知事件
/// </summary>
private void OnMatchFoundNotification(MatchFoundNotification notification)
{
    ASLogger.Instance.Info($"LoginGameMode: 匹配成功 - Room: {notification.Room?.Id}");
    
    _matchState = MatchState.MatchFound;
    PublishMatchStateChanged(0, 0);
    
    // 切换到联机游戏模式
    SwitchToMultiplayerMode();
}
```

#### 3.4.6 切换到联机模式

```csharp
/// <summary>
/// 切换到联机游戏模式
/// </summary>
private void SwitchToMultiplayerMode()
{
    try
    {
        ASLogger.Instance.Info("LoginGameMode: 切换到联机游戏模式");
        
        // 1. 设置联机模式
        GameConfig.Instance.SetSinglePlayerMode(false);
        
        // 2. 隐藏登录 UI
        HideLoginUI();
        
        // 3. 创建联机游戏模式并切换
        var multiplayerMode = new MultiplayerGameMode();
        GameDirector.Instance.SwitchGameMode(multiplayerMode);
        
        // 注意：联机模式会等待服务器的 GameStartNotification
        
        ASLogger.Instance.Info("LoginGameMode: 联机游戏模式切换成功");
    }
    catch (Exception ex)
    {
        ASLogger.Instance.Error($"LoginGameMode: 切换到联机模式失败 - {ex.Message}");
        PublishLoginError($"切换游戏模式失败: {ex.Message}");
    }
}
```

### 3.5 事件发布方法

```csharp
/// <summary>
/// 发布登录状态变化事件
/// </summary>
private void PublishLoginStateChanged()
{
    var eventData = new LoginStateChangedEventData
    {
        ConnectionState = _connectionState,
        MatchState = _matchState
    };
    EventSystem.Instance.Publish(eventData);
}

/// <summary>
/// 发布匹配状态变化事件
/// </summary>
private void PublishMatchStateChanged(int queuePosition, int queueSize)
{
    var eventData = new MatchStateChangedEventData
    {
        MatchState = _matchState,
        QueuePosition = queuePosition,
        QueueSize = queueSize
    };
    EventSystem.Instance.Publish(eventData);
}

/// <summary>
/// 发布登录错误事件
/// </summary>
private void PublishLoginError(string errorMessage)
{
    var eventData = new LoginErrorEventData
    {
        ErrorMessage = errorMessage
    };
    EventSystem.Instance.Publish(eventData);
}
```

### 3.6 UI 交互方法

```csharp
/// <summary>
/// 显示登录 UI
/// </summary>
private void ShowLoginUI()
{
    try
    {
        ASLogger.Instance.Info("LoginGameMode: 显示登录UI");
        _loginView = UIManager.Instance?.ShowUI("Login") as LoginView;
        
        if (_loginView != null)
        {
            // 设置 LoginView 的回调（可选，也可以通过事件）
            _loginView.SetLoginGameMode(this);
        }
    }
    catch (Exception ex)
    {
        ASLogger.Instance.Error($"LoginGameMode: 显示登录UI失败 - {ex.Message}");
    }
}

/// <summary>
/// 隐藏登录 UI
/// </summary>
private void HideLoginUI()
{
    try
    {
        ASLogger.Instance.Info("LoginGameMode: 隐藏登录UI");
        UIManager.Instance?.HideUI("Login");
        UIManager.Instance?.DestroyUI("Login");
        _loginView = null;
    }
    catch (Exception ex)
    {
        ASLogger.Instance.Error($"LoginGameMode: 隐藏登录UI失败 - {ex.Message}");
    }
}
```

---

## 4. LoginView 重构设计

### 4.1 重构后的职责

LoginView 重构为纯 UI 层，只负责：
1. **UI 元素管理**：按钮、文本、输入框等
2. **用户输入捕获**：按钮点击事件
3. **UI 状态更新**：根据事件更新显示内容
4. **与 LoginGameMode 通信**：通过引用或事件

### 4.2 重构后的核心方法

```csharp
public partial class LoginView
{
    // 引用 LoginGameMode（可选）
    private LoginGameMode _loginGameMode;
    
    /// <summary>
    /// 设置 LoginGameMode 引用
    /// </summary>
    public void SetLoginGameMode(LoginGameMode loginGameMode)
    {
        _loginGameMode = loginGameMode;
    }
    
    /// <summary>
    /// 初始化完成后的回调
    /// </summary>
    protected virtual void OnInitialize()
    {
        // 绑定按钮事件
        if (connectButtonButton != null)
        {
            connectButtonButton.onClick.AddListener(OnConnectButtonClicked);
        }
        
        if (singlePlayButtonButton != null)
        {
            singlePlayButtonButton.onClick.AddListener(OnSinglePlayButtonClicked);
        }
        
        // 订阅 LoginGameMode 的事件
        SubscribeToLoginEvents();
        
        // 初始化UI状态
        UpdateConnectionStatus("未连接");
        UpdateConnectButtonText("连接服务器");
    }
    
    /// <summary>
    /// 订阅登录事件
    /// </summary>
    private void SubscribeToLoginEvents()
    {
        EventSystem.Instance.Subscribe<LoginStateChangedEventData>(OnLoginStateChanged);
        EventSystem.Instance.Subscribe<MatchStateChangedEventData>(OnMatchStateChanged);
        EventSystem.Instance.Subscribe<LoginErrorEventData>(OnLoginError);
    }
    
    /// <summary>
    /// 连接按钮点击事件（委托给 LoginGameMode）
    /// </summary>
    private void OnConnectButtonClicked()
    {
        if (_loginGameMode == null)
        {
            Debug.LogError("LoginView: LoginGameMode 未设置");
            return;
        }
        
        // 根据当前状态执行不同操作
        // 具体逻辑由 LoginGameMode 决定
        _loginGameMode.OnConnectButtonClicked();
    }
    
    /// <summary>
    /// 单机游戏按钮点击事件（委托给 LoginGameMode）
    /// </summary>
    private void OnSinglePlayButtonClicked()
    {
        if (_loginGameMode == null)
        {
            Debug.LogError("LoginView: LoginGameMode 未设置");
            return;
        }
        
        _loginGameMode.StartSinglePlayerGame();
    }
    
    /// <summary>
    /// 登录状态变化事件处理（更新 UI）
    /// </summary>
    private void OnLoginStateChanged(LoginStateChangedEventData eventData)
    {
        switch (eventData.ConnectionState)
        {
            case ConnectionState.Disconnected:
                UpdateConnectionStatus("未连接");
                UpdateConnectButtonText("连接服务器");
                SetConnectButtonInteractable(true);
                break;
                
            case ConnectionState.Connecting:
                UpdateConnectionStatus("正在连接...");
                UpdateConnectButtonText("连接中...");
                SetConnectButtonInteractable(false);
                break;
                
            case ConnectionState.Connected:
                UpdateConnectionStatus("已连接，正在登录...");
                UpdateConnectButtonText("连接中...");
                SetConnectButtonInteractable(false);
                break;
                
            case ConnectionState.LoggingIn:
                UpdateConnectionStatus("正在登录...");
                UpdateConnectButtonText("登录中...");
                SetConnectButtonInteractable(false);
                break;
                
            case ConnectionState.LoggedIn:
                UpdateConnectionStatus("登录成功");
                UpdateConnectButtonText("快速联机");
                SetConnectButtonInteractable(true);
                break;
        }
    }
    
    /// <summary>
    /// 匹配状态变化事件处理（更新 UI）
    /// </summary>
    private void OnMatchStateChanged(MatchStateChangedEventData eventData)
    {
        switch (eventData.MatchState)
        {
            case MatchState.None:
                UpdateConnectButtonText("快速联机");
                SetConnectButtonInteractable(true);
                break;
                
            case MatchState.Matching:
                UpdateConnectionStatus($"匹配中... (队列位置: {eventData.QueuePosition + 1}/{eventData.QueueSize})");
                UpdateConnectButtonText("取消匹配");
                SetConnectButtonInteractable(true);
                break;
                
            case MatchState.MatchFound:
                UpdateConnectionStatus("匹配成功！正在进入房间...");
                UpdateConnectButtonText("进入游戏");
                SetConnectButtonInteractable(false);
                break;
        }
    }
    
    /// <summary>
    /// 登录错误事件处理（更新 UI）
    /// </summary>
    private void OnLoginError(LoginErrorEventData eventData)
    {
        UpdateConnectionStatus($"错误: {eventData.ErrorMessage}");
    }
    
    // UI 更新方法保持不变
    private void UpdateConnectionStatus(string status) { /* 实现 */ }
    private void UpdateConnectButtonText(string text) { /* 实现 */ }
    private void SetConnectButtonInteractable(bool interactable) { /* 实现 */ }
}
```

---

## 5. 新增事件定义

### 5.1 LoginStateChangedEventData

```csharp
/// <summary>
/// 登录状态变化事件数据
/// </summary>
public class LoginStateChangedEventData : EventData
{
    public ConnectionState ConnectionState { get; set; }
    public MatchState MatchState { get; set; }
}
```

### 5.2 MatchStateChangedEventData

```csharp
/// <summary>
/// 匹配状态变化事件数据
/// </summary>
public class MatchStateChangedEventData : EventData
{
    public MatchState MatchState { get; set; }
    public int QueuePosition { get; set; }
    public int QueueSize { get; set; }
}
```

### 5.3 LoginErrorEventData

```csharp
/// <summary>
/// 登录错误事件数据
/// </summary>
public class LoginErrorEventData : EventData
{
    public string ErrorMessage { get; set; }
}
```

---

## 6. 集成到 GameDirector

### 6.1 GameDirector 初始化时创建 LoginGameMode

```csharp
public class GameDirector : Singleton<GameDirector>
{
    public void Initialize()
    {
        ASLogger.Instance.Info("GameDirector: 初始化游戏导演");
        
        try
        {
            // 初始化各个 Manager
            InitializeManagers();
            
            // 设置初始状态
            ChangeGameState(GameState.ApplicationReady);
            
            // 创建并切换到 LoginGameMode
            var loginMode = new LoginGameMode();
            SwitchGameMode(loginMode);
            
            ASLogger.Instance.Info("GameDirector: 初始化完成");
        }
        catch (System.Exception ex)
        {
            ASLogger.Instance.Error($"GameDirector: 初始化失败 - {ex.Message}");
            throw;
        }
    }
}
```

### 6.2 移除 GameDirector 中的 ShowLoginUI

```csharp
// 删除以下方法，由 LoginGameMode 负责
// private void ShowLoginUI() { ... }
```

---

## 7. 实现完成与改进

### 7.1 GameModeType 枚举 - 优雅的模式切换

为了避免在代码中出现多个 `new GameMode()` 的调用，引入了 `GameModeType` 枚举，统一管理 GameMode 的创建。

```csharp
/// <summary>
/// 游戏模式类型枚举
/// </summary>
public enum GameModeType
{
    Login,          // 登录模式
    SinglePlayer,   // 单机模式
    Multiplayer     // 联机模式
}
```

#### GameDirector 的重载方法

```csharp
public class GameDirector : Singleton<GameDirector>
{
    // 原始方法 - 接受 IGameMode 实例
    public void SwitchGameMode(IGameMode newMode) { /* ... */ }
    
    // 新增重载 - 接受 GameModeType 枚举
    public void SwitchGameMode(GameModeType gameModeType)
    {
        IGameMode newGameMode = gameModeType switch
        {
            GameModeType.Login => new LoginGameMode(),
            GameModeType.SinglePlayer => new SinglePlayerGameMode(),
            GameModeType.Multiplayer => new MultiplayerGameMode(),
            _ => throw new System.ArgumentException($"Unknown GameMode type: {gameModeType}")
        };
        
        SwitchGameMode(newGameMode);
    }
}
```

#### 使用示例

```csharp
// 初始化时
GameDirector.Instance.SwitchGameMode(GameModeType.Login);

// LoginGameMode 中
GameDirector.Instance.SwitchGameMode(GameModeType.Multiplayer);
GameDirector.Instance.SwitchGameMode(GameModeType.SinglePlayer);

// GameMessageHandler 中
GameDirector.Instance.SwitchGameMode(GameModeType.Multiplayer);
```

#### 优点

- **优雅简洁**：一行代码完成模式切换
- **类型安全**：编译时检查，避免拼写错误
- **集中管理**：所有 GameMode 的创建逻辑在一处
- **易于扩展**：添加新的 GameMode 只需修改枚举和 switch 语句

---

### 7.2 MatchFoundNotification 的移除

经过确认，服务器快速匹配流程已更新：

**原流程**（已过时）：
```
QuickMatchRequest 
  → QuickMatchResponse 
  → MatchFoundNotification 
  → GameStartNotification
```

**新流程**（当前实现）：
```
QuickMatchRequest 
  → QuickMatchResponse 
  → [直接] GameStartNotification
```

#### 改动说明

**在 LoginGameMode 中**：
- ✅ 移除了 `MatchFoundNotification` 的订阅
- ✅ 移除了 `OnMatchFoundNotification` 事件处理器
- ✅ `SendQuickMatchRequest` 成功时设置状态为 `Matching`，然后等待 `GameStartNotification`

**流程**：
```csharp
// 1. 用户点击快速联机
OnConnectButtonClicked()
  → gameMode.SendQuickMatchRequest()

// 2. LoginGameMode 处理响应
OnQuickMatchResponse()
  → _matchState = MatchState.Matching
  → PublishLoginStateChanged()

// 3. 等待服务器发送 GameStartNotification

// 4. GameMessageHandler 处理通知
GameStartNotificationHandler
  → 检查当前 GameMode
    ├─ 如果是 LoginGameMode → 先切换到 MultiplayerGameMode
    └─ 调用 MultiplayerGameMode.OnGameStartNotification()
```

---

### 7.3 LoginView 状态更新完善

修复了 `OnLoginStateChanged` 未处理 `MatchState` 的问题：

```csharp
private void OnLoginStateChanged(LoginStateChangedEventData eventData)
{
    // 1. 根据 ConnectionState 更新 UI
    int connState = eventData.ConnectionState;
    if (connState == 0) // Disconnected
    {
        UpdateConnectionStatus("未连接");
        UpdateConnectButtonText("连接服务器");
        SetConnectButtonInteractable(true);
        isConnecting = false;
    }
    // ... 其他状态 ...
    
    // 2. 根据 MatchState 更新 UI（新增）
    int matchState = eventData.MatchState;
    if (matchState == 1) // Matching
    {
        currentMatchState = MatchState.Matching;
        UpdateConnectionStatus("正在匹配中...");
        UpdateConnectButtonText("取消匹配");
        SetConnectButtonInteractable(true);
    }
    else if (matchState == 2) // MatchFound
    {
        currentMatchState = MatchState.MatchFound;
        UpdateConnectionStatus("匹配成功！正在进入房间...");
        UpdateConnectButtonText("进入游戏");
        SetConnectButtonInteractable(false);
    }
}
```

#### 效果

- ✅ 快速联机后，UI 立即显示"正在匹配中..."
- ✅ 匹配成功后，UI 显示"匹配成功！正在进入房间..."
- ✅ 按钮文本和交互状态正确更新

---

### 7.4 GameMessageHandler 的完善

处理从 `LoginGameMode` 到 `MultiplayerGameMode` 的过渡：

```csharp
public class GameStartNotificationHandler : MessageHandlerBase<GameStartNotification>
{
    public override async Task HandleMessageAsync(GameStartNotification message)
    {
        var currentGameMode = GameDirector.Instance?.CurrentGameMode;
        
        // 情况1：已经在联机模式中
        if (currentGameMode is MultiplayerGameMode multiplayerMode)
        {
            multiplayerMode.OnGameStartNotification(message);
        }
        // 情况2：还在登录模式中，需要先切换
        else if (currentGameMode is LoginGameMode loginMode)
        {
            // 使用新的 GameModeType 方式切换
            GameDirector.Instance.SwitchGameMode(GameModeType.Multiplayer);
            
            // 切换完成后处理通知
            var newMultiplayerMode = GameDirector.Instance.CurrentGameMode as MultiplayerGameMode;
            if (newMultiplayerMode != null)
            {
                newMultiplayerMode.OnGameStartNotification(message);
            }
        }
    }
}
```

---

### 7.5 架构设计改进总结

| 维度 | 改进 | 效果 |
|------|------|------|
| **模式创建** | 引入 GameModeType 枚举 | 减少代码重复，提高可维护性 |
| **网络流程** | 移除 MatchFoundNotification | 与服务器实现同步，简化流程 |
| **UI 状态** | 完善 MatchState 处理 | 用户体验改进，状态显示准确 |
| **模式切换** | 集中处理切换逻辑 | GameMessageHandler 统一管理 |
| **代码质量** | 优化调用方式 | 代码更简洁、优雅、易维护 |

---

## 总结

`LoginGameMode` 的实现遵循了以下核心原则：

1. ✅ **关注点分离**：业务逻辑与 UI 完全分离
2. ✅ **单一职责**：LoginGameMode 专注于登录和匹配
3. ✅ **事件驱动**：通过 EventSystem 实现模块间通信
4. ✅ **优雅设计**：使用枚举和重载方法简化 API
5. ✅ **与服务器同步**：实现与服务器逻辑完全对齐

该架构为后续的游戏模式扩展和功能迭代提供了坚实的基础。

---

## 8. 风险评估

### 8.1 高风险项

1. **事件订阅时机问题**
   - 风险：LoginView 可能在 LoginGameMode 发布事件前订阅
   - 缓解：确保初始化顺序正确，或使用状态查询

2. **状态同步问题**
   - 风险：UI 状态与 LoginGameMode 状态不一致
   - 缓解：使用事件驱动，避免直接状态查询

### 8.2 中风险项

1. **现有代码兼容性**
   - 风险：重构可能影响现有功能
   - 缓解：渐进式迁移，保持接口兼容

2. **测试覆盖不足**
   - 风险：重构后可能引入新 bug
   - 缓解：充分的单元测试和集成测试

### 8.3 低风险项

1. **性能影响**
   - 风险：事件系统可能有性能开销
   - 缓解：登录流程不频繁，性能影响可忽略

---

## 9. 成功标准

### 9.1 功能标准

- ✅ 登录流程完全正常工作
- ✅ 匹配流程完全正常工作
- ✅ 单机游戏启动正常
- ✅ 联机游戏启动正常
- ✅ UI 响应及时，无卡顿

### 9.2 架构标准

- ✅ LoginView 只包含 UI 代码
- ✅ LoginGameMode 包含所有业务逻辑
- ✅ 事件驱动架构清晰
- ✅ 状态管理规范
- ✅ 代码可维护性高

### 9.3 质量标准

- ✅ 无编译错误
- ✅ 无运行时错误
- ✅ 无内存泄漏
- ✅ 代码审查通过
- ✅ 单元测试覆盖率 > 80%

---

## 10. 总结

### 10.1 设计优势

1. **职责清晰**：LoginGameMode 管理业务逻辑，LoginView 管理 UI
2. **易于测试**：业务逻辑与 UI 分离，可以独立测试
3. **易于扩展**：新增登录方式（如第三方登录）只需修改 LoginGameMode
4. **状态驱动**：基于 GameModeState 和内部状态的清晰流程控制
5. **事件驱动**：组件间通过事件通信，低耦合

### 10.2 与现有架构的一致性

- 继承 BaseGameMode，符合 GameMode 扩展计划
- 使用 EventSystem 进行事件通信，符合现有事件架构
- 集成到 GameDirector，符合游戏导演架构设计
- 状态管理符合 GameModeState 设计

### 10.3 后续扩展方向

1. **多种登录方式**：账号密码、第三方登录、游客登录
2. **登录配置**：服务器地址配置、自动登录、记住密码
3. **登录动画**：登录过程的动画效果
4. **错误处理增强**：更详细的错误提示和重试机制
5. **登录统计**：登录成功率、登录时长等数据统计

---

## 11. 参考文档

- [GameMode 系统扩展计划](./GameMode-Extension-Plan%20GameMode扩展计划.md)
- [GameDirector 游戏导演架构设计](./GameDirector-Architecture-Design%20游戏导演架构设计.md)
- [GameDirector 重构实施计划](./Refactoring-Implementation-Plan%20重构实施计划.md)
- [快速联机系统设计](../../01-GameDesign%20游戏设计/系统/联机/Quick-Match-System%20快速联机系统.md)

