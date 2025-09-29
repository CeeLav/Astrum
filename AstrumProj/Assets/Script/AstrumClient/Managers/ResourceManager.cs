using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Astrum.CommonBase;
using UnityEngine.SceneManagement;

#if YOO_ASSET_2
using YooAsset;
#endif

namespace Astrum.Client.Managers
{
    /// <summary>
    /// 资源管理器 - 基于YooAsset
    /// </summary>
    public class ResourceManager : Singleton<ResourceManager>
    {
        [Header("资源管理设置")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private string defaultPackageName = "DefaultPackage";
        
#if YOO_ASSET_2
        // 资源包引用
        private ResourcePackage _defaultPackage;
        
        // 已加载的资源句柄缓存
        private Dictionary<string, AssetHandle> _loadedHandles = new Dictionary<string, AssetHandle>();
#else
        // 已加载的资源缓存（回退到Resources）
        private Dictionary<string, UnityEngine.Object> _loadedResources = new Dictionary<string, UnityEngine.Object>();
#endif
        
        // 加载进度
        private float _loadingProgress = 0f;
        
        // 初始化状态
        private bool _isInitialized = false;
        
        /// <summary>
        /// 初始化资源管理器
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                if (enableLogging)
                    Debug.LogWarning("ResourceManager: 已经初始化过了");
                return;
            }

#if YOO_ASSET_2
            if (enableLogging)
                Debug.Log("ResourceManager: 开始初始化YooAsset资源管理器");

            try
            {
                // 初始化YooAsset
                InitializeYooAsset();
                
                // 获取默认资源包
                _defaultPackage = YooAssets.GetPackage(defaultPackageName);
                if (_defaultPackage == null)
                {
                    Debug.LogError($"ResourceManager: 无法获取资源包 {defaultPackageName}");
                    return;
                }

                // 启动异步初始化协程
                if (Application.isPlaying)
                {
                    var coroutineRunner = UnityEngine.Object.FindObjectOfType<MonoBehaviour>();
                    if (coroutineRunner != null)
                    {
                        coroutineRunner.StartCoroutine(InitPackage());
                    }
                    else
                    {
                        Debug.LogError("ResourceManager: 无法找到MonoBehaviour来启动协程");
                        _isInitialized = false;
                    }
                }
                else
                {
                    // 编辑器模式下，暂时标记为已初始化
                    _isInitialized = true;
                    _loadingProgress = 1f;
                    if (enableLogging)
                        Debug.Log("ResourceManager: 编辑器模式下跳过异步初始化");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ResourceManager: 初始化失败 - {ex.Message}");
                _isInitialized = false;
            }
#else
            if (enableLogging)
                Debug.Log("ResourceManager: 初始化简化资源管理器（使用Resources）");
            
            _isInitialized = true;
            _loadingProgress = 1f;
#endif
        }

#if YOO_ASSET_2
        /// <summary>
        /// 初始化YooAsset
        /// </summary>
        private void InitializeYooAsset()
        {
            // 初始化YooAsset
            YooAssets.Initialize();
            
            // 创建默认资源包
            var package = YooAssets.CreatePackage(defaultPackageName);
            
            // 设置默认资源包
            YooAssets.SetDefaultPackage(package);
            
            if (enableLogging)
                Debug.Log("ResourceManager: YooAsset包已创建，开始异步初始化");
        }

        /// <summary>
        /// 异步初始化YooAsset包
        /// </summary>
        private IEnumerator InitPackage()
        {  
            //var buildResult = EditorSimulateModeHelper.SimulateBuild("DefaultPackage");

            var packageRoot =
                "D:\\Develop\\Projects\\Astrum\\AstrumProj\\Bundles\\StandaloneWindows64\\DefaultPackage\\Simulate";//buildResult.PackageRootDirectory;
            var fileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            
            var createParameters = new EditorSimulateModeParameters();
            createParameters.EditorFileSystemParameters = fileSystemParams;
            
            var initOperation = _defaultPackage.InitializeAsync(createParameters);
            yield return initOperation;
            
            if(initOperation.Status != EOperationStatus.Succeed)
                yield break;
            var operation = _defaultPackage.RequestPackageVersionAsync();
            yield return operation;
            if (operation.Status != EOperationStatus.Succeed)
                yield break;
            
            var operation2 = _defaultPackage.UpdatePackageManifestAsync(operation.PackageVersion);
            yield return operation2;
            if (operation2.Status != EOperationStatus.Succeed)
                yield break; 
            _isInitialized = true;
            _loadingProgress = 1f;
            
            Debug.Log("ResourceManager: 资源包初始化成功！");
            
        }
#endif
        
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="assetName">资源名称</param>
        /// <returns>加载的资源</returns>
        public T LoadResource<T>(string assetName) where T : UnityEngine.Object
        {
            if (!_isInitialized)
            {
                Debug.LogError("ResourceManager: 资源管理器未初始化");
                return null;
            }

            if (string.IsNullOrEmpty(assetName))
            {
                Debug.LogError("ResourceManager: 资源名称不能为空");
                return null;
            }
            
#if YOO_ASSET_2
            // 检查缓存
            if (_loadedHandles.ContainsKey(assetName))
            {
                if (enableLogging)
                    Debug.Log($"ResourceManager: 从缓存加载资源 {assetName}");
                
                var handle = _loadedHandles[assetName];
                return handle.AssetObject as T;
            }
            
            try
            {
                // 使用YooAsset同步加载资源
                var handle = _defaultPackage.LoadAssetSync<T>(assetName);
                
                if (handle.Status == EOperationStatus.Succeed)
                {
                    // 缓存资源句柄
                    _loadedHandles[assetName] = handle;
                    
                    if (enableLogging)
                        Debug.Log($"ResourceManager: 成功加载资源 {assetName}");
                    
                    return handle.AssetObject as T;
                }
                else
                {
                    Debug.LogError($"ResourceManager: 加载资源 {assetName} 失败 - {handle.LastError}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ResourceManager: 加载资源 {assetName} 失败 - {ex.Message}");
                return null;
            }
#else
            // 检查缓存
            if (_loadedResources.ContainsKey(assetName))
            {
                if (enableLogging)
                    Debug.Log($"ResourceManager: 从缓存加载资源 {assetName}");
                
                return _loadedResources[assetName] as T;
            }
            
            try
            {
                // 从Resources文件夹加载资源
                T resource = Resources.Load<T>(assetName);
                
                if (resource != null)
                {
                    // 缓存资源
                    _loadedResources[assetName] = resource;
                    
                    if (enableLogging)
                        Debug.Log($"ResourceManager: 成功加载资源 {assetName}");
                    
                    return resource;
                }
                else
                {
                    Debug.LogError($"ResourceManager: 未找到资源 {assetName}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ResourceManager: 加载资源 {assetName} 失败 - {ex.Message}");
                return null;
            }
#endif
        }

        
        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="assetName">资源名称</param>
        public void UnloadResource(string assetName)
        {
#if YOO_ASSET_2
            if (_loadedHandles.ContainsKey(assetName))
            {
                var handle = _loadedHandles[assetName];
                if (handle != null)
                {
                    handle.Release();
                }
                
                _loadedHandles.Remove(assetName);
                
                if (enableLogging)
                    Debug.Log($"ResourceManager: 卸载资源 {assetName}");
            }
#else
            if (_loadedResources.ContainsKey(assetName))
            {
                var resource = _loadedResources[assetName];
                if (resource != null)
                {
                    Resources.UnloadAsset(resource);
                }
                
                _loadedResources.Remove(assetName);
                
                if (enableLogging)
                    Debug.Log($"ResourceManager: 卸载资源 {assetName}");
            }
#endif
        }
        
        /// <summary>
        /// 检查资源是否已加载
        /// </summary>
        /// <param name="assetName">资源名称</param>
        /// <returns>是否已加载</returns>
        public bool IsResourceLoaded(string assetName)
        {
#if YOO_ASSET_2
            return _loadedHandles.ContainsKey(assetName);
#else
            return _loadedResources.ContainsKey(assetName);
#endif
        }
        
        /// <summary>
        /// 获取加载进度
        /// </summary>
        /// <returns>加载进度 (0-1)</returns>
        public float GetLoadProgress()
        {
            return _loadingProgress;
        }
        
        /// <summary>
        /// 清理所有缓存的资源
        /// </summary>
        public void ClearCache()
        {
#if YOO_ASSET_2
            foreach (var kvp in _loadedHandles)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Release();
                }
            }
            
            _loadedHandles.Clear();
#else
            foreach (var kvp in _loadedResources)
            {
                if (kvp.Value != null)
                {
                    Resources.UnloadAsset(kvp.Value);
                }
            }
            
            _loadedResources.Clear();
#endif
            
            if (enableLogging)
                Debug.Log("ResourceManager: 清理所有缓存资源");
        }

        #region 场景加载功能

        /// <summary>
        /// 同步加载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="loadSceneMode">加载模式</param>
        /// <returns>是否加载成功</returns>
        public bool LoadScene(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode = UnityEngine.SceneManagement.LoadSceneMode.Single)
        {
            if (!_isInitialized)
            {
                Debug.LogError("ResourceManager: 资源管理器未初始化");
                return false;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("ResourceManager: 场景名称不能为空");
                return false;
            }

#if YOO_ASSET_2
            // 尝试多种地址格式加载场景
            string[] possibleAddresses = {
                sceneName,  // 直接使用场景名
                $"Assets/Scenes/{sceneName}.unity",  // 完整路径
                $"{sceneName}.unity"  // 带扩展名的文件名
            };
            
            foreach (var address in possibleAddresses)
            {
                try
                {
                    if (enableLogging)
                        Debug.Log($"ResourceManager: 尝试同步加载场景地址: {address}");
                    
                    var handle = _defaultPackage.LoadSceneSync(address, loadSceneMode);
                    
                    if (handle.Status == EOperationStatus.Succeed)
                    {
                        if (enableLogging)
                            Debug.Log($"ResourceManager: 成功加载场景 {sceneName}，使用地址: {address}");
                        return true;
                    }
                    else
                    {
                        if (enableLogging)
                            Debug.LogWarning($"ResourceManager: 地址 {address} 加载失败: {handle.LastError}");
                        handle?.Release();
                    }
                }
                catch (Exception ex)
                {
                    if (enableLogging)
                        Debug.LogWarning($"ResourceManager: 地址 {address} 加载异常: {ex.Message}");
                }
            }
            
            Debug.LogError($"ResourceManager: 加载场景 {sceneName} 失败，尝试了所有地址格式");
            return false;
#else
            try
            {
                // 使用Unity SceneManager加载场景
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, loadSceneMode);
                
                if (enableLogging)
                    Debug.Log($"ResourceManager: 成功加载场景 {sceneName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ResourceManager: 加载场景 {sceneName} 失败 - {ex.Message}");
                return false;
            }
#endif
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="loadSceneMode">加载模式</param>
        /// <param name="onLoaded">加载完成回调</param>
        /// <returns>协程</returns>
        public IEnumerator LoadSceneAsync(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, Action<bool> onLoaded)
        {
            if (!_isInitialized)
            {
                Debug.LogError("ResourceManager: 资源管理器未初始化");
                onLoaded?.Invoke(false);
                yield break;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("ResourceManager: 场景名称不能为空");
                onLoaded?.Invoke(false);
                yield break;
            }

#if YOO_ASSET_2
            // 尝试多种地址格式加载场景
            string[] possibleAddresses = {
                $"Assets/Scenes/{sceneName}.unity",  // 完整路径
                
                sceneName,  // 直接使用场景名
                $"{sceneName}.unity"  // 带扩展名的文件名
            };
            
            SceneHandle handle = null;
            string usedAddress = "";
            
            // 尝试不同的地址格式
            foreach (var address in possibleAddresses)
            {
                if (enableLogging)
                    Debug.Log($"ResourceManager: 尝试加载场景地址: {address}");
                
                handle = _defaultPackage.LoadSceneAsync(address, loadSceneMode);
                yield return handle;
                
                if (handle.Status == EOperationStatus.Succeed)
                {
                    usedAddress = address;
                    break;
                }
                else
                {
                    if (enableLogging)
                        Debug.LogWarning($"ResourceManager: 地址 {address} 加载失败: {handle.LastError}");
                    handle?.Release();
                    handle = null;
                }
            }
            
            if (handle != null && handle.Status == EOperationStatus.Succeed)
            {
                if (enableLogging)
                    Debug.Log($"ResourceManager: 成功异步加载场景 {sceneName}，使用地址: {usedAddress}");
                onLoaded?.Invoke(true);
            }
            else
            {
                Debug.LogError($"ResourceManager: 异步加载场景 {sceneName} 失败，尝试了所有地址格式");
                onLoaded?.Invoke(false);
            }
#else
            // 使用Unity SceneManager异步加载场景
            var operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            yield return operation;
            
            if (operation.isDone)
            {
                if (enableLogging)
                    Debug.Log($"ResourceManager: 成功异步加载场景 {sceneName}");
                onLoaded?.Invoke(true);
            }
            else
            {
                Debug.LogError($"ResourceManager: 异步加载场景 {sceneName} 失败");
                onLoaded?.Invoke(false);
            }
#endif
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <returns>是否卸载成功</returns>
        public bool UnloadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("ResourceManager: 场景名称不能为空");
                return false;
            }

            try
            {
                // 查找场景
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
                
                if (scene.IsValid())
                {
                    var operation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
                    
                    if (operation != null)
                    {
                        if (enableLogging)
                            Debug.Log($"ResourceManager: 成功卸载场景 {sceneName}");
                        return true;
                    }
                    else
                    {
                        Debug.LogError($"ResourceManager: 卸载场景 {sceneName} 失败");
                        return false;
                    }
                }
                else
                {
                    Debug.LogWarning($"ResourceManager: 场景 {sceneName} 不存在或未加载");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ResourceManager: 卸载场景 {sceneName} 失败 - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查场景是否已加载
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <returns>是否已加载</returns>
        public bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        /// <summary>
        /// 获取当前加载的场景列表
        /// </summary>
        /// <returns>场景名称列表</returns>
        public List<string> GetLoadedScenes()
        {
            var loadedScenes = new List<string>();
            
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded)
                {
                    loadedScenes.Add(scene.name);
                }
            }
            
            return loadedScenes;
        }

        #endregion
        
        /// <summary>
        /// 关闭资源管理器
        /// </summary>
        public void Shutdown()
        {
            if (enableLogging)
                Debug.Log("ResourceManager: 关闭资源管理器");
            
            ClearCache();
            _loadingProgress = 0f;
            _isInitialized = false;
            
#if YOO_ASSET_2
            // 销毁YooAsset
            YooAssets.Destroy();
#endif
        }

        /// <summary>
        /// 获取资源管理器状态信息
        /// </summary>
        /// <returns>状态信息</returns>
        public string GetStatusInfo()
        {
            if (!_isInitialized)
                return "ResourceManager: 未初始化";
            
            var loadedScenes = GetLoadedScenes();
            var sceneInfo = loadedScenes.Count > 0 ? $", 已加载场景: {string.Join(", ", loadedScenes)}" : "";
            
#if YOO_ASSET_2
            return $"ResourceManager: 已初始化(YooAsset), 缓存资源数量: {_loadedHandles.Count}, 进度: {_loadingProgress:F2}{sceneInfo}";
#else
            return $"ResourceManager: 已初始化(Resources), 缓存资源数量: {_loadedResources.Count}, 进度: {_loadingProgress:F2}{sceneInfo}";
#endif
        }
    }
}