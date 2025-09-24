using System;
using Astrum.CommonBase;

namespace AstrumServer.Network
{
    /// <summary>
    /// 网络管理器模式枚举
    /// </summary>
    public enum NetworkManagerMode
    {
        /// <summary>
        /// 网络模式：使用真实TCP网络通信
        /// </summary>
        Network,
        
        /// <summary>
        /// 本地模式：使用内存模拟，用于测试
        /// </summary>
        Local
    }

    /// <summary>
    /// 网络管理器工厂 - 根据模式创建相应的网络管理器
    /// </summary>
    public static class NetworkManagerFactory
    {
        /// <summary>
        /// 创建网络管理器实例
        /// </summary>
        /// <param name="mode">网络管理器模式</param>
        /// <returns>网络管理器实例</returns>
        public static IServerNetworkManager Create(NetworkManagerMode mode)
        {
            switch (mode)
            {
                case NetworkManagerMode.Network:
                    return CreateNetworkManager();
                    
                case NetworkManagerMode.Local:
                    return CreateLocalManager();
                    
                default:
                    throw new ArgumentException($"不支持的网络管理器模式: {mode}");
            }
        }

        /// <summary>
        /// 创建网络模式管理器
        /// </summary>
        /// <returns>网络模式管理器实例</returns>
        private static IServerNetworkManager CreateNetworkManager()
        {
            var manager = ServerNetworkManager.Instance;
            manager.SetLogger(ASLogger.Instance);
            return manager;
        }

        /// <summary>
        /// 创建本地模式管理器
        /// </summary>
        /// <returns>本地模式管理器实例</returns>
        private static IServerNetworkManager CreateLocalManager()
        {
            var manager = new LocalServerNetworkManager();
            manager.SetLogger(ASLogger.Instance);
            return manager;
        }

        /// <summary>
        /// 根据环境变量创建网络管理器
        /// 环境变量 NETWORK_MODE 可以设置为 "Network" 或 "Local"
        /// </summary>
        /// <returns>网络管理器实例</returns>
        public static IServerNetworkManager CreateFromEnvironment()
        {
            var modeString = Environment.GetEnvironmentVariable("NETWORK_MODE");
            
            if (string.IsNullOrEmpty(modeString))
            {
                // 默认使用网络模式
                return Create(NetworkManagerMode.Network);
            }

            if (Enum.TryParse<NetworkManagerMode>(modeString, true, out var mode))
            {
                return Create(mode);
            }

            throw new ArgumentException($"无效的网络模式环境变量: {modeString}，支持的值: Network, Local");
        }

        /// <summary>
        /// 创建网络模式管理器（便捷方法）
        /// </summary>
        /// <returns>网络模式管理器实例</returns>
        public static IServerNetworkManager CreateNetwork()
        {
            return Create(NetworkManagerMode.Network);
        }

        /// <summary>
        /// 创建本地模式管理器（便捷方法）
        /// </summary>
        /// <returns>本地模式管理器实例</returns>
        public static IServerNetworkManager CreateLocal()
        {
            return Create(NetworkManagerMode.Local);
        }
    }
}
