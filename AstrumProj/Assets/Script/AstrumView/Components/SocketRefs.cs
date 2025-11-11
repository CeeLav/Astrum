using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Astrum.View.Components
{
    /// <summary>
    /// 模型绑点引用组件
    /// 用于记录模型上的关键位置（如手部、法杖顶部、武器挂点等）
    /// </summary>
    public sealed class SocketRefs : MonoBehaviour
    {
        [Serializable]
        public struct SocketBinding
        {
            [Tooltip("绑点名称（如 MuzzlePoint、WeaponTip、LeftHand 等）")]
            public string Name;
            
            [Tooltip("绑点 Transform 引用")]
            public Transform Transform;
        }

        [SerializeField]
        [Tooltip("所有绑点列表")]
        private List<SocketBinding> _bindings = new List<SocketBinding>();

        private readonly Dictionary<string, Transform> _lookup = new Dictionary<string, Transform>();

        private void Awake()
        {
            // 构建查找表
            _lookup.Clear();
            foreach (var binding in _bindings)
            {
                if (!string.IsNullOrWhiteSpace(binding.Name) && binding.Transform != null)
                {
                    if (_lookup.ContainsKey(binding.Name))
                    {
                        Debug.LogWarning($"[SocketRefs] Duplicate socket name '{binding.Name}' on {gameObject.name}, using first occurrence");
                        continue;
                    }
                    _lookup[binding.Name] = binding.Transform;
                }
            }
        }

        /// <summary>
        /// 获取指定绑点的世界坐标和朝向
        /// </summary>
        /// <param name="socketName">绑点名称</param>
        /// <param name="position">输出：世界坐标</param>
        /// <param name="forward">输出：世界朝向</param>
        /// <returns>是否找到该绑点</returns>
        public bool TryGetWorldPosition(string socketName, out Vector3 position, out Vector3 forward)
        {
            position = Vector3.zero;
            forward = Vector3.forward;

            if (string.IsNullOrWhiteSpace(socketName))
                return false;

            if (_lookup.TryGetValue(socketName, out var socketTransform) && socketTransform != null)
            {
                position = socketTransform.position;
                forward = socketTransform.forward;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取指定绑点的 Transform（可能为 null）
        /// </summary>
        public Transform? GetSocketTransform(string socketName)
        {
            if (string.IsNullOrWhiteSpace(socketName))
                return null;

            return _lookup.TryGetValue(socketName, out var t) ? t : null;
        }

        /// <summary>
        /// 获取所有已注册的绑点名称
        /// </summary>
        public IEnumerable<string> GetAllSocketNames()
        {
            return _lookup.Keys;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 编辑器下可视化绑点
            if (_bindings == null)
                return;

            Gizmos.color = Color.cyan;
            foreach (var binding in _bindings)
            {
                if (binding.Transform != null)
                {
                    Gizmos.DrawWireSphere(binding.Transform.position, 0.05f);
                    Gizmos.DrawRay(binding.Transform.position, binding.Transform.forward * 0.2f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 选中时显示绑点名称
            if (_bindings == null)
                return;

            foreach (var binding in _bindings)
            {
                if (binding.Transform != null && !string.IsNullOrWhiteSpace(binding.Name))
                {
                    UnityEditor.Handles.Label(binding.Transform.position, binding.Name);
                }
            }
        }
#endif
    }
}

