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
        private string _searchText = "";
        private int _filterType = 0; // 0=全部, 1=伤害, 2=治疗, 3=击退, 4=Buff, 5=Debuff
        private Vector2 _scrollPosition;
        private SkillEffectTableData _selectedEffect;
        
        // === 回调 ===
        private Action<int> _onEffectSelected;
        
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
                string[] filterOptions = { "全部", "伤害", "治疗", "击退", "Buff", "Debuff" };
                _filterType = GUILayout.SelectionGrid(_filterType, filterOptions, 6, EditorStyles.toolbarButton);
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
            
            // 开始垂直布局
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    // ID和图标
                    GUIStyle idStyle = new GUIStyle(EditorStyles.boldLabel);
                    idStyle.fontSize = 14;
                    idStyle.normal.textColor = GetEffectTypeColor(effect.EffectType);
                    EditorGUILayout.LabelField($"{GetEffectTypeIcon(effect.EffectType)} {effect.SkillEffectId}", idStyle, GUILayout.Width(80));
                    
                    // 效果信息
                    EditorGUILayout.BeginVertical();
                    {
                        // 名称
                        string name = GenerateEffectName(effect);
                        EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
                        
                        // 详细信息
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField($"类型: {GetEffectTypeName(effect.EffectType)}", EditorStyles.miniLabel, GUILayout.Width(120));
                            EditorGUILayout.LabelField($"数值: {effect.EffectValue}", EditorStyles.miniLabel, GUILayout.Width(120));
                            EditorGUILayout.LabelField($"范围: {effect.EffectRange}m", EditorStyles.miniLabel, GUILayout.Width(120));
                            EditorGUILayout.LabelField($"目标: {GetTargetTypeName(effect.TargetType)}", EditorStyles.miniLabel, GUILayout.Width(100));
                            
                            if (effect.EffectDuration > 0)
                            {
                                EditorGUILayout.LabelField($"持续: {effect.EffectDuration}s", EditorStyles.miniLabel);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        // 参数
                        if (!string.IsNullOrEmpty(effect.EffectParams))
                        {
                            EditorGUILayout.LabelField($"参数: {effect.EffectParams}", EditorStyles.miniLabel);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    
                    GUILayout.FlexibleSpace();
                    
                    // 选择按钮
                    if (GUILayout.Button("选择", GUILayout.Width(60), GUILayout.Height(40)))
                    {
                        SelectEffect(effect);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            
            // 在内容绘制完成后获取Rect并绘制背景/处理交互
            if (Event.current.type == EventType.Repaint)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                
                // 绘制选中背景（在底层）
                if (isSelected)
                {
                    Color bgColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
                    EditorGUI.DrawRect(rect, bgColor);
                }
            }
            
            // 处理鼠标交互（在Layout/MouseDown事件中）
            if (Event.current.type == EventType.MouseDown)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                
                if (rect.Contains(Event.current.mousePosition))
                {
                    // 双击确认
                    if (Event.current.clickCount == 2)
                    {
                        SelectEffect(effect);
                        Event.current.Use();
                    }
                    // 单击选中
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
                // 搜索筛选
                if (!string.IsNullOrWhiteSpace(_searchText))
                {
                    string search = _searchText.ToLower();
                    string effectId = effect.SkillEffectId.ToString();
                    string effectValue = effect.EffectValue.ToString();
                    string effectParams = effect.EffectParams?.ToLower() ?? "";
                    
                    if (!effectId.Contains(search) && 
                        !effectValue.Contains(search) && 
                        !effectParams.Contains(search))
                    {
                        return false;
                    }
                }
                
                // 类型筛选
                if (_filterType > 0 && effect.EffectType != _filterType)
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
            string typeName = config.EffectType switch
            {
                1 => "伤害",
                2 => "治疗",
                3 => "击退",
                4 => "Buff",
                5 => "Debuff",
                _ => "效果"
            };
            
            if (config.EffectValue > 0)
            {
                return $"{typeName} {config.EffectValue}";
            }
            
            return $"{typeName}_{config.SkillEffectId}";
        }
        
        private string GetEffectTypeName(int type)
        {
            return type switch
            {
                1 => "伤害",
                2 => "治疗",
                3 => "击退",
                4 => "Buff",
                5 => "Debuff",
                _ => "未知"
            };
        }
        
        private string GetEffectTypeIcon(int type)
        {
            return type switch
            {
                1 => "⚔️",  // 伤害
                2 => "💚",  // 治疗
                3 => "💥",  // 击退
                4 => "✨",  // Buff
                5 => "🔥",  // Debuff
                _ => "❓"
            };
        }
        
        private Color GetEffectTypeColor(int type)
        {
            return type switch
            {
                1 => new Color(1f, 0.3f, 0.3f),     // 红色 - 伤害
                2 => new Color(0.3f, 1f, 0.3f),     // 绿色 - 治疗
                3 => new Color(1f, 0.7f, 0.2f),     // 橙色 - 击退
                4 => new Color(0.5f, 0.8f, 1f),     // 蓝色 - Buff
                5 => new Color(0.8f, 0.3f, 1f),     // 紫色 - Debuff
                _ => Color.gray
            };
        }
        
        private string GetTargetTypeName(int type)
        {
            return type switch
            {
                1 => "自身",
                2 => "敌人",
                3 => "友方",
                4 => "区域",
                _ => "未知"
            };
        }
    }
}

