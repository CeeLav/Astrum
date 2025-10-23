using System;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using Astrum.View.Core;
using UnityEngine;

namespace Astrum.Client.Managers.GameModes
{
    /// <summary>
    /// 单机游戏模式 - 本地游戏，无需网络连接
    /// </summary>
    public class SinglePlayerGameMode : BaseGameMode
    {
        // 核心属性
        public override Room MainRoom { get; set; }
        public override Stage MainStage { get; set; }
        public override long PlayerId { get; set; }
        public override string ModeName => "SinglePlayer";
        public override bool IsRunning { get; set; }
        
        /// <summary>
        /// 初始化单机游戏模式
        /// </summary>
        public override void Initialize()
        {
            ASLogger.Instance.Info("SinglePlayerGameMode: 初始化单机游戏模式");
            ChangeState(GameModeState.Initializing);
            
            // 注册事件处理器（使用现有 EventSystem）
            EventSystem.Instance.Subscribe<GameModeStateChangedEventData>(OnStateChanged);
            
            ChangeState(GameModeState.Ready);
        }
        
        /// <summary>
        /// 启动单机游戏
        /// </summary>
        /// <param name="sceneName">游戏场景名称</param>
        public override void StartGame(string sceneName)
        {
            ASLogger.Instance.Info($"SinglePlayerGameMode: 启动单机游戏 - 场景: {sceneName}");
            
            try
            {
                //ChangeState(GameModeState.Loading);
                
                // 1. 创建本地 Room 和 World
                CreateRoom();
                
                // 2. 创建 Stage
                CreateStage();
                
                // 3. 切换到游戏场景
                SwitchToGameScene(sceneName);
                
                // 4. 创建玩家（在场景加载完成后）
                // CreatePlayer() 会在 OnGameSceneLoaded() 中调用
                
                ChangeState(GameModeState.Playing);
                IsRunning = true;
                ASLogger.Instance.Info("SinglePlayerGameMode: 单机游戏启动成功");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"SinglePlayerGameMode: 启动游戏失败 - {ex.Message}");
                ChangeState(GameModeState.Finished);
                throw;
            }
        }
        
        /// <summary>
        /// 更新游戏逻辑（本地帧循环）
        /// </summary>
        /// <param name="deltaTime">时间差</param>
        public override void Update(float deltaTime)
        {
            if (!IsRunning || CurrentState != GameModeState.Playing) return;
            
            // 单机模式：模拟服务器的权威帧推进
            // 在联机模式下，AuthorityFrame 由服务器通过 FrameSyncData 更新
            // 在单机模式下，我们让 AuthorityFrame 跟随 PredictionFrame
            if (MainRoom?.LSController != null && MainRoom.LSController.IsRunning)
            {
                // 让权威帧等于预测帧（本地模式下无延迟，它们应该相等）
                MainRoom.LSController.AuthorityFrame = MainRoom.LSController.PredictionFrame;
            }
            
            // 更新 Room 和 Stage
            MainRoom?.Update(deltaTime);
            MainStage?.Update(deltaTime);
        }
        
        /// <summary>
        /// 关闭单机游戏模式
        /// </summary>
        public override void Shutdown()
        {
            ASLogger.Instance.Info("SinglePlayerGameMode: 关闭单机游戏模式");
            ChangeState(GameModeState.Ending);
            
            // 取消订阅事件
            if (MainStage != null)
            {
                MainStage.OnEntityViewAdded -= OnEntityViewAdded;
            }
            
            // 取消注册事件处理器
            EventSystem.Instance.Unsubscribe<GameModeStateChangedEventData>(OnStateChanged);
            
            // 清理资源
            MainRoom = null;
            MainStage = null;
            PlayerId = -1;
            IsRunning = false;
            
            ChangeState(GameModeState.Finished);
        }
        
        #region 私有方法 - Room/Stage/Player 创建
        
        /// <summary>
        /// 创建本地 Room
        /// </summary>
        private void CreateRoom()
        {
            var room = new Room(1, "SinglePlayerRoom");
            var world = new World();
            room.MainWorld = world;
            room.Initialize(); // 初始化 Room，但不启动帧同步（本地模式）
            
            MainRoom = room;
            ASLogger.Instance.Info($"SinglePlayerGameMode: 创建本地Room，ID: {room.RoomId}");
        }
        
        /// <summary>
        /// 创建 Stage
        /// </summary>
        private void CreateStage()
        {
            var stage = new Stage("GameStage", "游戏场景");
            stage.Initialize();
            stage.SetRoom(MainRoom);
            
            MainStage = stage;
            ASLogger.Instance.Info("SinglePlayerGameMode: 创建Stage");
        }
        
        /// <summary>
        /// 切换到游戏场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        private void SwitchToGameScene(string sceneName)
        {
            ASLogger.Instance.Info($"SinglePlayerGameMode: 切换到游戏场景 {sceneName}");
            
            var sceneManager = SceneManager.Instance;
            if (sceneManager == null)
            {
                throw new Exception("SceneManager不存在");
            }
            
            // 设置当前Stage
            StageManager.Instance.SetCurrentStage(MainStage);
            
            // 异步加载游戏场景
            sceneManager.LoadSceneAsync(sceneName, () => { OnGameSceneLoaded(); });
        }
        
        /// <summary>
        /// 游戏场景加载完成回调
        /// </summary>
        private void OnGameSceneLoaded()
        {
            ASLogger.Instance.Info("SinglePlayerGameMode: 游戏场景加载完成");
            
            // 关闭Login UI
            CloseLoginUI();
            
            // 激活Stage
            MainStage.SetActive(true);
            MainStage.OnEnter();
            
            // 订阅EntityView创建事件，用于设置相机跟随
            MainStage.OnEntityViewAdded += OnEntityViewAdded;
            
            // 创建玩家
            CreatePlayer();
            
            ASLogger.Instance.Info("SinglePlayerGameMode: 游戏准备完成");
        }
        
        /// <summary>
        /// 创建玩家
        /// </summary>
        private void CreatePlayer()
        {
            ASLogger.Instance.Info("SinglePlayerGameMode: 创建Player");
            
            var playerID = MainRoom.AddPlayer();
            PlayerId = playerID;
            
            // 设置 MainPlayerId（LSController 需要检查这个值）
            MainRoom.MainPlayerId = playerID;
            
            // 启动本地帧同步控制器
            if (MainRoom?.LSController != null && !MainRoom.LSController.IsRunning)
            {
                // 设置本地创建时间
                MainRoom.LSController.CreationTime = TimeInfo.Instance.ServerNow();
                
                // 启动控制器
                MainRoom.LSController.Start();
                ASLogger.Instance.Info("SinglePlayerGameMode: 本地帧同步控制器已启动");
            }
            
            // 设置相机跟随主玩家（如果 EntityView 已创建）
            SetCameraFollowMainPlayer();
            
            ASLogger.Instance.Info($"SinglePlayerGameMode: Player创建完成，ID: {PlayerId}");
        }
        
        /// <summary>
        /// 关闭Login UI
        /// </summary>
        private void CloseLoginUI()
        {
            try
            {
                var uiManager = UIManager.Instance;
                if (uiManager != null)
                {
                    uiManager.HideUI("Login");
                    uiManager.DestroyUI("Login");
                    ASLogger.Instance.Info("SinglePlayerGameMode: Login UI已关闭");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"SinglePlayerGameMode: 关闭Login UI失败 - {ex.Message}");
            }
        }
        
        #endregion
        
        #region 相机跟随逻辑
        
        /// <summary>
        /// 设置相机跟随主玩家
        /// </summary>
        private void SetCameraFollowMainPlayer()
        {
            if (MainStage == null || PlayerId <= 0) return;
            
            // 尝试从Stage中获取主玩家的EntityView
            if (MainStage.EntityViews.TryGetValue(PlayerId, out var entityView))
            {
                var cameraManager = CameraManager.Instance;
                if (cameraManager != null)
                {
                    cameraManager.SetFollowTarget(entityView.Transform);
                    ASLogger.Instance.Info($"SinglePlayerGameMode: 设置相机跟随主玩家，ID: {PlayerId}");
                }
                else
                {
                    ASLogger.Instance.Warning("SinglePlayerGameMode: CameraManager未找到");
                }
            }
            else
            {
                ASLogger.Instance.Debug($"SinglePlayerGameMode: 主玩家EntityView尚未创建，ID: {PlayerId}");
            }
        }
        
        /// <summary>
        /// EntityView创建事件处理
        /// </summary>
        private void OnEntityViewAdded(EntityView entityView)
        {
            if (entityView == null) return;
            
            // 检查是否是主玩家的EntityView
            if (entityView.EntityId == PlayerId)
            {
                ASLogger.Instance.Info($"SinglePlayerGameMode: 主玩家EntityView创建完成，ID: {entityView.EntityId}");
                
                // 设置相机跟随目标
                var cameraManager = CameraManager.Instance;
                if (cameraManager != null)
                {
                    cameraManager.SetFollowTarget(entityView.Transform);
                    ASLogger.Instance.Info("SinglePlayerGameMode: 设置相机跟随主玩家");
                }
            }
        }
        
        #endregion
        
        #region 状态管理和事件处理
        
        /// <summary>
        /// 状态变化事件处理
        /// </summary>
        private void OnStateChanged(GameModeStateChangedEventData evt)
        {
            ASLogger.Instance.Info($"SinglePlayerGameMode: 状态从 {evt.PreviousState} 变为 {evt.NewState}");
            
            // 根据状态变化执行特定逻辑
            switch (evt.NewState)
            {
                case GameModeState.Playing:
                    OnGameStart();
                    break;
                case GameModeState.Paused:
                    OnGamePause();
                    break;
                case GameModeState.Ending:
                    OnGameEnd();
                    break;
            }
        }
        
        /// <summary>
        /// 游戏开始时的处理
        /// </summary>
        private void OnGameStart()
        {
            ASLogger.Instance.Info("SinglePlayerGameMode: 游戏开始");
        }
        
        /// <summary>
        /// 游戏暂停时的处理
        /// </summary>
        private void OnGamePause()
        {
            ASLogger.Instance.Info("SinglePlayerGameMode: 游戏暂停");
        }
        
        /// <summary>
        /// 游戏结束时的处理
        /// </summary>
        private void OnGameEnd()
        {
            ASLogger.Instance.Info("SinglePlayerGameMode: 游戏结束");
        }
        
        #endregion
        
        #region 配置管理
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        protected override GameModeConfig CreateDefaultConfig()
        {
            return new GameModeConfig
            {
                ModeName = "SinglePlayer",
                AutoSave = true,
                UpdateInterval = 0.016f,
                CustomSettings = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "EnableDebugMode", false },
                    { "MaxFPS", 60 },
                    { "VSync", true }
                }
            };
        }
        
        #endregion
    }
}

