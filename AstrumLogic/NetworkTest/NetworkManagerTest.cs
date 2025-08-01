using System;
using System.Text.Json;
using NetworkTest.Models;

namespace NetworkTest
{
    /// <summary>
    /// NetworkManager测试程序
    /// </summary>
    public class NetworkManagerTest
    {
        private static NetworkManager? _networkManager;
        private static UserInfo? _currentUser;
        private static bool _isRunning = true;

        public static async Task RunTest(string[] args)
        {
            Console.WriteLine("=== Astrum NetworkManager 测试客户端 ===");
            Console.WriteLine("正在初始化网络管理器...");

            _networkManager = new NetworkManager();
            _networkManager.OnConnected += OnConnected;
            _networkManager.OnDisconnected += OnDisconnected;
            _networkManager.OnConnectionStatusChanged += OnConnectionStatusChanged;
            _networkManager.OnMessageReceived += OnMessageReceived;

            // 注册消息处理器
            RegisterMessageHandlers();

            if (await _networkManager.ConnectAsync())
            {
                await ShowMainMenu();
            }
            else
            {
                Console.WriteLine("无法连接到服务器，按任意键退出...");
                Console.ReadKey();
            }

            _networkManager?.Dispose();
        }

        private static void RegisterMessageHandlers()
        {
            _networkManager?.RegisterHandler("login", OnLoginResponse);
            _networkManager?.RegisterHandler("create_room", OnCreateRoomResponse);
            _networkManager?.RegisterHandler("join_room", OnJoinRoomResponse);
            _networkManager?.RegisterHandler("leave_room", OnLeaveRoomResponse);
            _networkManager?.RegisterHandler("get_rooms", OnGetRoomsResponse);
            _networkManager?.RegisterHandler("get_online_users", OnGetOnlineUsersResponse);
            _networkManager?.RegisterHandler("pong", OnPongResponse);
        }

        private static async Task ShowMainMenu()
        {
            while (_isRunning)
            {
                Console.WriteLine("\n=== 主菜单 ===");
                Console.WriteLine($"连接状态: {_networkManager?.ConnectionStatus}");
                
                if (_currentUser == null)
                {
                    Console.WriteLine("1. 自动登录");
                }
                else
                {
                    Console.WriteLine($"当前用户: {_currentUser.DisplayName}");
                    Console.WriteLine("1. 创建房间");
                    Console.WriteLine("2. 查看房间列表");
                    Console.WriteLine("3. 加入房间");
                    Console.WriteLine("4. 离开房间");
                    Console.WriteLine("5. 查看在线用户");
                    Console.WriteLine("6. 登出");
                }
                Console.WriteLine("7. 发送心跳");
                Console.WriteLine("8. 断开连接");
                Console.WriteLine("0. 退出");

                Console.Write("请选择操作: ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        if (_currentUser == null)
                            await AutoLogin();
                        else
                            await CreateRoom();
                        break;
                    case "2":
                        if (_currentUser != null)
                            await GetRooms();
                        else
                            Console.WriteLine("无效选择，请重试");
                        break;
                    case "3":
                        if (_currentUser != null)
                            await JoinRoom();
                        else
                            Console.WriteLine("无效选择，请重试");
                        break;
                    case "4":
                        if (_currentUser != null)
                            await LeaveRoom();
                        else
                            Console.WriteLine("无效选择，请重试");
                        break;
                    case "5":
                        if (_currentUser != null)
                            await GetOnlineUsers();
                        else
                            Console.WriteLine("无效选择，请重试");
                        break;
                    case "6":
                        if (_currentUser != null)
                            Logout();
                        else
                            Console.WriteLine("无效选择，请重试");
                        break;
                    case "7":
                        await SendHeartbeat();
                        break;
                    case "8":
                        Disconnect();
                        break;
                    case "0":
                        _isRunning = false;
                        break;
                    default:
                        Console.WriteLine("无效选择，请重试");
                        break;
                }
            }
        }

        private static async Task AutoLogin()
        {
            Console.WriteLine("正在自动登录...");
            var request = new LoginRequest { DisplayName = $"Player_{DateTime.Now:HHmmss}" };
            var message = NetworkMessage.CreateSuccess("login", request);
            await _networkManager!.SendMessageAsync(message);
        }

        private static async Task CreateRoom()
        {
            if (_currentUser == null)
            {
                Console.WriteLine("请先登录");
                return;
            }

            var roomName = $"房间_{DateTime.Now:HHmmss}";
            var maxPlayers = 4;
            
            Console.WriteLine($"正在创建房间: {roomName}");
            var request = new CreateRoomRequest { RoomName = roomName, MaxPlayers = maxPlayers };
            var message = NetworkMessage.CreateSuccess("create_room", request);
            await _networkManager!.SendMessageAsync(message);
        }

        private static async Task GetRooms()
        {
            Console.WriteLine("正在获取房间列表...");
            var message = NetworkMessage.CreateSuccess("get_rooms", null);
            await _networkManager!.SendMessageAsync(message);
        }

        private static async Task JoinRoom()
        {
            if (_currentUser == null)
            {
                Console.WriteLine("请先登录");
                return;
            }

            Console.Write("请输入房间ID: ");
            var roomId = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(roomId))
            {
                Console.WriteLine("房间ID不能为空");
                return;
            }

            Console.WriteLine($"正在加入房间: {roomId}");
            var request = new JoinRoomRequest { RoomId = roomId };
            var message = NetworkMessage.CreateSuccess("join_room", request);
            await _networkManager!.SendMessageAsync(message);
        }

        private static async Task LeaveRoom()
        {
            if (_currentUser == null)
            {
                Console.WriteLine("请先登录");
                return;
            }

            if (string.IsNullOrWhiteSpace(_currentUser.CurrentRoomId))
            {
                Console.WriteLine("当前不在任何房间中");
                return;
            }

            Console.WriteLine("正在离开房间...");
            var request = new LeaveRoomRequest { RoomId = _currentUser.CurrentRoomId };
            var message = NetworkMessage.CreateSuccess("leave_room", request);
            await _networkManager!.SendMessageAsync(message);
        }

        private static async Task GetOnlineUsers()
        {
            Console.WriteLine("正在获取在线用户...");
            var message = NetworkMessage.CreateSuccess("get_online_users", null);
            await _networkManager!.SendMessageAsync(message);
        }

        private static async Task SendHeartbeat()
        {
            Console.WriteLine("正在发送心跳...");
            var message = NetworkMessage.CreateSuccess("ping", new { timestamp = DateTime.Now });
            await _networkManager!.SendMessageAsync(message);
        }

        private static void Logout()
        {
            _currentUser = null;
            Console.WriteLine("已登出");
        }

        private static void Disconnect()
        {
            _networkManager?.Disconnect();
            Console.WriteLine("已断开连接");
        }

        // 事件处理器
        private static void OnConnected()
        {
            Console.WriteLine("[事件] 已连接到服务器");
        }

        private static void OnDisconnected()
        {
            Console.WriteLine("[事件] 与服务器断开连接");
            _isRunning = false;
        }

        private static void OnConnectionStatusChanged(ConnectionStatus status)
        {
            Console.WriteLine($"[事件] 连接状态变更: {status}");
        }

        private static void OnMessageReceived(NetworkMessage? message)
        {
            Console.WriteLine($"\n[消息接收] 类型: {message.Type}, 成功: {message.Success}");
            
            if (!message.Success)
            {
                Console.WriteLine($"错误: {message.Error}");
            }
        }

        // 消息处理器
        private static void OnLoginResponse(NetworkMessage? message)
        {
            if (message.Success && message.Data != null)
            {
                var userInfoJson = JsonSerializer.Serialize(message.Data);
                _currentUser = JsonSerializer.Deserialize<UserInfo>(userInfoJson);
                Console.WriteLine($"登录成功！欢迎 {_currentUser?.DisplayName}");
            }
            else
            {
                Console.WriteLine($"登录失败: {message.Error}");
            }
        }

        private static void OnCreateRoomResponse(NetworkMessage? message)
        {
            if (message.Success && message.Data != null)
            {
                var roomInfoJson = JsonSerializer.Serialize(message.Data);
                var roomInfo = JsonSerializer.Deserialize<RoomInfo>(roomInfoJson);
                Console.WriteLine($"房间创建成功！");
                Console.WriteLine($"房间ID: {roomInfo?.Id}");
                Console.WriteLine($"房间名称: {roomInfo?.Name}");
                Console.WriteLine($"当前玩家: {roomInfo?.CurrentPlayers}/{roomInfo?.MaxPlayers}");
                
                // 更新当前用户的房间信息
                if (_currentUser != null)
                {
                    _currentUser.CurrentRoomId = roomInfo?.Id;
                }
            }
            else
            {
                Console.WriteLine($"创建房间失败: {message.Error}");
            }
        }

        private static void OnJoinRoomResponse(NetworkMessage? message)
        {
            if (message.Success && message.Data != null)
            {
                var roomInfoJson = JsonSerializer.Serialize(message.Data);
                var roomInfo = JsonSerializer.Deserialize<RoomInfo>(roomInfoJson);
                Console.WriteLine($"成功加入房间！");
                Console.WriteLine($"房间名称: {roomInfo?.Name}");
                Console.WriteLine($"创建者: {roomInfo?.CreatorName}");
                Console.WriteLine($"玩家列表: {string.Join(", ", roomInfo?.PlayerNames ?? new List<string>())}");
                
                // 更新当前用户的房间信息
                if (_currentUser != null)
                {
                    _currentUser.CurrentRoomId = roomInfo?.Id;
                }
            }
            else
            {
                Console.WriteLine($"加入房间失败: {message.Error}");
            }
        }

        private static void OnLeaveRoomResponse(NetworkMessage? message)
        {
            if (message.Success)
            {
                Console.WriteLine("已离开房间");
                if (_currentUser != null)
                {
                    _currentUser.CurrentRoomId = null;
                }
            }
            else
            {
                Console.WriteLine($"离开房间失败: {message.Error}");
            }
        }

        private static void OnGetRoomsResponse(NetworkMessage? message)
        {
            if (message.Success && message.Data != null)
            {
                var roomsJson = JsonSerializer.Serialize(message.Data);
                var rooms = JsonSerializer.Deserialize<List<RoomInfo>>(roomsJson);
                Console.WriteLine("房间列表:");
                if (rooms?.Count > 0)
                {
                    foreach (var room in rooms)
                    {
                        Console.WriteLine($"  ID: {room.Id}");
                        Console.WriteLine($"  名称: {room.Name}");
                        Console.WriteLine($"  创建者: {room.CreatorName}");
                        Console.WriteLine($"  玩家: {room.CurrentPlayers}/{room.MaxPlayers}");
                        Console.WriteLine($"  玩家列表: {string.Join(", ", room.PlayerNames)}");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("  暂无房间");
                }
            }
            else
            {
                Console.WriteLine($"获取房间列表失败: {message.Error}");
            }
        }

        private static void OnGetOnlineUsersResponse(NetworkMessage? message)
        {
            if (message.Success && message.Data != null)
            {
                var usersJson = JsonSerializer.Serialize(message.Data);
                var users = JsonSerializer.Deserialize<List<UserInfo>>(usersJson);
                Console.WriteLine("在线用户:");
                if (users?.Count > 0)
                {
                    foreach (var user in users)
                    {
                        Console.WriteLine($"  {user.DisplayName} (ID: {user.Id})");
                    }
                }
                else
                {
                    Console.WriteLine("  暂无在线用户");
                }
            }
            else
            {
                Console.WriteLine($"获取在线用户失败: {message.Error}");
            }
        }

        private static void OnPongResponse(NetworkMessage? message)
        {
            Console.WriteLine("服务器响应ping");
        }
    }
} 