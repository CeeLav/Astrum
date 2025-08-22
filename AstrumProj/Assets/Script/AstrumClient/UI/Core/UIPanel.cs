using UnityEngine;
using System;

namespace Astrum.View.UI
{
    /// <summary>
    /// UI面板基类 - 非MonoBehaviour版本
    /// </summary>
    public abstract class UIPanel
    {
        protected string _panelName;
        protected GameObject _panelObject;
        protected RectTransform _rectTransform;
        protected bool _isVisible;
        protected bool _isInitialized;
        
        // 公共属性
        public string PanelName => _panelName;
        public GameObject PanelObject => _panelObject;
        public bool IsVisible => _isVisible;
        public bool IsInitialized => _isInitialized;
        
        // 事件
        public event Action<UIPanel> OnPanelShown;
        public event Action<UIPanel> OnPanelHidden;
        public event Action<string, object> OnPanelEvent;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        protected UIPanel(string panelName)
        {
            _panelName = panelName;
            _isVisible = false;
            _isInitialized = false;
        }
        
        /// <summary>
        /// 初始化面板
        /// </summary>
        public virtual void Initialize(Transform parent)
        {
            if (_isInitialized) return;
            
            CreatePanelObject(parent);
            SetupUI();
            BindEvents();
            
            _isInitialized = true;
            OnInitialize();
        }
        
        /// <summary>
        /// 显示面板
        /// </summary>
        public virtual void Show()
        {
            if (!_isInitialized || _isVisible) return;
            
            if (_panelObject != null)
            {
                _panelObject.SetActive(true);
            }
            
            _isVisible = true;
            OnShow();
            OnPanelShown?.Invoke(this);
        }
        
        /// <summary>
        /// 隐藏面板
        /// </summary>
        public virtual void Hide()
        {
            if (!_isInitialized || !_isVisible) return;
            
            if (_panelObject != null)
            {
                _panelObject.SetActive(false);
            }
            
            _isVisible = false;
            OnHide();
            OnPanelHidden?.Invoke(this);
        }
        
        /// <summary>
        /// 更新面板
        /// </summary>
        public virtual void Update()
        {
            if (!_isVisible) return;
            OnUpdate();
        }
        
        /// <summary>
        /// 销毁面板
        /// </summary>
        public virtual void Destroy()
        {
            UnbindEvents();
            OnDestroy();
            
            if (_panelObject != null)
            {
                UnityEngine.Object.Destroy(_panelObject);
                _panelObject = null;
            }
            
            _isInitialized = false;
            _isVisible = false;
        }
        
        /// <summary>
        /// 触发面板事件
        /// </summary>
        protected void TriggerEvent(string eventName, object data = null)
        {
            OnPanelEvent?.Invoke(eventName, data);
        }
        
        // 抽象方法 - 子类必须实现
        protected abstract void CreatePanelObject(Transform parent);
        protected abstract void SetupUI();
        protected abstract void BindEvents();
        protected abstract void UnbindEvents();
        protected abstract void OnInitialize();
        protected abstract void OnShow();
        protected abstract void OnHide();
        protected abstract void OnUpdate();
        protected abstract void OnDestroy();
    }
}
