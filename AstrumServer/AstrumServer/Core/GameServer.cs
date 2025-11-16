using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using AstrumServer.Network;
using AstrumServer.Managers;
using AstrumServer.Data;
using Astrum.Network;
using Astrum.Generated;
using Astrum.CommonBase;
using System.Collections.Generic;

namespace AstrumServer.Core
{
    /// <summary>
    /// Astrum游戏服务器 - 使用重构后的网络模块和完整的房间系统
    /// </summary>
    public class GameServer : BackgroundService
    {
        private readonly ServerNetworkManager _networkManager;
        private readonly UserManager _userManager;
        private readonly RoomManager _roomManager;
        private readonly FrameSyncManager _frameSyncManager;
        private readonly MatchmakingManager _matchmakingManager;
        private readonly AccountSaveManager _accountSaveManager;
        
        // 系统状态记录，用于检测变化
        private int _lastUserCount = -1;
        private int _lastTotalRooms = -1;
        private int _lastTotalPlayers = -1;
        private int _lastEmptyRooms = -1;
        
        public GameServer()
        {
            _networkManager = ServerNetworkManager.Instance;
            
            // 设置服务器日志级别为Debug，确保所有日志都能输出
            ASLogger.Instance.MinLevel = LogLevel.Debug;
            
            // 使用ASLogger创建管理器
            _userManager = new UserManager();
            _roomManager = new RoomManager(_networkManager, _userManager);
            _frameSyncManager = new FrameSyncManager(_roomManager, _networkManager, _userManager);
            _matchmakingManager = new MatchmakingManager(_roomManager, _userManager, _networkManager);
            _accountSaveManager = new AccountSaveManager();
            
            _networkManager.SetLogger(ASLogger.Instance);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                ASLogger.Instance.Info("Astrum游戏服务器正在启动...");
                
                // 初始化网络系统基础组件
                InitializeNetworkSystem();
                
                // 启动网络管理器
                var networkStarted = await _networkManager.InitializeAsync(8888);
                if (!networkStarted)
                {
                    ASLogger.Instance.Error("网络管理器启动失败");
                    return;
                }
                
                // 注册网络事件
                _networkManager.OnClientConnected += OnClientConnected;
                _networkManager.OnClientDisconnected += OnClientDisconnected;
                _networkManager.OnMessageReceived += OnMessageReceived;
                _networkManager.OnError += OnNetworkError;
                
                // 帧同步现在由 RoomManager 统一管理，无需单独启动
                
                ASLogger.Instance.Info("Astrum游戏服务器启动成功，监听端口: 8888");
                
                // 主循环
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // 更新网络服务
                        _networkManager.Update();
                        
                        // 更新所有房间（包括帧同步推进）
                        _roomManager.UpdateAllRooms();
                        
                        // 更新匹配系统（检查匹配和超时）
                        _matchmakingManager.Update();

                        // 处理客户端消息
                        await ProcessClientMessages();
                        
                        // 清理空房间和断线用户
                        CleanupSystem();
                        
                        // 等待一小段时间避免CPU占用过高
                        await Task.Delay(16, stoppingToken); // ~60fps
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常的取消操作
                        break;
                    }
                    catch (Exception ex)
                    {
                        ASLogger.Instance.Error($"服务器主循环出错: {ex.Message}");
                        ASLogger.Instance.LogException(ex, LogLevel.Error);
                        await Task.Delay(1000, stoppingToken); // 出错时等待1秒
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"服务器启动失败: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Fatal);
            }
            finally
            {
                ASLogger.Instance.Info("正在关闭Astrum游戏服务器...");
                Shutdown();
                ASLogger.Instance.Info("Astrum游戏服务器已关闭");
            }
        }
        
        private void OnClientConnected(Session client)
        {
            ASLogger.Instance.Info($"客户端已连接: {client.Id} from {client.RemoteAddress}");
            
            // 发送连接成功响应
            var response = ConnectResponse.Create();
            response.success = true;
            response.message = "连接成功";
            response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            _networkManager.SendMessage(client.Id.ToString(), response);
        }
        
        /// <summary>
        /// 处理连接请求
        /// </summary>
        private void HandleConnectRequest(Session client, ConnectRequest request)
        {
            try
            {
                ASLogger.Instance.Info($"收到客户端 {client.Id} 的连接请求");
                
                // 发送连接成功响应
                var response = ConnectResponse.Create();
                response.success = true;
                response.message = "连接成功";
                response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                _networkManager.SendMessage(client.Id.ToString(), response);
                
                ASLogger.Instance.Info($"已向客户端 {client.Id} 发送连接响应");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理连接请求时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                
                // 发送连接失败响应
                var response = ConnectResponse.Create();
                response.success = false;
                response.message = "连接失败: " + ex.Message;
                response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                _networkManager.SendMessage(client.Id.ToString(), response);
            }
        }
        
        private void OnClientDisconnected(Session client)
        {
            ASLogger.Instance.Info($"客户端已断开: {client.Id}");
            
            try
            {
                // 查找对应用户
                var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
                if (userInfo != null)
                {
                    // 从匹配队列移除（如果在队列中）
                    _matchmakingManager.DequeuePlayer(userInfo.Id);
                    
                    // 若在房间内，先从房间移除
                    if (!string.IsNullOrEmpty(userInfo.CurrentRoomId))
                    {
                        var roomId = userInfo.CurrentRoomId;
                        var left = _roomManager.LeaveRoom(roomId, userInfo.Id);
                        _userManager.UpdateUserRoom(userInfo.Id, "");

                        // 若房间已为空，RoomManager 应删除房间；这里触发一次系统清理以确保及时清理
                        CleanupSystem();
                        ASLogger.Instance.Info($"用户 {userInfo.Id} 断线，已从房间 {roomId} 移除 (left={left})");
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Warning($"处理断线清理时出现问题: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Warning);
            }
            finally
            {
                // 最终移除用户映射
                _userManager.RemoveUser(client.Id.ToString());
            }
        }
        
        private void OnMessageReceived(Session client, MessageObject message)
        {
            try
            {
                ASLogger.Instance.Debug($"收到来自客户端 {client.Id} 的消息: {message.GetType().Name}");
                
                // 根据消息类型处理
                switch (message)
                {
                    case ConnectRequest connectRequest:
                        HandleConnectRequest(client, connectRequest);
                        break;
                    case LoginRequest loginRequest:
                        HandleLoginRequest(client, loginRequest);
                        break;
                    case CreateRoomRequest createRoomRequest:
                        HandleCreateRoomRequest(client, createRoomRequest);
                        break;
                    case JoinRoomRequest joinRoomRequest:
                        HandleJoinRoomRequest(client, joinRoomRequest);
                        break;
                    case LeaveRoomRequest leaveRoomRequest:
                        HandleLeaveRoomRequest(client, leaveRoomRequest);
                        break;
                    case GetRoomListRequest getRoomListRequest:
                        HandleGetRoomListRequest(client, getRoomListRequest);
                        break;
                    case HeartbeatMessage heartbeatMessage:
                        HandleHeartbeatMessage(client, heartbeatMessage);
                        break;
                    case GameRequest gameRequest:
                        HandleGameRequest(client, gameRequest);
                        break;
                    case SingleInput singleInput:
                        HandleSingleInput(client, singleInput);
                        break;
                    case C2G_Ping c2gPing:
                        HandlePing(client, c2gPing);
                        break;
                    case QuickMatchRequest quickMatchRequest:
                        HandleQuickMatchRequest(client, quickMatchRequest);
                        break;
                    case CancelMatchRequest cancelMatchRequest:
                        HandleCancelMatchRequest(client, cancelMatchRequest);
                        break;
                    default:
                        ASLogger.Instance.Warning($"未处理的消息类型: {message.GetType().Name}");
                        break;
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理客户端 {client.Id} 的消息时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        private void OnNetworkError(Session client, Exception ex)
        {
            ASLogger.Instance.Error($"客户端 {client.Id} 发生网络错误: {ex.Message}");
            ASLogger.Instance.LogException(ex, LogLevel.Error);
        }
        
        private void HandleLoginRequest(Session client, LoginRequest request)
        {
            try
            {
                // 获取客户端实例ID（如果请求中包含）
                var clientInstanceId = request.ClientInstanceId;
                if (string.IsNullOrEmpty(clientInstanceId))
                {
                    // 兼容旧版本：使用Session ID作为临时标识
                    clientInstanceId = $"temp_{client.Id}";
                    ASLogger.Instance.Warning($"客户端未提供实例ID，使用临时标识: {clientInstanceId}");
                }
                
                ASLogger.Instance.Info($"客户端 {client.Id} 请求登录，实例ID: {clientInstanceId}, 显示名称: {request.DisplayName}");
                
                // 根据客户端实例ID获取或创建账号（客户端实例ID直接作为账号ID）
                var userInfo = _userManager.GetOrCreateUserByClientId(
                    clientInstanceId, 
                    client.Id.ToString(), 
                    request.DisplayName ?? $"Player_{client.Id}"
                );
                
                // 加载账号存档
                var accountSave = _accountSaveManager.LoadAccountSave(clientInstanceId);
                if (accountSave != null)
                {
                    ASLogger.Instance.Info($"AccountSaveManager: 已加载账号存档 - ClientInstanceId: {clientInstanceId}, Level: {accountSave.Level}");
                    // 注意：这里暂时不返回存档数据，后续可以通过单独的协议消息返回
                }
                
                // 发送登录成功响应
                var response = LoginResponse.Create();
                response.Success = true;
                response.Message = "登录成功";
                response.User = userInfo;
                response.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                _networkManager.SendMessage(client.Id.ToString(), response);
                ASLogger.Instance.Info($"客户端 {client.Id} 登录成功，用户ID: {userInfo.Id}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理登录请求时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                
                // 发送登录失败响应
                var response = LoginResponse.Create();
                response.Success = false;
                response.Message = "登录失败: " + ex.Message;
                response.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                _networkManager.SendMessage(client.Id.ToString(), response);
            }
        }
        
        private void HandleCreateRoomRequest(Session client, CreateRoomRequest request)
        {
            try
            {
                var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
                if (userInfo == null)
                {
                    ASLogger.Instance.Warning($"客户端 {client.Id} 尝试创建房间但未登录");
                    SendCreateRoomResponse(client.Id.ToString(), false, "请先登录", null);
                    return;
                }
                
                ASLogger.Instance.Info($"用户 {userInfo.Id} 请求创建房间，房间名称: {request.RoomName}，最大玩家数: {request.MaxPlayers}");
                
                // 如果用户已在房间中，先离开
                if (!string.IsNullOrEmpty(userInfo.CurrentRoomId))
                {
                    _roomManager.LeaveRoom(userInfo.CurrentRoomId, userInfo.Id);
                    _userManager.UpdateUserRoom(userInfo.Id, "");
                }
                
                // 创建房间
                var room = _roomManager.CreateRoom(userInfo.Id, request.RoomName, request.MaxPlayers);
                
                // 更新用户房间信息
                _userManager.UpdateUserRoom(userInfo.Id, room.Info.Id);
                
                // 发送创建成功响应
                SendCreateRoomResponse(client.Id.ToString(), true, "房间创建成功", room.Info);
                
                // 通知房间内所有玩家房间更新
                NotifyRoomUpdate(room.Info, "created", userInfo.Id);
                
                ASLogger.Instance.Info($"用户 {userInfo.Id} 创建房间成功，房间ID: {room.Info.Id}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理创建房间请求时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                SendCreateRoomResponse(client.Id.ToString(), false, "创建房间失败: " + ex.Message, null);
            }
        }
        
        private void HandleJoinRoomRequest(Session client, JoinRoomRequest request)
        {
            try
            {
                var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
                if (userInfo == null)
                {
                    ASLogger.Instance.Warning($"客户端 {client.Id} 尝试加入房间但未登录");
                    SendJoinRoomResponse(client.Id.ToString(), false, "请先登录", null);
                    return;
                }
                
                ASLogger.Instance.Info($"用户 {userInfo.Id} 请求加入房间: {request.RoomId}");
                
                // 如果用户已在房间中，先离开
                if (!string.IsNullOrEmpty(userInfo.CurrentRoomId))
                {
                    _roomManager.LeaveRoom(userInfo.CurrentRoomId, userInfo.Id);
                    _userManager.UpdateUserRoom(userInfo.Id, "");
                }
                
                // 加入房间
                var success = _roomManager.JoinRoom(request.RoomId, userInfo.Id);
                if (success)
                {
                    var room = _roomManager.GetRoom(request.RoomId);
                    if (room != null)
                    {
                        _userManager.UpdateUserRoom(userInfo.Id, request.RoomId);
                        
                        // 发送加入成功响应
                        SendJoinRoomResponse(client.Id.ToString(), true, "加入房间成功", room.Info);
                        
                        // 通知房间内所有玩家房间更新
                        NotifyRoomUpdate(room.Info, "joined", userInfo.Id);
                        
                        ASLogger.Instance.Info($"用户 {userInfo.Id} 加入房间成功: {request.RoomId}");
                    }
                }
                else
                {
                    SendJoinRoomResponse(client.Id.ToString(), false, "加入房间失败：房间不存在或已满", null);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理加入房间请求时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                SendJoinRoomResponse(client.Id.ToString(), false, "加入房间失败: " + ex.Message, null);
            }
        }
        
        private void HandleLeaveRoomRequest(Session client, LeaveRoomRequest request)
        {
            try
            {
                var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
                if (userInfo == null)
                {
                    ASLogger.Instance.Warning($"客户端 {client.Id} 尝试离开房间但未登录");
                    SendLeaveRoomResponse(client.Id.ToString(), false, "请先登录", "");
                    return;
                }
                
                ASLogger.Instance.Info($"用户 {userInfo.Id} 请求离开房间: {request.RoomId}");
                
                // 离开房间
                var success = _roomManager.LeaveRoom(request.RoomId, userInfo.Id);
                if (success)
                {
                    _userManager.UpdateUserRoom(userInfo.Id, "");
                    
                    // 发送离开成功响应
                    SendLeaveRoomResponse(client.Id.ToString(), true, "离开房间成功", request.RoomId);
                    
                    // 获取更新后的房间信息并通知其他玩家
                    var room = _roomManager.GetRoom(request.RoomId);
                    if (room != null)
                    {
                        NotifyRoomUpdate(room.Info, "left", userInfo.Id);
                    }
                    else
                    {
                        // 房间已被删除（房间变空），帧同步已由 Room 自动停止
                        ASLogger.Instance.Info($"房间 {request.RoomId} 已变空");
                    }
                    
                    ASLogger.Instance.Info($"用户 {userInfo.Id} 离开房间成功: {request.RoomId}");
                }
                else
                {
                    SendLeaveRoomResponse(client.Id.ToString(), false, "离开房间失败：用户不在房间中", request.RoomId);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理离开房间请求时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                SendLeaveRoomResponse(client.Id.ToString(), false, "离开房间失败: " + ex.Message, request.RoomId);
            }
        }
        
        private void HandleGetRoomListRequest(Session client, GetRoomListRequest request)
        {
            try
            {
                var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
                if (userInfo == null)
                {
                    ASLogger.Instance.Warning($"客户端 {client.Id} 尝试获取房间列表但未登录");
                    SendRoomListResponse(client.Id.ToString(), false, "请先登录", new List<RoomInfo>());
                    return;
                }
                
                ASLogger.Instance.Debug($"用户 {userInfo.Id} 请求获取房间列表");
                
                // 获取所有房间
                var rooms = _roomManager.GetAllRooms();
                
                // 发送房间列表响应
                SendRoomListResponse(client.Id.ToString(), true, "获取房间列表成功", rooms);
                
                ASLogger.Instance.Debug($"向用户 {userInfo.Id} 发送房间列表，共 {rooms.Count} 个房间");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理获取房间列表请求时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                SendRoomListResponse(client.Id.ToString(), false, "获取房间列表失败: " + ex.Message, new List<RoomInfo>());
            }
        }
        
        private void HandleHeartbeatMessage(Session client, HeartbeatMessage message)
        {
            // 发送心跳响应
            var response = HeartbeatResponse.Create();
            response.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            response.Message = "pong";
            
            _networkManager.SendMessage(client.Id.ToString(), response);
        }
        
        private async Task ProcessClientMessages()
        {
            // 这里可以添加其他客户端消息处理逻辑
            await Task.CompletedTask;
        }
        
        private void CleanupSystem()
        {
            try
            {
                // 清理空房间
                _roomManager.CleanupEmptyRooms();
                
                // 清理断线用户
                _userManager.CleanupDisconnectedUsers();
                
                // 检查系统状态变化，只在有变化时输出日志
                var userCount = _userManager.GetOnlineUserCount();
                var roomStats = _roomManager.GetRoomStatistics();
                
                // 检查是否有任何变化
                bool hasChanged = userCount != _lastUserCount || 
                                 roomStats.totalRooms != _lastTotalRooms || 
                                 roomStats.totalPlayers != _lastTotalPlayers || 
                                 roomStats.emptyRooms != _lastEmptyRooms;
                
                if (hasChanged)
                {
                    ASLogger.Instance.Info($"系统状态 - 在线用户: {userCount}, 房间: {roomStats.totalRooms}, 房间内玩家: {roomStats.totalPlayers}, 空房间: {roomStats.emptyRooms}");
                    
                    // 更新记录的状态
                    _lastUserCount = userCount;
                    _lastTotalRooms = roomStats.totalRooms;
                    _lastTotalPlayers = roomStats.totalPlayers;
                    _lastEmptyRooms = roomStats.emptyRooms;
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"系统清理时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        private void NotifyRoomUpdate(RoomInfo roomInfo, string updateType, string relatedUserId)
        {
            try
            {
                var notification = RoomUpdateNotification.Create();
                notification.Room = roomInfo;
                notification.UpdateType = updateType;
                notification.RelatedUserId = relatedUserId;
                notification.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                // 向房间内所有玩家发送通知
                foreach (var playerId in roomInfo.PlayerNames)
                {
                    var sessionId = _userManager.GetSessionIdByUserId(playerId);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _networkManager.SendMessage(sessionId, notification);
                    }
                }
                
                ASLogger.Instance.Debug($"向房间 {roomInfo.Id} 的所有玩家发送房间更新通知: {updateType}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"发送房间更新通知时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        private void SendCreateRoomResponse(string sessionId, bool success, string message, RoomInfo? room)
        {
            var response = CreateRoomResponse.Create();
            response.Success = success;
            response.Message = message;
            response.Room = room;
            response.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            _networkManager.SendMessage(sessionId, response);
        }
        
        private void SendJoinRoomResponse(string sessionId, bool success, string message, RoomInfo? room)
        {
            var response = JoinRoomResponse.Create();
            response.Success = success;
            response.Message = message;
            response.Room = room;
            response.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            _networkManager.SendMessage(sessionId, response);
        }
        
        private void SendLeaveRoomResponse(string sessionId, bool success, string message, string roomId)
        {
            var response = LeaveRoomResponse.Create();
            response.Success = success;
            response.Message = message;
            response.RoomId = roomId;
            response.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            _networkManager.SendMessage(sessionId, response);
        }
        
        private void SendRoomListResponse(string sessionId, bool success, string message, List<RoomInfo> rooms)
        {
            var response = GetRoomListResponse.Create();
            response.Success = success;
            response.Message = message;
            response.Rooms = rooms;
            response.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            _networkManager.SendMessage(sessionId, response);
        }
        
        private void SendGameResponse(string sessionId, bool success, string message)
        {
            var response = GameResponse.Create();
            response.success = success;
            response.message = message;
            response.requestId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            _networkManager.SendMessage(sessionId, response);
        }
        
        private void NotifyGameStart(RoomInfo roomInfo, string hostId)
        {
            try
            {
                // 创建游戏配置
                var gameConfig = GameConfig.Create();
                gameConfig.maxPlayers = roomInfo.MaxPlayers;
                gameConfig.roundTime = 60; // 60秒一回合
                gameConfig.maxRounds = 5; // 最多5回合
                gameConfig.allowSpectators = true;
                gameConfig.gameModes = new List<string> { "经典模式", "快速模式" };

                // 创建游戏房间状态
                var roomState = GameRoomState.Create();
                roomState.roomId = roomInfo.Id;
                roomState.currentRound = 1;
                roomState.maxRounds = gameConfig.maxRounds;
                roomState.roundStartTime = TimeInfo.Instance.ClientNow();
                roomState.activePlayers = new List<string>(roomInfo.PlayerNames);

                // 创建游戏开始通知
                var notification = GameStartNotification.Create();
                notification.roomId = roomInfo.Id;
                notification.config = gameConfig;
                notification.roomState = roomState;
                notification.startTime = roomInfo.GameStartTime;
                notification.playerIds = new List<string>(roomInfo.PlayerNames);

                // 通知房间内所有玩家游戏开始
                foreach (var playerId in roomInfo.PlayerNames)
                {
                    var sessionId = _userManager.GetSessionIdByUserId(playerId);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _networkManager.SendMessage(sessionId, notification);
                    }
                }
                
                ASLogger.Instance.Info($"已通知房间 {roomInfo.Id} 的所有玩家游戏开始 (Players: {roomInfo.CurrentPlayers})");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"通知游戏开始时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        private void HandleGameRequest(Session client, GameRequest request)
        {
            try
            {
                var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
                if (userInfo == null)
                {
                    ASLogger.Instance.Warning($"客户端 {client.Id} 尝试开始游戏但未登录");
                    SendGameResponse(client.Id.ToString(), false, "请先登录");
                    return;
                }
                
                if (string.IsNullOrEmpty(userInfo.CurrentRoomId))
                {
                    ASLogger.Instance.Warning($"用户 {userInfo.Id} 尝试开始游戏但不在房间中");
                    SendGameResponse(client.Id.ToString(), false, "请先加入房间");
                    return;
                }
                
                var room = _roomManager.GetRoom(userInfo.CurrentRoomId);
                if (room == null)
                {
                    ASLogger.Instance.Warning($"用户 {userInfo.Id} 尝试开始游戏但房间不存在: {userInfo.CurrentRoomId}");
                    SendGameResponse(client.Id.ToString(), false, "房间不存在");
                    return;
                }
                
                // 检查是否为房主
                if (room.Info.CreatorName != userInfo.Id)
                {
                    ASLogger.Instance.Warning($"用户 {userInfo.Id} 尝试开始游戏但不是房主");
                    SendGameResponse(client.Id.ToString(), false, "只有房主才能开始游戏");
                    return;
                }
                
                // 先通知房间内所有玩家游戏开始（构建客户端房间/舞台）
                // 注意：必须在 StartGame 之前发送，确保客户端游戏模式已切换
                NotifyGameStart(room.Info, userInfo.Id);
                
                // 使用房间管理器开始游戏（会自动启动帧同步）
                if (!_roomManager.StartGame(room.Info.Id, userInfo.Id))
                {
                    ASLogger.Instance.Warning($"房间管理器开始游戏失败: {room.Info.Id}");
                    SendGameResponse(client.Id.ToString(), false, "开始游戏失败");
                    return;
                }
                
                // 发送开始游戏成功响应
                SendGameResponse(client.Id.ToString(), true, "游戏开始成功");
                
                // 发送帧同步开始通知（在 GameStartNotification 之后）
                var gameSession = room.GetGameSession();
                if (gameSession != null)
                {
                    gameSession.SendFrameSyncStartNotificationIfReady();
                }
                
                ASLogger.Instance.Info($"用户 {userInfo.Id} 开始游戏成功 - 房间: {room.Info.Id}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理开始游戏请求时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                SendGameResponse(client.Id.ToString(), false, "开始游戏失败: " + ex.Message);
            }
        }
        
        /// <summary>
        /// 处理游戏结束请求
        /// </summary>
        private void HandleGameEndRequest(Session client, GameRequest request)
        {
            try
            {
                var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
                if (userInfo == null)
                {
                    ASLogger.Instance.Warning($"客户端 {client.Id} 尝试结束游戏但未登录");
                    SendGameResponse(client.Id.ToString(), false, "请先登录");
                    return;
                }
                
                if (string.IsNullOrEmpty(userInfo.CurrentRoomId))
                {
                    ASLogger.Instance.Warning($"用户 {userInfo.Id} 尝试结束游戏但不在房间中");
                    SendGameResponse(client.Id.ToString(), false, "请先加入房间");
                    return;
                }
                
                var room = _roomManager.GetRoom(userInfo.CurrentRoomId);
                if (room == null)
                {
                    ASLogger.Instance.Warning($"用户 {userInfo.Id} 尝试结束游戏但房间不存在: {userInfo.CurrentRoomId}");
                    SendGameResponse(client.Id.ToString(), false, "房间不存在");
                    return;
                }
                
                // 检查是否为房主
                if (room.Info.CreatorName != userInfo.Id)
                {
                    ASLogger.Instance.Warning($"用户 {userInfo.Id} 尝试结束游戏但不是房主");
                    SendGameResponse(client.Id.ToString(), false, "只有房主才能结束游戏");
                    return;
                }
                
                // 使用房间管理器结束游戏（会自动停止帧同步）
                if (!_roomManager.EndGame(room.Info.Id, "房主主动结束"))
                {
                    ASLogger.Instance.Warning($"房间管理器结束游戏失败: {room.Info.Id}");
                    SendGameResponse(client.Id.ToString(), false, "结束游戏失败");
                    return;
                }
                
                // 发送结束游戏成功响应
                SendGameResponse(client.Id.ToString(), true, "游戏结束成功");
                
                // 通知房间内所有玩家游戏结束
                NotifyGameEnd(room.Info, "房主主动结束");
                
                ASLogger.Instance.Info($"用户 {userInfo.Id} 结束游戏成功 - 房间: {room.Info.Id}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理结束游戏请求时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                SendGameResponse(client.Id.ToString(), false, "结束游戏失败: " + ex.Message);
            }
        }
        
        /// <summary>
        /// 通知游戏结束
        /// </summary>
        private void NotifyGameEnd(RoomInfo roomInfo, string reason)
        {
            try
            {
                // 创建游戏结果
                var gameResult = GameResult.Create();
                gameResult.roomId = roomInfo.Id;
                gameResult.endTime = roomInfo.GameEndTime;
                
                // 为每个玩家创建结果
                for (int i = 0; i < roomInfo.PlayerNames.Count; i++)
                {
                    var playerResult = PlayerResult.Create();
                    playerResult.playerId = roomInfo.PlayerNames[i];
                    playerResult.score = 100 - i * 10; // 简单的分数计算
                    playerResult.rank = i + 1;
                    playerResult.isWinner = i == 0; // 第一个玩家获胜
                    gameResult.playerResults.Add(playerResult);
                }
                
                // 创建游戏结束通知
                var notification = GameEndNotification.Create();
                notification.roomId = roomInfo.Id;
                notification.result = gameResult;
                notification.endTime = roomInfo.GameEndTime;
                notification.reason = reason;
                
                // 通知房间内所有玩家游戏结束
                foreach (var playerId in roomInfo.PlayerNames)
                {
                    var sessionId = _userManager.GetSessionIdByUserId(playerId);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _networkManager.SendMessage(sessionId, notification);
                    }
                }
                
                ASLogger.Instance.Info($"已通知房间 {roomInfo.Id} 的所有玩家游戏结束 (Reason: {reason})");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"通知游戏结束时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 初始化网络系统基础组件
        /// </summary>
        private void InitializeNetworkSystem()
        {
            ASLogger.Instance.Info("正在初始化服务器网络系统基础组件...");
            
            try
            {
                // 初始化ObjectPool（对象池系统）
                ObjectPool.Instance.Awake();
                ASLogger.Instance.Info("ObjectPool初始化完成");
                
                // 初始化CodeTypes（加载所有类型信息）
                CodeTypes.Instance.Awake();
                ASLogger.Instance.Info("CodeTypes初始化完成");
                
                // 初始化OpcodeType（注册消息类型和opcode映射）
                OpcodeType.Instance.Awake();
                ASLogger.Instance.Info("OpcodeType初始化完成");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"网络系统基础组件初始化失败: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Fatal);
                throw;
            }
        }
        
        /// <summary>
        /// 处理客户端发送的单帧输入数据
        /// </summary>
        private void HandleSingleInput(Session client, SingleInput singleInput)
        {
            try
            {
                var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
                if (userInfo == null)
                {
                    ASLogger.Instance.Warning($"客户端 {client.Id} 发送单帧输入但未登录");
                    return;
                }
                
                if (string.IsNullOrEmpty(userInfo.CurrentRoomId))
                {
                    ASLogger.Instance.Warning($"用户 {userInfo.Id} 发送单帧输入但不在房间中");
                    return;
                }
                
                // 将单帧输入数据传递给房间
                var room = _roomManager.GetRoom(userInfo.CurrentRoomId);
                if (room != null)
                {
                    room.HandleInput(userInfo.Id, singleInput);
                }
                else
                {
                    ASLogger.Instance.Warning($"用户 {userInfo.Id} 发送单帧输入但房间不存在: {userInfo.CurrentRoomId}");
                }
                
                ASLogger.Instance.Debug($"处理用户 {userInfo.Id} 的单帧输入，房间: {userInfo.CurrentRoomId}，帧: {singleInput.FrameID}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理单帧输入时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 处理快速匹配请求
        /// </summary>
        private void HandleQuickMatchRequest(Session client, QuickMatchRequest request)
        {
            try
            {
                var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
                if (userInfo == null)
                {
                    ASLogger.Instance.Warning($"客户端 {client.Id} 尝试快速匹配但未登录");
                    SendQuickMatchResponse(client.Id.ToString(), false, "请先登录", 0, 0);
                    return;
                }
                
                ASLogger.Instance.Info($"用户 {userInfo.Id} 请求快速匹配");
                
                // 检查用户是否已在房间中
                if (!string.IsNullOrEmpty(userInfo.CurrentRoomId))
                {
                    ASLogger.Instance.Warning($"用户 {userInfo.Id} 已在房间中，无法加入匹配队列");
                    SendQuickMatchResponse(client.Id.ToString(), false, "您已在房间中", 0, 0);
                    return;
                }
                
                // 加入匹配队列
                var success = _matchmakingManager.EnqueuePlayer(userInfo.Id, userInfo.DisplayName);
                if (!success)
                {
                    SendQuickMatchResponse(client.Id.ToString(), false, "加入匹配队列失败", 0, 0);
                    return;
                }
                
                // 获取队列信息
                var (position, total) = _matchmakingManager.GetQueueInfo(userInfo.Id);
                
                // 发送成功响应
                SendQuickMatchResponse(client.Id.ToString(), true, "已加入匹配队列", position, total);
                
                ASLogger.Instance.Info($"用户 {userInfo.Id} 加入匹配队列成功，队列位置: {position + 1}/{total}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理快速匹配请求时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                SendQuickMatchResponse(client.Id.ToString(), false, "服务器错误: " + ex.Message, 0, 0);
            }
        }

        /// <summary>
        /// 发送快速匹配响应
        /// </summary>
        private void SendQuickMatchResponse(string sessionId, bool success, string message, int position, int total)
        {
            try
            {
                var response = QuickMatchResponse.Create();
                response.Success = success;
                response.Message = message;
                response.Timestamp = TimeInfo.Instance.ClientNow();
                response.QueuePosition = position;
                response.QueueSize = total;
                
                _networkManager.SendMessage(sessionId, response);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"发送快速匹配响应时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 处理取消匹配请求
        /// </summary>
        private void HandleCancelMatchRequest(Session client, CancelMatchRequest request)
        {
            try
            {
                var userInfo = _userManager.GetUserBySessionId(client.Id.ToString());
                if (userInfo == null)
                {
                    ASLogger.Instance.Warning($"客户端 {client.Id} 尝试取消匹配但未登录");
                    SendCancelMatchResponse(client.Id.ToString(), false, "请先登录");
                    return;
                }
                
                ASLogger.Instance.Info($"用户 {userInfo.Id} 请求取消匹配");
                
                // 从匹配队列移除
                var success = _matchmakingManager.DequeuePlayer(userInfo.Id);
                if (!success)
                {
                    SendCancelMatchResponse(client.Id.ToString(), false, "您不在匹配队列中");
                    return;
                }
                
                // 发送成功响应
                SendCancelMatchResponse(client.Id.ToString(), true, "已取消匹配");
                
                ASLogger.Instance.Info($"用户 {userInfo.Id} 取消匹配成功");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理取消匹配请求时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                SendCancelMatchResponse(client.Id.ToString(), false, "服务器错误: " + ex.Message);
            }
        }

        /// <summary>
        /// 发送取消匹配响应
        /// </summary>
        private void SendCancelMatchResponse(string sessionId, bool success, string message)
        {
            try
            {
                var response = CancelMatchResponse.Create();
                response.Success = success;
                response.Message = message;
                response.Timestamp = TimeInfo.Instance.ClientNow();
                
                _networkManager.SendMessage(sessionId, response);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"发送取消匹配响应时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 处理客户端 Ping：回发服务器当前时间（毫秒）
        /// </summary>
        private void HandlePing(Session client, C2G_Ping request)
        {
            try
            {
                var resp = G2C_Ping.Create();
                resp.Time = TimeInfo.Instance.ClientNow(); // 服务器当前时间（毫秒）
                _networkManager.SendMessage(client.Id.ToString(), resp);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理Ping时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        private void Shutdown()
        {
            try
            {
                // 帧同步现在由 RoomManager 统一管理，房间销毁时会自动停止
                
                _networkManager.Shutdown();
                ASLogger.Instance.Info("网络管理器已停止");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"停止服务器时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
    }
}
