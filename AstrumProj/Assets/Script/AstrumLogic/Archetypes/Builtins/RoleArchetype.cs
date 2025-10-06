using System;

namespace Astrum.LogicCore.Archetypes
{
    [Archetype("Role", "BaseUnit", "Action", "Controllable")]
    public class RoleArchetype : Archetype
    {
        private static readonly Type[] _comps = Array.Empty<Type>();
        public override Type[] Components => _comps;
    }
}


