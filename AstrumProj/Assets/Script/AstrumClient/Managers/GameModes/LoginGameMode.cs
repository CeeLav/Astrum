using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.Client.Data;
using Astrum.Client.Managers;
using Astrum.Client.MessageHandlers;
using Astrum.Client.UI.Generated;
using Astrum.Network.Generated;
using Astrum.Generated;
using Astrum.LogicCore.Core;
using Astrum.View.Core;
using UnityEngine;
using AstrumClient.MonitorTools;
using GameConfig = Astrum.Client.Core.GameConfig;

namespace Astrum.Client.Managers.GameModes
{
    /// <summary>
    /// 登录游戏模式 - 管理登录、连接、匹配等登录相关逻辑
    /// </summary>
    [MonitorTarget]
    public class LoginGameMode : BaseGameMode
    {
        /// <summary>
        /// 游戏模式类型
        /// </summary>
        public override GameModeType ModeType => GameModeType.Login;
        
        #region 状态枚举

        /// <summary>
        /// 连接状态
        /// </summary>
        private enum ConnectionState
        {
            Disconnected,    // 未连接
            Connecting,      // 连接中
            Connected,       // 已连接
            LoggingIn,       // 登录中
            LoggedIn         // 已登录
        }

        /// <summary>
        /// 匹配状态
        /// </summary>
        private enum MatchState
        {
            None,           // 未匹配
            Matching,       // 匹配中
            MatchFound      // 匹配成功
        }

        #endregion

        #region 属性

        // 继承自 BaseGameMode
        public override Room MainRoom { get; set; }        // 登录模式不使用
        public override Stage MainStage { get; set; }      // 登录模式不使用
        public override long PlayerId { get; set; }        // 登录模式不使用
        public override string ModeName => "Login";
        public override bool IsRunning { get; set; }

        // 内部状态
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private MatchState _matchState = MatchState.None;
        
        // 重连状态
        private bool _isReconnecting = false;

        // 服务器配置
        private string _serverAddress = "127.0.0.1";
        private int _serverPort = 8888;

        #endregion

        #region 生命周期方法

        /// <summary>
        /// 初始化登录模式
        /// </summary>
        public override void Initialize()
        {
            //ASLogger.Instance.Info("LoginGameMode: 初始化登录模式");

            try
            {
                // 订阅网络事件
                SubscribeEvents();

                // 显示登录 UI
                ShowLoginUI();

                // 触发状态流转：Initializing -> Loading -> Ready
                TriggerStateEnter();
                
                //ASLogger.Instance.Info("LoginGameMode: 初始化完成");

                MonitorManager.Register(this); // 注册到全局监控
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LoginGameMode: 初始化失败 - {ex.Message}");
                ChangeState(GameModeState.Finished);
                throw;
            }
        }

        /// <summary>
        /// 更新（登录模式通常不需要每帧更新）
        /// </summary>
        public override void Update(float deltaTime)
        {
            // 登录模式所有逻辑都是事件驱动的，不需要每帧更新
        }

        /// <summary>
        /// 关闭登录模式
        /// </summary>
        public override void Shutdown()
        {
            ASLogger.Instance.Info("LoginGameMode: 关闭登录模式");
            ChangeState(GameModeState.Ending);

            try
            {
                // 取消订阅事件
                UnsubscribeEvents();
                
                // 清理状态
                _connectionState = ConnectionState.Disconnected;
                _matchState = MatchState.None;
                _isReconnecting = false;

                ChangeState(GameModeState.Finished);
                ASLogger.Instance.Info("LoginGameMode: 关闭完成");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LoginGameMode: 关闭失败 - {ex.Message}");
            }
        }

        #endregion

        #region 事件订阅/取消订阅

        /// <summary>
        /// 订阅网络事件
        /// </summary>
        private void SubscribeEvents()
        {
            // 订阅连接响应事件
            EventSystem.Instance.Subscribe<ConnectResponseEventData>(OnConnectResponse);

            // 订阅底层连接状态（使用 EventSystem）
            EventSystem.Instance.Subscribe<NetworkDisconnectedEventData>(OnNetworkDisconnected);
            EventSystem.Instance.Subscribe<NetworkConnectionStatusChangedEventData>(OnNetworkConnectionStatusChanged);
            
            // 订阅用户登录事件（使用 EventSystem）
            EventSystem.Instance.Subscribe<UserLoggedInEventData>(OnUserLoggedIn);
            EventSystem.Instance.Subscribe<UserLoginErrorEventData>(OnUserLoginError);
            
            // 订阅快速匹配相关事件
            EventSystem.Instance.Subscribe<QuickMatchResponse>(OnQuickMatchResponse);
            EventSystem.Instance.Subscribe<CancelMatchResponse>(OnCancelMatchResponse);
            EventSystem.Instance.Subscribe<MatchTimeoutNotification>(OnMatchTimeoutNotification);
            
            ASLogger.Instance.Info("LoginGameMode: 已订阅网络事件");
        }

        /// <summary>
        /// 取消订阅网络事件
        /// </summary>
        private void UnsubscribeEvents()
        {
            // 取消订阅连接响应事件
            EventSystem.Instance.Unsubscribe<ConnectResponseEventData>(OnConnectResponse);

            // 取消订阅底层连接状态（使用 EventSystem）
            EventSystem.Instance.Unsubscribe<NetworkDisconnectedEventData>(OnNetworkDisconnected);
            EventSystem.Instance.Unsubscribe<NetworkConnectionStatusChangedEventData>(OnNetworkConnectionStatusChanged);
            
            // 取消订阅用户登录事件（使用 EventSystem）
            EventSystem.Instance.Unsubscribe<UserLoggedInEventData>(OnUserLoggedIn);
            EventSystem.Instance.Unsubscribe<UserLoginErrorEventData>(OnUserLoginError);
            
            // 取消订阅快速匹配相关事件
            EventSystem.Instance.Unsubscribe<QuickMatchResponse>(OnQuickMatchResponse);
            EventSystem.Instance.Unsubscribe<CancelMatchResponse>(OnCancelMatchResponse);
            // 不再需要取消订阅 MatchFoundNotification
            // EventSystem.Instance.Unsubscribe<MatchFoundNotification>(OnMatchFoundNotification);
            EventSystem.Instance.Unsubscribe<MatchTimeoutNotification>(OnMatchTimeoutNotification);
            
            ASLogger.Instance.Info("LoginGameMode: 已取消订阅网络事件");
        }

        #endregion

        #region 配置管理

        /// <summary>
        /// 创建默认配置
        /// </summary>
        protected override GameModeConfig CreateDefaultConfig()
        {
            return new GameModeConfig
            {
                ModeName = "Login",
                AutoSave = false,
                UpdateInterval = 0.016f,
                CustomSettings = new Dictionary<string, object>
                {
                    { "ServerAddress", _serverAddress },
                    { "ServerPort", _serverPort },
                    { "ConnectionTimeout", 10 },
                    { "LoginTimeout", 10 }
                }
            };
        }

        #endregion

        #region 核心业务方法

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public async void ConnectToServer()
        {
            if (_connectionState != ConnectionState.Disconnected)
            {
                ASLogger.Instance.Warning("LoginGameMode: 已在连接或已连接状态");
                return;
            }

            try
            {
                _connectionState = ConnectionState.Connecting;
                PublishLoginStateChanged();

                ASLogger.Instance.Info($"LoginGameMode: 开始连接到服务器 {_serverAddress}:{_serverPort}");

                var networkManager = NetworkManager.Instance;
                if (networkManager == null)
                {
                    throw new Exception("NetworkManager 不存在");
                }

                // 连接到服务器
                var channelId = await networkManager.ConnectAsync(_serverAddress, _serverPort);
                ASLogger.Instance.Info($"LoginGameMode: 连接请求已发送，ChannelId: {channelId}");

                // 发送连接请求
                var connectRequest = ConnectRequest.Create();
                networkManager.Send(connectRequest);
                ASLogger.Instance.Info("LoginGameMode: 连接请求已发送");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LoginGameMode: 连接失败 - {ex.Message}");
                _connectionState = ConnectionState.Disconnected;
                PublishLoginStateChanged();
                PublishLoginError($"连接失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送登录请求
        /// </summary>
        private void SendLoginRequest()
        {
            if (_connectionState != ConnectionState.Connected)
            {
                ASLogger.Instance.Warning("LoginGameMode: 未连接，无法发送登录请求");
                return;
            }

            try
            {
                _connectionState = ConnectionState.LoggingIn;
                PublishLoginStateChanged();

                var networkManager = NetworkManager.Instance;
                if (networkManager == null)
                {
                    throw new Exception("NetworkManager 不存在");
                }

                // 创建登录请求
                var clientInstanceId = ClientInstanceIdManager.GetOrCreateInstanceId();
                ASLogger.Instance.Info($"LoginGameMode: 准备登录，使用客户端实例ID - {clientInstanceId}");
                
                var loginRequest = LoginRequest.Create();
                loginRequest.DisplayName = "Player_" + UnityEngine.Random.Range(1000, 9999);
                loginRequest.ClientInstanceId = clientInstanceId;
                
                ASLogger.Instance.Info($"LoginGameMode: 登录请求已创建 - DisplayName: {loginRequest.DisplayName}, ClientInstanceId: {loginRequest.ClientInstanceId}");

                // 发送登录请求
                networkManager.Send(loginRequest);
                ASLogger.Instance.Info($"LoginGameMode: 登录请求已发送，显示名称: {loginRequest.DisplayName}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LoginGameMode: 发送登录请求失败 - {ex.Message}");
                _connectionState = ConnectionState.Connected;
                PublishLoginStateChanged();
                PublishLoginError($"登录请求失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送快速匹配请求
        /// </summary>
        public void SendQuickMatchRequest()
        {
            if (_connectionState != ConnectionState.LoggedIn)
            {
                ASLogger.Instance.Warning("LoginGameMode: 未登录，无法发送匹配请求");
                return;
            }

            if (_matchState != MatchState.None)
            {
                ASLogger.Instance.Warning("LoginGameMode: 已在匹配状态");
                return;
            }
            
            // 检查是否正在重连中
            if (_isReconnecting)
            {
                ASLogger.Instance.Warning("LoginGameMode: 正在重连中，无法发送匹配请求");
                PublishLoginError("正在重连中，请稍候");
                return;
            }

            try
            {
                var networkManager = NetworkManager.Instance;
                if (networkManager == null)
                {
                    throw new Exception("NetworkManager 不存在");
                }

                // 创建快速匹配请求
                var quickMatchRequest = QuickMatchRequest.Create();
                quickMatchRequest.Timestamp = TimeInfo.Instance.ClientNow();

                // 发送请求
                networkManager.Send(quickMatchRequest);
                ASLogger.Instance.Info("LoginGameMode: 快速匹配请求已发送");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LoginGameMode: 发送快速匹配请求失败 - {ex.Message}");
                PublishLoginError($"匹配请求失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送取消匹配请求
        /// </summary>
        public void SendCancelMatchRequest()
        {
            if (_matchState != MatchState.Matching)
            {
                ASLogger.Instance.Warning("LoginGameMode: 未在匹配状态，无法取消匹配");
                return;
            }

            try
            {
                var networkManager = NetworkManager.Instance;
                if (networkManager == null)
                {
                    throw new Exception("NetworkManager 不存在");
                }

                // 创建取消匹配请求
                var cancelMatchRequest = CancelMatchRequest.Create();
                cancelMatchRequest.Timestamp = TimeInfo.Instance.ClientNow();

                // 发送请求
                networkManager.Send(cancelMatchRequest);
                ASLogger.Instance.Info("LoginGameMode: 取消匹配请求已发送");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LoginGameMode: 发送取消匹配请求失败 - {ex.Message}");
                PublishLoginError($"取消匹配请求失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动单机游戏（切换到 Hub 模式）
        /// </summary>
        public void StartSinglePlayerGame()
        {
            try
            {
                ASLogger.Instance.Info("LoginGameMode: 切换到 Hub 模式");

                // 1. 设置单机模式
                GameConfig.Instance.SetSinglePlayerMode(true);

                // 2. 隐藏登录 UI
                HideLoginUI();

                // 3. 切换到 Hub 模式（Hub模式会自动根据配置启动对应场景）
                GameDirector.Instance.SwitchGameMode(GameModeType.Hub);

                ASLogger.Instance.Info("LoginGameMode: 切换到 Hub 模式成功");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LoginGameMode: 切换到 Hub 模式失败 - {ex.Message}");
                PublishLoginError($"启动游戏失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动回放（切换到 Replay 模式）
        /// </summary>
        public void StartReplay(string replayFilePath)
        {
            try
            {
                ASLogger.Instance.Info($"LoginGameMode: 启动回放 - 文件路径: {replayFilePath}");

                // 1. 验证文件是否存在
                if (!System.IO.File.Exists(replayFilePath))
                {
                    PublishLoginError($"回放文件不存在: {replayFilePath}");
                    return;
                }

                // 2. 隐藏登录 UI
                HideLoginUI();

                // 3. 切换到 Replay 模式
                GameDirector.Instance.SwitchGameMode(GameModeType.Replay);

                // 4. 加载回放文件
                var replayGameMode = GameDirector.Instance.CurrentGameMode as ReplayGameMode;
                if (replayGameMode != null)
                {
                    if (!replayGameMode.Load(replayFilePath))
                    {
                        PublishLoginError("加载回放文件失败");
                        return;
                    }
                }
                else
                {
                    PublishLoginError("无法获取 ReplayGameMode 实例");
                    return;
                }

                ASLogger.Instance.Info("LoginGameMode: 启动回放成功");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LoginGameMode: 启动回放失败 - {ex.Message}");
                PublishLoginError($"启动回放失败: {ex.Message}");
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处理连接响应事件
        /// </summary>
        private void OnConnectResponse(ConnectResponseEventData eventData)
        {
            if (eventData.Success)
            {
                _connectionState = ConnectionState.Connected;
                PublishLoginStateChanged();
                ASLogger.Instance.Info("LoginGameMode: 连接成功");
                SendLoginRequest();
            }
            else
            {
                _connectionState = ConnectionState.Disconnected;
                PublishLoginStateChanged();
                PublishLoginError($"连接失败: {eventData.Message}");
            }
        }

        private void OnNetworkDisconnected(NetworkDisconnectedEventData eventData)
        {
            // TCP 层断开（包括连接失败 10061 等），确保状态不会卡在 Connecting
            if (_connectionState == ConnectionState.Connecting)
            {
                _connectionState = ConnectionState.Disconnected;
                PublishLoginStateChanged();
                PublishLoginError("连接失败：无法连接到服务器");
                return;
            }

            // 已连接/登录过程中的断开，也统一落回 Disconnected，允许重新连接
            if (_connectionState != ConnectionState.Disconnected)
            {
                _connectionState = ConnectionState.Disconnected;
                PublishLoginStateChanged();
                PublishLoginError("连接已断开");
            }
        }

        private void OnNetworkConnectionStatusChanged(NetworkConnectionStatusChangedEventData eventData)
        {
            if (eventData.Status == ConnectionStatus.Disconnected)
            {
                OnNetworkDisconnected(new NetworkDisconnectedEventData());
            }
        }

        /// <summary>
        /// 处理快速匹配响应事件
        /// </summary>
        private void OnQuickMatchResponse(QuickMatchResponse eventData)
        {
            if (eventData.Success)
            {
                _matchState = MatchState.Matching;
                PublishLoginStateChanged();
                ASLogger.Instance.Info("LoginGameMode: 快速匹配成功");
                // 等待服务器发送 GameStartNotification 来切换到联机模式
            }
            else
            {
                _matchState = MatchState.None;
                PublishLoginStateChanged();
                PublishLoginError($"快速匹配失败: {eventData.Message}");
            }
        }

        /// <summary>
        /// 处理取消匹配响应事件
        /// </summary>
        private void OnCancelMatchResponse(CancelMatchResponse eventData)
        {
            if (eventData.Success)
            {
                _matchState = MatchState.None;
                PublishLoginStateChanged();
                ASLogger.Instance.Info("LoginGameMode: 取消匹配成功");
            }
            else
            {
                _matchState = MatchState.None;
                PublishLoginStateChanged();
                PublishLoginError($"取消匹配失败: {eventData.Message}");
            }
        }

        /// <summary>
        /// 处理匹配超时通知事件
        /// </summary>
        private void OnMatchTimeoutNotification(MatchTimeoutNotification eventData)
        {
            _matchState = MatchState.None;
            PublishLoginStateChanged();
            ASLogger.Instance.Warning("LoginGameMode: 匹配超时");
            PublishLoginError("匹配超时，请重试");
        }

        /// <summary>
        /// 处理用户登录成功事件
        /// </summary>
        private void OnUserLoggedIn(UserLoggedInEventData eventData)
        {
            _connectionState = ConnectionState.LoggedIn;
            PublishLoginStateChanged();
            ASLogger.Instance.Info($"LoginGameMode: 用户登录成功 - {eventData.DisplayName}");
            
            // 从 UserManager 获取完整的用户信息以检查房间ID
            var userManager = UserManager.Instance;
            if (userManager != null && userManager.CurrentUser != null)
            {
                var userInfo = userManager.CurrentUser;
                // 检查是否有房间ID（断线重连情况）
                if (!string.IsNullOrEmpty(userInfo.CurrentRoomId))
                {
                    ASLogger.Instance.Info($"LoginGameMode: 检测到断线重连 - 房间ID: {userInfo.CurrentRoomId}");
                    HandleReconnect(userInfo.CurrentRoomId);
                }
            }
        }
        
        /// <summary>
        /// 处理断线重连
        /// 不再发送 JoinRoomRequest，直接等待服务器发送游戏开始通知
        /// </summary>
        private void HandleReconnect(string roomId)
        {
            if (_isReconnecting)
            {
                ASLogger.Instance.Warning("LoginGameMode: 已在重连中，忽略重复请求");
                return;
            }
            
            try
            {
                _isReconnecting = true;
                ASLogger.Instance.Info($"LoginGameMode: 检测到断线重连 - 房间ID: {roomId}，等待服务器发送游戏开始通知");
                
                // 订阅游戏开始通知，用于重置重连标志
                // 注意：这里不需要发送 JoinRoomRequest，服务器会在登录时检测到用户在房间中
                // 并自动发送 GameStartNotification 和 FrameSyncStartNotification
                
                // 设置一个超时，如果一定时间内没有收到游戏开始通知，则认为重连失败
                // 这里暂时不实现超时，等待服务器主动发送通知
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LoginGameMode: 断线重连处理失败 - {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                _isReconnecting = false;
            }
        }
        
        /// <summary>
        /// 重置重连标志（在收到游戏开始通知时调用）
        /// </summary>
        public void OnReconnectGameStarted()
        {
            ASLogger.Instance.Info("LoginGameMode: 重连成功，游戏已开始");
            _isReconnecting = false;
        }

        /// <summary>
        /// 处理用户登录错误事件
        /// </summary>
        private void OnUserLoginError(UserLoginErrorEventData eventData)
        {
            _connectionState = ConnectionState.Disconnected;
            PublishLoginStateChanged();
            PublishLoginError($"登录失败: {eventData.ErrorMessage}");
        }

        #endregion

        #region 事件发布方法

        /// <summary>
        /// 发布登录状态变化事件
        /// </summary>
        private void PublishLoginStateChanged()
        {
            var eventData = new LoginStateChangedEventData
            {
                ConnectionState = (int)_connectionState,
                MatchState = (int)_matchState
            };
            EventSystem.Instance.Publish(eventData);
        }

        /// <summary>
        /// 发布登录错误事件
        /// </summary>
        private void PublishLoginError(string errorMessage)
        {
            ASLogger.Instance.Error($"LoginGameMode: {errorMessage}");
            var eventData = new LoginErrorEventData
            {
                ErrorMessage = errorMessage
            };
            EventSystem.Instance.Publish(eventData);
        }

        #endregion

        #region UI 管理

        /// <summary>
        /// 显示登录 UI
        /// </summary>
        private void ShowLoginUI()
        {
            try
            {
                ASLogger.Instance.Info("LoginGameMode: 显示登录UI");
                UIManager.Instance?.ShowUI("Login");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LoginGameMode: 显示登录UI失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 隐藏登录 UI
        /// </summary>
        private void HideLoginUI()
        {
            try
            {
                ASLogger.Instance.Info("LoginGameMode: 隐藏登录UI");
                UIManager.Instance?.HideUI("Login");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LoginGameMode: 隐藏登录UI失败 - {ex.Message}");
            }
        }

        #endregion

        #region 游戏模式切换

        /// <summary>
        /// 切换到联机游戏模式
        /// </summary>
        private void SwitchToMultiplayerMode()
        {
            try
            {
                ASLogger.Instance.Info("LoginGameMode: 切换到联机游戏模式");

                // 1. 设置联机模式
                Client.Core.GameConfig.Instance.SetSinglePlayerMode(false);

                // 2. 隐藏登录 UI
                HideLoginUI();

                // 3. 创建联机游戏模式并切换
                GameDirector.Instance.SwitchGameMode(GameModeType.Multiplayer);

                ASLogger.Instance.Info("LoginGameMode: 联机游戏模式切换成功");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LoginGameMode: 切换到联机模式失败 - {ex.Message}");
                PublishLoginError($"切换游戏模式失败: {ex.Message}");
            }
        }

        #endregion
    }
}
