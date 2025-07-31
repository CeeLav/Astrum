# Astrum 网络通讯和房间系统

本项目实现了完整的客户端与服务器通讯和开房间的逻辑，包括用户认证、房间管理等功能。

## 功能特性

### 服务器端功能
- **用户管理**: 用户注册、登录、认证
- **房间管理**: 创建房间、加入房间、离开房间、房间列表
- **实时通讯**: 基于TCP Socket的JSON消息通讯
- **数据持久化**: 用户数据自动保存到本地文件

### 客户端功能
- **网络连接**: 自动连接到服务器
- **用户界面**: 交互式控制台界面
- **消息处理**: 自动处理服务器响应和推送消息
- **错误处理**: 完善的错误处理和重连机制

## 项目结构

```
Astrum/
├── AstrumServer/                 # 服务器端
│   └── AstrumServer/
│       ├── Models/               # 数据模型
│       │   ├── User.cs          # 用户模型
│       │   ├── Room.cs          # 房间模型
│       │   └── NetworkMessage.cs # 网络消息模型
│       ├── Managers/            # 管理器
│       │   ├── UserManager.cs   # 用户管理器
│       │   └── RoomManager.cs   # 房间管理器
│       └── Program.cs           # 主程序
└── AstrumLogic/                 # 客户端逻辑
    └── NetworkTest/             # 网络测试客户端
        ├── Models/              # 客户端数据模型
        ├── NetworkClient.cs     # 网络客户端
        └── Program.cs           # 客户端主程序
```

## 快速开始

### 1. 启动服务器

```bash
cd AstrumServer/AstrumServer
dotnet run
```

服务器将在端口 8888 上启动，并显示启动日志。

### 2. 运行客户端

```bash
cd AstrumLogic/NetworkTest
dotnet run
```

客户端将自动连接到服务器，并显示主菜单。

### 3. 测试流程

1. **注册用户**: 选择选项1，输入用户名和密码
2. **登录**: 选择选项2，使用注册的用户名和密码登录
3. **创建房间**: 选择选项3，输入房间名称和最大玩家数
4. **查看房间**: 选择选项4，查看所有可用房间
5. **加入房间**: 选择选项5，输入房间ID加入房间
6. **查看在线用户**: 选择选项7，查看当前在线用户

## 消息协议

### 客户端发送消息格式

```json
{
  "type": "消息类型",
  "data": {
    // 消息数据
  }
}
```

### 服务器响应格式

```json
{
  "type": "消息类型",
  "data": {
    // 响应数据
  },
  "success": true,
  "error": null,
  "timestamp": "2024-01-01T00:00:00"
}
```

### 支持的消息类型

| 类型 | 描述 | 请求数据 | 响应数据 |
|------|------|----------|----------|
| `register` | 用户注册 | `{username, password}` | `{message}` |
| `login` | 用户登录 | `{username, password}` | `UserInfo` |
| `create_room` | 创建房间 | `{roomName, maxPlayers}` | `RoomInfo` |
| `join_room` | 加入房间 | `{roomId}` | `RoomInfo` |
| `leave_room` | 离开房间 | `{roomId}` | `{message}` |
| `get_rooms` | 获取房间列表 | `null` | `RoomInfo[]` |
| `get_online_users` | 获取在线用户 | `null` | `UserInfo[]` |
| `ping` | 心跳检测 | `null` | `{timestamp}` |

## 数据模型

### User (用户)
```csharp
public class User
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public bool IsOnline { get; set; }
    public string? CurrentRoomId { get; set; }
}
```

### Room (房间)
```csharp
public class Room
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string CreatorId { get; set; }
    public int MaxPlayers { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public List<string> PlayerIds { get; set; }
}
```

## 扩展功能

### 添加新的消息类型

1. 在 `Models/NetworkMessage.cs` 中添加新的请求/响应类
2. 在服务器端 `Program.cs` 中添加新的处理方法
3. 在客户端 `NetworkClient.cs` 中添加新的发送方法
4. 在客户端 `Program.cs` 中添加新的处理逻辑

### 添加新的管理器

1. 创建新的管理器类（如 `GameManager`）
2. 在服务器端 `Program.cs` 中集成新管理器
3. 添加相应的消息处理逻辑

## 注意事项

1. **数据持久化**: 用户数据保存在 `users.json` 文件中
2. **连接管理**: 服务器自动管理客户端连接和断开
3. **错误处理**: 所有网络操作都有完善的错误处理
4. **线程安全**: 使用锁机制确保多线程环境下的数据安全

## 故障排除

### 常见问题

1. **连接失败**: 检查服务器是否启动，端口是否正确
2. **消息解析失败**: 检查JSON格式是否正确
3. **用户认证失败**: 检查用户名和密码是否正确
4. **房间操作失败**: 检查房间ID是否存在，用户是否已登录

### 日志查看

服务器端会输出详细的日志信息，包括：
- 客户端连接/断开
- 用户登录/注册
- 房间创建/加入/离开
- 错误信息

客户端会显示所有发送和接收的消息，便于调试。 