using System;
using System.Threading.Tasks;
using Astrum.CommonBase;

namespace Astrum.Network.MessageHandlers
{
    /// <summary>
    /// 消息处理器接口
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="message">消息对象</param>
        /// <returns>处理任务</returns>
        Task HandleAsync(MessageObject message);
        
        /// <summary>
        /// 获取支持的消息类型
        /// </summary>
        /// <returns>消息类型</returns>
        Type GetMessageType();
    }
    
    /// <summary>
    /// 泛型消息处理器接口
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    public interface IMessageHandler<in TMessage> : IMessageHandler where TMessage : MessageObject
    {
        /// <summary>
        /// 处理特定类型的消息
        /// </summary>
        /// <param name="message">消息对象</param>
        /// <returns>处理任务</returns>
        Task HandleAsync(TMessage message);
    }
}
