using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 帧同步输入系统，单例模式
    /// </summary>
    public class LSInputSystem
    {
        private static LSInputSystem? _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 帧缓冲区
        /// </summary>
        public FrameBuffer FrameBuffer { get; private set; }

        /// <summary>
        /// 输入延迟帧数
        /// </summary>
        public int InputDelay { get; set; } = 2;

        /// <summary>
        /// 回滚帧数
        /// </summary>
        public int RollbackFrames { get; set; } = 8;

        /// <summary>
        /// 当前处理的帧号
        /// </summary>
        public int CurrentProcessingFrame { get; private set; } = 0;

        /// <summary>
        /// 预测输入缓存
        /// </summary>
        private Dictionary<int, Dictionary<int, LSInput>> _predictedInputs = new();

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private LSInputSystem()
        {
            FrameBuffer = new FrameBuffer(RollbackFrames * 2);
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        /// <returns>输入系统实例</returns>
        public static LSInputSystem GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new LSInputSystem();
                    }
                }
            }
            return _instance;
        }

        /// <summary>
        /// 收集玩家输入
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="input">输入数据</param>
        public void CollectInput(int playerId, LSInput input)
        {
            if (input == null) return;

            int targetFrame = CurrentProcessingFrame + InputDelay;
            input.Frame = targetFrame;
            input.PlayerId = playerId;

            // 获取或创建目标帧的输入集合
            var frameInputs = FrameBuffer.GetFrame(targetFrame) ?? new OneFrameInputs(targetFrame);
            frameInputs.AddInput(playerId, input);

            // 更新帧缓冲区
            FrameBuffer.AddFrame(targetFrame, frameInputs);
        }

        /// <summary>
        /// 处理单帧
        /// </summary>
        /// <param name="frame">帧号</param>
        public void ProcessFrame(int frame)
        {
            CurrentProcessingFrame = frame;
            
            // 清理过旧的预测输入
            CleanupOldPredictedInputs(frame);
        }

        /// <summary>
        /// 检查是否可执行帧
        /// </summary>
        /// <param name="frame">帧号</param>
        /// <returns>是否可执行</returns>
        public bool CanExecuteFrame(int frame)
        {
            var frameInputs = FrameBuffer.GetFrame(frame);
            return frameInputs != null && frameInputs.IsComplete;
        }

        /// <summary>
        /// 获取预测输入
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="frame">帧号</param>
        /// <returns>预测的输入数据</returns>
        public LSInput GetPredictedInput(int playerId, int frame)
        {
            // 首先尝试从帧缓冲区获取真实输入
            var frameInputs = FrameBuffer.GetFrame(frame);
            var realInput = frameInputs?.GetInput(playerId);
            if (realInput != null)
            {
                return realInput;
            }

            // 检查是否有预测输入
            if (_predictedInputs.TryGetValue(frame, out var framePredictions) &&
                framePredictions.TryGetValue(playerId, out var predictedInput))
            {
                return predictedInput;
            }

            // 生成预测输入（基于历史输入）
            var prediction = GeneratePredictedInput(playerId, frame);
            
            // 缓存预测输入
            if (!_predictedInputs.ContainsKey(frame))
            {
                _predictedInputs[frame] = new Dictionary<int, LSInput>();
            }
            _predictedInputs[frame][playerId] = prediction;

            return prediction;
        }

        /// <summary>
        /// 更新输入系统
        /// </summary>
        public void Update()
        {
            // 更新帧缓冲区的当前帧
            FrameBuffer.CurrentFrame = CurrentProcessingFrame;
            
            // 清理过旧的帧数据
            int keepFrame = CurrentProcessingFrame - RollbackFrames;
            FrameBuffer.RemoveOldFrames(keepFrame);
        }

        /// <summary>
        /// 获取指定帧的输入
        /// </summary>
        /// <param name="frame">帧号</param>
        /// <returns>单帧输入数据</returns>
        public OneFrameInputs? GetFrameInputs(int frame)
        {
            return FrameBuffer.GetFrame(frame);
        }

        /// <summary>
        /// 设置帧为完成状态
        /// </summary>
        /// <param name="frame">帧号</param>
        /// <param name="playerCount">预期玩家数量</param>
        public void MarkFrameComplete(int frame, int playerCount)
        {
            var frameInputs = FrameBuffer.GetFrame(frame);
            if (frameInputs != null)
            {
                frameInputs.IsComplete = frameInputs.HasAllInputs(playerCount);
            }
        }

        /// <summary>
        /// 重置输入系统
        /// </summary>
        public void Reset()
        {
            FrameBuffer.Clear();
            _predictedInputs.Clear();
            CurrentProcessingFrame = 0;
        }

        /// <summary>
        /// 生成预测输入
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="frame">帧号</param>
        /// <returns>预测的输入</returns>
        private LSInput GeneratePredictedInput(int playerId, int frame)
        {
            // 简单的预测策略：使用最近的输入作为预测
            LSInput? lastInput = null;
            
            // 向前查找最近的真实输入
            for (int i = 1; i <= 5; i++)
            {
                int searchFrame = frame - i;
                var searchFrameInputs = FrameBuffer.GetFrame(searchFrame);
                lastInput = searchFrameInputs?.GetInput(playerId);
                if (lastInput != null) break;
            }

            // 如果找不到历史输入，返回空输入
            if (lastInput == null)
            {
                return new LSInput
                {
                    PlayerId = playerId,
                    Frame = frame,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
            }

            // 克隆最后的输入作为预测（可以添加更复杂的预测逻辑）
            var prediction = lastInput.Clone();
            prediction.Frame = frame;
            prediction.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 简单的预测：如果是移动输入，可以保持；如果是技能输入，则清零
            prediction.Attack = false;
            prediction.Skill1 = false;
            prediction.Skill2 = false;

            return prediction;
        }

        /// <summary>
        /// 清理过旧的预测输入
        /// </summary>
        /// <param name="currentFrame">当前帧</param>
        private void CleanupOldPredictedInputs(int currentFrame)
        {
            var framesToRemove = new List<int>();
            
            foreach (var frame in _predictedInputs.Keys)
            {
                if (frame < currentFrame - RollbackFrames)
                {
                    framesToRemove.Add(frame);
                }
            }

            foreach (var frame in framesToRemove)
            {
                _predictedInputs.Remove(frame);
            }
        }
    }
}
