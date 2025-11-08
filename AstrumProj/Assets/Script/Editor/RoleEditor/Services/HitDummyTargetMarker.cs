using UnityEngine;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 木桩目标标记：记录初始状态并提供击退操作
    /// </summary>
    public sealed class HitDummyTargetMarker
    {
        private readonly GameObject _instance;
        private readonly Transform _transform;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private Vector3 _initialLocalScale;

        private bool _initialized;
        private float _lastKnockbackDistance;
        private Vector3 _lastKnockbackDirection;
        private int _lastAppliedFrame = -1;

        private bool _knockbackActive;
        private float _knockbackDuration;
        private float _knockbackElapsed;
        private Vector3 _knockbackStartPosition;
        private Vector3 _knockbackEndPosition;
        private HitDummyKnockbackCurve _knockbackCurve = HitDummyKnockbackCurve.Linear;

        public Transform Transform => _transform;
        public GameObject Instance => _instance;

        public float LastKnockbackDistance => _lastKnockbackDistance;
        public Vector3 LastKnockbackDirection => _lastKnockbackDirection;
        public int LastAppliedFrame => _lastAppliedFrame;

        public string Name => _instance != null ? _instance.name : "<null>";

        public HitDummyTargetMarker(GameObject instance)
        {
            _instance = instance;
            _transform = instance != null ? instance.transform : null;
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized || _transform == null) return;

            _initialPosition = _transform.position;
            _initialRotation = _transform.rotation;
            _initialLocalPosition = _transform.localPosition;
            _initialLocalRotation = _transform.localRotation;
            _initialLocalScale = _transform.localScale;
            _initialized = true;
        }

        public void ResetState()
        {
            if (_transform == null)
                return;

            _transform.localPosition = _initialLocalPosition;
            _transform.localRotation = _initialLocalRotation;
            _lastKnockbackDirection = Vector3.zero;
            _lastAppliedFrame = -1;
            _knockbackActive = false;
            _knockbackElapsed = 0f;
            _knockbackDuration = 0f;
        }

        public bool ApplyKnockback(Vector3 direction, float distance, int frame, float durationSeconds, HitDummyKnockbackCurve curve)
        {
            Initialize();

            if (_transform == null || distance <= 0f)
                return false;

            if (frame == _lastAppliedFrame)
                return false;

            _lastKnockbackDirection = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
            _lastKnockbackDistance = distance;

            if (durationSeconds <= 1e-5f)
            {
                _transform.position += _lastKnockbackDirection * distance;
                _knockbackActive = false;
            }
            else
            {
                _knockbackActive = true;
                _knockbackDuration = Mathf.Max(durationSeconds, 1e-4f);
                _knockbackElapsed = 0f;
                _knockbackCurve = curve;

                // 以当前世界位置作为新的起点，支持击退叠加
                _knockbackStartPosition = _transform.position;
                _knockbackEndPosition = _knockbackStartPosition + _lastKnockbackDirection * distance;
            }

            _lastAppliedFrame = frame;
            return true;
        }

        public void Update(float deltaTime)
        {
            if (!_knockbackActive || _transform == null)
                return;

            _knockbackElapsed += Mathf.Max(0f, deltaTime);
            float normalizedTime = _knockbackDuration > 1e-6f
                ? Mathf.Clamp01(_knockbackElapsed / _knockbackDuration)
                : 1f;

            float progress = EvaluateCurve(normalizedTime, _knockbackCurve);
            _transform.position = Vector3.Lerp(_knockbackStartPosition, _knockbackEndPosition, progress);

            if (_knockbackElapsed >= _knockbackDuration)
            {
                _transform.position = _knockbackEndPosition;
                _knockbackActive = false;
            }
        }

        public void ApplyHighlight(Color color)
        {
            if (_instance == null)
                return;

            var renderers = _instance.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial == null) continue;
                renderer.sharedMaterial.SetColor("_Color", color);
            }
        }

        private static float EvaluateCurve(float normalizedTime, HitDummyKnockbackCurve curve)
        {
            normalizedTime = Mathf.Clamp01(normalizedTime);
            switch (curve)
            {
                case HitDummyKnockbackCurve.Decelerate:
                    return Mathf.SmoothStep(0f, 1f, normalizedTime);
                case HitDummyKnockbackCurve.Accelerate:
                    return normalizedTime * normalizedTime;
                case HitDummyKnockbackCurve.Custom:
                case HitDummyKnockbackCurve.Linear:
                default:
                    return normalizedTime;
            }
        }
    }
}


