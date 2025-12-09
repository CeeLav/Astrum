using MemoryPack;
using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.Generated
{
    // 游戏消息定义
    // 用于游戏逻辑相关的网络通信
    // 游戏网络消息
    [MemoryPackable]
    [MessageAttribute(2001)]
    [ResponseType(nameof(GameMessageResponse))]
    public partial class GameNetworkMessage : MessageObject
    {
        public static GameNetworkMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GameNetworkMessage), isFromPool) as GameNetworkMessage;
        }

        /// <summary>
        /// 消息类型
        /// </summary>
        [MemoryPackOrder(0)]
        public string type { get; set; }

        /// <summary>
        /// 消息数据
        /// </summary>
        [MemoryPackOrder(1)]
        public byte[] data { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        [MemoryPackOrder(2)]
        public string error { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        [MemoryPackOrder(3)]
        public bool success { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(4)]
        public long timestamp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.type = default;
            this.data = default;
            this.error = default;
            this.success = default;
            this.timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 游戏消息响应
    [MemoryPackable]
    [MessageAttribute(2002)]
    public partial class GameMessageResponse : MessageObject
    {
        public static GameMessageResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GameMessageResponse), isFromPool) as GameMessageResponse;
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

    // 游戏房间状态
    [MemoryPackable]
    [MessageAttribute(2003)]
    public partial class GameRoomState : MessageObject
    {
        public static GameRoomState Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GameRoomState), isFromPool) as GameRoomState;
        }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string roomId { get; set; }

        /// <summary>
        /// 当前回合
        /// </summary>
        [MemoryPackOrder(2)]
        public int currentRound { get; set; }

        /// <summary>
        /// 最大回合数
        /// </summary>
        [MemoryPackOrder(3)]
        public int maxRounds { get; set; }

        /// <summary>
        /// 回合开始时间
        /// </summary>
        [MemoryPackOrder(4)]
        public long roundStartTime { get; set; }

        /// <summary>
        /// 活跃玩家列表
        /// </summary>
        [MemoryPackOrder(5)]
        public List<string> activePlayers { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.roomId = default;
            this.currentRound = default;
            this.maxRounds = default;
            this.roundStartTime = default;
            this.activePlayers.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 帧同步输入数据
    [MemoryPackable]
    [MessageAttribute(2004)]
    public partial class LSInput : MessageObject
    {
        public static LSInput Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(LSInput), isFromPool) as LSInput;
        }

        /// <summary>
        /// 玩家ID
        /// </summary>
        [MemoryPackOrder(0)]
        public long PlayerId { get; set; }

        /// <summary>
        /// 帧号（下发帧号：服务器实际下发的帧号）
        /// </summary>
        [MemoryPackOrder(1)]
        public int Frame { get; set; }

        /// <summary>
        /// X轴移动输入（Q31.32 定点数）
        /// </summary>
        [MemoryPackOrder(2)]
        public long MoveX { get; set; }

        /// <summary>
        /// Y轴移动输入（Q31.32 定点数）
        /// </summary>
        [MemoryPackOrder(3)]
        public long MoveY { get; set; }

        /// <summary>
        /// 攻击输入
        /// </summary>
        [MemoryPackOrder(4)]
        public bool Attack { get; set; }

        /// <summary>
        /// 技能1输入
        /// </summary>
        [MemoryPackOrder(5)]
        public bool Skill1 { get; set; }

        /// <summary>
        /// 技能2输入
        /// </summary>
        [MemoryPackOrder(6)]
        public bool Skill2 { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [MemoryPackOrder(7)]
        public long Timestamp { get; set; }

        /// <summary>
        /// 出生信息
        /// </summary>
        [MemoryPackOrder(8)]
        public int BornInfo { get; set; }

        /// <summary>
        /// 翻滚输入
        /// </summary>
        [MemoryPackOrder(9)]
        public bool Roll { get; set; }

        /// <summary>
        /// 冲刺输入
        /// </summary>
        [MemoryPackOrder(10)]
        public bool Dash { get; set; }

        /// <summary>
        /// 鼠标世界坐标X（Q31.32 定点数）
        /// </summary>
        [MemoryPackOrder(11)]
        public long MouseWorldX { get; set; }

        /// <summary>
        /// 鼠标世界坐标Z（Q31.32 定点数）
        /// </summary>
        [MemoryPackOrder(12)]
        public long MouseWorldZ { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PlayerId = default;
            this.Frame = default;
            this.MoveX = default;
            this.MoveY = default;
            this.Attack = default;
            this.Skill1 = default;
            this.Skill2 = default;
            this.Timestamp = default;
            this.BornInfo = default;
            this.Roll = default;
            this.Dash = default;
            this.MouseWorldX = default;
            this.MouseWorldZ = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [MessageAttribute(2005)]
    public partial class OneFrameInputs : MessageObject, IMessage
    {
        public static OneFrameInputs Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(OneFrameInputs), isFromPool) as OneFrameInputs;
        }

        /// <summary>
        /// 玩家ID -> 输入数据映射
        /// </summary>
        [MemoryPackOrder(0)]
        public Dictionary<long, LSInput> Inputs { get; set; } = new();
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Inputs.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 游戏结果
    [MemoryPackable]
    [MessageAttribute(2006)]
    public partial class GameResult : MessageObject
    {
        public static GameResult Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GameResult), isFromPool) as GameResult;
        }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string roomId { get; set; }

        /// <summary>
        /// 玩家结果列表
        /// </summary>
        [MemoryPackOrder(1)]
        public List<PlayerResult> playerResults { get; set; } = new();

        /// <summary>
        /// 结束时间
        /// </summary>
        [MemoryPackOrder(2)]
        public long endTime { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.roomId = default;
            this.playerResults.Clear();
            this.endTime = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 玩家结果
    [MemoryPackable]
    [MessageAttribute(2007)]
    public partial class PlayerResult : MessageObject
    {
        public static PlayerResult Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(PlayerResult), isFromPool) as PlayerResult;
        }

        /// <summary>
        /// 玩家ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string playerId { get; set; }

        /// <summary>
        /// 分数
        /// </summary>
        [MemoryPackOrder(1)]
        public int score { get; set; }

        /// <summary>
        /// 排名
        /// </summary>
        [MemoryPackOrder(2)]
        public int rank { get; set; }

        /// <summary>
        /// 是否获胜
        /// </summary>
        [MemoryPackOrder(3)]
        public bool isWinner { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.playerId = default;
            this.score = default;
            this.rank = default;
            this.isWinner = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 游戏配置
    [MemoryPackable]
    [MessageAttribute(2008)]
    public partial class GameConfig : MessageObject
    {
        public static GameConfig Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GameConfig), isFromPool) as GameConfig;
        }

        /// <summary>
        /// 最大玩家数
        /// </summary>
        [MemoryPackOrder(0)]
        public int maxPlayers { get; set; }

        /// <summary>
        /// 最小玩家数
        /// </summary>
        [MemoryPackOrder(1)]
        public int minPlayers { get; set; }

        /// <summary>
        /// 回合时间（秒）
        /// </summary>
        [MemoryPackOrder(2)]
        public int roundTime { get; set; }

        /// <summary>
        /// 最大回合数
        /// </summary>
        [MemoryPackOrder(3)]
        public int maxRounds { get; set; }

        /// <summary>
        /// 是否允许观战
        /// </summary>
        [MemoryPackOrder(4)]
        public bool allowSpectators { get; set; }

        /// <summary>
        /// 游戏模式列表
        /// </summary>
        [MemoryPackOrder(5)]
        public List<string> gameModes { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.maxPlayers = default;
            this.minPlayers = default;
            this.roundTime = default;
            this.maxRounds = default;
            this.allowSpectators = default;
            this.gameModes.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 游戏开始通知
    [MemoryPackable]
    [MessageAttribute(2009)]
    public partial class GameStartNotification : MessageObject
    {
        public static GameStartNotification Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GameStartNotification), isFromPool) as GameStartNotification;
        }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string roomId { get; set; }

        /// <summary>
        /// 游戏配置
        /// </summary>
        [MemoryPackOrder(1)]
        public GameConfig config { get; set; }

        /// <summary>
        /// 房间状态
        /// </summary>
        [MemoryPackOrder(2)]
        public GameRoomState roomState { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        [MemoryPackOrder(3)]
        public long startTime { get; set; }

        /// <summary>
        /// 玩家ID列表
        /// </summary>
        [MemoryPackOrder(4)]
        public List<string> playerIds { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.roomId = default;
            this.config = default;
            this.roomState = default;
            this.startTime = default;
            this.playerIds.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 游戏结束通知
    [MemoryPackable]
    [MessageAttribute(2010)]
    public partial class GameEndNotification : MessageObject
    {
        public static GameEndNotification Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GameEndNotification), isFromPool) as GameEndNotification;
        }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string roomId { get; set; }

        /// <summary>
        /// 游戏结果
        /// </summary>
        [MemoryPackOrder(1)]
        public GameResult result { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        [MemoryPackOrder(2)]
        public long endTime { get; set; }

        /// <summary>
        /// 结束原因
        /// </summary>
        [MemoryPackOrder(3)]
        public string reason { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.roomId = default;
            this.result = default;
            this.endTime = default;
            this.reason = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 游戏状态更新
    [MemoryPackable]
    [MessageAttribute(2011)]
    public partial class GameStateUpdate : MessageObject
    {
        public static GameStateUpdate Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GameStateUpdate), isFromPool) as GameStateUpdate;
        }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string roomId { get; set; }

        /// <summary>
        /// 房间状态
        /// </summary>
        [MemoryPackOrder(1)]
        public GameRoomState roomState { get; set; }

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

            this.roomId = default;
            this.roomState = default;
            this.timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 帧同步开始通知
    [MemoryPackable]
    [MessageAttribute(2012)]
    public partial class FrameSyncStartNotification : MessageObject
    {
        public static FrameSyncStartNotification Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(FrameSyncStartNotification), isFromPool) as FrameSyncStartNotification;
        }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string roomId { get; set; }

        /// <summary>
        /// 帧率 (20FPS)
        /// </summary>
        [MemoryPackOrder(1)]
        public int frameRate { get; set; }

        /// <summary>
        /// 帧间隔 (50ms)
        /// </summary>
        [MemoryPackOrder(2)]
        public int frameInterval { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        [MemoryPackOrder(3)]
        public long startTime { get; set; }

        /// <summary>
        /// 玩家ID列表（UserId）
        /// </summary>
        [MemoryPackOrder(4)]
        public List<string> playerIds { get; set; } = new();

        /// <summary>
        /// UserId -> PlayerId 映射
        /// </summary>
        [MemoryPackOrder(5)]
        public Dictionary<string, long> playerIdMapping { get; set; } = new();
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.roomId = default;
            this.frameRate = default;
            this.frameInterval = default;
            this.startTime = default;
            this.playerIds.Clear();
            this.playerIdMapping.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 世界快照开始消息 - 用于标识世界数据传输的开始
    // 发送顺序：先发送 WorldSnapshotStart，然后发送 WorldSnapshotChunk 分片，最后发送 FrameSyncStartNotification
    [MemoryPackable]
    [MessageAttribute(2013)]
    public partial class WorldSnapshotStart : MessageObject
    {
        public static WorldSnapshotStart Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(WorldSnapshotStart), isFromPool) as WorldSnapshotStart;
        }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string roomId { get; set; }

        /// <summary>
        /// 总分片数
        /// </summary>
        [MemoryPackOrder(1)]
        public int totalChunks { get; set; }

        /// <summary>
        /// 完整快照的总大小（字节）
        /// </summary>
        [MemoryPackOrder(2)]
        public int totalSize { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.roomId = default;
            this.totalChunks = default;
            this.totalSize = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 帧同步结束通知
    [MemoryPackable]
    [MessageAttribute(2014)]
    public partial class FrameSyncEndNotification : MessageObject
    {
        public static FrameSyncEndNotification Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(FrameSyncEndNotification), isFromPool) as FrameSyncEndNotification;
        }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string roomId { get; set; }

        /// <summary>
        /// 最终帧号
        /// </summary>
        [MemoryPackOrder(1)]
        public int finalFrame { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        [MemoryPackOrder(2)]
        public long endTime { get; set; }

        /// <summary>
        /// 结束原因
        /// </summary>
        [MemoryPackOrder(3)]
        public string reason { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.roomId = default;
            this.finalFrame = default;
            this.endTime = default;
            this.reason = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 帧同步数据消息
    [MemoryPackable]
    [MessageAttribute(2015)]
    public partial class FrameSyncData : MessageObject
    {
        public static FrameSyncData Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(FrameSyncData), isFromPool) as FrameSyncData;
        }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string roomId { get; set; }

        /// <summary>
        /// 权威帧号
        /// </summary>
        [MemoryPackOrder(1)]
        public int authorityFrame { get; set; }

        /// <summary>
        /// 帧输入数据
        /// </summary>
        [MemoryPackOrder(2)]
        public OneFrameInputs frameInputs { get; set; }

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

            this.roomId = default;
            this.authorityFrame = default;
            this.frameInputs = default;
            this.timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 帧同步上传数据
    [MemoryPackable]
    [MessageAttribute(2016)]
    public partial class SingleInput : MessageObject
    {
        public static SingleInput Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(SingleInput), isFromPool) as SingleInput;
        }

        /// <summary>
        /// 帧号
        /// </summary>
        [MemoryPackOrder(0)]
        public int FrameID { get; set; }

        /// <summary>
        /// 玩家ID
        /// </summary>
        [MemoryPackOrder(1)]
        public long PlayerID { get; set; }

        /// <summary>
        /// 输入数据
        /// </summary>
        [MemoryPackOrder(2)]
        public LSInput Input { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.FrameID = default;
            this.PlayerID = default;
            this.Input = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 世界快照分片消息 - 用于分片发送世界快照数据
    // 发送顺序：先发送 WorldSnapshotStart，然后发送 WorldSnapshotChunk 分片，最后发送 FrameSyncStartNotification
    [MemoryPackable]
    [MessageAttribute(2017)]
    public partial class WorldSnapshotChunk : MessageObject
    {
        public static WorldSnapshotChunk Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(WorldSnapshotChunk), isFromPool) as WorldSnapshotChunk;
        }

        /// <summary>
        /// 房间ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string roomId { get; set; }

        /// <summary>
        /// 分片索引（从0开始）
        /// </summary>
        [MemoryPackOrder(1)]
        public int chunkIndex { get; set; }

        /// <summary>
        /// 分片数据
        /// </summary>
        [MemoryPackOrder(2)]
        public byte[] chunkData { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.roomId = default;
            this.chunkIndex = default;
            this.chunkData = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    public static class gamemessages
    {
        public const ushort GameNetworkMessage = 2001;
        public const ushort GameMessageResponse = 2002;
        public const ushort GameRoomState = 2003;
        public const ushort LSInput = 2004;
        public const ushort OneFrameInputs = 2005;
        public const ushort GameResult = 2006;
        public const ushort PlayerResult = 2007;
        public const ushort GameConfig = 2008;
        public const ushort GameStartNotification = 2009;
        public const ushort GameEndNotification = 2010;
        public const ushort GameStateUpdate = 2011;
        public const ushort FrameSyncStartNotification = 2012;
        public const ushort WorldSnapshotStart = 2013;
        public const ushort FrameSyncEndNotification = 2014;
        public const ushort FrameSyncData = 2015;
        public const ushort SingleInput = 2016;
        public const ushort WorldSnapshotChunk = 2017;
    }
}