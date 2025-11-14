using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Astrum.Generated;
using Astrum.CommonBase;
using AstrumServer.Network;

namespace AstrumServer.Managers
{
    /// <summary>
    /// 房间管理器 - 管理所有房间实例
    /// </summary>
    public class RoomManager
    {
        private readonly ConcurrentDictionary<string, ServerRoom> _rooms = new();
        private readonly ServerNetworkManager _networkManager;
        private readonly UserManager _userManager;

        public RoomManager(ServerNetworkManager networkManager, UserManager userManager)
        {
            _networkManager = networkManager;
            _userManager = userManager;
        }

        /// <summary>
        /// 创建房间
        /// </summary>
        public ServerRoom CreateRoom(string creatorId, string roomName, int maxPlayers)
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

                // 创建房间实例
                var room = new ServerRoom(roomInfo, _networkManager, _userManager);
                
                // 添加到管理器中
                _rooms[roomId] = room;

                ASLogger.Instance.Info($"创建房间: {roomId} (Name: {roomName}, Creator: {creatorId}, MaxPlayers: {maxPlayers})");

                return room;
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
                if (!_rooms.TryGetValue(roomId, out var room))
                {
                    ASLogger.Instance.Warning($"房间不存在: {roomId}");
                    return false;
                }

                return room.AddPlayer(userId);
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
                if (!_rooms.TryGetValue(roomId, out var room))
                {
                    ASLogger.Instance.Warning($"房间不存在: {roomId}");
                    return false;
                }

                var success = room.RemovePlayer(userId);

                // 如果房间空了，检查游戏状态并处理
                if (room.Info.CurrentPlayers == 0)
                {
                    // 如果游戏正在进行中，先结束游戏
                    if (room.Info.Status == 1) // 1=游戏中
                    {
                        room.EndGame("房间内所有玩家已离开");
                    }
                    
                    // 删除房间
                    DestroyRoom(roomId);
                }

                return success;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"离开房间时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 销毁房间
        /// </summary>
        public bool DestroyRoom(string roomId)
        {
            try
            {
                if (_rooms.TryRemove(roomId, out var room))
                {
                    // 如果游戏正在进行中，先结束游戏
                    if (room.IsPlaying)
                    {
                        room.EndGame("房间被销毁");
                    }
                    
                    ASLogger.Instance.Info($"删除房间: {roomId} (Name: {room.Info.Name})");
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
        /// 获取房间实例
        /// </summary>
        public ServerRoom? GetRoom(string roomId)
        {
            _rooms.TryGetValue(roomId, out var room);
            return room;
        }

        /// <summary>
        /// 获取房间信息（兼容旧接口）
        /// </summary>
        public RoomInfo? GetRoomInfo(string roomId)
        {
            return GetRoom(roomId)?.Info;
        }

        /// <summary>
        /// 获取所有房间列表
        /// </summary>
        public List<RoomInfo> GetAllRooms()
        {
            return _rooms.Values.Select(r => r.Info).ToList();
        }

        /// <summary>
        /// 获取所有房间实例
        /// </summary>
        public IEnumerable<ServerRoom> GetAllRoomInstances()
        {
            return _rooms.Values;
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
            if (_rooms.TryGetValue(roomId, out var room))
            {
                return room.Info.PlayerNames.Contains(userId);
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
                var room = kvp.Value;
                if (room.Info.PlayerNames.Contains(userId))
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
            if (_rooms.TryGetValue(roomId, out var room))
            {
                return new List<string>(room.Info.PlayerNames);
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
                var room = kvp.Value;
                
                if (room.Info.CurrentPlayers == 0)
                {
                    emptyRooms.Add(roomId);
                }
            }

            foreach (var roomId in emptyRooms)
            {
                DestroyRoom(roomId);
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
                if (!_rooms.TryGetValue(roomId, out var room))
                {
                    ASLogger.Instance.Warning($"房间不存在: {roomId}");
                    return false;
                }

                if (room.Info.Status != 0) // 0=等待中
                {
                    ASLogger.Instance.Warning($"房间状态不是等待中，无法开始游戏: {roomId} (Status: {room.Info.Status})");
                    return false;
                }

                if (room.Info.CreatorName != hostId)
                {
                    ASLogger.Instance.Warning($"只有房主才能开始游戏: {hostId} (Creator: {room.Info.CreatorName})");
                    return false;
                }

                // 调用房间的开始游戏方法
                room.StartGame();

                ASLogger.Instance.Info($"房间开始游戏: {roomId} (Host: {hostId}, Players: {room.Info.CurrentPlayers})");

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
                if (!_rooms.TryGetValue(roomId, out var room))
                {
                    ASLogger.Instance.Warning($"房间不存在: {roomId}");
                    return false;
                }

                if (room.Info.Status != 1) // 1=游戏中
                {
                    ASLogger.Instance.Warning($"房间不在游戏中，无法结束游戏: {roomId} (Status: {room.Info.Status})");
                    return false;
                }

                // 调用房间的结束游戏方法
                room.EndGame(reason);

                ASLogger.Instance.Info($"房间结束游戏: {roomId} (Reason: {reason})");

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
                if (!_rooms.TryGetValue(roomId, out var room))
                {
                    ASLogger.Instance.Warning($"房间不存在: {roomId}");
                    return false;
                }

                // 调用房间的重置方法
                room.Reset();

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
            var totalPlayers = _rooms.Values.Sum(r => r.Info.CurrentPlayers);
            var emptyRooms = _rooms.Values.Count(r => r.Info.CurrentPlayers == 0);
            var waitingRooms = _rooms.Values.Count(r => r.Info.Status == 0); // 0=等待中
            var playingRooms = _rooms.Values.Count(r => r.Info.Status == 1); // 1=游戏中
            var finishedRooms = _rooms.Values.Count(r => r.Info.Status == 2); // 2=已结束

            return (totalRooms, totalPlayers, emptyRooms, waitingRooms, playingRooms, finishedRooms);
        }

        /// <summary>
        /// 更新所有房间（由 GameServer 调用）
        /// </summary>
        public void UpdateAllRooms()
        {
            foreach (var room in _rooms.Values)
            {
                try
                {
                    room.Update();
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Error($"更新房间 {room.Info.Id} 时出错: {ex.Message}");
                    ASLogger.Instance.LogException(ex, LogLevel.Error);
                }
            }
        }
    }
}
