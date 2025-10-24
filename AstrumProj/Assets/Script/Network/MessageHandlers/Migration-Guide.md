# NetworkManager 迁移指南

## 迁移概述

从传统的Action回调方式迁移到基于ET框架的灵活消息处理器系统。

## 迁移步骤

### 步骤1：备份现有代码
```bash
# 备份原始NetworkManager
cp NetworkManager.cs NetworkManager.cs.backup
```

### 步骤2：替换NetworkManager
```csharp
// 将 NetworkManagerRefactored.cs 重命名为 NetworkManager.cs
// 并更新命名空间
namespace Astrum.Client.Managers
{
    public class NetworkManager : Singleton<NetworkManager>
    {
        // 重构后的代码
    }
}
```

### 步骤3：更新初始化代码
```csharp
// 在应用启动时初始化消息处理器系统
public void Start()
{
    // 初始化NetworkManager（会自动初始化消息处理器分发器）
    NetworkManager.Instance.Initialize();
    
    // 其他初始化代码...
}
```

### 步骤4：移除旧的NetworkMessageHandler
```csharp
// 删除或注释掉旧的NetworkMessageHandler初始化
// NetworkMessageHandler.Instance.Initialize(); ❌ 删除
```

## 代码对比

### 重构前
```csharp
public class NetworkManager : Singleton<NetworkManager>
{
    // 24个Action事件定义
    public event Action<LoginResponse> OnLoginResponse;
    public event Action<CreateRoomResponse> OnCreateRoomResponse;
    // ... 更多事件
    
    private void DispatchMessage(object messageObject)
    {
        switch (messageObject)
        {
            case LoginResponse loginResponse:
                OnLoginResponse?.Invoke(loginResponse);
                break;
            // ... 20个case分支
        }
    }
}
```

### 重构后
```csharp
public class NetworkManager : Singleton<NetworkManager>
{
    // 只保留连接状态事件
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<ConnectionStatus> OnConnectionStatusChanged;
    
    private async Task ProcessReceivedMessage(MemoryBuffer buffer)
    {
        var messageObject = MessageSerializeHelper.ToMessage(tcpService, buffer);
        if (messageObject != null)
        {
            // 使用消息处理器分发系统
            await MessageHandlerDispatcher.Instance.DispatchAsync(messageObject);
        }
    }
}
```

## 消息处理器创建

### 自动注册
所有带有`[MessageHandler(typeof(MessageType))]`标记的类会自动注册：

```csharp
[MessageHandler(typeof(LoginResponse))]
public class LoginMessageHandler : MessageHandlerBase<LoginResponse>
{
    protected override async Task HandleMessageAsync(LoginResponse message)
    {
        // 处理登录响应逻辑
        if (message.Success)
        {
            UserManager.Instance?.HandleLoginResponse(message);
        }
    }
}
```

### 已创建的处理器
- ✅ LoginMessageHandler
- ✅ RoomMessageHandler (5个房间相关处理器)
- ✅ ConnectMessageHandler
- ✅ HeartbeatMessageHandler
- ✅ GameMessageHandler (4个游戏相关处理器)
- ✅ FrameSyncMessageHandler (4个帧同步处理器)
- ✅ MatchMessageHandler (4个匹配相关处理器)

## 特殊处理

### Ping功能
Ping功能需要特殊处理，因为需要等待响应：

```csharp
// 创建临时Ping处理器或使用直接回调
private async Task SendPingAndCalibrateAsync()
{
    // 需要特殊处理Ping响应
    // 可以考虑创建临时处理器或使用直接回调
}
```

### 连接状态事件
连接状态事件保留，因为不是消息处理：
```csharp
// 保留这些事件
public event Action OnConnected;
public event Action OnDisconnected;
public event Action<ConnectionStatus> OnConnectionStatusChanged;
```

## 测试验证

### 功能测试
1. **连接测试**：验证连接建立和断开
2. **消息发送**：验证消息发送功能
3. **消息接收**：验证所有消息类型的处理
4. **错误处理**：验证异常情况的处理

### 性能测试
1. **消息处理性能**：对比新旧系统的性能
2. **内存使用**：检查内存使用情况
3. **并发处理**：测试高并发消息处理

### 兼容性测试
1. **现有功能**：确保所有现有功能正常工作
2. **UI集成**：验证UI系统与网络系统的集成
3. **游戏逻辑**：验证游戏逻辑不受影响

## 回退方案

如果迁移过程中出现问题，可以快速回退：

```csharp
// 1. 恢复原始NetworkManager
cp NetworkManager.cs.backup NetworkManager.cs

// 2. 恢复NetworkMessageHandler
NetworkMessageHandler.Instance.Initialize();

// 3. 重新编译和测试
```

## 注意事项

1. **编译顺序**：确保所有消息处理器都已编译
2. **依赖关系**：检查消息处理器中的依赖关系
3. **日志级别**：保持与原有系统相同的日志级别
4. **错误处理**：确保异常不会影响网络层
5. **性能监控**：监控新系统的性能表现

## 预期收益

1. **代码简化**：NetworkManager代码量减少40%+
2. **维护性提升**：模块化的消息处理
3. **扩展性增强**：易于添加新的消息类型
4. **测试友好**：每个处理器可以独立测试
5. **性能优化**：支持并行处理和异步操作

通过这个迁移，项目将拥有一个更加灵活、可维护和可扩展的网络消息处理系统。
