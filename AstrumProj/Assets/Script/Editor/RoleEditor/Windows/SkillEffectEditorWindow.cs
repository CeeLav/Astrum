using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using Astrum.Editor.RoleEditor.Services;
using Astrum.Editor.RoleEditor.SkillEffectEditors;
using UnityEditor;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Windows
{
    public class SkillEffectEditorWindow : EditorWindow
    {
        private SkillEffectTableData _original;
        private SkillEffectTableData _workingCopy;
        private ISkillEffectEditorPanel _panel;
        private Action _onSaved;
        private bool _dirty;
        private string _statusMessage = string.Empty;
        private MessageType _statusType = MessageType.Info;

        private static readonly string[] KnownTypes = { "Damage", "Heal", "Knockback", "Status", "Teleport" };

        public static void ShowWindow(int effectId, Action onSaved = null)
        {
            var window = GetWindow<SkillEffectEditorWindow>("技能效果编辑器");
            window.minSize = new Vector2(420, 480);
            window.LoadEffect(effectId);
            window._onSaved = onSaved;
            window.Show();
            window.Focus();
        }

        private void LoadEffect(int effectId)
        {
            _original = SkillEffectDataReader.GetSkillEffect(effectId)?.Clone();
            if (_original == null)
            {
                _original = new SkillEffectTableData
                {
                    SkillEffectId = effectId,
                    EffectType = KnownTypes[0],
                    IntParams = new List<int>(),
                    StringParams = new List<string>()
                };
            }

            _workingCopy = _original.Clone();
            ResolvePanel();
            _dirty = false;
            _statusMessage = string.Empty;
        }

        private void ResolvePanel()
        {
            _panel = SkillEffectEditorRegistry.GetPanel(_workingCopy.EffectType);
        }

        private void OnGUI()
        {
            if (_workingCopy == null)
            {
                EditorGUILayout.HelpBox("未能加载技能效果数据。", MessageType.Error);
                return;
            }

            EditorGUILayout.Space();
            DrawHeader();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("效果 ID", _workingCopy.SkillEffectId.ToString());

                string[] typeOptions = GetTypeOptions();
                int selected = Array.IndexOf(typeOptions, _workingCopy.EffectType);
                if (selected < 0) selected = 0;
                EditorGUI.BeginChangeCheck();
                selected = EditorGUILayout.Popup("效果类型", selected, typeOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    _workingCopy.EffectType = typeOptions[selected];
                    ResolvePanel();
                    _dirty = true;
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            {
                if (_panel != null)
                {
                    EditorGUI.BeginChangeCheck();
                    bool panelChanged = _panel.DrawContent(_workingCopy);
                    if (EditorGUI.EndChangeCheck() || panelChanged)
                    {
                        _dirty = true;
                    }
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            DrawStringParamsEditor();

            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }

            GUILayout.FlexibleSpace();

            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"编辑技能效果 #{_workingCopy.SkillEffectId}", EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新", GUILayout.Width(70)))
            {
                LoadEffect(_workingCopy.SkillEffectId);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStringParamsEditor()
        {
            if (_workingCopy.StringParams == null)
            {
                _workingCopy.StringParams = new List<string>();
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("字符串参数", EditorStyles.boldLabel);

            for (int i = 0; i < _workingCopy.StringParams.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                string value = EditorGUILayout.TextField($"参数 {i}", _workingCopy.StringParams[i] ?? string.Empty);
                if (EditorGUI.EndChangeCheck())
                {
                    _workingCopy.StringParams[i] = value;
                    _dirty = true;
                }

                if (GUILayout.Button("✖", GUILayout.Width(24)))
                {
                    _workingCopy.StringParams.RemoveAt(i);
                    _dirty = true;
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("添加字符串参数"))
            {
                _workingCopy.StringParams.Add(string.Empty);
                _dirty = true;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("关闭", GUILayout.Height(28)))
            {
                Close();
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(!_dirty))
            {
                if (GUILayout.Button("重置", GUILayout.Width(80), GUILayout.Height(28)))
                {
                    _workingCopy = _original.Clone();
                    ResolvePanel();
                    _dirty = false;
                    _statusMessage = "已恢复为原始数据";
                    _statusType = MessageType.Info;
                }

                if (GUILayout.Button("保存", GUILayout.Width(120), GUILayout.Height(28)))
                {
                    SaveEffect();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void SaveEffect()
        {
            if (!SkillEffectDataWriter.SaveEffect(_workingCopy.Clone()))
            {
                _statusMessage = "保存失败，请查看控制台日志";
                _statusType = MessageType.Error;
                return;
            }

            _original = _workingCopy.Clone();
            _dirty = false;
            _statusMessage = "已保存";
            _statusType = MessageType.Info;

            _onSaved?.Invoke();
        }

        private string[] GetTypeOptions()
        {
            var known = SkillEffectEditorRegistry.GetKnownEffectTypes();
            var list = new List<string>(known);
            if (!list.Contains(_workingCopy.EffectType))
            {
                list.Add(_workingCopy.EffectType);
            }
            return list.ToArray();
        }
    }
}

