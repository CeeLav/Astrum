using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Network;

namespace AstrumServer.Network
{
    /// <summary>
    /// 内存网络实现：不启端口，单进程内模拟会话/消息，适用于测试
    /// </summary>
    public class InMemoryServerNetwork : IServerNetwork
    {
        private readonly ConcurrentDictionary<long, Session> _sessions = new();
        private long _nextSessionId = 1;

        public event Action<Session>? OnClientConnected;
        public event Action<Session>? OnClientDisconnected;
        public event Action<Session, MessageObject>? OnMessageReceived;
        public event Action<Session, Exception>? OnError;

        public Task<bool> InitializeAsync(int port = 0)
        {
            // 无需真实初始化
            ASLogger.Instance.Info("InMemoryServerNetwork initialized");
            return Task.FromResult(true);
        }

        public void Update()
        {
            // 无操作
        }

        public void SendMessage(string sessionId, MessageObject message)
        {
            if (long.TryParse(sessionId, out var id) && _sessions.TryGetValue(id, out var session))
            {
                // 在内存中直接认为发送成功；测试可通过钩子自行验证
            }
        }

        public void BroadcastMessage(MessageObject message)
        {
            // no-op for tests
        }

        public void DisconnectSession(long sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                OnClientDisconnected?.Invoke(session);
            }
        }

        public int GetSessionCount() => _sessions.Count;

        public void Shutdown()
        {
            foreach (var kv in _sessions)
            {
                OnClientDisconnected?.Invoke(kv.Value);
            }
            _sessions.Clear();
        }

        public void SetLogger(ASLogger logger) { }

        // 测试辅助：模拟连接、断开、接收
        public long SimulateConnect(string remote = "127.0.0.1", int port = 0)
        {
            var id = System.Threading.Interlocked.Increment(ref _nextSessionId);
            var s = new Session();
            s.Initialize(null, id, new IPEndPoint(IPAddress.Parse(remote), port));
            _sessions[id] = s;
            OnClientConnected?.Invoke(s);
            return id;
        }

        public void SimulateDisconnect(long sessionId, bool abrupt = true)
        {
            if (_sessions.TryRemove(sessionId, out var s))
            {
                if (abrupt)
                {
                    OnError?.Invoke(s, new Exception("Abrupt disconnect"));
                }
                OnClientDisconnected?.Invoke(s);
            }
        }

        public void SimulateReceive(long sessionId, MessageObject message)
        {
            if (_sessions.TryGetValue(sessionId, out var s))
            {
                OnMessageReceived?.Invoke(s, message);
            }
        }
    }
}

