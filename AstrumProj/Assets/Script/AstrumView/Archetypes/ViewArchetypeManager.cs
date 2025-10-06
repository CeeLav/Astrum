using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Astrum.LogicCore.Archetypes;

namespace Astrum.View.Archetypes
{
    /// <summary>
    /// 视图原型管理器（单例）：扫描 AstrumView 程序集，收集各逻辑原型名对应的 ViewComponents 并集。
    /// </summary>
    public sealed class ViewArchetypeManager
    {
        private static readonly Lazy<ViewArchetypeManager> _instance = new Lazy<ViewArchetypeManager>(() => new ViewArchetypeManager());
        public static ViewArchetypeManager Instance => _instance.Value;

        private readonly Dictionary<string, HashSet<Type>> _logicNameToViewComponents = new Dictionary<string, HashSet<Type>>(StringComparer.OrdinalIgnoreCase);
        // 不再在视图侧维护父链，直接读取逻辑侧的合并链

        private bool _initialized;

        private ViewArchetypeManager() { }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // 1) 收集 ViewArchetype 声明
            var asm = Assembly.GetExecutingAssembly();
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || !typeof(ViewArchetype).IsAssignableFrom(type)) continue;
                var attrs = type.GetCustomAttributes(typeof(ViewArchetypeAttribute), false) as ViewArchetypeAttribute[];
                if (attrs == null || attrs.Length == 0) continue;

                // 实例化以获取 ViewComponents
                var instance = Activator.CreateInstance(type) as ViewArchetype;
                if (instance == null) continue;
                var comps = instance.ViewComponents ?? Array.Empty<Type>();

                foreach (var attr in attrs)
                {
                    if (string.IsNullOrWhiteSpace(attr.LogicArchetypeName)) continue;
                    if (!_logicNameToViewComponents.TryGetValue(attr.LogicArchetypeName, out var set))
                    {
                        set = new HashSet<Type>();
                        _logicNameToViewComponents[attr.LogicArchetypeName] = set;
                    }
                    foreach (var c in comps)
                    {
                        if (c != null) set.Add(c);
                    }
                }
            }

            // 父链由逻辑侧的 ArchetypeManager 提供
        }

        /// <summary>
        /// 获取指定逻辑原型名对应的视图组件并集（包含其合并父链）。
        /// </summary>
        public bool TryGetComponents(string logicArchetypeName, out Type[] viewComponents)
        {
            if (!_initialized) Initialize();
            var result = new HashSet<Type>();

            // 先合并父链（深度优先，自底向上并集）
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            MergeChain(logicArchetypeName, visited, result);

            viewComponents = result.ToArray();
            return result.Count > 0;
        }

        private void MergeChain(string logicName, HashSet<string> visited, HashSet<Type> accumulator)
        {
            if (string.IsNullOrWhiteSpace(logicName)) return;
            var chain = ArchetypeManager.Instance.GetMergeChain(logicName); // 父 → ... → 自身
            foreach (var name in chain)
            {
                if (_logicNameToViewComponents.TryGetValue(name, out var set))
                {
                    foreach (var t in set) if (t != null) accumulator.Add(t);
                }
            }
        }
    }
}


