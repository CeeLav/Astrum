using UnityEngine;
using Astrum.View.Core;
using Astrum.View.Managers;
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
            var entityConfig = OwnerEntity.EntityConfig;
            var modelConfig = OwnerEntity.ModelConfig;
            
            // 从 ModelConfig 获取模型路径
            string modelPath = null;
            if (modelConfig != null && modelConfig is cfg.Entity.EntityModelTable modelTable)
            {
                modelPath = modelTable.ModelPath;
            }
            
            if (string.IsNullOrEmpty(modelPath))
            {
                // 无模型路径，跳过
                return;
            }
            
            // 可在此初始化模型资源
            var prefab = ResourceManager.Instance.LoadResource<GameObject>(modelPath);
            Initialize(prefab);
        }

        public void Initialize(GameObject modelPrefab)
        {
            if (_ownerEntityView == null)
                return;
            
            if (_modelObject != null)
                UnityEngine.GameObject.Destroy(_modelObject);

            _modelObject = UnityEngine.GameObject.Instantiate(modelPrefab);
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
