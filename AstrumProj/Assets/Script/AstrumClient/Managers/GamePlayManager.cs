using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.Client.Managers.GameModes;
using Astrum.Generated;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using Astrum.View.Core;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;


namespace Astrum.Client.Managers
{

    /// <summary>
    /// 游戏玩法管理器 - 轻量级入口，委托给具体的 GameMode
    /// </summary>
    public class GamePlayManager : Singleton<GamePlayManager>
    {
        // 当前游戏模式实例
        private IGameMode _currentGameMode;
        
        // 对外接口属性（委托给 GameMode）
        public Stage MainStage => _currentGameMode?.MainStage;
        public Room MainRoom => _currentGameMode?.MainRoom;
        public long PlayerId => _currentGameMode?.PlayerId ?? -1;

        // 网络游戏相关 - 使用专门的管理器
        public UserInfo CurrentUser => UserManager.Instance?.CurrentUser;
        public List<RoomInfo> AvailableRooms => RoomSystemManager.Instance?.AvailableRooms ?? new List<RoomInfo>();
        public List<UserInfo> OnlineUsers { get; private set; } = new List<UserInfo>();

        // 网络游戏状态
        public bool IsLoggedIn => UserManager.Instance?.IsLoggedIn ?? false;
        public bool IsInRoom => RoomSystemManager.Instance?.IsInRoom ?? false;

        public void Initialize()
        {
            // 初始化用户管理器和房间系统管理器
            UserManager.Instance?.Initialize();
            RoomSystemManager.Instance?.Initialize();

            // 根据 GameConfig 创建对应的 GameMode
            if (Astrum.Client.Core.GameConfig.Instance.IsSinglePlayerMode)
            {
                _currentGameMode = new SinglePlayerGameMode();
                ASLogger.Instance.Info("GamePlayManager: 创建单机游戏模式");
            }
            else
            {
                _currentGameMode = new MultiplayerGameMode();
                ASLogger.Instance.Info("GamePlayManager: 创建联机游戏模式");
            }
            
            // 初始化 GameMode
            _currentGameMode.Initialize();

            // 注册网络消息处理器（Login/Room 相关）
            RegisterNetworkMessageHandlers();
            
            ASLogger.Instance.Info($"GamePlayManager: 初始化完成 - 模式: {_currentGameMode.ModeName}");
        }

        /// <summary>
        /// 注册网络消息处理器（仅 Login/Room 相关，游戏和帧同步消息在 GameMode 中注册）
        /// </summary>
        private void RegisterNetworkMessageHandlers()
        {
            var networkManager = GameApplication.Instance?.NetworkManager;
            if (networkManager != null)
            {
                // 注册 Login 和 Room 相关的消息处理器
                networkManager.OnLoginResponse += OnLoginResponse;
                networkManager.OnCreateRoomResponse += OnCreateRoomResponse;
                networkManager.OnJoinRoomResponse += OnJoinRoomResponse;
                networkManager.OnLeaveRoomResponse += OnLeaveRoomResponse;
                networkManager.OnGetRoomListResponse += OnGetRoomListResponse;
                networkManager.OnRoomUpdateNotification += OnRoomUpdateNotification;
                networkManager.OnHeartbeatResponse += OnHeartbeatResponse;
                
                // 游戏和帧同步消息已移至 MultiplayerGameMode

                ASLogger.Instance.Info("GamePlayManager: Login/Room 消息处理器注册完成");
            }
            else
            {
                ASLogger.Instance.Warning("GamePlayManager: NetworkManager 不存在，无法注册消息处理器");
            }
        }

        /// <summary>
        /// 取消注册网络消息处理器
        /// </summary>
        private void UnregisterNetworkMessageHandlers()
        {
            var networkManager = GameApplication.Instance?.NetworkManager;
            if (networkManager != null)
            {
                // 取消注册 Login 和 Room 相关的消息处理器
                networkManager.OnLoginResponse -= OnLoginResponse;
                networkManager.OnCreateRoomResponse -= OnCreateRoomResponse;
                networkManager.OnJoinRoomResponse -= OnJoinRoomResponse;
                networkManager.OnLeaveRoomResponse -= OnLeaveRoomResponse;
                networkManager.OnGetRoomListResponse -= OnGetRoomListResponse;
                networkManager.OnRoomUpdateNotification -= OnRoomUpdateNotification;
                networkManager.OnHeartbeatResponse -= OnHeartbeatResponse;
                
                // 游戏和帧同步消息已移至 MultiplayerGameMode

                ASLogger.Instance.Info("GamePlayManager: Login/Room 消息处理器取消注册完成");
            }
        }

        public void Update(float deltaTime)
        {
            // 委托给当前 GameMode 更新
            _currentGameMode?.Update(deltaTime);
        }

        public void Shutdown()
        {
            // 关闭当前 GameMode
            _currentGameMode?.Shutdown();
            _currentGameMode = null;
            
            // 取消注册网络消息处理器
            UnregisterNetworkMessageHandlers();

            // 清理网络游戏状态
            OnlineUsers.Clear();

            ASLogger.Instance.Info("GamePlayManager: 已关闭");
        }

        

        #region 网络消息事件处理器

        /// <summary>
        /// 处理登录响应
        /// </summary>
        private void OnLoginResponse(LoginResponse response)
        {
            try
            {
                ASLogger.Instance.Info("GamePlayManager: 收到登录响应");

                if (response.Success)
                {
                    // 使用用户管理器处理登录响应
                    UserManager.Instance?.HandleLoginResponse(response);
                    
                    // 登录成功后关闭Login UI并显示RoomList UI
                    CloseLoginUI();
                    ShowRoomListUI();
                }
                else
                {
                    ASLogger.Instance.Error($"GamePlayManager: 登录失败 - {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理登录响应时发生异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 处理创建房间响应
        /// </summary>
        private void OnCreateRoomResponse(CreateRoomResponse response)
        {
            try
            {
                ASLogger.Instance.Info("GamePlayManager: 收到创建房间响应");

                if (response.Success)
                {
                    // 使用房间系统管理器处理创建房间响应
                    RoomSystemManager.Instance?.HandleCreateRoomResponse(response);
                }
                else
                {
                    ASLogger.Instance.Error($"GamePlayManager: 创建房间失败 - {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理创建房间响应时发生异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 处理加入房间响应
        /// </summary>
        private void OnJoinRoomResponse(JoinRoomResponse response)
        {
            try
            {
                ASLogger.Instance.Info("GamePlayManager: 收到加入房间响应");

                if (response.Success)
                {
                    // 使用房间系统管理器处理加入房间响应
                    RoomSystemManager.Instance?.HandleJoinRoomResponse(response);
                }
                else
                {
                    ASLogger.Instance.Error($"GamePlayManager: 加入房间失败 - {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理加入房间响应时发生异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 处理离开房间响应
        /// </summary>
        private void OnLeaveRoomResponse(LeaveRoomResponse response)
        {
            try
            {
                ASLogger.Instance.Info("GamePlayManager: 收到离开房间响应");

                if (response.Success)
                {
                    // 使用房间系统管理器处理离开房间响应
                    RoomSystemManager.Instance?.HandleLeaveRoomResponse(response);
                }
                else
                {
                    ASLogger.Instance.Error($"GamePlayManager: 离开房间失败 - {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理离开房间响应时发生异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 处理获取房间列表响应
        /// </summary>
        private void OnGetRoomListResponse(GetRoomListResponse response)
        {
            try
            {
                ASLogger.Instance.Info("GamePlayManager: 收到获取房间列表响应");

                if (response.Success)
                {
                    // 使用房间系统管理器处理获取房间列表响应
                    RoomSystemManager.Instance?.HandleRoomListResponse(response);
                }
                else
                {
                    ASLogger.Instance.Error($"GamePlayManager: 获取房间列表失败 - {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理获取房间列表响应时发生异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 处理房间更新通知
        /// </summary>
        private void OnRoomUpdateNotification(RoomUpdateNotification notification)
        {
            try
            {
                ASLogger.Instance.Info("GamePlayManager: 收到房间更新通知");

                // 使用房间系统管理器处理房间更新通知
                RoomSystemManager.Instance?.HandleRoomUpdateNotification(notification);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理房间更新通知时发生异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 处理心跳响应
        /// </summary>
        private void OnHeartbeatResponse(HeartbeatResponse response)
        {
            try
            {
                ASLogger.Instance.Debug("GamePlayManager: 收到心跳响应");
                // 心跳响应通常不需要特殊处理
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理心跳响应时发生异常 - {ex.Message}");
            }
        }

        #endregion


        #region 游戏启动方法
        
        /// <summary>
        /// 启动游戏（统一入口，委托给 GameMode）
        /// </summary>
        /// <param name="gameSceneName">游戏场景名称</param>
        public void StartGame(string gameSceneName)
        {
            _currentGameMode?.StartGame(gameSceneName);
        }
        
        /// <summary>
        /// 启动单机游戏（兼容旧接口）
        /// </summary>
        public void StartSinglePlayerGame(string gameSceneName)
        {
            StartGame(gameSceneName);
        }
        
        #endregion
        
        #region UI 辅助方法
        
        /// <summary>
        /// 关闭Login UI
        /// </summary>
        private void CloseLoginUI()
        {
            try
            {
                var uiManager = GameApplication.Instance?.UIManager;
                if (uiManager != null)
                {
                    uiManager.HideUI("Login");
                    uiManager.DestroyUI("Login");
                    ASLogger.Instance.Info("GamePlayManager: Login UI已关闭");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 关闭Login UI失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 显示RoomList UI
        /// </summary>
        private void ShowRoomListUI()
        {
            try
            {
                var uiManager = GameApplication.Instance?.UIManager;
                if (uiManager != null)
                {
                    uiManager.ShowUI("RoomList");
                    ASLogger.Instance.Info("GamePlayManager: RoomList UI已显示");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 显示RoomList UI失败 - {ex.Message}");
            }
        }
        
        #endregion
        
        #region 已迁移到 GameMode 的方法（以下方法已废弃，保留注释作为参考）
        
        /* 
         * 以下方法已迁移至 SinglePlayerGameMode 或 MultiplayerGameMode：
         * - StartSinglePlayerGame() 核心逻辑 → SinglePlayerGameMode.StartGame()
         * - CreateRoom() → SinglePlayerGameMode.CreateRoom()
         * - CreateStage() → SinglePlayerGameMode.CreateStage()
         * - CreatePlayer() → SinglePlayerGameMode.CreatePlayer()
         * - SwitchToGameScene() → SinglePlayerGameMode.SwitchToGameScene()
         * - OnGameSceneLoaded() → SinglePlayerGameMode.OnGameSceneLoaded()
         * - SetCameraFollowMainPlayer() → SinglePlayerGameMode/MultiplayerGameMode
         * - OnEntityViewAdded() → SinglePlayerGameMode/MultiplayerGameMode
         * - OnGameResponse() → NetworkGameHandler.OnGameResponse()
         * - OnGameStartNotification() → NetworkGameHandler.OnGameStartNotification()
         * - OnGameEndNotification() → NetworkGameHandler.OnGameEndNotification()
         * - OnGameStateUpdate() → NetworkGameHandler.OnGameStateUpdate()
         * - OnFrameSyncStartNotification() → FrameSyncHandler.OnFrameSyncStartNotification()
         * - OnFrameSyncEndNotification() → FrameSyncHandler.OnFrameSyncEndNotification()
         * - OnFrameSyncData() → FrameSyncHandler.OnFrameSyncData()
         * - OnFrameInputs() → FrameSyncHandler.OnFrameInputs()
         * - DealNetFrameInputs() → FrameSyncHandler.DealNetFrameInputs()
         * - FrameDataUpload() → MultiplayerGameMode.FrameDataUpload()
         * - OnPlayerCreated() → MultiplayerGameMode.OnPlayerCreated()
         */
        
        #endregion
        
        // 所有单机和联机的游戏启动、帧同步、相机跟随逻辑已迁移至 GameMode 类
        // 如需查看旧代码，请查看 Git 历史记录
    }
}
