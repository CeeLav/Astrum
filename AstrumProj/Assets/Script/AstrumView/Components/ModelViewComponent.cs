using UnityEngine;
using System;
using Astrum.View.Core;

namespace Astrum.View.Components
{
    /// <summary>
    /// ModelViewComponent 用于管理 EntityView 的模型表现
    /// </summary>
    public class ModelViewComponent : ViewComponent
    {
        private GameObject _modelObject;

        protected override void OnInitialize()
        {
            // 可在此初始化模型资源
            // 初始化一个立方体模型资源
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Initialize(cube);
            
        }

        public void Initialize(GameObject modelPrefab)
        {
            if (_ownerEntityView == null)
                return;
            
            if (_modelObject != null)
                UnityEngine.GameObject.Destroy(_modelObject);

            _modelObject = modelPrefab;//UnityEngine.GameObject.Instantiate(modelPrefab);
            if (_ownerEntityView.GameObject != null)
                _modelObject.transform.SetParent(_ownerEntityView.GameObject.transform, false);
            _transform = _modelObject.transform;
            _gameObject = _modelObject;
            _isEnabled = true;
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 可在此更新模型表现
        }

        public void SetActive(bool active)
        {
            if (_modelObject != null)
                _modelObject.SetActive(active);
            _isEnabled = active;
        }

        protected override void OnDestroy()
        {
            if (_modelObject != null)
                UnityEngine.GameObject.Destroy(_modelObject);
            _modelObject = null;
            _isEnabled = false;
        }

        public new void Destroy()
        {
            OnDestroy();
        }

        protected override void OnSyncData(object entity)
        {
            // 可在此同步模型数据
        }

        /// <summary>
        /// 获取当前模型对象
        /// </summary>
        public GameObject ModelObject => _modelObject;

    }
}
