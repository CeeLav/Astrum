using UnityEditor;
using UnityEngine;
using Astrum.Editor.RoleEditor.Timeline;
using Astrum.Editor.RoleEditor.Timeline.EventData;

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
            
            EditorGUILayout.LabelField("特效事件编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 帧范围
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("起始帧:", GUILayout.Width(60));
            evt.StartFrame = EditorGUILayout.IntField(evt.StartFrame, GUILayout.Width(60));
            GUILayout.Space(10);
            EditorGUILayout.LabelField("结束帧:", GUILayout.Width(60));
            evt.EndFrame = EditorGUILayout.IntField(evt.EndFrame, GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 特效资源
            data.ResourcePath = EditorGUILayout.TextField("特效路径:", data.ResourcePath);
            
            // 变换参数
            EditorGUILayout.LabelField("变换参数:", EditorStyles.boldLabel);
            data.PositionOffset = EditorGUILayout.Vector3Field("位置偏移:", data.PositionOffset);
            data.Rotation = EditorGUILayout.Vector3Field("旋转:", data.Rotation);
            data.Scale = EditorGUILayout.FloatField("缩放:", data.Scale);
            
            EditorGUILayout.Space(5);
            
            // 播放参数
            EditorGUILayout.LabelField("播放参数:", EditorStyles.boldLabel);
            data.PlaybackSpeed = EditorGUILayout.Slider("播放速度:", data.PlaybackSpeed, 0.1f, 3f);
            data.FollowCharacter = EditorGUILayout.Toggle("跟随角色:", data.FollowCharacter);
            data.Loop = EditorGUILayout.Toggle("循环播放:", data.Loop);
            
            data.Note = EditorGUILayout.TextField("备注:", data.Note);
            
            // 更新显示名称
            evt.DisplayName = data.GetDisplayName();
            
            // 保存修改
            evt.SetEventData(data);
            
            return true;
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
