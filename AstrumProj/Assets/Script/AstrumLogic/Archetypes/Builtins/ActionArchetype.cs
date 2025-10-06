using System;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;

namespace Astrum.LogicCore.Archetypes
{
    [Archetype("Action")]
    public class ActionArchetype : Archetype
    {
        private static readonly Type[] _comps =
        {
            typeof(ActionComponent)
        };

        private static readonly Type[] _caps =
        {
            typeof(ActionCapability)
        };

        public override Type[] Components => _comps;
        public override Type[] Capabilities => _caps;
    }
}


