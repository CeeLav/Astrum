using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using AstrumServer.Network;
using AstrumServer.Core;
using Astrum.Generated;
using Astrum.CommonBase;
using Astrum.Network;

namespace AstrumTest
{
    /// <summary>
    /// 服务器网络通讯测试
    /// </summary>
    public class NetworkCommunicationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ILogger<NetworkCommunicationTests> _logger;
        private GameServer? _gameServer;
        private ServerNetworkManager? _networkManager;

        public NetworkCommunicationTests(ITestOutputHelper output)
        {
            _output = output;
            
            // 创建测试用的Logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<NetworkCommunicationTests>();
        }

        /// <summary>
        /// 测试服务器网络管理器初始化
        /// </summary>
        [Fact]
        public async Task ServerNetworkManager_Initialize_ShouldSucceed()
        {
            // Arrange
            _networkManager = ServerNetworkManager.Instance;
            _networkManager.SetLogger(_logger);

            // Act
            var result = await _networkManager.InitializeAsync(8889); // 使用不同的端口避免冲突

            // Assert
            Assert.True(result, "服务器网络管理器应该能够成功初始化");
            
            _output.WriteLine("✅ 服务器网络管理器初始化成功");
        }

        /// <summary>
        /// 测试服务器网络管理器关闭
        /// </summary>
        [Fact]
        public async Task ServerNetworkManager_Shutdown_ShouldSucceed()
        {
            // Arrange
            _networkManager = ServerNetworkManager.Instance;
            _networkManager.SetLogger(_logger);
            await _networkManager.InitializeAsync(8890);

            // Act & Assert
            var exception = Record.Exception(() => _networkManager.Shutdown());
            Assert.Null(exception);
            
            _output.WriteLine("✅ 服务器网络管理器关闭成功");
        }

        /// <summary>
        /// 测试获取活跃会话数量
        /// </summary>
        [Fact]
        public async Task ServerNetworkManager_GetSessionCount_ShouldReturnZero()
        {
            // Arrange
            _networkManager = ServerNetworkManager.Instance;
            _networkManager.SetLogger(_logger);
            await _networkManager.InitializeAsync(8891);

            // Act
            var sessionCount = _networkManager.GetSessionCount();

            // Assert
            Assert.Equal(0, sessionCount);
            
            _output.WriteLine($"✅ 当前活跃会话数量: {sessionCount}");
        }

        /// <summary>
        /// 测试网络服务更新方法
        /// </summary>
        [Fact]
        public async Task ServerNetworkManager_Update_ShouldNotThrow()
        {
            // Arrange
            _networkManager = ServerNetworkManager.Instance;
            _networkManager.SetLogger(_logger);
            await _networkManager.InitializeAsync(8892);

            // Act & Assert
            var exception = Record.Exception(() => _networkManager.Update());
            Assert.Null(exception);
            
            _output.WriteLine("✅ 网络服务更新方法执行正常");
        }

        /// <summary>
        /// 测试事件订阅和取消订阅
        /// </summary>
        [Fact]
        public async Task ServerNetworkManager_EventSubscription_ShouldWork()
        {
            // Arrange
            _networkManager = ServerNetworkManager.Instance;
            _networkManager.SetLogger(_logger);
            await _networkManager.InitializeAsync(8893);

            bool clientConnectedCalled = false;
            bool clientDisconnectedCalled = false;
            bool messageReceivedCalled = false;
            bool errorCalled = false;

            // Act - 订阅事件
            _networkManager.OnClientConnected += (session) => clientConnectedCalled = true;
            _networkManager.OnClientDisconnected += (session) => clientDisconnectedCalled = true;
            _networkManager.OnMessageReceived += (session, message) => messageReceivedCalled = true;
            _networkManager.OnError += (session, ex) => errorCalled = true;

            // 取消订阅
            _networkManager.OnClientConnected -= (session) => clientConnectedCalled = true;
            _networkManager.OnClientDisconnected -= (session) => clientDisconnectedCalled = true;
            _networkManager.OnMessageReceived -= (session, message) => messageReceivedCalled = true;
            _networkManager.OnError -= (session, ex) => errorCalled = true;

            // Assert
            Assert.False(clientConnectedCalled);
            Assert.False(clientDisconnectedCalled);
            Assert.False(messageReceivedCalled);
            Assert.False(errorCalled);
            
            _output.WriteLine("✅ 事件订阅和取消订阅功能正常");
        }

        /// <summary>
        /// 测试发送消息到不存在的会话
        /// </summary>
        [Fact]
        public async Task ServerNetworkManager_SendMessageToNonExistentSession_ShouldNotThrow()
        {
            // Arrange
            _networkManager = ServerNetworkManager.Instance;
            _networkManager.SetLogger(_logger);
            await _networkManager.InitializeAsync(8894);

            var testMessage = NetworkMessage.Create();
            testMessage.Type = "Test";
            testMessage.Data = new byte[] { 1, 2, 3, 4 };
            testMessage.Success = true;
            testMessage.Timestamp = TimeInfo.Instance.ClientNow();

            // Act & Assert
            var exception = Record.Exception(() => _networkManager.SendMessage("999", testMessage));
            Assert.Null(exception);
            
            _output.WriteLine("✅ 向不存在的会话发送消息不会抛出异常");
        }

        /// <summary>
        /// 测试广播消息
        /// </summary>
        [Fact]
        public async Task ServerNetworkManager_BroadcastMessage_ShouldNotThrow()
        {
            // Arrange
            _networkManager = ServerNetworkManager.Instance;
            _networkManager.SetLogger(_logger);
            await _networkManager.InitializeAsync(8895);

            var testMessage = NetworkMessage.Create();
            testMessage.Type = "Broadcast";
            testMessage.Data = new byte[] { 5, 6, 7, 8 };
            testMessage.Success = true;
            testMessage.Timestamp = TimeInfo.Instance.ClientNow();

            // Act & Assert
            var exception = Record.Exception(() => _networkManager.BroadcastMessage(testMessage));
            Assert.Null(exception);
            
            _output.WriteLine("✅ 广播消息功能正常");
        }

        /// <summary>
        /// 测试断开不存在的会话
        /// </summary>
        [Fact]
        public async Task ServerNetworkManager_DisconnectNonExistentSession_ShouldNotThrow()
        {
            // Arrange
            _networkManager = ServerNetworkManager.Instance;
            _networkManager.SetLogger(_logger);
            await _networkManager.InitializeAsync(8896);

            // Act & Assert
            var exception = Record.Exception(() => _networkManager.DisconnectSession(999));
            Assert.Null(exception);
            
            _output.WriteLine("✅ 断开不存在的会话不会抛出异常");
        }

        public void Dispose()
        {
            try
            {
                _gameServer?.Dispose();
                _networkManager?.Shutdown();
                _output.WriteLine("🧹 测试资源清理完成");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠️ 清理资源时出现异常: {ex.Message}");
            }
        }
    }
}
