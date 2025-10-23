using System;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.Client.Managers.GameModes.Handlers;
using Astrum.Generated;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using Astrum.View.Core;
using Unity.Plastic.Newtonsoft.Json;

namespace Astrum.Client.Managers.GameModes
{
    /// <summary>
    /// 联机游戏模式 - 网络多人对战
    /// </summary>
    public class MultiplayerGameMode : BaseGameMode
    {
        // 核心属性
        public override Room MainRoom { get; set; }
        public override Stage MainStage { get; set; }
        public override long PlayerId { get; set; }
        public override string ModeName => "Multiplayer";
        public override bool IsRunning { get; set; }
        
        // 辅助处理器
        private NetworkGameHandler _networkHandler;
        private FrameSyncHandler _frameSyncHandler;
        
        /// <summary>
        /// 初始化联机游戏模式
        /// </summary>
        public override void Initialize()
        {
            ASLogger.Instance.Info("MultiplayerGameMode: 初始化联机游戏模式");
            ChangeState(GameModeState.Initializing);
            
            // 创建辅助处理器
            _networkHandler = new NetworkGameHandler(this);
            _frameSyncHandler = new FrameSyncHandler(this);
            
            // 注册网络消息处理器
            RegisterNetworkHandlers();
            
            // 注册事件（使用现有 EventSystem）
            EventSystem.Instance.Subscribe<FrameDataUploadEventData>(FrameDataUpload);
            EventSystem.Instance.Subscribe<NewPlayerEventData>(OnPlayerCreated);
            
            // 注册状态变化处理器（使用现有 EventSystem）
            EventSystem.Instance.Subscribe<GameModeStateChangedEventData>(OnStateChanged);
            
            ChangeState(GameModeState.Ready);
        }
        
        /// <summary>
        /// 启动联机游戏（联机模式不主动启动，等待服务器通知）
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public override void StartGame(string sceneName)
        {
            ASLogger.Instance.Info("MultiplayerGameMode: 联机模式等待服务器游戏开始通知");
            ChangeState(GameModeState.Loading);
        }
        
        /// <summary>
        /// 更新游戏逻辑
        /// </summary>
        /// <param name="deltaTime">时间差</param>
        public override void Update(float deltaTime)
        {
            if (!IsRunning || CurrentState != GameModeState.Playing) return;
            
            // 更新 Room 和 Stage
            MainRoom?.Update(deltaTime);
            MainStage?.Update(deltaTime);
        }
        
        /// <summary>
        /// 关闭联机游戏模式
        /// </summary>
        public override void Shutdown()
        {
            ASLogger.Instance.Info("MultiplayerGameMode: 关闭联机游戏模式");
            ChangeState(GameModeState.Ending);
            
            // 取消注册网络消息处理器
            UnregisterNetworkHandlers();
            
            // 取消订阅事件
            EventSystem.Instance.Unsubscribe<FrameDataUploadEventData>(FrameDataUpload);
            EventSystem.Instance.Unsubscribe<NewPlayerEventData>(OnPlayerCreated);
            
            // 取消注册状态变化处理器
            EventSystem.Instance.Unsubscribe<GameModeStateChangedEventData>(OnStateChanged);
            
            // 取消订阅EntityView事件
            if (MainStage != null)
            {
                MainStage.OnEntityViewAdded -= OnEntityViewAdded;
            }
            
            // 清理资源
            MainRoom = null;
            MainStage = null;
            PlayerId = -1;
            IsRunning = false;
            
            ChangeState(GameModeState.Finished);
        }
        
        #region 网络消息注册
        
        /// <summary>
        /// 注册网络消息处理器
        /// </summary>
        private void RegisterNetworkHandlers()
        {
            var networkManager = NetworkManager.Instance;
            if (networkManager != null)
            {
                // 游戏流程消息
                networkManager.OnGameResponse += _networkHandler.OnGameResponse;
                networkManager.OnGameStartNotification += _networkHandler.OnGameStartNotification;
                networkManager.OnGameEndNotification += _networkHandler.OnGameEndNotification;
                networkManager.OnGameStateUpdate += _networkHandler.OnGameStateUpdate;
                
                // 帧同步消息
                networkManager.OnFrameSyncStartNotification += _frameSyncHandler.OnFrameSyncStartNotification;
                networkManager.OnFrameSyncEndNotification += _frameSyncHandler.OnFrameSyncEndNotification;
                networkManager.OnFrameSyncData += _frameSyncHandler.OnFrameSyncData;
                networkManager.OnFrameInputs += _frameSyncHandler.OnFrameInputs;
                
                ASLogger.Instance.Info("MultiplayerGameMode: 网络消息处理器注册完成");
            }
            else
            {
                ASLogger.Instance.Warning("MultiplayerGameMode: NetworkManager 不存在，无法注册消息处理器");
            }
        }
        
        /// <summary>
        /// 取消注册网络消息处理器
        /// </summary>
        private void UnregisterNetworkHandlers()
        {
            var networkManager = NetworkManager.Instance;
            if (networkManager != null)
            {
                // 游戏流程消息
                networkManager.OnGameResponse -= _networkHandler.OnGameResponse;
                networkManager.OnGameStartNotification -= _networkHandler.OnGameStartNotification;
                networkManager.OnGameEndNotification -= _networkHandler.OnGameEndNotification;
                networkManager.OnGameStateUpdate -= _networkHandler.OnGameStateUpdate;
                
                // 帧同步消息
                networkManager.OnFrameSyncStartNotification -= _frameSyncHandler.OnFrameSyncStartNotification;
                networkManager.OnFrameSyncEndNotification -= _frameSyncHandler.OnFrameSyncEndNotification;
                networkManager.OnFrameSyncData -= _frameSyncHandler.OnFrameSyncData;
                networkManager.OnFrameInputs -= _frameSyncHandler.OnFrameInputs;
                
                ASLogger.Instance.Info("MultiplayerGameMode: 网络消息处理器取消注册完成");
            }
        }
        
        #endregion
        
        #region 事件处理
        
        /// <summary>
        /// 帧数据上传事件
        /// </summary>
        private void FrameDataUpload(FrameDataUploadEventData eventData)
        {
            var request = SingleInput.Create();
            request.PlayerID = PlayerId;
            request.FrameID = eventData.FrameID;
            request.Input = eventData.Input;
            request.Input.BornInfo = 0;
            NetworkManager.Instance.Send(request);
            ASLogger.Instance.Debug($"MultiplayerGameMode: 发送单帧输入，帧: {eventData.FrameID}，输入: {JsonConvert.SerializeObject(eventData.Input)}");
        }
        
        /// <summary>
        /// 玩家创建事件
        /// </summary>
        private void OnPlayerCreated(NewPlayerEventData eventData)
        {
            if (eventData.BornInfo == UserManager.Instance.UserId.GetHashCode())
            {
                PlayerId = eventData.PlayerID;
                
                // 关键：设置 Room.MainPlayerId，否则 LSController 不会发布输入事件
                if (MainRoom != null)
                {
                    MainRoom.MainPlayerId = eventData.PlayerID;
                }
                
                if (MainRoom?.LSController != null)
                {
                    MainRoom.LSController.MaxPredictionFrames = 5;
                }
                
                ASLogger.Instance.Info("MultiplayerGameMode: 主玩家创建完成, ID: " + PlayerId);
                
                // 设置相机跟随主玩家
                SetCameraFollowMainPlayer();
            }
            ASLogger.Instance.Debug($"MultiplayerGameMode: <OnPlayerCreated>: {eventData.PlayerID} , BornInfo: {eventData.BornInfo}");
        }
        
        #endregion
        
        #region 公共方法（供 Handler 调用）
        
        /// <summary>
        /// 设置 Room 和 Stage（由 NetworkGameHandler 调用）
        /// </summary>
        public void SetRoomAndStage(Room room, Stage stage)
        {
            MainRoom = room;
            MainStage = stage;
            IsRunning = true;
        }
        
        /// <summary>
        /// 清理 Room 和 Stage（由 NetworkGameHandler 调用）
        /// </summary>
        public void ClearRoomAndStage()
        {
            MainRoom = null;
            MainStage = null;
            PlayerId = -1;
            IsRunning = false;
        }
        
        /// <summary>
        /// 请求创建玩家（由 NetworkGameHandler 调用）
        /// </summary>
        public void RequestCreatePlayer()
        {
            var request = SingleInput.Create();
            request.FrameID = MainRoom.LSController.PredictionFrame;
            request.Input = new LSInput();
            request.Input.BornInfo = UserManager.Instance.UserId.GetHashCode();
            NetworkManager.Instance.Send(request);
            ASLogger.Instance.Debug($"MultiplayerGameMode: 发送生成玩家请求，输入: {JsonConvert.SerializeObject(request.Input)}");
        }
        
        /// <summary>
        /// EntityView创建事件处理（由 NetworkGameHandler 调用）
        /// </summary>
        public void OnEntityViewAdded(EntityView entityView)
        {
            if (entityView == null) return;
            
            // 检查是否是主玩家的EntityView
            if (entityView.EntityId == PlayerId)
            {
                ASLogger.Instance.Info($"MultiplayerGameMode: 主玩家EntityView创建完成，ID: {entityView.EntityId}");
                
                // 设置相机跟随目标
                var cameraManager = CameraManager.Instance;
                if (cameraManager != null)
                {
                    cameraManager.SetFollowTarget(entityView.Transform);
                    ASLogger.Instance.Info("MultiplayerGameMode: 设置相机跟随主玩家");
                }
                else
                {
                    ASLogger.Instance.Warning("MultiplayerGameMode: CameraManager未找到");
                }
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
                    ASLogger.Instance.Info($"MultiplayerGameMode: 设置相机跟随主玩家，ID: {PlayerId}");
                }
                else
                {
                    ASLogger.Instance.Warning("MultiplayerGameMode: CameraManager未找到");
                }
            }
            else
            {
                ASLogger.Instance.Debug($"MultiplayerGameMode: 主玩家EntityView尚未创建，ID: {PlayerId}");
            }
        }
        
        #endregion
        
        #region 状态管理和事件处理
        
        /// <summary>
        /// 状态变化事件处理
        /// </summary>
        private void OnStateChanged(GameModeStateChangedEventData evt)
        {
            ASLogger.Instance.Info($"MultiplayerGameMode: 状态从 {evt.PreviousState} 变为 {evt.NewState}");
            
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
            ASLogger.Instance.Info("MultiplayerGameMode: 游戏开始");
        }
        
        /// <summary>
        /// 游戏暂停时的处理
        /// </summary>
        private void OnGamePause()
        {
            ASLogger.Instance.Info("MultiplayerGameMode: 游戏暂停");
        }
        
        /// <summary>
        /// 游戏结束时的处理
        /// </summary>
        private void OnGameEnd()
        {
            ASLogger.Instance.Info("MultiplayerGameMode: 游戏结束");
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
                ModeName = "Multiplayer",
                AutoSave = true,
                UpdateInterval = 0.016f,
                CustomSettings = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "NetworkTimeout", 30 },
                    { "MaxReconnectAttempts", 3 },
                    { "FrameSyncRate", 20 },
                    { "EnableLagCompensation", true }
                }
            };
        }
        
        #endregion
    }
}

