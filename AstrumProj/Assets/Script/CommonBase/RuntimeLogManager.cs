using System;
using System.Collections.Generic;

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
            
            // 在非Unity环境中跳过配置加载
            Console.WriteLine("[RuntimeLogManager] 非Unity环境，跳过配置加载");
        }
        
        /// <summary>
        /// 加载配置
        /// </summary>
        public void LoadConfig()
        {
            // 配置加载由上层负责，这里不做任何操作
            Console.WriteLine("[RuntimeLogManager] 配置加载由上层负责");
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
                    Console.WriteLine($"[RuntimeLogManager] 配置已设置");
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
                Console.WriteLine("[RuntimeLogManager] 运行时控制未启用或未初始化");
                return;
            }
            
            LogFilter.SetCategoryEnabled(category, enabled);
            
            if (_showDebugInfo)
            {
                Console.WriteLine($"[RuntimeLogManager] 分类 '{category}' 已{(enabled ? "启用" : "禁用")}");
            }
        }
        
        /// <summary>
        /// 启用/禁用单个日志
        /// </summary>
        public void SetLogEnabled(string logId, bool enabled)
        {
            if (!_enableRuntimeControl || !_isInitialized)
            {
                Console.WriteLine("[RuntimeLogManager] 运行时控制未启用或未初始化");
                return;
            }
            
            LogFilter.SetLogEnabled(logId, enabled);
            
            if (_showDebugInfo)
            {
                Console.WriteLine($"[RuntimeLogManager] 日志 '{logId}' 已{(enabled ? "启用" : "禁用")}");
            }
        }
        
        /// <summary>
        /// 设置分类的最小日志级别
        /// </summary>
        public void SetCategoryMinLevel(string category, LogLevel minLevel)
        {
            if (!_enableRuntimeControl || !_isInitialized)
            {
                Console.WriteLine("[RuntimeLogManager] 运行时控制未启用或未初始化");
                return;
            }
            
            LogFilter.SetCategoryMinLevel(category, minLevel);
            
            if (_showDebugInfo)
            {
                Console.WriteLine($"[RuntimeLogManager] 分类 '{category}' 最小级别设置为: {minLevel}");
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
                Console.WriteLine("[RuntimeLogManager] 运行时控制未启用或未初始化");
                return;
            }
            
            LogFilter.ResetAllFilters();
            
            if (_showDebugInfo)
            {
                Console.WriteLine("[RuntimeLogManager] 所有过滤器已重置");
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
                Console.WriteLine($"[RuntimeLogManager] 运行时控制已{(enabled ? "启用" : "禁用")}");
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
        /// 显示当前配置状态
        /// </summary>
        public void ShowConfigStatus()
        {
            if (!_isInitialized)
            {
                Console.WriteLine("[RuntimeLogManager] 未初始化");
                return;
            }
            
            var stats = GetGlobalStatistics();
            Console.WriteLine($"[RuntimeLogManager] 配置状态:\n" +
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
            // 在非Unity环境中跳过验证
        }
    }
}
