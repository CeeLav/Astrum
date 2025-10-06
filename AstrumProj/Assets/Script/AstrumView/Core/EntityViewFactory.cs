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
        /// <param name="entityId">实体ID</param>
        /// <returns>创建的EntityView</returns>
        public EntityView CreateEntityView(long entityId, Stage stage)
        {
            if (enableLogging)
                ASLogger.Instance.Info($"EntityViewFactory: 创建EntityView - ID:{entityId}");
            
            EntityView entityView = null;
            
            try
            {
                // 创建默认的 EntityView（后续通过 Archetype 装配 ViewComponents）
                entityView = new UnitView();
                
                // 初始化EntityView
                if (entityView != null)
                {
                    entityView.Initialize(entityId, stage);
                    
                    if (enableLogging)
                        ASLogger.Instance.Info($"EntityViewFactory: EntityView创建成功 - ID:{entityId}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"EntityViewFactory: 创建EntityView失败 - ID:{entityId}, 错误:{ex.Message}");
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