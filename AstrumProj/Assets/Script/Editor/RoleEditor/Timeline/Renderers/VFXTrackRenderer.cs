using UnityEditor;
using UnityEngine;
using Astrum.Editor.RoleEditor.Timeline;
using Astrum.Editor.RoleEditor.Timeline.EventData;
using System.IO;

namespace Astrum.Editor.RoleEditor.Timeline.Renderers
{
    /// <summary>
    /// 特效轨道渲染器
    /// </summary>
    public static class VFXTrackRenderer
    {
        public static void RenderEvent(Rect rect, TimelineEvent evt)
        {
            var data = evt.GetEventData<VFXEventData>();
            if (data == null)
            {
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                return;
            }
            
            // 绘制区间条（紫色渐变）
            Color barColor = new Color(0.8f, 0.4f, 1f, 0.7f);
            EditorGUI.DrawRect(rect, barColor);
            
            // 边框
            Handles.color = new Color(0.8f, 0.4f, 1f, 1f);
            DrawBorder(rect);
            
            // 显示特效名称
            if (rect.width > 50)
            {
                GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
                labelStyle.normal.textColor = Color.white;
                labelStyle.alignment = TextAnchor.MiddleLeft;
                labelStyle.fontSize = 9;
                
                GUI.Label(new Rect(rect.x + 3, rect.y, rect.width - 6, rect.height), data.GetDisplayName(), labelStyle);
            }
        }
        
        public static bool EditEvent(TimelineEvent evt)
        {
            var data = evt.GetEventData<VFXEventData>();
            if (data == null)
            {
                data = VFXEventData.CreateDefault();
                evt.SetEventData(data);
            }
            
            bool modified = false;
            
            EditorGUILayout.LabelField("特效事件编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 帧范围
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("起始帧:", GUILayout.Width(60));
            EditorGUI.BeginChangeCheck();
            evt.StartFrame = EditorGUILayout.IntField(evt.StartFrame, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck()) modified = true;
            GUILayout.Space(10);
            EditorGUILayout.LabelField("结束帧:", GUILayout.Width(60));
            EditorGUI.BeginChangeCheck();
            evt.EndFrame = EditorGUILayout.IntField(evt.EndFrame, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck()) modified = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 特效资源（支持拖动设置）
            EditorGUILayout.LabelField("特效资源", EditorStyles.boldLabel);
            
            // 从路径加载当前资源
            GameObject currentVFX = null;
            if (!string.IsNullOrEmpty(data.ResourcePath))
            {
                currentVFX = AssetDatabase.LoadAssetAtPath<GameObject>(data.ResourcePath);
            }
            
            // ObjectField：支持拖动设置
            EditorGUI.BeginChangeCheck();
            GameObject newVFX = EditorGUILayout.ObjectField("特效Prefab", currentVFX, typeof(GameObject), false) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                if (newVFX != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(newVFX);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        data.ResourcePath = assetPath;
                        modified = true;
                    }
                }
                else
                {
                    data.ResourcePath = "";
                    modified = true;
                }
            }
            
            // 文本字段：支持手动输入路径
            EditorGUI.BeginChangeCheck();
            string newPath = EditorGUILayout.TextField("特效路径", data.ResourcePath);
            if (EditorGUI.EndChangeCheck())
            {
                data.ResourcePath = newPath;
                modified = true;
            }
            
            EditorGUILayout.Space(5);
            
            // 变换参数
            EditorGUILayout.LabelField("变换参数:", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            data.PositionOffset = EditorGUILayout.Vector3Field("位置偏移:", data.PositionOffset);
            if (EditorGUI.EndChangeCheck()) modified = true;
            
            EditorGUI.BeginChangeCheck();
            data.Rotation = EditorGUILayout.Vector3Field("旋转:", data.Rotation);
            if (EditorGUI.EndChangeCheck()) modified = true;
            
            EditorGUI.BeginChangeCheck();
            data.Scale = EditorGUILayout.FloatField("缩放:", data.Scale);
            if (EditorGUI.EndChangeCheck()) modified = true;
            
            EditorGUILayout.Space(5);
            
            // 播放参数
            EditorGUILayout.LabelField("播放参数:", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            data.PlaybackSpeed = EditorGUILayout.Slider("播放速度:", data.PlaybackSpeed, 0.1f, 3f);
            if (EditorGUI.EndChangeCheck()) modified = true;
            
            EditorGUI.BeginChangeCheck();
            data.FollowCharacter = EditorGUILayout.Toggle("跟随角色:", data.FollowCharacter);
            if (EditorGUI.EndChangeCheck()) modified = true;
            
            EditorGUI.BeginChangeCheck();
            data.Loop = EditorGUILayout.Toggle("循环播放:", data.Loop);
            if (EditorGUI.EndChangeCheck()) modified = true;
            
            EditorGUI.BeginChangeCheck();
            data.Note = EditorGUILayout.TextField("备注:", data.Note);
            if (EditorGUI.EndChangeCheck()) modified = true;
            
            // 如果修改了，更新显示名称并保存
            if (modified)
            {
                // 更新显示名称
                evt.DisplayName = data.GetDisplayName();
                
                // 保存修改
                evt.SetEventData(data);
            }
            
            return modified;
        }
        
        private static void DrawBorder(Rect rect)
        {
            Handles.DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y));
            Handles.DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax));
            Handles.DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.x, rect.yMax));
            Handles.DrawLine(new Vector2(rect.xMax, rect.y), new Vector2(rect.xMax, rect.yMax));
        }
    }
}
