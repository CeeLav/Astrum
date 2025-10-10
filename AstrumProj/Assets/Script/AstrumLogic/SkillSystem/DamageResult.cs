namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 伤害结果数据
    /// </summary>
    public class DamageResult
    {
        /// <summary>最终伤害值</summary>
        public float FinalDamage { get; set; }
        
        /// <summary>是否暴击</summary>
        public bool IsCritical { get; set; }
        
        /// <summary>伤害类型</summary>
        public DamageType DamageType { get; set; }
    }
    
    /// <summary>
    /// 伤害类型枚举
    /// </summary>
    public enum DamageType
    {
        /// <summary>物理伤害</summary>
        Physical = 1,
        
        /// <summary>魔法伤害</summary>
        Magical = 2,
        
        /// <summary>真实伤害（无视防御）</summary>
        True = 3
    }
}

