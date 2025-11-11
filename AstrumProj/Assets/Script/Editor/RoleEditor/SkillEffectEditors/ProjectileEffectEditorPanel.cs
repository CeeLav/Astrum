using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using Astrum.Editor.RoleEditor.Services;
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

        public bool DrawContent(SkillEffectTableData data)
        {
            bool changed = false;

            // 确保参数列表最小长度
            SkillEffectEditorUtility.EnsureListSize(data.IntParams, 1);
            SkillEffectEditorUtility.EnsureListSize(data.StringParams, 1);

            EditorGUILayout.LabelField("抛射物效果参数", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            // ProjectileId 选择
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("弹道配置", EditorStyles.boldLabel);
                
                // 主 ProjectileId
                changed |= SkillEffectEditorUtility.DrawIntField("弹道ID (ProjectileId)", data.IntParams, 0);
                
                // 显示当前选择的弹道信息
                int projectileId = data.IntParams[0];
                if (projectileId > 0)
                {
                    var projectileInfo = GetProjectileInfo(projectileId);
                    if (!string.IsNullOrEmpty(projectileInfo))
                    {
                        EditorGUILayout.HelpBox(projectileInfo, MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox($"未找到弹道ID {projectileId} 的配置信息", MessageType.Warning);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("请选择或输入有效的弹道ID", MessageType.Info);
                }
                
                // 快速选择按钮
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("📋 选择弹道", GUILayout.Width(120)))
                {
                    // 这里可以弹出一个弹道选择窗口
                    ShowProjectileSelector(data);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);

            // ExtraEffectIds 列表
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("额外效果ID (ExtraEffectIds)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("这些效果会在弹道命中时额外触发", MessageType.None);
                
                // 显示当前已有的额外效果（从索引1开始）
                var extraEffectIds = data.IntParams.Skip(1).ToList();
                
                EditorGUILayout.LabelField($"当前额外效果数量: {extraEffectIds.Count}");
                
                // 添加新效果ID
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("添加效果ID:", GUILayout.Width(80));
                _newEffectId = EditorGUILayout.IntField(_newEffectId, GUILayout.Width(60));
                if (GUILayout.Button("+", GUILayout.Width(25)))
                {
                    if (_newEffectId > 0)
                    {
                        data.IntParams.Add(_newEffectId);
                        _newEffectId = 0;
                        changed = true;
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(3);
                
                // 显示已添加的效果列表
                if (extraEffectIds.Count > 0)
                {
                    EditorGUILayout.LabelField("已添加的效果:", EditorStyles.miniBoldLabel);
                    for (int i = 1; i < data.IntParams.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  效果 {i}: {data.IntParams[i]}", EditorStyles.miniLabel);
                        if (GUILayout.Button("✖", GUILayout.Width(20), GUILayout.Height(15)))
                        {
                            data.IntParams.RemoveAt(i);
                            changed = true;
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);

            // JSON 覆写配置
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("轨迹覆写配置 (JSON)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("可选：覆写弹道的轨迹参数，如速度、重力等", MessageType.None);
                
                // 当前 JSON 值
                string currentJson = data.StringParams.Count > 0 ? data.StringParams[0] : "";
                
                EditorGUI.BeginChangeCheck();
                _jsonFoldout = EditorGUILayout.Foldout(_jsonFoldout, "JSON 编辑器", true);
                if (EditorGUI.EndChangeCheck())
                {
                    changed = true;
                }
                
                if (_jsonFoldout)
                {
                    EditorGUI.BeginChangeCheck();
                    _jsonText = EditorGUILayout.TextArea(
                        string.IsNullOrEmpty(_jsonText) ? currentJson : _jsonText, 
                        GUILayout.Height(80), 
                        GUILayout.ExpandWidth(true)
                    );
                    
                    EditorGUILayout.Space(3);
                    
                    // JSON 格式化按钮
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("格式化 JSON", GUILayout.Width(100)))
                    {
                        try
                        {
                            var jsonObject = JsonUtility.FromJson<object>(_jsonText);
                            _jsonText = JsonUtility.ToJson(jsonObject, true);
                        }
                        catch
                        {
                            EditorUtility.DisplayDialog("错误", "JSON 格式无效，无法格式化", "确定");
                        }
                    }
                    
                    if (GUILayout.Button("应用", GUILayout.Width(60)))
                    {
                        data.StringParams[0] = _jsonText;
                        changed = true;
                        _jsonText = null; // 清空缓存
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    if (EditorGUI.EndChangeCheck() && string.IsNullOrEmpty(_jsonText))
                    {
                        changed = true;
                    }
                }
                
                // 显示当前 JSON 预览
                if (!string.IsNullOrEmpty(currentJson) && currentJson != "{}" && !_jsonFoldout)
                {
                    EditorGUILayout.LabelField("当前配置:", EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField(currentJson, EditorStyles.miniLabel);
                }
                
                // 示例配置
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("示例:", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("{ \"BaseSpeed\": 0.8, \"Gravity\": [0, -0.05, 0] }", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();

            return changed;
        }

        private void ShowProjectileSelector(SkillEffectTableData data)
        {
            // 这里可以实现一个弹道选择窗口
            // 暂时使用简单的输入框
            EditorUtility.DisplayDialog("提示", "弹道选择器功能待实现\n请手动输入弹道ID", "确定");
        }

        private string GetProjectileInfo(int projectileId)
        {
            try
            {
                // 这里可以从 ProjectileTable 读取配置信息
                // 暂时返回简单的信息
                return $"弹道ID: {projectileId}\n类型: 待加载\n生命周期: 待加载";
            }
            catch
            {
                return string.Empty;
            }
        }

        // 临时字段
        private int _newEffectId = 0;
        private bool _jsonFoldout = false;
        private string _jsonText = null;
    }
}
