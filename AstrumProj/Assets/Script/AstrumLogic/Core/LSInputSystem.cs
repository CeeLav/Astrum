using System;
using System.Collections.Generic;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 帧同步输入系统
    /// </summary>
    public class LSInputSystem
    {
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
        private Dictionary<int, Dictionary<long, LSInput>> _predictedInputs = new();

        /// <summary>
        /// 私有构造函数
        /// </summary>
        public LSInputSystem()
        {
            FrameBuffer = new FrameBuffer();
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
        /// 重置输入系统
        /// </summary>
        public void Reset()
        {
            //FrameBuffer.Clear();
            _predictedInputs.Clear();
            CurrentProcessingFrame = 0;
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
