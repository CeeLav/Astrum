using System;
using System.Collections.Generic;
using System.Linq;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 事件系统 - 全局事件管理
    /// </summary>
    public class EventSystem : Singleton<EventSystem>
    {
        private readonly Dictionary<Type, List<Delegate>> _eventHandlers = new Dictionary<Type, List<Delegate>>();
        private readonly Dictionary<string, List<Delegate>> _stringEventHandlers = new Dictionary<string, List<Delegate>>();
        private readonly object _lock = new object();

        /// <summary>
        /// 注册事件监听器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public void Subscribe<T>(Action<T> handler)
        {
            var eventType = typeof(T);
            lock (_lock)
            {
                if (!_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType] = new List<Delegate>();
                }
                _eventHandlers[eventType].Add(handler);
            }
        }

        /// <summary>
        /// 注册字符串事件监听器
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="handler">事件处理器</param>
        public void Subscribe(string eventName, Action<object> handler)
        {
            lock (_lock)
            {
                if (!_stringEventHandlers.ContainsKey(eventName))
                {
                    _stringEventHandlers[eventName] = new List<Delegate>();
                }
                _stringEventHandlers[eventName].Add(handler);
            }
        }

        /// <summary>
        /// 取消注册事件监听器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public void Unsubscribe<T>(Action<T> handler)
        {
            var eventType = typeof(T);
            lock (_lock)
            {
                if (_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType].Remove(handler);
                    if (_eventHandlers[eventType].Count == 0)
                    {
                        _eventHandlers.Remove(eventType);
                    }
                }
            }
        }

        /// <summary>
        /// 取消注册字符串事件监听器
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="handler">事件处理器</param>
        public void Unsubscribe(string eventName, Action<object> handler)
        {
            lock (_lock)
            {
                if (_stringEventHandlers.ContainsKey(eventName))
                {
                    _stringEventHandlers[eventName].Remove(handler);
                    if (_stringEventHandlers[eventName].Count == 0)
                    {
                        _stringEventHandlers.Remove(eventName);
                    }
                }
            }
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public void Publish<T>(T eventData)
        {
            var eventType = typeof(T);
            List<Delegate> handlers;

            lock (_lock)
            {
                if (!_eventHandlers.ContainsKey(eventType))
                {
                    return;
                }
                handlers = _eventHandlers[eventType].ToList();
            }

            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<T>)handler)(eventData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"事件处理器执行出错: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 发布字符串事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="eventData">事件数据</param>
        public void Publish(string eventName, object eventData = null)
        {
            List<Delegate> handlers;

            lock (_lock)
            {
                if (!_stringEventHandlers.ContainsKey(eventName))
                {
                    return;
                }
                handlers = _stringEventHandlers[eventName].ToList();
            }

            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<object>)handler)(eventData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"事件处理器执行出错: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 清除所有事件监听器
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _eventHandlers.Clear();
                _stringEventHandlers.Clear();
            }
        }

        /// <summary>
        /// 获取事件监听器数量
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>监听器数量</returns>
        public int GetListenerCount<T>()
        {
            var eventType = typeof(T);
            lock (_lock)
            {
                return _eventHandlers.ContainsKey(eventType) ? _eventHandlers[eventType].Count : 0;
            }
        }

        /// <summary>
        /// 获取字符串事件监听器数量
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <returns>监听器数量</returns>
        public int GetListenerCount(string eventName)
        {
            lock (_lock)
            {
                return _stringEventHandlers.ContainsKey(eventName) ? _stringEventHandlers[eventName].Count : 0;
            }
        }
    }

    /// <summary>
    /// 事件数据基类
    /// </summary>
    public abstract class EventData
    {
        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 事件发送者
        /// </summary>
        public object Sender { get; set; }
    }

    /// <summary>
    /// 游戏事件数据
    /// </summary>
    public class GameEventData : EventData
    {
        /// <summary>
        /// 事件类型
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// 事件数据
        /// </summary>
        public object Data { get; set; }
    }

    /// <summary>
    /// 玩家事件数据
    /// </summary>
    public class PlayerEventData : EventData
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        public string PlayerId { get; set; }

        /// <summary>
        /// 玩家名称
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// 事件类型
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// 事件数据
        /// </summary>
        public object Data { get; set; }
    }
} 