using System.Runtime.CompilerServices;
using TrueSync;
using FixMath.NET;
using BEPUVector3 = BEPUutilities.Vector3;
using BEPUQuaternion = BEPUutilities.Quaternion;
using BEPUMatrix = BEPUutilities.Matrix;

namespace Astrum.LogicCore.Physics
{
    /// <summary>
    /// TrueSync ↔ BEPU 类型转换工具
    /// 两者都使用 Q31.32 定点数格式，转换只是内存拷贝
    /// </summary>
    public static class PhysicsTypeConverter
    {
        #region 标量转换 (FP ↔ Fix64)

        /// <summary>
        /// TrueSync FP 转 BEPU Fix64
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fix64 ToFix64(this FP fp)
        {
            return new Fix64 { RawValue = fp._serializedValue };
        }

        /// <summary>
        /// BEPU Fix64 转 TrueSync FP
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FP ToFP(this Fix64 fix)
        {
            return new FP { _serializedValue = fix.RawValue };
        }

        #endregion

        #region 向量转换 (TSVector ↔ BEPUVector3)

        /// <summary>
        /// TrueSync TSVector 转 BEPU Vector3
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BEPUVector3 ToBepuVector(this TSVector ts)
        {
            return new BEPUVector3(
                ts.x.ToFix64(),
                ts.y.ToFix64(),
                ts.z.ToFix64()
            );
        }

        /// <summary>
        /// BEPU Vector3 转 TrueSync TSVector
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSVector ToTSVector(this BEPUVector3 bepu)
        {
            return new TSVector(
                bepu.X.ToFP(),
                bepu.Y.ToFP(),
                bepu.Z.ToFP()
            );
        }

        #endregion

        #region 四元数转换 (TSQuaternion ↔ BEPUQuaternion)

        /// <summary>
        /// TrueSync TSQuaternion 转 BEPU Quaternion
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BEPUQuaternion ToBepuQuaternion(this TSQuaternion ts)
        {
            return new BEPUQuaternion(
                ts.x.ToFix64(),
                ts.y.ToFix64(),
                ts.z.ToFix64(),
                ts.w.ToFix64()
            );
        }

        /// <summary>
        /// BEPU Quaternion 转 TrueSync TSQuaternion
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSQuaternion ToTSQuaternion(this BEPUQuaternion bepu)
        {
            return new TSQuaternion(
                bepu.X.ToFP(),
                bepu.Y.ToFP(),
                bepu.Z.ToFP(),
                bepu.W.ToFP()
            );
        }

        #endregion

        #region 矩阵转换 (TSMatrix ↔ BEPUMatrix)
        
        // 注意：TSMatrix 是 3x3 旋转矩阵，BEPUMatrix 是 4x4 仿射变换矩阵
        // 物理碰撞检测不需要矩阵变换，使用 四元数 + 位置向量 即可

        /// <summary>
        /// TrueSync TSMatrix 转 BEPU Matrix3x3（仅旋转部分）
        /// </summary>
        public static BEPUMatrix ToBepuMatrix3x3(this TSMatrix ts)
        {
            return new BEPUMatrix
            {
                M11 = ts.M11.ToFix64(), M12 = ts.M12.ToFix64(), M13 = ts.M13.ToFix64(),
                M21 = ts.M21.ToFix64(), M22 = ts.M22.ToFix64(), M23 = ts.M23.ToFix64(),
                M31 = ts.M31.ToFix64(), M32 = ts.M32.ToFix64(), M33 = ts.M33.ToFix64()
            };
        }

        /// <summary>
        /// BEPU Matrix 转 TrueSync TSMatrix（仅旋转部分）
        /// </summary>
        public static TSMatrix ToTSMatrix3x3(this BEPUMatrix bepu)
        {
            return new TSMatrix
            {
                M11 = bepu.M11.ToFP(), M12 = bepu.M12.ToFP(), M13 = bepu.M13.ToFP(),
                M21 = bepu.M21.ToFP(), M22 = bepu.M22.ToFP(), M23 = bepu.M23.ToFP(),
                M31 = bepu.M31.ToFP(), M32 = bepu.M32.ToFP(), M33 = bepu.M33.ToFP()
            };
        }

        #endregion

        #region 批量转换

        /// <summary>
        /// 批量转换 TSVector 数组为 BEPU Vector3 数组
        /// </summary>
        public static BEPUVector3[] ToBepuVectorArray(this TSVector[] tsArray)
        {
            var bepuArray = new BEPUVector3[tsArray.Length];
            for (int i = 0; i < tsArray.Length; i++)
            {
                bepuArray[i] = tsArray[i].ToBepuVector();
            }
            return bepuArray;
        }

        /// <summary>
        /// 批量转换 BEPU Vector3 数组为 TSVector 数组
        /// </summary>
        public static TSVector[] ToTSVectorArray(this BEPUVector3[] bepuArray)
        {
            var tsArray = new TSVector[bepuArray.Length];
            for (int i = 0; i < bepuArray.Length; i++)
            {
                tsArray[i] = bepuArray[i].ToTSVector();
            }
            return tsArray;
        }

        #endregion
    }
}

