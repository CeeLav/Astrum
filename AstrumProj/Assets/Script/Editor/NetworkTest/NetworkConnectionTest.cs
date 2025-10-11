using UnityEngine;
using UnityEditor;
using System.Net.Sockets;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using MemoryPack;

namespace Astrum.Editor
{
    /// <summary>
    /// 网络连接测试工具
    /// </summary>
    public class NetworkConnectionTest : EditorWindow
    {
        private TcpClient client;
        private NetworkStream stream;
        private bool isConnected = false;
        private string logText = "";
        private Vector2 scrollPosition;
        
        [MenuItem("Astrum/Testing 测试/Network 网络测试/Simple Connection Test 简单连接测试", false, 200)]
        public static void ShowWindow()
        {
            GetWindow<NetworkConnectionTest>("网络连接测试");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("网络连接测试工具", EditorStyles.boldLabel);
            
            if (!isConnected)
            {
                if (GUILayout.Button("连接服务器"))
                {
                    ConnectToServer();
                }
            }
            else
            {
                if (GUILayout.Button("断开连接"))
                {
                    DisconnectFromServer();
                }
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("发送登录请求"))
                {
                    SendLoginRequest();
                }
                
                if (GUILayout.Button("发送心跳"))
                {
                    SendHeartbeat();
                }
            }
            
            GUILayout.Space(10);
            
            GUILayout.Label("日志输出:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            EditorGUILayout.TextArea(logText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("清空日志"))
            {
                logText = "";
            }
        }
        
        private async void ConnectToServer()
        {
            try
            {
                Log("正在连接服务器 127.0.0.1:8888...");
                
                client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", 8888);
                stream = client.GetStream();
                isConnected = true;
                
                Log("连接成功！");
                
                // 开始接收消息
                _ = ReceiveMessagesAsync();
            }
            catch (System.Exception ex)
            {
                Log($"连接失败: {ex.Message}");
            }
        }
        
        private void DisconnectFromServer()
        {
            try
            {
                stream?.Close();
                client?.Close();
                isConnected = false;
                Log("已断开连接");
            }
            catch (System.Exception ex)
            {
                Log($"断开连接时出错: {ex.Message}");
            }
        }
        
        private async void SendLoginRequest()
        {
            if (!isConnected)
            {
                Log("请先连接服务器");
                return;
            }
            
            try
            {
                var loginRequest = LoginRequest.Create();
                loginRequest.DisplayName = "测试用户_" + System.DateTime.Now.Ticks;
                
                var data = MemoryPackSerializer.Serialize(loginRequest);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
                
                Log($"已发送登录请求: {loginRequest.DisplayName}");
            }
            catch (System.Exception ex)
            {
                Log($"发送登录请求失败: {ex.Message}");
            }
        }
        
        private async void SendHeartbeat()
        {
            if (!isConnected)
            {
                Log("请先连接服务器");
                return;
            }
            
            try
            {
                var heartbeat = HeartbeatMessage.Create();
                heartbeat.Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                var data = MemoryPackSerializer.Serialize(heartbeat);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
                
                Log("已发送心跳消息");
            }
            catch (System.Exception ex)
            {
                Log($"发送心跳失败: {ex.Message}");
            }
        }
        
        private async Task ReceiveMessagesAsync()
        {
            try
            {
                var buffer = new byte[4096];
                
                while (isConnected && client?.Connected == true)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    
                    if (bytesRead > 0)
                    {
                        var data = new byte[bytesRead];
                        System.Array.Copy(buffer, data, bytesRead);
                        
                        Log($"收到 {bytesRead} 字节数据");
                        
                        // 尝试反序列化不同类型的消息
                        var messageTypes = new System.Type[]
                        {
                            typeof(LoginResponse),
                            typeof(HeartbeatResponse),
                            typeof(CreateRoomResponse),
                            typeof(JoinRoomResponse),
                            typeof(GetRoomListResponse)
                        };
                        
                        foreach (var messageType in messageTypes)
                        {
                            try
                            {
                                var message = MemoryPackSerializer.Deserialize(messageType, data);
                                if (message != null)
                                {
                                    Log($"成功反序列化消息: {message.GetType().Name}");
                                    break;
                                }
                            }
                            catch
                            {
                                // 继续尝试下一个类型
                            }
                        }
                    }
                    else
                    {
                        // 连接已关闭
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log($"接收消息时出错: {ex.Message}");
            }
            
            Log("消息接收循环结束");
        }
        
        private void Log(string message)
        {
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            logText += $"[{timestamp}] {message}\n";
            Repaint();
        }
        
        private void OnDestroy()
        {
            DisconnectFromServer();
        }
    }
}
