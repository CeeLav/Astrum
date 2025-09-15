using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astrum.CommonBase;

namespace Astrum.Editor.ASLogger
{
    /// <summary>
    /// ASLogger管理器编辑器窗口
    /// </summary>
    public class ASLoggerManagerWindow : EditorWindow
    {
        private Astrum.Editor.ASLogger.LogManagerConfigEditor _config;
        private LogCategory _selectedCategory;
        private Vector2 _categoryScrollPosition;
        private Vector2 _logScrollPosition;
        private string _searchFilter = "";
        private LogLevel _levelFilter = LogLevel.Debug;
        private bool _showOnlyEnabled = false;
        private bool _showStatistics = true;
        
        // 窗口状态
        private bool _isInitialized = false;
        private float _splitterPosition = 300f;
        private bool _isDraggingSplitter = false;
        
        // 统计信息
        private int _totalCategories = 0;
        private int _enabledCategories = 0;
        private int _totalLogs = 0;
        private int _enabledLogs = 0;
        
        [MenuItem("Tools/ASLogger Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<ASLoggerManagerWindow>("ASLogger Manager");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            // 检查ASLogger是否已经初始化，如果已初始化则直接使用其配置
            if (Astrum.CommonBase.ASLogger.IsConfigInitialized && Astrum.CommonBase.ASLogger.CurrentConfig != null)
            {
                // 将运行时配置转换为编辑器配置
                var runtimeConfig = Astrum.CommonBase.ASLogger.CurrentConfig;
                _config = CreateInstance<LogManagerConfigEditor>();
                _config.UpdateFromRuntimeConfig(runtimeConfig);
                _isInitialized = true;
                UpdateStatistics();
                return;
            }
            
            Initialize();
        }
        
        private void OnDisable()
        {
            SaveConfig();
        }
        
        private void Initialize()
        {
            if (_isInitialized) return;
            
            LoadConfig();
            InitializeLogFilter();
            UpdateStatistics();
            
            _isInitialized = true;
            
            Debug.Log("[ASLoggerManager] 窗口初始化完成");
        }
        
        private void LoadConfig()
        {
            // 如果ASLogger已经有配置，直接使用
            if (Astrum.CommonBase.ASLogger.IsConfigInitialized && Astrum.CommonBase.ASLogger.CurrentConfig != null)
            {
                // 将运行时配置转换为编辑器配置
                var runtimeConfig = Astrum.CommonBase.ASLogger.CurrentConfig;
                _config = CreateInstance<LogManagerConfigEditor>();
                _config.UpdateFromRuntimeConfig(runtimeConfig);
                Debug.Log("[ASLoggerManager] 使用ASLogger现有配置");
                return;
            }
            
            // 首先尝试从Resources文件夹加载
            _config = Resources.Load<LogManagerConfigEditor>("LogManagerConfig");
            
            if (_config == null)
            {
                // 尝试从项目根目录加载
                var configs = Resources.FindObjectsOfTypeAll<LogManagerConfigEditor>();
                if (configs.Length > 0)
                {
                    _config = configs[0];
                }
                else
                {
                    // 创建新配置并保存到Resources文件夹
                    _config = CreateInstance<LogManagerConfigEditor>();
                    _config.name = "LogManagerConfig";
                    
                    // 确保Resources文件夹存在
                    string resourcesPath = "Assets/Resources";
                    if (!AssetDatabase.IsValidFolder(resourcesPath))
                    {
                        AssetDatabase.CreateFolder("Assets", "Resources");
                    }
                    
                    // 保存到Resources文件夹
                    string assetPath = "Assets/Resources/LogManagerConfig.asset";
                    AssetDatabase.CreateAsset(_config, assetPath);
                    AssetDatabase.SaveAssets();
                    
                    Debug.Log($"[ASLoggerManager] 创建新配置文件: {assetPath}");
                }
            }
            
            if (_config != null)
            {
                _config.Initialize();
                
                // 通知ASLogger加载配置
                Astrum.CommonBase.ASLogger.SetConfig(_config.ToRuntimeConfig());
                Debug.Log("[ASLoggerManager] 配置已同步到ASLogger");
            }
        }
        
        private void InitializeLogFilter()
        {
            if (_config != null)
            {
                LogFilter.SetConfig(_config.ToRuntimeConfig());
            }
        }
        
        private void SaveConfig()
        {
            if (_config != null)
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
                
                // 确保配置在Resources文件夹中
                string currentPath = AssetDatabase.GetAssetPath(_config);
                if (!currentPath.Contains("Resources"))
                {
                    // 移动到Resources文件夹
                    string resourcesPath = "Assets/Resources";
                    if (!AssetDatabase.IsValidFolder(resourcesPath))
                    {
                        AssetDatabase.CreateFolder("Assets", "Resources");
                    }
                    
                    string newPath = "Assets/Resources/LogManagerConfig.asset";
                    AssetDatabase.MoveAsset(currentPath, newPath);
                    AssetDatabase.SaveAssets();
                    
                    Debug.Log($"[ASLoggerManager] 配置文件已移动到: {newPath}");
                }
            }
        }
        
        private void OnGUI()
        {
            if (!_isInitialized)
            {
                Initialize();
                return;
            }
            
            if (_config == null)
            {
                EditorGUILayout.HelpBox("无法加载日志管理器配置", MessageType.Error);
                return;
            }
            
            try
            {
                DrawHeader();
                DrawMainLayout();
                DrawFooter();
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"绘制GUI时发生错误: {ex.Message}", MessageType.Error);
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ASLogger Manager", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                RefreshData();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
        }
        
        private void DrawMainLayout()
        {
            EditorGUILayout.BeginHorizontal();
            
            // 左侧分类树
            DrawCategoryTree();
            
            // 分隔线
            DrawSplitter();
            
            // 右侧日志列表
            DrawLogList();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawCategoryTree()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(_splitterPosition));
            
            // 标题栏
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("分类树", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("全部启用", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                EnableAll();
            }
            if (GUILayout.Button("全部禁用", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                DisableAll();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            _categoryScrollPosition = EditorGUILayout.BeginScrollView(_categoryScrollPosition);
            
            if (_config.categories.Count > 0)
            {
                foreach (var category in _config.categories)
                {
                    DrawCategoryNode(category, 0);
                }
            }
            else
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.LabelField("没有找到分类", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space(20);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCategoryNode(LogCategory category, int depth)
        {
            // 计算缩进
            var indentWidth = depth * 15f;
            
            EditorGUILayout.BeginVertical();
            
            // 主分类行
            EditorGUILayout.BeginHorizontal();
            
            // 缩进
            GUILayout.Space(indentWidth);
            
            // 复选框区域（更大的点击区域）
            var checkboxRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
            bool newEnabled = EditorGUI.Toggle(checkboxRect, category.enabled);
            if (newEnabled != category.enabled)
            {
                OnCategoryToggle(category, newEnabled);
            }
            
            // 分类名称区域（更大的点击区域）
            var nameRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            
            // 检查是否被选中
            bool isSelected = _selectedCategory == category;
            
            // 设置背景颜色
            Color backgroundColor;
            if (isSelected)
            {
                backgroundColor = new Color(0.2f, 0.5f, 1f, 0.3f); // 选中时的蓝色背景
            }
            else if (category.enabled)
            {
                backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f); // Unity Editor默认背景色
            }
            else
            {
                backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f); // 禁用时的深色背景
            }
            
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;
            
            // 绘制背景
            EditorGUI.DrawRect(nameRect, backgroundColor);
            
            // 绘制选中指示器
            if (isSelected)
            {
                var indicatorRect = new Rect(nameRect.x, nameRect.y, 3, nameRect.height);
                EditorGUI.DrawRect(indicatorRect, new Color(0.2f, 0.5f, 1f, 1f));
            }
            
            // 绘制分类名称
            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 0, 0),
                normal = { textColor = category.enabled ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f) },
                fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal
            };
            
            if (GUI.Button(nameRect, category.name, nameStyle))
            {
                SelectCategory(category);
            }
            
            GUI.backgroundColor = originalColor;
            
            // 统计信息
            if (_showStatistics)
            {
                var statsRect = GUILayoutUtility.GetRect(60, 20, GUILayout.Width(60));
                var statsStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f, 1f) }
                };
                EditorGUI.LabelField(statsRect, $"({category.enabledLogCount}/{category.totalLogCount})", statsStyle);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 绘制子分类
            if (category.children.Count > 0)
            {
                EditorGUILayout.BeginVertical();
                foreach (var child in category.children)
                {
                    DrawCategoryNode(child, depth + 1);
                }
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
            
            // 添加分隔线（可选）
            if (depth == 0)
            {
                EditorGUILayout.Space(2);
                var lineRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
                EditorGUILayout.Space(2);
            }
        }
        
        private void DrawSplitter()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(5));
            EditorGUILayout.Space();
            
            var rect = GUILayoutUtility.GetRect(5, 20, GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, Color.gray);
            
            // 处理拖拽
            var controlID = GUIUtility.GetControlID(FocusType.Passive);
            var eventType = Event.current.type;
            
            if (eventType == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _isDraggingSplitter = true;
                GUIUtility.hotControl = controlID;
                Event.current.Use();
            }
            else if (eventType == EventType.MouseUp && _isDraggingSplitter)
            {
                _isDraggingSplitter = false;
                GUIUtility.hotControl = 0;
                Event.current.Use();
            }
            else if (eventType == EventType.MouseDrag && _isDraggingSplitter)
            {
                _splitterPosition = Mathf.Clamp(Event.current.mousePosition.x, 200f, position.width - 200f);
                Repaint();
                Event.current.Use();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawLogList()
        {
            EditorGUILayout.BeginVertical("box");
            
            if (_selectedCategory != null)
            {
                DrawLogListHeader();
                DrawLogListContent();
            }
            else
            {
                EditorGUILayout.LabelField("请选择一个分类", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawLogListHeader()
        {
            // 标题栏
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField($"日志列表 - {_selectedCategory.name}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 过滤控制栏
            EditorGUILayout.BeginHorizontal();
            
            // 搜索框
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            
            // 级别过滤
            EditorGUILayout.LabelField("级别:", GUILayout.Width(40));
            _levelFilter = (LogLevel)EditorGUILayout.EnumPopup(_levelFilter, GUILayout.Width(80));
            
            // 只显示启用的日志
            _showOnlyEnabled = EditorGUILayout.Toggle("仅启用", _showOnlyEnabled, GUILayout.Width(60));
            
            // 统计信息
            if (_showStatistics)
            {
                GUILayout.FlexibleSpace();
                var filteredLogs = GetFilteredLogs();
                EditorGUILayout.LabelField($"显示: {filteredLogs.Count} 条", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
        }
        
        private void DrawLogListContent()
        {
            var filteredLogs = GetFilteredLogs();
            
            _logScrollPosition = EditorGUILayout.BeginScrollView(_logScrollPosition);
            
            if (filteredLogs.Count > 0)
            {
                foreach (var log in filteredLogs)
                {
                    DrawLogEntry(log);
                }
            }
            else
            {
                EditorGUILayout.LabelField("没有找到匹配的日志", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawLogEntry(LogEntry log)
        {
            // 计算行高
            var lineHeight = 22f;
            
            EditorGUILayout.BeginVertical();
            
            // 主行
            EditorGUILayout.BeginHorizontal();
            
            // 日志级别标签（固定宽度）
            var levelRect = GUILayoutUtility.GetRect(60, lineHeight, GUILayout.Width(60));
            var levelColor = GetLogLevelColor(log.level);
            var originalColor = GUI.color;
            GUI.color = levelColor;
            
            var levelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontStyle = FontStyle.Bold
            };
            
            EditorGUI.DrawRect(levelRect, levelColor);
            EditorGUI.LabelField(levelRect, $"[{log.level}]", levelStyle);
            GUI.color = originalColor;
            
            // 日志消息（可点击区域）
            var messageRect = GUILayoutUtility.GetRect(0, lineHeight, GUILayout.ExpandWidth(true));
            
            // 设置背景颜色
            var backgroundColor = log.enabled ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.15f, 0.15f, 0.15f, 1f);
            GUI.backgroundColor = backgroundColor;
            
            // 绘制背景
            EditorGUI.DrawRect(messageRect, backgroundColor);
            
            // 绘制消息
            var messageStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 0, 0),
                normal = { textColor = log.enabled ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f) },
                wordWrap = false
            };
            
            EditorGUI.LabelField(messageRect, log.message, messageStyle);
            GUI.backgroundColor = Color.white;
            
            // 启用/禁用复选框（更大的点击区域）
            var checkboxRect = GUILayoutUtility.GetRect(25, lineHeight, GUILayout.Width(25));
            bool newEnabled = EditorGUI.Toggle(checkboxRect, log.enabled);
            if (newEnabled != log.enabled)
            {
                OnLogToggle(log, newEnabled);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 详细信息行
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(65); // 对齐到消息开始位置
            
            var detailsStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f, 1f) }
            };
            
            EditorGUILayout.LabelField($"文件: {log.GetDisplayName()}", detailsStyle, GUILayout.Width(120));
            EditorGUILayout.LabelField($"方法: {log.methodName}", detailsStyle, GUILayout.Width(150));
            
            // 状态指示器
            var statusText = log.enabled ? "启用" : "禁用";
            var statusColor = log.enabled ? Color.green : Color.red;
            var statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = statusColor },
                fontStyle = FontStyle.Bold
            };
            
            EditorGUILayout.LabelField(statusText, statusStyle);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            // 添加分隔线
            EditorGUILayout.Space(1);
            var lineRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            EditorGUILayout.Space(1);
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.Space(10);
            
            // 统计信息栏
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (_showStatistics)
            {
                EditorGUILayout.LabelField($"总分类: {_totalCategories} | 启用: {_enabledCategories} | 总日志: {_totalLogs} | 启用: {_enabledLogs}", EditorStyles.miniLabel);
            }
            
            GUILayout.FlexibleSpace();
            
            // 操作按钮
            if (GUILayout.Button("刷新扫描", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                RefreshData();
            }
            
            if (GUILayout.Button("清空日志", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ClearLogs();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
        }
        
        private List<LogEntry> GetFilteredLogs()
        {
            if (_selectedCategory == null) return new List<LogEntry>();
            
            var logs = _selectedCategory.GetAllLogs();
            
            return logs.Where(log =>
            {
                // 搜索过滤
                if (!string.IsNullOrEmpty(_searchFilter) && 
                    !log.message.ToLower().Contains(_searchFilter.ToLower()) &&
                    !log.methodName.ToLower().Contains(_searchFilter.ToLower()))
                {
                    return false;
                }
                
                // 级别过滤
                if (log.level < _levelFilter)
                {
                    return false;
                }
                
                // 启用状态过滤
                if (_showOnlyEnabled && !log.enabled)
                {
                    return false;
                }
                
                return true;
            }).ToList();
        }
        
        private Color GetLogLevelColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return Color.gray;
                case LogLevel.Info:
                    return Color.white;
                case LogLevel.Warning:
                    return Color.yellow;
                case LogLevel.Error:
                    return Color.red;
                case LogLevel.Fatal:
                    return Color.magenta;
                default:
                    return Color.white;
            }
        }
        
        private void OnCategoryToggle(LogCategory category, bool enabled)
        {
            category.SetEnabled(enabled);
            
            // 通知ASLogger更新配置
            Astrum.CommonBase.ASLogger.SetCategoryEnabled(category.fullPath, enabled);
            
            UpdateStatistics();
            SaveConfig();
        }
        
        private void OnLogToggle(LogEntry log, bool enabled)
        {
            log.enabled = enabled;
            
            // 通知ASLogger更新配置
            Astrum.CommonBase.ASLogger.SetLogEnabled(log.id, enabled);
            
            // 注意：分类和日志的启用状态是隔离的，不自动修改分类状态
            
            UpdateStatistics();
            SaveConfig();
        }
        
        private void SelectCategory(LogCategory category)
        {
            _selectedCategory = category;
        }
        
        private void EnableAll()
        {
            foreach (var category in _config.categories)
            {
                category.SetEnabled(true);
                // 通知ASLogger更新分类状态
                Astrum.CommonBase.ASLogger.SetCategoryEnabled(category.fullPath, true);
            }
            UpdateStatistics();
            SaveConfig();
        }
        
        private void DisableAll()
        {
            foreach (var category in _config.categories)
            {
                category.SetEnabled(false);
                // 通知ASLogger更新分类状态
                Astrum.CommonBase.ASLogger.SetCategoryEnabled(category.fullPath, false);
            }
            UpdateStatistics();
            SaveConfig();
        }
        
        private void RefreshData()
        {
            try
            {
                EditorUtility.DisplayProgressBar("扫描日志", "正在扫描项目中的日志...", 0f);
                
                // 清空现有数据
                _config.Clear();
                
                // 扫描项目中的实际日志
                var projectLogs = LogScanner.ScanProject(_config.ToRuntimeConfig());
                
                // 添加扫描到的日志到配置
                foreach (var log in projectLogs)
                {
                    _config.AddLogEntry(log);
                    
                    // 确保分类存在
                    var category = _config.GetOrCreateCategory(log.category);
                    category.AddLog(log);
                }
                
                // 如果没有扫描到日志，添加一些示例数据用于演示
                if (projectLogs.Count == 0)
                {
                    Debug.LogWarning("未扫描到任何日志，添加示例数据用于演示");
                    var sampleLogs = LogScanner.CreateSampleLogEntries();
                    foreach (var log in sampleLogs)
                    {
                        _config.AddLogEntry(log);
                        var category = _config.GetOrCreateCategory(log.category);
                        category.AddLog(log);
                    }
                }
                
                // 更新统计信息
                UpdateStatistics();
                
                // 重新构建字典
                _config.RebuildDictionaries();
                
                // 通知ASLogger更新配置
                Astrum.CommonBase.ASLogger.SetConfig(_config.ToRuntimeConfig());
                
                // 保存配置
                SaveConfig();
                
                EditorUtility.ClearProgressBar();
                
                Debug.Log($"日志扫描完成，找到 {projectLogs.Count} 个实际日志条目");
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("扫描失败", $"扫描日志时发生错误: {ex.Message}", "确定");
                Debug.LogError($"扫描日志失败: {ex.Message}");
            }
        }
        
        private void ClearLogs()
        {
            if (EditorUtility.DisplayDialog("清空日志", "确定要清空所有日志吗？", "确定", "取消"))
            {
                _config.Clear();
                _selectedCategory = null;
                UpdateStatistics();
                SaveConfig();
            }
        }
        
        private void UpdateStatistics()
        {
            _config.UpdateStatistics();
            _totalCategories = _config.totalCategories;
            _enabledCategories = _config.enabledCategories;
            _totalLogs = _config.totalLogs;
            _enabledLogs = _config.enabledLogs;
        }
    }

    /// <summary>
    /// Unity编辑器版本的LogManagerConfig
    /// 继承自ScriptableObject，用于Unity编辑器
    /// </summary>
    [CreateAssetMenu(fileName = "LogManagerConfig", menuName = "ASLogger/Log Manager Config")]
    public class LogManagerConfigEditor : ScriptableObject
    {
        // 分类配置
        public System.Collections.Generic.List<LogCategory> categories = new System.Collections.Generic.List<LogCategory>();
        
        // 日志条目
        public System.Collections.Generic.List<LogEntry> logEntries = new System.Collections.Generic.List<LogEntry>();
        
        // 扫描设置
        public bool autoScanOnStart = true;
        public bool saveOnChange = true;
        public System.Collections.Generic.List<string> scanPaths = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> excludePaths = new System.Collections.Generic.List<string>();
        
        // 过滤设置
        public LogLevel globalMinLevel = LogLevel.Debug;
        public bool enableCategoryFilter = true;
        public bool enableLogFilter = true;
        
        // 统计信息
        public int totalCategories = 0;
        public int enabledCategories = 0;
        public int totalLogs = 0;
        public int enabledLogs = 0;
        public System.DateTime lastScanTime = System.DateTime.MinValue;
        
        /// <summary>
        /// 转换为运行时配置
        /// </summary>
        public LogManagerConfig ToRuntimeConfig()
        {
            var config = new LogManagerConfig();
            config.categories = categories;
            config.logEntries = logEntries;
            config.autoScanOnStart = autoScanOnStart;
            config.saveOnChange = saveOnChange;
            config.scanPaths = scanPaths;
            config.excludePaths = excludePaths;
            config.globalMinLevel = globalMinLevel;
            config.enableCategoryFilter = enableCategoryFilter;
            config.enableLogFilter = enableLogFilter;
            config.totalCategories = totalCategories;
            config.enabledCategories = enabledCategories;
            config.totalLogs = totalLogs;
            config.enabledLogs = enabledLogs;
            config.lastScanTime = lastScanTime;
            return config;
        }
        
        /// <summary>
        /// 从运行时配置更新
        /// </summary>
        public void UpdateFromRuntimeConfig(LogManagerConfig config)
        {
            if (config == null) return;
            
            categories = config.categories;
            logEntries = config.logEntries;
            autoScanOnStart = config.autoScanOnStart;
            saveOnChange = config.saveOnChange;
            scanPaths = config.scanPaths;
            excludePaths = config.excludePaths;
            globalMinLevel = config.globalMinLevel;
            enableCategoryFilter = config.enableCategoryFilter;
            enableLogFilter = config.enableLogFilter;
            totalCategories = config.totalCategories;
            enabledCategories = config.enabledCategories;
            totalLogs = config.totalLogs;
            enabledLogs = config.enabledLogs;
            lastScanTime = config.lastScanTime;
        }
        
        /// <summary>
        /// 初始化配置
        /// </summary>
        public void Initialize()
        {
            // 初始化默认扫描路径
            if (scanPaths.Count == 0)
            {
                scanPaths.Add("Assets/Script");
            }
            
            // 初始化默认排除路径
            if (excludePaths.Count == 0)
            {
                excludePaths.Add("Assets/Script/Generated");
                excludePaths.Add("Assets/Script/Editor");
            }
            
            // 构建字典
            RebuildDictionaries();
        }
        
        /// <summary>
        /// 重建字典
        /// </summary>
        public void RebuildDictionaries()
        {
            // 这里可以添加字典重建逻辑
        }
        
        /// <summary>
        /// 添加日志条目
        /// </summary>
        public void AddLogEntry(LogEntry logEntry)
        {
            if (logEntry == null) return;
            
            // 检查是否已存在
            var existing = FindLogEntry(logEntry.id);
            if (existing != null)
            {
                // 更新现有条目
                existing.UpdateMessage(logEntry.message);
                existing.enabled = logEntry.enabled;
                existing.lastModified = System.DateTime.Now;
            }
            else
            {
                // 添加新条目
                logEntries.Add(logEntry);
            }
            
            UpdateStatistics();
        }
        
        /// <summary>
        /// 查找日志条目
        /// </summary>
        public LogEntry FindLogEntry(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return null;
            
            // 尝试直接匹配
            foreach (var log in logEntries)
            {
                if (log.id == identifier || log.fallbackId == identifier)
                {
                    return log;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取或创建分类
        /// </summary>
        public LogCategory GetOrCreateCategory(string path)
        {
            var category = FindCategory(path);
            if (category != null)
            {
                return category;
            }
            
            // 创建新分类
            var pathParts = path.Split('.');
            var rootCategory = GetOrCreateRootCategory();
            
            return CreateCategoryPath(rootCategory, pathParts, 0);
        }
        
        /// <summary>
        /// 查找分类
        /// </summary>
        public LogCategory FindCategory(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            
            foreach (var category in categories)
            {
                if (category.fullPath == path)
                {
                    return category;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取或创建根分类
        /// </summary>
        private LogCategory GetOrCreateRootCategory()
        {
            if (categories.Count == 0)
            {
                var root = new LogCategory("Root", "Root");
                categories.Add(root);
            }
            
            return categories[0];
        }
        
        /// <summary>
        /// 递归创建分类路径
        /// </summary>
        private LogCategory CreateCategoryPath(LogCategory parent, string[] pathParts, int index)
        {
            if (index >= pathParts.Length) return parent;
            
            var currentName = pathParts[index];
            var currentPath = string.Join(".", pathParts, 0, index + 1);
            
            var child = parent.FindChild(currentName);
            if (child == null)
            {
                child = new LogCategory(currentName, currentPath);
                parent.AddChild(child);
            }
            
            return CreateCategoryPath(child, pathParts, index + 1);
        }
        
        /// <summary>
        /// 更新统计信息
        /// </summary>
        public void UpdateStatistics()
        {
            totalCategories = 0;
            enabledCategories = 0;
            totalLogs = 0;
            enabledLogs = 0;
            
            foreach (var category in categories)
            {
                category.UpdateStatistics();
                totalCategories += 1 + CountSubCategories(category);
                enabledCategories += (category.enabled ? 1 : 0) + CountEnabledSubCategories(category);
                totalLogs += category.totalLogCount;
                enabledLogs += category.enabledLogCount;
            }
        }
        
        /// <summary>
        /// 计算子分类数量
        /// </summary>
        private int CountSubCategories(LogCategory category)
        {
            int count = 0;
            foreach (var child in category.children)
            {
                count += 1 + CountSubCategories(child);
            }
            return count;
        }
        
        /// <summary>
        /// 计算启用的子分类数量
        /// </summary>
        private int CountEnabledSubCategories(LogCategory category)
        {
            int count = 0;
            foreach (var child in category.children)
            {
                count += (child.enabled ? 1 : 0) + CountEnabledSubCategories(child);
            }
            return count;
        }
        
        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            categories.Clear();
            logEntries.Clear();
            UpdateStatistics();
        }
    }
}
