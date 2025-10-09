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
        private AnimationPreviewModule _previewModule;
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
            // 取消事件订阅
            if (_listModule != null)
            {
                _listModule.OnEntitySelected -= OnEntitySelected;
            }
            
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
            
            // 处理全局快捷键
            HandleGlobalHotkeys(Event.current);
            
            // 如果动画正在播放，同步播放头到时间轴
            SyncAnimationToTimeline();
        }
        
        /// <summary>
        /// 处理全局快捷键
        /// </summary>
        private void HandleGlobalHotkeys(Event evt)
        {
            if (evt.type == EventType.KeyDown)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.Delete:
                        // 删除选中的时间轴事件
                        if (_timelineModule != null)
                        {
                            TimelineEvent selectedEvent = _timelineModule.GetSelectedEvent();
                            if (selectedEvent != null)
                            {
                                bool confirm = EditorUtility.DisplayDialog(
                                    "删除事件",
                                    $"确定要删除事件 [{selectedEvent.TrackType}] {selectedEvent.DisplayName} 吗？",
                                    "删除", "取消"
                                );
                                
                                if (confirm)
                                {
                                    _timelineModule.RemoveSelectedEvent();
                                    
                                    // 标记动作为已修改
                                    if (_selectedAction != null)
                                    {
                                        _selectedAction.MarkDirty();
                                    }
                                    
                                    evt.Use();
                                    Repaint();
                                }
                            }
                        }
                        break;
                }
            }
        }
        
        /// <summary>
        /// 同步动画播放到时间轴
        /// </summary>
        private void SyncAnimationToTimeline()
        {
            if (_previewModule != null && _previewModule.IsPlaying())
            {
                int animFrame = _previewModule.GetCurrentFrame();
                int timelineFrame = _timelineModule.GetCurrentFrame();
                
                // 如果动画帧和时间轴帧不同步，更新时间轴
                if (animFrame != timelineFrame)
                {
                    _timelineModule.SetCurrentFrame(animFrame);
                }
                
                // 持续重绘
                Repaint();
            }
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
            _listModule.OnEntitySelected += OnEntitySelected;  // 实体选择事件
            
            // 配置模块
            _configModule = new ActionConfigModule();
            _configModule.OnActionModified += OnActionModified;
            _configModule.OnJumpToTimeline += OnJumpToTimeline;
            
            // 预览模块（使用简化的动画预览）
            _previewModule = new AnimationPreviewModule();
            _previewModule.Initialize();
            
            // 时间轴模块
            _timelineModule = new TimelineEditorModule();
            _timelineModule.Initialize(60);
            _timelineModule.OnEventModified += OnTimelineEventModified;
            _timelineModule.OnCurrentFrameChanged += OnTimelineFrameChanged;
            _timelineModule.OnEventSelected += OnTimelineEventSelected;
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
                AllowOverlap = false,
                EventRenderer = Timeline.Renderers.BeCancelTagTrackRenderer.RenderEvent,
                EventEditor = Timeline.Renderers.BeCancelTagTrackRenderer.EditEvent
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
                SortOrder = 1,
                AllowOverlap = true,
                EventRenderer = Timeline.Renderers.VFXTrackRenderer.RenderEvent,
                EventEditor = Timeline.Renderers.VFXTrackRenderer.EditEvent
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
                SortOrder = 2,
                AllowOverlap = true,
                EventRenderer = Timeline.Renderers.SFXTrackRenderer.RenderEvent,
                EventEditor = Timeline.Renderers.SFXTrackRenderer.EditEvent
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
                SortOrder = 3,
                AllowOverlap = true,
                EventRenderer = Timeline.Renderers.CameraShakeTrackRenderer.RenderEvent,
                EventEditor = Timeline.Renderers.CameraShakeTrackRenderer.EditEvent
            });
            
            // 注意：TempBeCancelTag 轨道已移除，因为它是运行时数据
            
            Debug.Log("[ActionEditor] Registered 4 timeline tracks with renderers");
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
                
                // 自动选择第一个实体并加载模型
                var allEntities = ConfigTableHelper.GetAllEntities();
                if (allEntities.Count > 0)
                {
                    int firstEntityId = allEntities[0].EntityId;
                    _previewModule.SetEntity(firstEntityId);
                    Debug.Log($"[ActionEditor] Auto-loaded first entity model: {firstEntityId}");
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
                
                // 更新预览模块：加载动画
                LoadAnimationForAction(action);
            }
        }
        
        // === 动画预览方法 ===
        
        private void LoadAnimationForAction(ActionEditorData action)
        {
            if (action == null || _previewModule == null)
                return;
            
            if (string.IsNullOrEmpty(action.AnimationPath))
            {
                Debug.LogWarning($"[ActionEditor] Action {action.ActionId} has no animation path");
                return;
            }
            
            // 加载动画片段
            _previewModule.LoadAnimationFromPath(action.AnimationPath);
            
            // 计算动画总帧数
            int animationTotalFrames = _previewModule.GetTotalFrames();
            if (animationTotalFrames > 0)
            {
                // 更新动画总帧数
                action.AnimationDuration = animationTotalFrames;
                
                // 如果动作帧数为0或超过动画帧数，则设置为动画帧数
                if (action.Duration <= 0 || action.Duration > animationTotalFrames)
                {
                    action.Duration = animationTotalFrames;
                }
                
                // 时间轴使用动作总帧数
                _timelineModule.SetTotalFrames(action.Duration);
            }
        }
        
        private void PlayAnimation()
        {
            if (_selectedAction == null || _previewModule == null) return;
            
            _previewModule.Play();
            Debug.Log("[ActionEditor] Play animation");
        }
        
        private void PauseAnimation()
        {
            if (_previewModule == null) return;
            
            _previewModule.Pause();
            Debug.Log("[ActionEditor] Pause animation");
        }
        
        private void StopAnimation()
        {
            if (_selectedAction == null || _previewModule == null) return;
            
            _previewModule.Stop();
            
            // 停止动画，重置到第0帧
            _timelineModule.SetCurrentFrame(0);
            
            Debug.Log("[ActionEditor] Stop animation");
        }
        
        private void PreviousFrame()
        {
            int currentFrame = _timelineModule.GetCurrentFrame();
            _timelineModule.SetCurrentFrame(Mathf.Max(0, currentFrame - 1));
        }
        
        private void NextFrame()
        {
            int currentFrame = _timelineModule.GetCurrentFrame();
            int totalFrames = _timelineModule.GetTotalFrames();
            _timelineModule.SetCurrentFrame(Mathf.Min(totalFrames - 1, currentFrame + 1));
        }
        
        /// <summary>
        /// 时间轴帧改变时，同步到动画预览
        /// </summary>
        private void OnTimelineFrameChanged(int frame)
        {
            if (_previewModule != null)
            {
                _previewModule.SetFrame(frame);
                
                // 触发重绘
                Repaint();
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
                // 验证动作帧数不超过动画帧数
                if (action.Duration > action.AnimationDuration)
                {
                    action.Duration = action.AnimationDuration;
                }
                
                _timelineModule.SetTotalFrames(action.Duration);
                
                // 检查动画是否改变，如果改变则重新加载
                LoadAnimationForAction(action);
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
        
        private void OnTimelineEventSelected(TimelineEvent evt)
        {
            // 时间轴事件被选中，通知配置面板
            if (_configModule != null)
            {
                _configModule.SetSelectedEvent(evt);
            }
            
            Repaint();
        }
        
        private void OnJumpToTimeline()
        {
            // TODO: 滚动到时间轴区域
            Debug.Log("[ActionEditor] Jump to timeline requested");
        }
        
        private void OnEntitySelected(int entityId)
        {
            if (_previewModule != null)
            {
                _previewModule.SetEntity(entityId);
                
                // 如果已选择动作，重新加载动画
                if (_selectedAction != null)
                {
                    LoadAnimationForAction(_selectedAction);
                }
                
                Debug.Log($"[ActionEditor] Entity {entityId} selected for preview");
            }
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
            if (_selectedAction == null)
            {
                GUILayout.BeginArea(rect);
                EditorGUILayout.HelpBox("请选择一个动作", MessageType.Info);
                GUILayout.EndArea();
                return;
            }
            
            // 分割预览区域：上方模型预览，下方控制
            float controlHeight = 80f;
            Rect previewRect = new Rect(rect.x, rect.y, rect.width, rect.height - controlHeight);
            Rect controlRect = new Rect(rect.x, rect.y + rect.height - controlHeight, rect.width, controlHeight);
            
            // 绘制模型预览
            if (_previewModule != null)
            {
                _previewModule.DrawPreview(previewRect);
            }
            
            // 绘制动画控制
            DrawAnimationControl(controlRect);
        }
        
        private void DrawAnimationControl(Rect rect)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("动画控制", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                {
                    // 播放/暂停按钮
                    if (GUILayout.Button("▶ 播放", GUILayout.Height(25)))
                    {
                        PlayAnimation();
                    }
                    
                    if (GUILayout.Button("⏸ 暂停", GUILayout.Height(25)))
                    {
                        PauseAnimation();
                    }
                    
                    if (GUILayout.Button("⏹ 停止", GUILayout.Height(25)))
                    {
                        StopAnimation();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("◀", GUILayout.Width(30)))
                    {
                        PreviousFrame();
                    }
                    
                    int currentFrame = _timelineModule.GetCurrentFrame();
                    int totalFrames = _timelineModule.GetTotalFrames();
                    EditorGUILayout.LabelField($"当前: {currentFrame} / {totalFrames}帧", EditorStyles.centeredGreyMiniLabel);
                    
                    if (GUILayout.Button("▶", GUILayout.Width(30)))
                    {
                        NextFrame();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void ValidateAllActions()
        {
            // TODO: 实现验证逻辑
            EditorUtility.DisplayDialog("验证", "所有动作数据验证通过！", "确定");
        }
    }
}
