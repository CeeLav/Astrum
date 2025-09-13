using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Astrum.Network;
using Astrum.Network.Generated;
using Astrum.CommonBase;
using MemoryPack;

namespace AstrumServer.Network
{
    /// <summary>
    /// 服务器网络管理器 - 使用Unity的Network程序集 + MemoryPack序列化
    /// </summary>
    public class ServerNetworkManager : Singleton<ServerNetworkManager>
    {
        private AService? _tcpService;
        private readonly Dictionary<long, Session> _sessions = new();
        private readonly object _sessionsLock = new();
        // 事件定义
        public event Action<Session>? OnClientConnected;
        public event Action<Session>? OnClientDisconnected;
        public event Action<Session, MessageObject>? OnMessageReceived;
        public event Action<Session, Exception>? OnError;
        
        public ServerNetworkManager()
        {
        }
        
        public void SetLogger(ASLogger logger)
        {
            // ASLogger是单例，不需要存储引用
        }
        
        /// <summary>
        /// 初始化服务器网络服务
        /// </summary>
        /// <param name="port">监听端口</param>
        public Task<bool> InitializeAsync(int port = 8888)
        {
            try
            {
                ASLogger.Instance.Info($"正在初始化服务器网络管理器，端口: {port}");
                
                // 创建TCP服务
                var endPoint = new IPEndPoint(IPAddress.Any, port);
                _tcpService = new TService(endPoint, ServiceType.Outer);
                
                // 设置事件回调
                _tcpService.AcceptCallback = OnAccept;
                _tcpService.ReadCallback = OnRead;
                _tcpService.ErrorCallback = OnNetworkError;
                
                ASLogger.Instance.Info($"服务器网络管理器初始化成功，监听端口: {port}");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"服务器网络管理器初始化失败: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return Task.FromResult(false);
            }
        }
        
        /// <summary>
        /// 更新网络服务
        /// </summary>
        public void Update()
        {
            try
            {
                _tcpService?.Update();
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"网络服务更新时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 发送消息到指定会话
        /// </summary>
        public void SendMessage(string sessionId, MessageObject message)
        {
            try
            {
                lock (_sessionsLock)
                {
                    if (long.TryParse(sessionId, out var sessionIdLong) && _sessions.TryGetValue(sessionIdLong, out var session))
                    {
                        // 直接发送具体消息对象，不包装在 NetworkMessage 中
                        session.Send(message);
                        ASLogger.Instance.Debug($"发送消息到会话 {sessionId}: {message.GetType().Name}");
                    }
                    else
                    {
                        ASLogger.Instance.Warning($"会话 {sessionId} 不存在，无法发送消息");
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"发送消息到会话 {sessionId} 时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 广播消息到所有会话
        /// </summary>
        public void BroadcastMessage(MessageObject message)
        {
            try
            {
                lock (_sessionsLock)
                {
                    foreach (var session in _sessions.Values)
                    {
                        // 直接发送具体消息对象，不包装在 NetworkMessage 中
                        session.Send(message);
                    }
                    ASLogger.Instance.Debug($"广播消息: {message.GetType().Name} 到 {_sessions.Count} 个会话");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"广播消息时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
            }
        
        /// <summary>
        /// 断开指定会话
        /// </summary>
        public void DisconnectSession(long sessionId)
        {
            try
            {
                lock (_sessionsLock)
                {
                    if (_sessions.TryGetValue(sessionId, out var session))
                    {
                        _tcpService?.Remove(sessionId);
                        _sessions.Remove(sessionId);
                        ASLogger.Instance.Info($"断开会话 {sessionId}");
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"断开会话 {sessionId} 时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 获取所有活跃会话
        /// </summary>
        public List<Session> GetActiveSessions()
        {
            lock (_sessionsLock)
            {
                return new List<Session>(_sessions.Values);
            }
        }
        
        /// <summary>
        /// 获取会话数量
        /// </summary>
        public int GetSessionCount()
        {
            lock (_sessionsLock)
            {
                return _sessions.Count;
            }
        }
        
        /// <summary>
        /// 关闭网络服务
        /// </summary>
        public void Shutdown()
        {
            try
            {
                ASLogger.Instance.Info("正在关闭服务器网络管理器");
                
                // 断开所有会话
                lock (_sessionsLock)
                {
                    foreach (var sessionId in _sessions.Keys.ToArray())
                    {
                        _tcpService?.Remove(sessionId);
                    }
                    _sessions.Clear();
                }
                
                // 停止网络服务
                _tcpService?.Dispose();
                _tcpService = null;
                
                ASLogger.Instance.Info("服务器网络管理器已关闭");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"关闭服务器网络管理器时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        // 网络事件回调
        private void OnAccept(long channelId, IPEndPoint remoteAddress)
        {
            try
            {
                ASLogger.Instance.Info($"新客户端连接: {channelId} from {remoteAddress}");
                
                // 创建会话
                var session = new Session();
                session.Initialize(_tcpService!, channelId, remoteAddress);
                
                lock (_sessionsLock)
                {
                    _sessions[channelId] = session;
                }
                
                // 触发连接事件
                OnClientConnected?.Invoke(session);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理客户端连接时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        private void OnRead(long channelId, MemoryBuffer buffer)
        {
            try
            {
                lock (_sessionsLock)
                {
                    if (_sessions.TryGetValue(channelId, out var session))
                    {
                        // 更新会话接收时间
                        session.LastRecvTime = TimeInfo.Instance.ClientNow();
                        
                        // 解析网络消息
                        try
                        {
                            // 使用MessageSerializeHelper反序列化消息
                            var message = MessageSerializeHelper.ToMessage(_tcpService, buffer);
                            if (message != null)
                            {
                                // 触发消息接收事件
                                OnMessageReceived?.Invoke(session, message as MessageObject);
                            }
                        }
                        catch (Exception parseEx)
                        {
                            ASLogger.Instance.Warning($"解析消息时出错: {parseEx.Message}");
                            ASLogger.Instance.LogException(parseEx, LogLevel.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理接收数据时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        private void OnNetworkError(long channelId, int errorCode)
        {
            try
            {
                ASLogger.Instance.Error($"会话 {channelId} 发生错误，错误码: {errorCode}");
                
                lock (_sessionsLock)
                {
                    if (_sessions.TryGetValue(channelId, out var session))
                    {
                        _sessions.Remove(channelId);
                        
                        // 触发错误和断开连接事件
                        var exception = new Exception($"Network error: {errorCode}");
                        OnError?.Invoke(session, exception);
                        OnClientDisconnected?.Invoke(session);
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理网络错误时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
    }
}
