using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 运行时日志管理器
    /// 负责在运行时加载和管理日志配置
    /// </summary>
    public class RuntimeLogManager
    {
        private LogManagerConfig _logConfig;
        private bool _enableRuntimeControl = true;
        private bool _autoLoadConfig = true;
        private bool _showDebugInfo = false;
        
        private static RuntimeLogManager _instance;
        private bool _isInitialized = false;
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static RuntimeLogManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RuntimeLogManager();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// 当前配置
        /// </summary>
        public LogManagerConfig CurrentConfig => _logConfig;
        
        /// <summary>
        /// 是否启用运行时控制
        /// </summary>
        public bool EnableRuntimeControl
        {
            get => _enableRuntimeControl;
            set => _enableRuntimeControl = value;
        }
        
        /// <summary>
        /// 是否自动加载配置
        /// </summary>
        public bool AutoLoadConfig
        {
            get => _autoLoadConfig;
            set => _autoLoadConfig = value;
        }
        
        /// <summary>
        /// 是否显示调试信息
        /// </summary>
        public bool ShowDebugInfo
        {
            get => _showDebugInfo;
            set => _showDebugInfo = value;
        }
        
        private RuntimeLogManager()
        {
            Initialize();
        }
        
        /// <summary>
        /// 初始化
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized) return;
            
            // 尝试自动加载配置
            if (_logConfig == null)
            {
                LoadConfig();
            }
            
            // 设置到LogFilter
            if (_logConfig != null)
            {
                LogFilter.SetConfig(_logConfig);
                _isInitialized = true;
                
                if (_showDebugInfo)
                {
                    Debug.Log($"[RuntimeLogManager] 配置已加载: {_logConfig.name}");
                }
            }
            else
            {
                Debug.LogWarning("[RuntimeLogManager] 未找到日志配置，将使用默认设置");
            }
        }
        
        /// <summary>
        /// 加载配置
        /// </summary>
        public void LoadConfig()
        {
            if (_logConfig == null)
            {
                // 尝试从Resources文件夹加载
                _logConfig = Resources.Load<LogManagerConfig>("LogManagerConfig");
                
                if (_logConfig == null)
                {
                    // 尝试从项目根目录加载
                    _logConfig = Resources.LoadAll<LogManagerConfig>("").FirstOrDefault();
                }
            }
            
            if (_logConfig != null)
            {
                LogFilter.SetConfig(_logConfig);
                _isInitialized = true;
                
                if (_showDebugInfo)
                {
                    Debug.Log($"[RuntimeLogManager] 配置已加载: {_logConfig.name}");
                }
            }
            else
            {
                Debug.LogWarning("[RuntimeLogManager] 未找到日志配置文件");
            }
        }
        
        /// <summary>
        /// 设置配置
        /// </summary>
        public void SetConfig(LogManagerConfig config)
        {
            _logConfig = config;
            if (config != null)
            {
                LogFilter.SetConfig(config);
                _isInitialized = true;
                
                if (_showDebugInfo)
                {
                    Debug.Log($"[RuntimeLogManager] 配置已设置: {config.name}");
                }
            }
        }
        
        /// <summary>
        /// 启用/禁用分类
        /// </summary>
        public void SetCategoryEnabled(string category, bool enabled)
        {
            if (!_enableRuntimeControl || !_isInitialized)
            {
                Debug.LogWarning("[RuntimeLogManager] 运行时控制未启用或未初始化");
                return;
            }
            
            LogFilter.SetCategoryEnabled(category, enabled);
            
            if (_showDebugInfo)
            {
                Debug.Log($"[RuntimeLogManager] 分类 '{category}' 已{(enabled ? "启用" : "禁用")}");
            }
        }
        
        /// <summary>
        /// 启用/禁用单个日志
        /// </summary>
        public void SetLogEnabled(string logId, bool enabled)
        {
            if (!_enableRuntimeControl || !_isInitialized)
            {
                Debug.LogWarning("[RuntimeLogManager] 运行时控制未启用或未初始化");
                return;
            }
            
            LogFilter.SetLogEnabled(logId, enabled);
            
            if (_showDebugInfo)
            {
                Debug.Log($"[RuntimeLogManager] 日志 '{logId}' 已{(enabled ? "启用" : "禁用")}");
            }
        }
        
        /// <summary>
        /// 设置分类的最小日志级别
        /// </summary>
        public void SetCategoryMinLevel(string category, LogLevel minLevel)
        {
            if (!_enableRuntimeControl || !_isInitialized)
            {
                Debug.LogWarning("[RuntimeLogManager] 运行时控制未启用或未初始化");
                return;
            }
            
            LogFilter.SetCategoryMinLevel(category, minLevel);
            
            if (_showDebugInfo)
            {
                Debug.Log($"[RuntimeLogManager] 分类 '{category}' 最小级别设置为: {minLevel}");
            }
        }
        
        /// <summary>
        /// 获取分类状态
        /// </summary>
        public bool IsCategoryEnabled(string category)
        {
            return LogFilter.IsCategoryEnabled(category);
        }
        
        /// <summary>
        /// 获取日志状态
        /// </summary>
        public bool IsLogEnabled(string logId)
        {
            return LogFilter.IsLogEnabled(logId);
        }
        
        /// <summary>
        /// 获取分类统计信息
        /// </summary>
        public (int total, int enabled) GetCategoryStatistics(string category)
        {
            return LogFilter.GetCategoryStatistics(category);
        }
        
        /// <summary>
        /// 获取全局统计信息
        /// </summary>
        public (int totalCategories, int enabledCategories, int totalLogs, int enabledLogs) GetGlobalStatistics()
        {
            return LogFilter.GetGlobalStatistics();
        }
        
        /// <summary>
        /// 重置所有过滤器
        /// </summary>
        public void ResetAllFilters()
        {
            if (!_enableRuntimeControl || !_isInitialized)
            {
                Debug.LogWarning("[RuntimeLogManager] 运行时控制未启用或未初始化");
                return;
            }
            
            LogFilter.ResetAllFilters();
            
            if (_showDebugInfo)
            {
                Debug.Log("[RuntimeLogManager] 所有过滤器已重置");
            }
        }
        
        /// <summary>
        /// 启用/禁用运行时控制
        /// </summary>
        public void SetRuntimeControlEnabled(bool enabled)
        {
            _enableRuntimeControl = enabled;
            
            if (_showDebugInfo)
            {
                Debug.Log($"[RuntimeLogManager] 运行时控制已{(enabled ? "启用" : "禁用")}");
            }
        }
        
        /// <summary>
        /// 刷新配置
        /// </summary>
        public void RefreshConfig()
        {
            LoadConfig();
        }
        
        /// <summary>
        /// 获取所有启用的分类
        /// </summary>
        public List<string> GetEnabledCategories()
        {
            return LogFilter.GetEnabledCategories();
        }
        
        /// <summary>
        /// 测试日志输出
        /// </summary>
        [ContextMenu("测试日志输出")]
        public void TestLogOutput()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[RuntimeLogManager] 未初始化，无法测试日志输出");
                return;
            }
            
            Debug.Log("=== 开始测试日志输出 ===");
            
            // 测试不同分类的日志
            ASLogger.Instance.Debug("这是调试日志", "Test.Debug");
            ASLogger.Instance.Info("这是信息日志", "Test.Info");
            ASLogger.Instance.Warning("这是警告日志", "Test.Warning");
            ASLogger.Instance.Error("这是错误日志", "Test.Error");
            
            // 测试网络分类
            ASLogger.Instance.Info("网络连接成功", "Network.Connection");
            ASLogger.Instance.Debug("发送消息", "Network.Message");
            
            // 测试UI分类
            ASLogger.Instance.Info("UI初始化完成", "UI.Initialization");
            ASLogger.Instance.Debug("按钮点击", "UI.Interaction");
            
            Debug.Log("=== 日志输出测试完成 ===");
        }
        
        /// <summary>
        /// 显示当前配置状态
        /// </summary>
        [ContextMenu("显示配置状态")]
        public void ShowConfigStatus()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[RuntimeLogManager] 未初始化");
                return;
            }
            
            var stats = GetGlobalStatistics();
            Debug.Log($"[RuntimeLogManager] 配置状态:\n" +
                     $"总分类: {stats.totalCategories}\n" +
                     $"启用分类: {stats.enabledCategories}\n" +
                     $"总日志: {stats.totalLogs}\n" +
                     $"启用日志: {stats.enabledLogs}");
        }
        
        /// <summary>
        /// 验证配置
        /// </summary>
        public void ValidateConfig()
        {
            if (Application.isPlaying && _logConfig != null)
            {
                SetConfig(_logConfig);
            }
        }
    }
}
