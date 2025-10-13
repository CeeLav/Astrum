using MemoryPack;
using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.Generated
{
    // 网络通用消息定义
    // 用于客户端和服务器之间的通用通信
    // 用户信息
    [MemoryPackable]
    [MessageAttribute(1001)]
    public partial class UserInfo : MessageObject
    {
        public static UserInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(UserInfo), isFromPool) as UserInfo;
        }

        /// <summary>
        /// 用户ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string Id { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        [MemoryPackOrder(1)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        [MemoryPackOrder(2)]
        public long LastLoginAt { get; set; }

        /// <summary>
        /// 当前房间ID
        /// </summary>
        [MemoryPackOrder(3)]
        public string CurrentRoomId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Id = default;
            this.DisplayName = default;
            this.LastLoginAt = default;
            this.CurrentRoomId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 房间信息
    [MemoryPackable]
    [MessageAttribute(1002)]
    public partial class RoomInfo : MessageObject
    {
        public static RoomInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(RoomInfo), isFromPool) as RoomInfo;
        }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string Id { get; set; }

        /// <summary>
        /// 房间名称
        /// </summary>
        [MemoryPackOrder(1)]
        public string Name { get; set; }

        /// <summary>
        /// 创建者名称
        /// </summary>
        [MemoryPackOrder(2)]
        public string CreatorName { get; set; }

        /// <summary>
        /// 当前玩家数量
        /// </summary>
        [MemoryPackOrder(3)]
        public int CurrentPlayers { get; set; }

        /// <summary>
        /// 最大玩家数量
        /// </summary>
        [MemoryPackOrder(4)]
        public int MaxPlayers { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [MemoryPackOrder(5)]
        public long CreatedAt { get; set; }

        /// <summary>
        /// 玩家名称列表
        /// </summary>
        [MemoryPackOrder(6)]
        public List<string> PlayerNames { get; set; } = new();

        /// <summary>
        /// 房间状态 (0=等待中, 1=游戏中, 2=已结束)
        /// </summary>
        [MemoryPackOrder(7)]
        public int Status { get; set; }

        /// <summary>
        /// 游戏开始时间
        /// </summary>
        [MemoryPackOrder(8)]
        public long GameStartTime { get; set; }

        /// <summary>
        /// 游戏结束时间
        /// </summary>
        [MemoryPackOrder(9)]
        public long GameEndTime { get; set; }

        /// <summary>
        /// 是否为快速匹配房间
        /// </summary>
        [MemoryPackOrder(10)]
        public bool IsQuickMatch { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Id = default;
            this.Name = default;
            this.CreatorName = default;
            this.CurrentPlayers = default;
            this.MaxPlayers = default;
            this.CreatedAt = default;
            this.PlayerNames.Clear();
            this.Status = default;
            this.GameStartTime = default;
            this.GameEndTime = default;
            this.IsQuickMatch = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 登录请求
    [MemoryPackable]
    [MessageAttribute(1003)]
    public partial class LoginRequest : MessageObject
    {
        public static LoginRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(LoginRequest), isFromPool) as LoginRequest;
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        [MemoryPackOrder(0)]
        public string DisplayName { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.DisplayName = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 创建房间请求
    [MemoryPackable]
    [MessageAttribute(1004)]
    public partial class CreateRoomRequest : MessageObject
    {
        public static CreateRoomRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(CreateRoomRequest), isFromPool) as CreateRoomRequest;
        }

        /// <summary>
        /// 房间名称
        /// </summary>
        [MemoryPackOrder(0)]
        public string RoomName { get; set; }

        /// <summary>
        /// 最大玩家数量
        /// </summary>
        [MemoryPackOrder(1)]
        public int MaxPlayers { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(2)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RoomName = default;
            this.MaxPlayers = default;
            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 加入房间请求
    [MemoryPackable]
    [MessageAttribute(1005)]
    public partial class JoinRoomRequest : MessageObject
    {
        public static JoinRoomRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(JoinRoomRequest), isFromPool) as JoinRoomRequest;
        }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string RoomId { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(1)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RoomId = default;
            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 离开房间请求
    [MemoryPackable]
    [MessageAttribute(1006)]
    public partial class LeaveRoomRequest : MessageObject
    {
        public static LeaveRoomRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(LeaveRoomRequest), isFromPool) as LeaveRoomRequest;
        }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string RoomId { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(1)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RoomId = default;
            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 获取房间列表请求
    [MemoryPackable]
    [MessageAttribute(1007)]
    public partial class GetRoomListRequest : MessageObject
    {
        public static GetRoomListRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GetRoomListRequest), isFromPool) as GetRoomListRequest;
        }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(0)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 获取房间列表响应
    [MemoryPackable]
    [MessageAttribute(1008)]
    public partial class GetRoomListResponse : MessageObject
    {
        public static GetRoomListResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GetRoomListResponse), isFromPool) as GetRoomListResponse;
        }

        /// <summary>
        /// 是否成功
        /// </summary>
        [MemoryPackOrder(0)]
        public bool Success { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [MemoryPackOrder(1)]
        public string Message { get; set; }

        /// <summary>
        /// 房间列表
        /// </summary>
        [MemoryPackOrder(2)]
        public List<RoomInfo> Rooms { get; set; } = new();

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(3)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Success = default;
            this.Message = default;
            this.Rooms.Clear();
            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 房间更新通知
    [MemoryPackable]
    [MessageAttribute(1009)]
    public partial class RoomUpdateNotification : MessageObject
    {
        public static RoomUpdateNotification Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(RoomUpdateNotification), isFromPool) as RoomUpdateNotification;
        }

        /// <summary>
        /// 房间信息
        /// </summary>
        [MemoryPackOrder(0)]
        public RoomInfo Room { get; set; }

        /// <summary>
        /// 更新类型
        /// </summary>
        [MemoryPackOrder(1)]
        public string UpdateType { get; set; }

        /// <summary>
        /// 相关用户ID
        /// </summary>
        [MemoryPackOrder(2)]
        public string RelatedUserId { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(3)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Room = default;
            this.UpdateType = default;
            this.RelatedUserId = default;
            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 创建房间响应
    [MemoryPackable]
    [MessageAttribute(1010)]
    public partial class CreateRoomResponse : MessageObject
    {
        public static CreateRoomResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(CreateRoomResponse), isFromPool) as CreateRoomResponse;
        }

        /// <summary>
        /// 是否成功
        /// </summary>
        [MemoryPackOrder(0)]
        public bool Success { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [MemoryPackOrder(1)]
        public string Message { get; set; }

        /// <summary>
        /// 房间信息
        /// </summary>
        [MemoryPackOrder(2)]
        public RoomInfo Room { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(3)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Success = default;
            this.Message = default;
            this.Room = default;
            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 加入房间响应
    [MemoryPackable]
    [MessageAttribute(1011)]
    public partial class JoinRoomResponse : MessageObject
    {
        public static JoinRoomResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(JoinRoomResponse), isFromPool) as JoinRoomResponse;
        }

        /// <summary>
        /// 是否成功
        /// </summary>
        [MemoryPackOrder(0)]
        public bool Success { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [MemoryPackOrder(1)]
        public string Message { get; set; }

        /// <summary>
        /// 房间信息
        /// </summary>
        [MemoryPackOrder(2)]
        public RoomInfo Room { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(3)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Success = default;
            this.Message = default;
            this.Room = default;
            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 离开房间响应
    [MemoryPackable]
    [MessageAttribute(1012)]
    public partial class LeaveRoomResponse : MessageObject
    {
        public static LeaveRoomResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(LeaveRoomResponse), isFromPool) as LeaveRoomResponse;
        }

        /// <summary>
        /// 是否成功
        /// </summary>
        [MemoryPackOrder(0)]
        public bool Success { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [MemoryPackOrder(1)]
        public string Message { get; set; }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(2)]
        public string RoomId { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(3)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Success = default;
            this.Message = default;
            this.RoomId = default;
            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 登录响应
    [MemoryPackable]
    [MessageAttribute(1013)]
    public partial class LoginResponse : MessageObject
    {
        public static LoginResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(LoginResponse), isFromPool) as LoginResponse;
        }

        /// <summary>
        /// 是否成功
        /// </summary>
        [MemoryPackOrder(0)]
        public bool Success { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [MemoryPackOrder(1)]
        public string Message { get; set; }

        /// <summary>
        /// 用户信息
        /// </summary>
        [MemoryPackOrder(2)]
        public UserInfo User { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(3)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Success = default;
            this.Message = default;
            this.User = default;
            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 心跳消息
    [MemoryPackable]
    [MessageAttribute(1014)]
    public partial class HeartbeatMessage : MessageObject
    {
        public static HeartbeatMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(HeartbeatMessage), isFromPool) as HeartbeatMessage;
        }

        /// <summary>
        /// 客户端ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string ClientId { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(1)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ClientId = default;
            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 心跳响应
    [MemoryPackable]
    [MessageAttribute(1015)]
    public partial class HeartbeatResponse : MessageObject
    {
        public static HeartbeatResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(HeartbeatResponse), isFromPool) as HeartbeatResponse;
        }

        /// <summary>
        /// 响应时间戳
        /// </summary>
        [MemoryPackOrder(0)]
        public long Timestamp { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [MemoryPackOrder(1)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Timestamp = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 客户端 -> 服务器：Ping 请求（用于时间校准/测延迟）
    [MemoryPackable]
    [MessageAttribute(1016)]
    public partial class C2G_Ping : MessageObject
    {
        public static C2G_Ping Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_Ping), isFromPool) as C2G_Ping;
        }

        /// <summary>
        /// 客户端发送时间戳（可选）
        /// </summary>
        [MemoryPackOrder(0)]
        public long ClientSendTime { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ClientSendTime = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 服务器 -> 客户端：Ping 响应
    [MemoryPackable]
    [MessageAttribute(1017)]
    public partial class G2C_Ping : MessageObject
    {
        public static G2C_Ping Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_Ping), isFromPool) as G2C_Ping;
        }

        /// <summary>
        /// 服务器当前时间戳（毫秒）
        /// </summary>
        [MemoryPackOrder(0)]
        public long Time { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Time = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // ==================== 快速匹配系统 ====================
    // 快速匹配请求
    [MemoryPackable]
    [MessageAttribute(1018)]
    public partial class QuickMatchRequest : MessageObject
    {
        public static QuickMatchRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(QuickMatchRequest), isFromPool) as QuickMatchRequest;
        }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(0)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 快速匹配响应
    [MemoryPackable]
    [MessageAttribute(1019)]
    public partial class QuickMatchResponse : MessageObject
    {
        public static QuickMatchResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(QuickMatchResponse), isFromPool) as QuickMatchResponse;
        }

        /// <summary>
        /// 是否成功
        /// </summary>
        [MemoryPackOrder(0)]
        public bool Success { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [MemoryPackOrder(1)]
        public string Message { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(2)]
        public long Timestamp { get; set; }

        /// <summary>
        /// 队列位置（0表示第一个）
        /// </summary>
        [MemoryPackOrder(3)]
        public int QueuePosition { get; set; }

        /// <summary>
        /// 当前队列人数
        /// </summary>
        [MemoryPackOrder(4)]
        public int QueueSize { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Success = default;
            this.Message = default;
            this.Timestamp = default;
            this.QueuePosition = default;
            this.QueueSize = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 取消匹配请求
    [MemoryPackable]
    [MessageAttribute(1020)]
    public partial class CancelMatchRequest : MessageObject
    {
        public static CancelMatchRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(CancelMatchRequest), isFromPool) as CancelMatchRequest;
        }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(0)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 取消匹配响应
    [MemoryPackable]
    [MessageAttribute(1021)]
    public partial class CancelMatchResponse : MessageObject
    {
        public static CancelMatchResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(CancelMatchResponse), isFromPool) as CancelMatchResponse;
        }

        /// <summary>
        /// 是否成功
        /// </summary>
        [MemoryPackOrder(0)]
        public bool Success { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [MemoryPackOrder(1)]
        public string Message { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(2)]
        public long Timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Success = default;
            this.Message = default;
            this.Timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 匹配成功通知
    [MemoryPackable]
    [MessageAttribute(1022)]
    public partial class MatchFoundNotification : MessageObject
    {
        public static MatchFoundNotification Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(MatchFoundNotification), isFromPool) as MatchFoundNotification;
        }

        /// <summary>
        /// 房间信息
        /// </summary>
        [MemoryPackOrder(0)]
        public RoomInfo Room { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(1)]
        public long Timestamp { get; set; }

        /// <summary>
        /// 所有匹配到的玩家ID列表
        /// </summary>
        [MemoryPackOrder(2)]
        public List<string> PlayerIds { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Room = default;
            this.Timestamp = default;
            this.PlayerIds.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 匹配超时通知
    [MemoryPackable]
    [MessageAttribute(1023)]
    public partial class MatchTimeoutNotification : MessageObject
    {
        public static MatchTimeoutNotification Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(MatchTimeoutNotification), isFromPool) as MatchTimeoutNotification;
        }

        /// <summary>
        /// 超时消息
        /// </summary>
        [MemoryPackOrder(0)]
        public string Message { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(1)]
        public long Timestamp { get; set; }

        /// <summary>
        /// 等待时长（秒）
        /// </summary>
        [MemoryPackOrder(2)]
        public int WaitTimeSeconds { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Message = default;
            this.Timestamp = default;
            this.WaitTimeSeconds = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    public static class networkcommon
    {
        public const ushort UserInfo = 1001;
        public const ushort RoomInfo = 1002;
        public const ushort LoginRequest = 1003;
        public const ushort CreateRoomRequest = 1004;
        public const ushort JoinRoomRequest = 1005;
        public const ushort LeaveRoomRequest = 1006;
        public const ushort GetRoomListRequest = 1007;
        public const ushort GetRoomListResponse = 1008;
        public const ushort RoomUpdateNotification = 1009;
        public const ushort CreateRoomResponse = 1010;
        public const ushort JoinRoomResponse = 1011;
        public const ushort LeaveRoomResponse = 1012;
        public const ushort LoginResponse = 1013;
        public const ushort HeartbeatMessage = 1014;
        public const ushort HeartbeatResponse = 1015;
        public const ushort C2G_Ping = 1016;
        public const ushort G2C_Ping = 1017;
        public const ushort QuickMatchRequest = 1018;
        public const ushort QuickMatchResponse = 1019;
        public const ushort CancelMatchRequest = 1020;
        public const ushort CancelMatchResponse = 1021;
        public const ushort MatchFoundNotification = 1022;
        public const ushort MatchTimeoutNotification = 1023;
    }
}