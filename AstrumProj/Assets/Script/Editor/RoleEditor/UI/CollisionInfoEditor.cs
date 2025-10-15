using UnityEngine;
using UnityEditor;
using System;

namespace Astrum.Editor.RoleEditor.UI
{
    /// <summary>
    /// 碰撞盒信息可视化编辑器
    /// 提供友好的UI来编辑碰撞盒信息字符串（支持偏移量）
    /// </summary>
    public static class CollisionInfoEditor
    {
        // 碰撞盒类型枚举（用于UI）
        public enum CollisionShapeType
        {
            None = 0,
            Box = 1,
            Sphere = 2,
            Capsule = 3,
            Point = 4
        }
        
        /// <summary>
        /// 绘制碰撞盒编辑器
        /// </summary>
        /// <param name="collisionInfo">当前的碰撞盒信息字符串</param>
        /// <returns>更新后的碰撞盒信息字符串，如果未修改则返回原值</returns>
        public static string DrawCollisionInfoEditor(string collisionInfo, out bool modified)
        {
            modified = false;
            
            // 解析当前值
            var (shapeType, parameters, offset) = ParseCollisionInfo(collisionInfo);
            
            // 类型选择
            EditorGUI.BeginChangeCheck();
            CollisionShapeType newShapeType = (CollisionShapeType)EditorGUILayout.EnumPopup("碰撞盒类型", shapeType);
            if (EditorGUI.EndChangeCheck())
            {
                modified = true;
                shapeType = newShapeType;
                
                // 类型改变时，使用默认参数
                if (shapeType == CollisionShapeType.None)
                {
                    return "";
                }
            }
            
            if (shapeType == CollisionShapeType.None)
            {
                EditorGUILayout.HelpBox("请选择碰撞盒类型", MessageType.Info);
                return collisionInfo;
            }
            
            EditorGUILayout.Space(5);
            
            // 根据类型绘制不同的参数编辑器
            string baseCollisionInfo = collisionInfo;
            
            switch (shapeType)
            {
                case CollisionShapeType.Box:
                    baseCollisionInfo = DrawBoxEditor(parameters, out bool boxModified);
                    modified |= boxModified;
                    break;
                    
                case CollisionShapeType.Sphere:
                    baseCollisionInfo = DrawSphereEditor(parameters, out bool sphereModified);
                    modified |= sphereModified;
                    break;
                    
                case CollisionShapeType.Capsule:
                    baseCollisionInfo = DrawCapsuleEditor(parameters, out bool capsuleModified);
                    modified |= capsuleModified;
                    break;
                    
                case CollisionShapeType.Point:
                    baseCollisionInfo = "Point";
                    EditorGUILayout.HelpBox("点碰撞不需要额外参数", MessageType.Info);
                    break;
            }
            
            // 偏移量编辑器（高级设置）
            EditorGUILayout.Space(5);
            
            // 添加分隔线，使偏移量区域更明显
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            Vector3 newOffset = DrawOffsetEditor(offset, out bool offsetModified);
            modified |= offsetModified;
            
            // 组合最终的碰撞盒信息字符串
            string newCollisionInfo = baseCollisionInfo;
            if (newOffset != Vector3.zero)
            {
                newCollisionInfo += $"@{FormatFloat(newOffset.x)},{FormatFloat(newOffset.y)},{FormatFloat(newOffset.z)}";
            }
            
            // 显示预览
            if (!string.IsNullOrEmpty(newCollisionInfo))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("字符串格式预览:", EditorStyles.boldLabel);
                    EditorGUILayout.SelectableLabel(newCollisionInfo, EditorStyles.textField, GUILayout.Height(20));
                }
                EditorGUILayout.EndVertical();
            }
            
            return newCollisionInfo;
        }
        
        /// <summary>
        /// 绘制Box碰撞盒编辑器
        /// </summary>
        private static string DrawBoxEditor((float, float, float) parameters, out bool modified)
        {
            modified = false;
            
            EditorGUILayout.LabelField("盒子尺寸（全尺寸）", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            
            EditorGUI.BeginChangeCheck();
            float width = EditorGUILayout.FloatField("宽度 (X)", parameters.Item1 > 0 ? parameters.Item1 : 1f);
            float height = EditorGUILayout.FloatField("高度 (Y)", parameters.Item2 > 0 ? parameters.Item2 : 1f);
            float depth = EditorGUILayout.FloatField("深度 (Z)", parameters.Item3 > 0 ? parameters.Item3 : 1f);
            
            // 限制最小值
            width = Mathf.Max(0.1f, width);
            height = Mathf.Max(0.1f, height);
            depth = Mathf.Max(0.1f, depth);
            
            if (EditorGUI.EndChangeCheck())
            {
                modified = true;
            }
            
            EditorGUI.indentLevel--;
            
            // 可视化提示
            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox(
                $"碰撞盒范围：\n" +
                $"X: ±{width / 2:F1}m  Y: ±{height / 2:F1}m  Z: ±{depth / 2:F1}m",
                MessageType.None
            );
            
            // 格式化为字符串
            return $"Box:{FormatFloat(width)}x{FormatFloat(height)}x{FormatFloat(depth)}";
        }
        
        /// <summary>
        /// 绘制Sphere碰撞盒编辑器
        /// </summary>
        private static string DrawSphereEditor((float, float, float) parameters, out bool modified)
        {
            modified = false;
            
            EditorGUILayout.LabelField("球形参数", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            
            EditorGUI.BeginChangeCheck();
            float radius = EditorGUILayout.FloatField("半径", parameters.Item1 > 0 ? parameters.Item1 : 1f);
            
            // 限制最小值
            radius = Mathf.Max(0.1f, radius);
            
            if (EditorGUI.EndChangeCheck())
            {
                modified = true;
            }
            
            EditorGUI.indentLevel--;
            
            // 可视化提示
            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox(
                $"碰撞球半径: {radius:F1}m\n" +
                $"碰撞范围直径: {radius * 2:F1}m",
                MessageType.None
            );
            
            // 格式化为字符串
            return $"Sphere:{FormatFloat(radius)}";
        }
        
        /// <summary>
        /// 绘制Capsule碰撞盒编辑器
        /// </summary>
        private static string DrawCapsuleEditor((float, float, float) parameters, out bool modified)
        {
            modified = false;
            
            EditorGUILayout.LabelField("胶囊体参数", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            
            EditorGUI.BeginChangeCheck();
            float radius = EditorGUILayout.FloatField("半径", parameters.Item1 > 0 ? parameters.Item1 : 0.5f);
            float height = EditorGUILayout.FloatField("高度", parameters.Item2 > 0 ? parameters.Item2 : 2f);
            
            // 限制最小值
            radius = Mathf.Max(0.1f, radius);
            height = Mathf.Max(radius * 2, height); // 高度至少是直径
            
            if (EditorGUI.EndChangeCheck())
            {
                modified = true;
            }
            
            EditorGUI.indentLevel--;
            
            // 可视化提示
            EditorGUILayout.Space(3);
            float cylinderHeight = Mathf.Max(0, height - radius * 2);
            EditorGUILayout.HelpBox(
                $"胶囊体组成：\n" +
                $"• 圆柱部分高度: {cylinderHeight:F1}m\n" +
                $"• 上下半球半径: {radius:F1}m\n" +
                $"• 总高度: {height:F1}m",
                MessageType.None
            );
            
            // 格式化为字符串
            return $"Capsule:{FormatFloat(radius)}x{FormatFloat(height)}";
        }
        
        /// <summary>
        /// 绘制偏移量编辑器
        /// </summary>
        private static Vector3 DrawOffsetEditor(Vector3 currentOffset, out bool modified)
        {
            modified = false;
            Vector3 newOffset = currentOffset;  // 在方法开始处声明
            
            // 偏移量设置区域（使用更明显的样式）
            EditorGUILayout.BeginVertical("HelpBox");
            {
                // 使用大号粗体标题
                GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
                titleStyle.fontSize = 12;
                EditorGUILayout.LabelField("⚙️ 偏移量设置（高级）", titleStyle);
                EditorGUILayout.Space(3);
                
                EditorGUI.BeginChangeCheck();
                newOffset = EditorGUILayout.Vector3Field("本地偏移 (X,Y,Z)", currentOffset);
                if (EditorGUI.EndChangeCheck())
                {
                    modified = true;
                }
                
                EditorGUILayout.Space(3);
                
                // 提示信息
                if (newOffset != Vector3.zero)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.HelpBox(
                        $"碰撞盒将相对角色中心偏移：\n" +
                        $"• 前后（Z）: {(newOffset.z > 0 ? "向前" : newOffset.z < 0 ? "向后" : "无")} {Mathf.Abs(newOffset.z):F1}m\n" +
                        $"• 上下（Y）: {(newOffset.y > 0 ? "向上" : newOffset.y < 0 ? "向下" : "无")} {Mathf.Abs(newOffset.y):F1}m\n" +
                        $"• 左右（X）: {(newOffset.x > 0 ? "向右" : newOffset.x < 0 ? "向左" : "无")} {Mathf.Abs(newOffset.x):F1}m",
                        MessageType.Info
                    );
                }
                else
                {
                    EditorGUILayout.HelpBox("偏移量为零，碰撞盒中心位于角色中心", MessageType.None);
                }
            }
            EditorGUILayout.EndVertical();
            
            return newOffset;
        }
        
        /// <summary>
        /// 解析碰撞盒信息字符串
        /// </summary>
        /// <returns>(类型, (参数1, 参数2, 参数3), 偏移量)</returns>
        private static (CollisionShapeType, (float, float, float), Vector3) ParseCollisionInfo(string collisionInfo)
        {
            if (string.IsNullOrEmpty(collisionInfo))
            {
                return (CollisionShapeType.None, (0f, 0f, 0f), Vector3.zero);
            }
            
            try
            {
                // 分离偏移量部分
                Vector3 offset = Vector3.zero;
                string mainPart = collisionInfo;
                
                int atIndex = collisionInfo.IndexOf('@');
                if (atIndex > 0)
                {
                    mainPart = collisionInfo.Substring(0, atIndex).Trim();
                    string offsetPart = collisionInfo.Substring(atIndex + 1).Trim();
                    offset = ParseOffset(offsetPart);
                }
                
                string[] parts = mainPart.Split(':');
                string shapeTypeStr = parts[0].Trim().ToLower();
                
                // 解析类型
                CollisionShapeType shapeType = shapeTypeStr switch
                {
                    "box" => CollisionShapeType.Box,
                    "sphere" => CollisionShapeType.Sphere,
                    "capsule" => CollisionShapeType.Capsule,
                    "point" => CollisionShapeType.Point,
                    _ => CollisionShapeType.None
                };
                
                if (shapeType == CollisionShapeType.None || shapeType == CollisionShapeType.Point)
                {
                    return (shapeType, (0f, 0f, 0f), offset);
                }
                
                // 解析参数
                if (parts.Length < 2)
                {
                    return (shapeType, (0f, 0f, 0f), offset);
                }
                
                string parametersStr = parts[1].Trim();
                
                if (shapeType == CollisionShapeType.Box)
                {
                    // 格式: 5x2x1
                    string[] sizeParts = parametersStr.Split('x', 'X', '×');
                    if (sizeParts.Length == 3)
                    {
                        float width = ParseFloat(sizeParts[0]);
                        float height = ParseFloat(sizeParts[1]);
                        float depth = ParseFloat(sizeParts[2]);
                        return (shapeType, (width, height, depth), offset);
                    }
                }
                else if (shapeType == CollisionShapeType.Sphere)
                {
                    // 格式: 3.0
                    float radius = ParseFloat(parametersStr);
                    return (shapeType, (radius, 0f, 0f), offset);
                }
                else if (shapeType == CollisionShapeType.Capsule)
                {
                    // 格式: 2x5
                    string[] capsuleParts = parametersStr.Split('x', 'X', '×');
                    if (capsuleParts.Length == 2)
                    {
                        float radius = ParseFloat(capsuleParts[0]);
                        float height = ParseFloat(capsuleParts[1]);
                        return (shapeType, (radius, height, 0f), offset);
                    }
                }
                
                return (shapeType, (0f, 0f, 0f), offset);
            }
            catch
            {
                return (CollisionShapeType.None, (0f, 0f, 0f), Vector3.zero);
            }
        }
        
        /// <summary>
        /// 解析偏移量字符串
        /// 格式：x,y,z 例如 "0,1,0"
        /// </summary>
        private static Vector3 ParseOffset(string offsetStr)
        {
            try
            {
                string[] parts = offsetStr.Split(',');
                if (parts.Length != 3)
                {
                    return Vector3.zero;
                }
                
                float x = ParseFloat(parts[0]);
                float y = ParseFloat(parts[1]);
                float z = ParseFloat(parts[2]);
                
                return new Vector3(x, y, z);
            }
            catch
            {
                return Vector3.zero;
            }
        }
        
        /// <summary>
        /// 解析浮点数
        /// </summary>
        private static float ParseFloat(string value)
        {
            if (float.TryParse(value.Trim(), out float result))
            {
                return result;
            }
            return 0f;
        }
        
        /// <summary>
        /// 格式化浮点数（去除不必要的小数位）
        /// </summary>
        private static string FormatFloat(float value)
        {
            // 如果是整数，不显示小数点
            if (Mathf.Approximately(value, Mathf.Round(value)))
            {
                return Mathf.RoundToInt(value).ToString();
            }
            
            // 否则保留1位小数
            return value.ToString("F1");
        }
    }
}

