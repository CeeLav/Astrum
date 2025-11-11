using System;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.SkillEffectEditors
{
    /// <summary>
    /// 子弹特效偏移数据
    /// </summary>
    [Serializable]
    internal class ProjectileEffectOffset
    {
        public Vector3 Position = Vector3.zero;
        public Vector3 Rotation = Vector3.zero;
        public Vector3 Scale = Vector3.one;

        public static ProjectileEffectOffset Default()
        {
            return new ProjectileEffectOffset
            {
                Position = Vector3.zero,
                Rotation = Vector3.zero,
                Scale = Vector3.one
            };
        }

        public bool IsDefault()
        {
            return Approximately(Position, Vector3.zero) &&
                   Approximately(Rotation, Vector3.zero) &&
                   Approximately(Scale, Vector3.one);
        }

        private static bool Approximately(Vector3 a, Vector3 b)
        {
            return Mathf.Approximately(a.x, b.x) &&
                   Mathf.Approximately(a.y, b.y) &&
                   Mathf.Approximately(a.z, b.z);
        }
    }

    internal static class ProjectileEffectOffsetUtility
    {
        public static ProjectileEffectOffset Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return ProjectileEffectOffset.Default();
            }

            try
            {
                var offset = JsonUtility.FromJson<ProjectileEffectOffset>(json);
                if (offset == null)
                {
                    return ProjectileEffectOffset.Default();
                }

                offset.Scale = EnsureValidScale(offset.Scale);
                return offset;
            }
            catch
            {
                return ProjectileEffectOffset.Default();
            }
        }

        public static string ToJson(ProjectileEffectOffset offset)
        {
            if (offset == null || offset.IsDefault())
            {
                return string.Empty;
            }

            offset.Scale = EnsureValidScale(offset.Scale);
            return JsonUtility.ToJson(offset);
        }

        public static Vector3 EnsureValidScale(Vector3 scale)
        {
            if (Mathf.Approximately(scale.x, 0f) &&
                Mathf.Approximately(scale.y, 0f) &&
                Mathf.Approximately(scale.z, 0f))
            {
                return Vector3.one;
            }

            return scale;
        }
    }
}
