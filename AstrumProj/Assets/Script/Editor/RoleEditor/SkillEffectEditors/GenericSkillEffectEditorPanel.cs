using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using UnityEditor;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.SkillEffectEditors
{
    internal class GenericSkillEffectEditorPanel : ISkillEffectEditorPanel
    {
        public string EffectType => "Generic";
        public bool SupportsInlineEditing => false;

        public bool DrawContent(SkillEffectTableData data)
        {
            bool changed = false;

            EditorGUILayout.HelpBox("该效果类型未定义专用编辑器，以下为原始参数列表。", MessageType.Info);

            changed |= DrawIntParams(data.IntParams);
            changed |= DrawStringParams(data.StringParams);

            return changed;
        }

        private bool DrawIntParams(List<int> list)
        {
            bool changed = false;

            if (list == null)
                return false;

            EditorGUILayout.LabelField("整数参数", EditorStyles.boldLabel);

            for (int i = 0; i < list.Count; i++)
            {
                int value = EditorGUILayout.IntField($"Param[{i}]", list[i]);
                if (value != list[i])
                {
                    list[i] = value;
                    changed = true;
                }
            }

            if (GUILayout.Button("添加整数参数"))
            {
                list.Add(0);
                changed = true;
            }

            if (list.Count > 0 && GUILayout.Button("移除末尾整数参数"))
            {
                list.RemoveAt(list.Count - 1);
                changed = true;
            }

            return changed;
        }

        private bool DrawStringParams(List<string> list)
        {
            bool changed = false;

            if (list == null)
                return false;

            EditorGUILayout.LabelField("字符串参数", EditorStyles.boldLabel);

            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                string value = EditorGUILayout.TextField($"Param[{i}]", list[i] ?? string.Empty);
                if (value != list[i])
                {
                    list[i] = value;
                    changed = true;
                }

                if (GUILayout.Button("✖", GUILayout.Width(24)))
                {
                    list.RemoveAt(i);
                    changed = true;
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("添加字符串参数"))
            {
                list.Add(string.Empty);
                changed = true;
            }

            return changed;
        }
    }
}

