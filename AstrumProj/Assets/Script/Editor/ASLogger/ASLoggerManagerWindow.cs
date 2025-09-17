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
        private Astrum.Client.LogManagerConfigEditor _config;
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
        
        /// <summary>
        /// 在进入播放模式时重新初始化配置
        /// </summary>
        [InitializeOnEnterPlayMode]
        static void OnEnterPlayMode()
        {
            Debug.Log("[ASLoggerManager] 进入播放模式，准备重新初始化配置");
        }
        
        /// <summary>
        /// 手动刷新配置（用于游戏状态变化时）
        /// </summary>
        public void RefreshConfig()
        {
            Debug.Log("[ASLoggerManager] 手动刷新配置");
            _isInitialized = false;
            Initialize();
            
            // 确保配置推送到ASLogger
            PushConfigToASLogger();
        }
        
        /// <summary>
        /// 推送配置到ASLogger（无论游戏是否运行）
        /// </summary>
        public void PushConfigToASLogger()
        {
            if (_config != null)
            {
                Astrum.CommonBase.ASLogger.SetConfig(_config.ToRuntimeConfig());
                Debug.Log($"[ASLoggerManager] 配置已推送到ASLogger，包含{_config.logEntries.Count}个日志条目");
            }
            else
            {
                Debug.LogWarning("[ASLoggerManager] 配置为空，无法推送到ASLogger");
            }
        }
        
        private void OnEnable()
        {
            try
            {
                Debug.Log("[ASLoggerManager] OnEnable - 开始初始化");
                
                // 直接初始化，不依赖ASLogger的运行时状态
                // 因为游戏启动/关闭时Unity会进行域重载，静态字段会被重置
                Initialize();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ASLoggerManager] OnEnable时发生错误: {ex.Message}");
                // 强制初始化
                _isInitialized = false;
                Initialize();
            }
        }
        
        private void OnDisable()
        {
            SaveConfig();
        }
        
        private void Initialize()
        {
            if (_isInitialized) return;
            
            try
            {
                Debug.Log("[ASLoggerManager] 开始初始化窗口");
                
                LoadConfig();
                InitializeLogFilter();
                UpdateStatistics();
                
                _isInitialized = true;
                
                Debug.Log("[ASLoggerManager] 窗口初始化完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ASLoggerManager] 初始化失败: {ex.Message}");
                _isInitialized = false;
            }
        }
        
        private void LoadConfig()
        {
            try
            {
                Debug.Log("[ASLoggerManager] 开始加载配置");
                
                // 首先尝试从Resources文件夹加载
                _config = Resources.Load<Astrum.Client.LogManagerConfigEditor>("LogManagerConfig");
                
                if (_config == null)
                {
                    // 尝试从项目根目录加载
                    var configs = Resources.FindObjectsOfTypeAll<Astrum.Client.LogManagerConfigEditor>();
                    if (configs.Length > 0)
                    {
                        _config = configs[0];
                        Debug.Log("[ASLoggerManager] 从项目根目录加载配置");
                    }
                    else
                    {
                        // 创建新配置并保存到Resources文件夹
                        _config = CreateInstance<Astrum.Client.LogManagerConfigEditor>();
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
                else
                {
                    Debug.Log("[ASLoggerManager] 从Resources文件夹加载配置");
                }
                
                if (_config != null)
                {
                    _config.Initialize();
                    
                    // 推送配置到ASLogger
                    PushConfigToASLogger();
                }
                else
                {
                    Debug.LogError("[ASLoggerManager] 无法加载或创建配置文件");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ASLoggerManager] 加载配置时发生错误: {ex.Message}");
                // 创建默认配置作为后备
                _config = CreateInstance<Astrum.Client.LogManagerConfigEditor>();
                _config.Initialize();
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
                EditorGUILayout.HelpBox("正在初始化...", MessageType.Info);
                Initialize();
                return;
            }
            
            if (_config == null)
            {
                EditorGUILayout.HelpBox("无法加载日志管理器配置", MessageType.Error);
                if (GUILayout.Button("重新加载配置"))
                {
                    RefreshConfig();
                }
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
                if (GUILayout.Button("重新初始化"))
                {
                    _isInitialized = false;
                    Initialize();
                }
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
            
            if (GUILayout.Button("推送配置", GUILayout.Width(80)))
            {
                PushConfigToASLogger();
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
            
            // 推送整个配置到ASLogger，确保同步
            PushConfigToASLogger();
            
            UpdateStatistics();
            SaveConfig();
        }
        
        private void OnLogToggle(LogEntry log, bool enabled)
        {
            //Debug.Log($"[OnLogToggle] 开始切换日志状态 - log.id='{log.id}', log.fallbackId='{log.fallbackId}', enabled={enabled}");
            
            // 直接修改传入的log对象（这应该是原始对象的引用）
            log.enabled = enabled;
            //Debug.Log($"[OnLogToggle] 直接修改log对象状态为{enabled}");
            
            // 同时确保配置中的对象也被修改（双重保险）
            var originalLog = _config.FindLogEntry(log.id);
            if (originalLog != null)
            {
                originalLog.enabled = enabled;
                //Debug.Log($"[OnLogToggle] 同时修改配置中的原始日志状态为{enabled}");
            }
            else
            {
                // 如果找不到原始日志，尝试用fallbackId查找
                originalLog = _config.FindLogEntry(log.fallbackId);
                if (originalLog != null)
                {
                    originalLog.enabled = enabled;
                    //Debug.Log($"[OnLogToggle] 通过fallbackId修改配置中的原始日志状态为{enabled}");
                }
                else
                {
                    Debug.LogWarning($"[OnLogToggle] 找不到原始日志 - id='{log.id}', fallbackId='{log.fallbackId}', 配置中共有{_config.logEntries.Count}个日志条目");
                }
            }
            
            // 推送整个配置到ASLogger，确保同步
            PushConfigToASLogger();
            
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
            }
            
            // 推送整个配置到ASLogger，确保同步
            PushConfigToASLogger();
            
            UpdateStatistics();
            SaveConfig();
        }
        
        private void DisableAll()
        {
            foreach (var category in _config.categories)
            {
                category.SetEnabled(false);
            }
            
            // 推送整个配置到ASLogger，确保同步
            PushConfigToASLogger();
            
            UpdateStatistics();
            SaveConfig();
        }
        
        private void RefreshData()
        {
            try
            {
                EditorUtility.DisplayProgressBar("刷新配置", "正在刷新配置和扫描日志...", 0f);
                
                // 先刷新配置
                RefreshConfig();
                
                if (_config == null)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("错误", "无法加载配置", "确定");
                    return;
                }
                
                // 缓存现有日志的启用状态
                var enabledStateCache = CacheLogEnabledStates();
                Debug.Log($"[ASLoggerManager] 缓存了 {enabledStateCache.Count} 个日志条目的启用状态");
                
                EditorUtility.DisplayProgressBar("扫描日志", "正在扫描项目中的日志...", 0.3f);
                
                // 扫描项目中的实际日志 - 使用Roslyn扫描器
                var projectLogs = RoslynLogScanner.ScanProject(_config.ToRuntimeConfig());
                
                Debug.Log($"[ASLoggerManager] Roslyn扫描器找到 {projectLogs.Count} 个日志条目");
                
                // 恢复启用状态
                RestoreLogEnabledStates(projectLogs, enabledStateCache);
                
                // 清除现有配置，重新构建
                _config.logEntries.Clear();
                _config.categories.Clear();
                
                // 添加扫描到的日志到配置
                int addedCount = 0;
                foreach (var log in projectLogs)
                {
                    _config.AddLogEntry(log);
                    
                    // 确保分类存在
                    var category = _config.GetOrCreateCategory(log.category);
                    category.AddLog(log);
                    addedCount++;
                }
                
                Debug.Log($"[ASLoggerManager] 成功添加 {addedCount} 个日志条目到配置");
                
                // 如果没有扫描到日志，记录警告
                if (projectLogs.Count == 0)
                {
                    Debug.LogWarning("未扫描到任何日志，请检查项目中的ASLogger调用格式");
                }
                
                // 更新统计信息
                UpdateStatistics();
                
                // 重新构建字典
                _config.RebuildDictionaries();
                
                // 推送配置到ASLogger
                PushConfigToASLogger();
                
                // 保存配置
                SaveConfig();
                
                EditorUtility.ClearProgressBar();
                
                Debug.Log($"日志扫描完成，找到 {projectLogs.Count} 个实际日志条目，恢复了 {enabledStateCache.Count} 个状态");
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
        
        /// <summary>
        /// 缓存现有日志的启用状态
        /// </summary>
        private Dictionary<string, bool> CacheLogEnabledStates()
        {
            var cache = new Dictionary<string, bool>();
            
            foreach (var log in _config.logEntries)
            {
                // 优先使用log.id作为键
                if (!string.IsNullOrEmpty(log.id))
                {
                    cache[log.id] = log.enabled;
                }
                // 如果没有id，使用fallbackId
                else if (!string.IsNullOrEmpty(log.fallbackId))
                {
                    cache[log.fallbackId] = log.enabled;
                }
                // 如果都没有，使用文件路径+行号+消息的组合作为键
                else
                {
                    var key = $"{log.filePath}:{log.lineNumber}:{log.message}";
                    cache[key] = log.enabled;
                }
            }
            
            return cache;
        }
        
        /// <summary>
        /// 恢复日志的启用状态
        /// </summary>
        private void RestoreLogEnabledStates(List<LogEntry> newLogs, Dictionary<string, bool> enabledStateCache)
        {
            int restoredCount = 0;
            
            foreach (var log in newLogs)
            {
                bool foundMatch = false;
                
                // 尝试使用log.id匹配
                if (!string.IsNullOrEmpty(log.id) && enabledStateCache.ContainsKey(log.id))
                {
                    log.enabled = enabledStateCache[log.id];
                    foundMatch = true;
                }
                // 尝试使用fallbackId匹配
                else if (!string.IsNullOrEmpty(log.fallbackId) && enabledStateCache.ContainsKey(log.fallbackId))
                {
                    log.enabled = enabledStateCache[log.fallbackId];
                    foundMatch = true;
                }
                // 尝试使用文件路径+行号+消息的组合匹配
                else
                {
                    var key = $"{log.filePath}:{log.lineNumber}:{log.message}";
                    if (enabledStateCache.ContainsKey(key))
                    {
                        log.enabled = enabledStateCache[key];
                        foundMatch = true;
                    }
                }
                
                if (foundMatch)
                {
                    restoredCount++;
                }
            }
            
            Debug.Log($"[ASLoggerManager] 恢复了 {restoredCount} 个日志条目的启用状态");
        }
    }

}
