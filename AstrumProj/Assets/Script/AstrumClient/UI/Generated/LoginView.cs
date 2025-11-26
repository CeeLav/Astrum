// 此文件用于编写UI逻辑代码
// 第一次生成后，可以手动编辑，不会被重新生成覆盖

using UnityEngine;
using UnityEngine.UI;
using System;
using Astrum.Client.UI.Core;
using Astrum.Client.Managers;
using Astrum.Client.Managers.GameModes;
using Astrum.Client.Core;
using Astrum.Generated;
using Astrum.Network.Generated;
using Astrum.CommonBase;
using Astrum.Client.MessageHandlers;
using AstrumClient.MonitorTools;

namespace Astrum.Client.UI.Generated
{
    /// <summary>
    /// LoginView 逻辑部分
    /// 用于编写UI的业务逻辑代码
    /// </summary>
    public partial class LoginView : UIBase
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
        protected override void OnInitialize()
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

            // 绑定回放按钮事件
            if (replayButtonButton != null)
            {
                replayButtonButton.onClick.AddListener(OnReplayButtonClicked);
            }

            // 订阅登录事件
            SubscribeToLoginEvents();
            
            // 加载缓存的回放文件地址
            LoadCachedReplayFilePath();

            // 初始化UI状态
            UpdateConnectionStatus("未连接");
            UpdateConnectButtonText("连接服务器");
            
            // 初始化单机按钮文本
            if (singleButtonTextText != null)
            {
                singleButtonTextText.text = "单机游戏";
            }
            MonitorManager.Register(this); // 注册到全局监控
        }

        /// <summary>
        /// 显示时的回调
        /// </summary>
        protected override void OnShow()
        {
            // 更新连接状态显示
            UpdateConnectionStatus();
        }

        /// <summary>
        /// 隐藏时的回调
        /// </summary>
        protected override void OnHide()
        {
            // 取消订阅登录事件
            UnsubscribeFromLoginEvents();
        }

        #endregion

        #region Event Subscriptions

        /// <summary>
        /// 订阅登录事件
        /// </summary>
        private void SubscribeToLoginEvents()
        {
            EventSystem.Instance.Subscribe<LoginStateChangedEventData>(OnLoginStateChanged);
            EventSystem.Instance.Subscribe<LoginErrorEventData>(OnLoginError);
        }

        /// <summary>
        /// 取消订阅登录事件
        /// </summary>
        private void UnsubscribeFromLoginEvents()
        {
            EventSystem.Instance.Unsubscribe<LoginStateChangedEventData>(OnLoginStateChanged);
            EventSystem.Instance.Unsubscribe<LoginErrorEventData>(OnLoginError);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 登录状态变化事件处理（纯UI更新）
        /// </summary>
        private void OnLoginStateChanged(LoginStateChangedEventData eventData)
        {
            Debug.Log($"LoginView: 登录状态变化 - ConnectionState: {eventData.ConnectionState}, MatchState: {eventData.MatchState}");
            
            // 根据连接状态更新 UI
            int connState = eventData.ConnectionState;
            if (connState == 0) // Disconnected
            {
                UpdateConnectionStatus("未连接");
                UpdateConnectButtonText("连接服务器");
                SetConnectButtonInteractable(true);
                isConnecting = false;
            }
            else if (connState == 1) // Connecting
            {
                UpdateConnectionStatus("正在连接...");
                UpdateConnectButtonText("连接中...");
                SetConnectButtonInteractable(false);
                isConnecting = true;
            }
            else if (connState == 2) // Connected
            {
                UpdateConnectionStatus("已连接，正在登录...");
                UpdateConnectButtonText("连接中...");
                SetConnectButtonInteractable(false);
            }
            else if (connState == 3) // LoggingIn
            {
                UpdateConnectionStatus("正在登录...");
                UpdateConnectButtonText("登录中...");
                SetConnectButtonInteractable(false);
            }
            else if (connState == 4) // LoggedIn
            {
                currentMatchState = MatchState.LoggedIn;
                UpdateConnectionStatus("登录成功");
                UpdateConnectButtonText("快速联机");
                SetConnectButtonInteractable(true);
            }
            
            // 根据匹配状态更新 UI
            int matchState = eventData.MatchState;
            if (matchState == 0) // None
            {
                // 匹配状态为 None，不做额外处理，保持当前连接状态的提示
                if (connState != 4) // 如果不是已登录状态
                {
                    currentMatchState = MatchState.None;
                }
            }
            else if (matchState == 1) // Matching
            {
                currentMatchState = MatchState.Matching;
                UpdateConnectionStatus("正在匹配中...");
                UpdateConnectButtonText("取消匹配");
                SetConnectButtonInteractable(true);
            }
            else if (matchState == 2) // MatchFound
            {
                currentMatchState = MatchState.MatchFound;
                UpdateConnectionStatus("匹配成功！正在进入房间...");
                UpdateConnectButtonText("进入游戏");
                SetConnectButtonInteractable(false);
            }
        }

        /// <summary>
        /// 登录错误事件处理（纯UI更新）
        /// </summary>
        private void OnLoginError(LoginErrorEventData eventData)
        {
            Debug.LogError($"LoginView: 登录错误 - {eventData.ErrorMessage}");
            UpdateConnectionStatus($"错误: {eventData.ErrorMessage}");
            UpdateConnectButtonText("重新连接");
            SetConnectButtonInteractable(true);
            isConnecting = false;
            currentMatchState = MatchState.None;
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// 连接按钮点击事件
        /// </summary>
        private void OnConnectButtonClicked()
        {
            // 从 GameDirector 获取当前的 LoginGameMode
            var gameMode = GameDirector.Instance?.CurrentGameMode as LoginGameMode;
            if (gameMode == null)
            {
                Debug.LogError("LoginView: 无法从 GameDirector 获取 LoginGameMode");
                return;
            }

            // 根据当前状态执行不同操作
            if (currentMatchState == MatchState.None && !isConnecting)
            {
                gameMode.ConnectToServer();
            }
            else if (currentMatchState == MatchState.LoggedIn)
            {
                gameMode.SendQuickMatchRequest();
            }
            else if (currentMatchState == MatchState.Matching)
            {
                gameMode.SendCancelMatchRequest();
            }
        }
        
        /// <summary>
        /// 单机游戏按钮点击事件
        /// </summary>
        private void OnSinglePlayButtonClicked()
        {
            // 从 GameDirector 获取当前的 LoginGameMode
            var gameMode = GameDirector.Instance?.CurrentGameMode as LoginGameMode;
            if (gameMode == null)
            {
                Debug.LogError("LoginView: 无法从 GameDirector 获取 LoginGameMode");
                return;
            }

            gameMode.StartSinglePlayerGame();
        }

        /// <summary>
        /// 回放按钮点击事件
        /// </summary>
        private void OnReplayButtonClicked()
        {
            // 获取输入的文件路径
            string filePath = inputFieldInputField?.text?.Trim();
            
            if (string.IsNullOrEmpty(filePath))
            {
                UpdateConnectionStatus("错误: 请输入回放文件路径");
                return;
            }
            
            // 保存到缓存
            SaveReplayFilePath(filePath);
            
            // 从 GameDirector 获取当前的 LoginGameMode
            var gameMode = GameDirector.Instance?.CurrentGameMode as LoginGameMode;
            if (gameMode == null)
            {
                Debug.LogError("LoginView: 无法从 GameDirector 获取 LoginGameMode");
                return;
            }
            
            // 启动回放
            gameMode.StartReplay(filePath);
        }

        /// <summary>
        /// 加载缓存的回放文件地址
        /// </summary>
        private void LoadCachedReplayFilePath()
        {
            if (inputFieldInputField != null)
            {
                string cachedPath = PlayerPrefs.GetString("ReplayFilePath", "");
                if (!string.IsNullOrEmpty(cachedPath))
                {
                    inputFieldInputField.text = cachedPath;
                }
            }
        }

        /// <summary>
        /// 保存回放文件地址到缓存
        /// </summary>
        private void SaveReplayFilePath(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                PlayerPrefs.SetString("ReplayFilePath", filePath);
                PlayerPrefs.Save();
            }
        }

        #endregion

        #region UI Update Methods

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
                    var networkManager = NetworkManager.Instance;
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

        #endregion
    }
}
