using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Astrum.Generated;
using Astrum.CommonBase;
using Astrum.Network;
using AstrumServer.Network;

namespace AstrumServer.Managers
{
    /// <summary>
    /// 帧同步管理器 - 负责管理房间的帧同步逻辑
    /// </summary>
    public class FrameSyncManager
    {
        private readonly RoomManager _roomManager;
        private readonly ServerNetworkManager _networkManager;
        private readonly UserManager _userManager;
        
        // 帧同步配置
        private const int FRAME_RATE = 20; // 20FPS
        private const int FRAME_INTERVAL_MS = 1000 / FRAME_RATE; // 50ms
        
        // 房间帧同步状态
        private readonly ConcurrentDictionary<string, RoomFrameSyncState> _roomFrameStates = new();
        
        // 定时器
        private Timer? _frameTimer;
        private bool _isRunning = false;
        
        public FrameSyncManager(RoomManager roomManager, ServerNetworkManager networkManager, UserManager userManager)
        {
            _roomManager = roomManager;
            _networkManager = networkManager;
            _userManager = userManager;
        }
        
        /// <summary>
        /// 开始帧同步
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;
            
            _isRunning = true;
            _frameTimer = new Timer(OnFrameTimer, null, 0, FRAME_INTERVAL_MS);
            
            ASLogger.Instance.Info($"帧同步管理器已启动，帧率: {FRAME_RATE}FPS，间隔: {FRAME_INTERVAL_MS}ms", "FrameSync.Manager");
        }
        
        /// <summary>
        /// 停止帧同步
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;
            
            _isRunning = false;
            _frameTimer?.Dispose();
            _frameTimer = null;
            
            // 清理所有房间的帧同步状态
            _roomFrameStates.Clear();
            
            ASLogger.Instance.Info("帧同步管理器已停止", "FrameSync.Manager");
        }
        
        /// <summary>
        /// 为房间开始帧同步
        /// </summary>
        public void StartRoomFrameSync(string roomId)
        {
            try
            {
                var roomInfo = _roomManager.GetRoom(roomId);
                if (roomInfo == null)
                {
                    ASLogger.Instance.Warning($"房间不存在，无法开始帧同步: {roomId}", "FrameSync.Room");
                    return;
                }
                
                if (roomInfo.Status != 1) // 1=游戏中
                {
                    ASLogger.Instance.Warning($"房间不在游戏中，无法开始帧同步: {roomId} (Status: {roomInfo.Status})", "FrameSync.Room");
                    return;
                }
                
                // 创建房间帧同步状态
                var frameState = new RoomFrameSyncState
                {
                    RoomId = roomId,
                    AuthorityFrame = 0,
                    IsActive = true,
                    StartTime = TimeInfo.Instance.ClientNow(),
                    PlayerIds = new List<string>(roomInfo.PlayerNames)
                };
                
                _roomFrameStates[roomId] = frameState;
                
                // 发送帧同步开始通知
                SendFrameSyncStartNotification(roomId, frameState);
                
                ASLogger.Instance.Info($"房间 {roomId} 开始帧同步，玩家数: {frameState.PlayerIds.Count}", "FrameSync.Room");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"开始房间帧同步时出错: {ex.Message}", "FrameSync.Room");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 停止房间帧同步
        /// </summary>
        public void StopRoomFrameSync(string roomId, string reason = "游戏结束")
        {
            try
            {
                if (_roomFrameStates.TryRemove(roomId, out var frameState))
                {
                    frameState.IsActive = false;
                    
                    // 发送帧同步结束通知
                    SendFrameSyncEndNotification(roomId, frameState, reason);
                    
                    ASLogger.Instance.Info($"房间 {roomId} 停止帧同步，最终帧: {frameState.AuthorityFrame}，原因: {reason}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"停止房间帧同步时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理客户端发送的单帧输入数据
        /// </summary>
        public void HandleSingleInput(string roomId, SingleInput singleInput)
        {
            try
            {
                if (!_roomFrameStates.TryGetValue(roomId, out var frameState) || !frameState.IsActive)
                {
                    ASLogger.Instance.Warning($"房间 {roomId} 帧同步未激活，忽略单帧输入数据");
                    return;
                }
                
                // 将SingleInput转换为LSInput并存储
                var lsInput = singleInput.Input;
                // 使用服务器的权威帧数，而不是客户端上传的帧数
                lsInput.PlayerId = singleInput.PlayerID;
                // 将输入存储到下一帧，因为当前帧可能已经处理完了
                lsInput.Frame = frameState.AuthorityFrame + 1;
                
                // 存储输入数据到下一帧
                frameState.StoreFrameInput(lsInput);
                
                ASLogger.Instance.Debug($"收到玩家 {singleInput.PlayerID} 的单帧输入，房间: {roomId}，客户端帧: {singleInput.FrameID}，服务器帧: {frameState.AuthorityFrame}，输入已存储到帧 {lsInput.Frame}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理单帧输入时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 帧定时器回调
        /// </summary>
        private void OnFrameTimer(object? state)
        {
            if (!_isRunning) return;
            
            try
            {
                // 处理所有活跃房间的帧同步
                foreach (var kvp in _roomFrameStates)
                {
                    var roomId = kvp.Key;
                    var frameState = kvp.Value;
                    
                    if (frameState.IsActive)
                    {
                        ProcessRoomFrame(roomId, frameState);
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"帧定时器处理时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理房间的当前帧
        /// </summary>
        private void ProcessRoomFrame(string roomId, RoomFrameSyncState frameState)
        {
            try
            {
                // 检查房间是否还有玩家
                if (!CheckRoomHasPlayers(roomId, frameState))
                {
                    // 房间没有玩家了，停止帧同步
                    StopRoomFrameSync(roomId, "房间内没有玩家");
                    return;
                }
                
                // 递增权威帧
                frameState.AuthorityFrame++;
                
                // 收集当前帧的所有输入数据
                var frameInputs = frameState.CollectFrameInputs(frameState.AuthorityFrame);
                
                // 发送帧同步数据给房间内所有玩家
                SendFrameSyncData(roomId, frameState.AuthorityFrame, frameInputs);
                
                ASLogger.Instance.Debug($"处理房间 {roomId} 帧 {frameState.AuthorityFrame}，输入数: {frameInputs.Inputs.Count}", "FrameSync.Processing");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理房间帧时出错: {ex.Message}", "FrameSync.Processing");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 发送帧同步开始通知
        /// </summary>
        private void SendFrameSyncStartNotification(string roomId, RoomFrameSyncState frameState)
        {
            try
            {
                var notification = FrameSyncStartNotification.Create();
                notification.roomId = roomId;
                notification.frameRate = FRAME_RATE;
                notification.frameInterval = FRAME_INTERVAL_MS;
                notification.startTime = frameState.StartTime;
                notification.playerIds = new List<string>(frameState.PlayerIds);
                
                // 发送给房间内所有玩家
                foreach (var playerId in frameState.PlayerIds)
                {
                    var sessionId = _userManager.GetSessionIdByUserId(playerId);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _networkManager.SendMessage(sessionId, notification);
                    }
                }
                
                ASLogger.Instance.Info($"已发送帧同步开始通知给房间 {roomId} 的所有玩家");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"发送帧同步开始通知时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 发送帧同步结束通知
        /// </summary>
        private void SendFrameSyncEndNotification(string roomId, RoomFrameSyncState frameState, string reason)
        {
            try
            {
                var notification = FrameSyncEndNotification.Create();
                notification.roomId = roomId;
                notification.finalFrame = frameState.AuthorityFrame;
                notification.endTime = TimeInfo.Instance.ClientNow();
                notification.reason = reason;
                
                // 发送给房间内所有玩家
                foreach (var playerId in frameState.PlayerIds)
                {
                    var sessionId = _userManager.GetSessionIdByUserId(playerId);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _networkManager.SendMessage(sessionId, notification);
                    }
                }
                
                ASLogger.Instance.Info($"已发送帧同步结束通知给房间 {roomId} 的所有玩家");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"发送帧同步结束通知时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 发送帧同步数据
        /// </summary>
        private void SendFrameSyncData(string roomId, int authorityFrame, OneFrameInputs frameInputs)
        {
            try
            {
                var frameData = FrameSyncData.Create();
                frameData.roomId = roomId;
                frameData.authorityFrame = authorityFrame;
                frameData.frameInputs = frameInputs;
                frameData.timestamp = TimeInfo.Instance.ClientNow();
                
                // 发送给房间内所有玩家
                var frameState = _roomFrameStates.GetValueOrDefault(roomId);
                if (frameState != null)
                {
                    foreach (var playerId in frameState.PlayerIds)
                    {
                        var sessionId = _userManager.GetSessionIdByUserId(playerId);
                        if (!string.IsNullOrEmpty(sessionId))
                        {
                            _networkManager.SendMessage(sessionId, frameData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"发送帧同步数据时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 清理房间帧同步状态
        /// </summary>
        public void CleanupRoom(string roomId)
        {
            _roomFrameStates.TryRemove(roomId, out _);
            ASLogger.Instance.Debug($"已清理房间 {roomId} 的帧同步状态");
        }
        
        /// <summary>
        /// 检查房间是否还有玩家
        /// </summary>
        private bool CheckRoomHasPlayers(string roomId, RoomFrameSyncState frameState)
        {
            try
            {
                // 检查房间是否还存在
                var roomInfo = _roomManager.GetRoom(roomId);
                if (roomInfo == null)
                {
                    ASLogger.Instance.Debug($"房间 {roomId} 不存在，停止帧同步");
                    return false;
                }
                
                // 检查房间是否还有玩家
                if (roomInfo.CurrentPlayers == 0)
                {
                    ASLogger.Instance.Debug($"房间 {roomId} 没有玩家，停止帧同步");
                    return false;
                }
                
                // 检查是否有玩家在线
                var onlinePlayerCount = 0;
                foreach (var playerId in frameState.PlayerIds)
                {
                    var sessionId = _userManager.GetSessionIdByUserId(playerId);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        onlinePlayerCount++;
                    }
                }
                
                if (onlinePlayerCount == 0)
                {
                    ASLogger.Instance.Debug($"房间 {roomId} 没有在线玩家，停止帧同步");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"检查房间玩家时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return false;
            }
        }
        
        /// <summary>
        /// 获取帧同步统计信息
        /// </summary>
        public (int activeRooms, int totalFrames) GetStatistics()
        {
            var activeRooms = 0;
            var totalFrames = 0;
            
            foreach (var frameState in _roomFrameStates.Values)
            {
                if (frameState.IsActive)
                {
                    activeRooms++;
                    totalFrames += frameState.AuthorityFrame;
                }
            }
            
            return (activeRooms, totalFrames);
        }
    }
    
    /// <summary>
    /// 房间帧同步状态
    /// </summary>
    public class RoomFrameSyncState
    {
        public string RoomId { get; set; } = "";
        public int AuthorityFrame { get; set; } = 0;
        public bool IsActive { get; set; } = false;
        public long StartTime { get; set; } = 0;
        public List<string> PlayerIds { get; set; } = new();
        
        // 帧输入缓冲区 (帧号 -> 输入数据)
        private readonly Dictionary<int, Dictionary<long, LSInput>> _frameInputs = new();
        
        /// <summary>
        /// 存储帧输入数据
        /// </summary>
        public void StoreFrameInput(LSInput input)
        {
            if (!_frameInputs.ContainsKey(input.Frame))
            {
                _frameInputs[input.Frame] = new Dictionary<long, LSInput>();
            }
            
            _frameInputs[input.Frame][input.PlayerId] = input;
        }
        
        /// <summary>
        /// 收集指定帧的所有输入数据
        /// </summary>
        public OneFrameInputs CollectFrameInputs(int frame)
        {
            var frameInputs = OneFrameInputs.Create();
            
            if (_frameInputs.TryGetValue(frame, out var inputs))
            {
                foreach (var kvp in inputs)
                {
                    frameInputs.Inputs[kvp.Key] = kvp.Value;
                }
            }
            
            return frameInputs;
        }
    }
}
