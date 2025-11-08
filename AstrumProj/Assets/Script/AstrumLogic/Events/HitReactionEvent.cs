using TrueSync;

namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 受击反馈事件（由伤害/击退等效果处理器发送给受击者）
    /// </summary>
    public struct HitReactionEvent : IEvent
    {
        /// <summary>施法者ID</summary>
        public long CasterId;
        
        /// <summary>受击者ID</summary>
        public long TargetId;
        
        /// <summary>效果ID</summary>
        public int EffectId;
        
        /// <summary>效果类型键</summary>
        public string EffectType;
        
        /// <summary>伤害方向（用于播放受击动作）</summary>
        public TSVector HitDirection;
        
        /// <summary>是否产生硬直</summary>
        public bool CausesStun;

        /// <summary>视觉特效预制体路径</summary>
        public string VisualEffectPath;

        /// <summary>音效资源路径</summary>
        public string SoundEffectPath;
        
        /// <summary>受击位置偏移（相对于目标）</summary>
        public TrueSync.TSVector HitOffset;
        
        /// <summary>
        /// 是否在 Capability 未激活时也触发
        /// true: 无论 Capability 是否激活都会处理该事件（可用于主动激活 Capability）
        /// false: 只有在 Capability 已激活时才处理
        /// </summary>
        public bool TriggerWhenInactive { get; set; }
    }
}

