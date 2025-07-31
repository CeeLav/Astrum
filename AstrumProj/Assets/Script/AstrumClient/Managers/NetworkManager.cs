using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Astrum.CommonBase;

namespace Astrum.Client.Managers
{
    /// <summary>
    /// 网络管理�?- 负责客户端与服务器的网络通信
    /// </summary>
    public class NetworkManager : Singleton<NetworkManager>
    {
        [Header("网络设置")]
        private bool enableLogging = true;
        private string defaultServerAddress = "127.0.0.1";
        private int defaultPort = 8888;
        private float heartbeatInterval = 5f;
        private float reconnectInterval = 3f;
        private int maxReconnectAttempts = 5;
        
        // 网络连接状�?
        private bool isConnected = false;
        private string serverAddress;
        private int port;
        private string clientId;
        private float pingTime = 0f;
        private ConnectionStatus connectionStatus = ConnectionStatus.DISCONNECTED;
        
        // 网络客户�?
        private NetworkClient networkClient;
        private Coroutine heartbeatCoroutine;
        private Coroutine reconnectCoroutine;
        private int reconnectAttempts = 0;
        private MonoBehaviour coroutineRunner;
        
        // 消息处理
        private Dictionary<MessageType, Action<NetworkMessage>> messageHandlers = new Dictionary<MessageType, Action<NetworkMessage>>();
        private Queue<NetworkMessage> messageQueue = new Queue<NetworkMessage>();
        
        // 公共属�?
        public bool IsConnected => isConnected;
        public string ServerAddress => serverAddress;
        public int Port => port;
        public string ClientId => clientId;
        public float PingTime => pingTime;
        public ConnectionStatus ConnectionStatus => connectionStatus;
        
        // 事件
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<ConnectionStatus> OnConnectionStatusChanged;
        public event Action<NetworkMessage> OnMessageReceived;
        
        protected void Start()
        {
            // 创建协程运行器
            CreateCoroutineRunner();
        }
        
        /// <summary>
        /// 创建协程运行�?
        /// </summary>
        private void CreateCoroutineRunner()
        {
            GameObject runnerGO = new GameObject("NetworkManager Coroutine Runner");
            coroutineRunner = runnerGO.AddComponent<MonoBehaviour>();
            UnityEngine.Object.DontDestroyOnLoad(runnerGO);
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
            
            if (enableLogging)
                Debug.Log($"NetworkManager: 客户端ID - {clientId}");
        }
        
        /// <summary>
        /// 连接到服务器
        /// </summary>
        /// <param name="address">服务器地址</param>
        /// <param name="port">服务器端�?/param>
        public void Connect(string address = null, int port = 0)
        {
            if (isConnected)
            {
                Debug.LogWarning("NetworkManager: 已经连接到服务器");
                return;
            }
            
            // 使用参数或默认�?
            serverAddress = address ?? defaultServerAddress;
            this.port = port > 0 ? port : defaultPort;
            
            if (enableLogging)
                Debug.Log($"NetworkManager: 开始连接到服务�?{serverAddress}:{this.port}");
            
            // 更新连接状�?
            UpdateConnectionStatus(ConnectionStatus.CONNECTING);
            
            // 开始连�?
            coroutineRunner.StartCoroutine(ConnectCoroutine());
        }
        
        /// <summary>
        /// 连接协程
        /// </summary>
        private IEnumerator ConnectCoroutine()
        {
            try
            {
                // 尝试连接
                bool connected = networkClient.Connect(serverAddress, port);
                
                if (connected)
                {
                    // 连接成功
                    isConnected = true;
                    reconnectAttempts = 0;
                    UpdateConnectionStatus(ConnectionStatus.CONNECTED);
                    
                    // 启动心跳
                    StartHeartbeat();
                    
                    // 启动消息处理
                    coroutineRunner.StartCoroutine(ProcessMessagesCoroutine());
                    
                    if (enableLogging)
                        Debug.Log("NetworkManager: 连接成功");
                    
                    // 触发连接事件
                    OnConnected?.Invoke();
                }
                else
                {
                    // 连接失败
                    UpdateConnectionStatus(ConnectionStatus.FAILED);
                    
                    if (enableLogging)
                        Debug.LogError("NetworkManager: 连接失败");
                    
                    // 尝试重连
                    StartReconnect();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkManager: 连接时发生异�?- {ex.Message}");
                UpdateConnectionStatus(ConnectionStatus.FAILED);
                StartReconnect();
            }
            
            yield return null;
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
            
            // 停止心跳和重�?
            StopHeartbeat();
            StopReconnect();
            
            // 断开网络连接
            networkClient?.Disconnect();
            
            // 更新状�?
            isConnected = false;
            UpdateConnectionStatus(ConnectionStatus.DISCONNECTED);
            
            // 清理消息队列
            messageQueue.Clear();
            
            // 触发断开事件
            OnDisconnected?.Invoke();
        }
        
        /// <summary>
        /// 发送消�?
        /// </summary>
        /// <param name="message">网络消息</param>
        public void SendMessage(NetworkMessage message)
        {
            if (!isConnected)
            {
                Debug.LogWarning("NetworkManager: 未连接到服务器，无法发送消息");
                return;
            }
            
            if (message == null)
            {
                Debug.LogError("NetworkManager: 消息为空");
                return;
            }
            
            try
            {
                // 设置消息属�?
                message.SenderId = clientId;
                message.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                // 序列化并发�?
                byte[] data = message.Serialize();
                networkClient.Send(data);
                
                if (enableLogging)
                    Debug.Log($"NetworkManager: 发送消�?{message.MessageType}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkManager: 发送消息失�?- {ex.Message}");
            }
        }
        
        /// <summary>
        /// 注册消息处理�?
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="handler">处理�?/param>
        public void RegisterHandler<T>(Action<T> handler) where T : NetworkMessage
        {
            MessageType messageType = GetMessageType<T>();
            
            if (messageHandlers.ContainsKey(messageType))
            {
                messageHandlers[messageType] = (msg) => handler?.Invoke(msg as T);
            }
            else
            {
                messageHandlers.Add(messageType, (msg) => handler?.Invoke(msg as T));
            }
            
            if (enableLogging)
                Debug.Log($"NetworkManager: 注册消息处理�?{messageType}");
        }
        
        /// <summary>
        /// 获取连接状�?
        /// </summary>
        /// <returns>连接状�?/returns>
        public ConnectionStatus GetConnectionStatus()
        {
            return connectionStatus;
        }
        
        /// <summary>
        /// 开始心�?
        /// </summary>
        private void StartHeartbeat()
        {
            if (heartbeatCoroutine != null)
            {
                coroutineRunner.StopCoroutine(heartbeatCoroutine);
            }
            
            heartbeatCoroutine = coroutineRunner.StartCoroutine(HeartbeatCoroutine());
        }
        
        /// <summary>
        /// 停止心跳
        /// </summary>
        private void StopHeartbeat()
        {
            if (heartbeatCoroutine != null)
            {
                coroutineRunner.StopCoroutine(heartbeatCoroutine);
                heartbeatCoroutine = null;
            }
        }
        
        /// <summary>
        /// 心跳协程
        /// </summary>
        private IEnumerator HeartbeatCoroutine()
        {
            while (isConnected)
            {
                yield return new WaitForSeconds(heartbeatInterval);
                
                if (isConnected)
                {
                    SendHeartbeat();
                }
            }
        }
        
        /// <summary>
        /// 发送心�?
        /// </summary>
        private void SendHeartbeat()
        {
            try
            {
                // 创建心跳消息
                var heartbeatMessage = new HeartbeatMessage
                {
                    ClientId = clientId,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                
                SendMessage(heartbeatMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkManager: 发送心跳失�?- {ex.Message}");
            }
        }
        
        /// <summary>
        /// 开始重�?
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
            
            if (reconnectCoroutine != null)
            {
                coroutineRunner.StopCoroutine(reconnectCoroutine);
            }
            
            reconnectCoroutine = coroutineRunner.StartCoroutine(ReconnectCoroutine());
        }
        
        /// <summary>
        /// 停止重连
        /// </summary>
        private void StopReconnect()
        {
            if (reconnectCoroutine != null)
            {
                coroutineRunner.StopCoroutine(reconnectCoroutine);
                reconnectCoroutine = null;
            }
            
            reconnectAttempts = 0;
        }
        
        /// <summary>
        /// 重连协程
        /// </summary>
        private IEnumerator ReconnectCoroutine()
        {
            yield return new WaitForSeconds(reconnectInterval);
            
            // 尝试重新连接
            Connect(serverAddress, port);
        }
        
        /// <summary>
        /// 处理消息协程
        /// </summary>
        private IEnumerator ProcessMessagesCoroutine()
        {
            while (isConnected)
            {
                // 接收消息
                byte[] data = networkClient.Receive();
                if (data != null && data.Length > 0)
                {
                    try
                    {
                        // 反序列化消息
                        NetworkMessage message = NetworkMessage.Deserialize(data);
                        if (message != null)
                        {
                            // 添加到消息队�?
                            messageQueue.Enqueue(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"NetworkManager: 处理消息失败 - {ex.Message}");
                    }
                }
                
                yield return null;
            }
        }
        
        /// <summary>
        /// 处理消息队列
        /// </summary>
        public void Update()
        {
            while (messageQueue.Count > 0)
            {
                NetworkMessage message = messageQueue.Dequeue();
                
                // 触发消息接收事件
                OnMessageReceived?.Invoke(message);
                
                // 调用注册的处理器
                if (messageHandlers.TryGetValue(message.MessageType, out Action<NetworkMessage> handler))
                {
                    try
                    {
                        handler?.Invoke(message);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"NetworkManager: 处理消息 {message.MessageType} 时发生异�?- {ex.Message}");
                    }
                }
                else
                {
                    if (enableLogging)
                        Debug.LogWarning($"NetworkManager: 未找到消息处理器 {message.MessageType}");
                }
            }
        }
        
        /// <summary>
        /// 更新连接状�?
        /// </summary>
        /// <param name="status">新状�?/param>
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
        /// 获取消息类型
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <returns>消息类型枚举</returns>
        private MessageType GetMessageType<T>() where T : NetworkMessage
        {
            if (typeof(T) == typeof(GameStateMessage))
                return MessageType.GAME_STATE;
            else if (typeof(T) == typeof(PlayerInputMessage))
                return MessageType.PLAYER_INPUT;
            else if (typeof(T) == typeof(ServerEventMessage))
                return MessageType.SERVER_EVENT;
            else if (typeof(T) == typeof(HeartbeatMessage))
                return MessageType.HEARTBEAT;
            else
                return MessageType.UNKNOWN;
        }
        
        /// <summary>
        /// 关闭网络管理�?
        /// </summary>
        public void Shutdown()
        {
            if (enableLogging)
                Debug.Log("NetworkManager: 关闭网络管理器");
            
            // 断开连接
            Disconnect();
            
            // 清理处理器
            messageHandlers.Clear();
            messageQueue.Clear();
            
            // 清理网络客户�?
            networkClient?.Dispose();
            networkClient = null;
        }
    }
    
    /// <summary>
    /// 连接状态枚�?
    /// </summary>
    public enum ConnectionStatus
    {
        DISCONNECTED,   // 未连�?
        CONNECTING,     // 连接�?
        CONNECTED,      // 已连�?
        RECONNECTING,   // 重连�?
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
    /// 网络消息基类
    /// </summary>
    [Serializable]
    public abstract class NetworkMessage
    {
        public MessageType MessageType { get; set; }
        public long Timestamp { get; set; }
        public string SenderId { get; set; }
        
        public abstract byte[] Serialize();
        
        public static NetworkMessage Deserialize(byte[] data)
        {
            // 这里应该实现具体的反序列化逻辑
            // 暂时返回null，需要根据实际协议实现
            return null;
        }
    }
    
    /// <summary>
    /// 游戏状态消�?
    /// </summary>
    [Serializable]
    public class GameStateMessage : NetworkMessage
    {
        public GameStateData GameState { get; set; }
        public int FrameNumber { get; set; }
        
        public override byte[] Serialize()
        {
            // 实现序列化逻辑
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(this));
        }
    }
    
    /// <summary>
    /// 玩家输入消息
    /// </summary>
    [Serializable]
    public class PlayerInputMessage : NetworkMessage
    {
        public string PlayerId { get; set; }
        public InputData InputData { get; set; }
        public int InputFrame { get; set; }
        
        public override byte[] Serialize()
        {
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(this));
        }
    }
    
    /// <summary>
    /// 服务器事件消息
    /// </summary>
    [Serializable]
    public class ServerEventMessage : NetworkMessage
    {
        public string EventType { get; set; }
        public Dictionary<string, object> EventData { get; set; }
        
        public override byte[] Serialize()
        {
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(this));
        }
    }
    
    /// <summary>
    /// 心跳消息
    /// </summary>
    [Serializable]
    public class HeartbeatMessage : NetworkMessage
    {
        public string ClientId { get; set; }
        
        public override byte[] Serialize()
        {
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(this));
        }
    }
    
    /// <summary>
    /// 游戏状态数据
    /// </summary>
    [Serializable]
    public class GameStateData
    {
        public Dictionary<string, object> StateData { get; set; }
    }
    
    /// <summary>
    /// 输入数据
    /// </summary>
    [Serializable]
    public class InputData
    {
        public float MoveX { get; set; }
        public float MoveY { get; set; }
        public bool IsActionPressed { get; set; }
    }
    
    /// <summary>
    /// 网络客户端
    /// </summary>
    public class NetworkClient : System.IDisposable
    {
        private Socket socket;
        private bool isConnected = false;
        
        public bool Connect(string address, int port)
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(address, port);
                isConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkClient: 连接失败 - {ex.Message}");
                return false;
            }
        }
        
        public void Disconnect()
        {
            if (socket != null && socket.Connected)
            {
                socket.Close();
                socket = null;
            }
            isConnected = false;
        }
        
        public void Send(byte[] data)
        {
            if (socket != null && socket.Connected)
            {
                socket.Send(data);
            }
        }
        
        public byte[] Receive()
        {
            if (socket != null && socket.Connected)
            {
                byte[] buffer = new byte[1024];
                int bytesRead = socket.Receive(buffer);
                if (bytesRead > 0)
                {
                    byte[] data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);
                    return data;
                }
            }
            return null;
        }
        
        public void Dispose()
        {
            Disconnect();
        }
    }
} 
