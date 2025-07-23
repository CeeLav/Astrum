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

        public ConsoleLogHandler(bool useColors = true)
        {
            _useColors = useColors;
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
            return $"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
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

        public FileLogHandler(string logFilePath, int maxFileSize = 10, int maxFileCount = 5)
        {
            _logFilePath = logFilePath;
            _maxFileSize = maxFileSize;
            _maxFileCount = maxFileCount;

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
                    var formattedMessage = $"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
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
    public class Logger : Singleton<Logger>, ILogger
    {
        private readonly List<ILogHandler> _handlers = new List<ILogHandler>();
        private LogLevel _minLevel = LogLevel.Debug;
        private readonly object _lock = new object();

        /// <summary>
        /// 最小日志级别
        /// </summary>
        public LogLevel MinLevel
        {
            get => _minLevel;
            set => _minLevel = value;
        }

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
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Info(string message)
        {
            Log(LogLevel.Info, message);
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
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Error(string message)
        {
            Log(LogLevel.Error, message);
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
        /// 记录指定级别的日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        public void Log(LogLevel level, string message)
        {
            if (level < _minLevel) return;

            var timestamp = DateTime.Now;
            List<ILogHandler> handlers;

            lock (_lock)
            {
                handlers = new List<ILogHandler>(_handlers);
            }

            foreach (var handler in handlers)
            {
                try
                {
                    handler.HandleLog(level, message, timestamp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"日志处理器执行失败: {ex.Message}");
                }
            }
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