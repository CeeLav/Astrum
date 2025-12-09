using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Astrum.Editor.SceneEditor.Data;
using Astrum.Editor.SceneEditor.Persistence;
using Astrum.Editor.SceneEditor.Visualizers;
using Astrum.Editor.SceneEditor.Converters;
using Astrum.Editor.SceneEditor.Registry;
using Astrum.Editor.SceneEditor.UI;
using cfg;

namespace Astrum.Editor.SceneEditor.Windows
{
    public class SceneEditorWindow : EditorWindow
    {
        private string _selectedScenePath;
        private List<SceneTriggerEditorData> _triggers = new List<SceneTriggerEditorData>();
        private SceneTriggerEditorData _selectedTrigger;
        private Vector2 _triggerListScroll;
        private Vector2 _parameterScroll;
        private SceneTriggerVisualizer _visualizer;
        private bool _isDirty;
        
        private const float MIN_WINDOW_WIDTH = 1000f;
        private const float MIN_WINDOW_HEIGHT = 600f;
        
        [MenuItem("Astrum/Editor 编辑器/Scene Editor 场景编辑器", false, 5)]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneEditorWindow>("场景编辑器");
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }
        
        private void OnEnable()
        {
            _visualizer = new SceneTriggerVisualizer();
            _visualizer.Initialize();
            LoadData();
        }
        
        private void OnDisable()
        {
            _visualizer?.Cleanup();
        }
        
        private void OnDestroy()
        {
            _visualizer?.Cleanup();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            
            // 左侧面板
            DrawLeftPanel();
            
            // 中间面板
            DrawCenterPanel();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            
            // 场景选择
            DrawSceneSelection();
            
            EditorGUILayout.Space();
            
            // Trigger列表
            DrawTriggerList();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCenterPanel()
        {
            EditorGUILayout.BeginVertical();
            
            if (_selectedTrigger != null)
            {
                DrawTriggerDetails();
            }
            else
            {
                EditorGUILayout.HelpBox("请选择一个Trigger", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSceneSelection()
        {
            EditorGUILayout.LabelField("场景选择", EditorStyles.boldLabel);
            
            // 获取所有场景文件
            var sceneFiles = GetSceneFiles();
            var sceneNames = sceneFiles.Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToArray();
            var currentIndex = -1;
            
            if (!string.IsNullOrEmpty(_selectedScenePath))
            {
                var selectedName = Path.GetFileNameWithoutExtension(_selectedScenePath);
                currentIndex = System.Array.IndexOf(sceneNames, selectedName);
            }
            
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("场景", currentIndex >= 0 ? currentIndex : 0, sceneNames);
            if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < sceneFiles.Count)
            {
                _selectedScenePath = sceneFiles[newIndex].FullName;
                OpenScene(_selectedScenePath);
                LoadData();
            }
        }
        
        private void DrawTriggerList()
        {
            EditorGUILayout.LabelField("Trigger列表", EditorStyles.boldLabel);
            
            _triggerListScroll = EditorGUILayout.BeginScrollView(_triggerListScroll);
            
            foreach (var trigger in _triggers)
            {
                var style = _selectedTrigger == trigger ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                if (GUILayout.Button($"Trigger {trigger.TriggerId}", style))
                {
                    _selectedTrigger = trigger;
                    UpdateVisualizers();
                    Repaint();
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawTriggerDetails()
        {
            _parameterScroll = EditorGUILayout.BeginScrollView(_parameterScroll);
            
            EditorGUILayout.LabelField($"Trigger {_selectedTrigger.TriggerId}", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            // 条件列表
            EditorGUILayout.LabelField("条件", EditorStyles.boldLabel);
            foreach (var condition in _selectedTrigger.Conditions)
            {
                DrawConditionEditor(condition);
            }
            
            EditorGUILayout.Space();
            
            // 动作列表
            EditorGUILayout.LabelField("动作", EditorStyles.boldLabel);
            foreach (var action in _selectedTrigger.Actions)
            {
                DrawActionEditor(action);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                _isDirty = true;
                UpdateVisualizers();
            }
            
            EditorGUILayout.Space();
            
            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("导出 JSON"))
            {
                ExportToJson();
            }
            if (GUILayout.Button("导入 JSON"))
            {
                ImportFromJson();
            }
            if (GUILayout.Button("保存"))
            {
                SaveData();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawConditionEditor(ConditionEditorData condition)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"条件 {condition.ConditionId}: {condition.ConditionType}", EditorStyles.boldLabel);
            
            if (condition.Parameters != null)
            {
                ParameterEditorHelper.DrawParameterEditor(condition.Parameters);
            }
            else
            {
                EditorGUILayout.HelpBox("参数未初始化", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActionEditor(ActionEditorData action)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"动作 {action.ActionId}: {action.ActionType}", EditorStyles.boldLabel);
            
            if (action.Parameters != null)
            {
                ParameterEditorHelper.DrawParameterEditor(action.Parameters);
            }
            else
            {
                EditorGUILayout.HelpBox("参数未初始化", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void SaveData()
        {
            if (SceneTriggerDataWriter.WriteAll(_triggers))
            {
                _isDirty = false;
                EditorUtility.DisplayDialog("保存成功", "Trigger数据已保存到表格", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("保存失败", "保存Trigger数据时出错，请查看Console", "确定");
            }
        }
        
        private void ExportToJson()
        {
            if (_selectedTrigger == null || _selectedTrigger.Actions.Count == 0)
            {
                EditorUtility.DisplayDialog("导出失败", "请先选择一个包含动作的Trigger", "确定");
                return;
            }
            
            // 导出第一个动作的参数（示例）
            var action = _selectedTrigger.Actions[0];
            var converter = TriggerParameterConverterRegistry.GetActionConverter(action.ActionType);
            
            if (converter == null || action.Parameters == null)
            {
                EditorUtility.DisplayDialog("导出失败", "无法获取转换器或参数为空", "确定");
                return;
            }
            
            try
            {
                var json = ExportParametersToJson(converter, action.Parameters);
                var path = EditorUtility.SaveFilePanel("导出JSON", "", $"Trigger_{_selectedTrigger.TriggerId}_Action_{action.ActionId}.json", "json");
                
                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllText(path, json);
                    EditorUtility.DisplayDialog("导出成功", $"JSON已保存到:\n{path}", "确定");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("导出失败", $"导出时出错: {ex.Message}", "确定");
            }
        }
        
        private void ImportFromJson()
        {
            var path = EditorUtility.OpenFilePanel("导入JSON", "", "json");
            if (string.IsNullOrEmpty(path)) return;
            
            try
            {
                var json = File.ReadAllText(path);
                
                if (_selectedTrigger == null || _selectedTrigger.Actions.Count == 0)
                {
                    EditorUtility.DisplayDialog("导入失败", "请先选择一个包含动作的Trigger", "确定");
                    return;
                }
                
                var action = _selectedTrigger.Actions[0];
                var converter = TriggerParameterConverterRegistry.GetActionConverter(action.ActionType);
                
                if (converter == null)
                {
                    EditorUtility.DisplayDialog("导入失败", "无法获取转换器", "确定");
                    return;
                }
                
                dynamic result = ImportParametersFromJson(converter, json);
                
                if (result != null && result.Success)
                {
                    action.Parameters = result.Parameters;
                    _isDirty = true;
                    UpdateVisualizers();
                    
                    var message = "导入成功";
                    if (result.Warnings != null && result.Warnings.Count > 0)
                    {
                        var warnings = new List<ImportWarning>();
                        foreach (var w in result.Warnings)
                        {
                            warnings.Add(w);
                        }
                        message += $"\n\n警告:\n{string.Join("\n", warnings.Select(w => $"- {w.Message}"))}";
                    }
                    
                    EditorUtility.DisplayDialog("导入成功", message, "确定");
                }
                else
                {
                    var errorMsg = "导入失败";
                    if (result != null && result.Errors != null)
                    {
                        var errors = new List<ImportError>();
                        foreach (var e in result.Errors)
                        {
                            errors.Add(e);
                        }
                        errorMsg += $":\n{string.Join("\n", errors.Select(e => $"- {e.Message}"))}";
                    }
                    EditorUtility.DisplayDialog("导入失败", errorMsg, "确定");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("导入失败", $"导入时出错: {ex.Message}", "确定");
            }
        }
        
        private string ExportParametersToJson(object converter, object parameters)
        {
            try
            {
                // 使用dynamic简化调用
                dynamic dynamicConverter = converter;
                dynamic dynamicParameters = parameters;
                return dynamicConverter.ExportToJson(dynamicParameters, false);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SceneEditor] Failed to export JSON: {ex.Message}");
                return "{}";
            }
        }
        
        private object ImportParametersFromJson(object converter, string json)
        {
            try
            {
                // 使用dynamic简化调用
                dynamic dynamicConverter = converter;
                var result = dynamicConverter.ImportFromJson(json);
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SceneEditor] Failed to import JSON: {ex.Message}");
                return null;
            }
        }
        
        private List<FileInfo> GetSceneFiles()
        {
            var scenesPath = Path.Combine(Application.dataPath, "Scenes");
            if (!Directory.Exists(scenesPath))
                return new List<FileInfo>();
            
            var dir = new DirectoryInfo(scenesPath);
            return dir.GetFiles("*.unity").ToList();
        }
        
        private void OpenScene(string scenePath)
        {
            // scenePath是绝对路径（FileInfo.FullName）
            // 需要转换为Unity相对路径 Assets/Scenes/xxx.unity
            
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError("[SceneEditor] Scene path is empty");
                return;
            }
            
            // 统一路径格式（都使用正斜杠）
            var normalizedScenePath = scenePath.Replace("\\", "/");
            var normalizedDataPath = Application.dataPath.Replace("\\", "/");
            
            // 转换为Unity相对路径
            string unityPath;
            if (normalizedScenePath.StartsWith(normalizedDataPath))
            {
                // 绝对路径，转换为Assets/...格式
                var relativePath = normalizedScenePath.Substring(normalizedDataPath.Length);
                // 移除开头的斜杠（如果有）
                if (relativePath.StartsWith("/"))
                    relativePath = relativePath.Substring(1);
                unityPath = "Assets/" + relativePath;
            }
            else if (normalizedScenePath.StartsWith("Assets/"))
            {
                // 已经是Unity路径
                unityPath = normalizedScenePath;
            }
            else
            {
                // 假设在Assets/Scenes下
                unityPath = "Assets/Scenes/" + Path.GetFileName(normalizedScenePath);
            }
            
            // 验证文件是否存在（使用Unity路径验证）
            var fullPath = Path.Combine(Application.dataPath, unityPath.Substring(7));
            if (File.Exists(fullPath))
            {
                EditorSceneManager.OpenScene(unityPath);
            }
            else
            {
                Debug.LogError($"[SceneEditor] Scene file not found: '{unityPath}' (Full path: '{fullPath}')");
            }
        }
        
        private void LoadData()
        {
            _triggers = SceneTriggerDataReader.ReadAll();
            _selectedTrigger = null;
            _isDirty = false;
        }
        
        private void UpdateVisualizers()
        {
            if (_selectedTrigger == null) return;
            
            foreach (var action in _selectedTrigger.Actions)
            {
                if (action.Parameters is IPositionInfoProvider posInfo)
                {
                    _visualizer.CreateOrUpdateVisualizer(action, posInfo);
                }
            }
        }
    }
}

