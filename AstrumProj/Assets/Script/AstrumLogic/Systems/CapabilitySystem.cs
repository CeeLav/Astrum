using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Capabilities;
using Astrum.CommonBase;
using MemoryPack;

namespace Astrum.LogicCore.Systems
{
    /// <summary>
    /// Capability 统一调度系统
    /// 负责所有 Capability 的激活判定和 Tick 执行
    /// World 的成员变量，支持序列化用于帧同步回滚
    /// </summary>
    [MemoryPackable]
    public partial class CapabilitySystem
    {
        // ====== 静态注册信息（不序列化，避免回滚） ======
        
        /// <summary>
        /// 已注册的 Capability 列表（静态，所有 World 共享）
        /// Key: Capability 类型
        /// Value: Capability 实例（单例）
        /// </summary>
        [MemoryPackIgnore]
        private static readonly Dictionary<Type, ICapability> _registeredCapabilities 
            = new Dictionary<Type, ICapability>();
        
        /// <summary>
        /// 按优先级排序的 Capability 列表（静态，所有 World 共享）
        /// </summary>
        [MemoryPackIgnore]
        private static readonly List<ICapability> _sortedCapabilities = new List<ICapability>();
        
        /// <summary>
        /// Tag 到 Capability TypeId 集合的映射（静态，所有 World 共享）
        /// </summary>
        [MemoryPackIgnore]
        private static readonly Dictionary<CapabilityTag, HashSet<int>> _tagToCapabilityTypeIds 
            = new Dictionary<CapabilityTag, HashSet<int>>();
        
        /// <summary>
        /// TypeId 到 Capability 实例的映射（静态，所有 World 共享）
        /// </summary>
        [MemoryPackIgnore]
        private static readonly Dictionary<int, ICapability> _typeIdToCapability 
            = new Dictionary<int, ICapability>();
        
        /// <summary>
        /// 静态初始化标志（确保只初始化一次）
        /// </summary>
        [MemoryPackIgnore]
        private static bool _isStaticInitialized = false;
        
        // ====== 实例数据（序列化，支持回滚） ======
        
        /// <summary>
        /// Capability TypeId 到拥有此 Capability 的 Entity ID 集合的映射
        /// Key: Capability TypeId
        /// Value: 拥有此 Capability 的 Entity ID 集合
        /// </summary>
        public Dictionary<int, HashSet<long>> TypeIdToEntityIds { get; private set; }
            = new Dictionary<int, HashSet<long>>();
        
        /// <summary>
        /// 所属 World（不序列化，由 World 设置）
        /// </summary>
        [MemoryPackIgnore]
        public World World { get; set; }
        
        public CapabilitySystem() { }
        
        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public CapabilitySystem(Dictionary<int, HashSet<long>> typeIdToEntityIds)
        {
            TypeIdToEntityIds = typeIdToEntityIds ?? new Dictionary<int, HashSet<long>>();
            
            // 确保静态数据已初始化
            EnsureStaticInitialized();
        }
        
        /// <summary>
        /// 确保静态数据已初始化（线程安全）
        /// </summary>
        private static void EnsureStaticInitialized()
        {
            if (_isStaticInitialized)
                return;
            
            lock (_registeredCapabilities)
            {
                if (_isStaticInitialized)
                    return;
                
                // 自动扫描并注册所有 Capability
                RegisterAllCapabilities();
                
                // 按优先级排序
                SortCapabilities();
                
                // 构建 Tag 映射
                BuildTagMapping();
                
                _isStaticInitialized = true;
            }
        }
        
        /// <summary>
        /// 初始化系统（在游戏启动时调用）
        /// </summary>
        public void Initialize()
        {
            EnsureStaticInitialized();
        }
        
        /// <summary>
        /// 注册 Capability
        /// </summary>
        public static void RegisterCapability<T>() where T : ICapability, new()
        {
            var capability = new T();
            var type = typeof(T);
            
            lock (_registeredCapabilities)
            {
                if (!_registeredCapabilities.ContainsKey(type))
                {
                    _registeredCapabilities[type] = capability;
                    _sortedCapabilities.Add(capability);
                    
                    // 同时注册到 TypeId 映射
                    _typeIdToCapability[capability.TypeId] = capability;
                }
            }
        }
        
        /// <summary>
        /// 注册 Capability 实例（用于单例）
        /// </summary>
        private static void RegisterCapability(Type type, ICapability capability)
        {
            lock (_registeredCapabilities)
            {
                if (!_registeredCapabilities.ContainsKey(type))
                {
                    _registeredCapabilities[type] = capability;
                    _sortedCapabilities.Add(capability);
                    
                    // 同时注册到 TypeId 映射
                    _typeIdToCapability[capability.TypeId] = capability;
                }
            }
        }
        
        /// <summary>
        /// 自动扫描并注册所有 Capability
        /// </summary>
        private static void RegisterAllCapabilities()
        {
            var assembly = typeof(ICapability).Assembly;
            var capabilityTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(ICapability).IsAssignableFrom(t));
            
            foreach (var type in capabilityTypes)
            {
                try
                {
                    var capability = (ICapability)Activator.CreateInstance(type);
                    RegisterCapability(type, capability);
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Error($"Failed to register Capability: {type.Name}, Error: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 按优先级排序 Capability
        /// </summary>
        private static void SortCapabilities()
        {
            _sortedCapabilities.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
        
        /// <summary>
        /// 构建 Tag 到 Capability TypeId 的映射
        /// </summary>
        private static void BuildTagMapping()
        {
            _tagToCapabilityTypeIds.Clear();
            
            // 遍历所有已注册的 Capability
            foreach (var capability in _registeredCapabilities.Values)
            {
                var typeId = capability.TypeId;
                
                // 遍历此 Capability 的所有 Tag
                foreach (var tag in capability.Tags)
                {
                    if (!_tagToCapabilityTypeIds.TryGetValue(tag, out var typeIds))
                    {
                        typeIds = new HashSet<int>();
                        _tagToCapabilityTypeIds[tag] = typeIds;
                    }
                    typeIds.Add(typeId);
                }
            }
        }
        
        /// <summary>
        /// 根据 TypeId 获取 Capability
        /// </summary>
        public static ICapability GetCapability(int typeId)
        {
            return _typeIdToCapability.TryGetValue(typeId, out var capability) ? capability : null;
        }
        
        /// <summary>
        /// 根据 Type 获取 Capability
        /// </summary>
        public static ICapability GetCapability(Type type)
        {
            return _registeredCapabilities.TryGetValue(type, out var capability) ? capability : null;
        }
        
        // ====== Entity Capability 注册管理（实例方法，支持回滚） ======
        
        /// <summary>
        /// 注册 Entity 拥有某个 Capability
        /// 在 Entity 添加 Capability 时调用
        /// </summary>
        public void RegisterEntityCapability(long entityId, int typeId)
        {
            if (!TypeIdToEntityIds.TryGetValue(typeId, out var entityIds))
            {
                entityIds = new HashSet<long>();
                TypeIdToEntityIds[typeId] = entityIds;
            }
            entityIds.Add(entityId);
        }
        
        /// <summary>
        /// 注销 Entity 的某个 Capability
        /// 在 Entity 移除 Capability 时调用
        /// </summary>
        public void UnregisterEntityCapability(long entityId, int typeId)
        {
            if (TypeIdToEntityIds.TryGetValue(typeId, out var entityIds))
            {
                entityIds.Remove(entityId);
                
                // 如果集合为空，可以选择移除（或者保留，等待垃圾回收）
                if (entityIds.Count == 0)
                {
                    TypeIdToEntityIds.Remove(typeId);
                }
            }
        }
        
        /// <summary>
        /// 清理已销毁实体的 Capability 注册
        /// 在 Entity 销毁时调用
        /// </summary>
        public void UnregisterEntity(long entityId)
        {
            foreach (var kvp in TypeIdToEntityIds.ToList())
            {
                kvp.Value.Remove(entityId);
                
                // 如果集合为空，移除该 TypeId 的映射
                if (kvp.Value.Count == 0)
                {
                    TypeIdToEntityIds.Remove(kvp.Key);
                }
            }
        }
        
        /// <summary>
        /// 更新所有 Capability（按 Capability 遍历，只更新拥有该 Capability 的实体）
        /// </summary>
        public void Update(World world)
        {
            if (world == null || world.Entities == null)
                return;
            
            // 按优先级遍历所有 Capability
            foreach (var capability in _sortedCapabilities)
            {
                var typeId = capability.TypeId;
                
                // 只遍历拥有此 Capability 的实体（避免无效更新）
                if (!TypeIdToEntityIds.TryGetValue(typeId, out var entityIds))
                    continue;
                
                // 遍历拥有此 Capability 的实体 ID 集合
                foreach (var entityId in entityIds.ToList()) // ToList() 避免迭代时修改集合
                {
                    // 获取实体（可能已被销毁）
                    if (!world.Entities.TryGetValue(entityId, out var entity))
                    {
                        // 实体已被销毁，清理注册
                        UnregisterEntityCapability(entityId, typeId);
                        continue;
                    }
                    
                    if (entity == null || !entity.IsActive || entity.IsDestroyed)
                    {
                        // 实体已销毁或未激活，清理注册
                        UnregisterEntityCapability(entityId, typeId);
                        continue;
                    }
                    
                    // 检查此 Capability 是否仍然存在于实体上（双重检查，防止状态不一致）
                    if (!entity.CapabilityStates.TryGetValue(typeId, out var state))
                    {
                        // 状态不一致，清理注册
                        UnregisterEntityCapability(entityId, typeId);
                        continue;
                    }
                    
                    // 1. 更新激活状态
                    UpdateActivationState(capability, entity, ref state);
                    
                    // 2. 更新持续时间
                    UpdateDuration(capability, entity, ref state);
                    
                    // 3. 执行激活的 Capability 的 Tick
                    if (state.IsActive)
                    {
                        try
                        {
                            capability.Tick(entity);
                        }
                        catch (Exception ex)
                        {
                            ASLogger.Instance.Error($"Error executing Capability {capability.GetType().Name} on entity {entity.UniqueId}: {ex.Message}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 更新单个 Capability 在单个实体上的激活状态
        /// </summary>
        private void UpdateActivationState(ICapability capability, Entity entity, ref CapabilityState state)
        {
            var typeId = capability.TypeId;
            
            // 检查 Tag 是否被禁用
            bool isDisabled = IsCapabilityDisabledByTag(capability, entity);
            
            // 如果当前未激活，检查是否应该激活
            if (!state.IsActive)
            {
                // 只有在未被禁用的情况下才检查激活条件
                if (!isDisabled && capability.ShouldActivate(entity))
                {
                    state.IsActive = true;
                    state.ActiveDuration = 0; // 重置激活持续时间
                    entity.CapabilityStates[typeId] = state;
                    capability.OnActivate(entity);
                    return; // 成功激活后直接返回，避免无效的停用检查
                }
            }
            else // 当前已激活
            {
                // 检查是否应该停用（被禁用或 ShouldDeactivate 返回 true）
                if (isDisabled || capability.ShouldDeactivate(entity))
                {
                    state.IsActive = false;
                    state.DeactiveDuration = 0; // 重置禁用持续时间
                    entity.CapabilityStates[typeId] = state;
                    capability.OnDeactivate(entity);
                }
            }
        }
        
        /// <summary>
        /// 检查 Capability 是否被 Tag 禁用
        /// </summary>
        private bool IsCapabilityDisabledByTag(ICapability capability, Entity entity)
        {
            // 检查此 Capability 的任何一个 Tag 是否在 DisabledTags 中
            foreach (var tag in capability.Tags)
            {
                if (entity.DisabledTags.ContainsKey(tag) && entity.DisabledTags[tag].Count > 0)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// 更新 Capability 的持续时间
        /// </summary>
        private void UpdateDuration(ICapability capability, Entity entity, ref CapabilityState state)
        {
            var typeId = capability.TypeId;
            
            if (state.IsActive && capability.TrackActiveDuration)
            {
                state.ActiveDuration++;
                entity.CapabilityStates[typeId] = state;
            }
            else if (!state.IsActive && capability.TrackDeactiveDuration)
            {
                state.DeactiveDuration++;
                entity.CapabilityStates[typeId] = state;
            }
        }
        
        
        // ====== 外部接口：Tag 禁用/启用 ======
        
        /// <summary>
        /// 禁用实体上所有匹配指定 Tag 的 Capability
        /// </summary>
        public void DisableCapabilitiesByTag(Entity entity, CapabilityTag tag, long instigatorId, string reason = null)
        {
            // 查找所有匹配此 Tag 的 Capability
            if (!_tagToCapabilityTypeIds.TryGetValue(tag, out var typeIds))
                return;
            
            // 记录到 DisabledTags（用于快速检查）
            if (!entity.DisabledTags.TryGetValue(tag, out var instigators))
            {
                instigators = new HashSet<long>();
                entity.DisabledTags[tag] = instigators;
            }
            instigators.Add(instigatorId);
            
            // 注意：不需要在 CapabilityState 中存储 Disabler，因为禁用状态可以从 DisabledTags 中查询
        }
        
        /// <summary>
        /// 启用实体上所有匹配指定 Tag 的 Capability
        /// </summary>
        public void EnableCapabilitiesByTag(Entity entity, CapabilityTag tag, long instigatorId)
        {
            // 从 DisabledTags 中移除
            if (entity.DisabledTags.TryGetValue(tag, out var instigators))
            {
                instigators.Remove(instigatorId);
                if (instigators.Count == 0)
                    entity.DisabledTags.Remove(tag);
            }
        }
        
        /// <summary>
        /// 检查实体上是否有指定 Tag 的 Capability 被禁用
        /// </summary>
        public bool IsTagDisabled(Entity entity, CapabilityTag tag)
        {
            return entity.DisabledTags.ContainsKey(tag) && entity.DisabledTags[tag].Count > 0;
        }
    }
}

