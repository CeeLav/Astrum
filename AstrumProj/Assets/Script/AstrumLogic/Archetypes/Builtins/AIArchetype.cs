using System;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;
using cfg;

namespace Astrum.LogicCore.Archetypes
{
    /// <summary>
    /// AI 子原型：提供 AI 状态机组件与状态/调度能力。
    /// 作为可运行时挂载/卸载的 SubArchetype 使用（名称："AI"）。
    /// </summary>
    [Archetype(EArchetype.AI)]
    public class AIArchetype : Archetype
    {
        private static readonly Type[] _comps =
        {
            typeof(AIStateMachineComponent)
        };

        private static readonly Type[] _caps =
        {
            typeof(AIFSMCapability),
            typeof(IdleStateCapability),
            typeof(MoveStateCapability),
            typeof(BattleStateCapability)
        };

        public override Type[] Components => _comps;
        public override Type[] Capabilities => _caps;
    }
}


