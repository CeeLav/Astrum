using MemoryPack;

namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// BaseComponent 的 MemoryPack 序列化特性定义
    /// </summary>
    [MemoryPackUnion(0, typeof(TransComponent))]
    [MemoryPackUnion(1, typeof(MovementComponent))]
    [MemoryPackUnion(2, typeof(Astrum.LogicCore.FrameSync.LSInputComponent))]
    [MemoryPackUnion(3, typeof(ActionComponent))]
    [MemoryPackUnion(4, typeof(RoleInfoComponent))]
    [MemoryPackUnion(5, typeof(SkillComponent))]
    [MemoryPackUnion(6, typeof(CollisionComponent))]
    [MemoryPackUnion(7, typeof(BaseStatsComponent))]
    [MemoryPackUnion(8, typeof(DerivedStatsComponent))]
    [MemoryPackUnion(9, typeof(DynamicStatsComponent))]
    [MemoryPackUnion(10, typeof(StateComponent))]
    [MemoryPackUnion(11, typeof(BuffComponent))]
    [MemoryPackUnion(12, typeof(LevelComponent))]
    [MemoryPackUnion(13, typeof(GrowthComponent))]
    [MemoryPackUnion(14, typeof(AIStateMachineComponent))]
    [MemoryPackUnion(15, typeof(MonsterInfoComponent))]
    [MemoryPackable]
    public abstract partial class BaseComponent
    {
        // MemoryPack 序列化相关的特性在此文件中定义
        // 具体的组件逻辑在 BaseComponent.cs 中定义
    }
}