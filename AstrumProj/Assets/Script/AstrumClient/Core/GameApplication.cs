using UnityEngine;
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
using Astrum.Client.Profiling;

namespace Astrum.Client.Core
{
    /// <summary>
    /// 游戏应用程序主控制器
    /// </summary>
    public class GameApplication : MonoBehaviourSingleton<GameApplication>
    {
        private GameSetting gameSetting;
        private bool isRunning = true;
        
        /// <summary>
        /// 在场景切换时保持存活
        /// </summary>
        protected override bool DontDestroyOnLoad => true;
        
        public int FrameRate => gameSetting != null ? gameSetting.TargetFrameRate : 60;
        
        /// <summary>
        /// 是否启用逻辑专用线程
        /// </summary>
        public bool EnableLogicThread => gameSetting != null ? gameSetting.EnableLogicThread : false;
        
        /// <summary>
        /// 逻辑线程帧率
        /// </summary>
        public int LogicThreadTickRate => gameSetting != null ? gameSetting.LogicThreadTickRate : 20;
        
        // 核心GameObject访问器
        public GameObject UIRoot => gameSetting != null ? gameSetting.UIRoot : null;
        public Canvas HUDCanvas => gameSetting != null ? gameSetting.HUDCanvas : null;
        public GameObject StageRoot => gameSetting != null ? gameSetting.StageRoot : null;
        public Camera MainCamera => gameSetting != null ? gameSetting.MainCamera : null;
        public Camera UICamera => gameSetting != null ? gameSetting.UICamera : null;
        public UnityEngine.SceneManagement.SceneManager SceneManagerUnity => gameSetting != null ? gameSetting.SceneManagerUnity : null;
        public Astrum.Client.Behaviour.CoroutineRunner CoroutineRunner => gameSetting != null ? gameSetting.CoroutineRunner : null;
        
        // 场景ID访问器
        public int SinglePlayerSceneId => gameSetting != null ? gameSetting.SinglePlayerSceneId : 1;
        public int MultiplayerSceneId => gameSetting != null ? gameSetting.MultiplayerSceneId : 2;
        
        /// <summary>
        /// 根据游戏模式获取对应的场景ID
        /// </summary>
        /// <param name="isSinglePlayer">是否为单人模式</param>
        /// <returns>对应的场景ID</returns>
        public int GetSceneIdByMode(bool isSinglePlayer)
        {
            return gameSetting != null ? gameSetting.GetSceneIdByMode(isSinglePlayer) : (isSinglePlayer ? 1 : 2);
        }
        
        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            
            // 验证 GameSetting 引用
            if (gameSetting == null)
            {
                Debug.LogError("GameApplication: GameSetting 引用未设置！请在 Inspector 中设置 GameSetting 引用。");
                return;
            }
            
            Initialize();
        }
        
        private void Update()
        {
            if (!isRunning) return;
            float deltaTime = Time.deltaTime;
            
            // 使用 GameDirector 更新（新的核心控制器）
            GameDirector.Instance.Update(deltaTime);
        }
        
        protected override void OnDestroy()
        {
            Shutdown();
            base.OnDestroy();
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
                
                // 初始化性能监控系统
                InitializeProfiler();
                
                // 配置日志级别（必须在 GameDirector 初始化之前）
                ConfigureLogLevel();
                
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
        /// 初始化性能监控系统
        /// </summary>
        private void InitializeProfiler()
        {
#if ENABLE_PROFILER
            var unityHandler = new UnityProfilerHandler();
            ASProfiler.Instance.RegisterHandler(unityHandler);
            ASLogger.Instance.Info("[Profiler] Unity Profiler Handler 已注册");
#endif
        }
        
        /// <summary>
        /// 配置日志级别
        /// </summary>
        private void ConfigureLogLevel()
        {
            // 根据 Debug 开关设置日志级别
            if (gameSetting != null && gameSetting.EnableDebugLog)
            {
                ASLogger.Instance.MinLevel = Astrum.CommonBase.LogLevel.Debug;
                Debug.Log("GameApplication: 日志级别设置为 Debug，将输出所有级别的日志");
            }
            else
            {
                ASLogger.Instance.MinLevel = Astrum.CommonBase.LogLevel.Info;
                Debug.Log("GameApplication: 日志级别设置为 Info，只输出 Info 及以上级别的日志");
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