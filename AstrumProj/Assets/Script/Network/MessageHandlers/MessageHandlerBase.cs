using System;
using System.Threading.Tasks;
using Astrum.CommonBase;

namespace Astrum.Network.MessageHandlers
{
    /// <summary>
    /// 消息处理器基类
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    public abstract class MessageHandlerBase<TMessage> : IMessageHandler<TMessage> where TMessage : MessageObject
    {
        /// <summary>
        /// 处理消息的抽象方法，子类需要实现
        /// </summary>
        /// <param name="message">消息对象</param>
        /// <returns>处理任务</returns>
        protected abstract Task HandleMessageAsync(TMessage message);
        
        /// <summary>
        /// 实现IMessageHandler接口
        /// </summary>
        /// <param name="message">消息对象</param>
        /// <returns>处理任务</returns>
        public async Task HandleAsync(MessageObject message)
        {
            if (message is TMessage typedMessage)
            {
                await HandleMessageAsync(typedMessage);
            }
            else
            {
                ASLogger.Instance.Error($"消息类型转换失败: {message.GetType().Name} -> {typeof(TMessage).Name}");
            }
        }
        
        /// <summary>
        /// 实现IMessageHandler<TMessage>接口
        /// </summary>
        /// <param name="message">消息对象</param>
        /// <returns>处理任务</returns>
        public async Task HandleAsync(TMessage message)
        {
            await HandleMessageAsync(message);
        }
        
        /// <summary>
        /// 获取支持的消息类型
        /// </summary>
        /// <returns>消息类型</returns>
        public Type GetMessageType()
        {
            return typeof(TMessage);
        }
    }
}
