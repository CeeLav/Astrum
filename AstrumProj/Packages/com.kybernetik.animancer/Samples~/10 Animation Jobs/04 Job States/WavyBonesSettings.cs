// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2025 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System;
using UnityEngine;

namespace Animancer.Samples.Jobs
{
    /// <summary>
    /// Configuration data for <see cref="WavyBonesAnimationJob"/>.
    /// </summary>
    /// 
    /// <remarks>
    /// <strong>Sample:</strong>
    /// <see href="https://kybernetik.com.au/animancer/docs/samples/jobs/job-states">
    /// Job States</see>
    /// </remarks>
    /// 
    /// https://kybernetik.com.au/animancer/api/Animancer.Samples.Jobs/WavyBonesSettings
    [Serializable]
    public struct WavyBonesSettings
    {
        /************************************************************************************************************************/

        public Vector3 rootPosition;
        public Vector3 magnitudes;
        public Vector3 speeds;

        /************************************************************************************************************************/
    }
}
