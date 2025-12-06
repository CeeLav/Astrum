using System;
using System.Collections.Generic;
using Astrum.LogicCore.Components;
using UnityEditor;

namespace Astrum.Editor.EntityRuntimeInspector
{
    /// <summary>
    /// 组件显示器注册表
    /// 管理所有组件类型的显示器
    /// </summary>
    public static class ComponentViewerRegistry
    {
        private static Dictionary<Type, IComponentViewer> _viewers = new Dictionary<Type, IComponentViewer>();
        private static IComponentViewer _defaultViewer = new DefaultComponentViewer();
        private static bool _initialized = false;

        /// <summary>
        /// 初始化注册表，注册所有组件显示器
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            // 注册所有组件显示器
            Register<TransComponent, ComponentViewers.TransComponentViewer>();
            Register<ActionComponent, ComponentViewers.ActionComponentViewer>();
            Register<MovementComponent, ComponentViewers.MovementComponentViewer>();
            Register<LevelComponent, ComponentViewers.LevelComponentViewer>();
            Register<BaseStatsComponent, ComponentViewers.BaseStatsComponentViewer>();
            // 可以继续添加其他组件的显示器

            _initialized = true;
        }

        /// <summary>
        /// 注册组件显示器
        /// </summary>
        public static void Register<TComponent, TViewer>() 
            where TComponent : BaseComponent
            where TViewer : IComponentViewer, new()
        {
            _viewers[typeof(TComponent)] = new TViewer();
        }

        /// <summary>
        /// 获取组件显示器
        /// </summary>
        public static IComponentViewer GetViewer(Type componentType)
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (_viewers.TryGetValue(componentType, out var viewer))
            {
                return viewer;
            }

            return _defaultViewer;
        }

        /// <summary>
        /// 默认组件显示器（使用反射作为后备方案）
        /// </summary>
        private class DefaultComponentViewer : IComponentViewer
        {
            public void DrawComponent(BaseComponent component)
            {
                if (component == null) return;

                var type = component.GetType();
                var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.IsStatic) continue;
                    try
                    {
                        var value = field.GetValue(component);
                        EditorGUILayout.LabelField($"{field.Name}: {FormatValue(value)}", EditorStyles.miniLabel);
                    }
                    catch { }
                }

                foreach (var prop in properties)
                {
                    if (!prop.CanRead) continue;
                    try
                    {
                        var value = prop.GetValue(component);
                        EditorGUILayout.LabelField($"{prop.Name}: {FormatValue(value)}", EditorStyles.miniLabel);
                    }
                    catch { }
                }
            }

            private string FormatValue(object value)
            {
                if (value == null) return "null";
                return value.ToString();
            }
        }
    }
}

