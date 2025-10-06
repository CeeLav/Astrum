using System;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Archetypes
{
    [Archetype("Controllable")]
    public class ControllableArchetype : Archetype
    {
        private static readonly Type[] _comps =
        {
            typeof(LSInputComponent)
        };

        public override Type[] Components => _comps;
    }
}


