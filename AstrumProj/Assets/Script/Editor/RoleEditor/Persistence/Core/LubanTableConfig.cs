using System.Collections.Generic;

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
                "##type," + string.Join(",", Types),
                "##group," + string.Join(",", Groups),
                "##desc," + string.Join(",", Descriptions)
            };
        }
    }
}

