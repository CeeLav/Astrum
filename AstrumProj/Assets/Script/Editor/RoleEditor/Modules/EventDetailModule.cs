using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Timeline;

namespace Astrum.Editor.RoleEditor.Modules
{
    /// <summary>
    /// 事件详情面板模块
    /// 显示时间轴事件统计和选中事件的详细信息
    /// </summary>
    public class EventDetailModule
    {
        // === 数据 ===
        private ActionEditorData _currentAction;
        private TimelineEvent _selectedEvent;
        private Vector2 _scrollPosition;
        
        // === 折叠状态 ===
        private bool _statisticsFoldout = true;
        private bool _eventDetailFoldout = true;
        
        // === 事件 ===
        public event Action<ActionEditorData> OnActionModified;
        public event Action<TimelineEvent> OnEventModified;
        
        // === 核心方法 ===
        
        /// <summary>
        /// 绘制事件详情面板
        /// </summary>
        public void DrawEventDetail(Rect rect, ActionEditorData action, TimelineEvent selectedEvent)
        {
            _currentAction = action;
            _selectedEvent = selectedEvent;
            
            GUILayout.BeginArea(rect);
            
            if (action == null)
            {
                EditorGUILayout.HelpBox("请选择一个动作", MessageType.Info);
                GUILayout.EndArea();
                return;
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                // 绘制时间轴事件统计
                DrawEventStatistics();
                
                EditorGUILayout.Space(10);
                
                // 绘制选中事件详情
                DrawSelectedEventDetail();
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
        }
        
        /// <summary>
        /// 设置选中的事件
        /// </summary>
        public void SetSelectedEvent(TimelineEvent evt)
        {
            _selectedEvent = evt;
        }
        
        // === 私有绘制方法 ===
        
        /// <summary>
        /// 绘制时间轴事件统计
        /// </summary>
        private void DrawEventStatistics()
        {
            _statisticsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_statisticsFoldout, "📊 时间轴统计");
            
            if (_statisticsFoldout)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    if (_currentAction == null)
                    {
                        EditorGUILayout.LabelField("无数据", EditorStyles.miniLabel);
                    }
                    else
                    {
                        var stats = _currentAction.GetEventStatistics();
                        
                        if (stats == null || stats.Count == 0)
                        {
                            EditorGUILayout.LabelField("暂无时间轴事件", EditorStyles.miniLabel);
                        }
                        else
                        {
                            // 被取消标签
                            DrawEventStat("🚫 被取消标签", GetStatValue(stats, "BeCancelTag"), new Color(0.8f, 0.3f, 0.3f));
                            
                            // 特效
                            DrawEventStat("✨ 特效", GetStatValue(stats, "VFX"), new Color(0.8f, 0.4f, 1f));
                            
                            // 音效
                            DrawEventStat("🔊 音效", GetStatValue(stats, "SFX"), new Color(1f, 0.7f, 0.2f));
                            
                            // 相机震动
                            DrawEventStat("📷 相机震动", GetStatValue(stats, "CameraShake"), new Color(0.6f, 0.6f, 0.6f));
                            
                            // 技能效果
                            DrawEventStat("💥 技能效果", GetStatValue(stats, "SkillEffect"), new Color(1f, 0.3f, 0.3f));
                            
                            EditorGUILayout.Space(5);
                            
                            // 总计
                            int total = 0;
                            foreach (var kvp in stats)
                            {
                                total += kvp.Value;
                            }
                            
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.LabelField("总计", EditorStyles.boldLabel, GUILayout.Width(100));
                                EditorGUILayout.LabelField(total.ToString(), EditorStyles.boldLabel);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// 绘制单个事件统计
        /// </summary>
        private void DrawEventStat(string label, int count, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(120));
                
                // 颜色标记
                Color oldColor = GUI.color;
                GUI.color = color;
                EditorGUILayout.LabelField("●", GUILayout.Width(20));
                GUI.color = oldColor;
                
                EditorGUILayout.LabelField(count.ToString(), EditorStyles.boldLabel);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 获取统计值
        /// </summary>
        private int GetStatValue(Dictionary<string, int> dict, string key)
        {
            return dict != null && dict.ContainsKey(key) ? dict[key] : 0;
        }
        
        /// <summary>
        /// 绘制选中事件详情
        /// </summary>
        private void DrawSelectedEventDetail()
        {
            _eventDetailFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_eventDetailFoldout, "📌 选中事件详情");
            
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
                        DrawEventInfo();
                        
                        EditorGUILayout.Space(10);
                        
                        DrawEventEditor();
                    }
                }
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// 绘制事件基本信息
        /// </summary>
        private void DrawEventInfo()
        {
            if (_selectedEvent == null) return;
            
            // 标题
            EditorGUILayout.LabelField("事件信息", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            {
                // 事件类型
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("类型:", GUILayout.Width(80));
                    
                    // 根据类型显示不同颜色
                    Color typeColor = GetTrackColor(_selectedEvent.TrackType);
                    Color oldColor = GUI.color;
                    GUI.color = typeColor;
                    EditorGUILayout.LabelField($"● {_selectedEvent.TrackType}", EditorStyles.boldLabel);
                    GUI.color = oldColor;
                }
                EditorGUILayout.EndHorizontal();
                
                // 事件ID
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("事件ID:", GUILayout.Width(80));
                    EditorGUILayout.LabelField(_selectedEvent.EventId, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();
                
                // 帧范围
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("帧范围:", GUILayout.Width(80));
                    string frameRange = _selectedEvent.IsSingleFrame() 
                        ? $"帧 {_selectedEvent.StartFrame}" 
                        : $"帧 {_selectedEvent.StartFrame} - {_selectedEvent.EndFrame}";
                    EditorGUILayout.LabelField(frameRange, EditorStyles.boldLabel);
                }
                EditorGUILayout.EndHorizontal();
                
                // 持续时间
                if (!_selectedEvent.IsSingleFrame())
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("持续时间:", GUILayout.Width(80));
                        int duration = _selectedEvent.GetDuration();
                        EditorGUILayout.LabelField($"{duration} 帧 ({duration / 60f:F2} 秒)", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                // 显示名称
                if (!string.IsNullOrEmpty(_selectedEvent.DisplayName))
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("名称:", GUILayout.Width(80));
                        EditorGUILayout.LabelField(_selectedEvent.DisplayName);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制事件编辑器
        /// </summary>
        private void DrawEventEditor()
        {
            if (_selectedEvent == null) return;
            
            EditorGUILayout.LabelField("事件配置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            {
                // 获取轨道配置
                var track = TimelineTrackRegistry.GetTrack(_selectedEvent.TrackType);
                
                if (track != null && track.EventEditor != null)
                {
                    // 调用轨道专属的事件编辑器
                    bool modified = track.EventEditor(_selectedEvent);
                    
                    if (modified)
                    {
                        _currentAction?.MarkDirty();
                        OnActionModified?.Invoke(_currentAction);
                        OnEventModified?.Invoke(_selectedEvent);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        $"轨道类型 '{_selectedEvent.TrackType}' 没有配置编辑器", 
                        MessageType.Warning
                    );
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 获取轨道颜色
        /// </summary>
        private Color GetTrackColor(string trackType)
        {
            var track = TimelineTrackRegistry.GetTrack(trackType);
            return track != null ? track.TrackColor : Color.gray;
        }
    }
}


