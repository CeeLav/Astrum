using System;

namespace Astrum.LogicCore.Archetypes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ArchetypeAttribute : Attribute
    {
        public string Name { get; }
        public string[] Merge { get; }

        public ArchetypeAttribute(string name, params string[] merge)
        {
            Name = name;
            Merge = merge ?? Array.Empty<string>();
        }
    }
}


