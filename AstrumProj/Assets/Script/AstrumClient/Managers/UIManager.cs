using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Astrum.Client.UI.Core;
using Astrum.Client.Core;
using Astrum.CommonBase;
using Astrum.View.Managers;
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

        // UI缓存 (UIBase)
        private Dictionary<string, UIBase> viewCache = new Dictionary<string, UIBase>();

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
            // 遍历所有活动的 UI，调用其 Update 方法
            foreach (var view in viewCache.Values)
            {
                if (view != null && view.IsVisible)
                {
                    try
                    {
                        view.Update();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[UIManager] Error updating view: {ex.Message}\n{ex.StackTrace}");
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
            var view = GetOrCreateUI(uiName);
            if (view != null)
            {
                view.Show();
            }
        }

        /// <summary>
        /// 隐藏UI
        /// </summary>
        /// <param name="uiName">UI名称，支持路径格式，如 "Hub/Bag" 或 "Login"</param>
        public void HideUI(string uiName)
        {
            if (viewCache.TryGetValue(uiName, out var view))
            {
                view.Hide();
            }
        }

        /// <summary>
        /// 获取UI GameObject
        /// </summary>
        /// <param name="uiName">UI名称，支持路径格式，如 "Hub/Bag" 或 "Login"</param>
        public GameObject GetUI(string uiName)
        {
            if (viewCache.TryGetValue(uiName, out var view))
            {
                return view.GameObject;
            }
            return null;
        }

        /// <summary>
        /// 销毁UI
        /// </summary>
        /// <param name="uiName">UI名称，支持路径格式，如 "Hub/Bag" 或 "Login"</param>
        public void DestroyUI(string uiName)
        {
            if (viewCache.TryGetValue(uiName, out var view))
            {
                viewCache.Remove(uiName);
                if (view.GameObject != null)
                {
                    UnityEngine.Object.Destroy(view.GameObject);
                }
                view.Destroy();
            }
        }

        /// <summary>
        /// 检查UI是否已创建
        /// </summary>
        /// <param name="uiName">UI名称，支持路径格式，如 "Hub/Bag" 或 "Login"</param>
        public bool HasUI(string uiName)
        {
            return viewCache.ContainsKey(uiName);
        }

        /// <summary>
        /// 检查UI是否显示
        /// </summary>
        /// <param name="uiName">UI名称，支持路径格式，如 "Hub/Bag" 或 "Login"</param>
        public bool IsUIVisible(string uiName)
        {
            if (viewCache.TryGetValue(uiName, out var view))
            {
                return view.IsVisible;
            }
            return false;
        }

        private UIBase GetOrCreateUI(string uiName)
        {
            // 先从缓存中获取
            if (viewCache.TryGetValue(uiName, out var cachedView))
            {
                return cachedView;
            }

            // 使用 ResourceManager 加载Prefab
            string prefabPath = $"{uiPrefabPath}{uiName}.prefab";
            GameObject prefab = null;
            
            var resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                // 尝试使用 ResourceManager 加载资源
                prefab = resourceManager.LoadResource<GameObject>(prefabPath);
            }
            
            // 如果 ResourceManager 未初始化或加载失败，使用回退方案
            if (prefab == null)
            {
#if UNITY_EDITOR
                // 在Editor中使用AssetDatabase加载（回退方案）
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
#else
                // 在运行时使用Resources加载（回退方案）
                var resourcesPath = $"UI/{uiName}";
                prefab = Resources.Load<GameObject>(resourcesPath);
#endif
            }
            
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
            
            // 获取 UIRefs
            var uiRefs = uiGO.GetComponent<UIRefs>();
            if (uiRefs == null)
            {
                Debug.LogError($"[UIManager] UI {uiName} 缺少 UIRefs 组件");
                UnityEngine.Object.Destroy(uiGO);
                return null;
            }

            // 获取 Logic Instance
            var instance = uiRefs.GetUIInstance();
            if (instance == null)
            {
                Debug.LogError($"[UIManager] UI {uiName} 逻辑实例创建失败");
                UnityEngine.Object.Destroy(uiGO);
                return null;
            }

            // 转换为 UIBase
            var uiBase = instance as UIBase;
            if (uiBase == null)
            {
                Debug.LogError($"[UIManager] UI {uiName} 的逻辑类 ({instance.GetType().Name}) 未继承 UIBase");
                UnityEngine.Object.Destroy(uiGO);
                return null;
            }

            // 缓存UI（使用完整路径作为key）
            viewCache[uiName] = uiBase;
            
            return uiBase;
        }

        /// <summary>
        /// 清理所有UI
        /// </summary>
        public void ClearAllUI()
        {
            var keys = new List<string>(viewCache.Keys);
            foreach (var key in keys)
            {
                DestroyUI(key);
            }
            viewCache.Clear();
        }

    }
}
