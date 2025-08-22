using Astrum.CommonBase;

namespace Astrum.Network.Generated
{
    /// <summary>
    /// 连接状态枚举
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected = 0,      // 已断开连接
        Connecting = 1,        // 正在连接
        Connected = 2,         // 已连接
        Reconnecting = 3,      // 正在重连
        Failed = 4             // 连接失败
    }
}
