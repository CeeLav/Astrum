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
        
        private void Start()
        {
            // 设置目标帧率
            Application.targetFrameRate = targetFrameRate;
            
            // 切换到加载状�?
            ChangeState(ApplicationState.LOADING);
        }
        
        private void Update()
        {
            if (!isRunning) return;
            
            // 更新各个管理�?
            UpdateManagers();
            
            // 处理应用程序状�?
            UpdateApplicationState();
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
                ChangeState(ApplicationState.SHUTDOWN);
            }
        }
        
        /// <summary>
        /// 初始化各个管理器
        /// </summary>
        private void InitializeManagers()
        {
            // 初始化资源管理器
            if (resourceManager != null)
            {
                resourceManager.Initialize();
            }
            
            // 初始化场景管理器
            if (sceneManager != null)
            {
                sceneManager.Initialize();
            }
            
            // 初始化网络管理器
            if (networkManager != null)
            {
                networkManager.Initialize();
            }
            
            // 初始化输入管理器
            if (inputManager != null)
            {
                inputManager.Initialize();
            }
            
            // 初始化UI管理�?
            if (uiManager != null)
            {
                uiManager.Initialize();
            }
            
            // 初始化音频管理器
            if (audioManager != null)
            {
                audioManager.Initialize();
            }
        }
        
        /// <summary>
        /// 更新各个管理�?
        /// </summary>
        private void UpdateManagers()
        {
            // 更新输入管理�?
            inputManager?.Update();
            
            // 更新网络管理�?
            networkManager?.Update();
            
            // 更新UI管理�?
            uiManager?.Update();
            
            // 更新音频管理�?
            //audioManager?.Update();
        }
        
        /// <summary>
        /// 更新应用程序状�?
        /// </summary>
        private void UpdateApplicationState()
        {
            switch (currentState)
            {
                case ApplicationState.LOADING:
                    UpdateLoadingState();
                    break;
                    
                case ApplicationState.GAME_PLAYING:
                    UpdateGamePlayingState();
                    break;
                    
                case ApplicationState.PAUSED:
                    UpdatePausedState();
                    break;
                    
                case ApplicationState.SHUTDOWN:
                    UpdateShutdownState();
                    break;
            }
        }
        
        /// <summary>
        /// 更新加载状�?
        /// </summary>
        private void UpdateLoadingState()
        {
            // 检查资源加载进�?
            if (resourceManager != null && resourceManager.GetLoadProgress() >= 1.0f)
            {
                // 资源加载完成，切换到游戏状�?
                ChangeState(ApplicationState.GAME_PLAYING);
            }
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
        /// 切换应用程序状�?
        /// </summary>
        /// <param name="newState">新状�?/param>
        public void ChangeState(ApplicationState newState)
        {
            if (currentState == newState) return;
            
            Debug.Log($"GameApplication: 状态切�?{currentState} -> {newState}");
            
            // 退出当前状�?
            OnExitState(currentState);
            
            // 更新状�?
            currentState = newState;
            
            // 进入新状�?
            OnEnterState(newState);
        }
        
        /// <summary>
        /// 退出状态时的处�?
        /// </summary>
        /// <param name="state">要退出的状�?/param>
        private void OnExitState(ApplicationState state)
        {
            switch (state)
            {
                case ApplicationState.LOADING:
                    // 加载完成后的清理
                    break;
                    
                case ApplicationState.GAME_PLAYING:
                    // 游戏结束时的处理
                    break;
                    
                case ApplicationState.PAUSED:
                    // 恢复游戏时的处理
                    break;
            }
        }
        
        /// <summary>
        /// 进入状态时的处�?
        /// </summary>
        /// <param name="state">要进入的状�?/param>
        private void OnEnterState(ApplicationState state)
        {
            switch (state)
            {
                case ApplicationState.LOADING:
                    // 开始加载资�?
                    StartLoading();
                    break;
                    
                case ApplicationState.GAME_PLAYING:
                    // 开始游�?
                    StartGame();
                    break;
                    
                case ApplicationState.PAUSED:
                    // 暂停游戏
                    PauseGame();
                    break;
                    
                case ApplicationState.SHUTDOWN:
                    // 关闭应用程序
                    Shutdown();
                    break;
            }
        }
        
        /// <summary>
        /// 开始加�?
        /// </summary>
        private void StartLoading()
        {
            Debug.Log("GameApplication: 开始加载资�?..");
            
            // 预加载必要的资源
            if (resourceManager != null)
            {
                // 这里可以添加需要预加载的资源路�?
                // resourceManager.PreloadResources(resourcePaths);
            }
        }
        
        /// <summary>
        /// 开始游�?
        /// </summary>
        private void StartGame()
        {
            Debug.Log("GameApplication: 开始游�?..");
            
            // 加载游戏场景
            if (sceneManager != null)
            {
                sceneManager.LoadScene("GameScene", LoadSceneMode.SINGLE);
            }
        }
        
        /// <summary>
        /// 暂停游戏
        /// </summary>
        private void PauseGame()
        {
            Debug.Log("GameApplication: 游戏暂停");
            
            // 暂停游戏逻辑
            Time.timeScale = 0f;
        }
        
        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            Debug.Log("GameApplication: 游戏恢复");
            
            // 恢复游戏逻辑
            Time.timeScale = 1f;
            ChangeState(ApplicationState.GAME_PLAYING);
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
            
            isRunning = false;
        }
        
        /// <summary>
        /// 退出游�?
        /// </summary>
        public void QuitGame()
        {
            ChangeState(ApplicationState.SHUTDOWN);
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
