using Astrum.LogicCore.SkillSystem;
using TrueSync;
using UnityEngine;

namespace Astrum.LogicCore.Events
{
    /// <summary>
    /// 技能逻辑请求生成抛射物的事件（SkillExecutorCapability → ProjectileSpawnCapability）
    /// </summary>
    public struct ProjectileSpawnRequestEvent : IEvent
    {
        public long CasterEntityId;
        public int SkillEffectId;
        public string EffectParamsJson;
        public TriggerFrameInfo TriggerInfo;
        public TSVector SpawnPosition;
        public TSVector SpawnDirection;
        public string SocketName;
        public Vector3 SocketOffset;

        /// <summary>
        /// 是否在 Capability 未激活时也触发
        /// </summary>
        public bool TriggerWhenInactive { get; set; }
    }
}
