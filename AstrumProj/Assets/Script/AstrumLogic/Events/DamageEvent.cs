using TrueSync;

namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 伤害事件（由 DamageEffectHandler 发送给 DamageCapability）
    /// </summary>
    public struct DamageEvent : IEvent
    {
        /// <summary>施法者ID</summary>
        public long CasterId;
        
        /// <summary>效果ID</summary>
        public int EffectId;
        
        /// <summary>计算后的最终伤害值</summary>
        public FP Damage;
        
        /// <summary>是否暴击</summary>
        public bool IsCritical;
        
        /// <summary>伤害类型（1=物理/2=魔法/3=真实）</summary>
        public int DamageType;
        
        /// <summary>
        /// 是否在 Capability 未激活时也触发
        /// true: 无论 Capability 是否激活都会处理该事件（可用于主动激活 Capability）
        /// false: 只有在 Capability 已激活时才处理
        /// </summary>
        public bool TriggerWhenInactive { get; set; }
    }
}

