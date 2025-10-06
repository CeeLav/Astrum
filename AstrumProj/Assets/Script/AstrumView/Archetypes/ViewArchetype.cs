using System;

namespace Astrum.View.Archetypes
{
    /// <summary>
    /// 视图原型基类：仅声明视图组件集合。具体 EntityView 类型由工程层决定。
    /// </summary>
    public abstract class ViewArchetype
    {
        public virtual Type[] ViewComponents => Array.Empty<Type>();
    }
}


