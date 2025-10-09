using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;

namespace Astrum.Editor.RoleEditor.Modules
{
    /// <summary>
    /// 动作列表模块
    /// 显示和管理动作列表
    /// </summary>
    public class ActionListModule
    {
        // === 数据 ===
        private string _searchKeyword = "";
        private string _typeFilter = "全部";
        private string _sortMode = "ID升序";
        private Vector2 _scrollPosition;
        private ActionEditorData _selectedAction;
        
        // === 实体选择 ===
        private int _selectedEntityId = 0;
        
        // === 事件 ===
        public event Action<ActionEditorData> OnActionSelected;
        public event Action OnCreateNew;
        public event Action<ActionEditorData> OnDuplicate;
        public event Action<ActionEditorData> OnDelete;
        public event Action<int> OnEntitySelected;
        
        // === 核心方法 ===
        
        /// <summary>
        /// 绘制列表
        /// </summary>
        public void DrawList(Rect rect, List<ActionEditorData> actions)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical();
            {
                DrawHeader();
                DrawEntitySelection();  // 实体选择移到最上方
                DrawSearchBar();
                DrawFilterBar();
                DrawToolbar();
                DrawActionCards(actions);
                DrawStatusBar(actions);
            }
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// 选择动作
        /// </summary>
        public void SelectAction(ActionEditorData action)
        {
            _selectedAction = action;
            OnActionSelected?.Invoke(action);
        }
        
        /// <summary>
        /// 获取选中的动作
        /// </summary>
        public ActionEditorData GetSelectedAction()
        {
            return _selectedAction;
        }
        
        // === 私有绘制方法 ===
        
        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 12;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            
            EditorGUILayout.LabelField("动作编辑器", headerStyle, GUILayout.Height(25));
            EditorGUILayout.Space(2);
        }
        
        private void DrawEntitySelection()
        {
            EditorGUILayout.BeginVertical("box");
            {
                var allEntities = Services.ConfigTableHelper.GetAllEntities();
                if (allEntities.Count == 0)
                {
                    EditorGUILayout.HelpBox("没有可用的实体数据", MessageType.Warning);
                }
                else
                {
                    string[] entityNames = allEntities.Select(e => $"[{e.EntityId}] {e.ArchetypeName}").ToArray();
                    int[] entityIds = allEntities.Select(e => e.EntityId).ToArray();
                    
                    int currentIndex = System.Array.IndexOf(entityIds, _selectedEntityId);
                    if (currentIndex < 0) currentIndex = 0;
                    
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("预览角色", GUILayout.Width(60));
                        int newIndex = EditorGUILayout.Popup(currentIndex, entityNames);  // 占满剩余宽度
                        
                        if (newIndex != currentIndex && newIndex >= 0 && newIndex < entityIds.Length)
                        {
                            _selectedEntityId = entityIds[newIndex];
                            OnEntitySelected?.Invoke(_selectedEntityId);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            {
                _searchKeyword = EditorGUILayout.TextField(_searchKeyword, EditorStyles.toolbarSearchField);
                
                if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
                {
                    _searchKeyword = "";
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFilterBar()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("类型:", GUILayout.Width(40));
                
                var filterOptions = new string[] { "全部", "idle", "walk", "run", "skill", "jump", "interact" };
                int selectedIndex = System.Array.IndexOf(filterOptions, _typeFilter);
                if (selectedIndex < 0) selectedIndex = 0;
                
                int newIndex = EditorGUILayout.Popup(selectedIndex, filterOptions);
                _typeFilter = filterOptions[newIndex];
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("新建", GUILayout.Height(25)))
                {
                    OnCreateNew?.Invoke();
                }
                
                if (GUILayout.Button("复制", GUILayout.Height(25)))
                {
                    if (_selectedAction != null)
                    {
                        OnDuplicate?.Invoke(_selectedAction);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("删除", GUILayout.Height(25)))
                {
                    if (_selectedAction != null)
                    {
                        OnDelete?.Invoke(_selectedAction);
                    }
                }
                
                if (GUILayout.Button("排序▼", GUILayout.Height(25)))
                {
                    ShowSortMenu();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
        }
        
        private void DrawActionCards(List<ActionEditorData> actions)
        {
            var filteredActions = FilterAndSortActions(actions);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
            {
                foreach (var action in filteredActions)
                {
                    DrawActionCard(action);
                    EditorGUILayout.Space(2);
                }
                
                if (filteredActions.Count == 0)
                {
                    EditorGUILayout.HelpBox("没有找到匹配的动作", MessageType.Info);
                }
            }
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawActionCard(ActionEditorData action)
        {
            bool isSelected = action == _selectedAction;
            
            // 选中背景色
            var bgColor = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : Color.clear;
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            
            EditorGUILayout.BeginVertical("box", GUILayout.Height(65));
            {
                // 整个区域可点击
                Rect cardRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(60));
                
                if (GUI.Button(cardRect, "", GUIStyle.none))
                {
                    SelectAction(action);
                }
                
                // 绘制内容
                float x = cardRect.x + 5;
                float y = cardRect.y + 5;
                
                // 第一行：状态图标 + ID + 名称
                var titleStyle = new GUIStyle(EditorStyles.boldLabel);
                if (isSelected) titleStyle.normal.textColor = new Color(0.4f, 0.7f, 1f);
                
                string stateIcon = isSelected ? "⏸" : GetActionTypeIcon(action.ActionType);
                string title = $"{stateIcon} {action.ActionId} {action.ActionName}";
                
                GUI.Label(new Rect(x, y, cardRect.width - 30, 18), title, titleStyle);
                
                // 修改标记
                if (action.IsDirty)
                {
                    var dirtyStyle = new GUIStyle(EditorStyles.boldLabel);
                    dirtyStyle.normal.textColor = Color.yellow;
                    GUI.Label(new Rect(cardRect.xMax - 20, y, 15, 15), "*", dirtyStyle);
                }
                
                // 第二行：类型描述
                var subtitleStyle = new GUIStyle(EditorStyles.miniLabel);
                subtitleStyle.normal.textColor = GetActionTypeColor(action.ActionType);
                
                GUI.Label(new Rect(x, y + 20, cardRect.width - 10, 16), GetActionTypeLabel(action.ActionType), subtitleStyle);
                
                // 第三行：帧数 | 事件数
                var infoStyle = new GUIStyle(EditorStyles.miniLabel);
                infoStyle.normal.textColor = Color.gray;
                
                int eventCount = action.TimelineEvents?.Count ?? 0;
                string info = $"帧:{action.Duration} | 事件:{eventCount}";
                
                GUI.Label(new Rect(x, y + 38, cardRect.width - 10, 16), info, infoStyle);
            }
            EditorGUILayout.EndVertical();
            
            GUI.backgroundColor = originalColor;
        }
        
        private void DrawStatusBar(List<ActionEditorData> actions)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(20));
            {
                int totalCount = actions.Count;
                int modifiedCount = actions.Count(a => a.IsDirty);
                
                EditorGUILayout.LabelField($"总计: {totalCount}", EditorStyles.miniLabel, GUILayout.Width(80));
                
                // 始终绘制修改计数，确保 Layout 和 Repaint 事件中控件数量一致
                var style = new GUIStyle(EditorStyles.miniLabel);
                if (modifiedCount > 0)
                {
                    style.normal.textColor = Color.yellow;
                    EditorGUILayout.LabelField($"已修改: {modifiedCount}", style, GUILayout.Width(80));
                }
                else
                {
                    // 即使没有修改也绘制，保持 GUI 一致性
                    EditorGUILayout.LabelField("", style, GUILayout.Width(80));
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        // === 过滤和排序 ===
        
        private List<ActionEditorData> FilterAndSortActions(List<ActionEditorData> actions)
        {
            var filtered = actions.AsEnumerable();
            
            // 类型筛选
            if (_typeFilter != "全部")
            {
                filtered = filtered.Where(a => a.ActionType == _typeFilter);
            }
            
            // 关键字搜索
            if (!string.IsNullOrWhiteSpace(_searchKeyword))
            {
                string keyword = _searchKeyword.ToLower();
                filtered = filtered.Where(a =>
                    a.ActionName.ToLower().Contains(keyword) ||
                    a.ActionId.ToString().Contains(keyword) ||
                    a.ActionType.ToLower().Contains(keyword)
                );
            }
            
            // 排序
            filtered = ApplySort(filtered);
            
            return filtered.ToList();
        }
        
        private IEnumerable<ActionEditorData> ApplySort(IEnumerable<ActionEditorData> actions)
        {
            return _sortMode switch
            {
                "ID升序" => actions.OrderBy(a => a.ActionId),
                "ID降序" => actions.OrderByDescending(a => a.ActionId),
                "名称A-Z" => actions.OrderBy(a => a.ActionName),
                "名称Z-A" => actions.OrderByDescending(a => a.ActionName),
                "类型分组" => actions.OrderBy(a => a.ActionType).ThenBy(a => a.ActionId),
                _ => actions.OrderBy(a => a.ActionId)
            };
        }
        
        // === 辅助方法 ===
        
        private string GetActionTypeIcon(string actionType)
        {
            return actionType switch
            {
                "idle" => "⏸",
                "walk" => "🚶",
                "run" => "🏃",
                "skill" => "⚔️",
                "jump" => "🦘",
                "interact" => "🤝",
                _ => "▶"
            };
        }
        
        private string GetActionTypeLabel(string actionType)
        {
            return actionType switch
            {
                "idle" => "待机",
                "walk" => "行走",
                "run" => "奔跑",
                "skill" => "技能/攻击",
                "jump" => "跳跃",
                "interact" => "交互",
                _ => actionType
            };
        }
        
        private Color GetActionTypeColor(string actionType)
        {
            return actionType switch
            {
                "idle" => new Color(0.7f, 0.7f, 0.7f),
                "walk" => new Color(0.4f, 0.8f, 1f),
                "run" => new Color(0.4f, 1f, 0.4f),
                "skill" => new Color(1f, 0.4f, 0.4f),
                "jump" => new Color(1f, 0.8f, 0.4f),
                "interact" => new Color(0.8f, 0.6f, 1f),
                _ => Color.gray
            };
        }
        
        private void ShowSortMenu()
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("ID升序"), _sortMode == "ID升序", () => _sortMode = "ID升序");
            menu.AddItem(new GUIContent("ID降序"), _sortMode == "ID降序", () => _sortMode = "ID降序");
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("名称A-Z"), _sortMode == "名称A-Z", () => _sortMode = "名称A-Z");
            menu.AddItem(new GUIContent("名称Z-A"), _sortMode == "名称Z-A", () => _sortMode = "名称Z-A");
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("类型分组"), _sortMode == "类型分组", () => _sortMode = "类型分组");
            
            menu.ShowAsContext();
        }
    }
}
