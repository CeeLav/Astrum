using TrueSync;
using Astrum.LogicCore.Components;

namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 击退事件（由 KnockbackEffectHandler 发送给 KnockbackCapability）
    /// </summary>
    public struct KnockbackEvent : IEvent
    {
        /// <summary>施法者ID</summary>
        public long CasterId;
        
        /// <summary>效果ID</summary>
        public int EffectId;
        
        /// <summary>击退方向（世界空间，单位向量）</summary>
        public TSVector Direction;
        
        /// <summary>击退距离（米）</summary>
        public FP Distance;
        
        /// <summary>击退持续时间（秒）</summary>
        public FP Duration;
        
        /// <summary>击退类型</summary>
        public KnockbackType Type;
        
        /// <summary>
        /// 是否在 Capability 未激活时也触发
        /// true: 无论 Capability 是否激活都会处理该事件（可用于主动激活 Capability）
        /// false: 只有在 Capability 已激活时才处理
        /// </summary>
        public bool TriggerWhenInactive { get; set; }
    }
}

