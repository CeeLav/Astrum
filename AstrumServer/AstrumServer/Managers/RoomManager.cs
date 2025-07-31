using AstrumServer.Models;
using Microsoft.Extensions.Logging;

namespace AstrumServer.Managers
{
    public class RoomManager
    {
        private static RoomManager? _instance;
        private static readonly object _lock = new();
        
        public static RoomManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new RoomManager();
                    }
                }
                return _instance;
            }
        }

        private readonly Dictionary<string, Room> _rooms = new();
        private readonly ILogger? _logger;

        private RoomManager()
        {
        }

        public void SetLogger(ILogger logger)
        {
            var loggerField = typeof(RoomManager).GetField("_logger", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            loggerField?.SetValue(this, logger);
        }

        public Room? CreateRoom(string name, string creatorId, int maxPlayers = 4)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(creatorId))
                return null;

            if (maxPlayers < 1 || maxPlayers > 16)
                maxPlayers = 4;

            var room = new Room(name, creatorId, maxPlayers);
            _rooms[room.Id] = room;
            
            // 更新用户当前房间
            UserManager.Instance.UpdateUserRoom(creatorId, room.Id);
            
            _logger?.LogInformation("房间创建成功: {RoomName} (ID: {RoomId}) 创建者: {CreatorId}", 
                name, room.Id, creatorId);
            
            return room;
        }

        public bool JoinRoom(string roomId, string userId)
        {
            if (!_rooms.TryGetValue(roomId, out var room))
            {
                _logger?.LogWarning("尝试加入不存在的房间: {RoomId}", roomId);
                return false;
            }

            if (!room.CanJoin())
            {
                _logger?.LogWarning("房间 {RoomId} 已满或不可加入", roomId);
                return false;
            }

            if (room.PlayerIds.Contains(userId))
            {
                _logger?.LogWarning("用户 {UserId} 已在房间 {RoomId} 中", userId, roomId);
                return false;
            }

            if (room.AddPlayer(userId))
            {
                UserManager.Instance.UpdateUserRoom(userId, roomId);
                _logger?.LogInformation("用户 {UserId} 加入房间 {RoomId}", userId, roomId);
                
                // 通知房间内其他玩家
                NotifyRoomUpdate(room);
                return true;
            }

            return false;
        }

        public bool LeaveRoom(string roomId, string userId)
        {
            if (!_rooms.TryGetValue(roomId, out var room))
                return false;

            if (room.RemovePlayer(userId))
            {
                UserManager.Instance.UpdateUserRoom(userId, null);
                _logger?.LogInformation("用户 {UserId} 离开房间 {RoomId}", userId, roomId);

                // 如果房间空了，删除房间
                if (room.PlayerIds.Count == 0)
                {
                    _rooms.Remove(roomId);
                    _logger?.LogInformation("房间 {RoomId} 已删除（无玩家）", roomId);
                }
                else
                {
                    // 通知房间内其他玩家
                    NotifyRoomUpdate(room);
                }
                return true;
            }

            return false;
        }

        public Room? GetRoom(string roomId)
        {
            return _rooms.TryGetValue(roomId, out var room) ? room : null;
        }

        public List<Room> GetAllRooms()
        {
            return _rooms.Values.Where(r => r.IsActive).ToList();
        }

        public List<Room> GetAvailableRooms()
        {
            return _rooms.Values.Where(r => r.IsActive && r.CanJoin()).ToList();
        }

        public List<RoomInfo> GetRoomInfoList()
        {
            var roomInfos = new List<RoomInfo>();
            foreach (var room in _rooms.Values.Where(r => r.IsActive))
            {
                var creator = UserManager.Instance.GetUser(room.CreatorId);
                var roomInfo = new RoomInfo
                {
                    Id = room.Id,
                    Name = room.Name,
                    CreatorName = creator?.DisplayName ?? "未知",
                    CurrentPlayers = room.PlayerIds.Count,
                    MaxPlayers = room.MaxPlayers,
                    CreatedAt = room.CreatedAt,
                    PlayerNames = room.PlayerIds
                        .Select(id => UserManager.Instance.GetUser(id)?.DisplayName ?? "未知")
                        .ToList()
                };
                roomInfos.Add(roomInfo);
            }
            return roomInfos;
        }

        public void RemoveUserFromAllRooms(string userId)
        {
            var roomsToLeave = _rooms.Values.Where(r => r.PlayerIds.Contains(userId)).ToList();
            foreach (var room in roomsToLeave)
            {
                LeaveRoom(room.Id, userId);
            }
        }

        private void NotifyRoomUpdate(Room room)
        {
            var roomInfo = GetRoomInfoList().FirstOrDefault(r => r.Id == room.Id);
            if (roomInfo == null) return;

            var message = NetworkMessage.CreateSuccess("room_update", roomInfo);
            var json = System.Text.Json.JsonSerializer.Serialize(message);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json + "\n");

            foreach (var playerId in room.PlayerIds)
            {
                var user = UserManager.Instance.GetUser(playerId);
                if (user?.Client?.Connected == true)
                {
                    try
                    {
                        user.Client.GetStream().WriteAsync(bytes, 0, bytes.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "向用户 {UserId} 发送房间更新失败", playerId);
                    }
                }
            }
        }
    }
} 