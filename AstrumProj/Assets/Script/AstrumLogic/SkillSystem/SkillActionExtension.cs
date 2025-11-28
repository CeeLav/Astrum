using MemoryPack;
using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 技能动作扩展数据（运行时版本，支持 MemoryPack 序列化）
    /// 对应 SkillActionTable 的专属字段
    /// </summary>
    [MemoryPackable]
    public partial class SkillActionExtension
    {
        /// <summary>实际法力消耗（暂不使用，预留）</summary>
        public int ActualCost { get; set; } = 0;
        
        /// <summary>实际冷却帧数（暂不使用，预留）</summary>
        public int ActualCooldown { get; set; } = 0;
        
        /// <summary>触发帧信息（字符串格式，用于序列化）</summary>
        public string TriggerFrames { get; set; } = string.Empty;
        
        /// <summary>已解析的触发帧信息列表（运行时数据，根据技能等级构造，包含CollisionShape）</summary>
        [MemoryPackAllowSerialize]
        public List<TriggerFrameInfo> TriggerEffects { get; set; } = new();
        
        /// <summary>根节点位移数据（运行时数据，从配置表加载）</summary>
        [MemoryPackAllowSerialize]
        public AnimationRootMotionData RootMotionData { get; set; }
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public SkillActionExtension()
        {
            RootMotionData = new AnimationRootMotionData();
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public SkillActionExtension(int actualCost, int actualCooldown, string triggerFrames, 
            List<TriggerFrameInfo> triggerEffects, AnimationRootMotionData rootMotionData)
        {
            ActualCost = actualCost;
            ActualCooldown = actualCooldown;
            TriggerFrames = triggerFrames ?? string.Empty;
            TriggerEffects = triggerEffects ?? new List<TriggerFrameInfo>();
            RootMotionData = rootMotionData ?? new AnimationRootMotionData();
        }
    }
}

