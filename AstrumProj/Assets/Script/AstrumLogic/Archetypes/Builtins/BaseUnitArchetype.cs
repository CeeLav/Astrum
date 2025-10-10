using System;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;

namespace Astrum.LogicCore.Archetypes
{
    [Archetype("BaseUnit")]
    public class BaseUnitArchetype : Archetype
    {
        private static readonly Type[] _comps =
        {
            typeof(TransComponent),  // 包含 Position + Rotation
            typeof(MovementComponent),
            typeof(CollisionComponent)
        };

        private static readonly Type[] _caps =
        {
            typeof(MovementCapability)
        };

        public override Type[] Components => _comps;
        public override Type[] Capabilities => _caps;
    }
}


