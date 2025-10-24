using System;
using Astrum.CommonBase;

namespace Astrum.Client.MessageHandlers
{
    /// <summary>
    /// 消息处理器属性
    /// 用于标记消息处理器类，指定其处理的消息类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MessageHandlerAttribute : BaseAttribute
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// 初始化消息处理器属性
        /// </summary>
        /// <param name="messageType">消息类型</param>
        public MessageHandlerAttribute(Type messageType)
        {
            if (!typeof(MessageObject).IsAssignableFrom(messageType))
            {
                throw new ArgumentException($"MessageType must inherit from {nameof(MessageObject)}", nameof(messageType));
            }
            MessageType = messageType;
        }
    }
}
