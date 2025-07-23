using System;
using System.Collections.Generic;

namespace Astrum.LogicCore
{
    /// <summary>
    /// 玩家数据 - 与Unity解耦
    /// </summary>
    public class PlayerData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public bool IsGrounded { get; set; }
        public float Health { get; set; } = 100f;
        public float MaxHealth { get; set; } = 100f;
        public bool IsAlive { get; set; } = true;
        public DateTime JoinTime { get; set; }

        public PlayerData(string id, string name)
        {
            Id = id;
            Name = name;
            JoinTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 简单的Vector3结构 - 与Unity解耦
    /// </summary>
    public struct Vector3
    {
        public float x, y, z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3 operator *(Vector3 a, float b)
        {
            return new Vector3(a.x * b, a.y * b, a.z * b);
        }

        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z);

        public Vector3 normalized
        {
            get
            {
                float mag = magnitude;
                if (mag > 0)
                    return this * (1f / mag);
                return zero;
            }
        }

        public override string ToString()
        {
            return $"({x:F2}, {y:F2}, {z:F2})";
        }
    }

    /// <summary>
    /// 玩家管理器 - 与Unity解耦
    /// </summary>
    public class PlayerManager
    {
        private Dictionary<string, PlayerData> _players = new Dictionary<string, PlayerData>();
        private GameConfig _config;

        /// <summary>
        /// 玩家列表
        /// </summary>
        public IReadOnlyDictionary<string, PlayerData> Players => _players;

        /// <summary>
        /// 玩家数量
        /// </summary>
        public int PlayerCount => _players.Count;

        /// <summary>
        /// 玩家加入事件
        /// </summary>
        public event Action<PlayerData> OnPlayerJoined;

        /// <summary>
        /// 玩家离开事件
        /// </summary>
        public event Action<PlayerData> OnPlayerLeft;

        /// <summary>
        /// 玩家位置更新事件
        /// </summary>
        public event Action<PlayerData> OnPlayerPositionChanged;

        public PlayerManager()
        {
            _config = GameStateManager.Instance.Config;
        }

        /// <summary>
        /// 初始化玩家管理器
        /// </summary>
        public void Initialize()
        {
            _players.Clear();
        }

        /// <summary>
        /// 添加玩家
        /// </summary>
        public bool AddPlayer(string id, string name)
        {
            if (_players.Count >= _config.MaxPlayers)
                return false;

            if (_players.ContainsKey(id))
                return false;

            var player = new PlayerData(id, name);
            _players[id] = player;
            OnPlayerJoined?.Invoke(player);
            return true;
        }

        /// <summary>
        /// 移除玩家
        /// </summary>
        public bool RemovePlayer(string id)
        {
            if (_players.TryGetValue(id, out var player))
            {
                _players.Remove(id);
                OnPlayerLeft?.Invoke(player);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取玩家
        /// </summary>
        public PlayerData GetPlayer(string id)
        {
            _players.TryGetValue(id, out var player);
            return player;
        }

        /// <summary>
        /// 更新玩家位置
        /// </summary>
        public void UpdatePlayerPosition(string id, Vector3 position)
        {
            if (_players.TryGetValue(id, out var player))
            {
                player.Position = position;
                OnPlayerPositionChanged?.Invoke(player);
            }
        }

        /// <summary>
        /// 更新玩家速度
        /// </summary>
        public void UpdatePlayerVelocity(string id, Vector3 velocity)
        {
            if (_players.TryGetValue(id, out var player))
            {
                player.Velocity = velocity;
            }
        }

        /// <summary>
        /// 更新玩家地面状态
        /// </summary>
        public void UpdatePlayerGrounded(string id, bool isGrounded)
        {
            if (_players.TryGetValue(id, out var player))
            {
                player.IsGrounded = isGrounded;
            }
        }

        /// <summary>
        /// 更新游戏逻辑
        /// </summary>
        public void Update(float deltaTime)
        {
            // 这里可以添加玩家逻辑更新
            // 比如移动、碰撞检测等
        }

        /// <summary>
        /// 处理玩家输入
        /// </summary>
        public void HandlePlayerInput(string id, Vector3 input)
        {
            if (_players.TryGetValue(id, out var player))
            {
                // 计算移动
                Vector3 movement = input * _config.PlayerMoveSpeed;
                player.Velocity = movement;
                
                // 更新位置
                player.Position = player.Position + movement;
                OnPlayerPositionChanged?.Invoke(player);
            }
        }

        /// <summary>
        /// 处理玩家跳跃
        /// </summary>
        public void HandlePlayerJump(string id)
        {
            if (_players.TryGetValue(id, out var player) && player.IsGrounded)
            {
                player.Velocity = player.Velocity + Vector3.up * _config.PlayerJumpForce;
                player.IsGrounded = false;
            }
        }
    }
} 