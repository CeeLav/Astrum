using UnityEngine;
using System;
using Astrum.CommonBase;
using System.Collections.Generic;

namespace Astrum.View.Core
{
    /// <summary>
    /// 环境组件
    /// 管理Stage的环境设置，如光照、天气等
    /// </summary>
    public class Environment : MonoBehaviour
    {
        [Header("环境设置")]
        [SerializeField] private string environmentType = "Default";
        [SerializeField] private Vector3 lighting = Vector3.one;
        [SerializeField] private float weather = 0f;
        
        // 环境设置
        private Dictionary<string, object> _settings = new Dictionary<string, object>();
        
        // 公共属�?
        public string EnvironmentType => environmentType;
        public Vector3 Lighting => lighting;
        public float Weather => weather;
        public Dictionary<string, object> Settings => _settings;
        
        // 事件
        public event Action<Environment> OnEnvironmentChanged;
        
        private void Awake()
        {
            // 初始化默认设�?
            InitializeDefaultSettings();
        }
        
        /// <summary>
        /// 初始化默认设�?
        /// </summary>
        private void InitializeDefaultSettings()
        {
            _settings["ambientLight"] = Color.white;
            _settings["fogEnabled"] = false;
            _settings["fogColor"] = Color.gray;
            _settings["fogDensity"] = 0.01f;
            _settings["windSpeed"] = 0f;
            _settings["windDirection"] = Vector3.forward;
        }
        
        /// <summary>
        /// 与环境数据同�?
        /// </summary>
        /// <param name="environmentData">环境数据</param>
        public void SyncWithData(EnvironmentData environmentData)
        {
            if (environmentData == null) return;
            
            bool changed = false;
            
            // 更新环境类型
            if (environmentType != environmentData.EnvironmentType)
            {
                environmentType = environmentData.EnvironmentType;
                changed = true;
            }
            
            // 更新光照
            if (lighting != environmentData.Lighting)
            {
                lighting = environmentData.Lighting;
                changed = true;
            }
            
            // 更新天气
            if (weather != environmentData.Weather)
            {
                weather = environmentData.Weather;
                changed = true;
            }
            
            // 更新设置
            if (environmentData.Settings != null)
            {
                foreach (var kvp in environmentData.Settings)
                {
                    if (!_settings.ContainsKey(kvp.Key) || !_settings[kvp.Key].Equals(kvp.Value))
                    {
                        _settings[kvp.Key] = kvp.Value;
                        changed = true;
                    }
                }
            }
            
            // 应用环境设置
            if (changed)
            {
                ApplyEnvironmentSettings();
                OnEnvironmentChanged?.Invoke(this);
            }
        }
        
        /// <summary>
        /// 应用环境设置
        /// </summary>
        private void ApplyEnvironmentSettings()
        {
            // 应用光照设置
            if (_settings.ContainsKey("ambientLight") && _settings["ambientLight"] is Color ambientColor)
            {
                RenderSettings.ambientLight = ambientColor * new Color(lighting.x, lighting.y, lighting.z);
            }
            
            // 应用雾效设置
            if (_settings.ContainsKey("fogEnabled") && _settings["fogEnabled"] is bool fogEnabled)
            {
                RenderSettings.fog = fogEnabled;
                
                if (fogEnabled)
                {
                    if (_settings.ContainsKey("fogColor") && _settings["fogColor"] is Color fogColor)
                    {
                        RenderSettings.fogColor = fogColor;
                    }
                    
                    if (_settings.ContainsKey("fogDensity") && _settings["fogDensity"] is float fogDensity)
                    {
                        RenderSettings.fogMode = FogMode.Exponential;
                        RenderSettings.fogDensity = fogDensity;
                    }
                }
            }
            
            // 应用天气效果
            ApplyWeatherEffects();
        }
        
        /// <summary>
        /// 应用天气效果
        /// </summary>
        private void ApplyWeatherEffects()
        {
            // 根据天气值调整环�?
            if (weather > 0.7f)
            {
                // 雨天效果
                ApplyRainEffect();
            }
            else if (weather > 0.3f)
            {
                // 阴天效果
                ApplyCloudyEffect();
            }
            else
            {
                // 晴天效果
                ApplySunnyEffect();
            }
        }
        
        /// <summary>
        /// 应用雨天效果
        /// </summary>
        private void ApplyRainEffect()
        {
            // 降低环境光照
            RenderSettings.ambientLight *= 0.6f;
            
            // 启用雾效
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.5f, 0.5f, 0.6f);
            RenderSettings.fogDensity = 0.02f;
        }
        
        /// <summary>
        /// 应用阴天效果
        /// </summary>
        private void ApplyCloudyEffect()
        {
            // 降低环境光照
            RenderSettings.ambientLight *= 0.8f;
            
            // 轻微雾效
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.7f, 0.7f, 0.7f);
            RenderSettings.fogDensity = 0.005f;
        }
        
        /// <summary>
        /// 应用晴天效果
        /// </summary>
        private void ApplySunnyEffect()
        {
            // 正常环境光照
            RenderSettings.ambientLight = Color.white * new Color(lighting.x, lighting.y, lighting.z);
            
            // 关闭雾效
            RenderSettings.fog = false;
        }
        
        /// <summary>
        /// 设置环境类型
        /// </summary>
        /// <param name="type">环境类型</param>
        public void SetEnvironmentType(string type)
        {
            if (environmentType != type)
            {
                environmentType = type;
                ApplyEnvironmentSettings();
                OnEnvironmentChanged?.Invoke(this);
            }
        }
        
        /// <summary>
        /// 设置光照
        /// </summary>
        /// <param name="lighting">光照�?/param>
        public void SetLighting(Vector3 lighting)
        {
            if (this.lighting != lighting)
            {
                this.lighting = lighting;
                ApplyEnvironmentSettings();
                OnEnvironmentChanged?.Invoke(this);
            }
        }
        
        /// <summary>
        /// 设置天气
        /// </summary>
        /// <param name="weather">天气�?/param>
        public void SetWeather(float weather)
        {
            if (this.weather != weather)
            {
                this.weather = Mathf.Clamp01(weather);
                ApplyWeatherEffects();
                OnEnvironmentChanged?.Invoke(this);
            }
        }
        
        /// <summary>
        /// 设置环境参数
        /// </summary>
        /// <param name="key">参数�?/param>
        /// <param name="value">参数�?/param>
        public void SetSetting(string key, object value)
        {
            if (!_settings.ContainsKey(key) || !_settings[key].Equals(value))
            {
                _settings[key] = value;
                ApplyEnvironmentSettings();
                OnEnvironmentChanged?.Invoke(this);
            }
        }
        
        /// <summary>
        /// 获取环境参数
        /// </summary>
        /// <param name="key">参数�?/param>
        /// <returns>参数�?/returns>
        public object GetSetting(string key)
        {
            _settings.TryGetValue(key, out object value);
            return value;
        }
        
        /// <summary>
        /// 获取环境状态信息
        /// </summary>
        /// <returns>状态信息</returns>
        public string GetEnvironmentStatus()
        {
            return $"类型: {environmentType}, 光照: {lighting}, 天气: {weather:F2}";
        }
        
        /// <summary>
        /// 更新环境
        /// </summary>
        public virtual void Update()
        {
            // 环境更新逻辑，子类可以重写
        }
    }
} 
