using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Astrum.Client.UI.Core
{
    /// <summary>
    /// UIRefs组件，用于管理UI元素的引用和实例化对应的UI逻辑类
    /// </summary>
    public class UIRefs : MonoBehaviour
    {
        [Header("UI配置")]
        [SerializeField] private string uiClassName;
        [SerializeField] private string uiNamespace = "Astrum.Client.UI.Generated";
        
        [Header("引用缓存")]
        [SerializeField] public List<UIRefItem> uiRefItems = new List<UIRefItem>();
        
        // 运行时引用缓存
        private Dictionary<string, MonoBehaviour> componentRefs = new Dictionary<string, MonoBehaviour>();
        private Dictionary<string, GameObject> gameObjectRefs = new Dictionary<string, GameObject>();
        private Dictionary<string, Component> componentCache = new Dictionary<string, Component>();
        
        // UI逻辑类实例
        private object uiInstance;
        private Type uiType;
        
        private void Awake()
        {
            CollectReferences();
            InstantiateUIClass();
        }
        
        /// <summary>
        /// 收集所有子节点的引用
        /// </summary>
        private void CollectReferences()
        {
            try
            {
                componentRefs.Clear();
                gameObjectRefs.Clear();
                componentCache.Clear();
                
                // 收集所有节点引用（包括根节点），使用完整路径
                CollectReferencesRecursive(transform, "");
                
                //Debug.Log($"[UIRefs] 引用收集完成，共收集 {componentRefs.Count} 个组件引用，{gameObjectRefs.Count} 个GameObject引用");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIRefs] 收集引用时发生异常: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 递归收集引用
        /// </summary>
        private void CollectReferencesRecursive(Transform parent, string path)
        {
            // 收集当前节点（包括根节点）的引用
            // 处理Unity自动添加的(Clone)后缀
            string nodeName = parent.name;
            if (nodeName.EndsWith("(Clone)"))
            {
                nodeName = nodeName.Substring(0, nodeName.Length - 7); // 去掉"(Clone)"
            }
            
            string currentPath = string.IsNullOrEmpty(path) ? nodeName : $"{path}/{nodeName}";
            
            // 缓存GameObject引用
            gameObjectRefs[currentPath] = parent.gameObject;
            
            // 缓存所有MonoBehaviour组件引用
            var components = parent.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component != null && component != this)
                {
                    componentRefs[currentPath] = component;
                    break; // 只取第一个MonoBehaviour组件
                }
            }
            
            // 缓存其他重要组件
            var rectTransform = parent.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                componentCache[$"{currentPath}/RectTransform"] = rectTransform;
            }
            
            var image = parent.GetComponent<Image>();
            if (image != null)
            {
                componentCache[$"{currentPath}/Image"] = image;
            }
            
            var text = parent.GetComponent<Text>();
            if (text != null)
            {
                componentCache[$"{currentPath}/Text"] = text;
            }
            
            var button = parent.GetComponent<Button>();
            if (button != null)
            {
                componentCache[$"{currentPath}/Button"] = button;
            }
            
            var inputField = parent.GetComponent<InputField>();
            if (inputField != null)
            {
                componentCache[$"{currentPath}/InputField"] = inputField;
            }
            
            var scrollRect = parent.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                componentCache[$"{currentPath}/ScrollRect"] = scrollRect;
            }
            
            var toggle = parent.GetComponent<Toggle>();
            if (toggle != null)
            {
                componentCache[$"{currentPath}/Toggle"] = toggle;
            }
            
            var slider = parent.GetComponent<Slider>();
            if (slider != null)
            {
                componentCache[$"{currentPath}/Slider"] = slider;
            }
            
            var dropdown = parent.GetComponent<Dropdown>();
            if (dropdown != null)
            {
                componentCache[$"{currentPath}/Dropdown"] = dropdown;
            }
            
            // 递归处理子节点
            foreach (Transform child in parent)
            {
                CollectReferencesRecursive(child, currentPath);
            }
        }
        
        /// <summary>
        /// 实例化对应的UI逻辑类
        /// </summary>
        private void InstantiateUIClass()
        {
            try
            {
                if (string.IsNullOrEmpty(uiClassName))
                {
                    Debug.LogWarning("[UIRefs] UI类名未设置，跳过实例化");
                    return;
                }
                
                // 构建完整的类型名称
                string fullTypeName = string.IsNullOrEmpty(uiNamespace) ? uiClassName : $"{uiNamespace}.{uiClassName}";
                
                // 查找类型
                uiType = Type.GetType(fullTypeName);
                if (uiType == null)
                {
                    // 尝试在当前程序集中查找
                    uiType = Assembly.GetExecutingAssembly().GetType(fullTypeName);
                }
                
                if (uiType == null)
                {
                    Debug.LogError($"[UIRefs] 无法找到UI类型: {fullTypeName}");
                    return;
                }
                
                // 查找构造函数
                var constructor = uiType.GetConstructor(new Type[] { typeof(UIRefs) });
                if (constructor != null)
                {
                    // 使用UIRefs参数的构造函数
                    uiInstance = constructor.Invoke(new object[] { this });
                }
                else
                {
                    // 使用无参构造函数
                    constructor = uiType.GetConstructor(Type.EmptyTypes);
                    if (constructor != null)
                    {
                        uiInstance = constructor.Invoke(null);
                    }
                    else
                    {
                        Debug.LogError($"[UIRefs] 无法找到合适的构造函数: {fullTypeName}");
                        return;
                    }
                }
                
                // 调用Initialize方法
                var initMethod = uiType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance);
                if (initMethod != null)
                {
                    // 检查Initialize方法的参数
                    var parameters = initMethod.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(UIRefs))
                    {
                        // 传递UIRefs参数
                        initMethod.Invoke(uiInstance, new object[] { this });
                    }
                    else
                    {
                        // 无参数调用
                        initMethod.Invoke(uiInstance, null);
                    }
                    Debug.Log($"[UIRefs] UI类实例化成功: {fullTypeName}");
                }
                else
                {
                    Debug.LogWarning($"[UIRefs] UI类没有Initialize方法: {fullTypeName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIRefs] 实例化UI类时发生异常: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 获取GameObject引用
        /// </summary>
        public GameObject GetGameObject(string path)
        {
            return gameObjectRefs.TryGetValue(path, out var go) ? go : null;
        }
        
        /// <summary>
        /// 获取MonoBehaviour组件引用
        /// </summary>
        public T GetReference<T>(string path) where T : MonoBehaviour
        {
            if (componentRefs.TryGetValue(path, out var component))
            {
                return component as T;
            }
            return null;
        }
        
        /// <summary>
        /// 获取组件引用
        /// </summary>
        public T GetComponent<T>(string path) where T : Component
        {
            var go = GetGameObject(path);
            return go?.GetComponent<T>();
        }
        
        /// <summary>
        /// 获取缓存的组件引用
        /// </summary>
        public T GetCachedComponent<T>(string path) where T : Component
        {
            var cacheKey = $"{path}/{typeof(T).Name}";
            if (componentCache.TryGetValue(cacheKey, out var component))
            {
                return component as T;
            }
            return null;
        }
        
        /// <summary>
        /// 获取所有路径
        /// </summary>
        public string[] GetAllPaths()
        {
            return gameObjectRefs.Keys.ToArray();
        }
        
        /// <summary>
        /// 获取所有组件路径
        /// </summary>
        public string[] GetAllComponentPaths()
        {
            return componentRefs.Keys.ToArray();
        }
        
        /// <summary>
        /// 检查路径是否存在
        /// </summary>
        public bool HasPath(string path)
        {
            return gameObjectRefs.ContainsKey(path);
        }
        
        /// <summary>
        /// 检查组件路径是否存在
        /// </summary>
        public bool HasComponentPath(string path)
        {
            return componentRefs.ContainsKey(path);
        }
        
        /// <summary>
        /// 获取UI实例
        /// </summary>
        public object GetUIInstance()
        {
            return uiInstance;
        }
        
        /// <summary>
        /// 获取UI类型
        /// </summary>
        public Type GetUIType()
        {
            return uiType;
        }
        
        /// <summary>
        /// 设置UI类名
        /// </summary>
        public void SetUIClassName(string className)
        {
            uiClassName = className;
        }
        
        /// <summary>
        /// 设置UI命名空间
        /// </summary>
        public void SetUINamespace(string namespaceName)
        {
            uiNamespace = namespaceName;
        }
        
        /// <summary>
        /// 重新收集引用
        /// </summary>
        public void RefreshReferences()
        {
            CollectReferences();
        }
        
        /// <summary>
        /// 重新实例化UI类
        /// </summary>
        public void ReinstantiateUIClass()
        {
            InstantiateUIClass();
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// 编辑器模式下收集引用（用于预览）
        /// </summary>
        [ContextMenu("收集引用")]
        private void CollectReferencesInEditor()
        {
            CollectReferences();
        }
        
        /// <summary>
        /// 编辑器模式下实例化UI类（用于预览）
        /// </summary>
        [ContextMenu("实例化UI类")]
        private void InstantiateUIClassInEditor()
        {
            InstantiateUIClass();
        }
        #endif
    }
    
    /// <summary>
    /// UI引用项（用于序列化）
    /// </summary>
    [System.Serializable]
    public class UIRefItem
    {
        public string path;
        public string componentType;
        public UnityEngine.Object reference;
    }
}
