using System.Collections.Generic;
using TrueSync;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 根节点位移数据转换工具（运行时）
    /// 将 Luban 解析的整型数组转换为运行时定点数数据结构
    /// </summary>
    public static class RootMotionDataConverter
    {
        /// <summary>
        /// 从整型数组直接转换为运行时数据（整型转定点数）
        /// 这是运行时推荐的方式，Luban 已经将 CSV 数据解析为 List<int>，直接使用
        /// </summary>
        /// <param name="intArray">整型数组（Luban 解析后的格式）</param>
        /// <returns>运行时根节点位移数据，如果数组为空或格式错误则返回null</returns>
        public static AnimationRootMotionData ConvertFromIntArray(List<int> intArray)
        {
            if (intArray == null || intArray.Count == 0)
            {
                return null;
            }
            
            // 解析帧数
            if (intArray.Count < 1)
            {
                return null;
            }
            
            int frameCount = intArray[0];
            if (frameCount <= 0)
            {
                return null;
            }
            
            // 验证数据长度：1 (frameCount) + frameCount * 7 (dx,dy,dz,rx,ry,rz,rw) = 1 + 7*frameCount
            int expectedLength = 1 + frameCount * 7;
            if (intArray.Count < expectedLength)
            {
                return null;
            }
            
            FP SCALE = (FP)1000; // 缩放因子（定点数，不能声明为 const）
            
            var runtimeData = new AnimationRootMotionData
            {
                TotalFrames = frameCount
            };
            
            // 直接从整型转换为定点数
            for (int frame = 0; frame < frameCount; frame++)
            {
                int baseIndex = 1 + frame * 7; // 跳过 frameCount，每帧7个值
                
                int dxInt = intArray[baseIndex];
                int dyInt = intArray[baseIndex + 1];
                int dzInt = intArray[baseIndex + 2];
                int rxInt = intArray[baseIndex + 3];
                int ryInt = intArray[baseIndex + 4];
                int rzInt = intArray[baseIndex + 5];
                int rwInt = intArray[baseIndex + 6];
                
                // 整型直接转定点数（除以 1000）
                runtimeData.Frames.Add(new RootMotionFrameData
                {
                    FrameIndex = frame,
                    DeltaPosition = new TSVector(
                        (FP)dxInt / SCALE,
                        (FP)dyInt / SCALE,
                        (FP)dzInt / SCALE
                    ),
                    DeltaRotation = new TSQuaternion(
                        (FP)rxInt / SCALE,
                        (FP)ryInt / SCALE,
                        (FP)rzInt / SCALE,
                        (FP)rwInt / SCALE
                    ),
                    RelativePosition = TSVector.zero,
                    RelativeRotation = TSQuaternion.identity
                });
            }
            
            return runtimeData;
        }
    }
}

