using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.Generated;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using Astrum.View.Core;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;


namespace Astrum.Client.Managers
{

    /// <summary>
    /// 游戏玩法管理器 - 负责管理一局游戏的内容
    /// </summary>
    public class GamePlayManager : Singleton<GamePlayManager>
    {

        public Stage MainStage { get; private set; }
        public Room MainRoom { get; private set; }

        public long PlayerId
        {
            get => MainRoom?.MainPlayerId ?? -1;
            private set
            {
                if (MainRoom != null)
                {
                    MainRoom.MainPlayerId = value;
                }
            }
        }

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

            // 注册网络消息处理器
            RegisterNetworkMessageHandlers();
            EventSystem.Instance.Subscribe<FrameDataUploadEventData>(FrameDataUpload);
            EventSystem.Instance.Subscribe<NewPlayerEventData>(OnPlayerCreated);
            
            ASLogger.Instance.Info("GamePlayManager: 初始化完成");
        }

        private void FrameDataUpload(FrameDataUploadEventData eventData)
        {
            var request = SingleInput.Create();
            request.PlayerID = PlayerId;
            request.FrameID = eventData.FrameID;
            request.Input = eventData.Input;
            request.Input.BornInfo = 0;
            NetworkManager.Instance.Send(request);
            ASLogger.Instance.Debug($"GamePlayManager: 发送单帧输入，帧: {eventData.FrameID}，输入: {JsonConvert.SerializeObject(eventData.Input)}");

        }

        private void OnPlayerCreated(NewPlayerEventData eventData)
        {
            if (eventData.BornInfo == UserManager.Instance.UserId.GetHashCode())
            {
                PlayerId = eventData.PlayerID;
                //MainRoom.MainPlayerId = eventData.PlayerID;
                MainRoom.LSController.MaxPredictionFrames = 5;
                ASLogger.Instance.Info("GamePlayManager: 主玩家创建完成, ID: " + PlayerId);
            }
            ASLogger.Instance.Debug($"GamePlayManager: <OnPlayerCreated>: {eventData.PlayerID} , BornInfo: {eventData.BornInfo}");
        }

        /// <summary>
        /// 注册网络消息处理器
        /// </summary>
        private void RegisterNetworkMessageHandlers()
        {
            var networkManager = GameApplication.Instance?.NetworkManager;
            if (networkManager != null)
            {
                // 注册具体消息类型的事件处理器
                networkManager.OnLoginResponse += OnLoginResponse;
                networkManager.OnCreateRoomResponse += OnCreateRoomResponse;
                networkManager.OnJoinRoomResponse += OnJoinRoomResponse;
                networkManager.OnLeaveRoomResponse += OnLeaveRoomResponse;
                networkManager.OnGetRoomListResponse += OnGetRoomListResponse;
                networkManager.OnRoomUpdateNotification += OnRoomUpdateNotification;
                networkManager.OnHeartbeatResponse += OnHeartbeatResponse;
                networkManager.OnGameResponse += OnGameResponse;
                networkManager.OnGameStartNotification += OnGameStartNotification;
                networkManager.OnGameEndNotification += OnGameEndNotification;
                networkManager.OnGameStateUpdate += OnGameStateUpdate;
                networkManager.OnFrameSyncStartNotification += OnFrameSyncStartNotification;
                networkManager.OnFrameSyncEndNotification += OnFrameSyncEndNotification;
                networkManager.OnFrameSyncData += OnFrameSyncData;
                networkManager.OnFrameInputs += OnFrameInputs;

                ASLogger.Instance.Info("GamePlayManager: 网络消息处理器注册完成");
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
                // 取消注册具体消息类型的事件处理器
                networkManager.OnLoginResponse -= OnLoginResponse;
                networkManager.OnCreateRoomResponse -= OnCreateRoomResponse;
                networkManager.OnJoinRoomResponse -= OnJoinRoomResponse;
                networkManager.OnLeaveRoomResponse -= OnLeaveRoomResponse;
                networkManager.OnGetRoomListResponse -= OnGetRoomListResponse;
                networkManager.OnRoomUpdateNotification -= OnRoomUpdateNotification;
                networkManager.OnHeartbeatResponse -= OnHeartbeatResponse;
                networkManager.OnGameResponse -= OnGameResponse;
                networkManager.OnGameStartNotification -= OnGameStartNotification;
                networkManager.OnGameEndNotification -= OnGameEndNotification;
                networkManager.OnGameStateUpdate -= OnGameStateUpdate;
                networkManager.OnFrameSyncStartNotification -= OnFrameSyncStartNotification;
                networkManager.OnFrameSyncEndNotification -= OnFrameSyncEndNotification;
                networkManager.OnFrameSyncData -= OnFrameSyncData;
                networkManager.OnFrameInputs -= OnFrameInputs;

                ASLogger.Instance.Info("GamePlayManager: 网络消息处理器取消注册完成");
            }
        }

        public void Update(float deltaTime)
        {
            if (MainRoom != null)
            {
                MainRoom.Update(deltaTime);
            }

            if (MainStage != null)
            {
                MainStage.Update(deltaTime);
            }

        }

        public void Shutdown()
        {
            // 取消注册网络消息处理器
            UnregisterNetworkMessageHandlers();

            // 清理管理器事件
            //UnregisterManagerEvents();

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
                }
                else
                {
                    ASLogger.Instance.Error($"GamePlayManager: 登录失败 - {response.Message}");
                    //OnNetworkError?.Invoke($"登录失败: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理登录响应时发生异常 - {ex.Message}");
                //OnNetworkError?.Invoke($"处理登录响应失败: {ex.Message}");
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
                    //OnNetworkError?.Invoke($"创建房间失败: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理创建房间响应时发生异常 - {ex.Message}");
                //OnNetworkError?.Invoke($"处理创建房间响应失败: {ex.Message}");
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
                    //OnNetworkError?.Invoke($"加入房间失败: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理加入房间响应时发生异常 - {ex.Message}");
                //OnNetworkError?.Invoke($"处理加入房间响应失败: {ex.Message}");
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
                    //OnNetworkError?.Invoke($"离开房间失败: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理离开房间响应时发生异常 - {ex.Message}");
                //OnNetworkError?.Invoke($"处理离开房间响应失败: {ex.Message}");
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
                    //OnNetworkError?.Invoke($"获取房间列表失败: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理获取房间列表响应时发生异常 - {ex.Message}");
                //OnNetworkError?.Invoke($"获取房间列表失败: {ex.Message}");
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


        #region 网络消息响应处理

        #endregion

        /// <summary>
        /// 启动单机游戏
        /// </summary>
        /// <param name="gameSceneName">游戏场景名称</param>
        public void StartSinglePlayerGame(string gameSceneName)
        {
            ASLogger.Instance.Info("GameLauncher: 启动游戏");

            try
            {
                // 1. 创建Room (这里模拟逻辑层的Room)
                var room = CreateRoom();
                var world = new World();
                room.MainWorld = world;
                room.Initialize();
                // 2. 创建Stage
                Stage gameStage = CreateStage(room);

                SwitchToGameScene(gameSceneName, gameStage);
                CreatePlayer(gameStage, room);

                MainRoom = room;
                MainStage = gameStage;
                ASLogger.Instance.Info("GameLauncher: 游戏启动成功");
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"GameLauncher: 启动游戏失败 - {ex.Message}");
                throw;
            }
        }

        public void DealNetFrameInputs(OneFrameInputs frameInputs, int frame = -1)
        {
            if (MainRoom == null)
            {
                ASLogger.Instance.Warning("GamePlayManager: 当前没有Room");
                return;
            }
            
            try
            {
                // 将帧输入数据传递给房间的帧同步控制器
                if (MainRoom.LSController != null)
                {
                    // 更新权威帧
                    if (frame > 0)
                    {
                        MainRoom.LSController.AuthorityFrame = frame;
                    }
                    else
                    {
                        ++MainRoom.LSController.AuthorityFrame;
                    }
                    
                    MainRoom.LSController.SetOneFrameInputs(frameInputs);

                    
                    ASLogger.Instance.Debug($"GamePlayManager: 处理网络帧输入，帧: {MainRoom.LSController.AuthorityFrame}，输入数: {frameInputs.Inputs.Count}");
                }
                else
                {
                    ASLogger.Instance.Warning("GamePlayManager: LSController 不存在，无法处理帧输入");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理网络帧输入时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }


        /// <summary>
        /// 创建Room - 模拟逻辑层的Room创建
        /// </summary>
        private Room CreateRoom()
        {
            // 这里只是模拟创建Room，实际的Room逻辑在逻辑层
            var room = new Room(1, "SinglePlayerRoom");
            ASLogger.Instance.Info($"GameLauncher: 创建Room，ID: {room.RoomId}");
            return room;
        }

        /// <summary>
        /// 创建Stage
        /// </summary>
        private Stage CreateStage(Room room)
        {
            ASLogger.Instance.Info("GameLauncher: 创建Stage");

            // 创建游戏Stage
            Stage gameStage = new Stage("GameStage", "游戏场景");

            // 初始化Stage
            gameStage.Initialize();

            // 设置Room ID
            gameStage.SetRoom(room);

            return gameStage;
        }

        private void RequestCreatePlayer()
        {
            var request = SingleInput.Create();
            request.FrameID = MainRoom.LSController.PredictionFrame;
            request.Input = new LSInput();//eventData.Input;
            request.Input.BornInfo = UserManager.Instance.UserId.GetHashCode();
            NetworkManager.Instance.Send(request);
            ASLogger.Instance.Debug($"GamePlayManager: 发送生成玩家请求，输入: {JsonConvert.SerializeObject(request.Input)}");
        }
        
        /// <summary>
        /// 创建Player
        /// </summary>
        private void CreatePlayer(Stage gameStage, Room room,bool isMainPlayer = true)
        {
            ASLogger.Instance.Info("GameLauncher: 创建Player并加入Stage");


            var playerID = room.AddPlayer();
            if (isMainPlayer)
            {
                PlayerId = playerID;
            }
            //Vector3 playerPosition = new Vector3(-5f, 0.5f, 0f);


            ASLogger.Instance.Info($"GameLauncher: Player创建完成，ID: {PlayerId}");
        }

        /// <summary>
        /// 切换到游戏场景
        /// </summary>
        private void SwitchToGameScene(string gameSceneName, Stage gameStage)
        {
            ASLogger.Instance.Info($"GameLauncher: 切换到游戏场景 {gameSceneName}");

            var sceneManager = GameApplication.Instance?.SceneManager;
            if (sceneManager == null)
            {
                throw new System.Exception("SceneManager不存在");
            }

            // 设置当前Stage
            StageManager.Instance.SetCurrentStage(gameStage);

            // 异步加载游戏场景
            sceneManager.LoadSceneAsync(gameSceneName, () => { OnGameSceneLoaded(gameStage); });
        }

        /// <summary>
        /// 游戏场景加载完成回调
        /// </summary>
        private void OnGameSceneLoaded(Stage gameStage)
        {
            ASLogger.Instance.Info("GameLauncher: 游戏场景加载完成");

            // 激活Stage
            gameStage.SetActive(true);
            gameStage.OnEnter();
            RequestCreatePlayer();
            ASLogger.Instance.Info("GameLauncher: 游戏准备完成");
        }

        #region 游戏开始和结束事件处理器

        /// <summary>
        /// 处理游戏响应
        /// </summary>
        private void OnGameResponse(GameResponse response)
        {
            try
            {
                ASLogger.Instance.Info(
                    $"GamePlayManager: 收到游戏响应 - Success: {response.success}, Message: {response.message}");

                if (response.success)
                {
                    // 游戏操作成功
                    ASLogger.Instance.Info($"游戏操作成功: {response.message}");
                }
                else
                {
                    // 游戏操作失败
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
        private void OnGameStartNotification(GameStartNotification notification)
        {
            try
            {
                ASLogger.Instance.Info($"GamePlayManager: 收到游戏开始通知 - 房间: {notification.roomId}");

                // 创建游戏Room和Stage
                var room = CreateGameRoom(notification);
                var world = new World();
                room.MainWorld = world;
                room.Initialize();
                var stage = CreateGameStage(room);

                // 设置当前游戏状态
                MainRoom = room;
                MainStage = stage;

                // 切换到游戏场景
                SwitchToGameScene("Game", stage);
                //CreatePlayer(stage, room);
                //RequestCreatePlayer();

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
        private void OnGameEndNotification(GameEndNotification notification)
        {
            try
            {
                ASLogger.Instance.Info(
                    $"GamePlayManager: 收到游戏结束通知 - 房间: {notification.roomId}, 原因: {notification.reason}");

                // 显示游戏结果
                ShowGameResult(notification.result);

                // 清理游戏状态
                MainRoom = null;
                MainStage = null;

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
        private void OnGameStateUpdate(GameStateUpdate update)
        {
            try
            {
                ASLogger.Instance.Info(
                    $"GamePlayManager: 收到游戏状态更新 - 房间: {update.roomId}, 回合: {update.roomState.currentRound}");

                // 更新游戏状态
                if (MainRoom != null)
                {
                    // 这里可以更新游戏状态，比如回合数、时间等
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

                // 这里可以显示游戏结果UI
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
                // 这里可以切换回房间列表UI
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

        #region 帧同步消息处理器

        /// <summary>
        /// 处理帧同步开始通知
        /// </summary>
        private void OnFrameSyncStartNotification(FrameSyncStartNotification notification)
        {
            try
            {
                ASLogger.Instance.Info($"GamePlayManager: 收到帧同步开始通知 - 房间: {notification.roomId}，帧率: {notification.frameRate}FPS", "FrameSync.Client");
                
                // 初始化帧同步相关状态
                if (MainRoom?.LSController != null)
                {
                    MainRoom.LSController.Start();
                    ASLogger.Instance.Info($"帧同步控制器已启动，房间: {notification.roomId}", "FrameSync.Client");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理帧同步开始通知时出错: {ex.Message}", "FrameSync.Client");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 处理帧同步结束通知
        /// </summary>
        private void OnFrameSyncEndNotification(FrameSyncEndNotification notification)
        {
            try
            {
                ASLogger.Instance.Info($"GamePlayManager: 收到帧同步结束通知 - 房间: {notification.roomId}，最终帧: {notification.finalFrame}，原因: {notification.reason}", "FrameSync.Client");
                
                // 停止帧同步相关状态
                if (MainRoom?.LSController != null)
                {
                    MainRoom.LSController.Stop();
                    ASLogger.Instance.Info($"帧同步控制器已停止，房间: {notification.roomId}", "FrameSync.Client");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理帧同步结束通知时出错: {ex.Message}", "FrameSync.Client");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 处理帧同步数据
        /// </summary>
        private void OnFrameSyncData(FrameSyncData frameData)
        {
            try
            {
                ASLogger.Instance.Debug($"GamePlayManager: 收到帧同步数据 - 房间: {frameData.roomId}，权威帧: {frameData.authorityFrame}，输入数: {frameData.frameInputs.Inputs.Count}", "FrameSync.Client");
                
                // 处理帧同步数据
                DealNetFrameInputs(frameData.frameInputs, frameData.authorityFrame);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理帧同步数据时出错: {ex.Message}", "FrameSync.Client");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 处理帧输入数据（兼容旧接口）
        /// </summary>
        private void OnFrameInputs(OneFrameInputs frameInputs)
        {
            try
            {
                ASLogger.Instance.Debug($"GamePlayManager: 收到帧输入数据，输入数: {frameInputs.Inputs.Count}");
                
                // 处理帧输入数据
                DealNetFrameInputs(frameInputs);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理帧输入数据时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        #endregion
    }
}