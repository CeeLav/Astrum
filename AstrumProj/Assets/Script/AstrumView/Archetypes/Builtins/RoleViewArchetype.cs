using System;
using Astrum.View.Components;

namespace Astrum.View.Archetypes
{
    [ViewArchetype("Role")]
    public class RoleViewArchetype : ViewArchetype
    {
        private static readonly Type[] _comps =
        {
            
        };

        public override Type[] ViewComponents => _comps;
    }
}


