using UnityEditor;
using Astrum.LogicCore.Components;
using TrueSync;

namespace Astrum.Editor.EntityRuntimeInspector.ComponentViewers
{
    /// <summary>
    /// TransComponent 显示器
    /// </summary>
    public class TransComponentViewer : IComponentViewer
    {
        public void DrawComponent(BaseComponent component)
        {
            if (component is not TransComponent trans) return;

            var position = trans.GetPosition();
            EditorGUILayout.LabelField($"位置: ({position.x.AsFloat():F2}, {position.y.AsFloat():F2}, {position.z.AsFloat():F2})", EditorStyles.miniLabel);
            
            // 使用反射访问 internal 字段 Rotation
            var rotationProp = typeof(TransComponent).GetProperty("Rotation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (rotationProp != null)
            {
                var rotation = (TSQuaternion)rotationProp.GetValue(trans);
                var euler = rotation.eulerAngles;
                EditorGUILayout.LabelField($"旋转: ({euler.x.AsFloat():F2}°, {euler.y.AsFloat():F2}°, {euler.z.AsFloat():F2}°)", EditorStyles.miniLabel);
            }
        }
    }
}

