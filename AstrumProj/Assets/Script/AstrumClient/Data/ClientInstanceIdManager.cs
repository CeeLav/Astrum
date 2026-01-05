using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
        
        private static string InstanceIdFilePath
        {
            get
            {
                // 为了确保每个实例都有唯一的ID文件
                // 如果使用 ParrelSync，每个克隆实例应该有独立的 persistentDataPath
                // 但为了确保唯一性，我们在文件名中包含实例标识
                var isClone = ParrelSyncHelper.IsClone();
                var instanceId = ParrelSyncHelper.GetInstanceId();
                
                // 如果是 ParrelSync 克隆实例，使用实例ID作为文件名
                // ParrelSync 会为每个克隆实例设置独立的 persistentDataPath，但为了保险起见，还是加上实例ID
                if (isClone)
                {
                    var fileName = $"ClientInstanceId_{instanceId}.dat";
                    return Path.Combine(Application.persistentDataPath, fileName);
                }
                else
                {
                    // Main 实例，使用固定的文件名
                    // 注意：如果有多个 Main 实例同时运行，它们会共享同一个ID文件
                    // 但通常 Main 实例只有一个，或者使用 ParrelSync 来创建多个实例
                    var fileName = "ClientInstanceId_Main.dat";
                    return Path.Combine(Application.persistentDataPath, fileName);
                }
            }
        }
        
        /// <summary>
        /// 获取或生成客户端实例ID
        /// </summary>
        public static string GetOrCreateInstanceId()
        {
            if (!string.IsNullOrEmpty(_cachedInstanceId))
            {
                //ASLogger.Instance.Info($"ClientInstanceIdManager: 使用缓存的实例ID - {_cachedInstanceId}");
                return _cachedInstanceId;
            }
            
            // 尝试从文件加载
            //ASLogger.Instance.Info($"ClientInstanceIdManager: 尝试从文件加载实例ID - 路径: {InstanceIdFilePath}");
            if (File.Exists(InstanceIdFilePath))
            {
                try
                {
                    _cachedInstanceId = File.ReadAllText(InstanceIdFilePath).Trim();
                    if (!string.IsNullOrEmpty(_cachedInstanceId))
                    {
                        //ASLogger.Instance.Info($"ClientInstanceIdManager: 成功加载已存在的实例ID - {_cachedInstanceId}");
                        return _cachedInstanceId;
                    }
                    else
                    {
                        ASLogger.Instance.Warning("ClientInstanceIdManager: 文件存在但内容为空，将生成新的ID");
                    }
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Warning($"ClientInstanceIdManager: 加载实例ID失败 - {ex.Message}");
                    ASLogger.Instance.LogException(ex, LogLevel.Warning);
                }
            }
            else
            {
                //ASLogger.Instance.Info($"ClientInstanceIdManager: 实例ID文件不存在，将生成新的ID - 路径: {InstanceIdFilePath}");
            }
            
            // 生成新的实例ID
            _cachedInstanceId = GenerateInstanceId();
            //ASLogger.Instance.Info($"ClientInstanceIdManager: 生成新的实例ID - {_cachedInstanceId}");
            
            // 保存到文件
            try
            {
                var directory = Path.GetDirectoryName(InstanceIdFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(InstanceIdFilePath, _cachedInstanceId);
                //ASLogger.Instance.Info($"ClientInstanceIdManager: 成功保存实例ID到文件 - {InstanceIdFilePath}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ClientInstanceIdManager: 保存实例ID失败 - {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
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

