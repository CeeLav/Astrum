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
    /// 继承自 ActionConfigModule，扩展技能专属配置
    /// </summary>
    public class SkillActionConfigModule : ActionConfigModule
    {
        // === 数据 ===
        private SkillActionEditorData _currentSkillAction;
        
        // === 折叠状态 ===
        private bool _skillInfoFoldout = true;
        private bool _skillCostFoldout = true;
        private bool _attackBoxFoldout = true;
        private bool _triggerFramesFoldout = true;
        
        // === 核心方法 ===
        
        /// <summary>
        /// 绘制配置面板（重写）
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
            
            var scrollPosition = EditorGUILayout.BeginScrollView(Vector2.zero);
            {
                // 绘制基类内容（基础信息、动作配置等）
                base.DrawConfig(rect, skillAction);
                
                EditorGUILayout.Space(10);
                
                // 绘制技能专属内容
                DrawSkillInfo();
                DrawSkillCost();
                DrawAttackBoxes();
                DrawTriggerFramesRaw();
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
            base.SetAction(skillAction);
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

