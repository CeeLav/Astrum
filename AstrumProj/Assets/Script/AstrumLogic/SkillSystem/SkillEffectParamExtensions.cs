using System;
using System.Collections.Generic;
using cfg.Skill;
using Astrum.LogicCore.Stats;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// SkillEffectTable 参数访问扩展
    /// </summary>
    public static class SkillEffectParamExtensions
    {
        private static readonly int[] EmptyIntArray = Array.Empty<int>();
        private static readonly string[] EmptyStringArray = Array.Empty<string>();
        private const int VisualEffectPathIndex = 0;
        private const int SoundEffectPathIndex = 1;

        public static IReadOnlyList<int> GetIntParams(this SkillEffectTable effect)
        {
            return effect?.IntParams ?? (IReadOnlyList<int>)EmptyIntArray;
        }

        public static int GetIntParam(this SkillEffectTable effect, int index, int defaultValue = 0)
        {
            var list = effect?.IntParams;
            if (list == null || index < 0 || index >= list.Length)
            {
                return defaultValue;
            }

            return list[index];
        }

        public static IReadOnlyList<string> GetStringParams(this SkillEffectTable effect)
        {
            return effect?.StringParams ?? (IReadOnlyList<string>)EmptyStringArray;
        }

        public static string GetStringParam(this SkillEffectTable effect, int index, string defaultValue = "")
        {
            var list = effect?.StringParams;
            if (list == null || index < 0 || index >= list.Length)
            {
                return defaultValue;
            }

            return list[index];
        }

        public static Dictionary<string, string> GetStringParamMap(this SkillEffectTable effect)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var list = effect?.StringParams;
            if (list == null)
            {
                return map;
            }

            foreach (var entry in list)
            {
                if (string.IsNullOrWhiteSpace(entry))
                    continue;

                var parts = entry.Split(':');
                if (parts.Length == 2)
                {
                    map[parts[0].Trim()] = parts[1].Trim();
                }
            }

            return map;
        }

        public static string GetVisualEffectPath(this SkillEffectTable effect)
        {
            var list = effect?.StringParams;
            if (list == null || list.Length <= VisualEffectPathIndex)
            {
                return string.Empty;
            }

            return list[VisualEffectPathIndex] ?? string.Empty;
        }

        public static string GetSoundEffectPath(this SkillEffectTable effect)
        {
            var list = effect?.StringParams;
            if (list == null || list.Length <= SoundEffectPathIndex)
            {
                return string.Empty;
            }

            return list[SoundEffectPathIndex] ?? string.Empty;
        }

        public static int GetDamageTypeCode(this SkillEffectTable effect)
        {
            return effect.GetIntParam(1, 1);
        }

        public static DamageType GetDamageTypeEnum(this SkillEffectTable effect)
        {
            return effect.GetDamageTypeCode() switch
            {
                2 => DamageType.Magical,
                3 => DamageType.True,
                _ => DamageType.Physical
            };
        }

        public static float GetBaseCoefficientPercent(this SkillEffectTable effect)
        {
            int value = effect.GetIntParam(2, 0);
            return value / 10f;
        }
    }
}

