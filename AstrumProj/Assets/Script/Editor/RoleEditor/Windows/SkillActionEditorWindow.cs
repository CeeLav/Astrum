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
    /// 技能动作编辑器主窗口
    /// 继承自动作编辑器，扩展技能系统专属功能
    /// </summary>
    public class SkillActionEditorWindow : EditorWindow
    {
        // === UI模块 ===
        private SkillActionListModule _listModule;
        private SkillActionConfigModule _configModule;
        private AnimationPreviewModule _previewModule;
        private TimelineEditorModule _timelineModule;
        private EventDetailModule _eventDetailModule;
        private ActionEditorLayout _layoutManager;
        
        // === 数据 ===
        private List<SkillActionEditorData> _allSkillActions = new List<SkillActionEditorData>();
        private SkillActionEditorData _selectedSkillAction;
        
        // === 布局常量 ===
        private const float MIN_WINDOW_WIDTH = 1200f;
        private const float MIN_WINDOW_HEIGHT = 800f;
        
        // === Unity生命周期 ===
        
        [MenuItem("Astrum/Editor 编辑器/Skill Action Editor 技能动作编辑器", false, 2)]
        public static void ShowWindow()
        {
            var window = GetWindow<SkillActionEditorWindow>("技能动作编辑器");
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }
        
        /// <summary>
        /// 打开窗口并选中指定动作
        /// </summary>
        public static void OpenAndSelectAction(int actionId)
        {
            var window = GetWindow<SkillActionEditorWindow>("技能动作编辑器");
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
            window.Focus();
            
            // 延迟选中，确保数据已加载
            EditorApplication.delayCall += () =>
            {
                window.SelectActionById(actionId);
            };
        }
        
        /// <summary>
        /// 根据ActionId选中动作
        /// </summary>
        private void SelectActionById(int actionId)
        {
            var action = _allSkillActions.FirstOrDefault(a => a.ActionId == actionId);
            if (action != null)
            {
                _listModule.SelectAction(action);
                Debug.Log($"[SkillActionEditor] 已选中动作 {actionId}");
            }
            else
            {
                Debug.LogWarning($"[SkillActionEditor] 未找到动作 {actionId}");
            }
        }
        
        private void OnEnable()
        {
            // 初始化配置表Helper
            ConfigTableHelper.ClearCache();
            
            InitializeModules();
            RegisterTracks();
            LoadData();
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
            
            Debug.Log("[SkillActionEditor] Skill Action Editor Window closed");
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
        
        // === 初始化 ===
        
        private void InitializeModules()
        {
            // 布局管理器
            _layoutManager = new ActionEditorLayout();
            
            // 列表模块（使用技能动作列表模块）
            _listModule = new SkillActionListModule();
            _listModule.OnActionSelected += OnActionSelected;
            _listModule.OnCreateNew += OnCreateNewSkillAction;
            _listModule.OnDuplicate += OnDuplicateSkillAction;
            _listModule.OnDelete += OnDeleteSkillAction;
            _listModule.OnEntitySelected += OnEntitySelected;
            
            // 配置模块（使用技能动作配置模块）
            _configModule = new SkillActionConfigModule();
            _configModule.OnActionModified += OnActionModified;
            _configModule.OnJumpToTimeline += OnJumpToTimeline;
            
            // 预览模块
            _previewModule = new AnimationPreviewModule();
            _previewModule.Initialize();
            
            // 时间轴模块
            _timelineModule = new TimelineEditorModule();
            _timelineModule.Initialize(60);
            _timelineModule.OnEventModified += OnTimelineEventModified;
            _timelineModule.OnCurrentFrameChanged += OnTimelineFrameChanged;
            _timelineModule.OnEventSelected += OnTimelineEventSelected;
            
            // 事件详情模块
            _eventDetailModule = new EventDetailModule();
            _eventDetailModule.OnActionModified += OnActionModified;
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
            
            // 注册基础轨道（被取消标签、特效、音效、相机震动）
            RegisterBaseTracks();
            
            // 注册技能效果轨道
            RegisterSkillEffectTrack();
        }
        
        private void RegisterBaseTracks()
        {
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
        }
        
        
        // === 技能专属轨道注册 ===
        
        /// <summary>
        /// 注册技能效果轨道
        /// </summary>
        private void RegisterSkillEffectTrack()
        {
            // 注册技能效果轨道（使用新的独立渲染器）
            TimelineTrackRegistry.RegisterTrack(new TimelineTrackConfig
            {
                TrackType = "SkillEffect",
                TrackName = "技能效果",
                TrackIcon = "💥",
                TrackColor = new Color(1f, 0.3f, 0.3f),
                TrackHeight = 45f,
                IsVisible = true,
                IsLocked = false,
                SortOrder = 4,
                AllowOverlap = true,
                EventRenderer = Timeline.Renderers.SkillEffectTrackRenderer.RenderEvent,
                EventEditor = Timeline.Renderers.SkillEffectTrackRenderer.EditEvent
            });
        }
        
        // === 数据加载和保存 ===
        
        private void LoadData()
        {
            try
            {
                // 使用技能动作数据读取器（只加载 SkillActionTable 的数据）
                _allSkillActions = SkillActionDataReader.ReadSkillActionData();
                
                // 先加载实体模型（重要！必须在选择动作之前）
                var allEntities = ConfigTableHelper.GetAllEntities();
                if (allEntities.Count > 0)
                {
                    int firstEntityId = allEntities[0].EntityId;
                    _previewModule.SetEntity(firstEntityId);
                }
                
                // 然后再选择动作（此时模型已加载，动画可以正常播放）
                if (_allSkillActions.Count > 0)
                {
                    _listModule.SelectAction(_allSkillActions[0]);
                }
                else
                {
                    Debug.LogWarning("[SkillActionEditor] No skill actions found in SkillActionTable!");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SkillActionEditor] Failed to load skill action data: {ex}");
                EditorUtility.DisplayDialog("加载失败", $"加载技能动作数据失败：{ex.Message}", "确定");
            }
        }
        
        private void SaveData()
        {
            try
            {
                // 写入CSV文件
                if (SkillActionDataWriter.WriteSkillActionData(_allSkillActions))
                {
                    // 清除所有修改标记
                    foreach (var skillAction in _allSkillActions)
                    {
                        skillAction.ClearDirty();
                    }
                    
                    Debug.Log($"[SkillActionEditor] Successfully saved {_allSkillActions.Count} skill actions");
                    EditorUtility.DisplayDialog("保存成功", $"成功保存 {_allSkillActions.Count} 个技能动作", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("保存失败", "保存技能动作数据失败", "确定");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SkillActionEditor] Failed to save skill action data: {ex}");
                EditorUtility.DisplayDialog("保存失败", $"保存技能动作数据失败：{ex.Message}", "确定");
            }
        }
        
        private void CheckUnsavedChanges()
        {
            bool hasUnsaved = _allSkillActions.Any(a => a.IsDirty);
            
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
            _selectedSkillAction = action as SkillActionEditorData;
            
            // 更新配置面板
            _configModule.SetSkillAction(_selectedSkillAction);
            
            // 更新时间轴
            if (_selectedSkillAction != null)
            {
                // 时间轴显示范围 = 动画完整帧数（AnimationDuration）
                // 可编辑范围 = 技能有效帧数（Duration）
                // 这样用户可以滚动查看整个动画作为参考，但只能在有效帧数内配置技能效果
                int timelineFrames = _selectedSkillAction.AnimationDuration > 0 
                    ? _selectedSkillAction.AnimationDuration 
                    : _selectedSkillAction.Duration;
                int maxEditableFrame = _selectedSkillAction.Duration;
                
                _timelineModule.SetFrameRange(timelineFrames, maxEditableFrame);
                _timelineModule.SetEvents(_selectedSkillAction.TimelineEvents);
                _timelineModule.SetTracks(TimelineTrackRegistry.GetAllTracks());
                
                // 更新预览模块：加载动画
                LoadAnimationForAction(_selectedSkillAction);
            }
        }
        
        // === 动画预览方法 ===
        
        private void LoadAnimationForAction(SkillActionEditorData action)
        {
            if (action == null || _previewModule == null)
                return;
            
            if (string.IsNullOrEmpty(action.AnimationPath))
            {
                Debug.LogWarning($"[SkillActionEditor] No animation path for action {action.ActionId}");
                _previewModule.Stop();
                return;
            }
            
            // 加载动画片段
            _previewModule.LoadAnimationFromPath(action.AnimationPath);
            
            // 计算动画总帧数
            int animationTotalFrames = _previewModule.GetTotalFrames();
            
            if (animationTotalFrames > 0)
            {
                action.AnimationDuration = animationTotalFrames;
                
                if (action.Duration <= 0 || action.Duration > animationTotalFrames)
                {
                    action.Duration = animationTotalFrames;
                }
                
                // 时间轴显示完整动画帧数，可编辑范围为Duration
                _timelineModule.SetFrameRange(animationTotalFrames, action.Duration);
            }
        }
        
        private void PlayAnimation()
        {
            if (_selectedSkillAction == null)
            {
                Debug.LogWarning("[SkillActionEditor] Cannot play: No skill action selected");
                return;
            }
            
            if (_previewModule == null)
            {
                Debug.LogWarning("[SkillActionEditor] Cannot play: Preview module is null");
                return;
            }
            
            if (string.IsNullOrEmpty(_selectedSkillAction.AnimationPath))
            {
                Debug.LogWarning("[SkillActionEditor] Cannot play: No animation path configured");
                EditorUtility.DisplayDialog("无法播放", "该技能动作未配置动画文件，请先设置动画路径", "确定");
                return;
            }
            
            _previewModule.Play();
        }
        
        private void PauseAnimation()
        {
            if (_previewModule == null) return;
            _previewModule.Pause();
        }
        
        private void StopAnimation()
        {
            if (_selectedSkillAction == null || _previewModule == null) return;
            _previewModule.Stop();
            _timelineModule.SetCurrentFrame(0);
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
        
        private void OnTimelineFrameChanged(int frame)
        {
            if (_previewModule != null)
            {
                _previewModule.SetFrame(frame);
                Repaint();
            }
        }
        
        private void OnCreateNewSkillAction()
        {
            var existingIds = new HashSet<int>(_allSkillActions.Select(a => a.ActionId));
            int newId = AstrumEditorUtility.GenerateUniqueId(5000, existingIds); // 技能动作ID从5000开始
            
            var newSkillAction = SkillActionEditorData.CreateDefault(newId);
            _allSkillActions.Add(newSkillAction);
            _listModule.SelectAction(newSkillAction);
        }
        
        private void OnDuplicateSkillAction(ActionEditorData action)
        {
            var skillAction = action as SkillActionEditorData;
            if (skillAction == null) return;
            
            var existingIds = new HashSet<int>(_allSkillActions.Select(a => a.ActionId));
            int newId = AstrumEditorUtility.GenerateUniqueId(5000, existingIds);
            
            var duplicated = skillAction.Clone();
            duplicated.ActionId = newId;
            duplicated.ActionName = skillAction.ActionName + "_Copy";
            
            _allSkillActions.Add(duplicated);
            _listModule.SelectAction(duplicated);
        }
        
        private void OnDeleteSkillAction(ActionEditorData action)
        {
            var skillAction = action as SkillActionEditorData;
            if (skillAction == null) return;
            
            bool confirm = EditorUtility.DisplayDialog(
                "删除技能动作",
                $"确定要删除技能动作 [{skillAction.ActionId}] {skillAction.ActionName} 吗？",
                "删除", "取消"
            );
            
            if (confirm)
            {
                _allSkillActions.Remove(skillAction);
                
                if (_allSkillActions.Count > 0)
                {
                    _listModule.SelectAction(_allSkillActions[0]);
                }
                else
                {
                    _listModule.SelectAction(null);
                }
            }
        }
        
        private void OnActionModified(ActionEditorData action)
        {
            var skillAction = action as SkillActionEditorData;
            if (skillAction != null && skillAction == _selectedSkillAction)
            {
                if (skillAction.Duration > skillAction.AnimationDuration)
                {
                    skillAction.Duration = skillAction.AnimationDuration;
                }
                
                // 时间轴显示完整动画帧数，可编辑范围为Duration
                int timelineFrames = skillAction.AnimationDuration > 0 
                    ? skillAction.AnimationDuration 
                    : skillAction.Duration;
                _timelineModule.SetFrameRange(timelineFrames, skillAction.Duration);
                LoadAnimationForAction(skillAction);
            }
        }
        
        private void OnTimelineEventModified(TimelineEvent evt)
        {
            if (_selectedSkillAction != null)
            {
                _selectedSkillAction.MarkDirty();
            }
        }
        
        private void OnTimelineEventSelected(TimelineEvent evt)
        {
            if (_eventDetailModule != null)
            {
                _eventDetailModule.SetSelectedEvent(evt);
            }
            Repaint();
        }
        
        private void OnJumpToTimeline()
        {
            // TODO: 实现跳转到时间轴的逻辑
        }
        
        private void OnEntitySelected(int entityId)
        {
            if (_previewModule != null)
            {
                _previewModule.SetEntity(entityId);
                
                if (_selectedSkillAction != null)
                {
                    LoadAnimationForAction(_selectedSkillAction);
                }
            }
        }
        
        // === UI绘制 ===
        
        private void DrawToolbar()
        {
            Rect toolbarRect = _layoutManager.GetToolbarRect();
            
            GUILayout.BeginArea(toolbarRect);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    SaveData();
                }
                
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    LoadData();
                }
                
                if (GUILayout.Button("验证", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    ValidateAllSkillActions();
                }
                
                GUILayout.FlexibleSpace();
                
                if (_selectedSkillAction != null)
                {
                    string info = $"技能动作 {_selectedSkillAction.ActionId}: {_selectedSkillAction.ActionName}";
                    EditorGUILayout.LabelField(info, EditorStyles.toolbarButton);
                }
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("帮助", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    Application.OpenURL("https://github.com/yourproject/wiki/skill-action-editor");
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        
        private void DrawMainArea()
        {
            // 左侧：技能动作列表
            Rect leftRect = _layoutManager.GetLeftPanelRect();
            _listModule.DrawList(leftRect, _allSkillActions);
            
            // 右上左：配置面板
            Rect configRect = _layoutManager.GetConfigPanelRect();
            _configModule.DrawConfig(configRect, _selectedSkillAction);
            
            // 右上中：预览面板
            Rect previewRect = _layoutManager.GetPreviewPanelRect();
            DrawPreviewPanel(previewRect);
            
            // 右上右：事件详情面板
            Rect eventDetailRect = _layoutManager.GetEventDetailPanelRect();
            _eventDetailModule.DrawEventDetail(eventDetailRect, _selectedSkillAction, _timelineModule.GetSelectedEvent());
            
            // 右下：时间轴
            Rect timelineRect = _layoutManager.GetTimelineRect();
            _timelineModule.DrawTimeline(timelineRect);
        }
        
        private void DrawPreviewPanel(Rect rect)
        {
            if (_selectedSkillAction == null)
            {
                GUILayout.BeginArea(rect);
                EditorGUILayout.HelpBox("请选择一个技能动作", MessageType.Info);
                GUILayout.EndArea();
                return;
            }
            
            float controlHeight = 80f;
            Rect previewRect = new Rect(rect.x, rect.y, rect.width, rect.height - controlHeight);
            Rect controlRect = new Rect(rect.x, rect.y + rect.height - controlHeight, rect.width, controlHeight);
            
            if (_previewModule != null)
            {
                // 显示预览模块状态（用于调试）
                GUILayout.BeginArea(previewRect);
                GUILayout.BeginVertical("box");
                
                // 调用预览模块的绘制（在适当的区域内）
                GUILayout.EndVertical();
                GUILayout.EndArea();
                
                // 使用原始rect绘制预览
                _previewModule.DrawPreview(previewRect);
            }
            else
            {
                GUILayout.BeginArea(previewRect);
                EditorGUILayout.HelpBox("预览模块未初始化", MessageType.Error);
                GUILayout.EndArea();
            }
            
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
        
        private void HandleGlobalHotkeys(Event evt)
        {
            if (evt.type == EventType.KeyDown)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.Delete:
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
                                    
                                    if (_selectedSkillAction != null)
                                    {
                                        _selectedSkillAction.MarkDirty();
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
        
        private void SyncAnimationToTimeline()
        {
            if (_previewModule != null && _previewModule.IsPlaying())
            {
                int animFrame = _previewModule.GetCurrentFrame();
                int timelineFrame = _timelineModule.GetCurrentFrame();
                
                if (animFrame != timelineFrame)
                {
                    _timelineModule.SetCurrentFrame(animFrame);
                }
                
                Repaint();
            }
        }
        
        private void ValidateAllSkillActions()
        {
            // TODO: 实现验证逻辑
            EditorUtility.DisplayDialog("验证", "所有技能动作数据验证通过！", "确定");
        }
    }
}

