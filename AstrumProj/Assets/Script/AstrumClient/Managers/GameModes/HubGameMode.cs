using System;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.Client.Data;
using Astrum.Client.Managers;
using Astrum.LogicCore.Core;
using Astrum.View.Core;
using AstrumClient.MonitorTools;

namespace Astrum.Client.Managers.GameModes
{
    /// <summary>
    /// Hub 游戏模式 - 主世界基地管理
    /// </summary>
    [MonitorTarget]
    public class HubGameMode : BaseGameMode
    {
        // 继承属性
        public override Room MainRoom { get; set; }        // Hub 不使用Room
        public override Stage MainStage { get; set; }      // Hub 不使用Stage
        public override long PlayerId { get; set; }
        public override string ModeName => "Hub";
        public override bool IsRunning { get; set; }
        
        /// <summary>
        /// 游戏模式类型
        /// </summary>
        public override GameModeType ModeType => GameModeType.Hub;
        
        /// <summary>
        /// 初始�?Hub 游戏模式
        /// </summary>
        public override void Initialize()
        {
            ASLogger.Instance.Info("HubGameMode: 初始�?Hub 模式");
            
            // Hub 模式不需要提前加载数�?            // 数据�?PlayerDataManager 统一管理
            
            IsRunning = true;
            
            // 触发状态流转：Initializing -> Loading -> Ready
            TriggerStateEnter();
            
            ASLogger.Instance.Info("HubGameMode: 初始化完成");
            
            MonitorManager.Register(this); // 注册到全局监控
        }
        
        /// <summary>
        /// 加载逻辑（Hub 模式不需要加载，立即完成�?
        /// </summary>
        protected override void OnLoading(int sceneId)
        {
            var targetSceneId = sceneId > 0 ? sceneId : GameSetting.Instance.HubSceneId;
            ASLogger.Instance.Info($"HubGameMode: 开始加载场景 - 场景ID: {targetSceneId}");
            
            // 异步加载 HubScene 场景
            SceneManager.Instance.LoadSceneAsync(targetSceneId, OnSceneLoadedForLoading);
        }
        
        /// <summary>
        /// 场景加载完成回调（用于加载状态）
        /// </summary>
        private void OnSceneLoadedForLoading()
        {
            ASLogger.Instance.Info("HubGameMode: 场景加载完成");
            
            // 调用 CompleteLoading() 进入 Playing 状态
            CompleteLoading();
        }
        
        /// <summary>
        /// 启动游戏逻辑（加载场景）
        /// </summary>
        /// <param name="sceneId">场景ID</param>
        protected override void OnStartGame(int sceneId)
        {
            var targetSceneId = sceneId > 0 ? sceneId : GameSetting.Instance.HubSceneId;
            ASLogger.Instance.Info($"HubGameMode: 启动游戏 - 场景ID: {targetSceneId}");
            OnSceneLoaded();
        }
        
        /// <summary>
        /// 场景加载完成回调
        /// </summary>
        private void OnSceneLoaded()
        {
            ASLogger.Instance.Info("HubGameMode: 场景加载完成，显�?Hub UI");
            
            // 显示 Hub UI
            UIManager.Instance.ShowUI("Hub/Hub");
        }
        
        /// <summary>
        /// 更新 Hub 游戏模式
        /// </summary>
        public override void Update(float deltaTime)
        {
            // Hub 模式主要是UI 界面，无需每帧更新
            // 未来添加昼夜循环等功能时再实现
        }
        
        /// <summary>
        /// 关闭 Hub 游戏模式
        /// </summary>
        public override void Shutdown()
        {
            ASLogger.Instance.Info("HubGameMode: 关闭 Hub 模式");
            ChangeState(GameModeState.Ending);
            
            // 隐藏 Hub UI
            HideHubView();
            
            // 保存玩家数据
            PlayerDataManager.Instance.SaveProgressData();
            
            IsRunning = false;
            
            ChangeState(GameModeState.Finished);
        }
        
        /// <summary>
        /// 隐藏 Hub UI
        /// </summary>
        private void HideHubView()
        {
            try
            {
                ASLogger.Instance.Info("HubGameMode: 隐藏Hub");
                UIManager.Instance?.HideUI("Hub/Hub");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"HubGameMode: 隐藏Hub失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 启动探索（公开方法，供 UI 直接调用�?        /// </summary>
        public void StartExploration()
        {
            ASLogger.Instance.Info("HubGameMode: 启动探索");
            
            try
            {
                // 切换�?SinglePlayerGameMode
                // 切换到单人游戏模式（单人模式会自动根据配置启动对应场景）
                GameDirector.Instance.SwitchGameMode(GameModeType.SinglePlayer);
                
                ASLogger.Instance.Info("HubGameMode: 探索启动成功");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"HubGameMode: 启动探索失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        protected override GameModeConfig CreateDefaultConfig()
        {
            return GameModeConfig.CreateDefault("Hub");
        }
    }
}

