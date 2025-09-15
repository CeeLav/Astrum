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
            // 简化的字符串插值 - 带分类：ASLogger.Instance.Info($"message", "category") - 放在最前面
            new Regex(@"ASLogger\.Instance\.(Debug|Info|Warning|Error|Fatal)\s*\(\s*\$""([^""]+)""\s*,\s*""([^""]+)""\s*\)", RegexOptions.Compiled),
            
            // 带分类的日志：ASLogger.Instance.Info("message", "category")
            new Regex(@"ASLogger\.Instance\.(Debug|Info|Warning|Error|Fatal)\s*\(\s*""([^""]+)""\s*,\s*""([^""]+)""\s*\)", RegexOptions.Compiled),
            
            // 字符串插值 - 带分类：ASLogger.Instance.Info($"message {var}", "category")
            new Regex(@"ASLogger\.Instance\.(Debug|Info|Warning|Error|Fatal)\s*\(\s*\$""([^""]*\{[^}]*\}[^""]*)""\s*,\s*""([^""]+)""\s*\)", RegexOptions.Compiled),
            
            // 带分类和ID的日志：ASLogger.Instance.Info("message", "category", "id")
            new Regex(@"ASLogger\.Instance\.(Debug|Info|Warning|Error|Fatal)\s*\(\s*""([^""]+)""\s*,\s*""([^""]+)""\s*,\s*""([^""]+)""\s*\)", RegexOptions.Compiled),
            
            // 字符串插值 - 带分类和ID：ASLogger.Instance.Info($"message {var}", "category", "id")
            new Regex(@"ASLogger\.Instance\.(Debug|Info|Warning|Error|Fatal)\s*\(\s*\$""([^""]*\{[^}]*\}[^""]*)""\s*,\s*""([^""]+)""\s*,\s*""([^""]+)""\s*\)", RegexOptions.Compiled),
            
            // 无分类的日志：ASLogger.Instance.Info("message")
            new Regex(@"ASLogger\.Instance\.(Debug|Info|Warning|Error|Fatal)\s*\(\s*""([^""]+)""\s*\)", RegexOptions.Compiled),
            
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
                    
                    // 检查是否包含 ASLogger 调用
                    if (line.Contains("ASLogger.Instance."))
                    {
                        bool matched = false;
                        
                        // 尝试匹配各种日志模式，收集所有匹配结果
                        var allMatches = new List<(Match match, int patternIndex, int specificity)>();
                        
                        for (int patternIndex = 0; patternIndex < LogPatterns.Length; patternIndex++)
                        {
                            var pattern = LogPatterns[patternIndex];
                            var matches = pattern.Matches(line);
                            
                            if (matches.Count > 0)
                            {
                                matched = true;
                                
                                foreach (Match match in matches)
                                {
                                    // 计算匹配的具体性：带分类和ID > 带分类 > 无分类
                                    int specificity = 0;
                                    if (match.Groups.Count >= 5) // 带分类和ID
                                    {
                                        specificity = 3;
                                    }
                                    else if (match.Groups.Count >= 4) // 带分类
                                    {
                                        specificity = 2;
                                    }
                                    else if (match.Groups.Count >= 3) // 无分类
                                    {
                                        specificity = 1;
                                    }
                                    
                                    allMatches.Add((match, patternIndex, specificity));
                                }
                            }
                        }
                        
                        // 选择最具体的匹配结果
                        if (allMatches.Count > 0)
                        {
                            // 按具体性排序，选择最具体的匹配
                            var bestMatch = allMatches.OrderByDescending(m => m.specificity).First();
                            
                            var logEntry = CreateLogEntryFromMatch(bestMatch.match, filePath, lineNumber, config);
                            if (logEntry != null)
                            {
                                logEntries.Add(logEntry);
                                Debug.Log($"[LogScanner] 选择模式 {bestMatch.patternIndex + 1} (具体性: {bestMatch.specificity}) - 分类: {logEntry.category}");
                            }
                        }
                        
                        // 如果没有匹配成功，输出调试信息
                        if (!matched)
                        {
                            Debug.LogWarning($"[LogScanner] 匹配失败 - 文件: {Path.GetFileName(filePath)}:{lineNumber}");
                            Debug.LogWarning($"[LogScanner] 失败的行: {line.Trim()}");
                            
                            // 输出每个正则表达式的测试结果
                            for (int patternIndex = 0; patternIndex < LogPatterns.Length; patternIndex++)
                            {
                                var pattern = LogPatterns[patternIndex];
                                var testMatch = pattern.Match(line);
                                Debug.LogWarning($"[LogScanner] 模式 {patternIndex + 1} 测试: {(testMatch.Success ? "成功" : "失败")} - {pattern}");
                                if (testMatch.Success)
                                {
                                    Debug.LogWarning($"[LogScanner] 匹配成功但未处理，组数: {testMatch.Groups.Count}");
                                    for (int j = 0; j < testMatch.Groups.Count; j++)
                                    {
                                        Debug.LogWarning($"[LogScanner] 组 {j}: '{testMatch.Groups[j].Value}'");
                                    }
                                }
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
                
                // 提取类名、方法名和相对位置
                var className = ExtractClassName(filePath);
                var (methodName, relativePosition) = ExtractMethodInfo(filePath, lineNumber);
                
                // 创建日志条目
                var logEntry = new LogEntry(message, category, level, filePath, lineNumber, methodName, className, relativePosition);
                
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
        /// 从正则匹配结果中提取方法名
        /// </summary>
        private static string ExtractMethodNameFromMatch(Match match, string pattern)
        {
            // 特殊情况的处理方法
            if (pattern.Contains("this["))
            {
                return "Indexer";
            }
            else if (pattern.Contains("event"))
            {
                return $"Event_{match.Groups[3].Value}";
            }
            else if (pattern.Contains("=>"))
            {
                return "Lambda";
            }
            
            // 根据不同的正则表达式模式提取方法名
            if (pattern.Contains(@"(\w+)\s+(\w+)\s*\(") && !pattern.Contains("override")) // 标准方法：public void MethodName()
            {
                if (match.Groups.Count >= 4)
                {
                    return match.Groups[3].Value; // 方法名在第3组
                }
            }
            else if (pattern.Contains("override")) // 包含override的方法：protected override void MethodName()
            {
                if (match.Groups.Count >= 6)
                {
                    return match.Groups[5].Value; // 方法名在第5组
                }
                else if (match.Groups.Count >= 5)
                {
                    return match.Groups[4].Value; // 备用：方法名在第4组
                }
            }
            else if (pattern.Contains(@"void\s+(\w+)\s*\(")) // Unity生命周期方法：void Start()
            {
                if (match.Groups.Count >= 2)
                {
                    return match.Groups[1].Value; // 方法名在第1组
                }
            }
            else if (pattern.Contains(@"async\s+")) // 异步方法：public async Task MethodName() 或 public async Task<bool> MethodName()
            {
                if (match.Groups.Count >= 4)
                {
                    return match.Groups[3].Value; // 方法名在第3组（访问修饰符、async、方法名）
                }
                else if (match.Groups.Count >= 2)
                {
                    return match.Groups[1].Value; // 备用：方法名在第1组
                }
            }
            else if (pattern.Contains(@"(\w+)\s*\(\s*\)")) // 构造函数：public ClassName()
            {
                if (match.Groups.Count >= 3)
                {
                    return match.Groups[2].Value; // 类名在第2组
                }
            }
            
            // 默认处理：尝试从最后一个非空组获取
            for (int i = match.Groups.Count - 1; i >= 1; i--)
            {
                if (!string.IsNullOrEmpty(match.Groups[i].Value))
                {
                    return match.Groups[i].Value;
                }
            }
            
            return "Unknown";
        }
        
        /// <summary>
        /// 提取方法名和改进的位置信息
        /// </summary>
        private static (string methodName, int relativePosition) ExtractMethodInfo(string filePath, int lineNumber)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var lines = content.Split('\n');
                
                // 从当前行向上查找方法定义
                for (int i = lineNumber - 1; i >= 0; i--)
                {
                    var line = lines[i].Trim();
                    
                    // 扩展的方法定义模式，支持更多情况
                    var methodPatterns = new[]
                    {
                        // 标准方法：public void MethodName() 或 protected override void MethodName()
                        @"(public|private|protected|internal)\s+(static\s+)?(override\s+)?(\w+)\s+(\w+)\s*\(",
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
                        // 异步方法：public async Task MethodName() 或 public async Task<bool> MethodName()
                        @"(public|private|protected|internal)\s+async\s+(?:Task(?:<[^>]*>)?|void)\s+(\w+)\s*\(",
                        // 泛型方法：public T MethodName<T>()
                        @"(public|private|protected|internal)\s+(static\s+)?(\w+)\s+(\w+)\s*<",
                        // Lambda表达式所在的方法（简化处理）
                        @"=>\s*",
                    };
                    
                    foreach (var pattern in methodPatterns)
                    {
                        var match = Regex.Match(line, pattern);
                        if (match.Success)
                        {
                            string methodName = ExtractMethodNameFromMatch(match, pattern);
                            
                            // 计算相对位置（从方法开始到当前日志行的行数）
                            int relativePosition = lineNumber - i;
                            
                            Debug.Log($"[LogScanner] 提取方法信息 - 文件: {Path.GetFileName(filePath)}, 行: {lineNumber}, 匹配行: {i+1}, 方法行: '{line}', 提取的方法名: '{methodName}', 相对位置: {relativePosition}, 正则模式: {pattern}");
                            
                            return (methodName, relativePosition);
                        }
                    }
                }
                
                // 如果没找到方法定义，尝试查找最近的类定义
                for (int i = lineNumber - 1; i >= 0; i--)
                {
                    var line = lines[i].Trim();
                    var classMatch = Regex.Match(line, @"(public|private|protected|internal)?\s*(static\s+)?class\s+(\w+)");
                    if (classMatch.Success)
                    {
                        var className = classMatch.Groups[3].Value;
                        int relativePosition = lineNumber - i;
                        return ($"Class_{className}", relativePosition);
                    }
                }
            }
            catch
            {
                // 忽略错误
            }
            
            return ("Unknown", 0);
        }
        
        /// <summary>
        /// 提取方法名（保持向后兼容）
        /// </summary>
        private static string ExtractMethodName(string filePath, int lineNumber)
        {
            var (methodName, _) = ExtractMethodInfo(filePath, lineNumber);
            return methodName;
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
                new LogEntry("网络连接成功", "Network.Connection", LogLevel.Info, "Assets/Script/Network/NetworkManager.cs", 45, "OnConnect", "NetworkManager", 12),
                new LogEntry("发送消息", "Network.Message", LogLevel.Debug, "Assets/Script/Network/NetworkManager.cs", 67, "SendMessage", "NetworkManager", 8),
                new LogEntry("网络错误", "Network.Error", LogLevel.Error, "Assets/Script/Network/NetworkManager.cs", 89, "OnError", "NetworkManager", 15),
                
                new LogEntry("UI初始化完成", "UI.Initialization", LogLevel.Info, "Assets/Script/AstrumClient/UI/LoginView.cs", 23, "Initialize", "LoginView", 5),
                new LogEntry("按钮点击", "UI.Interaction", LogLevel.Debug, "Assets/Script/AstrumClient/UI/LoginView.cs", 45, "OnLoginButtonClick", "LoginView", 3),
                new LogEntry("UI警告", "UI.Warning", LogLevel.Warning, "Assets/Script/AstrumClient/UI/LoginView.cs", 67, "ShowWarning", "LoginView", 7),
                
                new LogEntry("玩家移动", "GameLogic.Player.Movement", LogLevel.Info, "Assets/Script/AstrumLogic/Core/Player.cs", 34, "Move", "Player", 10),
                new LogEntry("技能释放", "GameLogic.Player.Skill", LogLevel.Debug, "Assets/Script/AstrumLogic/Core/Player.cs", 56, "UseSkill", "Player", 6),
                new LogEntry("房间状态更新", "GameLogic.Room.Update", LogLevel.Info, "Assets/Script/AstrumLogic/Core/Room.cs", 78, "UpdateRoomState", "Room", 9),
                
                new LogEntry("默认日志", "Default", LogLevel.Info, "Assets/Script/CommonBase/ASLogger.cs", 100, "Log", "ASLogger", 4),
                new LogEntry("调试信息", "Default", LogLevel.Debug, "Assets/Script/CommonBase/ASLogger.cs", 120, "Debug", "ASLogger", 2)
            };
            
            return sampleLogs;
        }
    }
}
