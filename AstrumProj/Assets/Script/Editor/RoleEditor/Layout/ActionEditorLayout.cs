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
        private const float CONFIG_PANEL_WIDTH = 280f;
        private const float EVENT_PANEL_WIDTH = 320f;
        private const float TIMELINE_HEIGHT = 280f;
        private const float ANIMATION_CONTROL_HEIGHT = 80f;
        private const float SEPARATOR_WIDTH = 5f;
        private const float MIN_CONFIG_WIDTH = 250f;
        private const float MAX_CONFIG_WIDTH = 400f;
        private const float MIN_EVENT_WIDTH = 250f;
        private const float MAX_EVENT_WIDTH = 500f;
        
        // === 可调整尺寸 ===
        private float _customConfigWidth = CONFIG_PANEL_WIDTH;
        private float _customEventWidth = EVENT_PANEL_WIDTH;
        
        // === 拖拽状态 ===
        private bool _isDraggingConfigSeparator = false;
        private bool _isDraggingEventSeparator = false;
        
        // === 布局区域（缓存） ===
        private Rect _toolbarRect;
        private Rect _leftPanelRect;
        private Rect _rightTopRect;
        private Rect _configPanelRect;
        private Rect _previewPanelRect;
        private Rect _eventDetailPanelRect;
        private Rect _timelineRect;
        private Rect _configSeparatorRect;
        private Rect _eventSeparatorRect;
        
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
            
            // 右上：配置面板（左）
            _configPanelRect = new Rect(
                rightX,
                contentY,
                _customConfigWidth,
                rightTopHeight
            );
            
            // 配置分隔线
            _configSeparatorRect = new Rect(
                rightX + _customConfigWidth,
                contentY,
                SEPARATOR_WIDTH,
                rightTopHeight
            );
            
            // 右上：预览面板（中）
            float previewX = rightX + _customConfigWidth + SEPARATOR_WIDTH;
            float previewWidth = rightWidth - _customConfigWidth - _customEventWidth - SEPARATOR_WIDTH * 2;
            _previewPanelRect = new Rect(
                previewX,
                contentY,
                previewWidth,
                rightTopHeight
            );
            
            // 事件分隔线
            _eventSeparatorRect = new Rect(
                previewX + previewWidth,
                contentY,
                SEPARATOR_WIDTH,
                rightTopHeight
            );
            
            // 右上：事件详情面板（右）
            _eventDetailPanelRect = new Rect(
                rightX + rightWidth - _customEventWidth,
                contentY,
                _customEventWidth,
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
            DrawSeparators();
            
            // 处理配置面板分隔线拖拽
            HandleConfigSeparatorDrag(evt);
            
            // 处理事件面板分隔线拖拽
            HandleEventSeparatorDrag(evt, windowRect);
        }
        
        /// <summary>
        /// 处理配置面板分隔线拖拽
        /// </summary>
        private void HandleConfigSeparatorDrag(Event evt)
        {
            if (evt.type == EventType.MouseDown && _configSeparatorRect.Contains(evt.mousePosition))
            {
                _isDraggingConfigSeparator = true;
                evt.Use();
            }
            
            if (_isDraggingConfigSeparator && evt.type == EventType.MouseDrag)
            {
                float newWidth = evt.mousePosition.x - LEFT_LIST_WIDTH;
                _customConfigWidth = Mathf.Clamp(newWidth, MIN_CONFIG_WIDTH, MAX_CONFIG_WIDTH);
                evt.Use();
            }
            
            if (evt.type == EventType.MouseUp)
            {
                _isDraggingConfigSeparator = false;
            }
            
            // 鼠标光标
            if (_configSeparatorRect.Contains(evt.mousePosition) || _isDraggingConfigSeparator)
            {
                EditorGUIUtility.AddCursorRect(_configSeparatorRect, MouseCursor.ResizeHorizontal);
            }
        }
        
        /// <summary>
        /// 处理事件面板分隔线拖拽
        /// </summary>
        private void HandleEventSeparatorDrag(Event evt, Rect windowRect)
        {
            if (evt.type == EventType.MouseDown && _eventSeparatorRect.Contains(evt.mousePosition))
            {
                _isDraggingEventSeparator = true;
                evt.Use();
            }
            
            if (_isDraggingEventSeparator && evt.type == EventType.MouseDrag)
            {
                float rightEdge = windowRect.width;
                float newWidth = rightEdge - evt.mousePosition.x;
                _customEventWidth = Mathf.Clamp(newWidth, MIN_EVENT_WIDTH, MAX_EVENT_WIDTH);
                evt.Use();
            }
            
            if (evt.type == EventType.MouseUp)
            {
                _isDraggingEventSeparator = false;
            }
            
            // 鼠标光标
            if (_eventSeparatorRect.Contains(evt.mousePosition) || _isDraggingEventSeparator)
            {
                EditorGUIUtility.AddCursorRect(_eventSeparatorRect, MouseCursor.ResizeHorizontal);
            }
        }
        
        // === 区域获取方法 ===
        
        public Rect GetToolbarRect() => _toolbarRect;
        public Rect GetLeftPanelRect() => _leftPanelRect;
        public Rect GetRightTopRect() => _rightTopRect;
        public Rect GetConfigPanelRect() => _configPanelRect;
        public Rect GetPreviewPanelRect() => _previewPanelRect;
        public Rect GetEventDetailPanelRect() => _eventDetailPanelRect;
        public Rect GetTimelineRect() => _timelineRect;
        
        // === 私有方法 ===
        
        private void DrawSeparators()
        {
            EditorGUI.DrawRect(_configSeparatorRect, new Color(0.15f, 0.15f, 0.15f));
            EditorGUI.DrawRect(_eventSeparatorRect, new Color(0.15f, 0.15f, 0.15f));
        }
    }
}
