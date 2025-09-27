using System.Collections.Generic;
using UnityEngine;
using Astrum.Client.UI.Core;
using Astrum.Client.Core;
using Astrum.CommonBase;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Astrum.Client.Managers
{
    /// <summary>
    /// UI管理器 - 负责UI的创建、显示、隐藏和销毁
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {

        // UI配置
        private string uiPrefabPath = "Assets/ArtRes/UI/";

        // UI缓存
        private Dictionary<string, GameObject> uiCache = new Dictionary<string, GameObject>();

        /// <summary>
        /// 初始化UI管理器
        /// </summary>
        public void Initialize()
        {
            ASLogger.Instance.Info("UIManager: 初始化UI管理器");
            Debug.Log("[UIManager] 初始化完成");
        }

        /// <summary>
        /// 更新UI管理器
        /// </summary>
        public void Update()
        {
            // UI管理器更新逻辑（如果需要）
        }

        /// <summary>
        /// 关闭UI管理器
        /// </summary>
        public void Shutdown()
        {
            ClearAllUI();
            Debug.Log("[UIManager] 关闭完成");
        }


        /// <summary>
        /// 显示UI
        /// </summary>
        public void ShowUI(string uiName)
        {
            var uiGO = GetOrCreateUI(uiName);
            if (uiGO != null)
            {
                uiGO.SetActive(true);
            }
        }

        /// <summary>
        /// 隐藏UI
        /// </summary>
        public void HideUI(string uiName)
        {
            if (uiCache.TryGetValue(uiName, out var uiGO))
            {
                uiGO.SetActive(false);
            }
        }

        /// <summary>
        /// 获取UI GameObject
        /// </summary>
        public GameObject GetUI(string uiName)
        {
            if (uiCache.TryGetValue(uiName, out var uiGO))
            {
                return uiGO;
            }
            return null;
        }

        /// <summary>
        /// 销毁UI
        /// </summary>
        public void DestroyUI(string uiName)
        {
            if (uiCache.TryGetValue(uiName, out var uiGO))
            {
                uiCache.Remove(uiName);
                UnityEngine.Object.Destroy(uiGO);
            }
        }

        /// <summary>
        /// 检查UI是否已创建
        /// </summary>
        public bool HasUI(string uiName)
        {
            return uiCache.ContainsKey(uiName);
        }

        /// <summary>
        /// 检查UI是否显示
        /// </summary>
        public bool IsUIVisible(string uiName)
        {
            if (uiCache.TryGetValue(uiName, out var uiGO))
            {
                return uiGO.activeInHierarchy;
            }
            return false;
        }

        private GameObject GetOrCreateUI(string uiName)
        {
            // 先从缓存中获取
            if (uiCache.TryGetValue(uiName, out var cachedUI))
            {
                return cachedUI;
            }

            // 加载Prefab
            var prefabPath = $"{uiPrefabPath}{uiName}.prefab";
            GameObject prefab = null;
            
#if UNITY_EDITOR
            // 在Editor中使用AssetDatabase加载
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
#else
            // 在运行时使用Resources加载
            var resourcesPath = $"UI/{uiName}";
            prefab = Resources.Load<GameObject>(resourcesPath);
#endif
            
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] 无法加载UI Prefab: {prefabPath}");
                return null;
            }

            // 获取UIRoot引用
            var uiRootTransform = GameApplication.Instance?.UIRoot?.transform;
            if (uiRootTransform == null)
            {
                Debug.LogError("[UIManager] 无法获取UIRoot引用，请确保GameApplication已初始化");
                return null;
            }

            // 实例化UI
            var uiGO = UnityEngine.Object.Instantiate(prefab, uiRootTransform);
            uiGO.name = uiName;
            
            // 缓存UI
            uiCache[uiName] = uiGO;
            
            return uiGO;
        }

        /// <summary>
        /// 清理所有UI
        /// </summary>
        public void ClearAllUI()
        {
            foreach (var kvp in uiCache)
            {
                if (kvp.Value != null)
                {
                    UnityEngine.Object.Destroy(kvp.Value);
                }
            }
            uiCache.Clear();
        }

    }
}