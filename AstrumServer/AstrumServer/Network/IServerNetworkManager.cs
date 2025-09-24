using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Network;

namespace AstrumServer.Network
{
    /// <summary>
    /// 服务器网络管理器接口，支持网络模式和本地模式
    /// </summary>
    public interface IServerNetworkManager
    {
        /// <summary>
        /// 客户端连接事件
        /// </summary>
        event Action<Session>? OnClientConnected;
        
        /// <summary>
        /// 客户端断开连接事件
        /// </summary>
        event Action<Session>? OnClientDisconnected;
        
        /// <summary>
        /// 收到消息事件
        /// </summary>
        event Action<Session, MessageObject>? OnMessageReceived;
        
        /// <summary>
        /// 网络错误事件
        /// </summary>
        event Action<Session, Exception>? OnError;

        /// <summary>
        /// 初始化网络服务
        /// </summary>
        /// <param name="port">监听端口</param>
        /// <returns>是否初始化成功</returns>
        Task<bool> InitializeAsync(int port = 8888);

        /// <summary>
        /// 更新网络服务
        /// </summary>
        void Update();

        /// <summary>
        /// 发送消息到指定会话
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="message">消息对象</param>
        void SendMessage(string sessionId, MessageObject message);

        /// <summary>
        /// 广播消息到所有会话
        /// </summary>
        /// <param name="message">消息对象</param>
        void BroadcastMessage(MessageObject message);

        /// <summary>
        /// 断开指定会话
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        void DisconnectSession(long sessionId);

        /// <summary>
        /// 获取会话数量
        /// </summary>
        /// <returns>当前会话数量</returns>
        int GetSessionCount();

        /// <summary>
        /// 获取所有活跃会话
        /// </summary>
        /// <returns>活跃会话列表</returns>
        List<Session> GetActiveSessions();

        /// <summary>
        /// 关闭网络服务
        /// </summary>
        void Shutdown();

        /// <summary>
        /// 设置日志记录器
        /// </summary>
        /// <param name="logger">日志记录器</param>
        void SetLogger(ASLogger logger);
    }
}
