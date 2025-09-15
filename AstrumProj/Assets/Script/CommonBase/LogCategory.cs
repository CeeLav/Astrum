using System;
using System.Collections.Generic;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 日志分类
    /// </summary>
    [System.Serializable]
    public class LogCategory
    {
        // 分类信息
        public string name;
        public string fullPath;
        public bool enabled = true;
        public LogLevel minLevel = LogLevel.Debug;
        
        // 子分类和日志
        public List<LogCategory> children = new List<LogCategory>();
        public List<LogEntry> logs = new List<LogEntry>();
        
        // 统计信息
        public int totalLogCount = 0;
        public int enabledLogCount = 0;
        
        public LogCategory()
        {
            name = "";
            fullPath = "";
            children = new List<LogCategory>();
            logs = new List<LogEntry>();
        }
        
        public LogCategory(string name, string fullPath = "")
        {
            this.name = name;
            this.fullPath = string.IsNullOrEmpty(fullPath) ? name : fullPath;
            children = new List<LogCategory>();
            logs = new List<LogEntry>();
        }
        
        /// <summary>
        /// 添加子分类
        /// </summary>
        public void AddChild(LogCategory child)
        {
            if (child != null && !children.Contains(child))
            {
                children.Add(child);
            }
        }
        
        /// <summary>
        /// 添加日志条目
        /// </summary>
        public void AddLog(LogEntry log)
        {
            if (log != null && !logs.Contains(log))
            {
                logs.Add(log);
                UpdateStatistics();
            }
        }
        
        /// <summary>
        /// 更新统计信息
        /// </summary>
        public void UpdateStatistics()
        {
            totalLogCount = logs.Count;
            enabledLogCount = 0;
            
            foreach (var log in logs)
            {
                if (log.enabled)
                {
                    enabledLogCount++;
                }
            }
            
            // 递归更新子分类统计
            foreach (var child in children)
            {
                child.UpdateStatistics();
                totalLogCount += child.totalLogCount;
                enabledLogCount += child.enabledLogCount;
            }
        }
        
        /// <summary>
        /// 查找子分类
        /// </summary>
        public LogCategory FindChild(string name)
        {
            foreach (var child in children)
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 根据路径查找分类
        /// </summary>
        public LogCategory FindByPath(string path)
        {
            if (fullPath == path)
            {
                return this;
            }
            
            foreach (var child in children)
            {
                var result = child.FindByPath(path);
                if (result != null)
                {
                    return result;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取所有日志（包括子分类）
        /// </summary>
        public List<LogEntry> GetAllLogs()
        {
            var allLogs = new List<LogEntry>(logs);
            
            foreach (var child in children)
            {
                allLogs.AddRange(child.GetAllLogs());
            }
            
            return allLogs;
        }
        
        /// <summary>
        /// 启用/禁用所有日志
        /// </summary>
        public void SetAllLogsEnabled(bool enabled)
        {
            foreach (var log in logs)
            {
                log.enabled = enabled;
            }
            
            foreach (var child in children)
            {
                child.SetAllLogsEnabled(enabled);
            }
            
            UpdateStatistics();
        }
        
        /// <summary>
        /// 启用/禁用分类
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
            // 注意：分类和日志的启用状态是隔离的，不自动修改日志状态
        }
        
        /// <summary>
        /// 克隆分类
        /// </summary>
        public LogCategory Clone()
        {
            var clone = new LogCategory();
            clone.name = name;
            clone.fullPath = fullPath;
            clone.enabled = enabled;
            clone.minLevel = minLevel;
            clone.totalLogCount = totalLogCount;
            clone.enabledLogCount = enabledLogCount;
            
            // 深拷贝子分类
            clone.children = new List<LogCategory>();
            foreach (var child in children)
            {
                clone.children.Add(child.Clone());
            }
            
            // 深拷贝日志条目
            clone.logs = new List<LogEntry>();
            foreach (var log in logs)
            {
                clone.logs.Add(log.Clone());
            }
            
            return clone;
        }
    }
}
