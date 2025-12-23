using System.Collections.Concurrent;

namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 全局广播事件队列（线程安全）
    /// 用于需要所有实体响应的事件（阶段切换、全局公告、环境变化等）
    /// 
    /// 线程安全说明：
    /// - Logic 线程可以安全地调用 QueueGlobalEvent() 写入事件
    /// - CapabilitySystem 可以安全地读取和消费事件
    /// - 使用 ConcurrentQueue 实现无锁并发访问
    /// </summary>
    public class GlobalEventQueue
    {
        /// <summary>
        /// 全局事件队列（线程安全）
        /// </summary>
        private readonly ConcurrentQueue<EntityEvent> _globalEvents = new ConcurrentQueue<EntityEvent>();
        
        /// <summary>
        /// 发布全局事件（面向全体）（线程安全）
        /// Logic 线程可以安全调用此方法
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public void QueueGlobalEvent<T>(T eventData) where T : struct, IEvent
        {
            _globalEvents.Enqueue(new EntityEvent
            {
                EventType = typeof(T),
                EventData = eventData, // 装箱为 IEvent
                Frame = 0 // 可以从 World 获取
            });
        }
        
        /// <summary>
        /// 获取全局事件队列（供 CapabilitySystem 调度）
        /// 调用方应使用 TryDequeue() 来消费事件
        /// </summary>
        internal ConcurrentQueue<EntityEvent> GetEvents()
        {
            return _globalEvents;
        }
        
        /// <summary>
        /// 清空所有全局事件（线程安全）
        /// 使用 TryDequeue 循环清空，避免并发问题
        /// </summary>
        public void ClearAll()
        {
            while (_globalEvents.TryDequeue(out _))
            {
                // 丢弃所有事件
            }
        }
        
        /// <summary>
        /// 是否有待处理事件
        /// 注意：IsEmpty 在并发环境下是快照值，可能不精确，但不影响正确性
        /// </summary>
        public bool HasPendingEvents => !_globalEvents.IsEmpty;
    }
}

