using System;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;

namespace Astrum.LogicCore.Archetypes
{
    [Archetype("Combat", "Action")]
    public class CombatArchetype : Archetype
    {
        private static readonly Type[] _comps =
        {
            typeof(SkillComponent),
            // 数值系统组件
            typeof(BaseStatsComponent),
            typeof(DerivedStatsComponent),
            typeof(DynamicStatsComponent),
            typeof(BuffComponent),
            typeof(StateComponent),
            typeof(LevelComponent),
            typeof(GrowthComponent)
        };

        private static readonly Type[] _caps =
        {
            typeof(SkillCapability),
            typeof(SkillExecutorCapability)
        };

        public override Type[] Components => _comps;
        public override Type[] Capabilities => _caps;
    }
}


