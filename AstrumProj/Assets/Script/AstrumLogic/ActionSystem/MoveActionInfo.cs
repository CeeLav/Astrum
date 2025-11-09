using MemoryPack;
using System.Collections.Generic;
using TrueSync;
using Astrum.LogicCore.SkillSystem;

namespace Astrum.LogicCore.ActionSystem
{
    /// <summary>
    /// 移动动作信息 - 包含移动相关的扩展数据
    /// </summary>
    [MemoryPackable]
    public partial class MoveActionInfo : ActionInfo
    {
        /// <summary>
        /// 根节点位移数据（运行时数据，从 MoveActionTable 加载）
        /// </summary>
        [MemoryPackAllowSerialize]
        public AnimationRootMotionData RootMotionData { get; set; }
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public MoveActionInfo() : base()
        {
            RootMotionData = new AnimationRootMotionData();
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public MoveActionInfo(int id, string catalog, List<CancelTag> cancelTags, 
            List<BeCancelledTag> beCancelledTags, List<TempBeCancelledTag> tempBeCancelledTags, 
            List<ActionCommand> commands, int autoNextActionId, bool keepPlayingAnim, 
            bool autoTerminate, int priority, int duration, AnimationRootMotionData rootMotionData)
            : base(id, catalog, cancelTags, beCancelledTags, tempBeCancelledTags, commands,
                   autoNextActionId, keepPlayingAnim, autoTerminate, priority, duration)
        {
            RootMotionData = rootMotionData ?? new AnimationRootMotionData();
        }
    }
}

