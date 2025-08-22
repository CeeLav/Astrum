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
    /// RoomListView 逻辑部分
    /// 用于编写UI的业务逻辑代码
    /// </summary>
    public partial class RoomListView
    {
        #region Fields

        private List<RoomInfo> availableRooms = new List<RoomInfo>();
        private List<GameObject> roomItemInstances = new List<GameObject>();
        private bool isRefreshing = false;

        #endregion

        #region Events

        public event Action<string> OnJoinRoomRequested;
        public event Action OnCreateRoomRequested;
        public event Action OnRefreshRequested;
        public event Action OnExitRequested;

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
            // 自动刷新房间列表
            RefreshRoomList();
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
            if (createRoomBtnButton != null)
            {
                createRoomBtnButton.onClick.AddListener(OnCreateRoomButtonClicked);
            }

            if (refreshBtnButton != null)
            {
                refreshBtnButton.onClick.AddListener(OnRefreshButtonClicked);
            }

            if (exitBtnButton != null)
            {
                exitBtnButton.onClick.AddListener(OnExitButtonClicked);
            }

            if (joinBtnButton != null)
            {
                joinBtnButton.onClick.AddListener(OnJoinRoomButtonClicked);
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
                roomSystemManager.OnRoomListUpdated += OnRoomListUpdated;
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
                roomSystemManager.OnRoomListUpdated -= OnRoomListUpdated;
                roomSystemManager.OnRoomError -= OnRoomError;
            }
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// 创建房间按钮点击事件
        /// </summary>
        private void OnCreateRoomButtonClicked()
        {
            Debug.Log("RoomListView: 创建房间按钮被点击");
            OnCreateRoomRequested?.Invoke();
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// </summary>
        private void OnRefreshButtonClicked()
        {
            if (isRefreshing)
            {
                Debug.Log("RoomListView: 正在刷新中，请稍候...");
                return;
            }

            Debug.Log("RoomListView: 刷新房间列表");
            RefreshRoomList();
        }

        /// <summary>
        /// 退出按钮点击事件
        /// </summary>
        private void OnExitButtonClicked()
        {
            Debug.Log("RoomListView: 退出按钮被点击");
            OnExitRequested?.Invoke();
        }

        /// <summary>
        /// 加入房间按钮点击事件
        /// </summary>
        private void OnJoinRoomButtonClicked()
        {
            Debug.Log("RoomListView: 加入房间按钮被点击");
            // 这里需要获取当前选中的房间ID
            // 由于UI结构限制，这里暂时使用第一个房间作为示例
            if (availableRooms.Count > 0)
            {
                OnJoinRoomRequested?.Invoke(availableRooms[0].Id);
            }
        }

        #endregion

        #region Room System Event Handlers

        /// <summary>
        /// 房间列表更新事件
        /// </summary>
        private void OnRoomListUpdated(List<RoomInfo> rooms)
        {
            Debug.Log($"RoomListView: 收到房间列表更新，房间数量: {rooms?.Count ?? 0}");
            availableRooms = rooms ?? new List<RoomInfo>();
            UpdateRoomListDisplay();
            isRefreshing = false;
        }

        /// <summary>
        /// 房间错误事件
        /// </summary>
        private void OnRoomError(string errorMessage)
        {
            Debug.LogError($"RoomListView: 房间系统错误: {errorMessage}");
            isRefreshing = false;
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
                titleLabelText.text = "房间列表";
            }

            // 设置按钮文本
            if (createRoomTextText != null)
            {
                createRoomTextText.text = "创建房间";
            }

            if (refreshTextText != null)
            {
                refreshTextText.text = "刷新";
            }

            if (exitTextText != null)
            {
                exitTextText.text = "退出";
            }

            if (joinBtnTextText != null)
            {
                joinBtnTextText.text = "加入";
            }

            // 初始化房间列表显示
            UpdateRoomListDisplay();
        }

        /// <summary>
        /// 刷新房间列表
        /// </summary>
        public async void RefreshRoomList()
        {
            if (isRefreshing)
            {
                return;
            }

            try
            {
                isRefreshing = true;
                Debug.Log("RoomListView: 开始刷新房间列表");

                var roomSystemManager = RoomSystemManager.Instance;
                if (roomSystemManager == null)
                {
                    Debug.LogError("RoomListView: RoomSystemManager不存在");
                    isRefreshing = false;
                    return;
                }

                // 请求房间列表
                bool success = await roomSystemManager.GetRoomListAsync();
                if (!success)
                {
                    Debug.LogError("RoomListView: 获取房间列表失败");
                    isRefreshing = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"RoomListView: 刷新房间列表时发生异常: {ex.Message}");
                isRefreshing = false;
            }
        }

        /// <summary>
        /// 更新房间列表显示
        /// </summary>
        private void UpdateRoomListDisplay()
        {
            // 清理旧的房间项
            ClearRoomList();

            if (availableRooms == null || availableRooms.Count == 0)
            {
                Debug.Log("RoomListView: 暂无可用房间");
                ShowEmptyRoomList();
                return;
            }

            Debug.Log($"RoomListView: 显示 {availableRooms.Count} 个房间");

            // 由于UI结构限制，这里只显示第一个房间的信息
            // 在实际项目中，应该动态创建多个房间项
            if (availableRooms.Count > 0)
            {
                DisplayRoomInfo(availableRooms[0]);
            }
        }

        /// <summary>
        /// 显示房间信息
        /// </summary>
        private void DisplayRoomInfo(RoomInfo room)
        {
            if (room == null) return;

            // 显示房间ID
            if (roomIdTextText != null)
            {
                roomIdTextText.text = room.Id;
            }

            // 显示房主信息
            if (hostTextText != null)
            {
                hostTextText.text = room.CreatorName;
            }

            // 显示玩家数量
            if (playerCountTextText != null)
            {
                playerCountTextText.text = $"{room.CurrentPlayers}/{room.MaxPlayers}";
            }

            // 设置加入按钮状态
            if (joinBtnButton != null)
            {
                joinBtnButton.interactable = room.CurrentPlayers < room.MaxPlayers;
            }
        }

        /// <summary>
        /// 显示空房间列表
        /// </summary>
        private void ShowEmptyRoomList()
        {
            if (roomIdTextText != null)
            {
                roomIdTextText.text = "暂无房间";
            }

            if (hostTextText != null)
            {
                hostTextText.text = "-";
            }

            if (playerCountTextText != null)
            {
                playerCountTextText.text = "0/0";
            }

            if (joinBtnButton != null)
            {
                joinBtnButton.interactable = false;
            }
        }

        /// <summary>
        /// 清理房间列表
        /// </summary>
        private void ClearRoomList()
        {
            // 清理动态创建的房间项实例
            foreach (var instance in roomItemInstances)
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }
            roomItemInstances.Clear();
        }

        /// <summary>
        /// 设置房间列表数据
        /// </summary>
        public void SetRoomList(List<RoomInfo> rooms)
        {
            availableRooms = rooms ?? new List<RoomInfo>();
            UpdateRoomListDisplay();
        }

        #endregion
    }
}
