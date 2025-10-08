using System;
using System.Collections.Generic;
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
        
        // === 折叠状态 ===
        private bool _basicInfoFoldout = true;
        private bool _actionConfigFoldout = true;
        private bool _cancelTagFoldout = true;
        private bool _eventStatsFoldout = true;
        
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
                DrawCancelTagSection();
                DrawEventStatisticsSection();
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
            
            // 重建PropertyTree
            _propertyTree?.Dispose();
            if (_currentAction != null)
            {
                _propertyTree = PropertyTree.Create(_currentAction);
            }
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
        
        private void DrawCancelTagSection()
        {
            _cancelTagFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_cancelTagFoldout, "取消标签配置");
            
            if (_cancelTagFoldout)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("CancelTags:", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("当前版本暂不支持编辑，请在时间轴查看", MessageType.Info);
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.LabelField("BeCancelledTags:", EditorStyles.boldLabel);
                    int beCancelCount = _currentAction.GetEventCount("BeCancelTag");
                    EditorGUILayout.LabelField($"共 {beCancelCount} 个区间");
                    
                    if (GUILayout.Button("在时间轴编辑", GUILayout.Height(25)))
                    {
                        OnJumpToTimeline?.Invoke();
                    }
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.LabelField("TempBeCancelledTags:", EditorStyles.boldLabel);
                    int tempCancelCount = _currentAction.GetEventCount("TempBeCancelTag");
                    EditorGUILayout.LabelField($"共 {tempCancelCount} 个临时点");
                    
                    if (GUILayout.Button("在时间轴编辑", GUILayout.Height(25)))
                    {
                        OnJumpToTimeline?.Invoke();
                    }
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
                    DrawEventStat("⏱ 临时取消", GetStatValue(stats, "TempBeCancelTag"));
                    DrawEventStat("✨ 特效", GetStatValue(stats, "VFX"));
                    DrawEventStat("🔊 音效", GetStatValue(stats, "SFX"));
                    DrawEventStat("📷 相机震动", GetStatValue(stats, "CameraShake"));
                    
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
    }
}
