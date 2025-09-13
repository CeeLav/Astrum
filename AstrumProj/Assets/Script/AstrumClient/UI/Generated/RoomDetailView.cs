// 此文件用于编写UI逻辑代码
// 第一次生成后，可以手动编辑，不会被重新生成覆盖

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Astrum.Client.UI.Core;
using Astrum.Client.Managers;
using Astrum.Client.Core;
using Astrum.Generated;

namespace Astrum.Client.UI.Generated
{
    /// <summary>
    /// RoomDetailView 逻辑部分
    /// 用于编写UI的业务逻辑代码
    /// </summary>
    public partial class RoomDetailView
    {
        #region Fields

        private RoomInfo currentRoom;
        private bool isHost = false;
        private bool isLeavingRoom = false;
        private bool isStartingGame = false;

        #endregion

        #region Events

        public event Action OnLeaveRoomRequested;
        public event Action OnStartGameRequested;
        public event Action OnBackToRoomListRequested;

        #endregion

        #region Virtual Methods

        /// <summary>
        /// 初始化完成后的回调
        /// </summary>
        protected virtual void OnInitialize()
        {
            // 绑定按钮事件
            BindButtonEvents();

            // 订阅房间系统事件
            SubscribeToRoomSystemEvents();

            // 初始化UI状态
            InitializeUIState();
        }

        /// <summary>
        /// 显示时的回调
        /// </summary>
        protected virtual void OnShow()
        {
            // 更新房间信息显示
            UpdateRoomInfo();
        }

        /// <summary>
        /// 隐藏时的回调
        /// </summary>
        protected virtual void OnHide()
        {
            // 取消订阅事件
            UnsubscribeFromRoomSystemEvents();
        }

        #endregion

        #region Event Binding

        /// <summary>
        /// 绑定按钮事件
        /// </summary>
        private void BindButtonEvents()
        {
            if (startGameBtnButton != null)
            {
                startGameBtnButton.onClick.AddListener(OnStartGameButtonClicked);
            }

            if (leaveRoomBtnButton != null)
            {
                leaveRoomBtnButton.onClick.AddListener(OnLeaveRoomButtonClicked);
            }
        }

        /// <summary>
        /// 订阅房间系统事件
        /// </summary>
        private void SubscribeToRoomSystemEvents()
        {
            var roomSystemManager = RoomSystemManager.Instance;
            if (roomSystemManager != null)
            {
                roomSystemManager.OnRoomJoined += OnRoomJoined;
                roomSystemManager.OnRoomLeft += OnRoomLeft;
                roomSystemManager.OnRoomError += OnRoomError;
            }
        }

        /// <summary>
        /// 取消订阅房间系统事件
        /// </summary>
        private void UnsubscribeFromRoomSystemEvents()
        {
            var roomSystemManager = RoomSystemManager.Instance;
            if (roomSystemManager != null)
            {
                roomSystemManager.OnRoomJoined -= OnRoomJoined;
                roomSystemManager.OnRoomLeft -= OnRoomLeft;
                roomSystemManager.OnRoomError -= OnRoomError;
            }
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// 开始游戏按钮点击事件
        /// </summary>
        private void OnStartGameButtonClicked()
        {
            if (isStartingGame)
            {
                Debug.Log("RoomDetailView: 正在开始游戏中，请稍候...");
                return;
            }

            if (!isHost)
            {
                Debug.LogWarning("RoomDetailView: 只有房主才能开始游戏");
                return;
            }

            Debug.Log("RoomDetailView: 开始游戏按钮被点击");
            StartGame();
        }

        /// <summary>
        /// 离开房间按钮点击事件
        /// </summary>
        private void OnLeaveRoomButtonClicked()
        {
            if (isLeavingRoom)
            {
                Debug.Log("RoomDetailView: 正在离开房间中，请稍候...");
                return;
            }

            Debug.Log("RoomDetailView: 离开房间按钮被点击");
            LeaveRoom();
        }

        #endregion

        #region Room System Event Handlers

        /// <summary>
        /// 房间加入事件
        /// </summary>
        private void OnRoomJoined(RoomInfo room)
        {
            Debug.Log($"RoomDetailView: 加入房间成功: {room?.Id}");
            currentRoom = room;
            UpdateRoomInfo();
        }

        /// <summary>
        /// 房间离开事件
        /// </summary>
        private void OnRoomLeft()
        {
            Debug.Log("RoomDetailView: 离开房间成功");
            currentRoom = null;
            isHost = false;
            OnBackToRoomListRequested?.Invoke();
        }

        /// <summary>
        /// 房间错误事件
        /// </summary>
        private void OnRoomError(string errorMessage)
        {
            Debug.LogError($"RoomDetailView: 房间系统错误: {errorMessage}");
            isLeavingRoom = false;
            isStartingGame = false;
            // 可以在这里显示错误提示
        }

        #endregion

        #region Business Logic

        /// <summary>
        /// 初始化UI状态
        /// </summary>
        private void InitializeUIState()
        {
            // 设置标题
            if (titleLabelText != null)
            {
                titleLabelText.text = "房间详情";
            }

            // 设置按钮文本
            if (startGameTextText != null)
            {
                startGameTextText.text = "开始游戏";
            }

            if (leaveRoomTextText != null)
            {
                leaveRoomTextText.text = "离开房间";
            }

            // 设置标签文本
            if (roomIdLabelText != null)
            {
                roomIdLabelText.text = "房间ID:";
            }

            if (hostLabelText != null)
            {
                hostLabelText.text = "房主:";
            }

            if (playerCountLabelText != null)
            {
                playerCountLabelText.text = "人数:";
            }

            if (playerListTitleText != null)
            {
                playerListTitleText.text = "玩家列表";
            }

            // 初始化房间信息
            UpdateRoomInfo();
        }

        /// <summary>
        /// 设置当前房间信息
        /// </summary>
        public void SetRoomInfo(RoomInfo room)
        {
            currentRoom = room;
            UpdateRoomInfo();
        }

        /// <summary>
        /// 更新房间信息显示
        /// </summary>
        private void UpdateRoomInfo()
        {
            if (currentRoom == null)
            {
                ShowEmptyRoomInfo();
                return;
            }

            Debug.Log($"RoomDetailView: 更新房间信息: {currentRoom.Id}");

            // 显示房间ID
            if (roomIdTextText != null)
            {
                roomIdTextText.text = currentRoom.Id;
            }

            // 显示房主信息
            if (hostTextText != null)
            {
                hostTextText.text = currentRoom.CreatorName;
            }

            // 显示玩家数量
            if (playerCountTextText != null)
            {
                playerCountTextText.text = $"{currentRoom.CurrentPlayers}/{currentRoom.MaxPlayers}";
            }

            // 检查是否是房主
            var userManager = UserManager.Instance;
            isHost = userManager != null && userManager.CurrentUser != null && 
                     currentRoom.CreatorName == userManager.CurrentUser.Id;

            // 更新开始游戏按钮状态
            UpdateStartGameButton();

            // 更新玩家列表显示
            UpdatePlayerList();
        }

        /// <summary>
        /// 显示空房间信息
        /// </summary>
        private void ShowEmptyRoomInfo()
        {
            if (roomIdTextText != null)
            {
                roomIdTextText.text = "-";
            }

            if (hostTextText != null)
            {
                hostTextText.text = "-";
            }

            if (playerCountTextText != null)
            {
                playerCountTextText.text = "0/0";
            }

            if (playerNameTextText != null)
            {
                playerNameTextText.text = "暂无玩家";
            }

            if (hostBadgeTextText != null)
            {
                hostBadgeTextText.text = "";
            }

            UpdateStartGameButton();
        }

        /// <summary>
        /// 更新开始游戏按钮状态
        /// </summary>
        private void UpdateStartGameButton()
        {
            if (startGameBtnButton != null)
            {
                startGameBtnButton.interactable = isHost && currentRoom != null;
            }

            if (startGameTextText != null)
            {
                if (!isHost)
                {
                    startGameTextText.text = "仅房主可开始";
                }
                else if (currentRoom == null)
                {
                    startGameTextText.text = "需要房间信息";
                }
                else
                {
                    startGameTextText.text = "开始游戏";
                }
            }
        }

        /// <summary>
        /// 更新玩家列表显示
        /// </summary>
        private void UpdatePlayerList()
        {
            if (currentRoom == null || currentRoom.PlayerNames == null)
            {
                if (playerNameTextText != null)
                {
                    playerNameTextText.text = "暂无玩家";
                }
                if (hostBadgeTextText != null)
                {
                    hostBadgeTextText.text = "";
                }
                return;
            }

            // 显示第一个玩家（房主）
            if (currentRoom.PlayerNames.Count > 0)
            {
                if (playerNameTextText != null)
                {
                    playerNameTextText.text = currentRoom.PlayerNames[0];
                }
                if (hostBadgeTextText != null)
                {
                    hostBadgeTextText.text = "房主";
                }
            }
            else
            {
                if (playerNameTextText != null)
                {
                    playerNameTextText.text = "暂无玩家";
                }
                if (hostBadgeTextText != null)
                {
                    hostBadgeTextText.text = "";
                }
            }
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public async void StartGame()
        {
            if (isStartingGame || !isHost || currentRoom == null)
            {
                return;
            }

            try
            {
                isStartingGame = true;
                Debug.Log($"RoomDetailView: 开始游戏 - 房间: {currentRoom.Id}");

                var roomSystemManager = RoomSystemManager.Instance;
                if (roomSystemManager == null)
                {
                    Debug.LogError("RoomDetailView: RoomSystemManager不存在");
                    isStartingGame = false;
                    return;
                }

                // 发送开始游戏请求
                bool success = await roomSystemManager.StartGameAsync();
                if (success)
                {
                    Debug.Log("RoomDetailView: 开始游戏请求已发送");
                    OnStartGameRequested?.Invoke();
                }
                else
                {
                    Debug.LogError("RoomDetailView: 开始游戏失败");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"RoomDetailView: 开始游戏时发生异常: {ex.Message}");
            }
            finally
            {
                isStartingGame = false;
            }
        }

        /// <summary>
        /// 离开房间
        /// </summary>
        public async void LeaveRoom()
        {
            if (isLeavingRoom || currentRoom == null)
            {
                return;
            }

            try
            {
                isLeavingRoom = true;
                Debug.Log($"RoomDetailView: 离开房间 - 房间: {currentRoom.Id}");

                var roomSystemManager = RoomSystemManager.Instance;
                if (roomSystemManager == null)
                {
                    Debug.LogError("RoomDetailView: RoomSystemManager不存在");
                    isLeavingRoom = false;
                    return;
                }

                // 发送离开房间请求
                bool success = await roomSystemManager.LeaveRoomAsync();
                if (success)
                {
                    Debug.Log("RoomDetailView: 离开房间请求已发送");
                    // UI切换由RoomSystemManager统一处理
                    OnLeaveRoomRequested?.Invoke();
                }
                else
                {
                    Debug.LogError("RoomDetailView: 离开房间失败");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"RoomDetailView: 离开房间时发生异常: {ex.Message}");
            }
            finally
            {
                isLeavingRoom = false;
            }
        }

        #endregion
    }
}
