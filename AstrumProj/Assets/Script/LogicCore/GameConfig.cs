using System;

namespace Astrum.LogicCore
{
    /// <summary>
    /// 游戏配置 - 与Unity解耦
    /// </summary>
    public class GameConfig
    {
        /// <summary>
        /// 游戏名称
        /// </summary>
        public string GameName { get; set; } = "Astrum";

        /// <summary>
        /// 最大玩家数
        /// </summary>
        public int MaxPlayers { get; set; } = 4;

        /// <summary>
        /// 游戏版本
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int ServerPort { get; set; } = 8888;

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string ServerAddress { get; set; } = "localhost";

        /// <summary>
        /// 是否启用调试模式
        /// </summary>
        public bool DebugMode { get; set; } = true;

        /// <summary>
        /// 游戏时间限制（秒）
        /// </summary>
        public float GameTimeLimit { get; set; } = 300f; // 5分钟

        /// <summary>
        /// 玩家移动速度
        /// </summary>
        public float PlayerMoveSpeed { get; set; } = 5f;

        /// <summary>
        /// 玩家跳跃力度
        /// </summary>
        public float PlayerJumpForce { get; set; } = 5f;

        /// <summary>
        /// 从JSON加载配置
        /// </summary>
        public static GameConfig FromJson(string json)
        {
            try
            {
                // 这里可以使用任何JSON库，如Newtonsoft.Json
                // 为了保持与Unity解耦，这里只是示例
                return new GameConfig();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load game config from JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// 转换为JSON
        /// </summary>
        public string ToJson()
        {
            // 这里可以使用任何JSON库
            return "{}";
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        public bool Validate()
        {
            return MaxPlayers > 0 && 
                   MaxPlayers <= 16 && 
                   ServerPort > 0 && 
                   ServerPort <= 65535 &&
                   !string.IsNullOrEmpty(GameName);
        }
    }
} 