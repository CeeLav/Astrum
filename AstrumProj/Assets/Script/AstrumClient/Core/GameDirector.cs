using Astrum.CommonBase;
using Astrum.Client.Managers;
using Astrum.Client.Managers.GameModes;
using Astrum.LogicCore.Archetypes;
using Astrum.View.Managers;
using Astrum.LogicCore.Managers;
using Astrum.Network;
using Astrum.View.Archetypes;
using AstrumClient.MonitorTools;
using Astrum.Client.Data;

namespace Astrum.Client.Core
{
    /// <summary>
    /// 游戏模式类型枚举
    /// </summary>
    public enum GameModeType
    {
        Login,          // 登录模式
        Hub,            // Hub 模式
        SinglePlayer,   // 单机模式
        Multiplayer     // 联机模式
    }

    /// <summary>
    /// 游戏导演 - 统一管理游戏核心逻辑和状态
    /// </summary>
    [MonitorTarget]
    public class GameDirector : Singleton<GameDirector>
    {
        // 游戏状态
        private GameState _currentState = GameState.ApplicationStarting;
        private IGameMode _currentGameMode;
        
        // 公共属性
        public GameState CurrentState => _currentState;
        public IGameMode CurrentGameMode => _currentGameMode;
        
        
        
        /// <summary>
        /// 初始化游戏导演
        /// </summary>
        public void Initialize()
        {
            ASLogger.Instance.Info("GameDirector: 初始化游戏导演");
            
            try
            {
                // 初始化各个 Manager
                InitializeManagers();
                
                // 设置初始状态
                ChangeGameState(GameState.ApplicationReady);
                
                // 创建并切换到 LoginGameMode
                SwitchGameMode(GameModeType.Login);
                
                ASLogger.Instance.Info("GameDirector: 初始化完成");
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"GameDirector: 初始化失败 - {ex.Message}");
                throw;
            }
            MonitorManager.Register(this); // 注册到全局监控
        }
        
        /// <summary>
        /// 更新游戏导演
        /// </summary>
        public void Update(float deltaTime)
        {
            // 更新当前游戏模式
            _currentGameMode?.Update(deltaTime);
            
            // 更新各个 Manager
            UpdateManagers(deltaTime);
        }
        
        /// <summary>
        /// 改变游戏状态
        /// </summary>
        public void ChangeGameState(GameState newState)
        {
            if (_currentState == newState) return;
            
            var previousState = _currentState;
            _currentState = newState;
            
            // 使用现有 EventSystem 发布事件
            EventSystem.Instance.Publish(new GameStateChangedEventData(previousState, newState));
            
            ASLogger.Instance.Info($"GameDirector: 游戏状态从 {previousState} 变为 {newState}");
        }
        
        /// <summary>
        /// 切换游戏模式
        /// </summary>
        public void SwitchGameMode(IGameMode newMode)
        {
            if (_currentGameMode != null)
            {
                ASLogger.Instance.Info($"GameDirector: 关闭当前游戏模式 {_currentGameMode.ModeName}");
                _currentGameMode.Shutdown();
            }
            
            var previousMode = _currentGameMode;
            _currentGameMode = newMode;
            
            if (_currentGameMode != null)
            {
                ASLogger.Instance.Info($"GameDirector: 初始化新游戏模式 {_currentGameMode.ModeName}");
                _currentGameMode.Initialize();
            }
            
            // 使用现有 EventSystem 发布事件
            EventSystem.Instance.Publish(new GameModeChangedEventData(previousMode, newMode));
            
            ASLogger.Instance.Info($"GameDirector: 切换到游戏模式 {newMode?.ModeName ?? "None"}");
        }
        
        /// <summary>
        /// 根据类型切换游戏模式（更优雅的方式）
        /// </summary>
        public void SwitchGameMode(GameModeType gameModeType)
        {
            IGameMode newGameMode = gameModeType switch
            {
                GameModeType.Login => new LoginGameMode(),
                GameModeType.Hub => new HubGameMode(),
                GameModeType.SinglePlayer => new SinglePlayerGameMode(),
                GameModeType.Multiplayer => new MultiplayerGameMode(),
                _ => throw new System.ArgumentException($"Unknown GameMode type: {gameModeType}")
            };
            
            SwitchGameMode(newGameMode);
        }
        
        /// <summary>
        /// 启动游戏
        /// </summary>
        public void StartGame(string sceneName)
        {
            ASLogger.Instance.Info($"GameDirector: 启动游戏 - 场景: {sceneName}");
            
            ChangeGameState(GameState.GameLoading);
            
            if (_currentGameMode != null)
            {
                _currentGameMode.StartGame(sceneName);
                ChangeGameState(GameState.GamePlaying);
            }
            else
            {
                ASLogger.Instance.Warning("GameDirector: 没有当前游戏模式，无法启动游戏");
            }
        }
        
        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            ASLogger.Instance.Info("GameDirector: 暂停游戏");
            ChangeGameState(GameState.GamePaused);
        }
        
        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            ASLogger.Instance.Info("GameDirector: 恢复游戏");
            ChangeGameState(GameState.GamePlaying);
        }
        
        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndGame()
        {
            ASLogger.Instance.Info("GameDirector: 结束游戏");
            ChangeGameState(GameState.GameEnding);
            
            if (_currentGameMode != null)
            {
                _currentGameMode.Shutdown();
                _currentGameMode = null;
            }
            
            ChangeGameState(GameState.GameMenu);
        }
        
        /// <summary>
        /// 初始化各个 Manager
        /// </summary>
        private void InitializeManagers()
        {
            ASLogger.Instance.Info("GameDirector: 初始化各个 Manager");
            
            // 初始化日志管理器（必须在其他管理器之前初始化）
            InitializeLogManager();
            
            // 初始化配置管理器（必须在其他管理器之前初始化）
            InitializeConfigManager();

            // 初始化原型管理器（逻辑与视图）
            ArchetypeRegistry.Instance.Initialize();
            ViewArchetypeManager.Instance.Initialize();
            
            // 初始化网络系统基础组件（必须在网络管理器之前初始化）
            InitializeNetworkSystem();
            
            // 直接初始化各个 Manager
            ResourceManager.Instance.Initialize();
            SceneManager.Instance.Initialize();
            NetworkManager.Instance.Initialize();
            UIManager.Instance.Initialize();
            
            // 初始化玩家数据管理器
            PlayerDataManager.Instance.Initialize();
            
            // 初始化 HUD 管理器
            HUDManager.Instance.Initialize(GameApplication.Instance.HUDCanvas, GameApplication.Instance.MainCamera);
            
            AudioManager.Instance.Initialize();
            InputManager.Instance.Initialize();
            CameraManager.Instance.Initialize();
            // TODO: NetworkMessageHandler已被新的消息处理器系统替代
            // 新的消息处理器会自动初始化，无需手动调用
            
            ASLogger.Instance.Info("GameDirector: 所有 Manager 初始化完成");
        }
        
        /// <summary>
        /// 更新各个 Manager
        /// </summary>
        private void UpdateManagers(float deltaTime)
        {
            // 更新各个 Manager
            InputManager.Instance?.Update();
            NetworkManager.Instance?.Update();
            UIManager.Instance?.Update();
            AudioManager.Instance?.Update();
            CameraManager.Instance?.Update();
            VFXManager.Instance?.Update();
            // GamePlayManager 已删除，其功能已分散到其他管理器
        }
        
        /// <summary>
        /// 关闭游戏导演
        /// </summary>
        public void Shutdown()
        {
            ASLogger.Instance.Info("GameDirector: 关闭游戏导演");
            
            // 关闭当前游戏模式
            _currentGameMode?.Shutdown();
            _currentGameMode = null;
            
            // 关闭所有 Manager
            ResourceManager.Instance?.Shutdown();
            SceneManager.Instance?.Shutdown();
            NetworkManager.Instance?.Shutdown();
            UIManager.Instance?.Shutdown();
            AudioManager.Instance?.Shutdown();
            InputManager.Instance?.Shutdown();
            CameraManager.Instance?.Shutdown();
            // TODO: NetworkMessageHandler已被新的消息处理器系统替代
            // 新的消息处理器会自动管理，无需手动关闭
            TableConfig.Instance?.Shutdown();
            
            // 设置关闭状态
            ChangeGameState(GameState.SystemShutdown);
        }
        
        /// <summary>
        /// 初始化日志管理器
        /// </summary>
        private void InitializeLogManager()
        {
            ASLogger.Instance.Info("GameDirector: 初始化日志管理器");
            
            // 添加Unity日志处理器
            ASLogger.Instance.AddHandler(new UnityLogHandler());
        }
        
        /// <summary>
        /// 初始化配置管理器
        /// </summary>
        private void InitializeConfigManager()
        {
            ASLogger.Instance.Info("GameDirector: 初始化配置管理器");
            
            try
            {
                // 初始化配置管理器，指定配置路径
                // 配置文件位于 Astrum\AstrumConfig\Tables\output\Client 目录
                var configPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..","..", "AstrumConfig", "Tables", "output", "Client");
                TableConfig.Instance?.Initialize(configPath);
                
                ASLogger.Instance.Info("GameDirector: 配置管理器初始化完成");
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"GameDirector: 配置管理器初始化失败 - {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 初始化网络系统基础组件
        /// </summary>
        private void InitializeNetworkSystem()
        {
            ASLogger.Instance.Info("GameDirector: 初始化网络系统基础组件");
            
            try
            {
                // 初始化ObjectPool（对象池，网络系统的基础组件）
                ObjectPool.Instance.Awake();
                ASLogger.Instance.Info("GameDirector: ObjectPool初始化完成");
                
                // 初始化CodeTypes（加载所有类型信息）
                CodeTypes.Instance.Awake();
                ASLogger.Instance.Info("GameDirector: CodeTypes初始化完成");
                
                // 初始化OpcodeType（注册消息类型和opcode映射）
                OpcodeType.Instance.Awake();
                ASLogger.Instance.Info("GameDirector: OpcodeType初始化完成");
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"GameDirector: 网络系统基础组件初始化失败 - {ex.Message}");
                throw;
            }
        }
    }
}
