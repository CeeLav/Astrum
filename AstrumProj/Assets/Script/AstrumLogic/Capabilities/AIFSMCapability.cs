using System;
using System.Linq;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// AI 状态机调度能力：统一决策与状态切换，调用对应状态能力执行。
    /// </summary>
    public class AIFSMCapability : Capability
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

            // 状态切换（若指定 NextState）
            if (!string.IsNullOrEmpty(_fsm.NextState) && _fsm.NextState != _fsm.CurrentState)
            {
                SwitchTo(_fsm.NextState);
                _fsm.NextState = string.Empty;
            }

            // 执行当前状态能力
            var stateCap = ResolveStateCapability(_fsm.CurrentState);
            stateCap?.Tick();
        }

        private Capability? ResolveStateCapability(string state)
        {
            if (string.IsNullOrEmpty(state) || OwnerEntity == null) return null;
            var capTypeName = state switch
            {
                "Idle" => typeof(IdleStateCapability).FullName,
                "Move" => typeof(MoveStateCapability).FullName,
                _ => null
            };
            if (capTypeName == null) return null;
            return OwnerEntity.Capabilities.FirstOrDefault(c => c.GetType().FullName == capTypeName);
        }

        private void SwitchTo(string next)
        {
            if (_fsm == null) return;
            _fsm.LastState = _fsm.CurrentState;
            _fsm.CurrentState = next;
            _fsm.LastSwitchFrame = OwnerEntity?.World?.CurFrame ?? 0;
        }
    }
}


