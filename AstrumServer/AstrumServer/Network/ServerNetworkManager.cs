using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
        private Microsoft.Extensions.Logging.ILogger? _logger;
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
        
        public void SetLogger(Microsoft.Extensions.Logging.ILogger logger)
        {
            var field = typeof(ServerNetworkManager).GetField("_logger", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(this, logger);
        }
        
        /// <summary>
        /// 初始化服务器网络服务
        /// </summary>
        /// <param name="port">监听端口</param>
        public Task<bool> InitializeAsync(int port = 8888)
        {
            try
            {
                _logger?.LogInformation("正在初始化服务器网络管理器，端口: {Port}", port);
                
                // 创建TCP服务
                var endPoint = new IPEndPoint(IPAddress.Any, port);
                _tcpService = new TService(endPoint, ServiceType.Outer);
                
                // 设置事件回调
                _tcpService.AcceptCallback = OnAccept;
                _tcpService.ReadCallback = OnRead;
                _tcpService.ErrorCallback = OnNetworkError;
                
                _logger?.LogInformation("服务器网络管理器初始化成功，监听端口: {Port}", port);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "服务器网络管理器初始化失败");
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
                _logger?.LogError(ex, "网络服务更新时出错");
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
                        _logger?.LogDebug("发送消息到会话 {SessionId}: {MessageType}", sessionId, message.GetType().Name);
                    }
                    else
                    {
                        _logger?.LogWarning("会话 {SessionId} 不存在，无法发送消息", sessionId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "发送消息到会话 {SessionId} 时出错", sessionId);
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
                    _logger?.LogDebug("广播消息: {MessageType} 到 {SessionCount} 个会话", message.GetType().Name, _sessions.Count);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "广播消息时出错");
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
                        _logger?.LogInformation("断开会话 {SessionId}", sessionId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "断开会话 {SessionId} 时出错", sessionId);
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
                _logger?.LogInformation("正在关闭服务器网络管理器");
                
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
                
                _logger?.LogInformation("服务器网络管理器已关闭");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "关闭服务器网络管理器时出错");
            }
        }
        
        // 网络事件回调
        private void OnAccept(long channelId, IPEndPoint remoteAddress)
        {
            try
            {
                _logger?.LogInformation("新客户端连接: {ChannelId} from {RemoteAddress}", channelId, remoteAddress);
                
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
                _logger?.LogError(ex, "处理客户端连接时出错");
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
                            _logger?.LogWarning(parseEx, "解析消息时出错");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理接收数据时出错");
            }
        }
        
        private void OnNetworkError(long channelId, int errorCode)
        {
            try
            {
                _logger?.LogError("会话 {ChannelId} 发生错误，错误码: {ErrorCode}", channelId, errorCode);
                
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
                _logger?.LogError(ex, "处理网络错误时出错");
            }
        }
    }
}
