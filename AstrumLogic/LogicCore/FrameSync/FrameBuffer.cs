using System.Collections.Generic;

namespace Astrum.LogicCore.FrameSync
{
    /// <summary>
    /// 帧缓冲区，用于存储和管理帧数据
    /// </summary>
    public class FrameBuffer
    {
        /// <summary>
        /// 最大缓存帧数
        /// </summary>
        public int MaxSize { get; set; } = 60;

        /// <summary>
        /// 帧号 -> 单帧输入的字典
        /// </summary>
        public Dictionary<int, OneFrameInputs> Frames { get; private set; } = new Dictionary<int, OneFrameInputs>();

        /// <summary>
        /// 当前帧
        /// </summary>
        public int CurrentFrame { get; set; } = 0;

        /// <summary>
        /// 已确认帧
        /// </summary>
        public int ConfirmedFrame { get; set; } = -1;

        public FrameBuffer() { }

        public FrameBuffer(int maxSize)
        {
            MaxSize = maxSize;
        }

        /// <summary>
        /// 添加帧数据
        /// </summary>
        /// <param name="frame">帧号</param>
        /// <param name="inputs">单帧输入</param>
        public void AddFrame(int frame, OneFrameInputs inputs)
        {
            if (inputs != null)
            {
                inputs.Frame = frame;
                Frames[frame] = inputs;
                
                // 清理过旧的帧数据
                CleanupOldFrames();
            }
        }

        /// <summary>
        /// 获取帧数据
        /// </summary>
        /// <param name="frame">帧号</param>
        /// <returns>单帧输入，如果不存在返回null</returns>
        public OneFrameInputs? GetFrame(int frame)
        {
            return Frames.TryGetValue(frame, out var inputs) ? inputs : null;
        }

        /// <summary>
        /// 获取帧范围
        /// </summary>
        /// <param name="start">起始帧号</param>
        /// <param name="end">结束帧号</param>
        /// <returns>帧数据列表</returns>
        public List<OneFrameInputs> GetFrameRange(int start, int end)
        {
            var result = new List<OneFrameInputs>();
            
            for (int frame = start; frame <= end; frame++)
            {
                var frameInputs = GetFrame(frame);
                if (frameInputs != null)
                {
                    result.Add(frameInputs);
                }
            }
            
            return result;
        }

        /// <summary>
        /// 清理旧帧数据
        /// </summary>
        /// <param name="keepFrame">保留到此帧号</param>
        public void RemoveOldFrames(int keepFrame)
        {
            var framesToRemove = new List<int>();
            
            foreach (var frame in Frames.Keys)
            {
                if (frame < keepFrame)
                {
                    framesToRemove.Add(frame);
                }
            }
            
            foreach (var frame in framesToRemove)
            {
                Frames.Remove(frame);
            }
        }

        /// <summary>
        /// 检查是否有指定帧
        /// </summary>
        /// <param name="frame">帧号</param>
        /// <returns>是否存在该帧</returns>
        public bool HasFrame(int frame)
        {
            return Frames.ContainsKey(frame);
        }

        /// <summary>
        /// 获取最早的帧号
        /// </summary>
        /// <returns>最早的帧号，如果没有帧则返回-1</returns>
        public int GetEarliestFrame()
        {
            if (Frames.Count == 0) return -1;
            
            int earliest = int.MaxValue;
            foreach (var frame in Frames.Keys)
            {
                if (frame < earliest)
                    earliest = frame;
            }
            return earliest;
        }

        /// <summary>
        /// 获取最晚的帧号
        /// </summary>
        /// <returns>最晚的帧号，如果没有帧则返回-1</returns>
        public int GetLatestFrame()
        {
            if (Frames.Count == 0) return -1;
            
            int latest = int.MinValue;
            foreach (var frame in Frames.Keys)
            {
                if (frame > latest)
                    latest = frame;
            }
            return latest;
        }

        /// <summary>
        /// 获取缓存的帧数量
        /// </summary>
        /// <returns>帧数量</returns>
        public int GetFrameCount()
        {
            return Frames.Count;
        }

        /// <summary>
        /// 清空所有帧
        /// </summary>
        public void Clear()
        {
            Frames.Clear();
            CurrentFrame = 0;
            ConfirmedFrame = -1;
        }

        /// <summary>
        /// 自动清理过旧的帧数据
        /// </summary>
        private void CleanupOldFrames()
        {
            if (Frames.Count <= MaxSize) return;
            
            int keepFrame = CurrentFrame - MaxSize;
            RemoveOldFrames(keepFrame);
        }
    }
}
