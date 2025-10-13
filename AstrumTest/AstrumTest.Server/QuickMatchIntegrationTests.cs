using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using AstrumServer.Core;
using AstrumServer.Network;
using AstrumServer.Managers;
using Astrum.Generated;
using Astrum.CommonBase;
using Astrum.Network;

namespace AstrumTest.Server
{
    /// <summary>
    /// 快速匹配系统集成测试
    /// 使用 LocalServerNetworkManager 在单进程内模拟完整的服务器逻辑
    /// </summary>
    [Trait("TestLevel", "Integration")]
    [Trait("Category", "Server")]
    [Trait("Module", "QuickMatch")]
    public class QuickMatchIntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly LocalServerNetworkManager _networkManager;
        private readonly UserManager _userManager;
        private readonly RoomManager _roomManager;
        private readonly MatchmakingManager _matchmakingManager;

        public QuickMatchIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            
            // 设置日志输出
            ASLogger.Instance.MinLevel = LogLevel.Debug;
            
            // 创建本地测试网络管理器
            _networkManager = new LocalServerNetworkManager();
            _networkManager.InitializeAsync().Wait();
            
            // 创建管理器（注意：目前先不创建 FrameSyncManager，它需要接口支持）
            _userManager = new UserManager();
            _roomManager = new RoomManager();
            _matchmakingManager = new MatchmakingManager(_roomManager, _userManager, _networkManager);
            
            // 订阅网络事件
            _networkManager.OnClientConnected += OnClientConnected;
            _networkManager.OnClientDisconnected += OnClientDisconnected;
            _networkManager.OnMessageReceived += OnMessageReceived;
        }

        public void Dispose()
        {
            _networkManager.Shutdown();
        }

        private void OnClientConnected(Session client)
        {
            _output.WriteLine($"[测试] 客户端连接: SessionId={client.Id}");
        }

        private void OnClientDisconnected(Session client)
        {
            _output.WriteLine($"[测试] 客户端断开: SessionId={client.Id}");
            
            var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
            if (userInfo != null)
            {
                _matchmakingManager.DequeuePlayer(userInfo.Id);
                _userManager.RemoveUser(client.Id.ToString());
            }
        }

        private void OnMessageReceived(Session client, MessageObject message)
        {
            _output.WriteLine($"[测试] 收到消息: SessionId={client.Id}, MessageType={message.GetType().Name}");
            
            switch (message)
            {
                case LoginRequest loginRequest:
                    HandleLoginRequest(client, loginRequest);
                    break;
                    
                case QuickMatchRequest quickMatchRequest:
                    HandleQuickMatchRequest(client, quickMatchRequest);
                    break;
                    
                case CancelMatchRequest cancelMatchRequest:
                    HandleCancelMatchRequest(client, cancelMatchRequest);
                    break;
            }
        }

        private void HandleLoginRequest(Session client, LoginRequest request)
        {
            var displayName = request.DisplayName;
            
            var userInfo = _userManager.AssignUserId(client.Id.ToString(), displayName);
            
            var response = LoginResponse.Create();
            response.Success = true;
            response.Message = "登录成功";
            response.User = userInfo;
            
            _networkManager.SendMessage(client.Id.ToString(), response);
            _output.WriteLine($"[测试] 处理登录: UserId={userInfo.Id}, DisplayName={displayName}");
        }

        private void HandleQuickMatchRequest(Session client, QuickMatchRequest request)
        {
            var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
            if (userInfo == null)
            {
                SendQuickMatchResponse(client.Id.ToString(), false, "用户未登录", 0, 0);
                return;
            }
            
            bool success = _matchmakingManager.EnqueuePlayer(userInfo.Id, userInfo.DisplayName);
            var (position, total) = _matchmakingManager.GetQueueInfo(userInfo.Id);
            
            string message = success ? $"已加入匹配队列，排队位置: {position + 1}/{total}" : "加入匹配队列失败";
            SendQuickMatchResponse(client.Id.ToString(), success, message, position, total);
        }

        private void SendQuickMatchResponse(string sessionId, bool success, string message, int position, int total)
        {
            var response = QuickMatchResponse.Create();
            response.Success = success;
            response.Message = message;
            response.Timestamp = TimeInfo.Instance.ClientNow();
            response.QueuePosition = position;
            response.QueueSize = total;
            
            _networkManager.SendMessage(sessionId, response);
        }

        private void HandleCancelMatchRequest(Session client, CancelMatchRequest request)
        {
            var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
            if (userInfo == null)
            {
                SendCancelMatchResponse(client.Id.ToString(), false, "用户未登录");
                return;
            }
            
            bool success = _matchmakingManager.DequeuePlayer(userInfo.Id);
            string message = success ? "已退出匹配队列" : "退出匹配队列失败";
            
            SendCancelMatchResponse(client.Id.ToString(), success, message);
        }

        private void SendCancelMatchResponse(string sessionId, bool success, string message)
        {
            var response = CancelMatchResponse.Create();
            response.Success = success;
            response.Message = message;
            response.Timestamp = TimeInfo.Instance.ClientNow();
            
            _networkManager.SendMessage(sessionId, response);
        }

        [Fact]
        public async Task Test_SinglePlayer_EnterQueue()
        {
            // Arrange
            var sessionId = _networkManager.SimulateConnect();
            
            // 发送登录请求
            var loginRequest = LoginRequest.Create();
            loginRequest.DisplayName = "Player1";
            _networkManager.SimulateReceive(sessionId, loginRequest);
            _networkManager.Update();
            
            // 清空登录响应
            _networkManager.ClearPendingMessages(sessionId);
            
            // Act - 发送快速匹配请求
            var quickMatchRequest = QuickMatchRequest.Create();
            quickMatchRequest.Timestamp = Astrum.CommonBase.TimeInfo.Instance.ClientNow();
            _networkManager.SimulateReceive(sessionId, quickMatchRequest);
            _networkManager.Update();
            
            // Assert
            var messages = _networkManager.GetPendingMessages(sessionId);
            Assert.Single(messages);
            
            var response = messages[0] as QuickMatchResponse;
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(0, response.QueuePosition);
            Assert.Equal(1, response.QueueSize);
            
            _output.WriteLine($"[测试通过] 单个玩家成功加入队列");
        }

        [Fact]
        public async Task Test_TwoPlayers_AutoMatch()
        {
            // Arrange - 模拟两个客户端连接
            var session1 = _networkManager.SimulateConnect();
            var session2 = _networkManager.SimulateConnect();
            
            // 玩家1登录
            var login1 = LoginRequest.Create();
            login1.DisplayName = "Player1";
            _networkManager.SimulateReceive(session1, login1);
            _networkManager.Update();
            _networkManager.ClearPendingMessages(session1);
            
            // 玩家2登录
            var login2 = LoginRequest.Create();
            login2.DisplayName = "Player2";
            _networkManager.SimulateReceive(session2, login2);
            _networkManager.Update();
            _networkManager.ClearPendingMessages(session2);
            
            // Act - 玩家1请求快速匹配
            var match1 = QuickMatchRequest.Create();
            match1.Timestamp = Astrum.CommonBase.TimeInfo.Instance.ClientNow();
            _networkManager.SimulateReceive(session1, match1);
            _networkManager.Update();
            _networkManager.ClearPendingMessages(session1);
            
            // 玩家2请求快速匹配
            var match2 = QuickMatchRequest.Create();
            match2.Timestamp = Astrum.CommonBase.TimeInfo.Instance.ClientNow();
            _networkManager.SimulateReceive(session2, match2);
            _networkManager.Update();
            
            // 触发匹配检查
            _matchmakingManager.Update();
            Thread.Sleep(100); // 等待匹配完成
            
            // Assert - 检查是否收到匹配成功通知
            var messages1 = _networkManager.GetPendingMessages(session1);
            var messages2 = _networkManager.GetPendingMessages(session2);
            
            _output.WriteLine($"[测试] 玩家1收到 {messages1.Count} 条消息");
            _output.WriteLine($"[测试] 玩家2收到 {messages2.Count} 条消息");
            
            // 两个玩家都应该收到匹配成功通知
            var matchFound1 = messages1.Find(m => m is MatchFoundNotification) as MatchFoundNotification;
            var matchFound2 = messages2.Find(m => m is MatchFoundNotification) as MatchFoundNotification;
            
            Assert.NotNull(matchFound1);
            Assert.NotNull(matchFound2);
            Assert.NotNull(matchFound1.Room);
            Assert.NotNull(matchFound2.Room);
            Assert.Equal(matchFound1.Room.Id, matchFound2.Room.Id);
            Assert.Equal(2, matchFound1.Room.CurrentPlayers);
            
            _output.WriteLine($"[测试通过] 两个玩家成功匹配，房间ID: {matchFound1.Room.Id}");
        }

        [Fact]
        public async Task Test_Player_CancelMatch()
        {
            // Arrange
            var sessionId = _networkManager.SimulateConnect();
            
            // 登录
            var loginRequest = LoginRequest.Create();
            loginRequest.DisplayName = "Player1";
            _networkManager.SimulateReceive(sessionId, loginRequest);
            _networkManager.Update();
            _networkManager.ClearPendingMessages(sessionId);
            
            // 加入匹配队列
            var matchRequest = QuickMatchRequest.Create();
            matchRequest.Timestamp = Astrum.CommonBase.TimeInfo.Instance.ClientNow();
            _networkManager.SimulateReceive(sessionId, matchRequest);
            _networkManager.Update();
            _networkManager.ClearPendingMessages(sessionId);
            
            // Act - 取消匹配
            var cancelRequest = CancelMatchRequest.Create();
            cancelRequest.Timestamp = Astrum.CommonBase.TimeInfo.Instance.ClientNow();
            _networkManager.SimulateReceive(sessionId, cancelRequest);
            _networkManager.Update();
            
            // Assert
            var messages = _networkManager.GetPendingMessages(sessionId);
            var cancelResponse = messages.Find(m => m is CancelMatchResponse) as CancelMatchResponse;
            
            Assert.NotNull(cancelResponse);
            Assert.True(cancelResponse.Success);
            
            // 检查队列信息
            var userInfo = _userManager.GetUserBySessionId(sessionId.ToString());
            var (position, total) = _matchmakingManager.GetQueueInfo(userInfo.Id);
            Assert.Equal(-1, position); // 不在队列中
            Assert.Equal(0, total);
            
            _output.WriteLine($"[测试通过] 玩家成功取消匹配");
        }

        [Fact]
        public async Task Test_ThreePlayers_OnlyTwoMatch()
        {
            // Arrange - 模拟三个客户端连接
            var session1 = _networkManager.SimulateConnect();
            var session2 = _networkManager.SimulateConnect();
            var session3 = _networkManager.SimulateConnect();
            
            // 登录三个玩家
            for (int i = 0; i < 3; i++)
            {
                var sessionId = i == 0 ? session1 : (i == 1 ? session2 : session3);
                var login = LoginRequest.Create();
                login.DisplayName = $"Player{i + 1}";
                _networkManager.SimulateReceive(sessionId, login);
                _networkManager.Update();
                _networkManager.ClearPendingMessages(sessionId);
            }
            
            // Act - 三个玩家依次请求快速匹配
            var match1 = QuickMatchRequest.Create();
            _networkManager.SimulateReceive(session1, match1);
            _networkManager.Update();
            _networkManager.ClearPendingMessages(session1);
            
            var match2 = QuickMatchRequest.Create();
            _networkManager.SimulateReceive(session2, match2);
            _networkManager.Update();
            _networkManager.ClearPendingMessages(session2);
            
            var match3 = QuickMatchRequest.Create();
            _networkManager.SimulateReceive(session3, match3);
            _networkManager.Update();
            _networkManager.ClearPendingMessages(session3);
            
            // 触发匹配
            _matchmakingManager.Update();
            Thread.Sleep(100);
            
            // Assert
            var messages1 = _networkManager.GetPendingMessages(session1);
            var messages2 = _networkManager.GetPendingMessages(session2);
            var messages3 = _networkManager.GetPendingMessages(session3);
            
            // 前两个玩家应该收到匹配成功通知
            var matchFound1 = messages1.Find(m => m is MatchFoundNotification);
            var matchFound2 = messages2.Find(m => m is MatchFoundNotification);
            var matchFound3 = messages3.Find(m => m is MatchFoundNotification);
            
            Assert.NotNull(matchFound1);
            Assert.NotNull(matchFound2);
            Assert.Null(matchFound3); // 第三个玩家还在等待
            
            // 检查第三个玩家是否还在队列中
            var user3 = _userManager.GetUserBySessionId(session3.ToString());
            var (position, total) = _matchmakingManager.GetQueueInfo(user3.Id);
            Assert.Equal(0, position); // 队列中第一个
            Assert.Equal(1, total); // 队列中只有他一个
            
            _output.WriteLine($"[测试通过] 前两个玩家成功匹配，第三个玩家继续等待");
        }
    }
}

