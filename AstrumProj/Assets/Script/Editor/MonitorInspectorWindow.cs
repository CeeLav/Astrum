#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using AstrumClient.MonitorTools;

public class MonitorInspectorWindow : EditorWindow
{
    private const int MaxDepth = 3;
    private Vector2 scrollPos;

    [MenuItem("Window/Monitoring/Runtime Object Watcher")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<MonitorInspectorWindow>(false, "对象监控器");
        wnd.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("[运行时仅读] 当前所有已注册监控对象：", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        int idx = 0;
        foreach (var obj in MonitorManager.GetAllAliveMonitored())
        {
            if (obj == null) continue;
            EditorGUILayout.Space();
            DrawObjectFoldout(obj, $"[{idx}] {GetObjDisplayName(obj)}", 0, new HashSet<object>());
            idx++;
        }
        EditorGUILayout.EndScrollView();
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private Dictionary<object, bool> foldoutStates = new Dictionary<object, bool>();
    private void DrawObjectFoldout(object obj, string label, int depth, HashSet<object> ancestors)
    {
        if (obj == null || depth > MaxDepth || ancestors.Contains(obj)) return;
        var type = obj.GetType();
        bool foldout = true;
        if (!foldoutStates.TryGetValue(obj, out foldout))
            foldout = true;
        foldout = EditorGUILayout.Foldout(foldout, $"{label} <{type.Name}>");
        foldoutStates[obj] = foldout;
        if (!foldout) return;
        EditorGUI.indentLevel++;
        ancestors.Add(obj);
        // 展示所有字段（public+private, instance）
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            DrawMember(obj, field, depth, ancestors);
        }
        // 展示所有属性（public+private, instance，排除索引器和特殊方法属性）
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (prop.CanRead && prop.GetIndexParameters().Length == 0 && !prop.IsSpecialName)
            {
                DrawMember(obj, prop, depth, ancestors);
            }
        }
        // 父类也递归展示
        var baseType = type.BaseType;
        if (baseType != null && baseType != typeof(object))
        {
            DrawObjectFoldoutBase(obj, baseType, depth, ancestors);
        }
        ancestors.Remove(obj);
        EditorGUI.indentLevel--;
    }
    // 递归展示父类成员
    private void DrawObjectFoldoutBase(object obj, Type baseType, int depth, HashSet<object> ancestors)
    {
        EditorGUILayout.LabelField($"(Base: {baseType.Name})", EditorStyles.miniLabel);
        foreach (var field in baseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            DrawMember(obj, field, depth, ancestors);
        }
        foreach (var prop in baseType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (prop.CanRead && prop.GetIndexParameters().Length == 0 && !prop.IsSpecialName)
            {
                DrawMember(obj, prop, depth, ancestors);
            }
        }
        var nextBase = baseType.BaseType;
        if (nextBase != null && nextBase != typeof(object))
        {
            DrawObjectFoldoutBase(obj, nextBase, depth, ancestors);
        }
    }
    private void DrawMember(object obj, MemberInfo member, int depth, HashSet<object> ancestors)
    {
        object v = null;
        Type type = null;
        string access = "";
        try
        {
            if (member is FieldInfo fi)
            {
                v = fi.GetValue(obj);
                type = fi.FieldType;
                access = fi.IsPublic ? "public " : "private ";
            }
            else if (member is PropertyInfo pi)
            {
                v = pi.GetValue(obj);
                type = pi.PropertyType;
                MethodInfo getter = pi.GetGetMethod(true);
                access = getter != null && getter.IsPublic ? "public " : "private/protected ";
            }
        }
        catch (Exception e)
        {
            EditorGUILayout.LabelField($"{access}{member.Name}: <Error: {e.Message}>");
            return;
        }
        if (v == null)
        {
            EditorGUILayout.LabelField($"{access}{member.Name}: <null>");
            return;
        }
        if (IsSimple(type))
        {
            EditorGUILayout.LabelField($"{access}{member.Name}: {v}");
        }
        else if (typeof(IList).IsAssignableFrom(type))
        {
            IList list = v as IList;
            EditorGUILayout.LabelField($"{access}{member.Name} [List] Count: {list?.Count ?? 0}");
            if (list != null && depth + 1 <= MaxDepth)
            {
                EditorGUI.indentLevel++;
                int idx = 0;
                foreach (var el in list)
                {
                    DrawObjectFoldout(el, $"[{idx}]", depth + 1, ancestors);
                    idx++;
                }
                EditorGUI.indentLevel--;
            }
        }
        else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
        {
            EditorGUILayout.ObjectField(member.Name, v as UnityEngine.Object, type, true);
        }
        else
        {
            DrawObjectFoldout(v, member.Name, depth + 1, ancestors);
        }
    }
    private static string GetObjDisplayName(object obj)
    {
        if (obj is UnityEngine.Object uo)
            return uo.name;
        return obj.ToString();
    }
    private static bool IsSimple(Type type)
    {
        return type.IsPrimitive || type == typeof(string) || type.IsEnum || type == typeof(decimal);
    }
}
#endif
