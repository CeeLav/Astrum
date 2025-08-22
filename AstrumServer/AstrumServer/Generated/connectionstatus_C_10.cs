using MemoryPack;
using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.Generated
{
    // 连接状态枚举定义
    // 用于表示网络连接的各种状态
    // 连接状态响应
    [MemoryPackable]
    [MessageAttribute(11)]
    [ResponseType(nameof(ConnectionStatusResponse))]
    public partial class ConnectionStatusResponse : MessageObject
    {
        public static ConnectionStatusResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ConnectionStatusResponse), isFromPool) as ConnectionStatusResponse;
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        [MemoryPackOrder(1)]
        public string message { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(2)]
        public long timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.message = default;
            this.timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 连接状态变化消息
    [MemoryPackable]
    [MessageAttribute(12)]
    public partial class ConnectionStatusChanged : MessageObject
    {
        public static ConnectionStatusChanged Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ConnectionStatusChanged), isFromPool) as ConnectionStatusChanged;
        }

        /// <summary>
        /// 状态变化原因
        /// </summary>
        [MemoryPackOrder(2)]
        public string reason { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(3)]
        public long timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.reason = default;
            this.timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 连接请求
    [MemoryPackable]
    [MessageAttribute(13)]
    public partial class ConnectRequest : MessageObject
    {
        public static ConnectRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ConnectRequest), isFromPool) as ConnectRequest;
        }

        /// <summary>
        /// 服务器地址
        /// </summary>
        [MemoryPackOrder(0)]
        public string serverAddress { get; set; }

        /// <summary>
        /// 服务器端口
        /// </summary>
        [MemoryPackOrder(1)]
        public int serverPort { get; set; }

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        [MemoryPackOrder(2)]
        public int timeout { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.serverAddress = default;
            this.serverPort = default;
            this.timeout = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 连接响应
    [MemoryPackable]
    [MessageAttribute(14)]
    public partial class ConnectResponse : MessageObject
    {
        public static ConnectResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ConnectResponse), isFromPool) as ConnectResponse;
        }

        /// <summary>
        /// 是否成功
        /// </summary>
        [MemoryPackOrder(0)]
        public bool success { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [MemoryPackOrder(1)]
        public string message { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(2)]
        public long timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.success = default;
            this.message = default;
            this.timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 断开连接请求
    [MemoryPackable]
    [MessageAttribute(15)]
    public partial class DisconnectRequest : MessageObject
    {
        public static DisconnectRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(DisconnectRequest), isFromPool) as DisconnectRequest;
        }

        /// <summary>
        /// 断开原因
        /// </summary>
        [MemoryPackOrder(0)]
        public string reason { get; set; }

        /// <summary>
        /// 是否强制断开
        /// </summary>
        [MemoryPackOrder(1)]
        public bool force { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.reason = default;
            this.force = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 断开连接响应
    [MemoryPackable]
    [MessageAttribute(16)]
    public partial class DisconnectResponse : MessageObject
    {
        public static DisconnectResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(DisconnectResponse), isFromPool) as DisconnectResponse;
        }

        /// <summary>
        /// 是否成功
        /// </summary>
        [MemoryPackOrder(0)]
        public bool success { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [MemoryPackOrder(1)]
        public string message { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(2)]
        public long timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.success = default;
            this.message = default;
            this.timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    public static class connectionstatus
    {
        public const ushort ConnectionStatusResponse = 11;
        public const ushort ConnectionStatusChanged = 12;
        public const ushort ConnectRequest = 13;
        public const ushort ConnectResponse = 14;
        public const ushort DisconnectRequest = 15;
        public const ushort DisconnectResponse = 16;
    }
}