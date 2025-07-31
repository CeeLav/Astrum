using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AstrumServer.Models;
using AstrumServer.Managers;

namespace AstrumServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<GameServer>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
    }

    public class GameServer : BackgroundService
    {
        private readonly ILogger<GameServer> _logger;
        private TcpListener? _listener;
        private readonly object _clientsLock = new();
        private readonly Dictionary<string, TcpClient> _clientConnections = new();

        public GameServer(ILogger<GameServer> logger)
        {
            _logger = logger;
            
            // 设置管理器日志
            UserManager.Instance.SetLogger(logger);
            RoomManager.Instance.SetLogger(logger);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // 设置服务器配置
                var serverPort = 8888;
                var maxPlayers = 16;
                
                _logger.LogInformation("Astrum网络服务器正在启动");
                _logger.LogInformation("服务器配置: 最大玩家数={MaxPlayers}, 端口={Port}", 
                    maxPlayers, serverPort);



                _listener = new TcpListener(IPAddress.Any, serverPort);
                _listener.Start();
                _logger.LogInformation("Astrum游戏服务器已启动，监听端口: {Port}", serverPort);

                while (!stoppingToken.IsCancellationRequested)
                {
                                    var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                var clientId = Guid.NewGuid().ToString()[..8];
                lock (_clientsLock)
                {
                    _clientConnections[clientId] = client;
                }
                _ = HandleClientAsync(client, clientId, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "服务器运行出错");
            }
            finally
            {
                _listener?.Stop();
            }
        }

        private async Task HandleClientAsync(TcpClient client, string clientId, CancellationToken cancellationToken)
        {
            
            _logger.LogInformation("客户端 {ClientId} 已连接", clientId);

            try
            {
                var stream = client.GetStream();
                var buffer = new byte[1024];
                var messageBuffer = new StringBuilder();
                var isTelnet = false;

                // 发送欢迎消息
                var welcomeMessage = $"欢迎连接到Astrum游戏服务器！\r\n请使用JSON格式发送消息，例如：{{\"type\":\"login\",\"data\":{{\"displayName\":\"Player1\"}}}}\r\n";
                var welcomeBytes = Encoding.UTF8.GetBytes(welcomeMessage);
                await stream.WriteAsync(welcomeBytes, 0, welcomeBytes.Length, cancellationToken);

                while (client.Connected && !cancellationToken.IsCancellationRequested)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    
                    if (bytesRead == 0) break; // 客户端断开连接

                    var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    
                    // 检测是否为telnet客户端
                    if (!isTelnet && receivedData.Contains("\xFF"))
                    {
                        isTelnet = true;
                        _logger.LogInformation("检测到telnet客户端 {ClientId}", clientId);
                        
                        // 发送telnet协商命令
                        var telnetCommands = new byte[]
                        {
                            0xFF, 0xFD, 0x01,  // WILL ECHO
                            0xFF, 0xFD, 0x03,  // WILL SGA
                            0xFF, 0xFB, 0x01,  // DO ECHO
                            0xFF, 0xFB, 0x03   // DO SGA
                        };
                        await stream.WriteAsync(telnetCommands, 0, telnetCommands.Length, cancellationToken);
                    }
                    
                    // 处理接收到的数据
                    foreach (char c in receivedData)
                    {
                        if (c == '\r' || c == '\n')
                        {
                            // 遇到换行符，处理完整消息
                            if (messageBuffer.Length > 0)
                            {
                                var message = messageBuffer.ToString().Trim();
                                if (!string.IsNullOrEmpty(message))
                                {
                                    _logger.LogInformation("来自客户端 {ClientId} 的消息: {Message}", clientId, message);
                                    
                                    // 处理JSON消息
                                    var response = ProcessJsonMessage(clientId, message);
                                    var responseBytes = Encoding.UTF8.GetBytes(response + "\n");
                                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
                                }
                                messageBuffer.Clear();
                            }
                        }
                        else if (c >= 32 || c == '\t') // 只处理可打印字符
                        {
                            messageBuffer.Append(c);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理客户端 {ClientId} 时出错", clientId);
            }
            finally
            {
                // 登出客户端
                UserManager.Instance.LogoutClient(clientId);
                lock (_clientsLock)
                {
                    _clientConnections.Remove(clientId);
                }
                client.Close();
                _logger.LogInformation("客户端 {ClientId} 已断开连接", clientId);
            }
        }

        private string ProcessJsonMessage(string clientId, string jsonMessage)
        {
            try
            {
                _logger.LogInformation("收到JSON消息: {Message}", jsonMessage);
                
                var message = JsonSerializer.Deserialize<NetworkMessage>(jsonMessage);
                if (message == null)
                {
                    return JsonSerializer.Serialize(NetworkMessage.CreateError("error", "无效的JSON格式"));
                }

                _logger.LogInformation("消息类型: '{Type}', 数据: {Data}", message.Type, message.Data);

                return message.Type switch
                {
                    "login" => HandleLogin(clientId, message),
                    "create_room" => HandleCreateRoom(clientId, message),
                    "join_room" => HandleJoinRoom(clientId, message),
                    "leave_room" => HandleLeaveRoom(clientId, message),
                    "get_rooms" => HandleGetRooms(clientId, message),
                    "get_online_users" => HandleGetOnlineUsers(clientId, message),
                    "ping" => JsonSerializer.Serialize(NetworkMessage.CreateSuccess("pong", new { timestamp = DateTime.Now })),
                    _ => JsonSerializer.Serialize(NetworkMessage.CreateError("error", $"未知的消息类型: '{message.Type}'"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理JSON消息时出错: {Message}", jsonMessage);
                return JsonSerializer.Serialize(NetworkMessage.CreateError("error", "消息处理失败"));
            }
        }



        private string HandleLogin(string clientId, NetworkMessage message)
        {
            try
            {
                _logger.LogInformation("开始处理登录请求，客户端ID: {ClientId}", clientId);
                
                var request = JsonSerializer.Deserialize<LoginRequest>(JsonSerializer.Serialize(message.Data));
                _logger.LogInformation("解析登录请求: DisplayName='{DisplayName}'", 
                    request?.DisplayName ?? "null");

                // 直接创建新用户
                var user = UserManager.Instance.CreateUser(request?.DisplayName);
                
                if (user != null)
                {
                    _logger.LogInformation("用户创建成功: {DisplayName}, ID: {UserId}", user.DisplayName, user.Id);
                    var client = GetClientByClientId(clientId);
                    if (client != null)
                    {
                        UserManager.Instance.LoginClient(clientId, user, client);
                        var userInfo = new UserInfo
                        {
                            Id = user.Id,
                            DisplayName = user.DisplayName,
                            LastLoginAt = user.LastLoginAt,
                            CurrentRoomId = user.CurrentRoomId
                        };
                        _logger.LogInformation("登录成功，返回用户信息");
                        return JsonSerializer.Serialize(NetworkMessage.CreateSuccess("login", userInfo));
                    }
                    else
                    {
                        _logger.LogError("无法找到客户端连接: {ClientId}", clientId);
                    }
                }

                return JsonSerializer.Serialize(NetworkMessage.CreateError("login", "登录失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理登录请求时出错");
                return JsonSerializer.Serialize(NetworkMessage.CreateError("login", "登录失败"));
            }
        }

        private string HandleCreateRoom(string clientId, NetworkMessage message)
        {
            try
            {
                var user = UserManager.Instance.GetUserByClientId(clientId);
                if (user == null)
                {
                    return JsonSerializer.Serialize(NetworkMessage.CreateError("create_room", "请先登录"));
                }

                var request = JsonSerializer.Deserialize<CreateRoomRequest>(JsonSerializer.Serialize(message.Data));
                if (request == null || string.IsNullOrWhiteSpace(request.RoomName))
                {
                    return JsonSerializer.Serialize(NetworkMessage.CreateError("create_room", "房间名称不能为空"));
                }

                var room = RoomManager.Instance.CreateRoom(request.RoomName, user.Id, request.MaxPlayers);
                if (room != null)
                {
                    var roomInfo = new RoomInfo
                    {
                        Id = room.Id,
                        Name = room.Name,
                        CreatorName = user.DisplayName,
                        CurrentPlayers = room.PlayerIds.Count,
                        MaxPlayers = room.MaxPlayers,
                        CreatedAt = room.CreatedAt,
                        PlayerNames = room.PlayerIds
                            .Select(id => UserManager.Instance.GetUser(id)?.DisplayName ?? "未知")
                            .ToList()
                    };
                    return JsonSerializer.Serialize(NetworkMessage.CreateSuccess("create_room", roomInfo));
                }

                return JsonSerializer.Serialize(NetworkMessage.CreateError("create_room", "创建房间失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理创建房间请求时出错");
                return JsonSerializer.Serialize(NetworkMessage.CreateError("create_room", "创建房间失败"));
            }
        }

        private string HandleJoinRoom(string clientId, NetworkMessage message)
        {
            try
            {
                var user = UserManager.Instance.GetUserByClientId(clientId);
                if (user == null)
                {
                    return JsonSerializer.Serialize(NetworkMessage.CreateError("join_room", "请先登录"));
                }

                var request = JsonSerializer.Deserialize<JoinRoomRequest>(JsonSerializer.Serialize(message.Data));
                if (request == null || string.IsNullOrWhiteSpace(request.RoomId))
                {
                    return JsonSerializer.Serialize(NetworkMessage.CreateError("join_room", "房间ID不能为空"));
                }

                if (RoomManager.Instance.JoinRoom(request.RoomId, user.Id))
                {
                    var room = RoomManager.Instance.GetRoom(request.RoomId);
                    if (room != null)
                    {
                        var roomInfo = new RoomInfo
                        {
                            Id = room.Id,
                            Name = room.Name,
                            CreatorName = UserManager.Instance.GetUser(room.CreatorId)?.DisplayName ?? "未知",
                            CurrentPlayers = room.PlayerIds.Count,
                            MaxPlayers = room.MaxPlayers,
                            CreatedAt = room.CreatedAt,
                                                    PlayerNames = room.PlayerIds
                            .Select(id => UserManager.Instance.GetUser(id)?.DisplayName ?? "未知")
                            .ToList()
                        };
                        return JsonSerializer.Serialize(NetworkMessage.CreateSuccess("join_room", roomInfo));
                    }
                }

                return JsonSerializer.Serialize(NetworkMessage.CreateError("join_room", "加入房间失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理加入房间请求时出错");
                return JsonSerializer.Serialize(NetworkMessage.CreateError("join_room", "加入房间失败"));
            }
        }

        private string HandleLeaveRoom(string clientId, NetworkMessage message)
        {
            try
            {
                var user = UserManager.Instance.GetUserByClientId(clientId);
                if (user == null)
                {
                    return JsonSerializer.Serialize(NetworkMessage.CreateError("leave_room", "请先登录"));
                }

                // 检查用户是否在房间中
                if (string.IsNullOrWhiteSpace(user.CurrentRoomId))
                {
                    return JsonSerializer.Serialize(NetworkMessage.CreateError("leave_room", "当前不在任何房间中"));
                }

                // 尝试离开房间
                if (RoomManager.Instance.LeaveRoom(user.CurrentRoomId, user.Id))
                {
                    return JsonSerializer.Serialize(NetworkMessage.CreateSuccess("leave_room", new { message = "已离开房间" }));
                }

                return JsonSerializer.Serialize(NetworkMessage.CreateError("leave_room", "离开房间失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理离开房间请求时出错");
                return JsonSerializer.Serialize(NetworkMessage.CreateError("leave_room", "离开房间失败"));
            }
        }

        private string HandleGetRooms(string clientId, NetworkMessage message)
        {
            try
            {
                var rooms = RoomManager.Instance.GetRoomInfoList();
                return JsonSerializer.Serialize(NetworkMessage.CreateSuccess("get_rooms", rooms));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理获取房间列表请求时出错");
                return JsonSerializer.Serialize(NetworkMessage.CreateError("get_rooms", "获取房间列表失败"));
            }
        }

        private string HandleGetOnlineUsers(string clientId, NetworkMessage message)
        {
            try
            {
                var users = UserManager.Instance.GetOnlineUsers();
                var userInfos = users.Select(u => new UserInfo
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName,
                    LastLoginAt = u.LastLoginAt,
                    CurrentRoomId = u.CurrentRoomId
                }).ToList();
                return JsonSerializer.Serialize(NetworkMessage.CreateSuccess("get_online_users", userInfos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理获取在线用户请求时出错");
                return JsonSerializer.Serialize(NetworkMessage.CreateError("get_online_users", "获取在线用户失败"));
            }
        }

        private TcpClient? GetClientByClientId(string clientId)
        {
            lock (_clientsLock)
            {
                return _clientConnections.TryGetValue(clientId, out var client) ? client : null;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("正在关闭服务器...");
            await base.StopAsync(cancellationToken);
        }
    }
}
