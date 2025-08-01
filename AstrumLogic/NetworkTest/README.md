# Astrum 网络模块测试

## 概述

本项目包含了从Unity AstrumProj中抽取的网络模块，并进行了完整的测试验证。网络模块基于TCP协议，支持心跳检测、自动重连、消息队列等功能。

## 测试结果

✅ **连接测试成功**
- 成功连接到本地服务器 (127.0.0.1:8888)
- 心跳机制正常工作（每5秒发送一次）
- 服务器正常响应pong消息

✅ **用户功能测试成功**
- 用户登录功能正常
- 房间创建、加入、离开功能正常
- 在线用户查询功能正常

✅ **消息处理测试成功**
- JSON消息序列化/反序列化正常
- 消息队列处理机制正常
- 事件驱动架构工作正常

## 项目结构

```
AstrumLogic/NetworkTest/
├── Program.cs              # 主程序入口，支持选择测试模式
├── NetworkManager.cs       # 网络管理器（核心组件）
├── NetworkManagerTest.cs   # NetworkManager测试程序
├── NetworkClient.cs        # 网络客户端实现
├── Models/
│   └── NetworkMessage.cs   # 网络消息模型
└── README.md              # 本文档
```

## 核心组件

### NetworkManager
网络管理器是核心组件，负责：
- 管理网络连接状态
- 处理消息队列
- 自动心跳检测
- 自动重连机制
- 事件驱动架构

### NetworkClient
底层网络客户端，负责：
- TCP连接管理
- 消息发送/接收
- 异步消息处理

## 使用方法

### 1. 运行测试

```bash
cd AstrumLogic/NetworkTest
dotnet run
```

选择测试模式：
- 1: 原始NetworkClient测试
- 2: NetworkManager测试（推荐）

### 2. 在Unity中使用

将更新后的 `NetworkManager.cs` 复制到Unity项目中，然后：

```csharp
// 获取NetworkManager实例
var networkManager = NetworkManager.Instance;

// 注册事件
networkManager.OnConnected += OnConnected;
networkManager.OnDisconnected += OnDisconnected;
networkManager.OnMessageReceived += OnMessageReceived;

// 注册消息处理器
networkManager.RegisterHandler("login", OnLoginResponse);
networkManager.RegisterHandler("create_room", OnCreateRoomResponse);

// 连接到服务器
networkManager.Connect("127.0.0.1", 8888);

// 发送消息
var message = new HeartbeatMessage { ClientId = "test" };
networkManager.SendMessage(message);
```

## 主要特性

### 1. 自动心跳检测
- 每5秒自动发送心跳消息
- 检测连接状态
- 支持自定义心跳间隔

### 2. 自动重连机制
- 连接断开时自动重连
- 可配置重连次数和间隔
- 重连状态事件通知

### 3. 消息队列处理
- 线程安全的消息队列
- 异步消息处理
- 支持消息类型注册

### 4. 事件驱动架构
- 连接状态变化事件
- 消息接收事件
- 错误处理事件

## 消息协议

网络消息使用JSON格式，基本结构：

```json
{
  "type": "message_type",
  "data": {},
  "error": null,
  "success": true,
  "timestamp": "2025-08-01T11:27:28.1228615+08:00"
}
```

### 支持的消息类型

- `login`: 用户登录
- `create_room`: 创建房间
- `join_room`: 加入房间
- `leave_room`: 离开房间
- `get_rooms`: 获取房间列表
- `get_online_users`: 获取在线用户
- `ping/pong`: 心跳消息

## 配置参数

```csharp
// 网络设置
private string defaultServerAddress = "127.0.0.1";
private int defaultPort = 8888;
private int heartbeatInterval = 5000; // 5秒
private int reconnectInterval = 3000; // 3秒
private int maxReconnectAttempts = 5;
```

## 注意事项

1. **服务器要求**: 需要运行AstrumServer项目
2. **端口配置**: 确保服务器端口8888可用
3. **防火墙**: 确保防火墙允许TCP连接
4. **Unity版本**: 建议使用Unity 2022.3或更高版本

## 故障排除

### 连接失败
- 检查服务器是否运行
- 检查端口是否正确
- 检查防火墙设置

### 消息发送失败
- 检查网络连接状态
- 检查消息格式是否正确
- 查看控制台错误日志

### 心跳异常
- 检查网络稳定性
- 检查服务器响应
- 调整心跳间隔

## 更新日志

### v1.0.0 (2025-08-01)
- ✅ 完成网络模块抽取
- ✅ 实现完整的测试验证
- ✅ 支持心跳检测和自动重连
- ✅ 实现消息队列和事件驱动
- ✅ 更新Unity版本NetworkManager

## 贡献

欢迎提交Issue和Pull Request来改进这个网络模块。

## 许可证

本项目遵循MIT许可证。 