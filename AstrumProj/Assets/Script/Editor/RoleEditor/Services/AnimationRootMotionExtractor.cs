using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
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
        /// 支持两种格式：传统格式（m_LocalPosition/m_LocalRotation）和根运动格式（RootT/RootQ）
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
            
            // 使用AnimationUtility直接读取曲线数据，避免需要Legacy格式
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            
            // 检测使用的格式
            bool usesRootMotionFormat = UsesRootMotionFormat(bindings);
            
            // 自动检测根骨骼路径（空路径表示根对象）
            string rootBonePath = FindRootBonePathForSingleClip(bindings);
            
            if (rootBonePath == null)
            {
                // 如果找不到，尝试使用空路径（根对象）
                rootBonePath = "";
            }
            
            Debug.Log($"[AnimationRootMotionExtractor] Auto-detected root bone path: '{rootBonePath ?? "null"}', uses root motion format: {usesRootMotionFormat}");
            
            // 查找根骨骼的位置和旋转曲线
            string posPrefix = usesRootMotionFormat ? "RootT" : "m_LocalPosition";
            string rotPrefix = usesRootMotionFormat ? "RootQ" : "m_LocalRotation";
            
            EditorCurveBinding? posX = null, posY = null, posZ = null;
            EditorCurveBinding? rotX = null, rotY = null, rotZ = null, rotW = null;
            
            foreach (var binding in bindings)
            {
                if (binding.path == rootBonePath)
                {
                    if (binding.propertyName == $"{posPrefix}.x" || binding.propertyName == "m_LocalPosition.x" || binding.propertyName == "RootT.x") posX = binding;
                    else if (binding.propertyName == $"{posPrefix}.y" || binding.propertyName == "m_LocalPosition.y" || binding.propertyName == "RootT.y") posY = binding;
                    else if (binding.propertyName == $"{posPrefix}.z" || binding.propertyName == "m_LocalPosition.z" || binding.propertyName == "RootT.z") posZ = binding;
                    else if (binding.propertyName == $"{rotPrefix}.x" || binding.propertyName == "m_LocalRotation.x" || binding.propertyName == "RootQ.x") rotX = binding;
                    else if (binding.propertyName == $"{rotPrefix}.y" || binding.propertyName == "m_LocalRotation.y" || binding.propertyName == "RootQ.y") rotY = binding;
                    else if (binding.propertyName == $"{rotPrefix}.z" || binding.propertyName == "m_LocalRotation.z" || binding.propertyName == "RootQ.z") rotZ = binding;
                    else if (binding.propertyName == $"{rotPrefix}.w" || binding.propertyName == "m_LocalRotation.w" || binding.propertyName == "RootQ.w") rotW = binding;
                }
            }
            
            // 检查是否找到必要的位置曲线
            if (!posX.HasValue || !posY.HasValue || !posZ.HasValue)
            {
                Debug.LogWarning($"[AnimationRootMotionExtractor] Could not find root position curves in animation {clip.name}. Available bindings: {string.Join(", ", System.Array.ConvertAll(bindings, b => $"{b.path}/{b.propertyName}"))}");
                return new List<int>();
            }
            
            // 获取曲线
            AnimationCurve posXCurve = AnimationUtility.GetEditorCurve(clip, posX.Value);
            AnimationCurve posYCurve = AnimationUtility.GetEditorCurve(clip, posY.Value);
            AnimationCurve posZCurve = AnimationUtility.GetEditorCurve(clip, posZ.Value);
            
            // 调试：检查曲线的数值范围
            if (posXCurve != null && posXCurve.keys.Length > 0)
            {
                float minX = posXCurve.keys[0].value;
                float maxX = posXCurve.keys[0].value;
                float minY = posYCurve.keys[0].value;
                float maxY = posYCurve.keys[0].value;
                float minZ = posZCurve.keys[0].value;
                float maxZ = posZCurve.keys[0].value;
                
                foreach (var key in posXCurve.keys)
                {
                    minX = Mathf.Min(minX, key.value);
                    maxX = Mathf.Max(maxX, key.value);
                }
                foreach (var key in posYCurve.keys)
                {
                    minY = Mathf.Min(minY, key.value);
                    maxY = Mathf.Max(maxY, key.value);
                }
                foreach (var key in posZCurve.keys)
                {
                    minZ = Mathf.Min(minZ, key.value);
                    maxZ = Mathf.Max(maxZ, key.value);
                }
                
                Debug.Log($"[AnimationRootMotionExtractor] Curve value ranges - X: [{minX:F6}, {maxX:F6}], Y: [{minY:F6}, {maxY:F6}], Z: [{minZ:F6}, {maxZ:F6}]");
                Debug.Log($"[AnimationRootMotionExtractor] Total displacement range: X={maxX-minX:F6}, Y={maxY-minY:F6}, Z={maxZ-minZ:F6}");
            }
            
            AnimationCurve rotXCurve = rotX.HasValue ? AnimationUtility.GetEditorCurve(clip, rotX.Value) : null;
            AnimationCurve rotYCurve = rotY.HasValue ? AnimationUtility.GetEditorCurve(clip, rotY.Value) : null;
            AnimationCurve rotZCurve = rotZ.HasValue ? AnimationUtility.GetEditorCurve(clip, rotZ.Value) : null;
            AnimationCurve rotWCurve = rotW.HasValue ? AnimationUtility.GetEditorCurve(clip, rotW.Value) : null;
            
            // 格式: [frameCount, dx0, dy0, dz0, rx0, ry0, rz0, rw0, dx1, dy1, dz1, rx1, ry1, rz1, rw1, ...]
            var result = new List<int> { totalFrames };
            
            const int SCALE = 1000; // 缩放因子
            bool hasMotion = false;
            
            Vector3 prevPosition = Vector3.zero;
            Quaternion prevRotation = Quaternion.identity;
            
            // 记录第一帧的位置，用于调试
            Vector3 firstFramePos = Vector3.zero;
            
            // 逐帧采样
            for (int frame = 0; frame < totalFrames; frame++)
            {
                float time = Mathf.Clamp(frame * FRAME_TIME, 0f, clip.length);
                
                // 直接从曲线评估值
                Vector3 currentPosition = new Vector3(
                    posXCurve.Evaluate(time),
                    posYCurve.Evaluate(time),
                    posZCurve.Evaluate(time)
                );
                
                // 记录第一帧位置，用于调试
                if (frame == 0)
                {
                    firstFramePos = currentPosition;
                    Debug.Log($"[AnimationRootMotionExtractor] First frame position: ({currentPosition.x:F6}, {currentPosition.y:F6}, {currentPosition.z:F6})");
                }
                
                Quaternion currentRotation = Quaternion.identity;
                if (rotXCurve != null && rotYCurve != null && rotZCurve != null && rotWCurve != null)
                {
                    currentRotation = new Quaternion(
                        rotXCurve.Evaluate(time),
                        rotYCurve.Evaluate(time),
                        rotZCurve.Evaluate(time),
                        rotWCurve.Evaluate(time)
                    );
                }
                
                // 计算增量位移（相对于上一帧）
                Vector3 deltaPosition = Vector3.zero;
                Quaternion deltaRotation = Quaternion.identity;
                
                if (frame > 0)
                {
                    deltaPosition = currentPosition - prevPosition;
                    deltaRotation = currentRotation * Quaternion.Inverse(prevRotation);
                    
                    // 调试日志：前几帧的数据
                    if (frame <= 3 || deltaPosition.sqrMagnitude > 0.0001f)
                    {
                        Debug.Log($"[AnimationRootMotionExtractor] Frame {frame}: currentPos=({currentPosition.x:F6}, {currentPosition.y:F6}, {currentPosition.z:F6}), " +
                                  $"prevPos=({prevPosition.x:F6}, {prevPosition.y:F6}, {prevPosition.z:F6}), " +
                                  $"delta=({deltaPosition.x:F6}, {deltaPosition.y:F6}, {deltaPosition.z:F6}), " +
                                  $"deltaInt=({(int)(deltaPosition.x * SCALE)}, {(int)(deltaPosition.y * SCALE)}, {(int)(deltaPosition.z * SCALE)})");
                    }
                }
                else
                {
                    // 第一帧记录初始值，用于后续帧计算
                    prevPosition = currentPosition;
                    prevRotation = currentRotation;
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
                
                // 保存当前值作为下一帧的前一帧
                if (frame > 0)
                {
                    prevPosition = currentPosition;
                    prevRotation = currentRotation;
                }
            }
            
            // 如果没有检测到任何位移，返回空列表
            if (!hasMotion)
            {
                Debug.Log($"[AnimationRootMotionExtractor] Animation {clip.name} has no root motion detected");
                return new List<int>();
            }
            
            Debug.Log($"[AnimationRootMotionExtractor] Extracted root motion for {clip.name}: {totalFrames} frames, {result.Count} integers");
            
            return result;
        }
        
        /// <summary>
        /// 为单个动画片段自动检测根骨骼路径
        /// </summary>
        private static string FindRootBonePathForSingleClip(EditorCurveBinding[] bindings)
        {
            // 统计每个路径的位置曲线数量
            Dictionary<string, int> pathPosCount = new Dictionary<string, int>();
            
            foreach (var binding in bindings)
            {
                if (binding.propertyName == "m_LocalPosition.x" || 
                    binding.propertyName == "m_LocalPosition.y" || 
                    binding.propertyName == "m_LocalPosition.z" ||
                    binding.propertyName == "RootT.x" ||
                    binding.propertyName == "RootT.y" ||
                    binding.propertyName == "RootT.z")
                {
                    if (!pathPosCount.ContainsKey(binding.path))
                        pathPosCount[binding.path] = 0;
                    pathPosCount[binding.path]++;
                }
            }
            
            // 找出有完整位置数据（x,y,z = 3个曲线）的路径
            List<string> candidatePaths = new List<string>();
            foreach (var kvp in pathPosCount)
            {
                if (kvp.Value == 3)
                {
                    candidatePaths.Add(kvp.Key);
                }
            }
            
            if (candidatePaths.Count == 0)
            {
                Debug.LogWarning("[AnimationRootMotionExtractor] No bone found with complete position curves (x,y,z).");
                return null;
            }
            
            // 优先选择空路径（根对象），否则选择最短路径
            string bestPath = null;
            int minDepth = int.MaxValue;
            
            foreach (string path in candidatePaths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    bestPath = path;
                    break;
                }
                
                int depth = path.Split('/').Length;
                if (depth < minDepth)
                {
                    minDepth = depth;
                    bestPath = path;
                }
            }
            
            Debug.Log($"[AnimationRootMotionExtractor] Found {candidatePaths.Count} candidate root bone(s), selected: '{bestPath ?? "null"}' (depth: {minDepth})");
            
            return bestPath;
        }
        
        /// <summary>
        /// 使用参考动画提取Hips骨骼位移差值
        /// 通过对比带位移的参考动画和不带位移的基础动画，计算真正的位移
        /// </summary>
        /// <param name="baseClip">基础动画（不带位移）</param>
        /// <param name="referenceClip">参考动画（带位移）</param>
        /// <param name="hipsBoneName">Hips骨骼名称，默认为"Hips"</param>
        /// <param name="modelGameObject">角色模型GameObject（用于查找Hips骨骼）</param>
        /// <returns>整型数组（Luban格式），如果提取失败则返回空列表</returns>
        public static List<int> ExtractHipsMotionDifference(
            AnimationClip baseClip, 
            AnimationClip referenceClip, 
            string hipsBoneName = "Hips",
            GameObject modelGameObject = null)
        {
            if (baseClip == null || referenceClip == null)
            {
                Debug.LogWarning("[AnimationRootMotionExtractor] Base or reference clip is null");
                return new List<int>();
            }
            
            // 计算总帧数（使用两个动画中较短的那个）
            int totalFrames = Mathf.Min(
                Mathf.RoundToInt(baseClip.length * LOGIC_FRAME_RATE),
                Mathf.RoundToInt(referenceClip.length * LOGIC_FRAME_RATE)
            );
            
            if (totalFrames <= 0)
            {
                Debug.LogWarning($"[AnimationRootMotionExtractor] Invalid animation length");
                return new List<int>();
            }
            
            // 如果没有提供模型，尝试使用预览模块的模型
            if (modelGameObject == null)
            {
                Debug.LogWarning("[AnimationRootMotionExtractor] No model provided, cannot find Hips bone. Please provide a model GameObject.");
                return new List<int>();
            }
            
            // 创建两个独立的临时模型实例（避免Animation组件相互干扰）
            GameObject baseModel = Object.Instantiate(modelGameObject);
            baseModel.hideFlags = HideFlags.HideAndDontSave;
            
            GameObject refModel = Object.Instantiate(modelGameObject);
            refModel.hideFlags = HideFlags.HideAndDontSave;
            
            // 在两个模型上分别查找Hips骨骼
            Transform baseHipsBone = FindBoneRecursive(baseModel.transform, hipsBoneName);
            Transform refHipsBone = FindBoneRecursive(refModel.transform, hipsBoneName);
            
            if (baseHipsBone == null || refHipsBone == null)
            {
                Debug.LogWarning($"[AnimationRootMotionExtractor] Hips bone '{hipsBoneName}' not found in model {modelGameObject.name}");
                Object.DestroyImmediate(baseModel);
                Object.DestroyImmediate(refModel);
                return new List<int>();
            }
            
            string hipsPath = GetTransformPath(baseHipsBone);
            Debug.Log($"[AnimationRootMotionExtractor] Model Hips bone: {baseHipsBone.name} at path {hipsPath}");
            
            // 使用AnimationUtility直接读取动画曲线数据，自动检测根骨骼
            // 获取所有曲线绑定
            EditorCurveBinding[] baseBindings = AnimationUtility.GetCurveBindings(baseClip);
            EditorCurveBinding[] refBindings = AnimationUtility.GetCurveBindings(referenceClip);
            
            // 自动检测根骨骼路径（包含完整位置数据的骨骼）
            string detectedRootBonePath = FindRootBonePath(baseBindings, refBindings);
            
            if (string.IsNullOrEmpty(detectedRootBonePath))
            {
                Debug.LogError($"[AnimationRootMotionExtractor] Could not auto-detect root bone in animation clips.");
                Debug.LogError($"[AnimationRootMotionExtractor] Available bindings in base clip: {string.Join(", ", System.Array.ConvertAll(baseBindings, b => $"{b.path}/{b.propertyName}"))}");
                Debug.LogError($"[AnimationRootMotionExtractor] Available bindings in ref clip: {string.Join(", ", System.Array.ConvertAll(refBindings, b => $"{b.path}/{b.propertyName}"))}");
                Object.DestroyImmediate(baseModel);
                Object.DestroyImmediate(refModel);
                return new List<int>();
            }
            
            Debug.Log($"[AnimationRootMotionExtractor] Auto-detected root bone path: '{detectedRootBonePath}'");
            
            // 检测使用的属性名格式
            bool baseUsesRootMotion = UsesRootMotionFormat(baseBindings);
            bool refUsesRootMotion = UsesRootMotionFormat(refBindings);
            
            // 根据格式选择属性名前缀
            string posPrefix = baseUsesRootMotion ? "RootT" : "m_LocalPosition";
            string rotPrefix = baseUsesRootMotion ? "RootQ" : "m_LocalRotation";
            
            Debug.Log($"[AnimationRootMotionExtractor] Base clip uses root motion format: {baseUsesRootMotion}, Ref clip uses root motion format: {refUsesRootMotion}");
            
            // 查找根骨骼的位置和旋转曲线（支持两种格式）
            EditorCurveBinding? basePosX = null, basePosY = null, basePosZ = null;
            EditorCurveBinding? baseRotX = null, baseRotY = null, baseRotZ = null, baseRotW = null;
            EditorCurveBinding? refPosX = null, refPosY = null, refPosZ = null;
            EditorCurveBinding? refRotX = null, refRotY = null, refRotZ = null, refRotW = null;
            
            foreach (var binding in baseBindings)
            {
                if (binding.path == detectedRootBonePath)
                {
                    if (binding.propertyName == $"{posPrefix}.x" || binding.propertyName == "m_LocalPosition.x" || binding.propertyName == "RootT.x") basePosX = binding;
                    else if (binding.propertyName == $"{posPrefix}.y" || binding.propertyName == "m_LocalPosition.y" || binding.propertyName == "RootT.y") basePosY = binding;
                    else if (binding.propertyName == $"{posPrefix}.z" || binding.propertyName == "m_LocalPosition.z" || binding.propertyName == "RootT.z") basePosZ = binding;
                    else if (binding.propertyName == $"{rotPrefix}.x" || binding.propertyName == "m_LocalRotation.x" || binding.propertyName == "RootQ.x") baseRotX = binding;
                    else if (binding.propertyName == $"{rotPrefix}.y" || binding.propertyName == "m_LocalRotation.y" || binding.propertyName == "RootQ.y") baseRotY = binding;
                    else if (binding.propertyName == $"{rotPrefix}.z" || binding.propertyName == "m_LocalRotation.z" || binding.propertyName == "RootQ.z") baseRotZ = binding;
                    else if (binding.propertyName == $"{rotPrefix}.w" || binding.propertyName == "m_LocalRotation.w" || binding.propertyName == "RootQ.w") baseRotW = binding;
                }
            }
            
            foreach (var binding in refBindings)
            {
                if (binding.path == detectedRootBonePath)
                {
                    if (binding.propertyName == $"{posPrefix}.x" || binding.propertyName == "m_LocalPosition.x" || binding.propertyName == "RootT.x") refPosX = binding;
                    else if (binding.propertyName == $"{posPrefix}.y" || binding.propertyName == "m_LocalPosition.y" || binding.propertyName == "RootT.y") refPosY = binding;
                    else if (binding.propertyName == $"{posPrefix}.z" || binding.propertyName == "m_LocalPosition.z" || binding.propertyName == "RootT.z") refPosZ = binding;
                    else if (binding.propertyName == $"{rotPrefix}.x" || binding.propertyName == "m_LocalRotation.x" || binding.propertyName == "RootQ.x") refRotX = binding;
                    else if (binding.propertyName == $"{rotPrefix}.y" || binding.propertyName == "m_LocalRotation.y" || binding.propertyName == "RootQ.y") refRotY = binding;
                    else if (binding.propertyName == $"{rotPrefix}.z" || binding.propertyName == "m_LocalRotation.z" || binding.propertyName == "RootQ.z") refRotZ = binding;
                    else if (binding.propertyName == $"{rotPrefix}.w" || binding.propertyName == "m_LocalRotation.w" || binding.propertyName == "RootQ.w") refRotW = binding;
                }
            }
            
            // 检查是否找到必要的曲线
            bool hasBasePos = basePosX.HasValue && basePosY.HasValue && basePosZ.HasValue;
            bool hasRefPos = refPosX.HasValue && refPosY.HasValue && refPosZ.HasValue;
            
            if (!hasBasePos || !hasRefPos)
            {
                Debug.LogError($"[AnimationRootMotionExtractor] Could not find root bone curves. Base: {hasBasePos}, Ref: {hasRefPos}. Path: {detectedRootBonePath}");
                Object.DestroyImmediate(baseModel);
                Object.DestroyImmediate(refModel);
                return new List<int>();
            }
            
            // 获取曲线
            AnimationCurve basePosXCurve = AnimationUtility.GetEditorCurve(baseClip, basePosX.Value);
            AnimationCurve basePosYCurve = AnimationUtility.GetEditorCurve(baseClip, basePosY.Value);
            AnimationCurve basePosZCurve = AnimationUtility.GetEditorCurve(baseClip, basePosZ.Value);
            
            AnimationCurve refPosXCurve = AnimationUtility.GetEditorCurve(referenceClip, refPosX.Value);
            AnimationCurve refPosYCurve = AnimationUtility.GetEditorCurve(referenceClip, refPosY.Value);
            AnimationCurve refPosZCurve = AnimationUtility.GetEditorCurve(referenceClip, refPosZ.Value);
            
            AnimationCurve baseRotXCurve = baseRotX.HasValue ? AnimationUtility.GetEditorCurve(baseClip, baseRotX.Value) : null;
            AnimationCurve baseRotYCurve = baseRotY.HasValue ? AnimationUtility.GetEditorCurve(baseClip, baseRotY.Value) : null;
            AnimationCurve baseRotZCurve = baseRotZ.HasValue ? AnimationUtility.GetEditorCurve(baseClip, baseRotZ.Value) : null;
            AnimationCurve baseRotWCurve = baseRotW.HasValue ? AnimationUtility.GetEditorCurve(baseClip, baseRotW.Value) : null;
            
            AnimationCurve refRotXCurve = refRotX.HasValue ? AnimationUtility.GetEditorCurve(referenceClip, refRotX.Value) : null;
            AnimationCurve refRotYCurve = refRotY.HasValue ? AnimationUtility.GetEditorCurve(referenceClip, refRotY.Value) : null;
            AnimationCurve refRotZCurve = refRotZ.HasValue ? AnimationUtility.GetEditorCurve(referenceClip, refRotZ.Value) : null;
            AnimationCurve refRotWCurve = refRotW.HasValue ? AnimationUtility.GetEditorCurve(referenceClip, refRotW.Value) : null;
            
            // 格式: [frameCount, dx0, dy0, dz0, rx0, ry0, rz0, rw0, dx1, dy1, dz1, rx1, ry1, rz1, rw1, ...]
            var result = new List<int> { totalFrames };
            
            const int SCALE = 1000; // 缩放因子
            bool hasMotion = false;
            
            Vector3 prevHipsDelta = Vector3.zero;
            Quaternion prevHipsRotationDelta = Quaternion.identity;
            
            // 逐帧采样并计算差值
            for (int frame = 0; frame < totalFrames; frame++)
            {
                float time = Mathf.Clamp(frame * FRAME_TIME, 0f, Mathf.Min(baseClip.length, referenceClip.length));
                
                // 直接从曲线评估值
                Vector3 baseHipsPos = new Vector3(
                    basePosXCurve.Evaluate(time),
                    basePosYCurve.Evaluate(time),
                    basePosZCurve.Evaluate(time)
                );
                
                Vector3 refHipsPos = new Vector3(
                    refPosXCurve.Evaluate(time),
                    refPosYCurve.Evaluate(time),
                    refPosZCurve.Evaluate(time)
                );
                
                // 评估旋转（如果没有曲线，使用默认值）
                Quaternion baseHipsRot = Quaternion.identity;
                Quaternion refHipsRot = Quaternion.identity;
                
                if (baseRotXCurve != null && baseRotYCurve != null && baseRotZCurve != null && baseRotWCurve != null)
                {
                    baseHipsRot = new Quaternion(
                        baseRotXCurve.Evaluate(time),
                        baseRotYCurve.Evaluate(time),
                        baseRotZCurve.Evaluate(time),
                        baseRotWCurve.Evaluate(time)
                    );
                }
                
                if (refRotXCurve != null && refRotYCurve != null && refRotZCurve != null && refRotWCurve != null)
                {
                    refHipsRot = new Quaternion(
                        refRotXCurve.Evaluate(time),
                        refRotYCurve.Evaluate(time),
                        refRotZCurve.Evaluate(time),
                        refRotWCurve.Evaluate(time)
                    );
                }
                
                // 计算差值（参考动画 - 基础动画 = 真正的位移）
                Vector3 hipsDelta = refHipsPos - baseHipsPos;
                Quaternion hipsRotationDelta = refHipsRot * Quaternion.Inverse(baseHipsRot);
                
                // 计算增量位移（相对于上一帧）
                Vector3 deltaPosition = Vector3.zero;
                Quaternion deltaRotation = Quaternion.identity;
                
                if (frame > 0)
                {
                    deltaPosition = hipsDelta - prevHipsDelta;
                    deltaRotation = hipsRotationDelta * Quaternion.Inverse(prevHipsRotationDelta);
                }
                
                // 检查是否有位移
                if (!hasMotion && (deltaPosition.sqrMagnitude > 0.0001f || 
                    Mathf.Abs(deltaRotation.x) > 0.0001f || 
                    Mathf.Abs(deltaRotation.y) > 0.0001f || 
                    Mathf.Abs(deltaRotation.z) > 0.0001f || 
                    Mathf.Abs(deltaRotation.w - 1.0f) > 0.0001f))
                {
                    hasMotion = true;
                    Debug.Log($"[AnimationRootMotionExtractor] Found Hips motion at frame {frame}: deltaPos=({deltaPosition.x:F4}, {deltaPosition.y:F4}, {deltaPosition.z:F4}), hipsDelta=({hipsDelta.x:F4}, {hipsDelta.y:F4}, {hipsDelta.z:F4})");
                }
                
                // 转换为整型并添加到数组
                result.Add((int)(deltaPosition.x * SCALE));
                result.Add((int)(deltaPosition.y * SCALE));
                result.Add((int)(deltaPosition.z * SCALE));
                result.Add((int)(deltaRotation.x * SCALE));
                result.Add((int)(deltaRotation.y * SCALE));
                result.Add((int)(deltaRotation.z * SCALE));
                result.Add((int)(deltaRotation.w * SCALE));
                
                // 保存当前帧数据作为下一帧的前一帧
                prevHipsDelta = hipsDelta;
                prevHipsRotationDelta = hipsRotationDelta;
            }
            
            // 清理临时对象
            Object.DestroyImmediate(baseModel);
            Object.DestroyImmediate(refModel);
            
            if (!hasMotion)
            {
                Debug.LogWarning($"[AnimationRootMotionExtractor] No Hips motion detected between base and reference animations");
                return new List<int>();
            }
            
            Debug.Log($"[AnimationRootMotionExtractor] Extracted Hips motion difference: {totalFrames} frames, {result.Count} integers");
            
            return result;
        }
        
        /// <summary>
        /// 自动检测根骨骼路径（包含完整位置数据的骨骼）
        /// 支持两种格式：
        /// 1. 传统格式：m_LocalPosition.x/y/z, m_LocalRotation.x/y/z/w
        /// 2. 根运动格式：RootT.x/y/z, RootQ.x/y/z/w
        /// </summary>
        private static string FindRootBonePath(EditorCurveBinding[] baseBindings, EditorCurveBinding[] refBindings)
        {
            // 统计每个路径（骨骼）在两个动画中的位置曲线数量
            // 支持两种格式
            Dictionary<string, int> basePathPosCount = new Dictionary<string, int>();
            Dictionary<string, int> refPathPosCount = new Dictionary<string, int>();
            
            // 统计基础动画中每个路径的位置曲线
            foreach (var binding in baseBindings)
            {
                // 传统格式：m_LocalPosition.x/y/z
                // 根运动格式：RootT.x/y/z
                if (binding.propertyName == "m_LocalPosition.x" || 
                    binding.propertyName == "m_LocalPosition.y" || 
                    binding.propertyName == "m_LocalPosition.z" ||
                    binding.propertyName == "RootT.x" ||
                    binding.propertyName == "RootT.y" ||
                    binding.propertyName == "RootT.z")
                {
                    if (!basePathPosCount.ContainsKey(binding.path))
                        basePathPosCount[binding.path] = 0;
                    basePathPosCount[binding.path]++;
                }
            }
            
            // 统计参考动画中每个路径的位置曲线
            foreach (var binding in refBindings)
            {
                if (binding.propertyName == "m_LocalPosition.x" || 
                    binding.propertyName == "m_LocalPosition.y" || 
                    binding.propertyName == "m_LocalPosition.z" ||
                    binding.propertyName == "RootT.x" ||
                    binding.propertyName == "RootT.y" ||
                    binding.propertyName == "RootT.z")
                {
                    if (!refPathPosCount.ContainsKey(binding.path))
                        refPathPosCount[binding.path] = 0;
                    refPathPosCount[binding.path]++;
                }
            }
            
            // 找出在两个动画中都有完整位置数据（x,y,z = 3个曲线）的路径
            List<string> candidatePaths = new List<string>();
            foreach (var kvp in basePathPosCount)
            {
                if (kvp.Value == 3 && refPathPosCount.ContainsKey(kvp.Key) && refPathPosCount[kvp.Key] == 3)
                {
                    candidatePaths.Add(kvp.Key);
                }
            }
            
            if (candidatePaths.Count == 0)
            {
                Debug.LogWarning("[AnimationRootMotionExtractor] No bone found with complete position curves (x,y,z) in both animations.");
                return null;
            }
            
            // 如果有多个候选，选择路径最短的（通常是根骨骼，路径为空或最短）
            // 优先选择空路径（根对象），否则选择最短路径
            string bestPath = null;
            int minDepth = int.MaxValue;
            
            foreach (string path in candidatePaths)
            {
                // 空路径表示根对象，优先级最高
                if (string.IsNullOrEmpty(path))
                {
                    bestPath = path;
                    break;
                }
                
                // 计算路径深度（斜杠数量）
                int depth = path.Split('/').Length;
                if (depth < minDepth)
                {
                    minDepth = depth;
                    bestPath = path;
                }
            }
            
            Debug.Log($"[AnimationRootMotionExtractor] Found {candidatePaths.Count} candidate root bone(s), selected: '{bestPath}' (depth: {minDepth})");
            
            return bestPath;
        }
        
        /// <summary>
        /// 检测动画使用的属性名格式（传统格式或根运动格式）
        /// </summary>
        private static bool UsesRootMotionFormat(EditorCurveBinding[] bindings)
        {
            foreach (var binding in bindings)
            {
                if (binding.propertyName == "RootT.x" || binding.propertyName == "RootQ.x")
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 递归查找骨骼Transform
        /// </summary>
        private static Transform FindBoneRecursive(Transform parent, string boneName)
        {
            if (parent.name == boneName)
            {
                return parent;
            }
            
            foreach (Transform child in parent)
            {
                Transform found = FindBoneRecursive(child, boneName);
                if (found != null)
                {
                    return found;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取Transform的路径（用于调试）
        /// </summary>
        private static string GetTransformPath(Transform transform)
        {
            var path = new System.Collections.Generic.List<string>();
            Transform current = transform;
            while (current != null)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }
            return string.Join("/", path);
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
