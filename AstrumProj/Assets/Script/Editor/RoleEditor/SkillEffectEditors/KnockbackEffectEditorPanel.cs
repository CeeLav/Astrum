using Astrum.Editor.RoleEditor.Persistence.Mappings;
using UnityEditor;

namespace Astrum.Editor.RoleEditor.SkillEffectEditors
{
    internal class KnockbackEffectEditorPanel : ISkillEffectEditorPanel
    {
        private static readonly string[] TargetOptions = { "自身", "敌人", "友军", "区域" };
        private static readonly int[] TargetValues = { 0, 1, 2, 3 };

        private static readonly string[] DirectionOptions = { "向前", "向后", "远离施法者", "靠近施法者" };
        private static readonly int[] DirectionValues = { 0, 1, 2, 3 };

        private static readonly string[] CurveOptions = { "线性", "减速", "加速", "自定义" };
        private static readonly int[] CurveValues = { 0, 1, 2, 3 };

        public string EffectType => "Knockback";

        public bool DrawContent(SkillEffectTableData data)
        {
            bool changed = false;

            SkillEffectEditorUtility.EnsureListSize(data.IntParams, 7);

            EditorGUILayout.LabelField("击退效果参数", EditorStyles.boldLabel);

            changed |= SkillEffectEditorUtility.DrawPopup("目标", data.IntParams, 0, TargetOptions, TargetValues);

            EditorGUI.BeginChangeCheck();
            float distance = data.IntParams[1] / 1000f;
            distance = EditorGUILayout.FloatField("击退距离 (米)", distance);
            if (EditorGUI.EndChangeCheck())
            {
                data.IntParams[1] = (int)System.Math.Round(distance * 1000f);
                changed = true;
            }

            EditorGUI.BeginChangeCheck();
            float duration = data.IntParams[2] / 1000f;
            duration = EditorGUILayout.FloatField("持续时间 (秒)", duration);
            if (EditorGUI.EndChangeCheck())
            {
                data.IntParams[2] = (int)System.Math.Round(duration * 1000f);
                changed = true;
            }

            changed |= SkillEffectEditorUtility.DrawPopup("方向模式", data.IntParams, 3, DirectionOptions, DirectionValues);
            changed |= SkillEffectEditorUtility.DrawPopup("速度曲线", data.IntParams, 4, CurveOptions, CurveValues);
            changed |= SkillEffectEditorUtility.DrawIntField("视觉效果ID", data.IntParams, 5);
            changed |= SkillEffectEditorUtility.DrawIntField("音效ID", data.IntParams, 6);

            return changed;
        }
    }
}

