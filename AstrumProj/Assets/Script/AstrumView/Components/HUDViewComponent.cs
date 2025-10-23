using UnityEngine;
using UnityEngine.UI;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Stats;
using TrueSync;

namespace Astrum.View.Components
{
    /// <summary>
    /// HUD视图组件 - 处理实体的HUD显示（血条、状态等）
    /// </summary>
    public class HUDViewComponent : ViewComponent
    {
        // 血条设置
        private bool showHealthBar = true;
        private Vector3 healthBarOffset = new Vector3(0, 2.5f, 0);
        private Vector2 healthBarSize = new Vector2(120, 12);
        private Color fullHealthColor = Color.green;
        private Color lowHealthColor = Color.red;
        private float lowHealthThreshold = 0.3f;
        private float healthBarFadeTime = 2f;
        
        // 护盾设置
        private bool showShieldBar = true;
        private Color shieldColor = Color.cyan;
        
        // 血量状态
        private FP _currentHealth = FP.Zero;
        private FP _maxHealth = FP.Zero;
        private FP _currentShield = FP.Zero;
        private bool _isAlive = true;
        
        // UI组件
        private GameObject _hudCanvasObject;
        private GameObject _healthBarObject;
        private Slider _healthSlider;
        private Image _healthFillImage;
        private Image _healthBackgroundImage;
        private GameObject _shieldBarObject;
        private Slider _shieldSlider;
        private Image _shieldFillImage;
        
        // 显示控制
        private float _healthBarTimer = 0f;
        private bool _healthBarVisible = false;
        
        protected override void OnInitialize()
        {
            ASLogger.Instance.Info($"HUDViewComponent: 初始化HUD视图组件，ID: {_componentId}");
            
            if (_ownerEntityView != null && _ownerEntityView.GameObject != null)
            {
                CreateHUDCanvas();
                if (showHealthBar)
                {
                    CreateHealthBar();
                }
                if (showShieldBar)
                {
                    CreateShieldBar();
                }
                
                // 初始同步数据
                SyncHealthData();
            }
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            if (!_isEnabled || _ownerEntityView == null) return;
            
            // 更新HUD位置
            UpdateHUDPosition();
            
            // 更新血条显示
            UpdateHealthBarDisplay();
            
            // 更新护盾显示
            UpdateShieldBarDisplay();
            
            // 处理血条淡出
            UpdateHealthBarVisibility(deltaTime);
        }
        
        protected override void OnDestroy()
        {
            ASLogger.Instance.Info($"HUDViewComponent: 销毁HUD视图组件，ID: {_componentId}");
            
            // 销毁HUD对象
            if (_hudCanvasObject != null)
            {
                UnityEngine.Object.Destroy(_hudCanvasObject);
                _hudCanvasObject = null;
            }
        }
        
        protected override void OnSyncData(object data)
        {
            // 从逻辑层同步血量数据
            SyncHealthData();
        }
        
        /// <summary>
        /// 创建HUD画布
        /// </summary>
        private void CreateHUDCanvas()
        {
            _hudCanvasObject = new GameObject("HUDCanvas");
            _hudCanvasObject.transform.SetParent(_ownerEntityView.GameObject.transform);
            _hudCanvasObject.transform.localPosition = Vector3.zero;
            
            // 创建Canvas
            var canvas = _hudCanvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 100; // 确保HUD在最上层
            
            // 创建CanvasScaler
            var canvasScaler = _hudCanvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            // 创建GraphicRaycaster
            _hudCanvasObject.AddComponent<GraphicRaycaster>();
        }
        
        /// <summary>
        /// 创建血条
        /// </summary>
        private void CreateHealthBar()
        {
            _healthBarObject = new GameObject("HealthBar");
            _healthBarObject.transform.SetParent(_hudCanvasObject.transform);
            _healthBarObject.transform.localPosition = healthBarOffset;
            
            // 创建血条背景
            var background = new GameObject("Background");
            background.transform.SetParent(_healthBarObject.transform);
            var backgroundRect = background.AddComponent<RectTransform>();
            backgroundRect.sizeDelta = healthBarSize;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            
            _healthBackgroundImage = background.AddComponent<Image>();
            _healthBackgroundImage.color = new Color(0, 0, 0, 0.6f);
            
            // 创建血条
            var healthBar = new GameObject("HealthBar");
            healthBar.transform.SetParent(_healthBarObject.transform);
            var healthBarRect = healthBar.AddComponent<RectTransform>();
            healthBarRect.sizeDelta = healthBarSize;
            healthBarRect.anchorMin = Vector2.zero;
            healthBarRect.anchorMax = Vector2.one;
            healthBarRect.offsetMin = Vector2.zero;
            healthBarRect.offsetMax = Vector2.zero;
            
            _healthSlider = healthBar.AddComponent<Slider>();
            _healthSlider.minValue = 0f;
            _healthSlider.maxValue = 1f;
            _healthSlider.value = 1f;
            _healthSlider.interactable = false;
            
            // 创建血条填充
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(healthBar.transform);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            _healthFillImage = fill.AddComponent<Image>();
            _healthFillImage.color = fullHealthColor;
            
            _healthSlider.fillRect = fillRect;
        }
        
        /// <summary>
        /// 创建护盾条
        /// </summary>
        private void CreateShieldBar()
        {
            _shieldBarObject = new GameObject("ShieldBar");
            _shieldBarObject.transform.SetParent(_hudCanvasObject.transform);
            _shieldBarObject.transform.localPosition = healthBarOffset + new Vector3(0, -0.3f, 0);
            
            // 创建护盾条
            var shieldBar = new GameObject("ShieldBar");
            shieldBar.transform.SetParent(_shieldBarObject.transform);
            var shieldBarRect = shieldBar.AddComponent<RectTransform>();
            shieldBarRect.sizeDelta = new Vector2(healthBarSize.x, healthBarSize.y * 0.6f);
            shieldBarRect.anchorMin = Vector2.zero;
            shieldBarRect.anchorMax = Vector2.one;
            shieldBarRect.offsetMin = Vector2.zero;
            shieldBarRect.offsetMax = Vector2.zero;
            
            _shieldSlider = shieldBar.AddComponent<Slider>();
            _shieldSlider.minValue = 0f;
            _shieldSlider.maxValue = 1f;
            _shieldSlider.value = 0f;
            _shieldSlider.interactable = false;
            
            // 创建护盾条填充
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(shieldBar.transform);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            _shieldFillImage = fill.AddComponent<Image>();
            _shieldFillImage.color = shieldColor;
            
            _shieldSlider.fillRect = fillRect;
        }
        
        /// <summary>
        /// 更新HUD位置
        /// </summary>
        private void UpdateHUDPosition()
        {
            if (_hudCanvasObject != null && _ownerEntityView != null)
            {
                // 让HUD始终面向摄像机
                if (Camera.main != null)
                {
                    _hudCanvasObject.transform.LookAt(Camera.main.transform);
                    _hudCanvasObject.transform.Rotate(0, 180, 0);
                }
            }
        }
        
        /// <summary>
        /// 更新血条显示
        /// </summary>
        private void UpdateHealthBarDisplay()
        {
            if (_healthSlider != null && _healthFillImage != null)
            {
                // 更新血条值
                float healthPercent = _maxHealth > FP.Zero ? (float)(_currentHealth / _maxHealth) : 0f;
                _healthSlider.value = healthPercent;
                
                // 更新血条颜色
                Color targetColor = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
                _healthFillImage.color = targetColor;
                
                // 根据血量显示/隐藏血条
                bool shouldShow = showHealthBar && _isAlive && (healthPercent <= 1f || _healthBarVisible);
                _healthBarObject.SetActive(shouldShow);
                
                if (shouldShow)
                {
                    _healthBarVisible = true;
                    _healthBarTimer = 0f;
                }
            }
        }
        
        /// <summary>
        /// 更新护盾条显示
        /// </summary>
        private void UpdateShieldBarDisplay()
        {
            if (_shieldSlider != null && _shieldFillImage != null)
            {
                // 更新护盾条值
                float shieldPercent = _maxHealth > FP.Zero ? (float)(_currentShield / _maxHealth) : 0f;
                _shieldSlider.value = shieldPercent;
                
                // 根据护盾值显示/隐藏护盾条
                bool shouldShow = showShieldBar && _currentShield > FP.Zero;
                _shieldBarObject.SetActive(shouldShow);
            }
        }
        
        /// <summary>
        /// 更新血条可见性（淡出效果）
        /// </summary>
        private void UpdateHealthBarVisibility(float deltaTime)
        {
            if (_healthBarVisible && _healthBarObject.activeInHierarchy)
            {
                _healthBarTimer += deltaTime;
                
                if (_healthBarTimer >= healthBarFadeTime)
                {
                    float healthPercent = _maxHealth > FP.Zero ? (float)(_currentHealth / _maxHealth) : 0f;
                    if (healthPercent > 1f)
                    {
                        _healthBarObject.SetActive(false);
                        _healthBarVisible = false;
                    }
                }
            }
        }
        
        /// <summary>
        /// 同步血量数据
        /// </summary>
        private void SyncHealthData()
        {
            if (_ownerEntityView?.OwnerEntity == null) return;
            
            var entity = _ownerEntityView.OwnerEntity;
            var dynamicStats = entity.GetComponent<DynamicStatsComponent>();
            var derivedStats = entity.GetComponent<DerivedStatsComponent>();
            
            if (dynamicStats != null && derivedStats != null)
            {
                // 获取当前血量和最大血量
                _currentHealth = dynamicStats.Get(DynamicResourceType.CURRENT_HP);
                _maxHealth = derivedStats.Get(StatType.HP);
                _currentShield = dynamicStats.Get(DynamicResourceType.SHIELD);
                _isAlive = _currentHealth > FP.Zero;
                
                ASLogger.Instance.Debug($"HUDViewComponent: 同步血量数据，当前: {_currentHealth}/{_maxHealth}, 护盾: {_currentShield}");
            }
        }
        
        /// <summary>
        /// 设置血条可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        public void SetHealthBarVisible(bool visible)
        {
            if (_healthBarObject != null)
            {
                _healthBarObject.SetActive(visible);
                _healthBarVisible = visible;
                if (visible)
                {
                    _healthBarTimer = 0f;
                }
            }
        }
        
        /// <summary>
        /// 强制显示血条
        /// </summary>
        public void ForceShowHealthBar()
        {
            SetHealthBarVisible(true);
            _healthBarTimer = 0f;
        }
        
        /// <summary>
        /// 获取当前血量百分比
        /// </summary>
        /// <returns>血量百分比</returns>
        public float GetHealthPercentage()
        {
            return _maxHealth > FP.Zero ? (float)(_currentHealth / _maxHealth) : 0f;
        }
        
        /// <summary>
        /// 获取护盾百分比
        /// </summary>
        /// <returns>护盾百分比</returns>
        public float GetShieldPercentage()
        {
            return _maxHealth > FP.Zero ? (float)(_currentShield / _maxHealth) : 0f;
        }
    }
}
