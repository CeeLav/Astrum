using System.Collections.Generic;
using UnityEngine;
using Astrum.LogicCore.SkillSystem;
using TrueSync;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 动画根节点位移提取服务
    /// 从 AnimationClip 提取 Root Motion 数据，直接序列化为整型数组（运行时格式）
    /// </summary>
    public static class AnimationRootMotionExtractor
    {
        private const int LOGIC_FRAME_RATE = 20;     // 逻辑帧率：20 FPS
        private const float FRAME_TIME = 0.05f;      // 每帧时间：50ms
        
        /// <summary>
        /// 从动画片段提取根节点位移数据并直接序列化为整型数组
        /// 编辑器端直接使用运行时格式（整型数组），提取时直接转换并序列化
        /// </summary>
        /// <param name="clip">动画片段</param>
        /// <returns>整型数组（Luban格式），如果动画没有Root Motion则返回空列表</returns>
        public static List<int> ExtractRootMotionToIntArray(AnimationClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AnimationRootMotionExtractor] AnimationClip is null");
                return new List<int>();
            }
            
            // 计算总帧数
            int totalFrames = Mathf.RoundToInt(clip.length * LOGIC_FRAME_RATE);
            if (totalFrames <= 0)
            {
                Debug.Log($"[AnimationRootMotionExtractor] Animation {clip.name} has invalid length");
                return new List<int>();
            }
            
            // 创建临时GameObject用于采样动画
            GameObject tempGO = new GameObject("TempRootMotionSampler");
            tempGO.hideFlags = HideFlags.HideAndDontSave;
            
            // 添加Animation组件
            Animation anim = tempGO.AddComponent<Animation>();
            anim.AddClip(clip, "RootMotionSample");
            anim.clip = clip;
            
            // 获取动画状态，确保它存在
            AnimationState animState = anim["RootMotionSample"];
            if (animState == null)
            {
                Debug.LogWarning($"[AnimationRootMotionExtractor] Failed to get animation state for clip {clip.name}");
                Object.DestroyImmediate(tempGO);
                return new List<int>();
            }
            
            // 格式: [frameCount, dx0, dy0, dz0, rx0, ry0, rz0, rw0, dx1, dy1, dz1, rx1, ry1, rz1, rw1, ...]
            var result = new List<int> { totalFrames };
            
            const int SCALE = 1000; // 缩放因子
            
            bool hasMotion = false; // 用于标记是否检测到任何位移
            
            // 保存初始位置和旋转（用于计算相对位移）
            animState.time = 0f;
            animState.enabled = true;
            anim.Sample();
            Vector3 startPosition = tempGO.transform.localPosition;
            Quaternion startRotation = tempGO.transform.localRotation;
            
            // 逐帧采样
            for (int frame = 0; frame < totalFrames; frame++)
            {
                float time = Mathf.Clamp(frame * FRAME_TIME, 0f, clip.length);
                
                // 采样动画到指定时间
                animState.time = time;
                animState.enabled = true;
                anim.Sample();
                
                Vector3 currentPosition = tempGO.transform.localPosition;
                Quaternion currentRotation = tempGO.transform.localRotation;
                
                // 计算增量位移（相对于上一帧）
                Vector3 deltaPosition = Vector3.zero;
                Quaternion deltaRotation = Quaternion.identity;
                
                if (frame > 0)
                {
                    float prevTime = Mathf.Clamp((frame - 1) * FRAME_TIME, 0f, clip.length);
                    animState.time = prevTime;
                    animState.enabled = true;
                    anim.Sample();
                    
                    Vector3 prevPosition = tempGO.transform.localPosition;
                    Quaternion prevRotation = tempGO.transform.localRotation;
                    
                    deltaPosition = currentPosition - prevPosition;
                    deltaRotation = currentRotation * Quaternion.Inverse(prevRotation);
                }
                
                // 检查是否有位移或旋转（容忍一定的浮点误差）
                if (!hasMotion && (deltaPosition.sqrMagnitude > 0.0001f || 
                    Mathf.Abs(deltaRotation.x) > 0.0001f || Mathf.Abs(deltaRotation.y) > 0.0001f || 
                    Mathf.Abs(deltaRotation.z) > 0.0001f || Mathf.Abs(deltaRotation.w - 1.0f) > 0.0001f))
                {
                    hasMotion = true;
                }
                
                // 直接转换为整型（浮点数 * 1000）并添加到数组
                result.Add((int)(deltaPosition.x * SCALE));
                result.Add((int)(deltaPosition.y * SCALE));
                result.Add((int)(deltaPosition.z * SCALE));
                result.Add((int)(deltaRotation.x * SCALE));
                result.Add((int)(deltaRotation.y * SCALE));
                result.Add((int)(deltaRotation.z * SCALE));
                result.Add((int)(deltaRotation.w * SCALE));
            }
            
            // 清理临时对象
            Object.DestroyImmediate(tempGO);
            
            // 如果没有检测到任何位移，返回空列表（但保留帧数信息用于判断）
            if (!hasMotion)
            {
                Debug.Log($"[AnimationRootMotionExtractor] Animation {clip.name} has no root motion");
                return new List<int>();
            }
            
            Debug.Log($"[AnimationRootMotionExtractor] Extracted root motion for {clip.name}: {totalFrames} frames, {result.Count} integers");
            
            return result;
        }
        
        /// <summary>
        /// 从整型数组转换为运行时数据（整型转定点数）
        /// 这是运行时推荐的方式，Luban 已经将 CSV 数据解析为 List<int>，直接使用
        /// </summary>
        /// <param name="intArray">整型数组（Luban 解析后的格式）</param>
        /// <returns>运行时根节点位移数据，如果数组为空或格式错误则返回null</returns>
        public static AnimationRootMotionData ConvertToRuntimeFromIntArray(List<int> intArray)
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
            
            FP SCALE = (FP)1000; // 缩放因子（定点数），FP 不能声明为 const
            
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
        
        /// <summary>
        /// 从数组格式字符串转换为运行时数据（整型转定点数）
        /// 兼容方法：如果数据已经是字符串格式，使用此方法
        /// 注意：Luban 使用数组格式时，推荐使用 ConvertToRuntimeFromIntArray()
        /// </summary>
        /// <param name="arrayString">数组格式字符串（整型）</param>
        /// <returns>运行时根节点位移数据，如果字符串为空或格式错误则返回null</returns>
        public static AnimationRootMotionData ConvertToRuntimeFromArrayString(string arrayString)
        {
            if (string.IsNullOrEmpty(arrayString) || string.IsNullOrWhiteSpace(arrayString))
            {
                return null;
            }
            
            var parts = arrayString.Split(',');
            if (parts.Length < 1)
            {
                return null;
            }
            
            // 解析帧数
            if (!int.TryParse(parts[0].Trim(), out int frameCount) || frameCount <= 0)
            {
                return null;
            }
            
            // 验证数据长度
            int expectedLength = 1 + frameCount * 7;
            if (parts.Length < expectedLength)
            {
                return null;
            }
            
            FP SCALE = (FP)1000; // 缩放因子（定点数），FP 不能声明为 const
            
            var runtimeData = new AnimationRootMotionData
            {
                TotalFrames = frameCount
            };
            
            // 直接从整型转换为定点数
            for (int frame = 0; frame < frameCount; frame++)
            {
                int baseIndex = 1 + frame * 7;
                
                if (!int.TryParse(parts[baseIndex].Trim(), out int dxInt) ||
                    !int.TryParse(parts[baseIndex + 1].Trim(), out int dyInt) ||
                    !int.TryParse(parts[baseIndex + 2].Trim(), out int dzInt) ||
                    !int.TryParse(parts[baseIndex + 3].Trim(), out int rxInt) ||
                    !int.TryParse(parts[baseIndex + 4].Trim(), out int ryInt) ||
                    !int.TryParse(parts[baseIndex + 5].Trim(), out int rzInt) ||
                    !int.TryParse(parts[baseIndex + 6].Trim(), out int rwInt))
                {
                    continue;
                }
                
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
