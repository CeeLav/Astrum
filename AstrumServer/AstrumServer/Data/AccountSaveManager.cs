using System;
using System.IO;
using Astrum.CommonBase;
using Astrum.LogicCore.Data;
using MemoryPack;

namespace AstrumServer.Data
{
    /// <summary>
    /// 账号存档管理器 - 管理服务器端账号存档的持久化
    /// 服务器端和客户端共享 PlayerProgressData 类型（位于 LogicCore.Data）
    /// </summary>
    public class AccountSaveManager
    {
        private readonly string _saveDataPath;
        
        public AccountSaveManager(string saveDataPath = null)
        {
            _saveDataPath = saveDataPath ?? Path.Combine(
                AppContext.BaseDirectory, 
                "Data", 
                "AccountSaves"
            );
            Directory.CreateDirectory(_saveDataPath);
        }
        
        /// <summary>
        /// 获取账号存档路径（使用客户端实例ID）
        /// </summary>
        private string GetAccountSavePath(string clientInstanceId)
        {
            if (string.IsNullOrEmpty(clientInstanceId))
            {
                throw new ArgumentException("ClientInstanceId cannot be null or empty", nameof(clientInstanceId));
            }
            
            var userDir = Path.Combine(_saveDataPath, clientInstanceId);
            Directory.CreateDirectory(userDir);
            return Path.Combine(userDir, "PlayerProgressData.dat");
        }
        
        /// <summary>
        /// 加载账号存档（使用客户端实例ID）
        /// </summary>
        public PlayerProgressData LoadAccountSave(string clientInstanceId)
        {
            var path = GetAccountSavePath(clientInstanceId);
            
            if (!File.Exists(path))
            {
                ASLogger.Instance.Info($"AccountSaveManager: 账号存档不存在 - ClientInstanceId: {clientInstanceId}");
                return null;
            }
            
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var data = MemoryPackSerializer.Deserialize<PlayerProgressData>(bytes);
                ASLogger.Instance.Info($"AccountSaveManager: 成功加载账号存档 - ClientInstanceId: {clientInstanceId}");
                return data;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"AccountSaveManager: 加载账号存档失败 - ClientInstanceId: {clientInstanceId}, Error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 保存账号存档（使用客户端实例ID）
        /// </summary>
        public bool SaveAccountSave(string clientInstanceId, PlayerProgressData data)
        {
            if (data == null)
            {
                ASLogger.Instance.Warning($"AccountSaveManager: 存档数据为空，无法保存 - ClientInstanceId: {clientInstanceId}");
                return false;
            }
            
            var path = GetAccountSavePath(clientInstanceId);
            
            try
            {
                byte[] bytes = MemoryPackSerializer.Serialize(data);
                File.WriteAllBytes(path, bytes);
                ASLogger.Instance.Info($"AccountSaveManager: 成功保存账号存档 - ClientInstanceId: {clientInstanceId}");
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"AccountSaveManager: 保存账号存档失败 - ClientInstanceId: {clientInstanceId}, Error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 删除账号存档（使用客户端实例ID）
        /// </summary>
        public bool DeleteAccountSave(string clientInstanceId)
        {
            var path = GetAccountSavePath(clientInstanceId);
            
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    ASLogger.Instance.Info($"AccountSaveManager: 成功删除账号存档 - ClientInstanceId: {clientInstanceId}");
                }
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"AccountSaveManager: 删除账号存档失败 - ClientInstanceId: {clientInstanceId}, Error: {ex.Message}");
                return false;
            }
        }
    }
}

