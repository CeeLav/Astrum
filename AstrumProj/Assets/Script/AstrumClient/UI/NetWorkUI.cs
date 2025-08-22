using System.Collections.Generic;
using Astrum.Client.Managers;
using Astrum.Client.Core;
using Astrum.Generated;
using Astrum.Network.Generated;
using UnityEngine;
using UnityEngine.UI;

namespace Astrum.Client
{
    public class NetWorkUI : MonoBehaviour
    {
        [Header("UI组件")]
        public GameObject First;
        public GameObject Second;
        public Text SelectRoom;
        
        [Header("房间系统UI")]
        public InputField RoomNameInput;
        public InputField MaxPlayersInput;
        public Button CreateRoomButton;
        public Button GetRoomsButton;
        public Button LeaveRoomButton;
        public Text RoomListText;
        public Text CurrentRoomText;
        
        [Header("状态显示")]
        public Text ConnectionStatus;
        public Text LoginStatus;
        public Button LoginButton;
        public Button ReconnectButton;
        
        private bool isLoggingIn = false;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // 订阅网络事件
            if (GameApplication.Instance?.NetworkManager != null)
            {
                GameApplication.Instance.NetworkManager.OnConnectionStatusChanged += OnConnectionStatusChanged;
            }
            
            if (GameApplication.Instance?.GamePlayManager != null)
            {
                GameApplication.Instance.GamePlayManager.OnNetworkError += OnNetworkError;
                GameApplication.Instance.GamePlayManager.OnUserLoggedIn += OnUserLoggedIn;
                GameApplication.Instance.GamePlayManager.OnRoomCreated += OnRoomCreated;
                GameApplication.Instance.GamePlayManager.OnRoomJoined += OnRoomJoined;
                GameApplication.Instance.GamePlayManager.OnRoomLeft += OnRoomLeft;
                GameApplication.Instance.GamePlayManager.OnRoomListUpdated += OnRoomListUpdated;
            }
            
            // 初始化UI状态
            UpdateUIState();
        }

        // Update is called once per frame
        void Update()
        {
            // 定期更新网络状态显示
            if (Time.frameCount % 60 == 0) // 每60帧更新一次
            {
                UpdateUIState();
            }
        }
        
        private void OnDestroy()
        {
            // 取消订阅事件
            if (GameApplication.Instance?.NetworkManager != null)
            {
                GameApplication.Instance.NetworkManager.OnConnectionStatusChanged -= OnConnectionStatusChanged;
            }
            
            if (GameApplication.Instance?.GamePlayManager != null)
            {
                GameApplication.Instance.GamePlayManager.OnNetworkError -= OnNetworkError;
                GameApplication.Instance.GamePlayManager.OnUserLoggedIn -= OnUserLoggedIn;
                GameApplication.Instance.GamePlayManager.OnRoomCreated -= OnRoomCreated;
                GameApplication.Instance.GamePlayManager.OnRoomJoined -= OnRoomJoined;
                GameApplication.Instance.GamePlayManager.OnRoomLeft -= OnRoomLeft;
                GameApplication.Instance.GamePlayManager.OnRoomListUpdated -= OnRoomListUpdated;
            }
        }

        public async void Login()
        {
            if (isLoggingIn)
            {
                Debug.Log("正在登录中，请稍候...");
                return;
            }
            
            try
            {
                isLoggingIn = true;
                UpdateLoginStatus("正在登录...");
                
                // 调用GamePlayManager的连接并登录方法
                bool success = await GamePlayManager.Instance.ConnectAndLoginAsync();
                
                if (success)
                {
                    UpdateLoginStatus("登录成功");
                    First.SetActive(false);
                    Second.SetActive(true);
                    
                    // 自动获取房间列表
                    await GetRooms();
                }
                else
                {
                    UpdateLoginStatus("登录失败，请重试");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"登录失败: {ex.Message}");
                UpdateLoginStatus($"登录失败: {ex.Message}");
            }
            finally
            {
                isLoggingIn = false;
            }
        }

        public async void CreateRoom()
        {
            if (!GamePlayManager.Instance.IsLoggedIn)
            {
                Debug.LogWarning("请先登录");
                return;
            }
            
            if (string.IsNullOrEmpty(RoomNameInput?.text))
            {
                Debug.LogWarning("请输入房间名称");
                return;
            }
            
            string roomName = RoomNameInput.text;
            int maxPlayers = 4;
            
            if (int.TryParse(MaxPlayersInput?.text, out int inputMaxPlayers) && inputMaxPlayers > 0)
            {
                maxPlayers = inputMaxPlayers;
            }
            
            Debug.Log($"创建房间: {roomName}, 最大玩家: {maxPlayers}");
            
            bool success = await GamePlayManager.Instance.CreateRoomAsync(roomName, maxPlayers);
            if (success)
            {
                Debug.Log("创建房间请求已发送");
            }
            else
            {
                Debug.LogError("创建房间失败");
            }
        }
        
        public async void JoinRoom()
        {
            if (!GamePlayManager.Instance.IsLoggedIn)
            {
                Debug.LogWarning("请先登录");
                return;
            }
            
            if (string.IsNullOrEmpty(SelectRoom.text))
            {
                Debug.LogWarning("请输入房间ID");
                return;
            }
            
            Debug.Log($"加入房间: {SelectRoom.text}");
            
            bool success = await GamePlayManager.Instance.JoinRoomAsync(SelectRoom.text);
            if (success)
            {
                Debug.Log("加入房间请求已发送");
            }
            else
            {
                Debug.LogError("加入房间失败");
            }
        }
        
        public async void LeaveRoom()
        {
            if (!GamePlayManager.Instance.IsLoggedIn)
            {
                Debug.LogWarning("请先登录");
                return;
            }
            
            if (!GamePlayManager.Instance.IsInRoom)
            {
                Debug.LogWarning("当前不在房间中");
                return;
            }
            
            Debug.Log("离开房间");
            
            bool success = await GamePlayManager.Instance.LeaveRoomAsync();
            if (success)
            {
                Debug.Log("离开房间请求已发送");
            }
            else
            {
                Debug.LogError("离开房间失败");
            }
        }
        
        public async System.Threading.Tasks.Task GetRooms()
        {
            if (!GamePlayManager.Instance.IsLoggedIn)
            {
                Debug.LogWarning("请先登录");
                return;
            }
            
            Debug.Log("获取房间列表");
            
            bool success = await GamePlayManager.Instance.GetRoomsAsync();
            if (success)
            {
                Debug.Log("获取房间列表请求已发送");
            }
            else
            {
                Debug.LogError("获取房间列表失败");
            }
        }
        
        public async void Reconnect()
        {
            try
            {
                UpdateConnectionStatus("正在重连...");
                
                if (GameApplication.Instance?.GamePlayManager != null)
                {
                    bool success = await GameApplication.Instance.GamePlayManager.ForceReconnect();
                    if (success)
                    {
                        UpdateConnectionStatus("重连成功");
                    }
                    else
                    {
                        UpdateConnectionStatus("重连失败");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"重连失败: {ex.Message}");
                UpdateConnectionStatus($"重连失败: {ex.Message}");
            }
        }
        
        private void OnConnectionStatusChanged(ConnectionStatus status)
        {
            UpdateConnectionStatus($"连接状态: {status}");
        }
        
        private void OnNetworkError(string error)
        {
            UpdateLoginStatus($"网络错误: {error}");
        }
        
        private void OnUserLoggedIn(UserInfo userInfo)
        {
            UpdateLoginStatus($"用户 {userInfo.DisplayName} 登录成功");
        }
        
        private void OnRoomCreated(RoomInfo roomInfo)
        {
            Debug.Log($"房间创建成功: {roomInfo.Name} (ID: {roomInfo.Id})");
            UpdateCurrentRoomInfo(roomInfo);
        }
        
        private void OnRoomJoined(RoomInfo roomInfo)
        {
            Debug.Log($"房间加入成功: {roomInfo.Name} (ID: {roomInfo.Id})");
            UpdateCurrentRoomInfo(roomInfo);
        }
        
        private void OnRoomLeft()
        {
            Debug.Log("已离开房间");
            UpdateCurrentRoomInfo(null);
        }
        
        private void OnRoomListUpdated(List<RoomInfo> rooms)
        {
            Debug.Log($"房间列表更新: {rooms.Count} 个房间");
            UpdateRoomListText(rooms);
        }
        
        private void UpdateConnectionStatus(string status)
        {
            if (ConnectionStatus != null)
            {
                ConnectionStatus.text = status;
            }
        }
        
        private void UpdateLoginStatus(string status)
        {
            if (LoginStatus != null)
            {
                LoginStatus.text = status;
            }
        }
        
        private void UpdateUIState()
        {
            // 更新按钮状态
            if (LoginButton != null)
            {
                LoginButton.interactable = !isLoggingIn;
            }
            
            if (ReconnectButton != null)
            {
                ReconnectButton.interactable = !isLoggingIn;
            }
            
            // 更新房间系统按钮状态
            if (CreateRoomButton != null)
            {
                CreateRoomButton.interactable = GamePlayManager.Instance?.IsLoggedIn == true && 
                                               GamePlayManager.Instance?.IsInRoom == false;
            }
            
            if (GetRoomsButton != null)
            {
                GetRoomsButton.interactable = GamePlayManager.Instance?.IsLoggedIn == true;
            }
            
            if (LeaveRoomButton != null)
            {
                LeaveRoomButton.interactable = GamePlayManager.Instance?.IsInRoom == true;
            }
            
            // 更新连接状态显示
            if (GameApplication.Instance?.GamePlayManager != null)
            {
                string networkStatus = GameApplication.Instance.GamePlayManager.GetNetworkStatus();
                UpdateConnectionStatus(networkStatus);
            }
            
            // 更新登录状态显示
            if (GamePlayManager.Instance != null)
            {
                if (GamePlayManager.Instance.IsLoggedIn)
                {
                    UpdateLoginStatus($"已登录: {GamePlayManager.Instance.CurrentUser?.DisplayName}");
                }
                else
                {
                    UpdateLoginStatus("未登录");
                }
            }
        }
        
        private void UpdateCurrentRoomInfo(RoomInfo roomInfo)
        {
            if (CurrentRoomText != null)
            {
                if (roomInfo != null)
                {
                    CurrentRoomText.text = $"当前房间: {roomInfo.Name} (ID: {roomInfo.Id})\n" +
                                          $"玩家: {roomInfo.CurrentPlayers}/{roomInfo.MaxPlayers}\n" +
                                          $"创建者: {roomInfo.CreatorName}";
                }
                else
                {
                    CurrentRoomText.text = "当前不在房间中";
                }
            }
        }
        
        private void UpdateRoomListText(List<RoomInfo> rooms)
        {
            if (RoomListText != null)
            {
                if (rooms != null && rooms.Count > 0)
                {
                    var roomTexts = new List<string>();
                    foreach (var room in rooms)
                    {
                        roomTexts.Add($"房间: {room.Name} (ID: {room.Id})\n" +
                                     $"玩家: {room.CurrentPlayers}/{room.MaxPlayers}\n" +
                                     $"创建者: {room.CreatorName}\n" +
                                     $"---");
                    }
                    RoomListText.text = string.Join("\n", roomTexts);
                }
                else
                {
                    RoomListText.text = "暂无可用房间";
                }
            }
        }
    }
}
