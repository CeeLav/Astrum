using System;
/*
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;
using UnityEditor;
using Astrum.CommonBase;

namespace Astrum.Editor.ASLogger
{
    /// <summary>
    /// 基于Roslyn的日志扫描器 - 更准确的代码解析
    /// </summary>
    public static class RoslynLogScanner
    {
        /// <summary>
        /// 扫描项目中的日志
        /// </summary>
        public static List<LogEntry> ScanProject(LogManagerConfig config)
        {
            var logEntries = new List<LogEntry>();
            
            try
            {
                // 获取所有C#文件
                var csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\Generated\\") && !f.Contains("/Generated/"))
                    .ToArray();
                
                Debug.Log($"[RoslynLogScanner] 开始扫描 {csFiles.Length} 个C#文件...");
                
                foreach (var filePath in csFiles)
                {
                    try
                    {
                        var fileLogs = ScanFile(filePath, config);
                        logEntries.AddRange(fileLogs);
                        
                        if (fileLogs.Count > 0)
                        {
                            Debug.Log($"[RoslynLogScanner] 在 {Path.GetFileName(filePath)} 中找到 {fileLogs.Count} 个日志");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[RoslynLogScanner] 扫描文件 {filePath} 时出错: {ex.Message}");
                    }
                }
                
                Debug.Log($"[RoslynLogScanner] 扫描完成，总共找到 {logEntries.Count} 个日志条目");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoslynLogScanner] 扫描项目时出错: {ex.Message}");
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
                var sourceCode = File.ReadAllText(filePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, path: filePath);
                var root = syntaxTree.GetRoot();
                
                // 查找所有ASLogger调用
                var logCalls = root.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(IsASLoggerCall)
                    .ToList();
                
                foreach (var logCall in logCalls)
                {
                    try
                    {
                        var logEntry = CreateLogEntryFromCall(logCall, filePath, syntaxTree);
                        if (logEntry != null)
                        {
                            logEntries.Add(logEntry);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[RoslynLogScanner] 处理日志调用时出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RoslynLogScanner] 解析文件 {filePath} 时出错: {ex.Message}");
            }
            
            return logEntries;
        }
        
        /// <summary>
        /// 判断是否是ASLogger调用
        /// </summary>
        private static bool IsASLoggerCall(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.ValueText;
                
                // 支持直接调用：ASLogger.Instance.Debug/Info/Warning/Error/Fatal
                if (methodName is "Debug" or "Info" or "Warning" or "Error" or "Fatal")
                {
                    if (memberAccess.Expression is MemberAccessExpressionSyntax instanceAccess)
                    {
                        return instanceAccess.Name.Identifier.ValueText == "Instance" &&
                               instanceAccess.Expression is IdentifierNameSyntax identifier &&
                               identifier.Identifier.ValueText == "ASLogger";
                    }
                }
                
                // 支持Log方法调用：ASLogger.Instance.Log(LogLevel, ...)
                if (methodName == "Log")
                {
                    if (memberAccess.Expression is MemberAccessExpressionSyntax instanceAccess)
                    {
                        return instanceAccess.Name.Identifier.ValueText == "Instance" &&
                               instanceAccess.Expression is IdentifierNameSyntax identifier &&
                               identifier.Identifier.ValueText == "ASLogger";
                    }
                }
            }
            return false;
        }
        
        /// <summary>
        /// 从调用创建日志条目
        /// </summary>
        private static LogEntry CreateLogEntryFromCall(InvocationExpressionSyntax logCall, string filePath, SyntaxTree syntaxTree)
        {
            var memberAccess = (MemberAccessExpressionSyntax)logCall.Expression;
            var methodName = memberAccess.Name.Identifier.ValueText;
            
            // 获取参数
            var arguments = logCall.ArgumentList.Arguments;
            
            if (arguments.Count == 0)
            {
                Debug.LogWarning($"[RoslynLogScanner] ASLogger.{methodName} 调用缺少参数");
                return null;
            }
            
            // 解析日志级别和消息
            LogLevel level;
            string message = null;
            string category = "Default";
            
            if (methodName == "Log")
            {
                // ASLogger.Instance.Log(LogLevel, message, category?)
                if (arguments.Count < 2)
                {
                    Debug.LogWarning($"[RoslynLogScanner] ASLogger.Log 调用参数不足，需要至少2个参数");
                    return null;
                }
                
                // 第一个参数：LogLevel
                level = ParseLogLevelFromExpression(arguments[0].Expression);
                
                // 第二个参数：消息
                message = ExtractStringValue(arguments[1].Expression);
                
                // 第三个参数：分类（可选）
                if (arguments.Count >= 3)
                {
                    category = ExtractStringValue(arguments[2].Expression);
                }
            }
            else
            {
                // ASLogger.Instance.Debug/Info/Warning/Error/Fatal(message, category?)
                level = ParseLogLevel(methodName);
                
                // 第一个参数：消息
                message = ExtractStringValue(arguments[0].Expression);
                
                // 第二个参数：分类（可选）
                if (arguments.Count >= 2)
                {
                    category = ExtractStringValue(arguments[1].Expression);
                }
            }
            
            if (string.IsNullOrEmpty(message))
            {
                Debug.LogWarning($"[RoslynLogScanner] 无法提取消息内容");
                return null;
            }
            
            // 获取位置信息
            var lineSpan = logCall.GetLocation().GetLineSpan();
            var lineNumber = lineSpan.StartLinePosition.Line + 1; // Roslyn使用0基索引
            
            // 查找包含此调用的方法
            var methodInfo = FindContainingMethod(logCall, syntaxTree);
            
            // 创建日志条目
            var logEntry = new LogEntry
            {
                filePath = filePath,
                lineNumber = lineNumber,
                message = message,
                level = level,
                category = category,
                methodName = methodInfo.methodName,
                className = methodInfo.className,
                relativePosition = methodInfo.relativePosition,
                enabled = true,
                lastModified = DateTime.Now
            };
            
            // 生成ID
            logEntry.GenerateIds();
            
            Debug.Log($"[RoslynLogScanner] 创建日志条目 - 文件: {Path.GetFileName(filePath)}, 行: {lineNumber}, 方法: {methodInfo.methodName}, 类: {methodInfo.className}, 消息: {message}, 分类: {category}, 级别: {level}");
            
            return logEntry;
        }
        
        /// <summary>
        /// 解析日志级别
        /// </summary>
        private static LogLevel ParseLogLevel(string levelName)
        {
            return levelName switch
            {
                "Debug" => LogLevel.Debug,
                "Info" => LogLevel.Info,
                "Warning" => LogLevel.Warning,
                "Error" => LogLevel.Error,
                "Fatal" => LogLevel.Fatal,
                _ => LogLevel.Debug
            };
        }
        
        /// <summary>
        /// 从表达式解析日志级别（用于Log方法调用）
        /// </summary>
        private static LogLevel ParseLogLevelFromExpression(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    // LogLevel.Debug, LogLevel.Info 等
                    var levelName = memberAccess.Name.Identifier.ValueText;
                    return ParseLogLevel(levelName);
                    
                case IdentifierNameSyntax identifier:
                    // 直接使用LogLevel枚举值
                    return ParseLogLevel(identifier.Identifier.ValueText);
                    
                default:
                    Debug.LogWarning($"[RoslynLogScanner] 无法解析LogLevel表达式: {expression}");
                    return LogLevel.Debug;
            }
        }
        
        /// <summary>
        /// 提取字符串值（支持字符串字面量和字符串插值）
        /// </summary>
        private static string ExtractStringValue(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case LiteralExpressionSyntax literal:
                    return literal.Token.ValueText;
                    
                case InterpolatedStringExpressionSyntax interpolated:
                    // 对于字符串插值，我们提取模板部分
                    return $"$\"{string.Join("", interpolated.Contents.Select(c => c is InterpolatedStringTextSyntax text ? text.TextToken.ValueText : "{...}"))}\"";
                    
                default:
                    // 对于其他表达式，返回占位符
                    return expression.ToString();
            }
        }
        
        /// <summary>
        /// 查找包含指定节点的方法
        /// </summary>
        private static (string methodName, string className, int relativePosition) FindContainingMethod(SyntaxNode node, SyntaxTree syntaxTree)
        {
            var current = node.Parent;
            
            while (current != null)
            {
                switch (current)
                {
                    case MethodDeclarationSyntax method:
                        var className = FindContainingClass(method);
                        var methodName = method.Identifier.ValueText;
                        var relativePosition = CalculateRelativePosition(node, method);
                        return (methodName, className, relativePosition);
                        
                    case ConstructorDeclarationSyntax constructor:
                        var constClassName = FindContainingClass(constructor);
                        var constName = constructor.Identifier.ValueText;
                        var constRelativePosition = CalculateRelativePosition(node, constructor);
                        return ($"Constructor_{constName}", constClassName, constRelativePosition);
                        
                    case PropertyDeclarationSyntax property:
                        var propClassName = FindContainingClass(property);
                        var propName = property.Identifier.ValueText;
                        var propRelativePosition = CalculateRelativePosition(node, property);
                        return ($"Property_{propName}", propClassName, propRelativePosition);
                        
                    case AccessorDeclarationSyntax accessor:
                        var accessorClassName = FindContainingClass(accessor);
                        var accessorName = accessor.Keyword.ValueText;
                        var accessorRelativePosition = CalculateRelativePosition(node, accessor);
                        return ($"Accessor_{accessorName}", accessorClassName, accessorRelativePosition);
                }
                
                current = current.Parent;
            }
            
            // 如果没找到方法，尝试查找类
            var classInfo = FindContainingClass(node);
            return ("Unknown", classInfo, 0);
        }
        
        /// <summary>
        /// 查找包含指定节点的类
        /// </summary>
        private static string FindContainingClass(SyntaxNode node)
        {
            var current = node.Parent;
            
            while (current != null)
            {
                if (current is ClassDeclarationSyntax classDecl)
                {
                    return classDecl.Identifier.ValueText;
                }
                
                current = current.Parent;
            }
            
            return "Unknown";
        }
        
        /// <summary>
        /// 计算相对位置（从方法开始到日志调用的行数）
        /// </summary>
        private static int CalculateRelativePosition(SyntaxNode logCall, SyntaxNode method)
        {
            var logLine = logCall.GetLocation().GetLineSpan().StartLinePosition.Line;
            var methodLine = method.GetLocation().GetLineSpan().StartLinePosition.Line;
            return logLine - methodLine;
        }
    }
}
*/