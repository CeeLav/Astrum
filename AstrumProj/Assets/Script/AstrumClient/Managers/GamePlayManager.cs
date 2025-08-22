using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.Generated;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using Astrum.View.Core;
using Astrum.View.Stages;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Astrum.Network.Generated;
using MemoryPack;


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
        
        // 网络游戏相关 - 使用专门的管理器
        public UserInfo CurrentUser => UserManager.Instance?.CurrentUser;
        public List<RoomInfo> AvailableRooms => RoomSystemManager.Instance?.AvailableRooms ?? new List<RoomInfo>();
        public List<UserInfo> OnlineUsers { get; private set; } = new List<UserInfo>();
        
        // 网络游戏状态
        public bool IsLoggedIn => UserManager.Instance?.IsLoggedIn ?? false;
        public bool IsInRoom => RoomSystemManager.Instance?.IsInRoom ?? false;
        
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
            // 初始化用户管理器和房间系统管理器
            UserManager.Instance?.Initialize();
            RoomSystemManager.Instance?.Initialize();
            
            // 注册网络消息处理器
            RegisterNetworkMessageHandlers();
            
            // 注册管理器事件
            RegisterManagerEvents();
            
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
                // 注册具体消息类型的事件处理器
                networkManager.OnLoginResponse += OnLoginResponse;
                networkManager.OnCreateRoomResponse += OnCreateRoomResponse;
                networkManager.OnJoinRoomResponse += OnJoinRoomResponse;
                networkManager.OnLeaveRoomResponse += OnLeaveRoomResponse;
                networkManager.OnGetRoomListResponse += OnGetRoomListResponse;
                networkManager.OnRoomUpdateNotification += OnRoomUpdateNotification;
                networkManager.OnHeartbeatResponse += OnHeartbeatResponse;
                
                ASLogger.Instance.Info("GamePlayManager: 网络消息处理器注册完成");
            }
            else
            {
                ASLogger.Instance.Warning("GamePlayManager: NetworkManager 不存在，无法注册消息处理器");
            }
        }
        
        /// <summary>
        /// 注册管理器事件
        /// </summary>
        private void RegisterManagerEvents()
        {
            // 注册用户管理器事件
            if (UserManager.Instance != null)
            {
                UserManager.Instance.OnUserLoggedIn += OnUserLoggedIn;
                UserManager.Instance.OnUserLoggedOut += OnUserLoggedOut;
                UserManager.Instance.OnLoginError += OnNetworkError;
            }
            
            // 注册房间系统管理器事件
            if (RoomSystemManager.Instance != null)
            {
                RoomSystemManager.Instance.OnRoomCreated += OnRoomCreated;
                RoomSystemManager.Instance.OnRoomJoined += OnRoomJoined;
                RoomSystemManager.Instance.OnRoomLeft += OnRoomLeft;
                RoomSystemManager.Instance.OnRoomListUpdated += OnRoomListUpdated;
                RoomSystemManager.Instance.OnRoomError += OnNetworkError;
            }
            
            ASLogger.Instance.Info("GamePlayManager: 管理器事件注册完成");
        }
        
        /// <summary>
        /// 取消注册管理器事件
        /// </summary>
        private void UnregisterManagerEvents()
        {
            // 取消注册用户管理器事件
            if (UserManager.Instance != null)
            {
                UserManager.Instance.OnUserLoggedIn -= OnUserLoggedIn;
                UserManager.Instance.OnUserLoggedOut -= OnUserLoggedOut;
                UserManager.Instance.OnLoginError -= OnNetworkError;
            }
            
            // 取消注册房间系统管理器事件
            if (RoomSystemManager.Instance != null)
            {
                RoomSystemManager.Instance.OnRoomCreated -= OnRoomCreated;
                RoomSystemManager.Instance.OnRoomJoined -= OnRoomJoined;
                RoomSystemManager.Instance.OnRoomLeft -= OnRoomLeft;
                RoomSystemManager.Instance.OnRoomListUpdated -= OnRoomListUpdated;
                RoomSystemManager.Instance.OnRoomError -= OnNetworkError;
            }
            
            ASLogger.Instance.Info("GamePlayManager: 管理器事件取消注册完成");
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
            
            //throw new NotImplementedException();
        }
        
        public void Shutdown()
        {
            // 取消注册网络消息处理器
            UnregisterNetworkMessageHandlers();
            
            // 清理管理器事件
            UnregisterManagerEvents();
            
            // 清理网络游戏状态
            OnlineUsers.Clear();
            
            ASLogger.Instance.Info("GamePlayManager: 已关闭");
        }
        
        //#region 网络游戏接口
        
        /// <summary>
        /// 自动登录（连接成功后自动调用）
        /// </summary>
        public async Task<bool> AutoLoginAsync()
        {
            if (IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 用户已登录");
                return true;
            }
            
            ASLogger.Instance.Info("GamePlayManager: 开始自动登录");
            
            // 使用用户管理器进行自动登录
            if (UserManager.Instance != null)
            {
                return await UserManager.Instance.AutoLoginAsync();
            }
            
            ASLogger.Instance.Error("GamePlayManager: UserManager不存在");
            return false;
        }
        
        /// <summary>
        /// 连接并自动登录
        /// </summary>
        public async Task<bool> ConnectAndLoginAsync()
        {
            try
            {
                // 1. 检查网络管理器是否初始化
                var networkManager = GameApplication.Instance?.NetworkManager;
                if (networkManager == null)
                {
                    ASLogger.Instance.Error("GamePlayManager: NetworkManager不存在");
                    OnNetworkError?.Invoke("网络管理器不存在");
                    return false;
                }

                // 2. 检查网络连接状态
                if (!networkManager.IsConnected())
                {
                    ASLogger.Instance.Info("GamePlayManager: 网络未连接，尝试连接服务器...");
                    
                    // 触发连接状态变更事件
                    OnNetworkError?.Invoke("正在连接服务器...");
                    
                    try
                    {
                        // 尝试连接服务器
                        var channelId = await networkManager.ConnectAsync("127.0.0.1", 8888);
                        ASLogger.Instance.Info($"GamePlayManager: 连接请求已发送，ChannelId: {channelId}");
                        
                        // 等待连接完成或超时
                        var connectionResult = await WaitForConnection(networkManager, 10000); // 10秒超时
                        
                        if (!connectionResult)
                        {
                            ASLogger.Instance.Error("GamePlayManager: 连接服务器超时");
                            OnNetworkError?.Invoke("连接服务器超时，请检查服务器状态");
                            return false;
                        }
                        
                        ASLogger.Instance.Info("GamePlayManager: 成功连接到服务器");
                    }
                    catch (Exception connectEx)
                    {
                        ASLogger.Instance.Error($"GamePlayManager: 连接服务器失败 - {connectEx.Message}");
                        OnNetworkError?.Invoke($"连接服务器失败: {connectEx.Message}");
                        return false;
                    }
                }
                else
                {
                    ASLogger.Instance.Info("GamePlayManager: 网络已连接");
                }

                // 3. 验证连接状态
                if (!networkManager.IsConnected())
                {
                    ASLogger.Instance.Error("GamePlayManager: 网络连接验证失败");
                    OnNetworkError?.Invoke("网络连接验证失败");
                    return false;
                }

                // 4. 自动登录
                ASLogger.Instance.Info("GamePlayManager: 开始自动登录");
                
                // 使用用户管理器进行自动登录
                if (UserManager.Instance != null)
                {
                    return await UserManager.Instance.AutoLoginAsync();
                }
                
                ASLogger.Instance.Error("GamePlayManager: UserManager不存在");
                return false;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 登录过程发生异常 - {ex.Message}");
                OnNetworkError?.Invoke($"登录失败: {ex.Message}");
                return false;
            }
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
            
            // 使用用户管理器进行登出
            UserManager.Instance?.Logout();
        }
        
        /// <summary>
        /// 创建房间
        /// </summary>
        /// <param name="roomName">房间名称</param>
        /// <param name="maxPlayers">最大玩家数</param>
        public async Task<bool> CreateRoomAsync(string roomName = null, int maxPlayers = 4)
        {
            if (!IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 请先登录");
                OnNetworkError?.Invoke("请先登录");
                return false;
            }
            
            if (IsInRoom)
            {
                ASLogger.Instance.Warning("GamePlayManager: 已在房间中");
                OnNetworkError?.Invoke("已在房间中");
                return false;
            }
            
            if (string.IsNullOrEmpty(roomName))
            {
                roomName = $"房间_{DateTime.Now:HHmmss}";
            }
            
            ASLogger.Instance.Info($"GamePlayManager: 创建房间，名称: {roomName}, 最大玩家: {maxPlayers}");
            
            // 使用房间系统管理器创建房间
            if (RoomSystemManager.Instance != null)
            {
                return await RoomSystemManager.Instance.CreateRoomAsync(roomName, maxPlayers);
            }
            
            ASLogger.Instance.Error("GamePlayManager: RoomSystemManager不存在");
            return false;
        }
        
        /// <summary>
        /// 加入房间
        /// </summary>
        /// <param name="roomId">房间ID</param>
        public async Task<bool> JoinRoomAsync(string roomId)
        {
            if (!IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 请先登录");
                OnNetworkError?.Invoke("请先登录");
                return false;
            }
            
            if (IsInRoom)
            {
                ASLogger.Instance.Warning("GamePlayManager: 已在房间中");
                OnNetworkError?.Invoke("已在房间中");
                return false;
            }
            
            if (string.IsNullOrEmpty(roomId))
            {
                ASLogger.Instance.Warning("GamePlayManager: 房间ID不能为空");
                OnNetworkError?.Invoke("房间ID不能为空");
                return false;
            }
            
            ASLogger.Instance.Info($"GamePlayManager: 加入房间，ID: {roomId}");
            
            // 使用房间系统管理器加入房间
            if (RoomSystemManager.Instance != null)
            {
                return await RoomSystemManager.Instance.JoinRoomAsync(roomId);
            }
            
            ASLogger.Instance.Error("GamePlayManager: RoomSystemManager不存在");
            return false;
        }
        
        /// <summary>
        /// 离开房间
        /// </summary>
        public async Task<bool> LeaveRoomAsync()
        {
            if (!IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 请先登录");
                OnNetworkError?.Invoke("请先登录");
                return false;
            }
            
            if (!IsInRoom)
            {
                ASLogger.Instance.Warning("GamePlayManager: 当前不在房间中");
                OnNetworkError?.Invoke("当前不在房间中");
                return false;
            }
            
            ASLogger.Instance.Info("GamePlayManager: 离开房间");
            
            // 使用房间系统管理器离开房间
            if (RoomSystemManager.Instance != null)
            {
                return await RoomSystemManager.Instance.LeaveRoomAsync();
            }
            
            ASLogger.Instance.Error("GamePlayManager: RoomSystemManager不存在");
            return false;
        }
        
        /// <summary>
        /// 获取房间列表
        /// </summary>
        public async Task<bool> GetRoomsAsync()
        {
            if (!IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 请先登录");
                OnNetworkError?.Invoke("请先登录");
                return false;
            }
            
            ASLogger.Instance.Info("GamePlayManager: 获取房间列表");
            
            // 使用房间系统管理器获取房间列表
            if (RoomSystemManager.Instance != null)
            {
                return await RoomSystemManager.Instance.GetRoomListAsync();
            }
            
            ASLogger.Instance.Error("GamePlayManager: RoomSystemManager不存在");
            return false;
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
        public static NetworkMessage CreateSuccess(string type, object data = null)
        {
            var message = NetworkMessage.Create();
            message.Type = type;
            message.Data = data != null ? MemoryPackHelper.Serialize(data) : null;
            message.Success = true;
            message.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return message;
        }

        /// <summary>
        /// 创建错误消息
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <param name="error">错误信息</param>
        /// <returns>NetworkMessage实例</returns>
        public static NetworkMessage CreateError(string type, string error)
        {
            var message = NetworkMessage.Create();
            message.Type = type;
            message.Error = error;
            message.Success = false;
            message.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return message;
        }

        /// <summary>
        /// 创建普通消息
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <param name="data">消息数据</param>
        /// <returns>NetworkMessage实例</returns>
        public static NetworkMessage Create(string type, object data = null)
        {
            var message = NetworkMessage.Create();
            message.Type = type;
            message.Data = data != null ? MemoryPackHelper.Serialize(data) : null;
            message.Success = true;
            message.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return message;
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
            
            if (!networkManager.IsConnected())
            {
                ASLogger.Instance.Error("GamePlayManager: 网络未连接");
                OnNetworkError?.Invoke("网络未连接");
                return;
            }
            
            try
            {
                // 将GameNetworkMessage转换为NetworkMessage
                var networkMessage = Create(message.Type, message.Data);
                networkMessage.Success = message.Success;
                networkMessage.Error = message.Error;
                networkMessage.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                ASLogger.Instance.Info($"GamePlayManager: 发送消息 - {message.Type}");
                await networkManager.SendMessageAsync(networkMessage);
                
                // SendMessageAsync没有返回值，假设发送成功
                ASLogger.Instance.Info("GamePlayManager: 消息发送完成");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 发送消息异常 - {ex.Message}");
                OnNetworkError?.Invoke($"发送消息异常: {ex.Message}");
            }
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
                    OnNetworkError?.Invoke($"登录失败: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理登录响应时发生异常 - {ex.Message}");
                OnNetworkError?.Invoke($"处理登录响应失败: {ex.Message}");
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
                    OnNetworkError?.Invoke($"创建房间失败: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理创建房间响应时发生异常 - {ex.Message}");
                OnNetworkError?.Invoke($"处理创建房间响应失败: {ex.Message}");
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
                    OnNetworkError?.Invoke($"加入房间失败: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理加入房间响应时发生异常 - {ex.Message}");
                OnNetworkError?.Invoke($"处理加入房间响应失败: {ex.Message}");
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
                    OnNetworkError?.Invoke($"离开房间失败: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理离开房间响应时发生异常 - {ex.Message}");
                OnNetworkError?.Invoke($"处理离开房间响应失败: {ex.Message}");
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
                    OnNetworkError?.Invoke($"获取房间列表失败: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理获取房间列表响应时发生异常 - {ex.Message}");
                OnNetworkError?.Invoke($"获取房间列表失败: {ex.Message}");
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
        
        #region 消息处理方法
        
        // 旧的 NetworkMessage 处理方法已移除
        
        private void HandleRawMessage(NetworkMessage message)
        {
            ASLogger.Instance.Debug($"GamePlayManager: 收到原始消息，数据长度: {message.Data?.Length ?? 0}");
            // 处理原始消息，这里可能是服务器发送的其他类型数据
        }
        
        private void HandleLoginResponse(NetworkMessage message)
        {
            try
            {
                if (message.Success)
                {
                    ASLogger.Instance.Info("GamePlayManager: 收到登录成功响应");
                    
                    // 尝试解析登录响应
                    if (message.Data != null && message.Data.Length > 0)
                    {
                        try
                        {
                            // 尝试反序列化LoginResponse
                            var loginResponse = MemoryPackSerializer.Deserialize<LoginResponse>(message.Data);
                            if (loginResponse != null)
                            {
                                // 使用用户管理器处理登录响应
                                UserManager.Instance?.HandleLoginResponse(loginResponse);
                                return;
                            }
                        }
                        catch (Exception parseEx)
                        {
                            ASLogger.Instance.Warning($"GamePlayManager: 解析LoginResponse失败 - {parseEx.Message}");
                        }
                    }
                    
                    // 如果无法解析LoginResponse，记录错误
                    ASLogger.Instance.Error("GamePlayManager: 无法解析登录响应");
                    OnNetworkError?.Invoke("无法解析登录响应");
                    return;
                }
                else
                {
                    // 登录失败
                    var errorMessage = !string.IsNullOrEmpty(message.Error) ? message.Error : "未知错误";
                    ASLogger.Instance.Error($"GamePlayManager: 登录失败 - {errorMessage}");
                    OnNetworkError?.Invoke($"登录失败: {errorMessage}");
                    
                    // 清理用户状态 - 使用用户管理器
                    UserManager.Instance?.Logout();
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理登录响应时发生异常 - {ex.Message}");
                OnNetworkError?.Invoke($"处理登录响应失败: {ex.Message}");
                
                // 清理用户状态 - 使用用户管理器
                UserManager.Instance?.Logout();
            }
        }
        
        private void HandleCreateRoomResponse(NetworkMessage message)
        {
            try
            {
                if (message.Success)
                {
                    ASLogger.Instance.Info("GamePlayManager: 收到创建房间成功响应");
                    
                    // 尝试解析创建房间响应
                    if (message.Data != null && message.Data.Length > 0)
                    {
                        try
                        {
                            // 尝试反序列化CreateRoomResponse
                            var createRoomResponse = MemoryPackSerializer.Deserialize<CreateRoomResponse>(message.Data);
                            if (createRoomResponse != null)
                            {
                                // 使用房间系统管理器处理创建房间响应
                                RoomSystemManager.Instance?.HandleCreateRoomResponse(createRoomResponse);
                                return;
                            }
                        }
                        catch (Exception parseEx)
                        {
                            ASLogger.Instance.Warning($"GamePlayManager: 解析CreateRoomResponse失败 - {parseEx.Message}");
                        }
                    }
                    
                    // 如果无法解析CreateRoomResponse，记录错误
                    ASLogger.Instance.Error("GamePlayManager: 无法解析创建房间响应");
                    OnNetworkError?.Invoke("无法解析创建房间响应");
                }
                else
                {
                    // 创建房间失败
                    var errorMessage = !string.IsNullOrEmpty(message.Error) ? message.Error : "未知错误";
                    ASLogger.Instance.Error($"GamePlayManager: 创建房间失败 - {errorMessage}");
                    OnNetworkError?.Invoke($"创建房间失败: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理创建房间响应时发生异常 - {ex.Message}");
                OnNetworkError?.Invoke($"处理创建房间响应失败: {ex.Message}");
            }
        }
        
        private void HandleJoinRoomResponse(NetworkMessage message)
        {
            try
            {
                if (message.Success)
                {
                    ASLogger.Instance.Info("GamePlayManager: 收到加入房间成功响应");
                    
                    // 尝试解析加入房间响应
                    if (message.Data != null && message.Data.Length > 0)
                    {
                        try
                        {
                            // 尝试反序列化JoinRoomResponse
                            var joinRoomResponse = MemoryPackSerializer.Deserialize<JoinRoomResponse>(message.Data);
                            if (joinRoomResponse != null)
                            {
                                // 使用房间系统管理器处理加入房间响应
                                RoomSystemManager.Instance?.HandleJoinRoomResponse(joinRoomResponse);
                                return;
                            }
                        }
                        catch (Exception parseEx)
                        {
                            ASLogger.Instance.Warning($"GamePlayManager: 解析JoinRoomResponse失败 - {parseEx.Message}");
                        }
                    }
                    
                    // 如果无法解析JoinRoomResponse，记录错误
                    ASLogger.Instance.Error("GamePlayManager: 无法解析加入房间响应");
                    OnNetworkError?.Invoke("无法解析加入房间响应");
                }
                else
                {
                    // 加入房间失败
                    var errorMessage = !string.IsNullOrEmpty(message.Error) ? message.Error : "未知错误";
                    ASLogger.Instance.Error($"GamePlayManager: 加入房间失败 - {errorMessage}");
                    OnNetworkError?.Invoke($"加入房间失败: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理加入房间响应时发生异常 - {ex.Message}");
                OnNetworkError?.Invoke($"处理加入房间响应失败: {ex.Message}");
            }
        }
        
        private void HandleLeaveRoomResponse(NetworkMessage message)
        {
            try
            {
                if (message.Success)
                {
                    ASLogger.Instance.Info("GamePlayManager: 收到离开房间成功响应");
                    
                    // 尝试解析离开房间响应
                    if (message.Data != null && message.Data.Length > 0)
                    {
                        try
                        {
                            // 尝试反序列化LeaveRoomResponse
                            var leaveRoomResponse = MemoryPackSerializer.Deserialize<LeaveRoomResponse>(message.Data);
                            if (leaveRoomResponse != null)
                            {
                                // 使用房间系统管理器处理离开房间响应
                                RoomSystemManager.Instance?.HandleLeaveRoomResponse(leaveRoomResponse);
                                return;
                            }
                        }
                        catch (Exception parseEx)
                        {
                            ASLogger.Instance.Warning($"GamePlayManager: 解析LeaveRoomResponse失败 - {parseEx.Message}");
                        }
                    }
                    
                    // 如果无法解析LeaveRoomResponse，直接触发离开房间事件
                    ASLogger.Instance.Info("GamePlayManager: 离开房间成功");
                    OnRoomLeft?.Invoke();
                }
                else
                {
                    // 离开房间失败
                    var errorMessage = !string.IsNullOrEmpty(message.Error) ? message.Error : "未知错误";
                    ASLogger.Instance.Error($"GamePlayManager: 离开房间失败 - {errorMessage}");
                    OnNetworkError?.Invoke($"离开房间失败: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理离开房间响应时发生异常 - {ex.Message}");
                OnNetworkError?.Invoke($"处理离开房间响应失败: {ex.Message}");
            }
        }
        
        private void HandleGetRoomsResponse(NetworkMessage message)
        {
            try
            {
                if (message.Success)
                {
                    ASLogger.Instance.Info("GamePlayManager: 收到获取房间列表成功响应");
                    
                    // 尝试解析获取房间列表响应
                    if (message.Data != null && message.Data.Length > 0)
                    {
                        try
                        {
                            // 尝试反序列化GetRoomListResponse
                            var getRoomListResponse = MemoryPackSerializer.Deserialize<GetRoomListResponse>(message.Data);
                            if (getRoomListResponse != null)
                            {
                                // 使用房间系统管理器处理获取房间列表响应
                                RoomSystemManager.Instance?.HandleRoomListResponse(getRoomListResponse);
                                return;
                            }
                        }
                        catch (Exception parseEx)
                        {
                            ASLogger.Instance.Warning($"GamePlayManager: 解析GetRoomListResponse失败 - {parseEx.Message}");
                        }
                    }
                    
                    // 如果无法解析GetRoomListResponse，记录错误
                    ASLogger.Instance.Error("GamePlayManager: 无法解析获取房间列表响应");
                    OnNetworkError?.Invoke("无法解析获取房间列表响应");
                }
                else
                {
                    // 获取房间列表失败
                    var errorMessage = !string.IsNullOrEmpty(message.Error) ? message.Error : "未知错误";
                    ASLogger.Instance.Error($"GamePlayManager: 获取房间列表失败 - {errorMessage}");
                    OnNetworkError?.Invoke($"获取房间列表失败: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理获取房间列表响应时发生异常 - {ex.Message}");
                OnNetworkError?.Invoke($"处理获取房间列表响应失败: {ex.Message}");
            }
        }
        
        private void HandleGetOnlineUsersResponse(NetworkMessage message)
        {
            if (message.Success)
            {
                ASLogger.Instance.Info("GamePlayManager: 获取在线用户列表成功");
                // 这里可以解析用户列表
                // OnOnlineUsersUpdated?.Invoke(users);
            }
            else
            {
                ASLogger.Instance.Error($"GamePlayManager: 获取在线用户列表失败 - {message.Error}");
                OnNetworkError?.Invoke(message.Error);
            }
        }
        
        private void HandleRoomUpdateNotification(NetworkMessage message)
        {
            try
            {
                if (message.Success)
                {
                    ASLogger.Instance.Info("GamePlayManager: 收到房间更新通知");
                    
                    // 尝试解析房间更新通知
                    if (message.Data != null && message.Data.Length > 0)
                    {
                        try
                        {
                            // 尝试反序列化RoomUpdateNotification
                            var roomUpdateNotification = MemoryPackSerializer.Deserialize<RoomUpdateNotification>(message.Data);
                            if (roomUpdateNotification != null)
                            {
                                // 使用房间系统管理器处理房间更新通知
                                RoomSystemManager.Instance?.HandleRoomUpdateNotification(roomUpdateNotification);
                                return;
                            }
                        }
                        catch (Exception parseEx)
                        {
                            ASLogger.Instance.Warning($"GamePlayManager: 解析RoomUpdateNotification失败 - {parseEx.Message}");
                        }
                    }
                    
                    // 如果无法解析RoomUpdateNotification，记录错误
                    ASLogger.Instance.Error("GamePlayManager: 无法解析房间更新通知");
                }
                else
                {
                    var errorMessage = !string.IsNullOrEmpty(message.Error) ? message.Error : "未知错误";
                    ASLogger.Instance.Error($"GamePlayManager: 房间更新通知失败 - {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理房间更新通知时发生异常 - {ex.Message}");
            }
        }
        
        #endregion
        
        #region 网络消息响应处理
        
        /// <summary>
        /// 处理登录响应 (兼容旧版本)
        /// </summary>
        /// <param name="message">网络消息</param>
        private void HandleLoginResponse(GameNetworkMessage message)
        {
            try
            {
                if (message.Data != null)
                {
                    ASLogger.Instance.Info("GamePlayManager: 收到旧版本登录响应，已弃用");
                    // 旧版本登录响应已弃用，使用新的UserManager处理
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理登录响应失败 - {ex.Message}");
                OnNetworkError?.Invoke("处理登录响应失败");
            }
        }
        
        /// <summary>
        /// 处理创建房间响应 (兼容旧版本)
        /// </summary>
        /// <param name="message">网络消息</param>
        private void HandleCreateRoomResponse(GameNetworkMessage message)
        {
            try
            {
                if (message.Data != null)
                {
                    ASLogger.Instance.Info("GamePlayManager: 收到旧版本创建房间响应，已弃用");
                    // 旧版本创建房间响应已弃用，使用新的RoomSystemManager处理
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理创建房间响应失败 - {ex.Message}");
                OnNetworkError?.Invoke("处理创建房间响应失败");
            }
        }
        
        /// <summary>
        /// 处理加入房间响应 (兼容旧版本)
        /// </summary>
        /// <param name="message">网络消息</param>
        private void HandleJoinRoomResponse(GameNetworkMessage message)
        {
            try
            {
                if (message.Data != null)
                {
                    ASLogger.Instance.Info("GamePlayManager: 收到旧版本加入房间响应，已弃用");
                    // 旧版本加入房间响应已弃用，使用新的RoomSystemManager处理
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 处理加入房间响应失败 - {ex.Message}");
                OnNetworkError?.Invoke("处理加入房间响应失败");
            }
        }
        
        /// <summary>
        /// 处理离开房间响应 (兼容旧版本)
        /// </summary>
        /// <param name="message">网络消息</param>
        private void HandleLeaveRoomResponse(GameNetworkMessage message)
        {
            ASLogger.Instance.Info("GamePlayManager: 收到旧版本离开房间响应，已弃用");
            // 旧版本离开房间响应已弃用，使用新的RoomSystemManager处理
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
                    ASLogger.Instance.Info("GamePlayManager: 获取房间列表成功");
                    // 房间列表更新由RoomSystemManager处理
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
            //MainRoom?.LSController?.DealNetFrameInputs(frameInputs);
            
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
            room.MainPlayerId = PlayerId;
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
        
        #region 登录流程辅助方法
        
        /// <summary>
        /// 等待网络连接完成
        /// </summary>
        /// <param name="networkManager">网络管理器</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>是否连接成功</returns>
        private async Task<bool> WaitForConnection(NetworkManager networkManager, int timeoutMs)
        {
            var startTime = DateTime.Now;
            var checkInterval = 100; // 检查间隔100毫秒
            
            while (!networkManager.IsConnected() && 
                   (DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                await Task.Delay(checkInterval);
            }
            
            return networkManager.IsConnected();
        }
        
        /// <summary>
        /// 登录超时检查异步方法
        /// </summary>
        /// <param name="displayName">显示名称</param>
        private async Task LoginTimeoutCheckAsync(string displayName)
        {
            var startTime = DateTime.Now;
            var timeoutSeconds = 15; // 15秒超时
            
            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                // 检查是否已经登录成功
                if (IsLoggedIn)
                {
                    ASLogger.Instance.Info("GamePlayManager: 登录成功，停止超时检查");
                    return;
                }
                
                await Task.Delay(500); // 每0.5秒检查一次
            }
            
            // 超时处理
            if (!IsLoggedIn)
            {
                ASLogger.Instance.Warning("GamePlayManager: 登录超时，请重试");
                OnNetworkError?.Invoke("登录超时，请重试");
            }
        }
        
        /// <summary>
        /// 检查网络连接状态
        /// </summary>
        /// <returns>网络连接状态信息</returns>
        public string GetNetworkStatus()
        {
            var networkManager = GameApplication.Instance?.NetworkManager;
            if (networkManager == null)
            {
                return "网络管理器未初始化";
            }
            
            if (networkManager.IsConnected())
            {
                var session = networkManager.GetCurrentSession();
                if (session != null)
                {
                    return $"已连接 - 通道ID: {session.Id}";
                }
                return "已连接";
            }
            
            return "未连接";
        }
        
        /// <summary>
        /// 强制重连网络
        /// </summary>
        /// <returns>重连是否成功</returns>
        public async Task<bool> ForceReconnect()
        {
            try
            {
                var networkManager = GameApplication.Instance?.NetworkManager;
                if (networkManager == null)
                {
                    ASLogger.Instance.Error("GamePlayManager: NetworkManager不存在，无法重连");
                    return false;
                }
                
                // 断开当前连接
                if (networkManager.IsConnected())
                {
                    networkManager.Disconnect();
                    await Task.Delay(1000); // 等待断开完成
                }
                
                // 重新连接
                var channelId = await networkManager.ConnectAsync("127.0.0.1", 8888);
                ASLogger.Instance.Info($"GamePlayManager: 重连请求已发送，ChannelId: {channelId}");
                
                // 等待连接完成
                var connectionResult = await WaitForConnection(networkManager, 10000);
                
                if (connectionResult)
                {
                    ASLogger.Instance.Info("GamePlayManager: 重连成功");
                    return true;
                }
                else
                {
                    ASLogger.Instance.Error("GamePlayManager: 重连失败");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"GamePlayManager: 重连过程发生异常 - {ex.Message}");
                return false;
            }
        }
        
        #endregion
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