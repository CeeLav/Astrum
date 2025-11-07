using UnityEditor;
using UnityEngine;
using Astrum.Editor.RoleEditor.Timeline;
using Astrum.Editor.RoleEditor.Timeline.EventData;

namespace Astrum.Editor.RoleEditor.Timeline.Renderers
{
    /// <summary>
    /// 被取消标签轨道渲染器
    /// </summary>
    public static class BeCancelTagTrackRenderer
    {
        /// <summary>
        /// 渲染事件
        /// </summary>
        public static void RenderEvent(Rect rect, TimelineEvent evt)
        {
            var data = evt.GetEventData<BeCancelTagEventData>();
            if (data == null)
            {
                DrawDefaultBar(rect);
                return;
            }
            
            // 绘制区间条
            Color barColor = new Color(0.8f, 0.3f, 0.3f, 0.7f);
            EditorGUI.DrawRect(rect, barColor);
            
            // 边框
            Handles.color = new Color(0.8f, 0.3f, 0.3f, 1f);
            Handles.DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y));
            Handles.DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax));
            
            // 显示标签文本
            if (rect.width > 60)
            {
                GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
                labelStyle.normal.textColor = Color.white;
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.fontSize = 9;
                
                string displayText = data.GetDisplayName();
                GUI.Label(rect, displayText, labelStyle);
            }
        }
        
        /// <summary>
        /// 编辑事件
        /// </summary>
        public static bool EditEvent(TimelineEvent evt)
        {
            var data = evt.GetEventData<BeCancelTagEventData>();
            if (data == null)
            {
                data = BeCancelTagEventData.CreateDefault();
                evt.SetEventData(data);
            }
            
            bool modified = false;
            
            EditorGUILayout.LabelField("被取消标签编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 帧范围
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("起始帧:", GUILayout.Width(60));
            EditorGUI.BeginChangeCheck();
            int newStart = EditorGUILayout.IntField(evt.StartFrame, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck())
            {
                if (newStart != evt.StartFrame)
                {
                    evt.StartFrame = Mathf.Max(0, newStart);
                    modified = true;
                }
            }
            
            GUILayout.Space(10);
            EditorGUILayout.LabelField("结束帧:", GUILayout.Width(60));
            EditorGUI.BeginChangeCheck();
            int newEnd = EditorGUILayout.IntField(evt.EndFrame, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck())
            {
                if (newEnd != evt.EndFrame)
                {
                    evt.EndFrame = Mathf.Max(evt.StartFrame, newEnd);
                    modified = true;
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 标签列表
            EditorGUILayout.LabelField("标签列表:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            {
                for (int i = 0; i < data.Tags.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    string newTag = EditorGUILayout.TextField(data.Tags[i]);
                    if (EditorGUI.EndChangeCheck() && newTag != data.Tags[i])
                    {
                        data.Tags[i] = newTag;
                        modified = true;
                    }
                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        data.Tags.RemoveAt(i);
                        modified = true;
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                if (GUILayout.Button("+ 添加标签"))
                {
                    data.Tags.Add("new_tag");
                    modified = true;
                }
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // 其他参数
            EditorGUI.BeginChangeCheck();
            int newBlend = EditorGUILayout.IntField("融合时间(帧):", data.BlendOutFrames);
            if (EditorGUI.EndChangeCheck())
            {
                if (newBlend != data.BlendOutFrames)
                {
                    data.BlendOutFrames = Mathf.Max(0, newBlend);
                    modified = true;
                }
            }
            EditorGUI.BeginChangeCheck();
            int newPriority = EditorGUILayout.IntField("优先级:", data.Priority);
            if (EditorGUI.EndChangeCheck())
            {
                if (newPriority != data.Priority)
                {
                    data.Priority = newPriority;
                    modified = true;
                }
            }
            EditorGUI.BeginChangeCheck();
            string newNote = EditorGUILayout.TextField("备注:", data.Note);
            if (EditorGUI.EndChangeCheck() && newNote != data.Note)
            {
                data.Note = newNote;
                modified = true;
            }
            
            if (modified)
            {
                evt.DisplayName = data.GetDisplayName();
                evt.SetEventData(data);
            }
            
            return modified;
        }
        
        private static void DrawDefaultBar(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
    }
}
