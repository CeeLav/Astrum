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
    /// 技能编辑器主窗口
    /// </summary>
    public class SkillEditorWindow : EditorWindow
    {
        // === UI模块 ===
        private SkillListModule _listModule;
        private PropertyTree _propertyTree;
        
        // === 数据 ===
        private List<SkillEditorData> _allSkills = new List<SkillEditorData>();
        private SkillEditorData _selectedSkill;
        
        // === UI状态 ===
        private Vector2 _detailScrollPosition;
        
        // === 布局常量 ===
        private const float LIST_WIDTH = 250f;
        private const float MIN_WINDOW_WIDTH = 900f;
        private const float MIN_WINDOW_HEIGHT = 600f;
        
        // === Unity生命周期 ===
        
        [MenuItem("Astrum/Editor 编辑器/Skill Editor 技能编辑器", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<SkillEditorWindow>("技能编辑器");
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }
        
        private void OnEnable()
        {
            // 初始化配置表Helper
            ConfigTableHelper.ClearCache();
            
            InitializeModules();
            LoadData();
            
            Debug.Log("[SkillEditor] Skill Editor Window opened");
        }
        
        private void OnDisable()
        {
            CheckUnsavedChanges();
            CleanupModules();
            
            Debug.Log("[SkillEditor] Skill Editor Window closed");
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
                // 左侧：技能列表
                if (_listModule != null)
                {
                    _listModule.DrawList(_allSkills, LIST_WIDTH);
                }
                
                // 右侧：详情面板（使用Odin）
                DrawDetailPanel();
            }
            EditorGUILayout.EndHorizontal();
            
            DrawStatusBar();
        }
        
        // === 初始化 ===
        
        private void InitializeModules()
        {
            // 创建列表模块
            _listModule = new SkillListModule();
            _listModule.OnSkillSelected += OnSkillSelected;
            _listModule.OnCreateNew += OnCreateNewSkill;
            _listModule.OnDuplicate += OnDuplicateSkill;
            _listModule.OnDelete += OnDeleteSkill;
        }
        
        private void CleanupModules()
        {
            _propertyTree?.Dispose();
            _propertyTree = null;
        }
        
        // === 数据加载和保存 ===
        
        private void LoadData()
        {
            try
            {
                _allSkills = SkillDataReader.ReadSkillData();
                
                if (_allSkills.Count > 0)
                {
                    _listModule.SelectSkill(_allSkills[0]);
                }
                
                Debug.Log($"[SkillEditor] Loaded {_allSkills.Count} skills");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SkillEditor] Failed to load skill data: {ex}");
                EditorUtility.DisplayDialog("加载失败", $"加载技能数据失败：{ex.Message}", "确定");
            }
        }
        
        private void SaveData()
        {
            try
            {
                // 验证所有数据
                var invalidSkills = new List<string>();
                foreach (var skill in _allSkills)
                {
                    if (!SkillDataValidator.Validate(skill, out var errors))
                    {
                        invalidSkills.Add($"技能 [{skill.SkillId}] {skill.SkillName}: {string.Join(", ", errors)}");
                    }
                }
                
                if (invalidSkills.Count > 0)
                {
                    bool continueAnyway = EditorUtility.DisplayDialog(
                        "验证失败",
                        $"发现 {invalidSkills.Count} 个技能有验证错误：\n\n" +
                        string.Join("\n", invalidSkills.Take(3)) +
                        (invalidSkills.Count > 3 ? "\n..." : "") +
                        "\n\n是否仍要保存？",
                        "保存", "取消"
                    );
                    
                    if (!continueAnyway)
                        return;
                }
                
                // 写入CSV文件
                if (SkillDataWriter.WriteSkillData(_allSkills))
                {
                    // 清除所有修改标记
                    foreach (var skill in _allSkills)
                    {
                        skill.ClearDirty();
                    }
                    
                    Debug.Log($"[SkillEditor] Successfully saved {_allSkills.Count} skills");
                    EditorUtility.DisplayDialog("保存成功", $"成功保存 {_allSkills.Count} 个技能", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("保存失败", "保存技能数据失败", "确定");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SkillEditor] Failed to save skill data: {ex}");
                EditorUtility.DisplayDialog("保存失败", $"保存技能数据失败：{ex.Message}", "确定");
            }
        }
        
        private void CheckUnsavedChanges()
        {
            bool hasUnsaved = _allSkills.Any(s => s.IsDirty);
            
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
        
        private void OnSkillSelected(SkillEditorData skill)
        {
            _selectedSkill = skill;
            
            // 重建PropertyTree（Odin）
            _propertyTree?.Dispose();
            if (_selectedSkill != null)
            {
                _propertyTree = PropertyTree.Create(_selectedSkill);
                _propertyTree.UpdateTree();
            }
        }
        
        private void OnCreateNewSkill()
        {
            var existingIds = new System.Collections.Generic.HashSet<int>(_allSkills.Select(s => s.SkillId));
            int newId = AstrumEditorUtility.GenerateUniqueId(2000, existingIds);
            
            var newSkill = SkillEditorData.CreateDefault(newId);
            _allSkills.Add(newSkill);
            _listModule.SelectSkill(newSkill);
            
            Debug.Log($"[SkillEditor] Created new skill: {newId}");
        }
        
        private void OnDuplicateSkill(SkillEditorData skill)
        {
            if (skill == null) return;
            
            var existingIds = new System.Collections.Generic.HashSet<int>(_allSkills.Select(s => s.SkillId));
            int newId = AstrumEditorUtility.GenerateUniqueId(2000, existingIds);
            
            var duplicated = skill.Clone();
            duplicated.SkillId = newId;
            duplicated.SkillName = skill.SkillName + "_Copy";
            
            // 更新所有动作的SkillId
            foreach (var action in duplicated.SkillActions)
            {
                action.SkillId = newId;
            }
            
            _allSkills.Add(duplicated);
            _listModule.SelectSkill(duplicated);
            
            Debug.Log($"[SkillEditor] Duplicated skill: {skill.SkillId} → {newId}");
        }
        
        private void OnDeleteSkill(SkillEditorData skill)
        {
            if (skill == null) return;
            
            bool confirm = EditorUtility.DisplayDialog(
                "删除技能",
                $"确定要删除技能 [{skill.SkillId}] {skill.SkillName} 吗？",
                "删除", "取消"
            );
            
            if (confirm)
            {
                _allSkills.Remove(skill);
                
                if (_allSkills.Count > 0)
                {
                    _listModule.SelectSkill(_allSkills[0]);
                }
                else
                {
                    _listModule.SelectSkill(null);
                }
                
                Debug.Log($"[SkillEditor] Deleted skill: {skill.SkillId}");
            }
        }
        
        // === UI绘制 ===
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                // 保存按钮
                if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    SaveData();
                }
                
                // 刷新按钮
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    LoadData();
                }
                
                // 验证按钮
                if (GUILayout.Button("验证", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    ValidateAllSkills();
                }
                
                GUILayout.FlexibleSpace();
                
                // 帮助按钮
                if (GUILayout.Button("帮助", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    Application.OpenURL("https://github.com/yourproject/wiki/skill-editor");
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical();
            {
                if (_selectedSkill == null)
                {
                    EditorGUILayout.HelpBox("请选择一个技能", MessageType.Info);
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
                            _propertyTree.BeginDraw(true);
                            
                            // 遍历绘制所有属性
                            foreach (var property in _propertyTree.EnumerateTree(false))
                            {
                                property.Draw();
                            }
                            
                            _propertyTree.EndDraw();
                            
                            // 应用修改
                            if (_propertyTree.ApplyChanges())
                            {
                                _selectedSkill.MarkDirty();
                                EditorUtility.SetDirty(_selectedSkill);
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                int unsavedCount = _allSkills.Count(s => s.IsDirty);
                
                EditorGUILayout.LabelField(
                    $"技能总数: {_allSkills.Count}",
                    EditorStyles.miniLabel,
                    GUILayout.Width(100)
                );
                
                if (unsavedCount > 0)
                {
                    var style = new GUIStyle(EditorStyles.miniLabel);
                    style.normal.textColor = Color.yellow;
                    EditorGUILayout.LabelField(
                        $"未保存: {unsavedCount}",
                        style,
                        GUILayout.Width(80)
                    );
                }
                
                GUILayout.FlexibleSpace();
                
                if (_selectedSkill != null)
                {
                    EditorGUILayout.LabelField(
                        $"当前: [{_selectedSkill.SkillId}] {_selectedSkill.SkillName}",
                        EditorStyles.miniLabel
                    );
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        // === 验证 ===
        
        private void ValidateAllSkills()
        {
            var errors = new List<string>();
            
            foreach (var skill in _allSkills)
            {
                if (!SkillDataValidator.Validate(skill, out var skillErrors))
                {
                    errors.Add($"\n技能 [{skill.SkillId}] {skill.SkillName}:");
                    errors.AddRange(skillErrors.Select(e => $"  - {e}"));
                }
            }
            
            if (errors.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "验证成功",
                    "所有技能数据验证通过！",
                    "确定"
                );
            }
            else
            {
                string errorMessage = string.Join("\n", errors);
                Debug.LogWarning($"[SkillEditor] Validation errors:\n{errorMessage}");
                
                EditorUtility.DisplayDialog(
                    "验证失败",
                    $"发现 {errors.Count} 个验证错误，详见Console",
                    "确定"
                );
            }
        }
    }
}

