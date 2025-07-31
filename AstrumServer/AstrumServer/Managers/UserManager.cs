using AstrumServer.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.Sockets;

namespace AstrumServer.Managers
{
    public class UserManager
    {
        private static UserManager? _instance;
        private static readonly object _lock = new();
        
        public static UserManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new UserManager();
                    }
                }
                return _instance;
            }
        }

        private readonly Dictionary<string, User> _users = new();
        private readonly Dictionary<string, string> _clientToUser = new(); // clientId -> userId
        private readonly ILogger? _logger;
        private readonly string _usersFile = "users.json";

        private UserManager()
        {
            LoadUsers();
        }

        public void SetLogger(ILogger logger)
        {
            // 使用反射设置私有字段，因为构造函数中无法注入ILogger
            var loggerField = typeof(UserManager).GetField("_logger", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            loggerField?.SetValue(this, logger);
        }

        public User CreateUser(string? displayName = null)
        {
            var user = new User(displayName ?? "");
            _users[user.Id] = user;
            user.LastLoginAt = DateTime.Now;
            user.IsOnline = true;
            SaveUsers();
            
            _logger?.LogInformation("创建新用户: {DisplayName}, ID: {UserId}", user.DisplayName, user.Id);
            return user;
        }

        public User? GetUserById(string userId)
        {
            return _users.TryGetValue(userId, out var user) ? user : null;
        }

        public bool LoginClient(string clientId, User user, TcpClient client)
        {
            if (user == null) return false;

            user.Client = client;
            user.ClientId = clientId;
            user.IsOnline = true;
            _clientToUser[clientId] = user.Id;
            
            _logger?.LogInformation("客户端 {ClientId} 登录用户: {DisplayName}", clientId, user.DisplayName);
            return true;
        }

        public void LogoutClient(string clientId)
        {
            if (_clientToUser.TryGetValue(clientId, out var userId))
            {
                if (_users.TryGetValue(userId, out var user))
                {
                    user.IsOnline = false;
                    user.Client = null;
                    user.ClientId = null;
                    user.CurrentRoomId = null;
                }
                _clientToUser.Remove(clientId);
                _logger?.LogInformation("客户端 {ClientId} 登出", clientId);
            }
        }

        public User? GetUser(string userId)
        {
            return _users.TryGetValue(userId, out var user) ? user : null;
        }

        public User? GetUserByClientId(string clientId)
        {
            if (_clientToUser.TryGetValue(clientId, out var userId))
            {
                return GetUser(userId);
            }
            return null;
        }



        public List<User> GetOnlineUsers()
        {
            return _users.Values.Where(u => u.IsOnline).ToList();
        }

        public void UpdateUserRoom(string userId, string? roomId)
        {
            if (_users.TryGetValue(userId, out var user))
            {
                user.CurrentRoomId = roomId;
                SaveUsers();
            }
        }

        private void LoadUsers()
        {
            try
            {
                if (File.Exists(_usersFile))
                {
                    var json = File.ReadAllText(_usersFile);
                    var users = JsonSerializer.Deserialize<Dictionary<string, User>>(json);
                    if (users != null)
                    {
                        foreach (var user in users.Values)
                        {
                            user.IsOnline = false;
                            user.Client = null;
                            user.ClientId = null;
                        }
                        _users.Clear();
                        foreach (var kvp in users)
                        {
                            _users[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载用户数据失败");
            }
        }

        private void SaveUsers()
        {
            try
            {
                var usersToSave = new Dictionary<string, User>();
                foreach (var kvp in _users)
                {
                    var user = kvp.Value;
                    var userToSave = new User(user.DisplayName)
                    {
                        Id = user.Id,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt,
                        CurrentRoomId = user.CurrentRoomId
                    };
                    usersToSave[kvp.Key] = userToSave;
                }

                var json = JsonSerializer.Serialize(usersToSave, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(_usersFile, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存用户数据失败");
            }
        }
    }
} 