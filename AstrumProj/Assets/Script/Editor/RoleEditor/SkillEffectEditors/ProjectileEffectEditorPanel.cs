using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using Astrum.Editor.RoleEditor.Services;
using Astrum.Editor.RoleEditor.Persistence;
using Astrum.Editor.RoleEditor.Timeline.EventData;
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

        private const int ExtraParamJsonIndex = 0;
        private const int LegacySpawnOffsetIndex = 1;
        private const int LegacyLoopOffsetIndex = 2;
        private const int LegacyHitOffsetIndex = 3;

        private static readonly Vector3Int DefaultScaleInt = new Vector3Int(100, 100, 100);
        private static readonly Vector3Int ZeroInt = Vector3Int.zero;

        public string EffectType => "Projectile";
        public bool SupportsInlineEditing => true;

        // 缓存当前编辑的弹道配置
        private ProjectileTableData _currentProjectileData;
        private bool _projectileDataLoaded = false;
        
        public bool DrawContent(SkillEffectTableData data, object additionalContext = null)
        {
            bool skillChanged = false;
            bool projectileChanged = false;

            // 确保参数列表最小长度
            // IntParams: [0] = ProjectileId, [1] = TargetType, [2] = ExtraEffectId1, [3] = ExtraEffectId2, ...
            SkillEffectEditorUtility.EnsureListSize(data.IntParams, 1);
            
            // StringParams: [0] = ExtraEffectParams (JSON格式)
            // [1] = SpawnOffsetJson, [2] = LoopOffsetJson, [3] = HitOffsetJson
            if (data.StringParams == null)
            {
                data.StringParams = new List<string>();
            }

            SkillEffectEditorUtility.EnsureListSize(data.StringParams, LegacyHitOffsetIndex + 1);

            EditorGUILayout.LabelField("弹道效果配置", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            // ProjectileId 配置
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("弹道ID", EditorStyles.boldLabel);
                bool projectileIdChanged = SkillEffectEditorUtility.DrawIntField("ProjectileId", data.IntParams, 0);
                if (projectileIdChanged)
                {
                    skillChanged = true;
                }
                
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

                        EditorGUILayout.BeginVertical("box");
                        {
                            EditorGUILayout.LabelField("弹道基础属性", EditorStyles.boldLabel);

                            EditorGUI.BeginChangeCheck();
                            int newBaseSpeed = EditorGUILayout.IntField("基础速度", _currentProjectileData.BaseSpeed);
                            if (EditorGUI.EndChangeCheck())
                            {
                                _currentProjectileData.BaseSpeed = Mathf.Max(0, newBaseSpeed);
                                projectileChanged = true;
                            }

                            EditorGUILayout.HelpBox("基础速度以整型保存，实际速度 = 基础速度 / 100", MessageType.None);
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
                
                // 选择弹道按钮
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("📋 选择弹道", GUILayout.Width(120)))
                {
                    ShowProjectileSelector(data);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("预览发射", GUILayout.Height(24)))
            {
                var effectData = additionalContext as SkillEffectEventData;
                if (effectData != null)
                {
                    ProjectileManualPreviewService.Fire(data, effectData.SocketName, effectData.SocketOffset);
                }
                else
                {
                    Debug.LogWarning("[ProjectileEffectEditorPanel] 预览发射失败：未提供 SkillEffectEventData 上下文");
                }
            }
            if (GUILayout.Button("停止预览", GUILayout.Width(90), GUILayout.Height(24)))
            {
                ProjectileManualPreviewService.Stop();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);

            // 如果已加载弹道数据，显示特效配置
            if (_currentProjectileData != null)
            {
                EditorGUILayout.LabelField("弹道特效配置", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("这些特效配置将保存到 ProjectileTable", MessageType.Info);
                
                // 开火特效配置
                DrawProjectileEffectSection(
                    "开火特效 (Spawn)",
                    _currentProjectileData,
                    nameof(_currentProjectileData.SpawnEffectPath),
                    nameof(_currentProjectileData.SpawnEffectPositionOffset),
                    nameof(_currentProjectileData.SpawnEffectRotationOffset),
                    nameof(_currentProjectileData.SpawnEffectScaleOffset),
                    data.StringParams,
                    LegacySpawnOffsetIndex,
                    ref skillChanged,
                    ref projectileChanged);
                
                EditorGUILayout.Space(3);
                
                // 飞行特效配置
                DrawProjectileEffectSection(
                    "飞行特效 (Loop)",
                    _currentProjectileData,
                    nameof(_currentProjectileData.LoopEffectPath),
                    nameof(_currentProjectileData.LoopEffectPositionOffset),
                    nameof(_currentProjectileData.LoopEffectRotationOffset),
                    nameof(_currentProjectileData.LoopEffectScaleOffset),
                    data.StringParams,
                    LegacyLoopOffsetIndex,
                    ref skillChanged,
                    ref projectileChanged);
                
                EditorGUILayout.Space(3);
                
                // 命中特效配置
                DrawProjectileEffectSection(
                    "命中特效 (Hit)",
                    _currentProjectileData,
                    nameof(_currentProjectileData.HitEffectPath),
                    nameof(_currentProjectileData.HitEffectPositionOffset),
                    nameof(_currentProjectileData.HitEffectRotationOffset),
                    nameof(_currentProjectileData.HitEffectScaleOffset),
                    data.StringParams,
                    LegacyHitOffsetIndex,
                    ref skillChanged,
                    ref projectileChanged);
                
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
                                    skillChanged = true;
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
                        skillChanged = true;
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
                        skillChanged = true;
                    }
                    
                    if (GUILayout.Button("格式化JSON", GUILayout.Width(100)))
                    {
                        try
                        {
                            var jsonObj = JsonUtility.FromJson<object>(_jsonText);
                            _jsonText = JsonUtility.ToJson(jsonObj, true);
                            data.StringParams[0] = _jsonText;
                            skillChanged = true;
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

            if (projectileChanged && _projectileDataLoaded && _currentProjectileData != null)
            {
                SaveProjectileData(_currentProjectileData);
            }

            return skillChanged;
        }

        private void DrawProjectileEffectSection(
            string label,
            ProjectileTableData projectileData,
            string pathPropertyName,
            string positionPropertyName,
            string rotationPropertyName,
            string scalePropertyName,
            List<string> legacyStringParams,
            int legacyOffsetIndex,
            ref bool skillChanged,
            ref bool projectileChanged)
        {
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                
                // 获取当前路径
                var pathProperty = typeof(ProjectileTableData).GetProperty(pathPropertyName);
                var positionProperty = typeof(ProjectileTableData).GetProperty(positionPropertyName);
                var rotationProperty = typeof(ProjectileTableData).GetProperty(rotationPropertyName);
                var scaleProperty = typeof(ProjectileTableData).GetProperty(scalePropertyName);
                var currentPath = (string)pathProperty?.GetValue(projectileData);
                var currentPrefab = string.IsNullOrEmpty(currentPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(currentPath);
                
                // 允许通过 ObjectField 拖拽资源
                EditorGUI.BeginChangeCheck();
                var newPrefab = (GameObject)EditorGUILayout.ObjectField("特效资源", currentPrefab, typeof(GameObject), false);
                if (EditorGUI.EndChangeCheck())
                {
                    string newPath = newPrefab != null ? AssetDatabase.GetAssetPath(newPrefab) : string.Empty;
                    pathProperty?.SetValue(projectileData, newPath);
                    currentPath = newPath;
                    currentPrefab = newPrefab;
                    projectileChanged = true;
                }
                
                // 允许手动输入路径
                EditorGUI.BeginChangeCheck();
                var editedPath = EditorGUILayout.TextField("资源路径", currentPath ?? string.Empty);
                if (EditorGUI.EndChangeCheck())
                {
                    pathProperty?.SetValue(projectileData, editedPath);
                    currentPath = editedPath;
                    currentPrefab = string.IsNullOrEmpty(currentPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(currentPath);
                    projectileChanged = true;
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
                            pathProperty?.SetValue(projectileData, relativePath);
                            currentPath = relativePath;
                            currentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(currentPath);
                            projectileChanged = true;
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
                    currentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(currentPath);
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
                
                EditorGUILayout.Space(3);

                SkillEffectEditorUtility.EnsureListSize(legacyStringParams, legacyOffsetIndex + 1);
                string legacyJson = legacyStringParams[legacyOffsetIndex];

                var positionList = EnsureIntList(projectileData, positionProperty, ZeroInt);
                var rotationList = EnsureIntList(projectileData, rotationProperty, ZeroInt);
                var scaleList = EnsureIntList(projectileData, scaleProperty, DefaultScaleInt);

                if (!string.IsNullOrEmpty(legacyJson))
                {
                    MigrateLegacyOffset(label, legacyJson, positionList, rotationList, scaleList);
                    legacyStringParams[legacyOffsetIndex] = string.Empty;
                    skillChanged = true;
                    projectileChanged = true;
                }

                var position = ProjectileEffectOffsetConversion.ToVector3Int(positionList, ZeroInt);
                var rotation = ProjectileEffectOffsetConversion.ToVector3Int(rotationList, ZeroInt);
                var scale = ProjectileEffectOffsetConversion.ToVector3Int(scaleList, DefaultScaleInt);

                bool offsetChanged = DrawEffectOffsetSection(ref position, ref rotation, ref scale);
                if (offsetChanged)
                {
                    UpdateListFromVector(positionList, position);
                    UpdateListFromVector(rotationList, rotation);
                    UpdateListFromVector(scaleList, EnsureScaleVector(scale));
                    projectileChanged = true;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private bool DrawEffectOffsetSection(ref Vector3Int position, ref Vector3Int rotation, ref Vector3Int scale)
        {
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("偏移与缩放", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                var newPosition = EditorGUILayout.Vector3IntField("位置偏移", position);
                var newRotation = EditorGUILayout.Vector3IntField("旋转偏移", rotation);
                var newScale = EditorGUILayout.Vector3IntField("缩放", scale);
                bool fieldChanged = EditorGUI.EndChangeCheck();

                if (fieldChanged)
                {
                    position = newPosition;
                    rotation = newRotation;
                    scale = newScale;
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("重置", GUILayout.Width(70)))
                {
                    position = Vector3Int.zero;
                    rotation = Vector3Int.zero;
                    scale = DefaultScaleInt;
                    fieldChanged = true;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;

                if (scale == Vector3Int.zero)
                {
                    scale = DefaultScaleInt;
                }

                EditorGUILayout.HelpBox("缩放以整型表示，100 = 1.0", MessageType.None);

                EditorGUILayout.EndVertical();

                return fieldChanged;
            }
        }

        private void LoadProjectileData(int projectileId)
        {
            if (projectileId <= 0)
            {
                _currentProjectileData = null;
                _projectileDataLoaded = false;
                return;
            }
            
            if (_projectileDataLoaded && _currentProjectileData != null && _currentProjectileData.ProjectileId == projectileId)
            {
                return; // 已经加载了相同的数据
            }
            
            bool retried = false;
            while (true)
            {
                try
                {
                    _currentProjectileData = ProjectileDataReader.GetProjectile(projectileId);
                    _projectileDataLoaded = _currentProjectileData != null;
                    
                    if (_currentProjectileData == null)
                    {
                        if (!retried)
                        {
                            ProjectileDataReader.ClearCache();
                            retried = true;
                            continue;
                        }
                        
                        Debug.LogWarning($"[ProjectileEffectEditorPanel] 未找到弹道ID {projectileId} 的数据");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ProjectileEffectEditorPanel] 加载弹道数据失败: {ex.Message}");
                    _currentProjectileData = null;
                    _projectileDataLoaded = false;
                }
                
                break;
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
                    ProjectileDataReader.ClearCache();
                    LoadProjectileData(projectileData.ProjectileId);
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
                   $"基础速度: {projectileData.BaseSpeed} (≈ {projectileData.BaseSpeed * ProjectileEffectOffsetConversion.SpeedUnit:F2})\n" +
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

        private static List<int> EnsureIntList(ProjectileTableData data, PropertyInfo property, Vector3Int defaultValue)
        {
            if (property == null)
            {
                return new List<int> { defaultValue.x, defaultValue.y, defaultValue.z };
            }

            var list = property.GetValue(data) as List<int>;
            if (list == null || list.Count != 3)
            {
                list = new List<int> { defaultValue.x, defaultValue.y, defaultValue.z };
                property.SetValue(data, list);
            }

            return list;
        }

        private static void UpdateListFromVector(List<int> list, Vector3Int value)
        {
            if (list == null)
            {
                return;
            }

            if (list.Count < 3)
            {
                list.Clear();
                list.Add(value.x);
                list.Add(value.y);
                list.Add(value.z);
            }
            else
            {
                list[0] = value.x;
                list[1] = value.y;
                list[2] = value.z;
            }
        }

        private static Vector3Int EnsureScaleVector(Vector3Int scale)
        {
            return scale == Vector3Int.zero ? DefaultScaleInt : scale;
        }

        private static void MigrateLegacyOffset(string label, string legacyJson, List<int> positionList, List<int> rotationList, List<int> scaleList)
        {
            if (string.IsNullOrEmpty(legacyJson))
            {
                return;
            }

            var legacyOffset = ProjectileEffectOffsetUtility.Parse(legacyJson);

            ProjectileEffectOffsetConversion.FromVector3(legacyOffset.Position, ProjectileEffectOffsetConversion.PositionUnit, positionList);
            ProjectileEffectOffsetConversion.FromVector3(legacyOffset.Rotation, ProjectileEffectOffsetConversion.RotationUnit, rotationList);
            ProjectileEffectOffsetConversion.FromVector3(legacyOffset.Scale, ProjectileEffectOffsetConversion.ScaleUnit, scaleList);

            var scaleVector = ProjectileEffectOffsetConversion.ToVector3Int(scaleList, DefaultScaleInt);
            if (scaleVector == Vector3Int.zero)
            {
                ProjectileEffectOffsetConversion.FromVector3Int(DefaultScaleInt, scaleList);
            }

            Debug.Log($"[ProjectileEffectEditorPanel] 已将旧版 {label} 偏移数据迁移到整型配置");
        }
    }
}
