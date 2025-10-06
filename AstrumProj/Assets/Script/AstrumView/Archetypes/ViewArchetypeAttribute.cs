using System;

namespace Astrum.View.Archetypes
{
    /// <summary>
    /// 视图原型与逻辑原型名的绑定属性。
    /// 一个 ViewArchetype 可以绑定一个或多个逻辑原型名。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ViewArchetypeAttribute : Attribute
    {
        public string LogicArchetypeName { get; }

        public ViewArchetypeAttribute(string logicArchetypeName)
        {
            LogicArchetypeName = logicArchetypeName;
        }
    }
}


