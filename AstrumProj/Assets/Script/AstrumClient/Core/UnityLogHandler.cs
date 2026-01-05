using UnityEngine;
using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using ILogHandler = Astrum.CommonBase.ILogHandler;

namespace Astrum.Client.Core
{
    /// <summary>
    /// Unity日志处理器 - 将ASLogger的日志输出到Unity控制台
    /// </summary>
    public class UnityLogHandler : ILogHandler
    {
        [Header("Unity日志设置")]
        [SerializeField] private bool enableUnityConsole = true;
        [SerializeField] private bool enableUnityConsoleColors = true;
        [SerializeField] private bool enableStackTrace = false;
        [SerializeField] private bool enableTimestamp = true;
        [SerializeField] private bool enableLogLevel = true;
        [SerializeField] private int maxLogCount = 1000;
        
        // 日志缓存
        private readonly Queue<LogEntry> _logQueue = new Queue<LogEntry>();
        private readonly object _lock = new object();
        
        // 日志级别过滤
        private LogLevel _minLogLevel = LogLevel.Debug;
        
        // 日志统计
        private int _totalLogCount = 0;
        private int _debugCount = 0;
        private int _infoCount = 0;
        private int _warningCount = 0;
        private int _errorCount = 0;
        private int _fatalCount = 0;
        
        /// <summary>
        /// 日志条目
        /// </summary>
        public class LogEntry
        {
            public LogLevel Level { get; set; }
            public string Message { get; set; }
            public DateTime Timestamp { get; set; }
            public string StackTrace { get; set; }
            
            public LogEntry(LogLevel level, string message, DateTime timestamp, string stackTrace = null)
            {
                Level = level;
                Message = message;
                Timestamp = timestamp;
                StackTrace = stackTrace;
            }
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public UnityLogHandler()
        {
            Initialize();
        }
        
        /// <summary>
        /// 初始化Unity日志处理器
        /// </summary>
        private void Initialize()
        {
            // 注册到ASLogger
            ASLogger.Instance.AddHandler(this);
            
            // 使用 ASLogger 的日志级别，而不是从 PlayerPrefs 读取
            // 这样可以确保与 GameApplication 中设置的日志级别一致
            _minLogLevel = ASLogger.Instance.MinLevel;
            
            // 记录初始化日志
            //Info("UnityLogHandler: 初始化完成");
        }
        
        /// <summary>
        /// 从设置中获取最小日志级别
        /// </summary>
        /// <returns>最小日志级别</returns>
        private LogLevel GetMinLogLevelFromSettings()
        {
            // 可以从PlayerPrefs或其他设置中读取
            string levelStr = PlayerPrefs.GetString("LogLevel", "Debug");
            return levelStr.ToLower() switch
            {
                "debug" => LogLevel.Debug,
                "info" => LogLevel.Info,
                "warning" => LogLevel.Warning,
                "error" => LogLevel.Error,
                "fatal" => LogLevel.Fatal,
                _ => LogLevel.Debug
            };
        }
        
        /// <summary>
        /// 处理日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="timestamp">时间戳</param>
        public void HandleLog(LogLevel level, string message, DateTime timestamp)
        {
            if (level < _minLogLevel) return;
            
            lock (_lock)
            {
                // 更新统计
                UpdateLogStatistics(level);
                
                // 获取堆栈跟踪
                string stackTrace = enableStackTrace ? GetStackTrace() : null;
                
                // 创建日志条目
                var logEntry = new LogEntry(level, message, timestamp, stackTrace);
                
                // 添加到队列
                _logQueue.Enqueue(logEntry);
                
                // 限制队列大小
                while (_logQueue.Count > maxLogCount)
                {
                    _logQueue.Dequeue();
                }
                
                // 输出到Unity控制台
                if (enableUnityConsole)
                {
                    OutputToUnityConsole(logEntry);
                }
            }
        }
        
        /// <summary>
        /// 更新日志统计
        /// </summary>
        /// <param name="level">日志级别</param>
        private void UpdateLogStatistics(LogLevel level)
        {
            _totalLogCount++;
            
            switch (level)
            {
                case LogLevel.Debug:
                    _debugCount++;
                    break;
                case LogLevel.Info:
                    _infoCount++;
                    break;
                case LogLevel.Warning:
                    _warningCount++;
                    break;
                case LogLevel.Error:
                    _errorCount++;
                    break;
                case LogLevel.Fatal:
                    _fatalCount++;
                    break;
            }
        }
        
        /// <summary>
        /// 获取堆栈跟踪
        /// </summary>
        /// <returns>堆栈跟踪字符串</returns>
        private string GetStackTrace()
        {
            try
            {
                return Environment.StackTrace;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 输出到Unity控制台
        /// </summary>
        /// <param name="logEntry">日志条目</param>
        private void OutputToUnityConsole(LogEntry logEntry)
        {
            string formattedMessage = FormatLogMessage(logEntry);
            switch (logEntry.Level)
            {
                case LogLevel.Debug:
                    if (enableUnityConsoleColors)
                        Debug.Log($"<color=gray>{formattedMessage}</color>");
                    else
                        Debug.Log(formattedMessage);
                    _debugCount++;
                    break;
                case LogLevel.Info:
                    Debug.Log(formattedMessage);
                    break;
                case LogLevel.Warning:
                    if (enableUnityConsoleColors)
                        Debug.LogWarning($"<color=yellow>{formattedMessage}</color>");
                    else
                        Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                    if (enableUnityConsoleColors)
                        Debug.LogError($"<color=red>{formattedMessage}</color>");
                    else
                        Debug.LogError(formattedMessage);
                    break;
            }
        }
        
        /// <summary>
        /// 格式化日志消息
        /// </summary>
        /// <param name="logEntry">日志条目</param>
        /// <returns>格式化的日志消息</returns>
        private string FormatLogMessage(LogEntry logEntry)
        {
            var parts = new List<string>();
            
            // 添加时间戳
            if (enableTimestamp)
            {
                parts.Add($"[{logEntry.Timestamp:HH:mm:ss.fff}]");
            }
            
            // 添加日志级别
            if (enableLogLevel)
            {
                parts.Add($"[{logEntry.Level}]");
            }
            
            // 添加消息
            parts.Add(logEntry.Message);
            
            // 添加堆栈跟踪
            if (enableStackTrace && !string.IsNullOrEmpty(logEntry.StackTrace))
            {
                parts.Add($"\nStackTrace: {logEntry.StackTrace}");
            }
            
            return string.Join(" ", parts);
        }
        
        /// <summary>
        /// 设置最小日志级别
        /// </summary>
        /// <param name="level">最小日志级别</param>
        public void SetMinLogLevel(LogLevel level)
        {
            _minLogLevel = level;
            PlayerPrefs.SetString("LogLevel", level.ToString());
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// 获取最小日志级别
        /// </summary>
        /// <returns>最小日志级别</returns>
        public LogLevel GetMinLogLevel()
        {
            return _minLogLevel;
        }
        
        /// <summary>
        /// 获取日志统计信息
        /// </summary>
        /// <returns>日志统计信息</returns>
        public Dictionary<string, int> GetLogStatistics()
        {
            lock (_lock)
            {
                return new Dictionary<string, int>
                {
                    ["Total"] = _totalLogCount,
                    ["Debug"] = _debugCount,
                    ["Info"] = _infoCount,
                    ["Warning"] = _warningCount,
                    ["Error"] = _errorCount,
                    ["Fatal"] = _fatalCount
                };
            }
        }
        
        /// <summary>
        /// 获取最近的日志
        /// </summary>
        /// <param name="count">日志数量</param>
        /// <returns>最近的日志列表</returns>
        public List<LogEntry> GetRecentLogs(int count = 10)
        {
            lock (_lock)
            {
                var logs = new List<LogEntry>(_logQueue);
                logs.Reverse();
                return logs.GetRange(0, Math.Min(count, logs.Count));
            }
        }
        
        /// <summary>
        /// 清空日志缓存
        /// </summary>
        public void ClearLogCache()
        {
            lock (_lock)
            {
                _logQueue.Clear();
            }
        }
        
        /// <summary>
        /// 重置日志统计
        /// </summary>
        public void ResetStatistics()
        {
            lock (_lock)
            {
                _totalLogCount = 0;
                _debugCount = 0;
                _infoCount = 0;
                _warningCount = 0;
                _errorCount = 0;
                _fatalCount = 0;
            }
        }
        
        /// <summary>
        /// 启用/禁用Unity控制台输出
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetUnityConsoleEnabled(bool enabled)
        {
            enableUnityConsole = enabled;
        }
        
        /// <summary>
        /// 启用/禁用Unity控制台颜色
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetUnityConsoleColorsEnabled(bool enabled)
        {
            enableUnityConsoleColors = enabled;
        }
        
        /// <summary>
        /// 启用/禁用堆栈跟踪
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetStackTraceEnabled(bool enabled)
        {
            enableStackTrace = enabled;
        }
        
        /// <summary>
        /// 设置最大日志数量
        /// </summary>
        /// <param name="maxCount">最大数量</param>
        public void SetMaxLogCount(int maxCount)
        {
            lock (_lock)
            {
                maxLogCount = maxCount;
                while (_logQueue.Count > maxLogCount)
                {
                    _logQueue.Dequeue();
                }
            }
        }
        
        /// <summary>
        /// 输出日志统计信息
        /// </summary>
        public void OutputStatistics()
        {
            var stats = GetLogStatistics();
            Info("=== 日志统计信息 ===");
            foreach (var kvp in stats)
            {
                Info($"{kvp.Key}: {kvp.Value}");
            }
        }
        
        /// <summary>
        /// 输出最近的日志
        /// </summary>
        /// <param name="count">日志数量</param>
        public void OutputRecentLogs(int count = 10)
        {
            var logs = GetRecentLogs(count);
            Info($"=== 最近 {logs.Count} 条日志 ===");
            foreach (var log in logs)
            {
                string message = FormatLogMessage(log);
                switch (log.Level)
                {
                    case LogLevel.Debug:
                        Debug.Log(message);
                        break;
                    case LogLevel.Info:
                        Info(message);
                        break;
                    case LogLevel.Warning:
                        Warning(message);
                        break;
                    case LogLevel.Error:
                        Error(message);
                        break;
                    case LogLevel.Fatal:
                        Fatal(message);
                        break;
                }
            }
        }
        
        /// <summary>
        /// 便捷的日志方法
        /// </summary>
        public void Info(string message) => ASLogger.Instance.Info(message);
        public void Warning(string message) => ASLogger.Instance.Warning(message);
        public void Error(string message) => ASLogger.Instance.Error(message);
        public void Fatal(string message) => ASLogger.Instance.Fatal(message);
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 从ASLogger中移除
            ASLogger.Instance.RemoveHandler(this);
            
            // 清空缓存
            ClearLogCache();
            
            // 输出最终统计
            OutputStatistics();
        }
    }
}
