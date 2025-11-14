using System;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.Client.Data;
using Astrum.Client.Managers.GameModes.Handlers;
using Astrum.Generated;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using Astrum.View.Core;
using Unity.Plastic.Newtonsoft.Json;
using AstrumClient.MonitorTools;
using UnityEngine.VFX;

namespace Astrum.Client.Managers.GameModes
{
    /// <summary>
    /// 联机游戏模式 - 网络多人对战
    /// </summary>
    [MonitorTarget]
    public class MultiplayerGameMode : BaseGameMode
    {
        private const int DungeonsGameSceneId = 2;
        
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

            MonitorManager.Register(this); // 监控注册
        }
        
        /// <summary>
        /// 启动联机游戏（联机模式不主动启动，等待服务器通知）
        /// </summary>
        /// <param name="sceneId">场景配置ID</param>
        public override void StartGame(int sceneId)
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
            // TODO: 这些Action事件已被新的消息处理器系统替代
            // 新的消息处理器会自动处理这些消息类型
            ASLogger.Instance.Info("MultiplayerGameMode: 使用新的消息处理器系统，无需手动注册Action事件");
        }
        
        /// <summary>
        /// 取消注册网络消息处理器
        /// </summary>
        private void UnregisterNetworkHandlers()
        {
            // TODO: 这些Action事件已被新的消息处理器系统替代
            // 新的消息处理器系统会自动管理，无需手动取消注册
            ASLogger.Instance.Info("MultiplayerGameMode: 使用新的消息处理器系统，无需手动取消注册Action事件");
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
                
                if (MainRoom?.LSController is ClientLSController clientSync)
                {
                    clientSync.MaxPredictionFrames = 5;
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
            View.Managers.VFXManager.Instance.CurrentStage = stage;
            MainStage.SetRoom(room);
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
            if (MainRoom.LSController is ClientLSController clientSync)
            {
                request.FrameID = clientSync.PredictionFrame;
            }
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
                
                // 只创建 Stage，不创建 Room（Room 等待 FrameSyncStartNotification 到达时创建）
                if (MainStage == null)
                {
                    var stage = CreateGameStage(null);
                    
                    // 设置当前游戏状态（Room 可能为 null，等待 FrameSyncStartNotification）
                    MainStage = stage;
                    View.Managers.VFXManager.Instance.CurrentStage = stage;
                    
                    // 如果 Room 已存在（FrameSyncStartNotification 先到达），设置 Room 到 Stage
                    if (MainRoom != null)
                    {
                        SetRoomAndStage(MainRoom, MainStage);
                        ASLogger.Instance.Info($"已设置现有 Room 到 Stage", "MultiplayerGameMode");
                    }
                    
                    // 切换到游戏场景
                    SwitchToGameScene(DungeonsGameSceneId, stage);
                }
                else
                {
                    // Stage 已存在，只需要切换场景（如果还没切换）
                    ASLogger.Instance.Info($"Stage 已存在，只切换场景", "MultiplayerGameMode");
                    SwitchToGameScene(DungeonsGameSceneId, MainStage);
                }
                
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
        
        /// <summary>
        /// 处理帧同步开始通知（供消息处理器调用）
        /// </summary>
        public void OnFrameSyncStartNotification(FrameSyncStartNotification notification)
        {
            _frameSyncHandler?.OnFrameSyncStartNotification(notification);
        }
        
        /// <summary>
        /// 处理帧同步结束通知（供消息处理器调用）
        /// </summary>
        public void OnFrameSyncEndNotification(FrameSyncEndNotification notification)
        {
            _frameSyncHandler?.OnFrameSyncEndNotification(notification);
        }
        
        /// <summary>
        /// 处理帧同步数据（供消息处理器调用）
        /// </summary>
        public void OnFrameSyncData(FrameSyncData frameData)
        {
            _frameSyncHandler?.OnFrameSyncData(frameData);
        }
        
        /// <summary>
        /// 处理帧输入数据（供消息处理器调用）
        /// </summary>
        public void OnFrameInputs(OneFrameInputs frameInputs)
        {
            _frameSyncHandler?.OnFrameInputs(frameInputs);
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
        /// <param name="room">房间（可以为 null，后续再设置）</param>
        private Stage CreateGameStage(Room room)
        {
            var stage = new Stage("GameStage", "游戏场景");
            stage.Initialize();
            if (room != null)
            {
                stage.SetRoom(room);
            }
            ASLogger.Instance.Info($"创建游戏舞台{(room != null ? "（已设置Room）" : "（Room待设置）")}");
            return stage;
        }
        
        /// <summary>
        /// 切换到游戏场景
        /// </summary>
        private void SwitchToGameScene(int sceneId, Stage stage)
        {
            ASLogger.Instance.Info($"MultiplayerGameMode: 切换到游戏场景 SceneId={sceneId}");
            
            var sceneManager = SceneManager.Instance;
            if (sceneManager == null)
            {
                throw new Exception("SceneManager不存在");
            }
            
            // 设置当前Stage
            StageManager.Instance.SetCurrentStage(stage);
            ChangeState(GameModeState.Loading);
            // 异步加载游戏场景
            sceneManager.LoadSceneAsync(sceneId, () => { OnGameSceneLoaded(stage); });
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
            
            // 查找现有的 Entity 中是否有 Player，如果有则设置
            FindAndSetupPlayer();
            
            // 联机模式：请求创建玩家
            //RequestCreatePlayer();
            ChangeState(GameModeState.Playing);
            
            ASLogger.Instance.Info("MultiplayerGameMode: 游戏准备完成");
        }
        
        /// <summary>
        /// 查找现有的 Entity 中是否有 Player，如果有则设置相关属性
        /// </summary>
        private void FindAndSetupPlayer()
        {
            if (MainRoom?.MainWorld == null)
            {
                ASLogger.Instance.Warning("MultiplayerGameMode: MainRoom 或 MainWorld 为空，无法查找 Player", "MultiplayerGameMode");
                return;
            }
            
            // 如果 PlayerId 已经设置，检查对应的 Entity 是否存在
            if (PlayerId > 0)
            {
                var playerEntity = MainRoom.MainWorld.GetEntity(PlayerId);
                if (playerEntity != null && !playerEntity.IsDestroyed)
                {
                    ASLogger.Instance.Info($"MultiplayerGameMode: 找到现有 Player Entity - ID: {PlayerId}, Name: {playerEntity.Name}", "MultiplayerGameMode");
                    
                    // 确保 MainRoom.MainPlayerId 已设置
                    if (MainRoom.MainPlayerId != PlayerId)
                    {
                        MainRoom.MainPlayerId = PlayerId;
                        ASLogger.Instance.Info($"MultiplayerGameMode: 设置 MainRoom.MainPlayerId = {PlayerId}", "MultiplayerGameMode");
                    }
                    
                    // 设置相机跟随主玩家（如果 EntityView 已创建）
                    SetCameraFollowMainPlayer();

                    return;
                }
                else
                {
                    ASLogger.Instance.Warning($"MultiplayerGameMode: PlayerId 已设置 ({PlayerId})，但对应的 Entity 不存在或已销毁", "MultiplayerGameMode");
                }
            }
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

