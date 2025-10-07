using MemoryPack;
using System.Collections.Generic;

namespace Astrum.LogicCore.ActionSystem
{
    /// <summary>
    /// 普通动作信息 - ActionInfo 的具体实现类
    /// 用于普通动作（idle、walk、attack 等非技能动作）
    /// </summary>
    [MemoryPackable]
    public partial class NormalActionInfo : ActionInfo
    {
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public NormalActionInfo() : base()
        {
        }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public NormalActionInfo(int id, string catalog, List<CancelTag> cancelTags, 
            List<BeCancelledTag> beCancelledTags, List<TempBeCancelledTag> tempBeCancelledTags, 
            List<ActionCommand> commands, int autoNextActionId, bool keepPlayingAnim, 
            bool autoTerminate, int priority, int duration)
            : base(id, catalog, cancelTags, beCancelledTags, tempBeCancelledTags, commands,
                   autoNextActionId, keepPlayingAnim, autoTerminate, priority, duration)
        {
        }
    }
}

