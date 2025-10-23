﻿using UnityEngine;
using System;
using Astrum.Client.Managers;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.View.Managers;
using Astrum.Network.Generated;
using Astrum.Network;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.Archetypes;
using Astrum.View.Archetypes;

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
        
        [Header("配置管理器引用")]
        [SerializeField] private TableConfig _tableConfig;
        
        [Header("核心GameObject引用")]
        [SerializeField] private GameObject uiRoot;
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private GameObject stageRoot;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private UnityEngine.SceneManagement.SceneManager sceneManagerUnity;
        [SerializeField] private Astrum.Client.Behaviour.CoroutineRunner coroutineRunner;
        
        // 应用程序状态
        private ApplicationState currentState = ApplicationState.INITIALIZING;
        
        // 单例实例
        public static GameApplication Instance { get; private set; }
        
        // 公共属性
        public bool IsRunning => isRunning;
        public ApplicationState CurrentState => currentState;
        public int FrameRate => targetFrameRate;
        
        // 核心GameObject访问器
        public GameObject UIRoot => uiRoot;
        public Canvas HUDCanvas => hudCanvas;
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
        
        public void StartGame()
        {
            // 设置目标帧率
            Application.targetFrameRate = targetFrameRate;
            
            // 使用 GameDirector 启动游戏（新的核心控制器）
            GameDirector.Instance.StartGame("Game");
        }
        
        private void Update()
        {
            if (!isRunning) return;
            float deltaTime = Time.deltaTime;
            
            // 使用 GameDirector 更新（新的核心控制器）
            GameDirector.Instance.Update(deltaTime);
        }
        
        private void OnDestroy()
        {
            Shutdown();
        }
        
        /// <summary>
        /// 初始化应用程序
        /// </summary>
        private void Initialize()
        {
            Debug.Log("GameApplication: 初始化开始...");
            
            try
            {
                Application.runInBackground = true;
                
                // 初始化 GameDirector（新的核心控制器）
                GameDirector.Instance.Initialize();
                
                Debug.Log("GameApplication: 初始化完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameApplication: 初始化失败 - {ex.Message}");
                Shutdown();
            }
        }
        
        /// <summary>
        /// 关闭应用程序
        /// </summary>
        private void Shutdown()
        {
            Debug.Log("GameApplication: 关闭应用程序...");
            
            // 使用 GameDirector 关闭（新的核心控制器）
            GameDirector.Instance.Shutdown();
            

            
            isRunning = false;
        }
    }
    
    /// <summary>
    /// 应用程序状态枚举
    /// </summary>
    public enum ApplicationState
    {
        INITIALIZING,   // 初始化中
        LOADING,        // 加载中
        GAME_PLAYING,   // 游戏进行中
        PAUSED,         // 暂停
        SHUTDOWN        // 关闭
    }
}