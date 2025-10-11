using Astrum.LogicCore.ActionSystem;
using MemoryPack;
using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 技能动作信息 - 继承自NormalActionInfo，添加技能系统专属字段
    /// 对应SkillActionTable配置
    /// </summary>
    [MemoryPackable]
    public partial class SkillActionInfo : NormalActionInfo
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
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public SkillActionInfo() : base()
        {
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// 注意：必须包含基类的所有参数 + 派生类的参数
        /// </summary>
        [MemoryPackConstructor]
        public SkillActionInfo(
            // 基类ActionInfo的参数（按顺序）
            int id, string catalog, List<CancelTag> cancelTags, List<BeCancelledTag> beCancelledTags,
            List<TempBeCancelledTag> tempBeCancelledTags, List<ActionCommand> commands, int autoNextActionId,
            bool keepPlayingAnim, bool autoTerminate, int priority, int duration,
            // 派生类SkillActionInfo的参数
            int actualCost, int actualCooldown, 
            string triggerFrames, List<TriggerFrameInfo> triggerEffects)
            : base(id, catalog, cancelTags, beCancelledTags, tempBeCancelledTags, commands,
                   autoNextActionId, keepPlayingAnim, autoTerminate, priority, duration)
        {
            ActualCost = actualCost;
            ActualCooldown = actualCooldown;
            TriggerFrames = triggerFrames ?? string.Empty;
            TriggerEffects = triggerEffects ?? new List<TriggerFrameInfo>();
        }
    }
}

