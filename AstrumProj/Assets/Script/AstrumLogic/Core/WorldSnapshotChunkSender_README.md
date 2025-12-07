# 世界快照分片发送功能

## 概述

当 `worldSnapshot` 数据超过 60000 字节时，会自动使用分片发送机制，避免超过 Outer 协议的大小限制（65535 字节）。

## 客户端使用

客户端已经自动处理分片接收，无需额外代码。`WorldSnapshotChunkHandler` 会自动：
1. 接收所有分片
2. 验证分片完整性
3. 组装完整的快照数据
4. 通知 `FrameSyncHandler` 处理

## 服务器端使用

### 方法 1：使用辅助类（推荐）

```csharp
using Astrum.LogicCore.Core;
using Astrum.Generated;

// 获取快照数据
byte[] worldSnapshot = serverLSController.GetSnapshotBytes(0);

// 发送通知（自动处理分片）
FrameSyncStartNotificationHelper.SendFrameSyncStartNotification(
    roomId: "room_123",
    frameRate: 20,
    frameInterval: 50,
    startTime: serverLSController.CreationTime,
    playerIds: playerIdList,
    worldSnapshot: worldSnapshot,
    playerIdMapping: playerIdMapping,
    sendNotificationAction: (notification) => {
        // 发送 FrameSyncStartNotification
        SendMessageToClient(clientId, notification);
    },
    sendChunkAction: (chunk) => {
        // 发送 WorldSnapshotChunk（如果需要分片）
        SendMessageToClient(clientId, chunk);
    }
);
```

### 方法 2：使用 ServerLSController 辅助方法

```csharp
FrameSyncStartNotificationHelper.SendFrameSyncStartNotificationFromController(
    serverController: serverLSController,
    roomId: "room_123",
    playerIds: playerIdList,
    playerIdMapping: playerIdMapping,
    sendNotificationAction: (notification) => {
        SendMessageToClient(clientId, notification);
    },
    sendChunkAction: (chunk) => {
        SendMessageToClient(clientId, chunk);
    }
);
```

### 方法 3：手动分片发送

```csharp
// 创建通知（worldSnapshot 设为 null）
var notification = FrameSyncStartNotification.Create();
notification.roomId = roomId;
notification.frameRate = 20;
// ... 设置其他字段
notification.worldSnapshot = null; // 通过分片发送

// 发送通知
SendMessageToClient(clientId, notification);

// 分片发送快照
if (worldSnapshot != null && worldSnapshot.Length > 60000)
{
    WorldSnapshotChunkSender.SendSnapshotInChunks(
        roomId,
        worldSnapshot,
        (rid, index, total, data, size) =>
        {
            var chunk = WorldSnapshotChunkSender.CreateChunkMessage(rid, index, total, data, size);
            SendMessageToClient(clientId, chunk);
        });
}
```

## 注意事项

1. **分片大小限制**：单个分片最大 60000 字节，确保不超过 Outer 协议限制
2. **发送顺序**：先发送 `FrameSyncStartNotification`，再发送分片消息
3. **客户端处理**：客户端会自动接收并组装所有分片，无需手动处理
4. **超时处理**：如果 30 秒内未收到所有分片，接收器会被清理

## 错误处理

- 如果分片参数不匹配，会清理旧的接收器并创建新的
- 如果分片数据为空或索引超出范围，会记录错误并忽略
- 如果总大小不匹配，会记录警告但继续处理


