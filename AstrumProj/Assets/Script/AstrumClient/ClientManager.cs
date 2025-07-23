using System;
using System.Threading.Tasks;
using Astrum.LogicCore;
using Astrum.Network;
using Astrum.CommonBase;

namespace Astrum.Client
{
    /// <summary>
    /// 客户端状态枚举
    /// </summary>
    public enum ClientState
    {
        Disconnected,    // 未连接
        Connecting,      // 连接中
        Connected,       // 已连接
        Authenticating,  // 认证中
        Authenticated,   // 已认证
        InGame,         // 游戏中
        Disconnecting    // 断开连接中
    }

    /// <summary>
    /// 客户端主控制器
    /// 负责管理客户端状态、网络连接和游戏逻辑
    /// </summary>
    public class ClientManager : LazySingleton<ClientManager>, IInitializable
    {
        private NetworkManager _networkManager;
        private GameStateManager _gameStateManager;
        private PlayerManager _playerManager;
        private ClientState _currentState = ClientState.Disconnected;
        private string _serverAddress = "127.0.0.1";
        private int _serverPort = 8888;
        private string _playerName = "";
        private bool _isInitialized = false;

        /// <summary>
        /// 创建实例的静态方法
        /// </summary>
        protected static ClientManager CreateInstanceStatic()
        {
            return new ClientManager();
        }

        /// <summary>
        /// 创建实例的抽象方法（已废弃，使用 CreateInstanceStatic）
        /// </summary>
        [Obsolete("使用 CreateInstanceStatic 方法")]
        protected override ClientManager CreateInstance()
        {
            return new ClientManager();
        }

        /// <summary>
        /// 当前客户端状态
        /// </summary>
        public ClientState CurrentState
        {
            get => _currentState;
            private set
            {
                if (_currentState != value)
                {
                    var oldState = _currentState;
                    _currentState = value;
                    OnStateChanged?.Invoke(oldState, _currentState);
                    Logger.Instance.Info($"客户端状态变更: {oldState} -> {_currentState}");
                }
            }
        }

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string ServerAddress
        {
            get => _serverAddress;
            set => _serverAddress = value;
        }

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int ServerPort
        {
            get => _serverPort;
            set => _serverPort = value;
        }

        /// <summary>
        /// 玩家名称
        /// </summary>
        public string PlayerName
        {
            get => _playerName;
            set => _playerName = value;
        }

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => CurrentState == ClientState.Connected || CurrentState == ClientState.Authenticated || CurrentState == ClientState.InGame;

        /// <summary>
        /// 是否在游戏中
        /// </summary>
        public bool IsInGame => CurrentState == ClientState.InGame;

        /// <summary>
        /// 网络管理器
        /// </summary>
        public NetworkManager NetworkManager => _networkManager;

        /// <summary>
        /// 游戏状态管理器
        /// </summary>
        public GameStateManager GameStateManager => _gameStateManager;

        /// <summary>
        /// 玩家管理器
        /// </summary>
        public PlayerManager PlayerManager => _playerManager;

        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event Action<ClientState, ClientState> OnStateChanged;

        /// <summary>
        /// 连接成功事件
        /// </summary>
        public event Action OnConnected;

        /// <summary>
        /// 断开连接事件
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 认证成功事件
        /// </summary>
        public event Action OnAuthenticated;

        /// <summary>
        /// 进入游戏事件
        /// </summary>
        public event Action OnEnteredGame;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 初始化客户端管理器
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                // 初始化网络管理器
                _networkManager = NetworkManager.Instance;
                _networkManager.OnConnectionStateChanged += OnNetworkConnectionStateChanged;
                _networkManager.OnMessageReceived += OnMessageReceived;

                // 初始化游戏状态管理器
                _gameStateManager = GameStateManager.Instance;

                // 初始化玩家管理器
                _playerManager = new PlayerManager();

                // 注册事件监听
                EventSystem.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);

                _isInitialized = true;
                Logger.Instance.Info("客户端管理器初始化完成");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"客户端管理器初始化失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public async Task<bool> ConnectToServerAsync()
        {
            if (CurrentState != ClientState.Disconnected)
            {
                Logger.Instance.Warning("客户端已连接或正在连接中");
                return false;
            }

            try
            {
                CurrentState = ClientState.Connecting;
                Logger.Instance.Info($"正在连接到服务器 {_serverAddress}:{_serverPort}...");

                bool connected = await _networkManager.ConnectAsync(_serverAddress, _serverPort);
                if (connected)
                {
                    CurrentState = ClientState.Connected;
                    OnConnected?.Invoke();
                    Logger.Instance.Info("成功连接到服务器");
                    return true;
                }
                else
                {
                    CurrentState = ClientState.Disconnected;
                    Logger.Instance.Error("连接服务器失败");
                    return false;
                }
            }
            catch (Exception ex)
            {
                CurrentState = ClientState.Disconnected;
                Logger.Instance.Error($"连接服务器时发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 断开与服务器的连接
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (CurrentState == ClientState.Disconnected)
            {
                return;
            }

            try
            {
                CurrentState = ClientState.Disconnecting;
                Logger.Instance.Info("正在断开服务器连接...");

                await _networkManager.DisconnectAsync();
                CurrentState = ClientState.Disconnected;
                OnDisconnected?.Invoke();
                Logger.Instance.Info("已断开服务器连接");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"断开连接时发生错误: {ex.Message}");
                CurrentState = ClientState.Disconnected;
            }
        }

        /// <summary>
        /// 发送玩家认证
        /// </summary>
        public async Task<bool> AuthenticateAsync(string playerName)
        {
            if (CurrentState != ClientState.Connected)
            {
                Logger.Instance.Warning("客户端未连接到服务器");
                return false;
            }

            try
            {
                CurrentState = ClientState.Authenticating;
                _playerName = playerName;

                var authMessage = new DataMessage
                {
                    Type = MessageType.Data,
                    DataType = "PlayerAuth",
                    Data = new { playerName = _playerName }
                };

                await _networkManager.SendMessageAsync(authMessage);
                Logger.Instance.Info($"正在认证玩家: {_playerName}");
                return true;
            }
            catch (Exception ex)
            {
                CurrentState = ClientState.Connected;
                Logger.Instance.Error($"发送认证消息失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送玩家输入
        /// </summary>
        public async Task SendPlayerInputAsync(PlayerInput input)
        {
            if (!IsInGame)
            {
                Logger.Instance.Warning("客户端不在游戏中，无法发送输入");
                return;
            }

            try
            {
                var inputMessage = new DataMessage
                {
                    Type = MessageType.Data,
                    DataType = "PlayerInput",
                    Data = input
                };

                await _networkManager.SendMessageAsync(inputMessage);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"发送玩家输入失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 请求加入游戏
        /// </summary>
        public async Task<bool> RequestJoinGameAsync()
        {
            if (CurrentState != ClientState.Authenticated)
            {
                Logger.Instance.Warning("客户端未认证，无法加入游戏");
                return false;
            }

            try
            {
                var joinMessage = new DataMessage
                {
                    Type = MessageType.Data,
                    DataType = "JoinGame",
                    Data = new { playerName = _playerName }
                };

                await _networkManager.SendMessageAsync(joinMessage);
                Logger.Instance.Info("正在请求加入游戏...");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"请求加入游戏失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 网络连接状态变化回调
        /// </summary>
        private void OnNetworkConnectionStateChanged(ConnectionState oldState, ConnectionState newState)
        {
            Logger.Instance.Info($"网络连接状态变更: {oldState} -> {newState}");
            
            switch (newState)
            {
                case ConnectionState.Connected:
                    Logger.Instance.Info("网络连接已建立");
                    break;
                case ConnectionState.Disconnected:
                    CurrentState = ClientState.Disconnected;
                    Logger.Instance.Info("网络连接已断开");
                    break;
                case ConnectionState.Error:
                    Logger.Instance.Error("网络连接发生错误");
                    break;
            }
        }

        /// <summary>
        /// 接收网络消息回调
        /// </summary>
        private void OnMessageReceived(NetworkMessage message)
        {
            try
            {
                switch (message)
                {
                    case DataMessage dataMessage:
                        HandleDataMessage(dataMessage);
                        break;
                    case ErrorMessage errorMessage:
                        Logger.Instance.Error($"服务器错误: {errorMessage.ErrorText}");
                        break;
                    case InfoMessage infoMessage:
                        Logger.Instance.Info($"服务器信息: {infoMessage.Info}");
                        break;
                    default:
                        Logger.Instance.Warning($"收到未知消息类型: {message.GetType().Name}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"处理网络消息时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理数据消息
        /// </summary>
        private void HandleDataMessage(DataMessage message)
        {
            switch (message.DataType)
            {
                case "AuthResult":
                    HandleAuthResult(message.Data);
                    break;
                case "GameState":
                    HandleGameStateUpdate(message.Data);
                    break;
                case "PlayerData":
                    HandlePlayerDataUpdate(message.Data);
                    break;
                case "GameStart":
                    HandleGameStart(message.Data);
                    break;
                case "GameEnd":
                    HandleGameEnd(message.Data);
                    break;
                default:
                    Logger.Instance.Warning($"未知的数据消息类型: {message.DataType}");
                    break;
            }
        }

        /// <summary>
        /// 处理认证结果
        /// </summary>
        private void HandleAuthResult(object data)
        {
            // 这里应该解析认证结果
            // 简化处理，假设认证成功
            CurrentState = ClientState.Authenticated;
            OnAuthenticated?.Invoke();
            Logger.Instance.Info("玩家认证成功");
        }

        /// <summary>
        /// 处理游戏状态更新
        /// </summary>
        private void HandleGameStateUpdate(object data)
        {
            // 更新游戏状态
            if (data is string stateString && Enum.TryParse<GameState>(stateString, out var gameState))
            {
                _gameStateManager.SetGameState(gameState);
            }
        }

        /// <summary>
        /// 处理玩家数据更新
        /// </summary>
        private void HandlePlayerDataUpdate(object data)
        {
            // 更新玩家数据
            // 这里需要根据实际的数据结构进行解析
            Logger.Instance.Info("收到玩家数据更新");
        }

        /// <summary>
        /// 处理游戏开始
        /// </summary>
        private void HandleGameStart(object data)
        {
            CurrentState = ClientState.InGame;
            OnEnteredGame?.Invoke();
            Logger.Instance.Info("游戏开始");
        }

        /// <summary>
        /// 处理游戏结束
        /// </summary>
        private void HandleGameEnd(object data)
        {
            CurrentState = ClientState.Authenticated;
            Logger.Instance.Info("游戏结束");
        }

        /// <summary>
        /// 游戏状态变更回调
        /// </summary>
        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            Logger.Instance.Info($"游戏状态变更: {evt.OldState} -> {evt.NewState}");
        }

        /// <summary>
        /// 销毁客户端管理器
        /// </summary>
        public void Destroy()
        {
            if (!_isInitialized) return;

            try
            {
                // 取消事件订阅
                if (EventSystem.Instance != null)
                {
                    EventSystem.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
                }

                // 断开网络连接
                if (_networkManager != null)
                {
                    _networkManager.OnConnectionStateChanged -= OnNetworkConnectionStateChanged;
                    _networkManager.OnMessageReceived -= OnMessageReceived;
                }

                _isInitialized = false;
                Logger.Instance.Info("客户端管理器已销毁");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"销毁客户端管理器时发生错误: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 玩家输入数据
    /// </summary>
    [Serializable]
    public class PlayerInput
    {
        public float Horizontal { get; set; }
        public float Vertical { get; set; }
        public bool Jump { get; set; }
        public bool Attack { get; set; }
        public bool Interact { get; set; }
        public float MouseX { get; set; }
        public float MouseY { get; set; }
    }
}
