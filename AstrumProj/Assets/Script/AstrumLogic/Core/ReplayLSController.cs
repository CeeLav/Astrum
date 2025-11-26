using System;
using System.IO;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.FrameSync
{
    /// <summary>
    /// 回放专用的逻辑控制器
    /// 负责基于本地时间推进逻辑帧，不进行预测或回滚
    /// </summary>
    public class ReplayLSController : ILSControllerBase
    {
        private float _replayElapsedTime = 0f;
        
        // ILSControllerBase 接口实现
        public int TickRate { get; set; } = 20;
        public long CreationTime { get; set; }
        public int AuthorityFrame { get; set; } = 0;
        public int PredictionFrame { get; set; } = 0; // 回放不需要预测，保持为0或与AuthorityFrame一致
        public bool IsRunning { get; set; } = false;
        public bool IsPaused { get; set; } = false;
        public Room Room { get; set; }
        public int MaxPredictionWindow { get; set; } = 0; // 不使用预测窗口

        private FrameBuffer _frameBuffer;
        public FrameBuffer FrameBuffer => _frameBuffer;
        
        /// <summary>
        /// 输入提供者，用于在缺少输入时获取输入（通常由 ReplayGameMode 提供）
        /// </summary>
        public Func<int, OneFrameInputs> InputProvider { get; set; }

        /// <summary>
        /// 快照信息提供者，返回 (帧号, 快照数据)
        /// </summary>
        public Func<int, (int, byte[])> NearestSnapshotProvider { get; set; }

        public ReplayLSController()
        {
            _frameBuffer = new FrameBuffer(128, 4096); // 这里的 buffer size 可能需要根据回放长度调整，或者动态扩容
        }

        public void Start()
        {
            IsRunning = true;
            IsPaused = false;
            _replayElapsedTime = 0f;
            AuthorityFrame = 0;
            ASLogger.Instance.Info("ReplayLSController: Started", "Replay.Controller");
        }

        public void Stop()
        {
            IsRunning = false;
            IsPaused = false;
            ASLogger.Instance.Info("ReplayLSController: Stopped", "Replay.Controller");
        }

        /// <summary>
        /// 保存当前帧状态 (空实现)
        /// </summary>
        public void SaveState()
        {
            // 回放模式不使用 FrameBuffer 保存运行时状态
        }

        /// <summary>
        /// 加载指定帧的状态 (空实现)
        /// </summary>
        public World LoadState(int frame)
        {
            // 回放模式不使用 FrameBuffer 加载运行时状态
            return null;
        }

        /// <summary>
        /// 加载快照数据到 FrameBuffer (空实现)
        /// </summary>
        public void LoadSnapshot(int frame, byte[] snapshotData)
        {
            // 回放模式不使用 FrameBuffer 保存运行时状态
        }

        /// <summary>
        /// 回滚到指定帧（仅支持文件快照回滚）
        /// </summary>
        private bool Rollback(int targetFrame)
        {
            if (NearestSnapshotProvider == null)
            {
                ASLogger.Instance.Error("ReplayLSController: 无法回滚，NearestSnapshotProvider 未设置", "Replay.Controller");
                return false;
            }

            var (frame, data) = NearestSnapshotProvider(targetFrame);
            if (frame >= 0 && frame <= targetFrame && data != null)
            {
                return RollbackToFileSnapshot(frame, data, targetFrame);
            }

            ASLogger.Instance.Error($"ReplayLSController: 无法回滚，找不到 <= {targetFrame} 的有效文件快照", "Replay.Controller");
            return false;
        }

        private bool RollbackToFileSnapshot(int snapshotFrame, byte[] data, int targetFrame)
        {
            try
            {
                using (var memoryBuffer = new MemoryBuffer(data.Length))
                {
                    memoryBuffer.SetLength(0);
                    memoryBuffer.Seek(0, SeekOrigin.Begin);
                    memoryBuffer.Write(data, 0, data.Length);
                    memoryBuffer.Seek(0, SeekOrigin.Begin);
                    
                    var world = MemoryPackHelper.Deserialize(typeof(World), memoryBuffer) as World;
                    if (world != null)
                    {
                        ApplyWorldState(world, snapshotFrame);
                        ASLogger.Instance.Info($"ReplayLSController: 已从文件快照回滚到帧 {snapshotFrame} (目标: {targetFrame})", "Replay.Controller");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ReplayLSController: 文件快照回滚失败 - {ex.Message}", "Replay.Controller");
            }
            return false;
        }

        private void ApplyWorldState(World world, int frame)
        {
            Room.MainWorld = world;
            Room.MainWorld.RoomId = Room.RoomId;
            AuthorityFrame = frame;
            _replayElapsedTime = frame / (float)TickRate;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning || IsPaused || Room == null) return;

            _replayElapsedTime += deltaTime;
            
            // 计算期望到达的帧：floor(elapsedTime * TickRate)
            int expectedFrame = (int)(_replayElapsedTime * TickRate);
            
            // 推进到期望帧（单次最多推进若干帧，避免卡顿）
            int steps = 0;
            const int MAX_FRAMES_PER_TICK = 5; // 限制单次更新帧数
            
            while (AuthorityFrame < expectedFrame && steps < MAX_FRAMES_PER_TICK)
            {
                int nextFrame = AuthorityFrame + 1;
                
                // 从 InputProvider 获取输入 (Timeline)
                // 注意：FrameBuffer 可能会返回复用的非空对象，所以不能依赖 _frameBuffer.FrameInputs(nextFrame) == null 来判断
                // 必须直接从数据源获取
                OneFrameInputs inputs = null;
                if (InputProvider != null)
                {
                    inputs = InputProvider(nextFrame);
                }
                
                if (inputs != null)
                {
                    AuthorityFrame = nextFrame;
                    Room.FrameTick(inputs);
                    
                    // 回放模式不使用 FrameBuffer 保存运行时状态
                }
                else
                {
                    // 没有更多输入，可能是回放结束
                    // ASLogger.Instance.Debug($"ReplayLSController: 帧 {nextFrame} 无输入，停止推进", "Replay.Controller");
                    break;
                }
                
                steps++;
            }
        }
        
        public void Tick()
        {
            ASLogger.Instance.Warning("ReplayLSController: 请使用 Tick(float deltaTime) 方法，而不是无参 Tick()", "Replay.Controller");
        }

        /// <summary>
        /// 快速推进到指定帧（用于跳转）
        /// </summary>
        public void FastForwardTo(int targetFrame, Func<int, OneFrameInputs> getFrameInputs = null)
        {
            // 使用传入的 provider 或默认属性
            var provider = getFrameInputs ?? InputProvider;
            
            if (provider == null)
            {
                ASLogger.Instance.Error("ReplayLSController: FastForwardTo 需要提供 getFrameInputs 回调或设置 InputProvider", "Replay.Controller");
                return;
            }

            // 1. 如果目标帧小于当前帧，需要回滚
            if (targetFrame < AuthorityFrame)
            {
                if (!Rollback(targetFrame))
                {
                    return; // 回滚失败
                }
                // 回滚后 AuthorityFrame 会变小，继续执行下面的推进逻辑
            }

            // 2. 快速推进到目标帧（关闭中间渲染）
            int startFrame = AuthorityFrame + 1;
            
            for (int frame = startFrame; frame <= targetFrame; frame++)
            {
                // 直接从 Provider 获取输入
                var inputs = provider(frame);
                
                if (inputs != null)
                {
                    AuthorityFrame = frame;
                    
                    // 执行逻辑
                    Room.FrameTick(inputs);
                    
                    // 回放模式不使用 FrameBuffer 保存运行时状态
                }
                else
                {
                    // 没有该帧输入，停止推进
                    ASLogger.Instance.Warning($"ReplayLSController: 帧 {frame} 没有输入数据，停止推进", "Replay.Controller");
                    break;
                }
            }

            // 3. 更新回放时间，使其与当前帧同步
            _replayElapsedTime = AuthorityFrame / (float)TickRate;
        }
    }
}
