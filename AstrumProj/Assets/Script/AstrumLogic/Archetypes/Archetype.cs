using System;

namespace Astrum.LogicCore.Archetypes
{
    /// <summary>
    /// 逻辑原型基类：声明本体“增量”的组件与能力集合。
    /// </summary>
    public abstract class Archetype
    {
        public virtual Type[] Components => Array.Empty<Type>();
        public virtual Type[] Capabilities => Array.Empty<Type>();
    }
}


