using UnityEditor;
using Astrum.LogicCore.Components;

namespace Astrum.Editor.EntityRuntimeInspector.ComponentViewers
{
    /// <summary>
    /// LevelComponent 显示器
    /// </summary>
    public class LevelComponentViewer : IComponentViewer
    {
        public void DrawComponent(BaseComponent component)
        {
            if (component is not LevelComponent level) return;

            EditorGUILayout.LabelField($"当前等级: {level.CurrentLevel}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"当前经验: {level.CurrentExp}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"升级所需经验: {level.ExpToNextLevel}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"最大等级: {level.MaxLevel}", EditorStyles.miniLabel);
            
            if (level.ExpToNextLevel > 0)
            {
                var progress = (float)level.CurrentExp / level.ExpToNextLevel;
                EditorGUILayout.LabelField($"经验进度: {progress:P2}", EditorStyles.miniLabel);
            }
        }
    }
}

