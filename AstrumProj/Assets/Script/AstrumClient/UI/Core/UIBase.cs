namespace Astrum.Client.UI.Core
{
    /// <summary>
    /// UI基类 - 定义UI的基本生命周期
    /// </summary>
    public abstract class UIBase
    {
        protected UIRefs uiRefs;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => uiRefs != null;

        /// <summary>
        /// 初始化UI
        /// </summary>
        public virtual void Initialize(UIRefs refs)
        {
            uiRefs = refs;
            OnInitialize();
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
        /// 更新UI（每帧调用）
        /// </summary>
        public virtual void Update()
        {
            if (IsInitialized)
            {
                OnUpdate();
            }
        }

        #region 生命周期回调（子类重写）

        /// <summary>
        /// 初始化完成后的回调（仅调用一次）
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        /// <summary>
        /// 显示时的回调（每次显示时调用）
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// 隐藏时的回调（每次隐藏时调用）
        /// </summary>
        protected virtual void OnHide()
        {
        }

        /// <summary>
        /// 更新回调（每帧调用，仅在显示时）
        /// </summary>
        protected virtual void OnUpdate()
        {
        }

        #endregion
    }
}

