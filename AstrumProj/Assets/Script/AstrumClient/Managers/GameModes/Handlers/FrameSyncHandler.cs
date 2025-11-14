using System;
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
                
                // 检查世界快照数据
                if (notification.worldSnapshot == null || notification.worldSnapshot.Length == 0)
                {
                    ASLogger.Instance.Error("世界快照数据为空，无法恢复世界状态", "FrameSync.Client");
                    return;
                }
                
                // 如果 MainRoom 为空，创建 Room（Stage 应该在 GameStartNotification 中已创建）
                if (_gameMode.MainRoom == null)
                {
                    ASLogger.Instance.Info($"创建 Room（FrameSyncStartNotification 到达）", "FrameSync.Client");
                    
                    // 创建 Room
                    var room = new Room(1, notification.roomId);
                    _gameMode.MainRoom = room;
                    
                    // 如果 Stage 已存在，设置 Room 到 Stage
                    if (_gameMode.MainStage != null)
                    {
                        _gameMode.MainStage.SetRoom(room);
                        ASLogger.Instance.Info($"已设置 Room 到现有 Stage", "FrameSync.Client");
                    }
                    else
                    {
                        // Stage 不存在（消息顺序问题：FrameSyncStartNotification 先于 GameStartNotification 到达）
                        // 只创建 Room，不创建 Stage，等待 GameStartNotification 到达
                        ASLogger.Instance.Warning($"收到 FrameSyncStartNotification 但 Stage 为空，等待 GameStartNotification 创建 Stage", "FrameSync.Client");
                    }
                }
                
                // 反序列化 World
                World world = null;
                try
                {
                    world = MemoryPackHelper.Deserialize(typeof(World), notification.worldSnapshot, 0, notification.worldSnapshot.Length) as World;
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Error($"世界快照反序列化失败: {ex.Message}", "FrameSync.Client");
                    ASLogger.Instance.LogException(ex, LogLevel.Error);
                    return;
                }
                
                if (world == null)
                {
                    ASLogger.Instance.Error("世界快照反序列化结果为空", "FrameSync.Client");
                    return;
                }
                
                ASLogger.Instance.Info($"世界快照反序列化成功，实体数量: {world.Entities?.Count ?? 0}", "FrameSync.Client");
                
                // 替换 MainRoom.MainWorld
                if (_gameMode.MainRoom != null)
                {
                    // 清理旧的世界
                    _gameMode.MainRoom.MainWorld?.Cleanup();
                    
                    // 设置新世界
                    _gameMode.MainRoom.MainWorld = world;
                    
                    // 重建 World 的引用关系
                    world.RoomId = (long)_gameMode.MainRoom.RoomId; // 显式类型转换：Room.RoomId (int) -> World.RoomId (long)
                    // 注意：World 的 Systems 等引用会在反序列化后自动重建（通过 MemoryPackConstructor）
                }
                else
                {
                    ASLogger.Instance.Error("MainRoom 为空，无法恢复世界状态（即使已尝试创建）", "FrameSync.Client");
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
                    snapshotBuffer.Write(notification.worldSnapshot, 0, notification.worldSnapshot.Length);
                    
                    // 仅在帧同步开始通知时启动控制器，避免重复 Start
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
                
                // 为快照中的所有实体创建 EntityView
                if (world.Entities != null)
                {
                    int entityViewCount = 0;
                    foreach (var entity in world.Entities.Values)
                    {
                        if (!entity.IsDestroyed)
                        {
                            // 发布 EntityCreatedEventData 事件，触发 Stage 创建 EntityView
                            var eventData = new EntityCreatedEventData(entity, world.WorldId, world.RoomId);
                            EventSystem.Instance.Publish(eventData);
                            entityViewCount++;
                        }
                    }
                    ASLogger.Instance.Info($"已为 {entityViewCount} 个实体创建 EntityView", "FrameSync.Client");
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
                if (_gameMode.MainRoom.LSController is ClientLSController clientSync)
                {
                    // 更新权威帧
                    if (frame > 0)
                    {
                        clientSync.AuthorityFrame = frame;
                    }
                    else
                    {
                        ++clientSync.AuthorityFrame;
                    }
                    
                    clientSync.SetOneFrameInputs(frameInputs);
                    
                    ASLogger.Instance.Debug(
                        $"FrameSyncHandler: 处理网络帧输入，帧: {clientSync.AuthorityFrame}，输入数: {frameInputs.Inputs.Count}");
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
    }
}

