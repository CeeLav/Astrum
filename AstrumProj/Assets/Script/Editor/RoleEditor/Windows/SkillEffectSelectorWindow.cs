using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Services;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Windows
{
    /// <summary>
    /// 技能效果选择器窗口
    /// </summary>
    public class SkillEffectSelectorWindow : EditorWindow
    {
        private const string LOG_PREFIX = "[SkillEffectSelector]";
        
        // === 数据 ===
        private List<SkillEffectTableData> _allEffects = new List<SkillEffectTableData>();
        private List<SkillEffectTableData> _filteredEffects = new List<SkillEffectTableData>();
        
        // === UI状态 ===
        private string _searchText = string.Empty;
        private int _filterTypeIndex = 0;
        private Vector2 _scrollPosition;
        private SkillEffectTableData _selectedEffect;
        
        // === 回调 ===
        private Action<int> _onEffectSelected;
        private static readonly string[] FilterKeys = { "", "Damage", "Heal", "Knockback", "Buff", "Debuff", "Status", "Teleport" };
        
        // === 静态方法 ===
        
        /// <summary>
        /// 显示选择器窗口
        /// </summary>
        public static void ShowWindow(Action<int> onEffectSelected)
        {
            var window = GetWindow<SkillEffectSelectorWindow>("选择技能效果");
            window.minSize = new Vector2(600, 400);
            window._onEffectSelected = onEffectSelected;
            window.LoadData();
            window.Show();
        }
        
        // === Unity生命周期 ===
        
        private void OnEnable()
        {
            LoadData();
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(5);
            DrawEffectList();
            EditorGUILayout.Space(5);
            DrawFooter();
        }
        
        // === 数据加载 ===
        
        private void LoadData()
        {
            try
            {
                // 读取所有技能效果配置
                _allEffects = SkillEffectDataReader.ReadAllSkillEffects();
                ApplyFilters();
                Debug.Log($"{LOG_PREFIX} 加载 {_allEffects.Count} 个技能效果");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} 加载失败: {ex.Message}");
                _allEffects = new List<SkillEffectTableData>();
                _filteredEffects = new List<SkillEffectTableData>();
            }
        }
        
        // === UI绘制 ===
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                // 搜索框
                GUILayout.Label("搜索:", GUILayout.Width(40));
                EditorGUI.BeginChangeCheck();
                _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(200));
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyFilters();
                }
                
                GUILayout.Space(10);
                
                // 类型筛选
                GUILayout.Label("类型:", GUILayout.Width(40));
                EditorGUI.BeginChangeCheck();
                string[] filterOptions = { "全部", "伤害", "治疗", "击退", "Buff", "Debuff", "状态", "瞬移" };
                _filterTypeIndex = GUILayout.SelectionGrid(_filterTypeIndex, filterOptions, filterOptions.Length, EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyFilters();
                }
                
                GUILayout.FlexibleSpace();
                
                // 统计信息
                GUILayout.Label($"共 {_filteredEffects.Count} 个效果", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawEffectList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                foreach (var effect in _filteredEffects)
                {
                    DrawEffectItem(effect);
                }
                
                if (_filteredEffects.Count == 0)
                {
                    EditorGUILayout.HelpBox("没有找到符合条件的技能效果", MessageType.Info);
                }
            }
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawEffectItem(SkillEffectTableData effect)
        {
            bool isSelected = _selectedEffect == effect;

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUIStyle idStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 14
                    };
                    idStyle.normal.textColor = GetEffectTypeColor(effect.EffectType);
                    EditorGUILayout.LabelField($"{GetEffectTypeIcon(effect.EffectType)} {effect.SkillEffectId}", idStyle, GUILayout.Width(110));

                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField(GenerateEffectName(effect), EditorStyles.boldLabel);

                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField($"类型: {GetEffectTypeDisplayName(effect.EffectType)}", EditorStyles.miniLabel, GUILayout.Width(150));
                            string primaryValue = GetPrimaryValueDescription(effect);
                            if (!string.IsNullOrEmpty(primaryValue))
                            {
                                EditorGUILayout.LabelField($"主值: {primaryValue}", EditorStyles.miniLabel, GUILayout.Width(160));
                            }
                            EditorGUILayout.LabelField($"目标: {GetTargetTypeName(effect)}", EditorStyles.miniLabel, GUILayout.Width(140));
                        }
                        EditorGUILayout.EndHorizontal();

                        string paramText = FormatStringParams(effect.StringParams);
                        if (!string.IsNullOrEmpty(paramText))
                        {
                            EditorGUILayout.LabelField($"参数: {paramText}", EditorStyles.miniLabel);
                        }
                    }
                    EditorGUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("选择", GUILayout.Width(60), GUILayout.Height(40)))
                    {
                        SelectEffect(effect);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                if (isSelected)
                {
                    Color bgColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
                    EditorGUI.DrawRect(rect, bgColor);
                }
            }

            if (Event.current.type == EventType.MouseDown)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                if (rect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.clickCount == 2)
                    {
                        SelectEffect(effect);
                        Event.current.Use();
                    }
                    else
                    {
                        _selectedEffect = effect;
                        Event.current.Use();
                        Repaint();
                    }
                }
            }

            EditorGUILayout.Space(2);
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.HelpBox("提示：双击或点击\"选择\"按钮确认选择", MessageType.None);
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("取消", GUILayout.Width(80), GUILayout.Height(25)))
                {
                    Close();
                }
                
                EditorGUI.BeginDisabledGroup(_selectedEffect == null);
                if (GUILayout.Button("确定", GUILayout.Width(80), GUILayout.Height(25)))
                {
                    if (_selectedEffect != null)
                    {
                        SelectEffect(_selectedEffect);
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        // === 逻辑方法 ===
        
        private void ApplyFilters()
        {
            _filteredEffects = _allEffects.Where(effect =>
            {
                if (!string.IsNullOrWhiteSpace(_searchText))
                {
                    string search = _searchText.ToLower();
                    string effectId = effect.SkillEffectId.ToString();
                    string effectType = effect.EffectType?.ToLower() ?? string.Empty;
                    string intParams = string.Join("|", effect.IntParams ?? new List<int>()).ToLower();
                    string strParams = string.Join("|", effect.StringParams ?? new List<string>()).ToLower();

                    if (!effectId.Contains(search) &&
                        !effectType.Contains(search) &&
                        !intParams.Contains(search) &&
                        !strParams.Contains(search))
                    {
                        return false;
                    }
                }

                string filterKey = _filterTypeIndex >= 0 && _filterTypeIndex < FilterKeys.Length ? FilterKeys[_filterTypeIndex] : string.Empty;
                if (!string.IsNullOrEmpty(filterKey) && !string.Equals(effect.EffectType, filterKey, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            }).ToList();
        }
        
        private void SelectEffect(SkillEffectTableData effect)
        {
            _onEffectSelected?.Invoke(effect.SkillEffectId);
            Close();
        }
        
        // === 辅助方法 ===
        
        private string GenerateEffectName(SkillEffectTableData config)
        {
            string typeName = GetEffectTypeDisplayName(config.EffectType);
            string primary = GetPrimaryValueDescription(config);
            return string.IsNullOrEmpty(primary)
                ? $"{typeName}_{config.SkillEffectId}"
                : $"{typeName} {primary}";
        }
        
        private string GetEffectTypeDisplayName(string effectType)
        {
            return SkillEffectDataReader.GetEffectTypeDisplayName(effectType);
        }
        
        private string GetEffectTypeIcon(string effectType)
        {
            return SkillEffectDataReader.GetEffectTypeIcon(effectType);
        }
        
        private Color GetEffectTypeColor(string effectType)
        {
            switch ((effectType ?? string.Empty).ToLower())
            {
                case "damage":
                    return new Color(1f, 0.3f, 0.3f);
                case "heal":
                    return new Color(0.3f, 1f, 0.3f);
                case "knockback":
                    return new Color(1f, 0.7f, 0.2f);
                case "buff":
                    return new Color(0.5f, 0.8f, 1f);
                case "debuff":
                    return new Color(0.8f, 0.3f, 1f);
                case "status":
                    return new Color(0.9f, 0.6f, 0.2f);
                case "teleport":
                    return new Color(0.4f, 0.9f, 0.9f);
                default:
                    return Color.gray;
            }
        }
        
        private string GetTargetTypeName(SkillEffectTableData config)
        {
            if (config.IntParams == null || config.IntParams.Count == 0)
                return "未知";
            return config.IntParams[0] switch
            {
                0 => "自身",
                1 => "敌人",
                2 => "友军",
                3 => "区域",
                _ => "未知"
            };
        }
        
        private string GetPrimaryValueDescription(SkillEffectTableData config)
        {
            var ints = config.IntParams ?? new List<int>();
            switch ((config.EffectType ?? string.Empty).ToLower())
            {
                case "damage":
                case "heal":
                    if (ints.Count > 2)
                    {
                        float percent = ints[2] / 10f;
                        return $"{percent:0.#}%";
                    }
                    break;
                case "knockback":
                    if (ints.Count > 1)
                    {
                        float meters = ints[1] / 1000f;
                        return $"{meters:0.##}m";
                    }
                    break;
                case "status":
                    if (ints.Count > 2)
                    {
                        float seconds = ints[2] / 1000f;
                        return $"{seconds:0.##}s";
                    }
                    break;
                case "teleport":
                    if (ints.Count > 1)
                    {
                        float meters = ints[1] / 1000f;
                        return $"{meters:0.##}m";
                    }
                    break;
            }
            return string.Empty;
        }
        
        private string FormatStringParams(List<string> stringParams)
        {
            if (stringParams == null || stringParams.Count == 0)
                return string.Empty;
            return string.Join(" | ", stringParams);
        }
    }
}

