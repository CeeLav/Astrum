using System;
using System.Collections.Generic;
using System.Linq;

namespace Astrum.Editor.GMTool
{
    /// <summary>
    /// GM历史记录项
    /// </summary>
    [Serializable]
    public class GMHistoryItem
    {
        public string TypeName;
        public string MethodName;
        public List<string> ParameterValues = new List<string>();
        public string ExecuteTimeString;

        public GMHistoryItem()
        {
        }

        public GMHistoryItem(string typeName, string methodName, string[] parameterValues)
        {
            TypeName = typeName;
            MethodName = methodName;
            if (parameterValues != null)
            {
                ParameterValues.AddRange(parameterValues);
            }
            ExecuteTimeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public DateTime ExecuteTime
        {
            get
            {
                if (DateTime.TryParse(ExecuteTimeString, out DateTime result))
                    return result;
                return DateTime.Now;
            }
        }

        public string[] GetParameterArray()
        {
            return ParameterValues.ToArray();
        }

        /// <summary>
        /// 检查是否与另一个历史记录项相同（类型、方法和参数都相同）
        /// </summary>
        public bool IsSameAs(GMHistoryItem other)
        {
            if (other == null) return false;
            
            if (TypeName != other.TypeName || MethodName != other.MethodName)
                return false;
            
            if (ParameterValues.Count != other.ParameterValues.Count)
                return false;
            
            return ParameterValues.SequenceEqual(other.ParameterValues);
        }

        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName()
        {
            string displayType = TypeName;
            if (displayType.StartsWith("GameMode:"))
            {
                displayType = displayType.Substring(9); // 移除 "GameMode: " 前缀
            }
            
            string args = ParameterValues.Count > 0 
                ? $"({string.Join(", ", ParameterValues)})" 
                : "()";
            
            return $"{displayType}.{MethodName}{args}";
        }
    }
}

