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
    public class RoleEditorWindow : EditorWindow
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
        private int _selectedAnimationIndex = 0;
        
        // === 布局常量 ===
        private const float LIST_WIDTH = 250f;
        private const float PREVIEW_WIDTH = 400f;
        private const float MIN_WINDOW_WIDTH = 1000f;
        private const float MIN_WINDOW_HEIGHT = 600f;
        
        // === 可调整布局 ===
        private float _previewWidth = PREVIEW_WIDTH;
        private bool _isResizingPreview = false;
        
        // === Unity生命周期 ===
        
        [MenuItem("Tools/Role & Skill Editor/Role Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<RoleEditorWindow>("角色编辑器");
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }
        
        private void OnEnable()
        {
            // 初始化配置表Helper（现在直接读取CSV，不需要ConfigManager）
            ConfigTableHelper.ClearCache();
            
            InitializeModules();
            LoadData();
            
            Debug.Log("[RoleEditor] Role Editor Window opened");
        }
        
        private void OnDisable()
        {
            CheckUnsavedChanges();
            CleanupModules();
            
            Debug.Log("[RoleEditor] Role Editor Window closed");
        }
        
        private void OnDestroy()
        {
            // 清理PropertyTree
            _propertyTree?.Dispose();
            _propertyTree = null;
        }
        
        private void OnGUI()
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
                
                // 可调整大小的分隔条
                DrawResizeHandle();
                
                // 右侧：预览面板（可调整大小）
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
            
            // 重建PropertyTree（Odin）- 现在RoleEditorData继承自ScriptableObject，支持Undo
            _propertyTree?.Dispose();
            if (_selectedRole != null)
            {
                // 使用UnityObjectTree以支持Undo功能
                _propertyTree = PropertyTree.Create(_selectedRole);
                _propertyTree.UpdateTree(); // 确保树结构更新
            }
            
            // 重置动画选择
            _selectedAnimationIndex = 0;
            
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
                }
                else
                {
                    // 使用Odin绘制（自动根据特性生成UI）
                    _detailScrollPosition = EditorGUILayout.BeginScrollView(_detailScrollPosition);
                    {
                        if (_propertyTree != null)
                        {
                            // 更新树结构
                            _propertyTree.UpdateTree();
                            
                            // 绘制Inspector
                            InspectorUtilities.BeginDrawPropertyTree(_propertyTree, true);
                            
                            // 遍历绘制所有属性
                            foreach (var property in _propertyTree.EnumerateTree(false))
                            {
                                property.Draw();
                            }
                            
                            InspectorUtilities.EndDrawPropertyTree(_propertyTree);
                            
                            // 应用修改
                            if (_propertyTree.ApplyChanges())
                            {
                                _selectedRole.MarkDirty();
                                EditorUtility.SetDirty(_selectedRole);
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawResizeHandle()
        {
            // 绘制可拖拽的分隔条
            Rect resizeRect = GUILayoutUtility.GetRect(5, position.height, GUILayout.Width(5));
            
            EditorGUI.DrawRect(resizeRect, Color.gray);
            
            // 处理拖拽调整大小
            Event e = Event.current;
            if (e.type == EventType.MouseDown && resizeRect.Contains(e.mousePosition))
            {
                _isResizingPreview = true;
                e.Use();
            }
            
            if (_isResizingPreview)
            {
                if (e.type == EventType.MouseDrag)
                {
                    _previewWidth = Mathf.Clamp(_previewWidth - e.delta.x, 200f, position.width - LIST_WIDTH - 200f);
                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    _isResizingPreview = false;
                    e.Use();
                }
            }
        }
        
        private void DrawPreviewPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_previewWidth));
            {
                GUILayout.Label("模型预览", EditorStyles.boldLabel);
                
                // 预览区域（减去控制面板高度）
                Rect previewRect = GUILayoutUtility.GetRect(_previewWidth, position.height - 200);
                
                if (_previewModule != null)
                {
                    _previewModule.DrawPreview(previewRect);
                }
                
                // 动画控制面板（在预览面板底部）
                DrawAnimationControls();
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAnimationControls()
        {
            if (_selectedRole == null) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label("动画控制", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                {
                    // 播放/停止按钮
                    if (GUILayout.Button("播放", GUILayout.Height(25)))
                    {
                        _previewModule?.PlayAction(_selectedRole.IdleAction);
                    }
                    
                    if (GUILayout.Button("停止", GUILayout.Height(25)))
                    {
                        _previewModule?.StopAnimation();
                    }
                    
                    if (GUILayout.Button("重置视角", GUILayout.Height(25)))
                    {
                        _previewModule?.ResetCamera();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                // 动画选择下拉菜单
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("动画:", GUILayout.Width(40));
                    
                    var actions = ConfigTableHelper.GetAvailableActions(_selectedRole);
                    if (actions != null && actions.Count > 0)
                    {
                        // 确保索引有效
                        if (_selectedAnimationIndex >= actions.Count)
                        {
                            _selectedAnimationIndex = 0;
                        }
                        
                        string[] actionNames = actions.Select(a => $"{a.ActionName} (ID:{a.ActionId})").ToArray();
                        int newIndex = EditorGUILayout.Popup(_selectedAnimationIndex, actionNames, GUILayout.ExpandWidth(true));
                        
                        // 检测选择改变
                        if (newIndex != _selectedAnimationIndex && newIndex >= 0 && newIndex < actions.Count)
                        {
                            _selectedAnimationIndex = newIndex;
                            _previewModule?.PlayAction(actions[_selectedAnimationIndex].ActionId);
                        }
                        
                        if (GUILayout.Button("播放", GUILayout.Width(60)))
                        {
                            _previewModule?.PlayAction(actions[_selectedAnimationIndex].ActionId);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                // 播放速度控制
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("速度:", GUILayout.Width(40));
                    float speed = EditorGUILayout.Slider(1.0f, 0.1f, 3.0f, GUILayout.ExpandWidth(true));
                    _previewModule?.SetAnimationSpeed(speed);
                }
                EditorGUILayout.EndHorizontal();
                
                // 碰撞盒显示开关
                EditorGUILayout.BeginHorizontal();
                {
                    bool showCollision = _previewModule?.ShowCollisionShape ?? false;
                    bool newShowCollision = EditorGUILayout.Toggle("显示碰撞盒", showCollision);
                    
                    if (_previewModule != null)
                    {
                        _previewModule.ShowCollisionShape = newShowCollision;
                    }
                    
                    // 碰撞盒信息提示
                    if (newShowCollision && string.IsNullOrEmpty(_selectedRole?.CollisionData))
                    {
                        GUILayout.Label("(未配置)", EditorStyles.miniLabel);
                    }
                }
                EditorGUILayout.EndHorizontal();
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

