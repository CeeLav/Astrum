using System;
using System.Threading.Tasks;
using Astrum.CommonBase;

namespace AstrumServer.Network
{
    /// <summary>
    /// 适配现有 ServerNetworkManager 到 IServerNetwork 接口
    /// </summary>
    public class ServerNetworkAdapter : IServerNetwork
    {
        private readonly ServerNetworkManager _inner;

        public ServerNetworkAdapter(ServerNetworkManager inner)
        {
            _inner = inner;
            _inner.OnClientConnected += s => OnClientConnected?.Invoke(s);
            _inner.OnClientDisconnected += s => OnClientDisconnected?.Invoke(s);
            _inner.OnMessageReceived += (s, m) => OnMessageReceived?.Invoke(s, m);
            _inner.OnError += (s, e) => OnError?.Invoke(s, e);
        }

        public event Action<Astrum.Network.Session>? OnClientConnected;
        public event Action<Astrum.Network.Session>? OnClientDisconnected;
        public event Action<Astrum.Network.Session, Astrum.CommonBase.MessageObject>? OnMessageReceived;
        public event Action<Astrum.Network.Session, Exception>? OnError;

        public Task<bool> InitializeAsync(int port = 8888) => _inner.InitializeAsync(port);
        public void Update() => _inner.Update();
        public void SendMessage(string sessionId, Astrum.CommonBase.MessageObject message) => _inner.SendMessage(sessionId, message);
        public void BroadcastMessage(Astrum.CommonBase.MessageObject message) => _inner.BroadcastMessage(message);
        public void DisconnectSession(long sessionId) => _inner.DisconnectSession(sessionId);
        public int GetSessionCount() => _inner.GetSessionCount();
        public void Shutdown() => _inner.Shutdown();
        public void SetLogger(ASLogger logger) => _inner.SetLogger(logger);
    }
}

