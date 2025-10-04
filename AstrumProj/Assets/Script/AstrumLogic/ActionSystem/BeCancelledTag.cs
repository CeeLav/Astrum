using MemoryPack;
using System.Collections.Generic;

namespace Astrum.LogicCore.ActionSystem
{
    /// <summary>
    /// 被取消标签 - 当前动作可以被其他动作取消的依据
    /// </summary>
    [MemoryPackable]
    public partial class BeCancelledTag
    {
        /// <summary>被取消标签名称列表</summary>
        [MemoryPackAllowSerialize]
        public List<string> Tags { get; set; } = new();
        
        /// <summary>生效范围帧数 [起始帧, 结束帧]</summary>
        [MemoryPackAllowSerialize]
        public List<int> RangeFrames { get; set; } = new List<int>();
        
        /// <summary>融合时间（帧）</summary>
        public int BlendOutFrames { get; set; } = 0;
        
        /// <summary>优先级变化</summary>
        public int Priority { get; set; } = 0;
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public BeCancelledTag()
        {
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public BeCancelledTag(List<string> tags, List<int> rangeFrames, int blendOutFrames, int priority)
        {
            Tags = tags ?? new List<string>();
            RangeFrames = rangeFrames ?? new List<int>();
            BlendOutFrames = blendOutFrames;
            Priority = priority;
        }
    }
}
