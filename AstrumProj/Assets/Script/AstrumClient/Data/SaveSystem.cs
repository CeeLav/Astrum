using System.IO;
using Astrum.CommonBase;
using Astrum.LogicCore.Data;
using MemoryPack;
using UnityEngine;

namespace Astrum.Client.Data
{
    /// <summary>
    /// 保存系统 - 负责玩家数据的持久化
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>
        /// 存档类型
        /// </summary>
        public enum SaveType
        {
            Local,      // 单人存档
            Account      // 账号存档（暂存，需同步到服务器）
        }
        
        /// <summary>
        /// 获取单人存档路径
        /// </summary>
        private static string GetLocalSavePath()
        {
            var clientInstanceId = ClientInstanceIdManager.GetOrCreateInstanceId();
            var saveDir = Path.Combine(Application.persistentDataPath, "LocalSaves", clientInstanceId);
            Directory.CreateDirectory(saveDir);
            return Path.Combine(saveDir, "PlayerProgressData.dat");
        }
        
        /// <summary>
        /// 获取账号存档路径（客户端暂存）
        /// </summary>
        private static string GetAccountSavePath(string clientInstanceId = null)
        {
            // 如果没有提供，使用当前实例的客户端ID
            if (string.IsNullOrEmpty(clientInstanceId))
            {
                clientInstanceId = ClientInstanceIdManager.GetOrCreateInstanceId();
            }
            
            var saveDir = Path.Combine(Application.persistentDataPath, "AccountSaves", clientInstanceId);
            Directory.CreateDirectory(saveDir);
            return Path.Combine(saveDir, "PlayerProgressData.dat");
        }
        
        /// <summary>
        /// 加载玩家进度数据
        /// </summary>
        /// <param name="saveType">存档类型</param>
        /// <param name="clientInstanceId">客户端实例ID（账号存档时使用，如果为null则使用当前实例的ID）</param>
        public static PlayerProgressData LoadPlayerProgressData(SaveType saveType = SaveType.Local, string clientInstanceId = null)
        {
            string path = saveType == SaveType.Local 
                ? GetLocalSavePath() 
                : GetAccountSavePath(clientInstanceId);
            
            if (!File.Exists(path))
            {
                //ASLogger.Instance.Info($"SaveSystem: 存档文件不存在 - {path}");
                return null;
            }
            
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var data = MemoryPackSerializer.Deserialize<PlayerProgressData>(bytes);
                //ASLogger.Instance.Info($"SaveSystem: 成功加载玩家进度数据 - {path}");
                return data;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"SaveSystem: 加载玩家进度数据失败 - {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 保存玩家进度数据
        /// </summary>
        /// <param name="data">进度数据</param>
        /// <param name="saveType">存档类型</param>
        /// <param name="clientInstanceId">客户端实例ID（账号存档时使用，如果为null则使用当前实例的ID）</param>
        public static void SavePlayerProgressData(PlayerProgressData data, SaveType saveType = SaveType.Local, string clientInstanceId = null)
        {
            string path = saveType == SaveType.Local 
                ? GetLocalSavePath() 
                : GetAccountSavePath(clientInstanceId);
            
            try
            {
                byte[] bytes = MemoryPackSerializer.Serialize(data);
                File.WriteAllBytes(path, bytes);
                //ASLogger.Instance.Info($"SaveSystem: 成功保存玩家进度数据 - {path}");
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"SaveSystem: 保存玩家进度数据失败 - {ex.Message}");
            }
        }
        
        // 保留旧接口以保持向后兼容性
        /// <summary>
        /// 加载玩家进度数据（旧接口，默认使用单人存档）
        /// </summary>
        [System.Obsolete("使用 LoadPlayerProgressData(SaveType, string) 替代")]
        public static PlayerProgressData LoadPlayerProgressData()
        {
            return LoadPlayerProgressData(SaveType.Local);
        }
        
        /// <summary>
        /// 保存玩家进度数据（旧接口，默认使用单人存档）
        /// </summary>
        [System.Obsolete("使用 SavePlayerProgressData(PlayerProgressData, SaveType, string) 替代")]
        public static void SavePlayerProgressData(PlayerProgressData data)
        {
            SavePlayerProgressData(data, SaveType.Local);
        }
    }
}

