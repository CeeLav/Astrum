using System;
using System.Collections.Generic;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// Capability 抽象基类
    /// 使用泛型自动生成 TypeId，提供默认实现和辅助方法
    /// </summary>
    /// <typeparam name="T">Capability 自身类型</typeparam>
    public abstract class Capability<T> : ICapability where T : ICapability
    {
        // ====== 事件处理支持 ======
        
        /// <summary>
        /// 事件处理委托类型
        /// 第一个参数必须是 Entity，第二个参数是事件数据
        /// </summary>
        protected delegate void EntityEventHandler<TEvent>(Entity entity, TEvent evt) where TEvent : struct;
        
        /// <summary>
        /// 存储注册的事件处理器（延迟初始化）
        /// Key: 事件类型, Value: 处理委托
        /// </summary>
        private Dictionary<Type, Delegate> _eventHandlers;
        
        /// <summary>
        /// 注册事件处理函数（在子类中重写此方法来声明处理的事件）
        /// </summary>
        protected virtual void RegisterEventHandlers() { }
        
        /// <summary>
        /// 注册单个事件处理器
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">处理函数</param>
        protected void RegisterEventHandler<TEvent>(EntityEventHandler<TEvent> handler) where TEvent : struct
        {
            if (_eventHandlers == null)
                _eventHandlers = new Dictionary<Type, Delegate>();
            
            _eventHandlers[typeof(TEvent)] = handler;
        }
        
        /// <summary>
        /// 获取所有注册的事件处理器（供 CapabilitySystem 使用）
        /// </summary>
        internal Dictionary<Type, Delegate> GetEventHandlers()
        {
            if (_eventHandlers == null)
            {
                RegisterEventHandlers(); // 延迟初始化
            }
            return _eventHandlers;
        }
        
        // ====== 原有代码 ======
        
        /// <summary>
        /// 类型 ID（基于 TypeHash 的稳定哈希值，编译期常量）
        /// </summary>
        public static readonly int TypeId = TypeHash<T>.GetHash();
        
        /// <summary>
        /// 类型 ID（接口实现）
        /// </summary>
        int ICapability.TypeId => TypeId;
        
        /// <summary>
        /// 执行优先级（默认值，子类可重写）
        /// </summary>
        public virtual int Priority => 0;
        
        /// <summary>
        /// Tag 集合（默认空集合，子类可重写）
        /// </summary>
        public virtual IReadOnlyCollection<CapabilityTag> Tags => EmptyTags;
        
        /// <summary>
        /// 是否跟踪激活持续时间（默认 false）
        /// </summary>
        public virtual bool TrackActiveDuration => false;
        
        /// <summary>
        /// 是否跟踪禁用持续时间（默认 false）
        /// </summary>
        public virtual bool TrackDeactiveDuration => false;
        
        private static readonly HashSet<CapabilityTag> EmptyTags = new HashSet<CapabilityTag>();
        
        /// <summary>
        /// 判定此 Capability 是否应该激活
        /// </summary>
        public virtual bool ShouldActivate(Entity entity)
        {
            // 默认实现：检查是否存在且未被禁用
            // CapabilityState 存在即表示拥有此 Capability
            return HasCapabilityState(entity) && !IsCapabilityDisabled(entity);
        }
        
        /// <summary>
        /// 判定此 Capability 是否应该停用
        /// </summary>
        public virtual bool ShouldDeactivate(Entity entity)
        {
            // 默认实现：检查是否被禁用或不再存在
            return !HasCapabilityState(entity) || IsCapabilityDisabled(entity);
        }
        
        /// <summary>
        /// 首次挂载到实体时调用
        /// </summary>
        public virtual void OnAttached(Entity entity)
        {
            // 默认不执行任何操作
        }
        
        /// <summary>
        /// 从实体上完全卸载时调用
        /// </summary>
        public virtual void OnDetached(Entity entity)
        {
            // 默认清理状态
            RemoveCapabilityState(entity);
        }
        
        /// <summary>
        /// 激活时调用
        /// </summary>
        public virtual void OnActivate(Entity entity)
        {
            // 默认不执行任何操作
        }
        
        /// <summary>
        /// 停用时调用
        /// </summary>
        public virtual void OnDeactivate(Entity entity)
        {
            // 默认不执行任何操作
        }
        
        /// <summary>
        /// 每帧更新（抽象方法，子类必须实现）
        /// </summary>
        public abstract void Tick(Entity entity);
        
        // ====== 辅助方法 ======
        
        /// <summary>
        /// 获取此 Capability 在实体上的状态
        /// </summary>
        protected CapabilityState GetCapabilityState(Entity entity)
        {
            if (entity.CapabilityStates.TryGetValue(TypeId, out var state))
            {
                // 反序列化 CustomData（如果存在）
                if (state.CustomData == null && state.CustomDataSerialized != null && state.CustomDataSerialized.Count > 0)
                {
                    state.CustomData = new Dictionary<string, object>();
                    foreach (var kvp in state.CustomDataSerialized)
                    {
                        // 简单的类型推断：尝试解析为 int、float、bool 或保留为 string
                        if (int.TryParse(kvp.Value, out var intVal))
                            state.CustomData[kvp.Key] = intVal;
                        else if (float.TryParse(kvp.Value, out var floatVal))
                            state.CustomData[kvp.Key] = floatVal;
                        else if (bool.TryParse(kvp.Value, out var boolVal))
                            state.CustomData[kvp.Key] = boolVal;
                        else
                            state.CustomData[kvp.Key] = kvp.Value;
                    }
                }
                return state;
            }
            return default;
        }
        
        /// <summary>
        /// 设置此 Capability 在实体上的状态
        /// </summary>
        protected void SetCapabilityState(Entity entity, CapabilityState state)
        {
            // 序列化 CustomData 到 CustomDataSerialized（用于 MemoryPack）
            if (state.CustomData != null && state.CustomData.Count > 0)
            {
                state.CustomDataSerialized = new Dictionary<string, string>();
                foreach (var kvp in state.CustomData)
                {
                    state.CustomDataSerialized[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                }
            }
            else if (state.CustomDataSerialized == null)
            {
                state.CustomDataSerialized = new Dictionary<string, string>();
            }
            
            entity.CapabilityStates[TypeId] = state;
        }
        
        /// <summary>
        /// 移除此 Capability 在实体上的状态
        /// </summary>
        protected void RemoveCapabilityState(Entity entity)
        {
            entity.CapabilityStates.Remove(TypeId);
        }
        
        /// <summary>
        /// 检查此 Capability 是否在实体上存在（字典中存在即表示拥有）
        /// </summary>
        protected bool HasCapabilityState(Entity entity)
        {
            return entity.CapabilityStates.ContainsKey(TypeId);
        }
        
        /// <summary>
        /// 检查此 Capability 是否被禁用
        /// 通过检查 Entity.DisabledTags 中是否有此 Capability 的 Tag 被禁用
        /// </summary>
        protected bool IsCapabilityDisabled(Entity entity)
        {
            // 检查此 Capability 的任何一个 Tag 是否在 DisabledTags 中
            // 直接使用 this.Tags，因为 Capability<T> 本身就有 Tags 属性
            foreach (var tag in Tags)
            {
                if (entity.DisabledTags.ContainsKey(tag) && entity.DisabledTags[tag].Count > 0)
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 检查实体是否拥有指定组件
        /// </summary>
        protected bool HasComponent<TComponent>(Entity entity) where TComponent : BaseComponent
        {
            return entity.HasComponent<TComponent>();
        }
        
        /// <summary>
        /// 获取实体的组件（带性能监控）
        /// </summary>
        protected TComponent GetComponent<TComponent>(Entity entity) where TComponent : BaseComponent
        {
#if ENABLE_PROFILER
            using (new CommonBase.ProfileScope($"GetComponent"))
#endif
            {
                return entity.GetComponent<TComponent>();
            }
        }
    }
}

