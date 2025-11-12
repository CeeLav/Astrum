using Astrum.Editor.RoleEditor.Persistence.Mappings;
using UnityEditor;

namespace Astrum.Editor.RoleEditor.SkillEffectEditors
{
    internal class StatusEffectEditorPanel : ISkillEffectEditorPanel
    {
        private static readonly string[] TargetOptions = { "自身", "敌人", "友军", "区域" };
        private static readonly int[] TargetValues = { 0, 1, 2, 3 };

        private static readonly string[] ApplicationModeOptions = { "刷新时长", "叠加层数" };
        private static readonly int[] ApplicationModeValues = { 0, 1 };

        public string EffectType => "Status";
        public bool SupportsInlineEditing => true;

        public bool DrawContent(SkillEffectTableData data, object additionalContext)
        {
            bool changed = false;

            SkillEffectEditorUtility.EnsureListSize(data.IntParams, 7);

            EditorGUILayout.LabelField("状态效果参数", EditorStyles.boldLabel);

            changed |= SkillEffectEditorUtility.DrawPopup("目标", data.IntParams, 0, TargetOptions, TargetValues);
            changed |= SkillEffectEditorUtility.DrawIntField("状态类型ID", data.IntParams, 1);

            EditorGUI.BeginChangeCheck();
            float duration = data.IntParams[2] / 1000f;
            duration = EditorGUILayout.FloatField("持续时间 (秒)", duration);
            if (EditorGUI.EndChangeCheck())
            {
                data.IntParams[2] = (int)System.Math.Round(duration * 1000f);
                changed = true;
            }

            changed |= SkillEffectEditorUtility.DrawIntField("最大层数", data.IntParams, 3);
            changed |= SkillEffectEditorUtility.DrawPopup("叠加模式", data.IntParams, 4, ApplicationModeOptions, ApplicationModeValues);
            changed |= SkillEffectEditorUtility.DrawIntField("视觉效果ID", data.IntParams, 5);
            changed |= SkillEffectEditorUtility.DrawIntField("音效ID", data.IntParams, 6);

            return changed;
        }
    }
}

