using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using Astrum.Client.Behaviour;
using Astrum.Client.Core;
using Astrum.CommonBase;

namespace Astrum.Client.Managers
{


    /// <summary>
    /// 场景管理器 - 简化版本
    /// </summary>
    public class SceneManager : Singleton<SceneManager>
    {
        [Header("场景管理设置")]
        private bool enableLogging = true;
        
        // 当前场景信息
        private Scene currentScene;
        private bool isLoading = false;
        private float loadingProgress = 0f;
        
        // 协程运行器
        private MonoBehaviour coroutineRunner;
        
        // 公共属性
        public Scene CurrentScene => currentScene;
        public bool IsLoading => isLoading;
        public float LoadingProgress => loadingProgress;
        
        /// <summary>
        /// 初始化场景管理器
        /// </summary>
        public void Initialize()
        {
            if (enableLogging)
                Debug.Log("SceneManager: 初始化场景管理器");
            
            // 获取当前场景
            currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            
            // 创建协程运行器
            CreateCoroutineRunner();
            
            if (enableLogging)
                Debug.Log($"SceneManager: 当前场景 - {currentScene.name}");
        }
        
        /// <summary>
        /// 创建协程运行器
        /// </summary>
        private void CreateCoroutineRunner()
        {
            // 尝试从GameApplication获取CoroutineRunner引用
            var gameAppCoroutineRunner = GameApplication.Instance?.CoroutineRunner;
            if (gameAppCoroutineRunner != null)
            {
                coroutineRunner = gameAppCoroutineRunner;
                Debug.Log("SceneManager: 使用GameApplication中的CoroutineRunner引用");
            }
            else
            {
                // 如果没有，创建自己的CoroutineRunner
                GameObject runnerGO = new GameObject("SceneManager Coroutine Runner");
                coroutineRunner = runnerGO.AddComponent<CoroutineRunner>();
                UnityEngine.Object.DontDestroyOnLoad(runnerGO);
                Debug.Log("SceneManager: 创建独立的CoroutineRunner");
            }
        }
        
        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="mode">加载模式</param>
        public void LoadScene(string sceneName, LoadSceneMode mode)
        {
            if (isLoading)
            {
                Debug.LogWarning($"SceneManager: 正在加载场景中，无法加载 {sceneName}");
                return;
            }
            
            if (enableLogging)
                Debug.Log($"SceneManager: 开始加载场景 {sceneName}，模式 {mode}");
            
            try
            {
                // 获取ResourceManager实例
                var resourceManager = GameApplication.Instance?.ResourceManager;
                if (resourceManager == null)
                {
                    Debug.LogError("SceneManager: ResourceManager未初始化");
                    return;
                }
                
                // 根据模式加载场景
                switch (mode)
                {
                    case LoadSceneMode.SINGLE:
                        if (resourceManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single))
                        {
                            if (enableLogging)
                                Debug.Log($"SceneManager: 成功加载场景 {sceneName}");
                        }
                        else
                        {
                            Debug.LogError($"SceneManager: 加载场景 {sceneName} 失败");
                            return;
                        }
                        break;
                        
                    case LoadSceneMode.ADDITIVE:
                        if (resourceManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive))
                        {
                            if (enableLogging)
                                Debug.Log($"SceneManager: 成功加载场景 {sceneName}");
                        }
                        else
                        {
                            Debug.LogError($"SceneManager: 加载场景 {sceneName} 失败");
                            return;
                        }
                        break;
                        
                    case LoadSceneMode.ASYNC:
                        LoadSceneAsync(sceneName);
                        return;
                        
                    default:
                        Debug.LogError($"SceneManager: 不支持的加载模式 {mode}");
                        return;
                }
                
                // 更新当前场景
                currentScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
                isLoading = false;
                loadingProgress = 1f;
                
                if (enableLogging)
                    Debug.Log($"SceneManager: 场景 {sceneName} 加载完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SceneManager: 加载场景 {sceneName} 失败 - {ex.Message}");
                isLoading = false;
                loadingProgress = 0f;
            }
        }
        
        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="callback">加载完成回调</param>
        public void LoadSceneAsync(string sceneName, Action callback = null)
        {
            if (isLoading)
            {
                Debug.LogWarning($"SceneManager: 正在加载场景中，无法加载 {sceneName}");
                return;
            }
            
            if (enableLogging)
                Debug.Log($"SceneManager: 开始异步加载场景 {sceneName}");
            
            if (coroutineRunner != null)
            {
                coroutineRunner.StartCoroutine(LoadSceneAsyncCoroutine(sceneName, callback));
            }
            else
            {
                Debug.LogError("SceneManager: 协程运行器未初始化");
            }
        }
        
        /// <summary>
        /// 异步加载场景协程
        /// </summary>
        private IEnumerator LoadSceneAsyncCoroutine(string sceneName, Action callback)
        {
            isLoading = true;
            loadingProgress = 0f;
            
            // 获取ResourceManager实例
            var resourceManager = GameApplication.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("SceneManager: ResourceManager未初始化");
                isLoading = false;
                yield break;
            }
            
            // 使用ResourceManager异步加载场景
            yield return resourceManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single, (success) =>
            {
                if (success)
                {
                    if (enableLogging)
                        Debug.Log($"SceneManager: 成功异步加载场景 {sceneName}");
                    
                    // 更新当前场景
                    currentScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
                    isLoading = false;
                    loadingProgress = 1f;
                    
                    // 调用回调
                    callback?.Invoke();
                }
                else
                {
                    Debug.LogError($"SceneManager: 异步加载场景 {sceneName} 失败");
                    isLoading = false;
                    loadingProgress = 0f;
                }
            });
        }
        
        /// <summary>
        /// 卸载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public void UnloadScene(string sceneName)
        {
            if (enableLogging)
                Debug.Log($"SceneManager: 卸载场景 {sceneName}");
            
            // 获取ResourceManager实例
            var resourceManager = GameApplication.Instance?.ResourceManager;
            if (resourceManager == null)
            {
                Debug.LogError("SceneManager: ResourceManager未初始化");
                return;
            }
            
            // 使用ResourceManager卸载场景
            if (resourceManager.UnloadScene(sceneName))
            {
                if (enableLogging)
                    Debug.Log($"SceneManager: 场景 {sceneName} 卸载完成");
            }
            else
            {
                Debug.LogWarning($"SceneManager: 场景 {sceneName} 未加载，无法卸载");
            }
        }
        
        /// <summary>
        /// 获取当前场景
        /// </summary>
        /// <returns>当前场景</returns>
        public Scene GetCurrentScene()
        {
            return currentScene;
        }
        
        /// <summary>
        /// 检查场景是否已加载
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <returns>是否已加载</returns>
        public bool IsSceneLoaded(string sceneName)
        {
            return UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName).isLoaded;
        }
        
        /// <summary>
        /// 获取加载进度
        /// </summary>
        /// <returns>加载进度 (0-1)</returns>
        public float GetLoadProgress()
        {
            return loadingProgress;
        }
        
        /// <summary>
        /// 激活场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public void ActivateScene(string sceneName)
        {
            Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                currentScene = scene;
                
                if (enableLogging)
                    Debug.Log($"SceneManager: 激活场景 {sceneName}");
            }
            else
            {
                Debug.LogWarning($"SceneManager: 场景 {sceneName} 未加载，无法激活");
            }
        }
        
        /// <summary>
        /// 关闭场景管理器
        /// </summary>
        public void Shutdown()
        {
            if (enableLogging)
                Debug.Log("SceneManager: 关闭场景管理器");
            
            isLoading = false;
            loadingProgress = 0f;
        }
    }
    
    /// <summary>
    /// 场景加载模式枚举
    /// </summary>
    public enum LoadSceneMode
    {
        SINGLE,     // 单场景模式，卸载其他场景
        ADDITIVE,   // 叠加模式，保留其他场景
        ASYNC       // 异步加载
    }
}