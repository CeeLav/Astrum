# NetworkManager Action事件统计和Handler创建计划

## 统计结果

### 需要创建Handler的消息类型（共20个）

#### 1. 连接相关消息
1. **ConnectResponse** - 连接响应
2. **LoginResponse** - 登录响应 ✅ 已创建

#### 2. 房间相关消息
3. **CreateRoomResponse** - 创建房间响应 ✅ 已创建
4. **JoinRoomResponse** - 加入房间响应 ✅ 已创建
5. **LeaveRoomResponse** - 离开房间响应 ✅ 已创建
6. **GetRoomListResponse** - 获取房间列表响应 ✅ 已创建
7. **RoomUpdateNotification** - 房间更新通知 ✅ 已创建

#### 3. 心跳和Ping消息
8. **HeartbeatResponse** - 心跳响应
9. **G2C_Ping** - Ping响应

#### 4. 游戏相关消息
10. **GameResponse** - 游戏响应
11. **GameStartNotification** - 游戏开始通知
12. **GameEndNotification** - 游戏结束通知
13. **GameStateUpdate** - 游戏状态更新

#### 5. 帧同步相关消息
14. **FrameSyncStartNotification** - 帧同步开始通知
15. **FrameSyncEndNotification** - 帧同步结束通知
16. **FrameSyncData** - 帧同步数据
17. **OneFrameInputs** - 单帧输入数据

#### 6. 快速匹配相关消息
18. **QuickMatchResponse** - 快速匹配响应
19. **CancelMatchResponse** - 取消匹配响应
20. **MatchFoundNotification** - 匹配找到通知
21. **MatchTimeoutNotification** - 匹配超时通知

### 不需要创建Handler的事件（连接状态管理）
- **OnConnected** - 连接成功事件
- **OnDisconnected** - 断开连接事件
- **OnConnectionStatusChanged** - 连接状态变化事件

## 创建计划

### 阶段1：基础消息处理器
- [x] LoginMessageHandler
- [x] RoomMessageHandler (包含5个房间相关处理器)

### 阶段2：连接和心跳处理器
- [ ] ConnectMessageHandler
- [ ] HeartbeatMessageHandler
- [ ] PingMessageHandler

### 阶段3：游戏相关处理器
- [ ] GameMessageHandler
- [ ] GameNotificationHandler
- [ ] GameStateHandler

### 阶段4：帧同步处理器
- [ ] FrameSyncHandler
- [ ] FrameInputHandler

### 阶段5：匹配相关处理器
- [ ] MatchMessageHandler
- [ ] MatchNotificationHandler

## 文件组织

```
MessageHandlers/
├── IMessageHandler.cs
├── MessageHandlerBase.cs
├── MessageHandlerAttribute.cs
├── MessageHandlerDispatcher.cs
├── LoginMessageHandler.cs
├── RoomMessageHandler.cs
├── ConnectMessageHandler.cs
├── HeartbeatMessageHandler.cs
├── GameMessageHandler.cs
├── FrameSyncMessageHandler.cs
└── MatchMessageHandler.cs
```

## 注意事项

1. **优先级设置**：某些消息可能需要特定的处理顺序
2. **错误处理**：每个处理器都需要适当的异常处理
3. **日志记录**：保持与现有NetworkMessageHandler相同的日志级别
4. **性能考虑**：避免在处理器中执行耗时操作
5. **依赖注入**：处理器可能需要访问各种Manager实例

## 测试计划

1. **单元测试**：为每个处理器创建单元测试
2. **集成测试**：测试消息分发器的完整流程
3. **性能测试**：对比新旧系统的性能差异
4. **兼容性测试**：确保现有功能不受影响
