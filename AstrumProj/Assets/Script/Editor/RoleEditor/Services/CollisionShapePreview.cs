using UnityEngine;
using UnityEditor;
using Astrum.LogicCore.Physics;
using Astrum.LogicCore.SkillSystem;
using System.Collections.Generic;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 碰撞形状预览绘制工具
    /// 使用 GL 即时模式在预览窗口中绘制碰撞盒线框
    /// </summary>
    public static class CollisionShapePreview
    {
        private static Material _lineMaterial;
        private const int CIRCLE_SEGMENTS = 16;  // 圆形分段数
        
        /// <summary>
        /// 获取线框材质
        /// </summary>
        private static Material GetLineMaterial()
        {
            if (_lineMaterial == null)
            {
                // 创建内部着色器材质用于绘制线框
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                _lineMaterial = new Material(shader);
                _lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _lineMaterial.SetInt("_ZWrite", 0);
                _lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }
            return _lineMaterial;
        }
        
        /// <summary>
        /// 从 CollisionData 字符串绘制所有碰撞形状
        /// </summary>
        /// <param name="collisionData">碰撞数据字符串</param>
        /// <param name="camera">预览相机</param>
        /// <param name="color">线框颜色（默认绿色）</param>
        /// <param name="modelPosition">模型在预览场景中的位置（默认为原点）</param>
        public static void DrawCollisionData(string collisionData, Camera camera, Color? color = null, Vector3? modelPosition = null)
        {
            if (string.IsNullOrEmpty(collisionData) || camera == null)
                return;
            
            // 解析碰撞数据
            var shapes = CollisionDataParser.Parse(collisionData);
            if (shapes.Count == 0)
                return;
            
            Color wireColor = color ?? new Color(0f, 1f, 0f, 0.8f);  // 默认半透明绿色
            Vector3 basePosition = modelPosition ?? Vector3.zero;
            
            // 设置 GL 绘制上下文
            GetLineMaterial().SetPass(0);
            
            GL.PushMatrix();
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            GL.modelview = camera.worldToCameraMatrix;
            
            // 绘制每个碰撞形状（考虑模型位置）
            foreach (var shape in shapes)
            {
                DrawShape(shape, wireColor, basePosition);
            }
            
            GL.PopMatrix();
        }
        
        /// <summary>
        /// 从简化格式的 CollisionInfo 字符串绘制碰撞盒
        /// 格式：Box:5x2x1, Sphere:3.0, Capsule:2x5, Point
        /// </summary>
        /// <param name="collisionInfo">碰撞盒信息字符串（简化格式）</param>
        /// <param name="camera">预览相机</param>
        /// <param name="color">线框颜色（默认黄色）</param>
        /// <param name="modelPosition">模型在预览场景中的位置（默认为原点）</param>
        public static void DrawCollisionInfo(string collisionInfo, Camera camera, Color? color = null, Vector3? modelPosition = null)
        {
            if (string.IsNullOrEmpty(collisionInfo) || camera == null)
                return;
            
            // 使用 CollisionInfoParser 解析简化格式
            var shape = CollisionInfoParser.Parse(collisionInfo);
            if (shape == null)
                return;
            
            Color wireColor = color ?? new Color(1f, 1f, 0f, 0.8f);  // 默认半透明黄色（技能碰撞盒）
            Vector3 basePosition = modelPosition ?? Vector3.zero;
            
            // 设置 GL 绘制上下文
            GetLineMaterial().SetPass(0);
            
            GL.PushMatrix();
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            GL.modelview = camera.worldToCameraMatrix;
            
            // 绘制碰撞形状（考虑模型位置）
            DrawShape(shape.Value, wireColor, basePosition);
            
            GL.PopMatrix();
        }
        
        /// <summary>
        /// 绘制单个碰撞形状
        /// </summary>
        /// <param name="shape">碰撞形状</param>
        /// <param name="color">绘制颜色</param>
        /// <param name="basePosition">基准位置（模型在预览场景中的位置）</param>
        private static void DrawShape(CollisionShape shape, Color color, Vector3 basePosition = default)
        {
            // 将 TrueSync 类型转换为 Unity 类型
            Vector3 offset = new Vector3(
                (float)shape.LocalOffset.x,
                (float)shape.LocalOffset.y,
                (float)shape.LocalOffset.z
            );
            
            // 碰撞盒的实际中心位置 = 模型位置 + 相对偏移
            Vector3 center = basePosition + offset;
            
            Quaternion rotation = new Quaternion(
                (float)shape.LocalRotation.x,
                (float)shape.LocalRotation.y,
                (float)shape.LocalRotation.z,
                (float)shape.LocalRotation.w
            );
            
            switch (shape.ShapeType)
            {
                case HitBoxShape.Capsule:
                    float radius = (float)shape.Radius;
                    float height = (float)shape.Height;
                    DrawCapsuleWireframe(center, rotation, radius, height, color);
                    break;
                
                case HitBoxShape.Box:
                    Vector3 halfSize = new Vector3(
                        (float)shape.HalfSize.x,
                        (float)shape.HalfSize.y,
                        (float)shape.HalfSize.z
                    );
                    DrawBoxWireframe(center, rotation, halfSize, color);
                    break;
                
                case HitBoxShape.Sphere:
                    DrawSphereWireframe(center, (float)shape.Radius, color);
                    break;
            }
        }
        
        /// <summary>
        /// 绘制胶囊体线框
        /// </summary>
        public static void DrawCapsuleWireframe(Vector3 center, Quaternion rotation, float radius, float height, Color color)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);
            
            // 胶囊体 = 上半球 + 圆柱 + 下半球
            float cylinderHeight = Mathf.Max(0, height - radius * 2f);
            float halfCylinderHeight = cylinderHeight / 2f;
            
            // 上半球中心和下半球中心
            Vector3 topSphereCenter = center + rotation * new Vector3(0, halfCylinderHeight, 0);
            Vector3 bottomSphereCenter = center + rotation * new Vector3(0, -halfCylinderHeight, 0);
            
            // 绘制圆柱部分的竖直线（4 条）
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI / 2f;
                Vector3 offset2D = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Vector3 offset3D = rotation * offset2D;
                
                Vector3 top = topSphereCenter + offset3D;
                Vector3 bottom = bottomSphereCenter + offset3D;
                
                GL.Vertex(top);
                GL.Vertex(bottom);
            }
            
            // 绘制上半球
            DrawHalfSphere(topSphereCenter, rotation, radius, true);
            
            // 绘制下半球
            DrawHalfSphere(bottomSphereCenter, rotation, radius, false);
            
            // 绘制中间圆环（XZ 平面）
            DrawCircle(center, rotation, radius, Vector3.up);
            
            GL.End();
        }
        
        /// <summary>
        /// 绘制半球
        /// </summary>
        private static void DrawHalfSphere(Vector3 center, Quaternion rotation, float radius, bool upperHalf)
        {
            int segments = CIRCLE_SEGMENTS / 2;  // 半球用一半的分段
            
            // 绘制经线（多条从顶部到赤道的圆弧）
            for (int i = 0; i < CIRCLE_SEGMENTS; i++)
            {
                float angle = i * 2f * Mathf.PI / CIRCLE_SEGMENTS;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                
                for (int j = 0; j < segments; j++)
                {
                    float t1 = upperHalf ? (float)j / segments : -(float)j / segments;
                    float t2 = upperHalf ? (float)(j + 1) / segments : -(float)(j + 1) / segments;
                    
                    float angle1 = t1 * Mathf.PI / 2f;
                    float angle2 = t2 * Mathf.PI / 2f;
                    
                    Vector3 p1 = new Vector3(
                        direction.x * Mathf.Cos(angle1) * radius,
                        Mathf.Sin(angle1) * radius,
                        direction.z * Mathf.Cos(angle1) * radius
                    );
                    
                    Vector3 p2 = new Vector3(
                        direction.x * Mathf.Cos(angle2) * radius,
                        Mathf.Sin(angle2) * radius,
                        direction.z * Mathf.Cos(angle2) * radius
                    );
                    
                    GL.Vertex(center + rotation * p1);
                    GL.Vertex(center + rotation * p2);
                }
            }
            
            // 绘制纬线（半球的几个水平圆环）
            for (int j = 1; j < segments; j++)
            {
                float t = upperHalf ? (float)j / segments : -(float)j / segments;
                float angle = t * Mathf.PI / 2f;
                float circleRadius = Mathf.Cos(angle) * radius;
                float circleHeight = Mathf.Sin(angle) * radius;
                
                Vector3 circleCenter = center + rotation * new Vector3(0, circleHeight, 0);
                DrawCircle(circleCenter, rotation, circleRadius, Vector3.up);
            }
        }
        
        /// <summary>
        /// 绘制圆形
        /// </summary>
        private static void DrawCircle(Vector3 center, Quaternion rotation, float radius, Vector3 normal)
        {
            for (int i = 0; i < CIRCLE_SEGMENTS; i++)
            {
                float angle1 = i * 2f * Mathf.PI / CIRCLE_SEGMENTS;
                float angle2 = (i + 1) * 2f * Mathf.PI / CIRCLE_SEGMENTS;
                
                // 在垂直于 normal 的平面上绘制圆
                Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;
                if (tangent == Vector3.zero) tangent = Vector3.right;
                Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
                
                Vector3 p1 = center + rotation * (tangent * Mathf.Cos(angle1) * radius + bitangent * Mathf.Sin(angle1) * radius);
                Vector3 p2 = center + rotation * (tangent * Mathf.Cos(angle2) * radius + bitangent * Mathf.Sin(angle2) * radius);
                
                GL.Vertex(p1);
                GL.Vertex(p2);
            }
        }
        
        /// <summary>
        /// 绘制 Box 线框
        /// </summary>
        public static void DrawBoxWireframe(Vector3 center, Quaternion rotation, Vector3 halfSize, Color color)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);
            
            // Box 的 8 个顶点
            Vector3[] corners = new Vector3[8];
            corners[0] = rotation * new Vector3(-halfSize.x, -halfSize.y, -halfSize.z) + center;
            corners[1] = rotation * new Vector3(halfSize.x, -halfSize.y, -halfSize.z) + center;
            corners[2] = rotation * new Vector3(halfSize.x, -halfSize.y, halfSize.z) + center;
            corners[3] = rotation * new Vector3(-halfSize.x, -halfSize.y, halfSize.z) + center;
            corners[4] = rotation * new Vector3(-halfSize.x, halfSize.y, -halfSize.z) + center;
            corners[5] = rotation * new Vector3(halfSize.x, halfSize.y, -halfSize.z) + center;
            corners[6] = rotation * new Vector3(halfSize.x, halfSize.y, halfSize.z) + center;
            corners[7] = rotation * new Vector3(-halfSize.x, halfSize.y, halfSize.z) + center;
            
            // 底面 4 条边
            DrawLine(corners[0], corners[1]);
            DrawLine(corners[1], corners[2]);
            DrawLine(corners[2], corners[3]);
            DrawLine(corners[3], corners[0]);
            
            // 顶面 4 条边
            DrawLine(corners[4], corners[5]);
            DrawLine(corners[5], corners[6]);
            DrawLine(corners[6], corners[7]);
            DrawLine(corners[7], corners[4]);
            
            // 4 条竖边
            DrawLine(corners[0], corners[4]);
            DrawLine(corners[1], corners[5]);
            DrawLine(corners[2], corners[6]);
            DrawLine(corners[3], corners[7]);
            
            GL.End();
        }
        
        /// <summary>
        /// 绘制球体线框
        /// </summary>
        public static void DrawSphereWireframe(Vector3 center, float radius, Color color)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);
            
            // 绘制 3 个正交圆环（XY, XZ, YZ 平面）
            DrawCircleOnPlane(center, radius, Vector3.forward);  // XY 平面
            DrawCircleOnPlane(center, radius, Vector3.up);       // XZ 平面
            DrawCircleOnPlane(center, radius, Vector3.right);    // YZ 平面
            
            GL.End();
        }
        
        /// <summary>
        /// 在指定平面上绘制圆
        /// </summary>
        private static void DrawCircleOnPlane(Vector3 center, float radius, Vector3 normal)
        {
            Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;
            if (tangent == Vector3.zero) tangent = Vector3.right;
            Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
            
            for (int i = 0; i < CIRCLE_SEGMENTS; i++)
            {
                float angle1 = i * 2f * Mathf.PI / CIRCLE_SEGMENTS;
                float angle2 = (i + 1) * 2f * Mathf.PI / CIRCLE_SEGMENTS;
                
                Vector3 p1 = center + tangent * Mathf.Cos(angle1) * radius + bitangent * Mathf.Sin(angle1) * radius;
                Vector3 p2 = center + tangent * Mathf.Cos(angle2) * radius + bitangent * Mathf.Sin(angle2) * radius;
                
                GL.Vertex(p1);
                GL.Vertex(p2);
            }
        }
        
        /// <summary>
        /// 绘制线段（GL 辅助方法）
        /// </summary>
        private static void DrawLine(Vector3 p1, Vector3 p2)
        {
            GL.Vertex(p1);
            GL.Vertex(p2);
        }
        
        /// <summary>
        /// 在 Scene 视图中绘制碰撞盒（用于调试）
        /// </summary>
        public static void DrawCollisionDataGizmos(string collisionData, Transform transform, Color? color = null)
        {
            if (string.IsNullOrEmpty(collisionData))
                return;
            
            var shapes = CollisionDataParser.Parse(collisionData);
            Color gizmoColor = color ?? Color.green;
            Gizmos.color = gizmoColor;
            
            foreach (var shape in shapes)
            {
                Vector3 worldOffset = transform.TransformPoint(new Vector3(
                    (float)shape.LocalOffset.x,
                    (float)shape.LocalOffset.y,
                    (float)shape.LocalOffset.z
                ));
                
                Quaternion worldRotation = transform.rotation * new Quaternion(
                    (float)shape.LocalRotation.x,
                    (float)shape.LocalRotation.y,
                    (float)shape.LocalRotation.z,
                    (float)shape.LocalRotation.w
                );
                
                switch (shape.ShapeType)
                {
                    case HitBoxShape.Capsule:
                        DrawCapsuleGizmo(worldOffset, worldRotation, (float)shape.Radius, (float)shape.Height);
                        break;
                    
                    case HitBoxShape.Box:
                        Vector3 size = new Vector3(
                            (float)shape.HalfSize.x * 2f,
                            (float)shape.HalfSize.y * 2f,
                            (float)shape.HalfSize.z * 2f
                        );
                        Gizmos.matrix = Matrix4x4.TRS(worldOffset, worldRotation, Vector3.one);
                        Gizmos.DrawWireCube(Vector3.zero, size);
                        Gizmos.matrix = Matrix4x4.identity;
                        break;
                    
                    case HitBoxShape.Sphere:
                        Gizmos.DrawWireSphere(worldOffset, (float)shape.Radius);
                        break;
                }
            }
        }
        
        /// <summary>
        /// 使用 Gizmos 绘制胶囊体（近似）
        /// </summary>
        private static void DrawCapsuleGizmo(Vector3 center, Quaternion rotation, float radius, float height)
        {
            float cylinderHeight = Mathf.Max(0, height - radius * 2f);
            Vector3 up = rotation * Vector3.up;
            
            Vector3 topCenter = center + up * cylinderHeight / 2f;
            Vector3 bottomCenter = center - up * cylinderHeight / 2f;
            
            // 绘制上下两个球
            Gizmos.DrawWireSphere(topCenter, radius);
            Gizmos.DrawWireSphere(bottomCenter, radius);
            
            // 绘制中间圆环
            Gizmos.DrawWireSphere(center, radius);
        }
    }
}

