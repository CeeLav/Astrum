using System.Text.Json;
using NetworkTest.Models;

namespace NetworkTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Astrum 网络测试客户端 ===");
            Console.WriteLine("请选择测试模式:");
            Console.WriteLine("1. 原始NetworkClient测试");
            Console.WriteLine("2. NetworkManager测试");
            Console.WriteLine("0. 退出");

            Console.Write("请选择: ");
            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await RunOriginalTest();
                    break;
                case "2":
                    await RunNetworkManagerTest();
                    break;
                case "0":
                    Console.WriteLine("退出程序");
                    break;
                default:
                    Console.WriteLine("无效选择");
                    break;
            }
        }

        private static async Task RunOriginalTest()
        {
            Console.WriteLine("\n=== 运行原始NetworkClient测试 ===");
            await NetworkTest.RunOriginalTest(new string[0]);
        }

        private static async Task RunNetworkManagerTest()
        {
            Console.WriteLine("\n=== 运行NetworkManager测试 ===");
            await NetworkManagerTest.RunTest(new string[0]);
        }
    }

    // 原始测试代码保持不变
    public class NetworkTest
    {
        private static NetworkClient? _client;
        private static UserInfo? _currentUser;
        private static bool _isRunning = true;

        public static async Task RunOriginalTest(string[] args)
        {
            Console.WriteLine("=== Astrum 网络测试客户端 ===");
            Console.WriteLine("正在连接到服务器...");

            _client = new NetworkClient();
            _client.OnMessageReceived += OnMessageReceived;
            _client.OnError += OnError;
            _client.OnDisconnected += OnDisconnected;

            if (await _client.ConnectAsync())
            {
                await ShowMainMenu();
            }
            else
            {
                Console.WriteLine("无法连接到服务器，按任意键退出...");
                Console.ReadKey();
            }

            _client?.Dispose();
        }

        private static async Task ShowMainMenu()
        {
            while (_isRunning)
            {
                Console.WriteLine("\n=== 主菜单 ===");
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
            await _client!.LoginAsync("", "");
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
            await _client!.CreateRoomAsync(roomName, maxPlayers);
        }

        private static async Task GetRooms()
        {
            await _client!.GetRoomsAsync();
        }

        private static async Task JoinRoom()
        {
            if (_currentUser == null)
            {
                Console.WriteLine("请先登录");
                return;
            }

            Console.WriteLine("正在获取房间列表...");
            await _client!.GetRoomsAsync();
            
            // 这里需要等待房间列表响应，然后自动加入第一个房间
            // 暂时先提示用户手动操作
            Console.WriteLine("请先查看房间列表，然后手动输入房间ID加入");
        }

        private static async Task LeaveRoom()
        {
            if (_currentUser == null)
            {
                Console.WriteLine("请先登录");
                return;
            }

            Console.WriteLine("正在离开房间...");
            await _client!.LeaveRoomAsync("");
        }

        private static async Task GetOnlineUsers()
        {
            await _client!.GetOnlineUsersAsync();
        }

        private static void Logout()
        {
            _currentUser = null;
            Console.WriteLine("已登出");
        }

        private static void OnMessageReceived(NetworkMessage message)
        {
            Console.WriteLine($"\n[服务器响应] 类型: {message.Type}, 成功: {message.Success}");
            
            if (!message.Success)
            {
                Console.WriteLine($"错误: {message.Error}");
                return;
            }

            switch (message.Type)
            {
                case "register":
                    Console.WriteLine("注册成功！");
                    break;

                case "login":
                    if (message.Data != null)
                    {
                        var userInfoJson = JsonSerializer.Serialize(message.Data);
                        _currentUser = JsonSerializer.Deserialize<UserInfo>(userInfoJson);
                        Console.WriteLine($"登录成功！欢迎 {_currentUser?.DisplayName}");
                    }
                    break;

                case "create_room":
                    if (message.Data != null)
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
                    break;

                case "join_room":
                    if (message.Data != null)
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
                    break;

                case "leave_room":
                    Console.WriteLine("已离开房间");
                    if (_currentUser != null)
                    {
                        _currentUser.CurrentRoomId = null;
                    }
                    break;

                case "get_rooms":
                    if (message.Data != null)
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
                    break;

                case "get_online_users":
                    if (message.Data != null)
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
                    break;

                case "room_update":
                    if (message.Data != null)
                    {
                        var roomInfoJson = JsonSerializer.Serialize(message.Data);
                        var roomInfo = JsonSerializer.Deserialize<RoomInfo>(roomInfoJson);
                        Console.WriteLine($"房间更新: {roomInfo?.Name}");
                        Console.WriteLine($"当前玩家: {string.Join(", ", roomInfo?.PlayerNames ?? new List<string>())}");
                    }
                    break;

                case "pong":
                    Console.WriteLine("服务器响应ping");
                    break;

                default:
                    Console.WriteLine($"未知消息类型: {message.Type}");
                    break;
            }
        }

        private static void OnError(string error)
        {
            Console.WriteLine($"[错误] {error}");
        }

        private static void OnDisconnected()
        {
            Console.WriteLine("[连接] 与服务器断开连接");
            _isRunning = false;
        }
    }
} 