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
                entityView = new EntityView();
                
                // 初始化EntityView（不包含ViewComponents装配）
                if (entityView != null)
                {
                    entityView.Initialize(entityId, stage);
                    
                    // 获取Entity并装配ViewComponents
                    AssembleViewComponents(entityView, stage);
                    
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
        /// 通过Entity的Archetype装配ViewComponents
        /// </summary>
        private void AssembleViewComponents(EntityView entityView, Stage stage)
        {
            if (entityView == null || stage?.Room?.MainWorld == null)
            {
                ASLogger.Instance.Warning($"EntityViewFactory: 无法装配ViewComponents，EntityView或Stage为空");
                return;
            }
            
            try
            {
                // 获取Entity
                var entity = stage.Room.MainWorld.GetEntity(entityView.EntityId);
                if (entity == null)
                {
                    ASLogger.Instance.Warning($"EntityViewFactory: 找不到实体 - ID:{entityView.EntityId}");
                    return;
                }
                
                // 获取所需的ViewComponent类型
                var viewComponentTypes = GetRequiredViewComponentTypes(entity);
                if (viewComponentTypes == null || viewComponentTypes.Length == 0)
                {
                    if (enableLogging)
                        ASLogger.Instance.Info($"EntityViewFactory: 实体 {entityView.EntityId} 没有需要装配的ViewComponents");
                    return;
                }
                
                // 调用EntityView的BuildViewComponents装配
                entityView.BuildViewComponents(viewComponentTypes);
                
                if (enableLogging)
                    ASLogger.Instance.Info($"EntityViewFactory: 为实体 {entityView.EntityId} 装配了 {viewComponentTypes.Length} 个ViewComponents");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"EntityViewFactory: 装配ViewComponents失败 - ID:{entityView.EntityId}, 错误:{ex}");
            }
        }
        
        /// <summary>
        /// 通过Entity获取所需的ViewComponent类型列表
        /// </summary>
        private Type[] GetRequiredViewComponentTypes(Entity entity)
        {
            if (entity?.EntityConfig == null)
            {
                return Array.Empty<Type>();
            }
            
            var archetypeName = entity.EntityConfig.ArchetypeName;
            if (string.IsNullOrEmpty(archetypeName))
            {
                if (enableLogging)
                    ASLogger.Instance.Info($"EntityViewFactory: 实体 {entity.UniqueId} 没有指定ArchetypeName");
                return Array.Empty<Type>();
            }
            
            // 从ViewArchetypeManager获取ViewComponent类型
            if (ViewArchetypeManager.Instance.TryGetComponents(archetypeName, out var viewComponentTypes))
            {
                return viewComponentTypes;
            }
            
            return Array.Empty<Type>();
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