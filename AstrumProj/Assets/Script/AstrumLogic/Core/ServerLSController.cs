using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 服务器帧同步控制器，负责管理服务器权威帧同步逻辑
    /// </summary>
    public class ServerLSController : ILSControllerBase, IServerFrameSync
    {
        /// <summary>
        /// 所属房间
        /// </summary>
        public Room Room { get; set; }

        /// <summary>
        /// 权威帧
        /// </summary>
        public int AuthorityFrame { get; set; } = -1;

        /// <summary>
        /// 帧缓冲区
        /// </summary>
        private readonly FrameBuffer _frameBuffer = new FrameBuffer();

        public FrameBuffer FrameBuffer => _frameBuffer;

        /// <summary>
        /// 帧率（如60FPS）
        /// </summary>
        public int TickRate { get; set; } = 60;
        
        /// <summary>
        /// 创建时间（毫秒）
        /// </summary>
        public long CreationTime { get; set; }

        /// <summary>
        /// 上次更新时间
        /// </summary>
        public long LastUpdateTime { get; private set; }

        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPaused { get; set; } = false;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; } = false;

        /// <summary>
        /// 帧输入缓存（帧号 -> 玩家ID -> 输入）
        /// </summary>
        private readonly Dictionary<int, Dictionary<long, LSInput>> _frameInputs = new();

        /// <summary>
        /// 曾经上报过的玩家ID集合（用于保证所有玩家都有输入条目）
        /// </summary>
        private readonly HashSet<long> _uploadedPlayerIds = new();

        /// <summary>
        /// 最大缓存帧数
        /// </summary>
        private const int MAX_CACHE_FRAMES = 10;

        public ServerLSController()
        {
            LastUpdateTime = TimeInfo.Instance.ServerNow();
        }

        /// <summary>
        /// 服务器权威帧更新（统一使用 Tick() 方法）
        /// </summary>
        public void Tick()
        {
            if (!IsRunning || IsPaused || Room == null) return;
            
            // 检查是否到达下一帧时间
            long currentTime = TimeInfo.Instance.ServerNow();
            long targetFrameTime = CreationTime + (AuthorityFrame + 1) * LSConstValue.UpdateInterval;
            
            if (currentTime < targetFrameTime)
            {
                return; // 还没到下一帧时间
            }
            
            // 推进权威帧
            AuthorityFrame++;
            
            // 收集当前帧的所有输入
            var frameInputs = CollectFrameInputs(AuthorityFrame);
            
            // 确保 FrameBuffer 已准备好
            FrameBuffer.MoveForward(AuthorityFrame);
            
            // 执行逻辑
            Room.FrameTick(frameInputs);
            
            // 更新最后更新时间
            LastUpdateTime = currentTime;
        }

        /// <summary>
        /// 添加玩家输入到缓存
        /// </summary>
        public void AddPlayerInput(int frame, long playerId, LSInput input)
        {
            // 登记曾经上报过的非零玩家ID
            if (playerId != 0)
            {
                _uploadedPlayerIds.Add(playerId);
            }
            
            // 如果输入帧号已经过了，使用服务器的当前帧号
            if (frame < AuthorityFrame + 1)
            {
                ASLogger.Instance.Debug($"输入帧号 {frame} 已过期，使用服务器当前帧号 {AuthorityFrame + 1}，玩家: {playerId}");
                frame = AuthorityFrame + 1;
            }
            
            // 如果输入帧号比服务器当前帧晚太多，限制在合理范围内
            if (frame > AuthorityFrame + MAX_CACHE_FRAMES)
            {
                ASLogger.Instance.Warning($"输入帧号 {frame} 过于超前，限制为 {AuthorityFrame + MAX_CACHE_FRAMES}，玩家: {playerId}");
                frame = AuthorityFrame + MAX_CACHE_FRAMES;
            }
            
            if (!_frameInputs.ContainsKey(frame))
            {
                _frameInputs[frame] = new Dictionary<long, LSInput>();
            }
            
            _frameInputs[frame][playerId] = input;
            
            // 定期清理过期缓存
            CleanupExpiredCache();
        }

        /// <summary>
        /// 收集指定帧的所有玩家输入（从输入缓存中）
        /// </summary>
        public OneFrameInputs CollectFrameInputs(int frame)
        {
            var frameInputs = OneFrameInputs.Create();
            
            // 优先依据历史上报过的玩家ID进行下发（保证所有曾经上报过的玩家都有条目）
            if (_uploadedPlayerIds.Count > 0)
            {
                var hadInputs = _frameInputs.TryGetValue(frame, out var inputsThisFrame) ? inputsThisFrame : null;
                foreach (var playerId in _uploadedPlayerIds.OrderBy(x => x))
                {
                    if (playerId == 0) continue; // 保险过滤
                    if (hadInputs != null && hadInputs.TryGetValue(playerId, out var actual))
                    {
                        frameInputs.Inputs[playerId] = actual;
                    }
                    else
                    {
                        // 为本帧未上报的历史玩家使用上一帧的输入
                        var previousFrameInput = GetPreviousFrameInput(playerId, frame);
                        frameInputs.Inputs[playerId] = previousFrameInput;
                        ASLogger.Instance.Debug($"玩家 {playerId} 在帧 {frame} 未上报，使用上一帧输入");
                    }
                }
            }
            else
            {
                // 如果还没有历史上报ID，则仅收集本帧实际收到的有效输入（排除0）
                if (_frameInputs.TryGetValue(frame, out var inputs))
                {
                    foreach (var kvp in inputs)
                    {
                        var playerId = kvp.Key;
                        var input = kvp.Value;
                        if (playerId != 0)
                        {
                            frameInputs.Inputs[playerId] = input;
                        }
                    }
                }
            }
            
            return frameInputs;
        }

        /// <summary>
        /// 获取上一帧的输入
        /// </summary>
        private LSInput GetPreviousFrameInput(long playerId, int currentFrame)
        {
            // 从当前帧往前查找，直到找到该玩家的输入
            for (int frame = currentFrame - 1; frame >= Math.Max(0, currentFrame - MAX_CACHE_FRAMES); frame--)
            {
                if (_frameInputs.TryGetValue(frame, out var inputs) && inputs.TryGetValue(playerId, out var input))
                {
                    return input;
                }
            }
            
            // 如果找不到，创建默认输入
            return CreateDefaultInput(playerId, currentFrame);
        }

        /// <summary>
        /// 创建默认的空输入
        /// </summary>
        private LSInput CreateDefaultInput(long playerId, int frame)
        {
            var defaultInput = LSInput.Create();
            defaultInput.PlayerId = playerId;
            defaultInput.Frame = frame;
            return defaultInput;
        }

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        private void CleanupExpiredCache()
        {
            // 只保留最近 MAX_CACHE_FRAMES 帧的缓存
            var framesToRemove = _frameInputs.Keys.Where(f => f < AuthorityFrame - MAX_CACHE_FRAMES).ToList();
            foreach (var frame in framesToRemove)
            {
                _frameInputs.Remove(frame);
            }
        }

        /// <summary>
        /// 保存状态
        /// </summary>
        public void SaveState()
        {
            int frame = AuthorityFrame;
            
            if (frame > FrameBuffer.MaxFrame)
            {
                while (FrameBuffer.MaxFrame < frame)
                {
                    FrameBuffer.MoveForward(FrameBuffer.MaxFrame);
                }
            }
            
            var memoryBuffer = FrameBuffer.Snapshot(frame);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            memoryBuffer.SetLength(0);
            
            if (Room?.MainWorld != null)
            {
                var world = Room.MainWorld;
                ASLogger.Instance.Debug($"保存帧状态 - 帧: {frame}, World ID: {world.WorldId}, World Frame:{world.CurFrame} 实体数量: {world.Entities?.Count ?? 0}", "FrameSync.SaveState");
            }
            
            MemoryPackHelper.Serialize(Room.MainWorld, memoryBuffer);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            long hash = memoryBuffer.GetBuffer().Hash(0, (int)memoryBuffer.Length);
            FrameBuffer.SetHash(frame, hash);
            
            ASLogger.Instance.Debug($"帧状态保存完成 - 帧: {frame}, 数据大小: {memoryBuffer.Length} bytes, 哈希: {hash}", "FrameSync.SaveState");
        }

        /// <summary>
        /// 加载状态
        /// </summary>
        public World LoadState(int frame)
        {
            var memoryBuffer = FrameBuffer.Snapshot(frame);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            
            ASLogger.Instance.Debug($"加载帧状态 - 帧: {frame}, 数据大小: {memoryBuffer.Length} bytes", "FrameSync.LoadState");
            
            if (memoryBuffer.Length == 0)
            {
                ASLogger.Instance.Warning($"帧状态快照为空 - 帧: {frame}, 无法加载状态", "FrameSync.LoadState");
                return null;
            }
            
            try
            {
                World world = MemoryPackHelper.Deserialize(typeof(World), memoryBuffer) as World;
                memoryBuffer.Seek(0, SeekOrigin.Begin);
                
                if (world != null)
                {
                    ASLogger.Instance.Debug($"帧状态加载完成 - 帧: {frame}, World ID: {world.WorldId}, 实体数量: {world.Entities?.Count ?? 0}", "FrameSync.LoadState");
                }
                else
                {
                    ASLogger.Instance.Warning($"帧状态加载失败 - 帧: {frame}, 反序列化结果为 null", "FrameSync.LoadState");
                }
                
                return world;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"帧状态反序列化失败 - 帧: {frame}, 错误: {ex.Message}", "FrameSync.LoadState");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                memoryBuffer.Seek(0, SeekOrigin.Begin);
                return null;
            }
        }

        /// <summary>
        /// 开始控制器
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            IsPaused = false;
            AuthorityFrame = 0;
            LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 停止控制器
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            IsPaused = false;
        }
    }
}

