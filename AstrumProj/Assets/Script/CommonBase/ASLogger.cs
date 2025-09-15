using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }

    /// <summary>
    /// 日志接口
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Debug(string message);

        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Info(string message);

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Warning(string message);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Error(string message);

        /// <summary>
        /// 记录致命错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Fatal(string message);

        /// <summary>
        /// 记录指定级别的日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        void Log(LogLevel level, string message);
    }

    /// <summary>
    /// 日志处理器接口
    /// </summary>
    public interface ILogHandler
    {
        /// <summary>
        /// 处理日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="timestamp">时间戳</param>
        void HandleLog(LogLevel level, string message, DateTime timestamp);
    }

    /// <summary>
    /// 控制台日志处理器
    /// </summary>
    public class ConsoleLogHandler : ILogHandler
    {
        private readonly bool _useColors;
        private readonly bool _showTimestamp;
        private readonly string _timestampFormat;

        public ConsoleLogHandler(bool useColors = true, bool showTimestamp = true, string timestampFormat = "yyyy-MM-dd HH:mm:ss.fff")
        {
            _useColors = useColors;
            _showTimestamp = showTimestamp;
            _timestampFormat = timestampFormat;
        }

        public void HandleLog(LogLevel level, string message, DateTime timestamp)
        {
            var formattedMessage = FormatMessage(level, message, timestamp);
            
            if (_useColors)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = GetColorForLevel(level);
                Console.WriteLine(formattedMessage);
                Console.ForegroundColor = originalColor;
            }
            else
            {
                Console.WriteLine(formattedMessage);
            }
        }

        private string FormatMessage(LogLevel level, string message, DateTime timestamp)
        {
            if (_showTimestamp)
            {
                return $"[{timestamp.ToString(_timestampFormat)}] [{level}] {message}";
            }
            else
            {
                return $"[{level}] {message}";
            }
        }

        private ConsoleColor GetColorForLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
        }
    }

    /// <summary>
    /// 文件日志处理器
    /// </summary>
    public class FileLogHandler : ILogHandler, IDisposable
    {
        private readonly string _logFilePath;
        private StreamWriter _writer;
        private readonly object _lock = new object();
        private readonly int _maxFileSize; // MB
        private readonly int _maxFileCount;
        private readonly bool _showTimestamp;
        private readonly string _timestampFormat;

        public FileLogHandler(string logFilePath, int maxFileSize = 10, int maxFileCount = 5, bool showTimestamp = true, string timestampFormat = "yyyy-MM-dd HH:mm:ss.fff")
        {
            _logFilePath = logFilePath;
            _maxFileSize = maxFileSize;
            _maxFileCount = maxFileCount;
            _showTimestamp = showTimestamp;
            _timestampFormat = timestampFormat;

            // 确保日志目录存在
            var logDir = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            _writer = new StreamWriter(logFilePath, true, Encoding.UTF8);
            _writer.AutoFlush = true;
        }

        public void HandleLog(LogLevel level, string message, DateTime timestamp)
        {
            lock (_lock)
            {
                try
                {
                    CheckFileSize();
                    string formattedMessage;
                    if (_showTimestamp)
                    {
                        formattedMessage = $"[{timestamp.ToString(_timestampFormat)}] [{level}] {message}";
                    }
                    else
                    {
                        formattedMessage = $"[{level}] {message}";
                    }
                    _writer.WriteLine(formattedMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"写入日志文件失败: {ex.Message}");
                }
            }
        }

        private void CheckFileSize()
        {
            try
            {
                _writer.Flush();
                var fileInfo = new FileInfo(_logFilePath);
                
                if (fileInfo.Exists && fileInfo.Length > _maxFileSize * 1024 * 1024)
                {
                    RotateLogFiles();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查日志文件大小失败: {ex.Message}");
            }
        }

        private void RotateLogFiles()
        {
            try
            {
                _writer.Close();
                
                // 删除最旧的文件
                var oldestFile = $"{_logFilePath}.{_maxFileCount}";
                if (File.Exists(oldestFile))
                {
                    File.Delete(oldestFile);
                }

                // 重命名现有文件
                for (int i = _maxFileCount - 1; i >= 1; i--)
                {
                    var oldFile = $"{_logFilePath}.{i}";
                    var newFile = $"{_logFilePath}.{i + 1}";
                    if (File.Exists(oldFile))
                    {
                        File.Move(oldFile, newFile);
                    }
                }

                // 重命名当前文件
                var backupFile = $"{_logFilePath}.1";
                File.Move(_logFilePath, backupFile);

                // 创建新的日志文件
                _writer = new StreamWriter(_logFilePath, false, Encoding.UTF8);
                _writer.AutoFlush = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"轮转日志文件失败: {ex.Message}");
                // 重新创建writer
                _writer = new StreamWriter(_logFilePath, true, Encoding.UTF8);
                _writer.AutoFlush = true;
            }
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }

    /// <summary>
    /// 日志管理器
    /// </summary>
    public class ASLogger : Singleton<ASLogger>, ILogger
    {
        private readonly List<ILogHandler> _handlers = new List<ILogHandler>();
        private LogLevel _minLevel = LogLevel.Debug;
        private readonly object _lock = new object();
        
        // 配置管理
        private static LogManagerConfig _config;
        private static bool _isConfigInitialized = false;
        private static readonly object _configLock = new object();
        
        /// <summary>
        /// 是否显示时间戳
        /// </summary>
        public bool ShowTimestamp { get; set; } = true;
        
        /// <summary>
        /// 时间戳格式
        /// </summary>
        public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>
        /// 最小日志级别
        /// </summary>
        public LogLevel MinLevel
        {
            get => _minLevel;
            set => _minLevel = value;
        }
        
        /// <summary>
        /// 当前配置
        /// </summary>
        public static LogManagerConfig CurrentConfig => _config;
        
        /// <summary>
        /// 配置是否已初始化
        /// </summary>
        public static bool IsConfigInitialized => _isConfigInitialized;

        /// <summary>
        /// 添加日志处理器
        /// </summary>
        /// <param name="handler">日志处理器</param>
        public void AddHandler(ILogHandler handler)
        {
            lock (_lock)
            {
                if (!_handlers.Contains(handler))
                {
                    _handlers.Add(handler);
                }
            }
        }
        
        /// <summary>
        /// 添加控制台日志处理器
        /// </summary>
        /// <param name="useColors">是否使用颜色</param>
        public void AddConsoleHandler(bool useColors = true)
        {
            var handler = new ConsoleLogHandler(useColors, ShowTimestamp, TimestampFormat);
            AddHandler(handler);
        }
        
        /// <summary>
        /// 添加文件日志处理器
        /// </summary>
        /// <param name="logFilePath">日志文件路径</param>
        /// <param name="maxFileSize">最大文件大小(MB)</param>
        /// <param name="maxFileCount">最大文件数量</param>
        public void AddFileHandler(string logFilePath, int maxFileSize = 10, int maxFileCount = 5)
        {
            var handler = new FileLogHandler(logFilePath, maxFileSize, maxFileCount, ShowTimestamp, TimestampFormat);
            AddHandler(handler);
        }

        /// <summary>
        /// 移除日志处理器
        /// </summary>
        /// <param name="handler">日志处理器</param>
        public void RemoveHandler(ILogHandler handler)
        {
            lock (_lock)
            {
                _handlers.Remove(handler);
            }
        }

        /// <summary>
        /// 清空所有日志处理器
        /// </summary>
        public void ClearHandlers()
        {
            lock (_lock)
            {
                _handlers.Clear();
            }
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// 记录带分类的调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="category">分类</param>
        public void Debug(string message, string category)
        {
            Log(LogLevel.Debug, message, category);
        }

        /// <summary>
        /// 记录带分类和ID的调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="category">分类</param>
        /// <param name="logId">日志ID</param>
        public void Debug(string message, string category, string logId)
        {
            Log(LogLevel.Debug, message, category, logId);
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <summary>
        /// 记录带分类的信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="category">分类</param>
        public void Info(string message, string category)
        {
            Log(LogLevel.Info, message, category);
        }

        /// <summary>
        /// 记录带分类和ID的信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="category">分类</param>
        /// <param name="logId">日志ID</param>
        public void Info(string message, string category, string logId)
        {
            Log(LogLevel.Info, message, category, logId);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// 记录带分类的警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="category">分类</param>
        public void Warning(string message, string category)
        {
            Log(LogLevel.Warning, message, category);
        }

        /// <summary>
        /// 记录带分类和ID的警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="category">分类</param>
        /// <param name="logId">日志ID</param>
        public void Warning(string message, string category, string logId)
        {
            Log(LogLevel.Warning, message, category, logId);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        /// <summary>
        /// 记录带分类的错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="category">分类</param>
        public void Error(string message, string category)
        {
            Log(LogLevel.Error, message, category);
        }

        /// <summary>
        /// 记录带分类和ID的错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="category">分类</param>
        /// <param name="logId">日志ID</param>
        public void Error(string message, string category, string logId)
        {
            Log(LogLevel.Error, message, category, logId);
        }

        /// <summary>
        /// 记录致命错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Fatal(string message)
        {
            Log(LogLevel.Fatal, message);
        }

        /// <summary>
        /// 记录带分类的致命错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="category">分类</param>
        public void Fatal(string message, string category)
        {
            Log(LogLevel.Fatal, message, category);
        }

        /// <summary>
        /// 记录带分类和ID的致命错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="category">分类</param>
        /// <param name="logId">日志ID</param>
        public void Fatal(string message, string category, string logId)
        {
            Log(LogLevel.Fatal, message, category, logId);
        }

        /// <summary>
        /// 记录指定级别的日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        public void Log(LogLevel level, string message)
        {
            Log(level, message, "Default");
        }

        /// <summary>
        /// 记录带分类的指定级别日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="category">分类</param>
        public void Log(LogLevel level, string message, string category)
        {
            Log(level, message, category, null);
        }

        /// <summary>
        /// 记录日志（不经过过滤）
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="category">分类</param>
        /// <param name="logId">日志ID</param>
        public void LogUnfiltered(LogLevel level, string message, string category, string logId)
        {
            // 检查最小级别
            if (level < _minLevel) return;

            var timestamp = DateTime.Now;
            List<ILogHandler> handlers;

            lock (_lock)
            {
                handlers = new List<ILogHandler>(_handlers);
            }

            // 格式化消息（添加分类信息）
            string formattedMessage = FormatMessage(message, category);

            foreach (var handler in handlers)
            {
                try
                {
                    handler.HandleLog(level, formattedMessage, timestamp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"日志处理器执行失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 记录带分类和ID的指定级别日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="category">分类</param>
        /// <param name="logId">日志ID</param>
        public void Log(LogLevel level, string message, string category, string logId)
        {
            // 如果没有提供logId，尝试自动匹配（只有在有配置的情况下）
            if (string.IsNullOrEmpty(logId) && _isConfigInitialized && _config != null)
            {
                logId = TryMatchLogId(message, category, level);
            }
            
            // 检查是否应该输出日志（只有在有配置的情况下才启用过滤）
            if (_isConfigInitialized && _config != null && !LogFilter.ShouldLog(level, category, logId))
            {
                return;
            }

            // 检查最小级别
            if (level < _minLevel) return;

            var timestamp = DateTime.Now;
            List<ILogHandler> handlers;

            lock (_lock)
            {
                handlers = new List<ILogHandler>(_handlers);
            }

            // 格式化消息（添加分类信息）
            string formattedMessage = FormatMessage(message, category);

            foreach (var handler in handlers)
            {
                try
                {
                    handler.HandleLog(level, formattedMessage, timestamp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"日志处理器执行失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 格式化消息（添加分类信息）
        /// </summary>
        /// <param name="message">原始消息</param>
        /// <param name="category">分类</param>
        /// <returns>格式化后的消息</returns>
        private string FormatMessage(string message, string category)
        {
            if (string.IsNullOrEmpty(category) || category == "Default")
            {
                return message;
            }

            return $"[{category}] {message}";
        }

        /// <summary>
        /// 格式化异常信息
        /// </summary>
        /// <param name="ex">异常</param>
        /// <returns>格式化的异常信息</returns>
        public string FormatException(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"异常类型: {ex.GetType().Name}");
            sb.AppendLine($"异常消息: {ex.Message}");
            sb.AppendLine($"堆栈跟踪: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                sb.AppendLine("内部异常:");
                sb.AppendLine(FormatException(ex.InnerException));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 记录异常日志
        /// </summary>
        /// <param name="ex">异常</param>
        /// <param name="level">日志级别</param>
        public void LogException(Exception ex, LogLevel level = LogLevel.Error)
        {
            var message = FormatException(ex);
            Log(level, message);
        }
        
        #region 配置管理
        
        /// <summary>
        /// 设置日志配置
        /// </summary>
        /// <param name="config">日志配置</param>
        public static void SetConfig(LogManagerConfig config)
        {
            lock (_configLock)
            {
                _config = config;
                _isConfigInitialized = config != null;
                
                if (_isConfigInitialized)
                {
                    // 设置到LogFilter
                    LogFilter.SetConfig(config);
                }
            }
        }
        
        /// <summary>
        /// 加载配置
        /// </summary>
        public static void LoadConfig()
        {
            // 配置加载由上层负责，这里不做任何操作
            Console.WriteLine("[ASLogger] 配置加载由上层负责");
        }
        
        /// <summary>
        /// 启用/禁用分类
        /// </summary>
        /// <param name="category">分类路径</param>
        /// <param name="enabled">是否启用</param>
        public static void SetCategoryEnabled(string category, bool enabled)
        {
            if (!_isConfigInitialized) return;
            
            LogFilter.SetCategoryEnabled(category, enabled);
        }
        
        /// <summary>
        /// 启用/禁用单个日志
        /// </summary>
        /// <param name="logId">日志ID</param>
        /// <param name="enabled">是否启用</param>
        public static void SetLogEnabled(string logId, bool enabled)
        {
            if (!_isConfigInitialized) return;
            
            LogFilter.SetLogEnabled(logId, enabled);
        }
        
        /// <summary>
        /// 设置分类的最小日志级别
        /// </summary>
        /// <param name="category">分类路径</param>
        /// <param name="minLevel">最小级别</param>
        public static void SetCategoryMinLevel(string category, LogLevel minLevel)
        {
            if (!_isConfigInitialized) return;
            
            LogFilter.SetCategoryMinLevel(category, minLevel);
        }
        
        /// <summary>
        /// 检查分类是否启用
        /// </summary>
        /// <param name="category">分类路径</param>
        /// <returns>是否启用</returns>
        public static bool IsCategoryEnabled(string category)
        {
            return LogFilter.IsCategoryEnabled(category);
        }
        
        /// <summary>
        /// 检查日志是否启用
        /// </summary>
        /// <param name="logId">日志ID</param>
        /// <returns>是否启用</returns>
        public static bool IsLogEnabled(string logId)
        {
            return LogFilter.IsLogEnabled(logId);
        }
        
        /// <summary>
        /// 获取分类统计信息
        /// </summary>
        /// <param name="category">分类路径</param>
        /// <returns>统计信息</returns>
        public static (int total, int enabled) GetCategoryStatistics(string category)
        {
            return LogFilter.GetCategoryStatistics(category);
        }
        
        /// <summary>
        /// 获取全局统计信息
        /// </summary>
        /// <returns>全局统计信息</returns>
        public static (int totalCategories, int enabledCategories, int totalLogs, int enabledLogs) GetGlobalStatistics()
        {
            return LogFilter.GetGlobalStatistics();
        }
        
        /// <summary>
        /// 重置所有过滤器
        /// </summary>
        public static void ResetAllFilters()
        {
            if (!_isConfigInitialized) return;
            
            LogFilter.ResetAllFilters();
        }
        
        /// <summary>
        /// 获取所有启用的分类
        /// </summary>
        /// <returns>启用的分类列表</returns>
        public static List<string> GetEnabledCategories()
        {
            return LogFilter.GetEnabledCategories();
        }
        
        /// <summary>
        /// 刷新配置
        /// </summary>
        public static void RefreshConfig()
        {
            LoadConfig();
        }
        
        /// <summary>
        /// 尝试自动匹配日志ID
        /// </summary>
        private string TryMatchLogId(string message, string category, LogLevel level)
        {
            if (_config == null) 
            {
                return null;
            }
            
            // 获取调用栈信息
            var stackTrace = new System.Diagnostics.StackTrace(true);
            System.Diagnostics.StackFrame callingFrame = null;
            
            // 动态查找第一个不是ASLogger的调用帧
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                if (frame == null) continue;
                
                var method = frame.GetMethod();
                if (method == null) continue;
                
                var declaringType = method.DeclaringType;
                if (declaringType == null) continue;
                
                // 跳过ASLogger相关的类型
                if (declaringType.Name == "ASLogger" || 
                    declaringType.Namespace == "Astrum.CommonBase" && declaringType.Name.Contains("ASLogger"))
                {
                    continue;
                }
                
                // 找到第一个非ASLogger的调用帧
                callingFrame = frame;
                break;
            }
            
            if (callingFrame == null) 
            {
                return null;
            }
            
            var callingMethod = callingFrame.GetMethod();
            var callingFile = callingFrame.GetFileName();
            var callingLine = callingFrame.GetFileLineNumber();
            
            if (callingMethod == null || string.IsNullOrEmpty(callingFile)) 
            {
                return null;
            }
            
            var methodName = callingMethod.Name;
            var fileName = System.IO.Path.GetFileName(callingFile);
            
            // 计算相对位置（从方法开始到当前日志行的行数）
            int relativePosition = CalculateRelativePosition(callingFile, callingLine, methodName);
            
            // 获取类名
            string className = GetClassName(callingFile, callingLine);
            
            // 生成期望的ID格式：fileName:className:methodName:relativePosition
            string expectedId = $"{fileName}:{className}:{methodName}:{relativePosition}";
            
            // 首先尝试精确匹配新的ID格式
            foreach (var logEntry in _config.logEntries)
            {
                if (logEntry.id == expectedId)
                {
                    return logEntry.id;
                }
            }
            
            // 如果新格式匹配失败，尝试旧的匹配逻辑（向后兼容）
            foreach (var logEntry in _config.logEntries)
            {
                var logFileName = System.IO.Path.GetFileName(logEntry.filePath);
                var categoryMatch = logEntry.category == category;
                var levelMatch = logEntry.level == level;
                var methodMatch = logEntry.methodName == methodName;
                var fileMatch = logFileName == fileName;
                var lineMatch = logEntry.lineNumber == callingLine;
                
                // 匹配分类、级别、方法名和文件名
                if (categoryMatch && levelMatch && methodMatch && fileMatch)
                {
                    // 如果行号也匹配，优先选择
                    if (lineMatch)
                    {
                        return logEntry.id;
                    }
                }
            }
            
            // 如果精确行号匹配失败，尝试只匹配方法名和文件名
            foreach (var logEntry in _config.logEntries)
            {
                var logFileName = System.IO.Path.GetFileName(logEntry.filePath);
                if (logEntry.category == category && 
                    logEntry.level == level && 
                    logEntry.methodName == methodName &&
                    logFileName == fileName)
                {
                    return logEntry.id;
                }
            }
            
            // 匹配失败，输出详细的调试信息
            LogUnfiltered(LogLevel.Debug, 
                $"TryMatchLogId: 匹配失败 - " +
                $"category='{category}', level={level}, method='{methodName}', file='{fileName}', line={callingLine}, " +
                $"relativePosition={relativePosition}, expectedId='{expectedId}', " +
                $"message='{message}', 配置中总共有{_config.logEntries.Count}个日志条目", 
                "ASLogger.Debug", null);
            
            return null;
        }
        
        /// <summary>
        /// 获取类名
        /// </summary>
        private string GetClassName(string filePath, int lineNumber)
        {
            try
            {
                var content = System.IO.File.ReadAllText(filePath);
                var lines = content.Split('\n');
                
                // 从当前行向上查找类定义
                for (int i = lineNumber - 1; i >= 0; i--)
                {
                    var line = lines[i].Trim();
                    
                    // 匹配类定义：public class ClassName
                    var classMatch = System.Text.RegularExpressions.Regex.Match(line, @"(public|private|protected|internal)\s+(static\s+)?class\s+(\w+)");
                    if (classMatch.Success)
                    {
                        return classMatch.Groups[3].Value;
                    }
                }
                
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
        
        /// <summary>
        /// 计算相对位置（从方法开始到指定行的行数）
        /// </summary>
        private int CalculateRelativePosition(string filePath, int lineNumber, string methodName)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    return 0;
                }
                
                var content = System.IO.File.ReadAllText(filePath);
                var lines = content.Split('\n');
                
                // 从当前行向上查找方法定义
                for (int i = lineNumber - 1; i >= 0; i--)
                {
                    var line = lines[i].Trim();
                    
                    // 扩展的方法定义模式，与LogScanner中的模式保持一致
                    var methodPatterns = new[]
                    {
                        // 标准方法：public void MethodName()
                        @"(public|private|protected|internal)\s+(static\s+)?(\w+)\s+(\w+)\s*\(",
                        // 构造函数：public ClassName()
                        @"(public|private|protected|internal)\s+(\w+)\s*\(\s*\)",
                        // 析构函数：~ClassName()
                        @"(public|private|protected|internal)\s+~(\w+)\s*\(\s*\)",
                        // 属性：public string PropertyName { get; set; }
                        @"(public|private|protected|internal)\s+(static\s+)?(\w+)\s+(\w+)\s*\{\s*get",
                        // 索引器：public string this[int index]
                        @"(public|private|protected|internal)\s+(\w+)\s+this\s*\[",
                        // 事件：public event EventHandler EventName
                        @"(public|private|protected|internal)\s+event\s+(\w+)\s+(\w+)",
                        // 字段：private string _fieldName
                        @"(public|private|protected|internal)\s+(static\s+readonly\s+)?(\w+)\s+(\w+)",
                        // Unity生命周期方法：void Start()
                        @"void\s+(Start|Update|Awake|OnEnable|OnDisable|OnDestroy)\s*\(",
                        // Unity事件方法：void OnButtonClick()
                        @"void\s+On(\w+)\s*\(",
                        // 异步方法：async Task MethodName()
                        @"async\s+(Task|void)\s+(\w+)\s*\(",
                        // 泛型方法：public T MethodName<T>()
                        @"(public|private|protected|internal)\s+(static\s+)?(\w+)\s+(\w+)\s*<",
                        // Lambda表达式所在的方法（简化处理）
                        @"=>\s*",
                    };
                    
                    foreach (var pattern in methodPatterns)
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, pattern);
                        if (match.Success)
                        {
                            string foundMethodName;
                            if (pattern.Contains("this["))
                            {
                                foundMethodName = "Indexer";
                            }
                            else if (pattern.Contains("event"))
                            {
                                foundMethodName = $"Event_{match.Groups[3].Value}";
                            }
                            else if (pattern.Contains("=>"))
                            {
                                foundMethodName = "Lambda";
                            }
                            else if (match.Groups.Count >= 5)
                            {
                                foundMethodName = match.Groups[4].Value;
                            }
                            else if (match.Groups.Count >= 4)
                            {
                                foundMethodName = match.Groups[3].Value;
                            }
                            else if (match.Groups.Count >= 3)
                            {
                                foundMethodName = match.Groups[2].Value;
                            }
                            else
                            {
                                foundMethodName = match.Groups[1].Value;
                            }
                            
                            // 如果找到的方法名匹配，计算相对位置
                            if (foundMethodName == methodName)
                            {
                                return lineNumber - i;
                            }
                        }
                    }
                }
                
                // 如果没找到方法定义，尝试查找最近的类定义
                for (int i = lineNumber - 1; i >= 0; i--)
                {
                    var line = lines[i].Trim();
                    var classMatch = System.Text.RegularExpressions.Regex.Match(line, @"(public|private|protected|internal)?\s*(static\s+)?class\s+(\w+)");
                    if (classMatch.Success)
                    {
                        var className = classMatch.Groups[3].Value;
                        if ($"Class_{className}" == methodName)
                        {
                            return lineNumber - i;
                        }
                    }
                }
            }
            catch
            {
                // 忽略错误，返回0
            }
            
            return 0;
        }
        
        
        #endregion
    }

    /// <summary>
    /// 日志扩展方法
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="logger">日志器</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">参数</param>
        public static void Debug(this ILogger logger, string format, params object[] args)
        {
            logger.Debug(string.Format(format, args));
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="logger">日志器</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">参数</param>
        public static void Info(this ILogger logger, string format, params object[] args)
        {
            logger.Info(string.Format(format, args));
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="logger">日志器</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">参数</param>
        public static void Warning(this ILogger logger, string format, params object[] args)
        {
            logger.Warning(string.Format(format, args));
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="logger">日志器</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">参数</param>
        public static void Error(this ILogger logger, string format, params object[] args)
        {
            logger.Error(string.Format(format, args));
        }

        /// <summary>
        /// 记录致命错误日志
        /// </summary>
        /// <param name="logger">日志器</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">参数</param>
        public static void Fatal(this ILogger logger, string format, params object[] args)
        {
            logger.Fatal(string.Format(format, args));
        }
    }
} 