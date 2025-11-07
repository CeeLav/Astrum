using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Timeline;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 负责解析触发帧并将命中、击退、特效请求分发到木桩目标
    /// （当前为预处理骨架，后续可扩展具体命中计算）
    /// </summary>
    public static class HitDummyInteractionEvaluator
    {
        public class HitDummyFrameResult
        {
            public TimelineEvent SourceEvent;
            public HitDummyTargetMarker Target;
            public float KnockbackDistance;
            public Vector3 KnockbackDirection;
            public string VfxResourcePath;
            public Vector3 VfxOffset;
        }

        public static List<HitDummyFrameResult> ProcessFrame(
            int frame,
            IReadOnlyList<TimelineEvent> events,
            IReadOnlyList<HitDummyTargetMarker> markers)
        {
            var results = new List<HitDummyFrameResult>();

            if (events == null || markers == null || markers.Count == 0)
            {
                return results;
            }

            foreach (var timelineEvent in events)
            {
                if (timelineEvent == null) continue;
                if (frame < timelineEvent.StartFrame || frame > timelineEvent.EndFrame) continue;

                if (timelineEvent.TrackType != "SkillEffect" && timelineEvent.TrackType != "VFX")
                {
                    continue;
                }

                foreach (var marker in markers)
                {
                    if (marker == null) continue;

                    // 当前阶段：直接记录事件，后续将根据碰撞体积与木桩位置计算精准命中
                    results.Add(new HitDummyFrameResult
                    {
                        SourceEvent = timelineEvent,
                        Target = marker,
                        KnockbackDistance = 0f,
                        KnockbackDirection = Vector3.zero,
                        VfxResourcePath = ExtractVfxPath(timelineEvent),
                        VfxOffset = Vector3.zero
                    });
                }
            }

            return results;
        }

        private static string ExtractVfxPath(TimelineEvent timelineEvent)
        {
            if (timelineEvent.TrackType == "VFX")
            {
                var data = timelineEvent.GetEventData<Timeline.EventData.VFXEventData>();
                return data?.ResourcePath;
            }

            return null;
        }
    }
}


