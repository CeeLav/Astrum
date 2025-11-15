using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Network;
using Astrum.Client.Core;
using Astrum.Client.Managers.GameModes;
using Astrum.Network.MessageHandlers;

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
                
                // 如果当前是联机模式，调用MultiplayerGameMode的处理方法
                var currentGameMode = GameDirector.Instance?.CurrentGameMode;
                if (currentGameMode is MultiplayerGameMode multiplayerMode)
                {
                    multiplayerMode.OnFrameSyncStartNotification(message);
                }
                else
                {
                    ASLogger.Instance.Warning($"FrameSyncStartNotificationHandler: 收到帧同步开始通知，但当前不是联机模式");
                }
                
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
                
                // 如果当前是联机模式，调用MultiplayerGameMode的处理方法
                var currentGameMode = GameDirector.Instance?.CurrentGameMode;
                if (currentGameMode is MultiplayerGameMode multiplayerMode)
                {
                    multiplayerMode.OnFrameSyncEndNotification(message);
                }
                else
                {
                    ASLogger.Instance.Warning($"FrameSyncEndNotificationHandler: 收到帧同步结束通知，但当前不是联机模式");
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"FrameSyncEndNotificationHandler: 处理帧同步结束通知时发生异常 - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 帧同步数据消息处理器
    /// </summary>
    [MessageHandler(typeof(FrameSyncData))]
    public class FrameSyncDataHandler : MessageHandlerBase<FrameSyncData>
    {
        public override async Task HandleMessageAsync(FrameSyncData message)
        {
            try
            {
                // 注释掉频繁的日志
                // ASLogger.Instance.Debug($"FrameSyncDataHandler: 处理帧同步数据 - RoomId: {message.roomId}, AuthorityFrame: {message.authorityFrame}");
                
                // 如果当前是联机模式，调用MultiplayerGameMode的处理方法
                var currentGameMode = GameDirector.Instance?.CurrentGameMode;
                if (currentGameMode is MultiplayerGameMode multiplayerMode)
                {
                    multiplayerMode.OnFrameSyncData(message);
                }
                else
                {
                    ASLogger.Instance.Warning($"FrameSyncDataHandler: 收到帧同步数据，但当前不是联机模式");
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"FrameSyncDataHandler: 处理帧同步数据时发生异常 - {ex.Message}");
            }
        }
    }
}
