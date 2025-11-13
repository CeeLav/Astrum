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
    
    // Entity 监控相关
    private string _entityUidInput = "";
    private Astrum.LogicCore.Core.Entity _targetEntity = null;
    private Vector2 _leftScrollPos;
    private Vector2 _rightScrollPos;
    private string _entitySearchError = "";

    [MenuItem("Window/Monitoring/Runtime Object Watcher")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<MonitorInspectorWindow>(false, "对象监控器");
        wnd.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        
        // 左侧面板：全局监控对象
        DrawLeftPanel();
        
        // 右侧面板：Entity 监控
        DrawRightPanel();
        
        EditorGUILayout.EndHorizontal();
        
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
    
    /// <summary>
    /// 绘制左侧全局监控面板
    /// </summary>
    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f));
        
        EditorGUILayout.LabelField("全局监控对象", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        _leftScrollPos = EditorGUILayout.BeginScrollView(_leftScrollPos);
        int idx = 0;
        foreach (var obj in MonitorManager.GetAllAliveMonitored())
        {
            if (obj == null) continue;
            EditorGUILayout.Space();
            DrawObjectFoldout(obj, $"[{idx}] {GetObjDisplayName(obj)}", 0, new HashSet<object>());
            idx++;
        }
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 绘制右侧 Entity 监控面板
    /// </summary>
    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f));
        
        EditorGUILayout.LabelField("Entity 监控", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // UID 输入区域
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("UID:", GUILayout.Width(40));
        _entityUidInput = EditorGUILayout.TextField(_entityUidInput, GUILayout.Width(150));
        
        if (GUILayout.Button("查找", GUILayout.Width(60)))
        {
            if (long.TryParse(_entityUidInput, out long uid))
            {
                _targetEntity = FindEntityByUid(uid);
            }
            else
            {
                _entitySearchError = "UID 格式无效，请输入数字";
                _targetEntity = null;
            }
        }
        
        if (GUILayout.Button("清除", GUILayout.Width(60)))
        {
            _targetEntity = null;
            _entityUidInput = "";
            _entitySearchError = "";
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 错误信息显示
        if (!string.IsNullOrEmpty(_entitySearchError))
        {
            EditorGUILayout.HelpBox(_entitySearchError, MessageType.Warning);
            EditorGUILayout.Space();
        }
        
        // Entity 详情显示
        _rightScrollPos = EditorGUILayout.BeginScrollView(_rightScrollPos);
        
        if (_targetEntity != null)
        {
            EditorGUILayout.LabelField($"Entity 详情 (UID: {_targetEntity.UniqueId})", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            DrawObjectFoldout(_targetEntity, $"Entity [{_targetEntity.UniqueId}] {_targetEntity.Name}", 0, new HashSet<object>());
        }
        else if (string.IsNullOrEmpty(_entitySearchError))
        {
            EditorGUILayout.HelpBox("请输入 UID 并点击查找按钮", MessageType.Info);
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
    }

    private Dictionary<object, bool> foldoutStates = new Dictionary<object, bool>();
    private void DrawObjectFoldout(object obj, string label, int depth, HashSet<object> ancestors)
    {
        if (obj == null || depth > MaxDepth || ancestors.Contains(obj)) return;
        var type = obj.GetType();
        bool foldout = false;
        if (!foldoutStates.TryGetValue(obj, out foldout))
            foldout = false;
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
        else if (type.FullName == "TrueSync.FP")
        {
            // 处理 FP 定点数类型，转换为可读的浮点数
            try
            {
                var serializedValue = type.GetField("_serializedValue").GetValue(v);
                if (serializedValue is long rawValue)
                {
                    double floatValue = (double)rawValue / (1L << 32); // ONE = 1L << 32
                    EditorGUILayout.LabelField($"{access}{member.Name}: {floatValue:F6} (FP)");
                }
                else
                {
                    EditorGUILayout.LabelField($"{access}{member.Name}: {v} (FP)");
                }
            }
            catch
            {
                EditorGUILayout.LabelField($"{access}{member.Name}: {v} (FP)");
            }
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
    
    /// <summary>
    /// 通过 UID 查找 Entity
    /// </summary>
    private Astrum.LogicCore.Core.Entity FindEntityByUid(long uid)
    {
        _entitySearchError = "";
        
        try
        {
            // 检查 GameDirector 是否初始化
            if (!Astrum.Client.Core.GameDirector.IsInitialized)
            {
                _entitySearchError = "GameDirector 未初始化";
                return null;
            }
            
            var gameDirector = Astrum.Client.Core.GameDirector.Instance;
            if (gameDirector == null)
            {
                _entitySearchError = "GameDirector 实例为空";
                return null;
            }
            
            // 获取当前 GameMode
            var currentMode = gameDirector.CurrentGameMode;
            if (currentMode == null)
            {
                _entitySearchError = "当前没有活动的 GameMode";
                return null;
            }
            
            // 获取 MainRoom
            var mainRoom = currentMode.MainRoom;
            if (mainRoom == null)
            {
                _entitySearchError = "MainRoom 未初始化";
                return null;
            }
            
            // 获取 MainWorld
            var mainWorld = mainRoom.MainWorld;
            if (mainWorld == null)
            {
                _entitySearchError = "MainWorld 未初始化";
                return null;
            }
            
            // 查找实体
            var entity = mainWorld.GetEntity(uid);
            if (entity == null)
            {
                _entitySearchError = $"未找到 UID={uid} 的实体";
                return null;
            }
            
            return entity;
        }
        catch (Exception ex)
        {
            _entitySearchError = $"查找实体时发生错误: {ex.Message}";
            return null;
        }
    }
}
#endif
