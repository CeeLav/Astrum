using System;
using MemoryPack;
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

        public override void OnAttachToEntity(Entity entity)
        {
            // 默认状态置空，由调度能力决定首次状态
        }
    }
}


