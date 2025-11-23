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
        // 回放文件数据结构（现在在 AstrumLogic 中，服务器和客户端共享）
        private BattleReplayFile _replayData;
        
        // 快照索引（按帧号排序，用于二分查找）
        private readonly List<ReplaySnapshot> _snapshots = new();
        
        // 帧输入索引（帧号 -> 输入数据）
        private readonly Dictionary<int, ReplayFrameInputs> _frameInputs = new();

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
        public List<ReplayPlayerInfo> Players => _replayData?.Players ?? new List<ReplayPlayerInfo>();

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
                _replayData = DeserializeReplayFile(fileData);
                
                if (_replayData == null)
                {
                    ASLogger.Instance.Error("ReplayTimeline: 回放文件反序列化失败", "Replay.Timeline");
                    return false;
                }

                // 构建快照索引（按帧号排序）
                _snapshots.Clear();
                _snapshots.AddRange(_replayData.Snapshots);
                _snapshots.Sort((a, b) => a.Frame.CompareTo(b.Frame));

                // 构建帧输入索引
                _frameInputs.Clear();
                foreach (var frameInput in _replayData.FrameInputs)
                {
                    _frameInputs[frameInput.Frame] = frameInput;
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
        public ReplaySnapshot? GetNearestSnapshot(int frame)
        {
            if (_snapshots.Count == 0) return null;

            // 二分查找：找到 frame0 <= frame 的最大快照
            int left = 0;
            int right = _snapshots.Count - 1;
            ReplaySnapshot? result = null;

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
            if (_frameInputs.TryGetValue(frame, out var frameInputs))
            {
                // 反序列化输入数据
                return DeserializeFrameInputs(frameInputs.InputsData);
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
        private BattleReplayFile? DeserializeReplayFile(byte[] data)
        {
            try
            {
                // 直接使用 BattleReplayFile（现在在 AstrumLogic 中，服务器和客户端共享）
                object obj = MemoryPackHelper.Deserialize(typeof(BattleReplayFile), data, 0, data.Length);
                return obj as BattleReplayFile;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ReplayTimeline: 反序列化回放文件失败 - {ex.Message}", "Replay.Timeline");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return null;
            }
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

    }
}

