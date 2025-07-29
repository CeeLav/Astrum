using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using Astrum.CommonBase;

namespace Astrum.Client.Managers
{
    /// <summary>
    /// 协程运行器组件
    /// </summary>
    public class CoroutineRunner : MonoBehaviour
    {
        // 这个类专门用于运行协程
    }

    /// <summary>
    /// 场景管理器 - 简化版本
    /// </summary>
    public class SceneManager : Singleton<SceneManager>
    {
        [Header("场景管理设置")]
        private bool enableLogging = true;
        private float asyncLoadTimeout = 30f;
        
        // 当前场景信息
        private Scene currentScene;
        private bool isLoading = false;
        private float loadingProgress = 0f;
        
        // 异步加载相关
        private AsyncOperation currentAsyncOperation;
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
            GameObject runnerGO = new GameObject("SceneManager Coroutine Runner");
            coroutineRunner = runnerGO.AddComponent<CoroutineRunner>();
            UnityEngine.Object.DontDestroyOnLoad(runnerGO);
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
                // 根据模式加载场景
                switch (mode)
                {
                    case LoadSceneMode.SINGLE:
                        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                        break;
                        
                    case LoadSceneMode.ADDITIVE:
                        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
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
            
            // 开始异步加载
            currentAsyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            
            if (currentAsyncOperation == null)
            {
                Debug.LogError($"SceneManager: 无法开始异步加载场景 {sceneName}");
                isLoading = false;
                yield break;
            }
            
            currentAsyncOperation.allowSceneActivation = true;
            
            // 等待加载完成
            float startTime = Time.time;
            while (!currentAsyncOperation.isDone)
            {
                loadingProgress = currentAsyncOperation.progress;
                
                // 检查超时
                if (Time.time - startTime > asyncLoadTimeout)
                {
                    Debug.LogError($"SceneManager: 异步加载场景 {sceneName} 超时");
                    break;
                }
                
                yield return null;
            }
            
            // 加载完成
            loadingProgress = 1f;
            currentScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            
            if (enableLogging)
                Debug.Log($"SceneManager: 异步加载场景 {sceneName} 完成");
            
            // 调用回调
            callback?.Invoke();
            
            // 清理
            isLoading = false;
            currentAsyncOperation = null;
        }
        
        /// <summary>
        /// 卸载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public void UnloadScene(string sceneName)
        {
            if (enableLogging)
                Debug.Log($"SceneManager: 卸载场景 {sceneName}");
            
            try
            {
                Scene sceneToUnload = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
                if (sceneToUnload.isLoaded)
                {
                    UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
                    
                    if (enableLogging)
                        Debug.Log($"SceneManager: 场景 {sceneName} 卸载完成");
                }
                else
                {
                    Debug.LogWarning($"SceneManager: 场景 {sceneName} 未加载，无法卸载");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"SceneManager: 卸载场景 {sceneName} 失败 - {ex.Message}");
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
            currentAsyncOperation = null;
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