using System;
using System.Linq;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using MemoryPack;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// AI 状态机调度能力（旧架构，已废弃，保留用于兼容）
    /// 统一决策与状态切换，调用对应状态能力执行。
    /// </summary>
    [MemoryPackable]
    public partial class AIFSMCapabilityOld : Capability
    {
        private AIStateMachineComponent? _fsm;

        public override void Initialize()
        {
            base.Initialize();
            _fsm = GetOwnerComponent<AIStateMachineComponent>();
            if (_fsm != null && string.IsNullOrEmpty(_fsm.CurrentState))
            {
                _fsm.CurrentState = "Idle";
                _fsm.LastState = string.Empty;
                _fsm.LastSwitchFrame = OwnerEntity?.World?.CurFrame ?? 0;
            }
        }

        public override void Tick()
        {
            if (!CanExecute()) return;
            _fsm ??= GetOwnerComponent<AIStateMachineComponent>();
            if (_fsm == null) return;

            // 仅负责状态切换与激活管理，不直接调用其他能力的 Tick（由统一更新器驱动）。
            if (!string.IsNullOrEmpty(_fsm.NextState) && _fsm.NextState != _fsm.CurrentState)
            {
                SwitchTo(_fsm.NextState);
                _fsm.NextState = string.Empty;
            }
        }

        private void SwitchTo(string next)
        {
            if (_fsm == null) return;
            _fsm.LastState = _fsm.CurrentState;
            _fsm.CurrentState = next;
            _fsm.LastSwitchFrame = OwnerEntity?.World?.CurFrame ?? 0;
        }
        
        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;
            return OwnerHasComponent<AIStateMachineComponent>();
        }
    }
}

