using System;
using System.Collections.Generic;
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
        // UI状态
        private int _selectedTypeIndex = 0;
        private string[] _availableTypeNames = new string[0];
        private List<MethodInfo> _methods = new List<MethodInfo>();
        private int _selectedMethodIndex = -1;
        private string[] _parameterValues = new string[0];
        private Vector2 _typeScrollPosition;
        private Vector2 _methodScrollPosition;
        private Vector2 _parameterScrollPosition;
        private string _resultText = "";
        private bool _showResult = false;

        // 窗口设置
        private const float MIN_WINDOW_WIDTH = 600f;
        private const float MIN_WINDOW_HEIGHT = 500f;

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
                // 左侧：类型和方法列表
                DrawLeftPanel();

                // 中间分隔线
                DrawVerticalSeparator();

                // 右侧：参数输入和执行
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

                GUILayout.FlexibleSpace();

                EditorGUILayout.LabelField("GM工具 - 反射调用单例和GameMode方法", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制左侧面板（类型和方法列表）
        /// </summary>
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));

            // 类型选择
            EditorGUILayout.LabelField("目标类型", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            {
                int newIndex = EditorGUILayout.Popup(_selectedTypeIndex, _availableTypeNames);
                if (newIndex != _selectedTypeIndex)
                {
                    _selectedTypeIndex = newIndex;
                    OnTypeSelected();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 方法列表
            EditorGUILayout.LabelField("方法列表", EditorStyles.boldLabel);
            _methodScrollPosition = EditorGUILayout.BeginScrollView(_methodScrollPosition, GUILayout.ExpandHeight(true));

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

                    if (GUILayout.Button(signature, EditorStyles.label, GUILayout.Height(20)))
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
        /// 绘制右侧面板（参数输入和执行）
        /// </summary>
        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            if (_selectedMethodIndex < 0 || _selectedMethodIndex >= _methods.Count)
            {
                EditorGUILayout.HelpBox("请从左侧选择一个方法", MessageType.Info);
            }
            else
            {
                var method = _methods[_selectedMethodIndex];
                string signature = GMReflectionService.GetMethodSignature(method);

                // 方法签名显示
                EditorGUILayout.LabelField("方法签名", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(signature, EditorStyles.wordWrappedLabel);
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
                    _parameterScrollPosition = EditorGUILayout.BeginScrollView(_parameterScrollPosition);

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
                            EditorGUILayout.LabelField($"提示: {GMParameterConverter.GetParameterHint(param.ParameterType)}", EditorStyles.miniLabel);

                            _parameterValues[i] = EditorGUILayout.TextField("值", _parameterValues[i] ?? "");
                        }
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(5);
                    }

                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.Space();

                // 执行按钮
                GUI.enabled = true;
                if (GUILayout.Button("执行方法", GUILayout.Height(30)))
                {
                    ExecuteMethod();
                }
                GUI.enabled = true;
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制垂直分隔线
        /// </summary>
        private void DrawVerticalSeparator()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(5));
            EditorGUILayout.Space();
            Rect rect = GUILayoutUtility.GetRect(5, Screen.height, GUILayout.ExpandHeight(true));
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
        /// 刷新类型列表
        /// </summary>
        private void RefreshTypeList()
        {
            try
            {
                GMReflectionService.Reset();
                GMReflectionService.Initialize();
                _availableTypeNames = GMReflectionService.GetAvailableTypeNames().ToArray();

                if (_availableTypeNames.Length > 0)
                {
                    _selectedTypeIndex = 0;
                    OnTypeSelected();
                }
                else
                {
                    _selectedTypeIndex = -1;
                    _methods.Clear();
                }
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
            if (_selectedTypeIndex < 0 || _selectedTypeIndex >= _availableTypeNames.Length)
            {
                _methods.Clear();
                _selectedMethodIndex = -1;
                return;
            }

            string typeName = _availableTypeNames[_selectedTypeIndex];
            _methods = GMReflectionService.GetMethods(typeName);
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
            if (_selectedTypeIndex < 0 || _selectedTypeIndex >= _availableTypeNames.Length)
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

            string typeName = _availableTypeNames[_selectedTypeIndex];
            var method = _methods[_selectedMethodIndex];

            try
            {
                // 获取实例
                object instance = null;
                if (!method.IsStatic)
                {
                    Debug.Log($"[GMTool] 尝试获取实例: {typeName}");
                    instance = GMReflectionService.GetInstance(typeName);
                    if (instance == null)
                    {
                        _resultText = $"错误: 无法获取 {typeName} 的实例。\n请确保该单例已初始化或GameMode已激活。";
                        _showResult = true;
                        Debug.LogWarning($"[GMTool] 无法获取实例: {typeName}");
                        return;
                    }
                    Debug.Log($"[GMTool] 成功获取实例: {typeName}, 类型: {instance.GetType().Name}");
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
                Debug.Log($"[GMTool] 准备调用方法: {typeName}.{method.Name}()");
                object result = method.Invoke(instance, parameterObjects);
                Debug.Log($"[GMTool] 方法调用完成: {typeName}.{method.Name}()");

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
                Debug.Log($"[GMTool] 执行方法: {typeName}.{method.Name}() 成功");
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
    }
}

