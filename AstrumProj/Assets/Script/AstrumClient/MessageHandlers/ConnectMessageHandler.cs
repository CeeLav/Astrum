using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Network;
using Astrum.Client.Managers;

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
                ASLogger.Instance.Info($"ConnectMessageHandler: 处理连接响应 - Success: {message.success}, Message: {message.message}");
                
                if (message.success)
                {
                    // 连接成功，更新连接状态
                    ASLogger.Instance.Info("ConnectMessageHandler: 连接成功");
                }
                else
                {
                    // 连接失败，更新连接状态
                    ASLogger.Instance.Error($"ConnectMessageHandler: 连接失败 - {message.message}");
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"ConnectMessageHandler: 处理连接响应时发生异常 - {ex.Message}");
            }
        }
    }
}
