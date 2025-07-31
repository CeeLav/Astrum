using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NetworkTest.Models;

namespace NetworkTest
{
    public class NetworkClient : IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly string _serverAddress;
        private readonly int _serverPort;
        private bool _isConnected = false;
        private readonly object _lock = new();

        public event Action<NetworkMessage>? OnMessageReceived;
        public event Action<string>? OnError;
        public event Action? OnDisconnected;

        public bool IsConnected => _isConnected && _client?.Connected == true;

        public NetworkClient(string serverAddress = "127.0.0.1", int serverPort = 8888)
        {
            _serverAddress = serverAddress;
            _serverPort = serverPort;
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_serverAddress, _serverPort);
                _stream = _client.GetStream();
                _isConnected = true;

                // 启动接收消息的任务
                _ = Task.Run(ReceiveMessagesAsync);

                Console.WriteLine($"已连接到服务器 {_serverAddress}:{_serverPort}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接服务器失败: {ex.Message}");
                OnError?.Invoke($"连接失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendMessageAsync(NetworkMessage message)
        {
            if (!IsConnected)
            {
                OnError?.Invoke("未连接到服务器");
                return false;
            }

            try
            {
                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json + "\n");
                
                lock (_lock)
                {
                    _stream?.WriteAsync(bytes, 0, bytes.Length);
                }

                Console.WriteLine($"发送消息: {json}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");
                OnError?.Invoke($"发送失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LoginAsync(string displayName, string password)
        {
            var request = new LoginRequest { DisplayName = displayName };
            var message = NetworkMessage.CreateSuccess("login", request);
            return await SendMessageAsync(message);
        }

        public async Task<bool> CreateRoomAsync(string roomName, int maxPlayers = 4)
        {
            var request = new CreateRoomRequest { RoomName = roomName, MaxPlayers = maxPlayers };
            var message = NetworkMessage.CreateSuccess("create_room", request);
            return await SendMessageAsync(message);
        }

        public async Task<bool> JoinRoomAsync(string roomId)
        {
            var request = new JoinRoomRequest { RoomId = roomId };
            var message = NetworkMessage.CreateSuccess("join_room", request);
            return await SendMessageAsync(message);
        }

        public async Task<bool> LeaveRoomAsync(string roomId)
        {
            var request = new LeaveRoomRequest { RoomId = roomId };
            var message = NetworkMessage.CreateSuccess("leave_room", request);
            return await SendMessageAsync(message);
        }

        public async Task<bool> GetRoomsAsync()
        {
            var message = NetworkMessage.CreateSuccess("get_rooms", null);
            return await SendMessageAsync(message);
        }

        public async Task<bool> GetOnlineUsersAsync()
        {
            var message = NetworkMessage.CreateSuccess("get_online_users", null);
            return await SendMessageAsync(message);
        }

        public async Task<bool> PingAsync()
        {
            var message = NetworkMessage.CreateSuccess("ping", null);
            return await SendMessageAsync(message);
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[4096];
            var messageBuffer = new StringBuilder();

            try
            {
                while (IsConnected)
                {
                    var bytesRead = await _stream!.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // 服务器断开连接

                    var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuffer.Append(receivedData);

                    // 处理完整的消息（以换行符分隔）
                    var messages = messageBuffer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    messageBuffer.Clear();

                    // 保留最后一个不完整的消息
                    if (receivedData.EndsWith('\n'))
                    {
                        // 所有消息都是完整的
                    }
                    else if (messages.Length > 0)
                    {
                        // 最后一个消息可能不完整，保留在buffer中
                        messageBuffer.Append(messages[^1]);
                        messages = messages[..^1];
                    }

                    foreach (var messageJson in messages)
                    {
                        try
                        {
                            // 跳过非JSON消息（如欢迎消息）
                            if (!messageJson.Trim().StartsWith("{"))
                            {
                                Console.WriteLine($"收到非JSON消息: {messageJson.Trim()}");
                                continue;
                            }

                            var message = JsonSerializer.Deserialize<NetworkMessage>(messageJson);
                            if (message != null)
                            {
                                Console.WriteLine($"收到消息: {messageJson}");
                                OnMessageReceived?.Invoke(message);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"解析消息失败: {ex.Message}");
                            Console.WriteLine($"问题消息内容: {messageJson}");
                            OnError?.Invoke($"解析消息失败: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收消息时出错: {ex.Message}");
                OnError?.Invoke($"接收消息失败: {ex.Message}");
            }
            finally
            {
                _isConnected = false;
                OnDisconnected?.Invoke();
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
            Console.WriteLine("已断开连接");
        }

        public void Dispose()
        {
            Disconnect();
            _stream?.Dispose();
            _client?.Dispose();
        }
    }
} 