# AstrumClient 程序集

AstrumClient 是客户端的主控制和入口程序集，负责管理客户端状态、网络连接和游戏逻辑。

## 依赖关系

- **LogicCore**: 游戏逻辑核心
- **Network**: 网络通信
- **CommonBase**: 基础通用功能

## 主要组件

### ClientManager

客户端主控制器，负责管理客户端状态、网络连接和游戏逻辑。

#### 主要功能

- **状态管理**: 管理客户端连接状态（未连接、连接中、已连接、认证中、已认证、游戏中等）
- **网络连接**: 处理与服务器的连接、断开和重连
- **玩家认证**: 处理玩家身份验证
- **消息处理**: 处理来自服务器的各种消息
- **游戏逻辑**: 与LogicCore集成，处理游戏状态和玩家数据

#### 使用示例

```csharp
// 获取客户端管理器实例
var clientManager = ClientManager.Instance;

// 连接到服务器
bool connected = await clientManager.ConnectToServerAsync();

// 认证玩家
bool authenticated = await clientManager.AuthenticateAsync("PlayerName");

// 请求加入游戏
bool joined = await clientManager.RequestJoinGameAsync();

// 发送玩家输入
var input = new PlayerInput
{
    Horizontal = 1.0f,
    Vertical = 0.0f,
    Jump = true
};
await clientManager.SendPlayerInputAsync(input);
```

### ClientController

Unity MonoBehaviour组件，作为客户端在Unity中的集成入口。

#### 主要功能

- **Unity集成**: 提供Unity友好的接口
- **自动管理**: 自动处理连接、认证和游戏状态
- **调试支持**: 提供调试信息和GUI显示
- **配置管理**: 通过Inspector配置服务器设置

#### 使用示例

1. **在场景中添加组件**:
   ```csharp
   // 在GameObject上添加ClientController组件
   var controller = gameObject.AddComponent<ClientController>();
   ```

2. **配置设置**:
   - 在Inspector中设置服务器地址和端口
   - 设置玩家名称
   - 配置自动连接选项

3. **编程接口**:
   ```csharp
   var controller = FindObjectOfType<ClientController>();
   
   // 连接到服务器
   controller.ConnectToServer();
   
   // 认证玩家
   await controller.AuthenticatePlayer();
   
   // 加入游戏
   await controller.JoinGame();
   
   // 发送输入
   var input = new PlayerInput { Horizontal = 1.0f };
   controller.SendPlayerInput(input);
   ```

## 客户端状态

```csharp
public enum ClientState
{
    Disconnected,    // 未连接
    Connecting,      // 连接中
    Connected,       // 已连接
    Authenticating,  // 认证中
    Authenticated,   // 已认证
    InGame,         // 游戏中
    Disconnecting    // 断开连接中
}
```

## 事件系统

ClientManager提供丰富的事件系统，用于监听客户端状态变化：

```csharp
// 状态变更事件
clientManager.OnStateChanged += (oldState, newState) => {
    Debug.Log($"状态变更: {oldState} -> {newState}");
};

// 连接事件
clientManager.OnConnected += () => Debug.Log("已连接");
clientManager.OnDisconnected += () => Debug.Log("已断开");

// 认证事件
clientManager.OnAuthenticated += () => Debug.Log("认证成功");

// 游戏事件
clientManager.OnEnteredGame += () => Debug.Log("进入游戏");
```

## 玩家输入

PlayerInput类定义了玩家可以发送的输入数据：

```csharp
public class PlayerInput
{
    public float Horizontal { get; set; }  // 水平移动
    public float Vertical { get; set; }    // 垂直移动
    public bool Jump { get; set; }         // 跳跃
    public bool Attack { get; set; }       // 攻击
    public bool Interact { get; set; }     // 交互
    public float MouseX { get; set; }      // 鼠标X轴
    public float MouseY { get; set; }      // 鼠标Y轴
}
```

## 网络消息处理

ClientManager自动处理来自服务器的各种消息：

- **AuthResult**: 认证结果
- **GameState**: 游戏状态更新
- **PlayerData**: 玩家数据更新
- **GameStart**: 游戏开始
- **GameEnd**: 游戏结束

## 最佳实践

1. **单例模式**: ClientManager使用单例模式，确保全局只有一个实例
2. **异步操作**: 所有网络操作都是异步的，避免阻塞主线程
3. **错误处理**: 所有操作都包含适当的错误处理
4. **状态检查**: 在发送消息前检查客户端状态
5. **事件驱动**: 使用事件系统而不是轮询来响应状态变化

## 调试

ClientController提供调试功能：

- **Inspector配置**: 在Unity Inspector中配置所有设置
- **GUI调试**: 在游戏运行时显示调试信息
- **日志输出**: 详细的日志记录
- **状态监控**: 实时监控客户端状态

## 注意事项

1. **初始化顺序**: 确保在使用ClientManager前已正确初始化
2. **网络超时**: 网络操作可能超时，需要适当的重试机制
3. **内存管理**: 及时取消事件订阅，避免内存泄漏
4. **线程安全**: 网络回调可能在后台线程，注意Unity API的线程限制 