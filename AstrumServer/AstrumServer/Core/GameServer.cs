using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AstrumServer.Network;
using AstrumServer.Managers;
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
        private readonly ILogger<GameServer> _logger;
        private readonly ServerNetworkManager _networkManager;
        private readonly UserManager _userManager;
        private readonly RoomManager _roomManager;
        
        public GameServer(ILogger<GameServer> logger)
        {
            _logger = logger;
            _networkManager = ServerNetworkManager.Instance;
            
            // 创建LoggerFactory来创建其他Logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            });
            
            _userManager = new UserManager(loggerFactory.CreateLogger<UserManager>());
            _roomManager = new RoomManager(loggerFactory.CreateLogger<RoomManager>());
            
            _networkManager.SetLogger(logger);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Astrum游戏服务器正在启动...");
                ASLogger.Instance.Info("Astrum游戏服务器正在启动...");
                
                // 初始化网络系统基础组件
                InitializeNetworkSystem();
                
                // 启动网络管理器
                var networkStarted = await _networkManager.InitializeAsync(8888);
                if (!networkStarted)
                {
                    _logger.LogError("网络管理器启动失败");
                    return;
                }
                
                // 注册网络事件
                _networkManager.OnClientConnected += OnClientConnected;
                _networkManager.OnClientDisconnected += OnClientDisconnected;
                _networkManager.OnMessageReceived += OnMessageReceived;
                _networkManager.OnError += OnNetworkError;
                
                _logger.LogInformation("Astrum游戏服务器启动成功，监听端口: 8888");
                ASLogger.Instance.Info("Astrum游戏服务器启动成功，监听端口: 8888");
                
                // 主循环
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // 更新网络服务
                        _networkManager.Update();
                        
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
                        _logger.LogError(ex, "服务器主循环出错");
                        await Task.Delay(1000, stoppingToken); // 出错时等待1秒
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "服务器启动失败");
            }
            finally
            {
                _logger.LogInformation("正在关闭Astrum游戏服务器...");
                Shutdown();
                _logger.LogInformation("Astrum游戏服务器已关闭");
            }
        }
        
        private void OnClientConnected(Session client)
        {
            _logger.LogInformation("客户端已连接: {ClientId} from {RemoteEndpoint}", 
                client.Id, client.RemoteAddress);
            ASLogger.Instance.Info($"客户端已连接: {client.Id} from {client.RemoteAddress}");
            
            // 发送连接成功响应
            var response = ConnectResponse.Create();
            response.success = true;
            response.message = "连接成功";
            response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            _networkManager.SendMessage(client.Id.ToString(), response);
        }
        
        private void OnClientDisconnected(Session client)
        {
            _logger.LogInformation("客户端已断开: {ClientId}", client.Id);
            ASLogger.Instance.Info($"客户端已断开: {client.Id}");
            
            // 移除用户
            _userManager.RemoveUser(client.Id.ToString());
        }
        
        private void OnMessageReceived(Session client, MessageObject message)
        {
            try
            {
                _logger.LogDebug("收到来自客户端 {ClientId} 的消息: {MessageType}", 
                    client.Id, message.GetType().Name);
                
                // 根据消息类型处理
                switch (message)
                {
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
                    default:
                        _logger.LogWarning("未处理的消息类型: {MessageType}", message.GetType().Name);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理客户端 {ClientId} 的消息时出错", client.Id);
            }
        }
        
        private void OnNetworkError(Session client, Exception ex)
        {
            _logger.LogError(ex, "客户端 {ClientId} 发生网络错误", client.Id);
        }
        
        private void HandleLoginRequest(Session client, LoginRequest request)
        {
            try
            {
                _logger.LogInformation("客户端 {ClientId} 请求登录，显示名称: {DisplayName}", 
                    client.Id, request.DisplayName);
                ASLogger.Instance.Info($"客户端 {client.Id} 请求登录，显示名称: {request.DisplayName}");
                
                // 为用户分配ID
                var userInfo = _userManager.AssignUserId(client.Id.ToString(), request.DisplayName);
                
                // 发送登录成功响应
                var response = LoginResponse.Create();
                response.Success = true;
                response.Message = "登录成功";
                response.User = userInfo;
                response.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                _networkManager.SendMessage(client.Id.ToString(), response);
                _logger.LogInformation("客户端 {ClientId} 登录成功，用户ID: {UserId}", client.Id, userInfo.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理登录请求时出错");
                
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
                    _logger.LogWarning("客户端 {ClientId} 尝试创建房间但未登录", client.Id);
                    SendCreateRoomResponse(client.Id.ToString(), false, "请先登录", null);
                    return;
                }
                
                _logger.LogInformation("用户 {UserId} 请求创建房间，房间名称: {RoomName}，最大玩家数: {MaxPlayers}", 
                    userInfo.Id, request.RoomName, request.MaxPlayers);
                
                // 如果用户已在房间中，先离开
                if (!string.IsNullOrEmpty(userInfo.CurrentRoomId))
                {
                    _roomManager.LeaveRoom(userInfo.CurrentRoomId, userInfo.Id);
                    _userManager.UpdateUserRoom(userInfo.Id, "");
                }
                
                // 创建房间
                var roomInfo = _roomManager.CreateRoom(userInfo.Id, request.RoomName, request.MaxPlayers);
                
                // 更新用户房间信息
                _userManager.UpdateUserRoom(userInfo.Id, roomInfo.Id);
                
                // 发送创建成功响应
                SendCreateRoomResponse(client.Id.ToString(), true, "房间创建成功", roomInfo);
                
                // 通知房间内所有玩家房间更新
                NotifyRoomUpdate(roomInfo, "created", userInfo.Id);
                
                _logger.LogInformation("用户 {UserId} 创建房间成功，房间ID: {RoomId}", userInfo.Id, roomInfo.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理创建房间请求时出错");
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
                    _logger.LogWarning("客户端 {ClientId} 尝试加入房间但未登录", client.Id);
                    SendJoinRoomResponse(client.Id.ToString(), false, "请先登录", null);
                    return;
                }
                
                _logger.LogInformation("用户 {UserId} 请求加入房间: {RoomId}", userInfo.Id, request.RoomId);
                
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
                    var roomInfo = _roomManager.GetRoom(request.RoomId);
                    _userManager.UpdateUserRoom(userInfo.Id, request.RoomId);
                    
                    // 发送加入成功响应
                    SendJoinRoomResponse(client.Id.ToString(), true, "加入房间成功", roomInfo);
                    
                    // 通知房间内所有玩家房间更新
                    NotifyRoomUpdate(roomInfo, "joined", userInfo.Id);
                    
                    _logger.LogInformation("用户 {UserId} 加入房间成功: {RoomId}", userInfo.Id, request.RoomId);
                }
                else
                {
                    SendJoinRoomResponse(client.Id.ToString(), false, "加入房间失败：房间不存在或已满", null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理加入房间请求时出错");
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
                    _logger.LogWarning("客户端 {ClientId} 尝试离开房间但未登录", client.Id);
                    SendLeaveRoomResponse(client.Id.ToString(), false, "请先登录", "");
                    return;
                }
                
                _logger.LogInformation("用户 {UserId} 请求离开房间: {RoomId}", userInfo.Id, request.RoomId);
                
                // 离开房间
                var success = _roomManager.LeaveRoom(request.RoomId, userInfo.Id);
                if (success)
                {
                    _userManager.UpdateUserRoom(userInfo.Id, "");
                    
                    // 发送离开成功响应
                    SendLeaveRoomResponse(client.Id.ToString(), true, "离开房间成功", request.RoomId);
                    
                    // 获取更新后的房间信息并通知其他玩家
                    var roomInfo = _roomManager.GetRoom(request.RoomId);
                    if (roomInfo != null)
                    {
                        NotifyRoomUpdate(roomInfo, "left", userInfo.Id);
                    }
                    
                    _logger.LogInformation("用户 {UserId} 离开房间成功: {RoomId}", userInfo.Id, request.RoomId);
                }
                else
                {
                    SendLeaveRoomResponse(client.Id.ToString(), false, "离开房间失败：用户不在房间中", request.RoomId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理离开房间请求时出错");
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
                    _logger.LogWarning("客户端 {ClientId} 尝试获取房间列表但未登录", client.Id);
                    SendRoomListResponse(client.Id.ToString(), false, "请先登录", new List<RoomInfo>());
                    return;
                }
                
                _logger.LogDebug("用户 {UserId} 请求获取房间列表", userInfo.Id);
                
                // 获取所有房间
                var rooms = _roomManager.GetAllRooms();
                
                // 发送房间列表响应
                SendRoomListResponse(client.Id.ToString(), true, "获取房间列表成功", rooms);
                
                _logger.LogDebug("向用户 {UserId} 发送房间列表，共 {Count} 个房间", userInfo.Id, rooms.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理获取房间列表请求时出错");
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
                
                // 每60秒输出一次统计信息
                if (DateTime.Now.Second % 60 == 0)
                {
                    var userCount = _userManager.GetOnlineUserCount();
                    var roomStats = _roomManager.GetRoomStatistics();
                    
                    _logger.LogInformation("系统状态 - 在线用户: {UserCount}, 房间: {TotalRooms}, 房间内玩家: {TotalPlayers}, 空房间: {EmptyRooms}", 
                        userCount, roomStats.totalRooms, roomStats.totalPlayers, roomStats.emptyRooms);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "系统清理时出错");
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
                
                _logger.LogDebug("向房间 {RoomId} 的所有玩家发送房间更新通知: {UpdateType}", roomInfo.Id, updateType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送房间更新通知时出错");
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
        
        /// <summary>
        /// 初始化网络系统基础组件
        /// </summary>
        private void InitializeNetworkSystem()
        {
            _logger.LogInformation("正在初始化服务器网络系统基础组件...");
            ASLogger.Instance.Info("正在初始化服务器网络系统基础组件...");
            
            try
            {
                // 初始化ObjectPool（对象池系统）
                ObjectPool.Instance.Awake();
                _logger.LogInformation("ObjectPool初始化完成");
                ASLogger.Instance.Info("ObjectPool初始化完成");
                
                // 初始化CodeTypes（加载所有类型信息）
                CodeTypes.Instance.Awake();
                _logger.LogInformation("CodeTypes初始化完成");
                ASLogger.Instance.Info("CodeTypes初始化完成");
                
                // 初始化OpcodeType（注册消息类型和opcode映射）
                OpcodeType.Instance.Awake();
                _logger.LogInformation("OpcodeType初始化完成");
                ASLogger.Instance.Info("OpcodeType初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "网络系统基础组件初始化失败");
                ASLogger.Instance.Error($"网络系统基础组件初始化失败: {ex.Message}");
                throw;
            }
        }
        
        private void Shutdown()
        {
            try
            {
                _networkManager.Shutdown();
                _logger.LogInformation("网络管理器已停止");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止网络管理器时出错");
            }
        }
    }
}
