// 此文件用于编写UI逻辑代码
// 第一次生成后，可以手动编辑，不会被重新生成覆盖

using UnityEngine;
using UnityEngine.UI;
using System;
using Astrum.Client.Managers;
using Astrum.Client.UI.Core;
using Astrum.CommonBase;

namespace Astrum.Client.UI.Generated
{
    /// <summary>
    /// BagView 逻辑部分
    /// 用于编写UI的业务逻辑代码
    /// </summary>
    public partial class BagView
    {
        private string currentTab = "All"; // 当前选中的标签页
        
        #region Virtual Methods

        /// <summary>
        /// 初始化完成后的回调
        /// </summary>
        protected virtual void OnInitialize()
        {
            // 绑定关闭按钮
            if (btnCloseButton != null)
            {
                btnCloseButton.onClick.AddListener(OnCloseButtonClicked);
            }
            
            // 绑定标签页按钮
            if (allTabButton != null)
                allTabButton.onClick.AddListener(() => OnTabClicked("All"));
            if (armorTabButton != null)
                armorTabButton.onClick.AddListener(() => OnTabClicked("Armor"));
            if (weaponTabButton != null)
                weaponTabButton.onClick.AddListener(() => OnTabClicked("Weapon"));
            if (ringTabButton != null)
                ringTabButton.onClick.AddListener(() => OnTabClicked("Ring"));
            if (bootsTabButton != null)
                bootsTabButton.onClick.AddListener(() => OnTabClicked("Boots"));
            if (shieldTabButton != null)
                shieldTabButton.onClick.AddListener(() => OnTabClicked("Shield"));
            if (bookTabButton != null)
                bookTabButton.onClick.AddListener(() => OnTabClicked("Book"));
            
            // 初始化角色属性显示
            RefreshCharacterStats();
        }

        /// <summary>
        /// 显示时的回调
        /// </summary>
        protected virtual void OnShow()
        {
            // 显示时刷新数据
            RefreshCharacterStats();
        }

        /// <summary>
        /// 隐藏时的回调
        /// </summary>
        protected virtual void OnHide()
        {
        }

        #endregion

        #region Business Logic

        /// <summary>
        /// 关闭按钮点击
        /// </summary>
        private void OnCloseButtonClicked()
        {
            ASLogger.Instance.Info("BagView: 点击关闭按钮");
            UIManager.Instance?.HideUI("Hub/Bag");
        }
        
        /// <summary>
        /// 标签页点击
        /// </summary>
        private void OnTabClicked(string tabName)
        {
            ASLogger.Instance.Info($"BagView: 切换到标签页 {tabName}");
            currentTab = tabName;
            
            // TODO: 根据标签页过滤显示物品列表
        }
        
        /// <summary>
        /// 刷新角色属性显示
        /// </summary>
        private void RefreshCharacterStats()
        {
            // 使用假数据
            if (sliderHeartSlider != null)
                sliderHeartSlider.value = 0.8f;
            if (sliderSpeedSlider != null)
                sliderSpeedSlider.value = 0.6f;
            if (sliderAtkSlider != null)
                sliderAtkSlider.value = 0.7f;
            if (sliderDefSlider != null)
                sliderDefSlider.value = 0.5f;
        }

        #endregion
    }
}
