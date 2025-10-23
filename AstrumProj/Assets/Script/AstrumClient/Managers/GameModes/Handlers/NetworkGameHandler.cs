using System;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.Generated;
using Astrum.LogicCore.Core;
using Astrum.View.Core;

namespace Astrum.Client.Managers.GameModes.Handlers
{
    /// <summary>
    /// 网络游戏流程处理器 - 处理游戏开始/结束/状态更新
    /// </summary>
    public class NetworkGameHandler
    {
        private readonly MultiplayerGameMode _gameMode;
        
        public NetworkGameHandler(MultiplayerGameMode gameMode)
        {
            _gameMode = gameMode;
        }
        
        /// <summary>
        /// 处理游戏响应
        /// </summary>
        public void OnGameResponse(GameResponse response)
        {
            try
            {
                ASLogger.Instance.Info(
                    $"NetworkGameHandler: 收到游戏响应 - Success: {response.success}, Message: {response.message}");
                
                if (response.success)
                {
                    ASLogger.Instance.Info($"游戏操作成功: {response.message}");
                }
                else
                {
                    ASLogger.Instance.Warning($"游戏操作失败: {response.message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理游戏响应时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理游戏开始通知
        /// </summary>
        public void OnGameStartNotification(GameStartNotification notification)
        {
            try
            {
                ASLogger.Instance.Info($"NetworkGameHandler: 收到游戏开始通知 - 房间: {notification.roomId}");
                
                // 创建游戏Room和Stage
                var room = CreateGameRoom(notification);
                var world = new World();
                room.MainWorld = world;
                room.Initialize();
                var stage = CreateGameStage(room);
                
                // 设置当前游戏状态
                _gameMode.SetRoomAndStage(room, stage);
                
                // 切换到游戏场景
                SwitchToGameScene("DungeonsGame", stage);
                
                ASLogger.Instance.Info($"游戏开始 - 房间: {notification.roomId}, 玩家数: {notification.playerIds.Count}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理游戏开始通知时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理游戏结束通知
        /// </summary>
        public void OnGameEndNotification(GameEndNotification notification)
        {
            try
            {
                ASLogger.Instance.Info(
                    $"NetworkGameHandler: 收到游戏结束通知 - 房间: {notification.roomId}, 原因: {notification.reason}");
                
                // 显示游戏结果
                ShowGameResult(notification.result);
                
                // 清理游戏状态
                _gameMode.ClearRoomAndStage();
                
                // 返回房间列表
                ReturnToRoomList();
                
                ASLogger.Instance.Info($"游戏结束 - 房间: {notification.roomId}");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理游戏结束通知时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 处理游戏状态更新
        /// </summary>
        public void OnGameStateUpdate(GameStateUpdate update)
        {
            try
            {
                ASLogger.Instance.Info(
                    $"NetworkGameHandler: 收到游戏状态更新 - 房间: {update.roomId}, 回合: {update.roomState.currentRound}");
                
                // 更新游戏状态
                if (_gameMode.MainRoom != null)
                {
                    ASLogger.Instance.Info(
                        $"游戏状态更新 - 当前回合: {update.roomState.currentRound}/{update.roomState.maxRounds}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"处理游戏状态更新时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        #region 私有方法
        
        /// <summary>
        /// 创建游戏房间
        /// </summary>
        private Room CreateGameRoom(GameStartNotification notification)
        {
            var room = new Room(1, notification.roomId);
            ASLogger.Instance.Info($"创建游戏房间: {room.RoomId}");
            return room;
        }
        
        /// <summary>
        /// 创建游戏舞台
        /// </summary>
        private Stage CreateGameStage(Room room)
        {
            var stage = new Stage("GameStage", "游戏场景");
            stage.Initialize();
            stage.SetRoom(room);
            ASLogger.Instance.Info("创建游戏舞台");
            return stage;
        }
        
        /// <summary>
        /// 切换到游戏场景
        /// </summary>
        private void SwitchToGameScene(string sceneName, Stage stage)
        {
            ASLogger.Instance.Info($"NetworkGameHandler: 切换到游戏场景 {sceneName}");
            
            var sceneManager = SceneManager.Instance;
            if (sceneManager == null)
            {
                throw new Exception("SceneManager不存在");
            }
            
            // 设置当前Stage
            StageManager.Instance.SetCurrentStage(stage);
            
            // 异步加载游戏场景
            sceneManager.LoadSceneAsync(sceneName, () => { OnGameSceneLoaded(stage); });
        }
        
        /// <summary>
        /// 游戏场景加载完成回调
        /// </summary>
        private void OnGameSceneLoaded(Stage stage)
        {
            ASLogger.Instance.Info("NetworkGameHandler: 游戏场景加载完成");
            
            // 关闭Login UI
            CloseLoginUI();
            
            // 激活Stage
            stage.SetActive(true);
            stage.OnEnter();
            
            // 订阅EntityView创建事件，用于设置相机跟随
            stage.OnEntityViewAdded += _gameMode.OnEntityViewAdded;
            
            // 联机模式：请求创建玩家
            _gameMode.RequestCreatePlayer();
            
            ASLogger.Instance.Info("NetworkGameHandler: 游戏准备完成");
        }
        
        /// <summary>
        /// 关闭Login UI
        /// </summary>
        private void CloseLoginUI()
        {
            try
            {
                var uiManager = UIManager.Instance;
                if (uiManager != null)
                {
                    uiManager.HideUI("Login");
                    uiManager.DestroyUI("Login");
                    uiManager.DestroyUI("RoomList");
                    uiManager.DestroyUI("RoomDetail");
                    ASLogger.Instance.Info("NetworkGameHandler: Login UI已关闭");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"NetworkGameHandler: 关闭Login UI失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 显示游戏结果
        /// </summary>
        private void ShowGameResult(GameResult result)
        {
            try
            {
                ASLogger.Instance.Info($"游戏结果 - 房间: {result.roomId}");
                
                foreach (var playerResult in result.playerResults)
                {
                    ASLogger.Instance.Info(
                        $"玩家 {playerResult.playerId}: 分数 {playerResult.score}, 排名 {playerResult.rank}, 获胜: {playerResult.isWinner}");
                }
                
                // TODO: 实现游戏结果UI显示
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"显示游戏结果时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        /// <summary>
        /// 返回房间列表
        /// </summary>
        private void ReturnToRoomList()
        {
            try
            {
                // TODO: 实现返回房间列表的UI切换
                ASLogger.Instance.Info("返回房间列表");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"返回房间列表时出错: {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }
        
        #endregion
    }
}

