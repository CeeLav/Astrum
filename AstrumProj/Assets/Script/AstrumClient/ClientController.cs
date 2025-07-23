using UnityEngine;
using System.Threading.Tasks;
using Astrum.CommonBase;

namespace Astrum.Client
{
    /// <summary>
    /// 客户端控制器 - Unity集成组件
    /// 作为客户端在Unity中的主要入口点
    /// </summary>
    public class ClientController : MonoBehaviour
    {
        [Header("服务器设置")]
        [SerializeField] private string serverAddress = "127.0.0.1";
        [SerializeField] private int serverPort = 8888;
        [SerializeField] private string playerName = "Player";

        [Header("自动连接")]
        [SerializeField] private bool autoConnectOnStart = false;
        [SerializeField] private bool autoAuthenticateOnConnect = true;

        [Header("调试")]
        [SerializeField] private bool enableDebugLogs = true;

        private ClientManager _clientManager;
        private bool _isInitialized = false;

        /// <summary>
        /// 客户端管理器实例
        /// </summary>
        public ClientManager ClientManager => _clientManager;

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => _clientManager?.IsConnected ?? false;

        /// <summary>
        /// 是否在游戏中
        /// </summary>
        public bool IsInGame => _clientManager?.IsInGame ?? false;

        /// <summary>
        /// 当前客户端状态
        /// </summary>
        public ClientState CurrentState => _clientManager?.CurrentState ?? ClientState.Disconnected;

        private void Awake()
        {
            // 确保只有一个实例
            if (FindObjectsOfType<ClientController>().Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            InitializeClient();
        }

        private void Start()
        {
            if (autoConnectOnStart)
            {
                ConnectToServer();
            }
        }

        private void OnDestroy()
        {
            DisconnectFromServer();
        }

        /// <summary>
        /// 初始化客户端
        /// </summary>
        private void InitializeClient()
        {
            if (_isInitialized) return;

            try
            {
                // 初始化客户端管理器
                _clientManager = ClientManager.Instance;
                
                // 设置服务器配置
                _clientManager.ServerAddress = serverAddress;
                _clientManager.ServerPort = serverPort;
                _clientManager.PlayerName = playerName;

                // 注册事件
                _clientManager.OnStateChanged += OnClientStateChanged;
                _clientManager.OnConnected += OnClientConnected;
                _clientManager.OnDisconnected += OnClientDisconnected;
                _clientManager.OnAuthenticated += OnClientAuthenticated;
                _clientManager.OnEnteredGame += OnClientEnteredGame;

                _isInitialized = true;
                
                if (enableDebugLogs)
                {
                    Debug.Log("客户端控制器初始化完成");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"客户端控制器初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public async void ConnectToServer()
        {
            if (!_isInitialized || _clientManager == null)
            {
                Debug.LogError("客户端未初始化");
                return;
            }

            try
            {
                bool connected = await _clientManager.ConnectToServerAsync();
                if (connected && autoAuthenticateOnConnect)
                {
                    await AuthenticatePlayer();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"连接服务器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 断开服务器连接
        /// </summary>
        public async void DisconnectFromServer()
        {
            if (_clientManager != null)
            {
                await _clientManager.DisconnectAsync();
            }
        }

        /// <summary>
        /// 认证玩家
        /// </summary>
        public async Task<bool> AuthenticatePlayer()
        {
            if (_clientManager == null) return false;

            try
            {
                return await _clientManager.AuthenticateAsync(playerName);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"玩家认证失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 请求加入游戏
        /// </summary>
        public async Task<bool> JoinGame()
        {
            if (_clientManager == null) return false;

            try
            {
                return await _clientManager.RequestJoinGameAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"加入游戏失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送玩家输入
        /// </summary>
        public async void SendPlayerInput(PlayerInput input)
        {
            if (_clientManager != null && _clientManager.IsInGame)
            {
                await _clientManager.SendPlayerInputAsync(input);
            }
        }

        /// <summary>
        /// 客户端状态变更回调
        /// </summary>
        private void OnClientStateChanged(ClientState oldState, ClientState newState)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"客户端状态变更: {oldState} -> {newState}");
            }
        }

        /// <summary>
        /// 客户端连接成功回调
        /// </summary>
        private void OnClientConnected()
        {
            if (enableDebugLogs)
            {
                Debug.Log("客户端已连接到服务器");
            }
        }

        /// <summary>
        /// 客户端断开连接回调
        /// </summary>
        private void OnClientDisconnected()
        {
            if (enableDebugLogs)
            {
                Debug.Log("客户端已断开服务器连接");
            }
        }

        /// <summary>
        /// 客户端认证成功回调
        /// </summary>
        private void OnClientAuthenticated()
        {
            if (enableDebugLogs)
            {
                Debug.Log("客户端认证成功");
            }
        }

        /// <summary>
        /// 客户端进入游戏回调
        /// </summary>
        private void OnClientEnteredGame()
        {
            if (enableDebugLogs)
            {
                Debug.Log("客户端已进入游戏");
            }
        }

        /// <summary>
        /// 设置服务器地址
        /// </summary>
        public void SetServerAddress(string address)
        {
            serverAddress = address;
            if (_clientManager != null)
            {
                _clientManager.ServerAddress = address;
            }
        }

        /// <summary>
        /// 设置服务器端口
        /// </summary>
        public void SetServerPort(int port)
        {
            serverPort = port;
            if (_clientManager != null)
            {
                _clientManager.ServerPort = port;
            }
        }

        /// <summary>
        /// 设置玩家名称
        /// </summary>
        public void SetPlayerName(string name)
        {
            playerName = name;
            if (_clientManager != null)
            {
                _clientManager.PlayerName = name;
            }
        }

        /// <summary>
        /// 获取客户端统计信息
        /// </summary>
        public string GetClientStats()
        {
            if (_clientManager == null) return "客户端未初始化";

            return $"状态: {_clientManager.CurrentState}\n" +
                   $"服务器: {_clientManager.ServerAddress}:{_clientManager.ServerPort}\n" +
                   $"玩家: {_clientManager.PlayerName}\n" +
                   $"已连接: {_clientManager.IsConnected}\n" +
                   $"游戏中: {_clientManager.IsInGame}";
        }

        /// <summary>
        /// 在Inspector中显示调试信息
        /// </summary>
        private void OnGUI()
        {
            if (!enableDebugLogs) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Astrum Client Debug Info", GUI.skin.box);
            GUILayout.Label($"状态: {CurrentState}");
            GUILayout.Label($"已连接: {IsConnected}");
            GUILayout.Label($"游戏中: {IsInGame}");
            GUILayout.Label($"玩家: {playerName}");
            GUILayout.EndArea();
        }
    }
} 