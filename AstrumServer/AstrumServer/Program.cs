using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Astrum.LogicCore;

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
        private readonly GameStateManager _gameManager;
        private readonly object _clientsLock = new();

        public GameServer(ILogger<GameServer> logger)
        {
            _logger = logger;
            _gameManager = GameStateManager.Instance;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // 初始化游戏逻辑
                _gameManager.Initialize();
                
                // 设置服务器配置
                _gameManager.Config.ServerPort = 8888;
                _gameManager.Config.MaxPlayers = 16;
                _gameManager.Config.DebugMode = true;
                
                _logger.LogInformation("LogicCore游戏逻辑已初始化");
                _logger.LogInformation("游戏配置: 最大玩家数={MaxPlayers}, 端口={Port}", 
                    _gameManager.Config.MaxPlayers, _gameManager.Config.ServerPort);

                // 监听状态改变
                _gameManager.OnStateChanged += (previous, current) =>
                {
                    _logger.LogInformation("游戏状态从 {Previous} 变为 {Current}", previous, current);
                };

                // 监听玩家事件
                _gameManager.PlayerManager.OnPlayerJoined += (player) =>
                {
                    _logger.LogInformation("玩家 {PlayerName} (ID: {PlayerId}) 加入了游戏", player.Name, player.Id);
                };

                _gameManager.PlayerManager.OnPlayerLeft += (player) =>
                {
                    _logger.LogInformation("玩家 {PlayerName} (ID: {PlayerId}) 离开了游戏", player.Name, player.Id);
                };

                _gameManager.PlayerManager.OnPlayerPositionChanged += (player) =>
                {
                    _logger.LogDebug("玩家 {PlayerName} 位置更新: {Position}", player.Name, player.Position);
                };

                _listener = new TcpListener(IPAddress.Any, _gameManager.Config.ServerPort);
                _listener.Start();
                _logger.LogInformation("Astrum游戏服务器已启动，监听端口: {Port}", _gameManager.Config.ServerPort);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    _ = HandleClientAsync(client, stoppingToken);
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

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            var clientId = Guid.NewGuid().ToString()[..8];
            
            _logger.LogInformation("客户端 {ClientId} 已连接，当前连接数: {ClientCount}", 
                clientId, _gameManager.PlayerManager.PlayerCount);

            try
            {
                var stream = client.GetStream();
                var buffer = new byte[1024];
                var messageBuffer = new StringBuilder();
                var isTelnet = false;

                // 发送欢迎消息
                var welcomeMessage = $"欢迎连接到{_gameManager.Config.GameName}游戏服务器！\r\n输入 'help' 查看可用命令\r\n";
                var welcomeBytes = Encoding.UTF8.GetBytes(welcomeMessage);
                await stream.WriteAsync(welcomeBytes, 0, welcomeBytes.Length, cancellationToken);

                // 添加玩家到游戏逻辑
                if (!_gameManager.PlayerManager.AddPlayer(clientId, $"Player_{clientId}"))
                {
                    var errorMessage = "服务器已满，无法加入游戏\r\n";
                    var errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                    await stream.WriteAsync(errorBytes, 0, errorBytes.Length, cancellationToken);
                    return;
                }

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
                                    
                                    // 处理命令
                                    var response = ProcessCommand(clientId, message);
                                    var responseBytes = Encoding.UTF8.GetBytes(response + "\r\n");
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
                // 从游戏逻辑中移除玩家
                _gameManager.PlayerManager.RemovePlayer(clientId);
                client.Close();
                _logger.LogInformation("客户端 {ClientId} 已断开连接，当前连接数: {ClientCount}", 
                    clientId, _gameManager.PlayerManager.PlayerCount);
            }
        }

        private string ProcessCommand(string clientId, string command)
        {
            command = command.Trim().ToLower();
            
            return command switch
            {
                "help" => "可用命令:\r\n  help - 显示此帮助\r\n  status - 显示服务器状态\r\n  players - 显示玩家列表\r\n  move <x> <y> <z> - 移动玩家\r\n  jump - 跳跃\r\n  quit - 退出连接",
                "status" => $"服务器状态: {_gameManager.CurrentState}\r\n当前连接数: {_gameManager.PlayerManager.PlayerCount}\r\n最大玩家数: {_gameManager.Config.MaxPlayers}\r\n启动时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "players" => GetPlayersList(),
                "jump" => HandlePlayerJump(clientId),
                _ when command.StartsWith("move ") => HandlePlayerMove(clientId, command),
                "quit" => "再见！",
                _ => $"服务器收到: {command}"
            };
        }

        private string GetPlayersList()
        {
            var players = _gameManager.PlayerManager.Players;
            if (players.Count == 0)
                return "当前没有玩家在线";

            var playerList = new StringBuilder("玩家列表:\r\n");
            foreach (var player in players.Values)
            {
                playerList.AppendLine($"  {player.Name} (ID: {player.Id}) - 位置: {player.Position}");
            }
            return playerList.ToString();
        }

        private string HandlePlayerJump(string clientId)
        {
            _gameManager.PlayerManager.HandlePlayerJump(clientId);
            var player = _gameManager.PlayerManager.GetPlayer(clientId);
            return $"玩家 {player?.Name} 跳跃了！位置: {player?.Position}";
        }

        private string HandlePlayerMove(string clientId, string command)
        {
            try
            {
                var parts = command.Split(' ');
                if (parts.Length >= 4)
                {
                    float x = float.Parse(parts[1]);
                    float y = float.Parse(parts[2]);
                    float z = float.Parse(parts[3]);
                    
                    var input = new Vector3(x, y, z);
                    _gameManager.PlayerManager.HandlePlayerInput(clientId, input);
                    
                    var player = _gameManager.PlayerManager.GetPlayer(clientId);
                    return $"玩家 {player?.Name} 移动到: {player?.Position}";
                }
                return "用法: move <x> <y> <z>";
            }
            catch (Exception ex)
            {
                return $"移动失败: {ex.Message}";
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("正在关闭服务器...");
            await base.StopAsync(cancellationToken);
        }
    }
}
