using System;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Network;
using Astrum.Network.MessageHandlers;

namespace Astrum.Client.MessageHandlers
{
    /// <summary>
    /// 消息处理器基类
    /// 提供类型安全的消息处理
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    public abstract class MessageHandlerBase<TMessage> : IMessageHandler<TMessage> where TMessage : MessageObject
    {
        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="message">消息对象</param>
        /// <returns>处理任务</returns>
        public async Task HandleAsync(MessageObject message)
        {
            if (message is TMessage specificMessage)
            {
                await HandleMessageAsync(specificMessage);
            }
            else
            {
                ASLogger.Instance.Error($"消息类型不匹配: 期望 {typeof(TMessage).Name}, 实际 {message.GetType().Name}");
            }
        }

        /// <summary>
        /// 处理特定类型的消息
        /// </summary>
        /// <param name="message">消息对象</param>
        /// <returns>处理任务</returns>
        public async Task HandleAsync(TMessage message)
        {
            await HandleMessageAsync(message);
        }

        /// <summary>
        /// 处理特定类型的消息
        /// </summary>
        /// <param name="message">消息对象</param>
        /// <returns>处理任务</returns>
        public abstract Task HandleMessageAsync(TMessage message);

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
