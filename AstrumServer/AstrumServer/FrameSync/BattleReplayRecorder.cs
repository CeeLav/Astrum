using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;

namespace AstrumServer.FrameSync
{
    /// <summary>
    /// 战斗回放录制器 - 绑定到单个房间，对战斗全程进行录制
    /// </summary>
    public sealed class BattleReplayRecorder
    {
        private readonly string _roomId;
        private readonly int _tickRate;
        private readonly long _startTimestamp;
        private readonly int _randomSeed;
        private readonly List<ReplayPlayerInfo> _players;
        
        private readonly List<ReplaySnapshot> _snapshots = new();
        private readonly Dictionary<int, ReplayFrameInputs> _frameInputs = new();
        
        private int _totalFrames = 0;
        private bool _isFinished = false;
        
        // 定时保存相关
        private long _lastSaveTime = 0;
        private const long SAVE_INTERVAL_MS = 5000; // 5秒保存一次
        private string _currentFilePath = "";

        /// <summary>
        /// 构造函数
        /// </summary>
        public BattleReplayRecorder(string roomId, int tickRate, long startTimestamp, int randomSeed, List<ReplayPlayerInfo> players)
        {
            _roomId = roomId;
            _tickRate = tickRate;
            _startTimestamp = startTimestamp;
            _randomSeed = randomSeed;
            _players = players ?? new List<ReplayPlayerInfo>();
            _lastSaveTime = startTimestamp;
            
            // 初始化文件路径
            _currentFilePath = GetReplayFilePath(roomId, startTimestamp);
        }

        /// <summary>
        /// 记录世界快照（来自 StateSnapshotManager）
        /// </summary>
        public void OnWorldSnapshot(int frame, byte[] worldSnapshotData)
        {
            if (_isFinished)
            {
                ASLogger.Instance.Warning($"BattleReplayRecorder: 录制已结束，忽略快照 - 房间: {_roomId}, 帧: {frame}", "Replay.Recorder");
                return;
            }

            try
            {
                // 压缩快照数据（GZip）
                byte[] compressedData = CompressData(worldSnapshotData);
                
                var snapshot = new ReplaySnapshot
                {
                    Frame = frame,
                    WorldData = compressedData
                };
                
                _snapshots.Add(snapshot);
                
                ASLogger.Instance.Debug($"BattleReplayRecorder: 记录快照 - 房间: {_roomId}, 帧: {frame}, 原始大小: {worldSnapshotData.Length} bytes, 压缩后: {compressedData.Length} bytes", "Replay.Recorder");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"BattleReplayRecorder: 记录快照失败 - 房间: {_roomId}, 帧: {frame}, 错误: {ex.Message}", "Replay.Recorder");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 记录帧输入（来自 FrameSyncManager）
        /// </summary>
        public void OnFrameInputs(int frame, OneFrameInputs inputs)
        {
            if (_isFinished)
            {
                ASLogger.Instance.Warning($"BattleReplayRecorder: 录制已结束，忽略输入 - 房间: {_roomId}, 帧: {frame}", "Replay.Recorder");
                return;
            }

            try
            {
                // 序列化 OneFrameInputs（使用 Proto 序列化）
                byte[] inputsData = SerializeFrameInputs(inputs);
                
                var frameInputs = new ReplayFrameInputs
                {
                    Frame = frame,
                    InputsData = inputsData
                };
                
                _frameInputs[frame] = frameInputs;
                _totalFrames = Math.Max(_totalFrames, frame);
                
                // 检查是否需要定时保存（每5秒）
                long currentTime = TimeInfo.Instance.ClientNow();
                if (currentTime - _lastSaveTime >= SAVE_INTERVAL_MS)
                {
                    SaveReplayFilePeriodically();
                    _lastSaveTime = currentTime;
                }
                
                ASLogger.Instance.Debug($"BattleReplayRecorder: 记录帧输入 - 房间: {_roomId}, 帧: {frame}, 输入数: {inputs.Inputs.Count}, 数据大小: {inputsData.Length} bytes", "Replay.Recorder");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"BattleReplayRecorder: 记录帧输入失败 - 房间: {_roomId}, 帧: {frame}, 错误: {ex.Message}", "Replay.Recorder");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 完成录制，生成最终回放文件结构
        /// </summary>
        public BattleReplayFile Finish()
        {
            if (_isFinished)
            {
                ASLogger.Instance.Warning($"BattleReplayRecorder: 录制已结束，无法再次完成 - 房间: {_roomId}", "Replay.Recorder");
                return null;
            }

            try
            {
                _isFinished = true;

                // 最后一次保存
                SaveReplayFilePeriodically();

                // 按帧号排序快照
                _snapshots.Sort((a, b) => a.Frame.CompareTo(b.Frame));

                // 将帧输入转换为列表（按帧号排序）
                var frameInputsList = _frameInputs.Values.OrderBy(x => x.Frame).ToList();

                var replayFile = new BattleReplayFile
                {
                    Version = 1,
                    RoomId = _roomId,
                    TickRate = _tickRate,
                    TotalFrames = _totalFrames,
                    StartTimestamp = _startTimestamp,
                    RandomSeed = _randomSeed,
                    Players = _players,
                    Snapshots = _snapshots,
                    FrameInputs = frameInputsList
                };

                ASLogger.Instance.Info($"BattleReplayRecorder: 录制完成 - 房间: {_roomId}, 总帧数: {_totalFrames}, 快照数: {_snapshots.Count}, 帧输入数: {frameInputsList.Count}", "Replay.Recorder");

                return replayFile;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"BattleReplayRecorder: 完成录制失败 - 房间: {_roomId}, 错误: {ex.Message}", "Replay.Recorder");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// 定时保存回放文件（每5秒）
        /// </summary>
        private void SaveReplayFilePeriodically()
        {
            try
            {
                // 按帧号排序快照
                var sortedSnapshots = new List<ReplaySnapshot>(_snapshots);
                sortedSnapshots.Sort((a, b) => a.Frame.CompareTo(b.Frame));

                // 将帧输入转换为列表（按帧号排序）
                var frameInputsList = _frameInputs.Values.OrderBy(x => x.Frame).ToList();

                var replayFile = new BattleReplayFile
                {
                    Version = 1,
                    RoomId = _roomId,
                    TickRate = _tickRate,
                    TotalFrames = _totalFrames,
                    StartTimestamp = _startTimestamp,
                    RandomSeed = _randomSeed,
                    Players = _players,
                    Snapshots = sortedSnapshots,
                    FrameInputs = frameInputsList
                };

                // 序列化回放文件
                byte[] fileData = MemoryPackHelper.Serialize(replayFile);
                
                // 写入文件（覆盖）
                File.WriteAllBytes(_currentFilePath, fileData);
                
                ASLogger.Instance.Debug($"回放文件已定时保存 - 房间: {_roomId}, 文件: {_currentFilePath}, 大小: {fileData.Length} bytes, 总帧数: {_totalFrames}", "Replay.Save");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"定时保存回放文件失败 - 房间: {_roomId}, 错误: {ex.Message}", "Replay.Save");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 获取回放文件路径
        /// </summary>
        private string GetReplayFilePath(string roomId, long startTimestamp)
        {
            // 查找 AstrumConfig 目录
            string? configPath = null;
            var currentDir = AppContext.BaseDirectory;
            var searchDir = new DirectoryInfo(currentDir);
            const int maxLevels = 10;
            int levels = 0;

            while (searchDir != null && levels < maxLevels)
            {
                var candidatePath = Path.Combine(searchDir.FullName, "AstrumConfig", "Record");
                var parentDir = Path.GetDirectoryName(candidatePath);
                if (parentDir != null && Directory.Exists(parentDir)) // 检查 AstrumConfig 目录是否存在
                {
                    configPath = candidatePath;
                    break;
                }
                searchDir = searchDir.Parent;
                levels++;
            }

            if (string.IsNullOrEmpty(configPath))
            {
                // 如果找不到，使用当前目录下的 AstrumConfig/Record
                configPath = Path.Combine(currentDir, "AstrumConfig", "Record");
            }

            // 创建目录
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }

            // 生成文件名：房间ID_时间戳.replay
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(startTimestamp).ToString("yyyyMMdd_HHmmss");
            var fileName = $"{roomId}_{timestamp}.replay";
            return Path.Combine(configPath, fileName);
        }

        /// <summary>
        /// 压缩数据（GZip）
        /// </summary>
        private byte[] CompressData(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        /// <summary>
        /// 序列化帧输入（使用 Proto 序列化）
        /// </summary>
        private byte[] SerializeFrameInputs(OneFrameInputs inputs)
        {
            // 使用 MemoryPack 序列化（与现有系统保持一致）
            return MemoryPackHelper.Serialize(inputs);
        }
    }
}

