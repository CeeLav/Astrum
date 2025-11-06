using System;

namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 实体事件包装（统一的事件容器）
    /// 用于双模式事件系统：面向个体事件（存实体上）和面向全体事件（全局队列）
    /// </summary>
    public struct EntityEvent
    {
        /// <summary>
        /// 事件类型
        /// </summary>
        public Type EventType;
        
        /// <summary>
        /// 事件数据（struct 装箱）
        /// </summary>
        public object EventData;
        
        /// <summary>
        /// 触发帧（用于调试和排序）
        /// </summary>
        public int Frame;
    }
}

