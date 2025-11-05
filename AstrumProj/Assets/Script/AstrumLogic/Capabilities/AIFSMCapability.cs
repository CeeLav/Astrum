using System;
using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// AI 状态机调度能力（新架构，基于 Capability&lt;T&gt;）
    /// 统一决策与状态切换，调用对应状态能力执行。
    /// </summary>
    public class AIFSMCapability : Capability<AIFSMCapability>
    {
        // ====== 元数据 ======
        public override int Priority => 400; // AI 系统优先级较高
        
        public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
        private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag> 
        { 
            CapabilityTag.AI 
        };
        
        // ====== 生命周期 ======
        
        public override void OnAttached(Entity entity)
        {
            base.OnAttached(entity);
            
            var fsm = GetComponent<AIStateMachineComponent>(entity);
            if (fsm != null && string.IsNullOrEmpty(fsm.CurrentState))
            {
                fsm.CurrentState = "Idle";
                fsm.LastState = string.Empty;
                fsm.LastSwitchFrame = entity.World?.CurFrame ?? 0;
            }
        }
        
        public override bool ShouldActivate(Entity entity)
        {
            // 检查必需组件是否存在
            return base.ShouldActivate(entity) &&
                   HasComponent<AIStateMachineComponent>(entity);
        }
        
        public override bool ShouldDeactivate(Entity entity)
        {
            // 缺少任何必需组件则停用
            return base.ShouldDeactivate(entity) ||
                   !HasComponent<AIStateMachineComponent>(entity);
        }
        
        // ====== 每帧逻辑 ======
        
        public override void Tick(Entity entity)
        {
            var fsm = GetComponent<AIStateMachineComponent>(entity);
            if (fsm == null) return;

            // 仅负责状态切换与激活管理，不直接调用其他能力的 Tick（由统一更新器驱动）。
            if (!string.IsNullOrEmpty(fsm.NextState) && fsm.NextState != fsm.CurrentState)
            {
                SwitchTo(entity, fsm, fsm.NextState);
                fsm.NextState = string.Empty;
            }
        }
        
        // ====== 辅助方法 ======
        
        private void SwitchTo(Entity entity, AIStateMachineComponent fsm, string next)
        {
            if (fsm == null) return;
            fsm.LastState = fsm.CurrentState;
            fsm.CurrentState = next;
            fsm.LastSwitchFrame = entity.World?.CurFrame ?? 0;
        }
    }
}
