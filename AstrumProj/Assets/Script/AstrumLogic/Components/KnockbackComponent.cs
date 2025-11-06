using TrueSync;
using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 击退组件 - 存储击退相关数据
    /// </summary>
    [MemoryPackable]
    public partial class KnockbackComponent : BaseComponent
    {
        /// <summary>
        /// 是否正在击退中
        /// </summary>
        public bool IsKnockingBack { get; set; }
        
        /// <summary>
        /// 击退方向（已归一化）
        /// </summary>
        public TSVector Direction { get; set; }
        
        /// <summary>
        /// 击退速度（米/秒）
        /// </summary>
        public FP Speed { get; set; }
        
        /// <summary>
        /// 剩余时间（秒）
        /// </summary>
        public FP RemainingTime { get; set; }
        
        /// <summary>
        /// 总击退距离（米）
        /// </summary>
        public FP TotalDistance { get; set; }
        
        /// <summary>
        /// 已移动距离（米）
        /// </summary>
        public FP MovedDistance { get; set; }
        
        /// <summary>
        /// 击退类型
        /// </summary>
        public KnockbackType Type { get; set; }
        
        /// <summary>
        /// 施法者ID（用于追踪击退来源）
        /// </summary>
        public long CasterId { get; set; }
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public KnockbackComponent() : base() { }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public KnockbackComponent(
            int componentId,
            long entityId, 
            bool isKnockingBack, 
            TSVector direction, 
            FP speed, 
            FP remainingTime, 
            FP totalDistance, 
            FP movedDistance, 
            KnockbackType type, 
            long casterId)
        {
            ComponentId = componentId;
            EntityId = entityId;
            IsKnockingBack = isKnockingBack;
            Direction = direction;
            Speed = speed;
            RemainingTime = remainingTime;
            TotalDistance = totalDistance;
            MovedDistance = movedDistance;
            Type = type;
            CasterId = casterId;
        }
    }
    
    /// <summary>
    /// 击退类型
    /// </summary>
    public enum KnockbackType
    {
        /// <summary>
        /// 线性击退（匀速）
        /// </summary>
        Linear = 0,
        
        /// <summary>
        /// 减速击退（逐渐变慢）
        /// </summary>
        Decelerate = 1,
        
        /// <summary>
        /// 抛射击退（抛物线，可选扩展）
        /// </summary>
        Launch = 2
    }
}

