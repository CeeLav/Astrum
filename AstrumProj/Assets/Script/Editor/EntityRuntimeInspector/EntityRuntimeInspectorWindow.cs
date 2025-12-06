using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Astrum.Client.Core;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Capabilities;
using Astrum.LogicCore.Events;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Systems;

namespace Astrum.Editor.EntityRuntimeInspector
{
    /// <summary>
    /// 实体运行时检查器编辑器窗口
    /// 用于在运行时查看实体的组件数据、Capability 状态和消息日志
    /// </summary>
    public class EntityRuntimeInspectorWindow : EditorWindow
    {
        #region Fields

        // === 实体选择 ===
        private string _entityIdInput = "";
        private long _selectedEntityId = 0;
        private Entity _selectedEntity = null;
        private string _entitySearchError = "";

        // === UI 状态 ===
        private Vector2 _componentScrollPosition = Vector2.zero;
        private Vector2 _capabilityScrollPosition = Vector2.zero;
        private Vector2 _logicMessageScrollPosition = Vector2.zero;
        private Vector2 _viewMessageScrollPosition = Vector2.zero;

        // === 组件折叠状态 ===
        private Dictionary<string, bool> _componentFoldouts = new Dictionary<string, bool>();

        // === Capability 折叠状态 ===
        private Dictionary<int, bool> _capabilityFoldouts = new Dictionary<int, bool>();

        // === 反射缓存 ===
        private static Dictionary<Type, FieldInfo[]> _fieldCache = new Dictionary<Type, FieldInfo[]>();
        private static Dictionary<Type, PropertyInfo[]> _propertyCache = new Dictionary<Type, PropertyInfo[]>();

        // === 消息日志 ===
        private List<EntityEvent> _logicMessageLog = new List<EntityEvent>();
        private List<ViewEvent> _viewMessageLog = new List<ViewEvent>();
        private const int MAX_MESSAGE_LOG_COUNT = 100;

        // === 刷新机制 ===
        private bool _autoRefresh = true;
        private float _refreshInterval = 0.5f;
        private float _lastRefreshTime = 0f;

        // === 布局常量 ===
        private const float MIN_WINDOW_WIDTH = 800f;
        private const float MIN_WINDOW_HEIGHT = 600f;

        #endregion

        #region Unity Lifecycle

        [MenuItem("Astrum/Editor 编辑器/Entity Runtime Inspector", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<EntityRuntimeInspectorWindow>("Entity Runtime Inspector");
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_autoRefresh && EditorApplication.isPlaying)
            {
                if (Time.realtimeSinceStartup - _lastRefreshTime >= _refreshInterval)
                {
                    _lastRefreshTime = Time.realtimeSinceStartup;
                    RefreshAll();
                }
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(5);

            DrawEntitySelector();
            EditorGUILayout.Space(5);

            if (_selectedEntity != null)
            {
                DrawComponentView();
                EditorGUILayout.Space(5);

                DrawCapabilityView();
                EditorGUILayout.Space(5);

                DrawMessageLog();
            }
            else if (!string.IsNullOrEmpty(_entitySearchError))
            {
                EditorGUILayout.HelpBox(_entitySearchError, MessageType.Warning);
            }
        }

        #endregion

        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    RefreshAll();
                }

                _autoRefresh = EditorGUILayout.Toggle(_autoRefresh, GUILayout.Width(80));
                EditorGUILayout.LabelField("自动刷新", GUILayout.Width(60));

                EditorGUILayout.LabelField("刷新间隔:", GUILayout.Width(70));
                _refreshInterval = EditorGUILayout.FloatField(_refreshInterval, GUILayout.Width(50));
                _refreshInterval = Mathf.Clamp(_refreshInterval, 0.1f, 5.0f);
                EditorGUILayout.LabelField("秒", GUILayout.Width(30));

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Entity Selector

        private void DrawEntitySelector()
        {
            EditorGUILayout.LabelField("实体选择", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Entity ID:", GUILayout.Width(70));
                _entityIdInput = EditorGUILayout.TextField(_entityIdInput);

                if (GUILayout.Button("查找", GUILayout.Width(60)))
                {
                    OnEntityIdInputChanged();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_selectedEntity != null)
            {
                EditorGUILayout.LabelField($"实体名称: {_selectedEntity.Name}");
                EditorGUILayout.LabelField($"配置ID: {_selectedEntity.EntityConfigId} | 创建时间: {_selectedEntity.CreationTime:yyyy-MM-dd HH:mm:ss}");
            }

            EditorGUI.indentLevel--;
        }

        private void OnEntityIdInputChanged()
        {
            if (string.IsNullOrEmpty(_entityIdInput))
            {
                _selectedEntity = null;
                _selectedEntityId = 0;
                _entitySearchError = "";
                return;
            }

            if (!long.TryParse(_entityIdInput, out long entityId))
            {
                _entitySearchError = "请输入有效的实体ID";
                _selectedEntity = null;
                _selectedEntityId = 0;
                return;
            }

            _selectedEntityId = entityId;
            RefreshEntity();
        }

        private void RefreshEntity()
        {
            _entitySearchError = "";
            _selectedEntity = null;

            try
            {
                var world = GetWorld();
                if (world == null)
                {
                    _entitySearchError = "World 未初始化，请先运行游戏";
                    return;
                }

                _selectedEntity = world.GetEntity(_selectedEntityId);
                if (_selectedEntity == null)
                {
                    _entitySearchError = $"实体 {_selectedEntityId} 不存在";
                    return;
                }

                if (_selectedEntity.IsDestroyed)
                {
                    _entitySearchError = $"实体 {_selectedEntityId} 已被销毁";
                    _selectedEntity = null;
                    return;
                }
            }
            catch (Exception ex)
            {
                _entitySearchError = $"访问实体数据时发生错误: {ex.Message}";
                _selectedEntity = null;
            }
        }

        private World GetWorld()
        {
            if (!GameDirector.IsInitialized)
            {
                return null;
            }

            var gameDirector = GameDirector.Instance;
            if (gameDirector == null)
            {
                return null;
            }

            var currentMode = gameDirector.CurrentGameMode;
            if (currentMode == null)
            {
                return null;
            }

            var mainRoom = currentMode.MainRoom;
            if (mainRoom == null)
            {
                return null;
            }

            return mainRoom.MainWorld;
        }

        #endregion

        #region Component View

        private void DrawComponentView()
        {
            EditorGUILayout.LabelField("组件数据", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (_selectedEntity == null || _selectedEntity.Components == null || _selectedEntity.Components.Count == 0)
            {
                EditorGUILayout.LabelField("该实体没有挂载任何组件", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                return;
            }

            _componentScrollPosition = EditorGUILayout.BeginScrollView(_componentScrollPosition, GUILayout.Height(200));

            foreach (var component in _selectedEntity.Components.Values)
            {
                var componentType = component.GetType();
                var componentKey = componentType.Name;

                if (!_componentFoldouts.ContainsKey(componentKey))
                {
                    _componentFoldouts[componentKey] = false;
                }

                _componentFoldouts[componentKey] = EditorGUILayout.Foldout(
                    _componentFoldouts[componentKey],
                    componentType.Name,
                    true
                );

                if (_componentFoldouts[componentKey])
                {
                    EditorGUI.indentLevel++;
                    DrawComponentFields(component, componentType);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUI.indentLevel--;
        }

        private void DrawComponentFields(BaseComponent component, Type componentType)
        {
            try
            {
                var fields = GetCachedFields(componentType);
                var properties = GetCachedProperties(componentType);

                foreach (var field in fields)
                {
                    if (field.IsStatic) continue;

                    var value = field.GetValue(component);
                    var valueStr = FormatValue(value);
                    EditorGUILayout.LabelField($"{field.Name}: {valueStr}", EditorStyles.miniLabel);
                }

                foreach (var prop in properties)
                {
                    if (!prop.CanRead) continue;

                    try
                    {
                        var value = prop.GetValue(component);
                        var valueStr = FormatValue(value);
                        EditorGUILayout.LabelField($"{prop.Name}: {valueStr}", EditorStyles.miniLabel);
                    }
                    catch
                    {
                        // 忽略无法读取的属性
                    }
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.LabelField($"显示组件数据时发生错误: {ex.Message}", EditorStyles.miniLabel);
            }
        }

        private FieldInfo[] GetCachedFields(Type type)
        {
            if (!_fieldCache.ContainsKey(type))
            {
                _fieldCache[type] = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(f => !f.IsDefined(typeof(System.Runtime.Serialization.IgnoreDataMemberAttribute), false))
                    .ToArray();
            }
            return _fieldCache[type];
        }

        private PropertyInfo[] GetCachedProperties(Type type)
        {
            if (!_propertyCache.ContainsKey(type))
            {
                _propertyCache[type] = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && !p.IsDefined(typeof(System.Runtime.Serialization.IgnoreDataMemberAttribute), false))
                    .ToArray();
            }
            return _propertyCache[type];
        }

        private string FormatValue(object value)
        {
            if (value == null) return "null";

            var type = value.GetType();

            // 处理集合类型
            if (value is System.Collections.ICollection collection)
            {
                return $"[{collection.Count} items]";
            }

            // 处理字典类型
            if (value is System.Collections.IDictionary dict)
            {
                return $"[{dict.Count} entries]";
            }

            // 处理 Unity 类型
            if (value is UnityEngine.Vector3 vec3)
            {
                return $"({vec3.x:F2}, {vec3.y:F2}, {vec3.z:F2})";
            }

            if (value is UnityEngine.Vector2 vec2)
            {
                return $"({vec2.x:F2}, {vec2.y:F2})";
            }

            // 默认 ToString
            return value.ToString();
        }

        #endregion

        #region Capability View

        private void DrawCapabilityView()
        {
            EditorGUILayout.LabelField("Capability 状态", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (_selectedEntity == null || _selectedEntity.World?.CapabilitySystem == null)
            {
                EditorGUILayout.LabelField("CapabilitySystem 不可用", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                return;
            }

            if (_selectedEntity.CapabilityStates == null || _selectedEntity.CapabilityStates.Count == 0)
            {
                EditorGUILayout.LabelField("该实体没有注册任何 Capability", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                DrawDisabledTags();
                return;
            }

            _capabilityScrollPosition = EditorGUILayout.BeginScrollView(_capabilityScrollPosition, GUILayout.Height(200));

            var capSystem = _selectedEntity.World.CapabilitySystem;
            foreach (var kvp in _selectedEntity.CapabilityStates)
            {
                var typeId = kvp.Key;
                var state = kvp.Value;

                if (!_capabilityFoldouts.ContainsKey(typeId))
                {
                    _capabilityFoldouts[typeId] = false;
                }

                // 获取 Capability 实例（通过反射访问私有方法）
                ICapability capability = null;
                try
                {
                    var method = typeof(CapabilitySystem).GetMethod("GetCapability", 
                        BindingFlags.NonPublic | BindingFlags.Static, 
                        null, 
                        new[] { typeof(int) }, 
                        null);
                    if (method != null)
                    {
                        capability = method.Invoke(null, new object[] { typeId }) as ICapability;
                    }
                }
                catch
                {
                    // 忽略无法获取的 Capability
                }

                if (capability == null)
                {
                    EditorGUILayout.LabelField($"Unknown Capability (TypeId: {typeId})", EditorStyles.miniLabel);
                    continue;
                }

                var capabilityName = capability.GetType().Name;
                var statusColor = state.IsActive ? Color.green : Color.gray;
                var statusText = state.IsActive ? "[激活]" : "[禁用]";

                EditorGUILayout.BeginHorizontal();
                {
                    var originalColor = GUI.color;
                    GUI.color = statusColor;
                    EditorGUILayout.LabelField(statusText, GUILayout.Width(50));
                    GUI.color = originalColor;

                    _capabilityFoldouts[typeId] = EditorGUILayout.Foldout(
                        _capabilityFoldouts[typeId],
                        capabilityName,
                        true
                    );
                }
                EditorGUILayout.EndHorizontal();

                if (_capabilityFoldouts[typeId])
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"激活状态: {state.IsActive}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"优先级: {capability.Priority}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"标签: {string.Join(", ", capability.Tags)}", EditorStyles.miniLabel);

                    if (capability.TrackActiveDuration && state.IsActive)
                    {
                        EditorGUILayout.LabelField($"激活持续时间: {state.ActiveDuration} 帧", EditorStyles.miniLabel);
                    }

                    if (capability.TrackDeactiveDuration && !state.IsActive)
                    {
                        EditorGUILayout.LabelField($"禁用持续时间: {state.DeactiveDuration} 帧", EditorStyles.miniLabel);
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);
            DrawDisabledTags();

            EditorGUI.indentLevel--;
        }

        private void DrawDisabledTags()
        {
            if (_selectedEntity == null || _selectedEntity.DisabledTags == null || _selectedEntity.DisabledTags.Count == 0)
            {
                EditorGUILayout.LabelField("被禁用的 Tag: 无", EditorStyles.miniLabel);
                return;
            }

            EditorGUILayout.LabelField("被禁用的 Tag:", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            foreach (var kvp in _selectedEntity.DisabledTags)
            {
                var tag = kvp.Key;
                var disabledBy = kvp.Value; // HashSet<long>
                var disabledByStr = string.Join(", ", disabledBy);
                EditorGUILayout.LabelField($"• {tag} (禁用者: {disabledByStr})", EditorStyles.miniLabel);
            }
            EditorGUI.indentLevel--;
        }

        #endregion

        #region Message Log

        private void DrawMessageLog()
        {
            EditorGUILayout.LabelField("消息日志", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            UpdateMessageLog();

            // 逻辑层消息队列
            EditorGUILayout.LabelField("逻辑层消息队列", EditorStyles.miniLabel);
            _logicMessageScrollPosition = EditorGUILayout.BeginScrollView(_logicMessageScrollPosition, GUILayout.Height(150));

            if (_logicMessageLog.Count == 0)
            {
                EditorGUILayout.LabelField("暂无消息", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var msg in _logicMessageLog)
                {
                    var msgTypeName = msg.EventType?.Name ?? "Unknown";
                    var msgDataStr = msg.EventData?.ToString() ?? "null";
                    EditorGUILayout.LabelField($"[Frame: {msg.Frame}] {msgTypeName}: {msgDataStr}", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);

            // 视图层消息队列
            EditorGUILayout.LabelField("视图层消息队列", EditorStyles.miniLabel);
            _viewMessageScrollPosition = EditorGUILayout.BeginScrollView(_viewMessageScrollPosition, GUILayout.Height(150));

            if (_viewMessageLog.Count == 0)
            {
                EditorGUILayout.LabelField("暂无消息", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var msg in _viewMessageLog)
                {
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var msgTypeName = msg.GetType().Name;
                    var msgDataStr = msg.ToString();
                    EditorGUILayout.LabelField($"[{timestamp}] {msgTypeName}: {msgDataStr}", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("清空日志", GUILayout.Width(100)))
            {
                _logicMessageLog.Clear();
                _viewMessageLog.Clear();
            }

            EditorGUI.indentLevel--;
        }

        private void UpdateMessageLog()
        {
            if (_selectedEntity == null) return;

            try
            {
                // 更新逻辑层消息队列（使用反射访问 internal 属性）
                var eventQueueProperty = typeof(Entity).GetProperty("EventQueue", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (eventQueueProperty != null)
                {
                    var eventQueue = eventQueueProperty.GetValue(_selectedEntity) as System.Collections.Generic.Queue<EntityEvent>;
                    if (eventQueue != null)
                    {
                        var logicSnapshot = eventQueue.ToArray();
                        _logicMessageLog.AddRange(logicSnapshot);
                        if (_logicMessageLog.Count > MAX_MESSAGE_LOG_COUNT)
                        {
                            _logicMessageLog.RemoveRange(0, _logicMessageLog.Count - MAX_MESSAGE_LOG_COUNT);
                        }
                    }
                }

                // 更新视图层消息队列
                if (_selectedEntity.ViewEventQueue != null)
                {
                    var viewSnapshot = _selectedEntity.ViewEventQueue.ToArray();
                    _viewMessageLog.AddRange(viewSnapshot);
                    if (_viewMessageLog.Count > MAX_MESSAGE_LOG_COUNT)
                    {
                        _viewMessageLog.RemoveRange(0, _viewMessageLog.Count - MAX_MESSAGE_LOG_COUNT);
                    }
                }
            }
            catch (Exception ex)
            {
                // 忽略消息日志更新错误
                Debug.LogWarning($"EntityRuntimeInspector: 更新消息日志时发生错误: {ex.Message}");
            }
        }

        #endregion

        #region Refresh

        private void RefreshAll()
        {
            if (_selectedEntityId > 0)
            {
                RefreshEntity();
            }

            Repaint();
        }

        #endregion
    }
}

