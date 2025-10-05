using System;      
using Astrum.LogicCore.Components;
using TrueSync;


namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 单位类，继承自Entity，代表游戏中的可移动单位
    /// </summary>
    public class Unit : Entity
    {
        private static readonly Type[] _requiredComponents = new Type[]
        {
            typeof(Components.PositionComponent),
            typeof(Components.MovementComponent),
            typeof(Components.HealthComponent),
            typeof(FrameSync.LSInputComponent),
            typeof(Components.ActionComponent)
        };

        /// <summary>
        /// 获取Unit需要的组件类型列表（静态缓存，避免重复分配）
        /// </summary>
        /// <returns>组件类型列表</returns>
        public override Type[] GetRequiredComponentTypes()
        {
            return _requiredComponents;
        }

        private static readonly Type[] _requiredCapabilities = new Type[]
        {
            typeof(Capabilities.MovementCapability),
            typeof(Capabilities.ActionCapability),
        };

        /// <summary>
        /// 获取Unit需要的默认能力类型列表（静态缓存，避免重复分配）
        /// </summary>
        /// <returns>能力类型列表</returns>
        public override Type[] GetRequiredCapabilityTypes()
        {
            return _requiredCapabilities;
        }
    }
} 