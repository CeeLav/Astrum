using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Astrum.LogicCore.Archetypes
{
    /// <summary>
    /// Archetype 管理器（单例）。
    /// 说明：当前提供最小可用外观，便于后续接入真实 Archetype 扫描/合并逻辑。
    /// </summary>
    public sealed class ArchetypeManager
    {
        private static readonly Lazy<ArchetypeManager> _instance = new Lazy<ArchetypeManager>(() => new ArchetypeManager());
        public static ArchetypeManager Instance => _instance.Value;

        private readonly Dictionary<string, ArchetypeInfo> _nameToInfo = new Dictionary<string, ArchetypeInfo>(StringComparer.OrdinalIgnoreCase);

        private ArchetypeManager()
        {
        }

        /// <summary>
        /// 最小初始化（后续可扩展为反射扫描与递归合并）。
        /// </summary>
        public void Initialize()
        {
            _nameToInfo.Clear();

            // 扫描当前域的所有程序集，收集 Archetype
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }
                if (types == null) continue;

                foreach (var t in types)
                {
                    if (t == null || t.IsAbstract || !typeof(Archetype).IsAssignableFrom(t)) continue;
                    var attr = t.GetCustomAttribute<ArchetypeAttribute>();
                    if (attr == null || string.IsNullOrWhiteSpace(attr.Name)) continue;

                    var instance = Activator.CreateInstance(t) as Archetype;
                    if (instance == null) continue;

                    _nameToInfo[attr.Name] = new ArchetypeInfo
                    {
                        Name = attr.Name,
                        Merge = attr.Merge ?? Array.Empty<string>(),
                        RawMerge = attr.Merge ?? Array.Empty<string>(),
                        Components = instance.Components ?? Array.Empty<Type>(),
                        Capabilities = instance.Capabilities ?? Array.Empty<Type>()
                    };
                }
            }

            // 合并展开
            foreach (var kv in _nameToInfo.ToArray())
            {
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Expand(kv.Value, visited);
            }
        }

        private void Expand(ArchetypeInfo data, HashSet<string> visited)
        {
            if (visited.Contains(data.Name)) return;
            visited.Add(data.Name);

            var comps = new HashSet<Type>(data.Components ?? Array.Empty<Type>());
            var caps = new HashSet<Type>(data.Capabilities ?? Array.Empty<Type>());

            foreach (var parentName in data.Merge ?? Array.Empty<string>())
            {
                if (!_nameToInfo.TryGetValue(parentName, out var parent))
                    throw new Exception($"Missing merged archetype '{parentName}' in '{data.Name}'");

                Expand(parent, visited);
                foreach (var c in parent.Components ?? Array.Empty<Type>()) comps.Add(c);
                foreach (var c in parent.Capabilities ?? Array.Empty<Type>()) caps.Add(c);
            }

            data.Components = comps.ToArray();
            data.Capabilities = caps.ToArray();
            _nameToInfo[data.Name] = data;
        }

        public bool TryGet(string name, out ArchetypeInfo info)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                info = default;
                return false;
            }
            return _nameToInfo.TryGetValue(name, out info);
        }

        public ArchetypeInfo Get(string name)
        {
            if (TryGet(name, out var info)) return info;
            throw new Exception($"Archetype '{name}' not found");
        }

        public IEnumerable<string> AllNames => _nameToInfo.Keys;

        /// <summary>
        /// 获取给定 Archetype 的合并链（父 → ... → 子，不包含重复）。
        /// 返回序列末尾为传入的 name 本身。
        /// </summary>
        public IList<string> GetMergeChain(string name)
        {
            var sequence = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            BuildChain(name, sequence, visited);
            return sequence;
        }

        private void BuildChain(string name, IList<string> seq, HashSet<string> visited)
        {
            if (string.IsNullOrWhiteSpace(name) || visited.Contains(name)) return;
            visited.Add(name);
            if (!_nameToInfo.TryGetValue(name, out var info)) return;
            foreach (var p in info.RawMerge ?? Array.Empty<string>())
            {
                BuildChain(p, seq, visited);
            }
            if (!seq.Contains(name, StringComparer.OrdinalIgnoreCase)) seq.Add(name);
        }
    }

    /// <summary>
    /// 占位信息结构，后续可扩展为组件/能力并集等。
    /// </summary>
    public struct ArchetypeInfo
    {
        public string Name;
        public string[] Merge;
        public string[] RawMerge;
        public Type[] Components;
        public Type[] Capabilities;
    }
}


