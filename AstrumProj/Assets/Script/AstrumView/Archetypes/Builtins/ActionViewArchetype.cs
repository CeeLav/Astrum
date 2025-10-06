using System;
using Astrum.View.Components;

namespace Astrum.View.Archetypes
{
    [ViewArchetype("Action")]
    public class ActionViewArchetype : ViewArchetype
    {
        private static readonly Type[] _comps =
        {
            typeof(AnimationViewComponent)
        };

        public override Type[] ViewComponents => _comps;
    }
}


