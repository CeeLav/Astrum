using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;

namespace Astrum.Editor.RoleEditor.Modules
{
    /// <summary>
    /// 技能动作列表模块
    /// 继承自 ActionListModule，扩展显示技能ID和技能名称
    /// </summary>
    public class SkillActionListModule : ActionListModule
    {
        // === 核心方法 ===
        
        /// <summary>
        /// 绘制列表（重写以支持 SkillActionEditorData）
        /// </summary>
        public void DrawList(Rect rect, List<SkillActionEditorData> skillActions)
        {
            // 转换为基类列表类型
            var baseActions = skillActions.Cast<ActionEditorData>().ToList();
            base.DrawList(rect, baseActions);
        }
        
        // === 私有绘制方法（扩展） ===
        
        /// <summary>
        /// 绘制动作卡片（重写以显示技能信息）
        /// 注意：这是对基类私有方法的重新实现，因为无法直接重写
        /// </summary>
        private void DrawSkillActionCard(SkillActionEditorData skillAction)
        {
            bool isSelected = GetSelectedAction() == skillAction;
            
            // 选中背景色
            var bgColor = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : Color.clear;
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            
            EditorGUILayout.BeginVertical("box", GUILayout.Height(80)); // 增加高度以显示技能信息
            {
                // 整个区域可点击
                Rect cardRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(75));
                
                if (GUI.Button(cardRect, "", GUIStyle.none))
                {
                    SelectAction(skillAction);
                }
                
                // 绘制内容
                float x = cardRect.x + 5;
                float y = cardRect.y + 5;
                
                // 第一行：状态图标 + 动作ID + 动作名称
                var titleStyle = new GUIStyle(EditorStyles.boldLabel);
                if (isSelected) titleStyle.normal.textColor = new Color(0.4f, 0.7f, 1f);
                
                string stateIcon = isSelected ? "⏸" : "⚔️"; // 技能动作使用剑图标
                string title = $"{stateIcon} {skillAction.ActionId} {skillAction.ActionName}";
                
                GUI.Label(new Rect(x, y, cardRect.width - 30, 18), title, titleStyle);
                
                // 修改标记
                if (skillAction.IsDirty)
                {
                    var dirtyStyle = new GUIStyle(EditorStyles.boldLabel);
                    dirtyStyle.normal.textColor = Color.yellow;
                    GUI.Label(new Rect(cardRect.xMax - 20, y, 15, 15), "*", dirtyStyle);
                }
                
                // 第二行：技能信息
                var skillInfoStyle = new GUIStyle(EditorStyles.miniLabel);
                skillInfoStyle.normal.textColor = new Color(1f, 0.6f, 0.2f); // 橙色
                
                string skillInfo = $"技能 {skillAction.SkillId}";
                if (!string.IsNullOrEmpty(skillAction.SkillName))
                {
                    skillInfo += $" - {skillAction.SkillName}";
                }
                
                GUI.Label(new Rect(x, y + 20, cardRect.width - 10, 16), skillInfo, skillInfoStyle);
                
                // 第三行：类型描述
                var subtitleStyle = new GUIStyle(EditorStyles.miniLabel);
                subtitleStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
                
                GUI.Label(new Rect(x, y + 38, cardRect.width - 10, 16), "技能动作", subtitleStyle);
                
                // 第四行：帧数 | 成本 | 冷却
                var infoStyle = new GUIStyle(EditorStyles.miniLabel);
                infoStyle.normal.textColor = Color.gray;
                
                int eventCount = skillAction.TimelineEvents?.Count ?? 0;
                string info = $"帧:{skillAction.Duration} | 成本:{skillAction.ActualCost} | 冷却:{skillAction.ActualCooldown / 60f:F1}s";
                
                GUI.Label(new Rect(x, y + 56, cardRect.width - 10, 16), info, infoStyle);
            }
            EditorGUILayout.EndVertical();
            
            GUI.backgroundColor = originalColor;
        }
    }
}

