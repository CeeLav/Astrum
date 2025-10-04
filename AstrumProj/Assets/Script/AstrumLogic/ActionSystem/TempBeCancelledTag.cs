using MemoryPack;
using System.Collections.Generic;

namespace Astrum.LogicCore.ActionSystem
{
    /// <summary>
    /// 临时被取消标签 - 动作过程中临时开启的取消点
    /// </summary>
    [MemoryPackable]
    public partial class TempBeCancelledTag
    {
        /// <summary>临时标签ID</summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>被取消标签名称列表</summary>
        [MemoryPackAllowSerialize]
        public List<string> Tags { get; set; } = new();
        
        /// <summary>持续时间（帧）</summary>
        public int DurationFrames { get; set; } = 0;
        
        /// <summary>融合时间（帧）</summary>
        public int BlendOutFrames { get; set; } = 0;
        
        /// <summary>优先级变化</summary>
        public int Priority { get; set; } = 0;
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public TempBeCancelledTag()
        {
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public TempBeCancelledTag(string id, List<string> tags, int durationFrames, int blendOutFrames, int priority)
        {
            Id = id;
            Tags = tags ?? new List<string>();
            DurationFrames = durationFrames;
            BlendOutFrames = blendOutFrames;
            Priority = priority;
        }
    }
}
