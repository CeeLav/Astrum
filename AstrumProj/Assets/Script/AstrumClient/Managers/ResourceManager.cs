using UnityEngine;
using System;
using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.Client.Managers
{
    /// <summary>
    /// 资源管理器 - 简化版本
    /// </summary>
    public class ResourceManager : Singleton<ResourceManager>
    {
        [Header("资源管理设置")]
        private bool enableLogging = true;
        
        // 已加载的资源缓存
        private Dictionary<string, UnityEngine.Object> loadedResources = new Dictionary<string, UnityEngine.Object>();
        
        // 加载进度
        private float loadingProgress = 0f;
        
        /// <summary>
        /// 初始化资源管理器
        /// </summary>
        public void Initialize()
        {
            if (enableLogging)
                Debug.Log("ResourceManager: 初始化资源管理器");
            
            loadingProgress = 1f; // 简化版本直接标记为已完成
        }
        
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <returns>加载的资源</returns>
        public T LoadResource<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("ResourceManager: 资源路径不能为空");
                return null;
            }
            
            // 检查缓存
            if (loadedResources.ContainsKey(path))
            {
                if (enableLogging)
                    Debug.Log($"ResourceManager: 从缓存加载资源 {path}");
                
                return loadedResources[path] as T;
            }
            
            try
            {
                // 从Resources文件夹加载资源
                T resource = Resources.Load<T>(path);
                
                if (resource != null)
                {
                    // 缓存资源
                    loadedResources[path] = resource;
                    
                    if (enableLogging)
                        Debug.Log($"ResourceManager: 成功加载资源 {path}");
                    
                    return resource;
                }
                else
                {
                    Debug.LogError($"ResourceManager: 未找到资源 {path}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ResourceManager: 加载资源 {path} 失败 - {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="path">资源路径</param>
        public void UnloadResource(string path)
        {
            if (loadedResources.ContainsKey(path))
            {
                var resource = loadedResources[path];
                if (resource != null)
                {
                    Resources.UnloadAsset(resource);
                }
                
                loadedResources.Remove(path);
                
                if (enableLogging)
                    Debug.Log($"ResourceManager: 卸载资源 {path}");
            }
        }
        
        /// <summary>
        /// 检查资源是否已加载
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>是否已加载</returns>
        public bool IsResourceLoaded(string path)
        {
            return loadedResources.ContainsKey(path);
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
        /// 清理所有缓存的资源
        /// </summary>
        public void ClearCache()
        {
            foreach (var kvp in loadedResources)
            {
                if (kvp.Value != null)
                {
                    Resources.UnloadAsset(kvp.Value);
                }
            }
            
            loadedResources.Clear();
            
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
            loadingProgress = 0f;
        }
    }
}