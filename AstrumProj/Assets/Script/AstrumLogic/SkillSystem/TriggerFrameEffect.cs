using MemoryPack;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 触发帧效果信息（解析后的数据）
    /// </summary>
    [MemoryPackable]
    public partial class TriggerFrameEffect
    {
        /// <summary>触发帧号</summary>
        public int Frame { get; set; }
        
        /// <summary>触发类型（Collision/Direct/Condition）</summary>
        public string TriggerType { get; set; } = string.Empty;
        
        /// <summary>效果ID</summary>
        public int EffectId { get; set; }
        
        /// <summary>计算后的效果值</summary>
        public float EffectValue { get; set; }
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public TriggerFrameEffect()
        {
        }
        
        /// <summary>
        /// MemoryPack构造函数
        /// </summary>
        [MemoryPackConstructor]
        public TriggerFrameEffect(int frame, string triggerType, int effectId, float effectValue)
        {
            Frame = frame;
            TriggerType = triggerType ?? string.Empty;
            EffectId = effectId;
            EffectValue = effectValue;
        }
    }
}

