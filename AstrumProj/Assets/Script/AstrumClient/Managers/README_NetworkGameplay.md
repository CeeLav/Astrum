# GamePlayManager 网络游戏接口使用说明

## 概述

`GamePlayManager` 现在提供了完整的网络游戏接口，支持用户登录、房间管理等功能。这些接口与我们的测试服务器完全兼容。

## 主要功能

### 1. 用户管理
- **自动登录**: `AutoLogin()` - 使用默认显示名称自动登录
- **指定名称登录**: `Login(string displayName)` - 使用指定显示名称登录
- **登出**: `Logout()` - 用户登出

### 2. 房间管理
- **创建房间**: `CreateRoom(string roomName, int maxPlayers)` - 创建新房间
- **加入房间**: `JoinRoom(string roomId)` - 加入指定房间
- **离开房间**: `LeaveRoom()` - 离开当前房间
- **获取房间列表**: `GetRooms()` - 获取可用房间列表

### 3. 用户列表
- **获取在线用户**: `GetOnlineUsers()` - 获取在线用户列表

## 状态属性

```csharp
// 用户状态
public bool IsLoggedIn => CurrentUser != null;
public UserInfo CurrentUser { get; private set; }

// 房间状态
public bool IsInRoom => CurrentUser?.CurrentRoomId != null;

// 数据列表
public List<RoomInfo> AvailableRooms { get; private set; }
public List<UserInfo> OnlineUsers { get; private set; }
```

## 事件系统

GamePlayManager 提供了完整的事件系统，用于响应网络状态变化：

```csharp
// 用户事件
public event Action<UserInfo> OnUserLoggedIn;
public event Action OnUserLoggedOut;

// 房间事件
public event Action<RoomInfo> OnRoomCreated;
public event Action<RoomInfo> OnRoomJoined;
public event Action OnRoomLeft;

// 列表更新事件
public event Action<List<RoomInfo>> OnRoomListUpdated;
public event Action<List<UserInfo>> OnOnlineUsersUpdated;

// 错误事件
public event Action<string> OnNetworkError;
```

## 数据结构

### UserInfo (用户信息)
```csharp
public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime LastLoginAt { get; set; }
    public string CurrentRoomId { get; set; }
}
```

### RoomInfo (房间信息)
```csharp
public class RoomInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CreatorName { get; set; } = string.Empty;
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> PlayerNames { get; set; } = new List<string>();
}
```

## 使用示例

### 基本使用流程

```csharp
public class GameController : MonoBehaviour
{
    private GamePlayManager gamePlayManager;
    
    void Start()
    {
        gamePlayManager = GamePlayManager.Instance;
        
        // 注册事件
        gamePlayManager.OnUserLoggedIn += OnUserLoggedIn;
        gamePlayManager.OnRoomCreated += OnRoomCreated;
        gamePlayManager.OnNetworkError += OnNetworkError;
        
        // 自动登录
        gamePlayManager.AutoLogin();
    }
    
    private void OnUserLoggedIn(UserInfo userInfo)
    {
        Debug.Log($"用户登录成功: {userInfo.DisplayName}");
        
        // 登录成功后创建房间
        gamePlayManager.CreateRoom("我的房间", 4);
    }
    
    private void OnRoomCreated(RoomInfo roomInfo)
    {
        Debug.Log($"房间创建成功: {roomInfo.Name}");
    }
    
    private void OnNetworkError(string error)
    {
        Debug.LogError($"网络错误: {error}");
    }
}
```

### UI 集成示例

```csharp
public class NetworkGameUI : MonoBehaviour
{
    [SerializeField] private Button loginButton;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Text statusText;
    
    private GamePlayManager gamePlayManager;
    
    void Start()
    {
        gamePlayManager = GamePlayManager.Instance;
        
        // 设置按钮事件
        loginButton.onClick.AddListener(() => gamePlayManager.AutoLogin());
        createRoomButton.onClick.AddListener(() => gamePlayManager.CreateRoom());
        
        // 注册状态更新事件
        gamePlayManager.OnUserLoggedIn += UpdateUI;
        gamePlayManager.OnUserLoggedOut += UpdateUI;
        gamePlayManager.OnRoomCreated += UpdateUI;
        gamePlayManager.OnRoomLeft += UpdateUI;
    }
    
    private void UpdateUI()
    {
        string status = $"登录: {(gamePlayManager.IsLoggedIn ? "是" : "否")}\n";
        status += $"房间: {(gamePlayManager.IsInRoom ? "是" : "否")}";
        statusText.text = status;
        
        // 根据状态启用/禁用按钮
        createRoomButton.interactable = gamePlayManager.IsLoggedIn && !gamePlayManager.IsInRoom;
    }
}
```

## 注意事项

1. **网络连接**: 使用网络功能前，确保 `NetworkManager` 已连接
2. **状态检查**: 接口会自动检查用户登录状态和房间状态
3. **错误处理**: 所有网络错误都会通过 `OnNetworkError` 事件通知
4. **线程安全**: 所有事件都在主线程中触发，可以直接更新UI

## 与服务器的兼容性

这些接口与我们的测试服务器完全兼容，支持以下消息类型：
- `login` - 用户登录
- `create_room` - 创建房间
- `join_room` - 加入房间
- `leave_room` - 离开房间
- `get_rooms` - 获取房间列表
- `get_online_users` - 获取在线用户

## 测试

可以使用 `NetworkGameTest` 脚本来测试所有网络功能。该脚本提供了完整的UI界面来测试各种网络操作。 