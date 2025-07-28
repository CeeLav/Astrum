using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Astrum.Network
{
    /// <summary>
    /// 网络连接状�?
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Error
    }

    /// <summary>
    /// 网络管理�?- 与Unity解�?
    /// </summary>
    public class NetworkManager
    {
        private static NetworkManager _instance;
        public static NetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NetworkManager();
                }
                return _instance;
            }
        }

        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private readonly Dictionary<string, NetworkMessage> _pendingMessages = new();
        private readonly Queue<NetworkMessage> _messageQueue = new();
        private readonly object _queueLock = new();

        /// <summary>
        /// 连接状�?
        /// </summary>
        public ConnectionState ConnectionState
        {
            get => _connectionState;
            private set
            {
                if (_connectionState != value)
                {
                    var previousState = _connectionState;
                    _connectionState = value;
                    OnConnectionStateChanged?.Invoke(previousState, _connectionState);
                }
            }
        }

        /// <summary>
        /// 是否已连�?
        /// </summary>
        public bool IsConnected => ConnectionState == ConnectionState.Connected;

        /// <summary>
        /// 连接状态改变事�?
        /// </summary>
        public event Action<ConnectionState, ConnectionState> OnConnectionStateChanged;

        /// <summary>
        /// 消息接收事件
        /// </summary>
        public event Action<NetworkMessage> OnMessageReceived;

        /// <summary>
        /// 消息发送事�?
        /// </summary>
        public event Action<NetworkMessage> OnMessageSent;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event Action<string> OnError;

        private NetworkManager()
        {
        }

        /// <summary>
        /// 初始化网络管理器
        /// </summary>
        public void Initialize()
        {
            ConnectionState = ConnectionState.Disconnected;
            _pendingMessages.Clear();
            _messageQueue.Clear();
        }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public async Task<bool> ConnectAsync(string serverAddress, int port)
        {
            try
            {
                ConnectionState = ConnectionState.Connecting;
                
                // 这里应该实现实际的网络连接逻辑
                // 为了演示，我们模拟连接过�?
                await Task.Delay(1000);
                
                ConnectionState = ConnectionState.Connected;
                
                // 发送连接消�?
                var connectMessage = new ConnectMessage
                {
                    PlayerName = "Player",
                    Version = "1.0.0"
                };
                
                await SendMessageAsync(connectMessage);
                
                return true;
            }
            catch (Exception ex)
            {
                ConnectionState = ConnectionState.Error;
                OnError?.Invoke($"连接失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public async Task DisconnectAsync(string reason = "User disconnected")
        {
            if (IsConnected)
            {
                var disconnectMessage = new DisconnectMessage
                {
                    Reason = reason
                };
                
                await SendMessageAsync(disconnectMessage);
            }
            
            ConnectionState = ConnectionState.Disconnected;
        }

        /// <summary>
        /// 发送消�?
        /// </summary>
        public async Task<bool> SendMessageAsync(NetworkMessage message)
        {
            if (!IsConnected)
            {
                OnError?.Invoke("未连接到服务器");
                return false;
            }

            try
            {
                // 添加到待发送队列
                lock (_queueLock)
                {
                    _messageQueue.Enqueue(message);
                }

                // 这里应该实现实际的网络发送逻辑
                // 为了演示，我们模拟发送过程
                await Task.Delay(10);
                
                OnMessageSent?.Invoke(message);
                
                // 添加到待确认消息列表
                _pendingMessages[message.MessageId] = message;
                
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"发送消息失�? {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        public void ReceiveMessage(NetworkMessage message)
        {
            try
            {
                // 处理消息
                ProcessMessage(message);
                
                // 触发接收事件
                OnMessageReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"处理消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        private void ProcessMessage(NetworkMessage message)
        {
            switch (message.Type)
            {
                case MessageType.Connect:
                    HandleConnectMessage((ConnectMessage)message);
                    break;
                    
                case MessageType.Disconnect:
                    HandleDisconnectMessage((DisconnectMessage)message);
                    break;
                    
                case MessageType.Heartbeat:
                    HandleHeartbeatMessage((HeartbeatMessage)message);
                    break;
                    
                case MessageType.Data:
                    HandleDataMessage((DataMessage)message);
                    break;
                    
                case MessageType.Error:
                    HandleErrorMessage((ErrorMessage)message);
                    break;
                    
                case MessageType.Info:
                    HandleInfoMessage((InfoMessage)message);
                    break;
                    
                default:
                    OnError?.Invoke($"未知消息类型: {message.Type}");
                    break;
            }
        }

        private void HandleConnectMessage(ConnectMessage message)
        {
            // 处理连接消息
            Console.WriteLine($"客户�?{message.PlayerName} 连接到服务器");
        }

        private void HandleDisconnectMessage(DisconnectMessage message)
        {
            // 处理断开连接消息
            Console.WriteLine($"客户端断开连接: {message.Reason}");
        }

        private void HandleHeartbeatMessage(HeartbeatMessage message)
        {
            // 处理心跳消息
            if (message != null)
            {
                // 处理心跳逻辑
            }
        }

        private void HandleDataMessage(DataMessage message)
        {
            // 处理通用数据消息
            Console.WriteLine($"收到数据类型: {message.DataType}");
            // 这里可以触发数据接收事件，让上层应用处理具体逻辑
        }

        private void HandleErrorMessage(ErrorMessage message)
        {
            // 处理错误消息
            OnError?.Invoke($"服务器错�? {message.ErrorText}");
        }

        private void HandleInfoMessage(InfoMessage message)
        {
            // 处理信息消息
            Console.WriteLine($"服务器信�? {message.Info}");
        }

        /// <summary>
        /// 更新网络管理�?
        /// </summary>
        public void Update()
        {
            // 处理消息队列
            lock (_queueLock)
            {
                while (_messageQueue.Count > 0)
                {
                    var message = _messageQueue.Dequeue();
                    ProcessMessage(message);
                }
            }

            // 清理超时的待确认消息
            var currentTime = DateTime.Now;
            var expiredMessages = new List<string>();
            
            foreach (var kvp in _pendingMessages)
            {
                if ((currentTime - kvp.Value.Timestamp).TotalSeconds > 30)
                {
                    expiredMessages.Add(kvp.Key);
                }
            }
            
            foreach (var messageId in expiredMessages)
            {
                _pendingMessages.Remove(messageId);
            }
        }

        /// <summary>
        /// 发送心�?
        /// </summary>
        public async Task SendHeartbeatAsync()
        {
            if (IsConnected)
            {
                var heartbeat = new HeartbeatMessage();
                await SendMessageAsync(heartbeat);
            }
        }
    }
}
