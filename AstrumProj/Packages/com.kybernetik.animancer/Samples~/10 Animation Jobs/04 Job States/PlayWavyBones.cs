// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2025 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using UnityEngine;

namespace Animancer.Samples.Jobs
{
    /// <summary>
    /// A sample component which demonstrates how an <see cref="AnimationJobState{T}"/>
    /// can be used to implement a procedural animation.
    /// </summary>
    /// 
    /// <remarks>
    /// <strong>Sample:</strong>
    /// <see href="https://kybernetik.com.au/animancer/docs/samples/jobs/job-states">
    /// Job States</see>
    /// </remarks>
    /// 
    /// https://kybernetik.com.au/animancer/api/Animancer.Samples.Jobs/PlayWavyBones
    /// 
    [AddComponentMenu(Strings.SamplesMenuPrefix + "Jobs - Play Wavy Bones")]
    [AnimancerHelpUrl(typeof(PlayWavyBones))]
    public class PlayWavyBones : MonoBehaviour
    {
        /************************************************************************************************************************/

        [SerializeField] private AnimancerComponent _Animancer;
        [SerializeField] private Transform[] _Bones;
        [SerializeField] private WavyBonesSettings _Settings;

        private AnimationJobState<WavyBonesAnimationJob> _JobState;

        /************************************************************************************************************************/

        protected virtual void OnEnable()
        {
            // Create the state.

            if (_JobState == null)
            {
                WavyBonesAnimationJob job = new WavyBonesAnimationJob(
                    _Animancer.Animator,
                    _Bones,
                    _Settings);

                _JobState = new AnimationJobState<WavyBonesAnimationJob>(job);
            }

            // Play it with a fade.

            _Animancer.Play(_JobState);

            // You can treat the state like any other.
            // For example, you could give it a fade duration, control its speed, or add events to it.
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the settings are changed in the Inspector, assign the new settings to the job.
        /// </summary>
        protected virtual void OnValidate()
        {
            if (!_JobState.IsValid())
                return;

            WavyBonesAnimationJob job = _JobState.Job;
            job.settings = _Settings;
            _JobState.Job = job;
        }

        /************************************************************************************************************************/
    }
}
