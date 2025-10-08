using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Modules;
using Astrum.Editor.RoleEditor.Persistence;
using Astrum.Editor.RoleEditor.Services;

namespace Astrum.Editor.RoleEditor.Windows
{
    /// <summary>
    /// 角色编辑器主窗口
    /// </summary>
    public class RoleEditorWindow : OdinEditorWindow
    {
        // === UI模块 ===
        private RoleListModule _listModule;
        private RolePreviewModule _previewModule;
        private PropertyTree _propertyTree;
        
        // === 数据 ===
        private List<RoleEditorData> _allRoles = new List<RoleEditorData>();
        private RoleEditorData _selectedRole;
        
        // === UI状态 ===
        private Vector2 _detailScrollPosition;
        
        // === 布局常量 ===
        private const float LIST_WIDTH = 250f;
        private const float PREVIEW_WIDTH = 400f;
        private const float MIN_WINDOW_WIDTH = 1000f;
        private const float MIN_WINDOW_HEIGHT = 600f;
        
        // === Unity生命周期 ===
        
        [MenuItem("Tools/Role & Skill Editor/Role Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<RoleEditorWindow>("角色编辑器");
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            // 初始化配置表Helper（现在直接读取CSV，不需要ConfigManager）
            ConfigTableHelper.ClearCache();
            
            InitializeModules();
            LoadData();
            
            Debug.Log("[RoleEditor] Role Editor Window opened");
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();
            
            CheckUnsavedChanges();
            CleanupModules();
            
            Debug.Log("[RoleEditor] Role Editor Window closed");
        }
        
        protected override void OnGUI()
        {
            DrawToolbar();
            
            EditorGUILayout.BeginHorizontal();
            {
                // 左侧：角色列表
                if (_listModule != null)
                {
                    _listModule.DrawList(_allRoles, LIST_WIDTH);
                }
                
                // 中间：详情面板（使用Odin）
                DrawDetailPanel();
                
                // 右侧：预览面板
                DrawPreviewPanel();
            }
            EditorGUILayout.EndHorizontal();
            
            DrawStatusBar();
            
            // 强制重绘（预览动画需要）
            if (_previewModule != null)
            {
                Repaint();
            }
        }
        
        // === 初始化 ===
        
        private void InitializeModules()
        {
            // 创建列表模块
            _listModule = new RoleListModule();
            _listModule.OnRoleSelected += OnRoleSelected;
            _listModule.OnCreateNew += OnCreateNewRole;
            _listModule.OnDuplicate += OnDuplicateRole;
            _listModule.OnDelete += OnDeleteRole;
            
            // 创建预览模块
            _previewModule = new RolePreviewModule();
            _previewModule.Initialize();
        }
        
        private void CleanupModules()
        {
            _previewModule?.Cleanup();
            _propertyTree?.Dispose();
            _propertyTree = null;
        }
        
        // === 数据加载和保存 ===
        
        private void LoadData()
        {
            try
            {
                _allRoles = RoleDataReader.ReadRoleData();
                
                if (_allRoles.Count > 0)
                {
                    _listModule.SelectRole(_allRoles[0]);
                }
                
                Debug.Log($"[RoleEditor] Loaded {_allRoles.Count} roles");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RoleEditor] Failed to load role data: {ex}");
                EditorUtility.DisplayDialog("加载失败", $"加载角色数据失败：{ex.Message}", "确定");
            }
        }
        
        private void SaveData()
        {
            try
            {
                // 验证所有数据
                bool hasErrors = false;
                foreach (var role in _allRoles)
                {
                    if (!RoleDataValidator.Validate(role, out var errors))
                    {
                        hasErrors = true;
                        Debug.LogWarning($"[RoleEditor] Role {role.RoleId} has validation errors: {string.Join(", ", errors)}");
                    }
                }
                
                if (hasErrors)
                {
                    bool proceed = EditorUtility.DisplayDialog(
                        "验证警告",
                        "部分角色数据存在验证错误，是否仍要保存？",
                        "保存", "取消"
                    );
                    
                    if (!proceed) return;
                }
                
                // 保存数据
                bool success = RoleDataWriter.WriteRoleData(_allRoles);
                
                if (success)
                {
                    // 清除所有修改标记
                    foreach (var role in _allRoles)
                    {
                        role.ClearDirty();
                    }
                    
                    EditorUtility.DisplayDialog("保存成功", "角色数据已保存到CSV文件", "确定");
                    Debug.Log("[RoleEditor] Role data saved successfully");
                }
                else
                {
                    EditorUtility.DisplayDialog("保存失败", "保存角色数据失败，请查看Console", "确定");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RoleEditor] Failed to save role data: {ex}");
                EditorUtility.DisplayDialog("保存失败", $"保存角色数据失败：{ex.Message}", "确定");
            }
        }
        
        private void CheckUnsavedChanges()
        {
            bool hasUnsaved = _allRoles.Any(r => r.IsDirty);
            
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
        
        private void OnRoleSelected(RoleEditorData role)
        {
            _selectedRole = role;
            
            // 重建PropertyTree（Odin）
            _propertyTree?.Dispose();
            if (_selectedRole != null)
            {
                _propertyTree = PropertyTree.Create(_selectedRole);
            }
            
            // 更新预览
            _previewModule?.SetRole(role);
        }
        
        private void OnCreateNewRole()
        {
            var existingIds = new System.Collections.Generic.HashSet<int>(_allRoles.Select(r => r.RoleId));
            int newId = AstrumEditorUtility.GenerateUniqueId(1000, existingIds);
            
            var newRole = RoleEditorData.CreateDefault(newId);
            _allRoles.Add(newRole);
            _listModule.SelectRole(newRole);
            
            Debug.Log($"[RoleEditor] Created new role: {newId}");
        }
        
        private void OnDuplicateRole(RoleEditorData role)
        {
            if (role == null) return;
            
            var existingIds = new System.Collections.Generic.HashSet<int>(_allRoles.Select(r => r.RoleId));
            int newId = AstrumEditorUtility.GenerateUniqueId(1000, existingIds);
            
            var cloned = role.Clone();
            cloned.EntityId = newId;
            cloned.RoleId = newId;
            cloned.RoleName = $"{role.RoleName}_Copy";
            
            _allRoles.Add(cloned);
            _listModule.SelectRole(cloned);
            
            Debug.Log($"[RoleEditor] Duplicated role {role.RoleId} to {newId}");
        }
        
        private void OnDeleteRole(RoleEditorData role)
        {
            if (role == null) return;
            
            bool confirm = EditorUtility.DisplayDialog(
                "确认删除",
                $"确定要删除角色 [{role.RoleId}] {role.RoleName} 吗？",
                "删除", "取消"
            );
            
            if (confirm)
            {
                _allRoles.Remove(role);
                
                if (_allRoles.Count > 0)
                {
                    _listModule.SelectRole(_allRoles[0]);
                }
                else
                {
                    OnRoleSelected(null);
                }
                
                Debug.Log($"[RoleEditor] Deleted role: {role.RoleId}");
            }
        }
        
        // === UI绘制 ===
        
        private void DrawToolbar()
        {
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
                
                GUILayout.FlexibleSpace();
                
                // 显示未保存数量
                int dirtyCount = _allRoles.Count(r => r.IsDirty);
                if (dirtyCount > 0)
                {
                    GUILayout.Label($"未保存: {dirtyCount}", EditorStyles.toolbarButton);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical();
            {
                if (_selectedRole == null)
                {
                    EditorGUILayout.HelpBox("请选择一个角色", MessageType.Info);
                    return;
                }
                
                // 使用Odin绘制（自动根据特性生成UI）
                EditorGUI.BeginChangeCheck();
                
                _detailScrollPosition = EditorGUILayout.BeginScrollView(_detailScrollPosition);
                {
                    if (_propertyTree != null)
                    {
                        _propertyTree.Draw(false);
                    }
                }
                EditorGUILayout.EndScrollView();
                
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedRole.MarkDirty();
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPreviewPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(PREVIEW_WIDTH));
            {
                GUILayout.Label("模型预览", EditorStyles.boldLabel);
                
                Rect previewRect = GUILayoutUtility.GetRect(PREVIEW_WIDTH, position.height - 80);
                
                if (_previewModule != null)
                {
                    _previewModule.DrawPreview(previewRect);
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.Label($"角色总数: {_allRoles.Count}", EditorStyles.miniLabel);
                
                GUILayout.FlexibleSpace();
                
                if (_selectedRole != null)
                {
                    string status = _selectedRole.IsDirty ? "[已修改]" : "[未修改]";
                    GUILayout.Label($"当前: {_selectedRole.RoleName} {status}", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}

