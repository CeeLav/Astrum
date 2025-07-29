using System;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 简单的Vector3结构（用于逻辑层计算）
    /// </summary>
    public struct Vector3
    {
        public float X, Y, Z;
        
        public Vector3(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }

        public static Vector3 Zero => new Vector3(0, 0, 0);
        public static Vector3 One => new Vector3(1, 1, 1);
        public static Vector3 Forward => new Vector3(0, 0, 1);
        public static Vector3 Right => new Vector3(1, 0, 0);
        public static Vector3 Up => new Vector3(0, 1, 0);
        
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator *(Vector3 a, float s) => new Vector3(a.X * s, a.Y * s, a.Z * s);
        public static Vector3 operator /(Vector3 a, float s) => new Vector3(a.X / s, a.Y / s, a.Z / s);
        
        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public Vector3 Normalized => Magnitude > 0 ? this * (1f / Magnitude) : Zero;
        
        public float Dot(Vector3 other) => X * other.X + Y * other.Y + Z * other.Z;
        
        public Vector3 Cross(Vector3 other) => new Vector3(
            Y * other.Z - Z * other.Y,
            Z * other.X - X * other.Z,
            X * other.Y - Y * other.X
        );
        
        public override string ToString() => $"Vector3({X:F2}, {Y:F2}, {Z:F2})";
    }

    /// <summary>
    /// 简单的Quaternion结构（用于逻辑层旋转计算）
    /// </summary>
    public struct Quaternion
    {
        public float X, Y, Z, W;
        
        public Quaternion(float x, float y, float z, float w)
        {
            X = x; Y = y; Z = z; W = w;
        }

        public static Quaternion Identity => new Quaternion(0, 0, 0, 1);
        
        public static Quaternion FromEuler(float x, float y, float z)
        {
            float cx = (float)Math.Cos(x * 0.5f);
            float sx = (float)Math.Sin(x * 0.5f);
            float cy = (float)Math.Cos(y * 0.5f);
            float sy = (float)Math.Sin(y * 0.5f);
            float cz = (float)Math.Cos(z * 0.5f);
            float sz = (float)Math.Sin(z * 0.5f);

            return new Quaternion(
                sx * cy * cz - cx * sy * sz,
                cx * sy * cz + sx * cy * sz,
                cx * cy * sz - sx * sy * cz,
                cx * cy * cz + sx * sy * sz
            );
        }
        
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y + a.Y * b.W + a.Z * b.X - a.X * b.Z,
            a.W * b.Z + a.Z * b.W + a.X * b.Y - a.Y * b.X,
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z
        );
        
        public Vector3 ToEuler()
        {
            // 简化的四元数转欧拉角
            float x = (float)Math.Atan2(2 * (W * X + Y * Z), 1 - 2 * (X * X + Y * Y));
            float y = (float)Math.Asin(2 * (W * Y - Z * X));
            float z = (float)Math.Atan2(2 * (W * Z + X * Y), 1 - 2 * (Y * Y + Z * Z));
            return new Vector3(x, y, z);
        }
        
        public override string ToString() => $"Quaternion({X:F2}, {Y:F2}, {Z:F2}, {W:F2})";
    }
}
