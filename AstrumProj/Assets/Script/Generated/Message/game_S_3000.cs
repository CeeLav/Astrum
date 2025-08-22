using MemoryPack;
using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.Generated
{
    // 游戏服务端协议
    [MemoryPackable]
    [MessageAttribute(3001)]
    [ResponseType(nameof(GameResponse))]
    public partial class PlayerInfo : MessageObject
    {
        public static PlayerInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(PlayerInfo), isFromPool) as PlayerInfo;
        }

        /// <summary>
        /// 玩家ID
        /// </summary>
        [MemoryPackOrder(0)]
        public long playerId { get; set; }

        /// <summary>
        /// 玩家名称
        /// </summary>
        [MemoryPackOrder(1)]
        public string playerName { get; set; }

        /// <summary>
        /// 玩家等级
        /// </summary>
        [MemoryPackOrder(2)]
        public int level { get; set; }

        /// <summary>
        /// 技能列表
        /// </summary>
        [MemoryPackOrder(3)]
        public List<int> skills { get; set; } = new();

        /// <summary>
        /// 属性字典
        /// </summary>
        [MemoryPackOrder(4)]
        public Dictionary<string, int> attributes { get; set; } = new();
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.playerId = default;
            this.playerName = default;
            this.level = default;
            this.skills.Clear();
            this.attributes.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [MessageAttribute(3002)]
    public partial class GameRequest : MessageObject
    {
        public static GameRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GameRequest), isFromPool) as GameRequest;
        }

        /// <summary>
        /// 请求ID
        /// </summary>
        [MemoryPackOrder(0)]
        public long requestId { get; set; }

        /// <summary>
        /// 动作类型
        /// </summary>
        [MemoryPackOrder(1)]
        public string action { get; set; }

        /// <summary>
        /// 玩家信息
        /// </summary>
        [MemoryPackOrder(2)]
        public PlayerInfo player { get; set; }

        /// <summary>
        /// 参数列表
        /// </summary>
        [MemoryPackOrder(3)]
        public List<string> param { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.requestId = default;
            this.action = default;
            this.player = default;
            this.param.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [MessageAttribute(3003)]
    public partial class GameResponse : MessageObject
    {
        public static GameResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(GameResponse), isFromPool) as GameResponse;
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
        /// 对应的请求ID
        /// </summary>
        [MemoryPackOrder(2)]
        public long requestId { get; set; }

        /// <summary>
        /// 更新后的玩家信息
        /// </summary>
        [MemoryPackOrder(3)]
        public PlayerInfo updatedPlayer { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.success = default;
            this.message = default;
            this.requestId = default;
            this.updatedPlayer = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    public static class game
    {
        public const ushort PlayerInfo = 3001;
        public const ushort GameRequest = 3002;
        public const ushort GameResponse = 3003;
    }
}