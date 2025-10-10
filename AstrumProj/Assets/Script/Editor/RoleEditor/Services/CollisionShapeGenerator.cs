using UnityEngine;
using UnityEditor;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 碰撞形状自动生成工具
    /// 基于模型 Bounds 自动计算碰撞盒参数
    /// </summary>
    public static class CollisionShapeGenerator
    {
        /// <summary>
        /// 从模型自动生成胶囊体碰撞盒
        /// </summary>
        /// <param name="prefab">模型 Prefab</param>
        /// <param name="radiusScale">半径缩放因子 (0.3-1.0)，默认 0.5 为标准人形</param>
        /// <returns>CollisionData 字符串，格式: Capsule:偏移x,y,z:旋转x,y,z,w:半径:高度</returns>
        public static string GenerateCapsuleFromModel(GameObject prefab, float radiusScale = 0.5f)
        {
            if (prefab == null)
            {
                Debug.LogError("[CollisionShapeGenerator] Prefab is null");
                return string.Empty;
            }
            
            // 计算模型边界
            Bounds bounds = CalculateModelBounds(prefab);
            
            if (bounds.size == Vector3.zero)
            {
                Debug.LogWarning($"[CollisionShapeGenerator] Model has no renderers: {prefab.name}");
                return string.Empty;
            }
            
            // 计算胶囊体参数
            float height = bounds.size.y * 0.95f;  // 略小于实际高度
            // 使用最小值（更贴近身体实际宽度，避免武器/装备影响）
            float radius = Mathf.Min(bounds.size.x, bounds.size.z) / 2f * Mathf.Clamp(radiusScale, 0.3f, 1.0f);
            
            // 偏移：胶囊体底部在模型脚下，中心在模型中央
            Vector3 offset = new Vector3(0, bounds.size.y / 2f, 0);
            
            // 旋转：垂直朝上
            Quaternion rotation = Quaternion.identity;
            
            // 格式化为配置字符串
            string collisionData = FormatCapsuleData(offset, rotation, radius, height);
            
            Debug.Log($"[CollisionShapeGenerator] Generated capsule for {prefab.name}: " +
                     $"height={height:F2}, radius={radius:F2}, offset={offset}");
            
            return collisionData;
        }
        
        /// <summary>
        /// 计算模型的完整边界框
        /// </summary>
        private static Bounds CalculateModelBounds(GameObject prefab)
        {
            // 临时实例化以获取准确的 Bounds
            GameObject tempInstance = Object.Instantiate(prefab);
            tempInstance.hideFlags = HideFlags.HideAndDontSave;
            
            try
            {
                // 获取所有渲染器
                var renderers = tempInstance.GetComponentsInChildren<Renderer>();
                
                if (renderers.Length == 0)
                {
                    return new Bounds(Vector3.zero, Vector3.zero);
                }
                
                // 计算包围所有渲染器的边界框
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                
                // 转换为相对于模型根节点的局部边界
                Vector3 localCenter = tempInstance.transform.InverseTransformPoint(bounds.center);
                Vector3 localSize = bounds.size;
                
                return new Bounds(localCenter, localSize);
            }
            finally
            {
                // 清理临时实例
                Object.DestroyImmediate(tempInstance);
            }
        }
        
        /// <summary>
        /// 格式化胶囊体数据为字符串
        /// 格式：Capsule:偏移x,y,z:旋转x,y,z,w:半径:高度
        /// </summary>
        private static string FormatCapsuleData(Vector3 offset, Quaternion rotation, float radius, float height)
        {
            return $"Capsule:" +
                   $"{offset.x:F2},{offset.y:F2},{offset.z:F2}:" +
                   $"{rotation.x:F2},{rotation.y:F2},{rotation.z:F2},{rotation.w:F2}:" +
                   $"{radius:F2}:" +
                   $"{height:F2}";
        }
        
        /// <summary>
        /// 从模型自动生成 Box 碰撞盒
        /// </summary>
        public static string GenerateBoxFromModel(GameObject prefab, float scale = 0.9f)
        {
            if (prefab == null)
            {
                Debug.LogError("[CollisionShapeGenerator] Prefab is null");
                return string.Empty;
            }
            
            Bounds bounds = CalculateModelBounds(prefab);
            
            if (bounds.size == Vector3.zero)
            {
                Debug.LogWarning($"[CollisionShapeGenerator] Model has no renderers: {prefab.name}");
                return string.Empty;
            }
            
            // Box 半尺寸
            Vector3 halfSize = bounds.size / 2f * scale;
            Vector3 offset = bounds.center;
            Quaternion rotation = Quaternion.identity;
            
            return FormatBoxData(offset, rotation, halfSize);
        }
        
        /// <summary>
        /// 格式化 Box 数据为字符串
        /// 格式：Box:偏移x,y,z:旋转x,y,z,w:半尺寸x,y,z
        /// </summary>
        private static string FormatBoxData(Vector3 offset, Quaternion rotation, Vector3 halfSize)
        {
            return $"Box:" +
                   $"{offset.x:F2},{offset.y:F2},{offset.z:F2}:" +
                   $"{rotation.x:F2},{rotation.y:F2},{rotation.z:F2},{rotation.w:F2}:" +
                   $"{halfSize.x:F2},{halfSize.y:F2},{halfSize.z:F2}";
        }
    }
}

