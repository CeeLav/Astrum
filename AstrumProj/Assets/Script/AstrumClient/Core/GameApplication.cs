using UnityEngine;
using System;
using System.Linq;
using Astrum.Client.Managers;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.View.Managers;
using Astrum.Network.Generated;
using Astrum.Network;

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
        [SerializeField] private CameraManager cameraManager;
        
        [Header("核心GameObject引用")]
        [SerializeField] private GameObject uiRoot;
        [SerializeField] private GameObject stageRoot;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private UnityEngine.SceneManagement.SceneManager sceneManagerUnity;
        [SerializeField] private Astrum.Client.Behaviour.CoroutineRunner coroutineRunner;
        
        
        
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
        public CameraManager CameraManager => cameraManager;
        
        // 核心GameObject访问器
        public GameObject UIRoot => uiRoot;
        public GameObject StageRoot => stageRoot;
        public Camera MainCamera => mainCamera;
        public Camera UICamera => uiCamera;
        public UnityEngine.SceneManagement.SceneManager SceneManagerUnity => sceneManagerUnity;
        public Astrum.Client.Behaviour.CoroutineRunner CoroutineRunner => coroutineRunner;
        
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
        
        //public GameLauncher gameLauncher;
        
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
                Application.runInBackground = true;
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
            
            // 初始化网络系统基础组件（必须在网络管理器之前初始化）
            InitializeNetworkSystem();
            
            // 初始化资源管理器（单例赋值）
            resourceManager = ResourceManager.Instance;
            resourceManager.Initialize();

            // 初始化场景管理器（单例赋值）
            sceneManager = SceneManager.Instance;
            sceneManager.Initialize();

            // 初始化网络管理器（单例赋值）
            networkManager = NetworkManager.Instance;
            networkManager.Initialize();
            RegisterMessageHandlers();
            // 暂时注释掉自动连接，需要指定服务器地址和端口
            // networkManager.ConnectAsync("127.0.0.1", 8888);
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
            
            // 初始化相机管理器（单例赋值）
            cameraManager = CameraManager.Instance;
            cameraManager.Initialize();
            
            // 显示登录UI
            ShowLoginUI();
        }
        
        /// <summary>
        /// 显示登录UI
        /// </summary>
        private void ShowLoginUI()
        {
            try
            {
                Debug.Log("GameApplication: 显示登录UI");
                uiManager.ShowUI("Login");
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameApplication: 显示登录UI失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 初始化日志管理器
        /// </summary>
        private void InitializeLogManager()
        {
            Debug.Log("GameApplication: 初始化日志管理器");
            
            // 添加Unity日志处理器
            ASLogger.Instance.AddHandler(new UnityLogHandler());
            
        }
        
        /// <summary>
        /// 初始化网络系统基础组件
        /// </summary>
        private void InitializeNetworkSystem()
        {
            Debug.Log("GameApplication: 初始化网络系统基础组件 - 强制重新编译测试");
            
            try
            {
                // 初始化ObjectPool（对象池，网络系统的基础组件）
                ObjectPool.Instance.Awake();
                Debug.Log("GameApplication: ObjectPool初始化完成 - 测试日志");
                
                // 初始化CodeTypes（加载所有类型信息）
                CodeTypes.Instance.Awake();
                Debug.Log("GameApplication: CodeTypes初始化完成 - 测试日志");
                
                // 初始化OpcodeType（注册消息类型和opcode映射）
                OpcodeType.Instance.Awake();
                Debug.Log("GameApplication: OpcodeType初始化完成 - 测试日志");
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameApplication: 网络系统基础组件初始化失败 - {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 更新各个管理�?
        /// </summary>
        private void UpdateManagers(float deltaTime)
        {
            // 更新输入管理�?
            inputManager?.Update();
            
            // 更新网络管理器
            networkManager?.Update();
            
            // 更新UI管理�?
            uiManager?.Update();
            
            // 更新音频管理�?
            //audioManager?.Update();
            
            // 更新游戏玩法管理器
            gamePlayManager?.Update(deltaTime);
            
            // 更新相机管理器
            cameraManager?.Update();
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
        /// 注册消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // 网络消息处理已移至 GamePlayManager
            ASLogger.Instance.Info("GameApplication: 网络消息处理器注册完成");
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
            cameraManager?.Shutdown();
            
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
