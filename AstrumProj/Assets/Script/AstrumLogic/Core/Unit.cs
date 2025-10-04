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

        /// <summary>
        /// 设置Unit的初始位置
        /// </summary>
        /// <param name="position">初始位置（TSVector）</param>
        public void SetInitialPosition(TSVector position)
        {
            var positionComponent = GetComponent<PositionComponent>();
            if (positionComponent != null)
            {
                positionComponent.SetPosition(position.x, position.y, position.z);
            }
        }

        /// <summary>
        /// 设置Unit的初始位置（重载，FP 参数）
        /// </summary>
        public void SetInitialPosition(FP x, FP y, FP z)
        {
            SetInitialPosition(new TSVector(x, y, z));
        }
    }
} 