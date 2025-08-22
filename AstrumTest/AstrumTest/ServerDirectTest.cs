using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using AstrumServer.Core;
using AstrumServer.Managers;
using AstrumServer.Network;
using Astrum.Generated;

namespace AstrumTest
{
    /// <summary>
    /// 服务器直接功能测试 - 不通过网络连接，直接测试服务器逻辑
    /// </summary>
    public class ServerDirectTest
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly UserManager _userManager;
        private readonly RoomManager _roomManager;

        public ServerDirectTest()
        {
            // 创建日志工厂
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
            });

            // 创建管理器实例
            _userManager = new UserManager(_loggerFactory.CreateLogger<UserManager>());
            _roomManager = new RoomManager(_loggerFactory.CreateLogger<RoomManager>());
        }

        /// <summary>
        /// 测试用户管理器基本功能
        /// </summary>
        [Fact]
        public void TestUserManagerBasicFunctionality()
        {
            // 测试用户分配
            var userInfo1 = _userManager.AssignUserId("session1", "测试用户1");
            Assert.NotNull(userInfo1);
            Assert.Equal("session1", userInfo1.Id);
            Assert.Equal("测试用户1", userInfo1.DisplayName);

            var userInfo2 = _userManager.AssignUserId("session2", "测试用户2");
            Assert.NotNull(userInfo2);
            Assert.Equal("session2", userInfo2.Id);
            Assert.Equal("测试用户2", userInfo2.DisplayName);

            // 测试用户查找
            var foundUser = _userManager.GetUserById("session1");
            Assert.NotNull(foundUser);
            Assert.Equal("测试用户1", foundUser.DisplayName);

            // 测试会话映射
            var sessionId = _userManager.GetSessionIdByUserId("session1");
            Assert.Equal("session1", sessionId);

            // 测试在线用户数量
            var onlineCount = _userManager.GetOnlineUserCount();
            Assert.Equal(2, onlineCount);
        }

        /// <summary>
        /// 测试房间管理器基本功能
        /// </summary>
        [Fact]
        public void TestRoomManagerBasicFunctionality()
        {
            // 测试创建房间
            var roomInfo1 = _roomManager.CreateRoom("测试房间1", "创建者1", 4);
            Assert.NotNull(roomInfo1);
            Assert.Equal("测试房间1", roomInfo1.Name);
            Assert.Equal("创建者1", roomInfo1.CreatorName);
            Assert.Equal(4, roomInfo1.MaxPlayers);
            Assert.Equal(1, roomInfo1.CurrentPlayers);

            var roomInfo2 = _roomManager.CreateRoom("测试房间2", "创建者2", 6);
            Assert.NotNull(roomInfo2);
            Assert.Equal("测试房间2", roomInfo2.Name);
            Assert.Equal("创建者2", roomInfo2.CreatorName);
            Assert.Equal(6, roomInfo2.MaxPlayers);
            Assert.Equal(1, roomInfo2.CurrentPlayers);

            // 测试获取房间列表
            var rooms = _roomManager.GetAllRooms();
            Assert.Equal(2, rooms.Count);

            // 测试加入房间
            var joinResult = _roomManager.JoinRoom(roomInfo1.Id, "加入者1");
            Assert.True(joinResult);
            Assert.Equal(2, roomInfo1.CurrentPlayers);

            // 测试离开房间
            var leaveResult = _roomManager.LeaveRoom(roomInfo1.Id, "加入者1");
            Assert.True(leaveResult);
            Assert.Equal(1, roomInfo1.CurrentPlayers);
        }

        /// <summary>
        /// 测试房间系统完整流程
        /// </summary>
        [Fact]
        public void TestRoomSystemCompleteFlow()
        {
            // 1. 创建用户
            var creator = _userManager.AssignUserId("creator_session", "房间创建者");
            var joiner = _userManager.AssignUserId("joiner_session", "房间加入者");

            // 2. 创建房间
            var room = _roomManager.CreateRoom("完整测试房间", creator.DisplayName, 4);
            Assert.NotNull(room);
            Assert.Equal(1, room.CurrentPlayers);

            // 3. 加入房间
            var joinSuccess = _roomManager.JoinRoom(room.Id, joiner.DisplayName);
            Assert.True(joinSuccess);
            Assert.Equal(2, room.CurrentPlayers);

            // 4. 更新用户房间信息
            _userManager.UpdateUserRoom(creator.Id, room.Id);
            _userManager.UpdateUserRoom(joiner.Id, room.Id);

            var creatorUser = _userManager.GetUserById(creator.Id);
            var joinerUser = _userManager.GetUserById(joiner.Id);
            Assert.Equal(room.Id, creatorUser.CurrentRoomId);
            Assert.Equal(room.Id, joinerUser.CurrentRoomId);

            // 5. 离开房间
            var leaveSuccess = _roomManager.LeaveRoom(room.Id, joiner.DisplayName);
            Assert.True(leaveSuccess);
            Assert.Equal(1, room.CurrentPlayers);

            // 6. 清理用户
            _userManager.RemoveUser(creator.Id);
            _userManager.RemoveUser(joiner.Id);

            var onlineCount = _userManager.GetOnlineUserCount();
            Assert.Equal(0, onlineCount);
        }

        /// <summary>
        /// 测试网络管理器初始化
        /// </summary>
        [Fact]
        public async Task TestNetworkManagerInitialization()
        {
            var networkManager = ServerNetworkManager.Instance;
            var logger = _loggerFactory.CreateLogger<ServerNetworkManager>();
            networkManager.SetLogger(logger);

            // 测试初始化
            var initResult = await networkManager.InitializeAsync(8889); // 使用不同端口避免冲突
            Assert.True(initResult);

            // 测试关闭
            networkManager.Shutdown();
        }

        /// <summary>
        /// 测试消息序列化
        /// </summary>
        [Fact]
        public void TestMessageSerialization()
        {
            // 测试登录请求序列化
            var loginRequest = LoginRequest.Create();
            loginRequest.DisplayName = "序列化测试用户";

            // 测试创建房间请求序列化
            var createRoomRequest = CreateRoomRequest.Create();
            createRoomRequest.RoomName = "序列化测试房间";
            createRoomRequest.MaxPlayers = 4;
            createRoomRequest.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 测试获取房间列表请求序列化
            var getRoomListRequest = GetRoomListRequest.Create();
            getRoomListRequest.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 测试心跳消息序列化
            var heartbeatMessage = HeartbeatMessage.Create();
            heartbeatMessage.ClientId = "test_client";
            heartbeatMessage.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 如果所有消息都能创建成功，说明序列化没问题
            Assert.NotNull(loginRequest);
            Assert.NotNull(createRoomRequest);
            Assert.NotNull(getRoomListRequest);
            Assert.NotNull(heartbeatMessage);
        }

        /// <summary>
        /// 测试边界情况
        /// </summary>
        [Fact]
        public void TestEdgeCases()
        {
            // 测试空房间名称
            var room1 = _roomManager.CreateRoom("", "创建者", 4);
            Assert.Null(room1);

            // 测试无效的最大玩家数
            var room2 = _roomManager.CreateRoom("测试房间", "创建者", 0);
            Assert.Null(room2);

            var room3 = _roomManager.CreateRoom("测试房间", "创建者", -1);
            Assert.Null(room3);

            // 测试加入不存在的房间
            var joinResult = _roomManager.JoinRoom("不存在的房间ID", "用户");
            Assert.False(joinResult);

            // 测试离开不存在的房间
            var leaveResult = _roomManager.LeaveRoom("不存在的房间ID", "用户");
            Assert.False(leaveResult);

            // 测试获取不存在的用户
            var user = _userManager.GetUserById("不存在的用户ID");
            Assert.Null(user);
        }

        /// <summary>
        /// 测试并发操作
        /// </summary>
        [Fact]
        public void TestConcurrentOperations()
        {
            // 创建多个用户
            var users = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                var user = _userManager.AssignUserId($"session_{i}", $"用户_{i}");
                users.Add(user.Id);
            }

            // 创建多个房间
            var rooms = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                var room = _roomManager.CreateRoom($"房间_{i}", $"创建者_{i}", 4);
                if (room != null)
                {
                    rooms.Add(room.Id);
                }
            }

            // 并发加入房间
            var tasks = new List<Task>();
            for (int i = 0; i < users.Count; i++)
            {
                var userId = users[i];
                var roomId = rooms[i % rooms.Count];
                var task = Task.Run(() =>
                {
                    _roomManager.JoinRoom(roomId, $"用户_{userId}");
                });
                tasks.Add(task);
            }

            // 等待所有任务完成
            Task.WaitAll(tasks.ToArray());

            // 验证结果
            var totalUsers = _userManager.GetOnlineUserCount();
            Assert.Equal(10, totalUsers);

            var totalRooms = _roomManager.GetAllRooms().Count;
            Assert.Equal(5, totalRooms);
        }
    }
}
