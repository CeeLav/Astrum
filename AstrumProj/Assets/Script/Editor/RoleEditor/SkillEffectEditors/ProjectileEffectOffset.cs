using System;
using System.Collections.Generic;
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

    internal static class ProjectileEffectOffsetConversion
    {
        public const float PositionUnit = 0.01f;
        public const float RotationUnit = 1f;
        public const float ScaleUnit = 0.01f;
        public const float SpeedUnit = 0.01f;

        public static Vector3 ToVector3(List<int> values, float unit, Vector3 defaultValue)
        {
            if (values == null || values.Count < 3)
            {
                return defaultValue;
            }

            return new Vector3(values[0] * unit, values[1] * unit, values[2] * unit);
        }

        public static Vector3Int ToVector3Int(List<int> values, Vector3Int defaultValue)
        {
            if (values == null || values.Count < 3)
            {
                return defaultValue;
            }

            return new Vector3Int(values[0], values[1], values[2]);
        }

        public static void FromVector3(Vector3 source, float unit, List<int> target)
        {
            if (target == null)
            {
                return;
            }

            target.Clear();
            target.Add(Mathf.RoundToInt(source.x / unit));
            target.Add(Mathf.RoundToInt(source.y / unit));
            target.Add(Mathf.RoundToInt(source.z / unit));
        }

        public static void FromVector3Int(Vector3Int source, List<int> target)
        {
            if (target == null)
            {
                return;
            }

            target.Clear();
            target.Add(source.x);
            target.Add(source.y);
            target.Add(source.z);
        }
    }
}

