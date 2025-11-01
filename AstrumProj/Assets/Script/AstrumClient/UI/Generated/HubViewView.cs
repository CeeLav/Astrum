// 此文件用于编写UI逻辑代码
// 第一次生成后，可以手动编辑，不会被重新生成覆盖

using UnityEngine;
using UnityEngine.UI;
using System;
using Astrum.Client.UI.Core;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.Client.Data;
using Astrum.Client.Managers.GameModes;

namespace Astrum.Client.UI.Generated
{
    /// <summary>
    /// HubViewView 逻辑部分
    /// 用于编写UI的业务逻辑代码
    /// </summary>
    public partial class HubViewView
    {
        #region Virtual Methods

        /// <summary>
        /// 初始化完成后的回调
        /// </summary>
        protected virtual void OnInitialize()
        {
            // 绑定按钮事件
            if (startExplorationButtonButton != null)
            {
                startExplorationButtonButton.onClick.RemoveListener(OnStartExplorationClicked);
                startExplorationButtonButton.onClick.AddListener(OnStartExplorationClicked);
            }

            // 订阅玩家数据变化事件
            EventSystem.Instance.Subscribe<PlayerDataChangedEventData>(OnPlayerDataChanged);

            // 首次刷新
            var data = PlayerDataManager.Instance?.ProgressData;
            if (data != null)
            {
                RefreshUI(data);
            }
        }

        /// <summary>
        /// 显示时的回调
        /// </summary>
        protected virtual void OnShow()
        {
            // 显示时确保刷新一次（避免场景切换后数据未展示）
            var data = PlayerDataManager.Instance?.ProgressData;
            if (data != null)
            {
                RefreshUI(data);
            }
        }

        /// <summary>
        /// 隐藏时的回调
        /// </summary>
        protected virtual void OnHide()
        {
            // 隐藏时可选择性取消订阅（如果该视图会频繁显示/隐藏，可保持订阅）
            EventSystem.Instance.Unsubscribe<PlayerDataChangedEventData>(OnPlayerDataChanged);
        }

        #endregion

        #region Business Logic

        private void OnStartExplorationClicked()
        {
            // 直接调用 GameMode 的方法，不通过事件
            var hubMode = GameDirector.Instance?.CurrentGameMode as HubGameMode;
            if (hubMode != null)
            {
                hubMode.StartExploration();
            }
            else
            {
                ASLogger.Instance.Warning("HubViewView: 当前 GameMode 不是 HubGameMode");
            }
        }

        private void OnPlayerDataChanged(PlayerDataChangedEventData evt)
        {
            if (evt?.ProgressData == null) return;
            RefreshUI(evt.ProgressData);
        }

        private void RefreshUI(PlayerProgressData data)
        {
            if (levelTextText != null)
            {
                // 不显示 ExpToNextLevel，简化为 等级/经验
                levelTextText.text = $"等级: {data.Level}  经验: {data.Exp}";
            }

            if (starFragmentsTextText != null)
            {
                starFragmentsTextText.text = $"星能碎片: {data.StarFragments}";
            }
        }

        #endregion
    }
}
