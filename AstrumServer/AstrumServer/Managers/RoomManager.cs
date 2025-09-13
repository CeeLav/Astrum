using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Astrum.Generated;
using Astrum.CommonBase;

namespace AstrumServer.Managers
{
    /// <summary>
    /// 房间管理器 - 管理所有房间
    /// </summary>
    public class RoomManager
    {
        private readonly ConcurrentDictionary<string, RoomInfo> _rooms = new();
        private readonly ConcurrentDictionary<string, List<string>> _roomPlayers = new(); // roomId -> List<userId>

        public RoomManager()
        {
        }

        /// <summary>
        /// 创建房间
        /// </summary>
        public RoomInfo CreateRoom(string creatorId, string roomName, int maxPlayers)
        {
            try
            {
                // 生成唯一房间ID
                var roomId = GenerateRoomId();
                
                var roomInfo = RoomInfo.Create();
                roomInfo.Id = roomId;
                roomInfo.Name = roomName;
                roomInfo.CreatorName = creatorId;
                roomInfo.CurrentPlayers = 1;
                roomInfo.MaxPlayers = maxPlayers;
                roomInfo.CreatedAt = TimeInfo.Instance.ClientNow();
                roomInfo.PlayerNames = new List<string> { creatorId };
                roomInfo.Status = 0; // 初始状态为等待中 (0=等待中)
                roomInfo.GameStartTime = 0;
                roomInfo.GameEndTime = 0;

                // 添加到管理器中
                _rooms[roomId] = roomInfo;
                _roomPlayers[roomId] = new List<string> { creatorId };

                ASLogger.Instance.Info($"创建房间: {roomId} (Name: {roomName}, Creator: {creatorId}, MaxPlayers: {maxPlayers})");

                return roomInfo;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"创建房间时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// 加入房间
        /// </summary>
        public bool JoinRoom(string roomId, string userId)
        {
            try
            {
                if (!_rooms.TryGetValue(roomId, out var roomInfo))
                {
                    ASLogger.Instance.Warning($"房间不存在: {roomId}");
                    return false;
                }

                if (roomInfo.CurrentPlayers >= roomInfo.MaxPlayers)
                {
                    ASLogger.Instance.Warning($"房间已满: {roomId} (Current: {roomInfo.CurrentPlayers}, Max: {roomInfo.MaxPlayers})");
                    return false;
                }

                if (roomInfo.PlayerNames.Contains(userId))
                {
                    ASLogger.Instance.Warning($"用户已在房间中: {userId} in {roomId}");
                    return false;
                }

                // 添加用户到房间
                roomInfo.CurrentPlayers++;
                roomInfo.PlayerNames.Add(userId);

                if (_roomPlayers.TryGetValue(roomId, out var players))
                {
                    players.Add(userId);
                }

                ASLogger.Instance.Info($"用户加入房间: {userId} -> {roomId} (Current: {roomInfo.CurrentPlayers}/{roomInfo.MaxPlayers})");

                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"加入房间时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 离开房间
        /// </summary>
        public bool LeaveRoom(string roomId, string userId)
        {
            try
            {
                if (!_rooms.TryGetValue(roomId, out var roomInfo))
                {
                    ASLogger.Instance.Warning($"房间不存在: {roomId}");
                    return false;
                }

                if (!roomInfo.PlayerNames.Contains(userId))
                {
                    ASLogger.Instance.Warning($"用户不在房间中: {userId} in {roomId}");
                    return false;
                }

                // 从房间移除用户
                roomInfo.CurrentPlayers--;
                roomInfo.PlayerNames.Remove(userId);

                if (_roomPlayers.TryGetValue(roomId, out var players))
                {
                    players.Remove(userId);
                }

                ASLogger.Instance.Info($"用户离开房间: {userId} <- {roomId} (Current: {roomInfo.CurrentPlayers}/{roomInfo.MaxPlayers})");

                // 如果房间空了，检查游戏状态并处理
                if (roomInfo.CurrentPlayers == 0)
                {
                    // 如果游戏正在进行中，先结束游戏
                    if (roomInfo.Status == 1) // 1=游戏中
                    {
                        EndGame(roomId, "房间内所有玩家已离开");
                    }
                    
                    // 删除房间
                    DeleteRoom(roomId);
                }

                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"离开房间时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 删除房间
        /// </summary>
        public bool DeleteRoom(string roomId)
        {
            try
            {
                if (_rooms.TryRemove(roomId, out var roomInfo))
                {
                    _roomPlayers.TryRemove(roomId, out _);
                    
                    ASLogger.Instance.Info($"删除房间: {roomId} (Name: {roomInfo.Name})");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"删除房间时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 获取房间信息
        /// </summary>
        public RoomInfo? GetRoom(string roomId)
        {
            _rooms.TryGetValue(roomId, out var roomInfo);
            return roomInfo;
        }

        /// <summary>
        /// 获取所有房间列表
        /// </summary>
        public List<RoomInfo> GetAllRooms()
        {
            return _rooms.Values.ToList();
        }

        /// <summary>
        /// 获取房间数量
        /// </summary>
        public int GetRoomCount()
        {
            return _rooms.Count;
        }

        /// <summary>
        /// 检查房间是否存在
        /// </summary>
        public bool RoomExists(string roomId)
        {
            return _rooms.ContainsKey(roomId);
        }

        /// <summary>
        /// 检查用户是否在房间中
        /// </summary>
        public bool IsUserInRoom(string userId, string roomId)
        {
            if (_rooms.TryGetValue(roomId, out var roomInfo))
            {
                return roomInfo.PlayerNames.Contains(userId);
            }
            return false;
        }

        /// <summary>
        /// 获取用户在的房间ID
        /// </summary>
        public string? GetUserRoomId(string userId)
        {
            foreach (var kvp in _rooms)
            {
                var roomId = kvp.Key;
                var roomInfo = kvp.Value;
                if (roomInfo.PlayerNames.Contains(userId))
                {
                    return roomId;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取房间内的所有用户ID
        /// </summary>
        public List<string> GetRoomPlayers(string roomId)
        {
            if (_roomPlayers.TryGetValue(roomId, out var players))
            {
                return new List<string>(players);
            }
            return new List<string>();
        }

        /// <summary>
        /// 生成唯一房间ID
        /// </summary>
        private string GenerateRoomId()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = new Random().Next(1000, 9999);
            return $"room_{timestamp}_{random}";
        }

        /// <summary>
        /// 清理空房间
        /// </summary>
        public void CleanupEmptyRooms()
        {
            var emptyRooms = new List<string>();
            
            foreach (var kvp in _rooms)
            {
                var roomId = kvp.Key;
                var roomInfo = kvp.Value;
                
                if (roomInfo.CurrentPlayers == 0)
                {
                    emptyRooms.Add(roomId);
                }
            }

            foreach (var roomId in emptyRooms)
            {
                DeleteRoom(roomId);
            }

            if (emptyRooms.Count > 0)
            {
                ASLogger.Instance.Info($"清理了 {emptyRooms.Count} 个空房间");
            }
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public bool StartGame(string roomId, string hostId)
        {
            try
            {
                if (!_rooms.TryGetValue(roomId, out var roomInfo))
                {
                    ASLogger.Instance.Warning($"房间不存在: {roomId}");
                    return false;
                }

                if (roomInfo.Status != 0) // 0=等待中
                {
                    ASLogger.Instance.Warning($"房间状态不是等待中，无法开始游戏: {roomId} (Status: {roomInfo.Status})");
                    return false;
                }

                if (roomInfo.CreatorName != hostId)
                {
                    ASLogger.Instance.Warning($"只有房主才能开始游戏: {hostId} (Creator: {roomInfo.CreatorName})");
                    return false;
                }

                // 更新房间状态
                roomInfo.Status = 1; // 1=游戏中
                roomInfo.GameStartTime = TimeInfo.Instance.ClientNow();
                roomInfo.GameEndTime = 0;

                ASLogger.Instance.Info($"房间开始游戏: {roomId} (Host: {hostId}, Players: {roomInfo.CurrentPlayers})");

                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"开始游戏时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 结束游戏
        /// </summary>
        public bool EndGame(string roomId, string reason = "游戏结束")
        {
            try
            {
                if (!_rooms.TryGetValue(roomId, out var roomInfo))
                {
                    ASLogger.Instance.Warning($"房间不存在: {roomId}");
                    return false;
                }

                if (roomInfo.Status != 1) // 1=游戏中
                {
                    ASLogger.Instance.Warning($"房间不在游戏中，无法结束游戏: {roomId} (Status: {roomInfo.Status})");
                    return false;
                }

                // 更新房间状态
                roomInfo.Status = 2; // 2=已结束
                roomInfo.GameEndTime = TimeInfo.Instance.ClientNow();

                ASLogger.Instance.Info($"房间结束游戏: {roomId} (Reason: {reason}, Duration: {roomInfo.GameEndTime - roomInfo.GameStartTime}ms)");

                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"结束游戏时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 重置房间状态（用于重新开始游戏）
        /// </summary>
        public bool ResetRoom(string roomId)
        {
            try
            {
                if (!_rooms.TryGetValue(roomId, out var roomInfo))
                {
                    ASLogger.Instance.Warning($"房间不存在: {roomId}");
                    return false;
                }

                // 重置房间状态
                roomInfo.Status = 0; // 0=等待中
                roomInfo.GameStartTime = 0;
                roomInfo.GameEndTime = 0;

                ASLogger.Instance.Info($"重置房间状态: {roomId}");

                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"重置房间状态时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 获取房间统计信息
        /// </summary>
        public (int totalRooms, int totalPlayers, int emptyRooms, int waitingRooms, int playingRooms, int finishedRooms) GetRoomStatistics()
        {
            var totalRooms = _rooms.Count;
            var totalPlayers = _rooms.Values.Sum(r => r.CurrentPlayers);
            var emptyRooms = _rooms.Values.Count(r => r.CurrentPlayers == 0);
            var waitingRooms = _rooms.Values.Count(r => r.Status == 0); // 0=等待中
            var playingRooms = _rooms.Values.Count(r => r.Status == 1); // 1=游戏中
            var finishedRooms = _rooms.Values.Count(r => r.Status == 2); // 2=已结束

            return (totalRooms, totalPlayers, emptyRooms, waitingRooms, playingRooms, finishedRooms);
        }
    }
} 