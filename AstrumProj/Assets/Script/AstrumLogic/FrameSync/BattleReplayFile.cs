using System;
using System.Collections.Generic;
using MemoryPack;

namespace Astrum.LogicCore.FrameSync
{
    /// <summary>
    /// 战斗回放文件数据结构
    /// </summary>
    [MemoryPackable]
    public partial class BattleReplayFile
    {
        public int Version { get; set; } = 1;              // 回放格式版本，支持后续兼容
        public string RoomId { get; set; } = "";
        public int TickRate { get; set; }                  // 帧率，保证播放节奏一致
        public int TotalFrames { get; set; }               // 战斗结束时的最终帧
        public long StartTimestamp { get; set; }           // 战斗起始 UTC 时间
        public int RandomSeed { get; set; }               // 该战斗使用的随机种子（确保确定性）

        public List<ReplayPlayerInfo> Players { get; set; } = new();      // 玩家列表（名称/职业等展示用）
        public List<ReplaySnapshot> Snapshots { get; set; } = new();      // 世界快照 S(n)
        public List<ReplayFrameInputs> FrameInputs { get; set; } = new(); // 帧输入序列 H(n)
    }

    /// <summary>
    /// 回放玩家信息
    /// </summary>
    [MemoryPackable]
    public partial class ReplayPlayerInfo
    {
        public string UserId { get; set; } = "";
        public long PlayerId { get; set; }
        public string DisplayName { get; set; } = "";
    }

    /// <summary>
    /// 回放快照
    /// </summary>
    [MemoryPackable]
    public partial class ReplaySnapshot
    {
        public int Frame { get; set; }                // 快照对应的逻辑帧号
        public byte[] WorldData { get; set; } = Array.Empty<byte>();         // GZip(MemoryPack(World))，与 StateSnapshotManager 保持一致
    }

    /// <summary>
    /// 回放帧输入
    /// </summary>
    [MemoryPackable]
    public partial class ReplayFrameInputs
    {
        public int Frame { get; set; }                // 帧号
        public byte[] InputsData { get; set; } = Array.Empty<byte>();        // 对应 OneFrameInputs 的序列化结果（Proto/MemoryPack 均可）
    }
}

