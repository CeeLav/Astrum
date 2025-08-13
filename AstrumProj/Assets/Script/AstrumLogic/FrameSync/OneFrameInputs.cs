using System.Collections.Generic;

namespace Astrum.LogicCore.FrameSync
{
    /// <summary>
    /// 单帧输入数据集合
    /// </summary>
    public class OneFrameInputs
    {
        /// <summary>
        /// 帧号
        /// </summary>
        public int Frame { get; set; }

        /// <summary>
        /// 玩家ID -> 输入数据的字典
        /// </summary>
        public Dictionary<long, LSInput> Inputs { get; private set; } = new Dictionary<long, LSInput>();

        /// <summary>
        /// 是否收集齐所有玩家输入
        /// </summary>
        public bool IsComplete { get; set; }

        public OneFrameInputs() { }

        public OneFrameInputs(int frame)
        {
            Frame = frame;
        }
        public static OneFrameInputs Create(bool isFromPool = false)
        {
            return new  OneFrameInputs();
        }
        
        public void CopyTo(OneFrameInputs to)
        {
            to.Inputs.Clear();
            foreach (var kv in this.Inputs)
            {
                to.Inputs.Add(kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// 添加玩家输入
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="input">输入数据</param>
        public void AddInput(long playerId, LSInput input)
        {
            if (input != null)
            {
                input.PlayerId = playerId;
                input.Frame = Frame;
                Inputs[playerId] = input;
            }
        }

        /// <summary>
        /// 获取玩家输入
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>输入数据，如果不存在返回null</returns>
        public LSInput? GetInput(long playerId)
        {
            return Inputs.TryGetValue(playerId, out var input) ? input : null;
        }

        /// <summary>
        /// 检查是否有全部输入
        /// </summary>
        /// <param name="playerCount">预期的玩家数量</param>
        /// <returns>是否有全部输入</returns>
        public bool HasAllInputs(int playerCount)
        {
            return Inputs.Count >= playerCount;
        }

        /// <summary>
        /// 检查是否有指定玩家的输入
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否有该玩家的输入</returns>
        public bool HasInputForPlayer(int playerId)
        {
            return Inputs.ContainsKey(playerId);
        }

        /// <summary>
        /// 获取所有玩家ID
        /// </summary>
        /// <returns>玩家ID列表</returns>
        public List<long> GetPlayerIds()
        {
            return new List<long>(Inputs.Keys);
        }

        /// <summary>
        /// 克隆单帧输入
        /// </summary>
        /// <returns>克隆的单帧输入</returns>
        public OneFrameInputs Clone()
        {
            var clone = new OneFrameInputs(Frame)
            {
                IsComplete = IsComplete
            };

            foreach (var kvp in Inputs)
            {
                clone.Inputs[kvp.Key] = kvp.Value.Clone();
            }

            return clone;
        }

        /// <summary>
        /// 清空所有输入
        /// </summary>
        public void Clear()
        {
            Inputs.Clear();
            IsComplete = false;
        }

        /// <summary>
        /// 获取输入数量
        /// </summary>
        /// <returns>输入数量</returns>
        public int GetInputCount()
        {
            return Inputs.Count;
        }
    }
}
