using Astrum.Editor.RoleEditor.Persistence.Mappings;
using UnityEditor;

namespace Astrum.Editor.RoleEditor.SkillEffectEditors
{
    internal class HealEffectEditorPanel : ISkillEffectEditorPanel
    {
        private static readonly string[] TargetOptions = { "自身", "敌人", "友军", "区域" };
        private static readonly int[] TargetValues = { 0, 1, 2, 3 };

        private static readonly string[] HealModeOptions = { "瞬发", "持续" };
        private static readonly int[] HealModeValues = { 0, 1 };

        private static readonly string[] ScalingStatOptions = { "无", "攻击", "防御", "生命上限", "法强" };
        private static readonly int[] ScalingStatValues = { 0, 1, 2, 3, 4 };

        public string EffectType => "Heal";
        public bool SupportsInlineEditing => true;

        public bool DrawContent(SkillEffectTableData data, object additionalContext)
        {
            bool changed = false;

            SkillEffectEditorUtility.EnsureListSize(data.IntParams, 7);

            EditorGUILayout.LabelField("治疗效果参数", EditorStyles.boldLabel);

            changed |= SkillEffectEditorUtility.DrawPopup("目标", data.IntParams, 0, TargetOptions, TargetValues);
            changed |= SkillEffectEditorUtility.DrawPopup("治疗方式", data.IntParams, 1, HealModeOptions, HealModeValues);

            EditorGUI.BeginChangeCheck();
            float basePercent = data.IntParams[2] / 10f;
            basePercent = EditorGUILayout.FloatField("基础治疗 (%)", basePercent);
            if (EditorGUI.EndChangeCheck())
            {
                data.IntParams[2] = (int)System.Math.Round(basePercent * 10f);
                changed = true;
            }

            changed |= SkillEffectEditorUtility.DrawPopup("缩放属性", data.IntParams, 3, ScalingStatOptions, ScalingStatValues);

            EditorGUI.BeginChangeCheck();
            float scalingPercent = data.IntParams[4] / 10f;
            scalingPercent = EditorGUILayout.FloatField("缩放倍率 (%)", scalingPercent);
            if (EditorGUI.EndChangeCheck())
            {
                data.IntParams[4] = (int)System.Math.Round(scalingPercent * 10f);
                changed = true;
            }

            changed |= SkillEffectEditorUtility.DrawIntField("视觉效果ID", data.IntParams, 5);
            changed |= SkillEffectEditorUtility.DrawIntField("音效ID", data.IntParams, 6);

            return changed;
        }
    }
}

