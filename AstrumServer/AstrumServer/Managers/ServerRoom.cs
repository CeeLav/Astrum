using System;
using System.Collections.Generic;
using Astrum.Generated;
using Astrum.CommonBase;
using AstrumServer.Network;

namespace AstrumServer.Managers
{
    /// <summary>
    /// 服务器房间实例 - 封装房间的所有状态和行为
    /// </summary>
    public class ServerRoom
    {
        // 房间信息
        public RoomInfo Info { get; private set; }
        
        // 游戏会话
        private GameSession? _gameSession;
        
        // 玩家列表
        private readonly List<string> _playerIds = new();
        
        // 依赖注入
        private readonly ServerNetworkManager _networkManager;
        private readonly UserManager _userManager;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public ServerRoom(
            RoomInfo roomInfo,
            ServerNetworkManager networkManager,
            UserManager userManager)
        {
            Info = roomInfo;
            _networkManager = networkManager;
            _userManager = userManager;
            
            // 初始化玩家列表
            if (roomInfo.PlayerNames != null)
            {
                _playerIds.AddRange(roomInfo.PlayerNames);
            }
        }
        
        /// <summary>
        /// 房间是否激活
        /// </summary>
        public bool IsActive => Info != null && Info.CurrentPlayers > 0;
        
        /// <summary>
        /// 房间是否在游戏中
        /// </summary>
        public bool IsPlaying => Info != null && Info.Status == 1; // 1=游戏中
        
        /// <summary>
        /// 添加玩家到房间
        /// </summary>
        public bool AddPlayer(string userId)
        {
            if (_playerIds.Contains(userId))
            {
                ASLogger.Instance.Warning($"玩家 {userId} 已在房间 {Info.Id} 中");
                return false;
            }
            
            if (Info.CurrentPlayers >= Info.MaxPlayers)
            {
                ASLogger.Instance.Warning($"房间 {Info.Id} 已满，无法添加玩家 {userId}");
                return false;
            }
            
            _playerIds.Add(userId);
            Info.CurrentPlayers++;
            Info.PlayerNames.Add(userId);
            
            ASLogger.Instance.Info($"玩家 {userId} 加入房间 {Info.Id} (Current: {Info.CurrentPlayers}/{Info.MaxPlayers})");
            return true;
        }
        
        /// <summary>
        /// 从房间移除玩家
        /// </summary>
        public bool RemovePlayer(string userId)
        {
            if (!_playerIds.Contains(userId))
            {
                ASLogger.Instance.Warning($"玩家 {userId} 不在房间 {Info.Id} 中");
                return false;
            }
            
            _playerIds.Remove(userId);
            Info.CurrentPlayers--;
            Info.PlayerNames.Remove(userId);
            
            ASLogger.Instance.Info($"玩家 {userId} 离开房间 {Info.Id} (Current: {Info.CurrentPlayers}/{Info.MaxPlayers})");
            return true;
        }
        
        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            if (Info.Status != 0) // 0=等待中
            {
                ASLogger.Instance.Warning($"房间 {Info.Id} 状态不是等待中，无法开始游戏 (Status: {Info.Status})");
                return;
            }
            
            // 更新房间状态
            Info.Status = 1; // 1=游戏中
            Info.GameStartTime = TimeInfo.Instance.ClientNow();
            Info.GameEndTime = 0;
            
            // 创建游戏会话
            if (_gameSession == null)
            {
                _gameSession = new GameSession(this, _networkManager, _userManager);
            }
            
            // 开始游戏会话（包括帧同步）
            _gameSession.Start();
            
            ASLogger.Instance.Info($"房间 {Info.Id} 开始游戏，玩家数: {Info.CurrentPlayers}");
        }
        
        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndGame(string reason = "游戏结束")
        {
            if (Info.Status != 1) // 1=游戏中
            {
                ASLogger.Instance.Warning($"房间 {Info.Id} 不在游戏中，无法结束游戏 (Status: {Info.Status})");
                return;
            }
            
            // 停止游戏会话
            _gameSession?.Stop(reason);
            
            // 更新房间状态
            Info.Status = 2; // 2=已结束
            Info.GameEndTime = TimeInfo.Instance.ClientNow();
            
            ASLogger.Instance.Info($"房间 {Info.Id} 结束游戏，原因: {reason}，持续时间: {Info.GameEndTime - Info.GameStartTime}ms");
        }
        
        /// <summary>
        /// 更新房间状态（由 RoomManager 统一调用）
        /// </summary>
        public void Update()
        {
            // 更新游戏会话
            _gameSession?.Update();
        }
        
        /// <summary>
        /// 处理玩家输入
        /// </summary>
        public void HandleInput(string userId, SingleInput input)
        {
            _gameSession?.HandleInput(userId, input);
        }
        
        /// <summary>
        /// 获取游戏会话
        /// </summary>
        public GameSession? GetGameSession()
        {
            return _gameSession;
        }
        
        /// <summary>
        /// 获取逻辑房间实例（用于外部访问）
        /// </summary>
        public Astrum.LogicCore.Core.Room? GetLogicRoom()
        {
            return _gameSession?.LogicRoom;
        }
        
        /// <summary>
        /// 重置房间状态（用于重新开始游戏）
        /// </summary>
        public void Reset()
        {
            // 停止游戏会话
            _gameSession?.Stop("重置房间");
            _gameSession = null;
            
            // 重置房间状态
            Info.Status = 0; // 0=等待中
            Info.GameStartTime = 0;
            Info.GameEndTime = 0;
            
            ASLogger.Instance.Info($"房间 {Info.Id} 状态已重置");
        }
    }
}

