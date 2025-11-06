using System.Collections.Generic;

namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 全局广播事件队列
    /// 用于需要所有实体响应的事件（阶段切换、全局公告、环境变化等）
    /// </summary>
    public class GlobalEventQueue
    {
        // 全局事件队列
        private readonly Queue<EntityEvent> _globalEvents = new Queue<EntityEvent>(16);
        
        /// <summary>
        /// 发布全局事件（面向全体）
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
        /// 获取所有全局事件（供 CapabilitySystem 调度）
        /// </summary>
        internal Queue<EntityEvent> GetEvents()
        {
            return _globalEvents;
        }
        
        /// <summary>
        /// 清空所有全局事件
        /// </summary>
        public void ClearAll()
        {
            _globalEvents.Clear();
        }
        
        /// <summary>
        /// 是否有待处理事件
        /// </summary>
        public bool HasPendingEvents => _globalEvents.Count > 0;
    }
}

