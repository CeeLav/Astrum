# Network 程序集

## 概述

Network是Astrum游戏的网络通信程序集，**与Unity完全解耦**，提供完整的网络消息系统和连接管理功能。

## 设计理念

- **平台无关**: 不依赖Unity引擎，可在任何.NET环境中运行
- **消息驱动**: 基于消息的网络通信系统
- **事件驱动**: 使用事件系统进行状态通知
- **类型安全**: 强类型的消息系统
- **JSON序列化**: 支持跨平台的消息序列化

## 核心组件

### 1. NetworkMessage (网络消息基类)
- 所有网络消息的基类
- 支持JSON序列化和反序列化
- 包含消息ID、时间戳、发送者等元数据
- 支持消息类型枚举

### 2. NetworkManager (网络管理器)
- 单例模式的网络管理器
- 连接状态管理
- 消息发送和接收
- 事件系统集成

### 3. 消息类型
- **连接相关**: Connect, Disconnect, Heartbeat
- **通用数据**: Data (可携带任意类型数据)
- **系统相关**: Error, Info

## 文件结构

```
Network/
├── Network.asmdef          # Unity程序集定义文件
├── NetworkMessage.cs       # 网络消息系统
├── NetworkManager.cs       # 网络管理器
└── README.md              # 说明文档
```

## 使用方法

### 1. 初始化网络管理器

```csharp
using Astrum.Network;

// 获取网络管理器实例
var networkManager = NetworkManager.Instance;

// 初始化
networkManager.Initialize();

// 监听连接状态变化
networkManager.OnConnectionStateChanged += (previous, current) => {
    Console.WriteLine($"连接状态从 {previous} 变为 {current}");
};

// 监听消息接收
networkManager.OnMessageReceived += (message) => {
    Console.WriteLine($"收到消息: {message.Type}");
};
```

### 2. 连接和断开

```csharp
// 连接到服务器
bool connected = await networkManager.ConnectAsync("localhost", 8888);

if (connected)
{
    Console.WriteLine("连接成功！");
}

// 断开连接
await networkManager.DisconnectAsync("用户主动断开");
```

### 3. 发送消息

```csharp
// 发送通用数据消息
var dataMessage = new DataMessage
{
    DataType = "PlayerMove",
    Data = new { PlayerId = "player1", Position = new Vector3(1, 0, 0) }
};

await networkManager.SendMessageAsync(dataMessage);

// 发送自定义数据
var customData = new DataMessage
{
    DataType = "CustomEvent",
    Data = new { EventType = "Jump", PlayerId = "player1" }
};

await networkManager.SendMessageAsync(customData);
```

### 4. 消息序列化

```csharp
// 序列化消息
var message = new ConnectMessage
{
    PlayerName = "张三",
    Version = "1.0.0"
};

string json = message.ToJson();
Console.WriteLine(json);

// 反序列化消息
var deserializedMessage = NetworkMessage.FromJson<ConnectMessage>(json);
```

## 服务器端集成

在服务器端项目中引用Network程序集：

```csharp
// 在AstrumServer项目中
using Astrum.Network;

public class GameServer
{
    private NetworkManager _networkManager;
    
    public GameServer()
    {
        _networkManager = NetworkManager.Instance;
        _networkManager.Initialize();
        
        // 监听消息
        _networkManager.OnMessageReceived += HandleNetworkMessage;
    }
    
    private void HandleNetworkMessage(NetworkMessage message)
    {
        switch (message.Type)
        {
            case MessageType.Connect:
                HandleConnect((ConnectMessage)message);
                break;
            case MessageType.Data:
                HandleData((DataMessage)message);
                break;
            // ... 其他消息类型
        }
    }
    
    private void HandleData(DataMessage message)
    {
        // 根据DataType处理不同的数据
        switch (message.DataType)
        {
            case "PlayerMove":
                // 处理玩家移动
                break;
            case "PlayerJump":
                // 处理玩家跳跃
                break;
            // ... 其他数据类型
        }
    }
}
```

## 客户端集成

在Unity客户端中使用Network程序集：

```csharp
using Astrum.Network;

public class UnityNetworkController : MonoBehaviour
{
    private NetworkManager _networkManager;
    
    void Start()
    {
        _networkManager = NetworkManager.Instance;
        _networkManager.Initialize();
        
        // 监听连接状态
        _networkManager.OnConnectionStateChanged += OnConnectionStateChanged;
        
        // 监听消息
        _networkManager.OnMessageReceived += OnMessageReceived;
    }
    
    void Update()
    {
        // 更新网络管理器
        _networkManager.Update();
    }
    
    private async void ConnectToServer()
    {
        bool success = await _networkManager.ConnectAsync("localhost", 8888);
        if (success)
        {
            Debug.Log("连接成功！");
        }
    }
    
    private async void SendPlayerMove(Vector3 position, Vector3 velocity)
    {
        var dataMessage = new DataMessage
        {
            DataType = "PlayerMove",
            Data = new { 
                PlayerId = "localPlayer", 
                Position = new Vector3(position.X, position.Y, position.Z),
                Velocity = new Vector3(velocity.X, velocity.Y, velocity.Z)
            }
        };
        
        await _networkManager.SendMessageAsync(dataMessage);
    }
}
```

## 消息类型详解

### 连接消息
- **ConnectMessage**: 客户端连接请求
- **DisconnectMessage**: 断开连接通知
- **HeartbeatMessage**: 心跳保持连接

### 通用数据消息
- **DataMessage**: 通用数据消息，可携带任意类型数据
  - DataType: 数据类型标识，用于区分不同的数据
  - Data: 实际数据内容，可以是任意对象

### 系统消息
- **ErrorMessage**: 错误信息
- **InfoMessage**: 一般信息

## 优势

1. **类型安全**: 强类型的消息系统，编译时检查
2. **跨平台**: JSON序列化，支持任何平台
3. **事件驱动**: 异步事件处理，不阻塞主线程
4. **可扩展**: 易于添加新的消息类型
5. **解耦设计**: 与Unity和LogicCore解耦，可独立使用

## 注意事项

- Network程序集引用了LogicCore程序集
- 所有消息都支持JSON序列化
- 消息ID自动生成，确保唯一性
- 连接状态变化会触发事件
- 消息发送失败会触发错误事件

## 下一步开发

1. 实现实际的网络传输层
2. 添加消息压缩和加密
3. 实现可靠的消息传递
4. 添加网络延迟补偿
5. 实现断线重连机制 