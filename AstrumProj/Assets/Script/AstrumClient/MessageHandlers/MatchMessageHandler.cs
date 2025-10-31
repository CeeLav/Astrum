using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Network;
using Astrum.Network.MessageHandlers;

namespace Astrum.Client.MessageHandlers
{
    /// <summary>
    /// 快速匹配响应消息处理器
    /// </summary>
    [MessageHandler(typeof(QuickMatchResponse))]
    public class QuickMatchResponseHandler : MessageHandlerBase<QuickMatchResponse>
    {
        public override async Task HandleMessageAsync(QuickMatchResponse message)
        {
            try
            {
                ASLogger.Instance.Info($"QuickMatchResponseHandler: 处理快速匹配响应 - Success: {message.Success}, Message: {message.Message}");
                
                if (message.Success)
                {
                    ASLogger.Instance.Info("QuickMatchResponseHandler: 快速匹配请求已提交");
                }
                else
                {
                    ASLogger.Instance.Error($"QuickMatchResponseHandler: 快速匹配失败 - {message.Message}");
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"QuickMatchResponseHandler: 处理快速匹配响应时发生异常 - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 取消匹配响应消息处理器
    /// </summary>
    [MessageHandler(typeof(CancelMatchResponse))]
    public class CancelMatchResponseHandler : MessageHandlerBase<CancelMatchResponse>
    {
        public override async Task HandleMessageAsync(CancelMatchResponse message)
        {
            try
            {
                ASLogger.Instance.Info($"CancelMatchResponseHandler: 处理取消匹配响应 - Success: {message.Success}, Message: {message.Message}");
                
                if (message.Success)
                {
                    ASLogger.Instance.Info("CancelMatchResponseHandler: 取消匹配成功");
                }
                else
                {
                    ASLogger.Instance.Error($"CancelMatchResponseHandler: 取消匹配失败 - {message.Message}");
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"CancelMatchResponseHandler: 处理取消匹配响应时发生异常 - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 匹配找到通知消息处理器
    /// </summary>
    [MessageHandler(typeof(MatchFoundNotification))]
    public class MatchFoundNotificationHandler : MessageHandlerBase<MatchFoundNotification>
    {
        public override async Task HandleMessageAsync(MatchFoundNotification message)
        {
            try
            {
                ASLogger.Instance.Info($"MatchFoundNotificationHandler: 处理匹配找到通知 - Room: {message.Room?.Id}");
                // 这里可以触发UI更新或游戏状态变化
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"MatchFoundNotificationHandler: 处理匹配找到通知时发生异常 - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 匹配超时通知消息处理器
    /// </summary>
    [MessageHandler(typeof(MatchTimeoutNotification))]
    public class MatchTimeoutNotificationHandler : MessageHandlerBase<MatchTimeoutNotification>
    {
        public override async Task HandleMessageAsync(MatchTimeoutNotification message)
        {
            try
            {
                ASLogger.Instance.Info($"MatchTimeoutNotificationHandler: 处理匹配超时通知");
                // 这里可以显示超时提示或重新开始匹配
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"MatchTimeoutNotificationHandler: 处理匹配超时通知时发生异常 - {ex.Message}");
            }
        }
    }
}
