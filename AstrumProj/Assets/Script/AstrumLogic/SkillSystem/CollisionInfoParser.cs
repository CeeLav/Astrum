using System;
using Astrum.CommonBase;
using Astrum.LogicCore.Physics;
using TrueSync;

namespace Astrum.LogicCore.SkillSystem
{
    /// <summary>
    /// 碰撞盒信息解析器（简化格式）
    /// 用于从触发帧内联的碰撞盒信息构造 CollisionShape
    /// 格式：Box:5x2x1, Sphere:3.0, Capsule:2x5, Point
    /// </summary>
    public static class CollisionInfoParser
    {
        /// <summary>
        /// 解析碰撞盒信息字符串为 CollisionShape
        /// </summary>
        /// <param name="collisionInfo">碰撞盒信息字符串，例如："Box:5x2x1"</param>
        /// <returns>解析后的 CollisionShape，失败返回 null</returns>
        public static CollisionShape? Parse(string collisionInfo)
        {
            if (string.IsNullOrWhiteSpace(collisionInfo))
                return null;
            
            try
            {
                string[] parts = collisionInfo.Split(':');
                if (parts.Length < 1)
                {
                    ASLogger.Instance.Warning($"[CollisionInfoParser] Invalid collision info: {collisionInfo}");
                    return null;
                }
                
                string shapeType = parts[0].Trim().ToLower();
                
                switch (shapeType)
                {
                    case "box":
                        return ParseBox(parts);
                    
                    case "sphere":
                        return ParseSphere(parts);
                    
                    case "capsule":
                        return ParseCapsule(parts);
                    
                    case "point":
                        return ParsePoint();
                    
                    default:
                        ASLogger.Instance.Warning($"[CollisionInfoParser] Unknown shape type: {shapeType}");
                        return null;
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"[CollisionInfoParser] Parse error: {collisionInfo}, {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 解析盒子碰撞盒
        /// 格式：Box:5x2x1 (宽x高x深)
        /// </summary>
        private static CollisionShape? ParseBox(string[] parts)
        {
            if (parts.Length < 2)
            {
                ASLogger.Instance.Warning("[CollisionInfoParser] Box missing size parameter");
                return null;
            }
            
            // 解析尺寸 (宽x高x深)
            string[] sizeParts = parts[1].Split('x', 'X', '×');
            if (sizeParts.Length != 3)
            {
                ASLogger.Instance.Warning($"[CollisionInfoParser] Box size format error: {parts[1]}");
                return null;
            }
            
            FP width = ParseFP(sizeParts[0]);
            FP height = ParseFP(sizeParts[1]);
            FP depth = ParseFP(sizeParts[2]);
            
            // 半尺寸（CollisionShape使用半尺寸）
            TSVector halfSize = new TSVector(width / 2, height / 2, depth / 2);
            
            return new CollisionShape
            {
                ShapeType = HitBoxShape.Box,
                LocalOffset = TSVector.zero,
                LocalRotation = TSQuaternion.identity,
                HalfSize = halfSize,
                Radius = FP.Zero,
                Height = FP.Zero,
                QueryMode = HitQueryMode.Overlap
            };
        }
        
        /// <summary>
        /// 解析球形碰撞盒
        /// 格式：Sphere:3.0 (半径)
        /// </summary>
        private static CollisionShape? ParseSphere(string[] parts)
        {
            if (parts.Length < 2)
            {
                ASLogger.Instance.Warning("[CollisionInfoParser] Sphere missing radius parameter");
                return null;
            }
            
            FP radius = ParseFP(parts[1]);
            
            return new CollisionShape
            {
                ShapeType = HitBoxShape.Sphere,
                LocalOffset = TSVector.zero,
                LocalRotation = TSQuaternion.identity,
                HalfSize = TSVector.zero,
                Radius = radius,
                Height = FP.Zero,
                QueryMode = HitQueryMode.Overlap
            };
        }
        
        /// <summary>
        /// 解析胶囊碰撞盒
        /// 格式：Capsule:2x5 (半径x高度)
        /// </summary>
        private static CollisionShape? ParseCapsule(string[] parts)
        {
            if (parts.Length < 2)
            {
                ASLogger.Instance.Warning("[CollisionInfoParser] Capsule missing parameters");
                return null;
            }
            
            // 解析 半径x高度
            string[] sizeParts = parts[1].Split('x', 'X', '×');
            if (sizeParts.Length != 2)
            {
                ASLogger.Instance.Warning($"[CollisionInfoParser] Capsule format error: {parts[1]}");
                return null;
            }
            
            FP radius = ParseFP(sizeParts[0]);
            FP height = ParseFP(sizeParts[1]);
            
            return new CollisionShape
            {
                ShapeType = HitBoxShape.Capsule,
                LocalOffset = TSVector.zero,
                LocalRotation = TSQuaternion.identity,
                HalfSize = TSVector.zero,
                Radius = radius,
                Height = height,
                QueryMode = HitQueryMode.Overlap
            };
        }
        
        /// <summary>
        /// 解析点碰撞盒（球形半径为0）
        /// 格式：Point
        /// </summary>
        private static CollisionShape? ParsePoint()
        {
            return new CollisionShape
            {
                ShapeType = HitBoxShape.Sphere,
                LocalOffset = TSVector.zero,
                LocalRotation = TSQuaternion.identity,
                HalfSize = TSVector.zero,
                Radius = FP.Zero,
                Height = FP.Zero,
                QueryMode = HitQueryMode.Overlap
            };
        }
        
        /// <summary>
        /// 解析定点数（FP）
        /// </summary>
        private static FP ParseFP(string value)
        {
            value = value.Trim();
            
            if (decimal.TryParse(value, out var result))
            {
                return (FP)result;
            }
            
            ASLogger.Instance.Warning($"[CollisionInfoParser] Failed to parse FP: {value}, using 0");
            return FP.Zero;
        }
    }
}

