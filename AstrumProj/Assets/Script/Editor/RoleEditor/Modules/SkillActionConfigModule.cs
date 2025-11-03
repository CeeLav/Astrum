using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence;

namespace Astrum.Editor.RoleEditor.Modules
{
    /// <summary>
    /// 技能动作配置面板模块
    /// 不继承 ActionConfigModule，直接独立实现
    /// </summary>
    public class SkillActionConfigModule
    {
        // === 数据 ===
        private SkillActionEditorData _currentSkillAction;
        private Vector2 _scrollPosition;
        private PropertyTree _propertyTree;
        
        // === 事件编辑 ===
        private Timeline.TimelineEvent _selectedEvent;
        
        // === 折叠状态 ===
        private bool _skillCostFoldout = true;
        
        // === 事件 ===
        public event Action<ActionEditorData> OnActionModified;
        public event Action OnJumpToTimeline;
        
        // === 核心方法 ===
        
        /// <summary>
        /// 绘制配置面板
        /// </summary>
        public void DrawConfig(Rect rect, SkillActionEditorData skillAction)
        {
            GUILayout.BeginArea(rect);
            
            if (skillAction == null)
            {
                EditorGUILayout.HelpBox("请选择一个技能动作", MessageType.Info);
                GUILayout.EndArea();
                return;
            }
            
            // 如果动作变了，重建PropertyTree
            if (_currentSkillAction != skillAction)
            {
                SetSkillAction(skillAction);
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                // 绘制基础信息（使用 Odin Inspector）
                DrawOdinInspector();
                DrawAnimationSection();
                DrawAnimationStatusCheck();
                
                EditorGUILayout.Space(5);
                
                // 绘制技能专属内容
                DrawSkillCost();
            }
            EditorGUILayout.EndScrollView();
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// 设置当前技能动作
        /// </summary>
        public void SetSkillAction(SkillActionEditorData skillAction)
        {
            _currentSkillAction = skillAction;
            _selectedEvent = null;
            
            // 重建PropertyTree
            _propertyTree?.Dispose();
            if (_currentSkillAction != null)
            {
                _propertyTree = PropertyTree.Create(_currentSkillAction);
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
        
        // === 私有绘制方法（基础部分，复制自 ActionConfigModule） ===
        
        private void DrawOdinInspector()
        {
            if (_propertyTree == null) return;
            
            _propertyTree.UpdateTree();
            _propertyTree.BeginDraw(true);
            
            foreach (var property in _propertyTree.EnumerateTree(false))
            {
                // 只绘制带 TitleGroup 的属性
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
                _currentSkillAction.MarkDirty();
                EditorUtility.SetDirty(_currentSkillAction);
                OnActionModified?.Invoke(_currentSkillAction);
            }
        }
        
        private void DrawAnimationSection()
        {
            if (_currentSkillAction == null) return;
            
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("动画路径", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                string newPath = EditorGUILayout.TextField(_currentSkillAction.AnimationPath);
                if (EditorGUI.EndChangeCheck())
                {
                    _currentSkillAction.AnimationPath = newPath;
                    
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        _currentSkillAction.AnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newPath);
                    }
                    else
                    {
                        _currentSkillAction.AnimationClip = null;
                    }
                    
                    _currentSkillAction.MarkDirty();
                    EditorUtility.SetDirty(_currentSkillAction);
                    OnActionModified?.Invoke(_currentSkillAction);
                }
                
                EditorGUILayout.Space(3);
                
                EditorGUILayout.LabelField("动画文件", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                var newClip = EditorGUILayout.ObjectField(_currentSkillAction.AnimationClip, typeof(AnimationClip), false) as AnimationClip;
                if (EditorGUI.EndChangeCheck())
                {
                    _currentSkillAction.AnimationClip = newClip;
                    
                    if (newClip != null)
                    {
                        _currentSkillAction.AnimationPath = AssetDatabase.GetAssetPath(newClip);
                    }
                    else
                    {
                        _currentSkillAction.AnimationPath = "";
                    }
                    
                    _currentSkillAction.MarkDirty();
                    EditorUtility.SetDirty(_currentSkillAction);
                    OnActionModified?.Invoke(_currentSkillAction);
                }
                
                EditorGUILayout.HelpBox("💡 拖拽 AnimationClip 到上方字段自动更新路径", MessageType.None);
                
                EditorGUILayout.Space(5);
                
                // 提取位移数据按钮
                EditorGUILayout.BeginHorizontal();
                {
                    GUI.enabled = _currentSkillAction.AnimationClip != null;
                    if (GUILayout.Button("提取位移数据", GUILayout.Height(30)))
                    {
                        ExtractRootMotionData();
                    }
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();
                
                // 显示位移数据信息
                DrawRootMotionDataInfo();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// 提取根节点位移数据
        /// </summary>
        private void ExtractRootMotionData()
        {
            if (_currentSkillAction == null || _currentSkillAction.AnimationClip == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择动画文件", "确定");
                return;
            }
            
            try
            {
                var clip = _currentSkillAction.AnimationClip;
                _currentSkillAction.RootMotionDataArray = Astrum.Editor.RoleEditor.Services.AnimationRootMotionExtractor.ExtractRootMotionToIntArray(clip);
                
                if (_currentSkillAction.RootMotionDataArray != null && _currentSkillAction.RootMotionDataArray.Count > 0)
                {
                    int frameCount = _currentSkillAction.RootMotionDataArray[0];
                    EditorUtility.DisplayDialog("提取成功", 
                        $"已提取位移数据：\n帧数: {frameCount}\n数据大小: {_currentSkillAction.RootMotionDataArray.Count} 整数", 
                        "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "动画没有根节点位移数据", "确定");
                    _currentSkillAction.RootMotionDataArray = new List<int>();
                }
                
                _currentSkillAction.MarkDirty();
                EditorUtility.SetDirty(_currentSkillAction);
                OnActionModified?.Invoke(_currentSkillAction);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"提取位移数据失败：\n{ex.Message}", "确定");
                Debug.LogError($"[SkillActionConfigModule] Failed to extract root motion: {ex}");
            }
        }
        
        /// <summary>
        /// 显示根节点位移数据信息
        /// </summary>
        private void DrawRootMotionDataInfo()
        {
            if (_currentSkillAction == null) return;
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("位移数据信息", EditorStyles.boldLabel);
            
            if (_currentSkillAction.RootMotionDataArray == null || _currentSkillAction.RootMotionDataArray.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无位移数据", MessageType.Info);
                return;
            }
            
            int frameCount = _currentSkillAction.RootMotionDataArray[0];
            int dataSize = _currentSkillAction.RootMotionDataArray.Count;
            int expectedSize = 1 + frameCount * 7; // frameCount + (dx,dy,dz,rx,ry,rz,rw) * frameCount
            
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField($"帧数: {frameCount}", EditorStyles.label);
                EditorGUILayout.LabelField($"数据大小: {dataSize} 整数", EditorStyles.label);
                
                if (dataSize == expectedSize)
                {
                    EditorGUILayout.HelpBox("✓ 数据格式正确", MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox($"⚠️ 数据格式异常 (期望: {expectedSize}, 实际: {dataSize})", MessageType.Warning);
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAnimationStatusCheck()
        {
            if (_currentSkillAction == null) return;
            
            if (string.IsNullOrEmpty(_currentSkillAction.AnimationPath))
            {
                EditorGUILayout.HelpBox(
                    "⚠️ 未设置动画路径，请先配置动画文件才能正常使用此技能动作", 
                    MessageType.Warning
                );
                return;
            }
            
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(_currentSkillAction.AnimationPath);
            if (clip == null)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ 动画文件不存在: {_currentSkillAction.AnimationPath}", 
                    MessageType.Error
                );
                return;
            }
            
            if (_currentSkillAction.Duration > _currentSkillAction.AnimationDuration)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ 动作总帧数({_currentSkillAction.Duration})超过了动画总帧数({_currentSkillAction.AnimationDuration})", 
                    MessageType.Warning
                );
                
                if (GUILayout.Button("自动修正为动画总帧数"))
                {
                    _currentSkillAction.Duration = _currentSkillAction.AnimationDuration;
                    _currentSkillAction.MarkDirty();
                    OnActionModified?.Invoke(_currentSkillAction);
                }
            }
        }
        
        // === 私有绘制方法（技能专属） ===
        
        /// <summary>
        /// 绘制技能成本区域
        /// </summary>
        private void DrawSkillCost()
        {
            if (_currentSkillAction == null) return;
            
            _skillCostFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_skillCostFoldout, "技能成本");
            
            if (_skillCostFoldout)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    // 实际法力消耗
                    EditorGUILayout.LabelField("实际法力消耗", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    int newCost = EditorGUILayout.IntSlider(_currentSkillAction.ActualCost, 0, 1000);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _currentSkillAction.ActualCost = newCost;
                        _currentSkillAction.MarkDirty();
                        EditorUtility.SetDirty(_currentSkillAction);
                    }
                    
                    EditorGUILayout.Space(5);
                    
                    // 实际冷却（帧）
                    EditorGUILayout.LabelField("实际冷却（帧）", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    int newCooldown = EditorGUILayout.IntSlider(_currentSkillAction.ActualCooldown, 0, 3600);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _currentSkillAction.ActualCooldown = newCooldown;
                        _currentSkillAction.MarkDirty();
                        EditorUtility.SetDirty(_currentSkillAction);
                    }
                    
                    // 显示秒数提示
                    float seconds = _currentSkillAction.ActualCooldown / 60f;
                    EditorGUILayout.LabelField($"= {seconds:F2} 秒 (60帧 = 1秒)", EditorStyles.miniLabel);
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.HelpBox(
                        "💡 实际成本和冷却用于技能系统运行时计算，会覆盖技能表的基础值", 
                        MessageType.Info
                    );
                }
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}

