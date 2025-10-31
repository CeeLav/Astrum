using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Network;
using Astrum.Network.MessageHandlers;

namespace Astrum.Client.MessageHandlers
{
    /// <summary>
    /// 心跳响应消息处理器
    /// </summary>
    [MessageHandler(typeof(HeartbeatResponse))]
    public class HeartbeatMessageHandler : MessageHandlerBase<HeartbeatResponse>
    {
        public override async Task HandleMessageAsync(HeartbeatResponse message)
        {
            try
            {
                ASLogger.Instance.Debug($"HeartbeatMessageHandler: 收到心跳响应 - Timestamp: {message.Timestamp}");
                // 心跳响应通常不需要特殊处理，主要用于保持连接活跃
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"HeartbeatMessageHandler: 处理心跳响应时发生异常 - {ex.Message}");
            }
        }
    }
}
