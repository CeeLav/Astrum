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

            EditorGUILayout.LabelField($"速度: {movement.GetSpeed().AsFloat():F2}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"基准速度: {movement.GetBaseSpeed().AsFloat():F2}", EditorStyles.miniLabel);
            
            // 使用反射访问 internal 字段
            var canMoveField = typeof(MovementComponent).GetProperty("CanMove", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            bool canMove = canMoveField != null && (bool)canMoveField.GetValue(movement);
            EditorGUILayout.LabelField($"可以移动: {canMove}", EditorStyles.miniLabel);
        }
    }
}

