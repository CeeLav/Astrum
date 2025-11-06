namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 技能效果事件（用于事件队列系统）
    /// 与 SkillEffectData 不同，此结构用于双模式事件队列，是轻量级的 struct
    /// </summary>
    public struct SkillEffectEvent
    {
        /// <summary>
        /// 施法者ID
        /// </summary>
        public long CasterId;
        
        /// <summary>
        /// 效果ID
        /// </summary>
        public int EffectId;
        
        /// <summary>
        /// 触发帧
        /// </summary>
        public int TriggerFrame;
    }
}

