# 🔌 连接管理详解

**版本**: v1.0.0  
**最后更新**: 2025-10-31

---

## 概述

连接管理是网络系统的基础，负责建立和维护客户端与服务器之间的TCP连接，管理连接状态，处理连接事件。

---

## 连接生命周期

```
┌──────────┐
│ 未连接    │
└────┬─────┘
     │ ConnectAsync()
     ↓
┌──────────┐
│ 连接中    │
└────┬─────┘
     │ TCP握手成功
     ↓
┌──────────┐
│ 已连接    │
└────┬─────┘
     │ 正常通信
     ↓
┌──────────┐
│ 断开连接  │
└──────────┘
```

---

## 客户端连接管理

### NetworkManager (客户端)

**位置**: `AstrumProj/Assets/Script/AstrumClient/Managers/NetworkManager.cs`

#### 连接状态

```csharp
public enum ConnectionStatus
{
    Disconnected,    // 未连接
    Connecting,      // 连接中
    Connected,       // 已连接
    Reconnecting     // 重连中（规划中）
}
```

#### 连接方法

```csharp
// 连接到服务器
public async Task<long> ConnectAsync(string serverAddress, int serverPort)

// 断开连接
public void Disconnect(string reason = "Client disconnect")

// 检查连接状态
public bool IsConnected()
```

#### 连接流程

```csharp
1. ConnectAsync()
   ↓
2. 创建IPEndPoint
   ↓
3. TService.ConnectAsync(endPoint)
   ↓
4. 建立TCP连接
   ↓
5. 创建Session对象
   ↓
6. 触发OnConnected事件
   ↓
7. 发送ConnectRequest（可选）
   ↓
8. 等待ConnectResponse
   ↓
9. 连接完成
```

#### 连接事件

```csharp
public event Action OnConnected;              // 连接成功
public event Action OnDisconnected;            // 连接断开
public event Action<ConnectionStatus> OnConnectionStatusChanged; // 状态变化
```

---

## 服务器连接管理

### ServerNetworkManager

**位置**: `AstrumServer/AstrumServer/Network/ServerNetworkManager.cs`

#### 监听和接受连接

```csharp
// 初始化服务器
public Task<bool> InitializeAsync(int port = 8888)

// 更新网络服务
public void Update()

// 接受客户端连接
private void OnAccept(long channelId, IPEndPoint endPoint)
```

#### 连接接受流程

```
1. TService监听端口
   ↓
2. 客户端连接请求
   ↓
3. OnAccept回调
   ↓
4. 创建Session对象
   ↓
5. 添加到sessions字典
   ↓
6. 触发OnClientConnected事件
   ↓
7. GameServer.OnClientConnected()
   ↓
8. 发送ConnectResponse
```

#### Session管理

```csharp
private readonly Dictionary<long, Session> _sessions = new();
private readonly object _sessionsLock = new();

// 根据SessionId发送消息
public void SendMessage(string sessionId, MessageObject message)

// 断开指定Session
public void DisconnectSession(string sessionId, string reason)
```

---

## Session (网络会话)

**位置**: `AstrumProj/Assets/Script/Network/Session.cs`

### 职责

- 管理单个TCP连接
- 消息序列化/反序列化
- 底层网络IO操作
- 连接状态管理

### 关键方法

```csharp
// 发送消息
public void Send(MessageObject message)

// 断开连接
public void Dispose()

// 获取连接信息
public long Id { get; }              // Session ID
public IPEndPoint RemoteAddress { get; } // 远程地址
```

---

## TService (TCP服务)

**位置**: `AstrumProj/Assets/Script/Network/TService.cs`

### 职责

- TCP Socket封装
- 网络IO事件处理
- 缓冲区管理
- 异步IO操作

### 事件回调

```csharp
public Action<long, IPEndPoint> AcceptCallback;  // 接受连接
public Action<long, MemoryBuffer> ReadCallback;  // 读取数据
public Action<long, int> ErrorCallback;          // 错误处理
```

---

## 连接状态同步

### 客户端状态同步

```csharp
// LoginView订阅连接响应事件
EventSystem.Instance.Subscribe<ConnectResponseEventData>(OnConnectResponse);

private void OnConnectResponse(ConnectResponseEventData eventData)
{
    if (eventData.Success)
    {
        OnConnected();
        // 自动发送登录请求
        SendLoginRequest();
    }
}
```

### 服务器状态管理

```csharp
// GameServer.OnClientConnected
private void OnClientConnected(Session client)
{
    // 发送连接响应
    var response = ConnectResponse.Create();
    response.success = true;
    _networkManager.SendMessage(client.Id.ToString(), response);
}
```

---

## 心跳机制

### 目的

- 保持连接活跃
- 检测连接状态
- 超时断开

### 实现

**客户端**:
```csharp
// NetworkManager中定期发送心跳
private void SendHeartbeat()
{
    if (_lastPingAtMs == 0 || 
        TimeInfo.Instance.ClientNow() - _lastPingAtMs > HEARTBEAT_INTERVAL)
    {
        var heartbeat = HeartbeatMessage.Create();
        Send(heartbeat);
        _lastPingAtMs = TimeInfo.Instance.ClientNow();
    }
}
```

**服务器**:
```csharp
// GameServer处理心跳
case HeartbeatMessage heartbeatMessage:
    HandleHeartbeatMessage(client, heartbeatMessage);
    break;

private void HandleHeartbeatMessage(Session client, HeartbeatMessage message)
{
    // 更新最后活动时间
    // 发送心跳响应
    var response = HeartbeatResponse.Create();
    _networkManager.SendMessage(client.Id.ToString(), response);
}
```

---

## 断开处理

### 客户端断开

```csharp
// 用户主动断开
public void Disconnect(string reason = "Client disconnect")
{
    currentSession?.Dispose();
    currentSession = null;
    isConnected = false;
    OnDisconnected?.Invoke();
}

// 网络错误断开
private void OnTcpError(long channelId, int error)
{
    isConnected = false;
    OnDisconnected?.Invoke();
    // 处理重连逻辑（规划中）
}
```

### 服务器断开处理

```csharp
// GameServer.OnClientDisconnected
private void OnClientDisconnected(Session client)
{
    // 从用户管理器移除
    var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
    if (userInfo != null)
    {
        // 从匹配队列移除
        _matchmakingManager.DequeuePlayer(userInfo.Id);
        
        // 从房间移除
        if (!string.IsNullOrEmpty(userInfo.CurrentRoomId))
        {
            _roomManager.LeaveRoom(userInfo.CurrentRoomId, userInfo.Id);
        }
    }
}
```

---

## 错误处理

### 常见错误类型

1. **连接超时**: TCP握手超时
2. **网络异常**: 网络中断、连接重置
3. **服务器拒绝**: 服务器未启动或端口被占用
4. **序列化错误**: 消息格式错误

### 错误处理策略

```csharp
try
{
    await ConnectAsync(address, port);
}
catch (SocketException ex)
{
    // 网络错误
    ASLogger.Instance.Error($"连接失败: {ex.Message}");
    // 更新UI状态
    UpdateConnectionStatus($"连接失败: {ex.Message}");
}
catch (Exception ex)
{
    // 其他错误
    ASLogger.Instance.Error($"未知错误: {ex.Message}");
}
```

---

## 性能优化

### Session池化

**规划中**: 实现Session对象池，减少GC压力

### 连接复用

**规划中**: 支持连接复用，减少连接建立开销

### 批量发送

**规划中**: 支持批量发送消息，减少网络往返

---

## 断线重连机制（规划中）

### 重连策略

1. **立即重连**: 网络短暂中断
2. **延迟重连**: 指数退避策略
3. **状态恢复**: 恢复游戏状态

### 实现要点

- 保存关键状态信息
- 重连后状态同步
- 超时放弃重连

---

## 总结

连接管理是网络系统的基础，需要：

✅ **稳定可靠**: 处理各种网络异常  
✅ **状态清晰**: 明确的连接状态管理  
✅ **事件通知**: 及时的状态变化通知  
✅ **错误处理**: 完善的错误处理机制  
✅ **性能优化**: 高效的连接管理

