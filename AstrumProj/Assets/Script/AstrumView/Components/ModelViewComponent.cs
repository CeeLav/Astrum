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
            if (entityConfig == null)
                return;
            
            var modelId = entityConfig.ModelId;
            
            // 无模型，跳过
            if (modelId == 0)
                return;
            
            // 从 BaseUnitModelTable 获取模型配置
            var modelConfig = Astrum.LogicCore.Managers.TableConfig.Instance.Tables.TbBaseUnitModelTable?.Get(modelId);
            if (modelConfig == null)
            {
                Debug.LogWarning($"ModelViewComponent: BaseUnitModelTable not found for modelId={modelId}");
                return;
            }
            
            var modelPath = modelConfig.ModelPath;
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
