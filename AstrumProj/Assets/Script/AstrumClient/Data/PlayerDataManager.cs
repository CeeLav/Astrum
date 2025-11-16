using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Data;
using Astrum.LogicCore.Inventory;
using UnityEngine;

namespace Astrum.Client.Data
{
    /// <summary>
    /// 玩家数据管理器 - 统一管理玩家进度数据
    /// </summary>
    public class PlayerDataManager : Singleton<PlayerDataManager>
    {
        private PlayerProgressData _progressData;
        private SaveSystem.SaveType _currentSaveType = SaveSystem.SaveType.Local;
        private string _currentClientInstanceId = null;
        
        /// <summary>当前玩家进度数据</summary>
        public PlayerProgressData ProgressData => _progressData;
        
        /// <summary>
        /// 当前存档类型
        /// </summary>
        public SaveSystem.SaveType CurrentSaveType => _currentSaveType;
        
        /// <summary>
        /// 当前客户端实例ID
        /// </summary>
        public string CurrentClientInstanceId => _currentClientInstanceId ?? ClientInstanceIdManager.GetOrCreateInstanceId();
        
        /// <summary>
        /// 初始化管理器
        /// </summary>
        /// <param name="saveType">存档类型</param>
        /// <param name="clientInstanceId">客户端实例ID（账号存档时使用，如果为null则使用当前实例的ID）</param>
        public void Initialize(SaveSystem.SaveType saveType = SaveSystem.SaveType.Local, string clientInstanceId = null)
        {
            _currentSaveType = saveType;
            _currentClientInstanceId = clientInstanceId ?? ClientInstanceIdManager.GetOrCreateInstanceId();
            ASLogger.Instance.Info($"PlayerDataManager: 初始化 - SaveType: {saveType}, ClientInstanceId: {_currentClientInstanceId}");
            LoadProgressData();
        }
        
        /// <summary>
        /// 初始化管理器（旧接口，默认使用单人存档）
        /// </summary>
        [System.Obsolete("使用 Initialize(SaveSystem.SaveType, string) 替代")]
        public void Initialize()
        {
            Initialize(SaveSystem.SaveType.Local);
        }
        
        /// <summary>
        /// 将存档数据应用到实体
        /// </summary>
        public void ApplyProgressToEntity(Entity entity)
        {
            if (entity == null)
            {
                ASLogger.Instance.Warning("PlayerDataManager: ApplyProgressToEntity - 实体为空");
                return;
            }

            var data = EnsureProgressData();
            EnsureDataIntegrity(data);

            // 等级与经验
            var levelComp = entity.GetComponent<LevelComponent>();
            if (levelComp == null)
            {
                levelComp = new LevelComponent();
                entity.AddComponent(levelComp);
            }
            levelComp.CurrentLevel = data.Level;
            levelComp.CurrentExp = data.Exp;
            if (data.ExpToNextLevel > 0)
            {
                levelComp.ExpToNextLevel = data.ExpToNextLevel;
            }
            if (data.MaxLevel > 0)
            {
                levelComp.MaxLevel = data.MaxLevel;
            }

            // 自由加点
            var growthComp = entity.GetComponent<GrowthComponent>();
            if (growthComp == null)
            {
                growthComp = new GrowthComponent();
                entity.AddComponent(growthComp);
            }
            growthComp.RoleId = data.RoleId;
            growthComp.AvailableStatPoints = data.AvailableStatPoints;
            growthComp.AllocatedAttackPoints = data.AllocatedAttackPoints;
            growthComp.AllocatedDefensePoints = data.AllocatedDefensePoints;
            growthComp.AllocatedHealthPoints = data.AllocatedHealthPoints;
            growthComp.AllocatedSpeedPoints = data.AllocatedSpeedPoints;

            // 重建基础与派生属性
            var baseStats = entity.GetComponent<BaseStatsComponent>();
            if (baseStats != null)
            {
                baseStats.InitializeFromConfig(entity.EntityConfigId, levelComp.CurrentLevel);
                baseStats.ApplyAllocatedPoints(growthComp);
            }

            var derivedStats = entity.GetComponent<DerivedStatsComponent>();
            if (derivedStats != null && baseStats != null)
            {
                derivedStats.RecalculateAll(baseStats);
            }

            var dynamicStats = entity.GetComponent<DynamicStatsComponent>();
            if (dynamicStats != null && derivedStats != null)
            {
                dynamicStats.InitializeResources(derivedStats);
            }

            // 货币
            var currencyComp = entity.GetComponent<CurrencyComponent>();
            if (currencyComp == null)
            {
                currencyComp = new CurrencyComponent();
                entity.AddComponent(currencyComp);
            }
            currencyComp.Balances = CloneCurrencyDictionary(data.Currencies);

            // 背包
            var inventoryComp = entity.GetComponent<InventoryComponent>();
            if (inventoryComp == null)
            {
                inventoryComp = new InventoryComponent();
                entity.AddComponent(inventoryComp);
            }
            inventoryComp.Items = CloneInventoryList(data.Inventory);

            ASLogger.Instance.Info($"PlayerDataManager: 已将存档应用到实体 {entity.UniqueId}");
        }

        /// <summary>
        /// 从实体快照当前进度
        /// </summary>
        public void CaptureProgressFromEntity(Entity entity)
        {
            if (entity == null)
            {
                ASLogger.Instance.Warning("PlayerDataManager: CaptureProgressFromEntity - 实体为空");
                return;
            }

            var data = EnsureProgressData();
            EnsureDataIntegrity(data);

            var levelComp = entity.GetComponent<LevelComponent>();
            if (levelComp != null)
            {
                data.Level = levelComp.CurrentLevel;
                data.Exp = levelComp.CurrentExp;
                data.ExpToNextLevel = levelComp.ExpToNextLevel;
                data.MaxLevel = levelComp.MaxLevel;
            }

            var growthComp = entity.GetComponent<GrowthComponent>();
            if (growthComp != null)
            {
                data.RoleId = growthComp.RoleId != 0 ? growthComp.RoleId : data.RoleId;
                data.AvailableStatPoints = growthComp.AvailableStatPoints;
                data.AllocatedAttackPoints = growthComp.AllocatedAttackPoints;
                data.AllocatedDefensePoints = growthComp.AllocatedDefensePoints;
                data.AllocatedHealthPoints = growthComp.AllocatedHealthPoints;
                data.AllocatedSpeedPoints = growthComp.AllocatedSpeedPoints;
            }

            var currencyComp = entity.GetComponent<CurrencyComponent>();
            data.Currencies = currencyComp != null
                ? CloneCurrencyDictionary(currencyComp.Balances)
                : new Dictionary<CurrencyType, long>();

            var inventoryComp = entity.GetComponent<InventoryComponent>();
            data.Inventory = inventoryComp != null
                ? CloneInventoryList(inventoryComp.Items)
                : new List<ItemStack>();

            ASLogger.Instance.Info($"PlayerDataManager: 已从实体 {entity.UniqueId} 拍摄存档快照");
        }
        
        /// <summary>
        /// 保存玩家进度数据
        /// </summary>
        public void SaveProgressData(Entity entity = null)
        {
            if (entity != null)
            {
                CaptureProgressFromEntity(entity);
            }

            if (_progressData == null)
            {
                ASLogger.Instance.Warning("PlayerDataManager: 进度数据为空，无法保存");
                return;
            }

            EnsureDataIntegrity(_progressData);
            SaveSystem.SavePlayerProgressData(_progressData, _currentSaveType, _currentClientInstanceId);
        }
        
        /// <summary>
        /// 加载玩家进度数据
        /// </summary>
        public void LoadProgressData()
        {
            _progressData = SaveSystem.LoadPlayerProgressData(_currentSaveType, _currentClientInstanceId);
            if (_progressData == null)
            {
                _progressData = CreateDefaultProgressData();
                ASLogger.Instance.Info("PlayerDataManager: 创建默认进度数据");
            }
            else
            {
                EnsureDataIntegrity(_progressData);
                ASLogger.Instance.Info($"PlayerDataManager: 加载进度数据 - 等级 {_progressData.Level}, 经验 {_progressData.Exp}");
            }
        }
        
        /// <summary>
        /// 创建默认进度数据
        /// </summary>
        private PlayerProgressData CreateDefaultProgressData()
        {
            var data = new PlayerProgressData
            {
                Level = 1,
                Exp = 0,
                ExpToNextLevel = 1000,
                MaxLevel = 100,
                RoleId = 1001,
                AvailableStatPoints = 0,
                StarFragments = 0
            };
            EnsureDataIntegrity(data);
            return data;
        }

        private PlayerProgressData EnsureProgressData()
        {
            return _progressData ??= CreateDefaultProgressData();
        }

        private static void EnsureDataIntegrity(PlayerProgressData data)
        {
            data.Resources ??= new Dictionary<string, int>();
            data.Currencies ??= new Dictionary<CurrencyType, long>();
            data.Inventory ??= new List<ItemStack>();

            if (!data.Currencies.ContainsKey(CurrencyType.Gold))
            {
                data.Currencies[CurrencyType.Gold] = 0;
            }
            if (!data.Currencies.ContainsKey(CurrencyType.Diamond))
            {
                data.Currencies[CurrencyType.Diamond] = 0;
            }
            if (!data.Currencies.ContainsKey(CurrencyType.Voucher))
            {
                data.Currencies[CurrencyType.Voucher] = 0;
            }

            if (data.MaxLevel <= 0)
            {
                data.MaxLevel = 100;
            }
            if (data.ExpToNextLevel <= 0)
            {
                data.ExpToNextLevel = 1000;
            }
        }

        private static Dictionary<CurrencyType, long> CloneCurrencyDictionary(Dictionary<CurrencyType, long> source)
        {
            return source != null
                ? new Dictionary<CurrencyType, long>(source)
                : new Dictionary<CurrencyType, long>();
        }

        private static List<ItemStack> CloneInventoryList(IEnumerable<ItemStack> source)
        {
            var result = new List<ItemStack>();
            if (source == null)
            {
                return result;
            }

            foreach (var item in source)
            {
                if (item == null) continue;
                Dictionary<string, string>? metadataCopy = null;
                if (item.Metadata != null)
                {
                    metadataCopy = new Dictionary<string, string>(item.Metadata);
                }

                result.Add(new ItemStack(item.ItemId, item.Count, metadataCopy));
            }

            return result;
        }
    }
}

