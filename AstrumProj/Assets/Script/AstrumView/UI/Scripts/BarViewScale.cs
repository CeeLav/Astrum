using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Sharklib.ProgressBar {
    [RequireComponent(typeof(RectTransform))]
    public class BarViewScale : ProgressBarProView {

        [SerializeField] protected RectTransform graphic;

        [Header("Scale Options")]
        [Tooltip("If true, then the scale animates on each change. Otherwise it scales constantly based on value")]
        [SerializeField] bool animateOnChange = true;

        [UnityEngine.Serialization.FormerlySerializedAs("minSize")]
        [SerializeField] Vector3 minScale = Vector3.one;
        [UnityEngine.Serialization.FormerlySerializedAs("maxSize")]
        [SerializeField] Vector3 maxScale = new Vector3(2f, 2f, 2f);

        [UnityEngine.Serialization.FormerlySerializedAs("scale")]
        [Tooltip("A value of 0 is minSize, a value of 1 is maxSize. Time goes from 0 to 1.")]
        [SerializeField] AnimationCurve scaleAnim = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f));
        [SerializeField] float animDuration = 0.2f;

        private Coroutine scaleCoroutine;

        void OnEnable() {
            UpdateScale();
        }

        public override void NewChangeStarted(float currentValue, float targetValue) {
            if (gameObject.activeInHierarchy == false || !animateOnChange)
                return; // No Coroutine if we're disabled

            if (scaleCoroutine != null)
                StopCoroutine(scaleCoroutine);

            scaleCoroutine = StartCoroutine(DoBarScaleAnim(animDuration));
        }

        IEnumerator DoBarScaleAnim(float duration) {
            float time = 0f;

            while (time < duration) {
                UpdateScale(time / duration);
                time += Time.deltaTime;
                yield return null;
            }

            UpdateScale(0f);
            scaleCoroutine = null;
        }

        public override void UpdateView(float currentValue, float targetValue) {
            if (animateOnChange)
                return;

            if (scaleCoroutine == null) // if we're flashing don't update this since the coroutine handles our updates
                UpdateScale(currentValue);
        }

        void UpdateScale(float value = 0f) {
            graphic.localScale = GetCurrentScale(value);
        }

        Vector3 GetCurrentScale(float percentage) {
            return Vector3.Lerp(minScale, maxScale, scaleAnim.Evaluate(percentage));
        }

#if UNITY_EDITOR
        protected override void Reset() {
            base.Reset();

            graphic = GetComponent<RectTransform>();
        }

        void OnValidate() {
            UpdateScale();
        }
#endif
    }

}