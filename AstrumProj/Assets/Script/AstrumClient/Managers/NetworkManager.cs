using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Astrum.CommonBase;
using IDisposable = System.IDisposable;

namespace Astrum.Client.Managers
{
    /// <summary>
    /// 网络管理器 - 负责客户端与服务器的网络通信
    /// </summary>
    public class NetworkManager : Singleton<NetworkManager>
    {
        private bool enableLogging = true;
        private string defaultServerAddress = "127.0.0.1";
        private int defaultPort = 8888;
        private int heartbeatInterval = 5000; // 5秒
        private int reconnectInterval = 3000; // 3秒
        private int maxReconnectAttempts = 5;
        
        // 网络连接状态
        private bool isConnected = false;
        private string serverAddress = string.Empty;
        private int port;
        private string clientId = string.Empty;
        private ConnectionStatus connectionStatus = ConnectionStatus.DISCONNECTED;
        
        // 网络客户端
        private NetworkClient networkClient = null!;
        private CancellationTokenSource? heartbeatCancellation;
        private CancellationTokenSource? reconnectCancellation;
        private int reconnectAttempts = 0;
        
        // 消息处理
        private Dictionary<string, Action<NetworkMessage?>> messageHandlers = new Dictionary<string, Action<NetworkMessage?>>();
        private Queue<NetworkMessage> messageQueue = new Queue<NetworkMessage>();
        private readonly object messageQueueLock = new object();
        
        // 公共属性
        public bool IsConnected => isConnected;
        public string ServerAddress => serverAddress;
        public int Port => port;
        public string ClientId => clientId;
        public ConnectionStatus ConnectionStatus => connectionStatus;
        
        // 事件
        public event Action? OnConnected;
        public event Action? OnDisconnected;
        public event Action<ConnectionStatus>? OnConnectionStatusChanged;
        public event Action<NetworkMessage?>? OnMessageReceived;
        
        public NetworkManager()
        {
            Initialize();
        }
        
        /// <summary>
        /// 初始化网络管理器
        /// </summary>
        public void Initialize()
        {
            if (enableLogging)
                Debug.Log("NetworkManager: 初始化网络管理器");
            
            // 设置默认连接参数
            serverAddress = defaultServerAddress;
            port = defaultPort;
            
            // 生成客户端ID
            clientId = GenerateClientId();
            
            // 初始化网络客户端
            networkClient = new NetworkClient();
            networkClient.OnMessageReceived += OnClientMessageReceived;
            networkClient.OnError += OnClientError;
            networkClient.OnDisconnected += OnClientDisconnected;
            
            if (enableLogging)
                Debug.Log($"NetworkManager: 客户端ID - {clientId}");
        }
        
        /// <summary>
        /// 连接到服务器
        /// </summary>
        /// <param name="address">服务器地址</param>
        /// <param name="port">服务器端口</param>
        public async Task<bool> ConnectAsync(string? address = null, int port = 0)
        {
            if (isConnected)
            {
                Debug.LogWarning("NetworkManager: 已经连接到服务器");
                return true;
            }
            
            // 使用参数或默认值
            serverAddress = address ?? defaultServerAddress;
            this.port = port > 0 ? port : defaultPort;
            
            if (enableLogging)
                Debug.Log($"NetworkManager: 开始连接到服务器 {serverAddress}:{this.port}");
            
            // 更新连接状态
            UpdateConnectionStatus(ConnectionStatus.CONNECTING);
            
            // 开始连接
            bool connected = await networkClient.ConnectAsync();
            
            if (connected)
            {
                // 连接成功
                isConnected = true;
                reconnectAttempts = 0;
                UpdateConnectionStatus(ConnectionStatus.CONNECTED);
                
                // 启动心跳
                StartHeartbeat();
                
                // 启动消息处理
                _ = Task.Run(ProcessMessagesAsync);
                
                if (enableLogging)
                    Debug.Log("NetworkManager: 连接成功");
                
                // 触发连接事件
                OnConnected?.Invoke();
                return true;
            }
            else
            {
                // 连接失败
                UpdateConnectionStatus(ConnectionStatus.FAILED);
                
                if (enableLogging)
                    Debug.LogError("NetworkManager: 连接失败");
                
                // 尝试重连
                StartReconnect();
                return false;
            }
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (!isConnected)
            {
                return;
            }
            
            if (enableLogging)
                Debug.Log("NetworkManager: 断开连接");
            
            // 停止心跳和重连
            StopHeartbeat();
            StopReconnect();
            
            // 断开网络连接
            networkClient?.Disconnect();
            
            // 更新状态
            isConnected = false;
            UpdateConnectionStatus(ConnectionStatus.DISCONNECTED);
            
            // 清理消息队列
            lock (messageQueueLock)
            {
                messageQueue.Clear();
            }
            
            // 触发断开事件
            OnDisconnected?.Invoke();
        }
        
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">网络消息</param>
        public async Task<bool> SendMessageAsync(NetworkMessage? message)
        {
            if (!isConnected)
            {
                Debug.LogWarning("NetworkManager: 未连接到服务器，无法发送消息");
                return false;
            }
            
            if (message == null)
            {
                Debug.LogError("NetworkManager: 消息为空");
                return false;
            }
            
            try
            {
                // 设置消息属性
                message.Timestamp = DateTime.Now;
                
                // 发送消息
                bool success = await networkClient.SendMessageAsync(message);
                
                if (success && enableLogging)
                    Debug.Log($"NetworkManager: 发送消息 {message.Type}");
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkManager: 发送消息失败 - {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <param name="handler">处理器</param>
        public void RegisterHandler(string messageType, Action<NetworkMessage?> handler)
        {
            if (messageHandlers.ContainsKey(messageType))
            {
                messageHandlers[messageType] = handler;
            }
            else
            {
                messageHandlers.Add(messageType, handler);
            }
            
            if (enableLogging)
                Debug.Log($"NetworkManager: 注册消息处理器 {messageType}");
        }
        
        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <returns>连接状态</returns>
        public ConnectionStatus GetConnectionStatus()
        {
            return connectionStatus;
        }
        
        /// <summary>
        /// 开始心跳
        /// </summary>
        private void StartHeartbeat()
        {
            heartbeatCancellation?.Cancel();
            heartbeatCancellation = new CancellationTokenSource();
            
            _ = Task.Run(async () =>
            {
                while (!heartbeatCancellation.Token.IsCancellationRequested && isConnected)
                {
                    await Task.Delay(heartbeatInterval, heartbeatCancellation.Token);
                    
                    if (isConnected && !heartbeatCancellation.Token.IsCancellationRequested)
                    {
                        await SendHeartbeatAsync();
                    }
                }
            }, heartbeatCancellation.Token);
        }
        
        /// <summary>
        /// 停止心跳
        /// </summary>
        private void StopHeartbeat()
        {
            heartbeatCancellation?.Cancel();
            heartbeatCancellation?.Dispose();
            heartbeatCancellation = null;
        }
        
        /// <summary>
        /// 发送心跳
        /// </summary>
        private async Task SendHeartbeatAsync()
        {
            try
            {
                // 创建心跳消息
                var heartbeatMessage = NetworkMessage.CreateSuccess("ping", new { clientId = clientId });
                
                await SendMessageAsync(heartbeatMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkManager: 发送心跳失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 开始重连
        /// </summary>
        private void StartReconnect()
        {
            if (reconnectAttempts >= maxReconnectAttempts)
            {
                Debug.LogError($"NetworkManager: 重连次数已达上限 {maxReconnectAttempts}");
                return;
            }
            
            reconnectAttempts++;
            
            if (enableLogging)
                Debug.Log($"NetworkManager: 开始第 {reconnectAttempts} 次重连");
            
            UpdateConnectionStatus(ConnectionStatus.RECONNECTING);
            
            reconnectCancellation?.Cancel();
            reconnectCancellation = new CancellationTokenSource();
            
            _ = Task.Run(async () =>
            {
                await Task.Delay(reconnectInterval, reconnectCancellation.Token);
                
                if (!reconnectCancellation.Token.IsCancellationRequested)
                {
                    // 尝试重新连接
                    await ConnectAsync(serverAddress, port);
                }
            }, reconnectCancellation.Token);
        }
        
        /// <summary>
        /// 停止重连
        /// </summary>
        private void StopReconnect()
        {
            reconnectCancellation?.Cancel();
            reconnectCancellation?.Dispose();
            reconnectCancellation = null;
            reconnectAttempts = 0;
        }
        
        /// <summary>
        /// 处理消息异步
        /// </summary>
        private async Task ProcessMessagesAsync()
        {
            while (isConnected)
            {
                NetworkMessage? message = null;
                
                lock (messageQueueLock)
                {
                    if (messageQueue.Count > 0)
                    {
                        message = messageQueue.Dequeue();
                    }
                }
                
                if (message != null)
                {
                    // 触发消息接收事件
                    OnMessageReceived?.Invoke(message);
                    
                    // 调用注册的处理器
                    if (messageHandlers.TryGetValue(message.Type, out Action<NetworkMessage?> handler))
                    {
                        try
                        {
                            handler?.Invoke(message);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"NetworkManager: 处理消息 {message.Type} 时发生异常 - {ex.Message}");
                        }
                    }
                    else
                    {
                        if (enableLogging)
                            Debug.LogWarning($"NetworkManager: 未找到消息处理器 {message.Type}");
                    }
                }
                
                await Task.Delay(10); // 短暂延迟避免CPU占用过高
            }
        }
        
        /// <summary>
        /// 客户端消息接收回调
        /// </summary>
        private void OnClientMessageReceived(NetworkMessage? message)
        {
            if (message != null)
            {
                lock (messageQueueLock)
                {
                    messageQueue.Enqueue(message);
                }
            }
        }
        
        /// <summary>
        /// 客户端错误回调
        /// </summary>
        private void OnClientError(string? error)
        {
            Debug.LogError($"NetworkManager: 客户端错误 - {error}");
        }
        
        /// <summary>
        /// 客户端断开回调
        /// </summary>
        private void OnClientDisconnected()
        {
            Debug.Log("NetworkManager: 客户端断开连接");
            isConnected = false;
            UpdateConnectionStatus(ConnectionStatus.DISCONNECTED);
            OnDisconnected?.Invoke();
        }
        
        /// <summary>
        /// 更新连接状态
        /// </summary>
        /// <param name="status">新状态</param>
        private void UpdateConnectionStatus(ConnectionStatus status)
        {
            if (connectionStatus != status)
            {
                connectionStatus = status;
                OnConnectionStatusChanged?.Invoke(status);
            }
        }
        
        /// <summary>
        /// 生成客户端ID
        /// </summary>
        /// <returns>客户端ID</returns>
        private string GenerateClientId()
        {
            return $"Client_{SystemInfo.deviceUniqueIdentifier}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        }
        
        /// <summary>
        /// 关闭网络管理器
        /// </summary>
        public void Shutdown()
        {
            if (enableLogging)
                Debug.Log("NetworkManager: 关闭网络管理器");
            
            // 断开连接
            Disconnect();
            
            // 清理处理器
            messageHandlers.Clear();
            lock (messageQueueLock)
            {
                messageQueue.Clear();
            }
            
            // 清理网络客户端
            networkClient?.Dispose();
            networkClient = null;
        }
        
        public void Dispose()
        {
            Shutdown();
            heartbeatCancellation?.Dispose();
            reconnectCancellation?.Dispose();
        }
    }
    
    /// <summary>
    /// 连接状态枚举
    /// </summary>
    public enum ConnectionStatus
    {
        DISCONNECTED,   // 未连接
        CONNECTING,     // 连接中
        CONNECTED,      // 已连接
        RECONNECTING,   // 重连中
        FAILED          // 连接失败
    }
    
    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageType
    {
        UNKNOWN,
        GAME_STATE,
        PLAYER_INPUT,
        SERVER_EVENT,
        HEARTBEAT
    }
    
    /// <summary>
    /// 网络消息类
    /// </summary>
    [Serializable]
    public class NetworkMessage
    {
        public string Type { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; } = true;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public NetworkMessage()
        {
        }

        public NetworkMessage(string type, object? data = null)
        {
            Type = type;
            Data = data;
        }

        public static NetworkMessage CreateSuccess(string type, object? data = null)
        {
            return new NetworkMessage(type, data) { Success = true };
        }

        public static NetworkMessage CreateError(string type, string error)
        {
            return new NetworkMessage(type) { Success = false, Error = error };
        }
    }

    [Serializable]
    public class LoginRequest
    {
        public string? DisplayName { get; set; }
    }

    [Serializable]
    public class CreateRoomRequest
    {
        public string RoomName { get; set; } = string.Empty;
        public int MaxPlayers { get; set; } = 4;
    }

    [Serializable]
    public class JoinRoomRequest
    {
        public string RoomId { get; set; } = string.Empty;
    }

    [Serializable]
    public class LeaveRoomRequest
    {
        public string RoomId { get; set; } = string.Empty;
    }

    [Serializable]
    public class RoomInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CreatorName { get; set; } = string.Empty;
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> PlayerNames { get; set; } = new();
    }

    [Serializable]
    public class UserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime LastLoginAt { get; set; }
        public string? CurrentRoomId { get; set; }
    }
    
    /// <summary>
    /// 网络客户端
    /// </summary>
    public class NetworkClient : IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly string _serverAddress;
        private readonly int _serverPort;
        private bool _isConnected = false;
        private readonly object _lock = new();

        public event Action<NetworkMessage?>? OnMessageReceived;
        public event Action<string?>? OnError;
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

                Debug.Log($"已连接到服务器 {_serverAddress}:{_serverPort}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"连接服务器失败: {ex.Message}");
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
                // 手动构建JSON字符串，避免Unity JsonUtility对object?的限制
                var jsonBuilder = new StringBuilder();
                jsonBuilder.Append("{");
                jsonBuilder.Append($"\"type\":\"{message.Type}\",");
                jsonBuilder.Append($"\"success\":{message.Success.ToString().ToLower()},");
                jsonBuilder.Append($"\"timestamp\":\"{message.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}\"");
                
                if (message.Data != null)
                {
                    // 尝试序列化Data字段
                    string dataJson = "";
                    try
                    {
                        dataJson = JsonUtility.ToJson(message.Data);
                    }
                    catch
                    {
                        // 如果序列化失败，使用字符串表示
                        dataJson = $"\"{message.Data}\"";
                    }
                    jsonBuilder.Append($",\"data\":{dataJson}");
                }
                else
                {
                    jsonBuilder.Append(",\"data\":null");
                }
                
                if (!string.IsNullOrEmpty(message.Error))
                {
                    jsonBuilder.Append($",\"error\":\"{message.Error}\"");
                }
                else
                {
                    jsonBuilder.Append(",\"error\":null");
                }
                
                jsonBuilder.Append("}");
                
                var json = jsonBuilder.ToString();
                var bytes = Encoding.UTF8.GetBytes(json + "\n");
                
                // 调试：输出实际发送的JSON
                Debug.Log($"发送JSON: {json}");
                
                lock (_lock)
                {
                    _stream?.WriteAsync(bytes, 0, bytes.Length);
                }

                Debug.Log($"发送消息: {message.Type}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"发送消息失败: {ex.Message}");
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
                                Debug.Log($"收到非JSON消息: {messageJson.Trim()}");
                                continue;
                            }

                            // 使用简单的字符串解析，避免Unity JsonUtility的限制
                            var message = ParseNetworkMessage(messageJson);
                            if (message != null)
                            {
                                Debug.Log($"收到消息: {message.Type}");
                                OnMessageReceived?.Invoke(message);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"解析消息失败: {ex.Message}");
                            Debug.LogError($"问题消息内容: {messageJson}");
                            OnError?.Invoke($"解析消息失败: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"接收消息时出错: {ex.Message}");
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
            Debug.Log("已断开连接");
        }

        private NetworkMessage? ParseNetworkMessage(string json)
        {
            try
            {
                // 简单的JSON解析，适用于Unity
                var message = new NetworkMessage();
                
                // 提取基本字段
                if (json.Contains("\"type\":"))
                {
                    var typeStart = json.IndexOf("\"type\":") + 7;
                    var typeEnd = json.IndexOf("\"", typeStart + 1);
                    if (typeEnd > typeStart)
                    {
                        message.Type = json.Substring(typeStart, typeEnd - typeStart).Trim('"');
                    }
                }
                
                if (json.Contains("\"success\":"))
                {
                    var successStart = json.IndexOf("\"success\":") + 10;
                    var successEnd = json.IndexOf(",", successStart);
                    if (successEnd == -1) successEnd = json.IndexOf("}", successStart);
                    if (successEnd > successStart)
                    {
                        var successStr = json.Substring(successStart, successEnd - successStart).Trim();
                        message.Success = successStr == "true";
                    }
                }
                
                if (json.Contains("\"error\":"))
                {
                    var errorStart = json.IndexOf("\"error\":") + 8;
                    var errorEnd = json.IndexOf("\"", errorStart + 1);
                    if (errorEnd > errorStart)
                    {
                        message.Error = json.Substring(errorStart, errorEnd - errorStart).Trim('"');
                    }
                }
                
                return message;
            }
            catch
            {
                return null;
            }
        }
        
        public void Dispose()
        {
            Disconnect();
            _stream?.Dispose();
            _client?.Dispose();
        }
    }
} 
