using System;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;

namespace Astrum.LogicCore.Archetypes
{
    [Archetype("Projectile")]
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
