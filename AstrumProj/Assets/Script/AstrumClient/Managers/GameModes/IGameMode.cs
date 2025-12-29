using System;
using Astrum.LogicCore.Core;
using Astrum.View.Core;
using Astrum.CommonBase;
using Astrum.Client.Core;

namespace Astrum.Client.Managers.GameModes
{
    /// <summary>
    /// 游戏模式接口 - 定义所有游戏模式的通用行为
    /// </summary>
    public interface IGameMode
    {
        // 原有接口保持不变
        /// <summary>
        /// 初始化游戏模式
        /// </summary>
        void Initialize();
        
        // StartGame 方法已移除，由游戏模式自动根据配置加载场景
        
        /// <summary>
        /// 更新游戏逻辑
        /// </summary>
        /// <param name="deltaTime">时间差</param>
        void Update(float deltaTime);
        
        /// <summary>
        /// 关闭游戏模式
        /// </summary>
        void Shutdown();
        
        // 新增状态管理
        /// <summary>
        /// 当前游戏模式状态
        /// </summary>
        GameModeState CurrentState { get; }
        
        /// <summary>
        /// 状态进入时调用
        /// </summary>
        /// <param name="state">进入的状态</param>
        void OnStateEnter(GameModeState state);
        
        /// <summary>
        /// 状态退出时调用
        /// </summary>
        /// <param name="state">退出的状态</param>
        void OnStateExit(GameModeState state);
        
        /// <summary>
        /// 检查是否可以转换到目标状态
        /// </summary>
        /// <param name="targetState">目标状态</param>
        /// <returns>是否可以转换</returns>
        bool CanTransitionTo(GameModeState targetState);
        
        // 新增事件处理（基于现有 EventSystem）
        /// <summary>
        /// 处理游戏事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        void OnGameEvent(EventData eventData);
        
        /// <summary>
        /// 注册事件处理器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        void RegisterEventHandler<T>(Action<T> handler) where T : EventData;
        
        /// <summary>
        /// 取消注册事件处理器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        void UnregisterEventHandler<T>(Action<T> handler) where T : EventData;
        
        // 新增配置管理
        /// <summary>
        /// 获取配置
        /// </summary>
        /// <returns>游戏模式配置</returns>
        GameModeConfig GetConfig();
        
        /// <summary>
        /// 应用配置
        /// </summary>
        /// <param name="config">配置</param>
        void ApplyConfig(GameModeConfig config);
        
        /// <summary>
        /// 保存配置
        /// </summary>
        void SaveConfig();
        
        /// <summary>
        /// 加载配置
        /// </summary>
        void LoadConfig();
        
        // 原有属性
        /// <summary>
        /// 主游戏房间
        /// </summary>
        Room MainRoom { get; }
        
        /// <summary>
        /// 主游戏舞台
        /// </summary>
        Stage MainStage { get; }
        
        /// <summary>
        /// 主玩家ID
        /// </summary>
        long PlayerId { get; }
        
        /// <summary>
        /// 模式名称
        /// </summary>
        string ModeName { get; }
        
        /// <summary>
        /// 是否正在运行
        /// </summary>
        bool IsRunning { get; }
    }
}

