using MemoryPack;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// Capability 的 MemoryPack 序列化特性定义
    /// </summary>
    [MemoryPackUnion(0, typeof(MovementCapabilityOld))]  // 旧架构，保留用于兼容
    [MemoryPackUnion(1, typeof(ActionCapabilityOld))]  // 旧架构，保留用于兼容
    [MemoryPackUnion(2, typeof(SkillCapabilityOld))]  // 旧架构，保留用于兼容
    [MemoryPackUnion(3, typeof(SkillExecutorCapabilityOld))]  // 旧架构，保留用于兼容
    [MemoryPackUnion(4, typeof(AIFSMCapabilityOld))]  // 旧架构，保留用于兼容
    [MemoryPackUnion(5, typeof(IdleStateCapabilityOld))]  // 旧架构，保留用于兼容
    [MemoryPackUnion(6, typeof(MoveStateCapabilityOld))]  // 旧架构，保留用于兼容
    [MemoryPackUnion(7, typeof(BattleStateCapabilityOld))]  // 旧架构，保留用于兼容
    [MemoryPackUnion(8, typeof(SkillDisplacementCapabilityOld))]  // 旧架构，保留用于兼容
    [MemoryPackable]
    public abstract partial class Capability
    {
        // MemoryPack 序列化相关的特性在此文件中定义
        // 具体的能力逻辑在 Capability.cs 中定义
    }
}