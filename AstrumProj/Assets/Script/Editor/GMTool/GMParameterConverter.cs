using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Astrum.Editor.GMTool
{
    /// <summary>
    /// GM参数转换器 - 将字符串转换为方法参数所需的类型
    /// </summary>
    public static class GMParameterConverter
    {
        /// <summary>
        /// 将字符串转换为指定类型
        /// </summary>
        public static object ConvertParameter(string value, Type targetType)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            // null 或空字符串处理
            if (string.IsNullOrEmpty(value))
            {
                if (targetType.IsValueType && !IsNullableType(targetType))
                {
                    throw new ArgumentException($"不能将空字符串转换为值类型 {targetType.Name}");
                }
                return null;
            }

            // 基本类型转换
            if (targetType == typeof(string))
            {
                return value;
            }

            if (targetType == typeof(int))
            {
                if (int.TryParse(value, out int result))
                    return result;
                throw new ArgumentException($"无法将 '{value}' 转换为 int");
            }

            if (targetType == typeof(float))
            {
                if (float.TryParse(value, out float result))
                    return result;
                throw new ArgumentException($"无法将 '{value}' 转换为 float");
            }

            if (targetType == typeof(double))
            {
                if (double.TryParse(value, out double result))
                    return result;
                throw new ArgumentException($"无法将 '{value}' 转换为 double");
            }

            if (targetType == typeof(bool))
            {
                if (bool.TryParse(value, out bool result))
                    return result;
                // 也支持 "1"/"0" 和 "true"/"false" 的小写/大写
                value = value.ToLower();
                if (value == "1" || value == "true")
                    return true;
                if (value == "0" || value == "false")
                    return false;
                throw new ArgumentException($"无法将 '{value}' 转换为 bool");
            }

            if (targetType == typeof(long))
            {
                if (long.TryParse(value, out long result))
                    return result;
                throw new ArgumentException($"无法将 '{value}' 转换为 long");
            }

            // 枚举类型
            if (targetType.IsEnum)
            {
                try
                {
                    return Enum.Parse(targetType, value, true); // 忽略大小写
                }
                catch
                {
                    // 尝试按名称查找
                    var enumNames = Enum.GetNames(targetType);
                    var match = enumNames.FirstOrDefault(n => n.Equals(value, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        return Enum.Parse(targetType, match);
                    }
                    throw new ArgumentException($"无法将 '{value}' 转换为枚举 {targetType.Name}。可选值: {string.Join(", ", enumNames)}");
                }
            }

            // Unity 类型
            if (targetType == typeof(Vector2))
            {
                return ParseVector2(value);
            }

            if (targetType == typeof(Vector3))
            {
                return ParseVector3(value);
            }

            if (targetType == typeof(Vector4))
            {
                return ParseVector4(value);
            }

            // 可空类型
            Type underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                return ConvertParameter(value, underlyingType);
            }

            // 数组类型
            if (targetType.IsArray)
            {
                return ParseArray(value, targetType.GetElementType());
            }

            // List<T> 类型
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return ParseList(value, targetType.GetGenericArguments()[0]);
            }

            // 如果无法转换，抛出异常
            throw new NotSupportedException($"不支持的类型转换: {targetType.Name}");
        }

        /// <summary>
        /// 解析 Vector2（格式：x,y 或 (x,y)）
        /// </summary>
        private static Vector2 ParseVector2(string value)
        {
            value = value.Trim().Trim('(', ')');
            string[] parts = value.Split(',');
            if (parts.Length == 2)
            {
                if (float.TryParse(parts[0].Trim(), out float x) &&
                    float.TryParse(parts[1].Trim(), out float y))
                {
                    return new Vector2(x, y);
                }
            }
            throw new ArgumentException($"无法解析 Vector2: {value}，格式应为 'x,y' 或 '(x,y)'");
        }

        /// <summary>
        /// 解析 Vector3（格式：x,y,z 或 (x,y,z)）
        /// </summary>
        private static Vector3 ParseVector3(string value)
        {
            value = value.Trim().Trim('(', ')');
            string[] parts = value.Split(',');
            if (parts.Length == 3)
            {
                if (float.TryParse(parts[0].Trim(), out float x) &&
                    float.TryParse(parts[1].Trim(), out float y) &&
                    float.TryParse(parts[2].Trim(), out float z))
                {
                    return new Vector3(x, y, z);
                }
            }
            throw new ArgumentException($"无法解析 Vector3: {value}，格式应为 'x,y,z' 或 '(x,y,z)'");
        }

        /// <summary>
        /// 解析 Vector4（格式：x,y,z,w 或 (x,y,z,w)）
        /// </summary>
        private static Vector4 ParseVector4(string value)
        {
            value = value.Trim().Trim('(', ')');
            string[] parts = value.Split(',');
            if (parts.Length == 4)
            {
                if (float.TryParse(parts[0].Trim(), out float x) &&
                    float.TryParse(parts[1].Trim(), out float y) &&
                    float.TryParse(parts[2].Trim(), out float z) &&
                    float.TryParse(parts[3].Trim(), out float w))
                {
                    return new Vector4(x, y, z, w);
                }
            }
            throw new ArgumentException($"无法解析 Vector4: {value}，格式应为 'x,y,z,w' 或 '(x,y,z,w)'");
        }

        /// <summary>
        /// 解析数组（格式：item1,item2,item3 或 [item1,item2,item3]）
        /// </summary>
        private static Array ParseArray(string value, Type elementType)
        {
            value = value.Trim().Trim('[', ']');
            if (string.IsNullOrEmpty(value))
            {
                return Array.CreateInstance(elementType, 0);
            }

            string[] items = value.Split(',');
            Array array = Array.CreateInstance(elementType, items.Length);

            for (int i = 0; i < items.Length; i++)
            {
                try
                {
                    array.SetValue(ConvertParameter(items[i].Trim(), elementType), i);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"解析数组元素 {i} 失败: {ex.Message}");
                }
            }

            return array;
        }

        /// <summary>
        /// 解析 List<T>（格式：item1,item2,item3 或 [item1,item2,item3]）
        /// </summary>
        private static object ParseList(string value, Type elementType)
        {
            value = value.Trim().Trim('[', ']');
            Type listType = typeof(List<>).MakeGenericType(elementType);
            object list = Activator.CreateInstance(listType);

            if (string.IsNullOrEmpty(value))
            {
                return list;
            }

            string[] items = value.Split(',');
            MethodInfo addMethod = listType.GetMethod("Add");

            for (int i = 0; i < items.Length; i++)
            {
                try
                {
                    object item = ConvertParameter(items[i].Trim(), elementType);
                    addMethod.Invoke(list, new[] { item });
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"解析列表元素 {i} 失败: {ex.Message}");
                }
            }

            return list;
        }

        /// <summary>
        /// 检查是否是可空类型
        /// </summary>
        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// 获取参数类型的默认输入提示
        /// </summary>
        public static string GetParameterHint(Type type)
        {
            if (type == typeof(int)) return "整数 (例如: 100)";
            if (type == typeof(float)) return "浮点数 (例如: 3.14)";
            if (type == typeof(bool)) return "布尔值 (true/false 或 1/0)";
            if (type == typeof(string)) return "字符串";
            if (type == typeof(Vector2)) return "Vector2 (例如: 1,2 或 (1,2))";
            if (type == typeof(Vector3)) return "Vector3 (例如: 1,2,3 或 (1,2,3))";
            if (type.IsEnum) return $"枚举: {string.Join(", ", Enum.GetNames(type))}";
            if (type.IsArray) return $"数组 (例如: item1,item2 或 [item1,item2])";
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) 
                return $"列表 (例如: item1,item2 或 [item1,item2])";

            return type.Name;
        }
    }
}

