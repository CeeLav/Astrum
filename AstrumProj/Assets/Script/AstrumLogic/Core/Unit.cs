using System;      
using Astrum.LogicCore.Components;


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
            typeof(FrameSync.LSInputComponent)
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
            typeof(Capabilities.MovementCapability)
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
        /// <param name="position">初始位置</param>
        public void SetInitialPosition(Vector3 position)
        {
            var positionComponent = GetComponent<PositionComponent>();
            if (positionComponent != null)
            {
                positionComponent.SetPosition(position.X, position.Y, position.Z);
            }
        }

        /// <summary>
        /// 设置Unit的初始位置（重载方法，接受三个float参数）
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="z">Z坐标</param>
        public void SetInitialPosition(float x, float y, float z)
        {
            SetInitialPosition(new Vector3(x, y, z));
        }
    }
} 