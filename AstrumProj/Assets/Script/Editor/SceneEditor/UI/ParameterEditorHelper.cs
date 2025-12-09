using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.SceneEditor.Converters;

namespace Astrum.Editor.SceneEditor.UI
{
    public static class ParameterEditorHelper
    {
        /// <summary>
        /// 绘制参数对象编辑器（使用反射）
        /// </summary>
        public static bool DrawParameterEditor(object parameters)
        {
            if (parameters == null) return false;
            
            bool changed = false;
            var type = parameters.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var prop in properties)
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                
                EditorGUI.BeginChangeCheck();
                
                var value = prop.GetValue(parameters);
                var newValue = DrawPropertyField(prop, value);
                
                if (EditorGUI.EndChangeCheck())
                {
                    prop.SetValue(parameters, newValue);
                    changed = true;
                }
            }
            
            return changed;
        }
        
        private static object DrawPropertyField(PropertyInfo prop, object value)
        {
            var propType = prop.PropertyType;
            var propName = prop.Name;
            
            if (propType == typeof(int))
            {
                return EditorGUILayout.IntField(propName, (int)value);
            }
            else if (propType == typeof(float))
            {
                return EditorGUILayout.FloatField(propName, (float)value);
            }
            else if (propType == typeof(string))
            {
                return EditorGUILayout.TextField(propName, (string)value);
            }
            else if (propType == typeof(Vector3))
            {
                return EditorGUILayout.Vector3Field(propName, (Vector3)value);
            }
            else if (propType == typeof(Bounds))
            {
                return EditorGUILayout.BoundsField(propName, (Bounds)value);
            }
            else if (propType == typeof(Bounds?))
            {
                var bounds = (Bounds?)value;
                var center = bounds.HasValue ? bounds.Value.center : Vector3.zero;
                var size = bounds.HasValue ? bounds.Value.size : Vector3.one;
                
                EditorGUILayout.LabelField(propName);
                EditorGUI.indentLevel++;
                center = EditorGUILayout.Vector3Field("Center", center);
                size = EditorGUILayout.Vector3Field("Size", size);
                EditorGUI.indentLevel--;
                
                return new Bounds(center, size);
            }
            else if (propType.IsEnum)
            {
                return EditorGUILayout.EnumPopup(propName, (Enum)value);
            }
            else
            {
                EditorGUILayout.LabelField(propName, $"[{propType.Name}] (不支持的类型)");
                return value;
            }
        }
    }
}

