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

            // 订阅网络事件
            SubscribeToNetworkEvents();

            // 初始化UI状态
            UpdateConnectionStatus("未连接");
            UpdateConnectButtonText("连接服务器");
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
            }
            else
            {
                UpdateConnectionStatus($"连接失败: {response.message}");
                UpdateConnectButtonText("重新连接");
                SetConnectButtonInteractable(true);
            }
            isConnecting = false;
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// 连接按钮点击事件
        /// </summary>
        private void OnConnectButtonClicked()
        {
            if (isConnecting)
            {
                Debug.Log("LoginView: 正在连接中，请稍候...");
                return;
            }

            ConnectToServer();
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

        #endregion
    }
}
