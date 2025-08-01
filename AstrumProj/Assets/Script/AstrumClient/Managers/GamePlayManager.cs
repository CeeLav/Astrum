using System;
using System.Collections;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using Astrum.View.Core;
using Astrum.View.Stages;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;


namespace Astrum.Client.Managers
{

    /// <summary>
    /// 游戏玩法管理器 - 负责管理一局游戏的内容
    /// </summary>
    public class GamePlayManager : Singleton<GamePlayManager>
    {
        
        public Stage MainStage { get; private set; }
        public Room MainRoom { get; private set; }
        
        public long PlayerId { get; private set; } = -1;
        
        // 网络游戏相关
        public UserInfo CurrentUser { get; private set; }
        public List<RoomInfo> AvailableRooms { get; private set; } = new List<RoomInfo>();
        public List<UserInfo> OnlineUsers { get; private set; } = new List<UserInfo>();
        
        // 网络游戏状态
        public bool IsLoggedIn => CurrentUser != null;
        public bool IsInRoom => CurrentUser?.CurrentRoomId != null;
        
        // 网络游戏事件
        public event Action<UserInfo> OnUserLoggedIn;
        public event Action OnUserLoggedOut;
        public event Action<RoomInfo> OnRoomCreated;
        public event Action<RoomInfo> OnRoomJoined;
        public event Action OnRoomLeft;
        public event Action<List<RoomInfo>> OnRoomListUpdated;
        public event Action<List<UserInfo>> OnOnlineUsersUpdated;
        public event Action<string> OnNetworkError;
        public void Initialize()
        {
            // 注册网络消息处理器
            RegisterNetworkMessageHandlers();
            
            ASLogger.Instance.Info("GamePlayManager: 初始化完成");
        }
        
        /// <summary>
        /// 注册网络消息处理器
        /// </summary>
        private void RegisterNetworkMessageHandlers()
        {
            var networkManager = GameApplication.Instance?.NetworkManager;
            if (networkManager != null)
            {
                // 这里需要根据NetworkManager的实际接口来注册处理器
                // 暂时先注释掉，等NetworkManager接口完善后再实现
                // networkManager.RegisterMessageHandler<NetworkMessage>(OnNetworkMessageReceived);
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
            
            //throw new NotImplementedException();
        }
        
        public void Shutdown()
        {
            // 清理网络游戏状态
            CurrentUser = null;
            AvailableRooms.Clear();
            OnlineUsers.Clear();
            
            ASLogger.Instance.Info("GamePlayManager: 已关闭");
        }
        
        #region 网络游戏接口
        
        /// <summary>
        /// 自动登录（使用默认显示名称）
        /// </summary>
        public void AutoLogin()
        {
            if (IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 用户已登录");
                return;
            }
            
            ASLogger.Instance.Info("GamePlayManager: 开始自动登录");
            
            var loginRequest = new LoginRequest { DisplayName = "" };
            var message = GameNetworkMessage.CreateSuccess("login", loginRequest);
            
            SendNetworkMessage(message);
        }
        
        /// <summary>
        /// 使用指定显示名称登录
        /// </summary>
        /// <param name="displayName">显示名称</param>
        public void Login(string displayName)
        {
            if (IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 用户已登录");
                return;
            }
            
            ASLogger.Instance.Info($"GamePlayManager: 开始登录，显示名称: {displayName}");
            
            var loginRequest = new LoginRequest { DisplayName = displayName };
            var message = GameNetworkMessage.CreateSuccess("login", loginRequest);
            
            SendNetworkMessage(message);
        }
        
        /// <summary>
        /// 登出
        /// </summary>
        public void Logout()
        {
            if (!IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 用户未登录");
                return;
            }
            
            ASLogger.Instance.Info("GamePlayManager: 开始登出");
            
            CurrentUser = null;
            OnUserLoggedOut?.Invoke();
        }
        
        /// <summary>
        /// 创建房间
        /// </summary>
        /// <param name="roomName">房间名称</param>
        /// <param name="maxPlayers">最大玩家数</param>
        public void CreateRoom(string roomName = null, int maxPlayers = 4)
        {
            if (!IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 请先登录");
                OnNetworkError?.Invoke("请先登录");
                return;
            }
            
            if (IsInRoom)
            {
                ASLogger.Instance.Warning("GamePlayManager: 已在房间中");
                OnNetworkError?.Invoke("已在房间中");
                return;
            }
            
            if (string.IsNullOrEmpty(roomName))
            {
                roomName = $"房间_{DateTime.Now:HHmmss}";
            }
            
            ASLogger.Instance.Info($"GamePlayManager: 创建房间，名称: {roomName}, 最大玩家: {maxPlayers}");
            
            var createRoomRequest = new CreateRoomRequest 
            { 
                RoomName = roomName, 
                MaxPlayers = maxPlayers 
            };
            var message = GameNetworkMessage.CreateSuccess("create_room", createRoomRequest);
            
            SendNetworkMessage(message);
        }
        
        /// <summary>
        /// 加入房间
        /// </summary>
        /// <param name="roomId">房间ID</param>
        public void JoinRoom(string roomId)
        {
            if (!IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 请先登录");
                OnNetworkError?.Invoke("请先登录");
                return;
            }
            
            if (IsInRoom)
            {
                ASLogger.Instance.Warning("GamePlayManager: 已在房间中");
                OnNetworkError?.Invoke("已在房间中");
                return;
            }
            
            if (string.IsNullOrEmpty(roomId))
            {
                ASLogger.Instance.Warning("GamePlayManager: 房间ID不能为空");
                OnNetworkError?.Invoke("房间ID不能为空");
                return;
            }
            
            ASLogger.Instance.Info($"GamePlayManager: 加入房间，ID: {roomId}");
            
            var joinRoomRequest = new JoinRoomRequest { RoomId = roomId };
            var message = GameNetworkMessage.CreateSuccess("join_room", joinRoomRequest);
            
            SendNetworkMessage(message);
        }
        
        /// <summary>
        /// 离开房间
        /// </summary>
        public void LeaveRoom()
        {
            if (!IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 请先登录");
                OnNetworkError?.Invoke("请先登录");
                return;
            }
            
            if (!IsInRoom)
            {
                ASLogger.Instance.Warning("GamePlayManager: 当前不在房间中");
                OnNetworkError?.Invoke("当前不在房间中");
                return;
            }
            
            ASLogger.Instance.Info("GamePlayManager: 离开房间");
            
            var message = GameNetworkMessage.CreateSuccess("leave_room", null);
            SendNetworkMessage(message);
        }
        
        /// <summary>
        /// 获取房间列表
        /// </summary>
        public void GetRooms()
        {
            if (!IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 请先登录");
                OnNetworkError?.Invoke("请先登录");
                return;
            }
            
            ASLogger.Instance.Info("GamePlayManager: 获取房间列表");
            
            var message = GameNetworkMessage.CreateSuccess("get_rooms", null);
            SendNetworkMessage(message);
        }
        
        /// <summary>
        /// 获取在线用户列表
        /// </summary>
        public void GetOnlineUsers()
        {
            if (!IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 请先登录");
                OnNetworkError?.Invoke("请先登录");
                return;
            }
            
            ASLogger.Instance.Info("GamePlayManager: 获取在线用户列表");
            
            var message = GameNetworkMessage.CreateSuccess("get_online_users", null);
            SendNetworkMessage(message);
        }
        
        /// <summary>
        /// 发送网络消息
        /// </summary>
        /// <param name="message">网络消息</param>
        private async void SendNetworkMessage(GameNetworkMessage message)
        {
            var networkManager = GameApplication.Instance?.NetworkManager;
            if (networkManager == null)
            {
                ASLogger.Instance.Error("GamePlayManager: NetworkManager不存在");
                OnNetworkError?.Invoke("网络管理器不存在");
                return;
            }
            
            if (!networkManager.IsConnected)
            {
                ASLogger.Instance.Error("GamePlayManager: 网络未连接");
                OnNetworkError?.Invoke("网络未连接");
                return;
            }
            
            try
            {
                // 将GameNetworkMessage转换为NetworkMessage
                var networkMessage = new NetworkMessage(message.Type, message.Data)
                {
                    Success = message.Success,
                    Error = message.Error,
                    Timestamp = message.Timestamp
                };
                
                ASLogger.Instance.Info($"GamePlayManager: 发送消息 - {message.Type}");
                bool success = await networkManager.SendMessageAsync(networkMessage);
                
                if (!success)
                {
                    ASLogger.Instance.Error("GamePlayManager: 发送消息失败");
                    OnNetworkError?.Invoke("发送消息失败");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 发送消息异常 - {ex.Message}");
                OnNetworkError?.Invoke($"发送消息异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理接收到的网络消息
        /// </summary>
        /// <param name="message">网络消息</param>
        public void OnNetworkMessageReceived(GameNetworkMessage message)
        {
            ASLogger.Instance.Info($"GamePlayManager: 收到消息 - {message.Type}, 成功: {message.Success}");
            
            if (!message.Success)
            {
                ASLogger.Instance.Error($"GamePlayManager: 消息处理失败 - {message.Error}");
                OnNetworkError?.Invoke(message.Error);
                return;
            }
            
            switch (message.Type)
            {
                case "login":
                    HandleLoginResponse(message);
                    break;
                case "create_room":
                    HandleCreateRoomResponse(message);
                    break;
                case "join_room":
                    HandleJoinRoomResponse(message);
                    break;
                case "leave_room":
                    HandleLeaveRoomResponse(message);
                    break;
                case "get_rooms":
                    HandleGetRoomsResponse(message);
                    break;
                case "get_online_users":
                    HandleGetOnlineUsersResponse(message);
                    break;
                default:
                    ASLogger.Instance.Warning($"GamePlayManager: 未知消息类型 - {message.Type}");
                    break;
            }
        }
        
        #endregion
        
        #region 网络消息响应处理
        
        /// <summary>
        /// 处理登录响应
        /// </summary>
        /// <param name="message">网络消息</param>
        private void HandleLoginResponse(GameNetworkMessage message)
        {
            try
            {
                if (message.Data != null)
                {
                    var userInfoJson = JsonUtility.ToJson(message.Data);
                    CurrentUser = JsonUtility.FromJson<UserInfo>(userInfoJson);
                    
                    ASLogger.Instance.Info($"GamePlayManager: 登录成功，用户: {CurrentUser?.DisplayName}");
                    OnUserLoggedIn?.Invoke(CurrentUser);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理登录响应失败 - {ex.Message}");
                OnNetworkError?.Invoke("处理登录响应失败");
            }
        }
        
        /// <summary>
        /// 处理创建房间响应
        /// </summary>
        /// <param name="message">网络消息</param>
        private void HandleCreateRoomResponse(GameNetworkMessage message)
        {
            try
            {
                if (message.Data != null)
                {
                    var roomInfoJson = JsonConvert.SerializeObject(message.Data);
                    var roomInfo = JsonConvert.DeserializeObject<RoomInfo>(roomInfoJson);
                    
                    // 更新当前用户的房间信息
                    if (CurrentUser != null)
                    {
                        CurrentUser.CurrentRoomId = roomInfo?.Id;
                    }
                    
                    ASLogger.Instance.Info($"GamePlayManager: 房间创建成功，ID: {roomInfo?.Id}, 名称: {roomInfo?.Name}");
                    OnRoomCreated?.Invoke(roomInfo);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理创建房间响应失败 - {ex.Message}");
                OnNetworkError?.Invoke("处理创建房间响应失败");
            }
        }
        
        /// <summary>
        /// 处理加入房间响应
        /// </summary>
        /// <param name="message">网络消息</param>
        private void HandleJoinRoomResponse(GameNetworkMessage message)
        {
            try
            {
                if (message.Data != null)
                {
                    var roomInfoJson = JsonConvert.SerializeObject(message.Data);
                    var roomInfo = JsonConvert.DeserializeObject<RoomInfo>(roomInfoJson);
                    
                    // 更新当前用户的房间信息
                    if (CurrentUser != null)
                    {
                        CurrentUser.CurrentRoomId = roomInfo?.Id;
                    }
                    
                    ASLogger.Instance.Info($"GamePlayManager: 成功加入房间，名称: {roomInfo?.Name}");
                    OnRoomJoined?.Invoke(roomInfo);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理加入房间响应失败 - {ex.Message}");
                OnNetworkError?.Invoke("处理加入房间响应失败");
            }
        }
        
        /// <summary>
        /// 处理离开房间响应
        /// </summary>
        /// <param name="message">网络消息</param>
        private void HandleLeaveRoomResponse(GameNetworkMessage message)
        {
            // 更新当前用户的房间信息
            if (CurrentUser != null)
            {
                CurrentUser.CurrentRoomId = null;
            }
            
            ASLogger.Instance.Info("GamePlayManager: 已离开房间");
            OnRoomLeft?.Invoke();
        }
        
        /// <summary>
        /// 处理获取房间列表响应
        /// </summary>
        /// <param name="message">网络消息</param>
        private void HandleGetRoomsResponse(GameNetworkMessage message)
        {
            try
            {
                if (message.Data != null)
                {
                    var roomsJson = JsonConvert.SerializeObject(message.Data);
                    AvailableRooms = JsonConvert.DeserializeObject<List<RoomInfo>>(roomsJson) ?? new List<RoomInfo>();
                    
                    ASLogger.Instance.Info($"GamePlayManager: 获取房间列表成功，房间数量: {AvailableRooms.Count}");
                    OnRoomListUpdated?.Invoke(AvailableRooms);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理获取房间列表响应失败 - {ex.Message}");
                OnNetworkError?.Invoke("处理获取房间列表响应失败");
            }
        }
        
        /// <summary>
        /// 处理获取在线用户列表响应
        /// </summary>
        /// <param name="message">网络消息</param>
        private void HandleGetOnlineUsersResponse(GameNetworkMessage message)
        {
            try
            {
                if (message.Data != null)
                {
                    var usersJson = JsonConvert.SerializeObject(message.Data);
                    OnlineUsers = JsonConvert.DeserializeObject<List<UserInfo>>(usersJson) ?? new List<UserInfo>();
                    
                    ASLogger.Instance.Info($"GamePlayManager: 获取在线用户列表成功，用户数量: {OnlineUsers.Count}");
                    OnOnlineUsersUpdated?.Invoke(OnlineUsers);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理获取在线用户列表响应失败 - {ex.Message}");
                OnNetworkError?.Invoke("处理获取在线用户列表响应失败");
            }
        }
        
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
                room.AddWorld(world);
                room.Initialize();
                // 2. 创建Stage
                Stage gameStage = CreateStage(room);
                
                SwitchToGameScene(gameSceneName, gameStage);
                CreatePlayerAndJoin(gameStage, room);
                
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
        
        public void StartMultiPlayerGame(string gameSceneName)
        {
            ASLogger.Instance.Info("GameLauncher: 启动多人游戏");
            
            try
            {
                // 1. 创建Room
                var room = CreateRoom();
                var world = new World();
                room.AddWorld(world);
                room.Initialize();
                // 2. 创建Stage
                Stage gameStage = CreateStage(room);
                
                SwitchToGameScene(gameSceneName, gameStage);
                CreatePlayerAndJoin(gameStage, room);
                
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
        
        public void DealNetFrameInputs(OneFrameInputs frameInputs)
        {
            if (MainRoom == null)
            {
                ASLogger.Instance.Warning("GamePlayManager: 当前没有Room");
                return;
            }
            MainRoom?.LSController?.DealNetFrameInputs(frameInputs);
            
            ASLogger.Instance.Info("GamePlayManager: 处理网络帧输入");
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
        
        /// <summary>
        /// 创建Player并加入Stage
        /// </summary>
        private void CreatePlayerAndJoin(Stage gameStage, Room room)
        {
            ASLogger.Instance.Info("GameLauncher: 创建Player并加入Stage");
            
            
            PlayerId = room.AddPlayer();
            Vector3 playerPosition = new Vector3(-5f, 0.5f, 0f);

            
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
            sceneManager.LoadSceneAsync(gameSceneName, () =>
            {
                OnGameSceneLoaded(gameStage);
            });
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
            
            ASLogger.Instance.Info("GameLauncher: 游戏准备完成");
        }
    }
    
    /// <summary>
    /// 网络消息 - 用于与服务器通信
    /// </summary>
    [Serializable]
    public class GameNetworkMessage
    {
        public string Type { get; set; } = string.Empty;
        public object Data { get; set; }
        public string Error { get; set; }
        public bool Success { get; set; } = true;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public static GameNetworkMessage CreateSuccess(string type, object data = null)
        {
            return new GameNetworkMessage { Type = type, Data = data, Success = true };
        }
        
        public static GameNetworkMessage CreateError(string type, string error)
        {
            return new GameNetworkMessage { Type = type, Error = error, Success = false };
        }
    }
    

    

    
} 