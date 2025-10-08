using UnityEditor;
using UnityEngine;
using Astrum.Editor.RoleEditor.Timeline;
using Astrum.Editor.RoleEditor.Timeline.EventData;

namespace Astrum.Editor.RoleEditor.Timeline.Renderers
{
    /// <summary>
    /// 音效轨道渲染器
    /// </summary>
    public static class SFXTrackRenderer
    {
        public static void RenderEvent(Rect rect, TimelineEvent evt)
        {
            var data = evt.GetEventData<SFXEventData>();
            if (data == null)
            {
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                return;
            }
            
            if (evt.IsSingleFrame())
            {
                // 单帧：绘制音符标记
                DrawSoundMarker(rect);
            }
            else
            {
                // 区间：绘制橙色条
                Color barColor = new Color(1f, 0.7f, 0.2f, 0.7f);
                EditorGUI.DrawRect(rect, barColor);
                
                Handles.color = new Color(1f, 0.7f, 0.2f, 1f);
                DrawBorder(rect);
                
                // 显示音效名称
                if (rect.width > 40)
                {
                    GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
                    labelStyle.normal.textColor = Color.white;
                    labelStyle.alignment = TextAnchor.MiddleCenter;
                    labelStyle.fontSize = 9;
                    
                    GUI.Label(rect, data.GetDisplayName(), labelStyle);
                }
            }
        }
        
        public static bool EditEvent(TimelineEvent evt)
        {
            var data = evt.GetEventData<SFXEventData>();
            if (data == null)
            {
                data = SFXEventData.CreateDefault();
                evt.SetEventData(data);
            }
            
            EditorGUILayout.LabelField("音效事件编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 触发帧
            if (evt.IsSingleFrame())
            {
                evt.StartFrame = EditorGUILayout.IntField("触发帧:", evt.StartFrame);
                evt.EndFrame = evt.StartFrame;
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                evt.StartFrame = EditorGUILayout.IntField("起始帧:", evt.StartFrame);
                evt.EndFrame = EditorGUILayout.IntField("结束帧:", evt.EndFrame);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
            
            // 音效资源
            data.ResourcePath = EditorGUILayout.TextField("音效路径:", data.ResourcePath);
            
            EditorGUILayout.Space(5);
            
            // 音频参数
            EditorGUILayout.LabelField("音频参数:", EditorStyles.boldLabel);
            data.Volume = EditorGUILayout.Slider("音量:", data.Volume, 0f, 1f);
            data.Pitch = EditorGUILayout.Slider("音调:", data.Pitch, 0.5f, 2f);
            data.SpatialBlend = EditorGUILayout.Slider("空间混音 (2D→3D):", data.SpatialBlend, 0f, 1f);
            
            EditorGUILayout.Space(5);
            
            // 播放参数
            EditorGUILayout.LabelField("播放参数:", EditorStyles.boldLabel);
            data.Loop = EditorGUILayout.Toggle("循环播放:", data.Loop);
            data.FollowCharacter = EditorGUILayout.Toggle("跟随角色:", data.FollowCharacter);
            data.MaxDistance = EditorGUILayout.FloatField("最大距离:", data.MaxDistance);
            
            data.Note = EditorGUILayout.TextField("备注:", data.Note);
            
            // 更新显示名称
            evt.DisplayName = data.GetDisplayName();
            
            // 保存修改
            evt.SetEventData(data);
            
            return true;
        }
        
        private static void DrawSoundMarker(Rect rect)
        {
            // 绘制音符图标
            Vector2 center = rect.center;
            float size = Mathf.Min(rect.width, rect.height) * 0.6f;
            
            Rect iconRect = new Rect(center.x - size / 2, center.y - size / 2, size, size);
            
            Color iconColor = new Color(1f, 0.7f, 0.2f, 0.9f);
            EditorGUI.DrawRect(iconRect, iconColor);
            
            // 简单的圆形表示音符
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.normal.textColor = Color.white;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = 14;
            
            GUI.Label(iconRect, "♪", labelStyle);
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
