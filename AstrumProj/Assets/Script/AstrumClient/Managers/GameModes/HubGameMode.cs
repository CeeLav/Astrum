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
        public override Room MainRoom { get; set; }        // Hub 不使用 Room
        public override Stage MainStage { get; set; }      // Hub 不使用 Stage
        public override long PlayerId { get; set; }
        public override string ModeName => "Hub";
        public override bool IsRunning { get; set; }
        
        // Hub 特定场景名称
        private const string HubSceneName = "HubScene";
        
        /// <summary>
        /// 初始化 Hub 游戏模式
        /// </summary>
        public override void Initialize()
        {
            ASLogger.Instance.Info("HubGameMode: 初始化 Hub 模式");
            ChangeState(GameModeState.Initializing);
            
            // 订阅事件
            SubscribeToEvents();
            
            // Hub 模式不需要提前加载数据
            // 数据由 PlayerDataManager 统一管理
            
            ChangeState(GameModeState.Ready);
            IsRunning = true;
            
            ASLogger.Instance.Info("HubGameMode: 初始化完成");
            
            MonitorManager.Register(this); // 注册到全局监控
        }
        
        /// <summary>
        /// 启动 Hub 游戏（加载场景）
        /// </summary>
        public override void StartGame(string sceneName)
        {
            ASLogger.Instance.Info($"HubGameMode: 启动游戏 - 场景: {sceneName ?? HubSceneName}");
            
            // 加载 HubScene 场景
            var targetScene = sceneName ?? HubSceneName;
            SceneManager.Instance.LoadSceneAsync(targetScene, OnSceneLoaded);
        }
        
        /// <summary>
        /// 场景加载完成回调
        /// </summary>
        private void OnSceneLoaded()
        {
            ASLogger.Instance.Info("HubGameMode: 场景加载完成，显示 Hub UI");
            
            // 显示 Hub UI
            UIManager.Instance.ShowUI("HubView");
            
            ChangeState(GameModeState.Playing);
        }
        
        /// <summary>
        /// 更新 Hub 游戏模式
        /// </summary>
        public override void Update(float deltaTime)
        {
            // Hub 模式主要是 UI 界面，无需每帧更新
            // 未来添加昼夜循环等功能时再实现
        }
        
        /// <summary>
        /// 关闭 Hub 游戏模式
        /// </summary>
        public override void Shutdown()
        {
            ASLogger.Instance.Info("HubGameMode: 关闭 Hub 模式");
            ChangeState(GameModeState.Ending);
            
            // 取消订阅事件
            UnsubscribeFromEvents();
            
            // 保存玩家数据
            PlayerDataManager.Instance.SaveProgressData();
            
            IsRunning = false;
            
            ChangeState(GameModeState.Finished);
        }
        
        /// <summary>
        /// 订阅事件
        /// </summary>
        private void SubscribeToEvents()
        {
            EventSystem.Instance.Subscribe<StartExplorationRequestEventData>(OnStartExplorationRequested);
        }
        
        /// <summary>
        /// 取消订阅事件
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            EventSystem.Instance.Unsubscribe<StartExplorationRequestEventData>(OnStartExplorationRequested);
        }
        
        /// <summary>
        /// 开始探索请求事件处理
        /// </summary>
        private void OnStartExplorationRequested(StartExplorationRequestEventData eventData)
        {
            ASLogger.Instance.Info("HubGameMode: 收到开始探索请求");
            StartExploration();
        }
        
        /// <summary>
        /// 启动探索
        /// </summary>
        private void StartExploration()
        {
            ASLogger.Instance.Info("HubGameMode: 启动探索");
            
            try
            {
                // 切换到 SinglePlayerGameMode
                GameDirector.Instance.SwitchGameMode(GameModeType.SinglePlayer);
                
                // StartGame 会在后续调用
                GameDirector.Instance.StartGame("DungeonsGame");
                
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

