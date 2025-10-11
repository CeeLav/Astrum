using System;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.FrameSync;

namespace Astrum.Client.Managers.GameModes.Handlers
{
    /// <summary>
    /// 帧同步处理器 - 处理帧同步相关消息
    /// </summary>
    public class FrameSyncHandler
    {
        private readonly MultiplayerGameMode _gameMode;
        
        public FrameSyncHandler(MultiplayerGameMode gameMode)
        {
            _gameMode = gameMode;
        }
        
        /// <summary>
        /// 处理帧同步开始通知
        /// </summary>
        public void OnFrameSyncStartNotification(FrameSyncStartNotification notification)
        {
            try
            {
                ASLogger.Instance.Info(
                    $"FrameSyncHandler: 收到帧同步开始通知 - 房间: {notification.roomId}，帧率: {notification.frameRate}FPS", 
                    "FrameSync.Client");
                
                // 初始化帧同步相关状态
                if (_gameMode.MainRoom?.LSController != null)
                {
                    // 将客户端 Room/LSController 的 CreationTime 改为服务器时间
                    _gameMode.MainRoom.SetServerCreationTime(notification.startTime);
                    
                    // 仅在帧同步开始通知时启动控制器，避免重复 Start
                    if (!_gameMode.MainRoom.LSController.IsRunning)
                    {
                        _gameMode.MainRoom.LSController.Start();
                        ASLogger.Instance.Info(
                            $"帧同步控制器已启动，房间: {notification.roomId}", 
                            "FrameSync.Client");
                    }
                }
                else
                {
                    ASLogger.Instance.Error($"<LSController = null>: {notification.roomId}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理帧同步开始通知时出错: {ex.Message}", "FrameSync.Client");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理帧同步结束通知
        /// </summary>
        public void OnFrameSyncEndNotification(FrameSyncEndNotification notification)
        {
            try
            {
                ASLogger.Instance.Info(
                    $"FrameSyncHandler: 收到帧同步结束通知 - 房间: {notification.roomId}，最终帧: {notification.finalFrame}，原因: {notification.reason}", 
                    "FrameSync.Client");
                
                // 停止帧同步相关状态
                if (_gameMode.MainRoom?.LSController != null)
                {
                    _gameMode.MainRoom.LSController.Stop();
                    ASLogger.Instance.Info(
                        $"帧同步控制器已停止，房间: {notification.roomId}", 
                        "FrameSync.Client");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理帧同步结束通知时出错: {ex.Message}", "FrameSync.Client");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理帧同步数据
        /// </summary>
        public void OnFrameSyncData(FrameSyncData frameData)
        {
            try
            {
                ASLogger.Instance.Debug(
                    $"FrameSyncHandler: 收到帧同步数据 - 房间: {frameData.roomId}，权威帧: {frameData.authorityFrame}，输入数: {frameData.frameInputs.Inputs.Count}", 
                    "FrameSync.Client");
                
                // 处理帧同步数据
                DealNetFrameInputs(frameData.frameInputs, frameData.authorityFrame);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理帧同步数据时出错: {ex.Message}", "FrameSync.Client");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理帧输入数据（兼容旧接口）
        /// </summary>
        public void OnFrameInputs(OneFrameInputs frameInputs)
        {
            try
            {
                ASLogger.Instance.Debug($"FrameSyncHandler: 收到帧输入数据，输入数: {frameInputs.Inputs.Count}");
                
                // 处理帧输入数据
                DealNetFrameInputs(frameInputs);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理帧输入数据时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理网络帧输入
        /// </summary>
        private void DealNetFrameInputs(OneFrameInputs frameInputs, int frame = -1)
        {
            if (_gameMode.MainRoom == null)
            {
                ASLogger.Instance.Warning("FrameSyncHandler: 当前没有Room");
                return;
            }
            
            try
            {
                // 将帧输入数据传递给房间的帧同步控制器
                if (_gameMode.MainRoom.LSController != null)
                {
                    // 更新权威帧
                    if (frame > 0)
                    {
                        _gameMode.MainRoom.LSController.AuthorityFrame = frame;
                    }
                    else
                    {
                        ++_gameMode.MainRoom.LSController.AuthorityFrame;
                    }
                    
                    _gameMode.MainRoom.LSController.SetOneFrameInputs(frameInputs);
                    
                    ASLogger.Instance.Debug(
                        $"FrameSyncHandler: 处理网络帧输入，帧: {_gameMode.MainRoom.LSController.AuthorityFrame}，输入数: {frameInputs.Inputs.Count}");
                }
                else
                {
                    ASLogger.Instance.Warning("FrameSyncHandler: LSController 不存在，无法处理帧输入");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"FrameSyncHandler: 处理网络帧输入时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
    }
}

