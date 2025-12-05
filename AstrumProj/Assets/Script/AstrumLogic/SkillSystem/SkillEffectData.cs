using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 技能效果数据（运行时）
    /// 用于从 SkillExecutorCapability 传递到 SkillEffectManager
    /// 支持对象池复用，避免频繁创建产生 GC
    /// </summary>
    public partial class SkillEffectData : IPool
    {
        /// <summary>施法者实体ID</summary>
        public long CasterId { get; set; }
        
        /// <summary>目标实体ID</summary>
        public long TargetId { get; set; }
        
        /// <summary>效果ID</summary>
        public int EffectId { get; set; }
        
        /// <summary>效果参数（可选，用于传递额外数据）</summary>
        public Dictionary<string, object> Parameters { get; set; }
        
        // ====== 对象池支持 ======
        
        /// <summary>
        /// 标记此对象是否来自对象池
        /// </summary>
        public bool IsFromPool { get; set; }
        
        /// <summary>
        /// 从对象池创建 SkillEffectData 实例
        /// </summary>
        public static SkillEffectData Create(long casterId, long targetId, int effectId)
        {
            var instance = ObjectPool.Instance.Fetch<SkillEffectData>();
            instance.CasterId = casterId;
            instance.TargetId = targetId;
            instance.EffectId = effectId;
            instance.Parameters = null;  // 大多数情况不需要参数
            return instance;
        }
        
        /// <summary>
        /// 重置对象状态（对象池回收前调用）
        /// </summary>
        public void Reset()
        {
            CasterId = 0;
            TargetId = 0;
            EffectId = 0;
            Parameters = null;
        }
    }
}

