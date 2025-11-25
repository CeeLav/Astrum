using System;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;
using cfg;

namespace Astrum.LogicCore.Archetypes
{
    [Archetype(EArchetype.Projectile)]
    public class ProjectileArchetype : Archetype
    {
        private static readonly Type[] _components =
        {
            typeof(TransComponent),
            typeof(ProjectileComponent)
        };

        private static readonly Type[] _capabilities =
        {
            typeof(ProjectileCapability),
            typeof(DeadCapability)
        };

        public override Type[] Components => _components;
        public override Type[] Capabilities => _capabilities;
    }
}
