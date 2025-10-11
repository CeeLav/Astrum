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
    public class MultiplayerGameMode : IGameMode
    {
        // 核心属性
        public Room MainRoom { get; private set; }
        public Stage MainStage { get; private set; }
        public long PlayerId { get; private set; }
        public string ModeName => "Multiplayer";
        public bool IsRunning { get; private set; }
        
        // 辅助处理器
        private NetworkGameHandler _networkHandler;
        private FrameSyncHandler _frameSyncHandler;
        
        /// <summary>
        /// 初始化联机游戏模式
        /// </summary>
        public void Initialize()
        {
            ASLogger.Instance.Info("MultiplayerGameMode: 初始化联机游戏模式");
            
            // 创建辅助处理器
            _networkHandler = new NetworkGameHandler(this);
            _frameSyncHandler = new FrameSyncHandler(this);
            
            // 注册网络消息处理器
            RegisterNetworkHandlers();
            
            // 注册事件
            EventSystem.Instance.Subscribe<FrameDataUploadEventData>(FrameDataUpload);
            EventSystem.Instance.Subscribe<NewPlayerEventData>(OnPlayerCreated);
            
            IsRunning = false;
        }
        
        /// <summary>
        /// 启动联机游戏（联机模式不主动启动，等待服务器通知）
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public void StartGame(string sceneName)
        {
            ASLogger.Instance.Info("MultiplayerGameMode: 联机模式等待服务器游戏开始通知");
        }
        
        /// <summary>
        /// 更新游戏逻辑
        /// </summary>
        /// <param name="deltaTime">时间差</param>
        public void Update(float deltaTime)
        {
            if (!IsRunning) return;
            
            // 更新 Room 和 Stage
            MainRoom?.Update(deltaTime);
            MainStage?.Update(deltaTime);
        }
        
        /// <summary>
        /// 关闭联机游戏模式
        /// </summary>
        public void Shutdown()
        {
            ASLogger.Instance.Info("MultiplayerGameMode: 关闭联机游戏模式");
            
            // 取消注册网络消息处理器
            UnregisterNetworkHandlers();
            
            // 取消订阅事件
            EventSystem.Instance.Unsubscribe<FrameDataUploadEventData>(FrameDataUpload);
            EventSystem.Instance.Unsubscribe<NewPlayerEventData>(OnPlayerCreated);
            
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
        }
        
        #region 网络消息注册
        
        /// <summary>
        /// 注册网络消息处理器
        /// </summary>
        private void RegisterNetworkHandlers()
        {
            var networkManager = GameApplication.Instance?.NetworkManager;
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
            var networkManager = GameApplication.Instance?.NetworkManager;
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
                var cameraManager = GameApplication.Instance?.CameraManager;
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
                var cameraManager = GameApplication.Instance?.CameraManager;
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
    }
}

