using UnityEditor;
using UnityEngine;
using Astrum.Editor.RoleEditor.Timeline;
using Astrum.Editor.RoleEditor.Timeline.EventData;

namespace Astrum.Editor.RoleEditor.Timeline.Renderers
{
    /// <summary>
    /// 相机震动轨道渲染器
    /// </summary>
    public static class CameraShakeTrackRenderer
    {
        public static void RenderEvent(Rect rect, TimelineEvent evt)
        {
            var data = evt.GetEventData<CameraShakeEventData>();
            if (data == null)
            {
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                return;
            }
            
            // 绘制灰色区间条
            Color barColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            EditorGUI.DrawRect(rect, barColor);
            
            // 虚线边框
            Handles.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            DrawDashedBorder(rect);
            
            // 显示强度
            if (rect.width > 30)
            {
                GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
                labelStyle.normal.textColor = Color.white;
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.fontSize = 9;
                
                GUI.Label(rect, data.GetDisplayName(), labelStyle);
            }
        }
        
        public static bool EditEvent(TimelineEvent evt)
        {
            var data = evt.GetEventData<CameraShakeEventData>();
            if (data == null)
            {
                data = CameraShakeEventData.CreateDefault();
                evt.SetEventData(data);
            }
            
            EditorGUILayout.LabelField("相机震动事件编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 帧范围
            EditorGUILayout.BeginHorizontal();
            evt.StartFrame = EditorGUILayout.IntField("起始帧:", evt.StartFrame);
            evt.EndFrame = EditorGUILayout.IntField("结束帧:", evt.EndFrame);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 震动参数
            EditorGUILayout.LabelField("震动参数:", EditorStyles.boldLabel);
            data.Intensity = EditorGUILayout.Slider("强度:", data.Intensity, 0f, 1f);
            data.Frequency = EditorGUILayout.FloatField("频率(Hz):", data.Frequency);
            data.Direction = EditorGUILayout.Vector3Field("方向:", data.Direction);
            
            data.Note = EditorGUILayout.TextField("备注:", data.Note);
            
            // 更新显示名称
            evt.DisplayName = data.GetDisplayName();
            
            // 保存修改
            evt.SetEventData(data);
            
            return true;
        }
        
        private static void DrawDashedBorder(Rect rect)
        {
            // 简化：绘制虚线（实际应使用Handles.DrawDottedLine）
            Handles.DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y));
            Handles.DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax));
        }
    }
}
