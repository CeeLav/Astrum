using System;
using Astrum.CommonBase;
using Astrum.Generated;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// FrameSyncStartNotification 发送辅助类
    /// 自动处理 worldSnapshot 的分片发送
    /// </summary>
    public static class FrameSyncStartNotificationHelper
    {
        /// <summary>
        /// 单个分片的最大大小（字节），确保不超过 Outer 协议限制（65535）
        /// 留出一些空间给消息头和其他字段
        /// </summary>
        private const int MaxChunkSize = 60000;

        /// <summary>
        /// 创建并发送世界数据和帧同步开始通知
        /// 发送顺序：先发送 WorldSnapshotStart，然后发送 WorldSnapshotChunk 分片，最后发送 FrameSyncStartNotification
        /// </summary>
        /// <param name="roomId">房间ID</param>
        /// <param name="frameRate">帧率</param>
        /// <param name="frameInterval">帧间隔</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="playerIds">玩家ID列表</param>
        /// <param name="worldSnapshot">世界快照数据</param>
        /// <param name="playerIdMapping">玩家ID映射</param>
        /// <param name="sendSnapshotStartAction">发送 WorldSnapshotStart 的回调</param>
        /// <param name="sendChunkAction">发送 WorldSnapshotChunk 的回调</param>
        /// <param name="sendNotificationAction">发送 FrameSyncStartNotification 的回调</param>
        public static void SendFrameSyncStartNotification(
            string roomId,
            int frameRate,
            int frameInterval,
            long startTime,
            System.Collections.Generic.List<string> playerIds,
            byte[] worldSnapshot,
            System.Collections.Generic.Dictionary<string, long> playerIdMapping,
            Action<WorldSnapshotStart> sendSnapshotStartAction,
            Action<WorldSnapshotChunk> sendChunkAction,
            Action<FrameSyncStartNotification> sendNotificationAction)
        {
            if (sendSnapshotStartAction == null || sendChunkAction == null || sendNotificationAction == null)
            {
                ASLogger.Instance.Error("FrameSyncStartNotificationHelper: 所有发送回调都不能为空", "FrameSync.Server");
                return;
            }

            if (worldSnapshot == null || worldSnapshot.Length == 0)
            {
                ASLogger.Instance.Warning($"FrameSyncStartNotificationHelper: 世界快照数据为空 - 房间: {roomId}", "FrameSync.Server");
            }

            // 计算分片信息
            int totalSize = worldSnapshot?.Length ?? 0;
            int totalChunks = totalSize <= MaxChunkSize ? 1 : (int)System.Math.Ceiling((double)totalSize / MaxChunkSize);

            ASLogger.Instance.Info(
                $"FrameSyncStartNotificationHelper: 开始发送帧同步开始流程 - 房间: {roomId}, 快照大小: {totalSize} bytes, 分片数: {totalChunks}",
                "FrameSync.Server");

            // 步骤1：发送 WorldSnapshotStart
            var snapshotStart = WorldSnapshotStart.Create(isFromPool: true);
            snapshotStart.roomId = roomId;
            snapshotStart.totalChunks = totalChunks;
            snapshotStart.totalSize = totalSize;
            sendSnapshotStartAction(snapshotStart);

            // 步骤2：发送 WorldSnapshotChunk 分片
            if (worldSnapshot != null && worldSnapshot.Length > 0)
            {
                WorldSnapshotChunkSender.SendSnapshotInChunks(
                    roomId,
                    worldSnapshot,
                    (rid, index, chunkData) =>
                    {
                        var chunk = WorldSnapshotChunkSender.CreateChunkMessage(rid, index, chunkData);
                        sendChunkAction(chunk);
                    });
            }

            // 步骤3：发送 FrameSyncStartNotification（不包含 worldSnapshot）
            var notification = FrameSyncStartNotification.Create(isFromPool: true);
            notification.roomId = roomId;
            notification.frameRate = frameRate;
            notification.frameInterval = frameInterval;
            notification.startTime = startTime;
            notification.playerIds = playerIds ?? new System.Collections.Generic.List<string>();
            notification.playerIdMapping = playerIdMapping ?? new System.Collections.Generic.Dictionary<string, long>();
            sendNotificationAction(notification);

            ASLogger.Instance.Info(
                $"FrameSyncStartNotificationHelper: 帧同步开始流程发送完成 - 房间: {roomId}",
                "FrameSync.Server");
        }

        /// <summary>
        /// 从 ServerLSController 创建并发送世界数据和帧同步开始通知
        /// </summary>
        /// <param name="serverController">服务器帧同步控制器</param>
        /// <param name="roomId">房间ID</param>
        /// <param name="playerIds">玩家ID列表</param>
        /// <param name="playerIdMapping">玩家ID映射</param>
        /// <param name="sendSnapshotStartAction">发送 WorldSnapshotStart 的回调</param>
        /// <param name="sendChunkAction">发送 WorldSnapshotChunk 的回调</param>
        /// <param name="sendNotificationAction">发送 FrameSyncStartNotification 的回调</param>
        public static void SendFrameSyncStartNotificationFromController(
            ServerLSController serverController,
            string roomId,
            System.Collections.Generic.List<string> playerIds,
            System.Collections.Generic.Dictionary<string, long> playerIdMapping,
            Action<WorldSnapshotStart> sendSnapshotStartAction,
            Action<WorldSnapshotChunk> sendChunkAction,
            Action<FrameSyncStartNotification> sendNotificationAction)
        {
            if (serverController == null)
            {
                ASLogger.Instance.Error("FrameSyncStartNotificationHelper: serverController 不能为空", "FrameSync.Server");
                return;
            }

            // 获取快照数据
            byte[] worldSnapshot = serverController.GetSnapshotBytes(0);

            // 发送通知
            SendFrameSyncStartNotification(
                roomId,
                serverController.TickRate,
                1000 / serverController.TickRate, // 帧间隔（毫秒）
                serverController.CreationTime,
                playerIds,
                worldSnapshot,
                playerIdMapping,
                sendSnapshotStartAction,
                sendChunkAction,
                sendNotificationAction);
        }
    }
}


