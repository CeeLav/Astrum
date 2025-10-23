using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astrum.Client.Managers.GameModes
{
    /// <summary>
    /// 游戏模式配置基类
    /// </summary>
    [Serializable]
    public class GameModeConfig
    {
        [Header("基础配置")]
        public string ModeName = "";
        public bool AutoSave = true;
        public float UpdateInterval = 0.016f; // 60 FPS
        
        [Header("自定义设置")]
        public Dictionary<string, object> CustomSettings = new Dictionary<string, object>();
        
        /// <summary>
        /// 获取自定义设置
        /// </summary>
        public T GetCustomSetting<T>(string key, T defaultValue = default)
        {
            if (CustomSettings.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// 设置自定义设置
        /// </summary>
        public void SetCustomSetting<T>(string key, T value)
        {
            CustomSettings[key] = value;
        }
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static GameModeConfig CreateDefault(string modeName)
        {
            return new GameModeConfig
            {
                ModeName = modeName,
                AutoSave = true,
                UpdateInterval = 0.016f,
                CustomSettings = new Dictionary<string, object>()
            };
        }
    }
}
