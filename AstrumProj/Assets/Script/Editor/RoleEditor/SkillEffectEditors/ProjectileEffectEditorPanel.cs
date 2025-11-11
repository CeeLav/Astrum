using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using Astrum.Editor.RoleEditor.Services;
using Astrum.Editor.RoleEditor.Persistence;
using Astrum.Editor.RoleEditor.Windows;
using cfg.Projectile;
using UnityEditor;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.SkillEffectEditors
{
    internal class ProjectileEffectEditorPanel : ISkillEffectEditorPanel
    {
        private static readonly string[] TargetOptions = { "自身", "敌人", "友军", "区域" };
        private static readonly int[] TargetValues = { 0, 1, 2, 3 };

        public string EffectType => "Projectile";
        public bool SupportsInlineEditing => true;

        // 缓存当前编辑的弹道配置
        private ProjectileTableData _currentProjectileData;
        private bool _projectileDataLoaded = false;

        public bool DrawContent(SkillEffectTableData data)
        {
            bool changed = false;

            // 确保参数列表最小长度
            // IntParams: [0] = ProjectileId, [1] = TargetType, [2] = ExtraEffectId1, [3] = ExtraEffectId2, ...
            SkillEffectEditorUtility.EnsureListSize(data.IntParams, 1);
            
            // StringParams: [0] = ExtraEffectParams (JSON格式)
            SkillEffectEditorUtility.EnsureListSize(data.StringParams, 1);

            EditorGUILayout.LabelField("弹道效果配置", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            // ProjectileId 配置
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("弹道ID", EditorStyles.boldLabel);
                changed |= SkillEffectEditorUtility.DrawIntField("ProjectileId", data.IntParams, 0);
                
                if (data.IntParams[0] <= 0)
                {
                    EditorGUILayout.HelpBox("请设置有效的弹道ID", MessageType.Warning);
                }
                else
                {
                    // 加载对应的弹道数据
                    LoadProjectileData(data.IntParams[0]);
                    
                    if (_currentProjectileData != null)
                    {
                        EditorGUILayout.HelpBox(GetProjectileInfo(_currentProjectileData), MessageType.Info);
                    }
                }
                
                // 添加一个弹道选择按钮
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("📋 选择弹道", GUILayout.Width(120)))
                {
                    ShowProjectileSelector(data);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);

            // 如果已加载弹道数据，显示特效配置
            if (_currentProjectileData != null)
            {
                EditorGUILayout.LabelField("弹道特效配置", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("这些特效配置将保存到 ProjectileTable", MessageType.Info);
                
                // 开火特效配置
                DrawProjectileEffectSection("开火特效 (Spawn)", _currentProjectileData, nameof(_currentProjectileData.SpawnEffectPath), ref changed);
                
                EditorGUILayout.Space(3);
                
                // 飞行特效配置
                DrawProjectileEffectSection("飞行特效 (Loop)", _currentProjectileData, nameof(_currentProjectileData.LoopEffectPath), ref changed);
                
                EditorGUILayout.Space(3);
                
                // 命中特效配置
                DrawProjectileEffectSection("命中特效 (Hit)", _currentProjectileData, nameof(_currentProjectileData.HitEffectPath), ref changed);
                
                // 保存修改到 ProjectileTable
                if (changed && _projectileDataLoaded)
                {
                    SaveProjectileData(_currentProjectileData);
                }
            }

            EditorGUILayout.Space(5);

            // ExtraEffectIds 列表
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("额外效果ID (ExtraEffectIds)", EditorStyles.boldLabel);
                
                // 显示已配置的效果ID
                var effectIds = new List<int>();
                for (int i = 2; i < data.IntParams.Count; i++)
                {
                    effectIds.Add(data.IntParams[i]);
                }
                
                if (effectIds.Count > 0)
                {
                    EditorGUILayout.LabelField("当前配置的效果ID:");
                    EditorGUILayout.BeginVertical("box");
                    {
                        for (int i = 0; i < effectIds.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"效果 {i + 1}: {effectIds[i]}", GUILayout.Width(120));
                            if (GUILayout.Button("查看", GUILayout.Width(50)))
                            {
                                SkillEffectEditorWindow.ShowWindow(effectIds[i], () => {
                                    // 刷新数据
                                    SkillEffectDataReader.ClearCache();
                                });
                            }
                            if (GUILayout.Button("删除", GUILayout.Width(50)))
                            {
                                // 从IntParams中移除（索引2开始是效果ID）
                                if (i + 2 < data.IntParams.Count)
                                {
                                    data.IntParams.RemoveAt(i + 2);
                                    changed = true;
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("未配置额外效果", MessageType.Info);
                }
                
                EditorGUILayout.Space(5);
                
                // 添加新效果ID
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("添加效果ID:", GUILayout.Width(80));
                _newEffectId = EditorGUILayout.IntField(_newEffectId, GUILayout.Width(100));
                if (GUILayout.Button("添加", GUILayout.Width(50)))
                {
                    if (_newEffectId > 0)
                    {
                        data.IntParams.Add(_newEffectId);
                        changed = true;
                        _newEffectId = 0;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // JSON参数编辑（用于复杂配置）
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("额外参数 (JSON格式)", EditorStyles.boldLabel);
                
                _jsonFoldout = EditorGUILayout.Foldout(_jsonFoldout, "编辑JSON参数");
                if (_jsonFoldout)
                {
                    if (_jsonText == null)
                    {
                        _jsonText = data.StringParams[0] ?? "{}";
                    }
                    
                    EditorGUI.BeginChangeCheck();
                    _jsonText = EditorGUILayout.TextArea(_jsonText, GUILayout.MinHeight(60));
                    if (EditorGUI.EndChangeCheck())
                    {
                        data.StringParams[0] = _jsonText;
                        changed = true;
                    }
                    
                    if (GUILayout.Button("格式化JSON", GUILayout.Width(100)))
                    {
                        try
                        {
                            var jsonObj = JsonUtility.FromJson<object>(_jsonText);
                            _jsonText = JsonUtility.ToJson(jsonObj, true);
                            data.StringParams[0] = _jsonText;
                            changed = true;
                        }
                        catch
                        {
                            EditorUtility.DisplayDialog("错误", "JSON格式无效", "确定");
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(data.StringParams[0]))
                {
                    EditorGUILayout.HelpBox($"当前JSON: {data.StringParams[0]}", MessageType.Info);
                }
            }
            EditorGUILayout.EndVertical();

            return changed;
        }

        private void DrawProjectileEffectSection(string label, ProjectileTableData projectileData, string propertyName, ref bool changed)
        {
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                
                // 获取当前路径
                var currentPath = (string)typeof(ProjectileTableData).GetProperty(propertyName)?.GetValue(projectileData);
                
                EditorGUI.BeginChangeCheck();
                var newPath = EditorGUILayout.TextField("特效路径", currentPath ?? string.Empty);
                if (EditorGUI.EndChangeCheck())
                {
                    typeof(ProjectileTableData).GetProperty(propertyName)?.SetValue(projectileData, newPath);
                    changed = true;
                }
                
                // 预览和资源检查
                var currentPrefab = string.IsNullOrEmpty(newPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(newPath);
                if (currentPrefab != null)
                {
                    EditorGUILayout.ObjectField("预览", currentPrefab, typeof(GameObject), false);
                }
                
                // 路径选择按钮
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("选择特效", GUILayout.Width(80)))
                {
                    var selectedPath = EditorUtility.OpenFilePanelWithFilters(
                        $"选择 {label} 特效", 
                        "Assets/ArtRes/Effect", 
                        new[] { "Prefab files", "prefab", "All files", "*" }
                    );
                    
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        if (selectedPath.StartsWith(Application.dataPath))
                        {
                            var relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                            typeof(ProjectileTableData).GetProperty(propertyName)?.SetValue(projectileData, relativePath);
                            changed = true;
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("错误", "只能选择Assets目录下的资源", "确定");
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(currentPath) && GUILayout.Button("刷新资源", GUILayout.Width(80)))
                {
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("提示", "资源已刷新", "确定");
                }
                EditorGUILayout.EndHorizontal();
                
                // 显示当前路径和状态
                if (!string.IsNullOrEmpty(currentPath))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("当前路径:", GUILayout.Width(60));
                    EditorGUILayout.SelectableLabel(currentPath, EditorStyles.textField, GUILayout.Height(16));
                    EditorGUILayout.EndHorizontal();
                    
                    // 显示资源状态
                    if (currentPrefab != null)
                    {
                        EditorGUILayout.LabelField("状态: ✓ 已加载", EditorStyles.miniLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("状态: ✗ 未找到", EditorStyles.miniLabel);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("未设置特效路径", MessageType.Info);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void LoadProjectileData(int projectileId)
        {
            if (_projectileDataLoaded && _currentProjectileData != null && _currentProjectileData.ProjectileId == projectileId)
            {
                return; // 已经加载了相同的数据
            }
            
            try
            {
                // 使用新的 ProjectileDataWriter 读取数据
                _currentProjectileData = ProjectileDataWriter.GetProjectile(projectileId);
                _projectileDataLoaded = _currentProjectileData != null;
                
                if (_currentProjectileData == null)
                {
                    Debug.LogWarning($"[ProjectileEffectEditorPanel] 未找到弹道ID {projectileId} 的数据");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProjectileEffectEditorPanel] 加载弹道数据失败: {ex.Message}");
                _currentProjectileData = null;
                _projectileDataLoaded = false;
            }
        }

        private void SaveProjectileData(ProjectileTableData projectileData)
        {
            try
            {
                // 使用新的 ProjectileDataWriter 保存数据
                bool success = ProjectileDataWriter.SaveProjectile(projectileData);
                if (success)
                {
                    Debug.Log($"[ProjectileEffectEditorPanel] 成功保存弹道 {projectileData.ProjectileId} 的数据");
                }
                else
                {
                    Debug.LogError($"[ProjectileEffectEditorPanel] 保存弹道 {projectileData.ProjectileId} 数据失败");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProjectileEffectEditorPanel] 保存弹道数据失败: {ex.Message}");
            }
        }

        private string GetProjectileInfo(ProjectileTableData projectileData)
        {
            if (projectileData == null) return string.Empty;
            
            return $"弹道ID: {projectileData.ProjectileId}\n" +
                   $"名称: {projectileData.ProjectileName}\n" +
                   $"类型: {projectileData.TrajectoryType}\n" +
                   $"生命周期: {projectileData.LifeTime}帧\n" +
                   $"开火特效: {(string.IsNullOrEmpty(projectileData.SpawnEffectPath) ? "无" : "已设置")}\n" +
                   $"飞行特效: {(string.IsNullOrEmpty(projectileData.LoopEffectPath) ? "无" : "已设置")}\n" +
                   $"命中特效: {(string.IsNullOrEmpty(projectileData.HitEffectPath) ? "无" : "已设置")}";
        }

        private void ShowProjectileSelector(SkillEffectTableData data)
        {
            // 这里可以实现一个弹道选择窗口
            // 暂时使用简单的输入框
            EditorUtility.DisplayDialog("提示", "弹道选择器功能待实现\n请手动输入弹道ID", "确定");
        }

        // 临时字段
        private int _newEffectId = 0;
        private bool _jsonFoldout = false;
        private string _jsonText = null;
    }
}
