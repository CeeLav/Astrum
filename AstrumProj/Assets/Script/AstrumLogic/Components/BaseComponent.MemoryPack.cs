using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// BaseComponent 的 MemoryPack 序列化特性定义
    /// </summary>
    [MemoryPackUnion(0, typeof(HealthComponent))]
    [MemoryPackUnion(1, typeof(PositionComponent))]
    [MemoryPackUnion(2, typeof(MovementComponent))]
    [MemoryPackUnion(3, typeof(Astrum.LogicCore.FrameSync.LSInputComponent))]
    [MemoryPackUnion(4, typeof(ActionComponent))]
    [MemoryPackUnion(5, typeof(RoleInfoComponent))]
    [MemoryPackUnion(6, typeof(SkillComponent))]
    [MemoryPackable]
    public abstract partial class BaseComponent
    {
        // MemoryPack 序列化相关的特性在此文件中定义
        // 具体的组件逻辑在 BaseComponent.cs 中定义
    }
}