using System.Collections.Concurrent;
using System.Threading;
using Astrum.LogicCore.Events;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// Entity 扩展 - 视图事件队列支持（线程安全）
    /// 用于存储待处理的视图层事件，由 Stage 轮询处理
    /// 
    /// 线程安全说明：
    /// - Logic 线程可以安全地调用 QueueViewEvent() 写入事件
    /// - View 线程（主线程）可以安全地读取和消费 ViewEventQueue
    /// - 使用 ConcurrentQueue 实现无锁并发访问
    /// </summary>
    public partial class Entity
    {
        /// <summary>
        /// 静态标记：当前环境是否有视图层
        /// 客户端：true（有 Stage/EntityView）
        /// 服务器：false（没有视图层）
        /// 用于防止服务器端内存泄漏
        /// </summary>
        public static bool HasViewLayer { get; set; } = false;
        
        /// <summary>
        /// 视图事件队列（延迟创建，不序列化）
        /// 线程安全：使用 ConcurrentQueue 支持 Logic 线程写入、View 线程消费
        /// 注意：视图事件队列不需要序列化，因为事件是瞬时的，每帧处理完就清空
        /// </summary>
        [MemoryPack.MemoryPackIgnore]
        private ConcurrentQueue<ViewEvent> _viewEventQueue;
        
        /// <summary>
        /// 是否有待处理的视图事件
        /// 注意：IsEmpty 在并发环境下是快照值，可能不精确，但不影响正确性
        /// </summary>
        [MemoryPack.MemoryPackIgnore]
        public bool HasPendingViewEvents => _viewEventQueue != null && !_viewEventQueue.IsEmpty;
        
        /// <summary>
        /// 获取视图事件队列（供 Stage 访问）
        /// View 线程应使用 TryDequeue() 来消费事件
        /// </summary>
        [MemoryPack.MemoryPackIgnore]
        public ConcurrentQueue<ViewEvent> ViewEventQueue => _viewEventQueue;
        
        /// <summary>
        /// 向该实体的视图事件队列中添加事件（线程安全）
        /// Logic 线程可以安全调用此方法
        /// 服务器端会自动拒绝入队，避免内存泄漏
        /// </summary>
        /// <param name="evt">视图事件</param>
        public void QueueViewEvent(ViewEvent evt)
        {
            // 服务器端防护：没有视图层时拒绝入队
            // 避免服务器端积累无人消费的事件导致内存泄漏
            if (!HasViewLayer)
                return;
            
            // 线程安全的延迟创建队列，节省内存
            // 使用 Interlocked.CompareExchange 确保多线程环境下只创建一次
            if (_viewEventQueue == null)
            {
                var newQueue = new ConcurrentQueue<ViewEvent>();
                Interlocked.CompareExchange(ref _viewEventQueue, newQueue, null);
            }
            
            _viewEventQueue.Enqueue(evt);
        }
        
        /// <summary>
        /// 清空该实体的视图事件队列（线程安全）
        /// 使用 TryDequeue 循环清空，避免并发问题
        /// </summary>
        public void ClearViewEventQueue()
        {
            if (_viewEventQueue == null) return;
            
            // 使用 TryDequeue 清空队列（线程安全）
            while (_viewEventQueue.TryDequeue(out _))
            {
                // 丢弃所有事件
            }
        }
    }
}

