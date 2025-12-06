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

            EditorGUILayout.LabelField($"位置: ({trans.Position.x.AsFloat():F2}, {trans.Position.y.AsFloat():F2}, {trans.Position.z.AsFloat():F2})", EditorStyles.miniLabel);
            
            var rotation = trans.Rotation;
            var euler = rotation.eulerAngles;
            EditorGUILayout.LabelField($"旋转: ({euler.x.AsFloat():F2}°, {euler.y.AsFloat():F2}°, {euler.z.AsFloat():F2}°)", EditorStyles.miniLabel);
        }
    }
}

