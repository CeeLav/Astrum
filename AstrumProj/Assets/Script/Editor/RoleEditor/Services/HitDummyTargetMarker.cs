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
            Initialize();

            if (_transform == null)
                return;

            _transform.localPosition = _initialLocalPosition;
            _transform.localRotation = _initialLocalRotation;
            _transform.localScale = _initialLocalScale;
            _lastKnockbackDistance = 0f;
            _lastKnockbackDirection = Vector3.zero;
            _lastAppliedFrame = -1;
            Debug.Log($"[HitDummyMarker] Reset {Name} localPos={_transform.localPosition}");
        }

        public bool ApplyKnockback(Vector3 direction, float distance, int frame)
        {
            Initialize();

            if (_transform == null || distance <= 0f)
                return false;

            if (frame == _lastAppliedFrame)
                return false;

            _lastKnockbackDirection = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
            _lastKnockbackDistance = distance;

            _transform.position += _lastKnockbackDirection * distance;
            _lastAppliedFrame = frame;
            Debug.Log($"[HitDummyMarker] ApplyKnockback {Name} distance={distance:F3} dir={_lastKnockbackDirection}");
            return true;
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
    }
}


