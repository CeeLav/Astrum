using MemoryPack;
using Astrum.CommonBase;

namespace Astrum.LogicCore.ActionSystem
{
    /// <summary>
    /// 预订单动作信息 - 动作切换的预约信息
    /// 支持对象池复用，减少 GC 分配
    /// </summary>
    [MemoryPackable]
    public partial class PreorderActionInfo : IPool
    {
        /// <summary>
        /// 对象是否来自对象池（IPool 接口必需成员）
        /// </summary>
        [MemoryPackIgnore]
        public bool IsFromPool { get; set; }
        
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
        
        /// <summary>
        /// 从对象池创建实例（性能优化）
        /// </summary>
        public static PreorderActionInfo Create(int actionId, int priority, int transitionFrames = 3, int fromFrame = 0, int freezingFrames = 0)
        {
            var instance = ObjectPool.Instance.Fetch<PreorderActionInfo>();
            instance.ActionId = actionId;
            instance.Priority = priority;
            instance.TransitionFrames = transitionFrames;
            instance.FromFrame = fromFrame;
            instance.FreezingFrames = freezingFrames;
            return instance;
        }
        
        /// <summary>
        /// 重置状态（用于对象池复用）
        /// </summary>
        public void Reset()
        {
            ActionId = 0;
            Priority = 0;
            TransitionFrames = 0;
            FromFrame = 0;
            FreezingFrames = 0;
        }
    }
}
