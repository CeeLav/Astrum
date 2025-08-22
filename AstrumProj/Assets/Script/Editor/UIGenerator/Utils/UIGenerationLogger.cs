using UnityEngine;

namespace Astrum.Editor.UIGenerator.Utils
{
    public class UIGenerationLogger
    {
        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }
        
        private LogLevel currentLevel = LogLevel.Info;
        private bool enableConsoleOutput = true;
        private bool enableFileOutput = false;
        private string logFilePath;
        
        public UIGenerationLogger()
        {
            #if UNITY_EDITOR
            logFilePath = $"{Application.dataPath}/../UIGenerator.log";
            #endif
        }
        
        public void SetLogLevel(LogLevel level)
        {
            currentLevel = level;
        }
        
        public void EnableConsoleOutput(bool enable)
        {
            enableConsoleOutput = enable;
        }
        
        public void EnableFileOutput(bool enable)
        {
            enableFileOutput = enable;
        }
        
        public void SetLogFilePath(string path)
        {
            logFilePath = path;
        }
        
        public void LogDebug(string message)
        {
            if (currentLevel <= LogLevel.Debug)
            {
                LogMessage($"[DEBUG] {message}", LogLevel.Debug);
            }
        }
        
        public void LogInfo(string message)
        {
            if (currentLevel <= LogLevel.Info)
            {
                LogMessage($"[INFO] {message}", LogLevel.Info);
            }
        }
        
        public void LogWarning(string message)
        {
            if (currentLevel <= LogLevel.Warning)
            {
                LogMessage($"[WARNING] {message}", LogLevel.Warning);
            }
        }
        
        public void LogError(string message)
        {
            if (currentLevel <= LogLevel.Error)
            {
                LogMessage($"[ERROR] {message}", LogLevel.Error);
            }
        }
        
        private void LogMessage(string message, LogLevel level)
        {
            // 控制台输出
            if (enableConsoleOutput)
            {
                switch (level)
                {
                    case LogLevel.Debug:
                    case LogLevel.Info:
                        Debug.Log(message);
                        break;
                    case LogLevel.Warning:
                        Debug.LogWarning(message);
                        break;
                    case LogLevel.Error:
                        Debug.LogError(message);
                        break;
                }
            }
            
            // 文件输出
            if (enableFileOutput && !string.IsNullOrEmpty(logFilePath))
            {
                WriteToFile(message);
            }
        }
        
        private void WriteToFile(string message)
        {
            try
            {
                var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logEntry = $"{timestamp} {message}\n";
                
                System.IO.File.AppendAllText(logFilePath, logEntry);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"写入日志文件失败: {ex.Message}");
            }
        }
        
        public void ClearLogFile()
        {
            if (enableFileOutput && !string.IsNullOrEmpty(logFilePath))
            {
                try
                {
                    if (System.IO.File.Exists(logFilePath))
                    {
                        System.IO.File.Delete(logFilePath);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"清除日志文件失败: {ex.Message}");
                }
            }
        }
        
        public string GetLogFilePath()
        {
            return logFilePath;
        }
        
        public void LogProgress(string operation, float progress, string details = "")
        {
            var progressMessage = $"[PROGRESS] {operation}: {progress:P0}";
            if (!string.IsNullOrEmpty(details))
            {
                progressMessage += $" - {details}";
            }
            
            LogInfo(progressMessage);
        }
        
        public void LogSection(string sectionName)
        {
            LogInfo($"=== {sectionName} ===");
        }
        
        public void LogSubSection(string subSectionName)
        {
            LogInfo($"--- {subSectionName} ---");
        }
    }
}
