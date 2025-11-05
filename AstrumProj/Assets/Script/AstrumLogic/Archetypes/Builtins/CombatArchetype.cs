using System;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;
using Astrum.LogicCore.Core;

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
            typeof(SkillExecutorCapability),
            typeof(SkillDisplacementCapabilityV2)  // 使用新架构的 SkillDisplacementCapability
        };

        public override Type[] Components => _comps;
        public override Type[] Capabilities => _caps;

        public new static void OnAfterComponentsAttach(Entity entity, World world)
        {
            // 初始化数值系统组件
            var baseStats = entity.GetComponent<BaseStatsComponent>();
            var derivedStats = entity.GetComponent<DerivedStatsComponent>();
            var dynamicStats = entity.GetComponent<DynamicStatsComponent>();
            derivedStats.RecalculateAll(baseStats);
            dynamicStats.InitializeResources(derivedStats);
        }
    }
}


