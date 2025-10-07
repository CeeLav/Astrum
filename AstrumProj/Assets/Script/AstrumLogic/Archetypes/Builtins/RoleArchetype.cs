using System;
using Astrum.LogicCore.Components;

namespace Astrum.LogicCore.Archetypes
{
    [Archetype("Role", "BaseUnit", "Action", "Controllable")]
    public class RoleArchetype : Archetype
    {
        private static readonly Type[] _comps = 
        {
            typeof(RoleInfoComponent)
        };
        
        public override Type[] Components => _comps;
    }
}


