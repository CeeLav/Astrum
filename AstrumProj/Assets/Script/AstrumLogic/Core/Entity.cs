using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Systems;
using Astrum.LogicCore.Events;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.Managers;
using MemoryPack;
using cfg.BaseUnit;
using cfg;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 实体类，游戏中所有对象的基础类
    /// </summary>
    [MemoryPackable]
    public partial class Entity : IPool
    {
        private static long _nextId = 1;
        
        /// <summary>
        /// 对象是否来自对象池（IPool 接口实现）
        /// </summary>
        [MemoryPackIgnore]
        public bool IsFromPool { get; set; }

        /// <summary>
        /// 实体的全局唯一标识符
        /// </summary>
        public long UniqueId { get; set; }

        /// <summary>
        /// 实体名称，便于调试和识别
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 实体原型（用于生命周期钩子）
        /// </summary>
        public EArchetype Archetype { get; set; }
        
        public int EntityConfigId { get; set; } = 0;
        [MemoryPackIgnore]
        public BaseUnitTable EntityConfig
        {
            get
            {
                if (EntityConfigId == 0) return null;
                return TableConfig.Instance.Tables.TbBaseUnitTable.Get(EntityConfigId);
            }
        }


        /// <summary>
        /// 实体是否已销毁，用于生命周期管理
        /// </summary>
        public bool IsDestroyed { get; set; } = false;

        /// <summary>
        /// 实体创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 挂载的组件字典（ComponentTypeId -> Component Instance）
        /// 预分配容量 8，优化查询性能从 O(N) 到 O(1)
        /// </summary>
        public Dictionary<int, BaseComponent> Components { get; private set; } = new Dictionary<int, BaseComponent>(8);

        /// <summary>
        /// 父实体ID，-1表示无父实体
        /// </summary>
        public long ParentId { get; set; } = -1;

        /// <summary>
        /// 子实体ID列表（预分配容量 4）
        /// </summary>
        public List<long> ChildrenIds { get; private set; } = new List<long>(4);

        /// <summary>
        /// Capability 状态字典（TypeId -> 状态）
        /// 使用 int 类型的 TypeId 作为 Key，基于 TypeHash 生成，性能更高
        /// 预分配容量 4
        /// </summary>
        public Dictionary<int, CapabilityState> CapabilityStates { get; private set; } 
            = new Dictionary<int, CapabilityState>(4);
        
        /// <summary>
        /// 被禁用的 Tag 集合（Tag -> 禁用发起者实体ID集合）
        /// 用于支持基于 Tag 的 Capability 批量禁用
        /// 预分配容量 2
        /// </summary>
        public Dictionary<CapabilityTag, HashSet<long>> DisabledTags { get; private set; } 
            = new Dictionary<CapabilityTag, HashSet<long>>(2);

        /// <summary>
        /// 实体所属的 World（不序列化，由 World 设置）
        /// </summary>
        [MemoryPackIgnore]
        public World World { get; set; }

        /// <summary>
        /// 是否为影子实体（运行时标记，不序列化）
        /// </summary>
        [MemoryPackIgnore]
        public bool IsShadow { get; set; } = false;

        // 运行时子原型：激活列表与引入映射（用于安全卸载）
        // 预分配容量 2
        public List<EArchetype> ActiveSubArchetypes { get; private set; } = new List<EArchetype>(2);
        // 引用计数字典（类型名 -> 引用次数），用于决定组件/能力的装配与卸载
        // 预分配容量 8 和 4
        public Dictionary<string, int> ComponentRefCounts { get; private set; } = new Dictionary<string, int>(8, StringComparer.Ordinal);
        public Dictionary<string, int> CapabilityRefCounts { get; private set; } = new Dictionary<string, int>(4, StringComparer.Ordinal);
        
        /// <summary>
        /// 脏组件类型 ID 集合（使用 ComponentTypeId 作为唯一标识）
        /// 不序列化，运行时数据
        /// </summary>
        [MemoryPackIgnore]
        private HashSet<int> _dirtyComponentIds = new HashSet<int>();

        public Entity()
        {
            //UniqueId = _nextId++;
            CreationTime = DateTime.Now;
        }
        
        /// <summary>
        /// 获取下一个实体ID（静态方法，供外部使用）
        /// </summary>
        public static long GetNextId()
        {
            return _nextId++;
        }

        /// <summary>
        /// MemoryPack 构造函数（已更新：组件使用 Dictionary 存储）
        /// ⚠️ 序列化格式变更：不兼容旧版本存档
        /// </summary>
        [MemoryPackConstructor]
        public Entity(long uniqueId, string name, EArchetype archetype, int entityConfigId, bool isDestroyed, DateTime creationTime, Dictionary<int, BaseComponent> components, long parentId, List<long> childrenIds, Dictionary<int, CapabilityState> capabilityStates, Dictionary<CapabilityTag, HashSet<long>> disabledTags, List<EArchetype> activeSubArchetypes, Dictionary<string,int> componentRefCounts, Dictionary<string,int> capabilityRefCounts)
        {
            UniqueId = uniqueId;
            Name = name;
            Archetype = archetype;
            EntityConfigId = entityConfigId;
            IsDestroyed = isDestroyed;
            CreationTime = creationTime;
            Components = components ?? new Dictionary<int, BaseComponent>(8);
            ParentId = parentId;
            ChildrenIds = childrenIds;
            CapabilityStates = capabilityStates ?? new Dictionary<int, CapabilityState>();
            DisabledTags = disabledTags ?? new Dictionary<CapabilityTag, HashSet<long>>();

            // 同步 _nextId，确保反序列化后创建的新实体不会与已反序列化的实体ID冲突
            if (uniqueId >= _nextId)
            {
                _nextId = uniqueId + 1;
            }

            // 重建所有组件的 EntityId 关系
            foreach (var component in Components.Values)
            {
                component.EntityId = UniqueId;
            }

            ActiveSubArchetypes = activeSubArchetypes ?? new List<EArchetype>();
            ComponentRefCounts = componentRefCounts ?? new Dictionary<string, int>(StringComparer.Ordinal);
            CapabilityRefCounts = capabilityRefCounts ?? new Dictionary<string, int>(StringComparer.Ordinal);
        }

        /// <summary>
        /// 运行时挂载子原型（按需装配组件/能力，去重）。
        /// </summary>
        public bool AttachSubArchetype(EArchetype subArchetype, out string? reason)
        {
            reason = null;
            if (ActiveSubArchetypes.Contains(subArchetype)) { return true; }

            if (!Astrum.LogicCore.Archetypes.ArchetypeRegistry.Instance.TryGet(subArchetype, out var info))
            {
                reason = $"SubArchetype '{subArchetype}' not found";
                return false;
            }

            // 装配组件/能力（引用计数）
            var componentFactory = Astrum.LogicCore.Factories.ComponentFactory.Instance;
            foreach (var compType in info.Components ?? Array.Empty<Type>())
            {
                if (compType == null) continue;
                var key = GetTypeKey(compType);
                if (!ComponentRefCounts.TryGetValue(key, out var n)) n = 0;
                if (n == 0)
                {
                    var comp = componentFactory.CreateComponentFromType(compType);
                    if (comp == null) { reason = $"Create component '{compType.Name}' failed"; return false; }
                    AddComponent(comp);
                    comp.OnAttachToEntity(this);
                }
                ComponentRefCounts[key] = n + 1;
            }

            foreach (var capType in info.Capabilities ?? Array.Empty<Type>())
            {
                if (capType == null) continue;
                var key = GetTypeKey(capType);
                if (!CapabilityRefCounts.TryGetValue(key, out var n)) n = 0;
                
                // 检查是否是新的 ICapability 接口
                if (typeof(ICapability).IsAssignableFrom(capType))
                {
                    // 新方式：使用 CapabilitySystem
                    if (n == 0)
                    {
                        try
                        {
                            var capability = CapabilitySystem.GetCapability(capType);
                            if (capability == null)
                            {
                                reason = $"Capability {capType.Name} not registered in CapabilitySystem";
                                return false;
                            }
                            
                            var typeId = capability.TypeId;
                            
                            // 启用此 Capability（使用 TypeId，存在即表示拥有）
                            CapabilityStates[typeId] = new CapabilityState
                            {
                                IsActive = false,
                                ActiveDuration = 0,
                                DeactiveDuration = 0,
                                CustomData = new Dictionary<string, object>()
                            };
                            
                            // 注册到 CapabilitySystem
                            World?.CapabilitySystem?.RegisterEntityCapability(UniqueId, typeId);
                            
                            // 调用 OnAttached
                            capability.OnAttached(this);
                        }
                        catch (Exception ex)
                        {
                            reason = $"Create capability '{capType.Name}' failed: {ex.Message}";
                            return false;
                        }
                    }
                    CapabilityRefCounts[key] = n + 1;
                }
                else
                {
                    reason = $"Capability {capType.Name} does not implement ICapability interface";
                    return false;
                }
            }

            ActiveSubArchetypes.Add(subArchetype);
            
            // 发布子原型变化事件
            PublishSubArchetypeChangedEvent(subArchetype, SubArchetypeChangeType.Attach);
            
            return true;
        }

        /// <summary>
        /// 运行时卸载子原型：按声明差集卸载（主原型+其他子原型覆盖的成员保留）。
        /// </summary>
        public bool DetachSubArchetype(EArchetype subArchetype, out string? reason)
        {
            reason = null;
            if (!ActiveSubArchetypes.Contains(subArchetype)) return true;

            // 目标子原型声明
            if (!Astrum.LogicCore.Archetypes.ArchetypeRegistry.Instance.TryGet(subArchetype, out var subInfo))
            {
                ActiveSubArchetypes.Remove(subArchetype);
                return true;
            }

            // 引用计数减一，归零则移除
            foreach (var t in subInfo.Capabilities ?? Array.Empty<Type>())
            {
                if (t == null) continue;
                var key = GetTypeKey(t);
                if (!CapabilityRefCounts.TryGetValue(key, out var n)) n = 0;
                if (n > 0) n--;
                CapabilityRefCounts[key] = n;
                
                // 检查是否是新的 ICapability 接口
                if (typeof(ICapability).IsAssignableFrom(t))
                {
                    // 新方式：使用 CapabilitySystem
                    if (n == 0)
                    {
                        var capability = Systems.CapabilitySystem.GetCapability(t);
                        if (capability != null)
                        {
                            var typeId = capability.TypeId;
                            
                            // 调用 OnDetached
                            capability.OnDetached(this);
                            
                            // 移除状态（使用 TypeId）
                            CapabilityStates.Remove(typeId);
                            
                            // 从 CapabilitySystem 注销
                            World?.CapabilitySystem?.UnregisterEntityCapability(UniqueId, typeId);
                        }
                    }
                }
                else
                {
                    // 不是 ICapability 的类型（已废弃）
                }
            }

            foreach (var t in subInfo.Components ?? Array.Empty<Type>())
            {
                if (t == null) continue;
                var key = GetTypeKey(t);
                if (!ComponentRefCounts.TryGetValue(key, out var n)) n = 0;
                if (n > 0) n--;
                ComponentRefCounts[key] = n;
                if (n == 0)
                {
                    // 使用 TypeHash 查找组件（优化：避免遍历）
                    int typeId = t.AssemblyQualifiedName?.GetHashCode() ?? t.FullName?.GetHashCode() ?? 0;
                    if (Components.TryGetValue(typeId, out var comp))
                    {
                        Components.Remove(typeId);
                    }
                }
            }

            ActiveSubArchetypes.Remove(subArchetype);
            
            // 发布子原型变化事件
            PublishSubArchetypeChangedEvent(subArchetype, SubArchetypeChangeType.Detach);
            
            return true;
        }

        private static string GetTypeKey(Type t) => t.AssemblyQualifiedName ?? t.FullName;

        public bool IsSubArchetypeActive(EArchetype subArchetype)
        {
            return ActiveSubArchetypes.Contains(subArchetype);
        }

        public IReadOnlyList<EArchetype> ListActiveSubArchetypes() => ActiveSubArchetypes;

        /// <summary>
        /// 添加组件（优化：使用字典存储）
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件实例</param>
        public void AddComponent<T>(T component) where T : BaseComponent
        {
            if (component == null) return;

            component.EntityId = UniqueId;
            int typeId = component.GetComponentTypeId();
            Components[typeId] = component; // Dictionary 直接添加或覆盖
        }

        /// <summary>
        /// 移除组件（并回收到对象池）（优化：使用字典查询）
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        public void RemoveComponent<T>() where T : BaseComponent
        {
            int typeId = TypeHash<T>.GetHash();
            if (Components.TryGetValue(typeId, out var component))
            {
                // 从脏组件 ID 集合中移除
                _dirtyComponentIds.Remove(typeId);
                
                // 从字典移除
                Components.Remove(typeId);
                
                // 回收 Component 至对象池
                if (component.IsFromPool)
                {
                    component.Reset();
                    ObjectPool.Instance.Recycle(component);
                }
            }
        }

        /// <summary>
        /// 获取组件（优化：O(1) 字典查询）
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例，如果不存在返回null</returns>
        public T? GetComponent<T>() where T : BaseComponent
        {
            int typeId = TypeHash<T>.GetHash();
            if (Components.TryGetValue(typeId, out var component))
            {
                return component as T;
            }
            return null;
        }

        /// <summary>
        /// 检查是否有指定组件（优化：O(1) 字典查询）
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>是否拥有该组件</returns>
        public bool HasComponent<T>() where T : BaseComponent
        {
            int typeId = TypeHash<T>.GetHash();
            return Components.ContainsKey(typeId);
        }

        /// <summary>
        /// 获取所有组件（用于遍历，兼容性方法）
        /// </summary>
        /// <returns>所有组件的集合</returns>
        public IEnumerable<BaseComponent> GetAllComponents()
        {
            return Components.Values;
        }
        
        /// <summary>
        /// 按类型查找组件（非泛型版本，用于反射场景）
        /// </summary>
        public BaseComponent GetComponentByType(Type type)
        {
            // 使用类型完整名称的哈希作为 TypeId
            int typeId = type.AssemblyQualifiedName?.GetHashCode() ?? type.FullName?.GetHashCode() ?? 0;
            Components.TryGetValue(typeId, out var component);
            return component;
        }
        
        /// <summary>
        /// 检查是否有指定类型的组件（非泛型版本）
        /// </summary>
        public bool HasComponentOfType(Type type)
        {
            int typeId = type.AssemblyQualifiedName?.GetHashCode() ?? type.FullName?.GetHashCode() ?? 0;
            return Components.ContainsKey(typeId);
        }

        /// <summary>
        /// 应用输入到实体
        /// </summary>
        /// <param name="input">输入数据</param>
        public void ApplyInput(LSInput input)
        {
            var inputComponent = GetComponent<LSInputComponent>();
            inputComponent?.SetInput(input);
        }


        /// <summary>
        /// 销毁实体
        /// </summary>
        public void Destroy()
        {
            IsDestroyed = true;
            
            // 清理组件
            Components.Clear();
            
            // 清理 Capability 状态
            CapabilityStates.Clear();
        }

        /// <summary>
        /// 获取所有子实体
        /// </summary>
        /// <returns>子实体ID列表</returns>
        public List<long> GetChildren()
        {
            return new List<long>(ChildrenIds);
        }

        /// <summary>
        /// 设置父实体
        /// </summary>
        /// <param name="parentId">父实体ID</param>
        public void SetParent(long parentId)
        {
            ParentId = parentId;
        }

        /// <summary>
        /// 添加子实体
        /// </summary>
        /// <param name="childId">子实体ID</param>
        public void AddChild(long childId)
        {
            if (!ChildrenIds.Contains(childId))
            {
                ChildrenIds.Add(childId);
            }
        }

        /// <summary>
        /// 移除子实体
        /// </summary>
        /// <param name="childId">子实体ID</param>
        public void RemoveChild(long childId)
        {
            ChildrenIds.Remove(childId);
        }


        /// <summary>
        /// 获取实体需要的组件类型列表
        /// </summary>
        /// <returns>组件类型列表</returns>
        public virtual Type[] GetRequiredComponentTypes()
        {
            return new Type[0];
        }

        /// <summary>
        /// 获取实体需要的能力类型列表
        /// </summary>
        /// <returns>能力类型列表</returns>
        public virtual Type[] GetRequiredCapabilityTypes()
        {
            return new Type[0];
        }
        
        /// <summary>
        /// 发布子原型变化事件
        /// </summary>
        /// <param name="subArchetype">子原型类型</param>
        /// <param name="changeType">变化类型</param>
        private void PublishSubArchetypeChangedEvent(EArchetype subArchetype, SubArchetypeChangeType changeType)
        {
            if (World == null) return;
            
            // 使用视图事件队列（新机制）
            var eventData = new EntitySubArchetypeChangedEventData(this, World.WorldId, World.RoomId, subArchetype, changeType);
            this.QueueViewEvent(new ViewEvent(ViewEventType.SubArchetypeChanged, eventData, World.CurFrame));
            
            // 保留 EventSystem 发布用于其他系统
            // 注释掉以避免重复处理，但保留代码便于回退
            // EventSystem.Instance.Publish(eventData);
        }

        /// <summary>
        /// 记录组件为脏（由 Capability 调用）
        /// </summary>
        /// <param name="componentTypeId">组件的 ComponentTypeId</param>
        public void MarkComponentDirty(int componentTypeId)
        {
            _dirtyComponentIds.Add(componentTypeId);
        }
        
        /// <summary>
        /// 获取所有脏组件的 ComponentTypeId
        /// </summary>
        /// <returns>脏组件的 ComponentTypeId 集合</returns>
        public IReadOnlyCollection<int> GetDirtyComponentIds()
        {
            return _dirtyComponentIds;
        }
        
        /// <summary>
        /// 根据 ComponentTypeId 获取对应的组件实例（优化：直接字典查询）
        /// </summary>
        /// <param name="componentTypeId">组件的 ComponentTypeId</param>
        /// <returns>组件实例，如果不存在返回 null</returns>
        public BaseComponent GetComponentById(int componentTypeId)
        {
            Components.TryGetValue(componentTypeId, out var component);
            return component;
        }
        
        /// <summary>
        /// 获取所有脏组件实例（优化：直接字典查询）
        /// </summary>
        /// <returns>脏组件实例列表</returns>
        public List<BaseComponent> GetDirtyComponents()
        {
            var dirtyComponents = new List<BaseComponent>(_dirtyComponentIds.Count);
            foreach (var componentId in _dirtyComponentIds)
            {
                if (Components.TryGetValue(componentId, out var component))
                {
                    dirtyComponents.Add(component);
                }
            }
            return dirtyComponents;
        }
        
        /// <summary>
        /// 清除所有脏标记
        /// </summary>
        public void ClearDirtyComponents()
        {
            _dirtyComponentIds.Clear();
        }
        
        /// <summary>
        /// 重置 Entity 状态（用于对象池回收）
        /// </summary>
        public virtual void Reset()
        {
            // 重置基础字段
            UniqueId = 0;
            Name = string.Empty;
            Archetype = default(EArchetype);
            EntityConfigId = 0;
            IsDestroyed = false;
            CreationTime = default(DateTime);
            
            // 清空所有集合（保留对象引用，只清空内容）
            Components.Clear();
            ChildrenIds.Clear();
            CapabilityStates.Clear();
            DisabledTags.Clear();
            ActiveSubArchetypes.Clear();
            ComponentRefCounts.Clear();
            CapabilityRefCounts.Clear();
            _dirtyComponentIds.Clear();
            
            // 重置引用字段
            ParentId = -1;
            World = null;
        }
        
        /// <summary>
        /// 重写 ToString 方法，避免循环引用
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"Entity[Id={UniqueId}, Name={Name}, Components={Components.Count}, Capabilities={CapabilityStates.Count}]";
        }
    }
}
