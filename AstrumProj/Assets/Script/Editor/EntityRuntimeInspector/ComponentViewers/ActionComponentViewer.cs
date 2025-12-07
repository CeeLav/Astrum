using UnityEngine;
using UnityEditor;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.ActionSystem;
using TrueSync;
using System.Linq;

namespace Astrum.Editor.EntityRuntimeInspector.ComponentViewers
{
    /// <summary>
    /// ActionComponent 显示器
    /// </summary>
    public class ActionComponentViewer : IComponentViewer
    {
        private bool _showInputCommands = true;
        private bool _showAvailableActions = true;
        private bool _showPreorders = false;

        public void DrawComponent(BaseComponent component)
        {
            if (component is not ActionComponent action) return;

            // === 当前动作状态 ===
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("当前动作状态", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField($"动作ID: {action.CurrentActionId}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"当前帧: {action.CurrentFrame}", GUILayout.Width(100));
                }
                EditorGUILayout.EndHorizontal();
                
                if (action.CurrentAction != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField($"类型: {action.CurrentAction.Catalog ?? "N/A"}", GUILayout.Width(150));
                        EditorGUILayout.LabelField($"持续时间: {action.CurrentAction.Duration} 帧", GUILayout.Width(120));
                        EditorGUILayout.LabelField($"优先级: {action.CurrentAction.Priority}", GUILayout.Width(80));
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    if (action.CurrentAction.AutoTerminate)
                    {
                        EditorGUILayout.LabelField("自动终止: ✓", EditorStyles.miniLabel);
                    }
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // === 输入命令 ===
            _showInputCommands = EditorGUILayout.Foldout(_showInputCommands, $"输入命令 ({action.InputCommands?.Count ?? 0})", true);
            if (_showInputCommands && action.InputCommands != null && action.InputCommands.Count > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    foreach (var cmd in action.InputCommands)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField($"• {cmd.CommandName}", GUILayout.Width(120));
                            EditorGUILayout.LabelField($"有效帧: {cmd.ValidFrames}", GUILayout.Width(80));
                            
                            var targetX = FP.FromRaw(cmd.TargetPositionX);
                            var targetZ = FP.FromRaw(cmd.TargetPositionZ);
                            if (targetX != 0 || targetZ != 0)
                            {
                                EditorGUILayout.LabelField($"目标: ({targetX.AsFloat():F2}, {targetZ.AsFloat():F2})", EditorStyles.miniLabel);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(3);

            // === 可用动作列表 ===
            _showAvailableActions = EditorGUILayout.Foldout(_showAvailableActions, $"可用动作 ({action.AvailableActions?.Count ?? 0})", true);
            if (_showAvailableActions && action.AvailableActions != null && action.AvailableActions.Count > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // 按 ID 排序显示
                    var sortedActions = action.AvailableActions.OrderBy(kvp => kvp.Key).ToList();
                    
                    foreach (var kvp in sortedActions)
                    {
                        var actionId = kvp.Key;
                        var actionInfo = kvp.Value;
                        var isCurrent = actionId == action.CurrentActionId;
                        
                        EditorGUILayout.BeginVertical(isCurrent ? EditorStyles.selectionRect : EditorStyles.helpBox);
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                var label = isCurrent ? $"▶ {actionId} ({actionInfo.Catalog ?? "N/A"})" : $"{actionId} ({actionInfo.Catalog ?? "N/A"})";
                                EditorGUILayout.LabelField(label, isCurrent ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.Width(200));
                                EditorGUILayout.LabelField($"优先级: {actionInfo.Priority}", GUILayout.Width(80));
                                EditorGUILayout.LabelField($"持续时间: {actionInfo.Duration} 帧", GUILayout.Width(100));
                            }
                            EditorGUILayout.EndHorizontal();
                            
                            // 显示匹配命令
                            if (actionInfo.Commands != null && actionInfo.Commands.Count > 0)
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.LabelField("匹配命令:", EditorStyles.miniLabel);
                                EditorGUILayout.BeginHorizontal();
                                {
                                    var commandNames = actionInfo.Commands.Select(c => c.CommandName).Where(n => !string.IsNullOrEmpty(n));
                                    EditorGUILayout.LabelField(string.Join(", ", commandNames), EditorStyles.miniLabel);
                                }
                                EditorGUILayout.EndHorizontal();
                                EditorGUI.indentLevel--;
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(3);

            // === 预订单和外部预约 ===
            var preorderCount = action.PreorderActions?.Count ?? 0;
            var externalCount = action.ExternalPreorders?.Count ?? 0;
            if (preorderCount > 0 || externalCount > 0)
            {
                _showPreorders = EditorGUILayout.Foldout(_showPreorders, $"预订单 (预订单: {preorderCount}, 外部: {externalCount})", true);
                if (_showPreorders)
                {
                    EditorGUI.indentLevel++;
                    
                    if (preorderCount > 0)
                    {
                        EditorGUILayout.LabelField("预订单列表:", EditorStyles.miniLabel);
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        {
                            foreach (var preorder in action.PreorderActions)
                            {
                                EditorGUILayout.LabelField($"• ActionId: {preorder.ActionId}, 优先级: {preorder.Priority}", EditorStyles.miniLabel);
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                    
                    if (externalCount > 0)
                    {
                        EditorGUILayout.LabelField("外部预约:", EditorStyles.miniLabel);
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        {
                            foreach (var kvp in action.ExternalPreorders)
                            {
                                EditorGUILayout.LabelField($"• [{kvp.Key}] ActionId: {kvp.Value.ActionId}, 优先级: {kvp.Value.Priority}", EditorStyles.miniLabel);
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}

