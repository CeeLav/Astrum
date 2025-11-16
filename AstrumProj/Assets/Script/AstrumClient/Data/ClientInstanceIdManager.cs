using System;
using System.IO;
using Astrum.CommonBase;
using UnityEngine;

namespace Astrum.Client.Data
{
    /// <summary>
    /// 客户端实例ID管理器 - 管理客户端实例的唯一标识符
    /// 用于稳定识别客户端实例，直接作为账号ID使用
    /// </summary>
    public static class ClientInstanceIdManager
    {
        private static string _cachedInstanceId;
        private static string InstanceIdFilePath => 
            Path.Combine(Application.persistentDataPath, "ClientInstanceId.dat");
        
        /// <summary>
        /// 获取或生成客户端实例ID
        /// </summary>
        public static string GetOrCreateInstanceId()
        {
            if (!string.IsNullOrEmpty(_cachedInstanceId))
            {
                return _cachedInstanceId;
            }
            
            // 尝试从文件加载
            if (File.Exists(InstanceIdFilePath))
            {
                try
                {
                    _cachedInstanceId = File.ReadAllText(InstanceIdFilePath).Trim();
                    if (!string.IsNullOrEmpty(_cachedInstanceId))
                    {
                        ASLogger.Instance.Info($"ClientInstanceIdManager: 加载已存在的实例ID - {_cachedInstanceId}");
                        return _cachedInstanceId;
                    }
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Warning($"ClientInstanceIdManager: 加载实例ID失败 - {ex.Message}");
                }
            }
            
            // 生成新的实例ID
            _cachedInstanceId = GenerateInstanceId();
            
            // 保存到文件
            try
            {
                var directory = Path.GetDirectoryName(InstanceIdFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(InstanceIdFilePath, _cachedInstanceId);
                ASLogger.Instance.Info($"ClientInstanceIdManager: 生成并保存新的实例ID - {_cachedInstanceId}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ClientInstanceIdManager: 保存实例ID失败 - {ex.Message}");
            }
            
            return _cachedInstanceId;
        }
        
        /// <summary>
        /// 生成实例ID
        /// 格式：client_{instanceId}_{timestamp}_{random}
        /// </summary>
        private static string GenerateInstanceId()
        {
            var instanceId = ParrelSyncHelper.GetInstanceId();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = UnityEngine.Random.Range(1000, 9999);
            
            // 格式：client_{instanceId}_{timestamp}_{random}
            return $"client_{instanceId}_{timestamp}_{random}";
        }
        
        /// <summary>
        /// 清除缓存的实例ID（用于测试）
        /// </summary>
        public static void ClearCache()
        {
            _cachedInstanceId = null;
        }
        
        /// <summary>
        /// 获取当前实例ID（不生成新的）
        /// </summary>
        public static string GetCurrentInstanceId()
        {
            return _cachedInstanceId ?? GetOrCreateInstanceId();
        }
    }
}

