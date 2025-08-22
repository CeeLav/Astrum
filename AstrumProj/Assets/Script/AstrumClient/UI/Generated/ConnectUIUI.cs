using UnityEngine;
using UnityEngine.UI;
using System;
using Astrum.Client.UI.Core;

namespace Astrum.Client.UI.Generated
{
    /// <summary>
    /// ConnectUI UI逻辑类
    /// 由UI生成器自动生成
    /// </summary>
    public class ConnectUIUI
    {
        #region Fields

        // UI引用
        private UIRefs uiRefs;

        #endregion

        #region Properties

        public bool IsInitialized => uiRefs != null;

        #endregion

        #region Methods

        /// <summary>
        /// 初始化UI
        /// </summary>
        public void Initialize(UIRefs refs)
        {
            uiRefs = refs;
            OnInitialize();
        }

        /// <summary>
        /// 初始化完成后的回调
        /// </summary>
        protected virtual void OnInitialize()
        {
            // 在这里添加初始化逻辑
        }

        /// <summary>
        /// 显示UI
        /// </summary>
        public virtual void Show()
        {
            if (uiRefs != null)
            {
                uiRefs.gameObject.SetActive(true);
                OnShow();
            }
        }

        /// <summary>
        /// 隐藏UI
        /// </summary>
        public virtual void Hide()
        {
            if (uiRefs != null)
            {
                OnHide();
                uiRefs.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 显示时的回调
        /// </summary>
        protected virtual void OnShow()
        {
            // 在这里添加显示逻辑
        }

        /// <summary>
        /// 隐藏时的回调
        /// </summary>
        protected virtual void OnHide()
        {
            // 在这里添加隐藏逻辑
        }

        #endregion
    }
}
