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
            // 记录原数据数量
            int originalCategoryCount = categories.Count;
            int originalLogEntryCount = logEntries.Count;
            
            // 统计分类中的日志数量
            int totalLogsInCategories = 0;
            foreach (var category in categories)
            {
                totalLogsInCategories += category.GetAllLogs().Count;
            }
            
            Debug.Log($"[ToRuntimeConfig] 原数据统计 - 分类数量: {originalCategoryCount}, logEntries数量: {originalLogEntryCount}, 分类中日志总数: {totalLogsInCategories}");
            
            var config = new LogManagerConfig();
            
            // 深拷贝分类列表
            config.categories = new List<LogCategory>();
            foreach (var category in categories)
            {
                config.categories.Add(category.Clone());
            }
            
            // 深拷贝日志条目列表
            config.logEntries = new List<LogEntry>();
            foreach (var log in logEntries)
            {
                config.logEntries.Add(log.Clone());
            }
            
            // 拷贝其他属性
            config.autoScanOnStart = autoScanOnStart;
            config.saveOnChange = saveOnChange;
            config.scanPaths = new List<string>(scanPaths);
            config.excludePaths = new List<string>(excludePaths);
            config.globalMinLevel = globalMinLevel;
            config.enableCategoryFilter = enableCategoryFilter;
            config.enableLogFilter = enableLogFilter;
            config.totalCategories = totalCategories;
            config.enabledCategories = enabledCategories;
            config.totalLogs = totalLogs;
            config.enabledLogs = enabledLogs;
            config.lastScanTime = new DateTime(lastScanTimeTicks);
            
            // 记录转化后数据数量
            int convertedCategoryCount = config.categories.Count;
            int convertedLogEntryCount = config.logEntries.Count;
            
            // 统计转化后分类中的日志数量
            int totalLogsInConvertedCategories = 0;
            foreach (var category in config.categories)
            {
                totalLogsInConvertedCategories += category.GetAllLogs().Count;
            }
            
            Debug.Log($"[ToRuntimeConfig] 转化后统计 - 分类数量: {convertedCategoryCount}, logEntries数量: {convertedLogEntryCount}, 分类中日志总数: {totalLogsInConvertedCategories}");
            
            // 检查数据一致性
            if (originalCategoryCount != convertedCategoryCount)
            {
                Debug.LogWarning($"[ToRuntimeConfig] 警告：分类数量不一致！原数据: {originalCategoryCount}, 转化后: {convertedCategoryCount}");
            }
            
            if (originalLogEntryCount != convertedLogEntryCount)
            {
                Debug.LogWarning($"[ToRuntimeConfig] 警告：logEntries数量不一致！原数据: {originalLogEntryCount}, 转化后: {convertedLogEntryCount}");
            }
            
            if (totalLogsInCategories != totalLogsInConvertedCategories)
            {
                Debug.LogWarning($"[ToRuntimeConfig] 警告：分类中日志数量不一致！原数据: {totalLogsInCategories}, 转化后: {totalLogsInConvertedCategories}");
            }
            
            // 统计各分类的日志数量
            Debug.Log($"[ToRuntimeConfig] 分类详细统计：");
            for (int i = 0; i < Math.Min(config.categories.Count, 10); i++) // 只显示前10个分类
            {
                var category = config.categories[i];
                var logsInCategory = category.GetAllLogs();
                Debug.Log($"[ToRuntimeConfig] 分类 '{category.fullPath}': {logsInCategory.Count} 个日志");
            }
            
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
                // 更新现有条目（不添加新条目）
                Debug.Log($"[AddLogEntry] 发现重复ID '{logEntry.id}'，更新现有条目而不是添加新条目 - 原分类: '{existing.category}' -> 新分类: '{logEntry.category}'");
                existing.UpdateMessage(logEntry.message);
                existing.category = logEntry.category; // 更新分类信息
                existing.enabled = logEntry.enabled;
                existing.lastModified = DateTime.Now;
            }
            else
            {
                // 添加新条目
                Debug.Log($"[AddLogEntry] 添加新日志条目，ID: '{logEntry.id}', 分类: '{logEntry.category}'");
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
            Debug.Log($"[LogManagerConfig] GetOrCreateCategory 请求: {path}");
            
            var category = FindCategory(path);
            if (category != null)
            {
                Debug.Log($"[LogManagerConfig] 找到现有分类: {category.fullPath}");
                return category;
            }
            
            // 创建新分类
            Debug.Log($"[LogManagerConfig] 创建新分类: {path}");
            var pathParts = path.Split('.');
            var rootCategory = GetOrCreateRootCategory();
            
            var newCategory = CreateCategoryPath(rootCategory, pathParts, 0);
            Debug.Log($"[LogManagerConfig] 新分类创建完成: {newCategory.fullPath}");
            return newCategory;
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
            
            Debug.Log($"[LogManagerConfig] CreateCategoryPath: 处理 {currentName} (路径: {currentPath})");
            
            var child = parent.FindChild(currentName);
            if (child == null)
            {
                Debug.Log($"[LogManagerConfig] 创建子分类: {currentName} 在 {parent.fullPath} 下");
                child = new LogCategory(currentName, currentPath);
                parent.AddChild(child);
                Debug.Log($"[LogManagerConfig] 子分类创建完成，父分类现在有 {parent.children.Count} 个子分类");
            }
            else
            {
                Debug.Log($"[LogManagerConfig] 找到现有子分类: {currentName}");
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
