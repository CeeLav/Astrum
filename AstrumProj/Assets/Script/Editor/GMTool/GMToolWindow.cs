using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Astrum.Editor.GMTool
{
    /// <summary>
    /// GM工具窗口 - 通过反射调用单例和GameMode的方法
    /// </summary>
    public class GMToolWindow : EditorWindow
    {
        // UI状态 - 分类折叠
        private Dictionary<GMReflectionService.TypeCategory, bool> _categoryFoldouts = new Dictionary<GMReflectionService.TypeCategory, bool>
        {
            { GMReflectionService.TypeCategory.Manager, true },
            { GMReflectionService.TypeCategory.GameMode, true },
            { GMReflectionService.TypeCategory.Other, true }
        };
        private Dictionary<GMReflectionService.TypeCategory, List<string>> _categorizedTypes = new Dictionary<GMReflectionService.TypeCategory, List<string>>();
        private string _selectedTypeName = null;
        
        // UI状态 - 方法选择
        private List<MethodInfo> _methods = new List<MethodInfo>();
        private int _selectedMethodIndex = -1;
        private string[] _parameterValues = new string[0];
        private Vector2 _typeScrollPosition;
        private Vector2 _methodScrollPosition;
        private Vector2 _parameterScrollPosition;
        private Vector2 _historyScrollPosition;
        
        // UI状态 - 结果和历史
        private string _resultText = "";
        private bool _showResult = false;
        private List<GMHistoryItem> _historyList = new List<GMHistoryItem>();
        private const int MAX_HISTORY_COUNT = 10;
        private const string HISTORY_PREF_KEY = "GMTool_History";
        
        // 窗口设置
        private const float MIN_WINDOW_WIDTH = 800f;
        private const float MIN_WINDOW_HEIGHT = 600f;
        private const float LEFT_PANEL_WIDTH = 300f;
        private const float RIGHT_PANEL_WIDTH = 300f;

        [MenuItem("Astrum/Tools/GM Tool", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<GMToolWindow>("GM Tool");
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshTypeList();
            LoadHistory();
        }

        private void OnGUI()
        {
            // 检查是否在播放模式
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("GM工具只能在播放模式下使用。请先启动游戏。", MessageType.Warning);
                if (GUILayout.Button("进入播放模式", GUILayout.Height(30)))
                {
                    EditorApplication.isPlaying = true;
                }
                return;
            }

            DrawToolbar();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            {
                // 左侧：分类的类型列表和方法列表
                DrawLeftPanel();

                // 中间分隔线
                DrawVerticalSeparator();

                // 中间：参数输入和执行
                DrawCenterPanel();

                // 中间分隔线
                DrawVerticalSeparator();

                // 右侧：历史记录
                DrawRightPanel();
            }
            EditorGUILayout.EndHorizontal();

            // 结果显示
            if (_showResult)
            {
                DrawResultPanel();
            }
        }

        /// <summary>
        /// 绘制工具栏
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    RefreshTypeList();
                }

                if (GUILayout.Button("清空历史", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog("清空历史", "确定要清空所有历史记录吗？", "确定", "取消"))
                    {
                        ClearHistory();
                    }
                }

                GUILayout.FlexibleSpace();

                EditorGUILayout.LabelField("GM工具 - 反射调用单例和GameMode方法", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制左侧面板（分类的类型列表和方法列表）
        /// </summary>
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LEFT_PANEL_WIDTH));
            
            EditorGUILayout.LabelField("可调用实例", EditorStyles.boldLabel);
            _typeScrollPosition = EditorGUILayout.BeginScrollView(_typeScrollPosition, GUILayout.ExpandHeight(true));

            // 绘制分类折叠列表
            foreach (var category in new[] 
            { 
                GMReflectionService.TypeCategory.Manager, 
                GMReflectionService.TypeCategory.GameMode, 
                GMReflectionService.TypeCategory.Other 
            })
            {
                if (!_categorizedTypes.ContainsKey(category) || _categorizedTypes[category].Count == 0)
                    continue;

                string categoryName = GetCategoryDisplayName(category);
                int count = _categorizedTypes[category].Count;
                
                // 折叠标题
                _categoryFoldouts[category] = EditorGUILayout.Foldout(_categoryFoldouts[category], $"{categoryName} ({count})", true);
                
                if (_categoryFoldouts[category])
                {
                    EditorGUI.indentLevel++;
                    foreach (var typeName in _categorizedTypes[category])
                    {
                        // 显示类型名称（简化显示）
                        string displayName = GetSimpleTypeName(typeName);
                        
                        // 高亮选中的类型
                        if (typeName == _selectedTypeName)
                        {
                            GUI.backgroundColor = Color.cyan;
                        }

                        if (GUILayout.Button(displayName, EditorStyles.label, GUILayout.Height(18)))
                        {
                            _selectedTypeName = typeName;
                            OnTypeSelected();
                        }

                        GUI.backgroundColor = Color.white;
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("方法列表", EditorStyles.boldLabel);
            _methodScrollPosition = EditorGUILayout.BeginScrollView(_methodScrollPosition, GUILayout.Height(200));

            if (_methods.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到方法", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < _methods.Count; i++)
                {
                    var method = _methods[i];
                    string signature = GMReflectionService.GetMethodSignature(method);

                    // 高亮选中的方法
                    if (i == _selectedMethodIndex)
                    {
                        GUI.backgroundColor = Color.cyan;
                    }

                    if (GUILayout.Button(signature, EditorStyles.label, GUILayout.Height(18)))
                    {
                        _selectedMethodIndex = i;
                        OnMethodSelected();
                    }

                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制中间面板（参数输入和执行）
        /// </summary>
        private void DrawCenterPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(RIGHT_PANEL_WIDTH));

            if (string.IsNullOrEmpty(_selectedTypeName) || _selectedMethodIndex < 0 || _selectedMethodIndex >= _methods.Count)
            {
                EditorGUILayout.HelpBox("请从左侧选择一个类型和方法", MessageType.Info);
            }
            else
            {
                var method = _methods[_selectedMethodIndex];
                string signature = GMReflectionService.GetMethodSignature(method);

                // 方法签名显示
                EditorGUILayout.LabelField("方法签名", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(signature, EditorStyles.wordWrappedLabel, GUILayout.Height(50));
                EditorGUILayout.Space();

                // 参数输入
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    EditorGUILayout.HelpBox("此方法无需参数", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField("参数输入", EditorStyles.boldLabel);
                    _parameterScrollPosition = EditorGUILayout.BeginScrollView(_parameterScrollPosition, GUILayout.Height(300));

                    // 确保参数值数组大小正确
                    if (_parameterValues.Length != parameters.Length)
                    {
                        Array.Resize(ref _parameterValues, parameters.Length);
                    }

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        string hint = GMParameterConverter.GetParameterHint(param.ParameterType);

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        {
                            string typeShortName = GMReflectionService.GetTypeShortName(param.ParameterType);
                            EditorGUILayout.LabelField($"{param.Name} ({typeShortName})", EditorStyles.boldLabel);
                            EditorGUILayout.LabelField($"提示: {hint}", EditorStyles.miniLabel);

                            _parameterValues[i] = EditorGUILayout.TextField("值", _parameterValues[i] ?? "");
                        }
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(3);
                    }

                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.Space();

                // 执行按钮
                if (GUILayout.Button("执行方法", GUILayout.Height(30)))
                {
                    ExecuteMethod();
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制右侧面板（历史记录）
        /// </summary>
        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(RIGHT_PANEL_WIDTH));

            EditorGUILayout.LabelField("历史记录", EditorStyles.boldLabel);
            _historyScrollPosition = EditorGUILayout.BeginScrollView(_historyScrollPosition, GUILayout.ExpandHeight(true));

            if (_historyList.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无历史记录", MessageType.Info);
            }
            else
            {
                for (int i = _historyList.Count - 1; i >= 0; i--)
                {
                    var item = _historyList[i];
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    {
                        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                        {
                            EditorGUILayout.LabelField(item.GetDisplayName(), EditorStyles.miniLabel);
                            EditorGUILayout.LabelField(item.ExecuteTime.ToString("HH:mm:ss"), EditorStyles.centeredGreyMiniLabel);
                        }
                        EditorGUILayout.EndVertical();

                        if (GUILayout.Button("使用", GUILayout.Width(50), GUILayout.Height(35)))
                        {
                            LoadFromHistory(item);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(2);
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制垂直分隔线
        /// </summary>
        private void DrawVerticalSeparator()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(3));
            EditorGUILayout.Space();
            Rect rect = GUILayoutUtility.GetRect(3, Screen.height, GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f));
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制结果显示面板
        /// </summary>
        private void DrawResultPanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.LabelField("执行结果", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.TextArea(_resultText, GUILayout.Height(100));
            }
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("清除结果", GUILayout.Height(25)))
            {
                _resultText = "";
                _showResult = false;
            }
        }

        /// <summary>
        /// 获取分类显示名称
        /// </summary>
        private string GetCategoryDisplayName(GMReflectionService.TypeCategory category)
        {
            return category switch
            {
                GMReflectionService.TypeCategory.Manager => "Manager",
                GMReflectionService.TypeCategory.GameMode => "GameMode",
                GMReflectionService.TypeCategory.Other => "其他",
                _ => category.ToString()
            };
        }

        /// <summary>
        /// 获取简化的类型名称
        /// </summary>
        private string GetSimpleTypeName(string typeName)
        {
            if (typeName.StartsWith("GameMode:"))
            {
                return typeName.Substring(9); // 移除 "GameMode: " 前缀
            }
            
            // 只显示类名，不显示命名空间
            int lastDot = typeName.LastIndexOf('.');
            return lastDot >= 0 ? typeName.Substring(lastDot + 1) : typeName;
        }

        /// <summary>
        /// 刷新类型列表
        /// </summary>
        private void RefreshTypeList()
        {
            try
            {
                GMReflectionService.Reset();
                GMReflectionService.Initialize();
                _categorizedTypes = GMReflectionService.GetCategorizedTypes();
                _selectedTypeName = null;
                _methods.Clear();
                _selectedMethodIndex = -1;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GMTool] 刷新类型列表失败: {ex}");
                EditorUtility.DisplayDialog("错误", $"刷新类型列表失败: {ex.Message}", "确定");
            }
        }

        /// <summary>
        /// 类型选择回调
        /// </summary>
        private void OnTypeSelected()
        {
            if (string.IsNullOrEmpty(_selectedTypeName))
            {
                _methods.Clear();
                _selectedMethodIndex = -1;
                return;
            }

            _methods = GMReflectionService.GetMethods(_selectedTypeName);
            _selectedMethodIndex = -1;
            _parameterValues = new string[0];
            _resultText = "";
            _showResult = false;
        }

        /// <summary>
        /// 方法选择回调
        /// </summary>
        private void OnMethodSelected()
        {
            if (_selectedMethodIndex < 0 || _selectedMethodIndex >= _methods.Count)
            {
                _parameterValues = new string[0];
                return;
            }

            var method = _methods[_selectedMethodIndex];
            Debug.Log($"[GMTool] 选择方法: {method.Name}, 类型: {method.DeclaringType?.Name}");
            var parameters = method.GetParameters();
            _parameterValues = new string[parameters.Length];
            _resultText = "";
            _showResult = false;
        }

        /// <summary>
        /// 执行选中的方法
        /// </summary>
        private void ExecuteMethod()
        {
            if (string.IsNullOrEmpty(_selectedTypeName))
            {
                _resultText = "错误: 未选择类型";
                _showResult = true;
                return;
            }

            if (_selectedMethodIndex < 0 || _selectedMethodIndex >= _methods.Count)
            {
                _resultText = "错误: 未选择方法";
                _showResult = true;
                return;
            }

            var method = _methods[_selectedMethodIndex];

            try
            {
                // 获取实例
                object instance = null;
                if (!method.IsStatic)
                {
                    Debug.Log($"[GMTool] 尝试获取实例: {_selectedTypeName}");
                    instance = GMReflectionService.GetInstance(_selectedTypeName);
                    if (instance == null)
                    {
                        _resultText = $"错误: 无法获取 {_selectedTypeName} 的实例。\n请确保该单例已初始化或GameMode已激活。";
                        _showResult = true;
                        Debug.LogWarning($"[GMTool] 无法获取实例: {_selectedTypeName}");
                        return;
                    }
                    Debug.Log($"[GMTool] 成功获取实例: {_selectedTypeName}, 类型: {instance.GetType().Name}");
                }

                // 转换参数
                var parameters = method.GetParameters();
                object[] parameterObjects = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    try
                    {
                        parameterObjects[i] = GMParameterConverter.ConvertParameter(_parameterValues[i] ?? "", parameters[i].ParameterType);
                    }
                    catch (Exception ex)
                    {
                        _resultText = $"参数 '{parameters[i].Name}' 转换失败: {ex.Message}";
                        _showResult = true;
                        return;
                    }
                }

                // 调用方法
                Debug.Log($"[GMTool] 准备调用方法: {_selectedTypeName}.{method.Name}()");
                object result = method.Invoke(instance, parameterObjects);
                Debug.Log($"[GMTool] 方法调用完成: {_selectedTypeName}.{method.Name}()");

                // 显示结果
                if (method.ReturnType == typeof(void))
                {
                    _resultText = "方法执行成功（无返回值）";
                }
                else if (result == null)
                {
                    _resultText = "方法执行成功（返回 null）";
                }
                else
                {
                    _resultText = $"方法执行成功\n返回值 ({method.ReturnType.Name}):\n{result}";
                }

                _showResult = true;
                Debug.Log($"[GMTool] 执行方法: {_selectedTypeName}.{method.Name}() 成功");

                // 添加到历史记录
                AddToHistory(_selectedTypeName, method.Name, _parameterValues);
            }
            catch (TargetInvocationException ex)
            {
                // 获取内部异常
                Exception innerEx = ex.InnerException ?? ex;
                _resultText = $"执行失败:\n{innerEx.GetType().Name}: {innerEx.Message}\n\n堆栈跟踪:\n{innerEx.StackTrace}";
                _showResult = true;
                Debug.LogError($"[GMTool] 执行方法失败: {ex}");
            }
            catch (Exception ex)
            {
                _resultText = $"执行失败:\n{ex.GetType().Name}: {ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}";
                _showResult = true;
                Debug.LogError($"[GMTool] 执行方法失败: {ex}");
            }
        }

        /// <summary>
        /// 添加到历史记录
        /// </summary>
        private void AddToHistory(string typeName, string methodName, string[] parameterValues)
        {
            // 复制参数数组以避免引用问题
            string[] paramCopy = parameterValues != null ? (string[])parameterValues.Clone() : new string[0];
            var newItem = new GMHistoryItem(typeName, methodName, paramCopy);
            
            // 检查是否已存在相同的记录
            var existingIndex = _historyList.FindIndex(item => item.IsSameAs(newItem));
            if (existingIndex >= 0)
            {
                // 移除旧记录
                _historyList.RemoveAt(existingIndex);
            }
            
            // 添加到列表开头
            _historyList.Insert(0, newItem);
            
            // 限制最大数量
            if (_historyList.Count > MAX_HISTORY_COUNT)
            {
                _historyList.RemoveRange(MAX_HISTORY_COUNT, _historyList.Count - MAX_HISTORY_COUNT);
            }
            
            SaveHistory();
        }

        /// <summary>
        /// 从历史记录加载
        /// </summary>
        private void LoadFromHistory(GMHistoryItem item)
        {
            // 设置类型
            _selectedTypeName = item.TypeName;
            OnTypeSelected();
            
            // 查找方法
            var method = _methods.FirstOrDefault(m => m.Name == item.MethodName);
            if (method != null)
            {
                _selectedMethodIndex = _methods.IndexOf(method);
                OnMethodSelected();
                
                // 加载参数值
                var paramArray = item.GetParameterArray();
                if (paramArray != null && paramArray.Length == _parameterValues.Length)
                {
                    Array.Copy(paramArray, _parameterValues, paramArray.Length);
                }
                
                Debug.Log($"[GMTool] 从历史记录加载: {item.GetDisplayName()}");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", $"找不到方法: {item.MethodName}", "确定");
            }
        }

        /// <summary>
        /// 保存历史记录
        /// </summary>
        private void SaveHistory()
        {
            try
            {
                string json = JsonUtility.ToJson(new HistoryData { Items = _historyList });
                EditorPrefs.SetString(HISTORY_PREF_KEY, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GMTool] 保存历史记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载历史记录
        /// </summary>
        private void LoadHistory()
        {
            try
            {
                string json = EditorPrefs.GetString(HISTORY_PREF_KEY, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonUtility.FromJson<HistoryData>(json);
                    if (data != null && data.Items != null)
                    {
                        _historyList = data.Items;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GMTool] 加载历史记录失败: {ex.Message}");
                _historyList = new List<GMHistoryItem>();
            }
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        private void ClearHistory()
        {
            _historyList.Clear();
            EditorPrefs.DeleteKey(HISTORY_PREF_KEY);
        }

        /// <summary>
        /// 历史记录数据包装类（用于JSON序列化）
        /// </summary>
        [Serializable]
        private class HistoryData
        {
            public List<GMHistoryItem> Items = new List<GMHistoryItem>();
        }
    }
}
