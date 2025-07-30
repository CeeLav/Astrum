using UnityEngine;
using System;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.View.Core;

namespace Astrum.View.Components
{
    /// <summary>
    /// 视图组件基类 - 为EntityView提供特定的视觉功能
    /// </summary>
    public abstract class ViewComponent
    {
        // 视图组件设置
        protected int _componentId;
        protected bool _isEnabled = true;
        
        // 所属实体视图
        protected EntityView _ownerEntityView;
        
        // Unity组件引用
        protected GameObject _gameObject;
        protected Transform _transform;
        
        // 公共属性
        public int ComponentId => _componentId;
        public bool IsEnabled => _isEnabled;
        public EntityView OwnerEntityView 
        { 
            get => _ownerEntityView;
            set => _ownerEntityView = value;
        }

        public Entity OwnerEntity
        {
            get => _ownerEntityView.OwnerEntity;
        }
        
        public GameObject GameObject => _gameObject;
        public Transform Transform => _transform;
        
        // 事件
        public event Action<ViewComponent> OnComponentInitialized;
        public event Action<ViewComponent> OnComponentDestroyed;
        public event Action<ViewComponent, bool> OnEnabledChanged;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        protected ViewComponent()
        {
            _componentId = UnityEngine.Random.Range(1000, 9999);
        }
        
        /// <summary>
        /// 初始化组件
        /// </summary>
        public virtual void Initialize()
        {
            if (_componentId == 0)
            {
                _componentId = UnityEngine.Random.Range(1000, 9999);
            }
            
            Debug.Log($"ViewComponent: 初始化组件，ID: {_componentId}，类型: {GetType().Name}");
            
            try
            {
                // 设置Unity引用
                if (_ownerEntityView != null)
                {
                    _gameObject = _ownerEntityView.GameObject;
                    _transform = _ownerEntityView.Transform;
                }
                
                // 子类特定的初始化
                OnInitialize();
                
                OnComponentInitialized?.Invoke(this);
                
                Debug.Log($"ViewComponent: 组件初始化完成，ID: {_componentId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ViewComponent: 组件初始化失败，ID: {_componentId} - {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 更新组件
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        public virtual void Update(float deltaTime)
        {
            if (!_isEnabled) return;
            
            // 子类特定的更新逻辑
            OnUpdate(deltaTime);
        }
        
        /// <summary>
        /// 销毁组件
        /// </summary>
        public virtual void Destroy()
        {
            Debug.Log($"ViewComponent: 销毁组件，ID: {_componentId}，类型: {GetType().Name}");
            
            // 子类特定的销毁逻辑
            OnDestroy();
            
            OnComponentDestroyed?.Invoke(this);
        }
        
        /// <summary>
        /// 设置启用状态
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public virtual void SetEnabled(bool enabled)
        {
            if (_isEnabled == enabled) return;
            
            _isEnabled = enabled;
            
            OnEnabledChanged?.Invoke(this, enabled);
            Debug.Log($"ViewComponent: 设置启用状态，ID: {_componentId}，启用: {enabled}");
        }

        /// <summary>
        /// 获取组件类型名称
        /// </summary>
        /// <returns>组件类型名称</returns>
        public virtual string GetComponentTypeName()
        {
            return GetType().Name;
        }
        
        /// <summary>
        /// 获取组件状态信息
        /// </summary>
        /// <returns>状态信息</returns>
        public virtual string GetComponentStatus()
        {
            return $"ID: {_componentId}, 类型: {GetComponentTypeName()}, 启用: {_isEnabled}";
        }
        
        // 抽象方法 - 子类必须实现
        protected abstract void OnInitialize();
        protected abstract void OnUpdate(float deltaTime);
        protected abstract void OnDestroy();
        protected abstract void OnSyncData(object data);
    }
}
