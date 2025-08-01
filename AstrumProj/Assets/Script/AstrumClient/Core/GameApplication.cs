using UnityEngine;
using System;
using Astrum.Client.Managers;
using Astrum.CommonBase;
using Astrum.View.Managers;

namespace Astrum.Client.Core
{
    /// <summary>
    /// 游戏应用程序主控制器
    /// </summary>
    public class GameApplication : MonoBehaviour
    {
        [Header("应用程序设置")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool isRunning = true;
        
        [Header("管理器引用")]
        [SerializeField] private SceneManager sceneManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private InputManager inputManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private GamePlayManager gamePlayManager;
        
        // 应用程序状�?
        private ApplicationState currentState = ApplicationState.INITIALIZING;
        
        // 单例实例
        public static GameApplication Instance { get; private set; }
        
        // 公共属�?
        public bool IsRunning => isRunning;
        public ApplicationState CurrentState => currentState;
        public int FrameRate => targetFrameRate;
        
        // 管理器访问器
        public SceneManager SceneManager => sceneManager;
        public ResourceManager ResourceManager => resourceManager;
        public NetworkManager NetworkManager => networkManager;
        public InputManager InputManager => inputManager;
        public UIManager UIManager => uiManager;
        public AudioManager AudioManager => audioManager;
        public GamePlayManager GamePlayManager => gamePlayManager;
        
        private void Awake()
        {
            // 单例模式
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public GameLauncher gameLauncher;
        
        public void StartGame()
        {
            // 设置目标帧率
            Application.targetFrameRate = targetFrameRate;
            
            gamePlayManager.StartSinglePlayerGame("Game");
            
        }
        
        private void Update()
        {
            if (!isRunning) return;
            float deltaTime = Time.deltaTime;
            // 更新各个管理�?
            UpdateManagers(deltaTime);
            
        }
        
        private void OnDestroy()
        {
            Shutdown();
        }
        
        /// <summary>
        /// 初始化应用程�?
        /// </summary>
        private void Initialize()
        {
            Debug.Log("GameApplication: 初始化开�?..");
            
            try
            {
                // 初始化各个管理器
                InitializeManagers();
                
                Debug.Log("GameApplication: 初始化完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameApplication: 初始化失败 - {ex.Message}");
                Shutdown();
            }
        }
        
        /// <summary>
        /// 初始化各个管理器
        /// </summary>
        private void InitializeManagers()
        {
            // 初始化日志管理器（必须在其他管理器之前初始化）
            InitializeLogManager();
            
            // 初始化资源管理器（单例赋值）
            resourceManager = ResourceManager.Instance;
            resourceManager.Initialize();

            // 初始化场景管理器（单例赋值）
            sceneManager = SceneManager.Instance;
            sceneManager.Initialize();

            // 初始化网络管理器（单例赋值）
            networkManager = NetworkManager.Instance;
            networkManager.Initialize();
            //networkManager.Connect();
            // 初始化输入管理器（单例赋值）
            inputManager = InputManager.Instance;
            inputManager.Initialize();

            // 初始化UI管理器（单例赋值）
            uiManager = UIManager.Instance;
            uiManager.Initialize();

            // 初始化音频管理器（单例赋值）
            audioManager = AudioManager.Instance;
            audioManager.Initialize();

            // 初始化游戏玩法管理器（单例赋值）
            gamePlayManager = GamePlayManager.Instance;
            gamePlayManager.Initialize();
        }
        
        /// <summary>
        /// 初始化日志管理器
        /// </summary>
        private void InitializeLogManager()
        {
            Debug.Log("GameApplication: 初始化日志管理器");
            
            ASLogger.Instance.AddHandler(new UnityLogHandler());
        }
        
        /// <summary>
        /// 更新各个管理�?
        /// </summary>
        private void UpdateManagers(float deltaTime)
        {
            // 更新输入管理�?
            inputManager?.Update();
            
            // 更新网络管理�?
            //networkManager?.Update();
            
            // 更新UI管理�?
            uiManager?.Update();
            
            // 更新音频管理�?
            //audioManager?.Update();
            
            // 更新游戏玩法管理?
            gamePlayManager?.Update(deltaTime);
        }
        

        
        /// <summary>
        /// 更新游戏进行状�?
        /// </summary>
        private void UpdateGamePlayingState()
        {
            // 游戏主循环逻辑
            // 这里可以添加游戏特定的更新逻辑
        }
        
        /// <summary>
        /// 更新暂停状�?
        /// </summary>
        private void UpdatePausedState()
        {
            // 暂停状态下的逻辑
            // 可以处理暂停菜单�?
        }
        
        /// <summary>
        /// 更新关闭状�?
        /// </summary>
        private void UpdateShutdownState()
        {
            // 关闭应用程序
            isRunning = false;
            Application.Quit();
        }
        

        

        


        /// <summary>
        /// 关闭应用程序
        /// </summary>
        private void Shutdown()
        {
            Debug.Log("GameApplication: 关闭应用程序...");
            
            // 清理各个管理�?
            resourceManager?.Shutdown();
            sceneManager?.Shutdown();
            networkManager?.Shutdown();
            inputManager?.Shutdown();
            uiManager?.Shutdown();
            //audioManager?.Shutdown();
            gamePlayManager?.Shutdown();
            
            isRunning = false;
        }

    }
    
    /// <summary>
    /// 应用程序状态枚�?
    /// </summary>
    public enum ApplicationState
    {
        INITIALIZING,   // 初始化中
        LOADING,        // 加载�?
        GAME_PLAYING,   // 游戏进行�?
        PAUSED,         // 暂停
        SHUTDOWN        // 关闭
    }
} 
