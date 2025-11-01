using System.IO;
using Astrum.CommonBase;
using UnityEngine;

namespace Astrum.Client.Data
{
    /// <summary>
    /// 玩家数据管理器 - 统一管理玩家进度数据
    /// </summary>
    public class PlayerDataManager : Singleton<PlayerDataManager>
    {
        private PlayerProgressData _progressData;
        
        /// <summary>当前玩家进度数据</summary>
        public PlayerProgressData ProgressData => _progressData;
        
        /// <summary>
        /// 初始化管理器
        /// </summary>
        public void Initialize()
        {
            ASLogger.Instance.Info("PlayerDataManager: 初始化玩家数据管理器");
            LoadProgressData();
        }
        
        /// <summary>
        /// 保存玩家进度数据
        /// </summary>
        public void SaveProgressData()
        {
            if (_progressData == null)
            {
                ASLogger.Instance.Warning("PlayerDataManager: 进度数据为空，无法保存");
                return;
            }
            
            SaveSystem.SavePlayerProgressData(_progressData);
        }
        
        /// <summary>
        /// 加载玩家进度数据
        /// </summary>
        public void LoadProgressData()
        {
            _progressData = SaveSystem.LoadPlayerProgressData();
            if (_progressData == null)
            {
                _progressData = CreateDefaultProgressData();
                ASLogger.Instance.Info("PlayerDataManager: 创建默认进度数据");
            }
            else
            {
                ASLogger.Instance.Info($"PlayerDataManager: 加载进度数据 - 等级 {_progressData.Level}, 经验 {_progressData.Exp}");
            }
        }
        
        /// <summary>
        /// 创建默认进度数据
        /// </summary>
        private PlayerProgressData CreateDefaultProgressData()
        {
            return new PlayerProgressData
            {
                Level = 1,
                Exp = 0,
                RoleId = 1001, // 默认角色ID
                AvailableStatPoints = 0,
                StarFragments = 0
            };
        }
    }
}

