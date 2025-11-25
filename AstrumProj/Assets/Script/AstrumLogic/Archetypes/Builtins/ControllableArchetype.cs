using System;
using Astrum.LogicCore.FrameSync;
using cfg;

namespace Astrum.LogicCore.Archetypes
{
    [Archetype(EArchetype.Controllable)]
    public class ControllableArchetype : Archetype
    {
        private static readonly Type[] _comps =
        {
            typeof(LSInputComponent)
        };

        public override Type[] Components => _comps;
    }
}


