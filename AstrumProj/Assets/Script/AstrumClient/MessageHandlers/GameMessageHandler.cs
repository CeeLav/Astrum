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
                
                // 如果当前是联机模式，调用MultiplayerGameMode的处理方法
                var currentGameMode = GameDirector.Instance?.CurrentGameMode;
                if (currentGameMode is MultiplayerGameMode multiplayerMode)
                {
                    multiplayerMode.OnGameResponse(message);
                }
                else if (message.success)
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
                
                var currentGameMode = GameDirector.Instance?.CurrentGameMode;
                
                // 情况1：已经在联机模式中
                if (currentGameMode is MultiplayerGameMode multiplayerMode)
                {
                    ASLogger.Instance.Info("GameStartNotificationHandler: 当前已在MultiplayerGameMode中，直接处理通知");
                    multiplayerMode.OnGameStartNotification(message);
                }
                // 情况2：还在登录模式中，需要先切换到联机模式再处理
                else if (currentGameMode is LoginGameMode loginMode)
                {
                    ASLogger.Instance.Info("GameStartNotificationHandler: 当前在LoginGameMode中，先切换到联机模式");
                    
                    // 切换到联机模式
                    GameDirector.Instance.SwitchGameMode(GameModeType.Multiplayer);
                    
                    // 切换完成后，获取新的 MultiplayerGameMode 实例并处理通知
                    var newMultiplayerMode = GameDirector.Instance.CurrentGameMode as MultiplayerGameMode;
                    if (newMultiplayerMode != null)
                    {
                        newMultiplayerMode.OnGameStartNotification(message);
                    }
                }
                else
                {
                    ASLogger.Instance.Warning($"GameStartNotificationHandler: 收到游戏开始通知，但当前游戏模式不支持 (Mode: {currentGameMode?.ModeName ?? "Unknown"})");
                }
                
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
                
                // 如果当前是联机模式，调用MultiplayerGameMode的处理方法
                var currentGameMode = GameDirector.Instance?.CurrentGameMode;
                if (currentGameMode is MultiplayerGameMode multiplayerMode)
                {
                    multiplayerMode.OnGameEndNotification(message);
                }
                else
                {
                    ASLogger.Instance.Warning($"GameEndNotificationHandler: 收到游戏结束通知，但当前不是联机模式");
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"GameEndNotificationHandler: 处理游戏结束通知时发生异常 - {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 游戏状态更新消息处理器
    /// </summary>
    [MessageHandler(typeof(GameStateUpdate))]
    public class GameStateUpdateHandler : MessageHandlerBase<GameStateUpdate>
    {
        public override async Task HandleMessageAsync(GameStateUpdate message)
        {
            try
            {
                ASLogger.Instance.Info($"GameStateUpdateHandler: 处理游戏状态更新 - RoomId: {message.roomId}");
                
                // 如果当前是联机模式，调用MultiplayerGameMode的处理方法
                var currentGameMode = GameDirector.Instance?.CurrentGameMode;
                if (currentGameMode is MultiplayerGameMode multiplayerMode)
                {
                    multiplayerMode.OnGameStateUpdate(message);
                }
                else
                {
                    ASLogger.Instance.Warning($"GameStateUpdateHandler: 收到游戏状态更新，但当前不是联机模式");
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"GameStateUpdateHandler: 处理游戏状态更新时发生异常 - {ex.Message}");
            }
        }
    }
}
