using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Astrum.CommonBase;
using Astrum.Generated;

namespace Astrum.LogicCore.FrameSync
{
    /// <summary>
    /// 回放时间线与索引 - 封装回放文件，提供按帧号查询输入与快照的能力
    /// </summary>
    public class ReplayTimeline
    {
        // 回放文件数据结构（与服务器端保持一致）
        private ReplayFileData _replayData;
        
        // 快照索引（按帧号排序，用于二分查找）
        private readonly List<ReplaySnapshotData> _snapshots = new();
        
        // 帧输入索引（帧号 -> 输入数据）
        private readonly Dictionary<int, ReplayFrameInputsData> _frameInputs = new();

        /// <summary>
        /// 总帧数
        /// </summary>
        public int TotalFrames => _replayData?.TotalFrames ?? 0;

        /// <summary>
        /// 帧率
        /// </summary>
        public int TickRate => _replayData?.TickRate ?? 60;

        /// <summary>
        /// 起始时间戳
        /// </summary>
        public long StartTimestamp => _replayData?.StartTimestamp ?? 0;

        /// <summary>
        /// 随机种子
        /// </summary>
        public int RandomSeed => _replayData?.RandomSeed ?? 0;

        /// <summary>
        /// 玩家列表
        /// </summary>
        public List<ReplayPlayerInfoData> Players => _replayData?.Players ?? new List<ReplayPlayerInfoData>();

        /// <summary>
        /// 房间ID
        /// </summary>
        public string RoomId => _replayData?.RoomId ?? "";

        /// <summary>
        /// 从文件加载回放数据
        /// </summary>
        public bool LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    ASLogger.Instance.Error($"ReplayTimeline: 回放文件不存在 - {filePath}", "Replay.Timeline");
                    return false;
                }

                byte[] fileData = File.ReadAllBytes(filePath);
                
                // 反序列化回放文件（使用 MemoryPack）
                // 注意：需要引用服务器端的 BattleReplayFile 类型，或者创建客户端版本
                // 暂时使用动态反序列化
                _replayData = DeserializeReplayFile(fileData);
                
                if (_replayData == null)
                {
                    ASLogger.Instance.Error("ReplayTimeline: 回放文件反序列化失败", "Replay.Timeline");
                    return false;
                }

                // 构建快照索引（按帧号排序）
                _snapshots.Clear();
                foreach (var snapshot in _replayData.Snapshots)
                {
                    _snapshots.Add(new ReplaySnapshotData
                    {
                        Frame = snapshot.Frame,
                        WorldData = snapshot.WorldData
                    });
                }
                _snapshots.Sort((a, b) => a.Frame.CompareTo(b.Frame));

                // 构建帧输入索引
                _frameInputs.Clear();
                foreach (var frameInput in _replayData.FrameInputs)
                {
                    _frameInputs[frameInput.Frame] = new ReplayFrameInputsData
                    {
                        Frame = frameInput.Frame,
                        InputsData = frameInput.InputsData
                    };
                }

                ASLogger.Instance.Info($"ReplayTimeline: 加载回放文件成功 - 总帧数: {TotalFrames}, 快照数: {_snapshots.Count}, 帧输入数: {_frameInputs.Count}", "Replay.Timeline");
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ReplayTimeline: 加载回放文件失败 - {filePath}, 错误: {ex.Message}", "Replay.Timeline");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 获取最近快照（frame0 <= frame 且距离最近的快照）
        /// </summary>
        public ReplaySnapshotData? GetNearestSnapshot(int frame)
        {
            if (_snapshots.Count == 0) return null;

            // 二分查找：找到 frame0 <= frame 的最大快照
            int left = 0;
            int right = _snapshots.Count - 1;
            ReplaySnapshotData? result = null;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                var snapshot = _snapshots[mid];

                if (snapshot.Frame <= frame)
                {
                    result = snapshot;
                    left = mid + 1; // 继续向右查找更大的快照
                }
                else
                {
                    right = mid - 1;
                }
            }

            return result;
        }

        /// <summary>
        /// 获取指定帧的输入数据
        /// </summary>
        public OneFrameInputs? GetFrameInputs(int frame)
        {
            if (_frameInputs.TryGetValue(frame, out var frameInputsData))
            {
                // 反序列化输入数据
                return DeserializeFrameInputs(frameInputsData.InputsData);
            }
            return null;
        }

        /// <summary>
        /// 获取指定帧的快照数据（解压缩）
        /// </summary>
        public byte[]? GetSnapshotWorldData(int frame)
        {
            var snapshot = GetNearestSnapshot(frame);
            if (snapshot != null && snapshot.Frame == frame)
            {
                // 解压缩快照数据
                return DecompressData(snapshot.WorldData);
            }
            return null;
        }

        /// <summary>
        /// 反序列化回放文件
        /// </summary>
        private ReplayFileData DeserializeReplayFile(byte[] data)
        {
            try
            {
                // 使用反射获取服务器端的 BattleReplayFile 类型
                // 或者创建客户端版本的数据结构
                // 暂时使用简化版本
                var type = Type.GetType("AstrumServer.FrameSync.BattleReplayFile, AstrumLogic.Server");
                if (type == null)
                {
                    // 如果找不到服务器端类型，使用客户端版本
                    type = typeof(ReplayFileData);
                }

                object obj = MemoryPackHelper.Deserialize(type, data, 0, data.Length);
                return ConvertToReplayFileData(obj);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ReplayTimeline: 反序列化回放文件失败 - {ex.Message}", "Replay.Timeline");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// 转换服务器端类型到客户端数据结构
        /// </summary>
        private ReplayFileData ConvertToReplayFileData(object obj)
        {
            // 使用反射获取属性值
            var type = obj.GetType();
            var replayData = new ReplayFileData();

            var versionProp = type.GetProperty("Version");
            var roomIdProp = type.GetProperty("RoomId");
            var tickRateProp = type.GetProperty("TickRate");
            var totalFramesProp = type.GetProperty("TotalFrames");
            var startTimestampProp = type.GetProperty("StartTimestamp");
            var randomSeedProp = type.GetProperty("RandomSeed");
            var playersProp = type.GetProperty("Players");
            var snapshotsProp = type.GetProperty("Snapshots");
            var frameInputsProp = type.GetProperty("FrameInputs");

            if (versionProp != null) replayData.Version = (int)versionProp.GetValue(obj);
            if (roomIdProp != null) replayData.RoomId = (string)roomIdProp.GetValue(obj) ?? "";
            if (tickRateProp != null) replayData.TickRate = (int)tickRateProp.GetValue(obj);
            if (totalFramesProp != null) replayData.TotalFrames = (int)totalFramesProp.GetValue(obj);
            if (startTimestampProp != null) replayData.StartTimestamp = (long)startTimestampProp.GetValue(obj);
            if (randomSeedProp != null) replayData.RandomSeed = (int)randomSeedProp.GetValue(obj);

            if (playersProp != null)
            {
                var players = playersProp.GetValue(obj) as System.Collections.IEnumerable;
                if (players != null)
                {
                    foreach (var player in players)
                    {
                        var playerType = player.GetType();
                        var userIdProp = playerType.GetProperty("UserId");
                        var playerIdProp = playerType.GetProperty("PlayerId");
                        var displayNameProp = playerType.GetProperty("DisplayName");

                        replayData.Players.Add(new ReplayPlayerInfoData
                        {
                            UserId = (string)(userIdProp?.GetValue(player) ?? ""),
                            PlayerId = (long)(playerIdProp?.GetValue(player) ?? 0L),
                            DisplayName = (string)(displayNameProp?.GetValue(player) ?? "")
                        });
                    }
                }
            }

            if (snapshotsProp != null)
            {
                var snapshots = snapshotsProp.GetValue(obj) as System.Collections.IEnumerable;
                if (snapshots != null)
                {
                    foreach (var snapshot in snapshots)
                    {
                        var snapshotType = snapshot.GetType();
                        var frameProp = snapshotType.GetProperty("Frame");
                        var worldDataProp = snapshotType.GetProperty("WorldData");

                        replayData.Snapshots.Add(new ReplaySnapshotData
                        {
                            Frame = (int)(frameProp?.GetValue(snapshot) ?? 0),
                            WorldData = (byte[])(worldDataProp?.GetValue(snapshot) ?? Array.Empty<byte>())
                        });
                    }
                }
            }

            if (frameInputsProp != null)
            {
                var frameInputs = frameInputsProp.GetValue(obj) as System.Collections.IEnumerable;
                if (frameInputs != null)
                {
                    foreach (var frameInput in frameInputs)
                    {
                        var frameInputType = frameInput.GetType();
                        var frameProp = frameInputType.GetProperty("Frame");
                        var inputsDataProp = frameInputType.GetProperty("InputsData");

                        replayData.FrameInputs.Add(new ReplayFrameInputsData
                        {
                            Frame = (int)(frameProp?.GetValue(frameInput) ?? 0),
                            InputsData = (byte[])(inputsDataProp?.GetValue(frameInput) ?? Array.Empty<byte>())
                        });
                    }
                }
            }

            return replayData;
        }

        /// <summary>
        /// 反序列化帧输入
        /// </summary>
        private OneFrameInputs? DeserializeFrameInputs(byte[] data)
        {
            try
            {
                object obj = MemoryPackHelper.Deserialize(typeof(OneFrameInputs), data, 0, data.Length);
                return obj as OneFrameInputs;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ReplayTimeline: 反序列化帧输入失败 - {ex.Message}", "Replay.Timeline");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// 解压缩数据（GZip）
        /// </summary>
        private byte[] DecompressData(byte[] compressedData)
        {
            using (var output = new MemoryStream())
            {
                using (var input = new MemoryStream(compressedData))
                using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                {
                    gzip.CopyTo(output);
                }
                return output.ToArray();
            }
        }

        #region 内部数据结构（客户端版本）

        private class ReplayFileData
        {
            public int Version { get; set; }
            public string RoomId { get; set; } = "";
            public int TickRate { get; set; }
            public int TotalFrames { get; set; }
            public long StartTimestamp { get; set; }
            public int RandomSeed { get; set; }
            public List<ReplayPlayerInfoData> Players { get; set; } = new();
            public List<ReplaySnapshotData> Snapshots { get; set; } = new();
            public List<ReplayFrameInputsData> FrameInputs { get; set; } = new();
        }

        public class ReplayPlayerInfoData
        {
            public string UserId { get; set; } = "";
            public long PlayerId { get; set; }
            public string DisplayName { get; set; } = "";
        }

        public class ReplaySnapshotData
        {
            public int Frame { get; set; }
            public byte[] WorldData { get; set; } = Array.Empty<byte>();
        }

        private class ReplayFrameInputsData
        {
            public int Frame { get; set; }
            public byte[] InputsData { get; set; } = Array.Empty<byte>();
        }

        #endregion
    }
}

