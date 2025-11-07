using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Timeline.EventData;
using Astrum.Editor.RoleEditor.Windows;
using Astrum.Editor.RoleEditor.Services;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Timeline.Renderers
{
    /// <summary>
    /// 技能效果轨道渲染器
    /// 支持单帧和多帧事件的可视化渲染
    /// </summary>
    public static class SkillEffectTrackRenderer
    {
        // === 渲染常量 ===
        private const float SINGLE_FRAME_WIDTH = 12f;
        private const float DIAMOND_SIZE = 10f;
        private const float LABEL_OFFSET_X = 15f;
        
        // === 编辑器状态 ===
        private static System.Collections.Generic.Dictionary<string, bool> _effectFoldouts = new System.Collections.Generic.Dictionary<string, bool>();
        
        /// <summary>
        /// 渲染技能效果事件
        /// </summary>
        public static void RenderEvent(Rect rect, TimelineEvent evt)
        {
            var effectData = evt.GetEventData<SkillEffectEventData>();
            if (effectData == null)
            {
                effectData = SkillEffectEventData.CreateDefault();
            }
            
            // 刷新效果详情（如果需要）
            if (effectData.EffectIds != null && effectData.EffectIds.Count > 0 && string.IsNullOrEmpty(effectData.EffectName))
            {
                effectData.RefreshFromTable();
            }
            
            // 判断单帧还是多帧
            bool isSingleFrame = evt.IsSingleFrame();
            
            if (isSingleFrame)
            {
                RenderSingleFrameEvent(rect, evt, effectData);
            }
            else
            {
                RenderMultiFrameEvent(rect, evt, effectData);
            }
        }
        
        /// <summary>
        /// 渲染单帧事件（菱形标记）
        /// </summary>
        private static void RenderSingleFrameEvent(Rect rect, TimelineEvent evt, SkillEffectEventData effectData)
        {
            // 获取效果类型颜色
            Color effectColor = effectData.GetEffectTypeColor();
            
            // 绘制菱形标记
            Rect diamondRect = new Rect(rect.x + 5, rect.y + (rect.height - DIAMOND_SIZE) / 2, DIAMOND_SIZE, DIAMOND_SIZE);
            DrawDiamond(diamondRect, effectColor);
            
            // 绘制触发类型图标
            Rect iconRect = new Rect(rect.x + 2, rect.y + 2, 12, 12);
            GUI.color = Color.white;
            GUI.Label(iconRect, effectData.GetTriggerIcon(), EditorStyles.miniLabel);
            
            // 绘制效果信息
            Rect labelRect = new Rect(rect.x + LABEL_OFFSET_X, rect.y + 5, rect.width - LABEL_OFFSET_X - 5, 20);
            
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.normal.textColor = Color.white;
            
            string displayText = effectData.GetDisplayName();
            
            // 如果有碰撞盒，显示碰撞盒类型
            if (!string.IsNullOrEmpty(effectData.CollisionShapeType))
            {
                displayText += $" [{effectData.CollisionShapeType}]";
            }
            
            GUI.Label(labelRect, displayText, labelStyle);
            
            GUI.color = Color.white;
        }
        
        /// <summary>
        /// 渲染多帧事件（矩形区间）
        /// </summary>
        private static void RenderMultiFrameEvent(Rect rect, TimelineEvent evt, SkillEffectEventData effectData)
        {
            // 获取效果类型颜色
            Color effectColor = effectData.GetEffectTypeColor();
            
            // 绘制背景区间
            Color bgColor = new Color(effectColor.r, effectColor.g, effectColor.b, 0.3f);
            EditorGUI.DrawRect(rect, bgColor);
            
            // 绘制边框
            Rect borderRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            DrawBorder(borderRect, effectColor, 2f);
            
            // 绘制触发类型图标（左侧）
            Rect iconRect = new Rect(rect.x + 3, rect.y + 3, 14, 14);
            GUI.color = effectColor;
            GUI.Label(iconRect, effectData.GetTriggerIcon(), EditorStyles.boldLabel);
            
            // 绘制效果信息
            Rect labelRect = new Rect(rect.x + 20, rect.y + 5, rect.width - 25, 20);
            
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontStyle = FontStyle.Bold;
            
            string displayText = effectData.GetDisplayName();
            
            // 如果有碰撞盒，显示碰撞盒类型
            if (!string.IsNullOrEmpty(effectData.CollisionShapeType))
            {
                displayText += $" [{effectData.CollisionShapeType}]";
            }
            
            // 显示帧范围
            int duration = evt.GetDuration();
            displayText += $" ({duration}帧)";
            
            GUI.Label(labelRect, displayText, labelStyle);
            
            // 绘制持续指示（右侧）
            if (duration > 3)
            {
                Rect durationRect = new Rect(rect.xMax - 30, rect.y + rect.height - 15, 25, 12);
                GUIStyle durationStyle = new GUIStyle(EditorStyles.miniLabel);
                durationStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
                durationStyle.fontSize = 9;
                GUI.Label(durationRect, $"×{duration}", durationStyle);
            }
            
            GUI.color = Color.white;
        }
        
        /// <summary>
        /// 编辑技能效果事件
        /// </summary>
        public static bool EditEvent(TimelineEvent evt)
        {
            bool modified = false;
            
            // 获取或创建效果数据
            var effectData = evt.GetEventData<SkillEffectEventData>();
            if (effectData == null)
            {
                effectData = SkillEffectEventData.CreateDefault();
            }
            
            EditorGUILayout.LabelField("技能效果配置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // === 效果ID列表 ===
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("技能效果列表", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"共 {effectData.EffectIds?.Count ?? 0} 个", EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                
                // 显示当前效果ID列表
                if (effectData.EffectIds == null)
                {
                    effectData.EffectIds = new System.Collections.Generic.List<int>();
                }
                
                EditorGUILayout.Space(5);
                
                // 显示每个效果ID（可折叠）
                for (int i = 0; i < effectData.EffectIds.Count; i++)
                {
                    int effectId = effectData.EffectIds[i];
                    string foldoutKey = $"{evt.EventId}_effect_{i}";

                    string effectName = effectId > 0 ? $"效果_{effectId}" : "[未设置]";
                    string effectTypeKey = string.Empty;
                    float primaryValue = 0f;
                    int targetSelector = 0;
                    System.Collections.Generic.List<int> intParams = null;
                    System.Collections.Generic.List<string> stringParams = null;

                    if (effectId > 0)
                    {
                        try
                        {
                            var config = Services.SkillEffectDataReader.GetSkillEffect(effectId);
                            if (config != null)
                            {
                                effectTypeKey = config.EffectType ?? string.Empty;
                                intParams = config.IntParams ?? new System.Collections.Generic.List<int>();
                                stringParams = config.StringParams ?? new System.Collections.Generic.List<string>();
                                targetSelector = intParams.Count > 0 ? intParams[0] : 0;
                                primaryValue = ComputePrimaryValue(config);
                                effectName = BuildEffectDisplayName(effectTypeKey, primaryValue, effectId);
                            }
                        }
                        catch { }
                    }
                    
                    // 效果框
                    EditorGUILayout.BeginVertical("box");
                    {
                        // 折叠标题行
                        EditorGUILayout.BeginHorizontal();
                        {
                            // 折叠箭头和标题
                            if (!_effectFoldouts.ContainsKey(foldoutKey))
                                _effectFoldouts[foldoutKey] = false;
                            
                            _effectFoldouts[foldoutKey] = EditorGUILayout.Foldout(
                                _effectFoldouts[foldoutKey],
                                effectId > 0 ? $"效果 {i + 1}: {effectName}" : $"效果 {i + 1}: [未设置]",
                                true,
                                EditorStyles.foldoutHeader
                            );
                            
                            // 删除按钮
                            if (GUILayout.Button("✖", GUILayout.Width(25), GUILayout.Height(20)))
                            {
                                effectData.EffectIds.RemoveAt(i);
                                effectData.RefreshFromTable();
                                modified = true;
                                _effectFoldouts.Remove(foldoutKey);
                                break;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        // 折叠内容
                            if (_effectFoldouts[foldoutKey])
                        {
                            EditorGUILayout.Space(3);
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("编辑效果", GUILayout.Width(90)))
                                {
                                    int capturedEffectId = effectId;
                                    SkillEffectEditorWindow.ShowWindow(capturedEffectId, () =>
                                    {
                                        SkillEffectDataReader.ClearCache();
                                        effectData.RefreshFromTable();
                                        effectData.ParseCollisionInfo();
                                        evt.SetEventData(effectData);
                                        evt.DisplayName = effectData.GetDisplayName();
                                    });
                                }
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.Space(3);
                            
                            // 效果ID编辑和选择按钮
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUI.BeginChangeCheck();
                                int newEffectId = EditorGUILayout.IntField("效果ID", effectId);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    effectData.EffectIds[i] = newEffectId;
                                    effectData.RefreshFromTable();
                                    modified = true;
                                }
                                
                                // 选择效果按钮
                                if (GUILayout.Button("📋", GUILayout.Width(30), GUILayout.Height(18)))
                                {
                                    int capturedIndex = i; // 捕获当前索引
                                    SkillEffectSelectorWindow.ShowWindow((selectedEffectId) =>
                                    {
                                        effectData.EffectIds[capturedIndex] = selectedEffectId;
                                        effectData.RefreshFromTable();
                                        effectData.ParseCollisionInfo();
                                        
                                        // 更新事件数据
                                        evt.SetEventData(effectData);
                                        evt.DisplayName = effectData.GetDisplayName();
                                    });
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                            
                            // 显示效果详情（只读）
                            if (effectId > 0)
                            {
                                EditorGUILayout.Space(5);
                                
                                GUI.enabled = false;
                                EditorGUILayout.LabelField("效果详情", EditorStyles.boldLabel);
                                EditorGUILayout.BeginVertical("box");
                                {
                                    EditorGUILayout.TextField("类型", Services.SkillEffectDataReader.GetEffectTypeDisplayName(effectTypeKey));
                                    EditorGUILayout.TextField("主值", FormatPrimaryValue(effectTypeKey, primaryValue));
                                    EditorGUILayout.TextField("目标", GetTargetSelectorName(targetSelector));
                                    EditorGUILayout.TextField("Int 参数", intParams != null && intParams.Count > 0 ? string.Join("|", intParams) : "--");
                                    EditorGUILayout.TextField("String 参数", stringParams != null && stringParams.Count > 0 ? string.Join(" | ", stringParams) : "--");
                                }
                                EditorGUILayout.EndVertical();
                                GUI.enabled = true;
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.Space(3);
                }
                
                // 添加效果ID按钮
                if (GUILayout.Button("➕ 添加技能效果", GUILayout.Height(25)))
                {
                    effectData.EffectIds.Add(0);
                    modified = true;
                }
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // === 触发类型 ===
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("触发方式", EditorStyles.boldLabel);
                
                string[] triggerTypes = new string[] { "Direct", "Collision", "Condition" };
                string[] triggerLabels = new string[] { "→ 直接触发", "💥 碰撞触发", "❓ 条件触发" };
                int selectedIndex = System.Array.IndexOf(triggerTypes, effectData.TriggerType);
                if (selectedIndex < 0) selectedIndex = 0;
                
                EditorGUI.BeginChangeCheck();
                int newIndex = GUILayout.SelectionGrid(selectedIndex, triggerLabels, 1);
                if (EditorGUI.EndChangeCheck())
                {
                    effectData.TriggerType = triggerTypes[newIndex];
                    modified = true;
                }
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // === 碰撞盒配置（仅Collision类型） ===
            if (effectData.TriggerType == "Collision")
            {
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("碰撞盒配置", EditorStyles.boldLabel);
                    EditorGUILayout.Space(3);
                    
                    // 使用可视化碰撞盒编辑器
                    string newCollisionInfo = UI.CollisionInfoEditor.DrawCollisionInfoEditor(
                        effectData.CollisionInfo, 
                        out bool collisionModified
                    );
                    
                    if (collisionModified)
                    {
                        effectData.CollisionInfo = newCollisionInfo;
                        effectData.ParseCollisionInfo();
                        modified = true;
                    }
                }
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(5);
            }
            
            // === 帧范围提示 ===
            EditorGUILayout.BeginVertical("box");
            {
                if (evt.IsSingleFrame())
                {
                    EditorGUILayout.HelpBox($"📌 单帧事件：在第 {evt.StartFrame} 帧触发", MessageType.None);
                }
                else
                {
                    int duration = evt.GetDuration();
                    EditorGUILayout.HelpBox(
                        $"📌 多帧事件：第 {evt.StartFrame}-{evt.EndFrame} 帧持续触发\n" +
                        $"持续时间：{duration} 帧 ({duration / 60f:F2} 秒)\n" +
                        $"运行时将展开为 {duration} 个独立触发点",
                        MessageType.None
                    );
                }
            }
            EditorGUILayout.EndVertical();
            
            // 保存修改
            if (modified)
            {
                evt.SetEventData(effectData);
                
                // 更新显示名称
                string displayName = effectData.GetDisplayName();
                if (!string.IsNullOrEmpty(effectData.CollisionShapeType))
                {
                    displayName += $" [{effectData.CollisionShapeType}]";
                }
                evt.DisplayName = displayName;
            }
            
            return modified;
        }
        
        // === 私有绘制方法 ===
        
        /// <summary>
        /// 绘制菱形
        /// </summary>
        private static void DrawDiamond(Rect rect, Color color)
        {
            Vector3[] points = new Vector3[4];
            Vector2 center = rect.center;
            float halfSize = rect.width / 2f;
            
            // 上
            points[0] = new Vector3(center.x, center.y - halfSize);
            // 右
            points[1] = new Vector3(center.x + halfSize, center.y);
            // 下
            points[2] = new Vector3(center.x, center.y + halfSize);
            // 左
            points[3] = new Vector3(center.x - halfSize, center.y);
            
            // 绘制填充菱形
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawAAConvexPolygon(points);
            Handles.EndGUI();
            
            // 绘制边框
            Handles.BeginGUI();
            Handles.color = Color.white;
            Handles.DrawAAPolyLine(3f, points[0], points[1], points[2], points[3], points[0]);
            Handles.EndGUI();
        }
        
        /// <summary>
        /// 绘制边框
        /// </summary>
        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            // 上
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            // 下
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            // 左
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            // 右
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
        
        /// <summary>
        /// 绘制工具提示
        /// </summary>
        public static void DrawTooltip(Rect rect, TimelineEvent evt)
        {
            var effectData = evt.GetEventData<SkillEffectEventData>();
            if (effectData == null) return;
            
            string tooltip = effectData.GetDetailText();
            
            if (!string.IsNullOrEmpty(tooltip))
            {
                GUIStyle tooltipStyle = new GUIStyle("box");
                tooltipStyle.normal.textColor = Color.white;
                tooltipStyle.fontSize = 11;
                tooltipStyle.padding = new RectOffset(8, 8, 5, 5);
                
                Vector2 size = tooltipStyle.CalcSize(new GUIContent(tooltip));
                Rect tooltipRect = new Rect(
                    rect.x + rect.width + 5,
                    rect.y,
                    Mathf.Min(size.x + 10, 250),
                    size.y + 10
                );
                
                GUI.Box(tooltipRect, tooltip, tooltipStyle);
            }
        }

        private static string BuildEffectDisplayName(string effectTypeKey, float primaryValue, int effectId)
        {
            string typeName = SkillEffectDataReader.GetEffectTypeDisplayName(effectTypeKey);
            string primaryText = FormatPrimaryValue(effectTypeKey, primaryValue);
            return primaryValue > 0f ? $"{typeName} {primaryText}" : $"{typeName}_{effectId}";
        }

        private static float ComputePrimaryValue(SkillEffectTableData config)
        {
            var ints = config.IntParams ?? new List<int>();
            switch ((config.EffectType ?? string.Empty).ToLower())
            {
                case "damage":
                case "heal":
                    return ints.Count > 2 ? ints[2] / 10f : 0f;
                case "knockback":
                    return ints.Count > 1 ? ints[1] / 1000f : 0f;
                case "status":
                    return ints.Count > 2 ? ints[2] / 1000f : 0f;
                case "teleport":
                    return ints.Count > 1 ? ints[1] / 1000f : 0f;
                default:
                    return 0f;
            }
        }

        private static string FormatPrimaryValue(string effectTypeKey, float primaryValue)
        {
            if (primaryValue <= 0f)
                return "--";

            switch ((effectTypeKey ?? string.Empty).ToLower())
            {
                case "damage":
                case "heal":
                    return $"{primaryValue:0.#}%";
                case "knockback":
                case "teleport":
                    return $"{primaryValue:0.##}m";
                case "status":
                    return $"{primaryValue:0.##}s";
                default:
                    return primaryValue.ToString("0.##");
            }
        }

        private static string GetTargetSelectorName(int selector)
        {
            return selector switch
            {
                0 => "自身",
                1 => "敌人",
                2 => "友军",
                3 => "区域",
                _ => "未知"
            };
        }
    }
}

