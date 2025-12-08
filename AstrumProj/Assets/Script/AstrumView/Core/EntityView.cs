using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Events;
using Astrum.View.Components;
using Astrum.View.Archetypes;
using cfg;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Astrum.View.Core
{
    /// <summary>
    /// 实体视图基类
    /// 对应LogicCore中的Entity，负责实体的视觉表现
    /// </summary>
    public class EntityView
    {
        // 实体视图设置
        protected long _entityId = 0;
        protected Entity _ownerEntity = null;
        protected Stage _stage = null;
        
        // 组件管理
        protected List<ViewComponent> _viewComponents = new List<ViewComponent>();

        // ComponentTypeId 到 ViewComponent 列表的映射（用于脏组件同步）
        private Dictionary<int, List<ViewComponent>> _componentIdToViewComponentsMap = new Dictionary<int, List<ViewComponent>>();
        
        // 注意：不再维护实例级的事件映射，改用全局 ViewComponentEventRegistry
        // 类似 CapabilitySystem 使用全局 _eventToHandlers
        
        // 子原型管理
        protected List<EArchetype> _activeSubArchetypes = new List<EArchetype>();
        protected Dictionary<Type, int> _viewComponentRefCounts = new Dictionary<Type, int>();
        
        // Unity GameObject引用
        protected GameObject _gameObject;
        protected Transform _transform;
        
        // 公共属�?
        public long EntityId => _entityId;
        
        public Stage Stage => _stage;
        public Entity OwnerEntity
        {
            get
            {
                if (_ownerEntity == null || _ownerEntity.IsDestroyed || _ownerEntity.UniqueId != _entityId)
                {
                    // 检查权威实体是否有影子实体标记
                    var authorityEntity = _stage?.Room?.MainWorld?.GetEntity(EntityId);
                    bool hasShadow = authorityEntity != null && authorityEntity.HasShadow;
                    
                    if (hasShadow && _stage?.Room?.ShadowWorld != null)
                    {
                        // 有影子实体标记：优先从 ShadowWorld 获取影子实体
                        var shadowEntity = _stage.Room.ShadowWorld.GetEntity(EntityId);
                        if (shadowEntity != null && !shadowEntity.IsDestroyed)
                        {
                            _ownerEntity = shadowEntity;
                            return _ownerEntity;
                        }
                    }
                    
                    // 没有影子实体标记或影子实体不存在：使用权威实体
                    if (_stage?.Room?.MainWorld != null)
                    {
                        _ownerEntity = _stage.Room.MainWorld.GetEntity(EntityId);
                    }
                }

                if(_ownerEntity != null){
                    if(_ownerEntity.HasShadow){
                        var shadowEntity = _stage.Room.ShadowWorld.GetEntity(EntityId);
                        if (shadowEntity != null && !shadowEntity.IsDestroyed)
                        {
                            _ownerEntity = shadowEntity;
                            return _ownerEntity;
                        }
                    }
                }
                return _ownerEntity;
            }
        }
        public GameObject GameObject => _gameObject;
        public Transform Transform => _transform;
        public List<ViewComponent> ViewComponents => _viewComponents;
        public IReadOnlyList<EArchetype> ActiveSubArchetypes => _activeSubArchetypes;
        
        // 事件
        
        public EntityView()
        {
        }
        
        /// <summary>
        /// 初始化实体视�?
        /// </summary>
        /// <param name="entityId">实体ID</param>
        public virtual void Initialize(long entityId, Stage stage)
        {
            if (_entityId != 0)
            {
                ASLogger.Instance.Warning($"EntityView: 实体视图已经初始化，ID: {_entityId}");
                return;
            }
            
            _entityId = entityId;
            _stage = stage;
            
            // 检查权威实体是否有影子实体标记
            var authorityEntity = stage?.Room?.MainWorld?.GetEntity(entityId);
            bool hasShadow = authorityEntity != null && authorityEntity.HasShadow;
            
            if (hasShadow && stage?.Room?.ShadowWorld != null)
            {
                // 有影子实体标记：优先从 ShadowWorld 获取影子实体
                var shadowEntity = stage.Room.ShadowWorld.GetEntity(entityId);
                if (shadowEntity != null && !shadowEntity.IsDestroyed)
                {
                    _ownerEntity = shadowEntity;
                    ASLogger.Instance.Info($"EntityView: 初始化实体视图（有影子实体，绑定影子实体），ID: {entityId}");
                }
                else
                {
                    // 如果没有影子实体，不设置 _ownerEntity，让 getter 每次重新检查
                    // 这样当影子实体创建后，会自动切换到影子实体
                    ASLogger.Instance.Info($"EntityView: 初始化实体视图（有影子标记，影子不存在，等待影子创建），ID: {entityId}");
                }
            }
            else
            {
                // 没有影子实体标记：直接使用权威实体
                _ownerEntity = stage.Room.MainWorld?.GetEntity(entityId);
                ASLogger.Instance.Info($"EntityView: 初始化实体视图（无影子标记，绑定权威实体），ID: {entityId}");
            }
            
            try
            {
                // 创建Unity GameObject
                CreateGameObject();
                
                ASLogger.Instance.Info($"EntityView: 实体视图初始化完成，ID: {entityId}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"EntityView: 实体视图初始化失败，ID: {entityId} - {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 更新实体视图
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        public virtual void UpdateView(float deltaTime)
        {
            // 更新所有视图组件
            foreach (var component in _viewComponents)
            {
                if (component.IsEnabled)
                {
                    component.Update(deltaTime);
                }
            }

        }

        /// <summary>
        /// 重置视图（用于回放跳转时强制同步）
        /// </summary>
        public virtual void Reset()
        {
            if (_entityId == 0) return;
            
            // 1. 重置所有 ViewComponent
            foreach (var component in _viewComponents)
            {
                if (component != null && component.IsEnabled)
                {
                    component.Reset();
                }
            }
        }
        
        /// <summary>
        /// 添加视图组件
        /// </summary>
        /// <param name="component">视图组件</param>
        public virtual void AddViewComponent(ViewComponent component)
        {
            if (component == null)
            {
                ASLogger.Instance.Warning($"EntityView: 尝试添加空的视图组件，ID: {_entityId}");
                return;
            }
            
            if (_viewComponents.Contains(component))
            {
                ASLogger.Instance.Warning($"EntityView: 视图组件已存在，ID: {_entityId}");
                return;
            }
            
            _viewComponents.Add(component);
            component.OwnerEntityView = this;
            component.Initialize();
            
            // 建立脏组件映射关系
            RegisterViewComponentWatchedIds(component);
            
            // 注意：不再需要建立视图事件映射
            // ViewComponent 的事件处理器已在全局 ViewComponentEventRegistry 中注册
            
            ASLogger.Instance.Info($"EntityView: 添加视图组件，ID: {_entityId}，组件: {component.GetType().Name}");
        }
        
        /// <summary>
        /// 移除视图组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        public virtual void RemoveViewComponent<T>() where T : ViewComponent
        {
            var component = GetViewComponent<T>();
            if (component != null)
            {
                // 清理映射关系
                UnregisterViewComponentWatchedIds(component);
                
                _viewComponents.Remove(component);
                component.Destroy();
                
                ASLogger.Instance.Info($"EntityView: 移除视图组件，ID: {_entityId}，组件: {typeof(T).Name}");
            }
        }
        
        /// <summary>
        /// 获取视图组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>视图组件</returns>
        public virtual T GetViewComponent<T>() where T : ViewComponent
        {
            foreach (var component in _viewComponents)
            {
                if (component is T targetComponent)
                {
                    return targetComponent;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 销毁实体视�?
        /// </summary>
        public virtual void Destroy()
        {
            ASLogger.Instance.Info($"EntityView: 销毁实体视图，ID: {_entityId}");
            
            // 销毁所有视图组�?
            foreach (var component in _viewComponents)
            {
                if (component != null)
                {
                    component.Destroy();
                }
            }
            _viewComponents.Clear();
            
            // 清理映射关系
            _componentIdToViewComponentsMap.Clear();
            
            // 销毁Unity GameObject
            if (_gameObject != null)
            {
                UnityEngine.Object.Destroy(_gameObject);
                _gameObject = null;
                _transform = null;
            }
        }
        
        /// <summary>
        /// 获取实体视图的世界位�?
        /// </summary>
        /// <returns>世界位置</returns>
        public virtual Vector3 GetWorldPosition()
        {
            return _transform != null ? _transform.position : Vector3.zero;
        }
        
        /// <summary>
        /// 设置实体视图的世界位�?
        /// </summary>
        /// <param name="position">世界位置</param>
        public virtual void SetWorldPosition(Vector3 position)
        {
            if (_transform != null)
            {
                _transform.position = position;
            }
        }
        
        /// <summary>
        /// 获取实体视图的世界旋�?
        /// </summary>
        /// <returns>世界旋转</returns>
        public virtual Quaternion GetWorldRotation()
        {
            return _transform != null ? _transform.rotation : Quaternion.identity;
        }
        
        /// <summary>
        /// 设置实体视图的世界旋�?
        /// </summary>
        /// <param name="rotation">世界旋转</param>
        public virtual void SetWorldRotation(Quaternion rotation)
        {
            if (_transform != null)
            {
                _transform.rotation = rotation;
            }
        }
        
        /// <summary>
        /// 创建Unity GameObject
        /// </summary>
        protected virtual void CreateGameObject()
        {
            _gameObject = new GameObject($"EntityView_{_entityId}");
            _transform = _gameObject.transform;
        }
        
        /// <summary>
        /// 注册 ViewComponent 监听的组件 ID
        /// </summary>
        private void RegisterViewComponentWatchedIds(ViewComponent viewComponent)
        {
            var watchedIds = viewComponent.GetWatchedComponentIds();
            if (watchedIds == null || watchedIds.Length == 0)
            {
                return; // 没有需要监听的组件
            }
            
            foreach (var componentId in watchedIds)
            {
                if (!_componentIdToViewComponentsMap.ContainsKey(componentId))
                {
                    _componentIdToViewComponentsMap[componentId] = new List<ViewComponent>();
                }
                if (!_componentIdToViewComponentsMap[componentId].Contains(viewComponent))
                {
                    _componentIdToViewComponentsMap[componentId].Add(viewComponent);
                }
            }
        }
        
        /// <summary>
        /// 取消注册 ViewComponent 监听的组件 ID
        /// </summary>
        private void UnregisterViewComponentWatchedIds(ViewComponent viewComponent)
        {
            var watchedIds = viewComponent.GetWatchedComponentIds();
            if (watchedIds == null || watchedIds.Length == 0)
            {
                return; // 没有需要取消监听的组件
            }
            
            foreach (var componentId in watchedIds)
            {
                if (_componentIdToViewComponentsMap.ContainsKey(componentId))
                {
                    _componentIdToViewComponentsMap[componentId].Remove(viewComponent);
                    if (_componentIdToViewComponentsMap[componentId].Count == 0)
                    {
                        _componentIdToViewComponentsMap.Remove(componentId);
                    }
                }
            }
        }
        
        /// <summary>
        /// 同步脏组件（由 Stage 调用）
        /// </summary>
        /// <param name="dirtyComponentIds">脏组件的 ComponentTypeId 集合</param>
        public void SyncDirtyComponents(IReadOnlyCollection<int> dirtyComponentIds)
        {
            if (dirtyComponentIds == null || dirtyComponentIds.Count == 0)
            {
                return;
            }
            
            // 遍历所有脏组件 ID
            foreach (var componentId in dirtyComponentIds)
            {
                // 检查是否有 ViewComponent 监听此组件 ID
                if (!_componentIdToViewComponentsMap.ContainsKey(componentId))
                {
                    continue;
                }
                
                // 获取对应的 ViewComponent 列表
                var viewComponents = _componentIdToViewComponentsMap[componentId];
                
                // 通知所有监听的 ViewComponent
                foreach (var viewComponent in viewComponents)
                {
                    if (viewComponent != null && viewComponent.IsEnabled)
                    {
                        // 调用 ViewComponent 的数据同步方法（传入 ComponentTypeId）
                        viewComponent.SyncDataFromComponent(componentId);
                    }
                }
            }
        }
        
        /// <summary>
        /// 根据传入的类型列表构建和挂载视图组件（由EntityViewFactory调用）
        /// </summary>
        /// <param name="componentTypes">要构建的ViewComponent类型列表</param>
        public virtual void BuildViewComponents(Type[] componentTypes)
        {
            if (componentTypes == null || componentTypes.Length == 0)
            {
                return;
            }
            
            foreach (var componentType in componentTypes)
            {
                if (componentType != null && componentType.IsSubclassOf(typeof(ViewComponent)))
                {
                    try
                    {
                        var component = Activator.CreateInstance(componentType) as ViewComponent;
                        if (component != null)
                        {
                            AddViewComponent(component);
                        }
                    }
                    catch (Exception ex)
                    {
                        ASLogger.Instance.Error($"EntityView: 创建视图组件 {componentType.Name} 失败 - {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 挂载子原型
        /// </summary>
        /// <param name="subArchetype">子原型类型</param>
        /// <returns>是否成功</returns>
        public virtual bool AttachSubArchetype(EArchetype subArchetype)
        {
            if (_activeSubArchetypes.Contains(subArchetype))
            {
                return true; // 已经激活
            }
            
            // 查询子原型对应的 ViewComponents
            if (!ViewArchetypeManager.Instance.TryGetComponents(subArchetype, out var viewComponentTypes))
            {
                ASLogger.Instance.Warning($"EntityView: 子原型 {subArchetype} 没有对应的 ViewComponents，ID: {_entityId}");
                return false;
            }
            
            // 使用引用计数装配 ViewComponents
            foreach (var componentType in viewComponentTypes)
            {
                if (componentType == null || !componentType.IsSubclassOf(typeof(ViewComponent))) continue;
                
                if (!_viewComponentRefCounts.TryGetValue(componentType, out var count))
                {
                    count = 0;
                }
                
                if (count == 0)
                {
                    // 需要创建新的 ViewComponent
                    try
                    {
                        var component = Activator.CreateInstance(componentType) as ViewComponent;
                        if (component != null)
                        {
                            AddViewComponent(component);
                        }
                        else
                        {
                            ASLogger.Instance.Error($"EntityView: 创建 ViewComponent {componentType.Name} 失败，ID: {_entityId}");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        ASLogger.Instance.Error($"EntityView: 创建 ViewComponent {componentType.Name} 失败，ID: {_entityId} - {ex.Message}");
                        continue;
                    }
                }
                
                _viewComponentRefCounts[componentType] = count + 1;
            }
            
            _activeSubArchetypes.Add(subArchetype);
            ASLogger.Instance.Info($"EntityView: 挂载子原型 {subArchetype}，ID: {_entityId}");
            return true;
        }
        
        /// <summary>
        /// 卸载子原型
        /// </summary>
        /// <param name="subArchetype">子原型类型</param>
        /// <returns>是否成功</returns>
        public virtual bool DetachSubArchetype(EArchetype subArchetype)
        {
            if (!_activeSubArchetypes.Contains(subArchetype))
            {
                return true; // 已经卸载
            }
            
            // 查询子原型对应的 ViewComponents
            if (!ViewArchetypeManager.Instance.TryGetComponents(subArchetype, out var viewComponentTypes))
            {
                // 即使查询失败，也要从列表中移除
                _activeSubArchetypes.Remove(subArchetype);
                return true;
            }
            
            // 使用引用计数卸载 ViewComponents
            foreach (var componentType in viewComponentTypes)
            {
                if (componentType == null) continue;
                
                if (!_viewComponentRefCounts.TryGetValue(componentType, out var count))
                {
                    count = 0;
                }
                
                if (count > 0)
                {
                    count--;
                    _viewComponentRefCounts[componentType] = count;
                    
                    if (count == 0)
                    {
                        // 引用计数归零，移除 ViewComponent
                        var component = GetViewComponentByType(componentType);
                        if (component != null)
                        {
                            RemoveViewComponent(component);
                        }
                        _viewComponentRefCounts.Remove(componentType);
                    }
                }
            }
            
            _activeSubArchetypes.Remove(subArchetype);
            ASLogger.Instance.Info($"EntityView: 卸载子原型 {subArchetype}，ID: {_entityId}");
            return true;
        }
        
        /// <summary>
        /// 根据类型获取 ViewComponent
        /// </summary>
        /// <param name="componentType">组件类型</param>
        /// <returns>ViewComponent 实例，如果不存在返回 null</returns>
        private ViewComponent GetViewComponentByType(Type componentType)
        {
            return _viewComponents.FirstOrDefault(c => c.GetType() == componentType);
        }
        
        /// <summary>
        /// 移除视图组件（内部方法，用于子原型卸载）
        /// </summary>
        /// <param name="component">要移除的组件</param>
        private void RemoveViewComponent(ViewComponent component)
        {
            if (component == null) return;
            
            // 清理脏组件映射关系
            UnregisterViewComponentWatchedIds(component);
            
            // 注意：不再需要清理视图事件映射
            // ViewComponent 的事件处理器在全局注册表中，不需要清理
            
            _viewComponents.Remove(component);
            component.Destroy();
            
            ASLogger.Instance.Info($"EntityView: 移除视图组件，ID: {_entityId}，组件: {component.GetType().Name}");
        }
        
        #region 视图事件处理
        
        /// <summary>
        /// 处理视图事件（由 Stage 传递下来）
        /// EntityView 只处理 EntityView 和 ViewComponent 级别的事件
        /// Stage 级别的事件（EntityCreated, EntityDestroyed, WorldRollback）由 Stage 直接处理
        /// </summary>
        /// <param name="evt">视图事件</param>
        public void ProcessEvent(ViewEvent evt)
        {
            switch (evt.EventType)
            {
                case ViewEventType.SubArchetypeChanged:
                    // EntityView 级别：自己处理
                    ProcessEntityViewEvent_SubArchetypeChanged(evt);
                    break;
                    
                case ViewEventType.CustomViewEvent:
                    // ViewComponent 级别：分发给 ViewComponent
                    if (evt.EventData != null)
                    {
                        var eventDataType = evt.EventData.GetType();
                        DispatchViewEventToComponents(eventDataType, evt.EventData);
                    }
                    break;
                    
                default:
                    ASLogger.Instance.Warning($"EntityView: 收到不应由 EntityView 处理的事件类型 {evt.EventType}，EntityId: {_entityId}");
                    break;
            }
        }
        
        /// <summary>
        /// 处理 EntityView 级别事件：子原型变化
        /// </summary>
        private void ProcessEntityViewEvent_SubArchetypeChanged(ViewEvent evt)
        {
            if (evt.EventData is EntitySubArchetypeChangedEventData data)
            {
                ASLogger.Instance.Debug($"EntityView: 处理子原型变化事件，EntityId: {_entityId}, Type: {data.SubArchetype}, ChangeType: {data.ChangeType}");
                
                if (data.ChangeType == SubArchetypeChangeType.Attach)
                {
                    AttachSubArchetype(data.SubArchetype);
                }
                else if (data.ChangeType == SubArchetypeChangeType.Detach)
                {
                    DetachSubArchetype(data.SubArchetype);
                }
            }
            else
            {
                ASLogger.Instance.Warning($"EntityView: 子原型变化事件数据类型错误，EntityId: {_entityId}");
            }
        }
        
        /// <summary>
        /// 分发视图事件到 ViewComponent
        /// 使用全局 ViewComponentEventRegistry 查询映射，类似 CapabilitySystem.DispatchEventToEntity
        /// </summary>
        /// <param name="eventType">事件数据类型（例如 HitAnimationEvent）</param>
        /// <param name="eventData">事件数据实例</param>
        private void DispatchViewEventToComponents(Type eventType, object eventData)
        {
            // 1. 查询全局映射：哪些 ViewComponent 类型监听此事件
            var componentTypes = ViewComponentEventRegistry.Instance.GetComponentTypesForEvent(eventType);
            if (componentTypes == null || componentTypes.Count == 0)
            {
                // 没有 ViewComponent 监听此事件（正常情况，不输出警告）
                // ASLogger.Instance.Debug($"EntityView: 没有 ViewComponent 监听事件 {eventType.Name}，EntityId: {_entityId}");
                return;
            }
            
            ASLogger.Instance.Debug($"EntityView: 分发视图事件 {eventType.Name} 到 {componentTypes.Count} 种 ViewComponent，EntityId: {_entityId}");
            
            // 2. 检查当前 EntityView 是否有对应的 ViewComponent 实例
            foreach (var componentType in componentTypes)
            {
                // 查找对应类型的 ViewComponent 实例
                ViewComponent component = null;
                foreach (var c in _viewComponents)
                {
                    if (c.GetType() == componentType)
                    {
                        component = c;
                        break;
                    }
                }
                
                // 如果找到实例且已启用，则分发事件
                if (component != null && component.IsEnabled)
                {
                    try
                    {
                        // 3. 调用实例的事件处理器
                        component.InvokeEventHandler(eventType, eventData);
                        ASLogger.Instance.Debug($"EntityView: 成功分发事件到 {component.GetType().Name}，EventType: {eventType.Name}, EntityId: {_entityId}");
                    }
                    catch (Exception ex)
                    {
                        ASLogger.Instance.Error($"EntityView: 分发事件到 {component.GetType().Name} 时发生异常，EventType: {eventType.Name}, EntityId: {_entityId}, Error: {ex.Message}");
                    }
                }
            }
        }
        
        #endregion
    }
}
