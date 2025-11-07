using UnityEngine;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 木桩目标标记组件，用于记录初始状态并应用调试交互
    /// </summary>
    public class HitDummyTargetMarker : MonoBehaviour
    {
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        private bool _initialized;
        private float _lastKnockbackDistance;
        private Vector3 _lastKnockbackDirection;

        public float LastKnockbackDistance => _lastKnockbackDistance;
        public Vector3 LastKnockbackDirection => _lastKnockbackDirection;

        public void Initialize()
        {
            if (_initialized) return;

            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialized = true;
        }

        public void ResetState()
        {
            Initialize();

            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            _lastKnockbackDistance = 0f;
            _lastKnockbackDirection = Vector3.zero;
        }

        public void ApplyKnockback(Vector3 direction, float distance)
        {
            Initialize();

            if (distance <= 0f) return;

            _lastKnockbackDirection = direction.normalized;
            _lastKnockbackDistance = distance;

            transform.position += _lastKnockbackDirection * distance;
        }

        public void ApplyHighlight(Color color)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial == null) continue;
                renderer.sharedMaterial.SetColor("_Color", color);
            }
        }
    }
}


