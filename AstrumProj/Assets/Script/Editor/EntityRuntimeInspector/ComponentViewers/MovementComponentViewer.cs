using UnityEditor;
using Astrum.LogicCore.Components;

namespace Astrum.Editor.EntityRuntimeInspector.ComponentViewers
{
    /// <summary>
    /// MovementComponent 显示器
    /// </summary>
    public class MovementComponentViewer : IComponentViewer
    {
        public void DrawComponent(BaseComponent component)
        {
            if (component is not MovementComponent movement) return;

            EditorGUILayout.LabelField($"速度: {movement.Speed.AsFloat():F2}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"基准速度: {movement.BaseSpeed.AsFloat():F2}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"可以移动: {movement.CanMove}", EditorStyles.miniLabel);
        }
    }
}

