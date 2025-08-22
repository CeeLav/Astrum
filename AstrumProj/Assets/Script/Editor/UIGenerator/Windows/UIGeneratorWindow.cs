using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.UIGenerator.Core;
using Astrum.Editor.UIGenerator.Utils;
using Astrum.Client.UI.Core;

namespace Astrum.Editor.UIGenerator.Windows
{
    /// <summary>
    /// UI文件信息
    /// </summary>
    [System.Serializable]
    public class UIFileInfo
    {
        public string Name;
        public string Path;
        public string RelativePath;
        public bool IsFolder;
        public System.Collections.Generic.List<UIFileInfo> Children = new System.Collections.Generic.List<UIFileInfo>();
        public UIFileInfo Parent;
        public GameObject Prefab;
        public bool HasUIRefs;
        public bool HasGeneratedCode;
        
        // 增强的状态信息
        public UIStatusInfo StatusInfo = new UIStatusInfo();
        
        public UIFileInfo(string name, string path, bool isFolder = false)
        {
            Name = name;
            Path = path;
            IsFolder = isFolder;
            RelativePath = path.Replace(Application.dataPath, "Assets");
        }
    }
    
    /// <summary>
    /// UI状态信息
    /// </summary>
    [System.Serializable]
    public class UIStatusInfo
    {
        // UIRefs组件状态
        public bool HasUIRefsComponent = false;
        public bool UIRefsDataValid = false;
        public int UIRefsReferenceCount = 0;
        public string UIRefsClassName = "";
        public string UIRefsNamespace = "";
        public System.Collections.Generic.List<string> UIRefsErrors = new System.Collections.Generic.List<string>();
        
        // 代码生成状态
        public bool HasGeneratedCode = false;
        public string GeneratedCodePath = "";
        public bool CodeIsComplete = false;
        public System.DateTime CodeLastModified = System.DateTime.MinValue;
        public System.Collections.Generic.List<string> CodeErrors = new System.Collections.Generic.List<string>();
        
        // 整体状态
        public UIOverallStatus OverallStatus = UIOverallStatus.Unknown;
        public string StatusMessage = "";
        
        public void UpdateOverallStatus()
        {
            if (!HasUIRefsComponent && !HasGeneratedCode)
            {
                OverallStatus = UIOverallStatus.NotGenerated;
                StatusMessage = "未生成任何内容";
            }
            else if (HasUIRefsComponent && HasGeneratedCode && UIRefsDataValid && CodeIsComplete)
            {
                OverallStatus = UIOverallStatus.Complete;
                StatusMessage = "生成完整";
            }
            else if (HasUIRefsComponent && !UIRefsDataValid)
            {
                OverallStatus = UIOverallStatus.UIRefsInvalid;
                StatusMessage = "UIRefs数据无效";
            }
            else if (HasGeneratedCode && !CodeIsComplete)
            {
                OverallStatus = UIOverallStatus.CodeIncomplete;
                StatusMessage = "代码不完整";
            }
            else if (HasUIRefsComponent && !HasGeneratedCode)
            {
                OverallStatus = UIOverallStatus.UIRefsOnly;
                StatusMessage = "仅有UIRefs组件";
            }
            else if (!HasUIRefsComponent && HasGeneratedCode)
            {
                OverallStatus = UIOverallStatus.CodeOnly;
                StatusMessage = "仅有生成代码";
            }
            else
            {
                OverallStatus = UIOverallStatus.Partial;
                StatusMessage = "部分生成";
            }
        }
    }
    
    /// <summary>
    /// UI整体状态枚举
    /// </summary>
    public enum UIOverallStatus
    {
        Unknown,
        NotGenerated,
        UIRefsOnly,
        CodeOnly,
        Partial,
        Complete,
        UIRefsInvalid,
        CodeIncomplete
    }
    
    /// <summary>
    /// UI生成器编辑器窗口
    /// </summary>
    public class UIGeneratorWindow : EditorWindow
    {
        private Core.UIGenerator uiGenerator;
        private UIGeneratorConfig config;
        
        // UI状态
        private string uiName = "";
        private string prefabPath = "";
        private GameObject selectedPrefab;
        private bool showAdvancedSettings = false;
        private bool autoGenerate = false;
        private bool showLogs = true;
        
        // 文件浏览器状态
        private Vector2 fileBrowserScrollPosition;
        private Vector2 functionPanelScrollPosition;
        private Vector2 logScrollPosition;
        private string searchFilter = "";
        private System.Collections.Generic.Dictionary<string, bool> folderExpandedStates = new System.Collections.Generic.Dictionary<string, bool>();
        private System.Collections.Generic.List<UIFileInfo> uiFiles = new System.Collections.Generic.List<UIFileInfo>();
        private UIFileInfo selectedUIFile;
        
        // 日志内容
        private string logContent = "";
        private bool autoScrollLogs = true;
        
        // 生成结果
        private UIGenerationResult lastGenerationResult;
        private bool showResult = false;
        
        [MenuItem("Tools/UI Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<UIGeneratorWindow>("UI Generator");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            InitializeGenerator();
            RefreshUIFiles();
        }
        
        private void InitializeGenerator()
        {
            try
            {
                uiGenerator = new Core.UIGenerator();
                config = new UIGeneratorConfig();
                
                Debug.Log("[UI Generator] 初始化完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UI Generator] 初始化失败: {ex.Message}");
            }
        }
        
        private void RefreshUIFiles()
        {
            try
            {
                uiFiles.Clear();
                folderExpandedStates.Clear();
                
                string uiPath = Path.Combine(Application.dataPath, "ArtRes", "UI");
                if (Directory.Exists(uiPath))
                {
                    ScanDirectory(uiPath, uiFiles, null);
                }
                else
                {
                    AddLog("UI目录不存在: Assets/ArtRes/UI");
                }
                
                AddLog($"扫描完成，找到 {uiFiles.Count} 个UI文件");
                
                // 刷新所有UI文件的状态信息
                RefreshAllUIStatus();
            }
            catch (Exception ex)
            {
                AddLog($"扫描UI文件失败: {ex.Message}");
                Debug.LogError($"[UI Generator] 扫描UI文件失败: {ex.Message}");
            }
        }
        
        private void ScanDirectory(string directoryPath, System.Collections.Generic.List<UIFileInfo> parentList, UIFileInfo parent)
        {
            try
            {
                // 扫描子目录
                var directories = Directory.GetDirectories(directoryPath);
                foreach (var dir in directories)
                {
                    var dirInfo = new UIFileInfo(Path.GetFileName(dir), dir, true);
                    dirInfo.Parent = parent;
                    parentList.Add(dirInfo);
                    
                    // 递归扫描子目录
                    ScanDirectory(dir, dirInfo.Children, dirInfo);
                }
                
                // 扫描Prefab文件
                var prefabFiles = Directory.GetFiles(directoryPath, "*.prefab");
                foreach (var file in prefabFiles)
                {
                    var fileInfo = new UIFileInfo(Path.GetFileNameWithoutExtension(file), file);
                    fileInfo.Parent = parent;
                    
                    // 检查是否已有UIRefs组件
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fileInfo.RelativePath);
                    if (prefab != null)
                    {
                        fileInfo.Prefab = prefab;
                        fileInfo.HasUIRefs = prefab.GetComponent<UIRefs>() != null;
                        
                        // 检查是否已有生成的代码
                        string codePath = Path.Combine(UIGeneratorConfig.CODE_OUTPUT_PATH, $"{fileInfo.Name}UI.cs");
                        fileInfo.HasGeneratedCode = File.Exists(codePath);
                    }
                    
                    parentList.Add(fileInfo);
                }
            }
            catch (Exception ex)
            {
                AddLog($"扫描目录失败 {directoryPath}: {ex.Message}");
            }
        }
        
        private void LoadUIFile(UIFileInfo fileInfo)
        {
            try
            {
                if (fileInfo.Prefab == null)
                {
                    fileInfo.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fileInfo.RelativePath);
                }
                
                if (fileInfo.Prefab != null)
                {
                    selectedPrefab = fileInfo.Prefab;
                    prefabPath = fileInfo.RelativePath;
                    uiName = fileInfo.Name;
                    
                    AddLog($"加载UI文件: {fileInfo.Name}");
                    
                    // 自动打开Prefab进行编辑
                    OpenPrefabForEditing(fileInfo.Prefab);
                }
                else
                {
                    AddLog($"无法加载Prefab: {fileInfo.RelativePath}");
                }
            }
            catch (Exception ex)
            {
                AddLog($"加载UI文件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 打开Prefab进行编辑
        /// </summary>
        private void OpenPrefabForEditing(GameObject prefab)
        {
            try
            {
                if (prefab == null)
                {
                    AddLog("Prefab为空，无法打开编辑");
                    return;
                }
                
                // 使用AssetDatabase.OpenAsset打开Prefab进行编辑
                AssetDatabase.OpenAsset(prefab);
                AddLog($"已打开Prefab进行编辑: {prefab.name}");
            }
            catch (Exception ex)
            {
                AddLog($"打开Prefab编辑失败: {ex.Message}");
                Debug.LogError($"[UI Generator] 打开Prefab编辑失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 打开当前选中的Prefab进行编辑
        /// </summary>
        private void OpenSelectedPrefabForEditing()
        {
            try
            {
                if (selectedUIFile == null || selectedUIFile.Prefab == null)
                {
                    AddLog("错误: 请先选择一个UI文件");
                    return;
                }
                
                OpenPrefabForEditing(selectedUIFile.Prefab);
            }
            catch (Exception ex)
            {
                AddLog($"打开Prefab编辑时发生异常: {ex.Message}");
            }
        }
        
        private void OnGUI()
        {
            try
            {
                DrawHeader();
                DrawMainLayout();
                DrawLogs();
                DrawFooter();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UI Generator] 绘制GUI时发生异常: {ex.Message}");
                EditorGUILayout.HelpBox($"发生错误: {ex.Message}", MessageType.Error);
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("UI Generator", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                RefreshUIFiles();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
        }
        
        private void DrawMainLayout()
        {
            EditorGUILayout.BeginHorizontal();
            
            // 左侧文件浏览器
            DrawFileBrowser();
            
            // 分隔线
            EditorGUILayout.BeginVertical(GUILayout.Width(5));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            
            // 右侧功能面板
            DrawFunctionPanel();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFileBrowser()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(300));
            EditorGUILayout.LabelField("UI文件浏览器", EditorStyles.boldLabel);
            
            // 搜索框
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 文件列表
            fileBrowserScrollPosition = EditorGUILayout.BeginScrollView(fileBrowserScrollPosition);
            DrawUIFiles(uiFiles);
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawFunctionPanel()
        {
            EditorGUILayout.BeginVertical("box");
            
            if (selectedUIFile != null)
            {
                DrawUIFileInfo();
                DrawUIFunctions();
            }
            else
            {
                EditorGUILayout.LabelField("请选择一个UI文件", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawUIFiles(List<UIFileInfo> files)
        {
            foreach (var file in files)
            {
                if (!string.IsNullOrEmpty(searchFilter) && 
                    !file.Name.ToLower().Contains(searchFilter.ToLower()))
                {
                    continue;
                }
                
                DrawUIFileItem(file);
            }
        }
        
        private void DrawUIFileItem(UIFileInfo file)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (file.IsFolder)
            {
                // 文件夹
                bool isExpanded = folderExpandedStates.GetValueOrDefault(file.Path, false);
                bool newExpanded = EditorGUILayout.Foldout(isExpanded, file.Name, true);
                if (newExpanded != isExpanded)
                {
                    folderExpandedStates[file.Path] = newExpanded;
                }
                
                if (newExpanded)
                {
                    EditorGUI.indentLevel++;
                    DrawUIFiles(file.Children);
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                // 文件
                EditorGUI.indentLevel++;
                
                // 选择状态
                bool isSelected = selectedUIFile == file;
                bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                
                if (newSelected != isSelected)
                {
                    selectedUIFile = newSelected ? file : null;
                    if (newSelected)
                    {
                        LoadUIFile(file);
                    }
                }
                
                // 文件图标和名称
                GUIContent content = new GUIContent(file.Name, GetFileIcon(file));
                EditorGUILayout.LabelField(content);
                
                // 状态指示器
                DrawStatusIndicator(file);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private Texture2D GetFileIcon(UIFileInfo file)
        {
            if (file.IsFolder)
            {
                return EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            }
            else
            {
                return EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
            }
        }
        
        private void DrawUIFileInfo()
        {
            EditorGUILayout.LabelField("UI文件信息", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"名称: {selectedUIFile.Name}");
            EditorGUILayout.LabelField($"路径: {selectedUIFile.RelativePath}");
            
            if (selectedUIFile.Prefab != null)
            {
                EditorGUILayout.LabelField($"子对象数量: {selectedUIFile.Prefab.transform.childCount}");
                
                // 显示主要组件
                var components = selectedUIFile.Prefab.GetComponents<MonoBehaviour>();
                if (components.Length > 0)
                {
                    EditorGUILayout.LabelField("主要组件:");
                    foreach (var component in components)
                    {
                        if (component != null)
                        {
                            EditorGUILayout.LabelField($"  - {component.GetType().Name}");
                        }
                    }
                }
                
                // 显示Prefab编辑场景根节点引用信息
                DrawPrefabEditingSceneInfo();
            }
            
            EditorGUILayout.EndVertical();
            
            // 显示详细状态信息
            DrawDetailedStatusInfo();
        }
        
        /// <summary>
        /// 绘制Prefab编辑场景信息
        /// </summary>
        private void DrawPrefabEditingSceneInfo()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("生成状态", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            try
            {
                // 检查是否在Prefab编辑模式
                var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage == null)
                {
                    EditorGUILayout.LabelField("Prefab编辑模式: 未激活", EditorStyles.helpBox);
                }
                else
                {
                    EditorGUILayout.LabelField("Prefab编辑模式: 已激活", EditorStyles.helpBox);
                    
                    // 获取Prefab编辑场景的根节点
                    var scene = prefabStage.scene;
                    var rootObjects = scene.GetRootGameObjects();
                    
                    if (rootObjects.Length > 0)
                    {
                        // 查找Canvas下的UI根节点
                        GameObject prefabRoot = null;
                        foreach (var rootObj in rootObjects)
                        {
                            var canvas = rootObj.GetComponent<Canvas>();
                            if (canvas != null && rootObj.transform.childCount > 0)
                            {
                                // 获取Canvas下的第一个子对象作为UI根节点
                                prefabRoot = rootObj.transform.GetChild(0).gameObject;
                                break;
                            }
                        }
                        
                        // 如果没有找到Canvas下的子对象，使用第一个根对象
                        if (prefabRoot == null)
                        {
                            prefabRoot = rootObjects[0];
                        }
                        
                        // 显示根节点详细信息
                        EditorGUILayout.LabelField("根节点信息", EditorStyles.boldLabel);
                        EditorGUILayout.BeginVertical("box");
                        
                        // 使用ObjectField显示根节点对象
                        EditorGUILayout.LabelField("根节点对象:");
                        EditorGUILayout.ObjectField(prefabRoot, typeof(GameObject), true);
                        
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField($"名称: {prefabRoot.name}");
                        EditorGUILayout.LabelField($"激活状态: {(prefabRoot.activeInHierarchy ? "激活" : "未激活")}");
                        EditorGUILayout.LabelField($"子对象数量: {prefabRoot.transform.childCount}");
                        
                        // 显示根节点上的组件
                        var components = prefabRoot.GetComponents<MonoBehaviour>();
                        EditorGUILayout.LabelField($"组件数量: {components.Length}");
                        if (components.Length > 0)
                        {
                            EditorGUILayout.LabelField("组件列表:");
                            foreach (var component in components)
                            {
                                if (component != null)
                                {
                                    EditorGUILayout.LabelField($"  - {component.GetType().Name}");
                                }
                            }
                        }
                        EditorGUILayout.EndVertical();
                        
                        // 检查根节点上的UIRefs组件
                        var uiRefs = prefabRoot.GetComponent<Astrum.Client.UI.Core.UIRefs>();
                        if (uiRefs != null)
                        {
                            EditorGUILayout.LabelField("UIRefs组件: ✓ 已生成", EditorStyles.helpBox);
                            
                            // 显示UIRefs组件的关键信息
                            var uiClassNameField = typeof(Astrum.Client.UI.Core.UIRefs).GetField("uiClassName", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            
                            string uiClassName = uiClassNameField?.GetValue(uiRefs)?.ToString() ?? "未设置";
                            EditorGUILayout.LabelField($"UI类名: {uiClassName}");
                        }
                        else
                        {
                            EditorGUILayout.LabelField("UIRefs组件: ✗ 未生成", EditorStyles.helpBox);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("错误: 无法获取根节点", EditorStyles.helpBox);
                    }
                }
            }
            catch (System.Exception ex)
            {
                EditorGUILayout.LabelField($"错误: {ex.Message}", EditorStyles.helpBox);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制详细状态信息
        /// </summary>
        private void DrawDetailedStatusInfo()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("文件状态", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // 更新状态信息
            UpdateUIStatusInfo(selectedUIFile);
            
            // 整体状态
            Color originalColor = GUI.color;
            GUI.color = GetStatusColor(selectedUIFile.StatusInfo.OverallStatus);
            EditorGUILayout.LabelField($"整体状态: {GetStatusIcon(selectedUIFile.StatusInfo.OverallStatus)} {selectedUIFile.StatusInfo.StatusMessage}");
            GUI.color = originalColor;
            
            EditorGUILayout.Space(5);
            
            // 简化的状态显示
            EditorGUILayout.LabelField($"UIRefs组件: {(selectedUIFile.StatusInfo.HasUIRefsComponent ? "✓ 已生成" : "✗ 未生成")}");
            EditorGUILayout.LabelField($"UI代码: {(selectedUIFile.StatusInfo.HasGeneratedCode ? "✓ 已生成" : "✗ 未生成")}");
            
            if (selectedUIFile.StatusInfo.HasUIRefsComponent)
            {
                EditorGUILayout.LabelField($"引用数量: {selectedUIFile.StatusInfo.UIRefsReferenceCount}");
            }
            
            if (selectedUIFile.StatusInfo.HasGeneratedCode)
            {
                EditorGUILayout.LabelField($"代码完整性: {(selectedUIFile.StatusInfo.CodeIsComplete ? "完整" : "不完整")}");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawUIFunctions()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("功能操作", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // 生成UIRefs组件
            if (GUILayout.Button("生成UIRefs组件", GUILayout.Height(30)))
            {
                GenerateUIRefs();
            }
            
            // 生成UI代码
            if (GUILayout.Button("生成UI代码", GUILayout.Height(30)))
            {
                GenerateUICode();
            }
            
            // 批量生成
            if (GUILayout.Button("批量生成 (UIRefs + 代码)", GUILayout.Height(30)))
            {
                GenerateAll();
            }
            
            EditorGUILayout.Space(5);
            
            // 打开Prefab编辑
            if (GUILayout.Button("打开Prefab编辑", GUILayout.Height(30)))
            {
                OpenSelectedPrefabForEditing();
            }
            
            EditorGUILayout.Space(5);
            
            // 刷新状态信息
            if (GUILayout.Button("刷新状态信息", GUILayout.Height(25)))
            {
                RefreshAllUIStatus();
            }
            
            // 预览UI结构
            if (GUILayout.Button("预览UI结构", GUILayout.Height(25)))
            {
                PreviewUIStructure();
            }
            
            // 刷新引用
            if (GUILayout.Button("刷新引用", GUILayout.Height(25)))
            {
                RefreshReferences();
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("转换工具", EditorStyles.boldLabel);
            
            // TextMeshPro转Text工具
            if (GUILayout.Button("TextMeshPro → Text 转换", GUILayout.Height(25)))
            {
                ConvertTextMeshProToText();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawLogs()
        {
            if (!showLogs) return;
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("生成日志", EditorStyles.boldLabel);
            
            logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.Height(150));
            EditorGUILayout.TextArea(logContent, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空日志"))
            {
                ClearLogs();
            }
            if (GUILayout.Button("复制日志"))
            {
                CopyLogs();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawResult()
        {
            if (!showResult || lastGenerationResult == null) return;
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("生成结果", EditorStyles.boldLabel);
            
            if (lastGenerationResult.Success)
            {
                EditorGUILayout.HelpBox("UI生成成功！", MessageType.Info);
                EditorGUILayout.LabelField($"UI名称: {lastGenerationResult.Data.UIName}");
                EditorGUILayout.LabelField($"代码路径: {lastGenerationResult.Data.CodePath}");
                
                if (GUILayout.Button("在Project窗口中显示"))
                {
                    ShowInProjectWindow();
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"生成失败: {lastGenerationResult.ErrorMessage}", MessageType.Error);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("UI Generator v1.0", EditorStyles.centeredGreyMiniLabel);
        }
        
        private void SelectPrefab()
        {
            var path = EditorUtility.OpenFilePanel("选择Prefab文件", "Assets/", "prefab");
            if (!string.IsNullOrEmpty(path))
            {
                // 转换为Unity资源路径
                if (path.StartsWith(Application.dataPath))
                {
                    prefabPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
                else
                {
                    prefabPath = path;
                }
                
                EditorPrefs.SetString("UIGenerator_LastPrefabPath", prefabPath);
                LoadPrefab();
            }
        }
        
        private void LoadPrefab()
        {
            try
            {
                if (string.IsNullOrEmpty(prefabPath))
                {
                    AddLog("Prefab路径为空");
                    return;
                }
                
                selectedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (selectedPrefab == null)
                {
                    AddLog("无法加载Prefab文件");
                    return;
                }
                
                // 从文件名提取UI名称并更新显示
                var fileName = Path.GetFileNameWithoutExtension(prefabPath);
                uiName = fileName; // 直接设置为文件名，覆盖之前的UI名称
                
                AddLog($"Prefab加载成功: {prefabPath}");
                AddLog($"UI名称: {uiName}");
                
                // 强制重绘GUI以更新UI名称显示
                Repaint();
                
                if (autoGenerate)
                {
                    GenerateUIRefs();
                    GenerateUICode();
                }
            }
            catch (Exception ex)
            {
                AddLog($"加载Prefab失败: {ex.Message}");
                Debug.LogError($"[UI Generator] 加载Prefab失败: {ex.Message}");
            }
        }
        
        private void GenerateUIRefs()
        {
            try
            {
                if (selectedUIFile == null || selectedUIFile.Prefab == null)
                {
                    AddLog("错误: 请先选择一个UI文件");
                    return;
                }
                
                AddLog($"开始生成UIRefs组件: {selectedUIFile.Name}");
                
                var refsGenerator = new Generators.UIRefsGenerator(config);
                var result = refsGenerator.GenerateFromPrefab(selectedUIFile.Prefab, selectedUIFile.Name);
                
                if (result.Success)
                {
                    AddLog($"UIRefs组件生成成功: {selectedUIFile.Name}");
                    selectedUIFile.HasUIRefs = true;
                    AssetDatabase.Refresh();
                }
                else
                {
                    AddLog($"UIRefs组件生成失败: {result.ErrorMessage}");
                }
                
                Repaint();
            }
            catch (Exception ex)
            {
                AddLog($"生成UIRefs组件时发生异常: {ex.Message}");
            }
        }
        
        private void GenerateUICode()
        {
            try
            {
                if (selectedUIFile == null || selectedUIFile.Prefab == null)
                {
                    AddLog("错误: 请先选择一个UI文件");
                    return;
                }
                
                AddLog($"开始生成UI代码: {selectedUIFile.Name}");
                
                var codeGenerator = new Generators.CSharpCodeGenerator(config);
                var result = codeGenerator.GenerateFromPrefab(selectedUIFile.Prefab, selectedUIFile.Name);
                
                if (result.Success)
                {
                    AddLog($"UI代码生成成功: {selectedUIFile.Name}");
                    selectedUIFile.HasGeneratedCode = true;
                    AssetDatabase.Refresh();
                }
                else
                {
                    AddLog($"UI代码生成失败: {result.ErrorMessage}");
                }
                
                Repaint();
            }
            catch (Exception ex)
            {
                AddLog($"生成UI代码时发生异常: {ex.Message}");
            }
        }
        
        private void GenerateAll()
        {
            try
            {
                if (selectedUIFile == null || selectedUIFile.Prefab == null)
                {
                    AddLog("错误: 请先选择一个UI文件");
                    return;
                }
                
                AddLog($"开始批量生成: {selectedUIFile.Name}");
                
                // 生成UIRefs组件
                GenerateUIRefs();
                
                // 生成UI代码
                GenerateUICode();
                
                AddLog($"批量生成完成: {selectedUIFile.Name}");
            }
            catch (Exception ex)
            {
                AddLog($"批量生成时发生异常: {ex.Message}");
            }
        }
        
        private void PreviewUIStructure()
        {
            try
            {
                if (selectedUIFile == null || selectedUIFile.Prefab == null)
                {
                    AddLog("错误: 请先选择一个UI文件");
                    return;
                }
                
                AddLog($"预览UI结构: {selectedUIFile.Name}");
                // TODO: 实现UI结构预览功能
                AddLog("UI结构预览功能待实现");
            }
            catch (Exception ex)
            {
                AddLog($"预览UI结构时发生异常: {ex.Message}");
            }
        }
        
        private void RefreshReferences()
        {
            try
            {
                if (selectedUIFile == null || selectedUIFile.Prefab == null)
                {
                    AddLog("错误: 请先选择一个UI文件");
                    return;
                }
                
                AddLog($"刷新引用: {selectedUIFile.Name}");
                
                var uiRefs = selectedUIFile.Prefab.GetComponent<UIRefs>();
                if (uiRefs != null)
                {
                    uiRefs.RefreshReferences();
                    AddLog("引用刷新成功");
                }
                else
                {
                    AddLog("该UI没有UIRefs组件");
                }
            }
            catch (Exception ex)
            {
                AddLog($"刷新引用时发生异常: {ex.Message}");
            }
        }
        
        private void ClearContent()
        {
            selectedUIFile = null;
            selectedPrefab = null;
            uiName = "";
            prefabPath = "";
            showResult = false;
            lastGenerationResult = null;
            AddLog("选择已清空");
        }
        
        private void ClearLogs()
        {
            logContent = "";
            logScrollPosition = Vector2.zero;
        }
        
        private void CopyLogs()
        {
            if (!string.IsNullOrEmpty(logContent))
            {
                EditorGUIUtility.systemCopyBuffer = logContent;
                AddLog("日志已复制到剪贴板");
            }
        }
        
        private void ShowInProjectWindow()
        {
            if (lastGenerationResult?.Success == true && !string.IsNullOrEmpty(lastGenerationResult.Data.CodePath))
            {
                var assetPath = lastGenerationResult.Data.CodePath;
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
        }
        
        private void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            logContent += $"[{timestamp}] {message}\n";
            
            if (autoScrollLogs)
            {
                logScrollPosition.y = float.MaxValue;
            }
            
            Repaint();
        }
        
        /// <summary>
        /// 更新UI文件的状态信息
        /// </summary>
        private void UpdateUIStatusInfo(UIFileInfo uiFile)
        {
            if (uiFile == null || uiFile.Prefab == null)
                return;
                
            try
            {
                // 重置状态信息
                uiFile.StatusInfo = new UIStatusInfo();
                
                // 检查UIRefs组件
                UpdateUIRefsStatus(uiFile);
                
                // 检查生成代码
                UpdateGeneratedCodeStatus(uiFile);
                
                // 更新整体状态
                uiFile.StatusInfo.UpdateOverallStatus();
            }
            catch (Exception ex)
            {
                AddLog($"更新UI状态信息时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新UIRefs组件状态
        /// </summary>
        private void UpdateUIRefsStatus(UIFileInfo uiFile)
        {
            try
            {
                var uiRefs = uiFile.Prefab.GetComponent<UIRefs>();
                if (uiRefs == null)
                {
                    uiFile.StatusInfo.HasUIRefsComponent = false;
                    return;
                }
                
                uiFile.StatusInfo.HasUIRefsComponent = true;
                
                // 使用反射获取UIRefs的私有字段
                var uiClassNameField = typeof(UIRefs).GetField("uiClassName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var uiNamespaceField = typeof(UIRefs).GetField("uiNamespace", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var uiRefItemsField = typeof(UIRefs).GetField("uiRefItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (uiClassNameField != null)
                {
                    uiFile.StatusInfo.UIRefsClassName = uiClassNameField.GetValue(uiRefs)?.ToString() ?? "";
                }
                
                if (uiNamespaceField != null)
                {
                    uiFile.StatusInfo.UIRefsNamespace = uiNamespaceField.GetValue(uiRefs)?.ToString() ?? "";
                }
                
                if (uiRefItemsField != null)
                {
                    var uiRefItems = uiRefItemsField.GetValue(uiRefs) as System.Collections.Generic.List<UIRefItem>;
                    uiFile.StatusInfo.UIRefsReferenceCount = uiRefItems?.Count ?? 0;
                }
                
                // 验证UIRefs数据
                ValidateUIRefsData(uiFile, uiRefs);
            }
            catch (Exception ex)
            {
                uiFile.StatusInfo.UIRefsErrors.Add($"检查UIRefs组件时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 验证UIRefs数据
        /// </summary>
        private void ValidateUIRefsData(UIFileInfo uiFile, UIRefs uiRefs)
        {
            try
            {
                uiFile.StatusInfo.UIRefsErrors.Clear();
                
                // 检查UI类名
                if (string.IsNullOrEmpty(uiFile.StatusInfo.UIRefsClassName))
                {
                    uiFile.StatusInfo.UIRefsErrors.Add("UI类名未设置");
                }
                
                // 检查命名空间
                if (string.IsNullOrEmpty(uiFile.StatusInfo.UIRefsNamespace))
                {
                    uiFile.StatusInfo.UIRefsErrors.Add("UI命名空间未设置");
                }
                
                // 检查引用数量
                if (uiFile.StatusInfo.UIRefsReferenceCount == 0)
                {
                    uiFile.StatusInfo.UIRefsErrors.Add("没有UI引用");
                }
                
                // 检查UI类是否存在
                if (!string.IsNullOrEmpty(uiFile.StatusInfo.UIRefsClassName) && !string.IsNullOrEmpty(uiFile.StatusInfo.UIRefsNamespace))
                {
                    string fullTypeName = $"{uiFile.StatusInfo.UIRefsNamespace}.{uiFile.StatusInfo.UIRefsClassName}";
                    var uiType = System.Type.GetType(fullTypeName);
                    if (uiType == null)
                    {
                        uiFile.StatusInfo.UIRefsErrors.Add($"UI类不存在: {fullTypeName}");
                    }
                }
                
                // 判断UIRefs数据是否有效
                uiFile.StatusInfo.UIRefsDataValid = uiFile.StatusInfo.UIRefsErrors.Count == 0;
            }
            catch (Exception ex)
            {
                uiFile.StatusInfo.UIRefsErrors.Add($"验证UIRefs数据时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新生成代码状态
        /// </summary>
        private void UpdateGeneratedCodeStatus(UIFileInfo uiFile)
        {
            try
            {
                // 检查是否使用partial class模式
                if (config.CodeSettings.UsePartialClass)
                {
                    UpdatePartialClassCodeStatus(uiFile);
                }
                else
                {
                    UpdateSingleClassCodeStatus(uiFile);
                }
            }
            catch (Exception ex)
            {
                uiFile.StatusInfo.CodeErrors.Add($"检查生成代码时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新单个类文件状态
        /// </summary>
        private void UpdateSingleClassCodeStatus(UIFileInfo uiFile)
        {
            string expectedCodePath = System.IO.Path.Combine(UIGeneratorConfig.CODE_OUTPUT_PATH, $"{uiFile.Name}UI.cs");
            uiFile.StatusInfo.GeneratedCodePath = expectedCodePath;
            
            if (System.IO.File.Exists(expectedCodePath))
            {
                uiFile.StatusInfo.HasGeneratedCode = true;
                uiFile.StatusInfo.CodeLastModified = System.IO.File.GetLastWriteTime(expectedCodePath);
                
                // 验证代码完整性
                ValidateGeneratedCode(uiFile, expectedCodePath);
            }
            else
            {
                uiFile.StatusInfo.HasGeneratedCode = false;
                uiFile.StatusInfo.CodeIsComplete = false;
            }
        }
        
        /// <summary>
        /// 更新partial class文件状态
        /// </summary>
        private void UpdatePartialClassCodeStatus(UIFileInfo uiFile)
        {
            string className = $"{uiFile.Name}View";
            string designerPath = System.IO.Path.Combine(UIGeneratorConfig.CODE_OUTPUT_PATH, $"{className}{config.CodeSettings.DesignerFileSuffix}");
            string logicPath = System.IO.Path.Combine(UIGeneratorConfig.CODE_OUTPUT_PATH, $"{className}{config.CodeSettings.LogicFileSuffix}");
            
            bool hasDesignerFile = System.IO.File.Exists(designerPath);
            bool hasLogicFile = System.IO.File.Exists(logicPath);
            
            if (hasDesignerFile || hasLogicFile)
            {
                uiFile.StatusInfo.HasGeneratedCode = true;
                
                // 使用最新的修改时间
                System.DateTime designerTime = hasDesignerFile ? System.IO.File.GetLastWriteTime(designerPath) : System.DateTime.MinValue;
                System.DateTime logicTime = hasLogicFile ? System.IO.File.GetLastWriteTime(logicPath) : System.DateTime.MinValue;
                uiFile.StatusInfo.CodeLastModified = designerTime > logicTime ? designerTime : logicTime;
                
                // 设置代码路径（优先显示设计器文件）
                uiFile.StatusInfo.GeneratedCodePath = hasDesignerFile ? designerPath : logicPath;
                
                // 验证代码完整性
                if (hasDesignerFile)
                {
                    ValidateGeneratedCode(uiFile, designerPath);
                }
                if (hasLogicFile)
                {
                    ValidateGeneratedCode(uiFile, logicPath);
                }
                
                // 检查是否两个文件都存在
                if (!hasDesignerFile)
                {
                    uiFile.StatusInfo.CodeErrors.Add("缺少设计器文件(.designer.cs)");
                }
                if (!hasLogicFile)
                {
                    uiFile.StatusInfo.CodeErrors.Add("缺少逻辑文件(.cs)");
                }
            }
            else
            {
                uiFile.StatusInfo.HasGeneratedCode = false;
                uiFile.StatusInfo.CodeIsComplete = false;
                uiFile.StatusInfo.GeneratedCodePath = designerPath; // 显示期望的路径
            }
        }
        
        /// <summary>
        /// 验证生成代码的完整性
        /// </summary>
        private void ValidateGeneratedCode(UIFileInfo uiFile, string codePath)
        {
            try
            {
                string codeContent = System.IO.File.ReadAllText(codePath);
                string fileName = System.IO.Path.GetFileName(codePath);
                
                // 检查基本的代码结构
                if (!codeContent.Contains("class"))
                {
                    uiFile.StatusInfo.CodeErrors.Add($"{fileName}: 代码中缺少类定义");
                }
                
                if (!codeContent.Contains("namespace"))
                {
                    uiFile.StatusInfo.CodeErrors.Add($"{fileName}: 代码中缺少命名空间");
                }
                
                // 根据文件类型进行不同的验证
                if (fileName.EndsWith(".designer.cs"))
                {
                    ValidateDesignerFile(codeContent, fileName, uiFile);
                }
                else if (fileName.EndsWith(".cs") && !fileName.EndsWith(".designer.cs"))
                {
                    ValidateLogicFile(codeContent, fileName, uiFile);
                }
                else
                {
                    // 单个类文件的验证
                    if (!codeContent.Contains("Initialize"))
                    {
                        uiFile.StatusInfo.CodeErrors.Add($"{fileName}: 代码中缺少Initialize方法");
                    }
                }
                
                // 检查是否有编译错误（简单检查）
                if (codeContent.Contains("error CS") || codeContent.Contains("Error CS"))
                {
                    uiFile.StatusInfo.CodeErrors.Add($"{fileName}: 代码中包含编译错误");
                }
                
                // 判断代码是否完整
                uiFile.StatusInfo.CodeIsComplete = uiFile.StatusInfo.CodeErrors.Count == 0;
            }
            catch (Exception ex)
            {
                uiFile.StatusInfo.CodeErrors.Add($"验证生成代码时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 验证设计器文件
        /// </summary>
        private void ValidateDesignerFile(string codeContent, string fileName, UIFileInfo uiFile)
        {
            if (!codeContent.Contains("partial class"))
            {
                uiFile.StatusInfo.CodeErrors.Add($"{fileName}: 缺少partial class定义");
            }
            
            if (!codeContent.Contains("Initialize"))
            {
                uiFile.StatusInfo.CodeErrors.Add($"{fileName}: 缺少Initialize方法");
            }
            
            if (!codeContent.Contains("InitializeUIElements"))
            {
                uiFile.StatusInfo.CodeErrors.Add($"{fileName}: 缺少InitializeUIElements方法");
            }
            
            if (!codeContent.Contains("Show") || !codeContent.Contains("Hide"))
            {
                uiFile.StatusInfo.CodeErrors.Add($"{fileName}: 缺少Show或Hide方法");
            }
        }
        
        /// <summary>
        /// 验证逻辑文件
        /// </summary>
        private void ValidateLogicFile(string codeContent, string fileName, UIFileInfo uiFile)
        {
            if (!codeContent.Contains("partial class"))
            {
                uiFile.StatusInfo.CodeErrors.Add($"{fileName}: 缺少partial class定义");
            }
            
            if (!codeContent.Contains("OnInitialize") || !codeContent.Contains("OnShow") || !codeContent.Contains("OnHide"))
            {
                uiFile.StatusInfo.CodeErrors.Add($"{fileName}: 缺少虚方法定义");
            }
        }
        
        /// <summary>
        /// 刷新所有UI文件的状态信息
        /// </summary>
        private void RefreshAllUIStatus()
        {
            try
            {
                foreach (var uiFile in uiFiles)
                {
                    RefreshUIStatusRecursive(uiFile);
                }
                AddLog("UI状态信息刷新完成");
            }
            catch (Exception ex)
            {
                AddLog($"刷新UI状态信息时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 递归刷新UI状态信息
        /// </summary>
        private void RefreshUIStatusRecursive(UIFileInfo uiFile)
        {
            if (uiFile == null)
                return;
                
            if (!uiFile.IsFolder && uiFile.Prefab != null)
            {
                UpdateUIStatusInfo(uiFile);
            }
            
            foreach (var child in uiFile.Children)
            {
                RefreshUIStatusRecursive(child);
            }
        }
        
        /// <summary>
        /// 获取状态颜色
        /// </summary>
        private Color GetStatusColor(UIOverallStatus status)
        {
            switch (status)
            {
                case UIOverallStatus.Complete:
                    return Color.green;
                case UIOverallStatus.NotGenerated:
                    return Color.gray;
                case UIOverallStatus.UIRefsInvalid:
                case UIOverallStatus.CodeIncomplete:
                    return Color.red;
                case UIOverallStatus.UIRefsOnly:
                case UIOverallStatus.CodeOnly:
                case UIOverallStatus.Partial:
                    return Color.yellow;
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// 获取状态图标
        /// </summary>
        private string GetStatusIcon(UIOverallStatus status)
        {
            switch (status)
            {
                case UIOverallStatus.Complete:
                    return "✓";
                case UIOverallStatus.NotGenerated:
                    return "○";
                case UIOverallStatus.UIRefsInvalid:
                case UIOverallStatus.CodeIncomplete:
                    return "✗";
                case UIOverallStatus.UIRefsOnly:
                case UIOverallStatus.CodeOnly:
                case UIOverallStatus.Partial:
                    return "△";
                default:
                    return "?";
            }
        }
        
        /// <summary>
        /// 绘制状态指示器
        /// </summary>
        private void DrawStatusIndicator(UIFileInfo file)
        {
            if (file == null || file.IsFolder)
                return;
                
            // 更新状态信息（如果还没有更新过）
            if (file.StatusInfo.OverallStatus == UIOverallStatus.Unknown)
            {
                UpdateUIStatusInfo(file);
            }
            
            // 获取状态颜色和图标
            Color statusColor = GetStatusColor(file.StatusInfo.OverallStatus);
            string statusIcon = GetStatusIcon(file.StatusInfo.OverallStatus);
            
            // 保存原始颜色
            Color originalColor = GUI.color;
            
            // 设置状态颜色
            GUI.color = statusColor;
            
            // 绘制状态图标
            EditorGUILayout.LabelField(statusIcon, GUILayout.Width(20));
            
            // 恢复原始颜色
            GUI.color = originalColor;
            
            // 添加工具提示
            if (Event.current.type == EventType.Repaint)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                GUI.Label(rect, new GUIContent("", file.StatusInfo.StatusMessage));
            }
        }

        /// <summary>
        /// 将UI中所有TextMeshPro组件转换为Text组件，保留原文本内容
        /// </summary>
        private void ConvertTextMeshProToText()
        {
            if (selectedUIFile == null || selectedUIFile.Prefab == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个UI预制体", "确定");
                return;
            }

            // 确认转换操作
            if (!EditorUtility.DisplayDialog("确认转换", 
                $"确定要将 {selectedUIFile.Name} 中的所有TextMeshPro组件转换为Text组件吗？\n\n此操作将：\n• 保留所有文本内容\n• 保留字体大小和颜色\n• 保留对齐方式\n• 无法撤销", 
                "确定转换", "取消"))
            {
                return;
            }

            try
            {
                // 开始记录撤销操作
                Undo.RegisterCompleteObjectUndo(selectedUIFile.Prefab, "Convert TextMeshPro to Text");
                
                int convertedCount = 0;
                var textMeshProComponents = GetTextMeshProComponents(selectedUIFile.Prefab);
                
                foreach (var tmpComponent in textMeshProComponents)
                {
                    if (ConvertSingleTextMeshProToText(tmpComponent))
                    {
                        convertedCount++;
                    }
                }

                // 标记预制体为已修改
                EditorUtility.SetDirty(selectedUIFile.Prefab);
                
                // 显示结果
                EditorUtility.DisplayDialog("转换完成", 
                    $"成功转换了 {convertedCount} 个TextMeshPro组件为Text组件", "确定");
                
                AddLog($"TextMeshPro转Text转换完成: {convertedCount} 个组件已转换");
                
                // 刷新UI状态
                RefreshUIStatusRecursive(selectedUIFile);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("转换失败", $"转换过程中发生错误：\n{ex.Message}", "确定");
                AddLog($"TextMeshPro转Text转换失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取GameObject中的所有TextMeshPro组件（使用反射）
        /// </summary>
        /// <param name="prefab">要搜索的预制体</param>
        /// <returns>TextMeshPro组件列表</returns>
        private List<Component> GetTextMeshProComponents(GameObject prefab)
        {
            var components = new List<Component>();
            
            try
            {
                // 尝试获取TextMeshProUGUI类型
                Type textMeshProType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                if (textMeshProType == null)
                {
                    // 如果找不到TextMeshPro类型，返回空列表
                    AddLog("未找到TextMeshPro组件，可能未安装TextMeshPro包");
                    return components;
                }
                
                // 使用反射获取所有TextMeshPro组件
                var allComponents = prefab.GetComponentsInChildren(textMeshProType, true);
                foreach (var component in allComponents)
                {
                    components.Add(component);
                }
                
                AddLog($"找到 {components.Count} 个TextMeshPro组件");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"获取TextMeshPro组件失败: {ex.Message}");
            }
            
            return components;
        }

        /// <summary>
        /// 转换单个TextMeshPro组件为Text组件（使用反射）
        /// </summary>
        /// <param name="tmpComponent">要转换的TextMeshPro组件</param>
        /// <returns>是否转换成功</returns>
        private bool ConvertSingleTextMeshProToText(Component tmpComponent)
        {
            if (tmpComponent == null) return false;

            try
            {
                // 使用反射获取TextMeshPro的属性
                string text = GetPropertyValue<string>(tmpComponent, "text");
                float fontSize = GetPropertyValue<float>(tmpComponent, "fontSize");
                Color color = GetPropertyValue<Color>(tmpComponent, "color");
                object alignment = GetPropertyValue<object>(tmpComponent, "alignment");
                bool richText = GetPropertyValue<bool>(tmpComponent, "richText");
                bool raycastTarget = GetPropertyValue<bool>(tmpComponent, "raycastTarget");
                
                // 获取GameObject
                GameObject gameObject = tmpComponent.gameObject;
                
                // 记录撤销操作
                Undo.RegisterCompleteObjectUndo(gameObject, "Convert TextMeshPro to Text");
                
                // 移除TextMeshPro组件
                Undo.DestroyObjectImmediate(tmpComponent);
                
                // 添加Text组件
                UnityEngine.UI.Text textComponent = Undo.AddComponent<UnityEngine.UI.Text>(gameObject);
                
                // 设置Text组件属性
                textComponent.text = text ?? "";
                textComponent.fontSize = Mathf.RoundToInt(fontSize);
                textComponent.color = color;
                textComponent.raycastTarget = raycastTarget;
                textComponent.supportRichText = richText;
                
                // 转换对齐方式
                textComponent.alignment = ConvertTextAlignment(alignment);
                
                // 设置默认字体
                textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"转换TextMeshPro组件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用反射获取属性值
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="obj">对象</param>
        /// <param name="propertyName">属性名</param>
        /// <returns>属性值</returns>
        private T GetPropertyValue<T>(object obj, string propertyName)
        {
            try
            {
                PropertyInfo property = obj.GetType().GetProperty(propertyName);
                if (property != null && property.CanRead)
                {
                    object value = property.GetValue(obj);
                    if (value is T)
                    {
                        return (T)value;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"获取属性 {propertyName} 失败: {ex.Message}");
            }
            
            return default(T);
        }

        /// <summary>
        /// 将TextMeshPro的对齐方式转换为Text的对齐方式（使用反射）
        /// </summary>
        /// <param name="tmpAlignment">TextMeshPro对齐方式</param>
        /// <returns>Text对齐方式</returns>
        private TextAnchor ConvertTextAlignment(object tmpAlignment)
        {
            if (tmpAlignment == null) return TextAnchor.MiddleCenter;
            
            try
            {
                // 获取TextMeshPro对齐枚举的字符串值
                string alignmentString = tmpAlignment.ToString();
                
                // 根据字符串值转换为TextAnchor
                switch (alignmentString)
                {
                    case "TopLeft":
                        return TextAnchor.UpperLeft;
                    case "Top":
                        return TextAnchor.UpperCenter;
                    case "TopRight":
                        return TextAnchor.UpperRight;
                    case "Left":
                        return TextAnchor.MiddleLeft;
                    case "Center":
                        return TextAnchor.MiddleCenter;
                    case "Right":
                        return TextAnchor.MiddleRight;
                    case "BottomLeft":
                        return TextAnchor.LowerLeft;
                    case "Bottom":
                        return TextAnchor.LowerCenter;
                    case "BottomRight":
                        return TextAnchor.LowerRight;
                    default:
                        return TextAnchor.MiddleCenter;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"转换对齐方式失败: {ex.Message}");
                return TextAnchor.MiddleCenter;
            }
        }

        private void OnDestroy()
        {
            // 保存窗口状态
            if (selectedUIFile != null)
            {
                EditorPrefs.SetString("UIGenerator_LastSelectedUI", selectedUIFile.RelativePath);
            }
        }
    }
}
