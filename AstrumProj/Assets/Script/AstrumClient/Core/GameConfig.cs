using Astrum.CommonBase;

namespace Astrum.Client.Core
{
    /// <summary>
    /// 游戏配置管理器 - 单例模式
    /// </summary>
    public class GameConfig : Singleton<GameConfig>
    {
        private bool _isSinglePlayerMode;
        private string _currentGameMode;
        private GameDifficulty _difficulty;
        private GameSettings _gameSettings;
        
        // 公共属性
        public bool IsSinglePlayerMode => _isSinglePlayerMode;
        public string CurrentGameMode => _currentGameMode;
        public GameDifficulty Difficulty => _difficulty;
        public GameSettings GameSettings => _gameSettings;
        
        protected override void Awake()
        {
            base.Awake();
            InitializeDefaultSettings();
        }
        
        /// <summary>
        /// 初始化默认设置
        /// </summary>
        private void InitializeDefaultSettings()
        {
            _isSinglePlayerMode = false;
            _currentGameMode = "Menu";
            _difficulty = GameDifficulty.Normal;
            _gameSettings = new GameSettings();
            
            ASLogger.Instance.Info("GameConfig: 初始化默认设置");
        }
        
        /// <summary>
        /// 设置单机模式
        /// </summary>
        public void SetSinglePlayerMode(bool enabled)
        {
            _isSinglePlayerMode = enabled;
            _currentGameMode = enabled ? "SinglePlayer" : "Menu";
            
            ASLogger.Instance.Info($"GameConfig: 设置单机模式 = {enabled}");
        }
        
        /// <summary>
        /// 设置游戏难度
        /// </summary>
        public void SetDifficulty(GameDifficulty difficulty)
        {
            _difficulty = difficulty;
            ASLogger.Instance.Info($"GameConfig: 设置游戏难度 = {difficulty}");
        }
        
        /// <summary>
        /// 更新游戏设置
        /// </summary>
        public void UpdateGameSettings(GameSettings settings)
        {
            _gameSettings = settings ?? new GameSettings();
            ASLogger.Instance.Info("GameConfig: 更新游戏设置");
        }
    }
    
    /// <summary>
    /// 游戏难度枚举
    /// </summary>
    public enum GameDifficulty
    {
        Easy,
        Normal,
        Hard,
        Expert
    }
    
    /// <summary>
    /// 游戏设置
    /// </summary>
    public class GameSettings
    {
        public float MasterVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 0.8f;
        public float SFXVolume { get; set; } = 1.0f;
        public int GraphicsQuality { get; set; } = 2; // 0=Low, 1=Medium, 2=High
        public bool EnableVSync { get; set; } = true;
        public int TargetFrameRate { get; set; } = 60;
        
        public GameSettings()
        {
            // 默认设置已在属性初始化中设定
        }
    }
}
