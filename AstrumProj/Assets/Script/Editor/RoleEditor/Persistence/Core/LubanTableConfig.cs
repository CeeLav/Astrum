using System.Collections.Generic;
using System.Linq;

namespace Astrum.Editor.RoleEditor.Persistence.Core
{
    /// <summary>
    /// Luban表配置 - 定义表的结构
    /// </summary>
    public class LubanTableConfig
    {
        /// <summary>表文件路径（相对于项目根目录）</summary>
        public string FilePath { get; set; }
        
        /// <summary>表头行数（默认4行：##var, ##type, ##group, ##desc）</summary>
        public int HeaderLines { get; set; } = 4;
        
        /// <summary>是否首列为空（Luban表格式）</summary>
        public bool HasEmptyFirstColumn { get; set; } = true;
        
        /// <summary>表头定义（用于写入）</summary>
        public TableHeader Header { get; set; }
    }
    
    /// <summary>
    /// 表头定义
    /// </summary>
    public class TableHeader
    {
        /// <summary>变量名行（##var）</summary>
        public List<string> VarNames { get; set; } = new List<string>();
        
        /// <summary>类型行（##type）</summary>
        public List<string> Types { get; set; } = new List<string>();
        
        /// <summary>分组行（##group）</summary>
        public List<string> Groups { get; set; } = new List<string>();
        
        /// <summary>描述行（##desc）</summary>
        public List<string> Descriptions { get; set; } = new List<string>();
        
        /// <summary>生成表头行</summary>
        public string[] ToLines()
        {
            return new string[]
            {
                "##var," + string.Join(",", VarNames),
                "##type," + string.Join(",", Types.Select(t => QuoteIfNeeded(t))),
                "##group," + string.Join(",", Groups),
                "##desc," + string.Join(",", Descriptions.Select(d => QuoteIfNeeded(d)))
            };
        }
        
        /// <summary>如果字段包含逗号或引号，则添加引号</summary>
        private static string QuoteIfNeeded(string field)
        {
            if (string.IsNullOrEmpty(field))
                return field;
            
            // 如果包含逗号、引号或换行符，需要用引号包裹
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                // 转义内部引号
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            
            return field;
        }
    }
}

