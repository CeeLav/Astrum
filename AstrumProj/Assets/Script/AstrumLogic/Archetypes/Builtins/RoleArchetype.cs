using System;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;

namespace Astrum.LogicCore.Archetypes
{
    [Archetype("Role", "BaseUnit", "Action", "Controllable")]
    public class RoleArchetype : Archetype
    {
        private static readonly Type[] _comps = 
        {
            typeof(RoleInfoComponent),
            typeof(SkillComponent),
            typeof(HealthComponent)
        };
        
        private static readonly Type[] _caps =
        {
            typeof(SkillCapability)
        };

        public override Type[] Components => _comps;
        public override Type[] Capabilities => _caps;
    }
}


