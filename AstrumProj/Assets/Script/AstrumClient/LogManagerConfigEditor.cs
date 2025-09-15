using System;
using System.Collections.Generic;
using UnityEngine;
using Astrum.CommonBase;

namespace Astrum.Client
{
    /// <summary>
    /// Unity编辑器版本的LogManagerConfig
    /// 继承自ScriptableObject，用于Unity编辑器
    /// </summary>
    [CreateAssetMenu(fileName = "LogManagerConfig", menuName = "ASLogger/Log Manager Config")]
    [System.Serializable]
    public class LogManagerConfigEditor : ScriptableObject
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
        public long lastScanTimeTicks = 0; // 使用long存储DateTime的Ticks
        
        /// <summary>
        /// 最后扫描时间（属性，用于方便访问）
        /// </summary>
        public DateTime lastScanTime
        {
            get { return new DateTime(lastScanTimeTicks); }
            set { lastScanTimeTicks = value.Ticks; }
        }
        
        /// <summary>
        /// 转换为运行时配置
        /// </summary>
        public LogManagerConfig ToRuntimeConfig()
        {
            var config = new LogManagerConfig();
            config.categories = categories;
            config.logEntries = logEntries;
            config.autoScanOnStart = autoScanOnStart;
            config.saveOnChange = saveOnChange;
            config.scanPaths = scanPaths;
            config.excludePaths = excludePaths;
            config.globalMinLevel = globalMinLevel;
            config.enableCategoryFilter = enableCategoryFilter;
            config.enableLogFilter = enableLogFilter;
            config.totalCategories = totalCategories;
            config.enabledCategories = enabledCategories;
            config.totalLogs = totalLogs;
            config.enabledLogs = enabledLogs;
            config.lastScanTime = new DateTime(lastScanTimeTicks);
            return config;
        }
        
        /// <summary>
        /// 从运行时配置更新
        /// </summary>
        public void UpdateFromRuntimeConfig(LogManagerConfig config)
        {
            if (config == null) return;
            
            categories = config.categories;
            logEntries = config.logEntries;
            autoScanOnStart = config.autoScanOnStart;
            saveOnChange = config.saveOnChange;
            scanPaths = config.scanPaths;
            excludePaths = config.excludePaths;
            globalMinLevel = config.globalMinLevel;
            enableCategoryFilter = config.enableCategoryFilter;
            enableLogFilter = config.enableLogFilter;
            totalCategories = config.totalCategories;
            enabledCategories = config.enabledCategories;
            totalLogs = config.totalLogs;
            enabledLogs = config.enabledLogs;
            lastScanTimeTicks = config.lastScanTime.Ticks;
        }
        
        /// <summary>
        /// 初始化配置
        /// </summary>
        public void Initialize()
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
            
            // 构建字典
            RebuildDictionaries();
        }
        
        /// <summary>
        /// 重建字典
        /// </summary>
        public void RebuildDictionaries()
        {
            // 这里可以添加字典重建逻辑
        }
        
        /// <summary>
        /// 添加日志条目
        /// </summary>
        public void AddLogEntry(LogEntry logEntry)
        {
            if (logEntry == null) return;
            
            // 检查是否已存在
            var existing = FindLogEntry(logEntry.id);
            if (existing != null)
            {
                // 更新现有条目
                existing.UpdateMessage(logEntry.message);
                existing.enabled = logEntry.enabled;
                existing.lastModified = DateTime.Now;
            }
            else
            {
                // 添加新条目
                logEntries.Add(logEntry);
            }
            
            UpdateStatistics();
        }
        
        /// <summary>
        /// 查找日志条目
        /// </summary>
        public LogEntry FindLogEntry(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return null;
            
            // 尝试直接匹配
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
        /// 获取或创建分类
        /// </summary>
        public LogCategory GetOrCreateCategory(string path)
        {
            var category = FindCategory(path);
            if (category != null)
            {
                return category;
            }
            
            // 创建新分类
            var pathParts = path.Split('.');
            var rootCategory = GetOrCreateRootCategory();
            
            return CreateCategoryPath(rootCategory, pathParts, 0);
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
        
        /// <summary>
        /// 获取或创建根分类
        /// </summary>
        private LogCategory GetOrCreateRootCategory()
        {
            if (categories.Count == 0)
            {
                var root = new LogCategory("Root", "Root");
                categories.Add(root);
            }
            
            return categories[0];
        }
        
        /// <summary>
        /// 递归创建分类路径
        /// </summary>
        private LogCategory CreateCategoryPath(LogCategory parent, string[] pathParts, int index)
        {
            if (index >= pathParts.Length) return parent;
            
            var currentName = pathParts[index];
            var currentPath = string.Join(".", pathParts, 0, index + 1);
            
            var child = parent.FindChild(currentName);
            if (child == null)
            {
                child = new LogCategory(currentName, currentPath);
                parent.AddChild(child);
            }
            
            return CreateCategoryPath(child, pathParts, index + 1);
        }
        
        /// <summary>
        /// 更新统计信息
        /// </summary>
        public void UpdateStatistics()
        {
            totalCategories = 0;
            enabledCategories = 0;
            totalLogs = 0;
            enabledLogs = 0;
            
            foreach (var category in categories)
            {
                category.UpdateStatistics();
                totalCategories += 1 + CountSubCategories(category);
                enabledCategories += (category.enabled ? 1 : 0) + CountEnabledSubCategories(category);
                totalLogs += category.totalLogCount;
                enabledLogs += category.enabledLogCount;
            }
        }
        
        /// <summary>
        /// 计算子分类数量
        /// </summary>
        private int CountSubCategories(LogCategory category)
        {
            int count = 0;
            foreach (var child in category.children)
            {
                count += 1 + CountSubCategories(child);
            }
            return count;
        }
        
        /// <summary>
        /// 计算启用的子分类数量
        /// </summary>
        private int CountEnabledSubCategories(LogCategory category)
        {
            int count = 0;
            foreach (var child in category.children)
            {
                count += (child.enabled ? 1 : 0) + CountEnabledSubCategories(child);
            }
            return count;
        }
        
        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            categories.Clear();
            logEntries.Clear();
            UpdateStatistics();
        }
    }
}
