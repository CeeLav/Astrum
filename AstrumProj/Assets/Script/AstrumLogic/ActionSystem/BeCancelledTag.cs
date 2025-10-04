using MemoryPack;
using cfg;
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
        
        /// <summary>生效范围（时间百分比）</summary>
        public vector2 Range { get; set; } = new vector2();
        
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
        public BeCancelledTag(List<string> tags, vector2 range, int blendOutFrames, int priority)
        {
            Tags = tags ?? new List<string>();
            Range = range;
            BlendOutFrames = blendOutFrames;
            Priority = priority;
        }
    }
}
