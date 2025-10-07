using MemoryPack;

namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// Capability 的 MemoryPack 序列化特性定义
    /// </summary>
    [MemoryPackUnion(0, typeof(MovementCapability))]
    [MemoryPackUnion(1, typeof(ActionCapability))]
    [MemoryPackUnion(2, typeof(SkillCapability))]
    [MemoryPackable]
    public abstract partial class Capability
    {
        // MemoryPack 序列化相关的特性在此文件中定义
        // 具体的能力逻辑在 Capability.cs 中定义
    }
}