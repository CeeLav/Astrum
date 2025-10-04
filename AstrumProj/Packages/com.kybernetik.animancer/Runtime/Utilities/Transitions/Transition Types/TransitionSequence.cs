// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2025 Kybernetik //

#if ! UNITY_EDITOR
#pragma warning disable CS0618 // Type or member is obsolete (for Animancer Events in Animancer Lite).
#endif

using Animancer.Units;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animancer
{
    /// <inheritdoc/>
    /// <summary>A group of transitions which play one after the other.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer/TransitionSequence
    /// 
    [Serializable]
    public class TransitionSequence : Transition<SequenceState>,
        IAnimationClipCollection,
        ICopyable<TransitionSequence>
    {
        /************************************************************************************************************************/

        [SerializeField]
        [Tooltip(Strings.Tooltips.NormalizedStartTime)]
        [DefaultValue(float.NaN, 0f)]
        [AnimationTime(
            AnimationTimeAttribute.Units.Normalized,
            DisabledText = Strings.Tooltips.StartTimeDisabled)]
        private float _NormalizedStartTime = float.NaN;

        /// <inheritdoc/>
        public override float NormalizedStartTime
        {
            get => _NormalizedStartTime;
            set => _NormalizedStartTime = value;
        }

        /// <summary>
        /// If this transition will set the <see cref="AnimancerState.Time"/>,
        /// then it needs to use <see cref="FadeMode.FromStart"/>.
        /// </summary>
        public override FadeMode FadeMode
            => float.IsNaN(_NormalizedStartTime)
            ? default
            : FadeMode.FromStart;

        /************************************************************************************************************************/

        [DrawAfterEvents]
        [SerializeReference]
        [Tooltip("The transitions to play in this sequence.")]
        private ITransition[] _Transitions = Array.Empty<ITransition>();

        /// <summary>[<see cref="SerializeField"/>] The transitions to play in this sequence.</summary>
        public ref ITransition[] Transitions
            => ref _Transitions;

        /************************************************************************************************************************/

        /// <summary>Is everything in this sequence valid?</summary>
        public override bool IsValid
        {
            get
            {
                for (int i = 0; i < _Transitions.Length; i++)
                    if (!_Transitions[i].IsValid())
                        return false;

                return true;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Sequences don't loop.</summary>
        /// <remarks>
        /// If the last state in the sequence is set to loop it will do so,
        /// but the rest of the sequence won't replay automatically.
        /// </remarks>
        public override bool IsLooping
            => false;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float MaximumLength
        {
            get
            {
                var value = 0f;

                for (int i = 0; i < _Transitions.Length; i++)
                {
                    var transition = _Transitions[i];
                    if (!transition.IsValid())
                        continue;

                    var speed = transition.Speed;

                    var start = AnimancerEvent.Sequence.GetNormalizedStartTime(
                        transition.NormalizedStartTime,
                        speed);

                    var end = transition.SerializedEvents.GetNormalizedEndTime(
                        speed);

                    var normalizedLength = (end - start) / speed;
                    if (normalizedLength < 0)
                        continue;

                    value += transition.MaximumLength * normalizedLength;
                }

                return value;
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override SequenceState CreateState()
        {
            var state = new SequenceState();
            state.Set(_Transitions);
            return state;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void Apply(AnimancerState state)
        {
            base.Apply(state);
            ApplyNormalizedStartTime(state, _NormalizedStartTime);
        }

        /************************************************************************************************************************/

        /// <summary>Adds the <see cref="ClipTransition.Clip"/> of everything in this sequence to the collection.</summary>
        public virtual void GatherAnimationClips(ICollection<AnimationClip> clips)
        {
            for (int i = 0; i < _Transitions.Length; i++)
                clips.GatherFromSource(_Transitions[i]);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override Transition<SequenceState> Clone(CloneContext context)
            => new TransitionSequence();

        /// <inheritdoc/>
        public sealed override void CopyFrom(Transition<SequenceState> copyFrom, CloneContext context)
            => this.CopyFromBase(copyFrom, context);

        /// <inheritdoc/>
        public virtual void CopyFrom(TransitionSequence copyFrom, CloneContext context)
        {
            base.CopyFrom(copyFrom, context);

            _NormalizedStartTime = copyFrom._NormalizedStartTime;
            AnimancerUtilities.CopyExactArray(copyFrom._Transitions, ref _Transitions);
        }

        /************************************************************************************************************************/
    }
}

