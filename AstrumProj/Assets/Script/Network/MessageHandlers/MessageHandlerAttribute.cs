using System;
using Astrum.CommonBase;

namespace Astrum.Network.MessageHandlers
{
    /// <summary>
    /// 消息处理器属性标记
    /// 用于标记消息处理器类，支持自动注册
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MessageHandlerAttribute : BaseAttribute
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public Type MessageType { get; }
        
        /// <summary>
        /// 处理器优先级（数字越小优先级越高）
        /// </summary>
        public int Priority { get; set; } = 0;
        
        /// <summary>
        /// 是否启用处理器
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="messageType">消息类型</param>
        public MessageHandlerAttribute(Type messageType)
        {
            MessageType = messageType;
        }
    }
}
