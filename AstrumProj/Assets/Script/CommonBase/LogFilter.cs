using System;
using System.Collections.Generic;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 日志过滤器
    /// </summary>
    public static class LogFilter
    {
        private static LogManagerConfig _config;
        private static bool _isInitialized = false;
        
        /// <summary>
        /// 设置配置
        /// </summary>
        public static void SetConfig(LogManagerConfig config)
        {
            _config = config;
            _isInitialized = _config != null;
        }
        
        /// <summary>
        /// 检查日志是否应该输出
        /// </summary>
        public static bool ShouldLog(LogLevel level, string category, string logId = null)
        {
            if (!_isInitialized || _config == null)
            {
                // 默认不输出 Debug 级别日志，只有 Info 及以上级别才输出
                return level >= LogLevel.Info;
            }
            
            // 1. 检查全局最小级别
            if (level < _config.globalMinLevel)
            {
                return false;
            }
            
            // 2. 检查分类是否启用
            if (_config.enableCategoryFilter && !IsCategoryEnabled(category))
            {
                return false;
            }
            
            // 3. 检查分类的最小级别
            if (level < GetCategoryMinLevel(category))
            {
                return false;
            }
            
            // 4. 检查单个日志是否启用
            if (_config.enableLogFilter && !string.IsNullOrEmpty(logId))
            {
                var logEntry = _config.FindLogEntry(logId);
                if (logEntry != null && !logEntry.enabled)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 检查分类是否启用
        /// </summary>
        public static bool IsCategoryEnabled(string category)
        {
            if (!_isInitialized || _config == null || string.IsNullOrEmpty(category))
            {
                return true;
            }
            
            var categoryObj = _config.FindCategory(category);
            return categoryObj?.enabled ?? true;
        }
        
        /// <summary>
        /// 获取分类的最小级别
        /// </summary>
        public static LogLevel GetCategoryMinLevel(string category)
        {
            if (!_isInitialized || _config == null || string.IsNullOrEmpty(category))
            {
                return LogLevel.Debug;
            }
            
            var categoryObj = _config.FindCategory(category);
            return categoryObj?.minLevel ?? LogLevel.Debug;
        }
        
        /// <summary>
        /// 检查单个日志是否启用
        /// </summary>
        public static bool IsLogEnabled(string logId)
        {
            if (!_isInitialized || _config == null || string.IsNullOrEmpty(logId))
            {
                return true;
            }
            
            var logEntry = _config.FindLogEntry(logId);
            return logEntry?.enabled ?? true;
        }
        
        /// <summary>
        /// 启用/禁用分类
        /// </summary>
        public static void SetCategoryEnabled(string category, bool enabled)
        {
            if (!_isInitialized || _config == null || string.IsNullOrEmpty(category))
            {
                return;
            }
            
            var categoryObj = _config.FindCategory(category);
            if (categoryObj != null)
            {
                categoryObj.SetEnabled(enabled);
                // 统计信息由LogManagerConfigEditor维护
            }
        }
        
        /// <summary>
        /// 启用/禁用单个日志
        /// </summary>
        public static void SetLogEnabled(string logId, bool enabled)
        {
            if (!_isInitialized || _config == null || string.IsNullOrEmpty(logId))
            {
                return;
            }
            
            var logEntry = _config.FindLogEntry(logId);
            if (logEntry != null)
            {
                logEntry.enabled = enabled;
                // 统计信息由LogManagerConfigEditor维护
            }
        }
        
        /// <summary>
        /// 设置分类的最小级别
        /// </summary>
        public static void SetCategoryMinLevel(string category, LogLevel minLevel)
        {
            if (!_isInitialized || _config == null || string.IsNullOrEmpty(category))
            {
                return;
            }
            
            var categoryObj = _config.FindCategory(category);
            if (categoryObj != null)
            {
                categoryObj.minLevel = minLevel;
            }
        }
        
        /// <summary>
        /// 获取所有启用的分类
        /// </summary>
        public static List<string> GetEnabledCategories()
        {
            var enabledCategories = new List<string>();
            
            if (!_isInitialized || _config == null)
            {
                return enabledCategories;
            }
            
            foreach (var category in _config.categories)
            {
                CollectEnabledCategories(category, enabledCategories);
            }
            
            return enabledCategories;
        }
        
        /// <summary>
        /// 递归收集启用的分类
        /// </summary>
        private static void CollectEnabledCategories(LogCategory category, List<string> enabledCategories)
        {
            if (category.enabled)
            {
                enabledCategories.Add(category.fullPath);
            }
            
            foreach (var child in category.children)
            {
                CollectEnabledCategories(child, enabledCategories);
            }
        }
        
        /// <summary>
        /// 获取分类的统计信息
        /// </summary>
        public static (int total, int enabled) GetCategoryStatistics(string category)
        {
            if (!_isInitialized || _config == null || string.IsNullOrEmpty(category))
            {
                return (0, 0);
            }
            
            var categoryObj = _config.FindCategory(category);
            if (categoryObj != null)
            {
                return (categoryObj.totalLogCount, categoryObj.enabledLogCount);
            }
            
            return (0, 0);
        }
        
        /// <summary>
        /// 获取全局统计信息
        /// </summary>
        public static (int totalCategories, int enabledCategories, int totalLogs, int enabledLogs) GetGlobalStatistics()
        {
            if (!_isInitialized || _config == null)
            {
                return (0, 0, 0, 0);
            }
            
            return (_config.totalCategories, _config.enabledCategories, _config.totalLogs, _config.enabledLogs);
        }
        
        /// <summary>
        /// 重置所有过滤器
        /// </summary>
        public static void ResetAllFilters()
        {
            if (!_isInitialized || _config == null)
            {
                return;
            }
            
            foreach (var category in _config.categories)
            {
                ResetCategoryFilters(category);
            }
            
            // 统计信息由LogManagerConfigEditor维护
        }
        
        /// <summary>
        /// 递归重置分类过滤器
        /// </summary>
        private static void ResetCategoryFilters(LogCategory category)
        {
            category.enabled = true;
            category.minLevel = LogLevel.Debug;
            
            foreach (var log in category.logs)
            {
                log.enabled = true;
            }
            
            foreach (var child in category.children)
            {
                ResetCategoryFilters(child);
            }
        }
        
        /// <summary>
        /// 检查过滤器是否已初始化
        /// </summary>
        public static bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// 获取当前配置
        /// </summary>
        public static LogManagerConfig GetConfig()
        {
            return _config;
        }
    }
}
