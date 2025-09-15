using System;
using System.Collections.Generic;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 日志管理器配置 - 纯数据类
    /// </summary>
    [System.Serializable]
    public class LogManagerConfig
    {
        // 分类配置
        public List<LogCategory> categories = new List<LogCategory>();
        
        // 日志条目
        public List<LogEntry> logEntries = new List<LogEntry>();
        
        // 扫描设置
        public bool autoScanOnStart = true;
        public bool saveOnChange = true;
        public List<string> scanPaths = new List<string>();
        public List<string> excludePaths = new List<string>();
        
        // 过滤设置
        public LogLevel globalMinLevel = LogLevel.Debug;
        public bool enableCategoryFilter = true;
        public bool enableLogFilter = true;
        
        // 统计信息
        public int totalCategories = 0;
        public int enabledCategories = 0;
        public int totalLogs = 0;
        public int enabledLogs = 0;
        public DateTime lastScanTime = DateTime.MinValue;
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public LogManagerConfig()
        {
            // 初始化默认扫描路径
            if (scanPaths.Count == 0)
            {
                scanPaths.Add("Assets/Script");
            }
            
            // 初始化默认排除路径
            if (excludePaths.Count == 0)
            {
                excludePaths.Add("Assets/Script/Generated");
                excludePaths.Add("Assets/Script/Editor");
            }
        }
        
        /// <summary>
        /// 克隆配置
        /// </summary>
        public LogManagerConfig Clone()
        {
            var clone = new LogManagerConfig();
            clone.categories = new List<LogCategory>(categories);
            clone.logEntries = new List<LogEntry>(logEntries);
            clone.autoScanOnStart = autoScanOnStart;
            clone.saveOnChange = saveOnChange;
            clone.scanPaths = new List<string>(scanPaths);
            clone.excludePaths = new List<string>(excludePaths);
            clone.globalMinLevel = globalMinLevel;
            clone.enableCategoryFilter = enableCategoryFilter;
            clone.enableLogFilter = enableLogFilter;
            clone.totalCategories = totalCategories;
            clone.enabledCategories = enabledCategories;
            clone.totalLogs = totalLogs;
            clone.enabledLogs = enabledLogs;
            clone.lastScanTime = lastScanTime;
            return clone;
        }
        
        /// <summary>
        /// 导出配置为JSON字符串
        /// </summary>
        public string ExportConfig()
        {
            // 简单的JSON格式输出
            var json = "{\n" +
                      $"  \"totalCategories\": {totalCategories},\n" +
                      $"  \"enabledCategories\": {enabledCategories},\n" +
                      $"  \"totalLogs\": {totalLogs},\n" +
                      $"  \"enabledLogs\": {enabledLogs},\n" +
                      $"  \"globalMinLevel\": \"{globalMinLevel}\",\n" +
                      $"  \"enableCategoryFilter\": {enableCategoryFilter.ToString().ToLower()},\n" +
                      $"  \"enableLogFilter\": {enableLogFilter.ToString().ToLower()}\n" +
                      "}";
            return json;
        }
        
        /// <summary>
        /// 从JSON字符串导入配置
        /// </summary>
        public void ImportConfig(string json)
        {
            // 简单的配置导入，这里只做基本处理
            Console.WriteLine("[LogManagerConfig] 配置导入功能暂未实现");
        }
        
        /// <summary>
        /// 查找日志条目
        /// </summary>
        public LogEntry FindLogEntry(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return null;
            
            foreach (var log in logEntries)
            {
                if (log.id == identifier || log.fallbackId == identifier)
                {
                    return log;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 查找分类
        /// </summary>
        public LogCategory FindCategory(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            
            foreach (var category in categories)
            {
                if (category.fullPath == path)
                {
                    return category;
                }
            }
            
            return null;
        }
    }
}