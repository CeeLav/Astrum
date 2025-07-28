using UnityEngine;
using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.View.Components;

namespace Astrum.View.Core
{
    /// <summary>
    /// 实体视图基类
    /// 对应LogicCore中的Entity，负责实体的视觉表现
    /// </summary>
    public abstract class EntityView
    {
        // 实体视图设置
        protected long _entityId = 0;
        protected string _entityType = "";
        protected bool _isVisible = true;
        protected bool _isActive = true;
        
        // 组件管理
        protected List<ViewComponent> _viewComponents = new List<ViewComponent>();
        protected DateTime _lastSyncTime = DateTime.MinValue;
        
        // Unity GameObject引用
        protected GameObject _gameObject;
        protected Transform _transform;
        
        // 公共属�?
        public long EntityId => _entityId;
        public string EntityType => _entityType;
        public bool IsVisible => _isVisible;
        public bool IsActive => _isActive;
        public GameObject GameObject => _gameObject;
        public Transform Transform => _transform;
        public List<ViewComponent> ViewComponents => _viewComponents;
        public DateTime LastSyncTime => _lastSyncTime;
        
        // 事件
        public event Action<EntityView> OnEntityViewInitialized;
        public event Action<EntityView> OnEntityViewDestroyed;
        public event Action<EntityView, bool> OnVisibilityChanged;
        public event Action<EntityView, bool> OnActiveStateChanged;
        
        /// <summary>
        /// 构造函�?
        /// </summary>
        /// <param name="entityType">实体类型</param>
        protected EntityView(string entityType = "")
        {
            _entityType = entityType;
        }
        
        /// <summary>
        /// 初始化实体视�?
        /// </summary>
        /// <param name="entityId">实体ID</param>
        public virtual void Initialize(long entityId)
        {
            if (_entityId != 0)
            {
                ASLogger.Instance.Warning($"EntityView: 实体视图已经初始化，ID: {_entityId}");
                return;
            }
            
            _entityId = entityId;
            
            ASLogger.Instance.Info($"EntityView: 初始化实体视图，ID: {entityId}");
            
            try
            {
                // 创建Unity GameObject
                CreateGameObject();
                
                // 子类特定的初始化
                OnInitialize();
                
                OnEntityViewInitialized?.Invoke(this);
                
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
            if (!_isActive) return;
            
            // 更新所有视图组件
            foreach (var component in _viewComponents)
            {
                if (component.IsEnabled)
                {
                    component.Update(deltaTime);
                }
            }
            
            // 子类特定的更新逻辑
            OnUpdateView(deltaTime);
        }
        
        /// <summary>
        /// Unity Update 方法包装
        /// </summary>
        public virtual void Update()
        {
            UpdateView(Time.deltaTime);
        }
        
        /// <summary>
        /// 与逻辑实体同步
        /// </summary>
        /// <param name="entityData">实体数据</param>
        public virtual void SyncWithEntity(EntityData entityData)
        {
            if (entityData == null)
            {
                ASLogger.Instance.Warning($"EntityView: 实体数据为空，ID: {_entityId}");
                return;
            }
            
            // 更新基础属�?
            if (entityData.EntityId != _entityId)
            {
                ASLogger.Instance.Warning($"EntityView: 实体ID不匹配，期望: {_entityId}，实�? {entityData.EntityId}");
                return;
            }
            
            // 更新变换
            if (_transform != null)
            {
                _transform.position = entityData.Position;
                _transform.rotation = entityData.Rotation;
                _transform.localScale = entityData.Scale;
            }
            
            // 更新实体类型
            if (!string.IsNullOrEmpty(entityData.EntityType))
            {
                _entityType = entityData.EntityType;
            }
            
            // 同步组件数据
            SyncComponentData(entityData.ComponentData);
            
            _lastSyncTime = DateTime.Now;
            
            // 子类特定的同步逻辑
            OnSyncWithEntity(entityData);
        }
        
        /// <summary>
        /// 设置可见�?
        /// </summary>
        /// <param name="visible">是否可见</param>
        public virtual void SetVisible(bool visible)
        {
            if (_isVisible == visible) return;
            
            _isVisible = visible;
            
            if (_gameObject != null)
            {
                // 设置所有渲染器的可见�?
                var renderers = _gameObject.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.enabled = visible;
                }
                
                // 设置所有Canvas的可见�?
                var canvases = _gameObject.GetComponentsInChildren<Canvas>();
                foreach (var canvas in canvases)
                {
                    canvas.enabled = visible;
                }
            }
            
            OnVisibilityChanged?.Invoke(this, visible);
            ASLogger.Instance.Info($"EntityView: 设置可见性，ID: {_entityId}，可�? {visible}");
        }
        
        /// <summary>
        /// 设置激活状�?
        /// </summary>
        /// <param name="active">是否激�?/param>
        public virtual void SetActive(bool active)
        {
            if (_isActive == active) return;
            
            _isActive = active;
            if (_gameObject != null)
            {
                _gameObject.SetActive(active);
            }
            
            OnActiveStateChanged?.Invoke(this, active);
            ASLogger.Instance.Info($"EntityView: 设置激活状态，ID: {_entityId}，激�? {active}");
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
            
            // 销毁Unity GameObject
            if (_gameObject != null)
            {
                UnityEngine.Object.Destroy(_gameObject);
                _gameObject = null;
                _transform = null;
            }
            
            OnEntityViewDestroyed?.Invoke(this);
        }
        
        /// <summary>
        /// 同步组件数据
        /// </summary>
        /// <param name="componentData">组件数据</param>
        protected virtual void SyncComponentData(Dictionary<string, object> componentData)
        {
            if (componentData == null) return;
            
            foreach (var kvp in componentData)
            {
                string componentName = kvp.Key;
                object data = kvp.Value;
                
                // 查找对应的视图组件并同步数据
                foreach (var component in _viewComponents)
                {
                    if (component.GetType().Name == componentName)
                    {
                        component.SyncData(data);
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取实体视图的根变换
        /// </summary>
        /// <returns>根变�?/returns>
        public virtual Transform GetRootTransform()
        {
            return _transform;
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
        /// 获取实体视图的本地缩�?
        /// </summary>
        /// <returns>本地缩放</returns>
        public virtual Vector3 GetLocalScale()
        {
            return _transform != null ? _transform.localScale : Vector3.one;
        }
        
        /// <summary>
        /// 设置实体视图的本地缩�?
        /// </summary>
        /// <param name="scale">本地缩放</param>
        public virtual void SetLocalScale(Vector3 scale)
        {
            if (_transform != null)
            {
                _transform.localScale = scale;
            }
        }
        
        /// <summary>
        /// 创建Unity GameObject
        /// </summary>
        protected virtual void CreateGameObject()
        {
            _gameObject = new GameObject($"EntityView_{_entityId}_{_entityType}");
            _transform = _gameObject.transform;
        }
        
        // 抽象方法 - 子类必须实现
        protected abstract void OnInitialize();
        protected abstract void OnUpdateView(float deltaTime);
        protected abstract void OnSyncWithEntity(EntityData entityData);
    }
}
