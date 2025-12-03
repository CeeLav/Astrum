using UnityEngine;
using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.View.Core;

namespace Astrum.View.Components
{
    /// <summary>
    /// 视图组件基类 - 为EntityView提供特定的视觉功能
    /// </summary>
    public abstract class ViewComponent
    {
        // 视图组件设置
        protected int _componentId;
        protected bool _isEnabled = true;
        
        // 所属实体视图
        protected EntityView _ownerEntityView;
        
        // 视图事件处理器映射（EventType -> Handler）
        // 类似 Capability 的事件处理机制
        private Dictionary<Type, Delegate> _viewEventHandlers = new Dictionary<Type, Delegate>();
        
        // 事件处理器是否已注册标志（对象池优化）
        // 避免每次从对象池取出时重复注册回调
        private bool _eventHandlersRegistered = false;
        
        // 公共属性
        public int ComponentId => _componentId;
        public bool IsEnabled => _isEnabled;
        public EntityView OwnerEntityView 
        { 
            get => _ownerEntityView;
            set => _ownerEntityView = value;
        }

        public Entity OwnerEntity
        {
            get => _ownerEntityView.OwnerEntity;
        }
        
        // 事件
        public event Action<ViewComponent> OnComponentInitialized;
        public event Action<ViewComponent> OnComponentDestroyed;
        public event Action<ViewComponent, bool> OnEnabledChanged;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        protected ViewComponent()
        {
            _componentId = UnityEngine.Random.Range(1000, 9999);
        }
        
        /// <summary>
        /// 初始化组件
        /// </summary>
        public virtual void Initialize()
        {
            if (_componentId == 0)
            {
                _componentId = UnityEngine.Random.Range(1000, 9999);
            }
            
            Debug.Log($"ViewComponent: 初始化组件，ID: {_componentId}，类型: {GetType().Name}");
            
            try
            {
                // 注册视图事件处理器（类似 Capability.RegisterEventHandlers）
                // 对象池优化：只在第一次注册，避免重复注册开销
                if (!_eventHandlersRegistered)
                {
                    RegisterViewEventHandlers();
                    _eventHandlersRegistered = true;
                }
                
                // 子类特定的初始化
                OnInitialize();
                
                OnComponentInitialized?.Invoke(this);
                
                Debug.Log($"ViewComponent: 组件初始化完成，ID: {_componentId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ViewComponent: 组件初始化失败，ID: {_componentId} - {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 更新组件
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        public virtual void Update(float deltaTime)
        {
            if (!_isEnabled) return;
            
            // 子类特定的更新逻辑
            OnUpdate(deltaTime);
        }
        
        /// <summary>
        /// 销毁组件
        /// </summary>
        public virtual void Destroy()
        {
            // 子类特定的销毁逻辑
            OnDestroy();
            
            OnComponentDestroyed?.Invoke(this);
        }
        
        /// <summary>
        /// 设置启用状态
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public virtual void SetEnabled(bool enabled)
        {
            if (_isEnabled == enabled) return;
            
            _isEnabled = enabled;
            
            OnEnabledChanged?.Invoke(this, enabled);
            Debug.Log($"ViewComponent: 设置启用状态，ID: {_componentId}，启用: {enabled}");
        }
        
        /// <summary>
        /// 重置组件（用于回放跳转时强制与逻辑层同步）
        /// </summary>
        public virtual void Reset()
        {
            if (!_isEnabled || _ownerEntityView == null) return;
            
            // 子类特定的重置逻辑（强制同步逻辑层数据）
            OnReset();
        }

        /// <summary>
        /// 获取组件类型名称
        /// </summary>
        /// <returns>组件类型名称</returns>
        public virtual string GetComponentTypeName()
        {
            return GetType().Name;
        }
        
        /// <summary>
        /// 获取组件状态信息
        /// </summary>
        /// <returns>状态信息</returns>
        public virtual string GetComponentStatus()
        {
            return $"ID: {_componentId}, 类型: {GetComponentTypeName()}, 启用: {_isEnabled}";
        }
        
        // 抽象方法 - 子类必须实现
        protected abstract void OnInitialize();
        protected abstract void OnUpdate(float deltaTime);
        protected abstract void OnDestroy();
        protected abstract void OnSyncData(object data);
        
        // 虚方法 - 子类可以重写
        /// <summary>
        /// 重置组件（子类重写以实现强制同步逻辑层数据）
        /// 子类应该从 OwnerEntity 获取最新的组件数据并强制同步到视图
        /// </summary>
        protected virtual void OnReset()
        {
            // 默认实现：子类应该重写此方法，从 OwnerEntity 获取数据并同步
            // 例如：
            // var entity = OwnerEntity;
            // if (entity != null)
            // {
            //     var component = entity.GetComponent<SomeComponent>();
            //     if (component != null)
            //     {
            //         // 强制同步数据到视图
            //     }
            // }
        }
        
        /// <summary>
        /// 获取需要监听的 BaseComponent 的 ComponentTypeId 列表
        /// 子类重写此方法以声明需要监听的组件类型 ID
        /// </summary>
        /// <returns>需要监听的 ComponentTypeId 数组，如果不需要监听则返回 null</returns>
        public virtual int[] GetWatchedComponentIds()
        {
            return null; // 默认不监听任何组件
        }
        
        /// <summary>
        /// 根据 ComponentTypeId 从 OwnerEntity 获取组件并同步数据
        /// 子类重写此方法以自定义数据提取逻辑
        /// </summary>
        /// <param name="componentTypeId">BaseComponent 的 ComponentTypeId</param>
        public virtual void SyncDataFromComponent(int componentTypeId)
        {
            // 默认实现：子类可以重写以自定义数据提取逻辑
            // 例如：从 OwnerEntity 根据 ComponentTypeId 获取组件，提取数据，构造数据对象，然后调用 OnSyncData
            if (OwnerEntity == null) return;
            
            var component = OwnerEntity.GetComponentById(componentTypeId);
            if (component != null)
            {
                // 子类应该重写此方法，从 component 提取数据并调用 OnSyncData
            }
        }
        
        #region 视图事件处理机制（类似 Capability，全局注册 + 对象池优化）
        
        /// <summary>
        /// 注册视图事件处理器（子类重写）
        /// 类似 Capability.RegisterEventHandlers()
        /// 子类在此方法中调用 RegisterViewEventHandler 注册实例级事件处理器
        /// 
        /// 注意：
        /// 1. 静态注册（类型级）：在静态构造函数中调用 ViewComponentEventRegistry.Instance.RegisterEventHandler
        /// 2. 实例注册（实例级）：在此方法中调用 RegisterViewEventHandler 注册实例的处理器
        /// 3. 对象池优化：此方法只在第一次初始化时调用，避免重复注册开销
        /// </summary>
        protected virtual void RegisterViewEventHandlers()
        {
            // 默认不注册任何事件，子类重写以注册事件
        }
        
        /// <summary>
        /// 注册单个视图事件处理器（实例级）
        /// </summary>
        /// <typeparam name="TEvent">事件类型（必须是 struct）</typeparam>
        /// <param name="handler">事件处理器</param>
        protected void RegisterViewEventHandler<TEvent>(Action<TEvent> handler)
            where TEvent : struct
        {
            var eventType = typeof(TEvent);
            _viewEventHandlers[eventType] = handler;
        }
        
        /// <summary>
        /// 调用事件处理器（由 EntityView 调用）
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="eventData">事件数据</param>
        internal void InvokeEventHandler(Type eventType, object eventData)
        {
            if (_viewEventHandlers.TryGetValue(eventType, out var handler))
            {
                try
                {
                    handler.DynamicInvoke(eventData);
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Error($"ViewComponent: 调用事件处理器失败，EventType: {eventType.Name}, ComponentType: {GetType().Name}, Error: {ex.Message}");
                }
            }
        }
        
        #endregion
    }
}
