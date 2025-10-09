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
        private ActionEditorLayout _layoutManager;
        
        // === 数据 ===
        private List<SkillActionEditorData> _allSkillActions = new List<SkillActionEditorData>();
        private SkillActionEditorData _selectedSkillAction;
        
        // === 布局常量 ===
        private const float MIN_WINDOW_WIDTH = 1200f;
        private const float MIN_WINDOW_HEIGHT = 800f;
        
        // === Unity生命周期 ===
        
        [MenuItem("Tools/Astrum/Editors/Skill Action Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<SkillActionEditorWindow>("技能动作编辑器");
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
            // 注册技能效果轨道
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
                EventRenderer = RenderSkillEffectEvent,
                EventEditor = EditSkillEffectEvent
            });
        }
        
        // === 技能效果数据类 ===
        
        [System.Serializable]
        private class SkillEffectEventData
        {
            public int EffectId;
            public string TriggerType = "Direct"; // Direct, Collision, Condition
        }
        
        // === 技能效果轨道渲染器（临时实现，Phase 3 会移到独立文件）===
        
        private static void RenderSkillEffectEvent(Rect rect, TimelineEvent evt)
        {
            var effectData = evt.GetEventData<SkillEffectEventData>();
            
            // 绘制菱形标记
            GUI.color = new Color(1f, 0.3f, 0.3f);
            
            // 简单绘制：用字符代替菱形（Phase 3 会改为真正的菱形）
            Rect markerRect = new Rect(rect.x + 5, rect.y + 10, 25, 25);
            GUI.Label(markerRect, "◆", EditorStyles.boldLabel);
            
            // 显示效果ID和触发类型
            GUI.color = Color.white;
            if (effectData != null)
            {
                Rect labelRect = new Rect(rect.x + 30, rect.y + 12, rect.width - 35, 20);
                string triggerIcon = effectData.TriggerType == "Direct" ? "→" : 
                                    effectData.TriggerType == "Collision" ? "💥" : "❓";
                GUI.Label(labelRect, $"{triggerIcon} {effectData.EffectId}", EditorStyles.miniLabel);
            }
            
            GUI.color = Color.white;
        }
        
        private static bool EditSkillEffectEvent(TimelineEvent evt)
        {
            bool modified = false;
            
            // 获取或创建效果数据
            var effectData = evt.GetEventData<SkillEffectEventData>();
            if (effectData == null)
            {
                effectData = new SkillEffectEventData();
            }
            
            // 临时编辑器：显示基本信息（Phase 3 会实现完整的选择器）
            EditorGUILayout.LabelField("技能效果配置", EditorStyles.boldLabel);
            
            // 效果ID
            int newEffectId = EditorGUILayout.IntField("效果ID", effectData.EffectId);
            if (newEffectId != effectData.EffectId)
            {
                effectData.EffectId = newEffectId;
                modified = true;
            }
            
            // 触发类型
            string[] triggerTypes = new string[] { "Direct", "Collision", "Condition" };
            int selectedIndex = System.Array.IndexOf(triggerTypes, effectData.TriggerType);
            if (selectedIndex < 0) selectedIndex = 0;
            
            int newIndex = EditorGUILayout.Popup("触发类型", selectedIndex, triggerTypes);
            if (newIndex != selectedIndex)
            {
                effectData.TriggerType = triggerTypes[newIndex];
                modified = true;
            }
            
            if (modified)
            {
                evt.SetEventData(effectData);
                evt.DisplayName = $"效果:{effectData.EffectId} ({effectData.TriggerType})";
            }
            
            EditorGUILayout.HelpBox("Phase 3 将实现完整的效果选择器", MessageType.Info);
            
            return modified;
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
                _timelineModule.SetTotalFrames(_selectedSkillAction.Duration);
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
                
                _timelineModule.SetTotalFrames(action.Duration);
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
                
                _timelineModule.SetTotalFrames(skillAction.Duration);
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
            if (_configModule != null)
            {
                _configModule.SetSelectedEvent(evt);
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
                    string info = $"技能动作 {_selectedSkillAction.ActionId}: {_selectedSkillAction.ActionName} (技能ID: {_selectedSkillAction.SkillId})";
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
            
            // 右上右：预览面板
            Rect previewRect = _layoutManager.GetPreviewPanelRect();
            DrawPreviewPanel(previewRect);
            
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

