using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Network;
using Astrum.Network.Generated;
using Astrum.LogicCore;
using MemoryPack;

namespace Astrum.Client.Managers
{
        /// <summary>
    /// 网络管理器，基于ET框架的TCP服务实现
    /// </summary>
    public class NetworkManager : Singleton<NetworkManager>
    {
        private TService tcpService;
        private Session currentSession;
        private bool isInitialized = false;
        
        // 客户端代码期望的事件接口
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<ConnectionStatus> OnConnectionStatusChanged;
        
        // 具体消息类型的事件
        public event Action<ConnectResponse> OnConnectResponse;
        public event Action<LoginResponse> OnLoginResponse;
        public event Action<CreateRoomResponse> OnCreateRoomResponse;
        public event Action<JoinRoomResponse> OnJoinRoomResponse;
        public event Action<LeaveRoomResponse> OnLeaveRoomResponse;
        public event Action<GetRoomListResponse> OnGetRoomListResponse;
        public event Action<RoomUpdateNotification> OnRoomUpdateNotification;
        public event Action<HeartbeatResponse> OnHeartbeatResponse;
        public event Action<GameResponse> OnGameResponse;
        public event Action<GameStartNotification> OnGameStartNotification;
        public event Action<GameEndNotification> OnGameEndNotification;
        public event Action<GameStateUpdate> OnGameStateUpdate;
        public event Action<FrameSyncStartNotification> OnFrameSyncStartNotification;
        public event Action<FrameSyncEndNotification> OnFrameSyncEndNotification;
        public event Action<FrameSyncData> OnFrameSyncData;
        public event Action<OneFrameInputs> OnFrameInputs;
        
        // 客户端代码期望的属性
        public ConnectionStatus ConnectionStatus => GetConnectionStatus();
        public string ClientId => currentSession?.Id.ToString() ?? "-1";
        
        // 客户端连接状态
        private bool isConnected = false;
        
        /// <summary>
        /// 初始化网络管理器（客户端模式）
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                ASLogger.Instance.Warning("NetworkManager already initialized");
                return;
            }
            
            try
            {
                // 初始化TCP服务（客户端模式）
                tcpService = new TService(AddressFamily.InterNetwork, ServiceType.Outer);
                tcpService.AcceptCallback = OnTcpAccept;
                tcpService.ReadCallback = OnTcpRead;
                tcpService.ErrorCallback = OnTcpError;
                
                isInitialized = true;
                ASLogger.Instance.Info("NetworkManager initialized successfully (client mode)");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Failed to initialize NetworkManager: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 连接到服务器（兼容客户端代码）
        /// </summary>
        /// <param name="serverAddress">服务器地址</param>
        /// <param name="serverPort">服务器端口</param>
        /// <returns>连接ID</returns>
        public async Task<long> ConnectAsync(string serverAddress, int serverPort)
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);
            return await ConnectAsync(endPoint);
        }
        
        /// <summary>
        /// 连接到服务器
        /// </summary>
        /// <param name="endPoint">服务器端点</param>
        /// <returns>连接ID</returns>
        public Task<long> ConnectAsync(IPEndPoint endPoint)
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("NetworkManager not initialized");
            }
            
            try
            {
                // 注册连接响应事件处理
                OnConnectResponse += OnConnectResponseReceived;
                
                long channelId = NetServices.Instance.CreateConnectChannelId();
                
                if (tcpService != null)
                {
                    tcpService.Create(channelId, endPoint);
                    ASLogger.Instance.Info($"TCP connection initiated to {endPoint}, ChannelId: {channelId}");
                    
                    // 创建 Session
                    currentSession = new Session();
                    currentSession.Initialize(tcpService, channelId, endPoint);
                    
                    // 客户端模式：TCP连接建立后立即认为连接成功
                    // 因为TCP连接建立意味着网络层连接已成功
                    isConnected = true;
                    ASLogger.Instance.Info($"客户端TCP连接建立成功，ChannelId: {channelId}");
                    
                    // 触发连接成功事件
                    OnConnected?.Invoke();
                    OnConnectionStatusChanged?.Invoke(ConnectionStatus.Connected);
                }
                else
                {
                    throw new InvalidOperationException("TCP service not initialized");
                }
                
                return Task.FromResult(channelId);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Failed to connect to {endPoint}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 发送消息（兼容客户端代码）
        /// </summary>
        /// <param name="message">网络消息</param>
        /// <returns>发送任务</returns>
        public async Task SendMessageAsync(object message)
        {
            if (IsConnected() && currentSession != null)
            {
                try
                {
                    if (message is MessageObject messageObject)
                    {
                        currentSession.Send(messageObject);
                        ASLogger.Instance.Info($"Message sent via Session: {messageObject.GetType().Name}");
                    }
                    else
                    {
                        ASLogger.Instance.Error($"Invalid message type: {message.GetType().Name}");
                    }
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Error($"Failed to send message via Session: {ex.Message}");
                }
                await Task.CompletedTask;
            }
            else
            {
                ASLogger.Instance.Warning("Cannot send message: not connected to server or session is null");
            }
        }
        
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息对象</param>
        public void Send(IMessage message)
        {
            if (currentSession == null)
            {
                ASLogger.Instance.Error("No active session");
                return;
            }
            
            if (!IsConnected())
            {
                ASLogger.Instance.Warning("Cannot send message: not connected to server");
                return;
            }
            
            try
            {
                currentSession.Send(message);
                //ASLogger.Instance.Debug($"Message sent via Session: {message.GetType().Name}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Failed to send message via Session: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 关闭当前连接
        /// </summary>
        /// <param name="error">错误码</param>
        private void CloseCurrentConnection(int error = 0)
        {
            if (currentSession != null && tcpService != null)
            {
                tcpService.Remove(currentSession.Id, error);
                ASLogger.Instance.Info($"Connection closed, error: {error}");
            }
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="error">错误码</param>
        public void Disconnect(int error = 0)
        {
            if (IsConnected())
            {
                CloseCurrentConnection(error);
                currentSession = null;
                isConnected = false;
                ASLogger.Instance.Info("Current connection disconnected");
            }
        }
        
        /// <summary>
        /// 更新网络服务
        /// </summary>
        public void Update()
        {
            if (!isInitialized || tcpService == null) return;
            
            try
            {
                tcpService.Update();
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Error in NetworkManager.Update: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 检查是否已连接到服务器
        /// </summary>
        /// <returns>是否已连接</returns>
        public bool IsConnected()
        {
            return isConnected && currentSession != null;
        }
        
        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <returns>连接状态</returns>
        private ConnectionStatus GetConnectionStatus()
        {
            if (!isInitialized)
                return ConnectionStatus.Disconnected;
            
            if (isConnected && currentSession != null)
                return ConnectionStatus.Connected;
            
            if (currentSession != null && !isConnected)
                return ConnectionStatus.Connecting;
            
            return ConnectionStatus.Disconnected;
        }
        
        /// <summary>
        /// 获取当前连接的通道ID
        /// </summary>
        /// <returns>通道ID</returns>
        public long GetCurrentChannelId()
        {
            return currentSession?.Id ?? -1;
        }
        
        /// <summary>
        /// 获取当前服务器端点
        /// </summary>
        /// <returns>服务器端点</returns>
        public IPEndPoint GetCurrentServerEndPoint()
        {
            return currentSession?.RemoteAddress;
        }
        
        /// <summary>
        /// 获取当前 Session
        /// </summary>
        /// <returns>当前 Session</returns>
        public Session GetCurrentSession()
        {
            return currentSession;
        }
        
        /// <summary>
        /// 关闭网络管理器
        /// </summary>
        public void Shutdown()
        {
            if (!isInitialized) return;
            
            try
            {
                // 断开当前连接
                if (IsConnected())
                {
                    Disconnect();
                }
                
                // 关闭TCP服务
                tcpService?.Dispose();
                
                tcpService = null;
                currentSession = null;
                isConnected = false;
                isInitialized = false;
                
                ASLogger.Instance.Info("NetworkManager shutdown completed");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Error during NetworkManager shutdown: {ex.Message}");
            }
        }
        
        #region Private Methods
        
        private void OnTcpAccept(long channelId, IPEndPoint remoteEndPoint)
        {
            // 注意：在客户端模式下，这个回调通常不会被调用
            // 客户端连接成功应该通过其他方式检测
            ASLogger.Instance.Info($"TCP connection accepted from {remoteEndPoint}, ChannelId: {channelId}");
            
            // 更新连接状态
            if (currentSession != null && channelId == currentSession.Id)
            {
                isConnected = true;
                
                // 触发客户端代码期望的事件
                OnConnected?.Invoke();
                OnConnectionStatusChanged?.Invoke(ConnectionStatus.Connected);
            }
        }
        
        private void OnTcpRead(long channelId, MemoryBuffer buffer)
        {
            // 更新 Session 接收时间
            if (currentSession != null && channelId == currentSession.Id)
            {
                currentSession.LastRecvTime = TimeInfo.Instance.ClientNow();
                
                // 客户端模式：连接已在ConnectAsync中建立，这里只处理消息
                //ASLogger.Instance.Debug($"收到来自服务器的数据，ChannelId: {channelId}, 数据长度: {buffer.Length}");
            }
            
            ProcessMessage(channelId, buffer);
        }
        
        private void OnTcpError(long channelId, int error)
        {
            ASLogger.Instance.Error($"TCP error on channel {channelId}: {error}");
            
            // 更新连接状态
            if (currentSession != null && channelId == currentSession.Id)
            {
                isConnected = false;
                
                // 清理 Session
                currentSession.Error = error;
                currentSession = null;
                
                // 触发客户端代码期望的事件
                OnDisconnected?.Invoke();
                OnConnectionStatusChanged?.Invoke(ConnectionStatus.Disconnected);
            }
        }
        
        private void ProcessMessage(long channelId, MemoryBuffer buffer)
        {
            try
            {
                if (currentSession != null && channelId == currentSession.Id)
                {
                    // 尝试反序列化服务器发送的具体消息对象
                    ProcessReceivedMessage(buffer);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Error processing message on channel {channelId}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private void ProcessReceivedMessage(MemoryBuffer buffer)
        {
            try
            {
                // 检查 tcpService 是否已初始化
                if (tcpService == null)
                {
                    ASLogger.Instance.Error("TCP service not initialized, cannot process received message");
                    return;
                }
                
                // 使用 MessageSerializeHelper.ToMessage 直接序列化出 Message 对象
                var messageObject = MessageSerializeHelper.ToMessage(tcpService, buffer);
                if (messageObject != null)
                {
                    // 根据消息类型触发对应事件
                    DispatchMessage(messageObject);
                    //ASLogger.Instance.Debug($"成功反序列化消息: {messageObject.GetType().Name}");
                }
                else
                {
                    ASLogger.Instance.Warning($"无法反序列化消息，数据长度: {buffer.Length}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理接收消息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 分发消息到对应的事件处理器
        /// </summary>
        private void DispatchMessage(object messageObject)
        {
            switch (messageObject)
            {
                case ConnectResponse connectResponse:
                    OnConnectResponse?.Invoke(connectResponse);
                    break;
                case LoginResponse loginResponse:
                    OnLoginResponse?.Invoke(loginResponse);
                    break;
                case CreateRoomResponse createRoomResponse:
                    OnCreateRoomResponse?.Invoke(createRoomResponse);
                    break;
                case JoinRoomResponse joinRoomResponse:
                    OnJoinRoomResponse?.Invoke(joinRoomResponse);
                    break;
                case LeaveRoomResponse leaveRoomResponse:
                    OnLeaveRoomResponse?.Invoke(leaveRoomResponse);
                    break;
                case GetRoomListResponse getRoomListResponse:
                    OnGetRoomListResponse?.Invoke(getRoomListResponse);
                    break;
                case RoomUpdateNotification roomUpdateNotification:
                    OnRoomUpdateNotification?.Invoke(roomUpdateNotification);
                    break;
                case HeartbeatResponse heartbeatResponse:
                    OnHeartbeatResponse?.Invoke(heartbeatResponse);
                    break;
                case GameResponse gameResponse:
                    OnGameResponse?.Invoke(gameResponse);
                    break;
                case GameStartNotification gameStartNotification:
                    OnGameStartNotification?.Invoke(gameStartNotification);
                    break;
                case GameEndNotification gameEndNotification:
                    OnGameEndNotification?.Invoke(gameEndNotification);
                    break;
                case GameStateUpdate gameStateUpdate:
                    OnGameStateUpdate?.Invoke(gameStateUpdate);
                    break;
                case FrameSyncStartNotification frameSyncStartNotification:
                    OnFrameSyncStartNotification?.Invoke(frameSyncStartNotification);
                    break;
                case FrameSyncEndNotification frameSyncEndNotification:
                    OnFrameSyncEndNotification?.Invoke(frameSyncEndNotification);
                    break;
                case FrameSyncData frameSyncData:
                    OnFrameSyncData?.Invoke(frameSyncData);
                    break;
                case OneFrameInputs oneFrameInputs:
                    OnFrameInputs?.Invoke(oneFrameInputs);
                    break;
                default:
                    ASLogger.Instance.Warning($"未处理的消息类型: {messageObject.GetType().Name}");
                    break;
            }
        }
        


        
        /// <summary>
        /// 处理连接响应
        /// </summary>
        private void OnConnectResponseReceived(ConnectResponse response)
        {
            try
            {
                if (response.success)
                {
                    ASLogger.Instance.Info($"连接成功: {response.message}");
                    
                    // 更新连接状态
                    isConnected = true;
                    
                    // 触发连接成功事件
                    OnConnected?.Invoke();
                    OnConnectionStatusChanged?.Invoke(ConnectionStatus.Connected);
                }
                else
                {
                    ASLogger.Instance.Warning($"连接失败: {response.message}");
                    
                    // 更新连接状态
                    isConnected = false;
                    
                    // 触发连接失败事件
                    OnConnectionStatusChanged?.Invoke(ConnectionStatus.Disconnected);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理连接响应时出错: {ex.Message}");
            }
        }
        
        #endregion
    }
} 
