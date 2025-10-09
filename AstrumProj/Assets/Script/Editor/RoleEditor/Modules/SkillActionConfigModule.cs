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
        private bool _skillInfoFoldout = true;
        private bool _skillCostFoldout = true;
        private bool _attackBoxFoldout = true;
        private bool _triggerFramesFoldout = true;
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
                DrawSkillInfo();
                DrawSkillCost();
                DrawAttackBoxes();
                DrawTriggerFramesRaw();
                
                EditorGUILayout.Space(5);
                
                // 绘制取消标签和时间轴统计
                DrawCancelTagSection();
                DrawEventStatisticsSection();
                
                EditorGUILayout.Space(5);
                
                // 绘制事件详情
                DrawEventDetailSection();
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
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
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
        
        private void DrawCancelTagSection()
        {
            _cancelTagFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_cancelTagFoldout, "取消标签配置");
            
            if (_cancelTagFoldout)
            {
                EditorGUILayout.LabelField("BeCancelledTags (被取消标签区间):", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("此动作在哪些区间可以被其他动作取消", MessageType.Info);
                
                int beCancelCount = _currentSkillAction.GetEventCount("BeCancelTag");
                EditorGUILayout.LabelField($"共 {beCancelCount} 个被取消标签区间");
                
                if (GUILayout.Button("📋 在时间轴编辑被取消标签", GUILayout.Height(30)))
                {
                    OnJumpToTimeline?.Invoke();
                }
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.HelpBox(
                    "💡 提示：\n" +
                    "• CancelTags：此动作可以取消的标签（在 Odin Inspector 中编辑）\n" +
                    "• BeCancelledTags：此动作可被取消的区间（在时间轴编辑，自动生成 JSON）\n" +
                    "• 数据自动与 CSV 同步，保存时写入 JSON 格式",
                    MessageType.None
                );
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
                    var stats = _currentSkillAction.GetEventStatistics();
                    
                    DrawEventStat("🚫 被取消标签", GetStatValue(stats, "BeCancelTag"));
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
        
        private int GetStatValue(Dictionary<string, int> dict, string key)
        {
            return dict != null && dict.ContainsKey(key) ? dict[key] : 0;
        }
        
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
                        
                        var track = Timeline.TimelineTrackRegistry.GetTrack(_selectedEvent.TrackType);
                        if (track != null && track.EventEditor != null)
                        {
                            bool modified = track.EventEditor(_selectedEvent);
                            
                            if (modified)
                            {
                                _currentSkillAction?.MarkDirty();
                                OnActionModified?.Invoke(_currentSkillAction);
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
        
        // === 私有绘制方法（技能专属） ===
        
        /// <summary>
        /// 绘制技能信息区域
        /// </summary>
        private void DrawSkillInfo()
        {
            if (_currentSkillAction == null) return;
            
            _skillInfoFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_skillInfoFoldout, "技能信息");
            
            if (_skillInfoFoldout)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    // 所属技能ID
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("所属技能ID:", GUILayout.Width(100));
                        EditorGUILayout.LabelField(_currentSkillAction.SkillId.ToString(), EditorStyles.boldLabel);
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    // 技能名称
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("技能名称:", GUILayout.Width(100));
                        
                        if (string.IsNullOrEmpty(_currentSkillAction.SkillName))
                        {
                            EditorGUILayout.LabelField("(未关联技能)", EditorStyles.miniLabel);
                        }
                        else
                        {
                            EditorGUILayout.LabelField(_currentSkillAction.SkillName, EditorStyles.boldLabel);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(5);
                    
                    // 跳转到技能编辑器按钮
                    if (GUILayout.Button("🔗 跳转到技能编辑器", GUILayout.Height(30)))
                    {
                        JumpToSkillEditor();
                    }
                    
                    EditorGUILayout.HelpBox(
                        "💡 提示：技能ID和名称从 SkillTable 读取，在技能编辑器中配置", 
                        MessageType.Info
                    );
                }
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
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
        
        /// <summary>
        /// 绘制攻击碰撞盒区域
        /// </summary>
        private void DrawAttackBoxes()
        {
            if (_currentSkillAction == null) return;
            
            _attackBoxFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_attackBoxFoldout, "攻击碰撞盒");
            
            if (_attackBoxFoldout)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("碰撞盒配置字符串", EditorStyles.boldLabel);
                    
                    EditorGUI.BeginChangeCheck();
                    string newAttackBox = EditorGUILayout.TextArea(
                        _currentSkillAction.AttackBoxInfo, 
                        GUILayout.Height(60)
                    );
                    if (EditorGUI.EndChangeCheck())
                    {
                        _currentSkillAction.AttackBoxInfo = newAttackBox;
                        _currentSkillAction.MarkDirty();
                        EditorUtility.SetDirty(_currentSkillAction);
                    }
                    
                    EditorGUILayout.Space(5);
                    
                    // 格式校验（Phase 7 实现）
                    if (!string.IsNullOrWhiteSpace(_currentSkillAction.AttackBoxInfo))
                    {
                        // TODO: Phase 7 - 实现格式校验
                        // bool isValid = ValidateAttackBoxFormat(_currentSkillAction.AttackBoxInfo);
                        // if (!isValid)
                        // {
                        //     EditorGUILayout.HelpBox("⚠️ 碰撞盒格式错误", MessageType.Error);
                        // }
                        
                        EditorGUILayout.HelpBox("💡 格式校验功能待 Phase 7 实现", MessageType.None);
                    }
                    
                    EditorGUILayout.HelpBox(
                        "💡 碰撞盒配置格式待定，用于定义技能的攻击判定范围", 
                        MessageType.Info
                    );
                }
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// 绘制触发帧原始字符串区域
        /// </summary>
        private void DrawTriggerFramesRaw()
        {
            if (_currentSkillAction == null) return;
            
            _triggerFramesFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_triggerFramesFoldout, "触发帧配置");
            
            if (_triggerFramesFoldout)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("触发帧字符串", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "格式: Frame5:Collision:4001,Frame10:Direct:4002,Frame15:Condition:4003", 
                        MessageType.Info
                    );
                    
                    EditorGUI.BeginChangeCheck();
                    string newTriggerFrames = EditorGUILayout.TextArea(
                        _currentSkillAction.TriggerFrames, 
                        GUILayout.Height(80)
                    );
                    if (EditorGUI.EndChangeCheck())
                    {
                        _currentSkillAction.TriggerFrames = newTriggerFrames;
                        
                        // 即时校验（Phase 7 实现）
                        ValidateTriggerFramesFormat(newTriggerFrames);
                        
                        _currentSkillAction.MarkDirty();
                        EditorUtility.SetDirty(_currentSkillAction);
                    }
                    
                    EditorGUILayout.Space(5);
                    
                    // 解析按钮
                    if (GUILayout.Button("🔄 解析触发帧", GUILayout.Height(25)))
                    {
                        _currentSkillAction.ParseTriggerFrames();
                        Debug.Log($"[SkillActionConfig] 解析触发帧: {_currentSkillAction.TriggerEffects.Count} 个");
                    }
                    
                    // 显示解析结果
                    if (_currentSkillAction.TriggerEffects.Count > 0)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField($"已解析 {_currentSkillAction.TriggerEffects.Count} 个触发帧:", EditorStyles.boldLabel);
                        
                        foreach (var effect in _currentSkillAction.TriggerEffects)
                        {
                            EditorGUILayout.LabelField(
                                $"  • 帧{effect.Frame}: {effect.TriggerType} -> 效果{effect.EffectId}", 
                                EditorStyles.miniLabel
                            );
                        }
                    }
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.HelpBox(
                        "💡 触发类型:\n" +
                        "• Collision - 碰撞触发\n" +
                        "• Direct - 直接触发\n" +
                        "• Condition - 条件触发\n\n" +
                        "Phase 3 将实现在时间轴上可视化编辑触发帧", 
                        MessageType.None
                    );
                }
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        // === 辅助方法 ===
        
        /// <summary>
        /// 跳转到技能编辑器
        /// </summary>
        private void JumpToSkillEditor()
        {
            if (_currentSkillAction == null || _currentSkillAction.SkillId == 0)
            {
                EditorUtility.DisplayDialog("提示", "该技能动作未关联技能ID", "确定");
                return;
            }
            
            // TODO: 打开技能编辑器并定位到指定技能
            Debug.Log($"[SkillActionConfig] 跳转到技能 {_currentSkillAction.SkillId}（功能待实现）");
            EditorUtility.DisplayDialog("提示", $"跳转到技能 {_currentSkillAction.SkillId}（功能待实现）", "确定");
        }
        
        /// <summary>
        /// 校验触发帧格式
        /// </summary>
        private void ValidateTriggerFramesFormat(string triggerFramesStr)
        {
            if (string.IsNullOrWhiteSpace(triggerFramesStr))
                return;
            
            // TODO: Phase 7 - 实现完整的格式校验
            // 暂时只做简单检查
            string[] frames = triggerFramesStr.Split(',');
            foreach (string frameStr in frames)
            {
                string trimmed = frameStr.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;
                
                string[] parts = trimmed.Split(':');
                if (parts.Length != 3)
                {
                    Debug.LogWarning($"[SkillActionConfig] 触发帧格式错误: {trimmed}");
                }
            }
        }
    }
}

