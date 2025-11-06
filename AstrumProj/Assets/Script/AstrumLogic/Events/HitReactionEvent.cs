using TrueSync;

namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 受击反馈事件（由伤害/击退等效果处理器发送给受击者）
    /// </summary>
    public struct HitReactionEvent
    {
        /// <summary>施法者ID</summary>
        public long CasterId;
        
        /// <summary>受击者ID</summary>
        public long TargetId;
        
        /// <summary>效果ID</summary>
        public int EffectId;
        
        /// <summary>效果类型（1=伤害, 2=治疗, 3=击退等）</summary>
        public int EffectType;
        
        /// <summary>伤害方向（用于播放受击动作）</summary>
        public TSVector HitDirection;
        
        /// <summary>是否产生硬直</summary>
        public bool CausesStun;
    }
}

