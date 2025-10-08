using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;

namespace Astrum.Editor.RoleEditor.Modules
{
    /// <summary>
    /// 角色列表模块 - 显示和管理角色列表
    /// </summary>
    public class RoleListModule
    {
        // === 数据 ===
        private string _searchKeyword = "";
        private Vector2 _scrollPosition;
        private RoleEditorData _selectedRole;
        
        // === 属性 ===
        public RoleEditorData SelectedRole => _selectedRole;
        
        // === 事件 ===
        public event Action<RoleEditorData> OnRoleSelected;
        public event Action OnCreateNew;
        public event Action<RoleEditorData> OnDuplicate;
        public event Action<RoleEditorData> OnDelete;
        
        /// <summary>
        /// 绘制列表
        /// </summary>
        public void DrawList(List<RoleEditorData> roles, float width)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(width));
            {
                DrawSearchBar();
                DrawToolbar();
                DrawRoleList(roles);
            }
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 选择角色
        /// </summary>
        public void SelectRole(RoleEditorData role)
        {
            _selectedRole = role;
            OnRoleSelected?.Invoke(role);
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
                
                GUI.enabled = _selectedRole != null;
                
                if (GUILayout.Button("复制", GUILayout.Height(25)))
                {
                    OnDuplicate?.Invoke(_selectedRole);
                }
                
                if (GUILayout.Button("删除", GUILayout.Height(25)))
                {
                    OnDelete?.Invoke(_selectedRole);
                }
                
                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制角色列表
        /// </summary>
        private void DrawRoleList(List<RoleEditorData> roles)
        {
            var filteredRoles = GetFilteredRoles(roles);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                foreach (var role in filteredRoles)
                {
                    DrawRoleItem(role);
                    EditorGUILayout.Space(2);
                }
            }
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 绘制单个角色项
        /// </summary>
        private void DrawRoleItem(RoleEditorData role)
        {
            bool isSelected = role == _selectedRole;
            
            // 选中背景色
            var bgColor = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : Color.clear;
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            
            EditorGUILayout.BeginVertical("box", GUILayout.Height(50));
            {
                // 点击选中
                if (GUILayout.Button("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    SelectRole(role);
                }
                
                Rect lastRect = GUILayoutUtility.GetLastRect();
                
                // 标题：[ID] 名称
                var titleStyle = new GUIStyle(EditorStyles.boldLabel);
                if (isSelected)
                    titleStyle.normal.textColor = new Color(0.4f, 0.7f, 1f);
                
                GUI.Label(
                    new Rect(lastRect.x + 5, lastRect.y + 5, lastRect.width - 25, 18),
                    $"[{role.RoleId}] {role.RoleName}",
                    titleStyle
                );
                
                // 副标题：类型 - 描述
                var subtitleStyle = new GUIStyle(EditorStyles.miniLabel);
                subtitleStyle.normal.textColor = Color.gray;
                
                string subtitle = $"{role.RoleType}";
                if (!string.IsNullOrEmpty(role.RoleDescription))
                {
                    subtitle += $" - {role.RoleDescription}";
                }
                
                GUI.Label(
                    new Rect(lastRect.x + 5, lastRect.y + 25, lastRect.width - 25, 16),
                    subtitle,
                    subtitleStyle
                );
                
                // 修改标记
                if (role.IsDirty)
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
        /// 获取过滤后的角色列表
        /// </summary>
        private List<RoleEditorData> GetFilteredRoles(List<RoleEditorData> roles)
        {
            if (string.IsNullOrWhiteSpace(_searchKeyword))
                return roles;
            
            string keyword = _searchKeyword.ToLower();
            
            return roles.Where(r =>
                r.RoleName.ToLower().Contains(keyword) ||
                r.RoleDescription.ToLower().Contains(keyword) ||
                r.RoleId.ToString().Contains(keyword) ||
                r.RoleType.ToString().ToLower().Contains(keyword)
            ).ToList();
        }
    }
}

