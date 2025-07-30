using System;
using UnityEngine;
using Astrum.CommonBase;
using Astrum.View.Components;
using Astrum.View.EntityViews;

namespace Astrum.View.Core
{
    /// <summary>
    /// EntityView工厂 - 负责根据实体类型创建对应的EntityView
    /// </summary>
    public class EntityViewFactory : Singleton<EntityViewFactory>
    {
        [Header("工厂设置")]
        [SerializeField] private bool enableLogging = true;
        
        /// <summary>
        /// 根据实体类型创建EntityView
        /// </summary>
        /// <param name="entityType">实体类型</param>
        /// <param name="entityId">实体ID</param>
        /// <returns>创建的EntityView</returns>
        public EntityView CreateEntityView(string entityType, long entityId)
        {
            if (enableLogging)
                ASLogger.Instance.Info($"EntityViewFactory: 创建EntityView - 类型:{entityType}, ID:{entityId}");
            
            EntityView entityView = null;
            
            try
            {
                // 根据实体类型创建对应的EntityView
                switch (entityType.ToLower())
                {
                    case "player":
                    case "unit":
                        entityView = new UnitView();
                        break;
                    /*
                    case "enemy":
                        entityView = new EnemyEntityView();
                        break;
                    case "building":
                        entityView = new BuildingEntityView();
                        break;
                    case "projectile":
                        entityView = new ProjectileEntityView();
                        break;
                    case "item":
                        entityView = new ItemEntityView();
                        break;
                    case "environment":
                        entityView = new EnvironmentEntityView();
                        break;
                    default:
                        entityView = new DefaultEntityView();
                        break;*/
                }
                
                // 初始化EntityView
                if (entityView != null)
                {
                    entityView.Initialize(entityId);
                    
                    if (enableLogging)
                        ASLogger.Instance.Info($"EntityViewFactory: EntityView创建成功 - 类型:{entityType}, ID:{entityId}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"EntityViewFactory: 创建EntityView失败 - 类型:{entityType}, ID:{entityId}, 错误:{ex.Message}");
            }
            
            return entityView;
        }
        
        /// <summary>
        /// 根据实体类型和组件信息创建EntityView
        /// </summary>
        /// <param name="entityType">实体类型</param>
        /// <param name="entityId">实体ID</param>
        /// <param name="componentTypes">组件类型列表</param>
        /// <returns>创建的EntityView</returns>
        public EntityView CreateEntityViewWithComponents(string entityType, long entityId, string[] componentTypes)
        {
            if (enableLogging)
                ASLogger.Instance.Info($"EntityViewFactory: 创建带组件的EntityView - 类型:{entityType}, ID:{entityId}");
            
            var entityView = CreateEntityView(entityType, entityId);
            
            if (entityView != null && componentTypes != null)
            {
                // 根据组件类型添加对应的视图组件
                foreach (var componentType in componentTypes)
                {
                    AddViewComponent(entityView, componentType);
                }
            }
            
            return entityView;
        }
        
        /// <summary>
        /// 为EntityView添加视图组件
        /// </summary>
        /// <param name="entityView">实体视图</param>
        /// <param name="componentType">组件类型</param>
        private void AddViewComponent(EntityView entityView, string componentType)
        {
            try
            {
                ViewComponent viewComponent = null;
                
                switch (componentType.ToLower())
                {
                    /*
                    case "movement":
                        viewComponent = new MovementViewComponent();
                        break;
                    case "health":
                        viewComponent = new HealthViewComponent();
                        break;
                    case "animation":
                        viewComponent = new AnimationViewComponent();
                        break;

                    case "particle":
                        viewComponent = new ParticleViewComponent();
                        break;
                    case "sound":
                        viewComponent = new SoundViewComponent();
                        break;
                    case "ui":
                        viewComponent = new UIViewComponent();
                        break;
                    default:
                        if (enableLogging)
                            ASLogger.Instance.Warning($"EntityViewFactory: 未知的组件类型 - {componentType}");
                        break;*/
                }
                
                if (viewComponent != null)
                {
                    entityView.AddViewComponent(viewComponent);
                    
                    if (enableLogging)
                        ASLogger.Instance.Info($"EntityViewFactory: 添加视图组件成功 - 组件:{componentType}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"EntityViewFactory: 添加视图组件失败 - 组件:{componentType}, 错误:{ex.Message}");
            }
        }
        
        /// <summary>
        /// 销毁EntityView
        /// </summary>
        /// <param name="entityView">要销毁的EntityView</param>
        public void DestroyEntityView(EntityView entityView)
        {
            if (entityView != null)
            {
                if (enableLogging)
                    ASLogger.Instance.Info($"EntityViewFactory: 销毁EntityView - ID:{entityView.EntityId}");
                
                entityView.Destroy();
            }
        }
    }
} 