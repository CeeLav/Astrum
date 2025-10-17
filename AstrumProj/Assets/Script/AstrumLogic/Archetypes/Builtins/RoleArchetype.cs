using System;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;

namespace Astrum.LogicCore.Archetypes
{
    [Archetype("Role", "BaseUnit", "Combat", "Controllable")]
    public class RoleArchetype : Archetype
    {
        private static readonly Type[] _comps = 
        {
            typeof(RoleInfoComponent),

        };
        
        private static readonly Type[] _caps =
        {
            
        };

        public override Type[] Components => _comps;
        public override Type[] Capabilities => _caps;
    }
}


