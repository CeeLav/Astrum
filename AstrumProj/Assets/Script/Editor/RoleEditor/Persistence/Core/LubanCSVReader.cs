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
    /// 通用Luban CSV读取器（使用CsvHelper）
    /// </summary>
    public static class LubanCSVReader
    {
        private const string LOG_PREFIX = "[LubanCSVReader]";
        
        /// <summary>
        /// 读取CSV表格数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="config">表配置</param>
        /// <returns>数据列表</returns>
        public static List<T> ReadTable<T>(LubanTableConfig config) where T : class, new()
        {
            var result = new List<T>();
            
            string fullPath = GetFullPath(config.FilePath);
            
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"{LOG_PREFIX} Table file not found: {fullPath}");
                return result;
            }
            
            try
            {
                using (var reader = new StreamReader(fullPath))
                {
                    // 跳过Luban表头（默认4行）
                    for (int i = 0; i < config.HeaderLines; i++)
                    {
                        reader.ReadLine();
                    }
                    
                    // 配置CsvHelper
                    var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = false,
                        MissingFieldFound = null,
                        BadDataFound = null
                    };
                    
                    using (var csv = new CsvReader(reader, csvConfig))
                    {
                        // 全局配置自定义类型转换器（处理空值）
                        csv.Context.TypeConverterCache.AddConverter<int>(new NullableInt32Converter());
                        csv.Context.TypeConverterCache.AddConverter<float>(new NullableFloatConverter());
                        csv.Context.TypeConverterCache.AddConverter<double>(new NullableDoubleConverter());
                        csv.Context.TypeConverterCache.AddConverter<bool>(new NullableBooleanConverter());
                        csv.Context.TypeConverterCache.AddConverter<List<int>>(new IntListTypeConverter());
                        csv.Context.TypeConverterCache.AddConverter<List<string>>(new StringListTypeConverter());
                        
                        // 动态注册ClassMap
                        var classMap = CreateDynamicClassMap<T>(config);
                        csv.Context.RegisterClassMap(classMap);
                        
                        // 读取所有记录
                        result = csv.GetRecords<T>().ToList();
                    }
                }
                
                Debug.Log($"{LOG_PREFIX} Successfully loaded {result.Count} records from {config.FilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to read table {config.FilePath}: {ex}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 动态创建ClassMap（根据TableField特性）
        /// </summary>
        private static ClassMap<T> CreateDynamicClassMap<T>(LubanTableConfig config) where T : class, new()
        {
            var classMap = new DefaultClassMap<T>();
            var type = typeof(T);
            
            // 获取所有带TableField特性的成员
            var members = GetTableMembers(type);
            
            foreach (var member in members)
            {
                var attr = GetTableFieldAttribute(member);
                if (attr == null || attr.Ignore)
                    continue;
                
                // 计算实际列索引（如果有空首列，需要+1）
                int columnIndex = config.HasEmptyFirstColumn ? attr.Index + 1 : attr.Index;
                
                // 根据成员类型创建映射
                if (member is PropertyInfo property)
                {
                    var memberMap = classMap.Map(type, property);
                    memberMap.Index(columnIndex);
                    
                    // 如果是float类型，配置格式化
                    if (property.PropertyType == typeof(float))
                    {
                        memberMap.TypeConverterOption.CultureInfo(CultureInfo.InvariantCulture);
                    }
                    // 如果是List<int>类型，使用自定义转换器
                    else if (property.PropertyType == typeof(List<int>))
                    {
                        memberMap.TypeConverter(new IntListTypeConverter());
                    }
                    else if (property.PropertyType == typeof(List<string>))
                    {
                        memberMap.TypeConverter(new StringListTypeConverter());
                    }
                }
                else if (member is FieldInfo field)
                {
                    var memberMap = classMap.Map(type, field);
                    memberMap.Index(columnIndex);
                    
                    // 如果是float类型，配置格式化
                    if (field.FieldType == typeof(float))
                    {
                        memberMap.TypeConverterOption.CultureInfo(CultureInfo.InvariantCulture);
                    }
                    // 如果是List<int>类型，使用自定义转换器
                    else if (field.FieldType == typeof(List<int>))
                    {
                        memberMap.TypeConverter(new IntListTypeConverter());
                    }
                    else if (field.FieldType == typeof(List<string>))
                    {
                        memberMap.TypeConverter(new StringListTypeConverter());
                    }
                }
            }
            
            return classMap;
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
