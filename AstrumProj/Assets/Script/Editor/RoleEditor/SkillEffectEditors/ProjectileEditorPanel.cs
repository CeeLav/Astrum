using System;
using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using UnityEditor;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.SkillEffectEditors
{
    internal class ProjectileEditorPanel : ISkillEffectEditorPanel
    {
        public string EffectType => "Projectile";
        public bool SupportsInlineEditing => true;

        public bool DrawContent(SkillEffectTableData data, object additionalContext)
        {
            bool changed = false;

            // 确保参数列表最小长度
            // IntParams: [0] = ProjectileId
            SkillEffectEditorUtility.EnsureListSize(data.IntParams, 1);
            
            // StringParams: [0] = SpawnEffectPath, [1] = LoopEffectPath, [2] = HitEffectPath,
            //               [3] = SpawnOffsetJson, [4] = LoopOffsetJson, [5] = HitOffsetJson
            SkillEffectEditorUtility.EnsureListSize(data.StringParams, 6);

            EditorGUILayout.LabelField("子弹特效配置", EditorStyles.boldLabel);
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
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);

            // 开火特效配置
            DrawEffectSection("开火特效 (Spawn)", data, 0, 3, ref changed);
            
            EditorGUILayout.Space(3);
            
            // 飞行特效配置
            DrawEffectSection("飞行特效 (Loop)", data, 1, 4, ref changed);
            
            EditorGUILayout.Space(3);
            
            // 命中特效配置
            DrawEffectSection("命中特效 (Hit)", data, 2, 5, ref changed);

            return changed;
        }

        private void DrawEffectSection(string title, SkillEffectTableData data, int pathIndex, int offsetIndex, ref bool changed)
        {
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                
                // 特效路径
                EditorGUI.BeginChangeCheck();
                string currentPath = data.StringParams[pathIndex] ?? string.Empty;
                
                // 加载 Prefab
                GameObject currentPrefab = null;
                if (!string.IsNullOrEmpty(currentPath))
                {
                    currentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(currentPath);
                    
                    // 如果加载失败，显示警告
                    if (currentPrefab == null)
                    {
                        EditorGUILayout.HelpBox($"无法加载特效: {currentPath}\n请检查路径是否正确或资源是否存在", MessageType.Warning);
                        
                        // 尝试刷新 AssetDatabase
                        if (GUILayout.Button("刷新资源", GUILayout.Width(80)))
                        {
                            AssetDatabase.Refresh();
                            currentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(currentPath);
                        }
                    }
                }
                
                // 显示 ObjectField
                GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "特效 Prefab", 
                    currentPrefab, 
                    typeof(GameObject), 
                    false
                );
                
                if (EditorGUI.EndChangeCheck())
                {
                    string newPath = newPrefab != null ? AssetDatabase.GetAssetPath(newPrefab) : string.Empty;
                    data.StringParams[pathIndex] = newPath;
                    changed = true;
                }
                
                // 显示当前路径（只读）
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
                
                EditorGUILayout.Space(3);
                
                // 偏移配置
                DrawOffsetConfiguration(data, offsetIndex, ref changed);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawOffsetConfiguration(SkillEffectTableData data, int offsetIndex, ref bool changed)
        {
            string currentOffsetJson = data.StringParams[offsetIndex] ?? "{}";
            
            // 解析当前偏移数据
            BulletEffectOffset offsetData = ParseOffsetData(currentOffsetJson);
            
            EditorGUI.BeginChangeCheck();
            
            // 位置偏移
            offsetData.Position = EditorGUILayout.Vector3Field("位置偏移", offsetData.Position);
            
            // 旋转偏移
            offsetData.Rotation = EditorGUILayout.Vector3Field("旋转偏移", offsetData.Rotation);
            
            // 缩放
            offsetData.Scale = EditorGUILayout.FloatField("缩放", offsetData.Scale);
            
            if (EditorGUI.EndChangeCheck())
            {
                // 序列化回JSON
                data.StringParams[offsetIndex] = JsonUtility.ToJson(offsetData);
                changed = true;
            }
            
            // 显示当前JSON预览
            if (!string.IsNullOrEmpty(currentOffsetJson) && currentOffsetJson != "{}")
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("当前配置:", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(currentOffsetJson, EditorStyles.miniLabel);
            }
        }

        private BulletEffectOffset ParseOffsetData(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "{}")
            {
                return new BulletEffectOffset();
            }
            
            try
            {
                return JsonUtility.FromJson<BulletEffectOffset>(json);
            }
            catch
            {
                return new BulletEffectOffset();
            }
        }

        [Serializable]
        private class BulletEffectOffset
        {
            public Vector3 Position = Vector3.zero;
            public Vector3 Rotation = Vector3.zero;
            public float Scale = 1.0f;
        }
    }
}
