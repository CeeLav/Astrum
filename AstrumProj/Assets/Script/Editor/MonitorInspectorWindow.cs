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
        EditorGUILayout.LabelField("[运行时仅读] 所有被 [MonitorTarget] 标记并注册的对象：", EditorStyles.boldLabel);
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
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            DrawMember(obj, field, depth, ancestors);
        }
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (prop.CanRead && prop.GetIndexParameters().Length == 0)
            {
                DrawMember(obj, prop, depth, ancestors);
            }
        }
        ancestors.Remove(obj);
        EditorGUI.indentLevel--;
    }

    private void DrawMember(object obj, MemberInfo member, int depth, HashSet<object> ancestors)
    {
        object v = null;
        Type type = null;
        try
        {
            if (member is FieldInfo fi)
            {
                v = fi.GetValue(obj);
                type = fi.FieldType;
            }
            else if (member is PropertyInfo pi)
            {
                v = pi.GetValue(obj);
                type = pi.PropertyType;
            }
        }
        catch (Exception e)
        {
            EditorGUILayout.LabelField($"{member.Name}: <Error: {e.Message}>");
            return;
        }
        if (v == null)
        {
            EditorGUILayout.LabelField($"{member.Name}: <null>");
            return;
        }
        if (IsSimple(type))
        {
            EditorGUILayout.LabelField($"{member.Name}: {v}");
        }
        else if (typeof(IList).IsAssignableFrom(type))
        {
            IList list = v as IList;
            EditorGUILayout.LabelField($"{member.Name} [List] Count: {list?.Count ?? 0}");
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
