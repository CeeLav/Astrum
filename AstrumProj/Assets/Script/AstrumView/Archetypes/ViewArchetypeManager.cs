using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Astrum.LogicCore.Archetypes;
using cfg;

namespace Astrum.View.Archetypes
{
    /// <summary>
    /// 视图原型管理器（单例）：扫描 AstrumView 程序集，收集各逻辑原型名对应的 ViewComponents 并集。
    /// </summary>
    public sealed class ViewArchetypeManager
    {
        private static readonly Lazy<ViewArchetypeManager> _instance = new Lazy<ViewArchetypeManager>(() => new ViewArchetypeManager());
        public static ViewArchetypeManager Instance => _instance.Value;

        private readonly Dictionary<EArchetype, HashSet<Type>> _logicArchetypeToViewComponents = new Dictionary<EArchetype, HashSet<Type>>();
        private bool _initialized;

        private ViewArchetypeManager() { }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var asm = Assembly.GetExecutingAssembly();
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || !typeof(ViewArchetype).IsAssignableFrom(type)) continue;
                var attrs = type.GetCustomAttributes(typeof(ViewArchetypeAttribute), false) as ViewArchetypeAttribute[];
                if (attrs == null || attrs.Length == 0) continue;

                var instance = Activator.CreateInstance(type) as ViewArchetype;
                if (instance == null) continue;
                var comps = instance.ViewComponents ?? Array.Empty<Type>();

                foreach (var attr in attrs)
                {
                    if (string.IsNullOrWhiteSpace(attr.LogicArchetypeName)) continue;
                    if (!Enum.TryParse<EArchetype>(attr.LogicArchetypeName, true, out var archetypeEnum)) continue;
                    if (!_logicArchetypeToViewComponents.TryGetValue(archetypeEnum, out var set))
                    {
                        set = new HashSet<Type>();
                        _logicArchetypeToViewComponents[archetypeEnum] = set;
                    }
                    foreach (var c in comps)
                    {
                        if (c != null) set.Add(c);
                    }
                }
            }
        }

        /// <summary>
        /// 获取指定逻辑原型对应的视图组件并集（包含其合并父链）。
        /// </summary>
        public bool TryGetComponents(EArchetype logicArchetype, out Type[] viewComponents)
        {
            if (!_initialized) Initialize();
            var result = new HashSet<Type>();

            var chain = ArchetypeRegistry.Instance.GetMergeChain(logicArchetype);
            foreach (var archetype in chain)
            {
                if (_logicArchetypeToViewComponents.TryGetValue(archetype, out var set))
                {
                    foreach (var t in set)
                    {
                        if (t != null) result.Add(t);
                    }
                }
            }

            viewComponents = result.ToArray();
            return result.Count > 0;
        }
    }
}


