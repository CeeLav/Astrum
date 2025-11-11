namespace Astrum.CommonBase
{
    /// <summary>
    /// 触发帧 JSON 键名常量（运行时和编辑器共用）
    /// </summary>
    public static class TriggerFrameJsonKeys
    {
        // === 基础字段 ===
        
        /// <summary>帧号（单帧触发）</summary>
        public const string Frame = "frame";
        
        /// <summary>起始帧（多帧范围触发）</summary>
        public const string StartFrame = "startFrame";
        
        /// <summary>结束帧（多帧范围触发）</summary>
        public const string EndFrame = "endFrame";
        
        /// <summary>触发类型（顶层：SkillEffect, VFX, SFX）</summary>
        public const string Type = "type";
        
        // === SkillEffect 类型字段 ===
        
        /// <summary>触发类型（SkillEffect 内部：Collision, Direct, Condition）</summary>
        public const string TriggerType = "triggerType";
        
        /// <summary>效果ID（单个，已废弃）</summary>
        [System.Obsolete("使用 EffectIds 替代")]
        public const string EffectId = "effectId";
        
        /// <summary>效果ID列表（支持多个效果）</summary>
        public const string EffectIds = "effectIds";
        
        /// <summary>碰撞信息（字符串格式）</summary>
        public const string CollisionInfo = "collisionInfo";

        /// <summary>Socket 名称（用于定位发射点）</summary>
        public const string SocketName = "socketName";

        /// <summary>抛射物 ID（技能参数覆盖）</summary>
        public const string ProjectileId = "projectileId";

        /// <summary>额外效果 ID 列表</summary>
        public const string AdditionalEffectIds = "additionalEffectIds";

        /// <summary>轨迹覆盖数据</summary>
        public const string TrajectoryOverride = "trajectoryOverride";
        
        // === VFX 类型字段 ===
        
        /// <summary>特效资源路径</summary>
        public const string ResourcePath = "resourcePath";
        
        /// <summary>位置偏移</summary>
        public const string PositionOffset = "positionOffset";
        
        /// <summary>旋转（欧拉角）</summary>
        public const string Rotation = "rotation";
        
        /// <summary>缩放</summary>
        public const string Scale = "scale";
        
        /// <summary>播放速度</summary>
        public const string PlaybackSpeed = "playbackSpeed";
        
        /// <summary>是否跟随角色</summary>
        public const string FollowCharacter = "followCharacter";
        
        /// <summary>是否循环</summary>
        public const string Loop = "loop";
        
        // === 类型值常量 ===
        
        /// <summary>触发类型：SkillEffect</summary>
        public const string TypeSkillEffect = "SkillEffect";
        
        /// <summary>触发类型：VFX</summary>
        public const string TypeVFX = "VFX";
        
        /// <summary>触发类型：SFX</summary>
        public const string TypeSFX = "SFX";
        
        /// <summary>SkillEffect 触发类型：Collision</summary>
        public const string TriggerTypeCollision = "Collision";
        
        /// <summary>SkillEffect 触发类型：Direct</summary>
        public const string TriggerTypeDirect = "Direct";
        
        /// <summary>SkillEffect 触发类型：Condition</summary>
        public const string TriggerTypeCondition = "Condition";

        /// <summary>SkillEffect 触发类型：Projectile</summary>
        public const string TriggerTypeProjectile = "Projectile";
    }
}

