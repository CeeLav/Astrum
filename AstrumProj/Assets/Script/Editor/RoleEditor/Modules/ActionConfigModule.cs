using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Astrum.Editor.RoleEditor.Data;

namespace Astrum.Editor.RoleEditor.Modules
{
    /// <summary>
    /// 动作配置面板模块
    /// 显示和编辑动作配置
    /// </summary>
    public class ActionConfigModule
    {
        // === 数据 ===
        private ActionEditorData _currentAction;
        private Vector2 _scrollPosition;
        private PropertyTree _propertyTree;
        
        // === 事件编辑 ===
        private Timeline.TimelineEvent _selectedEvent;
        
        // === 折叠状态 ===
        private bool _basicInfoFoldout = true;
        private bool _actionConfigFoldout = true;
        private bool _cancelTagFoldout = true;
        private bool _eventStatsFoldout = true;
        private bool _eventDetailFoldout = true;
        
        // === 事件 ===
        public event Action<ActionEditorData> OnActionModified;
        public event Action OnJumpToTimeline;
        
        // === 核心方法 ===
        
        /// <summary>
        /// 绘制配置面板
        /// </summary>
        public void DrawConfig(Rect rect, ActionEditorData action)
        {
            GUILayout.BeginArea(rect);
            
            if (action == null)
            {
                EditorGUILayout.HelpBox("请选择一个动作", MessageType.Info);
                GUILayout.EndArea();
                return;
            }
            
            // 如果动作变了，重建PropertyTree
            if (_currentAction != action)
            {
                SetAction(action);
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                DrawOdinInspector();
                DrawAnimationStatusCheck();
                EditorGUILayout.Space(5);
                DrawCancelTagSection();
                DrawEventStatisticsSection();
                EditorGUILayout.Space(5);
                DrawEventDetailSection();
            }
            EditorGUILayout.EndScrollView();
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// 设置当前动作
        /// </summary>
        public void SetAction(ActionEditorData action)
        {
            _currentAction = action;
            _selectedEvent = null; // 切换动作时清除事件选中
            
            // 重建PropertyTree
            _propertyTree?.Dispose();
            if (_currentAction != null)
            {
                _propertyTree = PropertyTree.Create(_currentAction);
            }
        }
        
        /// <summary>
        /// 设置选中的时间轴事件
        /// </summary>
        public void SetSelectedEvent(Timeline.TimelineEvent evt)
        {
            _selectedEvent = evt;
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            _propertyTree?.Dispose();
            _propertyTree = null;
        }
        
        // === 私有绘制方法 ===
        
        private void DrawOdinInspector()
        {
            if (_propertyTree == null) return;
            
            // 使用 Odin 绘制基础信息和动作配置
            _propertyTree.UpdateTree();
            _propertyTree.BeginDraw(true);
            
            foreach (var property in _propertyTree.EnumerateTree(false))
            {
                // 只绘制带 TitleGroup 的属性（基础信息和动作配置）
                if (property.Info.GetAttribute<TitleGroupAttribute>() != null || 
                    property.Parent?.Info.GetAttribute<TitleGroupAttribute>() != null)
                {
                    property.Draw();
                }
            }
            
            _propertyTree.EndDraw();
            
            // 应用修改
            if (_propertyTree.ApplyChanges())
            {
                _currentAction.MarkDirty();
                EditorUtility.SetDirty(_currentAction);
                OnActionModified?.Invoke(_currentAction);
            }
        }
        
        private void DrawAnimationStatusCheck()
        {
            if (_currentAction == null) return;
            
            // 检查动画路径是否有效
            if (string.IsNullOrEmpty(_currentAction.AnimationPath))
            {
                EditorGUILayout.HelpBox(
                    "⚠️ 未设置动画路径，请先配置动画文件才能正常使用此动作", 
                    MessageType.Warning
                );
                return;
            }
            
            // 检查动画文件是否存在
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(_currentAction.AnimationPath);
            if (clip == null)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ 动画文件不存在: {_currentAction.AnimationPath}", 
                    MessageType.Error
                );
                return;
            }
            
            // 检查动作帧数是否超过动画帧数
            if (_currentAction.Duration > _currentAction.AnimationDuration)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ 动作总帧数({_currentAction.Duration})超过了动画总帧数({_currentAction.AnimationDuration})", 
                    MessageType.Warning
                );
                
                if (GUILayout.Button("自动修正为动画总帧数"))
                {
                    _currentAction.Duration = _currentAction.AnimationDuration;
                    _currentAction.MarkDirty();
                    OnActionModified?.Invoke(_currentAction);
                }
            }
        }
        
        private void DrawCancelTagSection()
        {
            _cancelTagFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_cancelTagFoldout, "取消标签配置");
            
            if (_cancelTagFoldout)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    // CancelTags 编辑
                    EditorGUILayout.LabelField("CancelTags (取消其他动作的标签):", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("此动作可以取消带有这些标签的其他动作", MessageType.Info);
                    
                    EditorGUI.BeginChangeCheck();
                    string newCancelTags = EditorGUILayout.TextField("标签列表 (逗号分隔)", _currentAction.CancelTags ?? "");
                    if (EditorGUI.EndChangeCheck())
                    {
                        _currentAction.CancelTags = newCancelTags;
                        _currentAction.MarkDirty();
                        OnActionModified?.Invoke(_currentAction);
                    }
                    
                    // 显示标签预览
                    if (!string.IsNullOrEmpty(_currentAction.CancelTags))
                    {
                        string[] tags = _currentAction.CancelTags.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (tags.Length > 0)
                        {
                            EditorGUILayout.LabelField($"当前标签: {string.Join(", ", tags.Select(t => t.Trim()))}");
                        }
                    }
                    
                    EditorGUILayout.Space(10);
                    
                    // BeCancelledTags 提示
                    EditorGUILayout.LabelField("BeCancelledTags (被取消标签区间):", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("此动作在哪些区间可以被其他动作取消", MessageType.Info);
                    
                    int beCancelCount = _currentAction.GetEventCount("BeCancelTag");
                    EditorGUILayout.LabelField($"共 {beCancelCount} 个被取消标签区间");
                    
                    if (GUILayout.Button("📋 在时间轴编辑被取消标签", GUILayout.Height(30)))
                    {
                        OnJumpToTimeline?.Invoke();
                    }
                    
                    EditorGUILayout.Space(5);
                    
                    // 说明
                    EditorGUILayout.HelpBox(
                        "💡 提示：\n" +
                        "• CancelTags：此动作可以取消的标签（静态配置）\n" +
                        "• BeCancelledTags：此动作可被取消的区间（时间轴编辑）\n" +
                        "• TempBeCancelledTags：运行时动态数据（不在编辑器配置）",
                        MessageType.None
                    );
                }
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private void DrawEventStatisticsSection()
        {
            _eventStatsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_eventStatsFoldout, "时间轴事件统计");
            
            if (_eventStatsFoldout)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    var stats = _currentAction.GetEventStatistics();
                    
                    DrawEventStat("🚫 被取消标签", GetStatValue(stats, "BeCancelTag"));
                    DrawEventStat("✨ 特效", GetStatValue(stats, "VFX"));
                    DrawEventStat("🔊 音效", GetStatValue(stats, "SFX"));
                    DrawEventStat("📷 相机震动", GetStatValue(stats, "CameraShake"));
                    
                    // 注意：TempBeCancelledTags 不显示（运行时数据）
                    
                    EditorGUILayout.Space(5);
                    
                    if (GUILayout.Button("跳转到时间轴", GUILayout.Height(30)))
                    {
                        OnJumpToTimeline?.Invoke();
                    }
                }
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private void DrawEventStat(string label, int count)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(120));
                EditorGUILayout.LabelField(count.ToString(), EditorStyles.boldLabel, GUILayout.Width(40));
            }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 从字典中获取值，如果键不存在则返回默认值
        /// </summary>
        private int GetStatValue(Dictionary<string, int> dict, string key)
        {
            return dict != null && dict.ContainsKey(key) ? dict[key] : 0;
        }
        
        // === 事件详情编辑 ===
        
        private void DrawEventDetailSection()
        {
            _eventDetailFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_eventDetailFoldout, "选中事件详情");
            
            if (_eventDetailFoldout)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    if (_selectedEvent == null)
                    {
                        EditorGUILayout.HelpBox("请在时间轴中选择一个事件", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"事件类型: {_selectedEvent.TrackType}", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"事件ID: {_selectedEvent.EventId}");
                        EditorGUILayout.LabelField($"帧范围: {_selectedEvent.StartFrame} - {_selectedEvent.EndFrame}");
                        
                        EditorGUILayout.Space(10);
                        
                        // 调用对应轨道的编辑器
                        var track = Timeline.TimelineTrackRegistry.GetTrack(_selectedEvent.TrackType);
                        if (track != null && track.EventEditor != null)
                        {
                            bool modified = track.EventEditor(_selectedEvent);
                            
                            if (modified)
                            {
                                // 标记动作为已修改
                                _currentAction?.MarkDirty();
                                OnActionModified?.Invoke(_currentAction);
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("此事件类型没有编辑器", MessageType.Warning);
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
