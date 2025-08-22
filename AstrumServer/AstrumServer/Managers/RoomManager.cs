using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Astrum.Generated;
using Astrum.CommonBase;

namespace AstrumServer.Managers
{
    /// <summary>
    /// 房间管理器 - 管理所有房间
    /// </summary>
    public class RoomManager
    {
        private readonly ILogger<RoomManager> _logger;
        private readonly ConcurrentDictionary<string, RoomInfo> _rooms = new();
        private readonly ConcurrentDictionary<string, List<string>> _roomPlayers = new(); // roomId -> List<userId>

        public RoomManager(ILogger<RoomManager> logger)
        {
            _logger = logger;
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

                // 添加到管理器中
                _rooms[roomId] = roomInfo;
                _roomPlayers[roomId] = new List<string> { creatorId };

                _logger.LogInformation("创建房间: {RoomId} (Name: {RoomName}, Creator: {CreatorId}, MaxPlayers: {MaxPlayers})", 
                    roomId, roomName, creatorId, maxPlayers);

                return roomInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建房间时出错");
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
                    _logger.LogWarning("房间不存在: {RoomId}", roomId);
                    return false;
                }

                if (roomInfo.CurrentPlayers >= roomInfo.MaxPlayers)
                {
                    _logger.LogWarning("房间已满: {RoomId} (Current: {Current}, Max: {Max})", 
                        roomId, roomInfo.CurrentPlayers, roomInfo.MaxPlayers);
                    return false;
                }

                if (roomInfo.PlayerNames.Contains(userId))
                {
                    _logger.LogWarning("用户已在房间中: {UserId} in {RoomId}", userId, roomId);
                    return false;
                }

                // 添加用户到房间
                roomInfo.CurrentPlayers++;
                roomInfo.PlayerNames.Add(userId);

                if (_roomPlayers.TryGetValue(roomId, out var players))
                {
                    players.Add(userId);
                }

                _logger.LogInformation("用户加入房间: {UserId} -> {RoomId} (Current: {Current}/{Max})", 
                    userId, roomId, roomInfo.CurrentPlayers, roomInfo.MaxPlayers);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加入房间时出错");
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
                    _logger.LogWarning("房间不存在: {RoomId}", roomId);
                    return false;
                }

                if (!roomInfo.PlayerNames.Contains(userId))
                {
                    _logger.LogWarning("用户不在房间中: {UserId} in {RoomId}", userId, roomId);
                    return false;
                }

                // 从房间移除用户
                roomInfo.CurrentPlayers--;
                roomInfo.PlayerNames.Remove(userId);

                if (_roomPlayers.TryGetValue(roomId, out var players))
                {
                    players.Remove(userId);
                }

                _logger.LogInformation("用户离开房间: {UserId} <- {RoomId} (Current: {Current}/{Max})", 
                    userId, roomId, roomInfo.CurrentPlayers, roomInfo.MaxPlayers);

                // 如果房间空了，删除房间
                if (roomInfo.CurrentPlayers == 0)
                {
                    DeleteRoom(roomId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "离开房间时出错");
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
                    
                    _logger.LogInformation("删除房间: {RoomId} (Name: {RoomName})", roomId, roomInfo.Name);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除房间时出错");
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
                _logger.LogInformation("清理了 {Count} 个空房间", emptyRooms.Count);
            }
        }

        /// <summary>
        /// 获取房间统计信息
        /// </summary>
        public (int totalRooms, int totalPlayers, int emptyRooms) GetRoomStatistics()
        {
            var totalRooms = _rooms.Count;
            var totalPlayers = _rooms.Values.Sum(r => r.CurrentPlayers);
            var emptyRooms = _rooms.Values.Count(r => r.CurrentPlayers == 0);

            return (totalRooms, totalPlayers, emptyRooms);
        }
    }
} 