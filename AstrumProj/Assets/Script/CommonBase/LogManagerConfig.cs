using System;
using System.Collections.Generic;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 日志管理器配置
    /// </summary>
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
        
        private Dictionary<string, LogEntry> _logEntryDict = new Dictionary<string, LogEntry>();
        private Dictionary<string, LogCategory> _categoryDict = new Dictionary<string, LogCategory>();
        private bool _isInitialized = false;
        
        private void OnEnable()
        {
            Initialize();
        }
        
        /// <summary>
        /// 初始化配置
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
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
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// 重建字典
        /// </summary>
        public void RebuildDictionaries()
        {
            _logEntryDict.Clear();
            _categoryDict.Clear();
            
            // 构建日志条目字典
            foreach (var log in logEntries)
            {
                if (!string.IsNullOrEmpty(log.id))
                {
                    _logEntryDict[log.id] = log;
                }
                if (!string.IsNullOrEmpty(log.fallbackId))
                {
                    _logEntryDict[log.fallbackId] = log;
                }
            }
            
            // 构建分类字典
            foreach (var category in categories)
            {
                BuildCategoryDictionary(category);
            }
        }
        
        /// <summary>
        /// 递归构建分类字典
        /// </summary>
        private void BuildCategoryDictionary(LogCategory category)
        {
            if (!string.IsNullOrEmpty(category.fullPath))
            {
                _categoryDict[category.fullPath] = category;
            }
            
            foreach (var child in category.children)
            {
                BuildCategoryDictionary(child);
            }
        }
        
        /// <summary>
        /// 添加分类
        /// </summary>
        public void AddCategory(LogCategory category)
        {
            if (category == null) return;
            
            if (!categories.Contains(category))
            {
                categories.Add(category);
            }
            
            if (!string.IsNullOrEmpty(category.fullPath))
            {
                _categoryDict[category.fullPath] = category;
            }
            
            UpdateStatistics();
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
            
            // 更新字典
            if (!string.IsNullOrEmpty(logEntry.id))
            {
                _logEntryDict[logEntry.id] = logEntry;
            }
            if (!string.IsNullOrEmpty(logEntry.fallbackId))
            {
                _logEntryDict[logEntry.fallbackId] = logEntry;
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
            if (_logEntryDict.TryGetValue(identifier, out var log))
            {
                return log;
            }
            
            // 尝试模糊匹配
            foreach (var kvp in _logEntryDict)
            {
                if (kvp.Value.Matches(identifier))
                {
                    return kvp.Value;
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
            
            if (_categoryDict.TryGetValue(path, out var category))
            {
                return category;
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
                _categoryDict[currentPath] = child;
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
            _logEntryDict.Clear();
            _categoryDict.Clear();
            UpdateStatistics();
        }
        
        /// <summary>
        /// 导出配置
        /// </summary>
        public string ExportConfig()
        {
            // 在非Unity环境中跳过配置导出
            return "{}";
        }
        
        /// <summary>
        /// 导入配置
        /// </summary>
        public void ImportConfig(string json)
        {
            // 在非Unity环境中跳过配置导入
        }
        
        private void OnValidate()
        {
            // 在非Unity环境中跳过验证
        }
    }
}
