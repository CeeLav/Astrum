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
            typeof(GrowthComponent),
            // 击退组件
            typeof(KnockbackComponent)
        };

        private static readonly Type[] _caps =
        {
            typeof(SkillCapability),
            typeof(SkillExecutorCapability),     // Priority 250 - 技能执行
            typeof(ProjectileSpawnCapability),   // Priority 260 - 抛射物生成
            typeof(DamageCapability),            // Priority 200 - 伤害处理
            typeof(HitReactionCapability),       // Priority 200 - 受击反应
            typeof(KnockbackCapability),         // Priority 150 - 击退
            typeof(SkillDisplacementCapability)  // Priority 150 - 技能位移
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


