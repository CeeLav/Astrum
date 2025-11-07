using Astrum.Editor.RoleEditor.Persistence.Mappings;
using UnityEditor;

namespace Astrum.Editor.RoleEditor.SkillEffectEditors
{
    internal class TeleportEffectEditorPanel : ISkillEffectEditorPanel
    {
        private static readonly string[] TargetOptions = { "自身", "敌人", "友军", "区域" };
        private static readonly int[] TargetValues = { 0, 1, 2, 3 };

        private static readonly string[] DirectionOptions = { "向前", "向后", "远离施法者", "靠近施法者" };
        private static readonly int[] DirectionValues = { 0, 1, 2, 3 };

        public string EffectType => "Teleport";

        public bool DrawContent(SkillEffectTableData data)
        {
            bool changed = false;

            SkillEffectEditorUtility.EnsureListSize(data.IntParams, 5);

            EditorGUILayout.LabelField("瞬移/位移效果参数", EditorStyles.boldLabel);

            changed |= SkillEffectEditorUtility.DrawPopup("目标", data.IntParams, 0, TargetOptions, TargetValues);

            EditorGUI.BeginChangeCheck();
            float offset = data.IntParams[1] / 1000f;
            offset = EditorGUILayout.FloatField("位移距离 (米)", offset);
            if (EditorGUI.EndChangeCheck())
            {
                data.IntParams[1] = (int)System.Math.Round(offset * 1000f);
                changed = true;
            }

            changed |= SkillEffectEditorUtility.DrawPopup("方向模式", data.IntParams, 2, DirectionOptions, DirectionValues);
            changed |= SkillEffectEditorUtility.DrawIntField("视觉效果ID", data.IntParams, 3);
            changed |= SkillEffectEditorUtility.DrawIntField("音效ID", data.IntParams, 4);

            return changed;
        }
    }
}

