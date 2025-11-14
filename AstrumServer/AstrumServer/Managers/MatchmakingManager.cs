using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.Generated;
using Astrum.CommonBase;
using AstrumServer.Network;

namespace AstrumServer.Managers
{
    /// <summary>
    /// 匹配管理器 - 负责快速匹配队列和自动匹配逻辑
    /// </summary>
    public class MatchmakingManager
    {
        /// <summary>
        /// 匹配队列项
        /// </summary>
        private class MatchmakingEntry
        {
            public string UserId { get; set; }
            public string DisplayName { get; set; }
            public long EnterQueueTime { get; set; }
            public long TimeoutTime { get; set; }
        }

        // 匹配配置
        private const long MATCH_TIMEOUT_MS = 60000;     // 1分钟超时
        private const int MIN_MATCH_PLAYERS = 2;         // 最小匹配人数
        private const int QUICK_MATCH_MAX_PLAYERS = 4;   // 快速匹配房间最大人数
        
        // 匹配队列（FIFO）
        private readonly Queue<MatchmakingEntry> _matchQueue = new();
        
        // 用户ID到队列项的映射（用于快速查找和移除）
        private readonly Dictionary<string, MatchmakingEntry> _userEntries = new();
        
        // 依赖的管理器
        private readonly RoomManager _roomManager;
        private readonly UserManager _userManager;
        private readonly IServerNetworkManager _networkManager;
        // 上次检查超时的时间
        private long _lastTimeoutCheckTime = 0;
        private const long TIMEOUT_CHECK_INTERVAL_MS = 5000; // 5秒检查一次超时
        
        public MatchmakingManager(RoomManager roomManager, UserManager userManager, IServerNetworkManager networkManager)
        {
            _roomManager = roomManager;
            _userManager = userManager;
            _networkManager = networkManager;
            _lastTimeoutCheckTime = TimeInfo.Instance.ClientNow();
        }

        /// <summary>
        /// 加入匹配队列
        /// </summary>
        public bool EnqueuePlayer(string userId, string displayName)
        {
            try
            {
                // 检查用户是否已在队列中
                if (_userEntries.ContainsKey(userId))
                {
                    ASLogger.Instance.Warning($"MatchmakingManager: 用户已在匹配队列中: {userId}");
                    return false;
                }
                
                // 检查用户是否已在房间中
                var existingRoomId = _roomManager.GetUserRoomId(userId);
                if (!string.IsNullOrEmpty(existingRoomId))
                {
                    ASLogger.Instance.Warning($"MatchmakingManager: 用户已在房间中，无法加入匹配队列: {userId} in room {existingRoomId}");
                    return false;
                }
                
                // 创建队列项
                var now = TimeInfo.Instance.ClientNow();
                var entry = new MatchmakingEntry
                {
                    UserId = userId,
                    DisplayName = displayName,
                    EnterQueueTime = now,
                    TimeoutTime = now + MATCH_TIMEOUT_MS
                };
                
                // 加入队列
                _matchQueue.Enqueue(entry);
                _userEntries[userId] = entry;
                
                ASLogger.Instance.Info($"MatchmakingManager: 用户加入匹配队列: {userId} ({displayName}), 队列人数: {_matchQueue.Count}");
                
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MatchmakingManager: 加入匹配队列时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 离开匹配队列
        /// </summary>
        public bool DequeuePlayer(string userId)
        {
            try
            {
                if (!_userEntries.ContainsKey(userId))
                {
                    ASLogger.Instance.Warning($"MatchmakingManager: 用户不在匹配队列中: {userId}");
                    return false;
                }
                
                // 从映射中移除
                var entry = _userEntries[userId];
                _userEntries.Remove(userId);
                
                // 从队列中移除（需要重建队列）
                var tempQueue = new Queue<MatchmakingEntry>();
                while (_matchQueue.Count > 0)
                {
                    var item = _matchQueue.Dequeue();
                    if (item.UserId != userId)
                    {
                        tempQueue.Enqueue(item);
                    }
                }
                
                // 重新构建队列
                while (tempQueue.Count > 0)
                {
                    _matchQueue.Enqueue(tempQueue.Dequeue());
                }
                
                ASLogger.Instance.Info($"MatchmakingManager: 用户离开匹配队列: {userId}, 剩余队列人数: {_matchQueue.Count}");
                
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MatchmakingManager: 离开匹配队列时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 获取队列信息
        /// </summary>
        public (int position, int total) GetQueueInfo(string userId)
        {
            if (!_userEntries.ContainsKey(userId))
            {
                return (-1, _matchQueue.Count);
            }
            
            // 查找用户在队列中的位置
            int position = 0;
            foreach (var entry in _matchQueue)
            {
                if (entry.UserId == userId)
                {
                    return (position, _matchQueue.Count);
                }
                position++;
            }
            
            return (-1, _matchQueue.Count);
        }

        /// <summary>
        /// 定期更新 - 检查匹配和超时
        /// </summary>
        public void Update()
        {
            try
            {
                // 检查并执行匹配
                CheckAndMatchPlayers();
                
                // 检查超时
                var now = TimeInfo.Instance.ClientNow();
                if (now - _lastTimeoutCheckTime >= TIMEOUT_CHECK_INTERVAL_MS)
                {
                    CheckTimeouts();
                    _lastTimeoutCheckTime = now;
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MatchmakingManager: 更新时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 检查并执行匹配
        /// </summary>
        private void CheckAndMatchPlayers()
        {
            // 如果队列中有至少2个玩家，立即匹配
            while (_matchQueue.Count >= MIN_MATCH_PLAYERS)
            {
                try
                {
                    // 取出前2个玩家
                    var player1 = _matchQueue.Dequeue();
                    var player2 = _matchQueue.Dequeue();
                    
                    // 从映射中移除
                    _userEntries.Remove(player1.UserId);
                    _userEntries.Remove(player2.UserId);
                    
                    // 创建快速匹配房间
                    var players = new List<MatchmakingEntry> { player1, player2 };
                    CreateQuickMatchRoom(players);
                    
                    ASLogger.Instance.Info($"MatchmakingManager: 匹配成功！玩家: {player1.UserId}, {player2.UserId}");
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Error($"MatchmakingManager: 执行匹配时出错: {ex.Message}");
                    ASLogger.Instance.LogException(ex, LogLevel.Error);
                    break; // 出错时停止匹配
                }
            }
        }

        /// <summary>
        /// 创建快速匹配房间
        /// </summary>
        private void CreateQuickMatchRoom(List<MatchmakingEntry> players)
        {
            try
            {
                if (players.Count == 0)
                {
                    ASLogger.Instance.Warning("MatchmakingManager: 无法创建房间，玩家列表为空");
                    return;
                }
                
                // 生成房间名称
                var roomName = $"QuickMatch_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                var creatorId = players[0].UserId;
                
                // 创建房间
                var room = _roomManager.CreateRoom(creatorId, roomName, QUICK_MATCH_MAX_PLAYERS);
                
                // 将创建者之外的玩家加入房间
                for (int i = 1; i < players.Count; i++)
                {
                    var playerId = players[i].UserId;
                    if (!_roomManager.JoinRoom(room.Info.Id, playerId))
                    {
                        ASLogger.Instance.Warning($"MatchmakingManager: 玩家加入房间失败: {playerId} -> {room.Info.Id}");
                    }
                }
                
                // 更新所有玩家的房间信息
                foreach (var player in players)
                {
                    _userManager.UpdateUserRoom(player.UserId, room.Info.Id);
                }
                
                // 快速匹配直接开始游戏（不再发送中间通知）
                AutoStartQuickMatchGame(room, players);
                
                ASLogger.Instance.Info($"MatchmakingManager: 创建快速匹配房间成功并自动开始游戏: {room.Info.Id}, 玩家数: {players.Count}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MatchmakingManager: 创建快速匹配房间时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 通知匹配成功（已废弃：快速匹配直接发送 GameStartNotification）
        /// </summary>
        private void NotifyMatchFound(List<MatchmakingEntry> players, RoomInfo room)
        {
            // 快速匹配流程改为直接开始游戏，不再发送 MatchFoundNotification
            // 客户端将直接收到 GameStartNotification 并进入游戏
            ASLogger.Instance.Debug($"MatchmakingManager: 跳过发送 MatchFoundNotification，将在 AutoStartQuickMatchGame 中发送 GameStartNotification");
            
            // 旧逻辑已注释：
            /*
            try
            {
                var notification = MatchFoundNotification.Create();
                notification.Room = room;
                notification.Timestamp = TimeInfo.Instance.ClientNow();
                notification.PlayerIds = players.Select(p => p.UserId).ToList();
                
                // 发送通知给所有匹配玩家
                foreach (var player in players)
                {
                    var sessionId = _userManager.GetSessionIdByUserId(player.UserId);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _networkManager.SendMessage(sessionId, notification);
                        ASLogger.Instance.Debug($"MatchmakingManager: 发送匹配成功通知给: {player.UserId}");
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MatchmakingManager: 通知匹配成功时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
            */
        }

        /// <summary>
        /// 快速匹配自动开始游戏
        /// </summary>
        private void AutoStartQuickMatchGame(ServerRoom room, List<MatchmakingEntry> players)
        {
            try
            {
                var firstPlayer = players[0];
                ASLogger.Instance.Info($"MatchmakingManager: 快速匹配自动开始游戏 - 房间: {room.Info.Id}");
                
                // 1. 构建游戏配置（参考 GameServer.NotifyGameStart）
                var gameConfig = GameConfig.Create();
                gameConfig.maxPlayers = QUICK_MATCH_MAX_PLAYERS;
                gameConfig.minPlayers = MIN_MATCH_PLAYERS;
                gameConfig.roundTime = 300; // 5分钟
                gameConfig.maxRounds = 3;
                gameConfig.allowSpectators = true;
                gameConfig.gameModes = new List<string> { "快速匹配模式" };

                // 2. 构建游戏房间状态
                var roomState = GameRoomState.Create();
                roomState.roomId = room.Info.Id;
                roomState.currentRound = 1;
                roomState.maxRounds = gameConfig.maxRounds;
                roomState.roundStartTime = TimeInfo.Instance.ClientNow();
                roomState.activePlayers = new List<string>(room.Info.PlayerNames);

                // 3. 创建游戏开始通知
                var notification = GameStartNotification.Create();
                notification.roomId = room.Info.Id;
                notification.config = gameConfig;
                notification.roomState = roomState;
                notification.startTime = room.Info.GameStartTime;
                notification.playerIds = new List<string>(room.Info.PlayerNames);

                // 4. 先发送 GameStartNotification 给房间内所有玩家（确保客户端游戏模式已切换）
                foreach (var player in players)
                {
                    var sessionId = _userManager.GetSessionIdByUserId(player.UserId);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _networkManager.SendMessage(sessionId, notification);
                        ASLogger.Instance.Debug($"MatchmakingManager: 发送 GameStartNotification 给: {player.UserId}");
                    }
                }
                
                ASLogger.Instance.Info($"MatchmakingManager: 已通知房间 {room.Info.Id} 的所有玩家游戏开始 (Players: {room.Info.CurrentPlayers})");
                
                // 5. 调用房间管理器开始游戏（会自动启动帧同步）
                if (!_roomManager.StartGame(room.Info.Id, room.Info.CreatorName))
                {
                    ASLogger.Instance.Warning($"MatchmakingManager: 房间管理器开始游戏失败: {room.Info.Id}");
                    return;
                }
                
                // 6. 发送帧同步开始通知（在 GameStartNotification 之后）
                var gameSession = room.GetGameSession();
                if (gameSession != null)
                {
                    gameSession.SendFrameSyncStartNotificationIfReady();
                }
                
                ASLogger.Instance.Info($"MatchmakingManager: 房间 {room.Info.Id} 帧同步已启动");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MatchmakingManager: 自动开始游戏时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 检查超时
        /// </summary>
        private void CheckTimeouts()
        {
            try
            {
                var now = TimeInfo.Instance.ClientNow();
                var timeoutPlayers = new List<MatchmakingEntry>();
                
                // 查找超时的玩家
                foreach (var entry in _matchQueue)
                {
                    if (now >= entry.TimeoutTime)
                    {
                        timeoutPlayers.Add(entry);
                    }
                }
                
                // 处理超时玩家
                foreach (var player in timeoutPlayers)
                {
                    ASLogger.Instance.Info($"MatchmakingManager: 玩家匹配超时: {player.UserId}, 等待时长: {(now - player.EnterQueueTime) / 1000}秒");
                    
                    // 从队列移除
                    DequeuePlayer(player.UserId);
                    
                    // 发送超时通知
                    NotifyMatchTimeout(player.UserId, (int)((now - player.EnterQueueTime) / 1000));
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MatchmakingManager: 检查超时时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 通知匹配超时
        /// </summary>
        private void NotifyMatchTimeout(string userId, int waitTimeSeconds)
        {
            try
            {
                var notification = MatchTimeoutNotification.Create();
                notification.Message = "匹配超时，请重试";
                notification.Timestamp = TimeInfo.Instance.ClientNow();
                notification.WaitTimeSeconds = waitTimeSeconds;
                
                var sessionId = _userManager.GetSessionIdByUserId(userId);
                if (!string.IsNullOrEmpty(sessionId))
                {
                    _networkManager.SendMessage(sessionId, notification);
                    ASLogger.Instance.Debug($"MatchmakingManager: 发送匹配超时通知给: {userId}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MatchmakingManager: 通知匹配超时时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 获取匹配队列统计信息
        /// </summary>
        public (int queueSize, long avgWaitTime) GetStatistics()
        {
            var queueSize = _matchQueue.Count;
            var avgWaitTime = 0L;
            
            if (queueSize > 0)
            {
                var now = TimeInfo.Instance.ClientNow();
                var totalWaitTime = 0L;
                
                foreach (var entry in _matchQueue)
                {
                    totalWaitTime += (now - entry.EnterQueueTime);
                }
                
                avgWaitTime = totalWaitTime / queueSize;
            }
            
            return (queueSize, avgWaitTime);
        }

        /// <summary>
        /// 清空匹配队列（用于服务器重启等情况）
        /// </summary>
        public void Clear()
        {
            _matchQueue.Clear();
            _userEntries.Clear();
            ASLogger.Instance.Info("MatchmakingManager: 匹配队列已清空");
        }
    }
}

