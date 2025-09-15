using System;
using System.Text.RegularExpressions;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 日志条目
    /// </summary>
    [System.Serializable]
    public class LogEntry
    {
        // 标识信息
        public string id;                    // 主标识（稳定）
        public string fallbackId;            // 备用标识（基于行号）
        
        // 分类和级别
        public string category;              // 分类路径
        public LogLevel level;               // 日志级别
        
        // 消息内容
        public string message;               // 日志消息
        public string cleanMessage;          // 清理后的消息（用于匹配）
        
        // 代码位置
        public string filePath;              // 文件路径
        public int lineNumber;               // 当前行号
        public string methodName;            // 方法名
        public string className;             // 类名
        public int relativePosition;         // 在函数内的相对位置（从函数开始的行数）
        
        // 状态
        public bool enabled = true;          // 单个日志启用状态
        public bool isGenerated = false;     // 是否自动生成
        public DateTime lastModified;        // 最后修改时间
        
        public LogEntry()
        {
            id = "";
            fallbackId = "";
            category = "Default";
            level = LogLevel.Info;
            message = "";
            cleanMessage = "";
            filePath = "";
            lineNumber = 0;
            methodName = "";
            className = "";
            relativePosition = 0;
            enabled = true;
            isGenerated = false;
            lastModified = DateTime.Now;
        }
        
        public LogEntry(string message, string category, LogLevel level, string filePath, int lineNumber, string methodName, string className, int relativePosition = 0)
        {
            this.message = message;
            this.category = category;
            this.level = level;
            this.filePath = filePath;
            this.lineNumber = lineNumber;
            this.methodName = methodName;
            this.className = className;
            this.relativePosition = relativePosition;
            this.enabled = true;
            this.isGenerated = false;
            this.lastModified = DateTime.Now;
            
            // 生成清理后的消息
            this.cleanMessage = CleanMessage(message);
            
            // 生成标识
            GenerateIds();
        }
        
        /// <summary>
        /// 生成主标识和备用标识
        /// </summary>
        public void GenerateIds()
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            
            // 主标识：基于文件名、类名、方法名和相对位置（确保唯一性）
            id = $"{fileName}:{className}:{methodName}:{relativePosition}";
            
            // 备用标识：基于行号（用于向后兼容）
            fallbackId = $"{fileName}:{lineNumber}:{methodName}";
            
            // 如果相对位置为0（未设置），回退到基于消息的旧方式
            if (relativePosition == 0)
            {
                id = $"{fileName}:{className}:{methodName}:{cleanMessage}";
            }
        }
        
        /// <summary>
        /// 清理消息内容，移除变量和参数
        /// </summary>
        private string CleanMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "";
            
            try
            {
                // 移除变量占位符 {variable}
                var cleaned = Regex.Replace(message, @"\{[^}]+\}", "{}");
                
                // 移除字符串插值 $"{variable}"
                cleaned = Regex.Replace(cleaned, @"\$""[^""]*""", "\"\"");
                
                // 移除数字
                cleaned = Regex.Replace(cleaned, @"\d+", "");
                
                // 移除特殊字符，只保留字母、空格和中文
                cleaned = Regex.Replace(cleaned, @"[^\w\s\u4e00-\u9fff]", "");
                
                // 移除多余空格
                cleaned = Regex.Replace(cleaned, @"\s+", " ");
                
                return cleaned.Trim();
            }
            catch
            {
                // 如果清理失败，返回原始消息
                return message;
            }
        }
        
        /// <summary>
        /// 更新行号（当代码重构时）
        /// </summary>
        public void UpdateLineNumber(int newLineNumber)
        {
            lineNumber = newLineNumber;
            lastModified = DateTime.Now;
            
            // 重新生成备用标识
            var fileName = System.IO.Path.GetFileName(filePath);
            fallbackId = $"{fileName}:{lineNumber}:{methodName}";
            
            // 注意：相对位置需要重新计算，这里只更新行号
            // 相对位置应该通过外部调用重新计算
        }
        
        /// <summary>
        /// 更新相对位置
        /// </summary>
        public void UpdateRelativePosition(int newRelativePosition)
        {
            relativePosition = newRelativePosition;
            lastModified = DateTime.Now;
            
            // 重新生成主标识
            GenerateIds();
        }
        
        /// <summary>
        /// 更新消息内容
        /// </summary>
        public void UpdateMessage(string newMessage)
        {
            message = newMessage;
            cleanMessage = CleanMessage(newMessage);
            lastModified = DateTime.Now;
            
            // 重新生成主标识
            var fileName = System.IO.Path.GetFileName(filePath);
            id = $"{fileName}:{methodName}:{cleanMessage}";
        }
        
        /// <summary>
        /// 检查是否匹配给定的标识
        /// </summary>
        public bool Matches(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;
            
            // 尝试主标识匹配
            if (id == identifier)
                return true;
            
            // 尝试备用标识匹配
            if (fallbackId == identifier)
                return true;
            
            // 尝试模糊匹配（基于清理后的消息）
            if (cleanMessage == identifier)
                return true;
            
            return false;
        }
        
        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName()
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            return $"{fileName}:{lineNumber}";
        }
        
        /// <summary>
        /// 获取详细信息
        /// </summary>
        public string GetDetails()
        {
            return $"文件: {filePath}:{lineNumber}\n方法: {methodName}\n类: {className}";
        }
        
        /// <summary>
        /// 克隆日志条目
        /// </summary>
        public LogEntry Clone()
        {
            var clone = new LogEntry();
            clone.id = id;
            clone.fallbackId = fallbackId;
            clone.category = category;
            clone.level = level;
            clone.message = message;
            clone.cleanMessage = cleanMessage;
            clone.filePath = filePath;
            clone.lineNumber = lineNumber;
            clone.methodName = methodName;
            clone.className = className;
            clone.relativePosition = relativePosition;
            clone.enabled = enabled;
            clone.isGenerated = isGenerated;
            clone.lastModified = lastModified;
            return clone;
        }
        
        public override string ToString()
        {
            return $"[{level}] {message} ({GetDisplayName()})";
        }
    }
}
