using UnityEditor;
using UnityEngine;
using Astrum.Editor.RoleEditor.Timeline;
using Astrum.Editor.RoleEditor.Timeline.EventData;

namespace Astrum.Editor.RoleEditor.Timeline.Renderers
{
    /// <summary>
    /// 临时被取消标签轨道渲染器
    /// </summary>
    public static class TempBeCancelTagTrackRenderer
    {
        public static void RenderEvent(Rect rect, TimelineEvent evt)
        {
            var data = evt.GetEventData<TempBeCancelTagEventData>();
            if (data == null)
            {
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                return;
            }
            
            // 绘制区间条
            Color barColor = new Color(0.8f, 0.6f, 0.3f, 0.7f);
            EditorGUI.DrawRect(rect, barColor);
            
            // 边框
            Handles.color = new Color(0.8f, 0.6f, 0.3f, 1f);
            Handles.DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y));
            Handles.DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax));
            
            // 显示ID和标签
            if (rect.width > 40)
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
            var data = evt.GetEventData<TempBeCancelTagEventData>();
            if (data == null)
            {
                data = TempBeCancelTagEventData.CreateDefault();
                evt.SetEventData(data);
            }
            
            EditorGUILayout.LabelField("临时取消标签编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 帧范围
            EditorGUILayout.BeginHorizontal();
            evt.StartFrame = EditorGUILayout.IntField("起始帧:", evt.StartFrame);
            evt.EndFrame = EditorGUILayout.IntField("结束帧:", evt.EndFrame);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 临时标签ID
            data.Id = EditorGUILayout.TextField("标签ID:", data.Id);
            
            // 标签列表
            EditorGUILayout.LabelField("标签列表:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            {
                for (int i = 0; i < data.Tags.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    data.Tags[i] = EditorGUILayout.TextField(data.Tags[i]);
                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        data.Tags.RemoveAt(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                if (GUILayout.Button("+ 添加标签"))
                {
                    data.Tags.Add("new_tag");
                }
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // 其他参数
            data.DurationFrames = EditorGUILayout.IntField("持续时间(帧):", data.DurationFrames);
            data.BlendOutFrames = EditorGUILayout.IntField("融合时间(帧):", data.BlendOutFrames);
            data.Priority = EditorGUILayout.IntField("优先级:", data.Priority);
            data.Note = EditorGUILayout.TextField("备注:", data.Note);
            
            // 更新显示名称和结束帧
            evt.DisplayName = data.GetDisplayName();
            evt.EndFrame = evt.StartFrame + data.DurationFrames - 1;
            
            // 保存修改
            evt.SetEventData(data);
            
            return true;
        }
    }
}
