using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using AstrumServer.Managers;
using Astrum.Generated;
using Astrum.CommonBase;

namespace AstrumTest
{
    /// <summary>
    /// 房间系统测试
    /// </summary>
    public class RoomSystemTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ILogger<RoomManager> _roomLogger;
        private readonly ILogger<UserManager> _userLogger;
        private readonly RoomManager _roomManager;
        private readonly UserManager _userManager;

        public RoomSystemTests(ITestOutputHelper output)
        {
            _output = output;
            
            // 创建测试用的Logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            });
            
            _roomLogger = loggerFactory.CreateLogger<RoomManager>();
            _userLogger = loggerFactory.CreateLogger<UserManager>();
            
            _roomManager = new RoomManager(_roomLogger);
            _userManager = new UserManager(_userLogger);
        }

        /// <summary>
        /// 测试房间创建功能
        /// </summary>
        [Fact]
        public void RoomManager_CreateRoom_ShouldSucceed()
        {
            // Arrange
            var creatorId = "test_creator_123";
            var roomName = "测试房间";
            var maxPlayers = 4;

            // Act
            var roomInfo = _roomManager.CreateRoom(creatorId, roomName, maxPlayers);

            // Assert
            Assert.NotNull(roomInfo);
            Assert.Equal(roomName, roomInfo.Name);
            Assert.Equal(creatorId, roomInfo.CreatorName);
            Assert.Equal(maxPlayers, roomInfo.MaxPlayers);
            Assert.Equal(1, roomInfo.CurrentPlayers);
            Assert.True(roomInfo.PlayerNames.Contains(creatorId));
            Assert.True(roomInfo.CreatedAt > 0);

            _output.WriteLine($"✅ 房间创建成功: {roomInfo.Id} (Name: {roomInfo.Name}, Creator: {roomInfo.CreatorName})");
        }

        /// <summary>
        /// 测试房间加入功能
        /// </summary>
        [Fact]
        public void RoomManager_JoinRoom_ShouldSucceed()
        {
            // Arrange
            var creatorId = "test_creator_456";
            var joinerId = "test_joiner_789";
            var roomName = "测试加入房间";
            var maxPlayers = 4;

            var roomInfo = _roomManager.CreateRoom(creatorId, roomName, maxPlayers);

            // Act
            var joinResult = _roomManager.JoinRoom(roomInfo.Id, joinerId);

            // Assert
            Assert.True(joinResult);
            
            var updatedRoom = _roomManager.GetRoom(roomInfo.Id);
            Assert.NotNull(updatedRoom);
            Assert.Equal(2, updatedRoom.CurrentPlayers);
            Assert.True(updatedRoom.PlayerNames.Contains(creatorId));
            Assert.True(updatedRoom.PlayerNames.Contains(joinerId));

            _output.WriteLine($"✅ 房间加入成功: {joinerId} -> {roomInfo.Id} (Current: {updatedRoom.CurrentPlayers}/{updatedRoom.MaxPlayers})");
        }

        /// <summary>
        /// 测试房间已满时无法加入
        /// </summary>
        [Fact]
        public void RoomManager_JoinFullRoom_ShouldFail()
        {
            // Arrange
            var creatorId = "test_creator_full";
            var roomName = "满员房间";
            var maxPlayers = 1;

            var roomInfo = _roomManager.CreateRoom(creatorId, roomName, maxPlayers);

            // Act
            var joinResult = _roomManager.JoinRoom(roomInfo.Id, "test_joiner_full");

            // Assert
            Assert.False(joinResult);
            
            var room = _roomManager.GetRoom(roomInfo.Id);
            Assert.NotNull(room);
            Assert.Equal(1, room.CurrentPlayers);
            Assert.Equal(maxPlayers, room.MaxPlayers);

            _output.WriteLine($"✅ 满员房间无法加入: {roomInfo.Id} (Current: {room.CurrentPlayers}/{room.MaxPlayers})");
        }

        /// <summary>
        /// 测试离开房间功能
        /// </summary>
        [Fact]
        public void RoomManager_LeaveRoom_ShouldSucceed()
        {
            // Arrange
            var creatorId = "test_creator_leave";
            var joinerId = "test_joiner_leave";
            var roomName = "测试离开房间";
            var maxPlayers = 4;

            var roomInfo = _roomManager.CreateRoom(creatorId, roomName, maxPlayers);
            _roomManager.JoinRoom(roomInfo.Id, joinerId);

            // Act
            var leaveResult = _roomManager.LeaveRoom(roomInfo.Id, joinerId);

            // Assert
            Assert.True(leaveResult);
            
            var updatedRoom = _roomManager.GetRoom(roomInfo.Id);
            Assert.NotNull(updatedRoom);
            Assert.Equal(1, updatedRoom.CurrentPlayers);
            Assert.False(updatedRoom.PlayerNames.Contains(joinerId));
            Assert.True(updatedRoom.PlayerNames.Contains(creatorId));

            _output.WriteLine($"✅ 房间离开成功: {joinerId} <- {roomInfo.Id} (Current: {updatedRoom.CurrentPlayers}/{updatedRoom.MaxPlayers})");
        }

        /// <summary>
        /// 测试房间空时自动删除
        /// </summary>
        [Fact]
        public void RoomManager_LeaveRoom_ShouldDeleteEmptyRoom()
        {
            // Arrange
            var creatorId = "test_creator_delete";
            var roomName = "测试删除房间";
            var maxPlayers = 1;

            var roomInfo = _roomManager.CreateRoom(creatorId, roomName, maxPlayers);

            // Act
            var leaveResult = _roomManager.LeaveRoom(roomInfo.Id, creatorId);

            // Assert
            Assert.True(leaveResult);
            
            var deletedRoom = _roomManager.GetRoom(roomInfo.Id);
            Assert.Null(deletedRoom);
            Assert.False(_roomManager.RoomExists(roomInfo.Id));

            _output.WriteLine($"✅ 空房间自动删除: {roomInfo.Id}");
        }

        /// <summary>
        /// 测试房间列表功能
        /// </summary>
        [Fact]
        public void RoomManager_GetAllRooms_ShouldReturnAllRooms()
        {
            // Arrange
            var room1 = _roomManager.CreateRoom("creator1", "房间1", 4);
            var room2 = _roomManager.CreateRoom("creator2", "房间2", 6);
            var room3 = _roomManager.CreateRoom("creator3", "房间3", 8);

            // Act
            var allRooms = _roomManager.GetAllRooms();

            // Assert
            Assert.True(allRooms.Count >= 3);
            Assert.True(allRooms.Any(r => r.Id == room1.Id));
            Assert.True(allRooms.Any(r => r.Id == room2.Id));
            Assert.True(allRooms.Any(r => r.Id == room3.Id));

            _output.WriteLine($"✅ 获取房间列表成功: 共 {allRooms.Count} 个房间");
        }

        /// <summary>
        /// 测试房间统计信息
        /// </summary>
        [Fact]
        public void RoomManager_GetRoomStatistics_ShouldReturnCorrectStats()
        {
            // Arrange
            var room1 = _roomManager.CreateRoom("creator1", "统计房间1", 4);
            var room2 = _roomManager.CreateRoom("creator2", "统计房间2", 6);
            _roomManager.JoinRoom(room2.Id, "joiner1");
            _roomManager.JoinRoom(room2.Id, "joiner2");

            // Act
            var stats = _roomManager.GetRoomStatistics();

            // Assert
            Assert.True(stats.totalRooms >= 2);
            Assert.True(stats.totalPlayers >= 4); // room1: 1, room2: 3
            Assert.True(stats.emptyRooms >= 0);

            _output.WriteLine($"✅ 房间统计信息: 房间数={stats.totalRooms}, 玩家数={stats.totalPlayers}, 空房间={stats.emptyRooms}");
        }

        /// <summary>
        /// 测试用户管理器分配ID功能
        /// </summary>
        [Fact]
        public void UserManager_AssignUserId_ShouldSucceed()
        {
            // Arrange
            var sessionId = "test_session_123";
            var displayName = "测试用户";

            // Act
            var userInfo = _userManager.AssignUserId(sessionId, displayName);

            // Assert
            Assert.NotNull(userInfo);
            Assert.NotNull(userInfo.Id);
            Assert.Equal(displayName, userInfo.DisplayName);
            Assert.True(userInfo.LastLoginAt > 0);
            Assert.Equal("", userInfo.CurrentRoomId);

            _output.WriteLine($"✅ 用户ID分配成功: {userInfo.Id} (DisplayName: {userInfo.DisplayName})");
        }

        /// <summary>
        /// 测试用户管理器会话映射功能
        /// </summary>
        [Fact]
        public void UserManager_SessionMapping_ShouldWork()
        {
            // Arrange
            var sessionId = "test_session_mapping";
            var displayName = "映射测试用户";

            var userInfo = _userManager.AssignUserId(sessionId, displayName);

            // Act & Assert
            var foundUser = _userManager.GetUserBySessionId(sessionId);
            Assert.NotNull(foundUser);
            Assert.Equal(userInfo.Id, foundUser.Id);

            var foundSessionId = _userManager.GetSessionIdByUserId(userInfo.Id);
            Assert.Equal(sessionId, foundSessionId);

            _output.WriteLine($"✅ 用户会话映射功能正常: Session={sessionId} <-> User={userInfo.Id}");
        }

        /// <summary>
        /// 测试用户管理器移除用户功能
        /// </summary>
        [Fact]
        public void UserManager_RemoveUser_ShouldSucceed()
        {
            // Arrange
            var sessionId = "test_session_remove";
            var displayName = "移除测试用户";

            var userInfo = _userManager.AssignUserId(sessionId, displayName);

            // Act
            _userManager.RemoveUser(sessionId);

            // Assert
            var removedUser = _userManager.GetUserBySessionId(sessionId);
            Assert.Null(removedUser);

            var removedSessionId = _userManager.GetSessionIdByUserId(userInfo.Id);
            Assert.Null(removedSessionId);

            _output.WriteLine($"✅ 用户移除成功: {userInfo.Id}");
        }

        /// <summary>
        /// 测试用户房间信息更新
        /// </summary>
        [Fact]
        public void UserManager_UpdateUserRoom_ShouldSucceed()
        {
            // Arrange
            var sessionId = "test_session_room";
            var displayName = "房间测试用户";
            var roomId = "test_room_123";

            var userInfo = _userManager.AssignUserId(sessionId, displayName);

            // Act
            _userManager.UpdateUserRoom(userInfo.Id, roomId);

            // Assert
            var updatedUser = _userManager.GetUserById(userInfo.Id);
            Assert.NotNull(updatedUser);
            Assert.Equal(roomId, updatedUser.CurrentRoomId);

            _output.WriteLine($"✅ 用户房间信息更新成功: {userInfo.Id} -> {roomId}");
        }

        /// <summary>
        /// 测试在线用户统计
        /// </summary>
        [Fact]
        public void UserManager_OnlineUserCount_ShouldBeAccurate()
        {
            // Arrange
            var initialCount = _userManager.GetOnlineUserCount();
            
            var user1 = _userManager.AssignUserId("session1", "用户1");
            var user2 = _userManager.AssignUserId("session2", "用户2");
            var user3 = _userManager.AssignUserId("session3", "用户3");

            // Act
            var currentCount = _userManager.GetOnlineUserCount();

            // Assert
            Assert.Equal(initialCount + 3, currentCount);

            _output.WriteLine($"✅ 在线用户统计准确: 初始={initialCount}, 当前={currentCount}");
        }

        /// <summary>
        /// 测试房间和用户的集成功能
        /// </summary>
        [Fact]
        public void RoomAndUser_Integration_ShouldWork()
        {
            // Arrange
            var creatorSession = "creator_session";
            var joinerSession = "joiner_session";
            
            var creator = _userManager.AssignUserId(creatorSession, "创建者");
            var joiner = _userManager.AssignUserId(joinerSession, "加入者");

            // Act - 创建房间
            var room = _roomManager.CreateRoom(creator.Id, "集成测试房间", 4);
            _userManager.UpdateUserRoom(creator.Id, room.Id);

            // Act - 加入房间
            var joinResult = _roomManager.JoinRoom(room.Id, joiner.Id);
            _userManager.UpdateUserRoom(joiner.Id, room.Id);

            // Assert
            Assert.True(joinResult);
            
            var updatedRoom = _roomManager.GetRoom(room.Id);
            Assert.NotNull(updatedRoom);
            Assert.Equal(2, updatedRoom.CurrentPlayers);

            var creatorUser = _userManager.GetUserById(creator.Id);
            var joinerUser = _userManager.GetUserById(joiner.Id);
            
            Assert.Equal(room.Id, creatorUser.CurrentRoomId);
            Assert.Equal(room.Id, joinerUser.CurrentRoomId);

            _output.WriteLine($"✅ 房间用户集成功能正常: 房间={room.Id}, 玩家数={updatedRoom.CurrentPlayers}");
        }

        public void Dispose()
        {
            try
            {
                _output.WriteLine("🧹 房间系统测试资源清理完成");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠️ 清理资源时出现异常: {ex.Message}");
            }
        }
    }
}
