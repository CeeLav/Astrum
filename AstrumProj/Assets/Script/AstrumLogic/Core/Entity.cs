using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Systems;
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
    public partial class Entity
    {
        private static long _nextId = 1;

        /// <summary>
        /// 实体的全局唯一标识符
        /// </summary>
        public long UniqueId { get; private set; }

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
        /// 实体是否激活，控制逻辑执行
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 实体是否已销毁，用于生命周期管理
        /// </summary>
        public bool IsDestroyed { get; set; } = false;

        /// <summary>
        /// 实体创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 组件掩码，用于快速查询和筛选
        /// </summary>
        public long ComponentMask { get; private set; } = 0;

        /// <summary>
        /// 挂载的组件列表
        /// </summary>
        public List<BaseComponent> Components { get; private set; } = new List<BaseComponent>();

        /// <summary>
        /// 父实体ID，-1表示无父实体
        /// </summary>
        public long ParentId { get; set; } = -1;

        /// <summary>
        /// 子实体ID列表
        /// </summary>
        public List<long> ChildrenIds { get; private set; } = new List<long>();

        /// <summary>
        /// Capability 状态字典（TypeId -> 状态）
        /// 使用 int 类型的 TypeId 作为 Key，基于 TypeHash 生成，性能更高
        /// </summary>
        public Dictionary<int, CapabilityState> CapabilityStates { get; private set; } 
            = new Dictionary<int, CapabilityState>();
        
        /// <summary>
        /// 被禁用的 Tag 集合（Tag -> 禁用发起者实体ID集合）
        /// 用于支持基于 Tag 的 Capability 批量禁用
        /// </summary>
        public Dictionary<CapabilityTag, HashSet<long>> DisabledTags { get; private set; } 
            = new Dictionary<CapabilityTag, HashSet<long>>();

        /// <summary>
        /// 实体所属的 World（不序列化，由 World 设置）
        /// </summary>
        [MemoryPackIgnore]
        public World World { get; set; }

        // 运行时子原型：激活列表与引入映射（用于安全卸载）
        public List<EArchetype> ActiveSubArchetypes { get; private set; } = new List<EArchetype>();
        // 引用计数字典（类型名 -> 引用次数），用于决定组件/能力的装配与卸载
        public Dictionary<string, int> ComponentRefCounts { get; private set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> CapabilityRefCounts { get; private set; } = new Dictionary<string, int>(StringComparer.Ordinal);

        public Entity()
        {
            UniqueId = _nextId++;
            CreationTime = DateTime.Now;
        }

        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public Entity(long uniqueId, string name, EArchetype archetype, int entityConfigId,bool isActive, bool isDestroyed, DateTime creationTime, long componentMask, List<BaseComponent> components, long parentId, List<long> childrenIds, Dictionary<int, CapabilityState> capabilityStates, Dictionary<CapabilityTag, HashSet<long>> disabledTags, List<EArchetype> activeSubArchetypes, Dictionary<string,int> componentRefCounts, Dictionary<string,int> capabilityRefCounts)
        {
            UniqueId = uniqueId;
            Name = name;
            Archetype = archetype;
            EntityConfigId = entityConfigId;
            IsActive = isActive;
            IsDestroyed = isDestroyed;
            CreationTime = creationTime;
            ComponentMask = componentMask;
            Components = components;
            ParentId = parentId;
            ChildrenIds = childrenIds;
            CapabilityStates = capabilityStates ?? new Dictionary<int, CapabilityState>();
            DisabledTags = disabledTags ?? new Dictionary<CapabilityTag, HashSet<long>>();

            foreach (var component in Components)
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
                    var comp = Components.FirstOrDefault(c => c.GetType() == t);
                    if (comp != null)
                    {
                        Components.Remove(comp);
                        UpdateComponentMask();
                        PublishComponentChangedEvent(comp, "Remove");
                    }
                }
            }

            ActiveSubArchetypes.Remove(subArchetype);
            return true;
        }

        private void RollbackAdded(EArchetype subArchetype, List<string> addedCompTypes, List<string> addedCapTypes)
        {
            // 旧 Capability 已废弃，不需要回滚
            // 移除刚添加的组件
            foreach (var tName in addedCompTypes)
            {
                var t = Type.GetType(tName);
                if (t == null) continue;
                var comp = Components.FirstOrDefault(c => c.GetType() == t);
                if (comp != null)
                {
                    Components.Remove(comp);
                    UpdateComponentMask();
                    PublishComponentChangedEvent(comp, "Remove");
                }
            }
        }

        private static string GetTypeKey(Type t) => t.AssemblyQualifiedName ?? t.FullName;

        public bool IsSubArchetypeActive(EArchetype subArchetype)
        {
            return ActiveSubArchetypes.Contains(subArchetype);
        }

        public IReadOnlyList<EArchetype> ListActiveSubArchetypes() => ActiveSubArchetypes;

        /// <summary>
        /// 添加组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件实例</param>
        public void AddComponent<T>(T component) where T : BaseComponent
        {
            if (component == null) return;

            component.EntityId = UniqueId;
            Components.Add(component);
            
            // 更新组件掩码
            UpdateComponentMask();
            
            // 发布组件添加事件
            PublishComponentChangedEvent(component, "Add");
        }

        /// <summary>
        /// 移除组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        public void RemoveComponent<T>() where T : BaseComponent
        {
            var component = Components.FirstOrDefault(c => c is T);
            if (component != null)
            {
                Components.Remove(component);
                UpdateComponentMask();
                
                // 发布组件移除事件
                PublishComponentChangedEvent(component, "Remove");
            }
        }

        /// <summary>
        /// 获取组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例，如果不存在返回null</returns>
        public T? GetComponent<T>() where T : class
        {
            return Components.FirstOrDefault(c => c is T) as T;
        }

        /// <summary>
        /// 检查是否有指定组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>是否拥有该组件</returns>
        public bool HasComponent<T>() where T : BaseComponent
        {
            return Components.Any(c => c is T);
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
        /// 设置激活状态
        /// </summary>
        /// <param name="active">是否激活</param>
        public void SetActive(bool active)
        {
            if (IsActive != active)
            {
                IsActive = active;
                
                // 发布激活状态变化事件
                PublishActiveStateChangedEvent(active);
            }
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        public void Destroy()
        {
            IsDestroyed = true;
            IsActive = false;
            
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
        /// 更新组件掩码
        /// </summary>
        private void UpdateComponentMask()
        {
            ComponentMask = 0;
            foreach (var component in Components)
            {
                // 这里可以根据组件类型设置相应的位标志
                ComponentMask |= (1L << component.ComponentId);
            }
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
        /// 发布激活状态变化事件
        /// </summary>
        /// <param name="isActive">是否激活</param>
        private void PublishActiveStateChangedEvent(bool isActive)
        {
            // 注意：这里需要获取World和Room信息，暂时通过静态方法获取
            // 在实际使用中，可能需要通过其他方式获取这些信息
            var eventData = new EntityActiveStateChangedEventData(this, 0, 0, isActive);
            EventSystem.Instance.Publish(eventData);
        }
        
        /// <summary>
        /// 发布组件变化事件
        /// </summary>
        /// <param name="component">组件</param>
        /// <param name="changeType">变化类型</param>
        private void PublishComponentChangedEvent(BaseComponent component, string changeType)
        {
            // 注意：这里需要获取World和Room信息，暂时通过静态方法获取
            // 在实际使用中，可能需要通过其他方式获取这些信息
            var eventData = new EntityComponentChangedEventData(this, 0, 0, component.GetType().Name, changeType, component);
            EventSystem.Instance.Publish(eventData);
        }

        /// <summary>
        /// 重写 ToString 方法，避免循环引用
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"Entity[Id={UniqueId}, Name={Name}, Active={IsActive}, Components={Components.Count}, Capabilities={CapabilityStates.Count}]";
        }
    }
}
