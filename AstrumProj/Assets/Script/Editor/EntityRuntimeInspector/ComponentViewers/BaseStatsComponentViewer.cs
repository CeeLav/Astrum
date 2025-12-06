using UnityEditor;
using Astrum.LogicCore.Components;

namespace Astrum.Editor.EntityRuntimeInspector.ComponentViewers
{
    /// <summary>
    /// BaseStatsComponent 显示器
    /// </summary>
    public class BaseStatsComponentViewer : IComponentViewer
    {
        public void DrawComponent(BaseComponent component)
        {
            if (component is not BaseStatsComponent stats) return;

            if (stats.BaseStats != null && stats.BaseStats.Values != null)
            {
                EditorGUILayout.LabelField($"属性数量: {stats.BaseStats.Values.Count}", EditorStyles.miniLabel);
                
                // 显示主要属性
                var hp = stats.BaseStats.Get(Astrum.LogicCore.Stats.StatType.HP);
                var atk = stats.BaseStats.Get(Astrum.LogicCore.Stats.StatType.ATK);
                var def = stats.BaseStats.Get(Astrum.LogicCore.Stats.StatType.DEF);
                var spd = stats.BaseStats.Get(Astrum.LogicCore.Stats.StatType.SPD);
                
                if (hp > 0) EditorGUILayout.LabelField($"HP: {hp.AsFloat():F2}", EditorStyles.miniLabel);
                if (atk > 0) EditorGUILayout.LabelField($"ATK: {atk.AsFloat():F2}", EditorStyles.miniLabel);
                if (def > 0) EditorGUILayout.LabelField($"DEF: {def.AsFloat():F2}", EditorStyles.miniLabel);
                if (spd > 0) EditorGUILayout.LabelField($"SPD: {spd.AsFloat():F2}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("BaseStats 为空", EditorStyles.miniLabel);
            }
        }
    }
}

