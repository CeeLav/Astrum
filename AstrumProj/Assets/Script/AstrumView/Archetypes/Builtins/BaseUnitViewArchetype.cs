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
            typeof(PredictedMovementViewComponent),
            typeof(HUDViewComponent),
            typeof(VFXViewComponent),
#if UNITY_EDITOR
            typeof(CollisionDebugViewComponent)
#endif
            
        };

        public override Type[] ViewComponents => _comps;
    }
}


