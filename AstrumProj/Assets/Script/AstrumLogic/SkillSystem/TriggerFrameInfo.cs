using MemoryPack;
using Astrum.LogicCore.Physics;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 触发帧信息（运行时数据，已包含解析好的碰撞形状）
    /// 从 SkillActionInfo.TriggerFrames 解析后构造
    /// </summary>
    [MemoryPackable]
    public partial class TriggerFrameInfo
    {
        /// <summary>触发帧号</summary>
        public int Frame { get; set; }
        
        /// <summary>触发类型（Collision/Direct/Condition）</summary>
        public string TriggerType { get; set; } = string.Empty;
        
        /// <summary>效果ID</summary>
        public int EffectId { get; set; }
        
        /// <summary>
        /// 碰撞形状（仅用于 Collision 类型，已在配置解析时构造）
        /// 运行时可直接使用，无需重新解析
        /// </summary>
        [MemoryPackAllowSerialize]
        public CollisionShape? CollisionShape { get; set; }
        
        /// <summary>触发条件（仅用于 Condition 类型）</summary>
        [MemoryPackAllowSerialize]
        public TriggerCondition? Condition { get; set; }
        
        [MemoryPackConstructor]
        public TriggerFrameInfo(int frame, string triggerType, int effectId, 
            CollisionShape? collisionShape, TriggerCondition? condition)
        {
            Frame = frame;
            TriggerType = triggerType ?? string.Empty;
            EffectId = effectId;
            CollisionShape = collisionShape;
            Condition = condition;
        }
        
        public TriggerFrameInfo() { }
    }
    
    /// <summary>
    /// 触发条件（用于 Condition 类型触发）
    /// </summary>
    [MemoryPackable]
    public partial class TriggerCondition
    {
        /// <summary>最小能量要求</summary>
        public float EnergyMin { get; set; }
        
        /// <summary>必需的状态标记</summary>
        public string RequiredTag { get; set; } = string.Empty;
        
        [MemoryPackConstructor]
        public TriggerCondition(float energyMin, string requiredTag)
        {
            EnergyMin = energyMin;
            RequiredTag = requiredTag ?? string.Empty;
        }
        
        public TriggerCondition() { }
    }
}

