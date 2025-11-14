// 此文件用于编写UI逻辑代码
// 第一次生成后，可以手动编辑，不会被重新生成覆盖

using UnityEngine;
using UnityEngine.UI;
using System;
using Astrum.Client.UI.Core;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.Client.Data;
using Astrum.Client.Managers;
using Astrum.Client.Managers.GameModes;

namespace Astrum.Client.UI.Generated
{
    /// <summary>
    /// HubView 逻辑部分
    /// 用于编写UI的业务逻辑代码
    /// </summary>
    public partial class HubView
    {
        #region Virtual Methods

        /// <summary>
        /// 初始化完成后的回调
        /// </summary>
        protected virtual void OnInitialize()
        {
            // 绑定探索按钮事件
            if (startExplorationButtonButton != null)
            {
                startExplorationButtonButton.onClick.AddListener(OnStartExplorationClicked);
            }
            
            // 绑定背包按钮事件
            if (bagButtonButton != null)
            {
                bagButtonButton.onClick.AddListener(OnBagButtonClicked);
            }
            
            // 绑定存储按钮事件
            if (storageButtonButton != null)
            {
                storageButtonButton.onClick.AddListener(OnStorageButtonClicked);
            }
            
            // 订阅玩家数据变化事件
            EventSystem.Instance.Subscribe<PlayerDataChangedEventData>(OnPlayerDataChanged);
            
            // 首次刷新
            RefreshUI();
        }
        
        /// <summary>
        /// 显示时的回调
        /// </summary>
        protected virtual void OnShow()
        {
            // 显示时确保刷新一次（避免场景切换后数据未展示）
            RefreshUI();
        }
        
        /// <summary>
        /// 隐藏时的回调
        /// </summary>
        protected virtual void OnHide()
        {
            // 取消订阅事件
            EventSystem.Instance.Unsubscribe<PlayerDataChangedEventData>(OnPlayerDataChanged);
        }

        #endregion

        #region Business Logic

        /// <summary>
        /// 开始探索按钮点击
        /// </summary>
        private void OnStartExplorationClicked()
        {
            ASLogger.Instance.Info("HubView: 点击开始探索");
            
            // 直接调用 GameMode 的方法，不通过事件
            var hubMode = GameDirector.Instance?.CurrentGameMode as HubGameMode;
            if (hubMode != null)
            {
                hubMode.StartExploration();
            }
            else
            {
                ASLogger.Instance.Warning("HubView: 当前 GameMode 不是 HubGameMode");
            }
        }
        
        /// <summary>
        /// 背包按钮点击
        /// </summary>
        private void OnBagButtonClicked()
        {
            ASLogger.Instance.Info("HubView: 点击背包按钮");
            UIManager.Instance?.ShowUI("Hub/Bag");
        }
        
        /// <summary>
        /// 存储按钮点击
        /// </summary>
        private void OnStorageButtonClicked()
        {
            ASLogger.Instance.Info("HubView: 点击存储按钮");
            // TODO: 实现存储界面
        }
        
        /// <summary>
        /// 玩家数据变化事件处理
        /// </summary>
        private void OnPlayerDataChanged(PlayerDataChangedEventData evt)
        {
            if (evt?.ProgressData == null) return;
            RefreshUI();
        }
        
        /// <summary>
        /// 刷新UI显示
        /// </summary>
        private void RefreshUI()
        {
            var data = PlayerDataManager.Instance?.ProgressData;
            if (data == null) return;
            
            // TODO: 更新UI显示，如等级、经验等
        }

        #endregion
    }
}
