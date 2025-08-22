using MemoryPack;
using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.Generated
{
    // 游戏消息定义
    // 用于游戏逻辑相关的网络通信
    // 游戏网络消息
    [MemoryPackable]
    [MessageAttribute(21)]
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
    [MessageAttribute(22)]
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
    [MessageAttribute(23)]
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

    // 游戏输入
    [MemoryPackable]
    [MessageAttribute(24)]
    public partial class GameInput : MessageObject
    {
        public static GameInput Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GameInput), isFromPool) as GameInput;
        }

        /// <summary>
        /// 玩家ID
        /// </summary>
        [MemoryPackOrder(0)]
        public string playerId { get; set; }

        /// <summary>
        /// 帧号
        /// </summary>
        [MemoryPackOrder(1)]
        public int frameNumber { get; set; }

        /// <summary>
        /// 输入数据
        /// </summary>
        [MemoryPackOrder(2)]
        public byte[] inputData { get; set; }

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

            this.playerId = default;
            this.frameNumber = default;
            this.inputData = default;
            this.timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 游戏帧同步
    [MemoryPackable]
    [MessageAttribute(25)]
    public partial class GameFrameSync : MessageObject
    {
        public static GameFrameSync Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GameFrameSync), isFromPool) as GameFrameSync;
        }

        /// <summary>
        /// 帧号
        /// </summary>
        [MemoryPackOrder(0)]
        public int frameNumber { get; set; }

        /// <summary>
        /// 输入列表
        /// </summary>
        [MemoryPackOrder(1)]
        public List<GameInput> inputs { get; set; } = new();

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

            this.frameNumber = default;
            this.inputs.Clear();
            this.timestamp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 游戏结果
    [MemoryPackable]
    [MessageAttribute(26)]
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
    [MessageAttribute(27)]
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
    [MessageAttribute(28)]
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
        /// 回合时间（秒）
        /// </summary>
        [MemoryPackOrder(1)]
        public int roundTime { get; set; }

        /// <summary>
        /// 最大回合数
        /// </summary>
        [MemoryPackOrder(2)]
        public int maxRounds { get; set; }

        /// <summary>
        /// 是否允许观战
        /// </summary>
        [MemoryPackOrder(3)]
        public bool allowSpectators { get; set; }

        /// <summary>
        /// 游戏模式列表
        /// </summary>
        [MemoryPackOrder(4)]
        public List<string> gameModes { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.maxPlayers = default;
            this.roundTime = default;
            this.maxRounds = default;
            this.allowSpectators = default;
            this.gameModes.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    public static class gamemessages
    {
        public const ushort GameNetworkMessage = 21;
        public const ushort GameMessageResponse = 22;
        public const ushort GameRoomState = 23;
        public const ushort GameInput = 24;
        public const ushort GameFrameSync = 25;
        public const ushort GameResult = 26;
        public const ushort PlayerResult = 27;
        public const ushort GameConfig = 28;
    }
}