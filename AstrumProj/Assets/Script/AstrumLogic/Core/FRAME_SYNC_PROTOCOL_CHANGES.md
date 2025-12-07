# 帧同步协议修改说明

## 修改概述

将世界数据从 `FrameSyncStartNotification` 协议中完全剥离，改为独立的传输流程。

## 新的发送/接收流程

### 服务器端发送顺序：
1. **WorldSnapshotStart** - 标识世界数据传输开始，包含总分片数和总大小
2. **WorldSnapshotChunk** (多个) - 分片发送世界快照数据
3. **FrameSyncStartNotification** - 帧同步开始通知（不再包含 worldSnapshot）

### 客户端接收顺序：
1. 接收 `WorldSnapshotStart`，创建接收器
2. 接收 `WorldSnapshotChunk` 分片并组装
3. 接收 `FrameSyncStartNotification`，使用已接收的世界数据初始化游戏

## Proto 文件修改

### 修改的文件：`AstrumConfig/Proto/gamemessages_C_2000.proto`

#### 1. 从 `FrameSyncStartNotification` 移除 `worldSnapshot` 字段

**修改前：**
```protobuf
message FrameSyncStartNotification
{
    string roomId = 1;
    int32 frameRate = 2;
    int32 frameInterval = 3;
    int64 startTime = 4;
    repeated string playerIds = 5;
    bytes worldSnapshot = 6;                // 已移除
    map<string, int64> playerIdMapping = 7;
}
```

**修改后：**
```protobuf
message FrameSyncStartNotification
{
    string roomId = 1;
    int32 frameRate = 2;
    int32 frameInterval = 3;
    int64 startTime = 4;
    repeated string playerIds = 5;
    map<string, int64> playerIdMapping = 6; // 序号改为 6
}
```

#### 2. 添加 `WorldSnapshotStart` 消息

```protobuf
message WorldSnapshotStart
{
    string roomId = 1;         // 房间ID
    int32 totalChunks = 2;     // 总分片数
    int32 totalSize = 3;       // 完整快照的总大小（字节）
}
```

#### 3. 简化 `WorldSnapshotChunk` 消息

**修改前：**
```protobuf
message WorldSnapshotChunk
{
    string roomId = 1;
    int32 chunkIndex = 2;
    int32 totalChunks = 3;     // 已移除
    bytes chunkData = 4;
    int32 totalSize = 5;       // 已移除
}
```

**修改后：**
```protobuf
message WorldSnapshotChunk
{
    string roomId = 1;
    int32 chunkIndex = 2;
    bytes chunkData = 3;       // 序号改为 3
}
```

## 代码修改

### 客户端修改

1. **新增 `WorldSnapshotStartHandler.cs`**
   - 处理 `WorldSnapshotStart` 消息
   - 调用 `WorldSnapshotChunkHandler.PrepareReceiver()` 创建接收器

2. **修改 `WorldSnapshotChunkHandler.cs`**
   - 移除对 `message.totalChunks` 和 `message.totalSize` 的引用
   - 添加 `PrepareReceiver()` 静态方法，由 `WorldSnapshotStartHandler` 调用
   - 添加线程安全的接收器管理

3. **修改 `FrameSyncHandler.cs`**
   - 移除对 `notification.worldSnapshot` 的处理
   - 修改流程：先接收世界数据，再处理帧同步开始通知
   - 添加 `_receivedWorldSnapshot` 字典存储已接收的世界数据
   - 修改 `OnWorldSnapshotComplete()` 和 `OnFrameSyncStartNotification()` 的交互逻辑

### 服务器端修改

1. **修改 `FrameSyncStartNotificationHelper.cs`**
   - 重写发送流程：先发送世界数据，再发送帧同步通知
   - 添加 `sendSnapshotStartAction` 参数
   - 移除对 `worldSnapshot` 字段的设置

2. **修改 `WorldSnapshotChunkSender.cs`**
   - 简化 `CreateChunkMessage()` 方法，移除 `totalChunks` 和 `totalSize` 参数
   - 简化 `SendSnapshotInChunks()` 的回调签名

## 后续步骤

1. **运行 Proto2CS 工具重新生成 C# 代码**
   ```bash
   # 需要运行 Proto2CS 工具生成新的消息类
   ```

2. **测试流程**
   - 服务器端发送顺序是否正确
   - 客户端接收和组装是否正常
   - 帧同步启动是否成功

3. **清理旧代码（可选）**
   - 删除不再使用的 `WorldSnapshotChunkSender_README.md` 或更新文档

## 注意事项

1. **消息顺序很重要**：必须按照 Start → Chunks → Notification 的顺序发送和接收
2. **线程安全**：接收器使用锁保护，确保多线程安全
3. **错误处理**：如果消息顺序错误，会有适当的错误日志
4. **向后兼容**：此修改是破坏性的，需要客户端和服务器端同时更新

