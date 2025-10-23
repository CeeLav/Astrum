using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Client.Core;
using UnityEngine;
using MemoryPack;

namespace Astrum.Client.Managers
{
    /// <summary>
    /// 客户端房间系统管理器 - 管理房间操作和状态同步
    /// </summary>
    public class RoomSystemManager : Singleton<RoomSystemManager>
    {
        // 房间数据
        public List<RoomInfo> AvailableRooms { get; private set; } = new List<RoomInfo>();
        public RoomInfo CurrentRoom { get; private set; }
        public bool IsInRoom => CurrentRoom != null;
        
        // 事件
        public event Action<List<RoomInfo>> OnRoomListUpdated;
        public event Action<RoomInfo> OnRoomCreated;
        public event Action<RoomInfo> OnRoomJoined;
        public event Action OnRoomLeft;
        public event Action<string> OnRoomError;
        public event Action OnGameStarted;
        public event Action<string> OnGameStartError;
        
        // 操作状态
        private bool isCreatingRoom = false;
        private bool isJoiningRoom = false;
        private bool isLeavingRoom = false;
        private bool isStartingGame = false;
        
        /// <summary>
        /// 初始化房间系统管理器
        /// </summary>
        public void Initialize()
        {
            ASLogger.Instance.Info("RoomSystemManager: 初始化完成");
        }
        
        /// <summary>
        /// 获取房间列表
        /// </summary>
        public async Task<bool> GetRoomListAsync()
        {
            try
            {
                ASLogger.Instance.Info("RoomSystemManager: 获取房间列表...");
                
                var networkManager = NetworkManager.Instance;
                if (networkManager == null)
                {
                    ASLogger.Instance.Error("RoomSystemManager: NetworkManager不存在");
                    OnRoomError?.Invoke("网络管理器不存在");
                    return false;
                }
                
                if (!networkManager.IsConnected())
                {
                    ASLogger.Instance.Error("RoomSystemManager: 网络未连接");
                    OnRoomError?.Invoke("网络未连接");
                    return false;
                }
                
                // 直接发送获取房间列表请求
                var request = GetRoomListRequest.Create();
                request.Timestamp = TimeInfo.Instance.ClientNow();
                
                networkManager.Send(request);
                ASLogger.Instance.Info("RoomSystemManager: 房间列表请求已发送");
                
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"RoomSystemManager: 获取房间列表失败 - {ex.Message}");
                OnRoomError?.Invoke($"获取房间列表失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 创建房间
        /// </summary>
        public async Task<bool> CreateRoomAsync(string roomName, int maxPlayers = 4)
        {
            if (IsInRoom)
            {
                ASLogger.Instance.Warning("RoomSystemManager: 已在房间中，无法创建新房间");
                OnRoomError?.Invoke("已在房间中，无法创建新房间");
                return false;
            }
            
            if (isCreatingRoom)
            {
                ASLogger.Instance.Warning("RoomSystemManager: 正在创建房间中，请稍候");
                return false;
            }
            
            try
            {
                isCreatingRoom = true;
                ASLogger.Instance.Info($"RoomSystemManager: 创建房间 - 名称: {roomName}, 最大玩家: {maxPlayers}");
                
                var networkManager = NetworkManager.Instance;
                if (networkManager == null)
                {
                    ASLogger.Instance.Error("RoomSystemManager: NetworkManager不存在");
                    OnRoomError?.Invoke("网络管理器不存在");
                    return false;
                }
                
                if (!networkManager.IsConnected())
                {
                    ASLogger.Instance.Error("RoomSystemManager: 网络未连接");
                    OnRoomError?.Invoke("网络未连接");
                    return false;
                }
                
                // 直接发送创建房间请求
                var request = CreateRoomRequest.Create();
                request.RoomName = roomName;
                request.MaxPlayers = maxPlayers;
                request.Timestamp = TimeInfo.Instance.ClientNow();
                
                networkManager.Send(request);
                ASLogger.Instance.Info("RoomSystemManager: 创建房间请求已发送");
                
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"RoomSystemManager: 创建房间失败 - {ex.Message}");
                OnRoomError?.Invoke($"创建房间失败: {ex.Message}");
                return false;
            }
            finally
            {
                isCreatingRoom = false;
            }
        }
        
        /// <summary>
        /// 加入房间
        /// </summary>
        public async Task<bool> JoinRoomAsync(string roomId)
        {
            if (IsInRoom)
            {
                ASLogger.Instance.Warning("RoomSystemManager: 已在房间中，无法加入其他房间");
                OnRoomError?.Invoke("已在房间中，无法加入其他房间");
                return false;
            }
            
            if (isJoiningRoom)
            {
                ASLogger.Instance.Warning("RoomSystemManager: 正在加入房间中，请稍候");
                return false;
            }
            
            try
            {
                isJoiningRoom = true;
                ASLogger.Instance.Info($"RoomSystemManager: 加入房间 - ID: {roomId}");
                
                var networkManager = NetworkManager.Instance;
                if (networkManager == null)
                {
                    ASLogger.Instance.Error("RoomSystemManager: NetworkManager不存在");
                    OnRoomError?.Invoke("网络管理器不存在");
                    return false;
                }
                
                if (!networkManager.IsConnected())
                {
                    ASLogger.Instance.Error("RoomSystemManager: 网络未连接");
                    OnRoomError?.Invoke("网络未连接");
                    return false;
                }
                
                // 直接发送加入房间请求
                var request = JoinRoomRequest.Create();
                request.RoomId = roomId;
                request.Timestamp = TimeInfo.Instance.ClientNow();
                
                networkManager.Send(request);
                ASLogger.Instance.Info("RoomSystemManager: 加入房间请求已发送");
                
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"RoomSystemManager: 加入房间失败 - {ex.Message}");
                OnRoomError?.Invoke($"加入房间失败: {ex.Message}");
                return false;
            }
            finally
            {
                isJoiningRoom = false;
            }
        }
        
        /// <summary>
        /// 离开房间
        /// </summary>
        public async Task<bool> LeaveRoomAsync()
        {
            if (!IsInRoom)
            {
                ASLogger.Instance.Warning("RoomSystemManager: 当前不在房间中");
                OnRoomError?.Invoke("当前不在房间中");
                return false;
            }
            
            if (isLeavingRoom)
            {
                ASLogger.Instance.Warning("RoomSystemManager: 正在离开房间中，请稍候");
                return false;
            }
            
            try
            {
                isLeavingRoom = true;
                ASLogger.Instance.Info("RoomSystemManager: 离开房间");
                
                var networkManager = NetworkManager.Instance;
                if (networkManager == null)
                {
                    ASLogger.Instance.Error("RoomSystemManager: NetworkManager不存在");
                    OnRoomError?.Invoke("网络管理器不存在");
                    return false;
                }
                
                if (!networkManager.IsConnected())
                {
                    ASLogger.Instance.Error("RoomSystemManager: 网络未连接");
                    OnRoomError?.Invoke("网络未连接");
                    return false;
                }
                
                // 直接发送离开房间请求
                var request = LeaveRoomRequest.Create();
                request.RoomId = CurrentRoom.Id;
                request.Timestamp = TimeInfo.Instance.ClientNow();
                
                networkManager.Send(request);
                ASLogger.Instance.Info("RoomSystemManager: 离开房间请求已发送");
                
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"RoomSystemManager: 离开房间失败 - {ex.Message}");
                OnRoomError?.Invoke($"离开房间失败: {ex.Message}");
                return false;
            }
            finally
            {
                isLeavingRoom = false;
            }
        }
        
        /// <summary>
        /// 开始游戏
        /// </summary>
        public async Task<bool> StartGameAsync()
        {
            
            if (!IsInRoom)
            {
                ASLogger.Instance.Warning("RoomSystemManager: 当前不在房间中，无法开始游戏");
                OnGameStartError?.Invoke("当前不在房间中，无法开始游戏");
                return false;
            }
            
            if (isStartingGame)
            {
                ASLogger.Instance.Warning("RoomSystemManager: 正在开始游戏中，请稍候");
                return false;
            }
            
            try
            {
                
                isStartingGame = true;
                ASLogger.Instance.Info($"RoomSystemManager: 开始游戏 - 房间ID: {CurrentRoom?.Id}");
                
                var networkManager = NetworkManager.Instance;
                if (networkManager == null)
                {
                    ASLogger.Instance.Error("RoomSystemManager: NetworkManager不存在");
                    OnGameStartError?.Invoke("网络管理器不存在");
                    return false;
                }
                
                if (!networkManager.IsConnected())
                {
                    ASLogger.Instance.Error("RoomSystemManager: 网络未连接");
                    OnGameStartError?.Invoke("网络未连接");
                    return false;
                }
                
                // 使用GameRequest发送开始游戏请求
                var request = GameRequest.Create();
                request.requestId = TimeInfo.Instance.ClientNow();
                request.action = "StartGame";
                
                // 添加房间ID作为参数
                request.param.Add(CurrentRoom.Id);
                
                networkManager.Send(request);
                ASLogger.Instance.Info("RoomSystemManager: 开始游戏请求已发送");
                
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"RoomSystemManager: 开始游戏失败 - {ex.Message}");
                OnGameStartError?.Invoke($"开始游戏失败: {ex.Message}");
                return false;
            }
            finally
            {
                isStartingGame = false;
            }
        }
        
        /// <summary>
        /// 处理房间列表响应
        /// </summary>
        public void HandleRoomListResponse(GetRoomListResponse response)
        {
            try
            {
                if (response.Success)
                {
                    AvailableRooms.Clear();
                    if (response.Rooms != null)
                    {
                        AvailableRooms.AddRange(response.Rooms);
                    }
                    
                    ASLogger.Instance.Info($"RoomSystemManager: 房间列表更新 - 房间数量: {AvailableRooms.Count}");
                    
                    // 触发房间列表更新事件
                    OnRoomListUpdated?.Invoke(AvailableRooms);
                }
                else
                {
                    ASLogger.Instance.Error($"RoomSystemManager: 获取房间列表失败 - {response.Message}");
                    OnRoomError?.Invoke(response.Message);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"RoomSystemManager: 处理房间列表响应时出错 - {ex.Message}");
                OnRoomError?.Invoke($"处理房间列表响应失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理创建房间响应
        /// </summary>
        public void HandleCreateRoomResponse(CreateRoomResponse response)
        {
            try
            {
                if (response.Success)
                {
                    // 使用响应中的房间信息
                    CurrentRoom = response.Room;
                    if (CurrentRoom != null)
                    {
                        ASLogger.Instance.Info($"RoomSystemManager: 房间创建成功 - ID: {CurrentRoom.Id}, 名称: {CurrentRoom.Name}");
                        
                        // 更新用户房间信息
                        var userManager = UserManager.Instance;
                        if (userManager != null)
                        {
                            userManager.UpdateUserRoom(CurrentRoom.Id);
                        }
                        
                        // 触发房间创建成功事件
                        OnRoomCreated?.Invoke(CurrentRoom);
                        
                        // 自动切换到房间详情界面
                        var uiManager = UIManager.Instance;
                        if (uiManager != null)
                        {
                            ASLogger.Instance.Info("RoomSystemManager: 切换到房间详情界面");
                            // 关闭Login UI
                            uiManager.HideUI("Login");
                            uiManager.ShowUI("RoomDetail");
                            
                            // 设置房间信息到RoomDetailView
                            var roomDetailView = uiManager.GetUI("RoomDetail")?.GetComponent<Astrum.Client.UI.Core.UIRefs>()?.GetUIInstance() as Astrum.Client.UI.Generated.RoomDetailView;
                            if (roomDetailView != null)
                            {
                                roomDetailView.SetRoomInfo(CurrentRoom);
                            }
                        }
                    }
                    else
                    {
                        ASLogger.Instance.Error("RoomSystemManager: 创建房间响应中房间信息为空");
                        OnRoomError?.Invoke("创建房间响应中房间信息为空");
                    }
                }
                else
                {
                    ASLogger.Instance.Error($"RoomSystemManager: 创建房间失败 - {response.Message}");
                    OnRoomError?.Invoke(response.Message);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"RoomSystemManager: 处理创建房间响应时出错 - {ex.Message}");
                OnRoomError?.Invoke($"处理创建房间响应失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理加入房间响应
        /// </summary>
        public void HandleJoinRoomResponse(JoinRoomResponse response)
        {
            try
            {
                if (response.Success)
                {
                    // 使用响应中的房间信息
                    CurrentRoom = response.Room;
                    if (CurrentRoom != null)
                    {
                        ASLogger.Instance.Info($"RoomSystemManager: 房间加入成功 - ID: {CurrentRoom.Id}, 名称: {CurrentRoom.Name}");
                        
                        // 更新用户房间信息
                        var userManager = UserManager.Instance;
                        if (userManager != null)
                        {
                            userManager.UpdateUserRoom(CurrentRoom.Id);
                        }
                        
                        // 触发房间加入成功事件
                        OnRoomJoined?.Invoke(CurrentRoom);
                        
                        // 自动切换到房间详情界面
                        var uiManager = UIManager.Instance;
                        if (uiManager != null)
                        {
                            ASLogger.Instance.Info("RoomSystemManager: 切换到房间详情界面");
                            // 关闭Login UI
                            uiManager.HideUI("Login");
                            uiManager.ShowUI("RoomDetail");
                            
                            // 设置房间信息到RoomDetailView
                            var roomDetailView = uiManager.GetUI("RoomDetail")?.GetComponent<Astrum.Client.UI.Core.UIRefs>()?.GetUIInstance() as Astrum.Client.UI.Generated.RoomDetailView;
                            if (roomDetailView != null)
                            {
                                roomDetailView.SetRoomInfo(CurrentRoom);
                            }
                        }
                    }
                    else
                    {
                        ASLogger.Instance.Error("RoomSystemManager: 加入房间响应中房间信息为空");
                        OnRoomError?.Invoke("加入房间响应中房间信息为空");
                    }
                }
                else
                {
                    ASLogger.Instance.Error($"RoomSystemManager: 加入房间失败 - {response.Message}");
                    OnRoomError?.Invoke(response.Message);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"RoomSystemManager: 处理加入房间响应时出错 - {ex.Message}");
                OnRoomError?.Invoke($"处理加入房间响应失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理离开房间响应
        /// </summary>
        public void HandleLeaveRoomResponse(LeaveRoomResponse response)
        {
            try
            {
                if (response.Success)
                {
                    ASLogger.Instance.Info("RoomSystemManager: 房间离开成功");
                    
                    // 清理房间信息
                    CurrentRoom = null;
                    
                    // 更新用户房间信息
                    var userManager = UserManager.Instance;
                    if (userManager != null)
                    {
                        userManager.UpdateUserRoom("");
                    }
                    
                    // 触发房间离开事件
                    OnRoomLeft?.Invoke();
                    
                    // 自动切换到房间列表界面
                    var uiManager = UIManager.Instance;
                    if (uiManager != null)
                    {
                        ASLogger.Instance.Info("RoomSystemManager: 切换到房间列表界面");
                        uiManager.HideUI("RoomDetail");
                        uiManager.ShowUI("RoomList");
                    }
                }
                else
                {
                    ASLogger.Instance.Error($"RoomSystemManager: 离开房间失败 - {response.Message}");
                    OnRoomError?.Invoke(response.Message);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"RoomSystemManager: 处理离开房间响应时出错 - {ex.Message}");
                OnRoomError?.Invoke($"处理离开房间响应失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理房间更新通知
        /// </summary>
        public void HandleRoomUpdateNotification(RoomUpdateNotification notification)
        {
            try
            {
                ASLogger.Instance.Info($"RoomSystemManager: 收到房间更新通知 - 房间ID: {notification.Room?.Id}, 更新类型: {notification.UpdateType}");
                
                // 更新房间信息
                if (CurrentRoom != null && notification.Room != null && CurrentRoom.Id == notification.Room.Id)
                {
                    CurrentRoom.CurrentPlayers = notification.Room.CurrentPlayers;
                    CurrentRoom.PlayerNames = notification.Room.PlayerNames ?? new List<string>();
                    
                    ASLogger.Instance.Info($"RoomSystemManager: 当前房间信息已更新 - 玩家数量: {CurrentRoom.CurrentPlayers}");
                }
                
                // 刷新房间列表
                _ = GetRoomListAsync();
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"RoomSystemManager: 处理房间更新通知时出错 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            AvailableRooms.Clear();
            CurrentRoom = null;
            OnRoomListUpdated = null;
            OnRoomCreated = null;
            OnRoomJoined = null;
            OnRoomLeft = null;
            OnRoomError = null;
            OnGameStarted = null;
            OnGameStartError = null;
        }
        
        /// <summary>
        /// 关闭管理器
        /// </summary>
        public void Shutdown()
        {
            try
            {
                ASLogger.Instance.Info("RoomSystemManager: 开始关闭");
                
                // 如果用户在房间中，先离开房间
                if (IsInRoom)
                {
                    ASLogger.Instance.Info($"RoomSystemManager: 用户离开房间 {CurrentRoom?.Id}");
                    // 这里可以发送离开房间请求
                }
                
                // 清理房间数据
                AvailableRooms.Clear();
                CurrentRoom = null;
                
                ASLogger.Instance.Info("RoomSystemManager: 关闭完成");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"RoomSystemManager: 关闭失败 - {ex.Message}");
            }
        }
    }
}
