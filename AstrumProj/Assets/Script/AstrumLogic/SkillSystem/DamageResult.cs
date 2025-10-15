using TrueSync;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 伤害结果数据
    /// </summary>
    public class DamageResult
    {
        /// <summary>最终伤害值（定点数）</summary>
        public FP FinalDamage { get; set; }
        
        /// <summary>是否暴击</summary>
        public bool IsCritical { get; set; }
        
        /// <summary>是否格挡</summary>
        public bool IsBlocked { get; set; }
        
        /// <summary>是否未命中</summary>
        public bool IsMiss { get; set; }
        
        /// <summary>伤害类型</summary>
        public Stats.DamageType DamageType { get; set; }
    }
}

