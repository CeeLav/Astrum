using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Modules;
using Astrum.Editor.RoleEditor.Layout;
using Astrum.Editor.RoleEditor.Persistence;
using Astrum.Editor.RoleEditor.Services;
using Astrum.Editor.RoleEditor.Timeline;

namespace Astrum.Editor.RoleEditor.Windows
{
    /// <summary>
    /// 动作编辑器主窗口
    /// </summary>
    public class ActionEditorWindow : EditorWindow
    {
        // === UI模块 ===
        private ActionListModule _listModule;
        private ActionConfigModule _configModule;
        private RolePreviewModule _previewModule;
        private TimelineEditorModule _timelineModule;
        private ActionEditorLayout _layoutManager;
        
        // === 数据 ===
        private List<ActionEditorData> _allActions = new List<ActionEditorData>();
        private ActionEditorData _selectedAction;
        
        // === 布局常量 ===
        private const float MIN_WINDOW_WIDTH = 1200f;
        private const float MIN_WINDOW_HEIGHT = 800f;
        
        // === Unity生命周期 ===
        
        [MenuItem("Tools/Action Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<ActionEditorWindow>("动作编辑器");
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }
        
        private void OnEnable()
        {
            // 初始化配置表Helper
            ConfigTableHelper.ClearCache();
            
            InitializeModules();
            RegisterTracks();
            LoadData();
            
            Debug.Log("[ActionEditor] Action Editor Window opened");
        }
        
        private void OnDisable()
        {
            CheckUnsavedChanges();
            CleanupModules();
            
            Debug.Log("[ActionEditor] Action Editor Window closed");
        }
        
        private void OnDestroy()
        {
            CleanupModules();
        }
        
        private void OnGUI()
        {
            // 计算布局
            _layoutManager.CalculateLayout(position);
            
            // 绘制UI
            DrawToolbar();
            DrawMainArea();
            
            // 处理分隔线拖拽
            _layoutManager.HandleSeparatorDrag(Event.current, position);
        }
        
        // === 初始化 ===
        
        private void InitializeModules()
        {
            // 布局管理器
            _layoutManager = new ActionEditorLayout();
            
            // 列表模块
            _listModule = new ActionListModule();
            _listModule.OnActionSelected += OnActionSelected;
            _listModule.OnCreateNew += OnCreateNewAction;
            _listModule.OnDuplicate += OnDuplicateAction;
            _listModule.OnDelete += OnDeleteAction;
            
            // 配置模块
            _configModule = new ActionConfigModule();
            _configModule.OnActionModified += OnActionModified;
            _configModule.OnJumpToTimeline += OnJumpToTimeline;
            
            // 预览模块
            _previewModule = new RolePreviewModule();
            _previewModule.Initialize();
            
            // 时间轴模块
            _timelineModule = new TimelineEditorModule();
            _timelineModule.Initialize(60);
            _timelineModule.OnEventModified += OnTimelineEventModified;
        }
        
        private void CleanupModules()
        {
            _configModule?.Cleanup();
            _previewModule?.Cleanup();
            _timelineModule?.Cleanup();
        }
        
        private void RegisterTracks()
        {
            // 清空现有轨道
            TimelineTrackRegistry.Clear();
            
            // 注册被取消标签轨道
            TimelineTrackRegistry.RegisterTrack(new TimelineTrackConfig
            {
                TrackType = "BeCancelTag",
                TrackName = "被取消标签",
                TrackIcon = "🚫",
                TrackColor = new Color(0.8f, 0.3f, 0.3f),
                TrackHeight = 45f,
                IsVisible = true,
                IsLocked = false,
                SortOrder = 0,
                AllowOverlap = false
            });
            
            // 注册临时取消标签轨道
            TimelineTrackRegistry.RegisterTrack(new TimelineTrackConfig
            {
                TrackType = "TempBeCancelTag",
                TrackName = "临时取消",
                TrackIcon = "⏱",
                TrackColor = new Color(0.8f, 0.6f, 0.3f),
                TrackHeight = 45f,
                IsVisible = true,
                IsLocked = false,
                SortOrder = 1,
                AllowOverlap = true
            });
            
            // 注册特效轨道
            TimelineTrackRegistry.RegisterTrack(new TimelineTrackConfig
            {
                TrackType = "VFX",
                TrackName = "特效",
                TrackIcon = "✨",
                TrackColor = new Color(0.8f, 0.4f, 1f),
                TrackHeight = 45f,
                IsVisible = true,
                IsLocked = false,
                SortOrder = 2,
                AllowOverlap = true
            });
            
            // 注册音效轨道
            TimelineTrackRegistry.RegisterTrack(new TimelineTrackConfig
            {
                TrackType = "SFX",
                TrackName = "音效",
                TrackIcon = "🔊",
                TrackColor = new Color(1f, 0.7f, 0.2f),
                TrackHeight = 45f,
                IsVisible = true,
                IsLocked = false,
                SortOrder = 3,
                AllowOverlap = true
            });
            
            // 注册相机震动轨道
            TimelineTrackRegistry.RegisterTrack(new TimelineTrackConfig
            {
                TrackType = "CameraShake",
                TrackName = "相机震动",
                TrackIcon = "📷",
                TrackColor = new Color(0.6f, 0.6f, 0.6f),
                TrackHeight = 45f,
                IsVisible = true,
                IsLocked = false,
                SortOrder = 4,
                AllowOverlap = true
            });
        }
        
        // === 数据加载和保存 ===
        
        private void LoadData()
        {
            try
            {
                _allActions = ActionDataReader.ReadActionData();
                
                if (_allActions.Count > 0)
                {
                    _listModule.SelectAction(_allActions[0]);
                }
                
                Debug.Log($"[ActionEditor] Loaded {_allActions.Count} actions");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ActionEditor] Failed to load action data: {ex}");
                EditorUtility.DisplayDialog("加载失败", $"加载动作数据失败：{ex.Message}", "确定");
            }
        }
        
        private void SaveData()
        {
            try
            {
                // 写入CSV文件
                if (ActionDataWriter.WriteActionData(_allActions))
                {
                    // 清除所有修改标记
                    foreach (var action in _allActions)
                    {
                        action.ClearDirty();
                    }
                    
                    Debug.Log($"[ActionEditor] Successfully saved {_allActions.Count} actions");
                    EditorUtility.DisplayDialog("保存成功", $"成功保存 {_allActions.Count} 个动作", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("保存失败", "保存动作数据失败", "确定");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ActionEditor] Failed to save action data: {ex}");
                EditorUtility.DisplayDialog("保存失败", $"保存动作数据失败：{ex.Message}", "确定");
            }
        }
        
        private void CheckUnsavedChanges()
        {
            bool hasUnsaved = _allActions.Any(a => a.IsDirty);
            
            if (hasUnsaved)
            {
                bool save = EditorUtility.DisplayDialog(
                    "未保存的修改",
                    "有未保存的修改，是否保存？",
                    "保存", "不保存"
                );
                
                if (save)
                {
                    SaveData();
                }
            }
        }
        
        // === 事件处理 ===
        
        private void OnActionSelected(ActionEditorData action)
        {
            _selectedAction = action;
            
            // 更新配置面板
            _configModule.SetAction(action);
            
            // 更新时间轴
            if (action != null)
            {
                _timelineModule.SetTotalFrames(action.Duration);
                _timelineModule.SetEvents(action.TimelineEvents);
                _timelineModule.SetTracks(TimelineTrackRegistry.GetAllTracks());
                
                // TODO: 更新预览模块（需要加载模型和动画）
            }
        }
        
        private void OnCreateNewAction()
        {
            var existingIds = new HashSet<int>(_allActions.Select(a => a.ActionId));
            int newId = AstrumEditorUtility.GenerateUniqueId(1000, existingIds);
            
            var newAction = ActionEditorData.CreateDefault(newId);
            _allActions.Add(newAction);
            _listModule.SelectAction(newAction);
            
            Debug.Log($"[ActionEditor] Created new action: {newId}");
        }
        
        private void OnDuplicateAction(ActionEditorData action)
        {
            if (action == null) return;
            
            var existingIds = new HashSet<int>(_allActions.Select(a => a.ActionId));
            int newId = AstrumEditorUtility.GenerateUniqueId(1000, existingIds);
            
            var duplicated = action.Clone();
            duplicated.ActionId = newId;
            duplicated.ActionName = action.ActionName + "_Copy";
            
            _allActions.Add(duplicated);
            _listModule.SelectAction(duplicated);
            
            Debug.Log($"[ActionEditor] Duplicated action: {action.ActionId} → {newId}");
        }
        
        private void OnDeleteAction(ActionEditorData action)
        {
            if (action == null) return;
            
            bool confirm = EditorUtility.DisplayDialog(
                "删除动作",
                $"确定要删除动作 [{action.ActionId}] {action.ActionName} 吗？",
                "删除", "取消"
            );
            
            if (confirm)
            {
                _allActions.Remove(action);
                
                if (_allActions.Count > 0)
                {
                    _listModule.SelectAction(_allActions[0]);
                }
                else
                {
                    _listModule.SelectAction(null);
                }
                
                Debug.Log($"[ActionEditor] Deleted action: {action.ActionId}");
            }
        }
        
        private void OnActionModified(ActionEditorData action)
        {
            // 动作配置被修改，可能需要更新时间轴
            if (action != null && action == _selectedAction)
            {
                _timelineModule.SetTotalFrames(action.Duration);
            }
        }
        
        private void OnTimelineEventModified(TimelineEvent evt)
        {
            // 时间轴事件被修改，标记动作为脏
            if (_selectedAction != null)
            {
                _selectedAction.MarkDirty();
            }
        }
        
        private void OnJumpToTimeline()
        {
            // TODO: 滚动到时间轴区域
            Debug.Log("[ActionEditor] Jump to timeline requested");
        }
        
        // === UI绘制 ===
        
        private void DrawToolbar()
        {
            Rect toolbarRect = _layoutManager.GetToolbarRect();
            
            GUILayout.BeginArea(toolbarRect);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                // 保存按钮
                if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    SaveData();
                }
                
                // 刷新按钮
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    LoadData();
                }
                
                // 验证按钮
                if (GUILayout.Button("验证", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    ValidateAllActions();
                }
                
                GUILayout.FlexibleSpace();
                
                // 当前动作信息
                if (_selectedAction != null)
                {
                    string info = $"动作 {_selectedAction.ActionId}: {_selectedAction.ActionName} ({_selectedAction.ActionType})";
                    EditorGUILayout.LabelField(info, EditorStyles.toolbarButton);
                }
                
                GUILayout.FlexibleSpace();
                
                // 帮助按钮
                if (GUILayout.Button("帮助", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    Application.OpenURL("https://github.com/yourproject/wiki/action-editor");
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        
        private void DrawMainArea()
        {
            // 左侧：动作列表
            Rect leftRect = _layoutManager.GetLeftPanelRect();
            _listModule.DrawList(leftRect, _allActions);
            
            // 右上左：配置面板
            Rect configRect = _layoutManager.GetConfigPanelRect();
            _configModule.DrawConfig(configRect, _selectedAction);
            
            // 右上右：预览面板
            Rect previewRect = _layoutManager.GetPreviewPanelRect();
            DrawPreviewPanel(previewRect);
            
            // 右下：时间轴
            Rect timelineRect = _layoutManager.GetTimelineRect();
            _timelineModule.DrawTimeline(timelineRect);
        }
        
        private void DrawPreviewPanel(Rect rect)
        {
            GUILayout.BeginArea(rect);
            
            if (_selectedAction == null)
            {
                EditorGUILayout.HelpBox("请选择一个动作", MessageType.Info);
            }
            else
            {
                // TODO: 绘制预览
                EditorGUILayout.HelpBox("动画预览（待实现）", MessageType.Info);
            }
            
            GUILayout.EndArea();
        }
        
        private void ValidateAllActions()
        {
            // TODO: 实现验证逻辑
            EditorUtility.DisplayDialog("验证", "所有动作数据验证通过！", "确定");
        }
    }
}
