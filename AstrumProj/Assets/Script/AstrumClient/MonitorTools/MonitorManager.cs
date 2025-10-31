using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AstrumClient.MonitorTools
{
    public static class MonitorManager
    {
        private static readonly List<WeakReference> _monitoredObjects = new List<WeakReference>();

        public static void Register(object instance)
        {
            if (instance == null) return;
            lock (_monitoredObjects)
                _monitoredObjects.Add(new WeakReference(instance));
        }

        public static void Unregister(object instance)
        {
            if (instance == null) return;
            lock (_monitoredObjects)
            {
                _monitoredObjects.RemoveAll(w => !w.IsAlive || w.Target == instance);
            }
        }

        public static IEnumerable<object> GetAllAliveMonitored()
        {
            lock (_monitoredObjects)
            {
                _monitoredObjects.RemoveAll(w => !w.IsAlive);
                foreach (var wr in _monitoredObjects)
                {
                    var obj = wr.Target;
                    if (obj != null)
                        yield return obj;
                }
            }
        }
    }
}
