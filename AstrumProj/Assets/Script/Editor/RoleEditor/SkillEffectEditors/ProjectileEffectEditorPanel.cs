using System;
using System.Collections.Generic;
using System.Linq;
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
        private const int SpawnOffsetIndex = 1;
        private const int LoopOffsetIndex = 2;
        private const int HitOffsetIndex = 3;

        public string EffectType => "Projectile";
        public bool SupportsInlineEditing => true;

        private const string SocketNamePrefKeyPrefix = "RoleEditor.Projectile.SocketName.";
        private const string SocketOffsetPrefKeyPrefix = "RoleEditor.Projectile.SocketOffset.";

        // 缓存当前编辑的弹道配置
        private ProjectileTableData _currentProjectileData;
        private bool _projectileDataLoaded = false;
        
        // 挂点配置（独立于 SkillEffectTable，存储在 EditorPrefs 中）
        private string _socketName = string.Empty;
        private Vector3 _socketOffset = Vector3.zero;
        private int _socketConfigEffectId = -1;

        public bool DrawContent(SkillEffectTableData data, object additionalContext = null)
        {
            bool changed = false;

            // 确保参数列表最小长度
            // IntParams: [0] = ProjectileId, [1] = TargetType, [2] = ExtraEffectId1, [3] = ExtraEffectId2, ...
            SkillEffectEditorUtility.EnsureListSize(data.IntParams, 1);
            
            // StringParams: [0] = ExtraEffectParams (JSON格式)
            // [1] = SpawnOffsetJson, [2] = LoopOffsetJson, [3] = HitOffsetJson
            if (data.StringParams == null)
            {
                data.StringParams = new List<string>();
            }
            SkillEffectEditorUtility.EnsureListSize(data.StringParams, HitOffsetIndex + 1);

            int effectId = data.SkillEffectId;
            EnsureSocketConfig(effectId);

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
            
            // 挂点配置
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("挂点配置", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                string newSocketName = EditorGUILayout.TextField("Socket 名称", _socketName);
                if (EditorGUI.EndChangeCheck())
                {
                    _socketName = string.IsNullOrWhiteSpace(newSocketName) ? string.Empty : newSocketName.Trim();
                    SaveSocketConfig(effectId);
                }

                EditorGUI.BeginChangeCheck();
                Vector3 newSocketOffset = EditorGUILayout.Vector3Field("Socket 偏移", _socketOffset);
                if (EditorGUI.EndChangeCheck())
                {
                    _socketOffset = newSocketOffset;
                    SaveSocketConfig(effectId);
                }

                if (!string.IsNullOrEmpty(_socketName))
                {
                    EditorGUILayout.HelpBox($"将从挂点 '{_socketName}' 发射", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("未设置挂点，将使用角色根节点", MessageType.Info);
                }

                if (effectId <= 0)
                {
                    EditorGUILayout.HelpBox("请先保存 SkillEffect 以获得有效的 ID，挂点配置会存储在本地 EditorPrefs。", MessageType.Warning);
                }
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("预览发射", GUILayout.Height(24)))
            {
                var effectData = (SkillEffectEventData)additionalContext;
                ProjectileManualPreviewService.Fire(data, effectData.SocketName, effectData.SocketOffset);
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
                    data.StringParams,
                    SpawnOffsetIndex,
                    ref changed);
                
                EditorGUILayout.Space(3);
                
                // 飞行特效配置
                DrawProjectileEffectSection(
                    "飞行特效 (Loop)",
                    _currentProjectileData,
                    nameof(_currentProjectileData.LoopEffectPath),
                    data.StringParams,
                    LoopOffsetIndex,
                    ref changed);
                
                EditorGUILayout.Space(3);
                
                // 命中特效配置
                DrawProjectileEffectSection(
                    "命中特效 (Hit)",
                    _currentProjectileData,
                    nameof(_currentProjectileData.HitEffectPath),
                    data.StringParams,
                    HitOffsetIndex,
                    ref changed);
                
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

        private void DrawProjectileEffectSection(
            string label,
            ProjectileTableData projectileData,
            string propertyName,
            List<string> stringParams,
            int offsetIndex,
            ref bool changed)
        {
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                
                // 获取当前路径
                var currentPath = (string)typeof(ProjectileTableData).GetProperty(propertyName)?.GetValue(projectileData);
                var currentPrefab = string.IsNullOrEmpty(currentPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(currentPath);
                
                // 允许通过 ObjectField 拖拽资源
                EditorGUI.BeginChangeCheck();
                var newPrefab = (GameObject)EditorGUILayout.ObjectField("特效资源", currentPrefab, typeof(GameObject), false);
                if (EditorGUI.EndChangeCheck())
                {
                    string newPath = newPrefab != null ? AssetDatabase.GetAssetPath(newPrefab) : string.Empty;
                    typeof(ProjectileTableData).GetProperty(propertyName)?.SetValue(projectileData, newPath);
                    currentPath = newPath;
                    currentPrefab = newPrefab;
                    changed = true;
                }
                
                // 允许手动输入路径
                EditorGUI.BeginChangeCheck();
                var editedPath = EditorGUILayout.TextField("资源路径", currentPath ?? string.Empty);
                if (EditorGUI.EndChangeCheck())
                {
                    typeof(ProjectileTableData).GetProperty(propertyName)?.SetValue(projectileData, editedPath);
                    currentPath = editedPath;
                    currentPrefab = string.IsNullOrEmpty(currentPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(currentPath);
                    changed = true;
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
                            currentPath = relativePath;
                            currentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(currentPath);
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
                DrawEffectOffsetSection(stringParams, offsetIndex, ref changed);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEffectOffsetSection(List<string> stringParams, int index, ref bool changed)
        {
            SkillEffectEditorUtility.EnsureListSize(stringParams, index + 1);

            var offset = ProjectileEffectOffsetUtility.Parse(stringParams[index]);

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("偏移与缩放", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                offset.Position = EditorGUILayout.Vector3Field("位置偏移", offset.Position);
                offset.Rotation = EditorGUILayout.Vector3Field("旋转偏移", offset.Rotation);
                offset.Scale = EditorGUILayout.Vector3Field("缩放", offset.Scale);
                bool fieldChanged = EditorGUI.EndChangeCheck();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("重置", GUILayout.Width(70)))
                {
                    offset = ProjectileEffectOffset.Default();
                    fieldChanged = true;
                }
                EditorGUILayout.EndHorizontal();

                if (fieldChanged)
                {
                    stringParams[index] = ProjectileEffectOffsetUtility.ToJson(offset);
                    changed = true;
                }

                EditorGUI.indentLevel--;
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
                _currentProjectileData = ProjectileDataReader.GetProjectile(projectileId);
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

        private void EnsureSocketConfig(int effectId)
        {
            if (effectId == _socketConfigEffectId)
            {
                return;
            }

            LoadSocketConfig(effectId);
        }

        private void LoadSocketConfig(int effectId)
        {
            if (effectId <= 0)
            {
                _socketName = string.Empty;
                _socketOffset = Vector3.zero;
                _socketConfigEffectId = effectId;
                return;
            }

            _socketName = EditorPrefs.GetString(GetSocketNameKey(effectId), string.Empty);
            _socketOffset = new Vector3(
                EditorPrefs.GetFloat(GetSocketOffsetKey(effectId, "x"), 0f),
                EditorPrefs.GetFloat(GetSocketOffsetKey(effectId, "y"), 0f),
                EditorPrefs.GetFloat(GetSocketOffsetKey(effectId, "z"), 0f)
            );
            _socketConfigEffectId = effectId;
        }

        private void SaveSocketConfig(int effectId)
        {
            if (effectId <= 0)
            {
                return;
            }

            EditorPrefs.SetString(GetSocketNameKey(effectId), _socketName ?? string.Empty);
            EditorPrefs.SetFloat(GetSocketOffsetKey(effectId, "x"), _socketOffset.x);
            EditorPrefs.SetFloat(GetSocketOffsetKey(effectId, "y"), _socketOffset.y);
            EditorPrefs.SetFloat(GetSocketOffsetKey(effectId, "z"), _socketOffset.z);
            _socketConfigEffectId = effectId;
        }

        private static string GetSocketNameKey(int effectId) => $"{SocketNamePrefKeyPrefix}{effectId}";
        private static string GetSocketOffsetKey(int effectId, string axis) => $"{SocketOffsetPrefKeyPrefix}{effectId}.{axis}";

        // 临时字段
        private int _newEffectId = 0;
        private bool _jsonFoldout = false;
        private string _jsonText = null;
    }
}
