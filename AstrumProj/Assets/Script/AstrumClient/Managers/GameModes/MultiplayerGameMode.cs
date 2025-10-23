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
        private FrameSyncHandler _frameSyncHandler;
        
        /// <summary>
        /// 初始化联机游戏模式
        /// </summary>
        public override void Initialize()
        {
            ASLogger.Instance.Info("MultiplayerGameMode: 初始化联机游戏模式");
            ChangeState(GameModeState.Initializing);
            
            // 创建辅助处理器
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
                networkManager.OnGameResponse += OnGameResponse;
                networkManager.OnGameStartNotification += OnGameStartNotification;
                networkManager.OnGameEndNotification += OnGameEndNotification;
                networkManager.OnGameStateUpdate += OnGameStateUpdate;
                
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
                networkManager.OnGameResponse -= OnGameResponse;
                networkManager.OnGameStartNotification -= OnGameStartNotification;
                networkManager.OnGameEndNotification -= OnGameEndNotification;
                networkManager.OnGameStateUpdate -= OnGameStateUpdate;
                
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
            ASLogger.Instance.Info($"MultiplayerGameMode: 发送生成玩家请求，输入: {JsonConvert.SerializeObject(request.Input)}");
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
        
        #region 网络消息处理（从 NetworkGameHandler 迁移）
        
        /// <summary>
        /// 处理游戏响应
        /// </summary>
        public void OnGameResponse(GameResponse response)
        {
            try
            {
                ASLogger.Instance.Info(
                    $"MultiplayerGameMode: 收到游戏响应 - Success: {response.success}, Message: {response.message}");
                
                if (response.success)
                {
                    ASLogger.Instance.Info($"游戏操作成功: {response.message}");
                }
                else
                {
                    ASLogger.Instance.Warning($"游戏操作失败: {response.message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理游戏响应时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理游戏开始通知
        /// </summary>
        public void OnGameStartNotification(GameStartNotification notification)
        {
            try
            {
                ASLogger.Instance.Info($"MultiplayerGameMode: 收到游戏开始通知 - 房间: {notification.roomId}");
                
                // 创建游戏Room和Stage
                var room = CreateGameRoom(notification);
                var world = new World();
                room.MainWorld = world;
                room.Initialize();
                var stage = CreateGameStage(room);
                
                // 设置当前游戏状态
                SetRoomAndStage(room, stage);
                
                // 切换到游戏场景
                SwitchToGameScene("DungeonsGame", stage);
                
                ASLogger.Instance.Info($"游戏开始 - 房间: {notification.roomId}, 玩家数: {notification.playerIds.Count}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理游戏开始通知时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理游戏结束通知
        /// </summary>
        public void OnGameEndNotification(GameEndNotification notification)
        {
            try
            {
                ASLogger.Instance.Info(
                    $"MultiplayerGameMode: 收到游戏结束通知 - 房间: {notification.roomId}, 原因: {notification.reason}");
                
                // 显示游戏结果
                ShowGameResult(notification.result);
                
                // 清理游戏状态
                ClearRoomAndStage();
                
                // 返回房间列表
                ReturnToRoomList();
                
                ASLogger.Instance.Info($"游戏结束 - 房间: {notification.roomId}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理游戏结束通知时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理游戏状态更新
        /// </summary>
        public void OnGameStateUpdate(GameStateUpdate update)
        {
            try
            {
                ASLogger.Instance.Info(
                    $"MultiplayerGameMode: 收到游戏状态更新 - 房间: {update.roomId}, 回合: {update.roomState.currentRound}");
                
                // 更新游戏状态
                if (MainRoom != null)
                {
                    ASLogger.Instance.Info(
                        $"游戏状态更新 - 当前回合: {update.roomState.currentRound}/{update.roomState.maxRounds}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理游戏状态更新时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        #region 私有辅助方法
        
        /// <summary>
        /// 创建游戏房间
        /// </summary>
        private Room CreateGameRoom(GameStartNotification notification)
        {
            var room = new Room(1, notification.roomId);
            ASLogger.Instance.Info($"创建游戏房间: {room.RoomId}");
            return room;
        }
        
        /// <summary>
        /// 创建游戏舞台
        /// </summary>
        private Stage CreateGameStage(Room room)
        {
            var stage = new Stage("GameStage", "游戏场景");
            stage.Initialize();
            stage.SetRoom(room);
            ASLogger.Instance.Info("创建游戏舞台");
            return stage;
        }
        
        /// <summary>
        /// 切换到游戏场景
        /// </summary>
        private void SwitchToGameScene(string sceneName, Stage stage)
        {
            ASLogger.Instance.Info($"MultiplayerGameMode: 切换到游戏场景 {sceneName}");
            
            var sceneManager = SceneManager.Instance;
            if (sceneManager == null)
            {
                throw new Exception("SceneManager不存在");
            }
            
            // 设置当前Stage
            StageManager.Instance.SetCurrentStage(stage);
            ChangeState(GameModeState.Loading);
            // 异步加载游戏场景
            sceneManager.LoadSceneAsync(sceneName, () => { OnGameSceneLoaded(stage); });
        }
        
        /// <summary>
        /// 游戏场景加载完成回调
        /// </summary>
        private void OnGameSceneLoaded(Stage stage)
        {
            ASLogger.Instance.Info("MultiplayerGameMode: 游戏场景加载完成");
            ChangeState(GameModeState.Ready);
            // 关闭Login UI
            CloseLoginUI();
            
            // 激活Stage
            stage.SetActive(true);
            stage.OnEnter();
            
            // 订阅EntityView创建事件，用于设置相机跟随
            stage.OnEntityViewAdded += OnEntityViewAdded;
            
            // 联机模式：请求创建玩家
            RequestCreatePlayer();
            ChangeState(GameModeState.Playing);
            
            ASLogger.Instance.Info("MultiplayerGameMode: 游戏准备完成");
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
                    uiManager.DestroyUI("RoomList");
                    uiManager.DestroyUI("RoomDetail");
                    ASLogger.Instance.Info("MultiplayerGameMode: Login UI已关闭");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MultiplayerGameMode: 关闭Login UI失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 显示游戏结果
        /// </summary>
        private void ShowGameResult(GameResult result)
        {
            try
            {
                ASLogger.Instance.Info($"游戏结果 - 房间: {result.roomId}");
                
                foreach (var playerResult in result.playerResults)
                {
                    ASLogger.Instance.Info(
                        $"玩家 {playerResult.playerId}: 分数 {playerResult.score}, 排名 {playerResult.rank}, 获胜: {playerResult.isWinner}");
                }
                
                // TODO: 实现游戏结果UI显示
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"显示游戏结果时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 返回房间列表
        /// </summary>
        private void ReturnToRoomList()
        {
            try
            {
                // TODO: 实现返回房间列表的UI切换
                ASLogger.Instance.Info("返回房间列表");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"返回房间列表时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        #endregion
        
        #endregion
    }
}

