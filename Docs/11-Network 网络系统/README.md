# 🌐 网络系统架构文档

**版本**: v1.0.0  
**状态**: ✅ 基础完成，🚧 持续优化  
**最后更新**: 2025-10-31

---

## 📋 目录

1. [概述](#概述)
2. [架构设计](#架构设计)
3. [核心组件](#核心组件)
4. [消息系统](#消息系统)
5. [连接流程](#连接流程)
6. [协议定义](#协议定义)
7. [消息处理器系统](#消息处理器系统)
8. [帧同步机制](#帧同步机制)
9. [性能优化](#性能优化)
10. [开发指南](#开发指南)

---

## 概述

Astrum 游戏采用**客户端-服务器（C/S）架构**，使用 **TCP/IP** 协议进行可靠通信。网络系统设计遵循以下原则：

- **平台无关性**: 客户端网络层（Network程序集）与Unity解耦，可在任何.NET环境运行
- **消息驱动**: 基于消息的网络通信，支持类型安全的消息处理
- **可扩展性**: 灵活的消息处理器系统，易于添加新的消息类型和处理逻辑
- **高性能**: 使用MemoryPack序列化，支持大量并发连接

---

## 架构设计

### 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                    客户端 (Unity Client)                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ LoginView    │  │ GameDirector │  │ GameMode     │      │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │
│         │                  │                  │              │
│         └──────────────────┼──────────────────┘              │
│                            │                                  │
│                  ┌─────────▼─────────┐                       │
│                  │  NetworkManager   │                       │
│                  │  (客户端管理器)    │                       │
│                  └─────────┬─────────┘                       │
│                            │                                  │
│                  ┌─────────▼─────────┐                       │
│                  │ MessageHandler    │                       │
│                  │ Registry          │                       │
│                  └─────────┬─────────┘                       │
│                            │                                  │
│         ┌─────────────────┼─────────────────┐              │
│         │                  │                  │              │
│  ┌──────▼──────┐  ┌────────▼────────┐  ┌────▼──────┐        │
│  │ ConnectMsg  │  │ LoginMessage   │  │ GameMsg   │        │
│  │ Handler     │  │ Handler         │  │ Handler  │        │
│  └─────────────┘  └─────────────────┘  └──────────┘        │
│                            │                                  │
│                  ┌─────────▼─────────┐                       │
│                  │ Session (会话)    │                       │
│                  │ TService (TCP)    │                       │
│                  └─────────┬─────────┘                       │
└────────────────────────────┼─────────────────────────────────┘
                             │
                    ┌────────▼────────┐
                    │   TCP/IP网络    │
                    └────────┬────────┘
                             │
┌────────────────────────────┼─────────────────────────────────┐
│                    服务器 (.NET Server)                       │
├────────────────────────────┼─────────────────────────────────┤
│                            │                                  │
│                  ┌─────────▼─────────┐                       │
│                  │ TService (TCP)    │                       │
│                  │ ServerNetworkMgr  │                       │
│                  └─────────┬─────────┘                       │
│                            │                                  │
│                  ┌─────────▼─────────┐                       │
│                  │    GameServer     │                       │
│                  │  (消息分发器)     │                       │
│                  └─────────┬─────────┘                       │
│                            │                                  │
│         ┌─────────────────┼─────────────────┐              │
│         │                  │                  │              │
│  ┌──────▼──────┐  ┌────────▼────────┐  ┌────▼──────┐        │
│  │UserManager  │  │ RoomManager     │  │Matchmaking│        │
│  └─────────────┘  └─────────────────┘  └───────────┘        │
└───────────────────────────────────────────────────────────────┘
```

### 分层架构

```
┌─────────────────────────────────────────────┐
│           应用层 (Application Layer)           │
│  - GameDirector, GameMode, UIManager         │
└─────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│           业务层 (Business Layer)            │
│  - UserManager, RoomSystemManager           │
│  - MessageHandlers (消息处理器)              │
└─────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│           网络层 (Network Layer)             │
│  - NetworkManager, Session, TService        │
│  - MessageHandlerRegistry, Dispatcher        │
└─────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────┐
│           传输层 (Transport Layer)           │
│  - TCP/IP Socket                            │
└─────────────────────────────────────────────┘
```

---

## 核心组件

### 客户端核心组件

#### 1. NetworkManager (客户端网络管理器)

**位置**: `AstrumProj/Assets/Script/AstrumClient/Managers/NetworkManager.cs`

**职责**:
- 管理TCP连接
- 消息发送和接收
- 连接状态管理
- 集成消息处理器系统

**关键方法**:
```csharp
public void Initialize()                          // 初始化网络管理器
public Task<long> ConnectAsync(string, int)      // 连接到服务器
public void Send(MessageObject message)          // 发送消息
public bool IsConnected()                        // 检查连接状态
```

#### 2. Session (网络会话)

**位置**: `AstrumProj/Assets/Script/Network/Session.cs`

**职责**:
- 管理单个TCP连接
- 消息序列化/反序列化
- 底层网络IO操作

#### 3. TService (TCP服务)

**位置**: `AstrumProj/Assets/Script/Network/TService.cs`

**职责**:
- TCP Socket封装
- 网络IO事件处理
- 缓冲区管理

### 服务器核心组件

#### 1. ServerNetworkManager (服务器网络管理器)

**位置**: `AstrumServer/AstrumServer/Network/ServerNetworkManager.cs`

**职责**:
- 监听客户端连接
- 管理多个Session
- 消息分发

**关键方法**:
```csharp
public Task<bool> InitializeAsync(int port)     // 初始化服务器
public void SendMessage(string sessionId, ...)  // 发送消息到指定客户端
public void Update()                            // 更新网络服务
```

#### 2. GameServer (游戏服务器主类)

**位置**: `AstrumServer/AstrumServer/Core/GameServer.cs`

**职责**:
- 处理所有客户端消息
- 协调各个管理器（UserManager, RoomManager等）
- 服务器主循环

#### 3. 管理器系统

- **UserManager**: 用户登录、会话管理
- **RoomManager**: 房间创建、加入、离开
- **MatchmakingManager**: 快速匹配队列管理
- **FrameSyncManager**: 帧同步管理

---

## 消息系统

### 消息定义

所有网络消息都继承自 `MessageObject`，通过 Protocol Buffers 定义协议文件，使用工具自动生成C#代码。

**协议文件位置**: `AstrumConfig/Proto/`

**主要协议文件**:
- `networkcommon_C_1000.proto` - 网络通用消息（连接、心跳等）
- `gamemessages_C_2000.proto` - 游戏消息（输入、帧同步等）
- `game_S_3000.proto` - 服务器游戏消息
- `connectionstatus_C_4000.proto` - 连接状态消息

### 消息格式

```
┌─────────────────────────────────────────┐
│  消息头 (4 bytes)                        │
├─────────────────────────────────────────┤
│  Length (2 bytes)    | 消息体长度        │
│  OpCode (2 bytes)    | 消息类型码        │
└─────────────────────────────────────────┘
┌─────────────────────────────────────────┐
│  消息体 (N bytes)                        │
├─────────────────────────────────────────┤
│  MemoryPack序列化的消息数据               │
└─────────────────────────────────────────┘
```

### 消息序列化

- **序列化库**: MemoryPack (高性能二进制序列化)
- **协议定义**: Protocol Buffers (.proto文件)
- **代码生成**: 通过Proto2CS工具自动生成

### OpCode管理

**位置**: `AstrumProj/Assets/Script/Network/OpcodeType.cs`

所有消息类型都通过OpCode注册，支持消息类型到OpCode的双向映射。

---

## 连接流程

### 客户端连接流程

```
1. 用户点击"连接"按钮
   ↓
2. LoginView.ConnectToServer()
   ↓
3. NetworkManager.ConnectAsync(serverAddress, port)
   ↓
4. TService建立TCP连接
   ↓
5. 客户端发送 ConnectRequest
   ↓
6. 服务器处理 ConnectRequest
   ↓
7. 服务器发送 ConnectResponse
   ↓
8. ConnectMessageHandler处理响应
   ↓
9. 发布 ConnectResponseEventData 事件
   ↓
10. LoginView订阅事件，更新UI状态
   ↓
11. 自动发送 LoginRequest
   ↓
12. 登录成功后进入游戏
```

### 服务器连接处理流程

```
1. TService接受新的TCP连接
   ↓
2. 创建Session对象
   ↓
3. 触发 OnClientConnected 事件
   ↓
4. GameServer.OnClientConnected()
   ↓
5. 发送 ConnectResponse (可选，或等待客户端ConnectRequest)
   ↓
6. 接收客户端消息
   ↓
7. GameServer.OnMessageReceived()
   ↓
8. 根据消息类型路由到对应处理方法
   ↓
9. 执行业务逻辑
   ↓
10. 发送响应消息
```

---

## 消息处理器系统

### 设计理念

基于ET框架的消息处理器系统，实现了灵活、可扩展的消息处理架构。

### 核心组件

#### 1. MessageHandlerAttribute (处理器标记)

```csharp
[MessageHandler(typeof(LoginResponse))]
public class LoginMessageHandler : MessageHandlerBase<LoginResponse>
{
    public override async Task HandleMessageAsync(LoginResponse message)
    {
        // 处理逻辑
    }
}
```

#### 2. MessageHandlerBase<T> (处理器基类)

提供统一的处理器接口和基础功能。

#### 3. MessageHandlerRegistry (处理器注册器)

**位置**: `AstrumProj/Assets/Script/AstrumClient/MessageHandlers/MessageHandlerRegistry.cs`

**职责**:
- 自动扫描所有带有`[MessageHandler]`特性的类
- 创建处理器实例
- 注册到MessageHandlerDispatcher

#### 4. MessageHandlerDispatcher (消息分发器)

**位置**: `AstrumProj/Assets/Script/Network/MessageHandlers/MessageHandlerDispatcher.cs`

**职责**:
- 根据消息类型路由到对应的处理器
- 支持多个处理器处理同一消息类型
- 支持处理器优先级

### 处理器列表

当前已实现的处理器：

| 处理器 | 消息类型 | 说明 |
|--------|---------|------|
| ConnectMessageHandler | ConnectResponse | 处理连接响应 |
| LoginMessageHandler | LoginResponse | 处理登录响应 |
| RoomMessageHandler | CreateRoomResponse, JoinRoomResponse等 | 处理房间相关消息 |
| GameMessageHandler | GameStartNotification, GameEndNotification等 | 处理游戏流程消息 |
| FrameSyncMessageHandler | FrameSyncStartNotification等 | 处理帧同步消息 |
| MatchMessageHandler | QuickMatchResponse等 | 处理匹配消息 |
| HeartbeatMessageHandler | HeartbeatResponse | 处理心跳响应 |

### 消息处理流程

```
客户端接收消息:
    ↓
NetworkManager.OnTcpRead()
    ↓
Session.OnRead()
    ↓
反序列化为MessageObject
    ↓
MessageHandlerDispatcher.DispatchAsync()
    ↓
根据消息类型查找处理器
    ↓
执行对应的HandleMessageAsync()
    ↓
业务逻辑处理
    ↓
可选: 发布事件到EventSystem
```

---

## 帧同步机制

### 帧同步概述

Astrum使用**服务器权威帧同步**机制，确保所有客户端游戏状态一致。

### 帧同步流程

```
客户端:
  1. 收集玩家输入
  2. 发送SingleInput到服务器
  3. 本地预测执行
  4. 接收服务器广播的OneFrameInputs
  5. 验证和纠正状态
  6. 显示结果

服务器:
  1. 收集所有客户端输入
  2. 等待固定时间（例如20ms）
  3. 执行确定性逻辑
  4. 广播OneFrameInputs到所有客户端
  5. 推进游戏帧
```

### 帧同步消息

- **SingleInput** (C→S): 单个玩家的输入
- **OneFrameInputs** (S→C): 一帧的所有玩家输入（广播）
- **FrameSyncStartNotification** (S→C): 帧同步开始通知
- **FrameSyncEndNotification** (S→C): 帧同步结束通知

---

## 性能优化

### 序列化优化

- 使用MemoryPack而非JSON，性能提升10-100倍
- 消息体压缩（未来计划）

### 连接管理

- Session池化管理
- 心跳机制保持连接活跃
- 断线重连（规划中）

### 消息批处理

- 支持批量发送消息
- 减少网络往返次数

---

## 开发指南

### 添加新的消息类型

1. **定义协议文件** (`.proto`)
   ```protobuf
   message MyNewMessage {
       string data = 1;
   }
   ```

2. **生成C#代码**
   ```bash
   # 运行Proto2CS工具
   cd AstrumTool/Proto2CS
   # 生成代码
   ```

3. **创建消息处理器**
   ```csharp
   [MessageHandler(typeof(MyNewMessage))]
   public class MyNewMessageHandler : MessageHandlerBase<MyNewMessage>
   {
       public override async Task HandleMessageAsync(MyNewMessage message)
       {
           // 处理逻辑
           await Task.CompletedTask;
       }
   }
   ```

4. **服务器处理**
   ```csharp
   // 在GameServer.OnMessageReceived中添加
   case MyNewMessage msg:
       HandleMyNewMessage(client, msg);
       break;
   ```

### 发送消息

**客户端**:
```csharp
var message = MyMessage.Create();
message.data = "Hello";
NetworkManager.Instance.Send(message);
```

**服务器**:
```csharp
var response = MyResponse.Create();
response.success = true;
_serverNetworkManager.SendMessage(sessionId, response);
```

### 调试技巧

- 启用NetworkManager的Debug日志
- 使用Unity Console查看消息日志
- 服务器日志文件: `AstrumServer/bin/Debug/net9.0/server.log`

---

## 相关文档

- [联机模式设计文档](../09-GameModes%20游戏模式/Network-Multiplayer%20联机模式.md)
- [消息处理器系统实现方案](../05-CoreArchitecture%20核心架构/序列化/)
- [Protocol Buffers协议定义](../../AstrumConfig/Proto/)

---

## 待优化项

- [ ] 消息压缩
- [ ] 断线重连机制
- [ ] WebSocket支持（用于Web客户端）
- [ ] 消息加密
- [ ] 网络流量监控和限流

