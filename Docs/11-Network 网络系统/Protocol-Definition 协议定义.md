# 📋 协议定义详解

**版本**: v1.0.0  
**最后更新**: 2025-10-31

---

## 概述

Astrum使用Protocol Buffers定义网络消息协议，通过工具自动生成C#代码，实现跨平台、类型安全的网络通信。

---

## 协议文件结构

### 文件位置

所有协议文件位于: `AstrumConfig/Proto/`

### 协议文件列表

| 文件名 | OpCode范围 | 说明 |
|--------|-----------|------|
| `networkcommon_C_1000.proto` | 1000-1999 | 网络通用消息（连接、心跳等） |
| `gamemessages_C_2000.proto` | 2000-2999 | 游戏消息（输入、帧同步等） |
| `game_S_3000.proto` | 3000-3999 | 服务器游戏消息 |
| `connectionstatus_C_4000.proto` | 4000-4999 | 连接状态消息 |

### OpCode分配

```
1000-1999: 网络通用消息
2000-2999: 客户端游戏消息
3000-3999: 服务器游戏消息
4000-4999: 连接状态消息
```

---

## 消息定义规范

### 基本消息结构

```protobuf
message MessageName {
    bool success = 1;              // 操作是否成功
    string message = 2;            // 消息内容
    int64 timestamp = 3;           // 时间戳
}
```

### 命名规范

- **请求消息**: `XxxRequest` (例如: `LoginRequest`)
- **响应消息**: `XxxResponse` (例如: `LoginResponse`)
- **通知消息**: `XxxNotification` (例如: `GameStartNotification`)

---

## 主要消息类型

### 1. 连接相关消息 (1000-1999)

#### ConnectRequest / ConnectResponse

```protobuf
message ConnectRequest {
    int64 timestamp = 1;
}

message ConnectResponse {
    bool success = 1;
    string message = 2;
    int64 timestamp = 3;
}
```

**用途**: 客户端连接服务器时的握手消息

**流程**:
1. 客户端建立TCP连接
2. 客户端发送 `ConnectRequest`
3. 服务器响应 `ConnectResponse`

### 2. 登录相关消息 (1000-1999)

#### LoginRequest / LoginResponse

```protobuf
message LoginRequest {
    string displayName = 1;
}

message LoginResponse {
    bool Success = 1;
    string Message = 2;
    string UserId = 3;
    string DisplayName = 4;
    int64 Timestamp = 5;
}
```

**用途**: 用户登录认证

**流程**:
1. 客户端发送 `LoginRequest`（包含显示名称）
2. 服务器验证并创建用户
3. 服务器响应 `LoginResponse`（包含用户ID）

### 3. 房间相关消息 (2000-2999)

#### CreateRoomRequest / CreateRoomResponse

```protobuf
message CreateRoomRequest {
    string roomName = 1;
    int32 maxPlayers = 2;
}

message CreateRoomResponse {
    bool Success = 1;
    string Message = 2;
    RoomInfo Room = 3;
}
```

#### JoinRoomRequest / JoinRoomResponse

```protobuf
message JoinRoomRequest {
    string roomId = 1;
}

message JoinRoomResponse {
    bool Success = 1;
    string Message = 2;
    RoomInfo Room = 3;
}
```

### 4. 游戏消息 (2000-2999)

#### GameStartNotification

```protobuf
message GameStartNotification {
    string roomId = 1;
    GameConfig config = 2;
    GameRoomState roomState = 3;
    int64 startTime = 4;
    repeated string playerIds = 5;
}
```

**用途**: 服务器通知所有客户端游戏开始

#### GameEndNotification

```protobuf
message GameEndNotification {
    string roomId = 1;
    string reason = 2;
    GameResult result = 3;
}
```

### 5. 帧同步消息 (2000-2999)

#### SingleInput (客户端→服务器)

```protobuf
message SingleInput {
    int64 PlayerID = 1;
    int32 FrameID = 2;
    LSInput Input = 3;
}
```

**用途**: 客户端上报单帧输入

#### OneFrameInputs (服务器→客户端)

```protobuf
message OneFrameInputs {
    int32 FrameID = 1;
    repeated PlayerInput Inputs = 2;
}
```

**用途**: 服务器广播一帧的所有玩家输入

#### FrameSyncStartNotification

```protobuf
message FrameSyncStartNotification {
    string roomId = 1;
    int32 frameRate = 2;
    int64 startTime = 3;
}
```

### 6. 匹配消息 (2000-2999)

#### QuickMatchRequest / QuickMatchResponse

```protobuf
message QuickMatchRequest {
    int64 timestamp = 1;
}

message QuickMatchResponse {
    bool Success = 1;
    string Message = 2;
    int32 QueuePosition = 3;
    int32 QueueSize = 4;
}
```

---

## 消息序列化

### 序列化格式

- **格式**: MemoryPack (高性能二进制序列化)
- **兼容**: 支持Protocol Buffers定义的消息类型

### 消息包结构

```
┌─────────────────────────────────────┐
│  消息头 (4 bytes)                   │
├─────────────────────────────────────┤
│  Length (2 bytes) | OpCode (2 bytes)│
└─────────────────────────────────────┘
┌─────────────────────────────────────┐
│  消息体 (N bytes)                    │
├─────────────────────────────────────┤
│  MemoryPack序列化的消息数据           │
└─────────────────────────────────────┘
```

### 序列化流程

```
1. 创建消息对象
   var message = LoginRequest.Create();
   message.displayName = "Player";
   
2. 获取OpCode
   var opcode = OpcodeType.Instance.GetOpcode(typeof(LoginRequest));
   
3. MemoryPack序列化
   var bytes = MemoryPackSerializer.Serialize(message);
   
4. 构造消息包
   [Length(2 bytes)] [OpCode(2 bytes)] [Body(N bytes)]
   
5. 发送
   session.Send(bytes);
```

---

## OpCode管理

### OpcodeType

**位置**: `AstrumProj/Assets/Script/Network/OpcodeType.cs`

**职责**:
- 管理消息类型到OpCode的映射
- 支持OpCode到消息类型的反向查找

### OpCode注册

```csharp
// 在应用启动时注册
OpcodeType.Instance.Awake();

// 内部自动扫描所有MessageObject类型
// 根据命名规范或特性分配OpCode
```

### OpCode查找

```csharp
// 消息类型 → OpCode
ushort opcode = OpcodeType.Instance.GetOpcode(typeof(LoginRequest));

// OpCode → 消息类型
Type messageType = OpcodeType.Instance.GetOpcodeType(opcode);
```

---

## 代码生成

### Proto2CS工具

**位置**: `AstrumTool/Proto2CS/`

**功能**:
1. 解析`.proto`文件
2. 生成C#消息类
3. 生成序列化代码

### 生成流程

```bash
# 1. 运行生成工具
cd AstrumTool/Proto2CS
dotnet run

# 2. 生成的文件位置
# 客户端: AstrumProj/Assets/Script/Generated/
# 服务器: AstrumServer/AstrumServer/Generated/
```

### 生成的消息类结构

```csharp
public partial class LoginRequest : MessageObject
{
    public string DisplayName { get; set; }
    
    public static LoginRequest Create(bool isFromPool = false)
    {
        return ObjectPool.Instance.Fetch(typeof(LoginRequest), isFromPool) as LoginRequest;
    }
    
    // MemoryPack序列化支持
    // ...
}
```

---

## 消息使用

### 客户端发送消息

```csharp
// 1. 创建消息
var request = LoginRequest.Create();
request.DisplayName = "Player_123";

// 2. 发送
NetworkManager.Instance.Send(request);
```

### 服务器发送消息

```csharp
// 1. 创建响应
var response = LoginResponse.Create();
response.Success = true;
response.UserId = "user_123";
response.DisplayName = "Player_123";

// 2. 发送到指定Session
_serverNetworkManager.SendMessage(sessionId, response);
```

### 消息处理

```csharp
// 通过消息处理器自动处理
[MessageHandler(typeof(LoginResponse))]
public class LoginMessageHandler : MessageHandlerBase<LoginResponse>
{
    public override async Task HandleMessageAsync(LoginResponse message)
    {
        if (message.Success)
        {
            // 处理登录成功
        }
    }
}
```

---

## 协议版本兼容

### 版本管理

**当前策略**: 向后兼容，新增字段使用新编号

### 兼容性规则

1. **新增字段**: 使用新的字段编号，不会破坏旧版本
2. **删除字段**: 标记为deprecated，保留字段编号
3. **修改字段**: 创建新的消息类型

---

## 最佳实践

### 1. 消息设计

- 保持消息简单，避免嵌套过深
- 使用repeated字段表示数组
- 使用optional字段表示可选数据

### 2. 字段命名

- 使用PascalCase (C#风格)
- 字段编号从1开始
- 不要重用已删除的字段编号

### 3. 消息大小

- 避免单条消息过大（建议<64KB）
- 使用增量更新而非全量同步
- 压缩大型数据

### 4. 错误处理

- 所有响应消息都包含success字段
- 错误信息通过message字段传递
- 记录关键消息的日志

---

## 总结

协议定义是网络通信的基础：

✅ **类型安全**: Protocol Buffers提供强类型定义  
✅ **跨平台**: 支持客户端和服务器共享协议  
✅ **高性能**: MemoryPack序列化性能优秀  
✅ **易维护**: 集中管理协议文件  
✅ **可扩展**: 支持版本兼容和字段扩展

