using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NetworkTest.Models;

namespace NetworkTest
{
    /// <summary>
    /// 网络管理器 - 负责客户端与服务器的网络通信
    /// </summary>
    public class NetworkManager : IDisposable
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
                Console.WriteLine("NetworkManager: 初始化网络管理器");
            
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
                Console.WriteLine($"NetworkManager: 客户端ID - {clientId}");
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
                Console.WriteLine("NetworkManager: 已经连接到服务器");
                return true;
            }
            
            // 使用参数或默认值
            serverAddress = address ?? defaultServerAddress;
            this.port = port > 0 ? port : defaultPort;
            
            if (enableLogging)
                Console.WriteLine($"NetworkManager: 开始连接到服务器 {serverAddress}:{this.port}");
            
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
                    Console.WriteLine("NetworkManager: 连接成功");
                
                // 触发连接事件
                OnConnected?.Invoke();
                return true;
            }
            else
            {
                // 连接失败
                UpdateConnectionStatus(ConnectionStatus.FAILED);
                
                if (enableLogging)
                    Console.WriteLine("NetworkManager: 连接失败");
                
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
                Console.WriteLine("NetworkManager: 断开连接");
            
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
                Console.WriteLine("NetworkManager: 未连接到服务器，无法发送消息");
                return false;
            }
            
            if (message == null)
            {
                Console.WriteLine("NetworkManager: 消息为空");
                return false;
            }
            
            try
            {
                // 设置消息属性
                message.Timestamp = DateTime.Now;
                
                // 发送消息
                bool success = await networkClient.SendMessageAsync(message);
                
                if (success && enableLogging)
                    Console.WriteLine($"NetworkManager: 发送消息 {message.Type}");
                
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NetworkManager: 发送消息失败 - {ex.Message}");
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
                Console.WriteLine($"NetworkManager: 注册消息处理器 {messageType}");
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
                Console.WriteLine($"NetworkManager: 发送心跳失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 开始重连
        /// </summary>
        private void StartReconnect()
        {
            if (reconnectAttempts >= maxReconnectAttempts)
            {
                Console.WriteLine($"NetworkManager: 重连次数已达上限 {maxReconnectAttempts}");
                return;
            }
            
            reconnectAttempts++;
            
            if (enableLogging)
                Console.WriteLine($"NetworkManager: 开始第 {reconnectAttempts} 次重连");
            
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
                NetworkMessage message = null;
                
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
                    if (messageHandlers.TryGetValue(message.Type, out Action<NetworkMessage> handler))
                    {
                        try
                        {
                            handler?.Invoke(message);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"NetworkManager: 处理消息 {message.Type} 时发生异常 - {ex.Message}");
                        }
                    }
                    else
                    {
                        if (enableLogging)
                            Console.WriteLine($"NetworkManager: 未找到消息处理器 {message.Type}");
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
            Console.WriteLine($"NetworkManager: 客户端错误 - {error}");
        }
        
        /// <summary>
        /// 客户端断开回调
        /// </summary>
        private void OnClientDisconnected()
        {
            Console.WriteLine("NetworkManager: 客户端断开连接");
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
            return $"Client_{Guid.NewGuid():N}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        }
        
        /// <summary>
        /// 关闭网络管理器
        /// </summary>
        public void Shutdown()
        {
            if (enableLogging)
                Console.WriteLine("NetworkManager: 关闭网络管理器");
            
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
} 