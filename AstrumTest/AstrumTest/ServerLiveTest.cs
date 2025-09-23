using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using MemoryPack;
using Astrum.Generated;
using Astrum.CommonBase;

namespace AstrumTest
{
    /// <summary>
    /// 服务器实时测试 - 测试正在运行的服务器
    /// </summary>
    public class ServerLiveTest : IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly string _serverAddress = "127.0.0.1";
        private readonly int _serverPort = 8888;

        public void Dispose()
        {
            _stream?.Close();
            _client?.Close();
        }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        private async Task<bool> ConnectAsync()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_serverAddress, _serverPort);
                _stream = _client.GetStream();
                Console.WriteLine($"已连接到服务器 {_serverAddress}:{_serverPort}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接服务器失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送消息到服务器
        /// </summary>
        private async Task<bool> SendMessageAsync(MessageObject message)
        {
            try
            {
                if (_stream == null) return false;

                // 序列化消息
                var data = MemoryPackSerializer.Serialize(message);
                
                // 发送数据
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
                
                Console.WriteLine($"已发送消息: {message.GetType().Name}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 接收服务器响应
        /// </summary>
        private async Task<MessageObject?> ReceiveMessageAsync()
        {
            try
            {
                if (_stream == null) return null;

                // 读取响应
                var buffer = new byte[4096];
                var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                
                if (bytesRead > 0)
                {
                    var data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);
                    
                    // 尝试反序列化不同类型的响应
                    var responseTypes = new Type[]
                    {
                        typeof(LoginResponse),
                        typeof(CreateRoomResponse),
                        typeof(JoinRoomResponse),
                        typeof(LeaveRoomResponse),
                        typeof(GetRoomListResponse),
                        typeof(RoomUpdateNotification),
                        typeof(HeartbeatResponse)
                    };

                    foreach (var responseType in responseTypes)
                    {
                        try
                        {
                            var response = MemoryPackSerializer.Deserialize(responseType, data);
                            if (response != null)
                            {
                                Console.WriteLine($"收到响应: {response.GetType().Name}");
                                return response as MessageObject;
                            }
                        }
                        catch
                        {
                            // 继续尝试下一个类型
                        }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收消息失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 测试基本连接 - 跳过，需要服务器运行
        /// </summary>
        [Fact(Skip = "需要服务器运行")]
        public async Task TestBasicConnection()
        {
            // 连接服务器
            var connected = await ConnectAsync();
            Assert.True(connected, "应该能够连接到服务器");
            
            // 等待连接响应
            await Task.Delay(100);
            
            // 检查连接状态
            Assert.True(_client?.Connected == true, "客户端应该保持连接状态");
        }

        /// <summary>
        /// 测试登录流程 - 跳过，需要服务器运行
        /// </summary>
        [Fact(Skip = "需要服务器运行")]
        public async Task TestLoginFlow()
        {
            // 连接服务器
            var connected = await ConnectAsync();
            Assert.True(connected, "应该能够连接到服务器");
            
            // 等待连接响应
            await Task.Delay(100);
            
            // 发送登录请求
            var loginRequest = LoginRequest.Create();
            loginRequest.DisplayName = "测试用户_" + DateTime.Now.Ticks;
            
            var sent = await SendMessageAsync(loginRequest);
            Assert.True(sent, "应该能够发送登录请求");
            
            // 等待登录响应
            await Task.Delay(500);
            
            // 尝试接收响应
            var response = await ReceiveMessageAsync();
            Assert.NotNull(response);
            Assert.IsType<LoginResponse>(response);
            
            var loginResponse = response as LoginResponse;
            Assert.NotNull(loginResponse);
            Assert.True(loginResponse.Success, "登录应该成功");
            Assert.NotNull(loginResponse.User);
            
            Console.WriteLine($"登录成功，用户ID: {loginResponse.User.Id}");
        }

        /// <summary>
        /// 测试房间创建流程 - 跳过，需要服务器运行
        /// </summary>
        [Fact(Skip = "需要服务器运行")]
        public async Task TestCreateRoomFlow()
        {
            // 连接并登录
            var connected = await ConnectAsync();
            Assert.True(connected, "应该能够连接到服务器");
            
            await Task.Delay(100);
            
            var loginRequest = LoginRequest.Create();
            loginRequest.DisplayName = "房间创建者_" + DateTime.Now.Ticks;
            
            await SendMessageAsync(loginRequest);
            await Task.Delay(500);
            
            // 创建房间
            var createRoomRequest = CreateRoomRequest.Create();
            createRoomRequest.RoomName = "测试房间_" + DateTime.Now.Ticks;
            createRoomRequest.MaxPlayers = 4;
            createRoomRequest.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            var sent = await SendMessageAsync(createRoomRequest);
            Assert.True(sent, "应该能够发送创建房间请求");
            
            // 等待响应
            await Task.Delay(500);
            
            var response = await ReceiveMessageAsync();
            Assert.NotNull(response);
            Assert.IsType<CreateRoomResponse>(response);
            
            var createRoomResponse = response as CreateRoomResponse;
            Assert.NotNull(createRoomResponse);
            Assert.True(createRoomResponse.Success, "创建房间应该成功");
            Assert.NotNull(createRoomResponse.Room);
            
            Console.WriteLine($"创建房间成功，房间ID: {createRoomResponse.Room.Id}");
        }

        /// <summary>
        /// 测试获取房间列表
        /// </summary>
        [Fact]
        public async Task TestGetRoomList()
        {
            // 连接并登录
            var connected = await ConnectAsync();
            Assert.True(connected, "应该能够连接到服务器");
            
            await Task.Delay(100);
            
            var loginRequest = LoginRequest.Create();
            loginRequest.DisplayName = "列表查看者_" + DateTime.Now.Ticks;
            
            await SendMessageAsync(loginRequest);
            await Task.Delay(500);
            
            // 获取房间列表
            var getRoomListRequest = GetRoomListRequest.Create();
            getRoomListRequest.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            var sent = await SendMessageAsync(getRoomListRequest);
            Assert.True(sent, "应该能够发送获取房间列表请求");
            
            // 等待响应
            await Task.Delay(500);
            
            var response = await ReceiveMessageAsync();
            Assert.NotNull(response);
            Assert.IsType<GetRoomListResponse>(response);
            
            var getRoomListResponse = response as GetRoomListResponse;
            Assert.NotNull(getRoomListResponse);
            Assert.True(getRoomListResponse.Success, "获取房间列表应该成功");
            Assert.NotNull(getRoomListResponse.Rooms);
            
            Console.WriteLine($"获取房间列表成功，房间数量: {getRoomListResponse.Rooms.Count}");
        }

        /// <summary>
        /// 测试心跳消息
        /// </summary>
        [Fact]
        public async Task TestHeartbeat()
        {
            // 连接并登录
            var connected = await ConnectAsync();
            Assert.True(connected, "应该能够连接到服务器");
            
            await Task.Delay(100);
            
            var loginRequest = LoginRequest.Create();
            loginRequest.DisplayName = "心跳测试者_" + DateTime.Now.Ticks;
            
            await SendMessageAsync(loginRequest);
            await Task.Delay(500);
            
            // 发送心跳
            var heartbeatMessage = HeartbeatMessage.Create();
            heartbeatMessage.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            var sent = await SendMessageAsync(heartbeatMessage);
            Assert.True(sent, "应该能够发送心跳消息");
            
            // 等待响应
            await Task.Delay(500);
            
            var response = await ReceiveMessageAsync();
            Assert.NotNull(response);
            Assert.IsType<HeartbeatResponse>(response);
            
            var heartbeatResponse = response as HeartbeatResponse;
            Assert.NotNull(heartbeatResponse);
            Assert.NotNull(heartbeatResponse.Message);
            
            Console.WriteLine("心跳测试成功");
        }

        /// <summary>
        /// 测试完整流程
        /// </summary>
        [Fact]
        public async Task TestCompleteFlow()
        {
            Console.WriteLine("开始完整流程测试...");
            
            // 1. 连接服务器
            var connected = await ConnectAsync();
            Assert.True(connected, "应该能够连接到服务器");
            await Task.Delay(100);
            
            // 2. 登录
            var loginRequest = LoginRequest.Create();
            loginRequest.DisplayName = "完整测试用户_" + DateTime.Now.Ticks;
            
            await SendMessageAsync(loginRequest);
            await Task.Delay(500);
            
            var loginResponse = await ReceiveMessageAsync() as LoginResponse;
            Assert.NotNull(loginResponse);
            Assert.True(loginResponse.Success, "登录应该成功");
            
            Console.WriteLine($"步骤1: 登录成功 - 用户ID: {loginResponse.User.Id}");
            
            // 3. 创建房间
            var createRoomRequest = CreateRoomRequest.Create();
            createRoomRequest.RoomName = "完整测试房间_" + DateTime.Now.Ticks;
            createRoomRequest.MaxPlayers = 4;
            createRoomRequest.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            await SendMessageAsync(createRoomRequest);
            await Task.Delay(500);
            
            var createRoomResponse = await ReceiveMessageAsync() as CreateRoomResponse;
            Assert.NotNull(createRoomResponse);
            Assert.True(createRoomResponse.Success, "创建房间应该成功");
            
            Console.WriteLine($"步骤2: 创建房间成功 - 房间ID: {createRoomResponse.Room.Id}");
            
            // 4. 获取房间列表
            var getRoomListRequest = GetRoomListRequest.Create();
            getRoomListRequest.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            await SendMessageAsync(getRoomListRequest);
            await Task.Delay(500);
            
            var getRoomListResponse = await ReceiveMessageAsync() as GetRoomListResponse;
            Assert.NotNull(getRoomListResponse);
            Assert.True(getRoomListResponse.Success, "获取房间列表应该成功");
            
            Console.WriteLine($"步骤3: 获取房间列表成功 - 房间数量: {getRoomListResponse.Rooms.Count}");
            
            // 5. 发送心跳
            var heartbeatMessage = HeartbeatMessage.Create();
            heartbeatMessage.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            await SendMessageAsync(heartbeatMessage);
            await Task.Delay(500);
            
            var heartbeatResponse = await ReceiveMessageAsync() as HeartbeatResponse;
            Assert.NotNull(heartbeatResponse);
            Assert.NotNull(heartbeatResponse.Message);
            
            Console.WriteLine("步骤4: 心跳测试成功");
            
            Console.WriteLine("完整流程测试完成！");
        }
    }
}
