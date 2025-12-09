using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Astrum.CommonBase;
using cfg;

namespace Astrum.LogicCore.Archetypes
{
    /// <summary>
    /// Archetype 管理器（单例）- 负责扫描、注册和合并所有 Archetype。
    /// </summary>
    public sealed class ArchetypeRegistry : Singleton<ArchetypeRegistry>
    {


        private readonly Dictionary<EArchetype, ArchetypeInfo> _archetypeToInfo = new Dictionary<EArchetype, ArchetypeInfo>();

        public ArchetypeRegistry()
        {
        }

        /// <summary>
        /// 初始化：扫描所有程序集，收集并合并 Archetype。
        /// </summary>
        public void Initialize()
        {
            _archetypeToInfo.Clear();

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
                    if (attr == null) continue;

                    var instance = Activator.CreateInstance(t) as Archetype;
                    if (instance == null) continue;

                    _archetypeToInfo[attr.Archetype] = new ArchetypeInfo
                    {
                        archetype = attr.Archetype,
                        RawMerge = attr.Merge ?? Array.Empty<EArchetype>(),
                        Components = instance.Components ?? Array.Empty<Type>(),
                        Capabilities = instance.Capabilities ?? Array.Empty<Type>(),
                        Instance = instance,  // 保存 Archetype 实例
                        ArchetypeType = t  // 保存 Archetype 类型
                    };
                }
            }

            // 合并展开
            foreach (var kv in _archetypeToInfo.ToArray())
            {
                var visited = new HashSet<EArchetype>();
                Expand(kv.Value, visited);
            }
        }

        private void Expand(ArchetypeInfo data, HashSet<EArchetype> visited)
        {
            if (visited.Contains(data.archetype)) return;
            visited.Add(data.archetype);

            var comps = new HashSet<Type>(data.Components ?? Array.Empty<Type>());
            var caps = new HashSet<Type>(data.Capabilities ?? Array.Empty<Type>());

            foreach (var parentArchetype in data.RawMerge ?? Array.Empty<EArchetype>())
            {
                if (!_archetypeToInfo.TryGetValue(parentArchetype, out var parent))
                    throw new Exception($"Missing merged archetype '{parentArchetype}' in '{data.archetype}'");

                Expand(parent, visited);
                foreach (var c in parent.Components ?? Array.Empty<Type>()) comps.Add(c);
                foreach (var c in parent.Capabilities ?? Array.Empty<Type>()) caps.Add(c);
            }

            data.Components = comps.ToArray();
            data.Capabilities = caps.ToArray();
            _archetypeToInfo[data.archetype] = data;
        }

        public bool TryGet(EArchetype archetype, out ArchetypeInfo info)
        {
            return _archetypeToInfo.TryGetValue(archetype, out info);
        }

        public ArchetypeInfo Get(EArchetype archetype)
        {
            if (TryGet(archetype, out var info)) return info;
            throw new Exception($"Archetype '{archetype}' not found");
        }

        public IEnumerable<EArchetype> AllArchetypes => _archetypeToInfo.Keys;

        /// <summary>
        /// 获取给定 Archetype 的合并链（父 → ... → 子，不包含重复）。
        /// 返回序列末尾为传入的 archetype 本身。
        /// </summary>
        public IList<EArchetype> GetMergeChain(EArchetype archetype)
        {
            var sequence = new List<EArchetype>();
            var visited = new HashSet<EArchetype>();
            BuildChain(archetype, sequence, visited);
            return sequence;
        }

        private void BuildChain(EArchetype archetype, IList<EArchetype> seq, HashSet<EArchetype> visited)
        {
            if (visited.Contains(archetype)) return;
            visited.Add(archetype);
            if (!_archetypeToInfo.TryGetValue(archetype, out var info)) return;
            foreach (var parentArchetype in info.RawMerge ?? Array.Empty<EArchetype>())
            {
                BuildChain(parentArchetype, seq, visited);
            }
            if (!seq.Contains(archetype)) seq.Add(archetype);
        }
    }

    /// <summary>
    /// Archetype 信息结构，包含名称、合并关系、组件和能力列表。
    /// </summary>
    public struct ArchetypeInfo
    {
        public EArchetype archetype;
        public EArchetype[] RawMerge;  // 直接父 Archetype 列表
        public Type[] Components;   // 合并后的组件类型列表
        public Type[] Capabilities; // 合并后的能力类型列表
        public Archetype Instance;  // Archetype 实例（用于调用生命周期钩子）
        public Type ArchetypeType;  // Archetype 类型（用于反射调用静态方法）
    }
}


