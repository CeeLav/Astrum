using System;

namespace Astrum.Editor.RoleEditor.Persistence.Core
{
    /// <summary>
    /// 表字段映射特性 - 用于标记字段在CSV中的列索引
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class TableFieldAttribute : Attribute
    {
        /// <summary>列索引（从0开始，但实际CSV从1开始因为首列为空）</summary>
        public int Index { get; set; }
        
        /// <summary>字段名称</summary>
        public string Name { get; set; }
        
        /// <summary>是否忽略此字段</summary>
        public bool Ignore { get; set; }
        
        public TableFieldAttribute(int index, string name = null)
        {
            Index = index;
            Name = name;
            Ignore = false;
        }
    }
}

