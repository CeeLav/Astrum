using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Persistence.Core
{
    /// <summary>
    /// 通用Luban CSV写入器（使用CsvHelper）
    /// </summary>
    public static class LubanCSVWriter
    {
        private const string LOG_PREFIX = "[LubanCSVWriter]";
        
        /// <summary>
        /// 写入CSV表格数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="config">表配置</param>
        /// <param name="data">数据列表</param>
        /// <param name="enableBackup">是否启用备份</param>
        /// <returns>是否成功</returns>
        public static bool WriteTable<T>(LubanTableConfig config, List<T> data, bool enableBackup = true) where T : class
        {
            string fullPath = GetFullPath(config.FilePath);
            
            try
            {
                // 备份原文件
                if (enableBackup && File.Exists(fullPath))
                {
                    AstrumEditorUtility.BackupFile(fullPath, 5);
                }
                
                // 确保目录存在
                AstrumEditorUtility.EnsureDirectoryExists(fullPath);
                
                using (var writer = new StreamWriter(fullPath, false, System.Text.Encoding.UTF8))
                {
                    // 写入Luban表头
                    if (config.Header != null)
                    {
                        foreach (var headerLine in config.Header.ToLines())
                        {
                            writer.WriteLine(headerLine);
                        }
                    }
                    
                    // 配置CsvHelper
                    var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = false,  // 不写标准CSV表头
                        ShouldQuote = args => ShouldQuoteField(args.Field) // 智能引号
                    };
                    
                    using (var csv = new CsvWriter(writer, csvConfig))
                    {
                        // 动态注册ClassMap
                        var classMap = CreateDynamicClassMap<T>(config);
                        csv.Context.RegisterClassMap(classMap);
                        
                        // 写入所有记录
                        foreach (var record in data)
                        {
                            // 如果有空首列，先写一个空字段
                            if (config.HasEmptyFirstColumn)
                            {
                                csv.WriteField("");
                            }
                            
                            csv.WriteRecord(record);
                            csv.NextRecord();
                        }
                    }
                }
                
                Debug.Log($"{LOG_PREFIX} Successfully saved {data.Count} records to {config.FilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to write table {config.FilePath}: {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// 动态创建ClassMap（根据TableField特性）
        /// </summary>
        private static ClassMap<T> CreateDynamicClassMap<T>(LubanTableConfig config) where T : class
        {
            var classMap = new DefaultClassMap<T>();
            var type = typeof(T);
            
            // 获取所有带TableField特性的成员
            var members = GetTableMembers(type);
            
            // 按Index排序
            var sortedMembers = members
                .Select(m => new { Member = m, Attr = GetTableFieldAttribute(m) })
                .Where(x => x.Attr != null && !x.Attr.Ignore)
                .OrderBy(x => x.Attr.Index)
                .ToList();
            
            int csvIndex = config.HasEmptyFirstColumn ? 1 : 0; // 如果有空首列，从索引1开始
            
            foreach (var item in sortedMembers)
            {
                var member = item.Member;
                var attr = item.Attr;
                
                // 根据成员类型创建映射
                if (member is PropertyInfo property)
                {
                    var memberMap = classMap.Map(type, property);
                    memberMap.Index(csvIndex++);
                    
                    // 配置格式化
                    ConfigureFormatting(memberMap, property.PropertyType, attr);
                }
                else if (member is FieldInfo field)
                {
                    var memberMap = classMap.Map(type, field);
                    memberMap.Index(csvIndex++);
                    
                    // 配置格式化
                    ConfigureFormatting(memberMap, field.FieldType, attr);
                }
            }
            
            return classMap;
        }
        
        /// <summary>
        /// 配置字段格式化
        /// </summary>
        private static void ConfigureFormatting(MemberMap memberMap, Type memberType, TableFieldAttribute attr)
        {
            if (memberType == typeof(float))
            {
                // float类型格式化：SpeedGrowth保留2位小数，其他保留1位
                int decimals = attr.Name != null && attr.Name.Contains("SpeedGrowth") ? 2 : 1;
                memberMap.TypeConverterOption.Format($"F{decimals}");
                memberMap.TypeConverterOption.CultureInfo(CultureInfo.InvariantCulture);
            }
            else if (memberType == typeof(double))
            {
                memberMap.TypeConverterOption.Format("F2");
                memberMap.TypeConverterOption.CultureInfo(CultureInfo.InvariantCulture);
            }
        }
        
        /// <summary>
        /// 判断字段是否需要加引号
        /// </summary>
        private static bool ShouldQuoteField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return false;
            
            // 包含逗号、引号或换行符时需要引号
            return field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r");
        }
        
        /// <summary>
        /// 获取类型的所有表字段成员
        /// </summary>
        private static List<MemberInfo> GetTableMembers(Type type)
        {
            var members = new List<MemberInfo>();
            
            // 获取属性
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            members.AddRange(properties);
            
            // 获取字段
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            members.AddRange(fields);
            
            return members;
        }
        
        /// <summary>
        /// 获取成员的TableField特性
        /// </summary>
        private static TableFieldAttribute GetTableFieldAttribute(MemberInfo member)
        {
            return member.GetCustomAttribute<TableFieldAttribute>();
        }
        
        /// <summary>
        /// 获取完整路径
        /// </summary>
        private static string GetFullPath(string relativePath)
        {
            // Application.dataPath = D:\Develop\Projects\Astrum\AstrumProj\Assets
            // 需要向上两级到达 D:\Develop\Projects\Astrum
            string assetsPath = Application.dataPath; // .../AstrumProj/Assets
            string astrumProjPath = Directory.GetParent(assetsPath).FullName; // .../AstrumProj
            string projectRoot = Directory.GetParent(astrumProjPath).FullName; // .../Astrum
            
            return Path.Combine(projectRoot, relativePath.Replace("/", "\\"));
        }
    }
}
