using System;
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
        private const string LOG_PREFIX = "[HitDummyEvaluator]";
        private const bool DEBUG_LOG = true;

        public class HitDummyFrameResult
        {
            public TimelineEvent SourceEvent;
            public HitDummyTargetMarker Target;
            public float KnockbackDistance;
            public Vector3 KnockbackDirection;
            public string VfxResourcePath;
            public Vector3 VfxOffset;
            public int DirectionMode;
            public int EffectId;
            public float KnockbackDuration;
        }

        public static List<HitDummyFrameResult> ProcessFrame(
            int frame,
            IReadOnlyList<TimelineEvent> events,
            IReadOnlyList<HitDummyTargetMarker> markers,
            Transform casterTransform = null,
            int templateTargetSelector = 0)
        {
            var results = new List<HitDummyFrameResult>();

            if (DEBUG_LOG)
            {
                Debug.Log($"{LOG_PREFIX} Frame {frame} evaluate events={(events?.Count ?? 0)} markers={(markers?.Count ?? 0)}");
            }

            if (events == null || markers == null || markers.Count == 0)
            {
                return results;
            }

            Dictionary<int, Persistence.Mappings.SkillEffectTableData> cache = new Dictionary<int, Persistence.Mappings.SkillEffectTableData>();

            foreach (var timelineEvent in events)
            {
                if (timelineEvent == null) continue;
                if (frame < timelineEvent.StartFrame || frame > timelineEvent.EndFrame) continue;

                if (timelineEvent.TrackType != "SkillEffect" && timelineEvent.TrackType != "VFX")
                {
                    continue;
                }

                if (timelineEvent.TrackType == "SkillEffect")
                {
                    if (DEBUG_LOG)
                    {
                        Debug.Log($"{LOG_PREFIX} SkillEffect event {timelineEvent.EventId} frame={frame} range={timelineEvent.StartFrame}-{timelineEvent.EndFrame}");
                    }

                    var eventData = timelineEvent.GetEventData<Timeline.EventData.SkillEffectEventData>();
                    if (eventData == null || eventData.EffectIds == null || eventData.EffectIds.Count == 0)
                    {
                        if (DEBUG_LOG)
                        {
                            Debug.LogWarning($"{LOG_PREFIX} Event {timelineEvent.EventId} has no effect ids");
                        }
                        continue;
                    }

                    foreach (int effectId in eventData.EffectIds)
                    {
                        if (effectId <= 0) continue;

                        if (!cache.TryGetValue(effectId, out var effectConfig))
                        {
                            effectConfig = SkillEffectDataReader.GetSkillEffect(effectId);
                            cache[effectId] = effectConfig;
                            if (DEBUG_LOG)
                            {
                                Debug.Log($"{LOG_PREFIX} Load effect {effectId} success={(effectConfig != null)}");
                            }
                        }

                        if (effectConfig == null)
                        {
                            if (DEBUG_LOG)
                            {
                                Debug.LogWarning($"{LOG_PREFIX} Effect {effectId} not found");
                            }
                            continue;
                        }

                        if (DEBUG_LOG)
                        {
                            string type = effectConfig.EffectType ?? "<null>";
                            int count = effectConfig.IntParams?.Count ?? 0;
                            Debug.Log($"{LOG_PREFIX} Effect {effectId} type={type} intCount={count}");
                        }

                        if (!string.Equals(effectConfig.EffectType, "Knockback", StringComparison.OrdinalIgnoreCase))
                        {
                            if (DEBUG_LOG)
                            {
                                Debug.Log($"{LOG_PREFIX} Effect {effectId} type {effectConfig.EffectType} ignored");
                            }
                            continue;
                        }

                        var intParams = effectConfig.IntParams ?? new List<int>();
                        if (intParams.Count > 0 && templateTargetSelector != 0 && intParams[0] != templateTargetSelector)
                        {
                            if (DEBUG_LOG)
                            {
                                Debug.Log($"{LOG_PREFIX} Effect {effectId} target selector {intParams[0]} mismatches template target {templateTargetSelector}");
                            }
                            continue;
                        }
                        if (DEBUG_LOG)
                        {
                            Debug.Log($"{LOG_PREFIX} IntParams [{string.Join(",", intParams)}]");
                        }
                        int targetSelector = intParams.Count > 0 ? intParams[0] : 0;
                        if (targetSelector == 0 || targetSelector == 2)
                        {
                            // 自身或友军目标暂不命中木桩
                            if (DEBUG_LOG)
                            {
                                Debug.Log($"{LOG_PREFIX} Effect {effectId} target selector {targetSelector} skipped");
                            }
                            continue;
                        }

                        float distance = intParams.Count > 1 ? intParams[1] / 1000f : 0f;
                        if (distance <= 0f)
                        {
                            if (DEBUG_LOG)
                            {
                                Debug.LogWarning($"{LOG_PREFIX} Effect {effectId} distance {distance} <= 0");
                            }
                            continue;
                        }

                        float duration = intParams.Count > 2 ? intParams[2] / 1000f : 0f;
                        int directionMode = intParams.Count > 3 ? intParams[3] : 2;

                foreach (var marker in markers)
                        {
                            if (marker == null) continue;

                    Vector3 direction = ComputeDirection(casterTransform, marker.Transform, directionMode);

                            results.Add(new HitDummyFrameResult
                            {
                                SourceEvent = timelineEvent,
                                Target = marker,
                                KnockbackDistance = distance,
                                KnockbackDirection = direction,
                                KnockbackDuration = duration,
                                DirectionMode = directionMode,
                                EffectId = effectId,
                                VfxResourcePath = ExtractVfxPath(timelineEvent),
                                VfxOffset = Vector3.zero
                            });

                            if (DEBUG_LOG)
                            {
                        Debug.Log($"{LOG_PREFIX} Result marker={marker.Name} distance={distance:F3} dirMode={directionMode}");
                            }
                        }
                    }
                }
                else
                {
                foreach (var marker in markers)
                    {
                        if (marker == null) continue;

                        // VFX 轨道保留原逻辑
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
            }

            return results;
        }

        private static Vector3 ComputeDirection(Transform casterTransform, Transform markerTransform, int directionMode)
        {
            Vector3 forward = casterTransform != null ? casterTransform.forward : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-6f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();

            Vector3 towardsMarker = casterTransform != null ? (markerTransform.position - casterTransform.position) : markerTransform.forward;
            towardsMarker.y = 0f;
            if (towardsMarker.sqrMagnitude < 1e-6f)
            {
                towardsMarker = forward;
            }
            towardsMarker.Normalize();

            switch (directionMode)
            {
                case 0: // Forward
                    return forward;
                case 1: // Backward
                    return -forward;
                case 2: // Outward from caster to target
                    return towardsMarker;
                case 3: // Inward towards caster
                    return -towardsMarker;
                default:
                    return towardsMarker;
            }
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


