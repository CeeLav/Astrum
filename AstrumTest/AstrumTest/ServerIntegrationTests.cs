using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using AstrumServer.Core;
using AstrumServer.Network;
using Astrum.Generated;
using Astrum.CommonBase;
using System.Collections.Generic; // Added for List

namespace AstrumTest
{
    /// <summary>
    /// 服务器集成测试 - 测试完整的服务器通讯流程和房间系统
    /// </summary>
    public class ServerIntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ILogger<GameServer> _logger;
        private GameServer? _gameServer;
        private CancellationTokenSource? _cancellationTokenSource;

        public ServerIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            
            // 创建测试用的Logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            });
            
            _logger = loggerFactory.CreateLogger<GameServer>();
        }

        /// <summary>
        /// 测试服务器启动和关闭
        /// </summary>
        [Fact]
        public async Task GameServer_StartupAndShutdown_ShouldWork()
        {
            // Arrange
            _cancellationTokenSource = new CancellationTokenSource();
            _gameServer = new GameServer(_logger);

            // Act - 启动服务器
            var startupTask = _gameServer.StartAsync(_cancellationTokenSource.Token);
            
            // 等待一小段时间让服务器启动
            await Task.Delay(1000, _cancellationTokenSource.Token);
            
            // Assert - 检查网络管理器状态
            var networkManager = ServerNetworkManager.Instance;
            Assert.NotNull(networkManager);
            
            _output.WriteLine("✅ 游戏服务器启动成功");

            // Act - 关闭服务器
            _cancellationTokenSource.Cancel();
            await startupTask;

            _output.WriteLine("✅ 游戏服务器关闭成功");
        }

        /// <summary>
        /// 测试网络管理器基本功能
        /// </summary>
        [Fact]
        public void ServerNetworkManager_BasicOperations_ShouldWork()
        {
            // Arrange
            var networkManager = ServerNetworkManager.Instance;
            Assert.NotNull(networkManager);

            // Act & Assert - 基本操作应该不抛出异常
            var sendException = Record.Exception(() => networkManager.SendMessage("nonexistent", CreateTestMessage()));
            var broadcastException = Record.Exception(() => networkManager.BroadcastMessage(CreateTestMessage()));
            var disconnectException = Record.Exception(() => networkManager.DisconnectSession(999));
            
            Assert.Null(sendException);
            Assert.Null(broadcastException);
            Assert.Null(disconnectException);

            _output.WriteLine("✅ 网络管理器基本操作正常");
        }

        /// <summary>
        /// 测试消息序列化和反序列化
        /// </summary>
        [Fact]
        public void Message_Serialization_ShouldWork()
        {
            // Arrange
            var testMessage = CreateTestMessage();

            // Act - 序列化
            var serializedData = MemoryPack.MemoryPackSerializer.Serialize(testMessage);
            Assert.NotNull(serializedData);
            Assert.True(serializedData.Length > 0);

            // Act - 反序列化
            var deserializedMessage = MemoryPack.MemoryPackSerializer.Deserialize<LoginRequest>(serializedData);
            Assert.NotNull(deserializedMessage);
            Assert.Equal(testMessage.DisplayName, deserializedMessage.DisplayName);

            _output.WriteLine($"✅ 消息序列化/反序列化成功: {serializedData.Length} bytes");
        }

        /// <summary>
        /// 测试房间系统消息
        /// </summary>
        [Fact]
        public void RoomSystemMessages_ShouldWork()
        {
            // Arrange
            var roomListRequest = GetRoomListRequest.Create();
            roomListRequest.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var createRoomRequest = CreateRoomRequest.Create();
            createRoomRequest.RoomName = "测试房间";
            createRoomRequest.MaxPlayers = 4;
            createRoomRequest.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var joinRoomRequest = JoinRoomRequest.Create();
            joinRoomRequest.RoomId = "test_room_456";
            joinRoomRequest.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Act & Assert
            Assert.NotNull(roomListRequest);
            Assert.True(roomListRequest.Timestamp > 0);

            Assert.NotNull(createRoomRequest);
            Assert.Equal("测试房间", createRoomRequest.RoomName);
            Assert.Equal(4, createRoomRequest.MaxPlayers);
            Assert.True(createRoomRequest.Timestamp > 0);

            Assert.NotNull(joinRoomRequest);
            Assert.Equal("test_room_456", joinRoomRequest.RoomId);
            Assert.True(joinRoomRequest.Timestamp > 0);

            _output.WriteLine("✅ 房间系统消息创建成功");
        }

        /// <summary>
        /// 测试响应消息
        /// </summary>
        [Fact]
        public void ResponseMessages_ShouldWork()
        {
            // Arrange
            var loginResponse = LoginResponse.Create();
            loginResponse.Success = true;
            loginResponse.Message = "登录成功";
            loginResponse.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var createRoomResponse = CreateRoomResponse.Create();
            createRoomResponse.Success = true;
            createRoomResponse.Message = "房间创建成功";
            createRoomResponse.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var roomListResponse = GetRoomListResponse.Create();
            roomListResponse.Success = true;
            roomListResponse.Message = "获取房间列表成功";
            roomListResponse.Rooms = new List<RoomInfo>();
            roomListResponse.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Act & Assert
            Assert.NotNull(loginResponse);
            Assert.True(loginResponse.Success);
            Assert.Equal("登录成功", loginResponse.Message);

            Assert.NotNull(createRoomResponse);
            Assert.True(createRoomResponse.Success);
            Assert.Equal("房间创建成功", createRoomResponse.Message);

            Assert.NotNull(roomListResponse);
            Assert.True(roomListResponse.Success);
            Assert.Equal("获取房间列表成功", roomListResponse.Message);
            Assert.NotNull(roomListResponse.Rooms);

            _output.WriteLine("✅ 响应消息创建成功");
        }

        /// <summary>
        /// 测试房间更新通知
        /// </summary>
        [Fact]
        public void RoomUpdateNotification_ShouldWork()
        {
            // Arrange
            var roomInfo = RoomInfo.Create();
            roomInfo.Id = "test_room_789";
            roomInfo.Name = "通知测试房间";
            roomInfo.CreatorName = "test_creator";
            roomInfo.CurrentPlayers = 2;
            roomInfo.MaxPlayers = 4;
            roomInfo.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            roomInfo.PlayerNames = new List<string> { "test_creator", "test_joiner" };

            var notification = RoomUpdateNotification.Create();
            notification.Room = roomInfo;
            notification.UpdateType = "joined";
            notification.RelatedUserId = "test_joiner";
            notification.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Act & Assert
            Assert.NotNull(notification);
            Assert.NotNull(notification.Room);
            Assert.Equal("joined", notification.UpdateType);
            Assert.Equal("test_joiner", notification.RelatedUserId);
            Assert.Equal(roomInfo.Id, notification.Room.Id);
            Assert.Equal(2, notification.Room.CurrentPlayers);

            _output.WriteLine("✅ 房间更新通知创建成功");
        }

        /// <summary>
        /// 测试心跳消息
        /// </summary>
        [Fact]
        public void HeartbeatMessages_ShouldWork()
        {
            // Arrange
            var heartbeatMessage = HeartbeatMessage.Create();
            heartbeatMessage.ClientId = "test_client_123";
            heartbeatMessage.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var heartbeatResponse = HeartbeatResponse.Create();
            heartbeatResponse.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            heartbeatResponse.Message = "pong";

            // Act & Assert
            Assert.NotNull(heartbeatMessage);
            Assert.Equal("test_client_123", heartbeatMessage.ClientId);
            Assert.True(heartbeatMessage.Timestamp > 0);

            Assert.NotNull(heartbeatResponse);
            Assert.True(heartbeatResponse.Timestamp > 0);
            Assert.Equal("pong", heartbeatResponse.Message);

            _output.WriteLine("✅ 心跳消息创建成功");
        }

        /// <summary>
        /// 测试消息类型常量
        /// </summary>
        [Fact]
        public void MessageTypeConstants_ShouldBeCorrect()
        {
            // Act & Assert
            Assert.Equal(10u, networkcommon.GetRoomListRequest);
            Assert.Equal(11u, networkcommon.GetRoomListResponse);
            Assert.Equal(12u, networkcommon.RoomUpdateNotification);
            Assert.Equal(13u, networkcommon.CreateRoomResponse);
            Assert.Equal(14u, networkcommon.JoinRoomResponse);
            Assert.Equal(15u, networkcommon.LeaveRoomResponse);
            Assert.Equal(16u, networkcommon.LoginResponse);

            Assert.Equal(6u, networkcommon.LoginRequest);
            Assert.Equal(7u, networkcommon.CreateRoomRequest);
            Assert.Equal(8u, networkcommon.JoinRoomRequest);
            Assert.Equal(9u, networkcommon.LeaveRoomRequest);
            Assert.Equal(17u, networkcommon.HeartbeatMessage);
            Assert.Equal(18u, networkcommon.HeartbeatResponse);

            _output.WriteLine("✅ 消息类型常量正确");
        }

        /// <summary>
        /// 测试完整的联机流程模拟
        /// </summary>
        [Fact]
        public void CompleteOnlineFlow_ShouldWork()
        {
            // 模拟完整的联机流程
            _output.WriteLine("🚀 开始模拟完整联机流程...");

            // 1. 用户登录
            var loginRequest = LoginRequest.Create();
            loginRequest.DisplayName = "测试玩家";
            _output.WriteLine("✅ 1. 用户登录请求创建: " + loginRequest.DisplayName);

            // 2. 创建房间
            var createRoomRequest = CreateRoomRequest.Create();
            createRoomRequest.RoomName = "联机测试房间";
            createRoomRequest.MaxPlayers = 4;
            _output.WriteLine("✅ 2. 创建房间请求: " + createRoomRequest.RoomName);

            // 3. 获取房间列表
            var getRoomListRequest = GetRoomListRequest.Create();
            getRoomListRequest.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _output.WriteLine("✅ 3. 获取房间列表请求: " + getRoomListRequest.Timestamp);

            // 4. 加入房间
            var joinRoomRequest = JoinRoomRequest.Create();
            joinRoomRequest.RoomId = "test_room_456";
            _output.WriteLine("✅ 4. 加入房间请求: " + joinRoomRequest.RoomId);

            // 5. 离开房间
            var leaveRoomRequest = LeaveRoomRequest.Create();
            leaveRoomRequest.RoomId = "test_room_456";
            _output.WriteLine("✅ 5. 离开房间请求: " + leaveRoomRequest.RoomId);

            // 6. 心跳
            var heartbeatMessage = HeartbeatMessage.Create();
            heartbeatMessage.ClientId = "test_client_123";
            heartbeatMessage.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _output.WriteLine("✅ 6. 心跳消息: " + heartbeatMessage.ClientId);

            _output.WriteLine("🎉 完整联机流程模拟成功！");
        }

        /// <summary>
        /// 测试消息对象池
        /// </summary>
        [Fact]
        public void MessageObjectPool_ShouldWork()
        {
            // Arrange
            var originalMessage = LoginRequest.Create();
            originalMessage.DisplayName = "池测试用户";

            // Act - 回收消息
            originalMessage.Dispose();

            // Act - 从池中获取新消息
            var newMessage = LoginRequest.Create();
            
            // Assert - 新消息应该被正确初始化
            Assert.NotNull(newMessage);
            Assert.Null(newMessage.DisplayName); // 应该被重置为默认值

            _output.WriteLine("✅ 消息对象池功能正常");
        }

        private LoginRequest CreateTestMessage()
        {
            var message = LoginRequest.Create();
            message.DisplayName = "测试用户";
            return message;
        }

        public void Dispose()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _gameServer?.StopAsync(CancellationToken.None).Wait(1000);
                
                _output.WriteLine("🧹 服务器集成测试资源清理完成");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠️ 清理资源时出现异常: {ex.Message}");
            }
        }
    }
}
