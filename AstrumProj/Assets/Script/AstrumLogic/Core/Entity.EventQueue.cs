using System.Collections.Concurrent;
using System.Threading;
using Astrum.LogicCore.Events;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// Entity 扩展 - 事件队列支持（线程安全）
    /// 
    /// 设计说明：
    /// - 使用 ConcurrentQueue 实现线程安全
    /// - 支持 Client/View 线程和 Logic 线程并发访问
    /// - 延迟初始化，节省内存
    /// </summary>
    public partial class Entity
    {
        /// <summary>
        /// 个体事件队列（延迟创建，不序列化，线程安全）
        /// 注意：事件队列不需要序列化，因为事件是瞬时的，每帧处理完就清空
        /// </summary>
        [MemoryPack.MemoryPackIgnore]
        private ConcurrentQueue<EntityEvent> _eventQueue;
        
        /// <summary>
        /// 是否有待处理事件（线程安全）
        /// </summary>
        [MemoryPack.MemoryPackIgnore]
        public bool HasPendingEvents => _eventQueue != null && !_eventQueue.IsEmpty;
        
        /// <summary>
        /// 获取事件队列（供 CapabilitySystem 访问）
        /// </summary>
        [MemoryPack.MemoryPackIgnore]
        internal ConcurrentQueue<EntityEvent> EventQueue => _eventQueue;
        
        /// <summary>
        /// 向该实体发布事件（面向个体，线程安全）
        /// 
        /// 可以从任意线程调用（Client/View 线程或 Logic 线程）
        /// 事件将在 Logic 线程的事件处理阶段被消费
        /// </summary>
        /// <typeparam name="T">事件类型（必须实现 IEvent 接口）</typeparam>
        /// <param name="eventData">事件数据</param>
        public void QueueEvent<T>(T eventData) where T : struct, IEvent
        {
            // 延迟创建队列，节省内存，并确保线程安全
            if (_eventQueue == null)
            {
                var newQueue = new ConcurrentQueue<EntityEvent>();
                Interlocked.CompareExchange(ref _eventQueue, newQueue, null);
            }
            
            _eventQueue.Enqueue(new EntityEvent
            {
                EventType = typeof(T),
                EventData = eventData, // 装箱为 IEvent
                Frame = World?.CurFrame ?? 0
            });
        }
        
        /// <summary>
        /// 清空该实体的事件队列（线程安全）
        /// </summary>
        internal void ClearEventQueue()
        {
            if (_eventQueue == null) return;
            
            while (_eventQueue.TryDequeue(out _))
            {
                // 清空队列
            }
        }
    }
}

