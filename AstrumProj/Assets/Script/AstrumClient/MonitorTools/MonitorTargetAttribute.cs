using System;

namespace AstrumClient.MonitorTools
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class MonitorTargetAttribute : Attribute
    {
    }
}
