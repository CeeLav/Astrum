using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Network;
using Astrum.Network.MessageHandlers;
using Astrum.Client.Managers;
using Astrum.Client.Core;
using Astrum.CommonBase;

namespace Astrum.Client.MessageHandlers
{
    /// <summary>
    /// 连接响应消息处理器
    /// </summary>
    [MessageHandler(typeof(ConnectResponse))]
    public class ConnectMessageHandler : MessageHandlerBase<ConnectResponse>
    {
        public override async Task HandleMessageAsync(ConnectResponse message)
        {
            try
            {
                //ASLogger.Instance.Info($"ConnectMessageHandler: 处理连接响应 - Success: {message.success}, Message: {message.message}");
                
                if (message.success)
                {
                    // 连接成功，更新连接状态
                    //ASLogger.Instance.Info("ConnectMessageHandler: 连接成功");
                    
                    // 通过事件系统通知连接成功
                    var eventData = new ConnectResponseEventData
                    {
                        Success = true,
                        Message = message.message
                    };
                    EventSystem.Instance.Publish(eventData);
                }
                else
                {
                    // 连接失败，更新连接状态
                    ASLogger.Instance.Error($"ConnectMessageHandler: 连接失败 - {message.message}");
                    
                    // 通过事件系统通知连接失败
                    var eventData = new ConnectResponseEventData
                    {
                        Success = false,
                        Message = message.message
                    };
                    EventSystem.Instance.Publish(eventData);
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"ConnectMessageHandler: 处理连接响应时发生异常 - {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 连接响应事件数据
    /// </summary>
    public class ConnectResponseEventData : EventData
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
