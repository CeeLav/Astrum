using UnityEditor;
using Astrum.LogicCore.Components;

namespace Astrum.Editor.EntityRuntimeInspector.ComponentViewers
{
    /// <summary>
    /// ActionComponent 显示器
    /// </summary>
    public class ActionComponentViewer : IComponentViewer
    {
        public void DrawComponent(BaseComponent component)
        {
            if (component is not ActionComponent action) return;

            EditorGUILayout.LabelField($"当前动作ID: {action.CurrentActionId}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"当前帧: {action.CurrentFrame}", EditorStyles.miniLabel);
            
            if (action.CurrentAction != null)
            {
                EditorGUILayout.LabelField($"动作ID: {action.CurrentAction.Id}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"动作类型: {action.CurrentAction.Catalog ?? "N/A"}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"持续时间: {action.CurrentAction.Duration} 帧", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"优先级: {action.CurrentAction.Priority}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"自动终止: {action.CurrentAction.AutoTerminate}", EditorStyles.miniLabel);
            }

            EditorGUILayout.LabelField($"输入命令数: {action.InputCommands?.Count ?? 0}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"预订单数: {action.PreorderActions?.Count ?? 0}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"外部预约数: {action.ExternalPreorders?.Count ?? 0}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"可用动作数: {action.AvailableActions?.Count ?? 0}", EditorStyles.miniLabel);
        }
    }
}

