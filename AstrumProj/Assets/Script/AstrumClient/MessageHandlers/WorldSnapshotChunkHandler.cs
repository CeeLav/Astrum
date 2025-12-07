using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Client.Managers.GameModes.Handlers;
using Astrum.Network.MessageHandlers;
using Astrum.Client.Core;

namespace Astrum.Client.MessageHandlers
{
    /// <summary>
    /// 世界快照分片消息处理器
    /// </summary>
    [MessageHandler(typeof(WorldSnapshotChunk))]
    public class WorldSnapshotChunkHandler : MessageHandlerBase<WorldSnapshotChunk>
    {
        // 存储每个房间的分片数据
        private static Dictionary<string, SnapshotChunkReceiver> _receivers = new Dictionary<string, SnapshotChunkReceiver>();
        private static readonly object _receiversLock = new object();
        
        // 分片接收超时时间（秒）
        private const int ChunkReceiveTimeout = 30;

        /// <summary>
        /// 准备接收器（由 WorldSnapshotStartHandler 调用）
        /// </summary>
        public static void PrepareReceiver(string roomId, int totalChunks, int totalSize)
        {
            lock (_receiversLock)
            {
                // 如果已存在接收器，先清理
                if (_receivers.ContainsKey(roomId))
                {
                    ASLogger.Instance.Warning(
                        $"WorldSnapshotChunkHandler: 接收器已存在，清理旧接收器 - 房间: {roomId}",
                        "FrameSync.Client");
                    _receivers.Remove(roomId);
                }

                // 创建新的接收器
                var receiver = new SnapshotChunkReceiver(roomId, totalChunks, totalSize);
                _receivers[roomId] = receiver;
                
                ASLogger.Instance.Info(
                    $"WorldSnapshotChunkHandler: 准备接收器 - 房间: {roomId}, 总分片数: {totalChunks}, 总大小: {totalSize} bytes",
                    "FrameSync.Client");
            }
        }

        /// <summary>
        /// 检查接收器是否存在
        /// </summary>
        public static bool HasReceiver(string roomId)
        {
            lock (_receiversLock)
            {
                return _receivers.ContainsKey(roomId);
            }
        }

        public override async Task HandleMessageAsync(WorldSnapshotChunk message)
        {
            try
            {
                ASLogger.Instance.Debug(
                    $"WorldSnapshotChunkHandler: 收到快照分片 - 房间: {message.roomId}, 分片索引: {message.chunkIndex}, 大小: {message.chunkData?.Length ?? 0} bytes",
                    "FrameSync.Client");

                // 获取接收器
                SnapshotChunkReceiver receiver;
                lock (_receiversLock)
                {
                    if (!_receivers.TryGetValue(message.roomId, out receiver))
                    {
                        ASLogger.Instance.Error(
                            $"WorldSnapshotChunkHandler: 未找到接收器，请先发送 WorldSnapshotStart - 房间: {message.roomId}, 分片索引: {message.chunkIndex}",
                            "FrameSync.Client");
                        return;
                    }
                }

                // 验证分片索引
                if (message.chunkIndex < 0 || message.chunkIndex >= receiver.TotalChunks)
                {
                    ASLogger.Instance.Error(
                        $"WorldSnapshotChunkHandler: 分片索引超出范围 - 房间: {message.roomId}, 分片索引: {message.chunkIndex}, 总分片数: {receiver.TotalChunks}",
                        "FrameSync.Client");
                    return;
                }

                // 添加分片
                if (!receiver.AddChunk(message.chunkIndex, message.chunkData))
                {
                    ASLogger.Instance.Warning(
                        $"WorldSnapshotChunkHandler: 分片添加失败 - 房间: {message.roomId}, 分片索引: {message.chunkIndex}",
                        "FrameSync.Client");
                    return;
                }

                // 检查是否所有分片都已接收
                if (receiver.IsComplete())
                {
                    ASLogger.Instance.Info(
                        $"WorldSnapshotChunkHandler: 所有分片接收完成 - 房间: {message.roomId}, 总大小: {receiver.TotalSize} bytes",
                        "FrameSync.Client");

                    // 获取完整的快照数据
                    byte[] completeSnapshot = receiver.GetCompleteSnapshot();

                    // 清理接收器
                    lock (_receiversLock)
                    {
                        _receivers.Remove(message.roomId);
                    }

                    // 通知 FrameSyncHandler 处理完整的快照
                    var gameMode = GameDirector.Instance.CurrentGameMode as Astrum.Client.Managers.GameModes.MultiplayerGameMode;
                    if (gameMode?.FrameSyncHandler != null)
                    {
                        gameMode.FrameSyncHandler.OnWorldSnapshotComplete(message.roomId, completeSnapshot);
                    }
                    else
                    {
                        ASLogger.Instance.Error(
                            $"WorldSnapshotChunkHandler: 无法找到 MultiplayerGameMode 或 FrameSyncHandler - 房间: {message.roomId}",
                            "FrameSync.Client");
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"WorldSnapshotChunkHandler: 处理分片消息时发生异常 - {ex.Message}", "FrameSync.Client");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 清理指定房间的接收器（用于超时或错误处理）
        /// </summary>
        public static void ClearReceiver(string roomId)
        {
            lock (_receiversLock)
            {
                if (_receivers.TryGetValue(roomId, out var receiver))
                {
                    ASLogger.Instance.Info(
                        $"WorldSnapshotChunkHandler: 清理接收器 - 房间: {roomId}, 已接收: {receiver.ReceivedChunks}/{receiver.TotalChunks}",
                        "FrameSync.Client");
                    _receivers.Remove(roomId);
                }
            }
        }

        /// <summary>
        /// 清理所有超时的接收器
        /// </summary>
        public static void ClearTimeoutReceivers()
        {
            var now = DateTime.UtcNow;
            var toRemove = new List<string>();
            
            lock (_receiversLock)
            {
                foreach (var kvp in _receivers)
                {
                    var receiver = kvp.Value;
                    if ((now - receiver.CreateTime).TotalSeconds > ChunkReceiveTimeout)
                    {
                        ASLogger.Instance.Warning(
                            $"WorldSnapshotChunkHandler: 接收器超时 - 房间: {kvp.Key}, 已接收: {receiver.ReceivedChunks}/{receiver.TotalChunks}",
                            "FrameSync.Client");
                        toRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var roomId in toRemove)
                {
                    _receivers.Remove(roomId);
                }
            }
        }
    }

    /// <summary>
    /// 快照分片接收器 - 管理分片的接收和组装
    /// </summary>
    internal class SnapshotChunkReceiver
    {
        private readonly string _roomId;
        private readonly int _totalChunks;
        private readonly int _totalSize;
        private readonly Dictionary<int, byte[]> _chunks = new Dictionary<int, byte[]>();
        private readonly object _lock = new object();

        public string RoomId => _roomId;
        public int TotalChunks => _totalChunks;
        public int TotalSize => _totalSize;
        public int ReceivedChunks
        {
            get
            {
                lock (_lock)
                {
                    return _chunks.Count;
                }
            }
        }
        public DateTime CreateTime { get; private set; }

        public SnapshotChunkReceiver(string roomId, int totalChunks, int totalSize)
        {
            _roomId = roomId;
            _totalChunks = totalChunks;
            _totalSize = totalSize;
            CreateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 添加分片
        /// </summary>
        public bool AddChunk(int chunkIndex, byte[] chunkData)
        {
            lock (_lock)
            {
                // 检查索引范围
                if (chunkIndex < 0 || chunkIndex >= _totalChunks)
                {
                    ASLogger.Instance.Error(
                        $"SnapshotChunkReceiver: 分片索引超出范围 - 房间: {_roomId}, 索引: {chunkIndex}, 总分片数: {_totalChunks}",
                        "FrameSync.Client");
                    return false;
                }

                // 检查是否已存在
                if (_chunks.ContainsKey(chunkIndex))
                {
                    ASLogger.Instance.Warning(
                        $"SnapshotChunkReceiver: 分片已存在，跳过 - 房间: {_roomId}, 索引: {chunkIndex}",
                        "FrameSync.Client");
                    return true; // 已存在，不算错误
                }

                // 检查数据
                if (chunkData == null || chunkData.Length == 0)
                {
                    ASLogger.Instance.Error(
                        $"SnapshotChunkReceiver: 分片数据为空 - 房间: {_roomId}, 索引: {chunkIndex}",
                        "FrameSync.Client");
                    return false;
                }

                // 添加分片
                _chunks[chunkIndex] = chunkData;

                ASLogger.Instance.Debug(
                    $"SnapshotChunkReceiver: 分片添加成功 - 房间: {_roomId}, 索引: {chunkIndex}, 进度: {_chunks.Count}/{_totalChunks}",
                    "FrameSync.Client");

                return true;
            }
        }

        /// <summary>
        /// 检查是否所有分片都已接收
        /// </summary>
        public bool IsComplete()
        {
            lock (_lock)
            {
                return _chunks.Count == _totalChunks;
            }
        }

        /// <summary>
        /// 获取完整的快照数据
        /// </summary>
        public byte[] GetCompleteSnapshot()
        {
            lock (_lock)
            {
                if (!IsComplete())
                {
                    ASLogger.Instance.Error(
                        $"SnapshotChunkReceiver: 分片未完全接收 - 房间: {_roomId}, 已接收: {_chunks.Count}/{_totalChunks}",
                        "FrameSync.Client");
                    return null;
                }

                // 按索引排序并组装
                var sortedChunks = new List<byte[]>(_totalChunks);
                for (int i = 0; i < _totalChunks; i++)
                {
                    if (!_chunks.TryGetValue(i, out var chunk))
                    {
                        ASLogger.Instance.Error(
                            $"SnapshotChunkReceiver: 缺少分片 - 房间: {_roomId}, 索引: {i}",
                            "FrameSync.Client");
                        return null;
                    }
                    sortedChunks.Add(chunk);
                }

                // 计算总大小
                int totalSize = 0;
                foreach (var chunk in sortedChunks)
                {
                    totalSize += chunk.Length;
                }

                // 验证总大小
                if (totalSize != _totalSize && _totalSize > 0)
                {
                    ASLogger.Instance.Warning(
                        $"SnapshotChunkReceiver: 总大小不匹配 - 房间: {_roomId}, 计算: {totalSize}, 期望: {_totalSize}",
                        "FrameSync.Client");
                }

                // 组装完整数据
                byte[] completeData = new byte[totalSize];
                int offset = 0;
                foreach (var chunk in sortedChunks)
                {
                    Array.Copy(chunk, 0, completeData, offset, chunk.Length);
                    offset += chunk.Length;
                }

                ASLogger.Instance.Info(
                    $"SnapshotChunkReceiver: 快照组装完成 - 房间: {_roomId}, 总大小: {totalSize} bytes",
                    "FrameSync.Client");

                return completeData;
            }
        }
    }
}

