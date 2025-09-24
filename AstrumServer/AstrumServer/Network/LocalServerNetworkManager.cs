using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Network;

namespace AstrumServer.Network
{
    /// <summary>
    /// 本地模式网络管理器 - 用于测试环境的内存模拟
    /// 支持直接数据注入和模拟客户端交互
    /// </summary>
    public class LocalServerNetworkManager : IServerNetworkManager
    {
        private readonly ConcurrentDictionary<long, Session> _sessions = new();
        private readonly ConcurrentQueue<(long sessionId, MessageObject message)> _clientMessageQueue = new();
        private readonly ConcurrentDictionary<long, ConcurrentQueue<MessageObject>> _clientOutboxes = new();
        private long _nextSessionId = 1;
        private bool _isInitialized = false;

        public event Action<Session>? OnClientConnected;
        public event Action<Session>? OnClientDisconnected;
        public event Action<Session, MessageObject>? OnMessageReceived;
        public event Action<Session, Exception>? OnError;

        /// <summary>
        /// 初始化本地网络管理器
        /// </summary>
        public Task<bool> InitializeAsync(int port = 8888)
        {
            _isInitialized = true;
            ASLogger.Instance.Info("LocalServerNetworkManager 初始化完成（本地模式）");
            return Task.FromResult(true);
        }

        /// <summary>
        /// 更新网络状态，处理消息队列
        /// </summary>
        public void Update()
        {
            if (!_isInitialized) return;

            // 处理来自客户端的消息
            while (_clientMessageQueue.TryDequeue(out var item))
            {
                if (_sessions.TryGetValue(item.sessionId, out var session))
                {
                    OnMessageReceived?.Invoke(session, item.message);
                }
            }
        }

        /// <summary>
        /// 发送消息到指定会话
        /// </summary>
        public void SendMessage(string sessionId, MessageObject message)
        {
            if (long.TryParse(sessionId, out var id) && _clientOutboxes.TryGetValue(id, out var outbox))
            {
                outbox.Enqueue(message);
                ASLogger.Instance.Debug($"本地模式：发送消息到会话 {sessionId}: {message.GetType().Name}");
            }
            else
            {
                ASLogger.Instance.Warning($"本地模式：会话 {sessionId} 不存在，无法发送消息");
            }
        }

        /// <summary>
        /// 广播消息到所有会话
        /// </summary>
        public void BroadcastMessage(MessageObject message)
        {
            foreach (var outbox in _clientOutboxes.Values)
            {
                outbox.Enqueue(message);
            }
            ASLogger.Instance.Debug($"本地模式：广播消息 {message.GetType().Name} 到 {_clientOutboxes.Count} 个会话");
        }

        /// <summary>
        /// 断开指定会话
        /// </summary>
        public void DisconnectSession(long sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                _clientOutboxes.TryRemove(sessionId, out _);
                OnClientDisconnected?.Invoke(session);
                ASLogger.Instance.Info($"本地模式：断开会话 {sessionId}");
            }
        }

        /// <summary>
        /// 获取会话数量
        /// </summary>
        public int GetSessionCount()
        {
            return _sessions.Count;
        }

        /// <summary>
        /// 获取所有活跃会话
        /// </summary>
        public List<Session> GetActiveSessions()
        {
            return _sessions.Values.ToList();
        }

        /// <summary>
        /// 关闭网络服务
        /// </summary>
        public void Shutdown()
        {
            _sessions.Clear();
            _clientMessageQueue.Clear();
            _clientOutboxes.Clear();
            _isInitialized = false;
            ASLogger.Instance.Info("LocalServerNetworkManager 已关闭");
        }

        /// <summary>
        /// 设置日志记录器
        /// </summary>
        public void SetLogger(ASLogger logger)
        {
            // ASLogger是单例，不需要存储引用
        }

        #region 测试模式专用方法

        /// <summary>
        /// 模拟客户端连接
        /// </summary>
        /// <returns>新会话ID</returns>
        public long SimulateConnect()
        {
            var sessionId = _nextSessionId++;
            var session = new Session();
            session.Initialize(null, sessionId, null); // 本地模式不需要真实的网络服务
            
            _sessions[sessionId] = session;
            _clientOutboxes[sessionId] = new ConcurrentQueue<MessageObject>();
            
            OnClientConnected?.Invoke(session);
            ASLogger.Instance.Info($"本地模式：模拟客户端连接，会话ID: {sessionId}");
            return sessionId;
        }

        /// <summary>
        /// 模拟客户端断开连接
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="abrupt">是否为异常断开</param>
        public void SimulateDisconnect(long sessionId, bool abrupt = false)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                _clientOutboxes.TryRemove(sessionId, out _);
                
                if (abrupt)
                {
                    var exception = new Exception("模拟异常断开连接");
                    OnError?.Invoke(session, exception);
                }
                
                OnClientDisconnected?.Invoke(session);
                ASLogger.Instance.Info($"本地模式：模拟客户端断开连接，会话ID: {sessionId}, 异常断开: {abrupt}");
            }
        }

        /// <summary>
        /// 模拟客户端发送消息
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="message">消息对象</param>
        public void SimulateReceive(long sessionId, MessageObject message)
        {
            if (_sessions.ContainsKey(sessionId))
            {
                _clientMessageQueue.Enqueue((sessionId, message));
                ASLogger.Instance.Debug($"本地模式：模拟客户端 {sessionId} 发送消息: {message.GetType().Name}");
            }
            else
            {
                ASLogger.Instance.Warning($"本地模式：会话 {sessionId} 不存在，无法接收消息");
            }
        }

        /// <summary>
        /// 获取指定会话的待发送消息
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <returns>待发送消息列表</returns>
        public List<MessageObject> GetPendingMessages(long sessionId)
        {
            if (_clientOutboxes.TryGetValue(sessionId, out var outbox))
            {
                return outbox.ToList();
            }
            return new List<MessageObject>();
        }

        /// <summary>
        /// 清空指定会话的待发送消息
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        public void ClearPendingMessages(long sessionId)
        {
            if (_clientOutboxes.TryGetValue(sessionId, out var outbox))
            {
                while (outbox.TryDequeue(out _)) { }
            }
        }

        /// <summary>
        /// 获取所有会话的待发送消息统计
        /// </summary>
        /// <returns>会话ID到消息数量的映射</returns>
        public Dictionary<long, int> GetPendingMessageStats()
        {
            var stats = new Dictionary<long, int>();
            foreach (var kvp in _clientOutboxes)
            {
                stats[kvp.Key] = kvp.Value.Count;
            }
            return stats;
        }

        #endregion
    }
}
