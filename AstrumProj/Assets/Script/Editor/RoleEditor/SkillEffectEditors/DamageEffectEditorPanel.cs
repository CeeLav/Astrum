using Astrum.Editor.RoleEditor.Persistence.Mappings;
using UnityEditor;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.SkillEffectEditors
{
    internal class DamageEffectEditorPanel : ISkillEffectEditorPanel
    {
        private static readonly string[] TargetOptions = { "自身", "敌人", "友军", "区域" };
        private static readonly int[] TargetValues = { 0, 1, 2, 3 };

        private static readonly string[] DamageTypeOptions = { "物理", "魔法", "真实" };
        private static readonly int[] DamageTypeValues = { 1, 2, 3 };

        private static readonly string[] ScalingStatOptions = { "无", "攻击", "防御", "生命上限", "法强" };
        private static readonly int[] ScalingStatValues = { 0, 1, 2, 3, 4 };

        public string EffectType => "Damage";
        public bool SupportsInlineEditing => true;

        public bool DrawContent(SkillEffectTableData data)
        {
            bool changed = false;

            SkillEffectEditorUtility.EnsureListSize(data.IntParams, 5);
            SkillEffectEditorUtility.EnsureListSize(data.StringParams, SkillEffectTableData.SoundEffectPathIndex + 1);

            EditorGUILayout.LabelField("伤害效果参数", EditorStyles.boldLabel);

            changed |= SkillEffectEditorUtility.DrawPopup("目标", data.IntParams, 0, TargetOptions, TargetValues);
            changed |= SkillEffectEditorUtility.DrawPopup("伤害类型", data.IntParams, 1, DamageTypeOptions, DamageTypeValues);

            EditorGUI.BeginChangeCheck();
            float basePercent = data.IntParams[2] / 10f;
            basePercent = EditorGUILayout.FloatField("基础倍率 (%)", basePercent);
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

            changed |= DrawVisualEffectField(data);
            changed |= DrawSoundEffectField(data);

            return changed;
        }

        private bool DrawVisualEffectField(SkillEffectTableData data)
        {
            string currentPath = data.GetVisualEffectPath();
            GameObject currentPrefab = string.IsNullOrEmpty(currentPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(currentPath);

            EditorGUI.BeginChangeCheck();
            GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField("视觉特效 Prefab", currentPrefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                string newPath = newPrefab != null ? AssetDatabase.GetAssetPath(newPrefab) : string.Empty;
                if (!string.Equals(newPath, currentPath))
                {
                    data.SetVisualEffectPath(newPath);
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(currentPath))
            {
                EditorGUILayout.LabelField("当前路径", currentPath, EditorStyles.miniLabel);
            }

            return false;
        }

        private bool DrawSoundEffectField(SkillEffectTableData data)
        {
            string currentPath = data.GetSoundEffectPath();
            AudioClip currentClip = string.IsNullOrEmpty(currentPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<AudioClip>(currentPath);

            EditorGUI.BeginChangeCheck();
            AudioClip newClip = (AudioClip)EditorGUILayout.ObjectField("音效 AudioClip", currentClip, typeof(AudioClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                string newPath = newClip != null ? AssetDatabase.GetAssetPath(newClip) : string.Empty;
                if (!string.Equals(newPath, currentPath))
                {
                    data.SetSoundEffectPath(newPath);
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(currentPath))
            {
                EditorGUILayout.LabelField("当前路径", currentPath, EditorStyles.miniLabel);
            }

            return false;
        }
    }
}

