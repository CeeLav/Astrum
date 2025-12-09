using cfg;
using System;

namespace Astrum.LogicCore.Archetypes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ArchetypeAttribute : Attribute
    {
        public EArchetype Archetype { get; }
        public EArchetype[] Merge { get; }

        public ArchetypeAttribute(EArchetype archetype, params EArchetype[] merge)
        {
            Archetype = archetype;
            Merge = merge ?? Array.Empty<EArchetype>();
        }
    }
}


