using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Network;

namespace Astrum.Client.MessageHandlers
{
    /// <summary>
    /// 帧同步开始通知消息处理器
    /// </summary>
    [MessageHandler(typeof(FrameSyncStartNotification))]
    public class FrameSyncStartNotificationHandler : MessageHandlerBase<FrameSyncStartNotification>
    {
        public override async Task HandleMessageAsync(FrameSyncStartNotification message)
        {
            try
            {
                ASLogger.Instance.Info($"FrameSyncStartNotificationHandler: 处理帧同步开始通知 - RoomId: {message.roomId}");
                // 这里可以触发帧同步开始的相关逻辑
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"FrameSyncStartNotificationHandler: 处理帧同步开始通知时发生异常 - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 帧同步结束通知消息处理器
    /// </summary>
    [MessageHandler(typeof(FrameSyncEndNotification))]
    public class FrameSyncEndNotificationHandler : MessageHandlerBase<FrameSyncEndNotification>
    {
        public override async Task HandleMessageAsync(FrameSyncEndNotification message)
        {
            try
            {
                ASLogger.Instance.Info($"FrameSyncEndNotificationHandler: 处理帧同步结束通知 - RoomId: {message.roomId}");
                // 这里可以触发帧同步结束的相关逻辑
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"FrameSyncEndNotificationHandler: 处理帧同步结束通知时发生异常 - {ex.Message}");
            }
        }
    }
}
