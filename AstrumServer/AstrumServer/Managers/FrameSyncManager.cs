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

        // 测试/观测用事件：下发帧数据前回调（便于单元测试捕获）
        public event Action<string, int, OneFrameInputs, string>? OnBeforeSendFrameToPlayer;
        
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
        
        public Dictionary<string,int> currentPlayerIdMap = new Dictionary<string, int>();
        
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
                
                // 调试：记录接收到的原始数据
                ASLogger.Instance.Debug($"收到SingleInput - PlayerID: {singleInput.PlayerID}, FrameID: {singleInput.FrameID}, Input.PlayerId: {singleInput.Input?.PlayerId}");
                
                // 将SingleInput转换为LSInput
                var lsInput = singleInput.Input;
                if (!currentPlayerIdMap.ContainsKey(roomId))
                {
                    currentPlayerIdMap[roomId] = 0;
                }
                lsInput.PlayerId = singleInput.PlayerID != 0 ? singleInput.PlayerID : ++currentPlayerIdMap[roomId];
                
                // 保持客户端原始帧号，让StoreFrameInput方法处理帧号验证
                lsInput.Frame = singleInput.FrameID;
                
                // 详细记录接收到的输入数据
                LogReceivedInputDetails(roomId, lsInput.PlayerId.ToString(), lsInput, frameState.AuthorityFrame);
                
                // 存储输入数据（内部会处理帧号验证和缓存）
                frameState.StoreFrameInput(lsInput);
                
                ASLogger.Instance.Debug($"收到玩家 {lsInput.PlayerId} 的单帧输入，房间: {roomId}，客户端帧: {singleInput.FrameID}，服务器帧: {frameState.AuthorityFrame}，最终存储帧: {lsInput.Frame}");
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
                
                // 收集当前帧的所有输入数据（从缓存中获取）
                var frameInputs = frameState.CollectFrameInputs(frameState.AuthorityFrame);
                
                // 发送帧同步数据给房间内所有玩家
                SendFrameSyncData(roomId, frameState.AuthorityFrame, frameInputs);
                
                ASLogger.Instance.Debug($"处理房间 {roomId} 帧 {frameState.AuthorityFrame}，输入数: {frameInputs.Inputs.Count}，缓存总帧数: {frameState.GetCacheFrameCount()}", "FrameSync.Processing");
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
                // 详细记录帧同步数据内容
                LogFrameSyncDataDetails(roomId, authorityFrame, frameInputs);
                
                var frameData = FrameSyncData.Create();
                frameData.roomId = roomId;
                frameData.authorityFrame = authorityFrame;
                frameData.frameInputs = frameInputs;
                frameData.timestamp = TimeInfo.Instance.ClientNow();
                ASLogger.Instance.Debug($"发送帧同步数据 - 房间: {roomId}, 帧: {authorityFrame}, 输入数: {frameInputs.Inputs.Count}, 时间戳: {frameData.timestamp}", "FrameSync.Send");
                if (frameData.frameInputs.Inputs.Count > 0)
                {
                    ASLogger.Instance.Debug($"input frame: {frameData.frameInputs.Inputs.Values.First().Frame}", "FrameSync.Send");
                }
                // 发送给房间内所有玩家
                var frameState = _roomFrameStates.GetValueOrDefault(roomId);
                if (frameState != null)
                {
                    int successCount = 0;
                    int failCount = 0;
                    
                    foreach (var playerId in frameState.PlayerIds)
                    {
                        // 先触发测试观测事件（不依赖底层网络），保证即使网络层失败也能在测试中捕获到数据
                        try
                        {
                            OnBeforeSendFrameToPlayer?.Invoke(roomId, authorityFrame, frameInputs, playerId);
                        }
                        catch (Exception cbEx)
                        {
                            ASLogger.Instance.Warning($"OnBeforeSendFrameToPlayer 回调抛出异常: {cbEx.Message}", "FrameSync.Send");
                        }

                        var sessionId = _userManager.GetSessionIdByUserId(playerId);
                        if (!string.IsNullOrEmpty(sessionId))
                        {
                            try
                            {
                                _networkManager.SendMessage(sessionId, frameData);
                                successCount++;
                                
                                // 记录发送给每个玩家的详细信息
                                LogPlayerFrameDataSent(roomId, playerId, authorityFrame, frameInputs);
                            }
                            catch (Exception ex)
                            {
                                failCount++;
                                ASLogger.Instance.Error($"发送帧数据给玩家 {playerId} 失败: {ex.Message}", "FrameSync.Send");
                            }
                        }
                        else
                        {
                            failCount++;
                            ASLogger.Instance.Warning($"玩家 {playerId} 的会话ID为空，无法发送帧数据", "FrameSync.Send");
                        }
                    }
                    
                    //ASLogger.Instance.Info($"房间 {roomId} 帧 {authorityFrame} 数据发送完成 - 成功: {successCount}, 失败: {failCount}", "FrameSync.Send");
                }
                else
                {
                    ASLogger.Instance.Warning($"房间 {roomId} 的帧同步状态不存在，无法发送帧数据", "FrameSync.Send");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"发送帧同步数据时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 详细记录接收到的输入数据
        /// </summary>
        private void LogReceivedInputDetails(string roomId, string playerId, LSInput input, int serverFrame)
        {
            try
            {
                if (input == null)
                {
                    ASLogger.Instance.Warning($"房间 {roomId} 玩家 {playerId} 输入数据为 null", "FrameSync.Input");
                    return;
                }
                
                // 检查输入内容
                bool hasMovement = input.MoveX != 0 || input.MoveY != 0;
                bool hasBornInfo = input.BornInfo != 0;
                bool hasSkills = input.Skill1 || input.Skill2;
                bool hasAttack = input.Attack;
                
                if (hasMovement || hasBornInfo || hasSkills || hasAttack)
                {
                    var inputDetails = new List<string>();
                    
                    if (hasMovement)
                    {
                        inputDetails.Add($"移动:({input.MoveX:F2},{input.MoveY:F2})");
                    }
                    
                    if (hasBornInfo)
                    {
                        inputDetails.Add($"出生信息:{input.BornInfo}");
                    }
                    
                    if (hasAttack)
                    {
                        inputDetails.Add("攻击");
                    }
                    
                    if (hasSkills)
                    {
                        var skills = new List<string>();
                        if (input.Skill1) skills.Add("技能1");
                        if (input.Skill2) skills.Add("技能2");
                        inputDetails.Add($"技能:[{string.Join(",", skills)}]");
                    }
                    
                    ASLogger.Instance.Info($"房间 {roomId} 收到玩家 {playerId} 有效输入 (帧:{input.Frame}): {string.Join(", ", inputDetails)}", "FrameSync.Input");
                }
                else
                {
                    ASLogger.Instance.Debug($"房间 {roomId} 收到玩家 {playerId} 空输入 (帧:{input.Frame})", "FrameSync.Input");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"记录接收输入详情时出错: {ex.Message}", "FrameSync.Input");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 记录发送给单个玩家的帧数据详情
        /// </summary>
        private void LogPlayerFrameDataSent(string roomId, string playerId, int authorityFrame, OneFrameInputs frameInputs)
        {
            try
            {
                if (frameInputs?.Inputs == null || frameInputs.Inputs.Count == 0)
                {
                    ASLogger.Instance.Debug($"发送给玩家 {playerId} 的帧 {authorityFrame} 数据为空", "FrameSync.Send");
                    return;
                }

                // 检查该玩家是否有输入数据（playerId 可能是非数字的用户ID，不能直接 Parse）
                LSInput? playerInput = null;
                var hasPlayerInput = false;
                if (long.TryParse(playerId, out var numericPlayerId))
                {
                    hasPlayerInput = frameInputs.Inputs.TryGetValue(numericPlayerId, out playerInput);
                }
                
                if (hasPlayerInput && playerInput != null)
                {
                    var inputDetails = new List<string>();
                    
                    if (playerInput.MoveX != 0 || playerInput.MoveY != 0)
                    {
                        inputDetails.Add($"移动:({playerInput.MoveX:F2},{playerInput.MoveY:F2})");
                    }
                    
                    if (playerInput.BornInfo != 0)
                    {
                        inputDetails.Add($"出生信息:{playerInput.BornInfo}");
                    }
                    
                    if (playerInput.Attack)
                    {
                        inputDetails.Add("攻击");
                    }
                    
                    if (playerInput.Skill1 || playerInput.Skill2)
                    {
                        var skills = new List<string>();
                        if (playerInput.Skill1) skills.Add("技能1");
                        if (playerInput.Skill2) skills.Add("技能2");
                        inputDetails.Add($"技能:[{string.Join(",", skills)}]");
                    }
                    
                    if (inputDetails.Count > 0)
                    {
                        ASLogger.Instance.Info($"发送给玩家 {playerId} 的帧 {authorityFrame} 数据: {string.Join(", ", inputDetails)}", "FrameSync.Send");
                    }
                    else
                    {
                        ASLogger.Instance.Debug($"发送给玩家 {playerId} 的帧 {authorityFrame} 数据为空输入", "FrameSync.Send");
                    }
                }
                else
                {
                    // 可能用户ID是非数字（如 user_***），或该玩家在本帧无输入
                    ASLogger.Instance.Debug($"发送给玩家 {playerId} 的帧 {authorityFrame} 数据: 无该玩家输入或用户ID非数字", "FrameSync.Send");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"记录玩家帧数据发送详情时出错: {ex.Message}", "FrameSync.Send");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 详细记录帧同步数据内容
        /// </summary>
        private void LogFrameSyncDataDetails(string roomId, int authorityFrame, OneFrameInputs frameInputs)
        {
            try
            {
                if (frameInputs?.Inputs == null || frameInputs.Inputs.Count == 0)
                {
                    ASLogger.Instance.Debug($"房间 {roomId} 帧 {authorityFrame} - 无输入数据", "FrameSync.Data");
                    return;
                }

                //ASLogger.Instance.Info($"房间 {roomId} 帧 {authorityFrame} - 输入数据详情 (玩家数: {frameInputs.Inputs.Count})", "FrameSync.Data");
                
                foreach (var kvp in frameInputs.Inputs)
                {
                    var playerId = kvp.Key;
                    var input = kvp.Value;
                    
                    if (input == null)
                    {
                        ASLogger.Instance.Warning($"玩家 {playerId} 的输入数据为 null", "FrameSync.Data");
                        continue;
                    }
                    
                    // 检查是否有移动输入
                    bool hasMovement = input.MoveX != 0 || input.MoveY != 0;
                    
                    // 检查是否有出生信息
                    bool hasBornInfo = input.BornInfo != 0;
                    
                    // 检查是否有技能输入
                    bool hasSkills = input.Skill1 || input.Skill2;
                    
                    // 检查是否有攻击输入
                    bool hasAttack = input.Attack;
                    
                    // 构建输入详情字符串
                    var inputDetails = new List<string>();
                    
                    if (hasMovement)
                    {
                        inputDetails.Add($"移动:({input.MoveX:F2},{input.MoveY:F2})");
                    }
                    
                    if (hasBornInfo)
                    {
                        inputDetails.Add($"出生信息:{input.BornInfo}");
                    }
                    
                    if (hasAttack)
                    {
                        inputDetails.Add("攻击");
                    }
                    
                    if (hasSkills)
                    {
                        var skills = new List<string>();
                        if (input.Skill1) skills.Add("技能1");
                        if (input.Skill2) skills.Add("技能2");
                        inputDetails.Add($"技能:[{string.Join(",", skills)}]");
                    }
                    
                    // 记录输入详情
                    if (inputDetails.Count > 0)
                    {
                        ASLogger.Instance.Info($"  玩家 {playerId} (帧:{input.Frame}): {string.Join(", ", inputDetails)}", "FrameSync.Data");
                    }
                    else
                    {
                        ASLogger.Instance.Debug($"  玩家 {playerId} (帧:{input.Frame}): 无有效输入", "FrameSync.Data");
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"记录帧同步数据详情时出错: {ex.Message}", "FrameSync.Data");
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
        
        // 帧数据缓存配置
        private const int MAX_CACHE_FRAMES = 300; // 缓存15秒的数据(20FPS)
        private const int CACHE_CLEANUP_INTERVAL = 60; // 每60帧清理一次缓存
        private int _lastCleanupFrame = 0;
        
        /// <summary>
        /// 存储帧输入数据
        /// </summary>
        public void StoreFrameInput(LSInput input)
        {
            // 如果输入帧号已经过了，使用服务器的当前帧号
            if (input.Frame < AuthorityFrame + 1)
            {
                ASLogger.Instance.Debug($"输入帧号 {input.Frame} 已过期，使用服务器当前帧号 {AuthorityFrame + 1}，玩家: {input.PlayerId}");
                input.Frame = AuthorityFrame + 1;
            }
            
            // 如果输入帧号比服务器当前帧晚太多，限制在合理范围内
            if (input.Frame > AuthorityFrame + MAX_CACHE_FRAMES)
            {
                ASLogger.Instance.Warning($"输入帧号 {input.Frame} 过于超前，限制为 {AuthorityFrame + MAX_CACHE_FRAMES}，玩家: {input.PlayerId}");
                input.Frame = AuthorityFrame + MAX_CACHE_FRAMES;
            }
            
            if (!_frameInputs.ContainsKey(input.Frame))
            {
                _frameInputs[input.Frame] = new Dictionary<long, LSInput>();
            }
            
            _frameInputs[input.Frame][input.PlayerId] = input;
            ASLogger.Instance.Debug($"存储玩家 {input.PlayerId} 的输入数据，帧号: {input.Frame}");
            
            // 定期清理过期缓存
            CleanupExpiredCache();
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
            ASLogger.Instance.Debug($"收集帧 {frame} 的输入数据，玩家数: {frameInputs.Inputs.Count}");
            
            return frameInputs;
        }
        
        /// <summary>
        /// 清理过期的帧数据缓存
        /// </summary>
        private void CleanupExpiredCache()
        {
            // 每60帧清理一次，避免频繁清理
            if (AuthorityFrame - _lastCleanupFrame < CACHE_CLEANUP_INTERVAL)
            {
                return;
            }
            
            _lastCleanupFrame = AuthorityFrame;
            
            var framesToRemove = new List<int>();
            var cutoffFrame = AuthorityFrame - MAX_CACHE_FRAMES;
            
            foreach (var frame in _frameInputs.Keys)
            {
                if (frame < cutoffFrame)
                {
                    framesToRemove.Add(frame);
                }
            }
            
            foreach (var frame in framesToRemove)
            {
                _frameInputs.Remove(frame);
            }
            
            if (framesToRemove.Count > 0)
            {
                ASLogger.Instance.Debug($"清理了 {framesToRemove.Count} 个过期帧缓存，当前缓存帧数: {_frameInputs.Count}");
            }
        }
        
        /// <summary>
        /// 获取当前缓存的帧数
        /// </summary>
        public int GetCacheFrameCount()
        {
            return _frameInputs.Count;
        }
    }
}
