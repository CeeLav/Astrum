using System;
using MemoryPack;
using TrueSync;
using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.Components
{
    [MemoryPackable]
    public partial class AIStateMachineComponent : BaseComponent
    {
        public string CurrentState { get; set; } = string.Empty;
        public string NextState { get; set; } = string.Empty;
        public string LastState { get; set; } = string.Empty;
        public int LastSwitchFrame { get; set; } = 0;

        // 当前追逐目标（实体ID，-1 表示无目标）
        public long CurrentTargetId { get; set; } = -1;

        // 随机漫游目标点（世界坐标）
        public TSVector WanderTarget { get; set; } = TSVector.zero;

        // 下一次允许触发随机漫游的帧号（用于去抖）
        public int NextWanderFrame { get; set; } = 0;

        public override void OnAttachToEntity(Entity entity)
        {
            // 默认状态置空，由调度能力决定首次状态
        }
    }
}


