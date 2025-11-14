using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.Generated;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using AstrumServer.Network;

namespace AstrumServer.Managers
{
    /// <summary>
    /// 游戏会话 - 管理单个房间的游戏逻辑和帧同步
    /// </summary>
    public class GameSession
    {
        private readonly ServerRoom _room;
        private readonly ServerNetworkManager _networkManager;
        private readonly UserManager _userManager;
        
        // 帧同步配置
        private const int FRAME_RATE = 20; // 20FPS
        private const int FRAME_INTERVAL_MS = 1000 / FRAME_RATE; // 50ms
        private const int MAX_ADVANCE_PER_UPDATE = 5; // 单次Update最多补帧数，防止雪崩
        
        // 帧同步状态
        public int AuthorityFrame { get; private set; } = 0;
        public bool IsActive { get; private set; } = false;
        public long StartTime { get; private set; } = 0;
        
        // 逻辑房间（LogicCore 的 Room 类）
        public Astrum.LogicCore.Core.Room? LogicRoom { get; private set; }
        
        // UserId -> PlayerId 映射（实体创建后确定）
        public Dictionary<string, long> UserIdToPlayerId { get; private set; } = new();
        
        // 玩家ID列表
        public List<string> PlayerIds { get; private set; } = new();
        
        // 帧输入缓冲区 (帧号 -> 输入数据)
        private readonly Dictionary<int, Dictionary<long, LSInput>> _frameInputs = new();
        
        // 历史上曾经上报过输入的玩家ID集合（仅记录非零ID）
        private readonly HashSet<long> _uploadedPlayerIds = new();
        
        // 帧数据缓存配置
        private const int MAX_CACHE_FRAMES = 300; // 缓存15秒的数据(20FPS)
        private const int CACHE_CLEANUP_INTERVAL = 60; // 每60帧清理一次缓存
        private int _lastCleanupFrame = 0;
        
        public GameSession(ServerRoom room, ServerNetworkManager networkManager, UserManager userManager)
        {
            _room = room;
            _networkManager = networkManager;
            _userManager = userManager;
        }
        
        /// <summary>
        /// 开始帧同步
        /// </summary>
        public void Start()
        {
            if (IsActive)
            {
                // 重连：发送当前帧快照
                HandleReconnect();
                return;
            }
            
            try
            {
                var roomInfo = _room.Info;
                if (roomInfo.Status != 1) // 1=游戏中
                {
                    ASLogger.Instance.Warning($"房间不在游戏中，无法开始帧同步: {roomInfo.Id} (Status: {roomInfo.Status})", "FrameSync.Controller");
                    return;
                }
                
                // 初始化帧同步状态
                AuthorityFrame = 0;
                IsActive = true;
                StartTime = TimeInfo.Instance.ClientNow();
                PlayerIds = new List<string>(roomInfo.PlayerNames);
                UserIdToPlayerId = new Dictionary<string, long>();
                
                // 初始化逻辑环境（通过 FrameSyncManager 的静态方法）
                // 注意：这个方法需要从 FrameSyncManager 中提取为静态方法
                // 暂时先调用，后续会重构
                EnsureLogicEnvironmentInitialized();
                
                // 创建逻辑房间
                LogicRoom = CreateLogicRoom(roomInfo.Id, roomInfo, StartTime);
                
                // 创建所有玩家实体（按 UserId 顺序，确保 UniqueId 一致）
                foreach (var userId in roomInfo.PlayerNames.OrderBy(x => x))
                {
                    var playerId = LogicRoom.AddPlayer(); // 创建玩家实体，返回 UniqueId
                    if (playerId > 0)
                    {
                        // 记录 PlayerId 映射
                        UserIdToPlayerId[userId] = playerId;
                        ASLogger.Instance.Info($"服务器：创建玩家实体 - UserId: {userId}, PlayerId: {playerId}", "FrameSync.Controller");
                    }
                    else
                    {
                        ASLogger.Instance.Warning($"服务器：创建玩家实体失败 - UserId: {userId}", "FrameSync.Controller");
                    }
                }
                
                // 启动帧同步控制器
                LogicRoom?.LSController?.Start();
                
                // 保存第0帧快照
                if (LogicRoom?.LSController is ServerLSController serverSync)
                {
                    serverSync.AuthorityFrame = 0;
                    serverSync.FrameBuffer.MoveForward(0);
                    serverSync.SaveState();
                    
                    // 获取快照数据
                    var snapshotBuffer = serverSync.FrameBuffer.Snapshot(0);
                    byte[] worldSnapshotData = new byte[snapshotBuffer.Length];
                    snapshotBuffer.Read(worldSnapshotData, 0, (int)snapshotBuffer.Length);
                    
                    // 检查快照大小
                    if (worldSnapshotData.Length > 1024 * 1024) // 1MB
                    {
                        ASLogger.Instance.Warning($"房间 {roomInfo.Id} 快照数据过大: {worldSnapshotData.Length} bytes", "FrameSync.Controller");
                    }
                    
                    // 发送帧同步开始通知（包含世界快照和 PlayerId 映射）
                    SendFrameSyncStartNotification(worldSnapshotData);
                    
                    ASLogger.Instance.Info($"房间 {roomInfo.Id} 开始帧同步，玩家数: {PlayerIds.Count}，快照大小: {worldSnapshotData.Length} bytes", "FrameSync.Controller");
                }
                else if (LogicRoom?.LSController != null)
                {
                    // 兼容旧代码：使用基础接口
                    LogicRoom.LSController.AuthorityFrame = 0;
                    LogicRoom.LSController.FrameBuffer.MoveForward(0);
                    LogicRoom.LSController.SaveState();
                    
                    var snapshotBuffer = LogicRoom.LSController.FrameBuffer.Snapshot(0);
                    byte[] worldSnapshotData = new byte[snapshotBuffer.Length];
                    snapshotBuffer.Read(worldSnapshotData, 0, (int)snapshotBuffer.Length);
                    
                    SendFrameSyncStartNotification(worldSnapshotData);
                    ASLogger.Instance.Info($"房间 {roomInfo.Id} 开始帧同步，玩家数: {PlayerIds.Count}，快照大小: {worldSnapshotData.Length} bytes", "FrameSync.Controller");
                }
                else
                {
                    ASLogger.Instance.Error($"房间 {roomInfo.Id} LSController 为空，无法保存快照", "FrameSync.Controller");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"开始房间帧同步时出错: {ex.Message}", "FrameSync.Controller");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 停止帧同步
        /// </summary>
        public void Stop(string reason = "游戏结束")
        {
            if (!IsActive) return;
            
            try
            {
                IsActive = false;
                
                LogicRoom?.Shutdown();
                
                // 发送帧同步结束通知
                SendFrameSyncEndNotification(reason);
                
                ASLogger.Instance.Info($"房间 {_room.Info.Id} 停止帧同步，最终帧: {AuthorityFrame}，原因: {reason}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"停止房间帧同步时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 更新帧同步（由 Room.Update 调用）
        /// </summary>
        public void Update()
        {
            if (!IsActive) return;
            
            try
            {                var now = TimeInfo.Instance.ClientNow();
                             
                             // 目标应到达的帧 = floor((now - StartTime) / interval)
                             var elapsed = now - StartTime;
                             if (elapsed < 0) return;
                             var expectedFrame = (int)(elapsed / FRAME_INTERVAL_MS);
                             
                             // 将 AuthorityFrame 补到 expectedFrame，单次最多推进若干帧
                             var steps = 0;
                             while (AuthorityFrame < expectedFrame && steps < MAX_ADVANCE_PER_UPDATE)
                             {
                                 ProcessFrame();
                                 steps++;
                             }

            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"帧同步Update推进时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理玩家输入
        /// </summary>
        public void HandleInput(string userId, SingleInput singleInput)
        {
            if (!IsActive)
            {
                ASLogger.Instance.Warning($"房间 {_room.Info.Id} 帧同步未激活，忽略单帧输入数据");
                return;
            }
            
            try
            {
                // 将SingleInput转换为LSInput
                var lsInput = singleInput.Input;
                
                // 记录玩家ID映射过程
                var originalPlayerId = lsInput.PlayerId;
                var singleInputPlayerId = singleInput.PlayerID;
                
                // 如果 UserIdToPlayerId 中有映射，使用映射的 PlayerId
                if (UserIdToPlayerId.TryGetValue(userId, out var mappedPlayerId))
                {
                    lsInput.PlayerId = mappedPlayerId;
                }
                else if (singleInputPlayerId != 0)
                {
                    lsInput.PlayerId = singleInputPlayerId;
                }
                else
                {
                    ASLogger.Instance.Warning($"房间 {_room.Info.Id} 玩家 {userId} 的 PlayerId 映射不存在，使用原始值: {originalPlayerId}");
                }
                
                // 保持客户端原始帧号，让StoreFrameInput方法处理帧号验证
                lsInput.Frame = singleInput.FrameID;
                
                // 详细记录接收到的输入数据
                LogReceivedInputDetails(userId, lsInput.PlayerId.ToString(), lsInput, AuthorityFrame);
                
                // 存储输入数据到 ServerLSController
                if (LogicRoom?.LSController is ServerLSController serverSync)
                {
                    // 使用 ServerLSController 的输入缓存
                    serverSync.AddPlayerInput(lsInput.Frame, lsInput.PlayerId, lsInput);
                }
                else
                {
                    // 兼容旧代码
                    StoreFrameInput(lsInput);
                }
                
                ASLogger.Instance.Debug($"收到玩家 {lsInput.PlayerId} 的单帧输入，房间: {_room.Info.Id}，客户端帧: {singleInput.FrameID}，服务器帧: {AuthorityFrame}，最终存储帧: {lsInput.Frame}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理单帧输入时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理房间的当前帧
        /// </summary>
        private void ProcessFrame()
        {
            try
            {
                // 检查房间是否还有玩家
                if (!CheckRoomHasPlayers())
                {
                    // 房间没有玩家了，停止帧同步
                    Stop("房间内没有玩家");
                    return;
                }
                
                // 推进逻辑世界（通过 ServerLSController.Tick()）
                if (LogicRoom?.LSController is ServerLSController serverSync)
                {
                    // 服务器使用 Tick() 方法推进权威帧（内部会推进 AuthorityFrame、收集输入、执行逻辑）
                    serverSync.Tick();
                    
                    // 同步 GameSession 的 AuthorityFrame（用于兼容性）
                    AuthorityFrame = serverSync.AuthorityFrame;
                    
                    // 收集当前帧的输入数据用于广播
                    var frameInputs = serverSync.CollectFrameInputs(serverSync.AuthorityFrame);
                    
                    // 发送帧同步数据给房间内所有玩家
                    SendFrameSyncData(serverSync.AuthorityFrame, frameInputs);
                    
                    ASLogger.Instance.Debug($"处理房间 {_room.Info.Id} 帧 {serverSync.AuthorityFrame}，输入数: {frameInputs.Inputs.Count}", "FrameSync.Processing");
                }
                else if (LogicRoom != null)
                {
                    // 兼容旧代码：如果没有 ServerLSController，使用旧方式
                    AuthorityFrame++;
                    
                    var frameInputs = CollectFrameInputs(AuthorityFrame);
                    
                    var controller = LogicRoom.LSController;
                    if (controller != null)
                    {
                        controller.AuthorityFrame = AuthorityFrame;
                    }
                    
                    LogicRoom.FrameTick(frameInputs);
                    
                    // 发送帧同步数据给房间内所有玩家
                    SendFrameSyncData(AuthorityFrame, frameInputs);
                    
                    ASLogger.Instance.Debug($"处理房间 {_room.Info.Id} 帧 {AuthorityFrame}，输入数: {frameInputs.Inputs.Count}，缓存总帧数: {GetCacheFrameCount()}", "FrameSync.Processing");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理房间帧时出错: {ex.Message}", "FrameSync.Processing");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 存储帧输入数据
        /// </summary>
        private void StoreFrameInput(LSInput input)
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
        private OneFrameInputs CollectFrameInputs(int frame)
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
                        // 为本帧未上报的历史玩家使用上一帧的输入
                        var previousFrameInput = GetPreviousFrameInput(playerId, frame);
                        frameInputs.Inputs[playerId] = previousFrameInput;
                        ASLogger.Instance.Debug($"玩家 {playerId} 在帧 {frame} 未上报，使用上一帧输入");
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
        /// 获取玩家上一帧的输入，如果找不到则返回默认空输入
        /// </summary>
        private LSInput GetPreviousFrameInput(long playerId, int currentFrame)
        {
            // 从当前帧往前查找，最多查找10帧
            for (int frameOffset = 1; frameOffset <= 10; frameOffset++)
            {
                int previousFrame = currentFrame - frameOffset;
                if (previousFrame < 0) break;
                
                if (_frameInputs.TryGetValue(previousFrame, out var previousInputs) &&
                    previousInputs.TryGetValue(playerId, out var previousInput))
                {
                    // 找到上一帧的输入，复制并更新帧号和时间戳
                    var input = LSInput.Create();
                    input.PlayerId = playerId;
                    input.Frame = currentFrame;
                    input.MoveX = previousInput.MoveX;
                    input.MoveY = previousInput.MoveY;
                    input.Attack = previousInput.Attack;
                    input.Skill1 = previousInput.Skill1;
                    input.Skill2 = previousInput.Skill2;
                    input.BornInfo = previousInput.BornInfo;
                    input.Timestamp = TimeInfo.Instance.ClientNow();
                    
                    ASLogger.Instance.Debug($"玩家 {playerId} 在帧 {currentFrame} 使用帧 {previousFrame} 的输入");
                    return input;
                }
            }
            
            // 如果找不到任何历史输入，返回默认空输入
            ASLogger.Instance.Debug($"玩家 {playerId} 在帧 {currentFrame} 找不到历史输入，使用默认空输入");
            return CreateDefaultInput(playerId, currentFrame);
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
        
        /// <summary>
        /// 检查房间是否还有玩家
        /// </summary>
        private bool CheckRoomHasPlayers()
        {
            try
            {
                var roomInfo = _room.Info;
                
                // 检查房间是否还有玩家
                if (roomInfo.CurrentPlayers == 0)
                {
                    ASLogger.Instance.Debug($"房间 {roomInfo.Id} 没有玩家，停止帧同步");
                    return false;
                }
                
                // 检查是否有玩家在线
                var onlinePlayerCount = 0;
                foreach (var playerId in PlayerIds)
                {
                    var sessionId = _userManager.GetSessionIdByUserId(playerId);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        onlinePlayerCount++;
                    }
                }
                
                if (onlinePlayerCount == 0)
                {
                    ASLogger.Instance.Debug($"房间 {roomInfo.Id} 没有在线玩家，停止帧同步");
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
        /// 创建逻辑房间
        /// </summary>
        private Astrum.LogicCore.Core.Room CreateLogicRoom(string roomId, RoomInfo roomInfo, long startTime)
        {
            var logicRoom = new Astrum.LogicCore.Core.Room();
            
            if (!int.TryParse(roomId, out var numericRoomId))
            {
                numericRoomId = Math.Abs(roomId.GetHashCode());
                if (numericRoomId == int.MinValue)
                {
                    numericRoomId = Math.Abs((roomId + "_logic").GetHashCode());
                }
            }
            
            logicRoom.RoomId = numericRoomId;
            logicRoom.Name = string.IsNullOrWhiteSpace(roomInfo.Name) ? roomId : roomInfo.Name;
            
            var world = new Astrum.LogicCore.Core.World
            {
                WorldId = numericRoomId,
                Name = $"World_{roomId}",
                RoomId = numericRoomId
            };
            
            logicRoom.MainWorld = world;
            logicRoom.Initialize("server"); // 指定使用 ServerLSController
            logicRoom.SetServerCreationTime(startTime);
            
            return logicRoom;
        }
        
        /// <summary>
        /// 确保逻辑环境已初始化（调用 FrameSyncManager 的静态方法）
        /// </summary>
        private void EnsureLogicEnvironmentInitialized()
        {
            FrameSyncManager.EnsureLogicEnvironmentInitializedStatic();
        }
        
        /// <summary>
        /// 处理重连
        /// </summary>
        private void HandleReconnect()
        {
            try
            {
                var currentFrame = AuthorityFrame;
                
                // 确保 FrameBuffer 已经准备好当前帧
                if (LogicRoom?.LSController is ServerLSController serverSync)
                {
                    serverSync.FrameBuffer.MoveForward(currentFrame);
                    serverSync.SaveState();
                    
                    var snapshotBuffer = serverSync.FrameBuffer.Snapshot(currentFrame);
                    if (snapshotBuffer != null && snapshotBuffer.Length > 0)
                    {
                        byte[] worldSnapshotData = new byte[snapshotBuffer.Length];
                        snapshotBuffer.Read(worldSnapshotData, 0, (int)snapshotBuffer.Length);
                        
                        // 发送包含当前帧快照的通知
                        SendFrameSyncStartNotification(worldSnapshotData);
                        ASLogger.Instance.Info($"房间 {_room.Info.Id} 重连，发送当前帧快照（帧: {currentFrame}）", "FrameSync.Controller");
                    }
                    else
                    {
                        ASLogger.Instance.Warning($"房间 {_room.Info.Id} 重连，但快照数据为空", "FrameSync.Controller");
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理重连时出错: {ex.Message}", "FrameSync.Controller");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 发送帧同步开始通知
        /// </summary>
        private void SendFrameSyncStartNotification(byte[] worldSnapshotData)
        {
            try
            {
                var notification = FrameSyncStartNotification.Create();
                notification.roomId = _room.Info.Id;
                notification.frameRate = FRAME_RATE;
                notification.frameInterval = FRAME_INTERVAL_MS;
                notification.startTime = StartTime;
                notification.playerIds = new List<string>(PlayerIds);
                notification.worldSnapshot = worldSnapshotData;
                notification.playerIdMapping = new Dictionary<string, long>(UserIdToPlayerId);
                
                ASLogger.Instance.Info($"准备发送帧同步开始通知，包含 {notification.playerIdMapping.Count} 个玩家的 PlayerId 映射，快照大小: {worldSnapshotData.Length} bytes", "FrameSync.Controller");
                
                // 发送给房间内所有玩家
                foreach (var userId in PlayerIds)
                {
                    var sessionId = _userManager.GetSessionIdByUserId(userId);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _networkManager.SendMessage(sessionId, notification);
                        if (UserIdToPlayerId.TryGetValue(userId, out var playerId))
                        {
                            ASLogger.Instance.Debug($"已发送帧同步开始通知给玩家 - UserId: {userId}, PlayerId: {playerId}", "FrameSync.Controller");
                        }
                    }
                }
                
                ASLogger.Instance.Info($"已发送帧同步开始通知给房间 {_room.Info.Id} 的所有玩家（共 {PlayerIds.Count} 个）", "FrameSync.Controller");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"发送帧同步开始通知时出错: {ex.Message}", "FrameSync.Controller");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 发送帧同步结束通知
        /// </summary>
        private void SendFrameSyncEndNotification(string reason)
        {
            try
            {
                var notification = FrameSyncEndNotification.Create();
                notification.roomId = _room.Info.Id;
                notification.finalFrame = AuthorityFrame;
                notification.endTime = TimeInfo.Instance.ClientNow();
                notification.reason = reason;
                
                // 发送给房间内所有玩家
                foreach (var playerId in PlayerIds)
                {
                    var sessionId = _userManager.GetSessionIdByUserId(playerId);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _networkManager.SendMessage(sessionId, notification);
                    }
                }
                
                ASLogger.Instance.Info($"已发送帧同步结束通知给房间 {_room.Info.Id} 的所有玩家");
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
        private void SendFrameSyncData(int authorityFrame, OneFrameInputs frameInputs)
        {
            try
            {
                var frameData = FrameSyncData.Create();
                frameData.roomId = _room.Info.Id;
                frameData.authorityFrame = authorityFrame;
                frameData.frameInputs = frameInputs;
                frameData.timestamp = TimeInfo.Instance.ClientNow();
                
                ASLogger.Instance.Debug($"发送帧同步数据 - 房间: {_room.Info.Id}, 帧: {authorityFrame}, 输入数: {frameInputs.Inputs.Count}, 时间戳: {frameData.timestamp}", "FrameSync.Send");
                
                // 发送给房间内所有玩家
                int successCount = 0;
                int failCount = 0;
                
                foreach (var playerId in PlayerIds)
                {
                    var sessionId = _userManager.GetSessionIdByUserId(playerId);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        try
                        {
                            _networkManager.SendMessage(sessionId, frameData);
                            successCount++;
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
            }
        }
    }
}

