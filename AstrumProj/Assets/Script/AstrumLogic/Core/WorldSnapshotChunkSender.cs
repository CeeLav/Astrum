using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.Generated;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 世界快照分片发送器 - 用于将大的 worldSnapshot 分片发送
    /// </summary>
    public static class WorldSnapshotChunkSender
    {
        /// <summary>
        /// 单个分片的最大大小（字节），确保不超过 Outer 协议限制（65535）
        /// 留出一些空间给消息头和其他字段
        /// </summary>
        private const int MaxChunkSize = 60000;

        /// <summary>
        /// 将 worldSnapshot 分片并发送
        /// </summary>
        /// <param name="roomId">房间ID</param>
        /// <param name="worldSnapshot">完整的世界快照数据</param>
        /// <param name="sendChunkAction">发送单个分片的回调函数 (roomId, chunkIndex, chunkData)</param>
        public static void SendSnapshotInChunks(
            string roomId,
            byte[] worldSnapshot,
            Action<string, int, byte[]> sendChunkAction)
        {
            if (worldSnapshot == null || worldSnapshot.Length == 0)
            {
                ASLogger.Instance.Warning($"WorldSnapshotChunkSender: 快照数据为空，房间: {roomId}", "FrameSync.Server");
                return;
            }

            int totalSize = worldSnapshot.Length;
            
            // 计算分片数量
            int totalChunks = totalSize <= MaxChunkSize ? 1 : (int)Math.Ceiling((double)totalSize / MaxChunkSize);

            ASLogger.Instance.Info(
                $"WorldSnapshotChunkSender: 开始分片发送快照 - 房间: {roomId}, 总大小: {totalSize} bytes, 分片数: {totalChunks}",
                "FrameSync.Server");

            // 分片并发送
            for (int i = 0; i < totalChunks; i++)
            {
                int offset = i * MaxChunkSize;
                int chunkSize = Math.Min(MaxChunkSize, totalSize - offset);
                
                byte[] chunkData = new byte[chunkSize];
                Array.Copy(worldSnapshot, offset, chunkData, 0, chunkSize);

                ASLogger.Instance.Debug(
                    $"WorldSnapshotChunkSender: 发送分片 {i + 1}/{totalChunks} - 房间: {roomId}, 大小: {chunkSize} bytes",
                    "FrameSync.Server");

                // 调用回调发送分片
                sendChunkAction(roomId, i, chunkData);
            }

            ASLogger.Instance.Info(
                $"WorldSnapshotChunkSender: 所有分片发送完成 - 房间: {roomId}, 总大小: {totalSize} bytes, 分片数: {totalChunks}",
                "FrameSync.Server");
        }

        /// <summary>
        /// 创建单个分片消息（用于服务器端）
        /// </summary>
        /// <param name="roomId">房间ID</param>
        /// <param name="chunkIndex">分片索引</param>
        /// <param name="chunkData">分片数据</param>
        /// <returns>WorldSnapshotChunk 消息对象</returns>
        public static WorldSnapshotChunk CreateChunkMessage(
            string roomId,
            int chunkIndex,
            byte[] chunkData)
        {
            var chunk = WorldSnapshotChunk.Create(isFromPool: true);
            chunk.roomId = roomId;
            chunk.chunkIndex = chunkIndex;
            chunk.chunkData = chunkData;
            return chunk;
        }
    }
}


