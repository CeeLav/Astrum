using System.IO;
using Astrum.CommonBase;
using MemoryPack;
using UnityEngine;

namespace Astrum.Client.Data
{
    /// <summary>
    /// 保存系统 - 负责玩家数据的持久化
    /// </summary>
    public static class SaveSystem
    {
        private static string PlayerProgressDataPath => 
            Path.Combine(Application.persistentDataPath, "PlayerProgressData.dat");
        
        /// <summary>
        /// 加载玩家进度数据
        /// </summary>
        public static PlayerProgressData LoadPlayerProgressData()
        {
            if (!File.Exists(PlayerProgressDataPath))
            {
                ASLogger.Instance.Info("SaveSystem: 存档文件不存在");
                return null;
            }
            
            try
            {
                byte[] bytes = File.ReadAllBytes(PlayerProgressDataPath);
                var data = MemoryPackSerializer.Deserialize<PlayerProgressData>(bytes);
                ASLogger.Instance.Info("SaveSystem: 成功加载玩家进度数据");
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
        public static void SavePlayerProgressData(PlayerProgressData data)
        {
            try
            {
                byte[] bytes = MemoryPackSerializer.Serialize(data);
                File.WriteAllBytes(PlayerProgressDataPath, bytes);
                ASLogger.Instance.Info("SaveSystem: 成功保存玩家进度数据");
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"SaveSystem: 保存玩家进度数据失败 - {ex.Message}");
            }
        }
    }
}

