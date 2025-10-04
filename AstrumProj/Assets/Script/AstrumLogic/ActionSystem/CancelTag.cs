using MemoryPack;

namespace Astrum.LogicCore.ActionSystem
{
    /// <summary>
    /// 取消标签 - 新动作可以取消其他动作的依据
    /// </summary>
    [MemoryPackable]
    public partial class CancelTag
    {
        /// <summary>取消标签名称</summary>
        public string Tag { get; set; } = string.Empty;
        
        /// <summary>起始帧（百分比 0.0-1.0）</summary>
        public float StartFrom { get; set; } = 0.0f;
        
        /// <summary>融合时间（帧）</summary>
        public int BlendInFrames { get; set; } = 0;
        
        /// <summary>优先级变化</summary>
        public int Priority { get; set; } = 0;
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public CancelTag()
        {
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public CancelTag(string tag, float startFrom, int blendInFrames, int priority)
        {
            Tag = tag;
            StartFrom = startFrom;
            BlendInFrames = blendInFrames;
            Priority = priority;
        }
    }
}
