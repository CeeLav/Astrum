using System;
using Astrum.View.Components;

namespace Astrum.View.Archetypes
{
    /// <summary>
    /// Projectile 实体的视图原型
    /// </summary>
    [ViewArchetype("Projectile")]
    public class ProjectileViewArchetype : ViewArchetype
    {
        private static readonly Type[] _comps =
        {
            typeof(TransViewComponent),
            typeof(ProjectileViewComponent),
#if UNITY_EDITOR
            typeof(CollisionDebugViewComponent)
#endif
        };

        public override Type[] ViewComponents => _comps;
    }
}

