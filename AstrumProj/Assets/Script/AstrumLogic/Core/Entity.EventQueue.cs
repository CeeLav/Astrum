using System.Collections.Generic;
using Astrum.LogicCore.Events;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// Entity 扩展 - 事件队列支持
    /// </summary>
    public partial class Entity
    {
        // 个体事件队列（延迟创建，不序列化）
        // 注意：事件队列不需要序列化，因为事件是瞬时的，每帧处理完就清空
        [MemoryPack.MemoryPackIgnore]
        private Queue<EntityEvent> _eventQueue;
        
        /// <summary>
        /// 是否有待处理事件
        /// </summary>
        [MemoryPack.MemoryPackIgnore]
        public bool HasPendingEvents => _eventQueue != null && _eventQueue.Count > 0;
        
        /// <summary>
        /// 获取事件队列（供 CapabilitySystem 访问）
        /// </summary>
        [MemoryPack.MemoryPackIgnore]
        internal Queue<EntityEvent> EventQueue => _eventQueue;
        
        /// <summary>
        /// 向该实体发布事件（面向个体）
        /// </summary>
        /// <typeparam name="T">事件类型（必须实现 IEvent 接口）</typeparam>
        /// <param name="eventData">事件数据</param>
        public void QueueEvent<T>(T eventData) where T : struct, IEvent
        {
            if (_eventQueue == null)
                _eventQueue = new Queue<EntityEvent>(4); // 延迟创建，初始容量4
            
            _eventQueue.Enqueue(new EntityEvent
            {
                EventType = typeof(T),
                EventData = eventData, // 装箱为 IEvent
                Frame = World?.CurFrame ?? 0
            });
            
            Astrum.CommonBase.ASLogger.Instance.Info($"[Entity.QueueEvent] Entity {UniqueId} queued event {typeof(T).Name}, " +
                $"queue size: {_eventQueue.Count}, triggerWhenInactive: {eventData.TriggerWhenInactive}");
        }
        
        /// <summary>
        /// 清空该实体的事件队列
        /// </summary>
        internal void ClearEventQueue()
        {
            _eventQueue?.Clear();
        }
    }
}

