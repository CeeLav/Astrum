using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;

namespace Astrum.Editor.RoleEditor.Modules
{
    /// <summary>
    /// 技能列表模块 - 显示和管理技能列表
    /// </summary>
    public class SkillListModule
    {
        // === 数据 ===
        private string _searchKeyword = "";
        private Vector2 _scrollPosition;
        private SkillEditorData _selectedSkill;
        private SkillTypeEnum _filterType = 0; // 0 = 全部
        
        // === 属性 ===
        public SkillEditorData SelectedSkill => _selectedSkill;
        
        // === 事件 ===
        public event Action<SkillEditorData> OnSkillSelected;
        public event Action OnCreateNew;
        public event Action<SkillEditorData> OnDuplicate;
        public event Action<SkillEditorData> OnDelete;
        
        /// <summary>
        /// 绘制列表
        /// </summary>
        public void DrawList(List<SkillEditorData> skills, float width)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(width));
            {
                DrawSearchBar();
                DrawFilterBar();
                DrawToolbar();
                DrawSkillList(skills);
            }
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 选择技能
        /// </summary>
        public void SelectSkill(SkillEditorData skill)
        {
            _selectedSkill = skill;
            OnSkillSelected?.Invoke(skill);
        }
        
        /// <summary>
        /// 绘制搜索栏
        /// </summary>
        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            {
                _searchKeyword = EditorGUILayout.TextField(_searchKeyword, EditorStyles.toolbarSearchField);
                
                if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
                {
                    _searchKeyword = "";
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制筛选栏
        /// </summary>
        private void DrawFilterBar()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("类型:", GUILayout.Width(40));
                
                var filterOptions = new string[] { "全部", "攻击", "控制", "位移", "Buff" };
                int selectedIndex = (int)_filterType;
                int newIndex = EditorGUILayout.Popup(selectedIndex, filterOptions);
                _filterType = (SkillTypeEnum)newIndex;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);
        }
        
        /// <summary>
        /// 绘制工具栏
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("新建", GUILayout.Height(25)))
                {
                    OnCreateNew?.Invoke();
                }
                
                GUI.enabled = _selectedSkill != null;
                
                if (GUILayout.Button("复制", GUILayout.Height(25)))
                {
                    OnDuplicate?.Invoke(_selectedSkill);
                }
                
                if (GUILayout.Button("删除", GUILayout.Height(25)))
                {
                    OnDelete?.Invoke(_selectedSkill);
                }
                
                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制技能列表
        /// </summary>
        private void DrawSkillList(List<SkillEditorData> skills)
        {
            var filteredSkills = GetFilteredSkills(skills);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                foreach (var skill in filteredSkills)
                {
                    DrawSkillItem(skill);
                    EditorGUILayout.Space(2);
                }
                
                if (filteredSkills.Count == 0)
                {
                    EditorGUILayout.HelpBox("没有找到匹配的技能", MessageType.Info);
                }
            }
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 绘制单个技能项
        /// </summary>
        private void DrawSkillItem(SkillEditorData skill)
        {
            bool isSelected = skill == _selectedSkill;
            
            // 选中背景色
            var bgColor = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : Color.clear;
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            
            EditorGUILayout.BeginVertical("box", GUILayout.Height(60));
            {
                // 点击选中
                if (GUILayout.Button("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    SelectSkill(skill);
                }
                
                Rect lastRect = GUILayoutUtility.GetLastRect();
                
                // 标题：[ID] 名称
                var titleStyle = new GUIStyle(EditorStyles.boldLabel);
                if (isSelected)
                    titleStyle.normal.textColor = new Color(0.4f, 0.7f, 1f);
                
                GUI.Label(
                    new Rect(lastRect.x + 5, lastRect.y + 5, lastRect.width - 25, 18),
                    $"[{skill.SkillId}] {skill.SkillName}",
                    titleStyle
                );
                
                // 副标题：类型标签
                var subtitleStyle = new GUIStyle(EditorStyles.miniLabel);
                subtitleStyle.normal.textColor = GetSkillTypeColor(skill.SkillType);
                
                GUI.Label(
                    new Rect(lastRect.x + 5, lastRect.y + 25, 100, 16),
                    GetSkillTypeLabel(skill.SkillType),
                    subtitleStyle
                );
                
                // 动作数量
                var actionCountStyle = new GUIStyle(EditorStyles.miniLabel);
                actionCountStyle.normal.textColor = Color.gray;
                
                GUI.Label(
                    new Rect(lastRect.x + 5, lastRect.y + 40, lastRect.width - 25, 16),
                    $"动作数: {skill.SkillActions.Count}",
                    actionCountStyle
                );
                
                // 修改标记
                if (skill.IsDirty)
                {
                    var dirtyStyle = new GUIStyle(EditorStyles.boldLabel);
                    dirtyStyle.normal.textColor = Color.yellow;
                    GUI.Label(new Rect(lastRect.xMax - 20, lastRect.y + 5, 15, 15), "*", dirtyStyle);
                }
            }
            EditorGUILayout.EndVertical();
            
            GUI.backgroundColor = originalColor;
        }
        
        /// <summary>
        /// 获取过滤后的技能列表
        /// </summary>
        private List<SkillEditorData> GetFilteredSkills(List<SkillEditorData> skills)
        {
            var filtered = skills.AsEnumerable();
            
            // 类型筛选
            if (_filterType != 0)
            {
                filtered = filtered.Where(s => s.SkillType == _filterType);
            }
            
            // 关键字搜索
            if (!string.IsNullOrWhiteSpace(_searchKeyword))
            {
                string keyword = _searchKeyword.ToLower();
                filtered = filtered.Where(s =>
                    s.SkillName.ToLower().Contains(keyword) ||
                    s.SkillDescription.ToLower().Contains(keyword) ||
                    s.SkillId.ToString().Contains(keyword)
                );
            }
            
            return filtered.OrderBy(s => s.SkillId).ToList();
        }
        
        /// <summary>
        /// 获取技能类型标签
        /// </summary>
        private string GetSkillTypeLabel(SkillTypeEnum type)
        {
            return type switch
            {
                SkillTypeEnum.攻击 => "⚔ 攻击",
                SkillTypeEnum.控制 => "🔒 控制",
                SkillTypeEnum.位移 => "💨 位移",
                SkillTypeEnum.Buff => "✨ Buff",
                _ => "未知"
            };
        }
        
        /// <summary>
        /// 获取技能类型颜色
        /// </summary>
        private Color GetSkillTypeColor(SkillTypeEnum type)
        {
            return type switch
            {
                SkillTypeEnum.攻击 => new Color(1f, 0.4f, 0.4f),      // 红色
                SkillTypeEnum.控制 => new Color(0.8f, 0.6f, 1f),      // 紫色
                SkillTypeEnum.位移 => new Color(0.4f, 0.8f, 1f),      // 蓝色
                SkillTypeEnum.Buff => new Color(0.4f, 1f, 0.4f),      // 绿色
                _ => Color.gray
            };
        }
    }
}

