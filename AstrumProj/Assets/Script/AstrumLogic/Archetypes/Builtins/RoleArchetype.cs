using System;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Capabilities;
using Astrum.LogicCore.Core;
using Astrum.CommonBase;

namespace Astrum.LogicCore.Archetypes
{
    [Archetype("Role", "BaseUnit", "Combat", "Controllable")]
    public class RoleArchetype : Archetype
    {
        private static readonly Type[] _comps = 
        {
            typeof(RoleInfoComponent),

        };
        
        private static readonly Type[] _caps =
        {
            
        };

        public override Type[] Components => _comps;
        public override Type[] Capabilities => _caps;

        // 生命周期钩子示例（可选重写）
        
        public override void OnBeforeComponentsAttach(Entity entity, World world)
        {
            // 组件挂载前的逻辑
            // 例如：初始化某些字段、记录日志等
            ASLogger.Instance.Debug($"Role Archetype: 实体 {entity.UniqueId} 准备挂载组件");
        }

        public override void OnAfterComponentsAttach(Entity entity, World world)
        {
            // 组件挂载后的逻辑
            // 例如：根据组件数据进行初始化配置
            ASLogger.Instance.Debug($"Role Archetype: 实体 {entity.UniqueId} 组件挂载完成");
        }

        public override void OnBeforeCapabilitiesAttach(Entity entity, World world)
        {
            // 能力挂载前的逻辑
            ASLogger.Instance.Debug($"Role Archetype: 实体 {entity.UniqueId} 准备挂载能力");
        }

        public override void OnAfterCapabilitiesAttach(Entity entity, World world)
        {
            // 能力挂载后，实体完全创建完成
            // 这里可以做最终的初始化工作
            ASLogger.Instance.Debug($"Role Archetype: 实体 {entity.UniqueId} 创建完成");
        }

        public override void OnEntityDestroy(Entity entity, World world)
        {
            // 实体销毁时的清理逻辑
            // 例如：清理资源、保存数据等
            ASLogger.Instance.Debug($"Role Archetype: 实体 {entity.UniqueId} 准备销毁");
        }
    }
}


