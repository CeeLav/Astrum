using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.View.Archetypes;
using Astrum.View.Components;

namespace Astrum.View.Core
{
    /// <summary>
    /// EntityView工厂 - 负责根据实体类型创建对应的EntityView
    /// </summary>
    public class EntityViewFactory : Singleton<EntityViewFactory>
    {
        [Header("工厂设置")]
        private bool enableLogging = true;
        
        /// <summary>
        /// 创建 EntityView 并装配 ViewComponents
        /// </summary>
        public EntityView CreateEntityView(long entityId, Stage stage)
        {
            if (enableLogging)
                ASLogger.Instance.Info($"EntityViewFactory: 创建EntityView - ID:{entityId}");
            
            try
            {
                var entityView = new EntityView();
                entityView.Initialize(entityId, stage);
                AssembleViewComponents(entityView, stage);
                
                if (enableLogging)
                    ASLogger.Instance.Info($"EntityViewFactory: EntityView创建成功 - ID:{entityId}");
                
                return entityView;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"EntityViewFactory: 创建EntityView失败 - ID:{entityId}, 错误:{ex.Message}");
                return null;
            }
        }
        
        private void AssembleViewComponents(EntityView entityView, Stage stage)
        {
            if (entityView == null || stage?.Room?.MainWorld == null) return;
            
            try
            {
                var entity = stage.Room.MainWorld.GetEntity(entityView.EntityId);
                if (entity == null) return;
                
                var viewComponentTypes = GetRequiredViewComponentTypes(entity);
                if (viewComponentTypes.Length == 0)
                {
                    if (enableLogging)
                        ASLogger.Instance.Info($"EntityViewFactory: 实体 {entityView.EntityId} 没有ViewComponents");
                    return;
                }
                
                entityView.BuildViewComponents(viewComponentTypes);
                
                if (enableLogging)
                    ASLogger.Instance.Info($"EntityViewFactory: 装配了 {viewComponentTypes.Length} 个ViewComponents - ID:{entityView.EntityId}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"EntityViewFactory: 装配ViewComponents失败 - ID:{entityView.EntityId}, 错误:{ex}");
            }
        }
        
        private Type[] GetRequiredViewComponentTypes(Entity entity)
        {
            var archetypeName = entity.ArchetypeName;
            if (string.IsNullOrEmpty(archetypeName)) return Array.Empty<Type>();
            
            ViewArchetypeManager.Instance.TryGetComponents(archetypeName, out var viewComponentTypes);
            return viewComponentTypes ?? Array.Empty<Type>();
        }
        
        /// <summary>
        /// 销毁 EntityView
        /// </summary>
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