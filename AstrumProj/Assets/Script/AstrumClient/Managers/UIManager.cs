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
            Debug.Log("[UIManager] 初始化完成");
        }

        /// <summary>
        /// 更新UI管理器
        /// </summary>
        public void Update()
        {
            // 更新所有已显示的UI
            foreach (var kvp in uiCache)
            {
                var uiGO = kvp.Value;
                if (uiGO != null && uiGO.activeInHierarchy)
                {
                    var uiRefs = uiGO.GetComponent<UIRefs>();
                    if (uiRefs != null)
                    {
                        var uiInstance = uiRefs.GetUIInstance();
                        if (uiInstance is UIBase uiBase)
                        {
                            // 使用UIBase接口调用Update
                            try
                            {
                                uiBase.Update();
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"[UIManager] 调用UI实例Update方法时发生异常 ({kvp.Key}): {ex.Message}");
                            }
                        }
                    }
                }
            }
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
        /// <param name="uiName">UI名称，支持路径格式，如 "Hub/Bag" 或 "Login"</param>
        public void ShowUI(string uiName)
        {
            var uiGO = GetOrCreateUI(uiName);
            if (uiGO != null)
            {
                var uiRefs = uiGO.GetComponent<UIRefs>();
                if (uiRefs != null)
                {
                    var uiInstance = uiRefs.GetUIInstance();
                    if (uiInstance is UIBase uiBase)
                    {
                        uiBase.Show();
                    }
                    else
                    {
                        // 兼容旧代码：如果没有继承UIBase，直接激活GameObject
                        uiGO.SetActive(true);
                    }
                }
                else
                {
                    uiGO.SetActive(true);
                }
            }
        }

        /// <summary>
        /// 隐藏UI
        /// </summary>
        /// <param name="uiName">UI名称，支持路径格式，如 "Hub/Bag" 或 "Login"</param>
        public void HideUI(string uiName)
        {
            if (uiCache.TryGetValue(uiName, out var uiGO))
            {
                var uiRefs = uiGO.GetComponent<UIRefs>();
                if (uiRefs != null)
                {
                    var uiInstance = uiRefs.GetUIInstance();
                    if (uiInstance is UIBase uiBase)
                    {
                        uiBase.Hide();
                    }
                    else
                    {
                        // 兼容旧代码：如果没有继承UIBase，直接隐藏GameObject
                        uiGO.SetActive(false);
                    }
                }
                else
                {
                    uiGO.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 获取UI GameObject
        /// </summary>
        /// <param name="uiName">UI名称，支持路径格式，如 "Hub/Bag" 或 "Login"</param>
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
        /// <param name="uiName">UI名称，支持路径格式，如 "Hub/Bag" 或 "Login"</param>
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
        /// <param name="uiName">UI名称，支持路径格式，如 "Hub/Bag" 或 "Login"</param>
        public bool HasUI(string uiName)
        {
            return uiCache.ContainsKey(uiName);
        }

        /// <summary>
        /// 检查UI是否显示
        /// </summary>
        /// <param name="uiName">UI名称，支持路径格式，如 "Hub/Bag" 或 "Login"</param>
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
            string prefabPath = $"{uiPrefabPath}{uiName}.prefab";
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
            // 提取纯名称作为GameObject名称（去掉路径）
            string cleanName = uiName.Contains("/") ? uiName.Substring(uiName.LastIndexOf("/") + 1) : uiName;
            var uiGO = UnityEngine.Object.Instantiate(prefab, uiRootTransform);
            uiGO.name = cleanName;
            
            // 缓存UI（使用完整路径作为key）
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