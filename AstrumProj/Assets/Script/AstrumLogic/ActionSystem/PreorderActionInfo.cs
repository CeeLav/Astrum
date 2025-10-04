using MemoryPack;

namespace Astrum.LogicCore.ActionSystem
{
    /// <summary>
    /// 预订单动作信息 - 动作切换的预约信息
    /// </summary>
    [MemoryPackable]
    public partial class PreorderActionInfo
    {
        /// <summary>目标动作ID</summary>
        public int ActionId { get; set; } = 0;
        
        /// <summary>优先级</summary>
        public int Priority { get; set; } = 0;
        
        /// <summary>融合帧数</summary>
        public int TransitionFrames { get; set; } = 0;
        
        /// <summary>起始帧</summary>
        public int FromFrame { get; set; } = 0;
        
        /// <summary>切换后硬直帧数</summary>
        public int FreezingFrames { get; set; } = 0;
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public PreorderActionInfo()
        {
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public PreorderActionInfo(int actionId, int priority, int transitionFrames, int fromFrame, int freezingFrames)
        {
            ActionId = actionId;
            Priority = priority;
            TransitionFrames = transitionFrames;
            FromFrame = fromFrame;
            FreezingFrames = freezingFrames;
        }
    }
}
