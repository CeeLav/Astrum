using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using Astrum.CommonBase;

namespace Astrum.View.Managers
{
    /// <summary>
    /// UI管理器 - 负责管理游戏UI界面
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [Header("UI设置")]
        private bool _enableLogging = true;
        private Canvas _uiCanvas;
        private UnityEngine.EventSystems.EventSystem _eventSystem;
        private int _maxActivePanels = 5;
        
        // UI面板管理
        private List<UIPanel> _activePanels = new List<UIPanel>();
        private Dictionary<string, UIPanel> _panelPrefabs = new Dictionary<string, UIPanel>();
        private Transform _panelContainer;
        
        // UI事件处理
        private bool _isHandlingUIEvents;
        
        // 公共属性
        public Canvas UICanvas => _uiCanvas;
        public UnityEngine.EventSystems.EventSystem EventSystem => _eventSystem;
        public List<UIPanel> ActivePanels => _activePanels;
        public int ActivePanelCount => _activePanels.Count;
        
        // 事件
        public event Action<UIPanel> OnPanelShown;
        public event Action<UIPanel> OnPanelHidden;
        public event Action<string> OnPanelEvent;
        
        void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// 初始化UI管理器
        /// </summary>
        public void Initialize()
        {
            if (_enableLogging)
                Debug.Log("UIManager: 初始化UI管理器");
            
            // 查找或创建UI Canvas
            if (_uiCanvas == null)
            {
                _uiCanvas = UnityEngine.Object.FindObjectOfType<Canvas>();
                if (_uiCanvas == null)
                {
                    CreateUICanvas();
                }
            }
            
            // 查找或创建EventSystem
            if (_eventSystem == null)
            {
                _eventSystem = UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                if (_eventSystem == null)
                {
                    CreateEventSystem();
                }
            }
            
            // 创建面板容器
            CreatePanelContainer();
            
            // 加载UI面板预制件
            LoadUIPanels();
            
            if (_enableLogging)
                Debug.Log("UIManager: UI管理器初始化完成");
        }
        
        /// <summary>
        /// 创建UI Canvas
        /// </summary>
        private void CreateUICanvas()
        {
            GameObject canvasGo = new GameObject("UI Canvas");
            _uiCanvas = canvasGo.AddComponent<Canvas>();
            _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _uiCanvas.sortingOrder = 100;
            
            // 添加CanvasScaler
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // 添加GraphicRaycaster
            canvasGo.AddComponent<GraphicRaycaster>();
            
            UnityEngine.Object.DontDestroyOnLoad(canvasGo);
            
            if (_enableLogging)
                Debug.Log("UIManager: 创建UI Canvas");
        }
        
        /// <summary>
        /// 创建EventSystem
        /// </summary>
        private void CreateEventSystem()
        {
            GameObject eventSystemGo = new GameObject("EventSystem");
            _eventSystem = eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
            
            UnityEngine.Object.DontDestroyOnLoad(eventSystemGo);
            
            if (_enableLogging)
                Debug.Log("UIManager: 创建EventSystem");
        }
        
        /// <summary>
        /// 创建面板容器
        /// </summary>
        private void CreatePanelContainer()
        {
            GameObject containerGo = new GameObject("Panel Container");
            _panelContainer = containerGo.transform;
            _panelContainer.SetParent(_uiCanvas.transform);
            
            // 设置容器位置
            RectTransform containerRect = containerGo.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            
            if (_enableLogging)
                Debug.Log("UIManager: 创建面板容器");
        }
        
        /// <summary>
        /// 加载UI面板预制件
        /// </summary>
        private void LoadUIPanels()
        {
            try
            {
                // 从Resources文件夹加载UI面板预制件
                UIPanel[] panels = Resources.LoadAll<UIPanel>("UI/Panels");
                
                foreach (UIPanel panel in panels)
                {
                    if (panel != null)
                    {
                        _panelPrefabs[panel.PanelName] = panel;
                        
                        if (_enableLogging)
                            Debug.Log($"UIManager: 加载UI面板预制件 - {panel.PanelName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"UIManager: 加载UI面板预制体失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新UI管理器
        /// </summary>
        public void Update()
        {
            // 更新活跃面板
            UpdateActivePanels();
            
            // 处理UI事件
            HandleUIEvents();
        }
        
        /// <summary>
        /// 更新活跃面板
        /// </summary>
        private void UpdateActivePanels()
        {
            // 更新所有活跃面�?
            for (int i = _activePanels.Count - 1; i >= 0; i--)
            {
                UIPanel panel = _activePanels[i];
                if (panel != null && panel.IsVisible)
                {
                    panel.Update();
                }
                else if (panel == null)
                {
                    // 移除空引�?
                    _activePanels.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// 处理UI事件
        /// </summary>
        private void HandleUIEvents()
        {
            if (_isHandlingUIEvents) return;
            
            _isHandlingUIEvents = true;
            
            try
            {
                // 处理UI事件逻辑
                // 这里可以添加全局UI事件处理
            }
            catch (Exception ex)
            {
                Debug.LogError($"UIManager: 处理UI事件时发生异�?- {ex.Message}");
            }
            finally
            {
                _isHandlingUIEvents = false;
            }
        }
        
        /// <summary>
        /// 显示面板
        /// </summary>
        /// <param name="panelName">面板名称</param>
        /// <param name="data">面板数据</param>
        /// <returns>面板实例</returns>
        public UIPanel ShowPanel(string panelName, object data = null)
        {
            if (string.IsNullOrEmpty(panelName))
            {
                Debug.LogError("UIManager: 面板名称为空");
                return null;
            }
            
            // 检查面板是否已经显示
            UIPanel existingPanel = GetActivePanel(panelName);
            if (existingPanel != null)
            {
                if (_enableLogging)
                    Debug.Log($"UIManager: 面板 {panelName} 已经显示");
                return existingPanel;
            }
            
            // 检查面板数量限制
            if (_activePanels.Count >= _maxActivePanels)
            {
                Debug.LogWarning($"UIManager: 活跃面板数量已达上限 {_maxActivePanels}");
                HideLowestPriorityPanel();
            }
            
            // 创建面板实例
            UIPanel panel = CreatePanel(panelName);
            if (panel != null)
            {
                if (data != null)
                {
                    panel.SetData(data);
                }
                
                panel.Show();
                _activePanels.Add(panel);
                
                SortPanelsByPriority();
                
                if (_enableLogging)
                    Debug.Log($"UIManager: 显示面板 {panelName}");
                
                OnPanelShown?.Invoke(panel);
                
                return panel;
            }
            else
            {
                Debug.LogError($"UIManager: 无法创建面板 {panelName}");
                return null;
            }
        }
        
        /// <summary>
        /// 隐藏面板
        /// </summary>
        /// <param name="panelName">面板名称</param>
        public void HidePanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName))
            {
                Debug.LogError("UIManager: 面板名称为空");
                return;
            }
            
            UIPanel panel = GetActivePanel(panelName);
            if (panel != null)
            {
                // 隐藏面板
                panel.Hide();
                
                // 从活跃面板列表移�?
                _activePanels.Remove(panel);
                
                if (_enableLogging)
                    Debug.Log($"UIManager: 隐藏面板 {panelName}");
                
                // 触发事件
                OnPanelHidden?.Invoke(panel);
                
                // 销毁面�?
                UnityEngine.Object.Destroy(panel.gameObject);
            }
            else
            {
                Debug.LogWarning($"UIManager: 面板 {panelName} 未找到或未显示");
            }
        }
        
        /// <summary>
        /// 隐藏所有面�?
        /// </summary>
        public void HideAllPanels()
        {
            if (_enableLogging)
                Debug.Log("UIManager: 隐藏所有面板");
            
            // 隐藏所有活跃面�?
            for (int i = _activePanels.Count - 1; i >= 0; i--)
            {
                UIPanel panel = _activePanels[i];
                if (panel != null)
                {
                    panel.Hide();
                    OnPanelHidden?.Invoke(panel);
                    UnityEngine.Object.Destroy(panel.gameObject);
                }
            }
            
            _activePanels.Clear();
        }
        
        /// <summary>
        /// 获取活跃面板
        /// </summary>
        /// <param name="panelName">面板名称</param>
        /// <returns>面板实例</returns>
        public UIPanel GetActivePanel(string panelName)
        {
            return _activePanels.FirstOrDefault(p => p != null && p.PanelName == panelName);
        }
        
        /// <summary>
        /// 检查面板是否显�?
        /// </summary>
        /// <param name="panelName">面板名称</param>
        /// <returns>是否显示</returns>
        public bool IsPanelVisible(string panelName)
        {
            UIPanel panel = GetActivePanel(panelName);
            return panel != null && panel.IsVisible;
        }
        
        /// <summary>
        /// 创建面板实例
        /// </summary>
        /// <param name="panelName">面板名称</param>
        /// <returns>面板实例</returns>
        private UIPanel CreatePanel(string panelName)
        {
            // 从预制体创建面板
            if (_panelPrefabs.TryGetValue(panelName, out UIPanel prefab))
            {
                UIPanel panel = UnityEngine.Object.Instantiate(prefab, _panelContainer);
                panel.name = panelName;
                return panel;
            }
            
            // 尝试动态创建面�?
            return CreateDynamicPanel(panelName);
        }
        
        /// <summary>
        /// 动态创建面�?
        /// </summary>
        /// <param name="panelName">面板名称</param>
        /// <returns>面板实例</returns>
        private UIPanel CreateDynamicPanel(string panelName)
        {
            // 根据面板名称创建不同类型的面�?
            switch (panelName.ToLower())
            {
                case "gamehud":
                    return CreateGameHUDPanel();
                case "settings":
                    return CreateSettingsPanel();
                case "pause":
                    return CreatePausePanel();
                default:
                    Debug.LogWarning($"UIManager: 未知面板类型 {panelName}");
                    return null;
            }
        }
        
        /// <summary>
        /// 创建游戏HUD面板
        /// </summary>
        /// <returns>面板实例</returns>
        private UIPanel CreateGameHUDPanel()
        {
            GameObject panelGO = new GameObject("GameHUDPanel");
            GameHUDPanel panel = panelGO.AddComponent<GameHUDPanel>();
            
            // 设置面板属�?
            panel.PanelName = "GameHUD";
            panel.Priority = 1;
            
            // 设置RectTransform
            RectTransform rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.SetParent(_panelContainer);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            return panel;
        }
        
        /// <summary>
        /// 创建设置面板
        /// </summary>
        /// <returns>面板实例</returns>
        private UIPanel CreateSettingsPanel()
        {
            GameObject panelGO = new GameObject("SettingsPanel");
            SettingsPanel panel = panelGO.AddComponent<SettingsPanel>();
            
            // 设置面板属�?
            panel.PanelName = "Settings";
            panel.Priority = 10;
            
            // 设置RectTransform
            RectTransform rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.SetParent(_panelContainer);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            return panel;
        }
        
        /// <summary>
        /// 创建暂停面板
        /// </summary>
        /// <returns>面板实例</returns>
        private UIPanel CreatePausePanel()
        {
            GameObject panelGO = new GameObject("PausePanel");
            UIPanel panel = panelGO.AddComponent<UIPanel>();
            
            // 设置面板属�?
            panel.PanelName = "Pause";
            panel.Priority = 5;
            
            // 设置RectTransform
            RectTransform rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.SetParent(_panelContainer);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            return panel;
        }
        
        /// <summary>
        /// 隐藏优先级最低的面板
        /// </summary>
        private void HideLowestPriorityPanel()
        {
            if (_activePanels.Count == 0) return;
            
            UIPanel lowestPriorityPanel = _activePanels.OrderBy(p => p.Priority).First();
            if (lowestPriorityPanel != null)
            {
                HidePanel(lowestPriorityPanel.PanelName);
            }
        }
        
        /// <summary>
        /// 按优先级排序面板
        /// </summary>
        private void SortPanelsByPriority()
        {
            _activePanels.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            
            // 更新面板的显示顺�?
            for (int i = 0; i < _activePanels.Count; i++)
            {
                if (_activePanels[i] != null)
                {
                    _activePanels[i].transform.SetSiblingIndex(i);
                }
            }
        }
        
        /// <summary>
        /// 触发面板事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        public void TriggerPanelEvent(string eventName)
        {
            if (_enableLogging)
                Debug.Log($"UIManager: 触发面板事件 {eventName}");
            
            OnPanelEvent?.Invoke(eventName);
        }
        
        /// <summary>
        /// 关闭UI管理�?
        /// </summary>
        public void Shutdown()
        {
            if (_enableLogging)
                Debug.Log("UIManager: 关闭UI管理器");
            
            // 隐藏所有面板
            HideAllPanels();
            
            // 清理事件
            OnPanelShown = null;
            OnPanelHidden = null;
            OnPanelEvent = null;
        }
    }
    
    /// <summary>
    /// UI面板基类
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        [Header("面板设置")]
        [SerializeField] protected bool isVisible;
        [SerializeField] protected int priority;
        [SerializeField] protected string panelName = "";
        
        // 公共属性
        public bool IsVisible => isVisible;
        public int Priority { get => priority; set => priority = value; }
        public string PanelName { get => panelName; set => panelName = value; }
        
        // 事件
        public event Action OnShowEvent;
        public event Action OnHideEvent;
        
        /// <summary>
        /// 显示面板
        /// </summary>
        public virtual void Show()
        {
            if (isVisible) return;
            
            isVisible = true;
            gameObject.SetActive(true);
            
            OnShow();
            OnShowEvent?.Invoke();
        }
        
        /// <summary>
        /// 隐藏面板
        /// </summary>
        public virtual void Hide()
        {
            if (!isVisible) return;
            
            isVisible = false;
            gameObject.SetActive(false);
            
            OnHide();
            OnHideEvent?.Invoke();
        }
        
        /// <summary>
        /// 设置面板数据
        /// </summary>
        public virtual void SetData(object data)
        {
            // 子类重写此方法以处理数据
        }
        
        /// <summary>
        /// 显示时调用
        /// </summary>
        protected virtual void OnShow()
        {
            // 子类重写此方法
        }
        
        /// <summary>
        /// 隐藏时调用
        /// </summary>
        protected virtual void OnHide()
        {
            // 子类重写此方法
        }
        
        /// <summary>
        /// 更新面板
        /// </summary>
        public virtual void Update()
        {
            // 子类重写此方法
        }
    }
    
    /// <summary>
    /// 游戏HUD面板
    /// </summary>
    public class GameHUDPanel : UIPanel
    {
        [Header("HUD元素")]
        [SerializeField] private ProgressBar healthBar;
        [SerializeField] private Text scoreText;
        [SerializeField] private Image minimapImage;
        
        protected override void OnShow()
        {
            base.OnShow();
            
            // 初始化HUD元素
            if (healthBar != null)
            {
                healthBar.SetValue(1f);
            }
            
            if (scoreText != null)
            {
                scoreText.text = "0";
            }
        }
        
        public override void Update()
        {
            // 更新HUD信息
            UpdateHUDInfo();
        }
        
        /// <summary>
        /// 更新HUD信息
        /// </summary>
        private void UpdateHUDInfo()
        {
            // 这里可以更新血量、分数等信息
            // 具体实现取决于游戏逻辑
        }
        
        /// <summary>
        /// 设置血�?
        /// </summary>
        /// <param name="health">血量�?(0-1)</param>
        public void SetHealth(float health)
        {
            if (healthBar != null)
            {
                healthBar.SetValue(health);
            }
        }
        
        /// <summary>
        /// 设置分数
        /// </summary>
        /// <param name="score">分数</param>
        public void SetScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
            }
        }
    }
    
    /// <summary>
    /// 设置面板
    /// </summary>
    public class SettingsPanel : UIPanel
    {
        [Header("设置元素")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Dropdown resolutionDropdown;
        
        protected override void OnShow()
        {
            base.OnShow();
            
            // 初始化设置�?
            InitializeSettings();
        }
        
        /// <summary>
        /// 初始化设�?
        /// </summary>
        private void InitializeSettings()
        {
            // 初始化音量设�?
            if (volumeSlider != null)
            {
                volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            
            // 初始化质量设�?
            if (qualityDropdown != null)
            {
                qualityDropdown.value = PlayerPrefs.GetInt("Quality", 2);
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            }
            
            // 初始化分辨率设置
            if (resolutionDropdown != null)
            {
                resolutionDropdown.value = PlayerPrefs.GetInt("Resolution", 0);
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }
        }
        
        /// <summary>
        /// 音量改变回调
        /// </summary>
        private void OnVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("Volume", value);
            AudioListener.volume = value;
        }
        
        /// <summary>
        /// 质量改变回调
        /// </summary>
        private void OnQualityChanged(int index)
        {
            PlayerPrefs.SetInt("Quality", index);
            QualitySettings.SetQualityLevel(index);
        }
        
        /// <summary>
        /// 分辨率改变回�?
        /// </summary>
        private void OnResolutionChanged(int index)
        {
            PlayerPrefs.SetInt("Resolution", index);
            // 这里可以实现分辨率切换逻辑
        }
    }
    
    /// <summary>
    /// 进度条组�?
    /// </summary>
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Text valueText;
        
        public void SetValue(float value)
        {
            value = Mathf.Clamp01(value);
            
            if (fillImage != null)
            {
                fillImage.fillAmount = value;
            }
            
            if (valueText != null)
            {
                valueText.text = $"{value * 100:F0}%";
            }
        }
    }
}
