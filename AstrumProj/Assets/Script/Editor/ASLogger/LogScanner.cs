using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Astrum.CommonBase;

namespace Astrum.Editor.ASLogger
{
    /// <summary>
    /// 日志扫描器
    /// </summary>
    public static class LogScanner
    {
        // 日志匹配模式
        private static readonly Regex[] LogPatterns = new[]
        {
            // 带分类的日志：ASLogger.Instance.Info("message", "category")
            new Regex(@"ASLogger\.Instance\.(Debug|Info|Warning|Error|Fatal)\s*\(\s*""([^""]+)""\s*,\s*""([^""]+)""\s*\)", RegexOptions.Compiled),
            
            // 带分类和ID的日志：ASLogger.Instance.Info("message", "category", "id")
            new Regex(@"ASLogger\.Instance\.(Debug|Info|Warning|Error|Fatal)\s*\(\s*""([^""]+)""\s*,\s*""([^""]+)""\s*,\s*""([^""]+)""\s*\)", RegexOptions.Compiled),
            
            // 无分类的日志：ASLogger.Instance.Info("message")
            new Regex(@"ASLogger\.Instance\.(Debug|Info|Warning|Error|Fatal)\s*\(\s*""([^""]+)""\s*\)", RegexOptions.Compiled),
            
            // 字符串插值 - 带分类：ASLogger.Instance.Info($"message {var}", "category")
            new Regex(@"ASLogger\.Instance\.(Debug|Info|Warning|Error|Fatal)\s*\(\s*\$""([^""]*\{[^}]*\}[^""]*)""\s*,\s*""([^""]+)""\s*\)", RegexOptions.Compiled),
            
            // 字符串插值 - 带分类和ID：ASLogger.Instance.Info($"message {var}", "category", "id")
            new Regex(@"ASLogger\.Instance\.(Debug|Info|Warning|Error|Fatal)\s*\(\s*\$""([^""]*\{[^}]*\}[^""]*)""\s*,\s*""([^""]+)""\s*,\s*""([^""]+)""\s*\)", RegexOptions.Compiled),
            
            // 字符串插值 - 无分类：ASLogger.Instance.Info($"message {var}")
            new Regex(@"ASLogger\.Instance\.(Debug|Info|Warning|Error|Fatal)\s*\(\s*\$""([^""]*\{[^}]*\}[^""]*)""\s*\)", RegexOptions.Compiled)
        };
        
        /// <summary>
        /// 扫描项目中的日志
        /// </summary>
        public static List<LogEntry> ScanProject(LogManagerConfig config)
        {
            var logEntries = new List<LogEntry>();
            
            try
            {
                // 获取所有C#文件，但排除Editor目录
                var csharpFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories)
                    .Where(file => !file.Contains("/Editor/") && !file.Contains("\\Editor\\"))
                    .ToArray();
                
                Debug.Log($"[LogScanner] 开始扫描 {csharpFiles.Length} 个C#文件...");
                
                foreach (var filePath in csharpFiles)
                {
                    // 检查是否在排除路径中
                    if (IsExcludedPath(filePath, config))
                    {
                        continue;
                    }
                    
                    // 扫描文件
                    var fileLogs = ScanFile(filePath, config);
                    if (fileLogs.Count > 0)
                    {
                        logEntries.AddRange(fileLogs);
                        Debug.Log($"[LogScanner] 在 {Path.GetFileName(filePath)} 中找到 {fileLogs.Count} 个日志");
                    }
                }
                
                Debug.Log($"[LogScanner] 日志扫描完成，找到 {logEntries.Count} 个日志条目");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LogScanner] 扫描项目时发生错误: {ex.Message}");
            }
            
            return logEntries;
        }
        
        /// <summary>
        /// 扫描单个文件
        /// </summary>
        private static List<LogEntry> ScanFile(string filePath, LogManagerConfig config)
        {
            var logEntries = new List<LogEntry>();
            
            try
            {
                var content = File.ReadAllText(filePath);
                var lines = content.Split('\n');
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var lineNumber = i + 1;
                    
                    // 尝试匹配各种日志模式
                    foreach (var pattern in LogPatterns)
                    {
                        var matches = pattern.Matches(line);
                        foreach (Match match in matches)
                        {
                            var logEntry = CreateLogEntryFromMatch(match, filePath, lineNumber, config);
                            if (logEntry != null)
                            {
                                logEntries.Add(logEntry);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"扫描文件 {filePath} 时发生错误: {ex.Message}");
            }
            
            return logEntries;
        }
        
        /// <summary>
        /// 从匹配结果创建日志条目
        /// </summary>
        private static LogEntry CreateLogEntryFromMatch(Match match, string filePath, int lineNumber, LogManagerConfig config)
        {
            try
            {
                var groups = match.Groups;
                
                // 解析日志级别
                var levelStr = groups[1].Value;
                var level = ParseLogLevel(levelStr);
                
                // 解析消息和分类
                string message;
                string category;
                string logId = null;
                
                if (groups.Count >= 5) // 带分类和ID的日志
                {
                    message = groups[2].Value;
                    category = groups[3].Value;
                    logId = groups[4].Value;
                }
                else if (groups.Count >= 4) // 带分类的日志
                {
                    message = groups[2].Value;
                    category = groups[3].Value;
                }
                else // 无分类的日志
                {
                    message = groups[2].Value;
                    category = "Default";
                }
                
                // 处理字符串插值消息，创建稳定的标识
                string cleanMessage = message;
                if (message.Contains("{"))
                {
                    // 将变量占位符替换为通用标识符
                    cleanMessage = System.Text.RegularExpressions.Regex.Replace(message, @"\{[^}]+\}", "{}");
                }
                
                // 提取类名和方法名
                var className = ExtractClassName(filePath);
                var methodName = ExtractMethodName(filePath, lineNumber);
                
                // 创建日志条目
                var logEntry = new LogEntry(message, category, level, filePath, lineNumber, methodName, className);
                
                // 如果提供了logId，使用它作为主标识
                if (!string.IsNullOrEmpty(logId))
                {
                    logEntry.id = logId;
                }
                else
                {
                    // 对于字符串插值，使用cleanMessage生成更稳定的ID
                    if (message.Contains("{"))
                    {
                        logEntry.cleanMessage = cleanMessage;
                        logEntry.GenerateIds(); // 重新生成ID
                    }
                }
                
                return logEntry;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"创建日志条目时发生错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 解析日志级别
        /// </summary>
        private static LogLevel ParseLogLevel(string levelStr)
        {
            return levelStr.ToLower() switch
            {
                "debug" => LogLevel.Debug,
                "info" => LogLevel.Info,
                "warning" => LogLevel.Warning,
                "error" => LogLevel.Error,
                "fatal" => LogLevel.Fatal,
                _ => LogLevel.Info
            };
        }
        
        /// <summary>
        /// 提取类名
        /// </summary>
        private static string ExtractClassName(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            return fileName;
        }
        
        /// <summary>
        /// 提取方法名（简化实现）
        /// </summary>
        private static string ExtractMethodName(string filePath, int lineNumber)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var lines = content.Split('\n');
                
                // 从当前行向上查找方法定义
                for (int i = lineNumber - 1; i >= 0; i--)
                {
                    var line = lines[i].Trim();
                    
                    // 查找方法定义模式
                    var methodMatch = Regex.Match(line, @"(public|private|protected|internal)\s+(static\s+)?(\w+)\s+(\w+)\s*\(");
                    if (methodMatch.Success)
                    {
                        return methodMatch.Groups[4].Value;
                    }
                }
            }
            catch
            {
                // 忽略错误
            }
            
            return "Unknown";
        }
        
        /// <summary>
        /// 检查路径是否在排除列表中
        /// </summary>
        private static bool IsExcludedPath(string filePath, LogManagerConfig config)
        {
            if (config == null || config.excludePaths == null)
            {
                return false;
            }
            
            var relativePath = filePath.Replace(Application.dataPath, "Assets");
            
            foreach (var excludePath in config.excludePaths)
            {
                if (relativePath.StartsWith(excludePath))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 创建示例日志条目
        /// </summary>
        public static List<LogEntry> CreateSampleLogEntries()
        {
            var sampleLogs = new List<LogEntry>
            {
                new LogEntry("网络连接成功", "Network.Connection", LogLevel.Info, "Assets/Script/Network/NetworkManager.cs", 45, "OnConnect", "NetworkManager"),
                new LogEntry("发送消息", "Network.Message", LogLevel.Debug, "Assets/Script/Network/NetworkManager.cs", 67, "SendMessage", "NetworkManager"),
                new LogEntry("网络错误", "Network.Error", LogLevel.Error, "Assets/Script/Network/NetworkManager.cs", 89, "OnError", "NetworkManager"),
                
                new LogEntry("UI初始化完成", "UI.Initialization", LogLevel.Info, "Assets/Script/AstrumClient/UI/LoginView.cs", 23, "Initialize", "LoginView"),
                new LogEntry("按钮点击", "UI.Interaction", LogLevel.Debug, "Assets/Script/AstrumClient/UI/LoginView.cs", 45, "OnLoginButtonClick", "LoginView"),
                new LogEntry("UI警告", "UI.Warning", LogLevel.Warning, "Assets/Script/AstrumClient/UI/LoginView.cs", 67, "ShowWarning", "LoginView"),
                
                new LogEntry("玩家移动", "GameLogic.Player.Movement", LogLevel.Info, "Assets/Script/AstrumLogic/Core/Player.cs", 34, "Move", "Player"),
                new LogEntry("技能释放", "GameLogic.Player.Skill", LogLevel.Debug, "Assets/Script/AstrumLogic/Core/Player.cs", 56, "UseSkill", "Player"),
                new LogEntry("房间状态更新", "GameLogic.Room.Update", LogLevel.Info, "Assets/Script/AstrumLogic/Core/Room.cs", 78, "UpdateRoomState", "Room"),
                
                new LogEntry("默认日志", "Default", LogLevel.Info, "Assets/Script/CommonBase/ASLogger.cs", 100, "Log", "ASLogger"),
                new LogEntry("调试信息", "Default", LogLevel.Debug, "Assets/Script/CommonBase/ASLogger.cs", 120, "Debug", "ASLogger")
            };
            
            return sampleLogs;
        }
    }
}
