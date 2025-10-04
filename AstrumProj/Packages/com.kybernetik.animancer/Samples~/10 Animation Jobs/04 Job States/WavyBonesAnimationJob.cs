// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2025 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;

namespace Animancer.Samples.Jobs
{
    /// <summary>
    /// A sample animation job which waves the character's bones back and forth.
    /// </summary>
    /// 
    /// <remarks>
    /// <strong>Sample:</strong>
    /// <see href="https://kybernetik.com.au/animancer/docs/samples/jobs/job-states">
    /// Job States</see>
    /// </remarks>
    /// 
    /// https://kybernetik.com.au/animancer/api/Animancer.Samples.Jobs/WavyBonesAnimationJob
    public struct WavyBonesAnimationJob :
        IAnimancerStateJob
    {
        /************************************************************************************************************************/

        public TransformStreamHandle rootTransform;
        public NativeArray<TransformStreamHandle> transforms;
        public WavyBonesSettings settings;

        /************************************************************************************************************************/

        // This sample doesn't really care about the length or looping flag.
        // They determine what the state looks like in the Inspector and how its events work.

        public readonly float Length
            => 1;

        public readonly bool IsLooping
            => true;

        /************************************************************************************************************************/

        public WavyBonesAnimationJob(
            Animator animator,
            Transform[] transforms,
            WavyBonesSettings settings)
        {
            this.settings = settings;

            this.transforms = new NativeArray<TransformStreamHandle>(
                transforms.Length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < transforms.Length; i++)
            {
                this.transforms[i] = animator.BindStreamTransform(transforms[i]);
            }

            rootTransform = animator.BindStreamTransform(animator.transform.GetChild(0));
        }

        /************************************************************************************************************************/

        readonly void IDisposable.Dispose()
        {
            transforms.Dispose();
        }

        /************************************************************************************************************************/

        readonly void IAnimancerStateJob.ProcessRootMotion(AnimationStream stream, double time)
        {
        }

        public readonly void ProcessAnimation(AnimationStream stream, double time)
        {
            rootTransform.SetLocalPosition(stream, settings.rootPosition);

            for (int i = 0; i < transforms.Length; i++)
            {
                TransformStreamHandle transform = transforms[i];

                Vector3 euler = new(
                    (float)(Math.Sin(time * settings.speeds.x) * settings.magnitudes.x),
                    (float)(Math.Sin(time * settings.speeds.y) * settings.magnitudes.y),
                    (float)(Math.Sin(time * settings.speeds.z) * settings.magnitudes.z));

                Quaternion rotation = Quaternion.Euler(euler);

                transform.SetLocalRotation(stream, rotation);
            }
        }

        /************************************************************************************************************************/
    }
}
