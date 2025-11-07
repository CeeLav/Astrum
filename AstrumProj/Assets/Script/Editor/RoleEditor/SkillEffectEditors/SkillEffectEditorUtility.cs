using System.Collections.Generic;
using UnityEditor;

namespace Astrum.Editor.RoleEditor.SkillEffectEditors
{
    internal static class SkillEffectEditorUtility
    {
        public static void EnsureListSize(List<int> list, int size)
        {
            if (list == null)
                return;

            while (list.Count < size)
            {
                list.Add(0);
            }
        }

        public static void EnsureListSize(List<string> list, int size)
        {
            if (list == null)
                return;

            while (list.Count < size)
            {
                list.Add(string.Empty);
            }
        }

        public static bool DrawIntField(string label, List<int> list, int index, int newDefault = 0)
        {
            if (list == null || index < 0)
                return false;

            EnsureListSize(list, index + 1);
            int original = list[index];
            int value = EditorGUILayout.IntField(label, original);
            if (value != original)
            {
                list[index] = value;
                return true;
            }

            return false;
        }

        public static bool DrawPopup(string label, List<int> list, int index, string[] displayOptions, int[] values)
        {
            if (list == null || index < 0)
                return false;

            EnsureListSize(list, index + 1);
            int currentValue = list[index];
            int selectedIndex = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == currentValue)
                {
                    selectedIndex = i;
                    break;
                }
            }

            int newIndex = EditorGUILayout.Popup(label, selectedIndex, displayOptions);
            int newValue = values[newIndex];
            if (newValue != currentValue)
            {
                list[index] = newValue;
                return true;
            }

            return false;
        }
    }
}

