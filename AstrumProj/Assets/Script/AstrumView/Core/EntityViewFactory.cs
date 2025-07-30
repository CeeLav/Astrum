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
        public EntityView CreateEntityView(string entityType, long entityId, Stage stage)
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
                    entityView.Initialize(entityId, stage);
                    
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