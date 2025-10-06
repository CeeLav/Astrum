using System;
using Astrum.View.Components;

namespace Astrum.View.Archetypes
{
    [ViewArchetype("BaseUnit")]
    public class BaseUnitViewArchetype : ViewArchetype
    {
        private static readonly Type[] _comps =
        {
            typeof(ModelViewComponent),
            typeof(TransViewComponent)
        };

        public override Type[] ViewComponents => _comps;
    }
}


