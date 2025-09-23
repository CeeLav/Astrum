using System;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Network;

namespace AstrumServer.Network
{
    /// <summary>
    /// 可替换的服务器网络接口，便于单进程内存测试或真实TCP实现
    /// </summary>
    public interface IServerNetwork
    {
        event Action<Session>? OnClientConnected;
        event Action<Session>? OnClientDisconnected;
        event Action<Session, MessageObject>? OnMessageReceived;
        event Action<Session, Exception>? OnError;

        Task<bool> InitializeAsync(int port = 8888);
        void Update();
        void SendMessage(string sessionId, MessageObject message);
        void BroadcastMessage(MessageObject message);
        void DisconnectSession(long sessionId);
        int GetSessionCount();
        void Shutdown();
        void SetLogger(ASLogger logger);
    }
}

