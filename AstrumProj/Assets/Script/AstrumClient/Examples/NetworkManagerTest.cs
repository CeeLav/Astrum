using UnityEngine;
using Astrum.Client.Managers;

namespace Astrum.Client.Examples
{
    /// <summary>
    /// NetworkManager测试脚本
    /// </summary>
    public class NetworkManagerTest : MonoBehaviour
    {
        [Header("测试设置")]
        public bool autoConnect = true;
        public string serverAddress = "127.0.0.1";
        public int serverPort = 8888;
        
        private NetworkManager networkManager;
        
        void Start()
        {
            // 获取NetworkManager实例
            networkManager = NetworkManager.Instance;
            
            // 注册事件
            networkManager.OnConnected += OnConnected;
            networkManager.OnDisconnected += OnDisconnected;
            networkManager.OnConnectionStatusChanged += OnConnectionStatusChanged;
            networkManager.OnMessageReceived += OnMessageReceived;
            
            // 注册消息处理器
            RegisterMessageHandlers();
            
            if (autoConnect)
            {
                // 自动连接
                ConnectToServer();
            }
        }
        
        void OnDestroy()
        {
            // 取消事件注册
            if (networkManager != null)
            {
                networkManager.OnConnected -= OnConnected;
                networkManager.OnDisconnected -= OnDisconnected;
                networkManager.OnConnectionStatusChanged -= OnConnectionStatusChanged;
                networkManager.OnMessageReceived -= OnMessageReceived;
            }
        }
        
        /// <summary>
        /// 连接到服务器
        /// </summary>
        public void ConnectToServer()
        {
            Debug.Log("开始连接到服务器...");
            _ = networkManager.ConnectAsync(serverAddress, serverPort);
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public void DisconnectFromServer()
        {
            Debug.Log("断开服务器连接...");
            networkManager.Disconnect();
        }
        
        /// <summary>
        /// 发送心跳
        /// </summary>
        public void SendHeartbeat()
        {
            if (networkManager.IsConnected)
            {
                var heartbeatMessage = NetworkMessage.CreateSuccess("ping", new { clientId = networkManager.ClientId });
                _ = networkManager.SendMessageAsync(heartbeatMessage);
                Debug.Log("发送心跳消息");
            }
            else
            {
                Debug.LogWarning("未连接到服务器，无法发送心跳");
            }
        }
        
        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
            networkManager.RegisterHandler("pong", OnPongReceived);
        }
        
        // 事件处理器
        private void OnConnected()
        {
            Debug.Log("[事件] 已连接到服务器");
        }
        
        private void OnDisconnected()
        {
            Debug.Log("[事件] 与服务器断开连接");
        }
        
        private void OnConnectionStatusChanged(ConnectionStatus status)
        {
            Debug.Log($"[事件] 连接状态变更: {status}");
        }
        
        private void OnMessageReceived(NetworkMessage? message)
        {
            if (message != null)
            {
                Debug.Log($"[消息接收] 类型: {message.Type}");
            }
        }
        
        // 消息处理器
        private void OnPongReceived(NetworkMessage? message)
        {
            Debug.Log("收到pong响应");
        }
        
        // GUI测试界面
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("NetworkManager 测试");
            GUILayout.Label($"连接状态: {networkManager?.ConnectionStatus}");
            GUILayout.Label($"客户端ID: {networkManager?.ClientId}");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("连接服务器"))
            {
                ConnectToServer();
            }
            
            if (GUILayout.Button("断开连接"))
            {
                DisconnectFromServer();
            }
            
            if (GUILayout.Button("发送心跳"))
            {
                SendHeartbeat();
            }
            
            GUILayout.EndArea();
        }
    }
}
