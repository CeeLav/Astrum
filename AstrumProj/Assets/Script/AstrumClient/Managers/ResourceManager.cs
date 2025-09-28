using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Astrum.CommonBase;

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

                _isInitialized = true;
                _loadingProgress = 1f;
                
                if (enableLogging)
                    Debug.Log("ResourceManager: YooAsset资源管理器初始化完成");
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
            
#if YOO_ASSET_2
            return $"ResourceManager: 已初始化(YooAsset), 缓存资源数量: {_loadedHandles.Count}, 进度: {_loadingProgress:F2}";
#else
            return $"ResourceManager: 已初始化(Resources), 缓存资源数量: {_loadedResources.Count}, 进度: {_loadingProgress:F2}";
#endif
        }
    }
}