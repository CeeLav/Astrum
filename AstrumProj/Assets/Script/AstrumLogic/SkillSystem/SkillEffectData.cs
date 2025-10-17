using System.Collections.Generic;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 技能效果数据（运行时）
    /// 用于从 SkillExecutorCapability 传递到 SkillEffectManager
    /// </summary>
    public class SkillEffectData
    {
        /// <summary>施法者实体ID</summary>
        public long CasterId { get; set; }
        
        /// <summary>目标实体ID</summary>
        public long TargetId { get; set; }
        
        /// <summary>效果ID</summary>
        public int EffectId { get; set; }
        
        /// <summary>效果参数（可选，用于传递额外数据）</summary>
        public Dictionary<string, object> Parameters { get; set; }
    }
}

