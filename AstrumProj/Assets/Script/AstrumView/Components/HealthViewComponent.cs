using UnityEngine;
using UnityEngine.UI;
using Astrum.CommonBase;

namespace Astrum.View.Components
{
    /// <summary>
    /// 健康视图组件 - 处理实体的血量表现
    /// </summary>
    public class HealthViewComponent : ViewComponent
    {
        [Header("血量设置")]
        [SerializeField] private bool showHealthBar = true;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 2f, 0);
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;
        [SerializeField] private float lowHealthThreshold = 0.3f;
        
        // 血量状态
        private float _currentHealth = 100f;
        private float _maxHealth = 100f;
        private bool _isAlive = true;
        
        // UI组件
        private GameObject _healthBarObject;
        private Slider _healthSlider;
        private Image _healthFillImage;
        private Canvas _healthCanvas;
        
        // 特效相关
        private ParticleSystem _damageEffect;
        private ParticleSystem _healEffect;
        private ParticleSystem _deathEffect;
        
        protected override void OnInitialize()
        {
            
            if (_ownerEntityView != null && _ownerEntityView.GameObject != null)
            {
                // 创建血量条
                if (showHealthBar)
                {
                    CreateHealthBar();
                }
                
                // 获取特效组件
                GetEffectComponents();
            }
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            if (!_isEnabled || _ownerEntityView == null) return;
            
            // 更新血量条位置
            UpdateHealthBarPosition();
            
            // 更新血量条显示
            UpdateHealthBarDisplay();
        }
        
        protected override void OnDestroy()
        {
            ASLogger.Instance.Info($"HealthViewComponent: 销毁健康视图组件，ID: {_componentId}");
            
            // 销毁血量条
            if (_healthBarObject != null)
            {
                UnityEngine.Object.Destroy(_healthBarObject);
                _healthBarObject = null;
            }
        }
        
        protected override void OnSyncData(object data)
        {
            if (data is HealthData healthData)
            {
                float previousHealth = _currentHealth;
                
                // 更新血量数据
                _currentHealth = healthData.CurrentHealth;
                _maxHealth = healthData.MaxHealth;
                _isAlive = healthData.IsAlive;
                
                // 检查血量变化
                if (_currentHealth < previousHealth)
                {
                    // 受到伤害
                    OnDamageTaken(previousHealth - _currentHealth);
                }
                else if (_currentHealth > previousHealth)
                {
                    // 受到治疗
                    OnHealthRestored(_currentHealth - previousHealth);
                }
                
                // 检查死亡
                if (!_isAlive && previousHealth > 0)
                {
                    OnDeath();
                }
                
                ASLogger.Instance.Debug($"HealthViewComponent: 同步血量数据，当前: {_currentHealth}/{_maxHealth}");
            }
        }
        
        /// <summary>
        /// 创建血量条
        /// </summary>
        private void CreateHealthBar()
        {
            // 创建血量条对象
            _healthBarObject = new GameObject("HealthBar");
            _healthBarObject.transform.SetParent(_ownerEntityView.GameObject.transform);
            _healthBarObject.transform.localPosition = healthBarOffset;
            
            // 创建Canvas
            _healthCanvas = _healthBarObject.AddComponent<Canvas>();
            _healthCanvas.renderMode = RenderMode.WorldSpace;
            _healthCanvas.worldCamera = Camera.main;
            
            // 创建CanvasScaler
            var canvasScaler = _healthBarObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            // 创建血量条背景
            var background = new GameObject("Background");
            background.transform.SetParent(_healthBarObject.transform);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.5f);
            background.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 10);
            
            // 创建血量条
            var healthBar = new GameObject("HealthBar");
            healthBar.transform.SetParent(_healthBarObject.transform);
            _healthSlider = healthBar.AddComponent<Slider>();
            _healthSlider.minValue = 0f;
            _healthSlider.maxValue = 1f;
            _healthSlider.value = 1f;
            
            // 创建血量条填充
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(healthBar.transform);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;
            
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            _healthFillImage = fill.AddComponent<Image>();
            _healthFillImage.color = fullHealthColor;
            fill.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            fill.GetComponent<RectTransform>().anchorMax = Vector2.one;
            fill.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            
            _healthSlider.fillRect = fill.GetComponent<RectTransform>();
        }
        
        /// <summary>
        /// 获取特效组件
        /// </summary>
        private void GetEffectComponents()
        {
            if (_ownerEntityView.GameObject != null)
            {
                // 查找特效组件
                _damageEffect = _ownerEntityView.GameObject.GetComponentInChildren<ParticleSystem>();
                _healEffect = _ownerEntityView.GameObject.GetComponentInChildren<ParticleSystem>();
                _deathEffect = _ownerEntityView.GameObject.GetComponentInChildren<ParticleSystem>();
            }
        }
        
        /// <summary>
        /// 更新血量条位置
        /// </summary>
        private void UpdateHealthBarPosition()
        {
            if (_healthBarObject != null && _ownerEntityView != null)
            {
                // 让血量条始终面向摄像机
                if (Camera.main != null)
                {
                    _healthBarObject.transform.LookAt(Camera.main.transform);
                    _healthBarObject.transform.Rotate(0, 180, 0);
                }
            }
        }
        
        /// <summary>
        /// 更新血量条显示
        /// </summary>
        private void UpdateHealthBarDisplay()
        {
            if (_healthSlider != null && _healthFillImage != null)
            {
                // 更新血量条值
                float healthPercent = _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
                _healthSlider.value = healthPercent;
                
                // 更新血量条颜色
                Color targetColor = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
                _healthFillImage.color = targetColor;
                
                // 根据血量显示/隐藏血量条
                bool shouldShow = showHealthBar && _isAlive && healthPercent < 1f;
                _healthBarObject.SetActive(shouldShow);
            }
        }
        
        /// <summary>
        /// 受到伤害时的处理
        /// </summary>
        /// <param name="damage">伤害值</param>
        private void OnDamageTaken(float damage)
        {
            ASLogger.Instance.Info($"HealthViewComponent: 受到伤害 {damage}");
            
            // 播放伤害特效
            if (_damageEffect != null)
            {
                _damageEffect.Play();
            }
            
            // 播放伤害音效
            PlayDamageSound();
            
            // 显示伤害数字
            ShowDamageNumber(damage);
        }
        
        /// <summary>
        /// 恢复血量时的处理
        /// </summary>
        /// <param name="healAmount">恢复量</param>
        private void OnHealthRestored(float healAmount)
        {
            ASLogger.Instance.Info($"HealthViewComponent: 恢复血量 {healAmount}");
            
            // 播放治疗特效
            if (_healEffect != null)
            {
                _healEffect.Play();
            }
            
            // 播放治疗音效
            PlayHealSound();
        }
        
        /// <summary>
        /// 死亡时的处理
        /// </summary>
        private void OnDeath()
        {
            ASLogger.Instance.Info($"HealthViewComponent: 实体死亡");
            
            // 播放死亡特效
            if (_deathEffect != null)
            {
                _deathEffect.Play();
            }
            
            // 播放死亡音效
            PlayDeathSound();
            
            // 隐藏血量条
            if (_healthBarObject != null)
            {
                _healthBarObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 播放伤害音效
        /// </summary>
        private void PlayDamageSound()
        {
            // TODO: 实现伤害音效播放
        }
        
        /// <summary>
        /// 播放治疗音效
        /// </summary>
        private void PlayHealSound()
        {
            // TODO: 实现治疗音效播放
        }
        
        /// <summary>
        /// 播放死亡音效
        /// </summary>
        private void PlayDeathSound()
        {
            // TODO: 实现死亡音效播放
        }
        
        /// <summary>
        /// 显示伤害数字
        /// </summary>
        /// <param name="damage">伤害值</param>
        private void ShowDamageNumber(float damage)
        {
            // TODO: 实现伤害数字显示
        }
        
        /// <summary>
        /// 设置血量
        /// </summary>
        /// <param name="currentHealth">当前血量</param>
        /// <param name="maxHealth">最大血量</param>
        public void SetHealth(float currentHealth, float maxHealth)
        {
            _currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            _maxHealth = maxHealth;
            _isAlive = _currentHealth > 0f;
        }
        
        /// <summary>
        /// 获取当前血量
        /// </summary>
        /// <returns>当前血量</returns>
        public float GetCurrentHealth()
        {
            return _currentHealth;
        }
        
        /// <summary>
        /// 获取最大血量
        /// </summary>
        /// <returns>最大血量</returns>
        public float GetMaxHealth()
        {
            return _maxHealth;
        }
        
        /// <summary>
        /// 获取血量百分比
        /// </summary>
        /// <returns>血量百分比</returns>
        public float GetHealthPercentage()
        {
            return _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
        }
        
        /// <summary>
        /// 是否存活
        /// </summary>
        /// <returns>是否存活</returns>
        public bool IsAlive()
        {
            return _isAlive;
        }
    }
    
    /// <summary>
    /// 血量数据
    /// </summary>
    public class HealthData
    {
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }
        public bool IsAlive { get; set; }
        
        public HealthData(float currentHealth, float maxHealth, bool isAlive = true)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            IsAlive = isAlive;
        }
    }
} 