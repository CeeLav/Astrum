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
        
        // 运行状态（弃用定时器，改为在Update中推进）
        private bool _isRunning = false;
        private const int MAX_ADVANCE_PER_UPDATE = 5; // 单次Update最多补帧数，防止雪崩

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
            
            ASLogger.Instance.Info($"帧同步管理器已启动，帧率: {FRAME_RATE}FPS，间隔: {FRAME_INTERVAL_MS}ms (基于Update推进)", "FrameSync.Manager");
        }
        
        /// <summary>
        /// 停止帧同步
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;
            
            _isRunning = false;
            
            // 清理所有房间的帧同步状态
            _roomFrameStates.Clear();
            
            ASLogger.Instance.Info("帧同步管理器已停止", "FrameSync.Manager");
        }

        /// <summary>
        /// 基于当前时间推进所有活跃房间的帧（在服务器主循环中调用）
        /// </summary>
        public void Update()
        {
            if (!_isRunning) return;
            try
            {
                var now = TimeInfo.Instance.ClientNow();
                foreach (var kvp in _roomFrameStates)
                {
                    var roomId = kvp.Key;
                    var state = kvp.Value;
                    if (!state.IsActive) continue;

                    // 目标应到达的帧 = floor((now - StartTime) / interval)
                    var elapsed = now - state.StartTime;
                    if (elapsed < 0) continue;
                    var expectedFrame = (int)(elapsed / FRAME_INTERVAL_MS);

                    // 将 AuthorityFrame 补到 expectedFrame，单次最多推进若干帧
                    var steps = 0;
                    while (state.AuthorityFrame < expectedFrame && steps < MAX_ADVANCE_PER_UPDATE)
                    {
                        ProcessRoomFrame(roomId, state);
                        steps++;
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"帧同步Update推进时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
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
                
                // 记录玩家ID映射过程
                var originalPlayerId = lsInput.PlayerId;
                var singleInputPlayerId = singleInput.PlayerID;
                lsInput.PlayerId = singleInput.PlayerID != 0 ? singleInput.PlayerID : ++currentPlayerIdMap[roomId];
                
                ASLogger.Instance.Debug($"玩家ID映射 - 房间: {roomId}, SingleInput.PlayerID: {singleInputPlayerId}, Input.PlayerId: {originalPlayerId}, 最终PlayerId: {lsInput.PlayerId}");
                
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
        
        // 定时器模式已废弃，保留占位以兼容旧代码（不再使用）
        
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
                
                // 计算目标帧并推进一帧（由Update循环控制推进次数）
                frameState.AuthorityFrame++;
                
                // 收集当前帧的所有输入数据（从缓存中获取）
                var frameInputs = frameState.CollectFrameInputs(frameState.AuthorityFrame);
                
                // 记录实际收集到的玩家数量
                var actualPlayerCount = frameInputs.Inputs.Count;
                ASLogger.Instance.Debug($"处理房间 {roomId} 帧 {frameState.AuthorityFrame}，实际收到输入玩家数: {actualPlayerCount}");
                
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
                // 兼容客户端Room/LSController的CreationTime对齐
                // 使用服务端帧开始时间作为CreationTime基准
                // 若有更精细定义，可替换为真实Room创建时间
                // 此处沿用frameState.StartTime以减少歧义
                // 客户端将据此设置 Room.CreationTime/LSController.CreationTime
                
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
        
        // 历史上曾经上报过输入的玩家ID集合（仅记录非零ID）
        private readonly HashSet<long> _uploadedPlayerIds = new();
        
        // 帧数据缓存配置
        private const int MAX_CACHE_FRAMES = 300; // 缓存15秒的数据(20FPS)
        private const int CACHE_CLEANUP_INTERVAL = 60; // 每60帧清理一次缓存
        private int _lastCleanupFrame = 0;
        
        /// <summary>
        /// 存储帧输入数据
        /// </summary>
        public void StoreFrameInput(LSInput input)
        {
            // 登记曾经上报过的非零玩家ID
            if (input.PlayerId != 0)
            {
                _uploadedPlayerIds.Add(input.PlayerId);
            }
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
            
            // 记录存储后的帧数据状态
            var framePlayerCount = _frameInputs[input.Frame].Count;
            var framePlayerIds = string.Join(", ", _frameInputs[input.Frame].Keys.OrderBy(x => x));
            ASLogger.Instance.Debug($"存储玩家 {input.PlayerId} 的输入数据，帧号: {input.Frame}，该帧当前玩家数: {framePlayerCount}，玩家ID: [{framePlayerIds}]");
            
            // 定期清理过期缓存
            CleanupExpiredCache();
        }
        
        /// <summary>
        /// 收集指定帧的所有输入数据
        /// </summary>
        public OneFrameInputs CollectFrameInputs(int frame)
        {
            var frameInputs = OneFrameInputs.Create();
            
            // 优先依据历史上报过的玩家ID进行下发（保证所有曾经上报过的玩家都有条目）
            if (_uploadedPlayerIds.Count > 0)
            {
                var hadInputs = _frameInputs.TryGetValue(frame, out var inputsThisFrame) ? inputsThisFrame : null;
                foreach (var playerId in _uploadedPlayerIds.OrderBy(x => x))
                {
                    if (playerId == 0) continue; // 保险过滤
                    if (hadInputs != null && hadInputs.TryGetValue(playerId, out var actual))
                    {
                        frameInputs.Inputs[playerId] = actual;
                    }
                    else
                    {
                        // 为本帧未上报的历史玩家填充默认空输入
                        frameInputs.Inputs[playerId] = CreateDefaultInput(playerId, frame);
                        ASLogger.Instance.Debug($"玩家 {playerId} 在帧 {frame} 未上报，填充默认空输入");
                    }
                }
            }
            else
            {
                // 如果还没有历史上报ID，则仅收集本帧实际收到的有效输入（排除0）
                if (_frameInputs.TryGetValue(frame, out var inputs))
                {
                    foreach (var kvp in inputs)
                    {
                        var playerId = kvp.Key;
                        var input = kvp.Value;
                        if (playerId != 0)
                        {
                            frameInputs.Inputs[playerId] = input;
                        }
                    }
                }
            }
            
            // 详细记录收集到的玩家ID
            var collectedPlayerIds = string.Join(", ", frameInputs.Inputs.Keys.OrderBy(x => x));
            ASLogger.Instance.Debug($"收集帧 {frame} 的输入数据，玩家数: {frameInputs.Inputs.Count}，玩家ID: [{collectedPlayerIds}]");
            
            return frameInputs;
        }
        
        /// <summary>
        /// 创建默认的空输入
        /// </summary>
        private LSInput CreateDefaultInput(long playerId, int frame)
        {
            var defaultInput = LSInput.Create();
            defaultInput.PlayerId = playerId;
            defaultInput.Frame = frame;
            defaultInput.MoveX = 0;
            defaultInput.MoveY = 0;
            defaultInput.Attack = false;
            defaultInput.Skill1 = false;
            defaultInput.Skill2 = false;
            defaultInput.BornInfo = 0;
            defaultInput.Timestamp = TimeInfo.Instance.ClientNow();
            return defaultInput;
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
