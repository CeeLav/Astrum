using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Core;
using Astrum.Client.Managers;
using Astrum.View.Core;

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
                
                // 新的流程：世界数据已经从 WorldSnapshotStart + WorldSnapshotChunk 接收完成
                // FrameSyncStartNotification 不包含 worldSnapshot，只包含配置信息
                // 检查是否已经接收到世界数据
                if (_receivedWorldSnapshot != null && _receivedWorldSnapshot.TryGetValue(notification.roomId, out var worldSnapshot))
                {
                    // 世界数据已接收，立即处理
                    ProcessFrameSyncStartWithWorldData(notification, worldSnapshot);
                    // 清理已使用的世界数据
                    _receivedWorldSnapshot.Remove(notification.roomId);
                }
                else
                {
                    // 世界数据尚未接收完成，检查是否有接收器正在接收
                    bool isReceiving = Astrum.Client.MessageHandlers.WorldSnapshotChunkHandler.HasReceiver(notification.roomId);
                    
                    if (isReceiving)
                    {
                        // 正在接收但尚未完成，这是协议顺序错误
                        ASLogger.Instance.Error(
                            $"FrameSyncHandler: 协议顺序错误！收到帧同步开始通知时，世界数据尚未接收完成 - 房间: {notification.roomId}。正确的顺序应该是：先发送 WorldSnapshotStart，然后发送 WorldSnapshotChunk 分片，最后发送 FrameSyncStartNotification",
                            "FrameSync.Client");
                        
                        // 清理错误的接收器
                        Astrum.Client.MessageHandlers.WorldSnapshotChunkHandler.ClearReceiver(notification.roomId);
                        
                        // 抛出异常，不允许继续
                        throw new System.InvalidOperationException(
                            $"协议顺序错误：收到帧同步开始通知时，房间 {notification.roomId} 的世界数据尚未接收完成");
                    }
                    else
                    {
                        // 没有接收器，说明 WorldSnapshotStart 都没收到，这是更严重的协议顺序错误
                        ASLogger.Instance.Error(
                            $"FrameSyncHandler: 协议顺序错误！收到帧同步开始通知时，未找到世界数据接收器（WorldSnapshotStart 可能未发送或已超时） - 房间: {notification.roomId}。正确的顺序应该是：先发送 WorldSnapshotStart，然后发送 WorldSnapshotChunk 分片，最后发送 FrameSyncStartNotification",
                            "FrameSync.Client");
                        
                        // 抛出异常，不允许继续
                        throw new System.InvalidOperationException(
                            $"协议顺序错误：收到帧同步开始通知时，房间 {notification.roomId} 的世界数据接收器不存在（WorldSnapshotStart 未发送或已超时）");
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理帧同步开始通知时出错: {ex.Message}", "FrameSync.Client");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        // 存储已接收的世界快照数据（房间ID -> 快照数据）
        private Dictionary<string, byte[]> _receivedWorldSnapshot = new Dictionary<string, byte[]>();

        /// <summary>
        /// 使用世界数据和帧同步通知初始化游戏
        /// </summary>
        private void ProcessFrameSyncStartWithWorldData(FrameSyncStartNotification notification, byte[] worldSnapshot)
        {
            try
            {
                // 如果 MainRoom 为空，创建 Room
                if (_gameMode.MainRoom == null)
                {
                    ASLogger.Instance.Info($"创建 Room（帧同步开始）", "FrameSync.Client");
                    
                    // 创建 Room 并初始化（传入 World 快照）
                    var room = new Room(1, notification.roomId);
                    room.Initialize("client", worldSnapshot);
                    _gameMode.MainRoom = room;
                    
                    // 如果 Stage 已存在，设置 Room 到 Stage
                    if (_gameMode.MainStage != null)
                    {
                        _gameMode.SetRoomAndStage(room, _gameMode.MainStage);
                        ASLogger.Instance.Info($"已设置 Room 到现有 Stage", "FrameSync.Client");
                    }
                    else
                    {
                        ASLogger.Instance.Warning($"收到 FrameSyncStartNotification 但 Stage 为空，等待 GameStartNotification 创建 Stage", "FrameSync.Client");
                    }
                }
                else
                {
                    // Room 已存在，使用 Room.LoadWorldFromSnapshot 加载 World 快照
                    var loadedWorld = _gameMode.MainRoom.LoadWorldFromSnapshot(worldSnapshot);
                    if (loadedWorld == null)
                    {
                        ASLogger.Instance.Error("World 快照加载失败", "FrameSync.Client");
                        return;
                    }
                }
                
                // 确保 MainRoom 存在
                if (_gameMode.MainRoom == null)
                {
                    ASLogger.Instance.Error("MainRoom 为空，无法恢复世界状态", "FrameSync.Client");
                    return;
                }
                
                // 获取加载后的 World（用于后续处理）
                var world = _gameMode.MainRoom.MainWorld;
                if (world == null)
                {
                    ASLogger.Instance.Error("MainWorld 为空，无法继续处理", "FrameSync.Client");
                    return;
                }
                
                // 从 playerIdMapping 获取 PlayerId
                if (notification.playerIdMapping != null && notification.playerIdMapping.Count > 0)
                {
                    var userId = UserManager.Instance.UserId;
                    if (notification.playerIdMapping.TryGetValue(userId, out var playerId))
                    {
                        _gameMode.PlayerId = playerId;
                        if (_gameMode.MainRoom != null)
                        {
                            _gameMode.MainRoom.MainPlayerId = playerId;
                        }
                        ASLogger.Instance.Info($"玩家注册成功 - UserId: {userId}, PlayerId: {playerId}", "FrameSync.Client");
                    }
                    else
                    {
                        ASLogger.Instance.Warning($"PlayerId 映射中未找到当前用户 - UserId: {userId}", "FrameSync.Client");
                    }
                }
                else
                {
                    ASLogger.Instance.Warning("PlayerId 映射为空，无法获取 PlayerId", "FrameSync.Client");
                }
                
                // 初始化帧同步相关状态
                if (_gameMode.MainRoom?.LSController != null)
                {
                    // 将客户端 Room/LSController 的 CreationTime 改为服务器时间
                    _gameMode.MainRoom.SetServerCreationTime(notification.startTime);
                    
                    // 将快照数据加载到 FrameBuffer（用于回滚）
                    var snapshotBuffer = _gameMode.MainRoom.LSController.FrameBuffer.Snapshot(0);
                    snapshotBuffer.Seek(0, SeekOrigin.Begin);
                    snapshotBuffer.SetLength(0);
                    snapshotBuffer.Write(worldSnapshot, 0, worldSnapshot.Length);
                    
                    // 启动控制器
                    if (!_gameMode.MainRoom.LSController.IsRunning)
                    {
                        _gameMode.MainRoom.LSController.Start();
                        ASLogger.Instance.Info(
                            $"帧同步控制器已启动，世界快照已加载到 FrameBuffer，房间: {notification.roomId}", 
                            "FrameSync.Client");
                    }
                }
                else
                {
                    ASLogger.Instance.Error($"LSController 为空，无法启动帧同步: {notification.roomId}", "FrameSync.Client");
                    return;
                }
                
                // 使用 Stage 的同步方法创建 EntityView
                if (_gameMode.MainStage != null)
                {
                    _gameMode.MainStage.SyncEntityViews();
                    _gameMode.FindAndSetupPlayer();
                }
                else
                {
                    ASLogger.Instance.Warning($"MainStage 为空，无法同步 EntityView", "FrameSync.Client");
                }
                
                ASLogger.Instance.Info($"帧同步已启动，世界状态已恢复", "FrameSync.Client");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理帧同步开始通知时出错: {ex.Message}", "FrameSync.Client");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }


        /// <summary>
        /// 处理完整的世界快照数据（由分片接收完成后调用）
        /// </summary>
        public void OnWorldSnapshotComplete(string roomId, byte[] worldSnapshot)
        {
            try
            {
                ASLogger.Instance.Info(
                    $"FrameSyncHandler: 收到完整的世界快照 - 房间: {roomId}，大小: {worldSnapshot?.Length ?? 0} bytes", 
                    "FrameSync.Client");

                if (worldSnapshot == null || worldSnapshot.Length == 0)
                {
                    ASLogger.Instance.Error("世界快照数据为空，无法恢复世界状态", "FrameSync.Client");
                    return;
                }

                // 保存世界快照数据，等待 FrameSyncStartNotification
                if (_receivedWorldSnapshot == null)
                {
                    _receivedWorldSnapshot = new Dictionary<string, byte[]>();
                }
                _receivedWorldSnapshot[roomId] = worldSnapshot;

                ASLogger.Instance.Info(
                    $"FrameSyncHandler: 世界数据已保存，等待帧同步开始通知 - 房间: {roomId}",
                    "FrameSync.Client");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理完整世界快照时出错: {ex.Message}", "FrameSync.Client");
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
                // 只在有实际输入时输出日志
                bool hasActualInput = HasAnyActualInput(frameData.frameInputs);
                if (hasActualInput)
                {
                    ASLogger.Instance.Debug(
                        $"FrameSyncHandler: 收到帧同步数据 - 房间: {frameData.roomId}，权威帧: {frameData.authorityFrame}，输入数: {frameData.frameInputs.Inputs.Count}", 
                        "FrameSync.Client");
                }
                
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
                // 只在有实际输入时输出日志
                bool hasActualInput = HasAnyActualInput(frameInputs);
                if (hasActualInput)
                {
                    ASLogger.Instance.Debug($"FrameSyncHandler: 收到帧输入数据，输入数: {frameInputs.Inputs.Count}");
                }
                
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
                if (_gameMode.MainRoom.LSController is ClientLSController clientSync)
                {
                    // 确定要设置的权威帧号
                    int targetAuthorityFrame = frame > 0 ? frame : clientSync.AuthorityFrame + 1;
                    
                    // 断线重连时，服务器发送的帧号可能远大于客户端当前 MaxFrame
                    // 需要先扩展 FrameBuffer 到足够的大小
                    var frameBuffer = clientSync.FrameBuffer;
                    if (targetAuthorityFrame > frameBuffer.MaxFrame)
                    {
                        ASLogger.Instance.Info(
                            $"FrameSyncHandler: 检测到帧号超出范围，扩展 FrameBuffer - 当前 MaxFrame: {frameBuffer.MaxFrame}, 目标帧: {targetAuthorityFrame}", 
                            "FrameSync.Client");
                        
                        // 扩展 FrameBuffer，留出足够的缓冲空间（至少1秒）
                        int targetMaxFrame = targetAuthorityFrame + LSConstValue.FrameCountPerSecond;
                        while (frameBuffer.MaxFrame < targetMaxFrame)
                        {
                            frameBuffer.MoveForward(frameBuffer.MaxFrame);
                        }
                        
                        ASLogger.Instance.Info(
                            $"FrameSyncHandler: FrameBuffer 已扩展 - 新 MaxFrame: {frameBuffer.MaxFrame}", 
                            "FrameSync.Client");
                    }
                    
                    // 更新权威帧
                    clientSync.AuthorityFrame = targetAuthorityFrame;
                    
                    // 断线重连时，如果 AuthorityFrame 远大于 PredictionFrame，需要同步设置 PredictionFrame
                    // 避免触发不必要的回滚逻辑，并确保预测帧从权威帧开始
                    if (targetAuthorityFrame > clientSync.PredictionFrame)
                    {
                        ASLogger.Instance.Info(
                            $"FrameSyncHandler: 检测到断线重连，同步 PredictionFrame - AuthorityFrame: {targetAuthorityFrame}, 旧 PredictionFrame: {clientSync.PredictionFrame}", 
                            "FrameSync.Client");
                        
                        // 将 PredictionFrame 设置为 AuthorityFrame - 1，这样下次 Tick 时会变成 AuthorityFrame
                        // 确保预测帧从权威帧开始，不会小于权威帧
                        clientSync.PredictionFrame = targetAuthorityFrame - 1;
                        
                        ASLogger.Instance.Info(
                            $"FrameSyncHandler: PredictionFrame 已同步 - 新 PredictionFrame: {clientSync.PredictionFrame} (下次 Tick 后会变成 {targetAuthorityFrame})", 
                            "FrameSync.Client");
                    }
                    
                    clientSync.SetOneFrameInputs(frameInputs);
                    
                    // 只在有实际输入时输出日志
                    bool hasActualInput = HasAnyActualInput(frameInputs);
                    if (hasActualInput)
                    {
                        ASLogger.Instance.Debug(
                            $"FrameSyncHandler: 处理网络帧输入，帧: {clientSync.AuthorityFrame}，输入数: {frameInputs.Inputs.Count}");
                    }
                }
                else
                {
                    ASLogger.Instance.Warning("FrameSyncHandler: LSController 不存在或不是客户端控制器，无法处理帧输入");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"FrameSyncHandler: 处理网络帧输入时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 检查帧输入中是否有任何实际输入
        /// </summary>
        private static bool HasAnyActualInput(OneFrameInputs frameInputs)
        {
            if (frameInputs?.Inputs == null) return false;
            
            foreach (var input in frameInputs.Inputs.Values)
            {
                if (HasActualInput(input))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 检查单个输入是否有实际动作（非空输入）
        /// </summary>
        private static bool HasActualInput(LSInput input)
        {
            if (input == null) return false;
            
            // 检查移动输入
            if (input.MoveX != 0 || input.MoveY != 0)
                return true;
            
            // 检查按钮输入
            if (input.Attack || input.Skill1 || input.Skill2 || input.Roll || input.Dash)
                return true;

            
            return false;
        }
    }
}

