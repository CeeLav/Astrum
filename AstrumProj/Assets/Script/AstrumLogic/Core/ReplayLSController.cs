using System;
using System.IO;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 回放专用帧同步控制器 - 专门用于回放场景，无需预测/RTT补偿/回滚
    /// </summary>
    public class ReplayLSController : ILSControllerBase
    {
        /// <summary>
        /// 所属房间
        /// </summary>
        public Room Room { get; set; }

        /// <summary>
        /// 权威帧（回放中的当前帧）
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
        /// 是否暂停
        /// </summary>
        public bool IsPaused { get; set; } = false;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; } = false;

        /// <summary>
        /// 回放本地时间（秒），通过 deltaTime 递增，不依赖 TimeInfo
        /// </summary>
        private float _replayElapsedTime = 0f;

        /// <summary>
        /// 单次最多推进帧数（避免卡顿）
        /// </summary>
        private const int MAX_FRAMES_PER_TICK = 5;

        public ReplayLSController()
        {
            CreationTime = TimeInfo.Instance.ClientNow();
        }

        /// <summary>
        /// 更新回放（传入 deltaTime，基于本地时间推进）
        /// 注意：ILSControllerBase 接口要求无参 Tick()，这里提供一个重载方法
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsRunning || IsPaused || Room == null) return;

            // 累计播放时间
            _replayElapsedTime += deltaTime;

            // 计算期望到达的帧：floor(elapsedTime * TickRate)
            int expectedFrame = (int)(_replayElapsedTime * TickRate);

            // 推进到期望帧（单次最多推进若干帧，避免卡顿）
            int steps = 0;
            while (AuthorityFrame < expectedFrame && steps < MAX_FRAMES_PER_TICK)
            {
                int nextFrame = AuthorityFrame + 1;

                // 从回放文件获取该帧输入（由 ReplayGameMode 提前设置到 FrameBuffer）
                OneFrameInputs inputs = _frameBuffer.FrameInputs(nextFrame);
                if (inputs != null)
                {
                    AuthorityFrame = nextFrame;
                    Room.FrameTick(inputs);
                }
                else
                {
                    // 没有更多输入，停止推进
                    break;
                }
                steps++;
            }
        }

        /// <summary>
        /// 实现 ILSControllerBase 接口的 Tick() 方法
        /// 回放场景下，这个方法应该由 ReplayGameMode 调用 Tick(float deltaTime) 重载
        /// </summary>
        public void Tick()
        {
            // 回放场景下，应该使用 Tick(float deltaTime) 重载
            // 这里提供一个默认实现，但建议直接调用 Tick(float deltaTime)
            ASLogger.Instance.Warning("ReplayLSController: 请使用 Tick(float deltaTime) 方法，而不是无参 Tick()", "Replay.Controller");
        }

        /// <summary>
        /// 设置当前帧的输入（从回放文件读取）
        /// </summary>
        public void SetFrameInputs(int frame, OneFrameInputs inputs)
        {
            if (inputs == null) return;

            // 确保 FrameBuffer 已准备好该帧
            _frameBuffer.MoveForward(frame);
            
            // 将输入设置到 FrameBuffer
            var frameInputs = _frameBuffer.FrameInputs(frame);
            if (frameInputs != null)
            {
                inputs.CopyTo(frameInputs);
            }
        }

        /// <summary>
        /// 快速推进到指定帧（用于跳转）
        /// </summary>
        public void FastForwardTo(int targetFrame, Func<int, OneFrameInputs> getFrameInputs)
        {
            if (getFrameInputs == null)
            {
                ASLogger.Instance.Error("ReplayLSController: FastForwardTo 需要提供 getFrameInputs 回调", "Replay.Controller");
                return;
            }

            // 1. 查找目标帧之前最近的快照（快照保存的是该帧输入运算前的状态）
            // 注意：这里需要从 FrameBuffer 中查找快照，或者由调用者提供快照加载逻辑
            // 简化处理：如果当前帧小于目标帧，直接推进；如果大于，需要回退（不支持）
            
            if (AuthorityFrame > targetFrame)
            {
                ASLogger.Instance.Warning($"ReplayLSController: 无法回退，当前帧 {AuthorityFrame} > 目标帧 {targetFrame}，需要重新加载快照", "Replay.Controller");
                return;
            }

            // 2. 快速推进到目标帧（关闭中间渲染）
            for (int frame = AuthorityFrame + 1; frame <= targetFrame; frame++)
            {
                var inputs = getFrameInputs(frame);
                if (inputs != null)
                {
                    // 设置输入到 FrameBuffer
                    SetFrameInputs(frame, inputs);
                    AuthorityFrame = frame;
                    
                    // 执行逻辑（可选：跳过视图更新，仅在最后同步一次）
                    Room.FrameTick(inputs);
                }
                else
                {
                    // 没有该帧输入，停止推进
                    ASLogger.Instance.Warning($"ReplayLSController: 帧 {frame} 没有输入数据，停止推进", "Replay.Controller");
                    break;
                }
            }

            // 3. 更新回放时间，使其与当前帧同步
            _replayElapsedTime = targetFrame / (float)TickRate;
        }

        /// <summary>
        /// 启动控制器
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            IsPaused = false;
            AuthorityFrame = 0;
            _replayElapsedTime = 0f;
        }

        /// <summary>
        /// 停止控制器
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            IsPaused = false;
        }

        /// <summary>
        /// 保存当前帧状态
        /// </summary>
        public void SaveState()
        {
            if (Room == null || Room.MainWorld == null) return;

            int frame = AuthorityFrame;
            if (frame < 0) return;

            // 确保 FrameBuffer 已经准备好当前帧
            if (frame > _frameBuffer.MaxFrame)
            {
                while (_frameBuffer.MaxFrame < frame)
                {
                    _frameBuffer.MoveForward(_frameBuffer.MaxFrame);
                }
            }

            var memoryBuffer = _frameBuffer.Snapshot(frame);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            memoryBuffer.SetLength(0);

            MemoryPackHelper.Serialize(Room.MainWorld, memoryBuffer);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            
            // 计算哈希值用于验证（使用 GetBuffer() 获取底层 byte[]）
            long hash = memoryBuffer.GetBuffer().Hash(0, (int)memoryBuffer.Length);
            _frameBuffer.SetHash(frame, hash);
        }

        /// <summary>
        /// 加载指定帧的状态
        /// </summary>
        public World LoadState(int frame)
        {
            var memoryBuffer = _frameBuffer.Snapshot(frame);
            memoryBuffer.Seek(0, SeekOrigin.Begin);

            if (memoryBuffer.Length == 0)
            {
                ASLogger.Instance.Warning($"ReplayLSController: 帧状态快照为空 - 帧: {frame}，无法加载状态", "Replay.Controller");
                return null;
            }

            try
            {
                World world = MemoryPackHelper.Deserialize(typeof(World), memoryBuffer) as World;
                memoryBuffer.Seek(0, SeekOrigin.Begin);

                if (world != null)
                {
                    ASLogger.Instance.Debug($"ReplayLSController: 帧状态加载完成 - 帧: {frame}, World ID: {world.WorldId}, 实体数量: {world.Entities?.Count ?? 0}", "Replay.Controller");
                }
                else
                {
                    ASLogger.Instance.Warning($"ReplayLSController: 帧状态加载失败 - 帧: {frame}，反序列化结果为 null", "Replay.Controller");
                }

                return world;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ReplayLSController: 帧状态反序列化失败 - 帧: {frame}, 错误: {ex.Message}", "Replay.Controller");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                memoryBuffer.Seek(0, SeekOrigin.Begin);
                return null;
            }
        }
    }
}

