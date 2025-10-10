using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.CommonBase.Structure.TrueSync;

namespace Astrum.LogicCore.Physics
{
    /// <summary>
    /// 碰撞盒数据解析器
    /// 格式：形状1|形状2|...
    /// Box 格式：Box:偏移x,y,z:旋转x,y,z,w:半尺寸x,y,z
    /// Sphere 格式：Sphere:偏移x,y,z:旋转x,y,z,w:半径
    /// Capsule 格式：Capsule:偏移x,y,z:旋转x,y,z,w:半径:高度
    /// </summary>
    public static class CollisionDataParser
    {
        /// <summary>
        /// 解析碰撞数据字符串
        /// </summary>
        public static List<CollisionShape> Parse(string collisionData)
        {
            var shapes = new List<CollisionShape>();
            
            if (string.IsNullOrEmpty(collisionData))
                return shapes;
            
            try
            {
                var shapeStrings = collisionData.Split('|');
                
                foreach (var shapeStr in shapeStrings)
                {
                    if (string.IsNullOrWhiteSpace(shapeStr))
                        continue;
                    
                    var parts = shapeStr.Split(':');
                    if (parts.Length < 3)
                    {
                        ASLogger.Instance.Warning($"CollisionDataParser: Invalid shape format: {shapeStr}");
                        continue;
                    }
                    
                    var shapeTypeName = parts[0].Trim();
                    var shape = ParseShape(shapeTypeName, parts);
                    
                    if (shape.HasValue)
                        shapes.Add(shape.Value);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"CollisionDataParser: Failed to parse collision data: {collisionData}, Error: {ex.Message}");
            }
            
            return shapes;
        }
        
        private static CollisionShape? ParseShape(string shapeType, string[] parts)
        {
            try
            {
                // 解析偏移（parts[1]: x,y,z）
                var offsetParts = parts[1].Split(',');
                var offset = new TSVector(
                    ParseFloat(offsetParts[0]),
                    ParseFloat(offsetParts[1]),
                    ParseFloat(offsetParts[2])
                );
                
                // 解析旋转（parts[2]: x,y,z,w）
                var rotationParts = parts[2].Split(',');
                var rotation = new TSQuaternion(
                    ParseFloat(rotationParts[0]),
                    ParseFloat(rotationParts[1]),
                    ParseFloat(rotationParts[2]),
                    ParseFloat(rotationParts[3])
                );
                
                switch (shapeType.ToLower())
                {
                    case "box":
                        if (parts.Length < 4)
                        {
                            ASLogger.Instance.Warning($"CollisionDataParser: Box missing halfSize parameter");
                            return null;
                        }
                        // parts[3]: 半尺寸 x,y,z
                        var halfSizeParts = parts[3].Split(',');
                        var halfSize = new TSVector(
                            ParseFloat(halfSizeParts[0]),
                            ParseFloat(halfSizeParts[1]),
                            ParseFloat(halfSizeParts[2])
                        );
                        return new CollisionShape(
                            HitBoxShape.Box,
                            offset,
                            rotation,
                            halfSize,
                            0f,
                            0f
                        );
                    
                    case "sphere":
                        if (parts.Length < 4)
                        {
                            ASLogger.Instance.Warning($"CollisionDataParser: Sphere missing radius parameter");
                            return null;
                        }
                        // parts[3]: 半径
                        var radius = ParseFloat(parts[3]);
                        return new CollisionShape(
                            HitBoxShape.Sphere,
                            offset,
                            rotation,
                            TSVector.zero,
                            radius,
                            0f
                        );
                    
                    case "capsule":
                        if (parts.Length < 5)
                        {
                            ASLogger.Instance.Warning($"CollisionDataParser: Capsule missing radius or height parameter");
                            return null;
                        }
                        // parts[3]: 半径, parts[4]: 高度
                        var capsuleRadius = ParseFloat(parts[3]);
                        var height = ParseFloat(parts[4]);
                        return new CollisionShape(
                            HitBoxShape.Capsule,
                            offset,
                            rotation,
                            TSVector.zero,
                            capsuleRadius,
                            height
                        );
                    
                    default:
                        ASLogger.Instance.Warning($"CollisionDataParser: Unknown shape type: {shapeType}");
                        return null;
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"CollisionDataParser: Failed to parse shape {shapeType}, Error: {ex.Message}");
                return null;
            }
        }
        
        private static float ParseFloat(string value)
        {
            if (float.TryParse(value.Trim(), out var result))
                return result;
            
            ASLogger.Instance.Warning($"CollisionDataParser: Failed to parse float value: {value}, using 0");
            return 0f;
        }
    }
}

