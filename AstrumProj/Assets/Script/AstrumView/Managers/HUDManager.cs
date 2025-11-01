using System.Collections.Generic;
using UnityEngine;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using TrueSync;
using Sharklib.ProgressBar;


namespace Astrum.View.Managers
{
    /// <summary>
    /// HUD管理器 - 管理所有HUD元素的显示和更新
    /// 使用单一Overlay Canvas + 屏幕空间映射的折中方案
    /// </summary>
    public class HUDManager : Singleton<HUDManager>
    {
        // HUD设置
        private Canvas hudCanvas;
        private Camera worldCamera;
        private GameObject hudPrefab; // HorizontalBoxWithShadow.prefab
        
        // HUD配置
        private Vector3 healthBarOffset = new Vector3(0, 2.5f, 0);
        private Vector2 healthBarSize = new Vector2(200, 40);
        private Vector3 healthBarScale = new Vector3(0.4f, 0.4f,0.4f);
        
        private Color fullHealthColor = Color.green;
        private Color lowHealthColor = Color.red;
        private float lowHealthThreshold = 0.3f;
        private float healthBarFadeTime = 2f;
        
        // HUD实例管理
        private Dictionary<long, HUDInstance> _hudInstances = new Dictionary<long, HUDInstance>();
        private Queue<GameObject> _hudPool = new Queue<GameObject>();
        
        protected override void Awake()
        {
            base.Awake();
        }
        
        public void Initialize(Canvas hudCanvas, Camera worldCamera)
        {
            this.hudCanvas = hudCanvas;
            this.worldCamera = worldCamera;
            InitializeHUDSystem();
        }
        
        private void InitializeHUDSystem()
        {
            // 确保Canvas设置正确
            if (hudCanvas == null)
            {
                // 在Singleton中无法直接获取Component，需要通过其他方式初始化
                ASLogger.Instance.Warning("HUDManager: Canvas未设置，需要在外部初始化");
            }
            
            if (hudCanvas != null)
            {
                hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                hudCanvas.sortingOrder = 100; // 确保HUD在最上层
            }
            
        }
        
        /// <summary>
        /// 注册实体的HUD
        /// </summary>
        public void RegisterHUD(long entityId, Entity entity, Vector3 worldPosition)
        {
            if (_hudInstances.ContainsKey(entityId))
            {
                ASLogger.Instance.Warning($"实体 {entityId} 的HUD已存在，跳过注册");
                return;
            }
            
            GameObject hudObject = GetHUDFromPool();
            if (hudObject == null)
            {
                ASLogger.Instance.Error("无法从对象池获取HUD实例");
                return;
            }
            
            HUDInstance hudInstance = new HUDInstance
            {
                EntityId = entityId,
                Entity = entity,
                GameObject = hudObject,
                WorldPosition = worldPosition,
                LastUpdateTime = Time.time
            };
            
            _hudInstances[entityId] = hudInstance;
            
            // 设置HUD位置
            UpdateHUDPosition(hudInstance);
            
            // 初始化HUD显示
            InitializeHUDDisplay(hudInstance);
            
        }
        
        /// <summary>
        /// 注销实体的HUD
        /// </summary>
        public void UnregisterHUD(long entityId)
        {
            if (!_hudInstances.TryGetValue(entityId, out var hudInstance))
            {
                return;
            }
            
            // 回收到对象池
            ReturnHUDToPool(hudInstance.GameObject);
            _hudInstances.Remove(entityId);
            
        }
        
        /// <summary>
        /// 更新HUD数据
        /// </summary>
        public void UpdateHUDData(long entityId, FP currentHealth, FP maxHealth, FP currentShield)
        {
            if (!_hudInstances.TryGetValue(entityId, out var hudInstance))
            {
                return;
            }
            
            // 更新血条
            UpdateHealthBar(hudInstance, currentHealth, maxHealth, currentShield);
            
            hudInstance.LastUpdateTime = Time.time;
        }
        
        /// <summary>
        /// 更新HUD世界位置
        /// </summary>
        public void UpdateHUDPosition(long entityId, Vector3 worldPosition)
        {
            if (!_hudInstances.TryGetValue(entityId, out var hudInstance))
            {
                return;
            }
            
            hudInstance.WorldPosition = worldPosition;
            UpdateHUDPosition(hudInstance);
        }
        
        private void Update()
        {
            // 更新所有HUD的位置
            foreach (var hudInstance in _hudInstances.Values)
            {
                UpdateHUDPosition(hudInstance);
            }
        }
        
        private void UpdateHUDPosition(HUDInstance hudInstance)
        {
            if (hudInstance.GameObject == null || worldCamera == null)
                return;
            
            // 世界坐标转屏幕坐标
            Vector3 screenPos = worldCamera.WorldToScreenPoint(hudInstance.WorldPosition + healthBarOffset);
            
            // 检查是否在屏幕范围内
            if (screenPos.z > 0 && screenPos.x >= 0 && screenPos.x <= Screen.width && 
                screenPos.y >= 0 && screenPos.y <= Screen.height)
            {
                hudInstance.GameObject.SetActive(true);
                
                // 设置屏幕位置
                RectTransform rectTransform = hudInstance.GameObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.position = screenPos;
                }
            }
            else
            {
                hudInstance.GameObject.SetActive(false);
            }
        }
        
        private void InitializeHUDDisplay(HUDInstance hudInstance)
        {
            // 设置HUD大小
            RectTransform rectTransform = hudInstance.GameObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = healthBarSize;
                rectTransform.localScale = healthBarScale;
            }
            
            // 初始化血条显示
            UpdateHealthBar(hudInstance, FP.Zero, FP.Zero, FP.Zero);
        }
        
        private void UpdateHealthBar(HUDInstance hudInstance, FP currentHealth, FP maxHealth, FP currentShield)
        {
            if (hudInstance.GameObject == null)
                return;
            
            // 查找ProgressBarPro组件
            var progressBar = hudInstance.GameObject.GetComponentInChildren<ProgressBarPro>();
            if (progressBar == null)
            {
                ASLogger.Instance.Warning($"实体 {hudInstance.EntityId} 的HUD缺少ProgressBarPro组件");
                return;
            }
            
            // 转换数值
            float currentHealthFloat = (float)currentHealth;
            float maxHealthFloat = (float)maxHealth;
            
            // 计算血量百分比用于血条显示
            float healthPercent = maxHealthFloat > 0f ? currentHealthFloat / maxHealthFloat : 0f;
            
            // 更新血条进度（使用百分比）
            progressBar.SetValue(healthPercent);
            
            // 配置文本显示组件以显示实际数值
            var textView = hudInstance.GameObject.GetComponentInChildren<BarViewValueText>();
            if (textView != null)
            {
                // 配置显示参数
                textView.ConfigureDisplay(0f, maxHealthFloat, true, "", 0);
                
                // 强制更新显示
                textView.UpdateView(healthPercent, healthPercent);
            }
            
            // 根据血量设置颜色
            Color healthColor = healthPercent <= lowHealthThreshold ? lowHealthColor : fullHealthColor;
            
            // 查找并更新血条颜色组件
            var colorView = hudInstance.GameObject.GetComponentInChildren<BarViewColor>();
            if (colorView != null)
            {
                colorView.SetBarColor(healthColor);
            }
            
            // 处理护盾显示（如果有护盾组件）
            if (currentShield > FP.Zero)
            {
                ASLogger.Instance.Debug($"实体 {hudInstance.EntityId} 有护盾: {(float)currentShield:F1}");
                // 可以在这里添加护盾条的逻辑
            }
        }
        
        private GameObject GetHUDFromPool()
        {
            if (_hudPool.Count > 0)
            {
                return _hudPool.Dequeue();
            }
            
            return CreateHUDInstance();
        }
        
        private GameObject CreateHUDInstance()
        {
            if (hudPrefab == null)
            {
                hudPrefab = ResourceManager.Instance.LoadResource<GameObject>("Assets/ArtRes/UI/HUD/HorizontalBoxWithShadow.prefab");
            }
            
            GameObject hudObject = UnityEngine.Object.Instantiate(hudPrefab, hudCanvas.transform);
            hudObject.SetActive(false);
            return hudObject;
        }
        
        private void ReturnHUDToPool(GameObject hudObject)
        {
            if (hudObject != null)
            {
                hudObject.SetActive(false);
                _hudPool.Enqueue(hudObject);
            }
        }
        
        /// <summary>
        /// 清理所有HUD实例
        /// </summary>
        public void ClearAll()
        {
            foreach (var hudInstance in _hudInstances.Values)
            {
                ReturnHUDToPool(hudInstance.GameObject);
            }
            _hudInstances.Clear();
            ASLogger.Instance.Info("HUDManager: 已清理所有HUD实例");
        }
        
    }
    
    /// <summary>
    /// HUD实例数据
    /// </summary>
    public class HUDInstance
    {
        public long EntityId;
        public Entity Entity;
        public GameObject GameObject;
        public Vector3 WorldPosition;
        public float LastUpdateTime;
    }
}
