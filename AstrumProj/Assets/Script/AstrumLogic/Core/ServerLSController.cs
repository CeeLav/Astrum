using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.FrameSync;
using cfg;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 服务器帧同步控制器，负责管理服务器权威帧同步逻辑
    /// </summary>
    public class ServerLSController : ILSControllerBase
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
        /// 最大缓存帧数
        /// </summary>
        private const int MAX_CACHE_FRAMES = 10;

        /// <summary>
        /// 帧同步配置（使用 LSConstValue）
        /// </summary>
        private const int MAX_ADVANCE_PER_UPDATE = 5; // 单次Update最多补帧数，防止雪崩

        /// <summary>
        /// 每帧推进后的回调（用于发送帧数据等）
        /// </summary>
        public Action<int, OneFrameInputs> OnFrameProcessed { get; set; }

        public ServerLSController()
        {
            LastUpdateTime = TimeInfo.Instance.ServerNow();
        }

        /// <summary>
        /// 服务器权威帧更新（统一使用 Tick() 方法）
        /// 基于时间计算期望帧，并推进到期望帧（最多推进 MAX_ADVANCE_PER_UPDATE 帧）
        /// </summary>
        public void Tick()
        {
            if (!IsRunning || IsPaused || Room == null) return;
            
            var now = TimeInfo.Instance.ServerNow();
            
            // 目标应到达的帧 = floor((now - CreationTime) / interval)
            var elapsed = now - CreationTime;
            if (elapsed < 0) return;
            var expectedFrame = (int)(elapsed / LSConstValue.UpdateInterval);
            
            // 将 AuthorityFrame 补到 expectedFrame，单次最多推进若干帧
            var steps = 0;
            while (AuthorityFrame < expectedFrame && steps < MAX_ADVANCE_PER_UPDATE)
            {
                // 推进权威帧
                AuthorityFrame++;
                
                // 收集当前帧的所有输入
                var frameInputs = CollectFrameInputs(AuthorityFrame);
                
                // 确保 FrameBuffer 已准备好
                FrameBuffer.MoveForward(AuthorityFrame);
                SaveState();
                // 执行逻辑
                Room.FrameTick(frameInputs);
                
                // 调用回调（用于发送帧数据等）
                OnFrameProcessed?.Invoke(AuthorityFrame, frameInputs);
                
                steps++;
            }
            
            // 更新最后更新时间
            LastUpdateTime = now;
        }

        /// <summary>
        /// 添加玩家输入到缓存
        /// </summary>
        public void AddPlayerInput(int frame, long playerId, LSInput input)
        {
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
        /// 保证房间内所有玩家都有输入：有当前帧输入用当前帧，没有用上一帧，都没有用默认输入
        /// </summary>
        public OneFrameInputs CollectFrameInputs(int frame)
        {
            var frameInputs = OneFrameInputs.Create();
            
            // 获取房间内所有玩家（通过Archetype查询）
            var players = Room?.GetEntitiesByArchetype(cfg.EArchetype.Role);
            if (players == null || !players.Any())
            {
                return frameInputs;
            }
            
            // 获取当前帧的输入缓存
            var currentFrameInputs = _frameInputs.TryGetValue(frame, out var inputsThisFrame) ? inputsThisFrame : null;
            
            // 遍历房间内所有玩家，确保每个玩家都有输入
            foreach (var player in players)
            {
                var playerId = player.UniqueId;
                if (playerId <= 0) continue; // 过滤无效ID
                
                LSInput input = null;
                
                // 1. 优先使用当前帧的输入
                if (currentFrameInputs != null && currentFrameInputs.TryGetValue(playerId, out input))
                {
                    frameInputs.Inputs[playerId] = input;
                }
                // 2. 如果没有当前帧输入，使用上一帧的输入
                else if ((input = GetPreviousFrameInput(playerId, frame)) != null)
                {
                    frameInputs.Inputs[playerId] = input;
                    ASLogger.Instance.Debug($"玩家 {playerId} 在帧 {frame} 未上报，使用上一帧输入");
                }
                // 3. 如果都没有，使用默认输入
                else
                {
                    frameInputs.Inputs[playerId] = CreateDefaultInput(playerId, frame);
                    ASLogger.Instance.Debug($"玩家 {playerId} 在帧 {frame} 未上报且无历史输入，使用默认输入");
                }
            }
            
            return frameInputs;
        }

        /// <summary>
        /// 获取上一帧的输入（如果找不到则返回 null）
        /// </summary>
        private LSInput? GetPreviousFrameInput(long playerId, int currentFrame)
        {
            // 从当前帧往前查找，直到找到该玩家的输入
            for (int frame = currentFrame - 1; frame >= Math.Max(0, currentFrame - MAX_CACHE_FRAMES); frame--)
            {
                if (_frameInputs.TryGetValue(frame, out var inputs) && inputs.TryGetValue(playerId, out var input))
                {
                    return input;
                }
            }
            
            // 如果找不到，返回 null
            return null;
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
            
            // 检查帧号是否有效
            if (frame < 0)
            {
                ASLogger.Instance.Warning($"ServerLSController.SaveState: 帧号无效 (AuthorityFrame: {AuthorityFrame})，跳过保存状态", "FrameSync.SaveState");
                return;
            }
            
            // 确保 FrameBuffer 已经准备好当前帧
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
        /// 获取快照数据的字节数组（用于网络传输）
        /// </summary>
        public byte[] GetSnapshotBytes(int frame)
        {
            var memoryBuffer = FrameBuffer.Snapshot(frame);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            
            if (memoryBuffer.Length == 0)
            {
                ASLogger.Instance.Warning($"GetSnapshotBytes: 帧 {frame} 快照数据为空", "FrameSync.GetSnapshot");
                return null;
            }
            
            // 使用与 SaveState 中计算哈希相同的方式获取数据
            // 参考 SaveState: memoryBuffer.GetBuffer().Hash(0, (int)memoryBuffer.Length)
            byte[] buffer = memoryBuffer.GetBuffer();
            if (buffer == null)
            {
                ASLogger.Instance.Error($"GetSnapshotBytes: 帧 {frame} 缓冲区为空", "FrameSync.GetSnapshot");
                return null;
            }
            
            // 验证缓冲区大小
            if (buffer.Length < memoryBuffer.Length)
            {
                ASLogger.Instance.Error($"GetSnapshotBytes: 帧 {frame} 缓冲区大小不足 - Buffer: {buffer.Length}, Length: {memoryBuffer.Length}", "FrameSync.GetSnapshot");
                return null;
            }
            
            // 复制实际数据（从位置 0 开始，长度为 Length）
            byte[] snapshotData = new byte[memoryBuffer.Length];
            Array.Copy(buffer, 0, snapshotData, 0, (int)memoryBuffer.Length);
            
            // 计算哈希值用于验证
            long hash = buffer.Hash(0, (int)memoryBuffer.Length);
            long storedHash = FrameBuffer.GetHash(frame);
            
            ASLogger.Instance.Debug($"GetSnapshotBytes: 帧 {frame}, 数据大小: {snapshotData.Length} bytes, 计算哈希: {hash}, 存储哈希: {storedHash}", "FrameSync.GetSnapshot");
            
            // 验证哈希值是否匹配
            if (hash != storedHash && storedHash != 0)
            {
                ASLogger.Instance.Warning($"GetSnapshotBytes: 帧 {frame} 哈希值不匹配 - 计算: {hash}, 存储: {storedHash}", "FrameSync.GetSnapshot");
            }
            
            return snapshotData;
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

