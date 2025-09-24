using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using AstrumServer.Core;
using AstrumServer.Managers;
using AstrumServer.Network;
using Astrum.Generated;
using Astrum.CommonBase;

namespace AstrumTest
{
    /// <summary>
    /// 网络管理器接口测试 - 验证网络模式和本地模式的功能
    /// </summary>
    public class NetworkManagerInterfaceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly LocalServerNetworkManager _localNetworkManager;
        private readonly GameServer _gameServer;
        private readonly UserManager _userManager;
        private readonly RoomManager _roomManager;

        public NetworkManagerInterfaceTests(ITestOutputHelper output)
        {
            _output = output;
            ASLogger.Instance.MinLevel = LogLevel.Error; // 减少测试时的日志输出

            // 创建本地模式网络管理器
            _localNetworkManager = new LocalServerNetworkManager();
            _userManager = new UserManager();
            _roomManager = new RoomManager();

            // 创建游戏服务器，使用本地模式网络管理器
            _gameServer = new GameServer(_localNetworkManager, _userManager, _roomManager, null);
        }

        [Fact]
        public async Task LocalNetworkManager_ShouldSupportDirectInteraction()
        {
            // 1. 初始化网络管理器
            var initResult = await _localNetworkManager.InitializeAsync(8888);
            Assert.True(initResult);

            // 2. 启动游戏服务器
            var cancellationTokenSource = new CancellationTokenSource();
            var serverTask = _gameServer.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(100); // 给服务器时间初始化

            try
            {
                // 3. 模拟客户端连接
                var sessionId = _localNetworkManager.SimulateConnect();
                Assert.True(sessionId > 0);

                // 4. 验证会话数量
                Assert.Equal(1, _localNetworkManager.GetSessionCount());

                // 5. 模拟客户端发送登录消息
                var loginRequest = LoginRequest.Create();
                loginRequest.DisplayName = "TestUser";
                _localNetworkManager.SimulateReceive(sessionId, loginRequest);

                // 6. 更新网络状态，处理消息
                _localNetworkManager.Update();
                await Task.Delay(50); // 给服务器时间处理消息

                // 7. 验证服务器响应
                var pendingMessages = _localNetworkManager.GetPendingMessages(sessionId);
                Assert.NotEmpty(pendingMessages);
                Assert.Contains(pendingMessages, msg => msg is LoginResponse);

                // 8. 模拟客户端断开连接
                _localNetworkManager.SimulateDisconnect(sessionId);
                Assert.Equal(0, _localNetworkManager.GetSessionCount());
            }
            finally
            {
                cancellationTokenSource.Cancel();
                await serverTask;
            }
        }

        [Fact]
        public async Task LocalNetworkManager_ShouldSupportRoomOperations()
        {
            // 初始化
            await _localNetworkManager.InitializeAsync(8888);

            // 启动游戏服务器
            var cancellationTokenSource = new CancellationTokenSource();
            var serverTask = _gameServer.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(100); // 给服务器时间初始化

            try
            {
                // 模拟客户端连接和登录
                var sessionId = _localNetworkManager.SimulateConnect();
                var loginRequest = LoginRequest.Create();
                loginRequest.DisplayName = "RoomCreator";
                _localNetworkManager.SimulateReceive(sessionId, loginRequest);
                _localNetworkManager.Update();
                await Task.Delay(50); // 给服务器时间处理消息

                // 清空登录响应
                _localNetworkManager.ClearPendingMessages(sessionId);

                // 模拟创建房间
                var createRoomRequest = CreateRoomRequest.Create();
                createRoomRequest.RoomName = "TestRoom";
                createRoomRequest.MaxPlayers = 4;
                _localNetworkManager.SimulateReceive(sessionId, createRoomRequest);
                _localNetworkManager.Update();
                await Task.Delay(50); // 给服务器时间处理消息

                // 验证房间创建响应
                var messages = _localNetworkManager.GetPendingMessages(sessionId);
                Assert.Contains(messages, msg => msg is CreateRoomResponse);

                // 验证房间管理器中有房间
                var rooms = _roomManager.GetAllRooms();
                Assert.Single(rooms);
                Assert.Equal("TestRoom", rooms[0].Name);
            }
            finally
            {
                cancellationTokenSource.Cancel();
                await serverTask;
            }
        }

        [Fact]
        public async Task LocalNetworkManager_ShouldSupportAbruptDisconnection()
        {
            // 初始化
            await _localNetworkManager.InitializeAsync(8888);

            // 模拟多个客户端连接
            var sessionIds = new List<long>();
            for (int i = 0; i < 3; i++)
            {
                var sessionId = _localNetworkManager.SimulateConnect();
                sessionIds.Add(sessionId);

                // 登录
                var loginRequest = LoginRequest.Create();
                loginRequest.DisplayName = $"User_{i}";
                _localNetworkManager.SimulateReceive(sessionId, loginRequest);
            }
            _localNetworkManager.Update();

            // 验证连接数量
            Assert.Equal(3, _localNetworkManager.GetSessionCount());

            // 模拟异常断开所有连接
            foreach (var sessionId in sessionIds)
            {
                _localNetworkManager.SimulateDisconnect(sessionId, abrupt: true);
            }

            // 验证所有连接都已断开
            Assert.Equal(0, _localNetworkManager.GetSessionCount());
        }

        [Fact]
        public async Task LocalNetworkManager_ShouldSupportMessageBroadcasting()
        {
            // 初始化
            await _localNetworkManager.InitializeAsync(8888);

            // 模拟多个客户端连接
            var sessionIds = new List<long>();
            for (int i = 0; i < 3; i++)
            {
                var sessionId = _localNetworkManager.SimulateConnect();
                sessionIds.Add(sessionId);
            }

            // 广播消息
            var heartbeatResponse = HeartbeatResponse.Create();
            heartbeatResponse.Message = "broadcast_test";
            _localNetworkManager.BroadcastMessage(heartbeatResponse);

            // 验证所有客户端都收到了广播消息
            foreach (var sessionId in sessionIds)
            {
                var messages = _localNetworkManager.GetPendingMessages(sessionId);
                Assert.Contains(messages, msg => msg is HeartbeatResponse);
            }
        }

        [Fact]
        public async Task LocalNetworkManager_ShouldProvideMessageStatistics()
        {
            // 初始化
            await _localNetworkManager.InitializeAsync(8888);

            // 模拟客户端连接
            var sessionId = _localNetworkManager.SimulateConnect();

            // 发送多个消息
            for (int i = 0; i < 5; i++)
            {
                var heartbeatResponse = HeartbeatResponse.Create();
                heartbeatResponse.Message = $"test_{i}";
                _localNetworkManager.SendMessage(sessionId.ToString(), heartbeatResponse);
            }

            // 验证消息统计
            var stats = _localNetworkManager.GetPendingMessageStats();
            Assert.Contains(sessionId, stats.Keys);
            Assert.Equal(5, stats[sessionId]);
        }

        [Fact]
        public void NetworkManagerFactory_ShouldCreateCorrectManagers()
        {
            // 测试创建网络模式管理器
            var networkManager = NetworkManagerFactory.CreateNetwork();
            Assert.NotNull(networkManager);
            Assert.IsType<ServerNetworkManager>(networkManager);

            // 测试创建本地模式管理器
            var localManager = NetworkManagerFactory.CreateLocal();
            Assert.NotNull(localManager);
            Assert.IsType<LocalServerNetworkManager>(localManager);

            // 测试工厂模式创建
            var networkManager2 = NetworkManagerFactory.Create(NetworkManagerMode.Network);
            Assert.NotNull(networkManager2);
            Assert.IsType<ServerNetworkManager>(networkManager2);

            var localManager2 = NetworkManagerFactory.Create(NetworkManagerMode.Local);
            Assert.NotNull(localManager2);
            Assert.IsType<LocalServerNetworkManager>(localManager2);
        }

        [Fact]
        public void GameServer_ShouldWorkWithLocalNetworkManager()
        {
            // 验证游戏服务器可以正常使用本地网络管理器
            Assert.NotNull(_gameServer);
            Assert.NotNull(_localNetworkManager);
            Assert.NotNull(_userManager);
            Assert.NotNull(_roomManager);

            // 验证网络管理器类型
            Assert.IsType<LocalServerNetworkManager>(_localNetworkManager);
        }

        public void Dispose()
        {
            _localNetworkManager?.Shutdown();
            _output.WriteLine("🧹 网络管理器接口测试资源清理完成");
        }
    }
}
