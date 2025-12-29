using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Network;
using Astrum.Network.MessageHandlers;
using Astrum.Network.Generated;
using Astrum.Client.MessageHandlers;
using Astrum.Client.Core;
using Astrum.Generated;
using IMessageHandler = Astrum.Network.MessageHandlers.IMessageHandler;
using MemoryPack;

namespace Astrum.Client.Managers
{
    /// <summary>
    /// 重构后的NetworkManager
    /// 使用消息处理器系统替换原有的Action回调方式
    /// </summary>
    public class NetworkManager : Singleton<NetworkManager>
    {
        private TService tcpService;
        private Session currentSession;
        private bool isInitialized = false;
        private long _lastPingAtMs = 0;
        private bool _pingInProgress = false;
        
        // 客户端连接状态
        private bool isConnected = false;
        
        // 保留必要的连接状态事件（这些不是消息处理，而是连接状态管理）
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<ConnectionStatus> OnConnectionStatusChanged;
        
        // 移除所有具体的消息Action事件，例如：
        // public event Action<LoginResponse> OnLoginResponse; ❌ 删除
        // public event Action<CreateRoomResponse> OnCreateRoomResponse; ❌ 删除
        // ... 其他20+个消息事件 ❌ 全部删除
        
        // 客户端代码期望的属性
        public ConnectionStatus ConnectionStatus => GetConnectionStatus();
        public string ClientId => currentSession?.Id.ToString() ?? "-1";
        
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
                
                // 初始化消息处理器分发器
                MessageHandlerDispatcher.Instance.Initialize();
                
                // 注册AstrumClient项目中的消息处理器
                MessageHandlerRegistry.Instance.RegisterAllHandlers();
                
                isInitialized = true;
                ASLogger.Instance.Info("NetworkManager initialized with MessageHandler system");
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
                long channelId = NetServices.Instance.CreateConnectChannelId();
                
                if (tcpService != null)
                {
                    tcpService.Create(channelId, endPoint);
                    ASLogger.Instance.Info($"TCP connection initiated to {endPoint}, ChannelId: {channelId}");
                    
                    // 创建 Session
                    currentSession = new Session();
                    currentSession.Initialize(tcpService, channelId, endPoint);
                    
                    // 客户端模式：TCP连接建立后立即认为连接成功
                    isConnected = true;
                    ASLogger.Instance.Info($"客户端TCP连接建立成功，ChannelId: {channelId}");
                    
                    // 触发连接成功事件
                    OnConnected?.Invoke();
                    OnConnectionStatusChanged?.Invoke(ConnectionStatus.Connected);
                    
                    // 发布 EventSystem 事件
                    EventSystem.Instance.Publish(new NetworkConnectionStatusChangedEventData
                    {
                        Status = ConnectionStatus.Connected
                    });

                    // 记录首次连接时间并立即发起一次校准
                    _lastPingAtMs = TimeInfo.Instance.ClientNow();
                    _ = SafePingOnceAsync();
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
                // ASLogger.Instance.Debug($"Message sent via Session: {message.GetType().Name}", "Network.Message");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Failed to send message via Session: {ex.Message}");
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

                // 每分钟进行一次Ping校准
                if (IsConnected() && !_pingInProgress)
                {
                    var now = TimeInfo.Instance.ClientNow();
                    if (now - _lastPingAtMs >= 60_000)
                    {
                        _ = SafePingOnceAsync();
                    }
                }
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
                
                // 关闭消息处理器分发器
                MessageHandlerDispatcher.Instance.Shutdown();
                
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
            ASLogger.Instance.Info($"TCP connection accepted from {remoteEndPoint}, ChannelId: {channelId}");
            
            // 更新连接状态
            if (currentSession != null && channelId == currentSession.Id)
            {
                isConnected = true;
                
                // 触发客户端代码期望的事件
                OnConnected?.Invoke();
                OnConnectionStatusChanged?.Invoke(ConnectionStatus.Connected);
                
                // 发布 EventSystem 事件
                EventSystem.Instance.Publish(new NetworkConnectionStatusChangedEventData
                {
                    Status = ConnectionStatus.Connected
                });
            }
        }
        
        private void OnTcpRead(long channelId, MemoryBuffer buffer)
        {
            // 更新 Session 接收时间
            if (currentSession != null && channelId == currentSession.Id)
            {
                currentSession.LastRecvTime = TimeInfo.Instance.ClientNow();
                // ASLogger.Instance.Debug($"收到来自服务器的数据，ChannelId: {channelId}, 数据长度: {buffer.Length}", "Network.Receive");
            }
            
            // 异步处理消息，但不等待（fire-and-forget）
            _ = ProcessMessageAsync(channelId, buffer);
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
                
                // 发布 EventSystem 事件
                EventSystem.Instance.Publish(new NetworkDisconnectedEventData());
                EventSystem.Instance.Publish(new NetworkConnectionStatusChangedEventData
                {
                    Status = ConnectionStatus.Disconnected
                });
            }
        }
        
        /// <summary>
        /// 处理接收到的消息 - 使用新的消息处理器系统
        /// </summary>
        private async Task ProcessMessageAsync(long channelId, MemoryBuffer buffer)
        {
            try
            {
                if (currentSession != null && channelId == currentSession.Id)
                {
                    await ProcessReceivedMessage(buffer);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Error processing message on channel {channelId}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理接收到的消息 - 核心改进：使用消息处理器分发器
        /// </summary>
        private async Task ProcessReceivedMessage(MemoryBuffer buffer)
        {
            try
            {
                if (tcpService == null)
                {
                    ASLogger.Instance.Error("TCP service not initialized, cannot process received message");
                    return;
                }
                
                // 使用 MessageSerializeHelper.ToMessage 直接序列化出 Message 对象
                var messageObject = MessageSerializeHelper.ToMessage(tcpService, buffer);
                if (messageObject != null && messageObject is MessageObject msgObj)
                {
                    // 使用新的消息处理器分发系统
                    await MessageHandlerDispatcher.Instance.DispatchAsync(msgObj);
                    // ASLogger.Instance.Debug($"消息已分发到处理器: {msgObj.GetType().Name}", "Network.Deserialize");
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
        /// 发送一次 Ping 并基于响应校准 ServerMinusClientTime
        /// </summary>
        public async Task SendPingAndCalibrateAsync()
        {
            if (!IsConnected() || currentSession == null)
            {
                ASLogger.Instance.Warning("Ping skipped: not connected");
                return;
            }

            // 无 RPC 管道：使用事件+TCS等待响应，支持超时与重试
            const int timeoutMs = 300;
            const int maxRetries = 2;
            int attempt = 0;
            bool success = false;
            while (attempt <= maxRetries && !success)
            {
                attempt++;
                var t1 = TimeInfo.Instance.ClientNow();
                var req = C2G_Ping.Create();
                req.ClientSendTime = t1;

                var tcs = new TaskCompletionSource<G2C_Ping?>(TaskCreationOptions.RunContinuationsAsynchronously);
                
                // 创建临时Ping处理器
                var tempHandler = new TempPingHandler(tcs);
                MessageHandlerDispatcher.Instance.RegisterHandler(typeof(G2C_Ping), tempHandler, 0, true);
                
                try
                {
                    Send(req);
                    var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
                    if (completed == tcs.Task)
                    {
                        var captured = tcs.Task.Result;
                        if (captured != null)
                        {
                            var t2 = TimeInfo.Instance.ClientNow();
                            var rtt = t2 - t1;
                            var offset = captured.Time + rtt / 2 - t2;
                            TimeInfo.Instance.RTT = rtt;
                            TimeInfo.Instance.ServerMinusClientTime = offset;
                            ASLogger.Instance.Info($"Ping RTT={rtt}ms, offset={offset}ms (attempt {attempt})");
                            success = true;
                        }
                    }
                    else
                    {
                        ASLogger.Instance.Warning($"Ping response timeout after {timeoutMs}ms (attempt {attempt})");
                    }
                }
                finally
                {
                    // 清理临时处理器
                    // 注意：这里需要从MessageHandlerDispatcher中移除临时处理器
                    // 由于MessageHandlerDispatcher没有提供移除方法，我们使用一次性处理
                }
            }

            if (!success)
            {
                ASLogger.Instance.Warning("Ping response not received in time");
            }
        }

        private async Task SafePingOnceAsync()
        {
            if (_pingInProgress) return;
            _pingInProgress = true;
            try
            {
                await SendPingAndCalibrateAsync();
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Warning($"Ping calibration failed: {ex.Message}");
            }
            finally
            {
                _lastPingAtMs = TimeInfo.Instance.ClientNow();
                _pingInProgress = false;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 临时Ping处理器，用于处理Ping响应
    /// </summary>
    internal class TempPingHandler : IMessageHandler
    {
        private readonly TaskCompletionSource<G2C_Ping?> _tcs;
        
        public TempPingHandler(TaskCompletionSource<G2C_Ping?> tcs)
        {
            _tcs = tcs;
        }
        
        public async Task HandleAsync(MessageObject message)
        {
            if (message is G2C_Ping pingResponse)
            {
                _tcs.TrySetResult(pingResponse);
            }
            await Task.CompletedTask;
        }
        
        public Type GetMessageType()
        {
            return typeof(G2C_Ping);
        }
    }
}