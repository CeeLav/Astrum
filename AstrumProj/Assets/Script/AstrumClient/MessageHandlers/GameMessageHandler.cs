using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Network;

namespace Astrum.Client.MessageHandlers
{
    /// <summary>
    /// 游戏响应消息处理器
    /// </summary>
    [MessageHandler(typeof(GameResponse))]
    public class GameResponseHandler : MessageHandlerBase<GameResponse>
    {
        public override async Task HandleMessageAsync(GameResponse message)
        {
            try
            {
                ASLogger.Instance.Info($"GameResponseHandler: 处理游戏响应 - Success: {message.success}, Message: {message.message}");
                
                if (message.success)
                {
                    ASLogger.Instance.Info("GameResponseHandler: 游戏操作成功");
                }
                else
                {
                    ASLogger.Instance.Error($"GameResponseHandler: 游戏操作失败 - {message.message}");
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"GameResponseHandler: 处理游戏响应时发生异常 - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 游戏开始通知消息处理器
    /// </summary>
    [MessageHandler(typeof(GameStartNotification))]
    public class GameStartNotificationHandler : MessageHandlerBase<GameStartNotification>
    {
        public override async Task HandleMessageAsync(GameStartNotification message)
        {
            try
            {
                ASLogger.Instance.Info($"GameStartNotificationHandler: 处理游戏开始通知 - RoomId: {message.roomId}");
                // 这里可以触发游戏开始的相关逻辑
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"GameStartNotificationHandler: 处理游戏开始通知时发生异常 - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 游戏结束通知消息处理器
    /// </summary>
    [MessageHandler(typeof(GameEndNotification))]
    public class GameEndNotificationHandler : MessageHandlerBase<GameEndNotification>
    {
        public override async Task HandleMessageAsync(GameEndNotification message)
        {
            try
            {
                ASLogger.Instance.Info($"GameEndNotificationHandler: 处理游戏结束通知 - RoomId: {message.roomId}");
                // 这里可以触发游戏结束的相关逻辑
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"GameEndNotificationHandler: 处理游戏结束通知时发生异常 - {ex.Message}");
            }
        }
    }
}
