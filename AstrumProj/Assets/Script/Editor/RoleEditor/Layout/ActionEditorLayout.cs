using UnityEditor;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Layout
{
    /// <summary>
    /// 动作编辑器布局管理器
    /// 负责计算和管理窗口布局
    /// </summary>
    public class ActionEditorLayout
    {
        // === 布局常量 ===
        private const float TOOLBAR_HEIGHT = 30f;
        private const float LEFT_LIST_WIDTH = 240f;
        private const float CONFIG_PANEL_WIDTH = 340f;
        private const float TIMELINE_HEIGHT = 280f;
        private const float ANIMATION_CONTROL_HEIGHT = 80f;
        private const float SEPARATOR_WIDTH = 5f;
        private const float MIN_CONFIG_WIDTH = 250f;
        private const float MAX_CONFIG_WIDTH = 500f;
        
        // === 可调整尺寸 ===
        private float _customConfigWidth = CONFIG_PANEL_WIDTH;
        
        // === 拖拽状态 ===
        private bool _isDraggingSeparator = false;
        
        // === 布局区域（缓存） ===
        private Rect _toolbarRect;
        private Rect _leftPanelRect;
        private Rect _rightTopRect;
        private Rect _configPanelRect;
        private Rect _previewPanelRect;
        private Rect _timelineRect;
        private Rect _separatorRect;
        
        // === 核心方法 ===
        
        /// <summary>
        /// 计算布局
        /// </summary>
        public void CalculateLayout(Rect windowRect)
        {
            // 工具栏
            _toolbarRect = new Rect(0, 0, windowRect.width, TOOLBAR_HEIGHT);
            
            // 主体区域（工具栏下方）
            float contentY = TOOLBAR_HEIGHT;
            float contentHeight = windowRect.height - TOOLBAR_HEIGHT;
            
            // 左侧动作列表（占满高度）
            _leftPanelRect = new Rect(
                0,
                contentY,
                LEFT_LIST_WIDTH,
                contentHeight
            );
            
            // 右侧区域
            float rightX = LEFT_LIST_WIDTH;
            float rightWidth = windowRect.width - LEFT_LIST_WIDTH;
            float rightTopHeight = contentHeight - TIMELINE_HEIGHT;
            
            _rightTopRect = new Rect(
                rightX,
                contentY,
                rightWidth,
                rightTopHeight
            );
            
            // 右上：配置面板
            _configPanelRect = new Rect(
                rightX,
                contentY,
                _customConfigWidth,
                rightTopHeight
            );
            
            // 右上：预览面板
            _previewPanelRect = new Rect(
                rightX + _customConfigWidth + SEPARATOR_WIDTH,
                contentY,
                rightWidth - _customConfigWidth - SEPARATOR_WIDTH,
                rightTopHeight
            );
            
            // 分隔线
            _separatorRect = new Rect(
                rightX + _customConfigWidth,
                contentY,
                SEPARATOR_WIDTH,
                rightTopHeight
            );
            
            // 右下：时间轴
            _timelineRect = new Rect(
                rightX,
                contentY + rightTopHeight,
                rightWidth,
                TIMELINE_HEIGHT
            );
        }
        
        /// <summary>
        /// 处理分隔线拖拽
        /// </summary>
        public void HandleSeparatorDrag(Event evt, Rect windowRect)
        {
            // 绘制分隔线
            DrawSeparator();
            
            // 处理拖拽
            if (evt.type == EventType.MouseDown && _separatorRect.Contains(evt.mousePosition))
            {
                _isDraggingSeparator = true;
                evt.Use();
            }
            
            if (_isDraggingSeparator && evt.type == EventType.MouseDrag)
            {
                float newWidth = evt.mousePosition.x - LEFT_LIST_WIDTH;
                _customConfigWidth = Mathf.Clamp(newWidth, MIN_CONFIG_WIDTH, MAX_CONFIG_WIDTH);
                evt.Use();
            }
            
            if (evt.type == EventType.MouseUp)
            {
                _isDraggingSeparator = false;
            }
            
            // 鼠标光标
            if (_separatorRect.Contains(evt.mousePosition) || _isDraggingSeparator)
            {
                EditorGUIUtility.AddCursorRect(_separatorRect, MouseCursor.ResizeHorizontal);
            }
        }
        
        // === 区域获取方法 ===
        
        public Rect GetToolbarRect() => _toolbarRect;
        public Rect GetLeftPanelRect() => _leftPanelRect;
        public Rect GetRightTopRect() => _rightTopRect;
        public Rect GetConfigPanelRect() => _configPanelRect;
        public Rect GetPreviewPanelRect() => _previewPanelRect;
        public Rect GetTimelineRect() => _timelineRect;
        
        // === 私有方法 ===
        
        private void DrawSeparator()
        {
            EditorGUI.DrawRect(_separatorRect, new Color(0.15f, 0.15f, 0.15f));
        }
    }
}
