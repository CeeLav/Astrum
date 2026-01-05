using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.View.Archetypes;
using Astrum.View.Components;
using cfg;

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
            // EntityView创建日志已移除以减少干扰（错误日志保留）
            
            try
            {
                var entityView = new EntityView();
                entityView.Initialize(entityId, stage);
                AssembleViewComponents(entityView, stage);
                
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
                
                // 1. 装配主原型的 ViewComponents
                var viewComponentTypes = GetRequiredViewComponentTypes(entity);
                if (viewComponentTypes.Length > 0)
                {
                    entityView.BuildViewComponents(viewComponentTypes);
                    // ViewComponents装配日志已移除以减少干扰
                }
                
                // 2. 同步 Entity 的活跃子原型
                var activeSubArchetypes = entity.ListActiveSubArchetypes();
                if (activeSubArchetypes != null && activeSubArchetypes.Count > 0)
                {
                    foreach (var subArchetype in activeSubArchetypes)
                    {
                        entityView.AttachSubArchetype(subArchetype);
                    }
                    // 子原型同步日志已移除以减少干扰
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"EntityViewFactory: 装配ViewComponents失败 - ID:{entityView.EntityId}, 错误:{ex}");
            }
        }
        
        private Type[] GetRequiredViewComponentTypes(Entity entity)
        {
            var archetype = entity.Archetype;
            if (archetype == default(EArchetype)) return Array.Empty<Type>();
            
            ViewArchetypeManager.Instance.TryGetComponents(archetype, out var viewComponentTypes);
            return viewComponentTypes ?? Array.Empty<Type>();
        }
        
        /// <summary>
        /// 销毁 EntityView
        /// </summary>
        public void DestroyEntityView(EntityView entityView)
        {
            if (entityView != null)
            {
                // EntityView销毁日志已移除以减少干扰
                entityView.Destroy();
            }
        }
    }
} 