namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 效果值查询结果（辅助类，不需要序列化）
    /// 用于 BaseEffectId + Level 映射查询的返回值
    /// </summary>
    public class EffectValueResult
    {
        /// <summary>实际使用的效果ID</summary>
        public int EffectId { get; set; }
        
        /// <summary>计算后的数值</summary>
        public float Value { get; set; }
        
        /// <summary>是否是插值结果（预留，暂不使用）</summary>
        public bool IsInterpolated { get; set; }
        
        /// <summary>原始效果数据</summary>
        public cfg.Skill.SkillEffectTable? EffectData { get; set; }
    }
}

