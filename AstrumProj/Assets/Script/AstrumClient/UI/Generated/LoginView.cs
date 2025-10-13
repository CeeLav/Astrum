// 此文件用于编写UI逻辑代码
// 第一次生成后，可以手动编辑，不会被重新生成覆盖

using UnityEngine;
using UnityEngine.UI;
using System;
using Astrum.Client.UI.Core;
using Astrum.Client.Managers;
using Astrum.Client.Core;
using Astrum.Generated;
using Astrum.Network.Generated;

namespace Astrum.Client.UI.Generated
{
    /// <summary>
    /// LoginView 逻辑部分
    /// 用于编写UI的业务逻辑代码
    /// </summary>
    public partial class LoginView
    {
        #region Fields

        private bool isConnecting = false;
        private string serverAddress = "127.0.0.1";
        private int serverPort = 8888;
        
        // 匹配状态
        private enum MatchState
        {
            None,           // 未登录/未连接
            LoggedIn,       // 已登录，未匹配
            Matching,       // 正在匹配中
            MatchFound      // 匹配成功
        }
        private MatchState currentMatchState = MatchState.None;

        #endregion

        #region Virtual Methods

        /// <summary>
        /// 初始化完成后的回调
        /// </summary>
        protected virtual void OnInitialize()
        {
            // 绑定按钮事件
            if (connectButtonButton != null)
            {
                connectButtonButton.onClick.AddListener(OnConnectButtonClicked);
            }
            
            // 绑定单机游戏按钮事件
            if (singlePlayButtonButton != null)
            {
                singlePlayButtonButton.onClick.AddListener(OnSinglePlayButtonClicked);
            }

            // 订阅网络事件
            SubscribeToNetworkEvents();

            // 初始化UI状态
            UpdateConnectionStatus("未连接");
            UpdateConnectButtonText("连接服务器");
            
            // 初始化单机按钮文本
            if (singleButtonTextText != null)
            {
                singleButtonTextText.text = "单机游戏";
            }
        }

        /// <summary>
        /// 显示时的回调
        /// </summary>
        protected virtual void OnShow()
        {
            // 更新连接状态显示
            UpdateConnectionStatus();
        }

        /// <summary>
        /// 隐藏时的回调
        /// </summary>
        protected virtual void OnHide()
        {
            // 取消订阅网络事件
            UnsubscribeFromNetworkEvents();
        }

        #endregion

        #region Network Event Handlers

        /// <summary>
        /// 订阅网络事件
        /// </summary>
        private void SubscribeToNetworkEvents()
        {
            var networkManager = GameApplication.Instance?.NetworkManager;
            if (networkManager != null)
            {
                networkManager.OnConnected += OnConnected;
                networkManager.OnDisconnected += OnDisconnected;
                networkManager.OnConnectionStatusChanged += OnConnectionStatusChanged;
                networkManager.OnConnectResponse += OnConnectResponse;
                networkManager.OnLoginResponse += OnLoginResponse;
                
                // 订阅快速匹配事件
                networkManager.OnQuickMatchResponse += OnQuickMatchResponse;
                networkManager.OnCancelMatchResponse += OnCancelMatchResponse;
                networkManager.OnMatchFoundNotification += OnMatchFoundNotification;
                networkManager.OnMatchTimeoutNotification += OnMatchTimeoutNotification;
            }
        }

        /// <summary>
        /// 取消订阅网络事件
        /// </summary>
        private void UnsubscribeFromNetworkEvents()
        {
            var networkManager = GameApplication.Instance?.NetworkManager;
            if (networkManager != null)
            {
                networkManager.OnConnected -= OnConnected;
                networkManager.OnDisconnected -= OnDisconnected;
                networkManager.OnConnectionStatusChanged -= OnConnectionStatusChanged;
                networkManager.OnConnectResponse -= OnConnectResponse;
                networkManager.OnLoginResponse -= OnLoginResponse;
                
                // 取消订阅快速匹配事件
                networkManager.OnQuickMatchResponse -= OnQuickMatchResponse;
                networkManager.OnCancelMatchResponse -= OnCancelMatchResponse;
                networkManager.OnMatchFoundNotification -= OnMatchFoundNotification;
                networkManager.OnMatchTimeoutNotification -= OnMatchTimeoutNotification;
            }
        }

        /// <summary>
        /// 连接成功事件
        /// </summary>
        private void OnConnected()
        {
            Debug.Log("LoginView: 连接成功");
            isConnecting = false;
            UpdateConnectionStatus("已连接");
            UpdateConnectButtonText("已连接");
            SetConnectButtonInteractable(false);
        }

        /// <summary>
        /// 断开连接事件
        /// </summary>
        private void OnDisconnected()
        {
            Debug.Log("LoginView: 连接断开");
            isConnecting = false;
            currentMatchState = MatchState.None; // 重置匹配状态
            UpdateConnectionStatus("连接断开");
            UpdateConnectButtonText("重新连接");
            SetConnectButtonInteractable(true);
        }

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        private void OnConnectionStatusChanged(ConnectionStatus status)
        {
            Debug.Log($"LoginView: 连接状态变化: {status}");
            switch (status)
            {
                case ConnectionStatus.Connected:
                    OnConnected();
                    break;
                case ConnectionStatus.Disconnected:
                    OnDisconnected();
                    break;
                case ConnectionStatus.Connecting:
                    UpdateConnectionStatus("连接中...");
                    UpdateConnectButtonText("连接中...");
                    SetConnectButtonInteractable(false);
                    break;
            }
        }

        /// <summary>
        /// 连接响应事件
        /// </summary>
        private void OnConnectResponse(ConnectResponse response)
        {
            Debug.Log($"LoginView: 收到连接响应: {response.success}, {response.message}");
            if (response.success)
            {
                UpdateConnectionStatus($"连接成功: {response.message}");
                UpdateConnectButtonText("已连接");
                SetConnectButtonInteractable(false);
                
                // 连接成功后自动发送登录请求
                SendLoginRequest();
            }
            else
            {
                UpdateConnectionStatus($"连接失败: {response.message}");
                UpdateConnectButtonText("重新连接");
                SetConnectButtonInteractable(true);
            }
            isConnecting = false;
        }

        /// <summary>
        /// 登录响应事件
        /// </summary>
        private void OnLoginResponse(LoginResponse response)
        {
            Debug.Log($"LoginView: 收到登录响应: {response.Success}, {response.Message}");
            if (response.Success)
            {
                // 更新状态为已登录
                currentMatchState = MatchState.LoggedIn;
                UpdateConnectionStatus($"登录成功: {response.Message}");
                UpdateConnectButtonText("快速联机");
                SetConnectButtonInteractable(true); // 允许点击快速匹配
                
                // 注意：不再自动切换到房间列表，而是等待玩家点击快速匹配
            }
            else
            {
                UpdateConnectionStatus($"登录失败: {response.Message}");
                UpdateConnectButtonText("重新登录");
                SetConnectButtonInteractable(true);
                currentMatchState = MatchState.None;
            }
        }
        
        /// <summary>
        /// 快速匹配响应事件
        /// </summary>
        private void OnQuickMatchResponse(QuickMatchResponse response)
        {
            Debug.Log($"LoginView: 收到快速匹配响应: {response.Success}, {response.Message}");
            if (response.Success)
            {
                currentMatchState = MatchState.Matching;
                UpdateConnectionStatus($"匹配中... (队列位置: {response.QueuePosition + 1}/{response.QueueSize})");
                UpdateConnectButtonText("取消匹配");
                SetConnectButtonInteractable(true);
            }
            else
            {
                UpdateConnectionStatus($"匹配失败: {response.Message}");
                UpdateConnectButtonText("快速联机");
                SetConnectButtonInteractable(true);
                currentMatchState = MatchState.LoggedIn;
            }
        }
        
        /// <summary>
        /// 取消匹配响应事件
        /// </summary>
        private void OnCancelMatchResponse(CancelMatchResponse response)
        {
            Debug.Log($"LoginView: 收到取消匹配响应: {response.Success}, {response.Message}");
            if (response.Success)
            {
                currentMatchState = MatchState.LoggedIn;
                UpdateConnectionStatus($"已取消匹配: {response.Message}");
                UpdateConnectButtonText("快速联机");
                SetConnectButtonInteractable(true);
            }
        }
        
        /// <summary>
        /// 匹配成功通知事件
        /// </summary>
        private void OnMatchFoundNotification(MatchFoundNotification notification)
        {
            Debug.Log($"LoginView: 匹配成功！房间ID: {notification.Room?.Id}");
            currentMatchState = MatchState.MatchFound;
            UpdateConnectionStatus($"匹配成功！正在进入房间...");
            UpdateConnectButtonText("匹配成功");
            SetConnectButtonInteractable(false);
            
            // 切换到房间列表或直接进入游戏
            try
            {
                var uiManager = GameApplication.Instance?.UIManager;
                if (uiManager != null)
                {
                    Debug.Log("LoginView: 匹配成功，切换到房间列表");
                    uiManager.ShowUI("RoomList");
                }
                else
                {
                    Debug.LogError("LoginView: 无法获取UIManager");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoginView: 切换到房间列表失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 匹配超时通知事件
        /// </summary>
        private void OnMatchTimeoutNotification(MatchTimeoutNotification notification)
        {
            Debug.Log($"LoginView: 匹配超时: {notification.Message}");
            currentMatchState = MatchState.LoggedIn;
            UpdateConnectionStatus($"匹配超时: {notification.Message} (等待时长: {notification.WaitTimeSeconds}秒)");
            UpdateConnectButtonText("快速联机");
            SetConnectButtonInteractable(true);
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// 连接按钮点击事件
        /// </summary>
        private void OnConnectButtonClicked()
        {
            // 根据当前匹配状态执行不同的操作
            switch (currentMatchState)
            {
                case MatchState.None:
                    // 未连接状态，执行连接
                    if (isConnecting)
                    {
                        Debug.Log("LoginView: 正在连接中，请稍候...");
                        return;
                    }
                    
                    // 设置联机模式
                    Astrum.Client.Core.GameConfig.Instance.SetSinglePlayerMode(false);
                    
                    // 提前创建 MultiplayerGameMode 以注册网络消息
                    GameApplication.Instance?.GamePlayManager.PrepareGameMode();

                    ConnectToServer();
                    break;
                    
                case MatchState.LoggedIn:
                    // 已登录状态，执行快速匹配
                    SendQuickMatchRequest();
                    break;
                    
                case MatchState.Matching:
                    // 匹配中状态，执行取消匹配
                    SendCancelMatchRequest();
                    break;
                    
                case MatchState.MatchFound:
                    // 匹配成功状态，不允许操作
                    Debug.Log("LoginView: 匹配已成功，正在进入房间");
                    break;
            }
        }
        
        /// <summary>
        /// 单机游戏按钮点击事件
        /// </summary>
        private void OnSinglePlayButtonClicked()
        {
            try
            {
                Debug.Log("LoginView: 启动单机游戏");
                
                // 1. 设置单机模式
                Astrum.Client.Core.GameConfig.Instance.SetSinglePlayerMode(true);
                
                // 2. 启动单机游戏
                var gamePlayManager = GameApplication.Instance?.GamePlayManager;
                if (gamePlayManager != null)
                {
                    // 关闭 Login UI
                    var uiManager = GameApplication.Instance?.UIManager;
                    uiManager?.HideUI("Login");
                    
                    // 启动单机游戏（场景名称可以从配置读取，这里暂时硬编码）
                    gamePlayManager.StartSinglePlayerGame("DungeonsGame");
                    
                    Debug.Log("LoginView: 单机游戏启动成功");
                }
                else
                {
                    Debug.LogError("LoginView: GamePlayManager 不存在，无法启动单机游戏");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoginView: 启动单机游戏失败 - {ex.Message}");
            }
        }

        #endregion

        #region Business Logic

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public async void ConnectToServer()
        {
            try
            {
                isConnecting = true;
                UpdateConnectionStatus("正在连接...");
                UpdateConnectButtonText("连接中...");
                SetConnectButtonInteractable(false);

                Debug.Log($"LoginView: 开始连接到服务器 {serverAddress}:{serverPort}");

                var networkManager = GameApplication.Instance?.NetworkManager;
                if (networkManager == null)
                {
                    Debug.LogError("LoginView: NetworkManager不存在");
                    UpdateConnectionStatus("错误: 网络管理器不存在");
                    UpdateConnectButtonText("重新连接");
                    SetConnectButtonInteractable(true);
                    isConnecting = false;
                    return;
                }

                // 连接到服务器
                var channelId = await networkManager.ConnectAsync(serverAddress, serverPort);
                Debug.Log($"LoginView: 连接请求已发送，ChannelId: {channelId}");

                // 发送连接请求
                var connectRequest = ConnectRequest.Create();
                networkManager.Send(connectRequest);
                Debug.Log("LoginView: 连接请求已发送");
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoginView: 连接失败: {ex.Message}");
                UpdateConnectionStatus($"连接失败: {ex.Message}");
                UpdateConnectButtonText("重新连接");
                SetConnectButtonInteractable(true);
                isConnecting = false;
            }
        }

        /// <summary>
        /// 发送登录请求
        /// </summary>
        private void SendLoginRequest()
        {
            try
            {
                var networkManager = GameApplication.Instance?.NetworkManager;
                if (networkManager == null)
                {
                    Debug.LogError("LoginView: NetworkManager不存在，无法发送登录请求");
                    return;
                }

                // 创建登录请求
                var loginRequest = LoginRequest.Create();
                loginRequest.DisplayName = "Player_" + UnityEngine.Random.Range(1000, 9999);
                
                // 发送登录请求
                networkManager.Send(loginRequest);
                Debug.Log($"LoginView: 登录请求已发送，显示名称: {loginRequest.DisplayName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoginView: 发送登录请求失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新连接状态显示
        /// </summary>
        private void UpdateConnectionStatus(string status = null)
        {
            if (connectionStatusText != null)
            {
                if (status != null)
                {
                    connectionStatusText.text = $"连接状态：{status}";
                }
                else
                {
                    var networkManager = GameApplication.Instance?.NetworkManager;
                    if (networkManager != null && networkManager.IsConnected())
                    {
                        connectionStatusText.text = "连接状态：已连接";
                    }
                    else
                    {
                        connectionStatusText.text = "连接状态：未连接";
                    }
                }
            }
        }

        /// <summary>
        /// 更新连接按钮文本
        /// </summary>
        private void UpdateConnectButtonText(string text)
        {
            if (buttonTextText != null)
            {
                buttonTextText.text = text;
            }
        }

        /// <summary>
        /// 设置连接按钮是否可交互
        /// </summary>
        private void SetConnectButtonInteractable(bool interactable)
        {
            if (connectButtonButton != null)
            {
                connectButtonButton.interactable = interactable;
            }
        }

        /// <summary>
        /// 设置服务器地址和端口
        /// </summary>
        public void SetServerInfo(string address, int port)
        {
            serverAddress = address;
            serverPort = port;
        }
        
        /// <summary>
        /// 发送快速匹配请求
        /// </summary>
        private void SendQuickMatchRequest()
        {
            try
            {
                var networkManager = GameApplication.Instance?.NetworkManager;
                if (networkManager == null)
                {
                    Debug.LogError("LoginView: NetworkManager不存在，无法发送快速匹配请求");
                    return;
                }
                
                // 创建快速匹配请求
                var quickMatchRequest = QuickMatchRequest.Create();
                quickMatchRequest.Timestamp = Astrum.CommonBase.TimeInfo.Instance.ClientNow();
                
                // 发送请求
                networkManager.Send(quickMatchRequest);
                Debug.Log("LoginView: 快速匹配请求已发送");
                
                // 更新UI状态
                UpdateConnectionStatus("正在请求快速匹配...");
                UpdateConnectButtonText("请求中...");
                SetConnectButtonInteractable(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoginView: 发送快速匹配请求失败: {ex.Message}");
                UpdateConnectionStatus($"快速匹配请求失败: {ex.Message}");
                UpdateConnectButtonText("快速联机");
                SetConnectButtonInteractable(true);
            }
        }
        
        /// <summary>
        /// 发送取消匹配请求
        /// </summary>
        private void SendCancelMatchRequest()
        {
            try
            {
                var networkManager = GameApplication.Instance?.NetworkManager;
                if (networkManager == null)
                {
                    Debug.LogError("LoginView: NetworkManager不存在，无法发送取消匹配请求");
                    return;
                }
                
                // 创建取消匹配请求
                var cancelMatchRequest = CancelMatchRequest.Create();
                cancelMatchRequest.Timestamp = Astrum.CommonBase.TimeInfo.Instance.ClientNow();
                
                // 发送请求
                networkManager.Send(cancelMatchRequest);
                Debug.Log("LoginView: 取消匹配请求已发送");
                
                // 更新UI状态
                UpdateConnectionStatus("正在取消匹配...");
                UpdateConnectButtonText("取消中...");
                SetConnectButtonInteractable(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoginView: 发送取消匹配请求失败: {ex.Message}");
                UpdateConnectionStatus($"取消匹配请求失败: {ex.Message}");
                UpdateConnectButtonText("取消匹配");
                SetConnectButtonInteractable(true);
            }
        }

        #endregion
    }
}
