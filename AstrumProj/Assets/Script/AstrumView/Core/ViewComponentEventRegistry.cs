using System;
using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.View.Core
{
    /// <summary>
    /// ViewComponent 事件注册表（全局单例）
    /// 类似 CapabilitySystem 的静态事件映射机制
    /// 维护 事件类型 -> ViewComponent 类型 的全局映射
    /// </summary>
    public class ViewComponentEventRegistry
    {
        private static ViewComponentEventRegistry _instance;
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static ViewComponentEventRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ViewComponentEventRegistry();
                    _instance.Initialize();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 全局映射：事件类型 -> ViewComponent 类型列表
        /// 类似 CapabilitySystem._eventToHandlers
        /// Key: 事件数据类型（例如 HitAnimationEvent）
        /// Value: 监听该事件的 ViewComponent 类型列表
        /// </summary>
        private readonly Dictionary<Type, List<Type>> _eventTypeToComponentTypes 
            = new Dictionary<Type, List<Type>>();
        
        private bool _initialized = false;
        
        /// <summary>
        /// 私有构造函数（单例模式）
        /// </summary>
        private ViewComponentEventRegistry()
        {
        }
        
        /// <summary>
        /// 初始化注册表
        /// </summary>
        private void Initialize()
        {
            if (_initialized) return;
            
            ASLogger.Instance.Info("ViewComponentEventRegistry: 初始化事件注册表");
            _initialized = true;
        }
        
        /// <summary>
        /// 注册 ViewComponent 类型监听的事件
        /// 由 ViewComponent 子类在静态构造函数中调用
        /// </summary>
        /// <param name="eventType">事件数据类型（例如 HitAnimationEvent）</param>
        /// <param name="componentType">ViewComponent 类型（例如 AnimationViewComponent）</param>
        public void RegisterEventHandler(Type eventType, Type componentType)
        {
            if (eventType == null || componentType == null)
            {
                ASLogger.Instance.Warning($"ViewComponentEventRegistry: 注册参数为空");
                return;
            }
            
            if (!_eventTypeToComponentTypes.ContainsKey(eventType))
            {
                _eventTypeToComponentTypes[eventType] = new List<Type>();
            }
            
            if (!_eventTypeToComponentTypes[eventType].Contains(componentType))
            {
                _eventTypeToComponentTypes[eventType].Add(componentType);
                ASLogger.Instance.Debug($"ViewComponentEventRegistry: 注册事件处理器 - {eventType.Name} -> {componentType.Name}");
            }
        }
        
        /// <summary>
        /// 获取监听指定事件的 ViewComponent 类型列表
        /// </summary>
        /// <param name="eventType">事件数据类型</param>
        /// <returns>ViewComponent 类型列表，如果没有则返回 null</returns>
        public List<Type> GetComponentTypesForEvent(Type eventType)
        {
            if (_eventTypeToComponentTypes.TryGetValue(eventType, out var types))
            {
                return types;
            }
            return null;
        }
        
        /// <summary>
        /// 获取所有注册的事件类型数量（用于调试）
        /// </summary>
        public int GetRegisteredEventCount()
        {
            return _eventTypeToComponentTypes.Count;
        }
        
        /// <summary>
        /// 输出所有注册信息（用于调试）
        /// </summary>
        public void LogRegistrations()
        {
            ASLogger.Instance.Info($"ViewComponentEventRegistry: 共注册 {_eventTypeToComponentTypes.Count} 种事件类型");
            
            foreach (var kvp in _eventTypeToComponentTypes)
            {
                var eventType = kvp.Key;
                var componentTypes = kvp.Value;
                ASLogger.Instance.Info($"  事件 {eventType.Name} -> {componentTypes.Count} 个 ViewComponent: {string.Join(", ", componentTypes.ConvertAll(t => t.Name))}");
            }
        }
    }
}

