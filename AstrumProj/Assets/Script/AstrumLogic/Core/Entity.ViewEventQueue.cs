using System.Collections.Generic;
using Astrum.LogicCore.Events;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// Entity 扩展 - 视图事件队列支持
    /// 用于存储待处理的视图层事件，由 Stage 轮询处理
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
        
        // 视图事件队列（延迟创建，不序列化）
        // 注意：视图事件队列不需要序列化，因为事件是瞬时的，每帧处理完就清空
        [MemoryPack.MemoryPackIgnore]
        private Queue<ViewEvent> _viewEventQueue;
        
        /// <summary>
        /// 是否有待处理的视图事件
        /// </summary>
        [MemoryPack.MemoryPackIgnore]
        public bool HasPendingViewEvents => _viewEventQueue != null && _viewEventQueue.Count > 0;
        
        /// <summary>
        /// 获取视图事件队列（供 Stage 访问）
        /// </summary>
        [MemoryPack.MemoryPackIgnore]
        public Queue<ViewEvent> ViewEventQueue => _viewEventQueue;
        
        /// <summary>
        /// 向该实体的视图事件队列中添加事件
        /// 服务器端会自动拒绝入队，避免内存泄漏
        /// </summary>
        /// <param name="evt">视图事件</param>
        public void QueueViewEvent(ViewEvent evt)
        {
            // 服务器端防护：没有视图层时拒绝入队
            // 避免服务器端积累无人消费的事件导致内存泄漏
            if (!HasViewLayer)
                return;
            
            // 延迟创建队列，节省内存
            if (_viewEventQueue == null)
                _viewEventQueue = new Queue<ViewEvent>(4); // 初始容量4
            
            _viewEventQueue.Enqueue(evt);
        }
        
        /// <summary>
        /// 清空该实体的视图事件队列
        /// </summary>
        public void ClearViewEventQueue()
        {
            _viewEventQueue?.Clear();
        }
    }
}

